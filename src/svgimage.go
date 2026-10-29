// svgimage.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// Floating point: every float multiplication in this file is wrapped in
// float32(...) or float64(...) on purpose, so that Go's arm64 compiler does
// not fuse it with an addition and round otherwise than amd64 and the other
// ports. Keep the wrapping; see "Floating point on ARM" in README.md.
// check-no-fma.sh fails if one is removed.

import (
	"bytes"
	"fmt"
	"io"
	"math"
	"os"
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/capstyle"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/internal/fastfloat"
	"github.com/edragoev1/pdfjet/v9/src/joinstyle"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
	"github.com/edragoev1/pdfjet/v9/src/xmlparser"
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
	font                *Font
	boldFont            *Font
	clipShapes          []*svgPath // The clip paths of the image

	// The core fonts the text is drawn in unless a font is set, one of each
	// for a document, made when a text is first drawn in it.
	corePDF       *PDF
	helvetica     *Font
	helveticaBold *Font
}

// maxSVGReused is the most elements drawn again, by use elements and as
// markers: a use of a group of uses, and so on, draws ten to the power of the
// number of groups, as an XML of entities in entities does.
const maxSVGReused = 1000000

// svgReader holds what is needed while an SVG file is read: the elements by
// their ids, the states of the elements being drawn, and the elements drawn
// again by a use element or as a marker, which are not drawn inside
// themselves.
type svgReader struct {
	image    *SVGImage
	colorMap map[string]int32
	rules    *svgStyleSheet
	stack    []svgState
	root     bool
	ids      map[string]*xmlparser.XMLNode
	reusing  map[*xmlparser.XMLNode]bool
	reused   int64
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
// stroke, stroke-width, stroke-dasharray, stroke-dashoffset, fill-rule,
// stroke-linecap, stroke-linejoin, opacity, fill-opacity, stroke-opacity,
// paint-order, clip-path and markers they have or inherit, as attributes, in
// a style attribute or in rules for their classes in a <style> element that
// comes before them. It draws the elements a <use> element names, and <text>
// and <tspan> elements with their x, y, dx, dy, font-size, font-weight,
// text-anchor and dominant-baseline, in the font set with SetFont and
// SetBoldFont, or in Helvetica. A gradient is drawn in the mean color of its
// stops. The width, height, viewBox and preserveAspectRatio of the <svg>
// element give the size of the image, and what is drawn outside it is cut
// off, as a browser does.
func NewSVGImage(reader io.Reader) (*SVGImage, error) {
	image := new(SVGImage)
	image.paths = make([]*svgPath, 0)

	document, err := xmlparser.ParseReaderSkippingDoctype(reader)
	if err != nil {
		return nil, fmt.Errorf("parsing SVG: %w", err)
	}
	r := &svgReader{
		image:    image,
		colorMap: svgColorMap,
		rules:    newSVGStyleSheet(),
		root:     true,
		ids:      make(map[string]*xmlparser.XMLNode),
		reusing:  make(map[*xmlparser.XMLNode]bool),
	}
	// The first element of an id, from the top down, is the one it names.
	elements := []*xmlparser.XMLNode{document}
	for i := 0; i < len(elements); i++ {
		element := elements[i]
		if id, ok := element.Attribute("id"); ok && id != "" {
			if _, found := r.ids[id]; !found {
				r.ids[id] = element
			}
		}
		elements = append(elements, element.Children()...)
	}
	svg := newSVGState()
	svg.paints, err = r.gradientColors()
	if err != nil {
		return nil, err
	}
	r.stack = []svgState{svg}
	if err := r.draw(document, false); err != nil {
		return nil, err
	}

	if err := image.processPaths(image.paths); err != nil {
		return nil, err
	}
	return image, nil
}

// gradientColors returns the colors the gradients are drawn in, by their ids:
// the mean of the colors of their stops, or of the stops of the gradient they
// name with href when they have none. A gradient without stops is none, and
// is left out.
func (r *svgReader) gradientColors() (map[string]int32, error) {
	colors := make(map[string]int32)
	for id, gradient := range r.ids {
		// Up to ten gradients along the hrefs, which may go round in a circle.
		for i := 0; i < 10 && gradient != nil && isSVGGradient(gradient); i++ {
			var red, green, blue int64
			count := int64(0)
			for _, stop := range gradient.Children() {
				if stop.LocalName() != "stop" {
					continue
				}
				state := newSVGState()
				state.fill = color.Black // stop-color is black unless given
				if c, ok := stop.Attribute("stop-color"); ok {
					if err := state.setProperty(r.colorMap, "fill", c); err != nil {
						return nil, err
					}
				}
				style, _ := stop.Attribute("style")
				for _, declaration := range parseSVGDeclarations(style) {
					if declaration[0] == "stop-color" {
						if err := state.setProperty(r.colorMap, "fill", declaration[1]); err != nil {
							return nil, err
						}
					}
				}
				c := int64(state.fill)
				if state.fill == color.Transparent {
					c = int64(color.Black)
				}
				red += (c >> 16) & 0xFF
				green += (c >> 8) & 0xFF
				blue += c & 0xFF
				count++
			}
			if count > 0 {
				colors[id] = int32((red+count/2)/count<<16 | (green+count/2)/count<<8 | (blue+count/2)/count)
				break
			}
			gradient = r.ids[svgHref(gradient)]
		}
	}
	return colors, nil
}

// isSVGGradient reports whether the element is a gradient.
func isSVGGradient(node *xmlparser.XMLNode) bool {
	name := node.LocalName()
	return name == "linearGradient" || name == "radialGradient"
}

// svgHref returns the id that the href of an element names, #id, with or
// without the xlink prefix, or "".
func svgHref(node *xmlparser.XMLNode) string {
	href := ""
	found := false
	for _, attr := range node.Attributes() {
		name := attr.Name
		localName := name
		if colon := strings.IndexByte(name, ':'); colon != -1 {
			localName = name[colon+1:]
		}
		if name == "href" || !found && localName == "href" {
			href = attr.Value
			found = true
		}
	}
	href = strings.TrimSpace(href)
	if len(href) > 1 && href[0] == '#' {
		return href[1:]
	}
	return ""
}

// svgAttributes returns the attributes of no namespace of the element, and
// their order: xlink:href, xml:space and the namespaces' own declarations,
// xmlns:..., are not read.
func svgAttributes(node *xmlparser.XMLNode) (map[string]string, []string) {
	attributes := make(map[string]string)
	order := make([]string, 0)
	for _, attr := range node.Attributes() {
		if !strings.Contains(attr.Name, ":") {
			attributes[attr.Name] = attr.Value
			order = append(order, attr.Name)
		}
	}
	return attributes, order
}

// svgAttribute returns the value of the attribute of the element, or "".
func svgAttribute(node *xmlparser.XMLNode, name string) string {
	value, _ := node.Attribute(name)
	return value
}

// countReused counts an element drawn again, and fails when there are too
// many.
func (r *svgReader) countReused() error {
	r.reused++
	if r.reused > maxSVGReused {
		return fmt.Errorf("The SVG draws more than %d elements again, with use elements or markers.", maxSVGReused)
	}
	return nil
}

// draw draws the element, then the ones in it, in the order of the document;
// the rules of a <style> element apply from its end. An element that is used,
// by a use element or as a marker, is drawn even where it is a template, as a
// symbol or a marker is.
func (r *svgReader) draw(node *xmlparser.XMLNode, used bool) error {
	image := r.image
	name := node.LocalName()
	if len(r.reusing) > 0 {
		if err := r.countReused(); err != nil {
			return err
		}
	}
	attributes, order := svgAttributes(node)
	state, err := elementState(r.stack[len(r.stack)-1], r.colorMap, r.rules, attributes, order)
	if err != nil {
		return err
	}
	if svgTemplates[name] && !used {
		state.hidden = true
	}
	if state.clipPath != "" && !state.hidden {
		if err := r.clip(&state); err != nil {
			return err
		}
	}
	r.stack = append(r.stack, state)
	defer func() { r.stack = r.stack[:len(r.stack)-1] }()

	if name == "svg" && r.root {
		r.root = false
		image.w = parseLength(attributes["width"])
		image.h = parseLength(attributes["height"])
		// viewbox in lower case when there is no viewBox, as an HTML
		// page's parser reads an inline svg, whose files keep it
		if viewBox, ok := attributes["viewBox"]; ok {
			image.viewBox = viewBox
		} else {
			image.viewBox = attributes["viewbox"]
		}
		image.preserveAspectRatio = attributes["preserveAspectRatio"]
	}
	if name == "text" {
		if !state.hidden {
			return r.addText(node, state, attributes)
		}
		return nil
	}
	if name == "use" {
		target := r.ids[svgHref(node)]
		if target != nil && !state.hidden && !r.reusing[target] {
			// The element is drawn as a child of the use element, moved by
			// its x and y.
			parent := state
			parent.matrix = svgMultiply(state.matrix, [6]float64{
				1, 0, 0, 1, parseSVGCoordinate(attributes["x"]), parseSVGCoordinate(attributes["y"])})
			r.stack = append(r.stack, parent)
			r.reusing[target] = true
			err := r.draw(target, true)
			delete(r.reusing, target)
			r.stack = r.stack[:len(r.stack)-1]
			if err != nil {
				return err
			}
		}
		return nil
	}
	operations, err := shapeOperations(name, attributes)
	if err != nil {
		return err
	}
	if name == "line" {
		state.fill = color.Transparent // A line has no inside to fill
		state.fillCurrent = false
	}
	var vertices [][3]float64
	if !state.hidden && len(operations) > 0 &&
		(state.markerStart != "" || state.markerMid != "" || state.markerEnd != "") &&
		(name == "path" || name == "line" || name == "polyline" || name == "polygon") {
		vertices = svgVertices(operations) // Before addPath moves the points
	}
	image.addPath(&state, operations)
	if vertices != nil {
		if err := r.drawMarkers(&state, vertices); err != nil {
			return err
		}
	}

	for _, child := range node.Children() {
		if err := r.draw(child, false); err != nil {
			return err
		}
	}
	if name == "style" {
		r.rules.add(parseSVGStyleSheet(node.Text()))
	}
	return nil
}

// elementState returns the state of an element whose parent has the given
// one: its presentation attributes, in their order, then the rules of the
// style sheet for its classes, in the order of the style sheet, then its
// style attribute, and its transform.
func elementState(parent svgState, colorMap map[string]int32, rules *svgStyleSheet,
	attributes map[string]string, order []string) (svgState, error) {
	state := parent
	state.ownOpacity = 1.0
	state.clipPath = ""
	for _, name := range order {
		if err := state.setProperty(colorMap, name, attributes[name]); err != nil {
			return state, err
		}
	}
	ofClasses, err := rules.forClasses(strings.Fields(attributes["class"]))
	if err != nil {
		return state, err
	}
	for _, rule := range ofClasses {
		for _, declaration := range rule.declarations {
			if err := state.setProperty(colorMap, declaration[0], declaration[1]); err != nil {
				return state, err
			}
		}
	}
	for _, declaration := range parseSVGDeclarations(attributes["style"]) {
		if err := state.setProperty(colorMap, declaration[0], declaration[1]); err != nil {
			return state, err
		}
	}
	state.opacity = float32(state.opacity * state.ownOpacity)
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
	kx := float64(svgKappa * rx)
	ky := float64(svgKappa * ry)
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
		// Path data starts with a moveto; data that does not is in error
		// from its start, and SVG 1.1 section 8.3.2 draws it up to there.
		if len(operations) > 0 && operations[0].cmd != 'M' {
			return nil, nil
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
		rx = math.Min(rx, float64(w/2.0))
		ry = math.Min(ry, float64(h/2.0))
		if rx <= 0.0 || ry <= 0.0 {
			b.moveTo(x, y)
			b.lineTo(x+w, y)
			b.lineTo(x+w, y+h)
			b.lineTo(x, y+h)
			b.closePath()
			return b.operations, nil
		}
		kx := float64(svgKappa * rx)
		ky := float64(svgKappa * ry)
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
	det := float64(m[0]*m[3]) - float64(m[1]*m[2])
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
	path.fillAlpha = float32(state.fillOpacity * state.opacity)
	path.strokeAlpha = float32(state.strokeOpacity * state.opacity)
	if path.fillAlpha == 0.0 {
		path.fill = color.Transparent
	}
	if path.strokeAlpha == 0.0 || state.strokeWidth == 0.0 {
		path.stroke = color.Transparent
	}
	if path.fill == color.Transparent && path.stroke == color.Transparent {
		return
	}
	scale := math.Sqrt(math.Abs(det))
	path.strokeWidth = float32(float64(float64(state.strokeWidth) * scale))
	path.evenOdd = state.evenOdd
	path.lineCap = state.lineCap
	path.lineJoin = state.lineJoin
	if state.dashArray != nil {
		path.dashArray = make([]float32, len(state.dashArray))
		for i, length := range state.dashArray {
			path.dashArray[i] = float32(float64(float64(length) * scale))
		}
		path.dashOffset = float32(float64(float64(state.dashOffset) * scale))
	}
	path.clips = state.clips
	svgTransformAll(m, operations)
	image.paths = append(image.paths, path)
}

// svgTransformAll moves the points of the operations by the matrix.
func svgTransformAll(m [6]float64, operations []*svgPathOp) {
	if m == svgIdentity {
		return
	}
	for _, op := range operations {
		op.x, op.y = svgTransform(m, op.x, op.y)
		if op.cmd == 'C' {
			op.x1, op.y1 = svgTransform(m, op.x1, op.y1)
			op.x2, op.y2 = svgTransform(m, op.x2, op.y2)
		}
	}
}

// clip sets the clip paths of the element: those of its ancestors and the one
// its clip-path names, the shapes of the clipPath element in the user space
// of the element. A clip path that is not there, or one in objectBoundingBox
// units, which PDFjet does not draw, is left out; one with no shape in it
// hides the element, as SVG clips all of it away.
func (r *svgReader) clip(state *svgState) error {
	clipPath := r.ids[state.clipPath]
	if clipPath == nil || clipPath.LocalName() != "clipPath" ||
		strings.TrimSpace(svgAttribute(clipPath, "clipPathUnits")) == "objectBoundingBox" {
		return nil
	}
	m := state.matrix
	if own, ok := parseSVGTransform(svgAttribute(clipPath, "transform")); ok {
		m = svgMultiply(m, own)
	}
	clip := new(svgPath)
	clip.operations = make([]*svgPathOp, 0)
	for _, child := range clipPath.Children() {
		attributes, _ := svgAttributes(child)
		shape := m
		if transform, ok := attributes["transform"]; ok {
			if matrix, ok := parseSVGTransform(transform); ok {
				shape = svgMultiply(m, matrix)
			}
		}
		operations, err := shapeOperations(child.LocalName(), attributes)
		if err != nil {
			return err
		}
		svgTransformAll(shape, operations)
		clip.operations = append(clip.operations, operations...)
	}
	if len(clip.operations) == 0 {
		state.hidden = true
		return nil
	}
	r.image.clipShapes = append(r.image.clipShapes, clip)
	clips := make([]*svgPath, 0, len(state.clips)+1)
	clips = append(clips, state.clips...)
	state.clips = append(clips, clip)
	return nil
}

// svgVertices returns the vertices of the path, where its markers are drawn,
// in its user space: x, y and the angle of the path there, in degrees, which
// is halfway between the directions in and out of a vertex in the middle.
func svgVertices(operations []*svgPathOp) [][3]float64 {
	vertices := make([][6]float64, 0) // x, y, in x, in y, out x, out y
	x := 0.0
	y := 0.0
	start := 0 // The vertex where the subpath starts
	for _, op := range operations {
		var in, out [2]float64
		if op.cmd == 'M' {
			start = len(vertices)
			vertices = append(vertices, [6]float64{float64(op.x), float64(op.y), 0, 0, 0, 0})
			x = float64(op.x)
			y = float64(op.y)
			continue
		} else if op.cmd == 'C' {
			out = svgDirection(x, y, float64(op.x1), float64(op.y1), float64(op.x2), float64(op.y2),
				float64(op.x), float64(op.y))
			in = svgDirection(float64(op.x), float64(op.y), float64(op.x2), float64(op.y2),
				float64(op.x1), float64(op.y1), x, y)
			in = [2]float64{-in[0], -in[1]}
		} else {
			// A line, or Z, the line back to the start of the subpath.
			out = [2]float64{float64(op.x) - x, float64(op.y) - y}
			in = out
		}
		if len(vertices) == 0 {
			continue
		}
		last := &vertices[len(vertices)-1]
		if last[4] == 0.0 && last[5] == 0.0 {
			last[4] = out[0]
			last[5] = out[1]
		}
		vertices = append(vertices, [6]float64{float64(op.x), float64(op.y), in[0], in[1], 0, 0})
		if op.cmd == 'Z' {
			// The vertex that closes the subpath goes on as its start does.
			closing := &vertices[len(vertices)-1]
			closing[4] = vertices[start][4]
			closing[5] = vertices[start][5]
		}
		x = float64(op.x)
		y = float64(op.y)
	}
	result := make([][3]float64, 0, len(vertices))
	for _, v := range vertices {
		inLength := math.Hypot(v[2], v[3])
		outLength := math.Hypot(v[4], v[5])
		dx := 0.0
		dy := 0.0
		if inLength > 0.0 {
			dx += v[2] / inLength
			dy += v[3] / inLength
		}
		if outLength > 0.0 {
			dx += v[4] / outLength
			dy += v[5] / outLength
		}
		if dx == 0.0 && dy == 0.0 && inLength > 0.0 {
			dx = v[2] // A path that turns back: the direction in
			dy = v[3]
		}
		result = append(result, [3]float64{v[0], v[1], svgDegrees(math.Atan2(dy, dx))})
	}
	return result
}

// svgDegrees and svgRadians convert an angle as Java's Math.toDegrees and
// Math.toRadians do, by one multiplication.
func svgDegrees(radians float64) float64 {
	return float64(radians * 57.29577951308232)
}

func svgRadians(degrees float64) float64 {
	return float64(degrees * 0.017453292519943295)
}

// svgDirection returns the direction from x, y to the first of the points
// that is not there, as a curve leaves its start.
func svgDirection(x, y float64, points ...float64) [2]float64 {
	for i := 0; i+1 < len(points); i += 2 {
		if points[i] != x || points[i+1] != y {
			return [2]float64{points[i] - x, points[i+1] - y}
		}
	}
	return [2]float64{0, 0}
}

// drawMarkers draws the markers of a path at its vertices: the start marker
// at the first, the end marker at the last and the mid marker at the others.
func (r *svgReader) drawMarkers(state *svgState, vertices [][3]float64) error {
	last := len(vertices) - 1
	for i := 0; i <= last; i++ {
		if i == 0 {
			if err := r.drawMarker(state.markerStart, state, vertices[i], true); err != nil {
				return err
			}
		}
		if i > 0 && i < last {
			if err := r.drawMarker(state.markerMid, state, vertices[i], false); err != nil {
				return err
			}
		}
		if i == last {
			if err := r.drawMarker(state.markerEnd, state, vertices[i], false); err != nil {
				return err
			}
		}
	}
	return nil
}

// drawMarker draws the marker at the vertex: its refX, refY there, turned as
// its orient says, and its viewBox fitted to its markerWidth and
// markerHeight, which are in units of the stroke width unless its
// markerUnits are userSpaceOnUse.
func (r *svgReader) drawMarker(id string, state *svgState, vertex [3]float64, start bool) error {
	if id == "" {
		return nil
	}
	marker := r.ids[id]
	if marker == nil || marker.LocalName() != "marker" || r.reusing[marker] {
		return nil
	}
	orient := strings.TrimSpace(svgAttribute(marker, "orient"))
	angle := 0.0
	if orient == "auto" {
		angle = vertex[2]
	} else if orient == "auto-start-reverse" {
		angle = vertex[2]
		if start {
			angle = vertex[2] + 180.0
		}
	} else {
		orient = strings.TrimSuffix(orient, "deg")
		number, err := parseSVGFloat(strings.TrimSpace(orient), 64)
		if err == nil && !math.IsNaN(number) && !math.IsInf(number, 0) {
			angle = number
		}
	}
	units := 1.0
	if strings.TrimSpace(svgAttribute(marker, "markerUnits")) != "userSpaceOnUse" {
		units = float64(state.strokeWidth)
	}
	width := svgMarkerSize(svgAttribute(marker, "markerWidth"))
	height := svgMarkerSize(svgAttribute(marker, "markerHeight"))
	sx := 1.0
	sy := 1.0
	if box, err := parseSVGNumbers(svgAttribute(marker, "viewBox")); err == nil && len(box) == 4 {
		if box[2] <= 0.0 || box[3] <= 0.0 {
			return nil // A viewBox of no size draws nothing
		}
		sx = width / box[2]
		sy = height / box[3]
		if strings.TrimSpace(svgAttribute(marker, "preserveAspectRatio")) != "none" {
			sx = math.Min(sx, sy)
			sy = sx
		}
	}
	radians := svgRadians(angle)
	cos := math.Cos(radians)
	sin := math.Sin(radians)
	m := svgMultiply(state.matrix, [6]float64{cos, sin, -sin, cos, vertex[0], vertex[1]})
	m = svgMultiply(m, [6]float64{float64(units * sx), 0, 0, float64(units * sy), 0, 0})
	m = svgMultiply(m, [6]float64{
		1, 0, 0, 1, -parseSVGCoordinate(svgAttribute(marker, "refX")),
		-parseSVGCoordinate(svgAttribute(marker, "refY"))})
	// The marker inherits from the svg element, not from the path.
	base := newSVGState()
	base.paints = state.paints
	base.clips = state.clips
	base.matrix = m
	r.stack = append(r.stack, base)
	r.reusing[marker] = true
	err := r.draw(marker, true)
	delete(r.reusing, marker)
	r.stack = r.stack[:len(r.stack)-1]
	return err
}

// svgMarkerSize reads the markerWidth or the markerHeight: 3 unless given.
func svgMarkerSize(value string) float64 {
	if strings.TrimSpace(value) == "" {
		return 3.0
	}
	size := parseSVGCoordinate(value)
	if size > 0.0 {
		return size
	}
	return 0.0
}

// addText adds a text element, with its tspan elements, as the runs of its
// characters.
func (r *svgReader) addText(node *xmlparser.XMLNode, state svgState, attributes map[string]string) error {
	text := new(svgText)
	text.matrix = state.matrix
	pending := [4]float64{math.NaN(), math.NaN(), 0, 0}
	if err := r.addRuns(text, node, state, attributes, &pending); err != nil {
		return err
	}
	text.collapseSpaces()
	empty := true
	for _, run := range text.runs {
		if run.text != "" {
			empty = false
		}
	}
	m := state.matrix
	if empty || float64(m[0]*m[3])-float64(m[1]*m[2]) == 0.0 {
		return nil
	}
	path := new(svgPath)
	path.operations = make([]*svgPathOp, 0)
	path.text = text
	path.clips = state.clips
	r.image.paths = append(r.image.paths, path)
	return nil
}

// addRuns adds the runs of a text or a tspan element. Its x, y, dx and dy
// apply to its first character, which may be in a tspan in it; they are kept
// in pending until then.
func (r *svgReader) addRuns(text *svgText, node *xmlparser.XMLNode, state svgState,
	attributes map[string]string, pending *[4]float64) error {
	x := svgTextLength(attributes["x"], state.fontSize)
	y := svgTextLength(attributes["y"], state.fontSize)
	dx := svgTextLength(attributes["dx"], state.fontSize)
	dy := svgTextLength(attributes["dy"], state.fontSize)
	if !math.IsNaN(x) {
		pending[0] = x
		pending[2] = 0.0
	}
	if !math.IsNaN(y) {
		pending[1] = y
		pending[3] = 0.0
	}
	if !math.IsNaN(dx) {
		pending[2] += dx
	}
	if !math.IsNaN(dy) {
		pending[3] += dy
	}
	for _, item := range node.Content() {
		switch item := item.(type) {
		case string:
			run := &svgTextRun{
				text:        item,
				x:           pending[0],
				y:           pending[1],
				dx:          pending[2],
				dy:          pending[3],
				fontSize:    state.fontSize,
				bold:        state.bold,
				anchor:      state.textAnchor,
				baseline:    state.baseline,
				fill:        state.fill,
				stroke:      state.stroke,
				fillAlpha:   float32(state.fillOpacity * state.opacity),
				strokeAlpha: float32(state.strokeOpacity * state.opacity),
				strokeWidth: state.strokeWidth,
				strokeFirst: state.strokeFirst,
				lineJoin:    state.lineJoin,
			}
			if state.fillCurrent {
				run.fill = state.color
			}
			if state.strokeCurrent {
				run.stroke = state.color
			}
			*pending = [4]float64{math.NaN(), math.NaN(), 0, 0}
			text.runs = append(text.runs, run)
		case *xmlparser.XMLNode:
			name := item.LocalName()
			if name != "tspan" && name != "a" {
				continue // A title or a desc is not drawn
			}
			childAttributes, childOrder := svgAttributes(item)
			childState, err := elementState(state, r.colorMap, r.rules, childAttributes, childOrder)
			if err != nil {
				return err
			}
			childState.matrix = state.matrix // A tspan has no transform
			if !childState.hidden {
				if err := r.addRuns(text, item, childState, childAttributes, pending); err != nil {
					return err
				}
			}
		}
	}
	return nil
}

// svgTextLength reads the first length of an x, y, dx or dy of a text, which
// may be in em of its font size, and returns NaN for none.
func svgTextLength(value string, fontSize float32) float64 {
	fields := strings.Fields(strings.ReplaceAll(value, ",", " "))
	if len(fields) == 0 {
		return math.NaN()
	}
	first := fields[0]
	var length float64
	if strings.HasSuffix(first, "em") {
		number, err := parseSVGFloat(first[:len(first)-2], 64)
		if err != nil {
			return math.NaN()
		}
		length = float64(number * float64(fontSize))
	} else if strings.HasSuffix(first, "%") {
		return math.NaN()
	} else {
		points, err := parseSVGLength(first)
		if err != nil {
			return math.NaN()
		}
		length = float64(points)
	}
	if math.IsInf(length, 0) {
		return math.NaN()
	}
	return length
}

// svgTransform returns the point that the matrix takes the point to.
func svgTransform(m [6]float64, x, y float32) (float32, float32) {
	return float32(float64(m[0]*float64(x)) + float64(m[2]*float64(y)) + m[4]),
		float32(float64(m[1]*float64(x)) + float64(m[3]*float64(y)) + m[5])
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
	number, err := parseSVGFloat(value, 32)
	if err != nil || math.IsInf(number, 0) || math.IsNaN(number) {
		return 0.0
	}
	return float32(float32(number) * scale)
}

// parseFloatLenient parses a numeric attribute value, treating an empty
// value as 0 — consistent with how an omitted attribute is handled.
func parseFloatLenient(value string) (float32, error) {
	value = strings.TrimSpace(value)
	if value == "" {
		return 0.0, nil
	}
	v, err := parseSVGFloat(value, 32)
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
		image.w = float32(image.h*box[2]) / box[3]
	} else if image.h == 0.0 {
		image.h = float32(image.w*box[3]) / box[2]
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
		tx = alignOffset(align[1:4], image.w-float32(box[2]*scale))
		ty = alignOffset(align[5:8], image.h-float32(box[3]*scale))
	}
	strokeScale := float32(math.Sqrt(float64(float64(sx) * float64(sy))))
	all := append(append(make([]*svgPath, 0, len(paths)+len(image.clipShapes)), paths...), image.clipShapes...)
	for _, path := range all {
		path.strokeWidth = float32(path.strokeWidth * strokeScale)
		if path.dashArray != nil {
			for i := range path.dashArray {
				path.dashArray[i] = float32(path.dashArray[i] * strokeScale)
			}
			path.dashOffset = float32(path.dashOffset * strokeScale)
		}
		if path.text != nil {
			m := path.text.matrix
			ax := float64(image.w / box[2])
			ay := float64(image.h / box[3])
			ox := 0.0
			oy := 0.0
			if uniform {
				ax = float64(sx)
				ay = float64(sy)
				ox = float64(tx)
				oy = float64(ty)
			}
			path.text.matrix = [6]float64{
				float64(m[0] * ax), float64(m[1] * ay), float64(m[2] * ax), float64(m[3] * ay),
				float64((m[4]-float64(box[0]))*ax) + ox, float64((m[5]-float64(box[1]))*ay) + oy}
		}
		for _, op := range path.operations {
			if uniform {
				op.x = float32((op.x-box[0])*sx) + tx
				op.y = float32((op.y-box[1])*sy) + ty
				op.x1 = float32((op.x1-box[0])*sx) + tx
				op.y1 = float32((op.y1-box[1])*sy) + ty
				op.x2 = float32((op.x2-box[0])*sx) + tx
				op.y2 = float32((op.y2-box[1])*sy) + ty
			} else {
				op.x = float32((op.x-box[0])*image.w) / box[2]
				op.y = float32((op.y-box[1])*image.h) / box[3]
				op.x1 = float32((op.x1-box[0])*image.w) / box[2]
				op.y1 = float32((op.y1-box[1])*image.h) / box[3]
				op.x2 = float32((op.x2-box[0])*image.w) / box[2]
				op.y2 = float32((op.y2-box[1])*image.h) / box[3]
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
	return float32(space / 2.0)
}

// ScaleBy scales this svgParser image by the specified factor.
func (image *SVGImage) ScaleBy(factor float32) *SVGImage {
	all := append(append(make([]*svgPath, 0, len(image.paths)+len(image.clipShapes)), image.paths...), image.clipShapes...)
	for _, path := range all {
		path.strokeWidth = float32(path.strokeWidth * factor)
		if path.dashArray != nil {
			for i := range path.dashArray {
				path.dashArray[i] = float32(path.dashArray[i] * factor)
			}
			path.dashOffset = float32(path.dashOffset * factor)
		}
		if path.text != nil {
			m := path.text.matrix
			f := float64(factor)
			path.text.matrix = [6]float64{
				float64(m[0] * f), float64(m[1] * f), float64(m[2] * f),
				float64(m[3] * f), float64(m[4] * f), float64(m[5] * f)}
		}
		for _, op := range path.operations {
			op.x1 = float32(op.x1 * factor)
			op.y1 = float32(op.y1 * factor)
			op.x2 = float32(op.x2 * factor)
			op.y2 = float32(op.y2 * factor)
			op.x = float32(op.x * factor)
			op.y = float32(op.y * factor)
		}
	}
	image.w = float32(image.w * factor)
	image.h = float32(image.h * factor)
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

// SetFont sets the font of the text of the image. Unless set, the text is
// drawn in Helvetica, which is not embedded and has the characters of Western
// European languages only; a document of PDF/A or PDF/UA needs a font that is
// embedded.
func (image *SVGImage) SetFont(font *Font) *SVGImage {
	image.font = font
	return image
}

// SetBoldFont sets the font of the bold text of the image, whose font-weight
// is bold or 600 or more. Unless set, it is the font set with SetFont, or
// Helvetica Bold when neither is set.
func (image *SVGImage) SetBoldFont(font *Font) *SVGImage {
	image.boldFont = font
	return image
}

// coreFont returns Helvetica, or Helvetica Bold when bold is true, made once
// for the document.
func (image *SVGImage) coreFont(pdf *PDF, bold bool) *Font {
	if image.corePDF != pdf {
		image.corePDF = pdf
		image.helvetica = nil
		image.helveticaBold = nil
	}
	if !bold {
		if image.helvetica == nil {
			image.helvetica = NewCoreFont(pdf, corefont.Helvetica())
		}
		return image.helvetica
	}
	if image.helveticaBold == nil {
		image.helveticaBold = NewCoreFont(pdf, corefont.HelveticaBold())
	}
	return image.helveticaBold
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
	// PDF/A-1 has no transparency, so there the path is drawn opaque, as an
	// annotation is (review of 12 October 2026, R9).
	alpha := !page.pdf.isPDFA1() && (fill && path.fillAlpha < 1.0 || stroke && path.strokeAlpha < 1.0)
	lineCap := stroke && path.lineCap != capstyle.Butt
	lineJoin := stroke && path.lineJoin != joinstyle.Miter
	dash := stroke && path.dashArray != nil
	state := alpha || lineCap || lineJoin || dash || path.clips != nil
	if state {
		page.SaveGraphicsState()
		image.clip(path.clips, page)
		if dash {
			page.appendByte('[')
			for i, length := range path.dashArray {
				if i > 0 {
					page.appendByte(' ')
				}
				page.appendFloat32(length)
			}
			page.appendString("] ")
			page.appendFloat32(path.dashOffset)
			page.appendString(" d\n")
		}
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
		// Z closes a subpath, and the path is stroked once, all of its
		// subpaths together, as SVG strokes it.
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
			} else if op.cmd == 'Z' {
				page.appendString("h\n")
			}
		}
		page.StrokePath()
	}

	if state {
		page.RestoreGraphicsState()
	}
}

// clip clips what is drawn next to the clip paths, each in turn.
func (image *SVGImage) clip(clips []*svgPath, page *Page) {
	for _, clip := range clips {
		for _, op := range clip.operations {
			if op.cmd == 'M' {
				page.MoveTo(op.x+image.x, op.y+image.y)
			} else if op.cmd == 'L' {
				page.LineTo(op.x+image.x, op.y+image.y)
			} else if op.cmd == 'C' {
				page.CurveTo(op.x1+image.x, op.y1+image.y, op.x2+image.x, op.y2+image.y, op.x+image.x, op.y+image.y)
			} else if op.cmd == 'Z' {
				page.appendString("h\n")
			}
		}
		page.ClipPath()
	}
}

// drawText draws a text: each run filled and then stroked, or stroked first
// when its paint-order says, with the text matrix that takes its glyphs, whose
// y goes up, to where the run is on the page.
func (image *SVGImage) drawText(text *svgText, clips []*svgPath, page *Page) {
	m := text.matrix
	scale := math.Sqrt(math.Abs(float64(m[0]*m[3]) - float64(m[1]*m[2])))
	for _, run := range text.runs {
		str := run.drawn()
		if str == "" || !(run.fontSize > 0.0) {
			continue
		}
		px := float64(run.px)
		py := float64(run.py)
		tm := [6]float32{
			float32(m[0]),
			float32(-m[1]),
			float32(-m[2]),
			float32(m[3]),
			float32(float64(m[0]*px)+float64(m[2]*py)+m[4]) + image.x,
			page.height - (float32(float64(m[1]*px)+float64(m[3]*py)+m[5]) + image.y)}
		writable := true
		for _, value := range tm {
			writable = writable && fastfloat.IsWritable(value)
		}
		strokeWidth := float32(float64(float64(run.strokeWidth) * scale))
		fill := run.fill != color.Transparent && run.fillAlpha > 0.0
		stroke := run.stroke != color.Transparent && run.strokeAlpha > 0.0 &&
			strokeWidth > 0.0 && fastfloat.IsWritable(strokeWidth)
		if !writable || !fill && !stroke {
			continue
		}
		alpha := !page.pdf.isPDFA1() && (fill && run.fillAlpha < 1.0 || stroke && run.strokeAlpha < 1.0)
		lineJoin := stroke && run.lineJoin != joinstyle.Miter
		state := alpha || lineJoin || clips != nil
		if state {
			page.SaveGraphicsState()
			image.clip(clips, page)
			if alpha {
				gs := NewGraphicsState()
				gs.SetAlphaNonStroking(run.fillAlpha)
				gs.SetAlphaStroking(run.strokeAlpha)
				page.SetGraphicsState(gs)
			}
			if lineJoin {
				page.SetLineJoinStyle(run.lineJoin)
			}
		}
		for pass := 0; pass < 2; pass++ {
			strokePass := (pass == 0) == run.strokeFirst
			if strokePass && stroke {
				page.SetPenColor(run.stroke)
				page.SetPenWidth(strokeWidth)
				page.drawSVGText(run.font, run.fontSize, str, tm, 1)
			} else if !strokePass && fill {
				page.SetBrushColor(run.fill)
				page.drawSVGText(run.font, run.fontSize, str, tm, 0)
			}
		}
		if state {
			page.RestoreGraphicsState()
		}
	}
}

// overflows reports whether something of the image is drawn outside its
// size, which is cut off then: a point of a path, wider by half its stroke,
// or the box of a run of a text, from its ascent to its descent.
func (image *SVGImage) overflows(page *Page) bool {
	if image.w <= 0.0 || image.h <= 0.0 {
		return false
	}
	for _, path := range image.paths {
		if path.text != nil {
			m := path.text.matrix
			for _, run := range path.text.runs {
				corners := [4][2]float32{
					{run.px, run.py - run.fontSize},
					{run.px + run.width, run.py - run.fontSize},
					{run.px, run.py + float32(0.3*run.fontSize)},
					{run.px + run.width, run.py + float32(0.3*run.fontSize)}}
				for _, corner := range corners {
					x, y := svgTransform(m, corner[0], corner[1])
					if image.outside(x, y, 0.0) {
						return true
					}
				}
			}
			continue
		}
		if !image.isWritable(path, page) {
			continue // Not drawn
		}
		margin := float32(0.0)
		if path.stroke != color.Transparent {
			margin = float32(path.strokeWidth / 2.0)
		}
		for _, op := range path.operations {
			if image.outside(op.x, op.y, margin) || op.cmd == 'C' &&
				(image.outside(op.x1, op.y1, margin) || image.outside(op.x2, op.y2, margin)) {
				return true
			}
		}
	}
	return false
}

// outside reports whether a point is outside the size of the image, by more
// than a hundredth of a point and the margin.
func (image *SVGImage) outside(px, py, margin float32) bool {
	e := 0.01 + margin
	return px < -e || py < -e || px > image.w+e || py > image.h+e
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
	// A linked image is a Figure in the Link its annotation joins
	var link *structElement
	if image.uri != "" || image.key != "" {
		link = page.beginLink()
	}
	page.AddBDC(structelem.Figure, image.language, image.actualText, image.altDescription)
	regular := image.font
	bold := image.boldFont
	for _, path := range image.paths {
		if path.text != nil {
			if regular == nil {
				regular = image.coreFont(page.pdf, false)
			}
			if bold == nil {
				bold = image.font
				if bold == nil {
					bold = image.coreFont(page.pdf, true)
				}
			}
			path.text.place(regular, bold)
		}
	}
	clip := image.overflows(page)
	if clip {
		page.SaveGraphicsState()
		page.ClipRect(image.x, image.y, image.w, image.h)
	}
	for _, path := range image.paths {
		if path.text != nil {
			image.drawText(path.text, path.clips, page)
		} else {
			image.drawPath(path, page)
		}
	}
	if clip {
		page.RestoreGraphicsState()
	}
	page.SetFigureBoundingBox(image.x, image.y, image.w, image.h)
	page.AddEMC()
	page.endLink(link)
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
			linkElement:    link,
		})
	}
	return [2]float32{image.x + image.w, image.y + image.h}
}
