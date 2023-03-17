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
	"io"
	"io/ioutil"
	"log"
	"strings"

	"github.com/edragoev1/pdfjet/src/color"
)

type SVGImage struct {
	x, y, w, h     float32
	pdfPathOps     []PathOp
	color          uint32
	penWidth       float32
	fillPath       bool
	uri            *string
	key            *string
	language       string
	altDescription string
	actualText     string
	structureType  string
}

/**
 * Used to embed SVG images in the PDF document.
 *
 * @param stream the input stream.
 */
func NewSVGImage(reader io.Reader) *SVGImage {
	svgImage := new(SVGImage)
	svgImage.fillPath = true
	svgImage.color = color.Black
	svgImage.penWidth = 0.3
	var paths = []string{}
	var builder = strings.Builder{}
	var inPath = false
	buf, err := ioutil.ReadAll(reader)
	if err != nil {
		log.Fatal(err)
	}
	for i := 0; i < len(buf); i++ {
		ch := buf[i]
		if !inPath && strings.HasSuffix(string(buf), "<path d=") {
			inPath = true
			builder.Reset()
		} else if inPath && ch == '"' {
			inPath = false
			paths = append(paths, builder.String())
			builder.Reset()
		} else {
			builder.WriteByte(ch)
		}
	}
	print(builder.String())
	svg := NewSVG()
	svgPathOps := svg.GetSVGPathOps(paths)
	println(len(svgPathOps))
	svgImage.pdfPathOps = svg.GetPDFPathOps(svgPathOps)
	return svgImage
}

func (image *SVGImage) SetLocation(x, y float32) {
	image.x = x
	image.y = y
}

func (image *SVGImage) SetPenWidth(w float32) {
	image.w = w
}

func (image *SVGImage) SetSize(w, h float32) {
	image.w = w
	image.h = h
}

func (image *SVGImage) SetHeight(h float32) {
	image.h = h
}

func (image *SVGImage) GetPenWidth() float32 {
	return image.w
}

func (image *SVGImage) GetHeight() float32 {
	return image.h
}

func (image *SVGImage) DrawOn(page *Page) []float32 {
	page.AddBMC(image.structureType, image.language, image.actualText, image.altDescription)
	page.SetPenWidth(image.penWidth)
	if image.fillPath {
		page.SetBrushColor(image.color)
	} else {
		page.SetPenColor(image.color)
	}
	for i := 0; i < len(image.pdfPathOps); i++ {
		op := image.pdfPathOps[i]
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
			if !image.fillPath {
				page.ClosePath()
			}
		}
	}
	if image.fillPath {
		page.FillPath()
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
