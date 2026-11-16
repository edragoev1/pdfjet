// glyphoutline_fuzz_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"encoding/binary"
	"math"
	"strings"
	"testing"
)

// The fuzz targets of the outlines Page.DrawGlyphs draws, read from the font
// file itself. A real font is read once, and one of its glyphs is replaced by
// the input: the bytes reach the reader of the glyph, its charstring or its
// glyf entry, instead of the tables around it, while the subroutines, the
// Font DICTs and the other glyphs a composite glyph names stay those of the
// font. Any input either draws an outline, or is refused with an error: an
// index out of range, a hang, gigabytes of memory, or a NaN or an infinity
// written into the path is a bug.
//
//	go test ./src -run '^$' -fuzz '^FuzzGlyphOutlineCFF$' -fuzztime 30m
//	go test ./src -run '^$' -fuzz '^FuzzGlyphOutlineTrueType$' -fuzztime 30m

// fuzzGlyphPath fails when an outline that was read has a number that is not
// finite, which would write a broken content stream.
func fuzzGlyphPath(t *testing.T, path glyphPath, err error) {
	if err != nil || path.operators == "" {
		return
	}
	if strings.Contains(path.operators, "NaN") || strings.Contains(path.operators, "Inf") {
		t.Fatalf("a number that is not finite in the path:\n%.300s", path.operators)
	}
	for _, v := range []float64{path.xMin, path.yMin, path.xMax, path.yMax} {
		if math.IsNaN(v) || math.IsInf(v, 0) {
			t.Fatalf("the box of the path is not finite: %v %v %v %v", path.xMin, path.yMin, path.xMax, path.yMax)
		}
	}
}

// FuzzGlyphOutlineCFF replaces the charstring of glyph 1 of IBM Plex Sans, a
// name-keyed CFF font, or of Source Han Sans, a CID-keyed one with a Font
// DICT of each glyph, with the input.
func FuzzGlyphOutlineCFF(f *testing.F) {
	pdf := testNewPDF()
	plex := NewFontFromFile(pdf, "../"+testPlexOTF)
	han := NewFontFromFile(pdf, "../"+testHanOTF)
	var bases [2]*glyphOutline
	for i, font := range []*Font{plex, han} {
		o, err := newGlyphOutline(font.program.font, true)
		if err != nil {
			f.Fatal(err)
		}
		bases[i] = o
	}
	charString := func(o *glyphOutline, gid int) []byte {
		return o.data[o.charStrings.objects[gid]:o.charStrings.objects[gid+1]]
	}
	for _, r := range "OgA&@%" {
		f.Add(charString(bases[0], plex.unicodeToGID.get(r)), false)
	}
	for _, r := range "日本語あ" {
		f.Add(charString(bases[1], han.unicodeToGID.get(r)), true)
	}
	f.Fuzz(func(t *testing.T, cs []byte, cid bool) {
		base := bases[0]
		if cid {
			base = bases[1]
		}
		o := *base
		o.data = append(base.data[:len(base.data):len(base.data)], cs...)
		objects := append([]int(nil), base.charStrings.objects...)
		objects[1] = len(base.data)
		objects[2] = len(o.data)
		o.charStrings.objects = objects
		fuzzRun(t, len(cs), func() {
			path, err := o.outline(1)
			fuzzGlyphPath(t, path, err)
		})
	})
}

// FuzzGlyphOutlineTrueType replaces the last glyph of Cascadia Mono, a
// TrueType font, with the input: a simple glyph, or a composite one of any of
// the font's glyphs, itself included.
func FuzzGlyphOutlineTrueType(f *testing.F) {
	pdf := testNewPDF()
	cascadia := NewFontFromFile(pdf, "../"+testCascadia)
	base, err := newGlyphOutline(cascadia.program.font, false)
	if err != nil {
		f.Fatal(err)
	}
	if !base.longOffsets {
		f.Fatal("the seed font has short loca offsets; the target writes long ones")
	}
	last := base.numGlyphs - 1
	glyph := func(gid int) []byte {
		start := int(binary.BigEndian.Uint32(base.data[base.locaOffset+4*gid:]))
		end := int(binary.BigEndian.Uint32(base.data[base.locaOffset+4*gid+4:]))
		return base.data[base.glyfOffset+start : base.glyfOffset+end]
	}
	for _, r := range "O%gÁÅé" {
		f.Add(glyph(cascadia.unicodeToGID.get(r)))
	}
	f.Fuzz(func(t *testing.T, g []byte) {
		o := *base
		data := make([]byte, len(base.data), len(base.data)+3+len(g))
		copy(data, base.data)
		for (len(data)-base.glyfOffset)%4 != 0 {
			data = append(data, 0)
		}
		start := len(data) - base.glyfOffset
		data = append(data, g...)
		binary.BigEndian.PutUint32(data[base.locaOffset+4*last:], uint32(start))
		binary.BigEndian.PutUint32(data[base.locaOffset+4*last+4:], uint32(start+len(g)))
		o.data = data
		o.glyfLength = len(data) - base.glyfOffset
		fuzzRun(t, len(g), func() {
			path, err := o.outline(last)
			fuzzGlyphPath(t, path, err)
		})
	})
}
