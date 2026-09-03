package pdfjet

import (
	"bytes"
	"fmt"
	"math"
	"strconv"

	"github.com/edragoev1/pdfjet/src/token"
)

// Stamp struct
type Stamp struct {
	objNumber     int
	pdf           *PDF
	x             float32
	y             float32
	width         float32
	height        float32
	fillColor     []float32
	strokeColor   []float32
	strokeWidth   float32
	rotateDegrees float32
	buf           *bytes.Buffer
	fonts         []*Font
}

// NewStamp creates a new Stamp instance
func NewStamp(pdf *PDF) *Stamp {
	return &Stamp{
		pdf:         pdf,
		buf:         &bytes.Buffer{},
		strokeWidth: 1.0,
	}
}

// WithSize sets the stamp dimensions
func (s *Stamp) WithSize(width, height float32) *Stamp {
	s.width = width
	s.height = height
	return s
}

// WithFont adds a font to the stamp
func (s *Stamp) WithFont(font *Font) *Stamp {
	s.fonts = append(s.fonts, font)
	return s
}

// SetPosition sets the position (doesn't return self)
func (s *Stamp) SetPosition(x, y float32) {
	s.x = x
	s.y = y
}

// SetLocation sets the location and returns self for chaining
func (s *Stamp) SetLocation(x, y float32) *Stamp {
	s.x = x
	s.y = y
	return s
}

// AppendFloat appends a float value to the buffer
func (s *Stamp) AppendFloat(value float32) {
	s.buf.Write(toByteArray(value))
}

// AppendString appends a string to the buffer
func (s *Stamp) AppendString(str string) {
	s.buf.Write([]byte(str))
}

// SetFillColorRGB sets fill color from RGB array
func (s *Stamp) SetFillColorRGB(rgbColor []float32) *Stamp {
	s.AppendFloat(rgbColor[0])
	s.AppendString(" ")
	s.AppendFloat(rgbColor[1])
	s.AppendString(" ")
	s.AppendFloat(rgbColor[2])
	s.AppendString(" rg\n")
	s.fillColor = rgbColor
	return s
}

// SetFillColor sets fill color from integer (RGBA format)
func (s *Stamp) SetFillColor(color int) *Stamp {
	r := float32((color>>16)&0xff) / 255.0
	g := float32((color>>8)&0xff) / 255.0
	b := float32(color&0xff) / 255.0
	return s.SetFillColorRGB([]float32{r, g, b})
}

// SetStrokeColorRGB sets stroke color from RGB array
func (s *Stamp) SetStrokeColorRGB(rgbColor []float32) *Stamp {
	s.AppendFloat(rgbColor[0])
	s.AppendString(" ")
	s.AppendFloat(rgbColor[1])
	s.AppendString(" ")
	s.AppendFloat(rgbColor[2])
	s.AppendString(" RG\n")
	s.strokeColor = rgbColor
	return s
}

// SetStrokeColor sets stroke color from integer (RGBA format)
func (s *Stamp) SetStrokeColor(color int) *Stamp {
	r := float32((color>>16)&0xff) / 255.0
	g := float32((color>>8)&0xff) / 255.0
	b := float32(color&0xff) / 255.0
	return s.SetStrokeColorRGB([]float32{r, g, b})
}

// SetStrokeWidth sets the stroke width
func (s *Stamp) SetStrokeWidth(width float32) *Stamp {
	s.AppendFloat(width)
	s.AppendString(" w\n")
	s.strokeWidth = width
	return s
}

// MoveTo adds a move-to path command
func (s *Stamp) MoveTo(x, y float32) *Stamp {
	s.AppendFloat(x)
	s.AppendString(" ")
	s.AppendFloat(s.height - y)
	s.AppendString(" m\n")
	return s
}

// LineTo adds a line-to path command
func (s *Stamp) LineTo(x, y float32) *Stamp {
	s.AppendFloat(x)
	s.AppendString(" ")
	s.AppendFloat(s.height - y)
	s.AppendString(" l\n")
	return s
}

// CurveTo adds a cubic Bezier curve command
func (s *Stamp) CurveTo(x1, y1, x2, y2, x3, y3 float32) *Stamp {
	s.AppendFloat(x1)
	s.AppendString(" ")
	s.AppendFloat(s.height - y1)
	s.AppendString(" ")
	s.AppendFloat(x2)
	s.AppendString(" ")
	s.AppendFloat(s.height - y2)
	s.AppendString(" ")
	s.AppendFloat(x3)
	s.AppendString(" ")
	s.AppendFloat(s.height - y3)
	s.AppendString(" c\n")
	return s
}

// StrokePath adds stroke operator
func (s *Stamp) StrokePath() *Stamp {
	s.AppendString("S\n")
	return s
}

// ClosePath adds close+stroke operator
func (s *Stamp) ClosePath() *Stamp {
	s.AppendString("s\n")
	return s
}

// FillPath adds fill operator
func (s *Stamp) FillPath() *Stamp {
	s.AppendString("f\n")
	return s
}

// CloseFillAndStrokePath adds close+fill+stroke operator
func (s *Stamp) CloseFillAndStrokePath() *Stamp {
	s.AppendString("b\n")
	return s
}

// Rectangle is a TODO stub
func (s *Stamp) Rectangle() *Stamp {
	return s
}

// Draw is a TODO stub
func (s *Stamp) Draw() *Stamp {
	return s
}

// DrawRect draws a rectangle outline
func (s *Stamp) DrawRect(x, y, w, h float32) *Stamp {
	s.MoveTo(x, y)
	s.LineTo(x+w, y)
	s.LineTo(x+w, y+h)
	s.LineTo(x, y+h)
	s.ClosePath()
	return s
}

// FillRect draws a filled rectangle
func (s *Stamp) FillRect(x, y, w, h float32) *Stamp {
	s.MoveTo(x, y)
	s.LineTo(x+w, y)
	s.LineTo(x+w, y+h)
	s.LineTo(x, y+h)
	s.FillPath()
	return s
}

// DrawTextUsingParams draws text using the TextParameters data.
func (s *Stamp) DrawTextUsingParams(params *TextParameters) *Stamp {
	return s.drawText(params.font, params.fontSize, params.x, params.y, params.text)
}

// DrawText draws text on the stamp
func (s *Stamp) drawText(font *Font, fontSize, x, y float32, text string) *Stamp {
	s.AppendString("BT\n")
	s.AppendString("/F")
	s.AppendFloat(float32(font.objNumber))
	s.AppendString(" ")
	s.AppendFloat(fontSize)
	s.AppendString(" Tf\n")
	s.AppendFloat(x)
	s.AppendString(" ")
	s.AppendFloat(s.height - y)
	s.AppendString(" Td\n")
	s.AppendString("<")
	s.DrawText(font, text)
	s.AppendString("> Tj\n")
	s.AppendString("ET\n")
	return s
}

// Rotate sets the rotation angle
func (s *Stamp) Rotate(degrees float64) *Stamp {
	s.rotateDegrees = float32(degrees)
	return s
}

// SetRotation sets the rotation angle
func (s *Stamp) SetRotation(degrees float64) *Stamp {
	s.rotateDegrees = float32(degrees)
	return s
}

// SetRotationClockwise sets clockwise rotation
func (s *Stamp) SetRotationClockwise(degrees float64) *Stamp {
	s.rotateDegrees = float32(-degrees)
	return s
}

// SetRotationCounterClockwise sets counter-clockwise rotation
func (s *Stamp) SetRotationCounterClockwise(degrees float64) *Stamp {
	s.rotateDegrees = float32(degrees)
	return s
}

func toByteArray(value float32) []byte {
	return []byte(strconv.FormatFloat(float64(value), 'g', -1, 32))
}

func toString(value float32) string {
	return strconv.FormatFloat(float64(value), 'g', -1, 32)
}

// Complete finalizes the stamp object
func (s *Stamp) Complete() {
	s.pdf.newobj()
	s.pdf.appendByteArray(token.BeginDictionary)
	s.pdf.appendString("/Type /XObject\n")
	s.pdf.appendString("/Subtype /Form\n")

	s.pdf.appendString("/BBox [0 0 ")
	s.pdf.appendString(toString(s.width))
	s.pdf.appendString(" ")
	s.pdf.appendString(toString(s.height))
	s.pdf.appendString("]\n")

	s.pdf.appendString("/Resources <<\n")
	if len(s.fonts) > 0 {
		s.pdf.appendString("/Font <<\n")
		for _, font := range s.fonts {
			s.pdf.appendString("/F")
			s.pdf.appendString(fmt.Sprintf("%d", font.objNumber))
			s.pdf.appendString(" ")
			s.pdf.appendString(fmt.Sprintf("%d", font.objNumber))
			s.pdf.appendString(" 0 R\n")
		}
		s.pdf.appendString(">>\n")
	}
	s.pdf.appendString(">>\n")
	s.pdf.appendString("/Length ")
	s.pdf.appendString(fmt.Sprintf("%d", s.buf.Len()))
	s.pdf.appendByte(token.Newline)
	s.pdf.appendByteArray(token.EndDictionary)
	s.pdf.appendByteArray(token.Stream)
	s.pdf.appendByteArray(s.buf.Bytes())
	s.pdf.appendByteArray(token.EndStream)
	s.pdf.endobj()
	s.pdf.stamps = append(s.pdf.stamps, s)
	s.objNumber = s.pdf.getObjNumber()
}

// DrawText draws encoded text characters
func (s *Stamp) DrawText(font *Font, str string) {
	for _, codePoint := range str {
		if codePoint == 0xFEFF { // Skip the BOM
			continue
		}

		var gid int
		if codePoint < font.firstChar || codePoint > font.lastChar {
			gid = font.unicodeToGID[0x0020]
		} else {
			gid = font.unicodeToGID[codePoint]
		}
		s.appendCodePointAsHex(gid)
	}
}

// AppendPoint appends a Point to the buffer
func (s *Stamp) AppendPoint(point *Point) {
	s.AppendFloat(point.x)
	s.AppendString(" ")
	s.AppendFloat(s.height - point.y)
	s.AppendString(" ")
}

// DrawPath draws a path of Points
// DrawPath draws a path of Points
func (s *Stamp) DrawPath(path []*Point, pathOperator string) {
	if len(path) < 2 {
		panic("Path must contain at least 2 points")
	}

	point := path[0]
	s.MoveTo(point.x, point.y)

	var controlPoint byte = 0
	for i := 1; i < len(path); i++ {
		point = path[i]
		if point.controlPoint != 0 {
			controlPoint = point.controlPoint
			s.AppendPoint(point)
		} else {
			if controlPoint != 0 {
				s.AppendPoint(point)
				s.buf.WriteByte(controlPoint) // More efficient than WriteString()
				s.buf.WriteByte('\n')
				controlPoint = 0
			} else {
				s.LineTo(point.x, point.y)
			}
		}
	}

	if controlPoint != 0 {
		panic("Path ends with unconsumed control point(s). Each 'c' requires 2 CPs + 1 endpoint, 'v'/'y' require 1 CP + 1 endpoint.")
	}

	s.buf.WriteString(pathOperator)
	s.buf.WriteByte('\n')
}

// appendCodePointAsHex appends a code point as hexadecimal
func (s *Stamp) appendCodePointAsHex(codePoint int) {
	s.buf.WriteByte(hexDigits[(codePoint>>12)&0xF])
	s.buf.WriteByte(hexDigits[(codePoint>>8)&0xF])
	s.buf.WriteByte(hexDigits[(codePoint>>4)&0xF])
	s.buf.WriteByte(hexDigits[codePoint&0xF])
}

// DrawOn draws the stamp on a page
func (s *Stamp) DrawOn(page *Page) []float32 {
	page.SaveGraphicsState()

	drawX := s.x
	drawY := (page.height - s.height) - s.y

	// 5. POSITION
	page.appendString("1 0 0 1 ")
	page.appendFloat32(drawX)
	page.appendString(" ")
	page.appendFloat32(drawY)
	page.appendString(" cm\n")

	// 4. MOVE BACK
	page.appendString("1 0 0 1 ")
	page.appendFloat32(s.width / 2)
	page.appendString(" ")
	page.appendFloat32(s.height / 2)
	page.appendString(" cm\n")

	// 3. ROTATE
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

	// 2. MOVE
	page.appendString("1 0 0 1 ")
	page.appendFloat32(-s.width / 2)
	page.appendString(" ")
	page.appendFloat32(-s.height / 2)
	page.appendString(" cm\n")

	// 1. DRAW
	page.appendString("/Fm")
	page.appendFloat32(float32(s.objNumber))
	page.appendString(" Do\n")

	page.RestoreGraphicsState()

	return []float32{s.x + s.width, s.y + s.height}
}
