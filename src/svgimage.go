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
)

type SVGImage struct {
	x, y, w, h     float32
	pdfPathOps     []PathOp
	color          uint32
	penWidth       float32
	fillPath       bool
	uri            string
	key            string
	language       string
	actualText     string
	altDescription string
}

/**
 * Used to embed SVG images in the PDF document.
 *
 * @param stream the input stream.
 */
func NewSVGImage(reader io.Reader) *SVGImage {
	svgImage := new(SVGImage)
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
			paths = append(paths, string(buf))
			builder.Reset()
		} else {
			builder.WriteByte(ch)
		}
	}
	svg := NewSVG()
	svgPathOps := svg.GetSVGPathOps(paths)
	svgImage.pdfPathOps = svg.GetPDFPathOps(svgPathOps)
	return svgImage
}
