// harfbuzz.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package harfbuzz gives PDFjet's ComplexText HarfBuzz to shape Hindi,
// Bengali, Tamil, Khmer, Lao, Burmese and the other scripts whose letters
// change shape and place: go-text/typesetting, HarfBuzz written in Go, on
// every system and CPU Go runs on, with nothing to install. Imported, it is
// the shaper of every ComplexText given none:
//
//	import _ "github.com/edragoev1/pdfjet/harfbuzz/v9"
//
// It shapes as HBShape does with HarfBuzz's program hb-shape, and gives the
// same lines: the same glyphs at the same places, measured on the Universal
// Declaration of Human Rights in seven languages.
package harfbuzz

import (
	"bytes"
	"math"
	"sync"

	"github.com/edragoev1/pdfjet/v9/src"
	"github.com/go-text/typesetting/di"
	"github.com/go-text/typesetting/font"
	"github.com/go-text/typesetting/language"
	"github.com/go-text/typesetting/shaping"
	"golang.org/x/image/math/fixed"
)

func init() {
	pdfjet.SetDefaultShaper(NewShaper())
}

// NewShaper returns a shaper for ComplexText that shapes with
// go-text/typesetting, for a ComplexText given it with SetShaper.
func NewShaper() pdfjet.Shaper {
	return pdfjet.NewHarfBuzzShaper(&goText{faces: map[*byte]*font.Face{}})
}

// maxFaces is how many faces goText keeps: a document's fonts, read once
// for all its paragraphs, and never all the fonts of a server's documents,
// each of its own file (ultra review, 22 November 2026)
const maxFaces = 8

// goText is the HarfBuzz of go-text/typesetting: the face of each font file
// read once, kept by the address of the file's first byte, the last maxFaces
// used.
type goText struct {
	lock  sync.Mutex
	faces map[*byte]*font.Face
	order []*byte // Least recently used first
}

func (g *goText) face(file []byte) (*font.Face, error) {
	g.lock.Lock()
	defer g.lock.Unlock()
	key := &file[0]
	if face, ok := g.faces[key]; ok {
		g.used(key)
		return face, nil
	}
	face, err := font.ParseTTF(bytes.NewReader(file))
	if err != nil {
		return nil, err
	}
	if len(g.order) >= maxFaces {
		delete(g.faces, g.order[0])
		g.order = g.order[1:]
	}
	g.faces[key] = face
	g.order = append(g.order, key)
	return face, nil
}

// used moves the key to the end of order, the most recently used
func (g *goText) used(key *byte) {
	for i, k := range g.order {
		if k == key {
			g.order = append(append(g.order[:i:i], g.order[i+1:]...), key)
			return
		}
	}
}

// scriptOf returns the script of the first character of the runes that has
// one of its own, as HarfBuzz guesses it, Latin if none has.
func scriptOf(runes []rune) language.Script {
	for _, r := range runes {
		if script := language.LookupScript(r); script != language.Common && script != language.Inherited {
			return script
		}
	}
	return language.Latin
}

// ShapeRuns shapes each run in the font at a size of a pixel a font unit, so
// that its glyphs are in font units, as hb-shape gives them.
func (g *goText) ShapeRuns(file []byte, runs []string, lang string) ([][]pdfjet.HarfBuzzGlyph, error) {
	if len(file) == 0 {
		return nil, errEmptyFont
	}
	face, err := g.face(file)
	if err != nil {
		return nil, err
	}
	upem := int(face.Upem())
	var shaper shaping.HarfbuzzShaper // One for each call: it is not for two goroutines at once
	shaped := make([][]pdfjet.HarfBuzzGlyph, len(runs))
	for i, run := range runs {
		runes := []rune(run)
		output := shaper.Shape(shaping.Input{
			Text: runes, RunStart: 0, RunEnd: len(runes),
			Direction: di.DirectionLTR, Face: face, Size: fixed.I(upem),
			Script: scriptOf(runes), Language: language.NewLanguage(lang),
		})
		glyphs := make([]pdfjet.HarfBuzzGlyph, len(output.Glyphs))
		for k, gl := range output.Glyphs {
			glyphs[k] = pdfjet.HarfBuzzGlyph{
				Glyph: int(gl.GlyphID), Cluster: gl.TextIndex(),
				XAdvance: units(gl.Advance), XOffset: units(gl.XOffset), YOffset: units(gl.YOffset),
			}
		}
		shaped[i] = glyphs
	}
	return shaped, nil
}

// units is a length of go-text's, in 64ths of a pixel, in font units
func units(v fixed.Int26_6) int {
	return int(math.Round(float64(v) / 64))
}

type harfBuzzError string

func (e harfBuzzError) Error() string { return string(e) }

const errEmptyFont = harfBuzzError("PDFjet's HarfBuzz: an empty font file")
