// svgimage.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"encoding/xml"
	"fmt"
	"io"
	"math"
	"os"
	"strconv"
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/capstyle"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/internal/fastfloat"
	"github.com/edragoev1/pdfjet/v9/src/joinstyle"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// SVGImage is used to draw svgParser images on a page.
type SVGImage struct {
	x, y, w, h          float32
	viewBox             string
	preserveAspectRatio string
	paths               []*svgPath
	uri                 string
	key                 string
	language            string
	altDescription      string
	actualText          string
}

// NewSVGImageFromFile reads and parses an svgParser image from a file.
// The file is fully read before parsing, so no file handle lifecycle is involved.
func NewSVGImageFromFile(filePath string) (*SVGImage, error) {
	data, err := os.ReadFile(filePath)
	if err != nil {
		return nil, fmt.Errorf("reading SVG %q: %w", filePath, err)
	}
	return NewSVGImage(bytes.NewReader(data))
}

// svgTemplates are the elements whose content is not drawn where it is, but
// used by other elements, which PDFjet does not draw.
var svgTemplates = map[string]bool{
	"defs": true, "clipPath": true, "mask": true, "pattern": true, "symbol": true,
	"marker": true, "linearGradient": true, "radialGradient": true,
}

// NewSVGImage parses an svgParser image from a reader, for embedding in a PDF document.
// It draws the <path>, <rect>, <circle>, <ellipse>, <line>, <polyline> and
// <polygon> elements, in groups or not, with their transforms, and the fill,
// stroke, stroke-width, fill-rule, stroke-linecap, stroke-linejoin, opacity,
// fill-opacity and stroke-opacity they have or inherit, as attributes, in a
// style attribute or in rules for their classes in a <style> element that
// comes before them. The width, height, viewBox and preserveAspectRatio of
// the <svg> element give the size of the image.
func NewSVGImage(reader io.Reader) (*SVGImage, error) {
	image := new(SVGImage)
	colorMap := newColorMap()
	image.paths = make([]*svgPath, 0)
	rules := make([]svgRule, 0)
	stack := []svgState{newSVGState()}
	names := make([]string, 0)
	var styleSheet strings.Builder
	root := true

	decoder := xml.NewDecoder(reader)
	for {
		token, err := decoder.Token()
		if err == io.EOF {
			break
		}
		if err != nil {
			return nil, fmt.Errorf("parsing SVG: %w", err)
		}

		switch token := token.(type) {
		case xml.StartElement:
			name := token.Name.Local
			attributes := make(map[string]string)
			order := make([]string, 0, len(token.Attr))
			for _, attr := range token.Attr {
				if attr.Name.Space == "" {
					attributes[attr.Name.Local] = attr.Value
					order = append(order, attr.Name.Local)
				}
			}
			state, err := elementState(stack[len(stack)-1], colorMap, rules, attributes, order)
			if err != nil {
				return nil, err
			}
			if svgTemplates[name] {
				state.hidden = true
			}
			stack = append(stack, state)
			names = append(names, name)

			if name == "svg" && root {
				root = false
				image.w = parseLength(attributes["width"])
				image.h = parseLength(attributes["height"])
				image.viewBox = attributes["viewBox"]
				image.preserveAspectRatio = attributes["preserveAspectRatio"]
			}
			operations, err := shapeOperations(name, attributes)
			if err != nil {
				return nil, err
			}
			if name == "line" {
				state.fill = color.Transparent // A line has no inside to fill
				state.fillCurrent = false
			}
			image.addPath(&state, operations)

		case xml.EndElement:
			if len(names) > 0 && names[len(names)-1] == "style" {
				rules = append(rules, parseSVGStyleSheet(styleSheet.String())...)
				styleSheet.Reset()
			}
			if len(stack) > 1 {
				stack = stack[:len(stack)-1]
				names = names[:len(names)-1]
			}

		case xml.CharData:
			if len(names) > 0 && names[len(names)-1] == "style" {
				styleSheet.Write(token)
			}
		}
	}

	if err := image.processPaths(image.paths); err != nil {
		return nil, err
	}
	return image, nil
}

// elementState returns the state of an element whose parent has the given
// one: its presentation attributes, in their order, then the rules of the
// style sheet for its classes, in the order of the style sheet, then its
// style attribute, and its transform.
func elementState(parent svgState, colorMap map[string]int32, rules []svgRule,
	attributes map[string]string, order []string) (svgState, error) {
	state := parent
	state.ownOpacity = 1.0
	for _, name := range order {
		if err := state.setProperty(colorMap, name, attributes[name]); err != nil {
			return state, err
		}
	}
	if classes := strings.Fields(attributes["class"]); len(classes) > 0 {
		for _, rule := range rules {
			for _, class := range classes {
				if class != rule.class {
					continue
				}
				for _, declaration := range rule.declarations {
					if err := state.setProperty(colorMap, declaration[0], declaration[1]); err != nil {
						return state, err
					}
				}
				break
			}
		}
	}
	for _, declaration := range parseSVGDeclarations(attributes["style"]) {
		if err := state.setProperty(colorMap, declaration[0], declaration[1]); err != nil {
			return state, err
		}
	}
	state.opacity *= state.ownOpacity
	if transform, ok := attributes["transform"]; ok {
		if matrix, ok := parseSVGTransform(transform); ok {
			state.matrix = svgMultiply(parent.matrix, matrix)
		}
	}
	return state, nil
}

// svgKappa is how far the control points of the cubic curve that draws a
// quarter of a circle are from its ends, for a radius of 1.
const svgKappa = 0.5522847498307936

// svgBuilder builds the PDF path operations of a shape.
type svgBuilder struct {
	operations []*svgPathOp
	x0, y0     float64 // The start of the subpath
}

func (b *svgBuilder) moveTo(x, y float64) {
	b.x0 = x
	b.y0 = y
	b.operations = append(b.operations, newSVGPathOpXY('M', float32(x), float32(y)))
}

func (b *svgBuilder) lineTo(x, y float64) {
	b.operations = append(b.operations, newSVGPathOpXY('L', float32(x), float32(y)))
}

func (b *svgBuilder) curveTo(x1, y1, x2, y2, x, y float64) {
	op := newSVGPathOp('C')
	op.setCubicPoints(float32(x1), float32(y1), float32(x2), float32(y2), float32(x), float32(y))
	b.operations = append(b.operations, op)
}

func (b *svgBuilder) closePath() {
	b.operations = append(b.operations, newSVGPathOpXY('Z', float32(b.x0), float32(b.y0)))
}

// ellipse adds the ellipse as four cubic curves, from its right end.
func (b *svgBuilder) ellipse(cx, cy, rx, ry float64) {
	kx := svgKappa * rx
	ky := svgKappa * ry
	b.moveTo(cx+rx, cy)
	b.curveTo(cx+rx, cy+ky, cx+kx, cy+ry, cx, cy+ry)
	b.curveTo(cx-kx, cy+ry, cx-rx, cy+ky, cx-rx, cy)
	b.curveTo(cx-rx, cy-ky, cx-kx, cy-ry, cx, cy-ry)
	b.curveTo(cx+kx, cy-ry, cx+rx, cy-ky, cx+rx, cy)
	b.closePath()
}

// shapeOperations returns the PDF path operations of an element that draws a
// path or a basic shape, in its user space, and none for another element or
// a shape of no size. It returns an error for path data that is not numbers.
func shapeOperations(name string, attributes map[string]string) ([]*svgPathOp, error) {
	number := func(name string) float64 {
		return parseSVGCoordinate(attributes[name])
	}
	b := new(svgBuilder)
	switch name {
	case "path":
		data := attributes["d"]
		operations, err := toPDF(newSVGParser().getOperations(data))
		if err != nil {
			return nil, fmt.Errorf("invalid path data %q: %w", data, err)
		}
		return operations, nil
	case "rect":
		x, y, w, h := number("x"), number("y"), number("width"), number("height")
		if w <= 0.0 || h <= 0.0 {
			return nil, nil
		}
		// A radius that is not given is the other one, and neither is more
		// than half the side.
		rx, rxSet := radius(attributes["rx"])
		ry, rySet := radius(attributes["ry"])
		if !rxSet {
			rx = ry
		}
		if !rySet {
			ry = rx
		}
		rx = math.Min(rx, w/2.0)
		ry = math.Min(ry, h/2.0)
		if rx <= 0.0 || ry <= 0.0 {
			b.moveTo(x, y)
			b.lineTo(x+w, y)
			b.lineTo(x+w, y+h)
			b.lineTo(x, y+h)
			b.closePath()
			return b.operations, nil
		}
		kx := svgKappa * rx
		ky := svgKappa * ry
		b.moveTo(x+rx, y)
		b.lineTo(x+w-rx, y)
		b.curveTo(x+w-rx+kx, y, x+w, y+ry-ky, x+w, y+ry)
		b.lineTo(x+w, y+h-ry)
		b.curveTo(x+w, y+h-ry+ky, x+w-rx+kx, y+h, x+w-rx, y+h)
		b.lineTo(x+rx, y+h)
		b.curveTo(x+rx-kx, y+h, x, y+h-ry+ky, x, y+h-ry)
		b.lineTo(x, y+ry)
		b.curveTo(x, y+ry-ky, x+rx-kx, y, x+rx, y)
		b.closePath()
	case "circle":
		r := number("r")
		if r <= 0.0 {
			return nil, nil
		}
		b.ellipse(number("cx"), number("cy"), r, r)
	case "ellipse":
		rx, ry := number("rx"), number("ry")
		if rx <= 0.0 || ry <= 0.0 {
			return nil, nil
		}
		b.ellipse(number("cx"), number("cy"), rx, ry)
	case "line":
		b.moveTo(number("x1"), number("y1"))
		b.lineTo(number("x2"), number("y2"))
	case "polyline", "polygon":
		points, err := parseSVGNumbers(attributes["points"])
		if err != nil || len(points) < 4 {
			return nil, nil // Drawn as far as it can be read, which is not a line
		}
		b.moveTo(points[0], points[1])
		for i := 2; i+1 < len(points); i += 2 {
			b.lineTo(points[i], points[i+1])
		}
		if name == "polygon" {
			b.closePath()
		}
	}
	return b.operations, nil
}

// radius reads the rx or the ry of a rectangle; ok is false when it is not
// given, is auto or is not a length of zero or more.
func radius(value string) (float64, bool) {
	value = strings.TrimSpace(value)
	if value == "" || value == "auto" {
		return 0.0, false
	}
	length, err := parseSVGLength(value)
	if err != nil || length < 0.0 || strings.HasSuffix(value, "%") {
		return 0.0, false
	}
	return float64(length), true
}

// addPath adds the operations of a path or a shape to the image, in the space
// of the svg element, with what the state draws them with, unless they draw
// nothing: a shape in <defs> or of no size, or with neither a fill nor a stroke.
func (image *SVGImage) addPath(state *svgState, operations []*svgPathOp) {
	m := state.matrix
	det := m[0]*m[3] - m[1]*m[2]
	if state.hidden || len(operations) == 0 || det == 0.0 {
		return
	}
	path := new(svgPath)
	path.operations = operations
	path.fill = state.fill
	if state.fillCurrent {
		path.fill = state.color
	}
	path.stroke = state.stroke
	if state.strokeCurrent {
		path.stroke = state.color
	}
	path.fillAlpha = state.fillOpacity * state.opacity
	path.strokeAlpha = state.strokeOpacity * state.opacity
	if path.fillAlpha == 0.0 {
		path.fill = color.Transparent
	}
	if path.strokeAlpha == 0.0 || state.strokeWidth == 0.0 {
		path.stroke = color.Transparent
	}
	if path.fill == color.Transparent && path.stroke == color.Transparent {
		return
	}
	path.strokeWidth = float32(float64(state.strokeWidth) * math.Sqrt(math.Abs(det)))
	path.evenOdd = state.evenOdd
	path.lineCap = state.lineCap
	path.lineJoin = state.lineJoin
	if m != svgIdentity {
		for _, op := range operations {
			op.x, op.y = svgTransform(m, op.x, op.y)
			if op.cmd == 'C' {
				op.x1, op.y1 = svgTransform(m, op.x1, op.y1)
				op.x2, op.y2 = svgTransform(m, op.x2, op.y2)
			}
		}
	}
	image.paths = append(image.paths, path)
}

// svgTransform returns the point that the matrix takes the point to.
func svgTransform(m [6]float64, x, y float32) (float32, float32) {
	return float32(m[0]*float64(x) + m[2]*float64(y) + m[4]),
		float32(m[1]*float64(x) + m[3]*float64(y) + m[5])
}

// svgUnits are the units of a length of an SVG file, in points. A number
// without a unit is in the user unit of the file, which PDFjet draws as a
// point, and so is a number in px.
var svgUnits = []struct {
	suffix string
	points float32
}{
	{"px", 1.0},
	{"pt", 1.0},
	{"pc", 12.0},
	{"in", 72.0},
	{"mm", 72.0 / 25.4},
	{"cm", 72.0 / 2.54},
}

// parseLength returns the width or the height of the svg element in points,
// and 0 for a length that PDFjet cannot read, a percentage among them, which
// leaves the size to the viewBox.
func parseLength(value string) float32 {
	value = strings.TrimSpace(value)
	scale := float32(1.0)
	for _, unit := range svgUnits {
		if strings.HasSuffix(value, unit.suffix) {
			value = strings.TrimSpace(strings.TrimSuffix(value, unit.suffix))
			scale = unit.points
			break
		}
	}
	number, err := strconv.ParseFloat(value, 32)
	if err != nil || math.IsInf(number, 0) || math.IsNaN(number) {
		return 0.0
	}
	return float32(number) * scale
}

// parseFloatLenient parses a numeric attribute value, treating an empty
// value as 0 — consistent with how an omitted attribute is handled.
func parseFloatLenient(value string) (float32, error) {
	value = strings.TrimSpace(value)
	if value == "" {
		return 0.0, nil
	}
	v, err := strconv.ParseFloat(value, 32)
	if err != nil {
		return 0.0, fmt.Errorf("%q: %w", value, err)
	}
	return float32(v), nil
}

func (image *SVGImage) processPaths(paths []*svgPath) error {
	if image.viewBox == "" {
		return nil
	}
	var box [4]float32
	list, err := parseSVGNumbers(image.viewBox)
	if err != nil || len(list) != 4 {
		return fmt.Errorf("Invalid SVG viewBox %q: four numbers are needed.", image.viewBox)
	}
	for i := range box {
		box[i] = float32(list[i])
	}
	if box[2] == 0 || box[3] == 0 {
		return fmt.Errorf("Invalid SVG viewBox %q: its width and height cannot be zero.", image.viewBox)
	}
	// A size the file does not give, or one in a unit PDFjet cannot read, is
	// the other one in the proportions of the viewBox, or the size of the
	// viewBox when both are missing, where scaling it by a width of zero would
	// draw every path at the origin.
	if image.w == 0.0 && image.h == 0.0 {
		image.w = box[2]
		image.h = box[3]
	} else if image.w == 0.0 {
		image.w = image.h * box[2] / box[3]
	} else if image.h == 0.0 {
		image.h = image.w * box[3] / box[2]
	}

	// The viewBox is scaled to the size, and, unless preserveAspectRatio is
	// none, by the same factor across and down, as large as it fits (meet) or
	// as small as it covers (slice), and placed as it says, in the middle by
	// default.
	sx := image.w / box[2]
	sy := image.h / box[3]
	var tx, ty float32
	fields := strings.Fields(image.preserveAspectRatio)
	if len(fields) > 0 && fields[0] == "defer" {
		fields = fields[1:]
	}
	align := "xMidYMid"
	if len(fields) > 0 {
		align = fields[0]
	}
	uniform := align != "none" && sx != sy
	if uniform {
		scale := float32(math.Min(float64(sx), float64(sy)))
		if len(fields) > 1 && fields[1] == "slice" {
			scale = float32(math.Max(float64(sx), float64(sy)))
		}
		sx = scale
		sy = scale
		if len(align) != 8 {
			align = "xMidYMid"
		}
		tx = alignOffset(align[1:4], image.w-box[2]*scale)
		ty = alignOffset(align[5:8], image.h-box[3]*scale)
	}
	strokeScale := float32(math.Sqrt(float64(sx) * float64(sy)))
	for _, path := range paths {
		path.strokeWidth *= strokeScale
		for _, op := range path.operations {
			if uniform {
				op.x = (op.x-box[0])*sx + tx
				op.y = (op.y-box[1])*sy + ty
				op.x1 = (op.x1-box[0])*sx + tx
				op.y1 = (op.y1-box[1])*sy + ty
				op.x2 = (op.x2-box[0])*sx + tx
				op.y2 = (op.y2-box[1])*sy + ty
			} else {
				op.x = (op.x - box[0]) * image.w / box[2]
				op.y = (op.y - box[1]) * image.h / box[3]
				op.x1 = (op.x1 - box[0]) * image.w / box[2]
				op.y1 = (op.y1 - box[1]) * image.h / box[3]
				op.x2 = (op.x2 - box[0]) * image.w / box[2]
				op.y2 = (op.y2 - box[1]) * image.h / box[3]
			}
		}
	}
	return nil
}

// alignOffset returns how far the viewBox is moved across or down in the
// space left over by it: none for Min, half for Mid and all for Max.
func alignOffset(align string, space float32) float32 {
	switch align {
	case "Min":
		return 0.0
	case "Max":
		return space
	}
	return space / 2.0
}

// ScaleBy scales this svgParser image by the specified factor.
func (image *SVGImage) ScaleBy(factor float32) *SVGImage {
	for _, path := range image.paths {
		path.strokeWidth *= factor
		for _, op := range path.operations {
			op.x1 *= factor
			op.y1 *= factor
			op.x2 *= factor
			op.y2 *= factor
			op.x *= factor
			op.y *= factor
		}
	}
	image.w *= factor
	image.h *= factor
	return image
}

// SetLocation sets the location of the top left corner of this image on the page.
func (image *SVGImage) SetLocation(x, y float32) Drawable {
	image.x = x
	image.y = y
	return image
}

// SetURIAction sets the URI for the "click box" action.
func (image *SVGImage) SetURIAction(uri string) *SVGImage {
	image.uri = uri
	return image
}

// SetGoToAction sets the destination key for the action.
func (image *SVGImage) SetGoToAction(key string) *SVGImage {
	image.key = key
	return image
}

// SetAltDescription sets the alternate description of this image.
func (image *SVGImage) SetAltDescription(altDescription string) *SVGImage {
	image.altDescription = altDescription
	return image
}

// SetActualText sets the actual text of this image.
func (image *SVGImage) SetActualText(actualText string) *SVGImage {
	image.actualText = actualText
	return image
}

// SetLanguage sets the language of this image, for example "en-US".
func (image *SVGImage) SetLanguage(language string) *SVGImage {
	image.language = language
	return image
}

// GetWidth returns the width of this svgParser image.
func (image *SVGImage) GetWidth() float32 {
	return image.w
}

// GetHeight returns the height of this svgParser image.
func (image *SVGImage) GetHeight() float32 {
	return image.h
}

// drawPath draws a path, filled and then stroked. An opacity, a line cap
// and a line join are set in a graphics state of the path's own.
func (image *SVGImage) drawPath(path *svgPath, page *Page) {
	fill := path.fill != color.Transparent
	stroke := path.stroke != color.Transparent
	if len(path.operations) == 0 || !fill && !stroke || !image.isWritable(path, page) {
		return
	}
	alpha := fill && path.fillAlpha < 1.0 || stroke && path.strokeAlpha < 1.0
	lineCap := stroke && path.lineCap != capstyle.Butt
	lineJoin := stroke && path.lineJoin != joinstyle.Miter
	state := alpha || lineCap || lineJoin
	if state {
		page.SaveGraphicsState()
		if alpha {
			gs := NewGraphicsState()
			gs.SetAlphaNonStroking(path.fillAlpha)
			gs.SetAlphaStroking(path.strokeAlpha)
			page.SetGraphicsState(gs)
		}
		if lineCap {
			page.SetLineCapStyle(path.lineCap)
		}
		if lineJoin {
			page.SetLineJoinStyle(path.lineJoin)
		}
	}

	if fill {
		page.SetBrushColor(path.fill)
		for _, op := range path.operations {
			if op.cmd == 'M' {
				page.MoveTo(op.x+image.x, op.y+image.y)
			} else if op.cmd == 'L' {
				page.LineTo(op.x+image.x, op.y+image.y)
			} else if op.cmd == 'C' {
				page.CurveTo(
					op.x1+image.x, op.y1+image.y,
					op.x2+image.x, op.y2+image.y,
					op.x+image.x, op.y+image.y)
			}
		}
		if path.evenOdd {
			page.appendString("f*\n")
		} else {
			page.FillPath()
		}
	}

	if stroke {
		page.SetPenColor(path.stroke)
		page.SetPenWidth(path.strokeWidth)
		// Z closes and strokes a subpath; a path that is still open at the end
		// is stroked without closing it.
		open := false
		for _, op := range path.operations {
			if op.cmd == 'M' {
				page.MoveTo(op.x+image.x, op.y+image.y)
				open = true
			} else if op.cmd == 'L' {
				page.LineTo(op.x+image.x, op.y+image.y)
				open = true
			} else if op.cmd == 'C' {
				page.CurveTo(
					op.x1+image.x, op.y1+image.y,
					op.x2+image.x, op.y2+image.y,
					op.x+image.x, op.y+image.y)
				open = true
			} else if op.cmd == 'Z' {
				page.ClosePath()
				open = false
			}
		}
		if open {
			page.StrokePath()
		}
	}

	if state {
		page.RestoreGraphicsState()
	}
}

// isWritable reports whether a PDF can hold the points and the stroke width
// of the path where it is drawn: a transform or a scale such as scale(1e30)
// can take them out of its range, and such a path is not drawn.
func (image *SVGImage) isWritable(path *svgPath, page *Page) bool {
	point := func(x, y float32) bool {
		x += image.x
		y += image.y
		return fastfloat.IsWritable(x) && fastfloat.IsWritable(y) && fastfloat.IsWritable(page.height-y)
	}
	if !fastfloat.IsWritable(path.strokeWidth) {
		return false
	}
	for _, op := range path.operations {
		if !point(op.x, op.y) || op.cmd == 'C' && (!point(op.x1, op.y1) || !point(op.x2, op.y2)) {
			return false
		}
	}
	return true
}

// DrawOn draws this svgParser image on the specified page.
func (image *SVGImage) DrawOn(page *Page) [2]float32 {
	if page == nil {
		return [2]float32{image.x + image.w, image.y + image.h} // Measured, not drawn
	}
	page.AddBDC(structelem.Figure, image.language, image.actualText, image.altDescription)
	for _, path := range image.paths {
		image.drawPath(path, page)
	}
	page.AddEMC()
	if image.uri != "" || image.key != "" {
		page.addAnnotation(&annotationObject{
			annotationType: annotationLink,
			x1:             image.x,
			y1:             image.y,
			x2:             image.x + image.w,
			y2:             image.y + image.h,
			vertices:       nil,
			opacity:        0.0,
			title:          "",
			contents:       "",
			uri:            image.uri,
			key:            image.key, // The destination name
			language:       image.language,
			actualText:     image.actualText,
			altDescription: image.altDescription,
		})
	}
	return [2]float32{image.x + image.w, image.y + image.h}
}
