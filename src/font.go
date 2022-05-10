package pdfjet

/**
 * font.go
 *
Copyright 2022 Innovatics Inc.

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
	"log"
	"strings"

	"github.com/edragoev1/pdfjet/src/corefont"
)

// Font is used to create font objects.
// The font objects must added to the PDF before they can be used to draw text.
type Font struct {
	name      string
	info      string
	objNumber int
	fontID    string

	fileObjNumber           int // The object number of the embedded font file
	fontDescriptorObjNumber int
	cidFontDictObjNumber    int
	toUnicodeCMapObjNumber  int

	unitsPerEm             int
	bBoxLLx                int16 // Font bounding box
	bBoxLLy                int16
	bBoxURx                int16
	bBoxURy                int16
	fontAscent             int16
	fontDescent            int16
	firstChar              rune
	lastChar               rune
	capHeight              int16
	fontUnderlinePosition  int16
	fontUnderlineThickness int16
	advanceWidth           []uint16
	glyphWidth             []uint16
	unicodeToGID           []int
	cff                    bool
	compressedSize         int
	uncompressedSize       int
	metrics                [][]int // Only used for core fonts.

	// Don't change the following default values!
	size       float32
	isCoreFont bool
	isCJK      bool
	skew15     bool
	kernPairs  bool

	ascent             float32
	descent            float32
	bodyHeight         float32
	underlinePosition  float32
	underlineThickness float32
}

const (
	// AdobeMingStdLight is Chinese (Traditional) font
	AdobeMingStdLight = "AdobeMingStd-Light"

	// STHeitiSCLight is Chinese (Simplified) font
	STHeitiSCLight = "STHeitiSC-Light"

	// KozMinProVIRegular is Japanese font
	KozMinProVIRegular = "KozMinProVI-Regular"

	// AdobeMyungjoStdMedium is Korean font
	AdobeMyungjoStdMedium = "AdobeMyungjoStd-Medium"

	defaultFontSize float32 = 12.0
)

// NewCoreFont is the constructor for the 14 standard fonts.
// Creates a font object and adds it to the PDF.
//
// <pre>
// Examples:
//     Font font1 = new Font(pdf, CoreFont.HELVETICA)
//     Font font2 = new Font(pdf, CoreFont.TIMES_ITALIC)
//     Font font3 = new Font(pdf, CoreFont.ZAPF_DINGBATS)
//     ...
// </pre>
//
// @param pdf the PDF to add this font to.
// @param coreFont the core font. Must be one the names defined in the CoreFont class.
//
// font := CoreFont(pdf, corefont.Helvetica())
//
func NewCoreFont(pdf *PDF, coreFont *corefont.CoreFont) *Font {
	font := new(Font)
	font.isCoreFont = true
	font.name = coreFont.Name
	font.size = defaultFontSize
	font.unitsPerEm = 1000
	font.firstChar = 32
	font.lastChar = 255
	font.bBoxLLx = coreFont.BBoxLLx
	font.bBoxLLy = coreFont.BBoxLLy
	font.bBoxURx = coreFont.BBoxURx
	font.bBoxURy = coreFont.BBoxURy
	font.metrics = coreFont.Metrics
	font.fontUnderlinePosition = coreFont.UnderlinePosition
	font.fontUnderlineThickness = coreFont.UnderlineThickness
	font.fontAscent = coreFont.BBoxURy
	font.fontDescent = coreFont.BBoxLLy
	font.SetSize(font.size)

	pdf.newobj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /Font\n")
	pdf.appendString("/Subtype /Type1\n")
	pdf.appendString("/BaseFont /")
	pdf.appendString(font.name)
	pdf.appendString("\n")
	if font.name != "Symbol" && font.name != "ZapfDingbats" {
		pdf.appendString("/Encoding /WinAnsiEncoding\n")
	}
	pdf.appendString(">>\n")
	pdf.endobj()

	font.objNumber = pdf.getObjNumber()
	pdf.fonts = append(pdf.fonts, font)

	return font
}

// NewCoreFontForPDFobj is the constructor used by PDFobj
// Core font should be interface!
func NewCoreFontForPDFobj(coreFont *corefont.CoreFont) *Font {
	font := new(Font)
	font.isCoreFont = true
	font.name = coreFont.Name
	font.size = defaultFontSize
	font.unitsPerEm = 1000
	font.firstChar = 32
	font.lastChar = 255
	font.bBoxLLx = coreFont.BBoxLLx
	font.bBoxLLy = coreFont.BBoxLLy
	font.bBoxURx = coreFont.BBoxURx
	font.bBoxURy = coreFont.BBoxURy
	font.metrics = coreFont.Metrics
	font.fontUnderlinePosition = coreFont.UnderlinePosition
	font.fontUnderlineThickness = coreFont.UnderlineThickness
	font.fontAscent = coreFont.BBoxURy
	font.fontDescent = coreFont.BBoxLLy
	font.SetSize(font.size)
	return font
}

// NewCJKFont is the constructor for CJK - Chinese, Japanese and Korean fonts.
// Please see Example_04.
//
// @param pdf the PDF to add this font to.
// @param fontName the font name. Please see Example_04.
func NewCJKFont(pdf *PDF, fontName string) *Font {
	font := new(Font)
	font.isCJK = true
	font.name = fontName
	font.size = defaultFontSize
	font.unitsPerEm = 1000
	font.firstChar = 0x0020
	font.lastChar = 0xFFEE
	font.ascent = font.size
	font.descent = font.size / 4
	font.bodyHeight = font.ascent + font.descent

	// Font Descriptor
	pdf.newobj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /FontDescriptor\n")
	pdf.appendString("/FontName /")
	pdf.appendString(fontName)
	pdf.appendString("\n")
	pdf.appendString("/Flags 4\n")
	pdf.appendString("/FontBBox [0 0 0 0]\n")
	pdf.appendString(">>\n")
	pdf.endobj()

	// CIDFont Dictionary
	pdf.newobj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /Font\n")
	pdf.appendString("/Subtype /CIDFontType0\n")
	pdf.appendString("/BaseFont /")
	pdf.appendString(fontName)
	pdf.appendString("\n")
	pdf.appendString("/FontDescriptor ")
	pdf.appendInteger(pdf.getObjNumber() - 1)
	pdf.appendString(" 0 R\n")
	pdf.appendString("/CIDSystemInfo <<\n")
	pdf.appendString("/Registry (Adobe)\n")
	if strings.HasPrefix(fontName, "AdobeMingStd") {
		pdf.appendString("/Ordering (CNS1)\n")
		pdf.appendString("/Supplement 4\n")
	} else if strings.HasPrefix(fontName, "AdobeSongStd") || strings.HasPrefix(fontName, "STHeitiSC") {
		pdf.appendString("/Ordering (GB1)\n")
		pdf.appendString("/Supplement 4\n")
	} else if strings.HasPrefix(fontName, "KozMinPro") {
		pdf.appendString("/Ordering (Japan1)\n")
		pdf.appendString("/Supplement 4\n")
	} else if strings.HasPrefix(fontName, "AdobeMyungjoStd") {
		pdf.appendString("/Ordering (Korea1)\n")
		pdf.appendString("/Supplement 1\n")
	} else {
		log.Fatal("Unsupported font: " + fontName)
	}
	pdf.appendString(">>\n")
	pdf.appendString(">>\n")
	pdf.endobj()

	// Type0 Font Dictionary
	pdf.newobj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /Font\n")
	pdf.appendString("/Subtype /Type0\n")
	pdf.appendString("/BaseFont /")
	if strings.HasPrefix(fontName, "AdobeMingStd") {
		pdf.appendString(fontName + "-UniCNS-UTF16-H\n")
		pdf.appendString("/Encoding /UniCNS-UTF16-H\n")
	} else if strings.HasPrefix(fontName, "AdobeSongStd") || strings.HasPrefix(fontName, "STHeitiSC") {
		pdf.appendString(fontName + "-UniGB-UTF16-H\n")
		pdf.appendString("/Encoding /UniGB-UTF16-H\n")
	} else if strings.HasPrefix(fontName, "KozMinPro") {
		pdf.appendString(fontName + "-UniJIS-UCS2-H\n")
		pdf.appendString("/Encoding /UniJIS-UCS2-H\n")
	} else if strings.HasPrefix(fontName, "AdobeMyungjoStd") {
		pdf.appendString(fontName + "-UniKS-UCS2-H\n")
		pdf.appendString("/Encoding /UniKS-UCS2-H\n")
	} else {
		log.Fatal("Unsupported font: " + fontName)
	}
	pdf.appendString("/DescendantFonts [")
	pdf.appendInteger(pdf.getObjNumber() - 1)
	pdf.appendString(" 0 R]\n")
	pdf.appendString(">>\n")
	pdf.endobj()
	font.objNumber = pdf.getObjNumber()
	pdf.fonts = append(pdf.fonts, font)

	return font
}

// NewFontStream1 constructs font object from .ttf.stream and add it to the PDF
func NewFontStream1(pdf *PDF, reader io.Reader) *Font {
	font := new(Font)
	FontStream1(pdf, font, reader)
	font.SetSize(defaultFontSize)
	return font
}

// NewFontStream2 constructs font object from .ttf.stream and add it to the array of PDFobj
func NewFontStream2(objects *[]*PDFobj, reader io.Reader) *Font {
	font := new(Font)
	FontStream2(objects, font, reader)
	font.SetSize(defaultFontSize)
	return font
}

// NewFont constructs font object from OpenType and TrueType font.
func NewFont(pdf *PDF, reader io.Reader) *Font {
	font := new(Font)
	registerOpenTypeFont(pdf, font, reader)
	font.SetSize(defaultFontSize)
	return font
}

// SetSize sets the size of this font.
func (font *Font) SetSize(fontSize float32) *Font {
	font.size = fontSize
	if font.isCJK {
		font.ascent = font.size
		font.descent = font.ascent / 4
		font.bodyHeight = font.ascent + font.descent
		return font
	}
	font.ascent = float32(font.fontAscent) * font.size / float32(font.unitsPerEm)
	font.descent = -float32(font.fontDescent) * font.size / float32(font.unitsPerEm)
	font.bodyHeight = font.ascent + font.descent
	font.underlineThickness = float32(font.fontUnderlineThickness) * font.size / float32(font.unitsPerEm)
	font.underlinePosition = -float32(font.fontUnderlinePosition)*font.size/float32(font.unitsPerEm) + font.underlineThickness/2.0
	return font
}

// GetSize returns the current font size.
func (font *Font) GetSize() float32 {
	return font.size
}

// SetKernPairs sets the kerning for the selected font to 'true' or 'false'
// depending on the passed value of kernPairs parameter.
// The kerning is implemented only for the 14 standard fonts.
func (font *Font) SetKernPairs(kernPairs bool) {
	font.kernPairs = kernPairs
}

// GetAscent returns the ascent of this font.
func (font *Font) GetAscent() float32 {
	return font.ascent
}

// GetDescent returns the descent of this font.
func (font *Font) GetDescent() float32 {
	return font.descent
}

// GetHeight returns the height of this font.
func (font *Font) GetHeight() float32 {
	return font.ascent + font.descent
}

// GetBodyHeight returns the height of the body of the font.
func (font *Font) GetBodyHeight() float32 {
	return font.bodyHeight
}

// GetFitChars returns the number of characters from the specified text string
// that will fit within the specified width.
func (font *Font) GetFitChars(text string, width float32) int {
	w := width * float32(font.unitsPerEm) / font.size
	if font.isCJK {
		return int(w / font.ascent)
	}

	if font.isCoreFont {
		return font.getCoreFontFitChars(text, w)
	}

	runes := []rune(text)
	i := 0
	for i < len(runes) {
		c1 := runes[i]
		if c1 < font.firstChar || c1 > font.lastChar {
			w -= float32(font.advanceWidth[0])
		} else {
			w -= float32(font.glyphWidth[c1])
		}
		if w < 0 {
			break
		}
		i++
	}

	return i
}

func (font *Font) getCoreFontFitChars(text string, width float32) int {
	w := width

	runes := []rune(text)
	i := 0
	for i < len(runes) {
		ch := runes[i]
		if ch < font.firstChar || ch > font.lastChar {
			ch = 32
		}

		ch -= 32
		w -= float32(font.metrics[ch][1])
		if w < 0 {
			return i
		}

		if font.kernPairs && i < (len(runes)-1) {
			c2 := runes[i+1]
			if c2 < font.firstChar || c2 > font.lastChar {
				c2 = 32
			}
			for j := 2; j < len(font.metrics[ch]); j += 2 {
				if rune(font.metrics[c2][j]) == c2 {
					w -= float32(font.metrics[c2][j+1])
					if w < 0 {
						return i
					}
					break
				}
			}
		}
		i++
	}

	return i
}

// SetItalic sets the skew15 private variable.
// When the variable is set to 'true' all glyphs in the font are skewed on 15 degrees.
// This makes a regular font look like an italic type font.
// Use this method when you don't have real italic font in the font family,
// or when you want to generate smaller PDF files.
// For example you could embed only the Regular and Bold fonts and synthesize the RegularItalic and BoldItalic.
func (font *Font) SetItalic(skew15 bool) {
	font.skew15 = skew15
}

// stringWidth returns the width of the specified string when drawn on the
// page with this font using the current font size.
func (font *Font) stringWidth(str string) float32 {
	if str == "" {
		return 0.0
	}

	if font.isCJK {
		return float32(len([]rune(str))) * font.ascent
	}

	runes := []rune(str)
	var width float32
	for i, c1 := range runes {
		if font.isCoreFont {
			if c1 < rune(font.firstChar) || c1 > rune(font.lastChar) {
				c1 = 0x20
			}
			c1 -= 32

			width += float32(font.metrics[c1][1])

			if font.kernPairs && i < (len(runes)-1) {
				c2 := runes[i+1]
				if c2 < font.firstChar || c2 > font.lastChar {
					c2 = 32
				}
				for j := 2; j < len(font.metrics[c1]); j += 2 {
					if rune(font.metrics[c1][j]) == c2 {
						width += float32(font.metrics[c1][j+1])
						break
					}
				}
			}
		} else {
			if c1 < font.firstChar || c1 > font.lastChar {
				width += float32(font.advanceWidth[0])
			} else {
				width += float32(font.glyphWidth[c1])
			}
		}
	}

	return width * font.size / float32(font.unitsPerEm)
}

// StringWidth returns the width of text string drawn using main and fallback fonts.
func (font *Font) StringWidth(fallbackFont *Font, text string) float32 {
	var width float32 = 0.0

	if font.isCoreFont || font.isCJK || fallbackFont == nil || fallbackFont.isCoreFont || fallbackFont.isCJK {
		return font.stringWidth(text)
	}

	activeFont := font
	var buf strings.Builder
	runes := []rune(text)
	for _, ch := range runes {
		if activeFont.unicodeToGID[ch] == 0 {
			width += activeFont.stringWidth(buf.String())
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
	width += activeFont.stringWidth(buf.String())

	return width
}
