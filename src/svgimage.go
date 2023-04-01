package pdfjet

/**
 * svgimage.go
 *
Copyright 2023 Innovatics Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

import (
	"bufio"
	"io"
	"io/ioutil"
	"log"
	"os"
	"strconv"
	"strings"

	"github.com/edragoev1/pdfjet/src/color"
)

type SVGImage struct {
	x, y, w, h     float32
	viewBox        string
	fill           int32
	stroke         int32
	strokeWidth    float32
	paths          []*SVGPath
	uri            *string
	key            *string
	language       string
	altDescription string
	actualText     string
	structureType  string
}

func NewSVGImageFromFile(filePath string) *SVGImage {
	file, err := os.Open(filePath)
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	reader := bufio.NewReader(file)
	return NewSVGImage(reader)
}

/**
 * Used to embed SVG images in the PDF document.
 *
 * @param stream the input stream.
 */
func NewSVGImage(reader io.Reader) *SVGImage {
	image := new(SVGImage)
	colorMap := NewColorMap()
	image.paths = make([]*SVGPath, 0)
	var path *SVGPath
	buffer, err := ioutil.ReadAll(reader)
	if err != nil {
		log.Fatal(err)
	}
	var builder = strings.Builder{}
	var token = false
	var header = false
	var param string
	for i := 0; i < len(buffer); i++ {
		ch := buffer[i]
		if strings.HasSuffix(builder.String(), "<svg") {
			header = true
			builder.Reset()
		} else if ch == '>' {
			header = false
			builder.Reset()
		} else if !token && strings.HasSuffix(builder.String(), " width=") {
			token = true
			param = "width"
			builder.Reset()
		} else if !token && strings.HasSuffix(builder.String(), " height=") {
			token = true
			param = "height"
			builder.Reset()
		} else if !token && strings.HasSuffix(builder.String(), " viewBox=") {
			token = true
			param = "viewBox"
			builder.Reset()
		} else if !token && strings.HasSuffix(builder.String(), " d=") {
			token = true
			if path != nil {
				image.paths = append(image.paths, path)
			}
			path = NewSVGPath()
			token = true
			param = "data"
			builder.Reset()
		} else if !token && strings.HasSuffix(builder.String(), " fill=") {
			token = true
			param = "fill"
			builder.Reset()
		} else if !token && strings.HasSuffix(builder.String(), " stroke=") {
			token = true
			param = "stroke"
			builder.Reset()
		} else if !token && strings.HasSuffix(builder.String(), " stroke-width=") {
			token = true
			param = "stroke-width"
			builder.Reset()
		} else if token && ch == '"' {
			token = false
			if param == "width" {
				width, err := strconv.ParseFloat(builder.String(), 32)
				if err == nil {
					image.w = float32(width)
				} else {
					log.Fatal(err)
				}
			} else if param == "height" {
				height, err := strconv.ParseFloat(builder.String(), 32)
				if err == nil {
					image.h = float32(height)
				} else {
					log.Fatal(err)
				}
			} else if param == "viewBox" {
				image.viewBox = builder.String()
			} else if param == "data" {
				path.data = builder.String()
			} else if param == "fill" {
				var fillColor = getColor(colorMap, builder.String())
				if header {
					image.fill = fillColor
				} else {
					path.fill = fillColor
				}
			} else if param == "stroke" {
				var strokeColor = getColor(colorMap, builder.String())
				if header {
					image.stroke = strokeColor
				} else {
					path.stroke = strokeColor
				}
			} else if param == "stroke-width" {
				strokeWidth, err := strconv.ParseFloat(builder.String(), 32)
				if err == nil {
					if header {
						image.strokeWidth = float32(strokeWidth)
					} else {
						path.strokeWidth = float32(strokeWidth)
					}
				} else {
					if header {
						image.strokeWidth = 0.0
					} else {
						path.strokeWidth = 0.0
					}
				}
			}
			builder.Reset()
		} else {
			builder.WriteByte(ch)
		}
	}
	if path != nil {
		image.paths = append(image.paths, path)
	}
	image.processPaths(image.paths)
	return image
}

func (image *SVGImage) processPaths(paths []*SVGPath) {
	box := make([]float32, 4)
	if image.viewBox != "" {
		view := strings.Fields(strings.TrimSpace(image.viewBox))
		val0, err := strconv.ParseFloat(view[0], 32)
		if err != nil {
			log.Fatal(err)
		}
		val1, err := strconv.ParseFloat(view[1], 32)
		if err != nil {
			log.Fatal(err)
		}
		val2, err := strconv.ParseFloat(view[2], 32)
		if err != nil {
			log.Fatal(err)
		}
		val3, err := strconv.ParseFloat(view[3], 32)
		if err != nil {
			log.Fatal(err)
		}
		box[0] = float32(val0)
		box[1] = float32(val1)
		box[2] = float32(val2)
		box[3] = float32(val3)
	}
	svg := NewSVG()
	for _, path := range paths {
		path.operations = svg.GetOperations(path.data)
		path.operations = svg.ToPDF(path.operations)
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
}

func getColor(colorMap map[string]int32, colorName string) int32 {
	if strings.HasPrefix(colorName, "#") {
		if len(colorName) == 7 {
			color, err := strconv.ParseInt(colorName[1:], 16, 32)
			if err != nil {
				log.Fatal(err)
			}
			return int32(color)
		} else if len(colorName) == 4 {
			str := string([]byte{
				colorName[1], colorName[1],
				colorName[2], colorName[2],
				colorName[3], colorName[3],
			})
			color, err := strconv.ParseInt(str, 16, 32)
			if err != nil {
				log.Fatal(err)
			}
			return int32(color)
		} else {
			return color.Transparent
		}
	}
	value, ok := colorMap[colorName]
	if ok {
		return value
	}
	return int32(color.Transparent)
}

func (image *SVGImage) SetLocation(x, y float32) {
	image.x = x
	image.y = y
}

func (image *SVGImage) GetWidth() float32 {
	return image.w
}

func (image *SVGImage) GetHeight() float32 {
	return image.h
}

func (image *SVGImage) drawPath(path *SVGPath, page *Page) {
	var fillColor = path.fill
	if fillColor == color.Transparent {
		fillColor = image.fill
	}
	var strokeColor = path.stroke
	if strokeColor == color.Transparent {
		strokeColor = image.stroke
	}
	var strokeWidth = image.strokeWidth
	if path.strokeWidth > strokeWidth {
		strokeWidth = path.strokeWidth
	}

	if fillColor == color.Transparent &&
		strokeColor == color.Transparent {
		fillColor = color.Black
	}

	page.SetBrushColor(fillColor)
	page.SetPenColor(strokeColor)
	page.SetPenWidth(strokeWidth)

	if fillColor != color.Transparent {
		for i := 0; i < len(path.operations); i++ {
			op := path.operations[i]
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
		for i := 0; i < len(path.operations); i++ {
			op := path.operations[i]
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
				page.ClosePath()
			}
		}
	}
}

func (image *SVGImage) DrawOn(page *Page) []float32 {
	page.AddBMC(image.structureType, image.language, image.actualText, image.altDescription)
	for i := 0; i < len(image.paths); i++ {
		path := image.paths[i]
		image.drawPath(path, page)
	}
	page.AddEMC()
	if image.uri != nil || image.key != nil {
		page.AddAnnotation(NewAnnotation(
			image.uri,
			image.key, // The destination name
			image.x,
			image.y,
			image.x+image.w,
			image.y+image.h,
			image.language,
			image.actualText,
			image.altDescription))
	}
	return []float32{image.x + image.w, image.y + image.h}
}
