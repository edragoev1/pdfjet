// font.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"io"
	"os"
	"strings"
	"unicode"
	"unicode/utf8"

	"github.com/edragoev1/pdfjet/v9/src/cjkfont"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
)

// Font is used to create font objects.
// The font objects must be added to the PDF before they can be used to draw text.
type Font struct {
	name      string
	info      string
	objNumber int
	fontID    string
	pdf       *PDF // The PDF the font was added to, or nil for a font of an existing PDF

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
	fontLineGap            int16 // The space the font puts between its lines.
	firstChar              rune
	lastChar               rune
	capHeight              int16
	fontUnderlinePosition  int16
	fontUnderlineThickness int16
	advanceWidth           []uint16
	unicodeToGID           []int
	markToMarkOffsets      map[int][2]int // From the GPOS table, in a .otf, .ttf or .stream file
	markAnchors            []map[int][]int
	baseAnchors            []map[int][]int
	markData               []byte // Where the marks go, compressed, until a mark is drawn
	cff                    bool
	compressedSize         int
	uncompressedSize       int
	metrics                [][]int // Only used for core fonts.
	// checksum tells the font program this font was read from apart from
	// every other, so that a PDF embeds each one once.
	checksum uint64

	// Don't change the following default values!
	size       float32
	isCoreFont bool
	isCJK      bool
	skew15     bool
	kernPairs  bool
	// symbolic is true for Symbol and ZapfDingbats, whose characters are the
	// codes of their own encodings, not WinAnsi.
	symbolic bool

	ascent             float32
	descent            float32
	bodyHeight         float32
	underlinePosition  float32
	underlineThickness float32
}

const (
	defaultFontSize float32 = 12.0
)

// NewCoreFont is the constructor for the 14 standard fonts.
// Creates a font object and adds it to the PDF.
//
// Examples:
//
//		font1 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())
//		font2 := pdfjet.NewCoreFont(pdf, corefont.TimesItalic())
//		font3 := pdfjet.NewCoreFont(pdf, corefont.ZapfDingbats())
//
//	  - pdf: the PDF to add this font to.
//	  - coreFont: the core font, for example corefont.Helvetica().
func NewCoreFont(pdf *PDF, coreFont *corefont.CoreFont) *Font {
	font := new(Font)
	font.pdf = pdf
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
	font.symbolic = coreFont.Name == "Symbol" || coreFont.Name == "ZapfDingbats"
	font.fontUnderlinePosition = coreFont.UnderlinePosition
	font.fontUnderlineThickness = coreFont.UnderlineThickness
	font.fontAscent = coreFont.BBoxURy
	font.fontDescent = coreFont.BBoxLLy
	font.SetSize(font.size)

	pdf.newObj()
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
	pdf.endObj()

	font.objNumber = pdf.getObjNumber()
	pdf.fonts = append(pdf.fonts, font)

	return font
}

// newCoreFontForPDFobj creates a core font that is not added to a PDF, for
// PDFobj.AddCoreFontResource.
func newCoreFontForPDFobj(coreFont *corefont.CoreFont) *Font {
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
	font.symbolic = coreFont.Name == "Symbol" || coreFont.Name == "ZapfDingbats"
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
//   - pdf: the PDF to add this font to.
//   - cjkFont: the font. Please see Example_04.
func NewCJKFont(pdf *PDF, cjkFont cjkfont.Font) *Font {
	var fontName string
	switch cjkFont {
	case cjkfont.AdobeMingStdLight: // Chinese (Traditional) font
		fontName = "AdobeMingStd-Light"
	case cjkfont.STHeitiSCLight: // Chinese (Simplified) font
		fontName = "STHeitiSC-Light"
	case cjkfont.KozMinProVIRegular: // Japanese font
		fontName = "KozMinProVI-Regular"
	case cjkfont.AdobeMyungjoStdMedium: // Korean font
		fontName = "AdobeMyungjoStd-Medium"
	}

	font := new(Font)
	font.pdf = pdf
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
	pdf.newObj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /FontDescriptor\n")
	pdf.appendString("/FontName /")
	pdf.appendString(fontName)
	pdf.appendString("\n")
	pdf.appendString("/Flags 4\n")
	pdf.appendString("/FontBBox [0 0 0 0]\n")
	pdf.appendString(">>\n")
	pdf.endObj()

	// CIDFont Dictionary
	pdf.newObj()
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
		panic("Unsupported font: " + fontName)
	}
	pdf.appendString(">>\n")
	pdf.appendString(">>\n")
	pdf.endObj()

	// Type0 Font Dictionary
	pdf.newObj()
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
		panic("Unsupported font: " + fontName)
	}
	pdf.appendString("/DescendantFonts [")
	pdf.appendInteger(pdf.getObjNumber() - 1)
	pdf.appendString(" 0 R]\n")
	pdf.appendString(">>\n")
	pdf.endObj()
	font.objNumber = pdf.getObjNumber()
	pdf.fonts = append(pdf.fonts, font)

	return font
}

// NewFontStream1 constructs font object from .ttf.stream and add it to the PDF
func NewFontStream1(pdf *PDF, reader io.Reader) *Font {
	font := new(Font)
	font.pdf = pdf
	fontStream1(pdf, font, reader)
	font.SetSize(defaultFontSize)
	return font
}

// NewFontStream2 constructs font object from .ttf.stream and add it to the array of PDFobj
func NewFontStream2(objects *[]*PDFobj, reader io.Reader) *Font {
	font := new(Font)
	fontStream2(objects, font, reader)
	font.SetSize(defaultFontSize)
	return font
}

// NewFont constructs a font from an OpenType, TrueType, .otf.stream or
// .ttf.stream font and adds it to the PDF. The format is told from the first
// bytes the reader returns.
func NewFont(pdf *PDF, reader io.Reader) *Font {
	font := new(Font)
	font.pdf = pdf
	buffered := bufio.NewReader(reader)
	if isOpenTypeFont(buffered) {
		registerOpenTypeFont(pdf, font, buffered)
	} else {
		fontStream1(pdf, font, buffered)
	}
	font.SetSize(defaultFontSize)
	return font
}

// isOpenTypeFont returns true if the reader starts with the version of an
// OpenType or TrueType font.
func isOpenTypeFont(reader *bufio.Reader) bool {
	b, err := reader.Peek(4)
	if err != nil {
		return false
	}
	version := uint32(b[0])<<24 | uint32(b[1])<<16 | uint32(b[2])<<8 | uint32(b[3])
	return version == 0x00010000 || version == 0x74727565 || version == 0x4F54544F
}

// NewFontFromFile creates a font from the file at the specified path and adds it to the PDF.
// Files ending in .stream are read as stream fonts. It panics if the file cannot be opened.
func NewFontFromFile(pdf *PDF, filePath string) *Font {
	var font *Font
	f, err := os.Open(filePath)
	if err != nil {
		panic(err)
	}
	defer func(f *os.File) {
		err := f.Close()
		if err != nil {
			panic("Error closing file: " + err.Error())
		}
	}(f)
	reader := bufio.NewReader(f)
	if strings.HasSuffix(filePath, ".stream") {
		font = NewFontStream1(pdf, reader)
	} else {
		font = NewFont(pdf, reader)
	}
	return font
}

// checksumOf returns a number that identifies the font program: the units it
// is drawn in, its advance widths and its character map, which say what its
// glyphs are and which glyph each character has. Two fonts of one name that
// give the same number are the same program, whether it was read from a .otf,
// a .ttf or a .stream file, and the PDF embeds it once and writes one
// descriptor, one CID font and one ToUnicode map for both. The name is not
// enough on its own: PDFjet ships subsets of the Noto CJK fonts under the
// name of the whole font, and the text of the one embedded second was drawn
// with the glyphs of the first.
func checksumOf(font *Font) uint64 {
	hash := uint64(0xcbf29ce484222325)
	hash = foldChecksum(hash, font.unitsPerEm)
	hash = foldChecksum(hash, len(font.advanceWidth))
	for _, width := range font.advanceWidth {
		hash = foldChecksum(hash, int(width))
	}
	for _, gid := range font.unicodeToGID {
		hash = foldChecksum(hash, gid)
	}
	return hash
}

// foldChecksum is one step of the FNV-1a hash, which is the same in the four
// ports.
func foldChecksum(hash uint64, value int) uint64 {
	return (hash ^ uint64(uint32(value))) * 0x100000001b3
}

// SetSize sets the size of this font.
func (font *Font) SetSize(fontSize float32) *Font {
	font.size = fontSize
	if font.isCJK {
		font.ascent = font.size
		font.descent = font.size / 4
		font.bodyHeight = font.ascent + font.descent
		return font
	}
	font.ascent = float32(font.fontAscent) * font.size / float32(font.unitsPerEm)
	font.descent = -(float32(font.fontDescent) * font.size / float32(font.unitsPerEm))
	font.bodyHeight = font.ascent + font.descent
	font.underlineThickness = float32(font.fontUnderlineThickness) *
		font.size / float32(font.unitsPerEm)
	font.underlinePosition = -float32(font.fontUnderlinePosition)*
		font.size/float32(font.unitsPerEm) + font.underlineThickness/2.0
	return font
}

// GetSize returns the current font size.
func (font *Font) GetSize() float32 {
	return font.size
}

// GetName returns the name of this font.
func (font *Font) GetName() string {
	return font.name
}

// SetKernPairs sets the kerning for the selected font to 'true' or 'false'
// depending on the passed value of kernPairs parameter.
// The kerning is implemented only for the 14 standard fonts.
func (font *Font) SetKernPairs(kernPairs bool) *Font {
	font.kernPairs = kernPairs
	return font
}

// GetAscent returns the ascent of this font at the font size.
func (font *Font) GetAscent(fontSize float32) float32 {
	if font.isCJK {
		return fontSize
	}
	return float32(font.fontAscent) * fontSize / float32(font.unitsPerEm)
}

// GetDescent returns the descent of this font at the font size.
func (font *Font) GetDescent(fontSize float32) float32 {
	if font.isCJK {
		return fontSize / 4
	}
	return -float32(font.fontDescent) * fontSize / float32(font.unitsPerEm)
}

// GetLineGap returns the line gap at the font size: the space the font puts
// between the descent of a line and the ascent of the next one. It is 0 for
// most fonts, which leave that space in their ascent and descent, and 1 em for
// the Japanese and Chinese IBM Plex fonts, whose ascent and descent add up to
// 1 em.
func (font *Font) GetLineGap(fontSize float32) float32 {
	if font.isCJK {
		return 0
	}
	return float32(font.fontLineGap) * fontSize / float32(font.unitsPerEm)
}

// GetBodyHeight returns the height of the body of the font at the font size.
func (font *Font) GetBodyHeight(fontSize float32) float32 {
	return font.GetAscent(fontSize) + font.GetDescent(fontSize)
}

// GetUnderlineThickness returns the underline thickness at the font size.
func (font *Font) GetUnderlineThickness(fontSize float32) float32 {
	return float32(font.fontUnderlineThickness) * fontSize / float32(font.unitsPerEm)
}

// GetUnderlinePosition returns the underline position at the font size.
func (font *Font) GetUnderlinePosition(fontSize float32) float32 {
	return -(float32(font.fontUnderlinePosition) * fontSize / float32(font.unitsPerEm)) +
		font.GetUnderlineThickness(fontSize)/2.0
}

// GetFitChars returns the number of characters from the specified text string
// that will fit within the specified width.
func (font *Font) GetFitChars(text string, width float32) int {
	w := width * float32(font.unitsPerEm) / font.size
	runes := []rune(text)
	if font.isCJK {
		// Every glyph of a CJK font is as wide as the font size.
		return max(0, min(int(width/font.size), len(runes)))
	}

	if font.isCoreFont {
		return font.getCoreFontFitChars(text, w)
	}

	i := 0
	for i < len(runes) {
		w -= float32(font.advanceWidthOf(runes[i]))
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
		c1 := font.coreFontCode(runes[i])
		w -= float32(font.metrics[c1-32][1])
		if w < 0 {
			return i
		}

		if font.kernPairs && i < (len(runes)-1) {
			w -= float32(font.kerning(c1, font.coreFontCode(runes[i+1])))
			if w < 0 {
				return i
			}
		}
		i++
	}

	return i
}

// coreFontCode returns the code of the character in the encoding of this core
// font: WinAnsi, which puts ’ at 146, “ and ” at 147 and 148, the dashes at
// 150 and 151 and € at 128, or, for Symbol and ZapfDingbats, the character
// itself. A character that is not in the encoding is drawn as a space.
func (font *Font) coreFontCode(cp rune) int {
	if !font.symbolic {
		cp = winAnsiCode(cp)
	}
	if cp < 32 || cp > 255 {
		return 32
	}
	return int(cp)
}

// winAnsiCode returns the WinAnsi code of the character: the character itself
// below 128 and from 160 to 255, the code of the character WinAnsi puts from
// 128 to 159, and a space for any other character, the C1 controls U+0080 to
// U+009F too.
func winAnsiCode(cp rune) rune {
	if cp < 0x80 || (cp >= 0xA0 && cp <= 0xFF) {
		return cp
	}
	switch cp {
	case 0x20AC: // €
		return 128
	case 0x201A: // ‚
		return 130
	case 0x0192: // ƒ
		return 131
	case 0x201E: // „
		return 132
	case 0x2026: // …
		return 133
	case 0x2020: // †
		return 134
	case 0x2021: // ‡
		return 135
	case 0x02C6: // ˆ
		return 136
	case 0x2030: // ‰
		return 137
	case 0x0160: // Š
		return 138
	case 0x2039: // ‹
		return 139
	case 0x0152: // Œ
		return 140
	case 0x017D: // Ž
		return 142
	case 0x2018: // ‘
		return 145
	case 0x2019: // ’
		return 146
	case 0x201C: // “
		return 147
	case 0x201D: // ”
		return 148
	case 0x2022: // •
		return 149
	case 0x2013: // –
		return 150
	case 0x2014: // —
		return 151
	case 0x02DC: // ˜
		return 152
	case 0x2122: // ™
		return 153
	case 0x0161: // š
		return 154
	case 0x203A: // ›
		return 155
	case 0x0153: // œ
		return 156
	case 0x017E: // ž
		return 158
	case 0x0178: // Ÿ
		return 159
	}
	return 32
}

// kerning returns the kerning of a pair of characters of this core font, in
// 1/1000 of the font size: negative when the second character moves closer to
// the first, and 0 when the font has no kerning for the pair.
func (font *Font) kerning(c1, c2 int) int {
	row := font.metrics[c1-32]
	for j := 2; j < len(row); j += 2 {
		if row[j] == c2 {
			return row[j+1]
		}
	}
	return 0
}

// SetItalic sets the skew15 private variable.
// When the variable is set to 'true' all glyphs in the font are skewed on 15 degrees.
// This makes a regular font look like an italic type font.
// Use this method when you don't have real italic font in the font family,
// or when you want to generate smaller PDF files.
// For example, you could embed only the Regular and Bold fonts and synthesize the RegularItalic and BoldItalic.
func (font *Font) SetItalic(skew15 bool) *Font {
	font.skew15 = skew15
	return font
}

// StringWidth returns the width of the specified string when drawn on the
// page with this font using the current font size.
func (font *Font) StringWidth(fontSize float32, str string) float32 {
	var width float32 = 0.0
	if str == "" {
		return width
	}

	runes := []rune(str)
	if font.isCJK {
		// Every glyph of a CJK font is as wide as the font size.
		return float32(len(runes)) * fontSize
	}

	if font.isCoreFont {
		for i, cp := range runes {
			c1 := font.coreFontCode(cp)
			width += float32(font.metrics[c1-32][1])
			if font.kernPairs && i < (len(runes)-1) {
				width += float32(font.kerning(c1, font.coreFontCode(runes[i+1])))
			}
		}
	} else {
		for _, c1 := range runes {
			width += float32(font.advanceWidthOf(c1))
		}
	}

	return width * fontSize / float32(font.unitsPerEm)
}

// advanceWidthOf returns the advance width, in font units, of the glyph that
// Page draws for the character: none for an RLM, LRM, ZWNJ, ZWJ or byte order
// mark, which are not drawn, and that of a space for a character the font does
// not cover.
func (font *Font) advanceWidthOf(c rune) int {
	if isJoinerOrRLM(c) || c == 0xFEFF {
		return 0
	}
	gid := 0
	if c < font.firstChar || c > font.lastChar {
		gid = font.unicodeToGID[0x20]
	} else {
		gid = font.unicodeToGID[c]
	}
	return font.glyphAdvance(gid)
}

// glyphAdvance returns the advance width of the glyph, in font units. A font
// can list fewer advance widths than it has glyphs, and the glyphs past the
// end have the width of the last one, as OpenType says and the PDF font's /DW
// does.
func (font *Font) glyphAdvance(gid int) int {
	return int(font.advanceWidth[min(gid, len(font.advanceWidth)-1)])
}

// hasGlyph returns true if the font has a glyph for the character. A character
// past the end of the glyph table of the font, like an emoji, has none, and a
// core font has the characters of its encoding.
func (font *Font) hasGlyph(c rune) bool {
	if font.isCoreFont {
		return c == 32 || font.coreFontCode(c) != 32
	}
	return int(c) < len(font.unicodeToGID) && font.unicodeToGID[c] != 0
}

// fontOf returns the font, of the font and its fallback font, that draws the
// character at byte index i of the text, when the character before it is drawn
// with the active font: the font when it has a glyph for the character, else
// the fallback font when that has one, else the font. A combining mark stays
// with the character before it when that font has it, and an RLM, LRM, ZWNJ or
// ZWJ goes with the character after it.
func fontOf(font, fallbackFont, active *Font, text string, i int) *Font {
	cp, size := utf8.DecodeRuneInString(text[i:])
	if i > 0 && unicode.Is(unicode.Mn, cp) && active.hasGlyph(cp) {
		return active
	}
	if isJoinerOrRLM(cp) && i+size < len(text) {
		cp, _ = utf8.DecodeRuneInString(text[i+size:])
	}
	if font.hasGlyph(cp) || !fallbackFont.hasGlyph(cp) {
		return font
	}
	return fallbackFont
}

// StringWidthUsingFallbackFont returns the width of text string drawn using main and fallback fonts.
func (font *Font) StringWidthUsingFallbackFont(fallbackFont *Font, fontSize float32, text string) float32 {
	return font.stringWidthFBSizes(fallbackFont, fontSize, fontSize, text)
}

// stringWidthFBSizes returns the width of a string drawn using two fonts, with
// the characters in the fallback font at the fallback font size.
func (font *Font) stringWidthFBSizes(fallbackFont *Font, fontSize, fallbackFontSize float32, text string) float32 {
	var width float32 = 0.0

	if font.isCJK || fallbackFont == nil || fallbackFont.isCJK {
		return font.StringWidth(fontSize, text)
	}

	activeFont := font
	activeSize := fontSize
	// The runs of text drawn with one font are the pieces of the string
	// between the characters that switch fonts, so measuring them copies
	// nothing: text that is all in the primary font is measured in one piece.
	start := 0
	for i := 0; i < len(text); {
		_, size := utf8.DecodeRuneInString(text[i:])
		if charFont := fontOf(font, fallbackFont, activeFont, text, i); charFont != activeFont {
			width += activeFont.StringWidth(activeSize, text[start:i])
			start = i
			activeFont = charFont
			activeSize = fontSize
			if activeFont != font {
				activeSize = fallbackFontSize
			}
		}
		i += size
	}
	width += activeFont.StringWidth(activeSize, text[start:])

	return width
}

// isJoinerOrRLM returns true for the right-to-left and left-to-right marks
// and the zero width non-joiner and joiner, which are not drawn: Page gives
// the glyph before or after them an actual text.
func isJoinerOrRLM(ch rune) bool {
	return ch == 0x200F || ch == 0x200E || ch == 0x200C || ch == 0x200D
}
