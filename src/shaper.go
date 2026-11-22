// shaper.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"errors"
	"math"
)

// Shaper is a text engine that shapes text for TextBlock and TextLine: it
// wraps the text to a width and finds, for each line, the glyphs of the fonts
// and their places. PDFjet does not shape scripts such as Hindi and Khmer
// itself; a shaper does, and PDFjet draws what it found, as outlines. Go has
// no text engine of its own that gives glyphs: the one line
// import _ "github.com/edragoev1/pdfjet/harfbuzz/v9" gives PDFjet HarfBuzz,
// nothing to install; SetShaper gives another, such as HBShape, which runs
// HarfBuzz's program hb-shape, used only when set.
type Shaper interface {
	// Shape wraps the text to the width and shapes each line: the text, its
	// language, the font files, the font size and the width of the lines are
	// in the request. It returns the lines, in order.
	Shape(request ShapingRequest) ([]ShapedLine, error)
}

// ShapingRequest is what a Shaper is asked to shape. One struct, so that what
// a shaper is told can grow without changing the Shaper interface.
type ShapingRequest struct {
	// Text is the text.
	Text string
	// Language is the language of the text, such as "hi" or "km", or "".
	Language string
	// FontFiles are the bytes of the font files: the first for the text, the
	// others, in order, for the characters it does not have.
	FontFiles [][]byte
	// FontSize is the font size, in points.
	FontSize float32
	// Width is the width of the lines, in points.
	Width float32
}

// ShapedLine is a line of text a Shaper shaped: the part of the text it
// holds, and its glyphs, each with its font and its place.
type ShapedLine struct {
	// Start is the index in the text, in bytes, of the line's first character.
	Start int
	// End is the index in the text, in bytes, after the line's last character.
	End int
	// FontIndexes is, for each glyph, the index of its font among the font
	// files the shaper was given, or -1 for a glyph of none of them, which is
	// not drawn.
	FontIndexes []int
	// Glyphs are the glyph numbers, in their fonts.
	Glyphs []int
	// Positions are the x and the y of each glyph, in points, from the left
	// end of the line at its baseline, y growing down: x0, y0, x1, y1 and so
	// on.
	Positions []float32
	// Width is the width of the line, in points, without the white space at
	// its end: what is centered or aligned to the right.
	Width float32
}

// needsShaper tells whether the text has a character of a script whose
// letters change shape and place, which a shaper draws: Devanagari, Bengali,
// Gurmukhi, Gujarati, Oriya, Tamil, Telugu, Kannada, Malayalam, Sinhala,
// Lao, Myanmar and Khmer. Thai is not one: TextBlock draws it as text, its
// lines broken by ICU's dictionary.
func needsShaper(text string) bool {
	for _, r := range text {
		if r < 0x0900 {
			continue
		}
		if (r >= 0x0900 && r <= 0x0DFF) || // Devanagari to Sinhala
			(r >= 0x0E80 && r <= 0x0EFF) || // Lao
			(r >= 0x1000 && r <= 0x109F) || // Myanmar
			(r >= 0x1780 && r <= 0x17FF) || (r >= 0x19E0 && r <= 0x19FF) || // Khmer
			(r >= 0xA8E0 && r <= 0xA8FF) || // Devanagari Extended
			(r >= 0xA9E0 && r <= 0xA9FF) || (r >= 0xAA60 && r <= 0xAA7F) || // Myanmar Extended
			(r >= 0x111E0 && r <= 0x111FF) { // Sinhala Archaic Numbers
			return true
		}
	}
	return false
}

// shapeText shapes the text with the shaper, or the default one, in the
// fonts, at the size, wrapped to the width; the lines checked, so that a
// shaper of the program's own cannot make PDFjet draw past them.
func shapeText(shaper Shaper, fonts []*Font, size float32, language, text string, width float32) ([]ShapedLine, error) {
	if shaper == nil {
		shaper = defaultShaper()
	}
	if shaper == nil {
		return nil, errNoShaper
	}
	files := make([][]byte, len(fonts))
	for i, f := range fonts {
		file, err := f.shapingFile()
		if err != nil {
			return nil, err
		}
		if file == nil {
			return nil, errors.New("Text such as Hindi or Khmer needs fonts loaded from their font files, " +
				"by path or from their bytes, as the text engine reads the same file.")
		}
		files[i] = file
	}
	lines, err := shaper.Shape(ShapingRequest{Text: text, Language: language,
		FontFiles: files, FontSize: size, Width: width})
	if err != nil {
		return nil, err
	}
	for _, line := range lines {
		if len(line.FontIndexes) != len(line.Glyphs) || len(line.Positions) != 2*len(line.Glyphs) ||
			line.Start < 0 || line.Start > line.End || line.End > len(text) {
			return nil, errors.New("A shaped line needs a font and an x and a y for each glyph, " +
				"and its part of the text.")
		}
	}
	return lines, nil
}

// shapingFonts returns the fonts text is shaped in: the font, and the
// fallback font for the characters the font has not, when it has a file the
// text engine can read; a fallback without one, as a core font, is left out,
// so that a Latin fallback does not fail Hindi text.
func shapingFonts(font, fallback *Font) []*Font {
	fonts := []*Font{font}
	if fallback != nil && fallback != font {
		if file, err := fallback.shapingFile(); err == nil && file != nil {
			fonts = append(fonts, fallback)
		}
	}
	return fonts
}

// shapeOneLine shapes the text as one line, not wrapped, with the default
// shaper: the lines of a shaper that broke it anyway, joined.
func shapeOneLine(fonts []*Font, size float32, language, text string) (*shapedLine, error) {
	lines, err := shapeText(nil, fonts, size, language, text, math.MaxFloat32/4)
	if err != nil {
		return nil, err
	}
	shaped := &shapedLine{text: text, fonts: fonts, size: size}
	for i, line := range lines {
		if i == 0 {
			shaped.line = line
			continue
		}
		x := shaped.line.Width
		shaped.line.End = line.End
		shaped.line.FontIndexes = append(shaped.line.FontIndexes, line.FontIndexes...)
		shaped.line.Glyphs = append(shaped.line.Glyphs, line.Glyphs...)
		for j := 0; j < len(line.Positions); j += 2 {
			shaped.line.Positions = append(shaped.line.Positions, x+line.Positions[j], line.Positions[j+1])
		}
		shaped.line.Width = x + line.Width
	}
	return shaped, nil
}

// shapingStyle is what shaped text depends on besides its text: a cache of
// shaped text is good only for the style it was made in.
type shapingStyle struct {
	font     *Font
	fallback *Font
	size     float32
	language string
}

// shapedPieces is a TextLine's cache of the texts it shaped, its own and its
// words', which the copies a TextFrame or a TextColumn draws share, so that
// each is shaped once, however often it is measured and drawn.
type shapedPieces struct {
	style  shapingStyle
	shaped map[string]*shapedLine
}

// shapedLine is a line of TextBlock or TextLine a shaper shaped: its glyphs,
// drawn as outlines in its fonts, and its text, which tags them.
type shapedLine struct {
	line  ShapedLine
	text  string // The line's text: its part of the paragraph
	fonts []*Font
	size  float32
}

// drawTurnedShapedLine draws the shaped line as drawShapedLine does, turned
// by the degrees, clockwise, around its start at x on the baseline, as a
// TextLine's text is turned. It returns false if the document failed.
func (page *Page) drawTurnedShapedLine(shaped *shapedLine, x, baseline float32, language string, degrees int) bool {
	if degrees%360 == 0 {
		return page.drawShapedLine(shaped, x, baseline, language)
	}
	page.SaveGraphicsState()
	page.rotateAroundCenter(x, page.height-baseline, float32(-degrees))
	drawn := page.drawShapedLine(shaped, x, baseline, language)
	page.RestoreGraphicsState()
	return drawn
}

// drawShapedLine draws the shaped line, its left at x and its baseline at
// baseline, in the language: the glyphs of each font, one run at a time,
// tagged with the line's text for screen readers and the copying of text.
// It returns false if the document failed.
func (page *Page) drawShapedLine(shaped *shapedLine, x, baseline float32, language string) bool {
	line := shaped.line
	if len(line.Glyphs) == 0 {
		return true
	}
	page.beginActualText(shaped.text, language)
	for from := 0; from < len(line.Glyphs); {
		to := from
		for to < len(line.Glyphs) && line.FontIndexes[to] == line.FontIndexes[from] {
			to++
		}
		index := line.FontIndexes[from]
		if index >= 0 && index < len(shaped.fonts) {
			run := make([]int, to-from)
			places := make([]float32, 2*(to-from))
			for i := from; i < to; i++ {
				run[i-from] = line.Glyphs[i]
				places[2*(i-from)] = x + line.Positions[2*i]
				places[2*(i-from)+1] = baseline + line.Positions[2*i+1]
			}
			if !page.glyphsDrawable(shaped.fonts[index], run, places) {
				return false
			}
			page.placeGlyphs(shaped.fonts[index], shaped.size, run, places)
		}
		from = to
	}
	page.endActualText()
	return true
}
