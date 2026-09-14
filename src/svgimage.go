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
	"os"
	"strconv"
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// SVGImage is used to draw svgParser images on a page.
type SVGImage struct {
	x, y, w, h     float32
	viewBox        string
	fill           int32
	stroke         int32
	fillNone       bool // fill="none" on the svg element
	strokeNone     bool // stroke="none" on the svg element
	strokeWidth    float32
	paths          []*svgPath
	uri            string
	key            string
	language       string
	altDescription string
	actualText     string
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

// NewSVGImage parses an svgParser image from a reader, for embedding in a PDF document.
// Only the attributes of the <svg> and <path> elements are interpreted:
// width, height, viewBox, fill, stroke, stroke-width, and d.
func NewSVGImage(reader io.Reader) (*SVGImage, error) {
	image := new(SVGImage)
	colorMap := newColorMap()
	image.fill = color.Transparent
	image.stroke = color.Transparent
	image.paths = make([]*svgPath, 0)

	decoder := xml.NewDecoder(reader)
	for {
		token, err := decoder.Token()
		if err == io.EOF {
			break
		}
		if err != nil {
			return nil, fmt.Errorf("parsing SVG: %w", err)
		}

		start, ok := token.(xml.StartElement)
		if !ok {
			continue
		}

		switch start.Name.Local {
		case "svg":
			for _, attr := range start.Attr {
				switch attr.Name.Local {
				case "width":
					w, err := parseDim(attr.Value)
					if err != nil {
						return nil, fmt.Errorf("invalid SVG width: %w", err)
					}
					image.w = w
				case "height":
					h, err := parseDim(attr.Value)
					if err != nil {
						return nil, fmt.Errorf("invalid SVG height: %w", err)
					}
					image.h = h
				case "viewBox":
					image.viewBox = attr.Value
				case "fill":
					c, err := getColor(colorMap, attr.Value)
					if err != nil {
						return nil, fmt.Errorf("invalid SVG fill: %w", err)
					}
					image.fill = c
					image.fillNone = isNone(attr.Value)
				case "stroke":
					c, err := getColor(colorMap, attr.Value)
					if err != nil {
						return nil, fmt.Errorf("invalid SVG stroke: %w", err)
					}
					image.stroke = c
					image.strokeNone = isNone(attr.Value)
				case "stroke-width":
					sw, err := parseFloatLenient(attr.Value)
					if err != nil {
						return nil, fmt.Errorf("invalid SVG stroke-width: %w", err)
					}
					image.strokeWidth = sw
				}
			}

		case "path":
			path := newSVGPath()
			for _, attr := range start.Attr {
				switch attr.Name.Local {
				case "d":
					path.data = attr.Value
				case "fill":
					c, err := getColor(colorMap, attr.Value)
					if err != nil {
						return nil, fmt.Errorf("invalid path fill: %w", err)
					}
					path.fill = c
					path.fillNone = isNone(attr.Value)
				case "stroke":
					c, err := getColor(colorMap, attr.Value)
					if err != nil {
						return nil, fmt.Errorf("invalid path stroke: %w", err)
					}
					path.stroke = c
					path.strokeNone = isNone(attr.Value)
				case "stroke-width":
					sw, err := parseFloatLenient(attr.Value)
					if err != nil {
						return nil, fmt.Errorf("invalid path stroke-width: %w", err)
					}
					path.strokeWidth = sw
				}
			}
			image.paths = append(image.paths, path)
		}
	}

	if err := image.processPaths(image.paths); err != nil {
		return nil, err
	}
	return image, nil
}

// parseDim parses an svgParser dimension value such as "100".
func parseDim(value string) (float32, error) {
	v, err := strconv.ParseFloat(strings.TrimSpace(value), 32)
	if err != nil {
		return 0, fmt.Errorf("%q: %w", value, err)
	}
	return float32(v), nil
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
	var box [4]float32
	if image.viewBox != "" {
		list := strings.Fields(strings.TrimSpace(image.viewBox))
		if len(list) != 4 {
			return fmt.Errorf("invalid viewBox %q: expected 4 values, got %d", image.viewBox, len(list))
		}
		for i := range box {
			val, err := strconv.ParseFloat(list[i], 32)
			if err != nil {
				return fmt.Errorf("invalid viewBox %q: %w", image.viewBox, err)
			}
			box[i] = float32(val)
		}
		if box[2] == 0 || box[3] == 0 {
			return fmt.Errorf("degenerate viewBox %q: zero width or height", image.viewBox)
		}
	}

	svg := newSVGParser()
	for _, path := range paths {
		operations, err := toPDF(svg.getOperations(path.data))
		if err != nil {
			return fmt.Errorf("invalid path data %q: %w", path.data, err)
		}
		path.operations = operations
		if image.viewBox != "" {
			for _, op := range path.operations {
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

func getColor(colorMap map[string]int32, colorName string) (int32, error) {
	if strings.HasPrefix(colorName, "#") {
		hex := colorName[1:]
		switch len(hex) {
		case 6:
			value, err := strconv.ParseInt(hex, 16, 32)
			if err != nil {
				return 0, fmt.Errorf("invalid color %q: %w", colorName, err)
			}
			return int32(value), nil
		case 3:
			expanded := string([]byte{
				hex[0], hex[0],
				hex[1], hex[1],
				hex[2], hex[2],
			})
			value, err := strconv.ParseInt(expanded, 16, 32)
			if err != nil {
				return 0, fmt.Errorf("invalid color %q: %w", colorName, err)
			}
			return int32(value), nil
		}
		return int32(color.Transparent), nil
	}
	if value, ok := colorMap[colorName]; ok {
		return value, nil
	}
	return int32(color.Transparent), nil
}

// ScaleBy scales this svgParser image by the specified factor.
func (image *SVGImage) ScaleBy(factor float32) *SVGImage {
	for _, path := range image.paths {
		for _, op := range path.operations {
			op.x1 *= factor
			op.y1 *= factor
			op.x2 *= factor
			op.y2 *= factor
			op.x *= factor
			op.y *= factor
		}
	}
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

// isNone reports whether the value is none, which turns the fill or the stroke off.
func isNone(value string) bool {
	return strings.TrimSpace(value) == "none"
}

func (image *SVGImage) drawPath(path *svgPath, page *Page) {
	// none on the path wins over the color of the svg element; a color that
	// is not set is taken from the svg element.
	noFill := path.fillNone || (path.fill == color.Transparent && image.fillNone)
	fillColor := path.fill
	if noFill {
		fillColor = color.Transparent
	} else if fillColor == color.Transparent {
		fillColor = image.fill
	}
	noStroke := path.strokeNone || (path.stroke == color.Transparent && image.strokeNone)
	strokeColor := path.stroke
	if noStroke {
		strokeColor = color.Transparent
	} else if strokeColor == color.Transparent {
		strokeColor = image.stroke
	}
	var strokeWidth = image.strokeWidth
	if path.strokeWidth > strokeWidth {
		strokeWidth = path.strokeWidth
	}

	// A path whose fill is not none, with no fill and no stroke color, is
	// filled black, as svgParser fills a path black by default.
	if !noFill && fillColor == color.Transparent &&
		strokeColor == color.Transparent {
		fillColor = color.Black
	}
	if fillColor == color.Transparent && strokeColor == color.Transparent {
		return // fill="none" and no stroke: nothing to draw
	}

	page.SetBrushColor(fillColor)
	page.SetPenColor(strokeColor)
	page.SetPenWidth(strokeWidth)

	if fillColor != color.Transparent {
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
		page.FillPath()
	}

	if strokeColor != color.Transparent {
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
}

// DrawOn draws this svgParser image on the specified page.
func (image *SVGImage) DrawOn(page *Page) [2]float32 {
	page.AddBDC(structelem.P, image.language, image.actualText, image.altDescription)
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
			transparency:   0.0,
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
