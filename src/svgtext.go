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
	lineJoin    joinstyle.JoinStyle

	// Where the run is placed: its start, on its baseline, and its width.
	px, py float32
	width  float32
	font   *Font
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
