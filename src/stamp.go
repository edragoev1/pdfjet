package pdfjet

import (
	"bytes"
	"math"
	"strconv"

	"github.com/edragoev1/pdfjet/v9/src/fastfloat"
	"github.com/edragoev1/pdfjet/v9/src/single"
	"github.com/edragoev1/pdfjet/v9/src/structtype"
	"github.com/edragoev1/pdfjet/v9/src/token"
)

// Stamp is content that is drawn once, written as a PDF form XObject, and placed on pages with DrawOn.
// Please see Example_35.
type Stamp struct {
	objNumber      int
	pdf            *PDF
	x              float32
	y              float32
	width          float32
	height         float32
	fillColor      []float32
	strokeColor    []float32
	strokeWidth    float32
	rotateDegrees  float32
	buf            *bytes.Buffer
	fonts          []*Font
	language       string
	actualText     string
	altDescription string
}

// NewStamp creates a stamp for the specified document.
func NewStamp(pdf *PDF) *Stamp {
	return &Stamp{
		pdf:            pdf,
		buf:            &bytes.Buffer{},
		strokeWidth:    1.0,
		actualText:     single.Space,
		altDescription: single.Space,
	}
}

// WithSize sets the size of this stamp.
func (s *Stamp) WithSize(width, height float32) *Stamp {
	s.width = width
	s.height = height
	return s
}

// WithFont adds a font used by the text on this stamp.
func (s *Stamp) WithFont(font *Font) *Stamp {
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
	s.buf.Write(fastfloat.ToByteArray(value))
}

func (s *Stamp) appendInt(value int) {
	s.buf.WriteString(strconv.Itoa(value))
}

func (s *Stamp) appendString(str string) {
	s.buf.WriteString(str)
}

// SetFillColorRGB sets the fill color for the content drawn after it,
// from the red, green and blue components, from 0.0 to 1.0.
func (s *Stamp) SetFillColorRGB(rgbColor []float32) *Stamp {
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
func (s *Stamp) SetFillColor(color int) *Stamp {
	r := float32((color>>16)&0xff) / 255.0
	g := float32((color>>8)&0xff) / 255.0
	b := float32(color&0xff) / 255.0
	return s.SetFillColorRGB([]float32{r, g, b})
}

// SetStrokeColorRGB sets the stroke color for the content drawn after it,
// from the red, green and blue components, from 0.0 to 1.0.
func (s *Stamp) SetStrokeColorRGB(rgbColor []float32) *Stamp {
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
func (s *Stamp) SetStrokeColor(color int) *Stamp {
	r := float32((color>>16)&0xff) / 255.0
	g := float32((color>>8)&0xff) / 255.0
	b := float32(color&0xff) / 255.0
	return s.SetStrokeColorRGB([]float32{r, g, b})
}

// SetStrokeWidth sets the stroke width for the content drawn after it.
func (s *Stamp) SetStrokeWidth(width float32) *Stamp {
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
	return s.DrawText(params.font, params.fontSize, params.x, params.y, params.text)
}

// DrawText draws text on this stamp. Add the font with WithFont too.
func (s *Stamp) DrawText(font *Font, fontSize, x, y float32, text string) *Stamp {
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

// Rotate sets the rotation angle of this stamp, in degrees.
func (s *Stamp) Rotate(degrees float64) *Stamp {
	s.rotateDegrees = float32(degrees)
	return s
}

// SetRotation sets the rotation angle of this stamp, in degrees.
func (s *Stamp) SetRotation(degrees float64) *Stamp {
	s.rotateDegrees = float32(degrees)
	return s
}

// SetRotationClockwise sets a clockwise rotation, in degrees.
func (s *Stamp) SetRotationClockwise(degrees float64) *Stamp {
	s.rotateDegrees = float32(-degrees)
	return s
}

// SetRotationCounterClockwise sets a counterclockwise rotation, in degrees.
func (s *Stamp) SetRotationCounterClockwise(degrees float64) *Stamp {
	s.rotateDegrees = float32(degrees)
	return s
}

// Complete writes this stamp to the document as a form XObject.
// Call it once, after drawing the content and before DrawOn.
func (s *Stamp) Complete() {
	s.pdf.newobj()
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
	s.pdf.endobj()
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
// It panics if the path has fewer than 2 points or ends with an unconsumed control point.
func (s *Stamp) DrawPath(path []*Point, pathOperator string) {
	if len(path) < 2 {
		panic("The Path object must contain at least 2 points")
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
	s.appendString(pathOperator)
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
	page.AddBMC(structtype.P, s.language, s.actualText, s.altDescription)
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
