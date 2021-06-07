package pdfjet

/**
 * page.go
 *
Copyright 2020 Innovatics Inc.

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
	"fmt"
	"log"
	"math"
	"pdfjet/color"
	"pdfjet/compliance"
	"pdfjet/corefont"
	"pdfjet/operation"
	"pdfjet/shape"
	"strings"
	"unicode"
)

// Page is used to create PDF page objects.
//
// Please note:
// <pre>
//   The coordinate (0.0, 0.0) is the top left corner of the page.
//   The size of the pages are represented in points.
//   1 point is 1/72 inches.
// </pre>
type Page struct {
	pdf           *PDF
	buf           []byte
	pageObj       *PDFobj
	objNumber     int
	tm            [4]float32
	renderingMode int
	width         float32
	height        float32
	contents      []int
	annots        []*Annotation
	destinations  []*Destination
	cropBox       []float32
	bleedBox      []float32
	trimBox       []float32
	artBox        []float32
	structures    []*StructElem
	pen           [3]float32
	brush         [3]float32
	penCMYK       [4]float32
	brushCMYK     [4]float32
	penWidth      float32
	lineCapStyle  int
	lineJoinStyle int
	linePattern   string
	font          *Font
	savedStates   []*State
	mcid          int
	savedHeight   float32
}

// Constants from Android's Matrix object:
const (
	mScaleX = iota
	mSkewX
	mTransX
	mScaleY
	mSkewY
	mTransY
)

/**
 *  Creates page object and add it to the PDF document.
 *
 *  Please note:
 *  <pre>
 *  The coordinate (0.0, 0.0) is the top left corner of the page.
 *  The size of the pages are represented in points.
 *  1 point is 1/72 inches.
 *  </pre>
 *
 *  @param pdf the pdf object.
 *  @param pageSize the page size of this page.
 */
/*
func NewPage(pdf PDF, pageSize []float32) {
	return NewPage(pdf, pageSize, true)
}
*/

// NewPage constructs page object and adds it to the PDF document.
//
// Please note:
// <pre>
//   The coordinate (0.0, 0.0) is the top left corner of the page.
//   The size of the pages are represented in points.
//   1 point is 1/72 inches.
// </pre>
//
// @param pdf the pdf object.
// @param pageSize the page size of this page.
// @param addPageToPDF boolean flag.
func NewPage(pdf *PDF, pageSize [2]float32, addPageToPDF bool) *Page {
	page := new(Page)
	page.pdf = pdf
	page.contents = make([]int, 0)
	page.width = pageSize[0]
	page.height = pageSize[1]
	page.linePattern = "[] 0"
	page.savedHeight = math.MaxFloat32
	page.tm = [4]float32{1.0, 0.0, 0.0, 1.0}
	page.buf = make([]byte, 0)
	page.penWidth = -1.0
	if addPageToPDF {
		pdf.AddPage(page)
	}
	return page
}

// NewPageFromObject creates page object from PDFobj.
func NewPageFromObject(pdf *PDF, pageObj *PDFobj) *Page {
	page := new(Page)
	page.pdf = pdf
	page.pageObj = pageObj
	page.width = pageObj.GetPageSize()[0]
	page.height = pageObj.GetPageSize()[1]
	page.tm = [4]float32{1.0, 0.0, 0.0, 1.0}
	page.buf = make([]byte, 0)
	appendString(&page.buf, "q\n")
	if pageObj.gsNumber != 0 {
		appendString(&page.buf, "/GS")
		appendInteger(&page.buf, pageObj.gsNumber+1)
		appendString(&page.buf, " gs\n")
	}
	return page
}

// AddCoreFontResource adds core font to the PDF objects.
func (page *Page) AddCoreFontResource(coreFont *corefont.CoreFont, objects *[]*PDFobj) *Font {
	return page.pageObj.addCoreFontResource(coreFont, objects)
}

// AddImageResource adds an image to the PDF objects.
func (page *Page) AddImageResource(image *Image, objects *[]*PDFobj) {
	page.pageObj.AddImageResource(image, objects)
}

// AddFontResource adds font to the PDF objects.
func (page *Page) AddFontResource(font *Font, objects *[]*PDFobj) {
	page.pageObj.AddFontResource(font, objects)
}

// Complete completes adding content to the existing PDF.
func (page *Page) Complete(objects *[]*PDFobj) {
	appendString(&page.buf, "Q\n")
	page.pageObj.addContent(page.getContent(), objects)
}

func (page *Page) getContent() []byte {
	return page.buf
}

// addDestination adds destination to this page.
// @param name The destination name.
// @param yPosition The vertical position of the destination on this page.
func (page *Page) addDestination(name string, yPosition float32) *Destination {
	dest := NewDestination(name, page.height-yPosition)
	page.destinations = append(page.destinations, dest)
	return dest
}

// GetWidth returns the width of this page.
func (page *Page) GetWidth() float32 {
	return page.width
}

// GetHeight returns the height of this page.
func (page *Page) GetHeight() float32 {
	return page.height
}

// DrawLine draws a line on the page, using the current color, between the points (x1, y1) and (x2, y2).
func (page *Page) DrawLine(x1, y1, x2, y2 float32) {
	page.MoveTo(x1, y1)
	page.LineTo(x2, y2)
	page.StrokePath()
}

// DrawString draws a string using the specified font1 and font2 at the x, y location.
func (page *Page) DrawString(font1 *Font, font2 *Font, text string, x, y float32) {
	page.DrawStringUsingColorMap(font1, font2, text, x, y, nil)
}

// DrawStringUsingColorMap draws the text given by the specified string,
// using the specified main font and the current brush color.
// If the main font is missing some glyphs - the fallback font is used.
// The baseline of the leftmost character is at position (x, y) on the page.
func (page *Page) DrawStringUsingColorMap(
	font, fallbackFont *Font, text string, x, y float32, colors map[string]uint32) {
	if font.isCoreFont || font.isCJK || fallbackFont == nil || fallbackFont.isCoreFont || fallbackFont.isCJK {
		page.drawString(font, text, x, y, colors)
	} else {
		activeFont := font
		var buf strings.Builder
		runes := []rune(text)
		for _, ch := range runes {
			if activeFont.unicodeToGID[ch] == 0 {
				page.drawString(activeFont, buf.String(), x, y, colors)
				x += activeFont.stringWidth(buf.String())
				buf.Reset()
				// Switch the active font
				if activeFont == font {
					activeFont = fallbackFont
				} else {
					activeFont = font
				}
			}
			buf.WriteRune(ch)
		}
		page.drawString(activeFont, buf.String(), x, y, colors)
	}
}

// drawString draws the text given by the specified string,
// using the specified font and the current brush color.
// The baseline of the leftmost character is at position (x, y) on the page.
//
// @param font the font to use.
// @param str the string to be drawn.
// @param x the x coordinate.
// @param y the y coordinate.
func (page *Page) drawString(font *Font, str string, x, y float32, colors map[string]uint32) {
	if str == "" {
		return
	}

	appendString(&page.buf, "BT\n")

	// if font.fontID == nil {
	if font.fontID == "" {
		page.SetTextFont(font)
	} else {
		appendString(&page.buf, "/")
		appendString(&page.buf, font.fontID)
		appendString(&page.buf, " ")
		appendFloat32(&page.buf, font.size)
		appendString(&page.buf, " Tf\n")
	}

	if page.renderingMode != 0 {
		appendInteger(&page.buf, page.renderingMode)
		appendString(&page.buf, " Tr\n")
	}

	var skew float32 = 0.0
	if font.skew15 &&
		page.tm[0] == 1.0 &&
		page.tm[1] == 0.0 &&
		page.tm[2] == 0.0 &&
		page.tm[3] == 1.0 {
		skew = 0.26
	}

	appendFloat32(&page.buf, page.tm[0])
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, page.tm[1])
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, page.tm[2]+skew)
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, page.tm[3])
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, x)
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, page.height-y)
	appendString(&page.buf, " Tm\n")

	if colors == nil {
		appendString(&page.buf, "[<")
		if font.isCoreFont {
			page.drawASCIIString(font, str)
		} else {
			page.drawUnicodeString(font, str)
		}
		appendString(&page.buf, ">] TJ\n")
	} else {
		page.drawColoredString(font, str, colors)
	}

	appendString(&page.buf, "ET\n")
}

func (page *Page) drawASCIIString(font *Font, text string) {
	runes := []rune(text)
	for i, c1 := range runes {
		if c1 < font.firstChar || c1 > font.lastChar {
			appendString(&page.buf, fmt.Sprintf("%02X", 0x20))
			return
		}
		appendString(&page.buf, fmt.Sprintf("%02X", c1))
		if font.isCoreFont && font.kernPairs && i < (len(runes)-1) {
			c1 -= 32
			c2 := runes[i+1]
			if c2 < font.firstChar || c2 > font.lastChar {
				c2 = 32
			}
			for i := 2; i < len(font.metrics[c1]); i += 2 {
				if font.metrics[c1][i] == int(c2) {
					appendString(&page.buf, ">")
					appendInteger(&page.buf, -page.font.metrics[c1][i+1])
					appendString(&page.buf, "<")
					break
				}
			}
		}
	}
}

func (page *Page) drawUnicodeString(font *Font, text string) {
	runes := []rune(text)
	if font.isCJK {
		for _, c1 := range runes {
			if c1 == 0xFEFF { // BOM marker
				continue
			}
			if c1 < font.firstChar || c1 > font.lastChar {
				appendString(&page.buf, fmt.Sprintf("%04X", 0x0020))
			} else {
				appendString(&page.buf, fmt.Sprintf("%04X", c1))
			}
		}
	} else {
		for _, c1 := range runes {
			if c1 < font.firstChar || c1 > font.lastChar {
				appendString(&page.buf, fmt.Sprintf("%04X", font.unicodeToGID[0x0020]))
			} else {
				appendString(&page.buf, fmt.Sprintf("%04X", font.unicodeToGID[c1]))
			}
		}
	}
}

// SetGraphicsState sets the graphics state. Please see Example_31.
// @param gs the graphics state to use.
func (page *Page) SetGraphicsState(gs *GraphicsState) {
	var buf strings.Builder
	buf.WriteString("/CA ")
	buf.WriteString(fmt.Sprintf("%.2f", gs.GetAlphaStroking()))
	buf.WriteString(" ")
	buf.WriteString("/ca ")
	buf.WriteString(fmt.Sprintf("%.2f", gs.GetAlphaNonStroking()))
	state := buf.String()
	n, ok := page.pdf.states[state]
	if !ok {
		n = len(page.pdf.states) + 1
		page.pdf.states[state] = n
	}
	appendString(&page.buf, "/GS")
	appendInteger(&page.buf, n)
	appendString(&page.buf, " gs\n")
}

// SetPenColorRGB sets the color for stroking operations.
// The pen color is used when drawing lines and splines.
//
// @param r the red component is float value from 0.0 to 1.0.
// @param g the green component is float value from 0.0 to 1.0.
// @param b the blue component is float value from 0.0 to 1.0.
func (page *Page) SetPenColorRGB(r, g, b float32) {
	if page.pen[0] != r || page.pen[1] != g || page.pen[2] != b {
		page.SetColorRGB(r, g, b)
		appendString(&page.buf, " RG\n")
		page.pen[0] = r
		page.pen[1] = g
		page.pen[2] = b
	}
}

// SetPenColorCMYK sets the color for stroking operations using CMYK.
// The pen color is used when drawing lines and splines.
//
// @param c the cyan component is float value from 0.0 to 1.0.
// @param m the magenta component is float value from 0.0 to 1.0.
// @param y the yellow component is float value from 0.0 to 1.0.
// @param k the black component is float value from 0.0 to 1.0.
func (page *Page) SetPenColorCMYK(c, m, y, k float32) {
	if page.penCMYK[0] != c || page.penCMYK[1] != m || page.penCMYK[2] != y || page.penCMYK[3] != k {
		page.SetColorCMYK(c, m, y, k)
		appendString(&page.buf, " K\n")
		page.penCMYK[0] = c
		page.penCMYK[1] = m
		page.penCMYK[2] = y
		page.penCMYK[3] = k
	}
}

// setBrushColorRGB sets the color for brush operations.
// This is the color used when drawing regular text and filling shapes.
// @param r the red component is float value from 0.0 to 1.0.
// @param g the green component is float value from 0.0 to 1.0.
// @param b the blue component is float value from 0.0 to 1.0.
func (page *Page) setBrushColorRGB(r, g, b float32) {
	if page.brush[0] != r || page.brush[1] != g || page.brush[2] != b {
		page.SetColorRGB(r, g, b)
		appendString(&page.buf, " rg\n")
		page.brush[0] = r
		page.brush[1] = g
		page.brush[2] = b
	}
}

// SetBrushColorCMYK sets the color for brush operations using CMYK.
// This is the color used when drawing regular text and filling shapes.
// @param c the cyan component is float value from 0.0 to 1.0.
// @param m the magenta component is float value from 0.0 to 1.0.
// @param y the yellow component is float value from 0.0 to 1.0.
// @param k the black component is float value from 0.0 to 1.0.
func (page *Page) SetBrushColorCMYK(c, m, y, k float32) {
	if page.brushCMYK[0] != c || page.brushCMYK[1] != m || page.brushCMYK[2] != y || page.brushCMYK[3] != k {
		page.SetColorCMYK(c, m, y, k)
		appendString(&page.buf, " k\n")
		page.brushCMYK[0] = c
		page.brushCMYK[1] = m
		page.brushCMYK[2] = y
		page.brushCMYK[3] = k
	}
}

// SetBrushColorFloat32Array sets the color for brush operations.
// @param color the color.
func (page *Page) SetBrushColorFloat32Array(color [3]float32) {
	page.setBrushColorRGB(color[0], color[1], color[2])
}

// GetBrushColor returns the brush color.
// @return the brush color.
func (page *Page) GetBrushColor() [3]float32 {
	return page.brush
}

// SetColorRGB sets the RGB color.
func (page *Page) SetColorRGB(r, g, b float32) {
	appendFloat32(&page.buf, r)
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, g)
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, b)
}

// SetColorCMYK sets the CMYK color.
func (page *Page) SetColorCMYK(c, m, y, k float32) {
	appendFloat32(&page.buf, c)
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, m)
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, y)
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, k)
}

// SetPenColor sets the pen color.
// See the Color class for predefined values or define your own using 0x00RRGGBB packed integers.
func (page *Page) SetPenColor(color uint32) {
	r := float32(((color >> 16) & 0xff)) / 255.0
	g := float32(((color >> 8) & 0xff)) / 255.0
	b := float32(((color) & 0xff)) / 255.0
	page.SetPenColorRGB(r, g, b)
}

// SetBrushColor sets the brush color.
// See the Color class for predefined values or define your own using 0x00RRGGBB packed integers.
func (page *Page) SetBrushColor(color uint32) {
	r := float32(((color >> 16) & 0xff)) / 255.0
	g := float32(((color >> 8) & 0xff)) / 255.0
	b := float32(((color) & 0xff)) / 255.0
	page.setBrushColorRGB(r, g, b)
}

// SetDefaultLineWidth sets the line width to the default.
// The default is the finest line width.
func (page *Page) SetDefaultLineWidth() {
	if page.penWidth != 0.0 {
		page.penWidth = 0.0
		appendFloat32(&page.buf, page.penWidth)
		appendString(&page.buf, " w\n")
	}
}

// SetLinePattern the line dash pattern controls the pattern of dashes and gaps used to stroke paths.
// It is specified by a dash array and a dash phase.
// The elements of the dash array are positive numbers that specify the lengths of
// alternating dashes and gaps.
// The dash phase specifies the distance into the dash pattern at which to start the dash.
// The elements of both the dash array and the dash phase are expressed in user space units.
// <pre>
// Examples of line dash patterns:
//
//     "[Array] Phase"     Appearance          Description
//      _______________     _________________   ____________________________________
//
//      "[] 0"              -----------------   Solid line
//      "[3] 0"             ---   ---   ---     3 units on, 3 units off, ...
//      "[2] 1"             -  --  --  --  --   1 on, 2 off, 2 on, 2 off, ...
//      "[2 1] 0"           -- -- -- -- -- --   2 on, 1 off, 2 on, 1 off, ...
//      "[3 5] 6"             ---     ---       2 off, 3 on, 5 off, 3 on, 5 off, ...
//      "[2 3] 11"          -   --   --   --    1 on, 3 off, 2 on, 3 off, 2 on, ...
// </pre>
//
// @param pattern the line dash pattern.
func (page *Page) SetLinePattern(pattern string) {
	if page.linePattern != pattern {
		page.linePattern = pattern
		appendString(&page.buf, page.linePattern)
		appendString(&page.buf, " d\n")
	}
}

// SetDefaultLinePattern sets the default line dash pattern - solid line.
func (page *Page) SetDefaultLinePattern() {
	appendString(&page.buf, "[] 0")
	appendString(&page.buf, " d\n")
}

// SetPenWidth sets the pen width that will be used to draw lines and splines on this page.
func (page *Page) SetPenWidth(width float32) {
	if page.penWidth != width {
		page.penWidth = width
		appendFloat32(&page.buf, page.penWidth)
		appendString(&page.buf, " w\n")
	}
}

// SetLineCapStyle sets the current line cap style.
// Supported values: Cap.BUTT, Cap.ROUND and Cap.PROJECTING_SQUARE
func (page *Page) SetLineCapStyle(style int) {
	if page.lineCapStyle != style {
		page.lineCapStyle = style
		appendInteger(&page.buf, page.lineCapStyle)
		appendString(&page.buf, " J\n")
	}
}

// SetLineJoinStyle sets the line join style.
// Supported values: Join.MITER, Join.ROUND and Join.BEVEL
func (page *Page) SetLineJoinStyle(style int) {
	if page.lineJoinStyle != style {
		page.lineJoinStyle = style
		appendInteger(&page.buf, page.lineJoinStyle)
		appendString(&page.buf, " j\n")
	}
}

// MoveTo moves the pen to the point with coordinates (x, y) on the page.
//
// @param x the x coordinate of new pen position.
// @param y the y coordinate of new pen position.
func (page *Page) MoveTo(x, y float32) {
	appendFloat32(&page.buf, x)
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, page.height-y)
	appendString(&page.buf, " m\n")
}

// LineTo draws a line from the current pen position to the point with coordinates (x, y),
// using the current pen width and stroke color.
// Make sure you call strokePath(), closePath() or fillPath() after the last call to this method.
func (page *Page) LineTo(x, y float32) {
	appendFloat32(&page.buf, x)
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, page.height-y)
	appendString(&page.buf, " l\n")
}

// StrokePath draws the path using the current pen color.
func (page *Page) StrokePath() {
	appendString(&page.buf, "S\n")
}

// ClosePath closes the path and draws it using the current pen color.
func (page *Page) ClosePath() {
	appendString(&page.buf, "s\n")
}

// FillPath closes and fills the path with the current brush color.
func (page *Page) FillPath() {
	appendString(&page.buf, "f\n")
}

// DrawRect draws the outline of the specified rectangle on the page.
// The left and right edges of the rectangle are at x and x + w.
// The top and bottom edges are at y and y + h.
// The rectangle is drawn using the current pen color.
// @param x the x coordinate of the rectangle to be drawn.
// @param y the y coordinate of the rectangle to be drawn.
// @param w the width of the rectangle to be drawn.
// @param h the height of the rectangle to be drawn.
func (page *Page) DrawRect(x, y, w, h float32) {
	page.MoveTo(x, y)
	page.LineTo(x+w, y)
	page.LineTo(x+w, y+h)
	page.LineTo(x, y+h)
	page.ClosePath()
}

// FillRect fills the specified rectangle on the page.
// The left and right edges of the rectangle are at x and x + w.
// The top and bottom edges are at y and y + h.
// The rectangle is drawn using the current pen color.
// @param x the x coordinate of the rectangle to be drawn.
// @param y the y coordinate of the rectangle to be drawn.
// @param w the width of the rectangle to be drawn.
// @param h the height of the rectangle to be drawn.
func (page *Page) FillRect(x, y, w, h float32) {
	page.MoveTo(x, y)
	page.LineTo(x+w, y)
	page.LineTo(x+w, y+h)
	page.LineTo(x, y+h)
	page.FillPath()
}

// DrawPath draws or fills the specified path using the current pen or brush.
// @param path the path.
// @param operation specifies 'stroke' or 'fill' operation.
func (page *Page) DrawPath(path []*Point, operation string) {
	if len(path) < 2 {
		log.Fatal("The Path object must contain at least 2 points.")
	}
	point := path[0]
	page.MoveTo(point.x, point.y)
	var curve bool = false
	for i := 1; i < len(path); i++ {
		point = path[i]
		if point.isControlPoint {
			curve = true
			page.appendPoint(point)
		} else {
			if curve {
				curve = false
				page.appendPoint(point)
				appendString(&page.buf, "c\n")
			} else {
				page.LineTo(point.x, point.y)
			}
		}
	}

	appendString(&page.buf, operation)
	appendString(&page.buf, "\n")
}

// DrawCircle sdraws a circle on the page.
//
// The outline of the circle is drawn using the current pen color.
//
// @param x the x coordinate of the center of the circle to be drawn.
// @param y the y coordinate of the center of the circle to be drawn.
// @param r the radius of the circle to be drawn.
func (page *Page) DrawCircle(x, y, r float32) {
	page.drawEllipse(x, y, r, r, operation.Stroke)
}

// FillCircle draws the specified circle on the page and fills it with the current brush color.
//
// @param x the x coordinate of the center of the circle to be drawn.
// @param y the y coordinate of the center of the circle to be drawn.
// @param r the radius of the circle to be drawn.
// @param operation must be Operation.STROKE, Operation.CLOSE or Operation.FILL.
func (page *Page) FillCircle(x, y, r float32) {
	page.drawEllipse(x, y, r, r, operation.Fill)
}

// DrawEllipse draws an ellipse on the page using the current pen color.
// @param x the x coordinate of the center of the ellipse to be drawn.
// @param y the y coordinate of the center of the ellipse to be drawn.
// @param r1 the horizontal radius of the ellipse to be drawn.
// @param r2 the vertical radius of the ellipse to be drawn.
func (page *Page) DrawEllipse(x, y, r1, r2 float32) {
	page.drawEllipse(x, y, r1, r2, operation.Stroke)
}

// FillEllipse fills an ellipse on the page using the current pen color.
// @param x the x coordinate of the center of the ellipse to be drawn.
// @param y the y coordinate of the center of the ellipse to be drawn.
// @param r1 the horizontal radius of the ellipse to be drawn.
// @param r2 the vertical radius of the ellipse to be drawn.
func (page *Page) FillEllipse(x, y, r1, r2 float32) {
	page.drawEllipse(x, y, r1, r2, operation.Fill)
}

// drawEllipse draws an ellipse on the page and fills it using the current brush color.
// @param x the x coordinate of the center of the ellipse to be drawn.
// @param y the y coordinate of the center of the ellipse to be drawn.
// @param r1 the horizontal radius of the ellipse to be drawn.
// @param r2 the vertical radius of the ellipse to be drawn.
// @param operation the operation.
func (page *Page) drawEllipse(x, y, r1, r2 float32, operation string) {
	// The best 4-spline magic number
	var m4 float32 = 0.551784

	// Starting point
	page.MoveTo(x, y-r2)

	page.appendPointXY(x+m4*r1, y-r2)
	page.appendPointXY(x+r1, y-m4*r2)
	page.appendPointXY(x+r1, y)
	appendString(&page.buf, "c\n")

	page.appendPointXY(x+r1, y+m4*r2)
	page.appendPointXY(x+m4*r1, y+r2)
	page.appendPointXY(x, y+r2)
	appendString(&page.buf, "c\n")

	page.appendPointXY(x-m4*r1, y+r2)
	page.appendPointXY(x-r1, y+m4*r2)
	page.appendPointXY(x-r1, y)
	appendString(&page.buf, "c\n")

	page.appendPointXY(x-r1, y-m4*r2)
	page.appendPointXY(x-m4*r1, y-r2)
	page.appendPointXY(x, y-r2)
	appendString(&page.buf, "c\n")

	appendString(&page.buf, operation)
	appendString(&page.buf, "\n")
}

// DrawPoint draws a point on the page using the current pen color.
// @param p the point.
func (page *Page) DrawPoint(p *Point) {
	if p.shape != shape.Invisible {
		var list []*Point
		if p.shape == shape.Circle {
			if p.fillShape {
				page.FillCircle(p.x, p.y, p.r)
			} else {
				page.DrawCircle(p.x, p.y, p.r)
			}
		} else if p.shape == shape.Diamond {
			list = make([]*Point, 0)
			list = append(list, NewPoint(p.x, p.y-p.r))
			list = append(list, NewPoint(p.x+p.r, p.y))
			list = append(list, NewPoint(p.x, p.y+p.r))
			list = append(list, NewPoint(p.x-p.r, p.y))
			if p.fillShape {
				page.DrawPath(list, operation.Fill)
			} else {
				page.DrawPath(list, operation.Close)
			}
		} else if p.shape == shape.Box {
			list = make([]*Point, 0)
			list = append(list, NewPoint(p.x-p.r, p.y-p.r))
			list = append(list, NewPoint(p.x+p.r, p.y-p.r))
			list = append(list, NewPoint(p.x+p.r, p.y+p.r))
			list = append(list, NewPoint(p.x-p.r, p.y+p.r))
			if p.fillShape {
				page.DrawPath(list, operation.Fill)
			} else {
				page.DrawPath(list, operation.Close)
			}
		} else if p.shape == shape.Plus {
			page.DrawLine(p.x-p.r, p.y, p.x+p.r, p.y)
			page.DrawLine(p.x, p.y-p.r, p.x, p.y+p.r)
		} else if p.shape == shape.UpArrow {
			list = make([]*Point, 0)
			list = append(list, NewPoint(p.x, p.y-p.r))
			list = append(list, NewPoint(p.x+p.r, p.y+p.r))
			list = append(list, NewPoint(p.x-p.r, p.y+p.r))
			if p.fillShape {
				page.DrawPath(list, operation.Fill)
			} else {
				page.DrawPath(list, operation.Close)
			}
		} else if p.shape == shape.DownArrow {
			list = make([]*Point, 0)
			list = append(list, NewPoint(p.x-p.r, p.y-p.r))
			list = append(list, NewPoint(p.x+p.r, p.y-p.r))
			list = append(list, NewPoint(p.x, p.y+p.r))
			if p.fillShape {
				page.DrawPath(list, operation.Fill)
			} else {
				page.DrawPath(list, operation.Close)
			}
		} else if p.shape == shape.LeftArrow {
			list = make([]*Point, 0)
			list = append(list, NewPoint(p.x+p.r, p.y+p.r))
			list = append(list, NewPoint(p.x-p.r, p.y))
			list = append(list, NewPoint(p.x+p.r, p.y-p.r))
			if p.fillShape {
				page.DrawPath(list, operation.Fill)
			} else {
				page.DrawPath(list, operation.Close)
			}
		} else if p.shape == shape.RightArrow {
			list = make([]*Point, 0)
			list = append(list, NewPoint(p.x-p.r, p.y-p.r))
			list = append(list, NewPoint(p.x+p.r, p.y))
			list = append(list, NewPoint(p.x-p.r, p.y+p.r))
			if p.fillShape {
				page.DrawPath(list, operation.Fill)
			} else {
				page.DrawPath(list, operation.Close)
			}
		} else if p.shape == shape.HDash {
			page.DrawLine(p.x-p.r, p.y, p.x+p.r, p.y)
		} else if p.shape == shape.VDash {
			page.DrawLine(p.x, p.y-p.r, p.x, p.y+p.r)
		} else if p.shape == shape.XMark {
			page.DrawLine(p.x-p.r, p.y-p.r, p.x+p.r, p.y+p.r)
			page.DrawLine(p.x-p.r, p.y+p.r, p.x+p.r, p.y-p.r)
		} else if p.shape == shape.Multiply {
			page.DrawLine(p.x-p.r, p.y-p.r, p.x+p.r, p.y+p.r)
			page.DrawLine(p.x-p.r, p.y+p.r, p.x+p.r, p.y-p.r)
			page.DrawLine(p.x-p.r, p.y, p.x+p.r, p.y)
			page.DrawLine(p.x, p.y-p.r, p.x, p.y+p.r)
		} else if p.shape == shape.Star {
			angle := math.Pi / 10
			sin18 := float32(math.Sin(angle))
			cos18 := float32(math.Cos(angle))
			a := p.r * cos18
			b := p.r * sin18
			c := 2 * a * sin18
			d := 2*a*cos18 - p.r
			list = make([]*Point, 0)
			list = append(list, NewPoint(p.x, p.y-p.r))
			list = append(list, NewPoint(p.x+c, p.y+d))
			list = append(list, NewPoint(p.x-a, p.y-b))
			list = append(list, NewPoint(p.x+a, p.y-b))
			list = append(list, NewPoint(p.x-c, p.y+d))
			if p.fillShape {
				page.DrawPath(list, operation.Fill)
			} else {
				page.DrawPath(list, operation.Close)
			}
		}
	}
}

/**
 *  Sets the text rendering mode.
 *
 *  @param mode the rendering mode.
 */
func (page *Page) setTextRenderingMode(mode int) {
	if mode >= 0 && mode <= 7 {
		page.renderingMode = mode
	} else {
		log.Fatal("Invalid text rendering mode: " + fmt.Sprint(mode))
	}
}

// SetTextDirection sets the text direction.
// @param degrees the angle.
func (page *Page) SetTextDirection(degrees int) {
	if degrees > 360 {
		degrees %= 360
	}
	if degrees == 0 {
		page.tm = [4]float32{1.0, 0.0, 0.0, 1.0}
	} else if degrees == 90 {
		page.tm = [4]float32{0.0, 1.0, -1.0, 0.0}
	} else if degrees == 180 {
		page.tm = [4]float32{-1.0, 0.0, 0.0, -1.0}
	} else if degrees == 270 {
		page.tm = [4]float32{0.0, -1.0, 1.0, 0.0}
	} else if degrees == 360 {
		page.tm = [4]float32{1.0, 0.0, 0.0, 1.0}
	} else {
		sinOfAngle := float32(math.Sin(float64(degrees) * (math.Pi / 180)))
		cosOfAngle := float32(math.Cos(float64(degrees) * (math.Pi / 180)))
		page.tm = [4]float32{cosOfAngle, sinOfAngle, -sinOfAngle, cosOfAngle}
	}
}

/**
 *  Draws a bezier curve starting from the current point.
 *  <strong>Please note:</strong> You must call the fillPath, closePath or strokePath method after the last bezierCurveTo call.
 *  <p><i>Author:</i> <strong>Pieter Libin</strong>, pieter@emweb.be</p>
 *
 *  @param p1 first control point
 *  @param p2 second control point
 *  @param p3 end point
 */
func (page *Page) bezierCurveTo(p1, p2, p3 *Point) {
	page.appendPoint(p1)
	page.appendPoint(p2)
	page.appendPoint(p3)
	appendString(&page.buf, "c\n")
}

// SetTextStart sets the start of text block.
// Please see Example_32. This method must have matching call to SetTextEnd().
func (page *Page) SetTextStart() {
	appendString(&page.buf, "BT\n")
}

// SetTextLocation sets the text location.
// Please see Example_32.
// @param x the x coordinate of new text location.
// @param y the y coordinate of new text location.
func (page *Page) SetTextLocation(x, y float32) {
	appendFloat32(&page.buf, x)
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, page.height-y)
	appendString(&page.buf, " Td\n")
}

// SetTextBegin writes the text begin operator.
func (page *Page) SetTextBegin(x, y float32) {
	appendString(&page.buf, "BT\n")
	appendFloat32(&page.buf, x)
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, page.height-y)
	appendString(&page.buf, " Td\n")
}

// SetTextLeading sets the text leading.
// Please see Example_32.
func (page *Page) SetTextLeading(leading float32) {
	appendFloat32(&page.buf, leading)
	appendString(&page.buf, " TL\n")
}

// SetCharSpacing sets the spacing between the characters.
func (page *Page) SetCharSpacing(spacing float32) {
	appendFloat32(&page.buf, spacing)
	appendString(&page.buf, " Tc\n")
}

// SetWordSpacing sets the word spacing.
func (page *Page) SetWordSpacing(spacing float32) {
	appendFloat32(&page.buf, spacing)
	appendString(&page.buf, " Tw\n")
}

// SetTextScaling sets the text scaling.
func (page *Page) SetTextScaling(scaling float32) {
	appendFloat32(&page.buf, scaling)
	appendString(&page.buf, " Tz\n")
}

// SetTextRise sets the text rise.
func (page *Page) SetTextRise(rise float32) {
	appendFloat32(&page.buf, rise)
	appendString(&page.buf, " Ts\n")
}

// SetTextFont sets the text fonts.
func (page *Page) SetTextFont(font *Font) {
	page.font = font
	appendString(&page.buf, "/F")
	appendInteger(&page.buf, font.objNumber)
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, font.size)
	appendString(&page.buf, " Tf\n")
}

// Println prints a line of text and moves to the next line.
// Please see Example_32.
func (page *Page) Println(text string) {
	page.Print(text)
	page.println()
}

// Print prints a line of text.
// Please see Example_32.
func (page *Page) Print(text string) {
	if page.font == nil {
		return
	}
	appendString(&page.buf, "[<")
	if page.font.isCoreFont {
		page.drawASCIIString(page.font, text)
	} else {
		page.drawUnicodeString(page.font, text)
	}
	appendString(&page.buf, ">] TJ\n")
}

// println moves to the next line.
// Please see Example_32.
func (page *Page) println() {
	appendString(&page.buf, "T*\n")
}

// SetTextEnd sets the end of text block.
// Please see Example_32.
func (page *Page) SetTextEnd() {
	appendString(&page.buf, "ET\n")
}

// DrawRectRoundCorners draws rectangle with rounded corners.
// Code provided by:
// Dominique Andre Gunia <contact@dgunia.de>
// <<
func (page *Page) DrawRectRoundCorners(x, y, w, h, r1, r2 float32, operation string) {

	// The best 4-spline magic number
	var m4 float32 = 0.551784

	list := make([]*Point, 0)

	// Starting point
	list = append(list, NewPoint(x+w-r1, y))
	list = append(list, NewControlPoint(x+w-r1+m4*r1, y))
	list = append(list, NewControlPoint(x+w, y+r2-m4*r2))
	list = append(list, NewPoint(x+w, y+r2))

	list = append(list, NewPoint(x+w, y+h-r2))
	list = append(list, NewControlPoint(x+w, y+h-r2+m4*r2))
	list = append(list, NewControlPoint(x+w-m4*r1, y+h))
	list = append(list, NewPoint(x+w-r1, y+h))

	list = append(list, NewPoint(x+r1, y+h))
	list = append(list, NewControlPoint(x+r1-m4*r1, y+h))
	list = append(list, NewControlPoint(x, y+h-m4*r2))
	list = append(list, NewPoint(x, y+h-r2))

	list = append(list, NewPoint(x, y+r2))
	list = append(list, NewControlPoint(x, y+r2-m4*r2))
	list = append(list, NewControlPoint(x+m4*r1, y))
	list = append(list, NewPoint(x+r1, y))
	list = append(list, NewPoint(x+w-r1, y))

	page.DrawPath(list, operation)
}

// clipPath clips the path.
func (page *Page) clipPath() {
	appendString(&page.buf, "W\n")
	appendString(&page.buf, "n\n") // Close the path without painting it.
}

func (page *Page) clipRect(x, y, w, h float32) {
	page.MoveTo(x, y)
	page.LineTo(x+w, y)
	page.LineTo(x+w, y+h)
	page.LineTo(x, y+h)
	page.clipPath()
}

func (page *Page) save() {
	appendString(&page.buf, "q\n")
	page.savedStates = append(page.savedStates, NewState(
		page.pen,
		page.brush,
		page.penWidth,
		page.lineCapStyle,
		page.lineJoinStyle,
		page.linePattern))
	page.savedHeight = page.height
}

func (page *Page) restore() {
	appendString(&page.buf, "Q\n")
	if len(page.savedStates) > 0 {
		savedState := page.savedStates[len(page.savedStates)-1]
		page.pen = savedState.GetPen()
		page.brush = savedState.GetBrush()
		page.penWidth = savedState.GetPenWidth()
		page.lineCapStyle = savedState.GetLineCapStyle()
		page.lineJoinStyle = savedState.GetLineJoinStyle()
		page.linePattern = savedState.GetLinePattern()
		page.savedStates = page.savedStates[len(page.savedStates)-1:]
	}
	if page.savedHeight != math.MaxFloat32 {
		page.height = page.savedHeight
		page.savedHeight = math.MaxFloat32
	}
}

// <<

// SetCropBox sets the page CropBox.
// See page 77 of the PDF32000_2008.pdf specification.
// @param upperLeftX the top left X coordinate of the CropBox.
// @param upperLeftY the top left Y coordinate of the CropBox.
// @param lowerRightX the bottom right X coordinate of the CropBox.
// @param lowerRightY the bottom right Y coordinate of the CropBox.
func (page *Page) SetCropBox(upperLeftX, upperLeftY, lowerRightX, lowerRightY float32) {
	page.cropBox = []float32{upperLeftX, upperLeftY, lowerRightX, lowerRightY}
}

// SetBleedBox sets the page BleedBox.
// See page 77 of the PDF32000_2008.pdf specification.
// @param upperLeftX the top left X coordinate of the BleedBox.
// @param upperLeftY the top left Y coordinate of the BleedBox.
// @param lowerRightX the bottom right X coordinate of the BleedBox.
// @param lowerRightY the bottom right Y coordinate of the BleedBox.
func (page *Page) SetBleedBox(upperLeftX, upperLeftY, lowerRightX, lowerRightY float32) {
	page.bleedBox = []float32{upperLeftX, upperLeftY, lowerRightX, lowerRightY}
}

// SetTrimBox sets the page TrimBox.
// See page 77 of the PDF32000_2008.pdf specification.
// @param upperLeftX the top left X coordinate of the TrimBox.
// @param upperLeftY the top left Y coordinate of the TrimBox.
// @param lowerRightX the bottom right X coordinate of the TrimBox.
// @param lowerRightY the bottom right Y coordinate of the TrimBox.
func (page *Page) SetTrimBox(upperLeftX, upperLeftY, lowerRightX, lowerRightY float32) {
	page.trimBox = []float32{upperLeftX, upperLeftY, lowerRightX, lowerRightY}
}

// SetArtBox sets the page ArtBox.
// See page 77 of the PDF32000_2008.pdf specification.
// @param upperLeftX the top left X coordinate of the ArtBox.
// @param upperLeftY the top left Y coordinate of the ArtBox.
// @param lowerRightX the bottom right X coordinate of the ArtBox.
// @param lowerRightY the bottom right Y coordinate of the ArtBox.
func (page *Page) SetArtBox(upperLeftX, upperLeftY, lowerRightX, lowerRightY float32) {
	page.artBox = []float32{upperLeftX, upperLeftY, lowerRightX, lowerRightY}
}

func (page *Page) appendPointXY(x, y float32) {
	appendFloat32(&page.buf, x)
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, page.height-y)
	appendString(&page.buf, " ")
}

func (page *Page) appendPoint(point *Point) {
	appendFloat32(&page.buf, point.x)
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, page.height-point.y)
	appendString(&page.buf, " ")
}

func (page *Page) drawWord(font *Font, buf *strings.Builder, colors map[string]uint32) {
	if brushColor, ok := colors[buf.String()]; ok {
		page.SetBrushColor(brushColor)
	} else {
		page.SetBrushColor(color.Black)
	}
	appendString(&page.buf, "[<")
	if font.isCoreFont {
		page.drawASCIIString(font, buf.String())
	} else {
		page.drawUnicodeString(font, buf.String())
	}
	appendString(&page.buf, ">] TJ\n")
	buf.Reset()
}

func (page *Page) drawColoredString(font *Font, str string, colors map[string]uint32) {
	var buf1 strings.Builder
	var buf2 strings.Builder
	runes := []rune(str)
	for _, ch := range runes {
		if unicode.IsLetter(ch) || unicode.IsDigit(ch) {
			page.drawWord(font, &buf2, colors)
			buf1.WriteRune(ch)
		} else {
			page.drawWord(font, &buf1, colors)
			buf2.WriteRune(ch)
		}
	}
	page.drawWord(font, &buf1, colors)
	page.drawWord(font, &buf2, colors)
}

func (page *Page) setStructElementsPageObjNumber(pageObjNumber int) {
	for _, element := range page.structures {
		element.pageObjNumber = pageObjNumber
	}
}

// AddBMC adds BMC to the page.
func (page *Page) AddBMC(structure, language, actualText, altDescription string) {
	if page.pdf.compliance == compliance.PDF_UA {
		element := NewStructElem()
		element.structure = structure
		element.mcid = page.mcid
		element.language = language
		element.actualText = actualText
		element.altDescription = altDescription
		page.structures = append(page.structures, element)

		appendString(&page.buf, "/")
		appendString(&page.buf, structure)
		appendString(&page.buf, " <</MCID ")
		appendInteger(&page.buf, page.mcid)
		page.mcid++
		appendString(&page.buf, ">>\n")
		appendString(&page.buf, "BDC\n")
	}
}

// AddEMC adds EMC to the page.
func (page *Page) AddEMC() {
	if page.pdf.compliance == compliance.PDF_UA {
		appendString(&page.buf, "EMC\n")
	}
}

// AddAnnotation adds annotation to the page.
func (page *Page) AddAnnotation(annotation *Annotation) {
	annotation.y1 = page.height - annotation.y1
	annotation.y2 = page.height - annotation.y2
	page.annots = append(page.annots, annotation)
	if page.pdf.compliance == compliance.PDF_UA {
		element := NewStructElem()
		element.structure = "Link"
		element.language = annotation.language
		element.actualText = *annotation.actualText
		element.altDescription = *annotation.altDescription
		element.annotation = annotation
		page.structures = append(page.structures, element)
	}
}

func (page *Page) beginTransform(x, y, xScale, yScale float32) {
	appendString(&page.buf, "q\n")

	appendFloat32(&page.buf, xScale)
	appendString(&page.buf, " 0 0 ")
	appendFloat32(&page.buf, yScale)
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, x)
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, y)
	appendString(&page.buf, " cm\n")

	appendFloat32(&page.buf, xScale)
	appendString(&page.buf, " 0 0 ")
	appendFloat32(&page.buf, yScale)
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, x)
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, y)
	appendString(&page.buf, " Tm\n")
}

func (page *Page) endTransform() {
	appendString(&page.buf, "Q\n")
}

// DrawContents draws the contents on the page.
func (page *Page) DrawContents(
	content []byte,
	h float32, // The height of the graphics object in points.
	x float32,
	y float32,
	xScale float32,
	yScale float32) {
	page.beginTransform(x, (page.height-yScale*h)-y, xScale, yScale)
	appendByteArray(&page.buf, content)
	page.endTransform()
}

// DrawArrayOfCharacters draws array of equally spaced characters.
func (page *Page) DrawArrayOfCharacters(font *Font, text string, x, y, dx float32) {
	x1 := x
	for i := 0; i < len(text); i++ {
		page.DrawStringUsingColorMap(font, nil, text[i:i+1], x1, y, nil)
		x1 += dx
	}
}

// AddWatermark add watermark to the page.
func (page *Page) AddWatermark(font *Font, text string) {
	hypotenuse := float32(math.Sqrt(
		float64(page.height*page.height + page.width*page.width)))
	stringWidth := font.stringWidth(text)
	offset := (hypotenuse - stringWidth) / 2.0
	angle := math.Atan(float64(page.height / page.width))
	watermark := NewTextLine(font, "")
	watermark.SetColor(color.Lightgrey)
	watermark.SetText(text)
	watermark.SetLocation(
		offset*float32(math.Cos(angle)),
		page.height-offset*float32(math.Sin(angle)))
	watermark.SetTextDirection((int)(angle * (180.0 / math.Pi)))
	watermark.DrawOn(page)
}

// InvertYAxis inverts the Y axis.
func (page *Page) InvertYAxis() {
	appendString(&page.buf, "1 0 0 -1 0 ")
	appendFloat32(&page.buf, page.height)
	appendString(&page.buf, " cm\n")
}

// Transformation matrix.
// Use save before, restore afterwards!
// 9 value array like generated by androids Matrix.getValues()
func (page *Page) transform(values []float32) {
	scalex := values[mScaleX]
	scaley := values[mScaleY]
	transx := values[mTransX]
	transy := values[mTransY]

	appendFloat32(&page.buf, scalex)
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, values[mSkewX])
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, values[mSkewY])
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, scaley)
	appendString(&page.buf, " ")

	if math.Asin(float64(values[mSkewY])) != 0.0 {
		transx -= values[mSkewY] * page.height / scaley
	}

	appendFloat32(&page.buf, transx)
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, -transy)
	appendString(&page.buf, " cm\n")

	page.height = page.height / scaley
}

// AddHeader adds header to this page.
func (page *Page) AddHeader(textLine *TextLine) []float32 {
	return page.AddHeaderOffsetBy(textLine, 1.5*textLine.font.ascent)
}

// AddHeaderOffsetBy adds header to this page offset by the specified value.
func (page *Page) AddHeaderOffsetBy(textLine *TextLine, offset float32) []float32 {
	textLine.SetLocation((page.GetWidth()-textLine.GetWidth())/2, offset)
	xy := textLine.DrawOn(page)
	xy[1] += page.font.descent
	return xy
}

// AddFooter adds footer to this page.
func (page *Page) AddFooter(textLine *TextLine) []float32 {
	return page.AddFooterOffsetBy(textLine, textLine.font.ascent)
}

// AddFooterOffsetBy adds footer to this page offset by the specified value.
func (page *Page) AddFooterOffsetBy(textLine *TextLine, offset float32) []float32 {
	textLine.SetLocation((page.GetWidth()-textLine.GetWidth())/2, page.GetHeight()-offset)
	return textLine.DrawOn(page)
}
