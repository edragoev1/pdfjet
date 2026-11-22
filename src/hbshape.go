// hbshape.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"encoding/json"
	"errors"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"strconv"
	"strings"
	"unicode"
)

// HBShape is a Shaper for ComplexText that runs hb-shape, the program of
// HarfBuzz, the text engine of Chrome and Android: the same shaping on every
// server, with nothing linked into the program. Lines break at spaces and, in
// Thai, Lao, Khmer and Burmese, between words, by ICU's dictionaries of the
// world fonts. hb-shape is installed on the server, HarfBuzz 15 or newer:
//
//	Ubuntu, Debian: apt install libharfbuzz-bin
//	Fedora, RHEL:   dnf install harfbuzz-utils
//	macOS, Linux:   brew install harfbuzz
//	Windows:        harfbuzz-win64-<version>.zip of github.com/harfbuzz/harfbuzz/releases
//
// It talks with hb-shape through files, not pipes, the same on every system:
// the text of each font in a temporary file, the glyphs back in another.
//
//	text := pdfjet.NewComplexText(hindi, paragraphs).
//		SetFallbackFont(latin).
//		SetShaper(pdfjet.NewHBShape())
type HBShape struct {
	// Program is hb-shape's path, or its name looked for on the PATH.
	Program string
}

// defaultShaper is ComplexText's shaper when none is set
var defaultShaper Shaper = NewHBShape()

// NewHBShape returns an HBShape that runs hb-shape of the PATH.
func NewHBShape() *HBShape {
	return &HBShape{Program: "hb-shape"}
}

// Version returns what hb-shape --version prints first, such as
// "hb-shape (HarfBuzz) 15.2.0", or an error when it cannot be run.
func (hb *HBShape) Version() (string, error) {
	out, err := exec.Command(hb.Program, "--version").Output()
	if err != nil {
		return "", hb.notRun(err, nil)
	}
	line, _, _ := strings.Cut(string(out), "\n")
	return strings.TrimSpace(line), nil
}

func (hb *HBShape) notRun(err error, stderr []byte) error {
	if errors.Is(err, exec.ErrNotFound) {
		return errors.New("ComplexText shapes with HarfBuzz's program hb-shape, which is not installed: " +
			"install it (Ubuntu: apt install libharfbuzz-bin; macOS: brew install harfbuzz; Windows: " +
			"HarfBuzz's harfbuzz-win64 zip, its path in HBShape.Program), or give ComplexText a shaper " +
			"of your own with SetShaper, such as one that calls go-text/typesetting (Example_52)")
	}
	message := strings.TrimSpace(string(stderr))
	if message != "" {
		return fmt.Errorf("hb-shape: %v: %s", err, message)
	}
	return fmt.Errorf("hb-shape: %v", err)
}

// An hb-shape glyph, in font units, its cluster the index of a character in
// its line of the input
type hbGlyph struct {
	G  int `json:"g"`
	Cl int `json:"cl"`
	Ax int `json:"ax"`
	Dx int `json:"dx"`
	Dy int `json:"dy"`
}

// A glyph shaped, in points from the start of the text
type hbShaped struct {
	id, font, cluster int // The cluster: the index in the text, in runes, of its first character
	x, y, advance     float32
}

// readFont reads the character map and the units per em of a font file, or
// returns an error for a file that is not a font.
func readFont(file []byte) (otf *openTypeFont, err error) {
	defer func() {
		if r := recover(); r != nil {
			err = fmt.Errorf("%v", r)
		}
	}()
	return newOpenTypeFont(file), nil
}

// Shape shapes the text with hb-shape, each run of one font in that font,
// and breaks it into lines no wider than the width.
func (hb *HBShape) Shape(request ShapingRequest) ([]ShapedLine, error) {
	text := request.Text
	if strings.ContainsAny(text, "\r\n") {
		return nil, errors.New("HBShape shapes a paragraph: a text without line feeds")
	}
	fonts := make([]*openTypeFont, len(request.FontFiles))
	for i, file := range request.FontFiles {
		otf, err := readFont(file)
		if err != nil {
			return nil, err
		}
		fonts[i] = otf
	}
	if len(fonts) == 0 {
		return nil, errors.New("HBShape needs a font file")
	}
	runes := []rune(text)
	// The index in the text, in bytes, of each rune, and of the end
	offsets := make([]int, 0, len(runes)+1)
	for i := range text {
		offsets = append(offsets, i)
	}
	offsets = append(offsets, len(text))

	// Each character in the first font that has it, else in the first; a
	// mark in the font of the character it is on
	fontOf := make([]int, len(runes))
	for i, r := range runes {
		if i > 0 && unicode.Is(unicode.M, r) {
			fontOf[i] = fontOf[i-1]
			continue
		}
		for k, otf := range fonts {
			if isControl(r) || (r >= otf.firstChar && r <= otf.lastChar && otf.unicodeToGID.get(r) != 0) {
				fontOf[i] = k
				break
			}
		}
	}
	// The runs of one font, by font
	type run struct{ start, end int }
	runsOf := make([][]run, len(fonts))
	var runs []run
	for start := 0; start < len(runes); {
		end := start + 1
		for end < len(runes) && fontOf[end] == fontOf[start] {
			end++
		}
		runsOf[fontOf[start]] = append(runsOf[fontOf[start]], run{start, end})
		runs = append(runs, run{start, end})
		start = end
	}

	// Each font's runs shaped in one run of hb-shape, a run a line
	dir, err := os.MkdirTemp("", "pdfjet-hbshape-")
	if err != nil {
		return nil, err
	}
	defer os.RemoveAll(dir)
	shapedRuns := map[int][]hbGlyph{} // By the start of the run
	for k, list := range runsOf {
		if len(list) == 0 {
			continue
		}
		fontPath := filepath.Join(dir, "font"+strconv.Itoa(k))
		if err := os.WriteFile(fontPath, request.FontFiles[k], 0o600); err != nil {
			return nil, err
		}
		var input strings.Builder
		for _, r := range list {
			input.WriteString(string(runes[r.start:r.end]))
			input.WriteByte('\n')
		}
		inPath := filepath.Join(dir, "in"+strconv.Itoa(k)+".txt")
		outPath := filepath.Join(dir, "out"+strconv.Itoa(k)+".txt")
		if err := os.WriteFile(inPath, []byte(input.String()), 0o600); err != nil {
			return nil, err
		}
		args := []string{"--font-file=" + fontPath, "--text-file=" + inPath, "--output-file=" + outPath,
			"--no-glyph-names", "--output-format=json"}
		if request.Language != "" {
			args = append(args, "--language="+request.Language)
		}
		var stderr bytes.Buffer
		command := exec.Command(hb.Program, args...)
		command.Stderr = &stderr
		if err := command.Run(); err != nil {
			return nil, hb.notRun(err, stderr.Bytes())
		}
		out, err := os.ReadFile(outPath)
		if err != nil {
			return nil, err
		}
		var lines []string
		for _, line := range strings.Split(string(out), "\n") {
			if line = strings.TrimRight(line, "\r"); line != "" {
				lines = append(lines, line)
			}
		}
		if len(lines) != len(list) {
			return nil, fmt.Errorf("hb-shape gave %d lines for %d runs of text", len(lines), len(list))
		}
		for i, line := range lines {
			var glyphs []hbGlyph
			if err := json.Unmarshal([]byte(line), &glyphs); err != nil {
				return nil, fmt.Errorf("hb-shape's output: %v", err)
			}
			shapedRuns[list[i].start] = glyphs
		}
	}

	// The glyphs of the runs in order, in points
	var glyphs []hbShaped
	var pen float32
	for _, r := range runs {
		k := fontOf[r.start]
		scale := request.FontSize / float32(fonts[k].unitsPerEm)
		for _, g := range shapedRuns[r.start] {
			if r.start+g.Cl >= r.end {
				return nil, errors.New("hb-shape gave a glyph of a character past its run")
			}
			glyphs = append(glyphs, hbShaped{
				id: g.G, font: k, cluster: r.start + g.Cl,
				x: pen + float32(float32(g.Dx)*scale), y: -float32(g.Dy) * scale,
				advance: float32(float32(g.Ax) * scale),
			})
			pen += float32(float32(g.Ax) * scale)
		}
	}
	return hbLines(glyphs, runes, offsets, len(text), request.Width), nil
}

// hbLines breaks the glyphs into lines no wider than the width: after a
// space, or between two words of Thai, Lao, Khmer or Burmese, the last place
// that fits; a word wider than a line alone on its line.
func hbLines(glyphs []hbShaped, runes []rune, offsets []int, end int, width float32) []ShapedLine {
	wordEnd := map[int]bool{}
	for _, i := range dictionaryWordEnds(runes) {
		wordEnd[i] = true
	}
	// A line may break before the glyph that begins a cluster after a space
	// or at the end of a word
	breakBefore := func(i int) bool {
		c := glyphs[i].cluster
		if i == 0 || glyphs[i-1].cluster == c {
			return false
		}
		return runes[c-1] == ' ' || (wordEnd[c] && runes[c] != ' ')
	}
	var lines []ShapedLine
	for first := 0; first < len(glyphs); {
		lineStart := glyphs[first].x
		last := len(glyphs)
		lastBreak := -1
		for i := first; i < len(glyphs); i++ {
			if i > first && breakBefore(i) {
				lastBreak = i
			}
			if runes[glyphs[i].cluster] != ' ' && glyphs[i].x+glyphs[i].advance-lineStart > width && lastBreak > first {
				last = lastBreak
				break
			}
		}
		line := ShapedLine{Start: offsets[glyphs[first].cluster], End: end}
		if last < len(glyphs) {
			line.End = offsets[glyphs[last].cluster]
		}
		// The width of the line without the spaces at its end
		visible := last
		for visible > first && runes[glyphs[visible-1].cluster] == ' ' {
			visible--
		}
		if visible > first {
			line.Width = glyphs[visible-1].x + glyphs[visible-1].advance - lineStart
		}
		for i := first; i < last; i++ {
			line.FontIndexes = append(line.FontIndexes, glyphs[i].font)
			line.Glyphs = append(line.Glyphs, glyphs[i].id)
			line.Positions = append(line.Positions, glyphs[i].x-lineStart, glyphs[i].y)
		}
		lines = append(lines, line)
		first = last
	}
	return lines
}
