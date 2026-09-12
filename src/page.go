//
// page.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.
//

package pdfjet

import (
	"fmt"
	"log"
	"math"
	"strconv"
	"strings"
	"unicode"
	"unicode/utf16"

	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/fastfloat"
	"github.com/edragoev1/pdfjet/v9/src/pathoperator"
	"github.com/edragoev1/pdfjet/v9/src/shape"
	"github.com/edragoev1/pdfjet/v9/src/structtype"
	"github.com/edragoev1/pdfjet/v9/src/token"
)

// Page is used to create PDF page objects.
//
// Please note:
//   - The coordinate (0.0, 0.0) is the top left corner of the page.
//   - Page sizes are in points; 1 point is 1/72 inch.
type Page struct {
	pdf           *PDF
	buf           []byte
	rotateDegrees float32
	pageObj       *PDFobj
	objNumber     int
	renderingMode int
	width         float32
	height        float32

	cropBox  []float32
	bleedBox []float32
	trimBox  []float32
	artBox   []float32

	penColor   [3]float32
	brushColor [3]float32

	tmx [4]float32
	tm0 []byte
	tm1 []byte
	tm2 []byte
	tm3 []byte

	textFontSize float32 // The font size of the text drawn last
	textRise     float32

	penWidth          float32
	lineCapStyle      int
	lineJoinStyle     int
	strokeDashPattern string
	savedHeight       float32

	contents     []int
	annots       []*Annotation
	destinations []*Destination
	structures   []*StructElem

	mcid int
}

// Indices in an Android Matrix value array, for Transform.
const (
	MScaleX = iota // Index of the horizontal scale.
	MSkewX         // Index of the horizontal skew.
	MTransX        // Index of the horizontal translation.
	MSkewY         // Index of the vertical skew.
	MScaleY        // Index of the vertical scale.
	MTransY        // Index of the vertical translation.
)

// NewPage creates a page of the specified size and adds it to the PDF.
func NewPage(pdf *PDF, pageSize [2]float32) *Page {
	return newPage(pdf, pageSize, true)
}

// NewPageDetached creates a page of the specified size without adding it to the PDF.
func NewPageDetached(pdf *PDF, pageSize [2]float32) *Page {
	return newPage(pdf, pageSize, false)
}

// NewPage constructs page object and adds it to the PDF document.
//
// Please note:
//   - The coordinate (0.0, 0.0) is the top left corner of the page.
//   - Page sizes are in points; 1 point is 1/72 inch.
//
// @param pdf the pdf object.
// @param pageSize the page size of this page.
// @param addPageToPDF boolean flag.
func newPage(pdf *PDF, pageSize [2]float32, addToPDF bool) *Page {
	page := new(Page)
	page.pdf = pdf
	page.contents = []int{}
	page.width = pageSize[0]
	page.height = pageSize[1]
	page.strokeDashPattern = "[] 0"
	page.savedHeight = math.MaxFloat32
	page.penWidth = 0.5
	page.tmx = [4]float32{1.0, 0.0, 0.0, 1.0}
	page.tm0 = fastfloat.ToByteArray(page.tmx[0])
	page.tm1 = fastfloat.ToByteArray(page.tmx[1])
	page.tm2 = fastfloat.ToByteArray(page.tmx[2])
	page.tm3 = fastfloat.ToByteArray(page.tmx[3])
	if addToPDF {
		pdf.AddPage(page)
	}
	return page
}

// NewPageFromObject creates page object from PDFobj.
func NewPageFromObject(pdf *PDF, pageObj *PDFobj) *Page {
	page := new(Page)
	page.pdf = pdf
	page.pageObj = page.removeComments(pageObj)
	page.width = pageObj.GetPageSize()[0]
	page.height = pageObj.GetPageSize()[1]
	page.tmx = [4]float32{1.0, 0.0, 0.0, 1.0}
	page.tm0 = fastfloat.ToByteArray(page.tmx[0])
	page.tm1 = fastfloat.ToByteArray(page.tmx[1])
	page.tm2 = fastfloat.ToByteArray(page.tmx[2])
	page.tm3 = fastfloat.ToByteArray(page.tmx[3])
	page.SaveGraphicsState()
	if pageObj.gsNumber != -1 {
		page.appendString("/GS")
		page.appendInteger(pageObj.gsNumber + 1)
		page.appendString(" gs\n")
	}
	return page
}

// Complete completes adding content to the existing PDF.
func (page *Page) Complete(objects *[]*PDFobj) {
	page.RestoreGraphicsState()
	page.pageObj.AddContent(page.getContent(), objects)
}

// removeComments removes object dictionary comments.
func (page *Page) removeComments(obj *PDFobj) *PDFobj {
	list := make([]string, 0)
	comment := false
	for _, str := range obj.dict {
		if str == "%" {
			comment = true
		} else {
			if strings.HasPrefix(str, "/") {
				comment = false
				list = append(list, str)
			} else {
				if !comment {
					list = append(list, str)
				}
			}
		}
	}
	obj.dict = list
	return obj
}

// AddCoreFontResource adds core font to the PDF objects.
func (page *Page) AddCoreFontResource(coreFont *corefont.CoreFont, objects *[]*PDFobj) *Font {
	return page.pageObj.AddCoreFontResource(coreFont, objects)
}

// AddImageResource adds an image to the PDF objects.
func (page *Page) AddImageResource(image *Image, objects *[]*PDFobj) {
	page.pageObj.AddImageResource(image, objects)
}

// AddFontResource adds font to the PDF objects.
func (page *Page) AddFontResource(font *Font, objects *[]*PDFobj) {
	page.pageObj.AddFontResource(font, objects)
}

func (page *Page) getContent() []byte {
	return page.buf
}

// AddDestination adds destination to this page.
// @param name The destination name.
// @param yPosition The vertical position of the destination on this page.
func (page *Page) AddDestination(name string, yPosition float32) *Destination {
	return page.AddDestinationAt(name, 0.0, yPosition)
}

// AddDestinationAt adds destination to this page.
// @param name The destination name.
// @param xPosition The horizontal position of the destination on this page.
// @param yPosition The vertical position of the destination on this page.
func (page *Page) AddDestinationAt(name string, xPosition, yPosition float32) *Destination {
	dest := NewDestination(name, xPosition, page.height-yPosition)
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
	page.DrawStringUsingColorMap(font1, font2, font1.size, text, x, y, [3]float32{0.0, 0.0, 0.0}, nil)
}

// DrawStringUsingFontSize draws a string using the specified font and font size
// at the x, y location. The baseline of the leftmost character is at (x, y).
func (page *Page) DrawStringUsingFontSize(
	font *Font, fontSize float32, text string, x, y float32) {
	page.drawString(font, fontSize, text, x, y, [3]float32{0.0, 0.0, 0.0}, nil)
}

// DrawStringUsingColorMap draws the text given by the specified string,
// using the specified main font and the current brushColor color.
// If the main font is missing some glyphs - the fallback font is used.
// The baseline of the leftmost character is at position (x, y) on the page.
func (page *Page) DrawStringUsingColorMap(
	font, fallbackFont *Font, fontSize float32, text string, x, y float32, brush [3]float32, colors map[string]int32) {
	if font.isCoreFont || font.isCJK || fallbackFont == nil || fallbackFont.isCoreFont || fallbackFont.isCJK {
		page.drawString(font, fontSize, text, x, y, brush, colors)
	} else {
		activeFont := font
		var buf strings.Builder
		runes := []rune(text)
		for i, ch := range runes {
			// An RLM, ZWNJ or ZWJ goes with the character after it.
			next := ch
			if isJoinerOrRLM(ch) && i+1 < len(runes) {
				next = runes[i+1]
			}
			if activeFont.unicodeToGID[next] == 0 {
				page.drawString(activeFont, fontSize, buf.String(), x, y, brush, colors)
				x += activeFont.StringWidth(fontSize, buf.String())
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
		page.drawString(activeFont, fontSize, buf.String(), x, y, brush, colors)
	}
}

// drawString draws the text given by the specified string,
// using the specified font and the current brushColor color.
// The baseline of the leftmost character is at position (x, y) on the page.
//
// @param font the font to use.
// @param str the string to be drawn.
// @param x the x coordinate.
// @param y the y coordinate.
func (page *Page) drawString(
	font *Font, fontSize float32, str string, x, y float32, brush [3]float32, colors map[string]int32) {
	if str == "" {
		return
	}
	page.appendString("BT\n")
	page.setTextFont(font, fontSize)

	if page.renderingMode != 0 {
		page.appendInteger(page.renderingMode)
		page.appendString(" Tr\n")
	}

	if font.skew15 &&
		page.tmx[0] == 1.0 &&
		page.tmx[1] == 0.0 &&
		page.tmx[2] == 0.0 &&
		page.tmx[3] == 1.0 {
		var skew float32 = 0.26
		page.appendFloat32(page.tmx[0])
		page.appendString(" ")
		page.appendFloat32(page.tmx[1])
		page.appendString(" ")
		page.appendFloat32(page.tmx[2] + skew)
		page.appendString(" ")
		page.appendFloat32(page.tmx[3])
	} else {
		page.appendByteArray(page.tm0)
		page.appendString(" ")
		page.appendByteArray(page.tm1)
		page.appendString(" ")
		page.appendByteArray(page.tm2)
		page.appendString(" ")
		page.appendByteArray(page.tm3)
	}
	page.appendString(" ")
	page.appendFloat32(x)
	page.appendString(" ")
	page.appendFloat32(page.height - y)
	page.appendString(" Tm\n")

	if colors == nil {
		page.SetBrushColorRGB(brush)
		if font.isCoreFont {
			page.appendString("[<")
			page.drawASCIIString(font, str)
			page.appendString(">] TJ\n")
		} else {
			page.appendString("<")
			page.drawUnicodeString(font, str)
			page.appendString("> Tj\n")
		}
	} else {
		page.drawColoredString(font, str, brush, colors)
	}
	page.appendString("ET\n")
}

func (page *Page) drawASCIIString(font *Font, text string) {
	runes := []rune(text)
	for i, c1 := range runes {
		if c1 < font.firstChar || c1 > font.lastChar {
			page.appendByteAsHex(0x20)
			continue
		}
		page.appendByteAsHex(byte(c1))
		if font.isCoreFont && font.kernPairs && i < (len(runes)-1) {
			c1 -= 32
			c2 := runes[i+1]
			if c2 < font.firstChar || c2 > font.lastChar {
				c2 = 32
			}
			for i := 2; i < len(font.metrics[c1]); i += 2 {
				if font.metrics[c1][i] == int(c2) {
					page.appendString(">")
					page.appendInteger(-font.metrics[c1][i+1])
					page.appendString("<")
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
			if c1 != 0xFEFF { // BOM marker
				if c1 < font.firstChar || c1 > font.lastChar {
					// page.appendString(fmt.Sprintf("%04X", 0x0020))
					page.appendCodePointAsHex(0x0020)
				} else {
					// page.appendString(fmt.Sprintf("%04X", c1))
					page.appendCodePointAsHex(int(c1))
				}
			}
		}
	} else if font.markAnchors == nil && !strings.ContainsRune(text, 0x200F) && !strings.ContainsRune(text, 0x200E) &&
		!strings.ContainsRune(text, 0x200C) && !strings.ContainsRune(text, 0x200D) { // RLM, LRM, ZWNJ, ZWJ
		for _, c1 := range runes {
			if c1 != 0xFEFF { // BOM marker
				page.appendCodePointAsHex(glyphOf(font, c1))
			}
		}
	} else {
		// The marks are moved to where the GPOS table of the font puts them, the
		// characters Bidi mirrored, each after an RLM, are given the characters
		// they stand for as actual text, and a zero width non-joiner or joiner
		// that the font has no glyph for is put in the actual text of the glyph
		// before it. A font that has a glyph for it draws the glyph, which has
		// no width.
		codePoints := make([]rune, 0, len(runes))
		gids := make([]int, 0, len(runes))
		var mirroredAt []bool
		var joiners []rune
		var runEdge []bool // An LRM before the glyph at the index
		hasMarks := false
		afterRLM := false
		for _, c1 := range runes {
			if c1 == 0x200F { // RLM
				afterRLM = true
			} else if c1 == 0x200E { // LRM
				// Bidi puts an LRM on each side of the brackets of a right to
				// left pair around left to right text; the glyphs between the
				// two are drawn in one span, see appendRun.
				if runEdge == nil {
					runEdge = make([]bool, len(runes)+1)
				}
				runEdge[len(codePoints)] = true
			} else if (c1 == 0x200C || c1 == 0x200D) && font.unicodeToGID[c1] == 0 {
				// A ZWNJ or ZWJ the font has no glyph for goes with the glyph
				// before it. One with nothing before it is left out.
				if len(codePoints) > 0 {
					if joiners == nil {
						joiners = make([]rune, len(runes))
					}
					joiners[len(codePoints)-1] = c1
				}
			} else if c1 != 0xFEFF { // BOM marker
				if _, ok := mirrored(c1); afterRLM && ok {
					if mirroredAt == nil {
						mirroredAt = make([]bool, len(runes))
					}
					mirroredAt[len(codePoints)] = true
				}
				afterRLM = false
				codePoints = append(codePoints, c1)
				gids = append(gids, glyphOf(font, c1))
				hasMarks = hasMarks || isMark(c1)
			}
		}
		var offsets []int
		if hasMarks && font.markAnchors != nil {
			offsets = markOffsets(font, codePoints, gids)
		} else if mirroredAt == nil && joiners == nil && runEdge == nil {
			for _, gid := range gids {
				page.appendCodePointAsHex(gid)
			}
			return
		}
		n := len(gids)
		i := 0
		for i < n {
			if runEdge != nil && runEdge[i] {
				j := i + 1
				for j < n && !runEdge[j] {
					j++
				}
				page.appendRun(font, codePoints, gids, offsets, i, j)
				runEdge[j] = false
				i = j
				continue
			}
			end := i + 1
			if codePoints[i] != 0x20 {
				for end < n && codePoints[end] != 0x20 && (runEdge == nil || !runEdge[end]) {
					end++
				}
			}
			// A glyph with a joiner after it is drawn in a span of its own, and
			// so are the mirrored characters at the ends of a word with moved
			// marks, like its brackets: MuPDF leaves out or repeats text when
			// the glyphs of a span do not match its actual text one by one.
			k := i
			for k < end {
				j := k
				for j < end && (joiners == nil || joiners[j] == 0) {
					j++
				}
				page.appendWord(font, codePoints, gids, mirroredAt, offsets, k, j)
				if j < end {
					page.appendGlyphWithActualText(font, codePoints, gids, offsets, mirroredAt, joiners, j)
				}
				k = j + 1
			}
			i = end
		}
	}
}

// appendRun draws the glyphs of a left to right run in brackets, nested in
// right to left text, in a marked content span with the text of the run
// between two left-to-right marks as its actual text. Poppler and MuPDF then
// keep the brackets with the run when the text is copied; with the brackets
// in spans of their own, or mirrored back like the other brackets, they come
// out the wrong way round. No glyph stands in for the marks: MuPDF moves the
// first bracket to the right to left text when one does.
func (page *Page) appendRun(font *Font, codePoints []rune, gids, offsets []int, start, end int) {
	var text strings.Builder
	text.WriteRune(0x200E)
	for k := start; k < end; k++ {
		text.WriteString(textOf(font, codePoints[k]))
	}
	text.WriteRune(0x200E)
	page.appendString("> Tj\n/Span <</ActualText <")
	page.appendString(toUTF16Hex(text.String()))
	page.appendString(">>> BDC\n<")
	for k := start; k < end; k++ {
		if offsets == nil || (offsets[2*k] == 0 && offsets[2*k+1] == 0) {
			page.appendCodePointAsHex(gids[k])
		} else {
			page.appendMovedGlyph(font, gids[k], offsets[2*k], offsets[2*k+1])
		}
	}
	page.appendString("> Tj\nEMC\n<")
}

// appendWord draws the glyphs of a word, or of the part of a word before a
// joiner, in a marked content span with the text of the word when it has moved
// marks, with the mirrored characters at its ends in spans of their own.
func (page *Page) appendWord(
	font *Font, codePoints []rune, gids []int, mirroredAt []bool, offsets []int, i, end int) {
	wordStart, wordEnd := i, end
	for mirroredAt != nil && wordStart < wordEnd && mirroredAt[wordStart] {
		wordStart++
	}
	for mirroredAt != nil && wordEnd > wordStart && mirroredAt[wordEnd-1] {
		wordEnd--
	}
	if offsets != nil && isMoved(offsets, wordStart, wordEnd) {
		page.appendGlyphs(font, codePoints, gids, mirroredAt, i, wordStart)
		page.appendWordWithMovedMarks(font, codePoints, gids, offsets, wordStart, wordEnd)
		page.appendGlyphs(font, codePoints, gids, mirroredAt, wordEnd, end)
	} else {
		page.appendGlyphs(font, codePoints, gids, mirroredAt, i, end)
	}
}

// appendGlyphs draws the glyphs from start to end, each character Bidi mirrored
// in a span of its own.
func (page *Page) appendGlyphs(font *Font, codePoints []rune, gids []int, mirroredAt []bool, start, end int) {
	for k := start; k < end; k++ {
		if mirroredAt != nil && mirroredAt[k] {
			page.appendGlyphWithActualText(font, codePoints, gids, nil, mirroredAt, nil, k)
		} else {
			page.appendCodePointAsHex(gids[k])
		}
	}
}

// appendGlyphWithActualText draws a glyph in a marked content span that has the
// text it stands for as its actual text: the character Bidi mirrored, since
// text extraction reverses right to left text but does not mirror the brackets
// back, or the text of the glyph followed by the zero width non-joiner or
// joiner after it, which the font has no glyph for. A space glyph that takes no
// room stands in for the joiner, so that the span has a glyph for each
// character of its actual text but the last: MuPDF pairs the characters with
// the glyphs, and repeats text when a glyph matches its character after one
// that does not. Poppler takes the actual text as it is.
func (page *Page) appendGlyphWithActualText(
	font *Font, codePoints []rune, gids []int, offsets []int, mirroredAt []bool, joiners []rune, k int) {
	c := codePoints[k]
	var joiner rune
	if joiners != nil {
		joiner = joiners[k]
	}
	var text strings.Builder
	if mirroredAt != nil && mirroredAt[k] {
		m, _ := mirrored(c)
		text.WriteRune(m)
	} else {
		text.WriteString(textOf(font, c))
	}
	if joiner != 0 {
		text.WriteRune(joiner)
	}
	page.appendString("> Tj\n/Span <</ActualText <")
	page.appendString(toUTF16Hex(text.String()))
	page.appendString(">>> BDC\n")
	if offsets != nil && (offsets[2*k] != 0 || offsets[2*k+1] != 0) {
		page.appendString("<")
		page.appendMovedGlyph(font, gids[k], offsets[2*k], offsets[2*k+1])
		page.appendString("> Tj\n")
	} else {
		page.appendString("<")
		page.appendCodePointAsHex(gids[k])
		page.appendString("> Tj\n")
	}
	if joiner != 0 {
		space := font.unicodeToGID[0x0020]
		page.appendString("[<")
		page.appendCodePointAsHex(space)
		page.appendString("> ")
		page.appendFloat32(1000.0 * float32(font.advanceWidth[space]) / float32(font.unitsPerEm))
		page.appendString("] TJ\n")
	}
	page.appendString("EMC\n<")
}

// textOf returns the text the glyph of the character maps to: a glyph missing
// from the font is a space, and an Arabic letter form is its letter.
func textOf(font *Font, c rune) string {
	if c < font.firstChar || c > font.lastChar {
		return " "
	} else if letters := lettersOf(c); letters != nil {
		return string(letters)
	}
	return string(c)
}

// glyphOf returns the glyph ID of the character, or of a space if the font does
// not have the character.
func glyphOf(font *Font, c rune) int {
	if c < font.firstChar || c > font.lastChar {
		return font.unicodeToGID[0x0020]
	}
	return font.unicodeToGID[c]
}

// markOffsets returns the offsets that move the marks to where the GPOS table
// of the font puts them, dx and dy in font units for each glyph. A mark goes on
// the letter or ligature before it. Right to left text is drawn in visual order,
// with the marks before their letter, so a Hebrew or Arabic mark goes on the
// letter after it. A mark that attaches to the mark before it, like a Thai tone
// mark above an upper vowel, goes on that mark instead. The marks of a letter
// are taken in logical order, sorted as HarfBuzz sorts them, so a fatha goes
// above a shadda whichever was typed first.
func markOffsets(font *Font, codePoints []rune, gids []int) []int {
	n := len(gids)
	// Where each glyph is drawn before it is moved, in font units.
	x := make([]int, n)
	for i := 1; i < n; i++ {
		x[i] = x[i-1] + int(font.advanceWidth[gids[i-1]])
	}
	offsets := make([]int, 2*n)
	for i := 0; i < n; i++ {
		if !isMark(codePoints[i]) {
			continue
		}
		step := -1
		if isRightToLeft(codePoints[i]) {
			step = 1
		}
		base := i + step
		for base >= 0 && base < n && isMark(codePoints[base]) {
			base += step
		}
		if base >= 0 && base < n {
			if offset := markToBaseOffset(font, codePoints[base], gids[base], gids[i]); offset != nil {
				offsets[2*i] = x[base] + offset[0] - x[i]
				offsets[2*i+1] = offset[1]
			}
		}
	}
	order := make([]int, n)
	start := 0
	for start < n {
		if !isMark(codePoints[start]) {
			start++
			continue
		}
		// The marks from start to end - 1 go on the same letter.
		rightToLeft := isRightToLeft(codePoints[start])
		end := start + 1
		for end < n && isMark(codePoints[end]) && isRightToLeft(codePoints[end]) == rightToLeft {
			end++
		}
		count := end - start
		for k := 0; k < count; k++ {
			mark := start + k
			if rightToLeft {
				mark = end - 1 - k
			}
			// Moves the mark back past the marks that sort after it. A mark of
			// order 0 is never moved, and no mark moves past it.
			rank := markOrder(codePoints[mark])
			l := k - 1
			for rank != 0 && l >= 0 && markOrder(codePoints[order[l]]) > rank {
				order[l+1] = order[l]
				l--
			}
			order[l+1] = mark
		}
		for k := 1; k < count; k++ {
			placeOnMark(font, gids, x, offsets, order[k], order[k-1])
		}
		start = end
	}
	return offsets
}

// markToBaseOffset returns the offset of the mark from the letter or ligature it
// goes on, in font units, or nil. A font can have the anchors of an isolated
// Arabic letter form only for its letter, which looks the same.
func markToBaseOffset(font *Font, baseCodePoint rune, baseGID, markGID int) []int {
	offset := anchorOffset(font, baseGID, markGID)
	if offset == nil {
		if letter := letterOfIsolatedForm(baseCodePoint); letter != 0 {
			offset = anchorOffset(font, font.unicodeToGID[letter], markGID)
		}
	}
	return offset
}

// anchorOffset returns the offset of the mark from the glyph it goes on, from
// the first MarkToBase or MarkToLigature subtable that has an anchor for both,
// or nil.
func anchorOffset(font *Font, baseGID, markGID int) []int {
	for i, marks := range font.markAnchors {
		mark, ok := marks[markGID]
		if !ok {
			continue
		}
		anchors, ok := font.baseAnchors[i][baseGID]
		c := 3 * mark[0]
		if ok && c+2 < len(anchors) && anchors[c] == 1 {
			return []int{anchors[c+1] - mark[1], anchors[c+2] - mark[2]}
		}
	}
	return nil
}

// placeOnMark moves the mark onto the other mark, if the font has an offset for
// the two.
func placeOnMark(font *Font, gids, x, offsets []int, mark, other int) {
	if offset, ok := font.markToMarkOffsets[(gids[other]<<16)|gids[mark]]; ok {
		offsets[2*mark] = offsets[2*other] + x[other] + offset[0] - x[mark]
		offsets[2*mark+1] = offsets[2*other+1] + offset[1]
	}
}

func isMark(c rune) bool {
	return unicode.In(c, unicode.Mn, unicode.Me)
}

// isRightToLeft returns true if the character is in the blocks of the right to
// left scripts.
func isRightToLeft(c rune) bool {
	return (c >= 0x0590 && c <= 0x08FF) ||
		(c >= 0xFB1D && c <= 0xFDFF) ||
		(c >= 0xFE70 && c <= 0xFEFF) ||
		(c >= 0x10800 && c <= 0x10FFF) ||
		(c >= 0x1E800 && c <= 0x1EFFF)
}

// markOrder returns the order of a mark among the marks of its letter, or 0 for
// a mark that stays where it is: the canonical combining class of the mark,
// changed as in HarfBuzz to put the Hebrew points in the order of the SBL Hebrew
// manual, and the Arabic shadda before the other Arabic vowel marks.
func markOrder(c rune) int {
	if c >= 0x05B0 && c <= 0x05C7 { // Hebrew points
		return hebrewMarkOrder[c-0x05B0]
	}
	if c >= 0x064B && c <= 0x0652 { // Arabic fathatan to sukun
		return arabicMarkOrder[c-0x064B]
	}
	switch c {
	case 0x0670: // ARABIC LETTER SUPERSCRIPT ALEF
		return 35
	case 0x0E38, 0x0E39: // THAI SARA U, SARA UU
		return 103
	case 0x0E3A: // THAI PHINTHU
		return 9
	case 0x0E48, 0x0E49, 0x0E4A, 0x0E4B: // THAI tone marks
		return 107
	case 0x0EB8, 0x0EB9: // LAO VOWEL SIGN U, UU
		return 118
	case 0x0EC8, 0x0EC9, 0x0ECA, 0x0ECB: // LAO tone marks
		return 122
	}
	return 0
}

var hebrewMarkOrder = [...]int{
	22, 15, 16, 17, 23, 18, 19, 20, // U+05B0 sheva to U+05B7 patah
	21, 14, 14, 24, 12, 25, 0, 13, // U+05B8 qamats to U+05BF rafe
	0, 10, 11, 0, 230, 220, 0, 21, // U+05C0 to U+05C7 qamats qatan
}

var arabicMarkOrder = [...]int{
	28, 29, 30, 31, 32, 33, 27, 34, // U+064B fathatan to U+0652 sukun
}

func isMoved(offsets []int, start, end int) bool {
	for k := start; k < end; k++ {
		if offsets[2*k] != 0 || offsets[2*k+1] != 0 {
			return true
		}
	}
	return false
}

// appendWordWithMovedMarks draws a word with moved marks in a marked content
// span that has the text of the word as its actual text. Text extraction would
// otherwise take a moved mark for text above or below the line, and break the
// word there. Poppler puts the actual text where the first glyph of the span is
// drawn, so a word that starts with a moved mark, like a Hebrew or Arabic word
// drawn in visual order, starts with a space drawn back over the space before
// it, and its actual text starts with a space. MuPDF takes the glyphs that match
// the actual text as they are, and leaves out a space drawn over a space.
func (page *Page) appendWordWithMovedMarks(font *Font, codePoints []rune, gids, offsets []int, start, end int) {
	leadingSpace := isMoved(offsets, start, start+1)
	var text strings.Builder
	if leadingSpace {
		text.WriteRune(' ')
	}
	for k := start; k < end; k++ {
		text.WriteString(textOf(font, codePoints[k])) // The text the glyphs map to
	}
	page.appendString("> Tj\n/Span <</ActualText <")
	page.appendString(toUTF16Hex(text.String()))
	page.appendString(">>> BDC\n")
	if leadingSpace {
		space := font.unicodeToGID[0x0020]
		page.appendString("[")
		page.appendFloat32(1000 * float32(font.advanceWidth[space]) / float32(font.unitsPerEm))
		page.appendString(" <")
		page.appendCodePointAsHex(space)
		page.appendString(">] TJ\n")
	}
	page.appendString("<")
	for k := start; k < end; k++ {
		if offsets[2*k] == 0 && offsets[2*k+1] == 0 {
			page.appendCodePointAsHex(gids[k])
		} else {
			page.appendMovedGlyph(font, gids[k], offsets[2*k], offsets[2*k+1])
		}
	}
	page.appendString("> Tj\nEMC\n<")
}

// appendMovedGlyph ends the string of glyphs, draws the glyph moved by dx and dy
// font units, and starts the string again.
func (page *Page) appendMovedGlyph(font *Font, gid, dx, dy int) {
	fontSize := font.size
	if page.textFontSize != 0 {
		fontSize = page.textFontSize
	}
	page.appendString("> Tj\n")
	page.appendFloat32(page.textRise + float32(dy)*fontSize/float32(font.unitsPerEm))
	page.appendString(" Ts\n")
	if dx == 0 {
		page.appendString("<")
		page.appendCodePointAsHex(gid)
		page.appendString("> Tj\n")
	} else {
		adjustment := 1000 * float32(dx) / float32(font.unitsPerEm)
		page.appendString("[")
		page.appendFloat32(-adjustment)
		page.appendString(" <")
		page.appendCodePointAsHex(gid)
		page.appendString("> ")
		page.appendFloat32(adjustment)
		page.appendString("] TJ\n")
	}
	page.appendFloat32(page.textRise)
	page.appendString(" Ts\n<")
}

// Pre-allocated hex digits
var hexDigits = [16]byte{
	'0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
	'A', 'B', 'C', 'D', 'E', 'F',
}

func (page *Page) appendByteAsHex(b byte) {
	page.buf = append(page.buf, hexDigits[(b>>4)&0xF], hexDigits[b&0xF])
}

func (page *Page) appendCodePointAsHex(codePoint int) {
	if codePoint <= 0xFFFF {
		page.buf = append(page.buf,
			hexDigits[(codePoint>>12)&0xF],
			hexDigits[(codePoint>>8)&0xF],
			hexDigits[(codePoint>>4)&0xF],
			hexDigits[codePoint&0xF],
		)
	} else {
		page.buf = append(page.buf,
			hexDigits[(codePoint>>20)&0xF],
			hexDigits[(codePoint>>16)&0xF],
			hexDigits[(codePoint>>12)&0xF],
			hexDigits[(codePoint>>8)&0xF],
			hexDigits[(codePoint>>4)&0xF],
			hexDigits[codePoint&0xF],
		)
	}
}

// SaveGraphicsState saves the current graphics state. Please see Example_31.
func (page *Page) SaveGraphicsState() {
	page.appendString("q\n")
}

// SetGraphicsState sets the graphics state. Please see Example_31.
// @param gs the graphics state to use.
func (page *Page) SetGraphicsState(gs *GraphicsState) *Page {
	var sb strings.Builder
	sb.WriteString("/CA ")
	sb.WriteString(fmt.Sprintf("%.2f", gs.GetAlphaStroking()))
	sb.WriteString(" ")
	sb.WriteString("/ca ")
	sb.WriteString(fmt.Sprintf("%.2f", gs.GetAlphaNonStroking()))
	state := sb.String()
	n, ok := page.pdf.states[state]
	if !ok {
		n = len(page.pdf.states) + 1
		page.pdf.states[state] = n
	}
	page.appendString("/GS")
	page.appendInteger(n)
	page.appendString(" gs\n")
	return page
}

// RestoreGraphicsState restores the last saved graphics state. Please see Example_31.
func (page *Page) RestoreGraphicsState() {
	page.appendString("Q\n")
}

// SetPenColor sets the pen color using a packed RGB color integer.
// The color should be provided in the 0x00RRGGBB format (hexadecimal),
// where RR, GG, and BB represent the red, green, and blue components
// of the color, respectively, each in the range 00 to FF (0-255).
//
// Parameters:
//
//	color: A 32-bit integer representing the color in the format 0x00RRGGBB.
//	       The method converts this integer into RGB float values between 0.0 and 1.0,
//	       and sets the pen color accordingly.
//
// Notes:
//   - The color components are extracted by bit-shifting and masking the integer
//     to separate the red, green, and blue channels. Each component is then scaled
//     to a float value between 0.0 and 1.0 (by dividing by 255).
//   - The method calls SetPenColorRGB internally to apply the color using float32 values.
func (page *Page) SetPenColor(color int32) *Page {
	r := float32((color>>16)&0xff) / 255.0
	g := float32((color>>8)&0xff) / 255.0
	b := float32((color)&0xff) / 255.0
	page.SetPenColorRGB([3]float32{r, g, b})
	return page
}

// SetPenColorRGB sets the pen color using an RGB color array.
// Each element in the array represents the red, green, and blue components
// of the color as floating-point values between 0.0 and 1.0.
//
// Parameters:
//
//	rgbColor: A fixed-size array of 3 float32 values representing the
//	red, green, and blue color components respectively. Each value should
//	be between 0.0 (no intensity) and 1.0 (full intensity).
//
// Notes:
//   - The method performs a range check to ensure that each color component
//     is within the valid range [0.0, 1.0]. If any component is out of range,
//     the method prints a warning and exits early without modifying the color.
//   - The method then sets the penColor and appends the color values to the
//     appropriate output stream (e.g., for a PDF or graphics context).
func (page *Page) SetPenColorRGB(rgbColor [3]float32) *Page {
	if rgbColor[0] < 0.0 || rgbColor[0] > 1.0 ||
		rgbColor[1] < 0.0 || rgbColor[1] > 1.0 ||
		rgbColor[2] < 0.0 || rgbColor[2] > 1.0 {
		log.Println("Warning: RGB color values must be between 0f and 1f. Ignoring request.")
		return page // Early exit if out of range
	}

	// Now set the penColor color
	page.penColor = rgbColor

	// Proceed with setting the color (example)
	page.appendFloat32(rgbColor[0])
	page.appendString(" ")
	page.appendFloat32(rgbColor[1])
	page.appendString(" ")
	page.appendFloat32(rgbColor[2])
	page.appendString(" RG\n")
	return page
}

// GetPenColorRGB returns the current pen color as an RGB float32 array.
// The returned array contains three float32 values representing the
// red, green, and blue components of the pen color, each in the range
// [0.0, 1.0].
//
// @return: A [3]float32 array representing the pen color in RGB format.
//
//	The array contains values in the range [0.0, 1.0] corresponding to
//	the red, green, and blue color components.
func (page *Page) GetPenColorRGB() [3]float32 {
	return page.penColor
}

// SetBrushColor sets the brush color using a packed RGB color integer.
// The color should be provided in the 0x00RRGGBB format (hexadecimal),
// where RR, GG, and BB represent the red, green, and blue components
// of the color, respectively, each in the range 00 to FF (0-255).
//
// Parameters:
//
//	color: A 32-bit integer representing the color in the format 0x00RRGGBB.
//	       The method converts this integer into RGB float values between 0.0 and 1.0,
//	       and sets the brush color accordingly.
//
// Notes:
//   - The color components are extracted by bit-shifting and masking the integer
//     to separate the red, green, and blue channels. Each component is then scaled
//     to a float value between 0.0 and 1.0 (by dividing by 255).
//   - The method calls SetBrushColorRGB internally to apply the color using float32 values.
func (page *Page) SetBrushColor(color int32) *Page {
	r := float32((color>>16)&0xff) / 255.0
	g := float32((color>>8)&0xff) / 255.0
	b := float32((color)&0xff) / 255.0
	page.SetBrushColorRGB([3]float32{r, g, b})
	return page
}

// SetBrushColorRGB sets the brush color using an RGB color array.
// Each element in the array represents the red, green, and blue components
// of the color as floating-point values between 0.0 and 1.0.
//
// Parameters:
//
//	rgbColor: A fixed-size array of 3 float32 values representing the
//	red, green, and blue color components respectively. Each value should
//	be between 0.0 (no intensity) and 1.0 (full intensity).
//
// Notes:
//   - The method performs a range check to ensure that each color component
//     is within the valid range [0.0, 1.0]. If any component is out of range,
//     the method prints a warning and exits early without modifying the color.
//   - The method then sets the brushColor and appends the color values to the
//     appropriate output stream (e.g., for a PDF or graphics context).
func (page *Page) SetBrushColorRGB(rgbColor [3]float32) *Page {
	if rgbColor[0] < 0.0 || rgbColor[0] > 1.0 ||
		rgbColor[1] < 0.0 || rgbColor[1] > 1.0 ||
		rgbColor[2] < 0.0 || rgbColor[2] > 1.0 {
		log.Println("Warning: RGB color values must be between 0f and 1f. Ignoring request.")
		return page // Early exit if out of range
	}

	// Now set the brush color
	page.brushColor = rgbColor

	// Proceed with setting the color (example)
	page.appendFloat32(rgbColor[0])
	page.appendString(" ")
	page.appendFloat32(rgbColor[1])
	page.appendString(" ")
	page.appendFloat32(rgbColor[2])
	page.appendString(" rg\n")
	return page
}

// GetBrushColorRGB returns the current brush color as an RGB float32 array.
// The returned array contains three float32 values representing the
// red, green, and blue components of the brush color, each in the range
// [0.0, 1.0].
//
// @return: A [3]float32 array representing the brush color in RGB format.
//
//	The array contains values in the range [0.0, 1.0] corresponding to
//	the red, green, and blue color components.
func (page *Page) GetBrushColorRGB() [3]float32 {
	return page.brushColor
}

// SetPenColorCMYK sets the color for stroking operations using CMYK.
// The penColor color is used when drawing lines and splines.
//
// @param c the cyan component is float value from 0.0 to 1.0.
// @param m the magenta component is float value from 0.0 to 1.0.
// @param y the yellow component is float value from 0.0 to 1.0.
// @param k the black component is float value from 0.0 to 1.0.
func (page *Page) SetPenColorCMYK(c, m, y, k float32) *Page {
	page.appendFloat32(c)
	page.appendString(" ")
	page.appendFloat32(m)
	page.appendString(" ")
	page.appendFloat32(y)
	page.appendString(" ")
	page.appendFloat32(k)
	page.appendString(" K\n")
	return page
}

// SetBrushColorCMYK sets the color for brushColor operations using CMYK.
// This is the color used when drawing regular text and filling shapes.
// @param c the cyan component is float value from 0.0 to 1.0.
// @param m the magenta component is float value from 0.0 to 1.0.
// @param y the yellow component is float value from 0.0 to 1.0.
// @param k the black component is float value from 0.0 to 1.0.
func (page *Page) SetBrushColorCMYK(c, m, y, k float32) *Page {
	page.appendFloat32(c)
	page.appendString(" ")
	page.appendFloat32(m)
	page.appendString(" ")
	page.appendFloat32(y)
	page.appendString(" ")
	page.appendFloat32(k)
	page.appendString(" k\n")
	return page
}

// SetDefaultLineWidth sets the line width to the default.
// The default is the finest line width.
func (page *Page) SetDefaultLineWidth() *Page {
	page.appendFloat32(0.0)
	page.appendString(" w\n")
	return page
}

// SetStrokeDashPattern the stroke dash pattern controls the pattern of dashes and gaps used to stroke paths.
// It is specified by a dash array and a dash phase.
// The elements of the dash array are positive numbers that specify the lengths of
// alternating dashes and gaps.
// The dash phase specifies the distance into the dash pattern at which to start the dash.
// The elements of both the dash array and the dash phase are expressed in user space units.
//
// Examples of line dash patterns:
//
//	"[Array] Phase"     Appearance          Description
//	 _______________     _________________   ____________________________________
//
//	 "[] 0"              -----------------   Solid line
//	 "[3] 0"             ---   ---   ---     3 units on, 3 units off, ...
//	 "[2] 1"             -  --  --  --  --   1 on, 2 off, 2 on, 2 off, ...
//	 "[2 1] 0"           -- -- -- -- -- --   2 on, 1 off, 2 on, 1 off, ...
//	 "[3 5] 6"             ---     ---       2 off, 3 on, 5 off, 3 on, 5 off, ...
//	 "[2 3] 11"          -   --   --   --    1 on, 3 off, 2 on, 3 off, 2 on, ...
//
// @param strokeDashPattern the line dash pattern.
func (page *Page) SetStrokeDashPattern(strokeDashPattern string) *Page {
	page.strokeDashPattern = strokeDashPattern
	page.appendString(page.strokeDashPattern)
	page.appendString(" d\n")
	return page
}

// SetDefaultStrokeDashPattern sets the default line dash pattern - solid line.
func (page *Page) SetDefaultStrokeDashPattern() *Page {
	page.strokeDashPattern = "[] 0"
	page.appendString(page.strokeDashPattern)
	page.appendString(" d\n")
	return page
}

// SetPenWidth sets the penColor width that will be used to draw lines and splines on this page.
func (page *Page) SetPenWidth(width float32) *Page {
	page.penWidth = width
	page.appendFloat32(width)
	page.appendString(" w\n")
	return page
}

// GetPenWidth returns the current pen width.
func (page *Page) GetPenWidth() float32 {
	return page.penWidth
}

// SetLineCapStyle sets the current line cap style.
// Supported values: capstyle.Butt, capstyle.Round and capstyle.ProjectingSquare
func (page *Page) SetLineCapStyle(style int) *Page {
	page.lineCapStyle = style
	page.appendInteger(page.lineCapStyle)
	page.appendString(" J\n")
	return page
}

// SetLineJoinStyle sets the line join style.
// Supported values: joinstyle.Miter, joinstyle.Round and joinstyle.Bevel
func (page *Page) SetLineJoinStyle(style int) *Page {
	page.lineJoinStyle = style
	page.appendInteger(page.lineJoinStyle)
	page.appendString(" j\n")
	return page
}

// MoveTo moves the penColor to the point with coordinates (x, y) on the page.
//
// @param x the x coordinate of new penColor position.
// @param y the y coordinate of new penColor position.
func (page *Page) MoveTo(x, y float32) {
	page.appendFloat32(x)
	page.appendString(" ")
	page.appendFloat32(page.height - y)
	page.appendString(" m\n")
}

// LineTo draws a line from the current penColor position to the point with coordinates (x, y),
// using the current penColor width and stroke color.
// Make sure you call strokePath(), closePath() or fillPath() after the last call to this method.
func (page *Page) LineTo(x, y float32) {
	page.appendFloat32(x)
	page.appendString(" ")
	page.appendFloat32(page.height - y)
	page.appendString(" l\n")
}

// StrokePath draws the path using the current penColor color.
func (page *Page) StrokePath() {
	page.appendString("S\n")
}

// ClosePath closes the path and draws it using the current penColor color.
func (page *Page) ClosePath() {
	page.appendString("s\n")
}

// FillPath closes and fills the path with the current brushColor color.
func (page *Page) FillPath() {
	page.appendString("f\n")
}

// DrawRect draws the outline of the specified rectangle on the page.
// The left and right edges of the rectangle are at x and x + w.
// The top and bottom edges are at y and y + h.
// The rectangle is drawn using the current penColor color.
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
// The rectangle is drawn using the current penColor color.
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

// DrawPath draws a path consisting of multiple points using the specified path operator.
// The path can include both straight lines and Bézier curves defined by control points.
//
// path: A slice of Points that defines the path. Must contain at least 2 points.
// pathOperator: The PDF path painting operator to apply (e.g., "S" for stroke, "f" for fill).
//
// The method starts at the first point and processes subsequent points as either
// line segments or curve control points based on their controlPoint field.
func (page *Page) DrawPath(path []*Point, pathOperator string) {
	if len(path) < 2 {
		log.Fatal("The Path object must contain at least 2 points.")
	}
	point := path[0]
	page.MoveTo(point.x, point.y)
	var controlPoint byte = 0
	for i := 1; i < len(path); i++ {
		point = path[i]
		if point.controlPoint != 0 {
			controlPoint = point.controlPoint
			page.appendPoint(point)
		} else {
			if controlPoint != 0 {
				page.appendPoint(point)
				page.appendByte(controlPoint)
				page.appendString("\n")
				controlPoint = 0
			} else {
				page.LineTo(point.x, point.y)
			}
		}
	}
	page.appendString(pathOperator)
	page.appendString("\n")
}

// DrawCircle draws a circle on the page.
//
// The outline of the circle is drawn using the current penColor color.
//
// @param x the x coordinate of the center of the circle to be drawn.
// @param y the y coordinate of the center of the circle to be drawn.
// @param r the radius of the circle to be drawn.
func (page *Page) DrawCircle(x, y, r float32) {
	page.drawEllipse(x, y, r, r, pathoperator.Stroke)
}

// DrawCircleUsingPathOperator draws the specified circle on the page using the path operator.
//
// @param x the x coordinate of the center of the circle to be drawn.
// @param y the y coordinate of the center of the circle to be drawn.
// @param r the radius of the circle to be drawn.
// @param pathOperator the path operator, for example pathoperator.Stroke or pathoperator.Fill.
func (page *Page) DrawCircleUsingPathOperator(x, y, r float32, pathOperator string) {
	page.drawEllipse(x, y, r, r, pathOperator)
}

// DrawEllipse draws an ellipse on the page using the current penColor color.
// @param x the x coordinate of the center of the ellipse to be drawn.
// @param y the y coordinate of the center of the ellipse to be drawn.
// @param r1 the horizontal radius of the ellipse to be drawn.
// @param r2 the vertical radius of the ellipse to be drawn.
func (page *Page) DrawEllipse(x, y, r1, r2 float32) {
	page.drawEllipse(x, y, r1, r2, pathoperator.Stroke)
}

// FillEllipse fills an ellipse on the page using the current penColor color.
// @param x the x coordinate of the center of the ellipse to be drawn.
// @param y the y coordinate of the center of the ellipse to be drawn.
// @param r1 the horizontal radius of the ellipse to be drawn.
// @param r2 the vertical radius of the ellipse to be drawn.
func (page *Page) FillEllipse(x, y, r1, r2 float32) {
	page.drawEllipse(x, y, r1, r2, pathoperator.Fill)
}

// drawEllipse draws an ellipse on the page and fills it using the current brushColor color.
// @param x the x coordinate of the center of the ellipse to be drawn.
// @param y the y coordinate of the center of the ellipse to be drawn.
// @param r1 the horizontal radius of the ellipse to be drawn.
// @param r2 the vertical radius of the ellipse to be drawn.
// @param pathOperator the path operator.
func (page *Page) drawEllipse(x, y, r1, r2 float32, pathOperator string) {
	// The best 4-spline magic number
	var m4 float32 = 0.55228

	// Starting point
	page.MoveTo(x, y-r2)

	page.appendPointXY(x+m4*r1, y-r2)
	page.appendPointXY(x+r1, y-m4*r2)
	page.appendPointXY(x+r1, y)
	page.appendString("c\n")

	page.appendPointXY(x+r1, y+m4*r2)
	page.appendPointXY(x+m4*r1, y+r2)
	page.appendPointXY(x, y+r2)
	page.appendString("c\n")

	page.appendPointXY(x-m4*r1, y+r2)
	page.appendPointXY(x-r1, y+m4*r2)
	page.appendPointXY(x-r1, y)
	page.appendString("c\n")

	page.appendPointXY(x-r1, y-m4*r2)
	page.appendPointXY(x-m4*r1, y-r2)
	page.appendPointXY(x, y-r2)
	page.appendString("c\n")

	page.appendString(pathOperator)
	page.appendString("\n")
}

// DrawPoint draws a point on the page using the current penColor color.
// @param p the point.
func (page *Page) DrawPoint(p *Point) {
	if p.shape != shape.Invisible {
		var list []*Point
		switch p.shape {
		case shape.Circle:
			page.drawEllipse(p.x, p.y, p.r, p.r, p.pathOperator)
		case shape.Diamond:
			list = append(list, NewPoint(p.x, p.y-p.r*1.2))
			list = append(list, NewPoint(p.x+p.r*1.2, p.y))
			list = append(list, NewPoint(p.x, p.y+p.r*1.2))
			list = append(list, NewPoint(p.x-p.r*1.2, p.y))
			page.DrawPath(list, p.pathOperator)
		case shape.Box:
			list = append(list, NewPoint(p.x-p.r*0.886, p.y-p.r*0.886))
			list = append(list, NewPoint(p.x+p.r*0.886, p.y-p.r*0.886))
			list = append(list, NewPoint(p.x+p.r*0.886, p.y+p.r*0.886))
			list = append(list, NewPoint(p.x-p.r*0.886, p.y+p.r*0.886))
			page.DrawPath(list, p.pathOperator)
		case shape.Plus:
			page.DrawLine(p.x-p.r, p.y, p.x+p.r, p.y)
			page.DrawLine(p.x, p.y-p.r, p.x, p.y+p.r)
		case shape.UpArrow:
			list = append(list, NewPoint(p.x, p.y-p.r))
			list = append(list, NewPoint(p.x+p.r, p.y+p.r))
			list = append(list, NewPoint(p.x-p.r, p.y+p.r))
			list = append(list, NewPoint(p.x, p.y-p.r))
			page.DrawPath(list, p.pathOperator)
		case shape.DownArrow:
			list = append(list, NewPoint(p.x-p.r, p.y-p.r))
			list = append(list, NewPoint(p.x+p.r, p.y-p.r))
			list = append(list, NewPoint(p.x, p.y+p.r))
			list = append(list, NewPoint(p.x-p.r, p.y-p.r))
			page.DrawPath(list, p.pathOperator)
		case shape.LeftArrow:
			list = append(list, NewPoint(p.x+p.r, p.y+p.r))
			list = append(list, NewPoint(p.x-p.r, p.y))
			list = append(list, NewPoint(p.x+p.r, p.y-p.r))
			list = append(list, NewPoint(p.x+p.r, p.y+p.r))
			page.DrawPath(list, p.pathOperator)
		case shape.RightArrow:
			list = append(list, NewPoint(p.x-p.r, p.y-p.r))
			list = append(list, NewPoint(p.x+p.r, p.y))
			list = append(list, NewPoint(p.x-p.r, p.y+p.r))
			list = append(list, NewPoint(p.x-p.r, p.y-p.r))
			page.DrawPath(list, p.pathOperator)
		case shape.HDash:
			page.DrawLine(p.x-p.r, p.y, p.x+p.r, p.y)
		case shape.VDash:
			page.DrawLine(p.x, p.y-p.r, p.x, p.y+p.r)
		case shape.XMark:
			page.DrawLine(p.x-p.r, p.y-p.r, p.x+p.r, p.y+p.r)
			page.DrawLine(p.x-p.r, p.y+p.r, p.x+p.r, p.y-p.r)
		case shape.Multiply:
			page.DrawLine(p.x-p.r, p.y-p.r, p.x+p.r, p.y+p.r)
			page.DrawLine(p.x-p.r, p.y+p.r, p.x+p.r, p.y-p.r)
			page.DrawLine(p.x-p.r, p.y, p.x+p.r, p.y)
			page.DrawLine(p.x, p.y-p.r, p.x, p.y+p.r)
		case shape.Star:
			for i := 0; i < 10; i++ {
				theta := float64(i) * 36.0 * (math.Pi / 180.0)
				radius := float64(p.r) * 1.147
				if i%2 != 0 {
					radius = float64(p.r) * 0.38196 * 1.147
				}
				x := float64(p.x) + radius*math.Sin(theta)
				y := float64(p.y) - radius*math.Cos(theta) // minus because y grows down
				list = append(list, NewPoint(float32(x), float32(y)))
			}
			page.DrawPath(list, p.pathOperator)
		}
	}
}

// SetTextRenderingMode sets the text rendering mode for the page.
// The mode determines how text is rendered, and must be an integer value between 0 and 7 (inclusive).
//
// Parameters:
//
//	mode (int): the text rendering mode to set (0 to 7)
//
// Example usage:
//
//	page.SetTextRenderingMode(3)
func (page *Page) SetTextRenderingMode(mode int) *Page {
	if mode >= 0 && mode <= 7 {
		page.renderingMode = mode
	} else {
		log.Fatal("Invalid text rendering mode: " + fmt.Sprint(mode))
	}
	return page
}

// SetTextDirection sets the text direction for rendering text on the page.
// The direction is specified as an angle in degrees (0-360).
// If the degree value is greater than 360, it will be normalized to the range [0, 360).
//
// Parameters:
//
//	degrees (int): the angle (in degrees) to set the text direction
//
// Example usage:
//
//	page.SetTextDirection(90)
func (page *Page) SetTextDirection(degrees int) *Page {
	if degrees > 360 {
		degrees %= 360
	}
	switch degrees {
	case 0:
		page.tmx = [4]float32{1.0, 0.0, 0.0, 1.0}
	case 90:
		page.tmx = [4]float32{0.0, 1.0, -1.0, 0.0}
	case 180:
		page.tmx = [4]float32{-1.0, 0.0, 0.0, -1.0}
	case 270:
		page.tmx = [4]float32{0.0, -1.0, 1.0, 0.0}
	case 360:
		page.tmx = [4]float32{1.0, 0.0, 0.0, 1.0}
	default:
		sinOfAngle := float32(math.Sin(float64(degrees) * (math.Pi / 180)))
		cosOfAngle := float32(math.Cos(float64(degrees) * (math.Pi / 180)))
		page.tmx = [4]float32{cosOfAngle, sinOfAngle, -sinOfAngle, cosOfAngle}
	}
	page.tm0 = fastfloat.ToByteArray(page.tmx[0])
	page.tm1 = fastfloat.ToByteArray(page.tmx[1])
	page.tm2 = fastfloat.ToByteArray(page.tmx[2])
	page.tm3 = fastfloat.ToByteArray(page.tmx[3])
	return page
}

// CurveTo adds a cubic Bézier curve command to the page’s content stream.
// The coordinates (x1,y1) and (x2,y2) are the two control points,
// and (x3,y3) is the end point of the curve.
func (page *Page) CurveTo(x1, y1, x2, y2, x3, y3 float32) {
	page.appendFloat32(x1)
	page.appendString(" ")
	page.appendFloat32(page.height - y1)
	page.appendString(" ")
	page.appendFloat32(x2)
	page.appendString(" ")
	page.appendFloat32(page.height - y2)
	page.appendString(" ")
	page.appendFloat32(x3)
	page.appendString(" ")
	page.appendFloat32(page.height - y3)
	page.appendString(" c\n")
}

// DrawCircularArc adds a circular arc to the current path.
// It returns the control points and the end point of the last curve segment.
func (page *Page) DrawCircularArc(
	x, y, r, startAngle, sweepDegrees float32) []float32 {
	return page.DrawArc(x, y, r, r, startAngle, sweepDegrees)
}

// DrawArc adds an elliptical arc to the current path.
// It returns the control points and the end point of the last curve segment.
func (page *Page) DrawArc(
	x, y, rx, ry, startAngle, sweepDegrees float32) []float32 {
	var x1, y1, x2, y2, x3, y3 float32

	numSegments := int(math.Ceil(math.Abs(float64(sweepDegrees)) / 90.0))
	angleRad := float64(startAngle) * math.Pi / 180.0
	deltaPerSeg := float64(sweepDegrees/float32(numSegments)) * math.Pi / 180.0

	for i := 0; i < numSegments; i++ {
		segStart := angleRad
		segEnd := angleRad + deltaPerSeg
		deltaRad := segEnd - segStart // guaranteed ≤ ±π/2

		// Calculate safe κ
		k := float32(4.0 / 3.0 * math.Tan(deltaRad/4.0))

		cosStart := float32(math.Cos(segStart))
		sinStart := float32(math.Sin(segStart))
		cosEnd := float32(math.Cos(segEnd))
		sinEnd := float32(math.Sin(segEnd))

		// End points
		x0 := x + rx*cosStart
		y0 := y + ry*sinStart
		x3 = x + rx*cosEnd
		y3 = y + ry*sinEnd

		// Control points
		x1 = x0 - (k * rx * sinStart)
		y1 = y0 + (k * ry * cosStart)
		x2 = x3 + (k * rx * sinEnd)
		y2 = y3 - (k * ry * cosEnd)

		if i == 0 {
			page.MoveTo(x0, y0)
		}
		page.CurveTo(x1, y1, x2, y2, x3, y3)

		angleRad = segEnd
	}

	return []float32{x1, y1, x2, y2, x3, y3}
}

// BezierCurveTo draw a Bézier curve starting from the current point.
// Please note: You must call the fillPath, closePath or strokePath method after the last bezierCurveTo call.
//
// Author: Pieter Libin, pieter@emweb.be
//
// @param p1 first control point
// @param p2 second control point
// @param p3 end point
func (page *Page) BezierCurveTo(p1, p2, p3 *Point) {
	page.appendPoint(p1)
	page.appendPoint(p2)
	page.appendPoint(p3)
	page.appendString("c\n")
}

// setTextFont sets the text font.
func (page *Page) setTextFont(font *Font, fontSize float32) *Page {
	if font.fontID != "" {
		page.appendByte('/')
		page.appendString(font.fontID)
	} else {
		page.appendString("/F")
		page.appendInteger(font.objNumber)
	}
	page.appendByte(token.Space)
	page.appendFloat32(fontSize)
	page.appendString(" Tf\n")
	page.textFontSize = fontSize
	return page
}

// DrawRectRoundCorners draws rectangle with rounded corners.
// Code provided by:
// Dominique Andre Gunia <contact@dgunia.de>
func (page *Page) DrawRectRoundCorners(x, y, w, h, r1, r2 float32, operation string) {
	// The best 4-spline magic number
	var m4 float32 = 0.55228

	list := []*Point{}

	// Starting point
	list = append(list, NewPoint(x+w-r1, y))
	list = append(list, NewControlPointC(x+w-r1+m4*r1, y))
	list = append(list, NewControlPointC(x+w, y+r2-m4*r2))
	list = append(list, NewPoint(x+w, y+r2))

	list = append(list, NewPoint(x+w, y+h-r2))
	list = append(list, NewControlPointC(x+w, y+h-r2+m4*r2))
	list = append(list, NewControlPointC(x+w-m4*r1, y+h))
	list = append(list, NewPoint(x+w-r1, y+h))

	list = append(list, NewPoint(x+r1, y+h))
	list = append(list, NewControlPointC(x+r1-m4*r1, y+h))
	list = append(list, NewControlPointC(x, y+h-m4*r2))
	list = append(list, NewPoint(x, y+h-r2))

	list = append(list, NewPoint(x, y+r2))
	list = append(list, NewControlPointC(x, y+r2-m4*r2))
	list = append(list, NewControlPointC(x+m4*r1, y))
	list = append(list, NewPoint(x+r1, y))
	list = append(list, NewPoint(x+w-r1, y))

	page.DrawPath(list, operation)
}

// ClipPath clips the path.
func (page *Page) ClipPath() {
	page.appendString("W\n")
	page.appendString("n\n") // Close the path without painting it.
}

// ClipRect sets the clipping path to the specified rectangle.
func (page *Page) ClipRect(x, y, w, h float32) {
	page.MoveTo(x, y)
	page.LineTo(x+w, y)
	page.LineTo(x+w, y+h)
	page.LineTo(x, y+h)
	page.ClipPath()
}

// SetCropBox sets the page CropBox.
// See page 77 of the PDF32000_2008.pdf specification.
// @param upperLeftX the top left X coordinate of the CropBox.
// @param upperLeftY the top left Y coordinate of the CropBox.
// @param lowerRightX the bottom right X coordinate of the CropBox.
// @param lowerRightY the bottom right Y coordinate of the CropBox.
func (page *Page) SetCropBox(upperLeftX, upperLeftY, lowerRightX, lowerRightY float32) *Page {
	page.cropBox = []float32{upperLeftX, upperLeftY, lowerRightX, lowerRightY}
	return page
}

// SetBleedBox sets the page BleedBox.
// See page 77 of the PDF32000_2008.pdf specification.
// @param upperLeftX the top left X coordinate of the BleedBox.
// @param upperLeftY the top left Y coordinate of the BleedBox.
// @param lowerRightX the bottom right X coordinate of the BleedBox.
// @param lowerRightY the bottom right Y coordinate of the BleedBox.
func (page *Page) SetBleedBox(upperLeftX, upperLeftY, lowerRightX, lowerRightY float32) *Page {
	page.bleedBox = []float32{upperLeftX, upperLeftY, lowerRightX, lowerRightY}
	return page
}

// SetTrimBox sets the page TrimBox.
// See page 77 of the PDF32000_2008.pdf specification.
// @param upperLeftX the top left X coordinate of the TrimBox.
// @param upperLeftY the top left Y coordinate of the TrimBox.
// @param lowerRightX the bottom right X coordinate of the TrimBox.
// @param lowerRightY the bottom right Y coordinate of the TrimBox.
func (page *Page) SetTrimBox(upperLeftX, upperLeftY, lowerRightX, lowerRightY float32) *Page {
	page.trimBox = []float32{upperLeftX, upperLeftY, lowerRightX, lowerRightY}
	return page
}

// SetArtBox sets the page ArtBox.
// See page 77 of the PDF32000_2008.pdf specification.
// @param upperLeftX the top left X coordinate of the ArtBox.
// @param upperLeftY the top left Y coordinate of the ArtBox.
// @param lowerRightX the bottom right X coordinate of the ArtBox.
// @param lowerRightY the bottom right Y coordinate of the ArtBox.
func (page *Page) SetArtBox(upperLeftX, upperLeftY, lowerRightX, lowerRightY float32) *Page {
	page.artBox = []float32{upperLeftX, upperLeftY, lowerRightX, lowerRightY}
	return page
}

func (page *Page) appendPointXY(x, y float32) {
	page.appendFloat32(x)
	page.appendString(" ")
	page.appendFloat32(page.height - y)
	page.appendString(" ")
}

func (page *Page) appendPoint(point *Point) {
	page.appendFloat32(point.x)
	page.appendString(" ")
	page.appendFloat32(page.height - point.y)
	page.appendString(" ")
}

func (page *Page) drawWord(font *Font, buf *strings.Builder, brush [3]float32, colors map[string]int32) {
	if buf.Len() > 0 {
		if brushColor, ok := colors[buf.String()]; ok {
			page.SetBrushColor(brushColor)
		} else {
			page.SetBrushColorRGB(brush)
		}

		if font.isCoreFont {
			page.appendString("[<")
			page.drawASCIIString(font, buf.String())
			page.appendString(">] TJ\n")
		} else {
			page.appendString("<")
			page.drawUnicodeString(font, buf.String())
			page.appendString("> Tj\n")
		}

		buf.Reset()
	}
}

func (page *Page) drawColoredString(font *Font, str string, brush [3]float32, colors map[string]int32) {
	var buf1 strings.Builder
	var buf2 strings.Builder
	for _, ch := range str {
		if unicode.IsLetter(ch) || unicode.IsDigit(ch) {
			page.drawWord(font, &buf2, brush, colors)
			buf1.WriteRune(ch)
		} else {
			page.drawWord(font, &buf1, brush, colors)
			buf2.WriteRune(ch)
		}
	}
	page.drawWord(font, &buf1, brush, colors)
	page.drawWord(font, &buf2, brush, colors)
}

func (page *Page) setStructElementsPageObjNumber(pageObjNumber int) {
	for _, element := range page.structures {
		element.pageObjNumber = pageObjNumber
	}
}

// AddBMC adds BMC to the page.
func (page *Page) AddBMC(structure, language, actualText, altDescription string) {
	if page.pdf.compliance == compliance.PDF_UA_1 {
		element := newStructElem()
		element.structure = structure
		element.mcid = page.mcid
		element.language = language
		element.actualText = actualText
		element.altDescription = altDescription
		page.pdf.structElements = append(page.pdf.structElements, element)
		page.structures = append(page.structures, element)

		page.appendString("/")
		page.appendString(structure)
		page.appendString(" <</MCID ")
		page.appendInteger(page.mcid)
		page.mcid++
		page.appendString(">>\n")
		page.appendString("BDC\n")
	}
}

// AddArtifactBMC begins marked content for an artifact when the document is PDF/UA compliant.
func (page *Page) AddArtifactBMC() {
	if page.pdf.compliance == compliance.PDF_UA_1 {
		page.appendString("/Artifact BMC\n")
	}
}

// AddEMC adds EMC to the page.
func (page *Page) AddEMC() {
	if page.pdf.compliance == compliance.PDF_UA_1 {
		page.appendString("EMC\n")
	}
}

// addAnnotation adds annotation to the page.
func (page *Page) addAnnotation(annotation *Annotation) {
	annotation.setDescriptionFallback()
	annotation.y1 = page.height - annotation.y1
	annotation.y2 = page.height - annotation.y2
	page.annots = append(page.annots, annotation)
	if page.pdf.compliance == compliance.PDF_UA_1 {
		element := newStructElem()
		element.structure = structtype.Link
		element.language = annotation.language
		element.actualText = annotation.actualText
		element.altDescription = annotation.altDescription
		element.annotation = annotation
		page.pdf.structElements = append(page.pdf.structElements, element)
		page.structures = append(page.structures, element)
	}
}

func (page *Page) beginTransform(x, y, xScale, yScale float32) {
	page.SaveGraphicsState()

	page.appendFloat32(xScale)
	page.appendString(" 0 0 ")
	page.appendFloat32(yScale)
	page.appendString(" ")
	page.appendFloat32(x)
	page.appendString(" ")
	page.appendFloat32(y)
	page.appendString(" cm\n")

	page.appendFloat32(xScale)
	page.appendString(" 0 0 ")
	page.appendFloat32(yScale)
	page.appendString(" ")
	page.appendFloat32(x)
	page.appendString(" ")
	page.appendFloat32(y)
	page.appendString(" Tm\n")
}

func (page *Page) endTransform() {
	page.RestoreGraphicsState()
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
	page.appendByteArray(content)
	page.endTransform()
}

// DrawStringUsingSpacing draws the characters of the string dx apart, the
// first one at (x, y).
func (page *Page) DrawStringUsingSpacing(font *Font, fontSize float32, text string, x, y, dx float32) {
	x1 := x
	for _, ch := range text {
		page.DrawStringUsingFontSize(font, fontSize, string(ch), x1, y)
		x1 += dx
	}
}

// AddWatermark add watermark to the page.
func (page *Page) AddWatermark(font *Font, text string) {
	hypotenuse := float32(math.Sqrt(
		float64(page.height*page.height + page.width*page.width)))
	stringWidth := font.StringWidth(font.size, text)
	offset := (hypotenuse - stringWidth) / 2.0
	angle := math.Atan(float64(page.height / page.width))
	watermark := NewTextLine(font, "")
	watermark.SetTextColor(color.LightGray)
	watermark.SetText(text)
	watermark.SetLocation(
		offset*float32(math.Cos(angle)),
		page.height-offset*float32(math.Sin(angle)))
	watermark.SetTextDirection((int)(angle * (180.0 / math.Pi)))
	watermark.DrawOn(page)
}

// GetContent returns the content stream of this page.
func (page *Page) GetContent() []byte {
	return page.buf
}

// RotateBy sets the rotation of this page. Only 0, 90, 180 and 270 are accepted; other values are ignored.
func (page *Page) RotateBy(rotateDegrees float64) {
	if rotateDegrees == 0 || rotateDegrees == 90 || rotateDegrees == 180 || rotateDegrees == 270 {
		page.rotateDegrees = float32(rotateDegrees)
	}
}

// InvertYAxis inverts the Y axis.
func (page *Page) InvertYAxis() {
	page.appendString("1 0 0 -1 0 ")
	page.appendFloat32(page.height)
	page.appendString(" cm\n")
}

// Transform use save before, restore afterwards!
// 9 value array like generated by androids Matrix.getValues()
func (page *Page) Transform(values []float32) {
	scalex := values[MScaleX]
	scaley := values[MScaleY]
	transx := values[MTransX]
	transy := values[MTransY]

	page.appendFloat32(scalex)
	page.appendString(" ")
	page.appendFloat32(values[MSkewX])
	page.appendString(" ")
	page.appendFloat32(values[MSkewY])
	page.appendString(" ")
	page.appendFloat32(scaley)
	page.appendString(" ")

	if math.Asin(float64(values[MSkewY])) != 0.0 {
		transx -= values[MSkewY] * page.height / scaley
	}

	page.appendFloat32(transx)
	page.appendString(" ")
	page.appendFloat32(-transy)
	page.appendString(" cm\n")

	page.height = page.height / scaley
}

// AddHeader adds header to this page.
func (page *Page) AddHeader(textLine *TextLine) [2]float32 {
	return page.AddHeaderOffsetBy(textLine, 1.5*textLine.font.ascent)
}

// AddHeaderOffsetBy adds header to this page offset by the specified value.
func (page *Page) AddHeaderOffsetBy(textLine *TextLine, offset float32) [2]float32 {
	textLine.SetLocation((page.GetWidth()-textLine.GetWidth())/2, offset)
	xy := textLine.DrawOn(page)
	xy[1] += textLine.font.descent
	return xy
}

// AddFooter adds footer to this page.
func (page *Page) AddFooter(textLine *TextLine) [2]float32 {
	return page.AddFooterOffsetBy(textLine, textLine.font.ascent)
}

// AddFooterOffsetBy adds footer to this page offset by the specified value.
func (page *Page) AddFooterOffsetBy(textLine *TextLine, offset float32) [2]float32 {
	textLine.SetLocation((page.GetWidth()-textLine.GetWidth())/2, page.GetHeight()-offset)
	return textLine.DrawOn(page)
}

// beginText begins text block.
func (page *Page) beginText() {
	page.appendString("BT\n")
}

// endText ends text block.
func (page *Page) endText() {
	page.appendString("ET\n")
}

// setTextLocation moves the text position to the specified location.
func (page *Page) setTextLocation(x, y float32) *Page {
	page.appendFloat32(x)
	page.appendByte(token.Space)
	page.appendFloat32(page.height - y)
	page.appendString(" Td\n")
	return page
}

// setTextLeading sets the distance between lines of text.
func (page *Page) setTextLeading(leading float32) *Page {
	page.appendFloat32(leading)
	page.appendString(" TL\n")
	return page
}

// nextLine moves the text position to the start of the next line.
func (page *Page) nextLine() {
	page.appendString("T*\n")
}

// setTextScaling sets the horizontal scaling of the text in percent.
func (page *Page) setTextScaling(scaling float32) *Page {
	page.appendFloat32(scaling)
	page.appendString(" Tz\n")
	return page
}

// setTextRise moves the text baseline up or down by the specified amount.
func (page *Page) setTextRise(rise float32) *Page {
	page.appendFloat32(rise)
	page.appendString(" Ts\n")
	page.textRise = rise
	return page
}

// drawTextLine draws a string at the specified location using the current font size.
func (page *Page) drawTextLine(font *Font, str string, x float32, y float32) {
	page.beginText()
	page.setTextLocation(x, y)
	page.setTextFont(font, font.size)
	if font.isCoreFont {
		page.appendString("[<")
		page.drawASCIIString(font, str)
		page.appendString(">] TJ\n")
	} else {
		page.appendString("<")
		page.drawUnicodeString(font, str)
		page.appendString("> Tj\n")
	}
	page.endText()
}

func (page *Page) appendInteger(value int) {
	page.buf = append(page.buf, []byte(strconv.Itoa(value))...)
}

func (page *Page) appendFloat32(value float32) {
	page.buf = append(page.buf, fastfloat.ToByteArray(value)...)
}

func (page *Page) appendString(s1 string) {
	page.buf = append(page.buf, s1...)
}

func (page *Page) appendByte(b byte) {
	page.buf = append(page.buf, b)
}

func (page *Page) appendByteArray(a []byte) {
	page.buf = append(page.buf, a...)
}

// scaleAndRotate scales content to w by h, rotates it around its center
// and places its top left corner at x, y.
func (page *Page) scaleAndRotate(x, y, w, h, degrees float32) {
	// PDF transformations apply LAST-TO-FIRST (like a stack: last command = first applied)

	// [FINAL POSITIONING - Applied First]
	// Moves rotated/scaled image to target (x,y) on page
	page.appendString("1 0 0 1 ")
	page.appendFloat32(x + w/2)
	page.appendString(" ")
	page.appendFloat32((page.height - y) - h/2)
	page.appendString(" cm\n")

	// [ROTATION - Applied Second]
	// Rotates around current origin (0,0) by 'degrees'
	radians := degrees * (math.Pi / 180)
	cos := float32(math.Cos(float64(radians)))
	sin := float32(math.Sin(float64(radians)))
	page.appendByteArray(fastfloat.ToByteArray(cos))
	page.appendString(" ")
	page.appendByteArray(fastfloat.ToByteArray(sin))
	page.appendString(" ")
	page.appendByteArray(fastfloat.ToByteArray(-sin))
	page.appendString(" ")
	page.appendByteArray(fastfloat.ToByteArray(cos))
	page.appendString(" 0 0 cm\n")

	// [ORIGIN SETUP - Applied Last]
	// Centers image at (0,0) and sets scale
	page.appendFloat32(w)
	page.appendString(" 0 0 ")
	page.appendFloat32(h)
	page.appendString(" ")
	page.appendFloat32(-w / 2)
	page.appendString(" ")
	page.appendFloat32(-h / 2)
	page.appendString(" cm\n")
}

// rotateAroundCenter rotates the coordinate system around the specified center.
func (page *Page) rotateAroundCenter(centerX, centerY, degrees float32) {
	page.appendString("1 0 0 1 ")
	page.appendFloat32(centerX)
	page.appendString(" ")
	page.appendFloat32(centerY)
	page.appendString(" cm\n")

	radians := degrees * (math.Pi / 180)
	cos := float32(math.Cos(float64(radians)))
	sin := float32(math.Sin(float64(radians)))
	page.appendByteArray(fastfloat.ToByteArray(cos))
	page.appendString(" ")
	page.appendByteArray(fastfloat.ToByteArray(sin))
	page.appendString(" ")
	page.appendByteArray(fastfloat.ToByteArray(-sin))
	page.appendString(" ")
	page.appendByteArray(fastfloat.ToByteArray(cos))
	page.appendString(" 0 0 cm\n")

	page.appendString("1 0 0 1 ")
	page.appendFloat32(-centerX)
	page.appendString(" ")
	page.appendFloat32(-centerY)
	page.appendString(" cm\n")
}

// toUTF16Hex returns the string as a PDF text string, in UTF-16BE with a byte
// order mark, written in hexadecimal.
func toUTF16Hex(str string) string {
	var sb strings.Builder
	sb.WriteString("FEFF")
	for _, unit := range utf16.Encode([]rune(str)) {
		fmt.Fprintf(&sb, "%04X", unit)
	}
	return sb.String()
}

func (page *Page) drawTextBlock(
	font *Font,
	fontSize float32,
	textLines []*TextLine,
	x float32,
	y float32,
	leading float32,
	textColor [3]float32,
	highlightColors map[string]int32,
	language string) {

	if len(textLines) == 0 {
		return
	}

	// A span gives the language of the text, for screen readers and text extraction.
	hasLanguage := language != ""
	if hasLanguage {
		page.appendString("/Span <</Lang <")
		page.appendString(toUTF16Hex(language))
		page.appendString(">>> BDC\n")
	}
	page.appendString("BT\n")
	page.SetBrushColorRGB(textColor)
	page.setTextFont(font, fontSize)
	yText := y
	for _, textLine := range textLines {
		page.appendString("1 0 0 1 ")
		page.appendFloat32(x + textLine.xOffset)
		page.appendString(" ")
		page.appendFloat32(page.height - (yText + font.ascent))
		page.appendString(" Tm\n")
		if highlightColors == nil {
			if font.isCoreFont {
				page.appendString("[<")
				page.drawASCIIString(font, textLine.text)
				page.appendString(">] TJ\n")
			} else {
				page.appendString("<")
				page.drawUnicodeString(font, textLine.text)
				page.appendString("> Tj\n")
			}
		} else {
			page.drawColoredString(font, textLine.text, textColor, highlightColors)
		}
		yText += leading
	}
	page.appendString("ET\n")
	if hasLanguage {
		page.appendString("EMC\n")
	}

	yLine := y + font.GetBodyHeightAt(fontSize)
	for _, textLine := range textLines {
		if textLine.underline {
			page.MoveTo(x+textLine.xOffset, yLine)
			page.LineTo(x+textLine.xOffset+font.StringWidth(fontSize, textLine.text), yLine)
			page.StrokePath()
		}
		yLine += leading
	}
}
