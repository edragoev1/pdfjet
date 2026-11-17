// complextext.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"errors"
	"math"
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// Shaper is a text engine that shapes text for ComplexText: it wraps the
// text to a width and finds, for each line, the glyphs of the fonts and
// their places. PDFjet does not shape scripts such as Hindi and Khmer itself;
// a shaper does, and ComplexText draws what it found. Go has no text engine
// of its own that gives glyphs: a program gives one, such as a shaper that
// calls go-text/typesetting, HarfBuzz in Go.
type Shaper interface {
	// Shape wraps the text to the width and shapes each line.
	//   - text: the text.
	//   - language: the language of the text, such as "hi" or "km", or "".
	//   - fontFiles: the bytes of the font files: the first for the text, the
	//     others, in order, for the characters it does not have.
	//   - fontSize: the font size, in points.
	//   - width: the width of the lines, in points.
	// Returns the lines, in order.
	Shape(text, language string, fontFiles [][]byte, fontSize, width float32) ([]ShapedLine, error)
}

// ShapedLine is a line of text a Shaper shaped: the part of the text it
// holds, and its glyphs, each with its font and its place.
type ShapedLine struct {
	// Start is the index in the text, in bytes, of the line's first character.
	Start int
	// End is the index in the text, in bytes, after the line's last character.
	End int
	// Fonts is, for each glyph, the index of its font among the font files
	// the shaper was given, or -1 for a glyph of none of them, which is not
	// drawn.
	Fonts []int
	// Glyphs are the glyph numbers, in their fonts.
	Glyphs []int
	// Positions are the x and the y of each glyph, in points, from the left
	// end of the line at its baseline, y growing down: x0, y0, x1, y1 and so
	// on.
	Positions []float32
	// Width is the width of the line, in points, without the white space at
	// its end: what is centered, aligned to the right or justified.
	Width float32
}

// ComplexText is text in a script PDFjet does not shape itself, such as
// Hindi or Khmer, wrapped to a width and drawn as simply as a TextBlock. A
// text engine shapes it -- conjuncts, vowel signs, stacked consonants -- and
// PDFjet draws the glyphs it found with Page.DrawGlyphs: as outlines read
// from the font file, sharp on screen and in print, each glyph drawn once and
// reused.
//
//	hindi := pdfjet.NewFontFromFile(pdf, IBMPlexSansDevanagari.Regular)
//	latin := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular) // The English words
//	hindi.SetSize(11)
//	corner := pdfjet.NewComplexText(hindi, text).
//		SetFallbackFont(latin).
//		SetLanguage("hi").
//		SetShaper(shaper).
//		SetWidth(512).
//		SetLocation(50, 100).
//		DrawOn(page)
//
// Paragraphs, given as a list, flow from page to page, as a TextFrame's:
// with a height, DrawOn draws the lines that fit and keeps the rest, a
// paragraph cut where the height ends, for the next DrawOn, on the next page.
//
//	text := pdfjet.NewComplexTextFromList(hindi, paragraphs).
//		SetFallbackFont(latin).
//		SetLanguage("hi").
//		SetShaper(shaper).
//		SetWidth(512).
//		SetHeight(692)
//	for more, _ := text.HasMoreText(); more; more, _ = text.HasMoreText() {
//		text.SetLocation(50, 50).DrawOn(pdfjet.NewPage(pdf, letter.Portrait()))
//	}
//
// Go has no text engine of its own that gives glyphs: SetShaper gives one,
// such as a shaper that calls go-text/typesetting. Left-to-right scripts:
// Arabic and Hebrew are TextLine's and TextBlock's.
//
// In a tagged document, PDF/UA or a PDF/A of level A, each paragraph is a
// paragraph in its language, each line with its text, which screen readers
// read. The text cannot be selected, copied or searched, as it is drawn as
// outlines; a document that needs its exact text along can carry it with
// PDF.AttachText.
//
// The fonts must be loaded from their font files, by path, as the fonts
// PDFjet carries are, or from their bytes, as the engine reads the same file:
// the glyph numbers it finds are those of that file.
type ComplexText struct {
	font          *Font
	fallbackFont  *Font
	paragraphs    []string
	language      string
	x, y          float32
	width         float32
	height        float32 // 0: all of the text
	paragraphGap  float32 // Less than 0: half the font's size
	textColor     int32
	textAlignment alignment.Alignment
	lineSpacing   float32
	structureType structelem.StructElem
	shaper        Shaper

	// The paragraph drawn next, and its line drawn next
	paragraph int
	line      int

	// The lines of each paragraph, shaped the first time they are needed,
	// until a font, the language or the width changes; and the font's size
	// they were shaped at
	shapedLines [][]ShapedLine
	shapedSize  float32
}

// NewComplexText returns text to draw in the font, at its size, loaded from
// its font file.
func NewComplexText(font *Font, text string) *ComplexText {
	return NewComplexTextFromList(font, []string{text})
}

// NewComplexTextFromList returns paragraphs to draw in the font, at its
// size, loaded from its font file, one after the other.
func NewComplexTextFromList(font *Font, paragraphs []string) *ComplexText {
	return &ComplexText{
		font:          font,
		paragraphs:    append([]string(nil), paragraphs...),
		width:         300,
		paragraphGap:  -1,
		textColor:     color.Black,
		textAlignment: alignment.Left,
		lineSpacing:   1,
		structureType: structelem.P,
	}
}

// SetFallbackFont sets the font of the characters the font does not have,
// such as English words in Hindi text, drawn at the font's size.
func (complexText *ComplexText) SetFallbackFont(font *Font) *ComplexText {
	complexText.fallbackFont = font
	complexText.shapedLines = nil
	return complexText
}

// SetLanguage sets the language of the text, such as "hi" or "km": the
// engine shapes by it, and a tagged document gives it to screen readers.
func (complexText *ComplexText) SetLanguage(language string) *ComplexText {
	complexText.language = language
	complexText.shapedLines = nil
	return complexText
}

// SetLocation sets the location of the top left corner of the text, in
// points. It returns the text as a Drawable, so in a chain of setter calls
// SetLocation goes last, right before DrawOn.
func (complexText *ComplexText) SetLocation(x, y float32) Drawable {
	complexText.x = x
	complexText.y = y
	return complexText
}

// SetWidth sets the width the text is wrapped to; 300 points by default.
func (complexText *ComplexText) SetWidth(width float32) *ComplexText {
	complexText.width = width
	complexText.shapedLines = nil
	return complexText
}

// SetHeight sets the height the text is drawn in: DrawOn draws the lines
// that fit and keeps the rest for the next DrawOn. With a height of 0, the
// default, DrawOn draws all of the text.
func (complexText *ComplexText) SetHeight(height float32) *ComplexText {
	complexText.height = height
	return complexText
}

// SetParagraphGap sets the space between paragraphs, from the bottom of a
// paragraph's last line to the top of the next one's first; half the font's
// size by default. A negative gap is taken as 0.
func (complexText *ComplexText) SetParagraphGap(paragraphGap float32) *ComplexText {
	complexText.paragraphGap = max(0, paragraphGap)
	return complexText
}

// SetTextColor sets the color of the text, as 0xRRGGBB.
func (complexText *ComplexText) SetTextColor(c int32) *ComplexText {
	complexText.textColor = c
	return complexText
}

// SetTextAlignment sets the alignment of the lines: alignment.Left, the
// default, alignment.Center, alignment.Right or alignment.Justify, which
// spreads the words of each line but the last of a paragraph to the width.
func (complexText *ComplexText) SetTextAlignment(textAlignment alignment.Alignment) *ComplexText {
	complexText.textAlignment = textAlignment
	return complexText
}

// SetLineSpacing sets the line spacing as a multiple of the font's body
// height, as TextBlock's: 1.0 by default.
func (complexText *ComplexText) SetLineSpacing(lineSpacing float32) *ComplexText {
	complexText.lineSpacing = lineSpacing
	return complexText
}

// SetStructureType sets the structure type of each paragraph in a tagged
// document; P, a paragraph, by default.
func (complexText *ComplexText) SetStructureType(structureType structelem.StructElem) *ComplexText {
	complexText.structureType = structureType
	return complexText
}

// SetShaper sets the text engine that shapes the text.
func (complexText *ComplexText) SetShaper(shaper Shaper) *ComplexText {
	complexText.shaper = shaper
	complexText.shapedLines = nil
	return complexText
}

// HasMoreText returns true if some of the text has not been drawn yet.
func (complexText *ComplexText) HasMoreText() (bool, error) {
	for p := complexText.paragraph; p < len(complexText.paragraphs); p++ {
		lines, err := complexText.shaped(p)
		if err != nil {
			return false, err
		}
		done := 0
		if p == complexText.paragraph {
			done = complexText.line
		}
		if len(lines) > done {
			return true, nil
		}
	}
	return false, nil
}

// GetHeight returns the height of the text not drawn yet, wrapped to the
// width, whatever the height set, in points: it shapes the text, if it is not
// shaped yet.
func (complexText *ComplexText) GetHeight() (float32, error) {
	var total float32
	first := true
	for p := complexText.paragraph; p < len(complexText.paragraphs); p++ {
		lines, err := complexText.shaped(p)
		if err != nil {
			return 0, err
		}
		count := len(lines)
		if p == complexText.paragraph {
			count -= complexText.line
		}
		if count > 0 {
			if !first {
				total += complexText.gap()
			}
			total += float32(float32(count) * complexText.lineHeight())
			first = false
		}
	}
	return total, nil
}

// DrawOn draws the text not drawn yet, wrapped to the width, from its
// location: all of it, or with a height, the lines that fit, the rest kept
// for the next DrawOn. It returns the x and the y of the bottom right corner
// of the text drawn. Text that cannot be shaped fails the document, as
// Complete says.
func (complexText *ComplexText) DrawOn(page *Page) [2]float32 {
	x, y, width := complexText.x, complexText.y, complexText.width
	fonts := complexText.fonts()
	font := complexText.font
	size := font.GetSize()
	ascent := font.GetAscent(size)
	lineHeight := complexText.lineHeight()
	bottom := float32(math.MaxFloat32)
	if complexText.height > 0 {
		bottom = y + complexText.height
	}
	// The space of each font, for justified lines
	spaces := make([]int, len(fonts))
	for i, f := range fonts {
		if f.unicodeToGID != nil {
			spaces[i] = f.unicodeToGID.get(' ')
		}
	}

	page.SetBrushColor(complexText.textColor)
	top := y
	drawn := false // A line drawn by this DrawOn
	for complexText.paragraph < len(complexText.paragraphs) {
		lines, err := complexText.shaped(complexText.paragraph)
		if err != nil {
			page.pdf.fail(err.Error())
			return [2]float32{x + width, top}
		}
		if complexText.line >= len(lines) {
			complexText.paragraph++
			complexText.line = 0
			continue
		}
		var gap float32
		if drawn && complexText.line == 0 {
			gap = complexText.gap()
		}
		// At least one line each DrawOn, so that text in a height too small
		// for a line still ends
		if drawn && top+gap+lineHeight > bottom {
			break
		}
		top += gap
		text := complexText.paragraphs[complexText.paragraph]
		own := page.pdf.isTagged() && !page.insideTaggedContent()
		if own {
			page.AddBDC(complexText.structureType, complexText.language, "", "")
		}
		for complexText.line < len(lines) && (!drawn || top+lineHeight <= bottom) {
			last := complexText.line == len(lines)-1
			if !complexText.drawLine(page, lines[complexText.line], text, top+ascent, last, fonts, spaces, size) {
				return [2]float32{x + width, top}
			}
			top += lineHeight
			complexText.line++
			drawn = true
		}
		if own {
			page.AddEMC()
		}
		if complexText.line < len(lines) {
			break // The height is full
		}
		complexText.paragraph++
		complexText.line = 0
	}
	return [2]float32{x + width, top}
}

// drawLine draws the line at its baseline: the glyphs of each font, one run
// of them at a time, tagged with the line's text; false if the document
// failed.
func (complexText *ComplexText) drawLine(page *Page, line ShapedLine, text string, baseline float32,
	last bool, fonts []*Font, spaces []int, size float32) bool {
	lineText := text[line.Start:line.End]
	if strings.TrimSpace(lineText) == "" {
		return true
	}
	shifts := complexText.shifts(line, last, spaces)
	page.beginActualText(lineText, complexText.language)
	for from := 0; from < len(line.Glyphs); {
		to := from
		for to < len(line.Glyphs) && line.Fonts[to] == line.Fonts[from] {
			to++
		}
		index := line.Fonts[from]
		if index >= 0 && index < len(fonts) {
			run := make([]int, to-from)
			places := make([]float32, 2*(to-from))
			for i := from; i < to; i++ {
				run[i-from] = line.Glyphs[i]
				places[2*(i-from)] = complexText.x + line.Positions[2*i] + shifts[i]
				places[2*(i-from)+1] = baseline + line.Positions[2*i+1]
			}
			if !page.glyphsDrawable(fonts[index], run, places) {
				return false
			}
			page.placeGlyphs(fonts[index], size, run, places)
		}
		from = to
	}
	page.endActualText()
	return true
}

func (complexText *ComplexText) fonts() []*Font {
	fonts := []*Font{complexText.font}
	if complexText.fallbackFont != nil {
		fonts = append(fonts, complexText.fallbackFont)
	}
	return fonts
}

// lineHeight returns the distance from a line to the next, as TextBlock's.
func (complexText *ComplexText) lineHeight() float32 {
	font := complexText.font
	size := font.GetSize()
	return (font.GetAscent(size) + font.GetDescent(size) + font.GetLineGap(size)) * complexText.lineSpacing
}

func (complexText *ComplexText) gap() float32 {
	if complexText.paragraphGap < 0 {
		return complexText.font.GetSize() / 2
	}
	return complexText.paragraphGap
}

// shifts returns how far each glyph of the line moves to the right, for its
// alignment: all the same for Center and Right; for Justify, each glyph by
// the room given to the spaces before it, the last line left as it is.
func (complexText *ComplexText) shifts(line ShapedLine, last bool, spaces []int) []float32 {
	shifts := make([]float32, len(line.Glyphs))
	room := complexText.width - line.Width
	if line.Width <= 0 || room <= 0 {
		return shifts
	}
	isSpace := func(i int) bool {
		f := line.Fonts[i]
		return f >= 0 && f < len(spaces) && spaces[f] != 0 && line.Glyphs[i] == spaces[f]
	}
	switch {
	case complexText.textAlignment == alignment.Center || complexText.textAlignment == alignment.Right:
		shift := room
		if complexText.textAlignment == alignment.Center {
			shift = room / 2
		}
		for i := range shifts {
			shifts[i] = shift
		}
	case complexText.textAlignment == alignment.Justify && !last:
		// The spaces between the words: not those at the end of the line
		end := len(line.Glyphs)
		for end > 0 && isSpace(end-1) {
			end--
		}
		count := 0
		for i := 0; i < end; i++ {
			if isSpace(i) {
				count++
			}
		}
		if count > 0 {
			var shift float32
			for i := range shifts {
				shifts[i] = shift
				if i < end && isSpace(i) {
					shift += room / float32(count)
				}
			}
		}
	}
	return shifts
}

// shaped returns the lines of the paragraph, shaped the first time they are
// needed.
func (complexText *ComplexText) shaped(p int) ([]ShapedLine, error) {
	font := complexText.font
	if complexText.shapedLines == nil || complexText.shapedSize != font.GetSize() {
		complexText.shapedLines = make([][]ShapedLine, len(complexText.paragraphs))
		complexText.shapedSize = font.GetSize()
	}
	if complexText.shapedLines[p] != nil {
		return complexText.shapedLines[p], nil
	}
	if complexText.shaper == nil {
		return nil, errors.New("ComplexText needs a text engine to shape the text, given with SetShaper, " +
			"such as one that calls go-text/typesetting.")
	}
	fonts := complexText.fonts()
	files := make([][]byte, len(fonts))
	for i, f := range fonts {
		file, err := f.shapingFile()
		if err != nil {
			return nil, err
		}
		if file == nil {
			return nil, errors.New("ComplexText needs fonts loaded from their font files, " +
				"by path or from their bytes, as the text engine reads the same file.")
		}
		files[i] = file
	}
	text := complexText.paragraphs[p]
	lines, err := complexText.shaper.Shape(text, complexText.language, files,
		complexText.shapedSize, complexText.width)
	if err != nil {
		return nil, err
	}
	for _, line := range lines {
		if len(line.Fonts) != len(line.Glyphs) || len(line.Positions) != 2*len(line.Glyphs) ||
			line.Start < 0 || line.Start > line.End || line.End > len(text) {
			return nil, errors.New("A shaped line needs a font and an x and a y for each glyph, " +
				"and its part of the text.")
		}
	}
	if lines == nil {
		lines = []ShapedLine{}
	}
	complexText.shapedLines[p] = lines
	return lines, nil
}
