// svgtext.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// Floating point: every float multiplication in this file is wrapped in
// float32(...) or float64(...) on purpose, so that Go's arm64 compiler does
// not fuse it with an addition and round otherwise than amd64 and the other
// ports. Keep the wrapping; see "Floating point on ARM" in README.md.
// check-no-fma.sh fails if one is removed.

import (
	"math"
	"strings"
	"unicode/utf8"

	"github.com/edragoev1/pdfjet/v9/src/joinstyle"
)

// svgText is a text element of an SVG file, with its tspan elements: the runs
// of its characters, each with where it starts, if it says, and what it is
// drawn with. The runs are placed when the text is drawn, as their widths are
// those of the font it is drawn with.
type svgText struct {
	// The transform from the user space of the text element to the space of
	// the svg element, and after the svg element is drawn at its size, to the
	// space of the image.
	matrix [6]float64
	runs   []*svgTextRun

	// A tspan with a systemLanguage: an image is read before the language is
	// known, so all the runs are kept as they were read, and the ones drawn
	// are chosen, placed and collapsed when it is drawn.
	languages bool
	all       []*svgTextRun
	moves     []svgTextMove // Of the elements read since the last run
}

// svgTextMove is the x, y, dx and dy of an element of a text, NaN where it has
// none, in the choices of the element: one that is not chosen does not move
// the first character after it, whose place stays with the elements around it.
type svgTextMove struct {
	at      [4]float64
	choices []svgChoice
}

// newSVGPending returns the x, y, dx and dy of no element, kept until the first
// character.
func newSVGPending() [4]float64 {
	return [4]float64{math.NaN(), math.NaN(), 0, 0}
}

// applyTo adds the move to the x, y, dx and dy kept for the first character.
func (move *svgTextMove) applyTo(pending *[4]float64) {
	if !math.IsNaN(move.at[0]) {
		pending[0] = move.at[0]
		pending[2] = 0.0
	}
	if !math.IsNaN(move.at[1]) {
		pending[1] = move.at[1]
		pending[3] = 0.0
	}
	if !math.IsNaN(move.at[2]) {
		pending[2] += move.at[2]
	}
	if !math.IsNaN(move.at[3]) {
		pending[3] += move.at[3]
	}
}

// svgTextRun holds the characters of a text or a tspan element between the
// elements in it.
type svgTextRun struct {
	text        string
	x, y        float64 // Where the run starts, if it says, or NaN: a new chunk
	dx, dy      float64 // How far it moves from where the run before it ends
	fontSize    float32
	weight      int // The font-weight, 400 normal and 700 bold
	italic      bool
	family      int // bundledSans, bundledSerif or bundledMono
	anchor      int // 0 start, 1 middle, 2 end
	baseline    int // 0 alphabetic, 1 middle, 2 hanging
	fill        int32
	stroke      int32
	fillAlpha   float32
	strokeAlpha float32
	strokeWidth float32 // In the user space of the text element
	strokeFirst bool
	miterLimit  float32
	lineJoin    joinstyle.JoinStyle
	invisible   bool          // It keeps its place in the text, and a clip path leaves it out
	choices     []svgChoice   // Those of its element, and of the text
	moves       []svgTextMove // Of the elements that start at the run
	read        string        // The text as read, of a text with languages

	// Where the run is placed: its start, on its baseline, and its width.
	px, py float32
	width  float32
	font   *Font
}

// clips reports whether the run has glyphs for a clip path to clip with.
func (run *svgTextRun) clips() bool {
	return run.text != "" && run.fontSize > 0.0 && !run.invisible
}

// clips reports whether the text has glyphs for a clip path to clip with.
func (text *svgText) clips() bool {
	for _, run := range text.runs {
		if run.clips() {
			return true
		}
	}
	return false
}

// operations returns what a clip path writes for the text, as operations.
func (text *svgText) operations() int {
	count := 0
	for _, run := range text.runs {
		count += 1 + utf8.RuneCountInString(run.text)
	}
	return count
}

// keepRuns keeps the runs as they were read, for choose.
func (text *svgText) keepRuns() {
	text.all = append(text.all, text.runs...)
	for _, run := range text.all {
		run.read = run.text
	}
}

// choose makes the runs those of the language chosen, each where the moves of
// the elements chosen put it, and white space collapsed among them alone.
func (text *svgText) choose() {
	if text.all == nil {
		return
	}
	text.runs = make([]*svgTextRun, 0, len(text.all))
	pending := newSVGPending()
	for _, run := range text.all {
		for i := range run.moves {
			if svgAreChosen(run.moves[i].choices) {
				run.moves[i].applyTo(&pending)
			}
		}
		if !svgAreChosen(run.choices) {
			continue
		}
		run.text = run.read
		run.x, run.y, run.dx, run.dy = pending[0], pending[1], pending[2], pending[3]
		pending = newSVGPending()
		text.runs = append(text.runs, run)
	}
	text.collapseSpaces()
}

// collapseSpaces collapses the white space of the runs as a browser does: a
// newline or a tab is a space, spaces one after the other are one, and the
// text has none at its start and at its end.
func (text *svgText) collapseSpaces() {
	space := true // A space before the text: one at its start is left out
	for _, run := range text.runs {
		var buf strings.Builder
		for i := 0; i < len(run.text); i++ {
			ch := run.text[i]
			if ch == '\n' || ch == '\r' || ch == '\t' || ch == ' ' {
				if !space {
					buf.WriteByte(' ')
				}
				space = true
			} else {
				buf.WriteByte(ch)
				space = false
			}
		}
		run.text = buf.String()
	}
	for i := len(text.runs) - 1; i >= 0; i-- {
		run := text.runs[i]
		if strings.HasSuffix(run.text, " ") {
			run.text = run.text[:len(run.text)-1]
			break
		}
		if run.text != "" {
			break
		}
	}
}

// place places the runs, in the user space of the text element, in the fonts
// they are given: each where it says or after the run before it, and each
// chunk, the runs from one that says where it starts to the next, moved by
// its width as its text-anchor asks.
func (text *svgText) place() {
	x := 0.0
	y := 0.0
	chunk := 0 // The first run of the chunk
	for i, run := range text.runs {
		if i == 0 || !math.IsNaN(run.x) || !math.IsNaN(run.y) {
			text.anchor(chunk, i)
			chunk = i
		}
		if !math.IsNaN(run.x) {
			x = run.x
		}
		if !math.IsNaN(run.y) {
			y = run.y
		}
		x += run.dx
		y += run.dy
		run.width = run.font.StringWidth(run.fontSize, run.drawn())
		run.px = float32(x)
		run.py = float32(y)
		if run.baseline == 1 {
			run.py += float32(0.35 * run.fontSize)
		} else if run.baseline == 2 {
			run.py += float32(0.8 * run.fontSize)
		}
		x += float64(run.width)
	}
	text.anchor(chunk, len(text.runs))
}

// anchor moves the runs of a chunk by its width as the text-anchor of its
// first run asks.
func (text *svgText) anchor(from, to int) {
	if from >= to {
		return
	}
	anchor := text.runs[from].anchor
	if anchor == 0 {
		return
	}
	width := float32(0.0)
	for i := from; i < to; i++ {
		width += text.runs[i].width
	}
	shift := -width
	if anchor == 1 {
		shift = float32(-width / 2.0)
	}
	for i := from; i < to; i++ {
		text.runs[i].px += shift
	}
}

// drawn returns the characters of the run as its font draws them: a core
// font, which has no minus sign, draws the hyphen-minus for it, as the
// numbers of a chart are written with it.
func (run *svgTextRun) drawn() string {
	if run.font.isCoreFont {
		return strings.ReplaceAll(run.text, "−", "-")
	}
	return run.text
}
