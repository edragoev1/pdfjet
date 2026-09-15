package pdfjet

import (
	"bytes"
	"math"
	"strconv"

	"github.com/edragoev1/pdfjet/v9/src/internal/fastfloat"
	"github.com/edragoev1/pdfjet/v9/src/internal/token"
	"github.com/edragoev1/pdfjet/v9/src/pathoperator"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// Stamp is content that is drawn once with the path and text methods of this
// type, written to the document as a PDF form XObject by Complete, and placed
// on pages with DrawOn, at a location, a rotation and a scale, as a Container is.
//
// Use a Stamp for content that repeats on many pages, like a header, a footer,
// a logo or a watermark: the content is stored once in the file, and each
// placement adds a few bytes to the page. Use a Container to group drawable
// elements, like Rect, TextLine and Image, that are moved, rotated and scaled
// together on one page. Please see Example_35.
type Stamp struct {
	objNumber      int
	pdf            *PDF
	x              float32
	y              float32
	width          float32
	height         float32
	fillColor      [3]float32
	strokeColor    [3]float32
	strokeWidth    float32
	rotateDegrees  float32
	scaleX         float32
	scaleY         float32
	buf            *bytes.Buffer
	fonts          []*Font
	language       string
	actualText     string
	altDescription string
	completed      bool
}

// NewStamp creates a stamp for the specified document.
func NewStamp(pdf *PDF) *Stamp {
	return &Stamp{
		pdf:         pdf,
		buf:         &bytes.Buffer{},
		strokeWidth: 1.0,
		scaleX:      1.0,
		scaleY:      1.0,
	}
}

// SetSize sets the size of this stamp.
func (s *Stamp) SetSize(width, height float32) *Stamp {
	s.width = width
	s.height = height
	return s
}

// AddFont adds a font used by the text on this stamp.
func (s *Stamp) AddFont(font *Font) *Stamp {
	if font.pdf != nil && font.pdf != s.pdf {
		s.pdf.fail("The font belongs to another PDF.")
		return s
	}
	for _, f := range s.fonts {
		if f == font {
			return s
		}
	}
	s.fonts = append(s.fonts, font)
	return s
}

// SetLocation sets the location of the top left corner of this stamp on the page.
// It returns the stamp as a Drawable, so in a chain of setter calls
// SetLocation goes last, right before DrawOn.
func (s *Stamp) SetLocation(x, y float32) Drawable {
	s.x = x
	s.y = y
	return s
}

// SetLanguage sets the language of this stamp, used for accessibility, for example "en-US".
func (s *Stamp) SetLanguage(language string) *Stamp {
	s.language = language
	return s
}

// SetAltDescription sets the alternate description of this stamp.
func (s *Stamp) SetAltDescription(altDescription string) *Stamp {
	s.altDescription = altDescription
	return s
}

// SetActualText sets the actual text for this stamp.
func (s *Stamp) SetActualText(actualText string) *Stamp {
	s.actualText = actualText
	return s
}

func (s *Stamp) appendFloat(value float32) {
	if !s.open() {
		return
	}
	if !fastfloat.IsWritable(value) {
		s.pdf.fail(notWritable)
	}
	s.buf.Write(fastfloat.ToByteArray(value))
}

func (s *Stamp) appendInt(value int) {
	if s.open() {
		s.buf.WriteString(strconv.Itoa(value))
	}
}

func (s *Stamp) appendString(str string) {
	if s.open() {
		s.buf.WriteString(str)
	}
}

// open returns true until Complete: the content drawn after it would be lost.
func (s *Stamp) open() bool {
	if s.completed {
		s.pdf.fail("The stamp was already completed.")
		return false
	}
	return true
}

// SetFillColorRGB sets the fill color for the content drawn after it,
// from the red, green and blue components, from 0.0 to 1.0.
func (s *Stamp) SetFillColorRGB(rgbColor [3]float32) *Stamp {
	s.appendFloat(rgbColor[0])
	s.appendString(" ")
	s.appendFloat(rgbColor[1])
	s.appendString(" ")
	s.appendFloat(rgbColor[2])
	s.appendString(" rg\n")
	s.fillColor = rgbColor
	return s
}

// SetFillColor sets the fill color for the content drawn after it,
// as a 0xRRGGBB value, for example color.Blue.
func (s *Stamp) SetFillColor(color int32) *Stamp {
	return s.SetFillColorRGB(colorToRGB(color))
}

// SetStrokeColorRGB sets the stroke color for the content drawn after it,
// from the red, green and blue components, from 0.0 to 1.0.
func (s *Stamp) SetStrokeColorRGB(rgbColor [3]float32) *Stamp {
	s.appendFloat(rgbColor[0])
	s.appendString(" ")
	s.appendFloat(rgbColor[1])
	s.appendString(" ")
	s.appendFloat(rgbColor[2])
	s.appendString(" RG\n")
	s.strokeColor = rgbColor
	return s
}

// SetStrokeColor sets the stroke color for the content drawn after it,
// as a 0xRRGGBB value, for example color.Blue.
func (s *Stamp) SetStrokeColor(color int32) *Stamp {
	return s.SetStrokeColorRGB(colorToRGB(color))
}

// SetStrokeWidth sets the stroke width for the content drawn after it.
func (s *Stamp) SetStrokeWidth(width float32) *Stamp {
	if width < 0 {
		s.pdf.fail("The stroke width cannot be negative.")
		return s
	}
	s.appendFloat(width)
	s.appendString(" w\n")
	s.strokeWidth = width
	return s
}

// MoveTo begins a new path at the specified point.
func (s *Stamp) MoveTo(x, y float32) *Stamp {
	s.appendFloat(x)
	s.appendString(" ")
	s.appendFloat(s.height - y)
	s.appendString(" m\n")
	return s
}

// LineTo adds a straight line from the current point to the specified point.
func (s *Stamp) LineTo(x, y float32) *Stamp {
	s.appendFloat(x)
	s.appendString(" ")
	s.appendFloat(s.height - y)
	s.appendString(" l\n")
	return s
}

// CurveTo adds a cubic Bézier curve from the current point to x3, y3,
// using x1, y1 and x2, y2 as control points.
func (s *Stamp) CurveTo(x1, y1, x2, y2, x3, y3 float32) *Stamp {
	s.appendFloat(x1)
	s.appendString(" ")
	s.appendFloat(s.height - y1)
	s.appendString(" ")
	s.appendFloat(x2)
	s.appendString(" ")
	s.appendFloat(s.height - y2)
	s.appendString(" ")
	s.appendFloat(x3)
	s.appendString(" ")
	s.appendFloat(s.height - y3)
	s.appendString(" c\n")
	return s
}

// StrokePath strokes the current path.
func (s *Stamp) StrokePath() *Stamp {
	s.appendString("S\n")
	return s
}

// ClosePath closes and strokes the current path.
func (s *Stamp) ClosePath() *Stamp {
	s.appendString("s\n")
	return s
}

// FillPath fills the current path.
func (s *Stamp) FillPath() *Stamp {
	s.appendString("f\n")
	return s
}

// CloseFillAndStrokePath closes, fills and strokes the current path.
func (s *Stamp) CloseFillAndStrokePath() *Stamp {
	s.appendString("b\n")
	return s
}

// DrawRect draws the outline of a rectangle with its top left corner at x, y.
func (s *Stamp) DrawRect(x, y, w, h float32) *Stamp {
	s.MoveTo(x, y)
	s.LineTo(x+w, y)
	s.LineTo(x+w, y+h)
	s.LineTo(x, y+h)
	s.ClosePath()
	return s
}

// FillRect draws a filled rectangle with its top left corner at x, y.
func (s *Stamp) FillRect(x, y, w, h float32) *Stamp {
	s.MoveTo(x, y)
	s.LineTo(x+w, y)
	s.LineTo(x+w, y+h)
	s.LineTo(x, y+h)
	s.FillPath()
	return s
}

// DrawTextUsingParams draws text using the font, font size, location and text in the parameters.
func (s *Stamp) DrawTextUsingParams(params *TextParameters) *Stamp {
	if params == nil {
		s.pdf.fail("Stamp text needs a font and a text.")
		return s
	}
	return s.DrawText(params.font, params.fontSize, params.x, params.y, params.text)
}

// DrawText draws text on this stamp with an embedded font, which the stamp
// adds to its fonts.
func (s *Stamp) DrawText(font *Font, fontSize, x, y float32, text string) *Stamp {
	if font == nil {
		s.pdf.fail("Stamp text needs a font and a text.")
		return s
	}
	if font.isCoreFont || font.isCJK {
		s.pdf.fail("A stamp draws text with an embedded font, not a core or CJK font.")
		return s
	}
	s.AddFont(font)
	s.appendString("BT\n")
	s.appendString("/F")
	s.appendInt(font.objNumber)
	s.appendString(" ")
	s.appendFloat(fontSize)
	s.appendString(" Tf\n")
	s.appendFloat(x)
	s.appendString(" ")
	s.appendFloat(s.height - y)
	s.appendString(" Td\n")
	s.appendString("<")
	s.drawEncodedText(font, text)
	s.appendString("> Tj\n")
	s.appendString("ET\n")
	return s
}

// SetRotation rotates this stamp around its center: clockwise for a positive
// angle, as every rotation in PDFjet turns, and counterclockwise for a negative
// angle.
func (s *Stamp) SetRotation(degrees float64) *Stamp {
	// The rotation of the page turns counterclockwise.
	s.rotateDegrees = float32(-degrees)
	return s
}

// ScaleBy scales this stamp around its center when it is placed on a page;
// 1 is its size.
func (s *Stamp) ScaleBy(factor float32) *Stamp {
	return s.ScaleByWidthAndHeight(factor, factor)
}

// ScaleByWidthAndHeight scales this stamp around its center when it is placed
// on a page.
func (s *Stamp) ScaleByWidthAndHeight(sx, sy float32) *Stamp {
	s.scaleX = sx
	s.scaleY = sy
	return s
}

// Complete writes this stamp to the document as a form XObject.
// Call it once, after drawing the content and before DrawOn.
func (s *Stamp) Complete() {
	if s.completed {
		s.pdf.fail("Complete was already called on the stamp.")
		return
	}
	s.completed = true
	s.pdf.newObj()
	s.pdf.appendByteArray(token.BeginDictionary)
	s.pdf.appendString("/Type /XObject\n")
	s.pdf.appendString("/Subtype /Form\n")

	s.pdf.appendString("/BBox [0 0 ")
	s.pdf.appendFloat32(s.width)
	s.pdf.appendString(" ")
	s.pdf.appendFloat32(s.height)
	s.pdf.appendString("]\n")

	s.pdf.appendString("/Resources <<\n")
	if len(s.fonts) > 0 {
		s.pdf.appendString("/Font <<\n")
		for _, font := range s.fonts {
			s.pdf.appendString("/F")
			s.pdf.appendInteger(font.objNumber)
			s.pdf.appendString(" ")
			s.pdf.appendInteger(font.objNumber)
			s.pdf.appendString(" 0 R\n")
		}
		s.pdf.appendString(">>\n")
	}
	s.pdf.appendString(">>\n")
	s.pdf.appendString("/Length ")
	s.pdf.appendInteger(s.buf.Len())
	s.pdf.appendByte(token.Newline)
	s.pdf.appendByteArray(token.EndDictionary) // End of XObject dictionary
	s.pdf.appendByteArray(token.Stream)
	s.pdf.appendByteArray(s.buf.Bytes())
	s.pdf.appendByteArray(token.EndStream)
	s.pdf.endObj()
	s.pdf.stamps = append(s.pdf.stamps, s)
	s.objNumber = s.pdf.getObjNumber()
}

// drawEncodedText appends the glyph IDs of the text as hexadecimal.
func (s *Stamp) drawEncodedText(font *Font, str string) {
	for _, codePoint := range str {
		if codePoint == 0xFEFF { // Skip the BOM
			continue
		}
		var gid int
		if codePoint < font.firstChar || codePoint > font.lastChar {
			gid = font.unicodeToGID[0x0020] // Use space fallback
		} else {
			gid = font.unicodeToGID[codePoint]
		}
		s.appendCodePointAsHex(gid)
	}
}

func (s *Stamp) appendPoint(point *Point) {
	s.appendFloat(point.x)
	s.appendString(" ")
	s.appendFloat(s.height - point.y)
	s.appendString(" ")
}

// DrawPath draws a path through the points. Control points define Bézier curves.
// Fewer than two points paint nothing. It panics if the path ends with an
// unconsumed control point.
func (s *Stamp) DrawPath(path []*Point, pathOperator pathoperator.PathOperator) {
	if len(path) < 2 {
		return // A path needs two points to paint anything.
	}
	point := path[0]
	s.MoveTo(point.x, point.y)
	var controlPoint byte = 0
	for i := 1; i < len(path); i++ {
		point = path[i]
		if point.controlPoint != 0 {
			controlPoint = point.controlPoint
			s.appendPoint(point)
		} else {
			if controlPoint != 0 {
				s.appendPoint(point)
				s.buf.WriteByte(controlPoint)
				s.buf.WriteByte('\n')
				controlPoint = 0
			} else {
				s.LineTo(point.x, point.y)
			}
		}
	}
	// Catch unflushed control point
	if controlPoint != 0 {
		panic("Path ends with unconsumed control point(s). " +
			"Each 'c' requires 2 CPs + 1 endpoint, 'v'/'y' require 1 CP + 1 endpoint.")
	}
	s.appendString(string(pathOperator))
	s.buf.WriteByte('\n')
}

func (s *Stamp) appendCodePointAsHex(codePoint int) {
	s.buf.WriteByte(hexDigits[(codePoint>>12)&0xF])
	s.buf.WriteByte(hexDigits[(codePoint>>8)&0xF])
	s.buf.WriteByte(hexDigits[(codePoint>>4)&0xF])
	s.buf.WriteByte(hexDigits[codePoint&0xF])
}

// DrawOn draws this stamp on the specified page and returns the x and y
// coordinates of its bottom right corner.
func (s *Stamp) DrawOn(page *Page) [2]float32 {
	if page.pdf != s.pdf {
		page.pdf.fail("The stamp belongs to another PDF.")
		return [2]float32{s.x + s.width, s.y + s.height}
	}
	if !s.completed {
		page.pdf.fail("Call Complete on the stamp before drawing it.")
		return [2]float32{s.x + s.width, s.y + s.height}
	}
	if s.width == 0 || s.height == 0 || s.scaleX == 0 || s.scaleY == 0 {
		return [2]float32{s.x + s.width, s.y + s.height} // Nothing to paint.
	}
	page.AddBDC(structelem.P, s.language, s.actualText, s.altDescription)
	page.SaveGraphicsState()

	drawX := s.x
	drawY := (page.height - s.height) - s.y

	// 5. POSITION: move to desired location on page
	page.appendString("1 0 0 1 ")
	page.appendFloat32(drawX)
	page.appendString(" ")
	page.appendFloat32(drawY)
	page.appendString(" cm\n")

	// 4. MOVE BACK: after rotation
	page.appendString("1 0 0 1 ")
	page.appendFloat32(s.width / 2)
	page.appendString(" ")
	page.appendFloat32(s.height / 2)
	page.appendString(" cm\n")

	// 3. ROTATE: rotate around origin
	radians := float64(s.rotateDegrees) * (math.Pi / 180)
	cos := float32(math.Cos(radians))
	sin := float32(math.Sin(radians))
	page.appendFloat32(cos)
	page.appendString(" ")
	page.appendFloat32(sin)
	page.appendString(" ")
	page.appendFloat32(-sin)
	page.appendString(" ")
	page.appendFloat32(cos)
	page.appendString(" 0 0 cm\n")

	// SCALE: around the center, like a Container
	if s.scaleX != 1.0 || s.scaleY != 1.0 {
		page.appendFloat32(s.scaleX)
		page.appendString(" 0 0 ")
		page.appendFloat32(s.scaleY)
		page.appendString(" 0 0 cm\n")
	}

	// 2. MOVE: move the center of the object to origin
	page.appendString("1 0 0 1 ")
	page.appendFloat32(-s.width / 2)
	page.appendString(" ")
	page.appendFloat32(-s.height / 2)
	page.appendString(" cm\n")

	// 1. DRAW: draw the object
	page.appendString("/Fm")
	page.appendInteger(s.objNumber)
	page.appendString(" Do\n")

	page.RestoreGraphicsState()
	page.AddEMC()

	return [2]float32{s.x + s.width, s.y + s.height}
}
