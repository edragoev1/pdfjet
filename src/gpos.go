// gpos.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import "sync"

// gpos is where the marks go, from the GPOS table of a font: the marks on
// letters and ligatures, like Hebrew and Arabic vowel marks, from its
// MarkToBase and MarkToLigature lookups, and the marks that attach to other
// marks, like a Thai tone mark above an upper vowel, from its MarkToMark
// lookups.
//
// The table is read the first time a mark is drawn, not when the font is
// loaded: most documents draw no mark, and reading every anchor of a font of
// many scripts, as Noto Sans, took most of the time of a page drawn in it and
// much of what it allocated.
type gpos struct {
	buf        []byte // The font data the table is in, which the font keeps
	start, end int    // The table, from start to end of buf
	numGlyphs  int    // Of the maxp table, or 0 when the font has none
	gposWork   int
	once       sync.Once

	// markToMarkOffsets has the offsets of the marks that attach to other
	// marks, in font units, by the glyph IDs of the two marks.
	markToMarkOffsets map[int][2]int
	// markAnchors has the anchors of the marks, by glyph ID, for each
	// MarkToBase and MarkToLigature lookup subtable: the class and anchor of
	// each mark.
	markAnchors []map[int][]int
	// baseAnchors has the anchors of the letters and ligatures that the marks
	// go on, by glyph ID, for each subtable in markAnchors: 1 and an anchor,
	// or 0 and no anchor, for each class of marks.
	baseAnchors []map[int][]int
}

// maxGposWork is the most work the GPOS table of a font is read with, counted
// in the glyphs of the coverage tables it names, the glyphs their indexes make
// room for, the marks and the letters its mark lookups keep and the pairs they
// place: each mark against each letter or mark it goes on. The lookups, the
// subtables, the ranges of a coverage table and its indexes all say their own
// count, and subtables can share one another, so a table that claims more than
// a font holds is stopped here rather than read to an end it does not have.
// Of the 252 fonts PDFjet ships the most this takes is 76,168, in Noto Sans.
const maxGposWork = 1 << 20

// newGpos returns the GPOS table at the offset of the font data, of the
// length. A value outside the table is read as 0, so a broken table cannot
// stop the font from loading, or a mark from being drawn.
func newGpos(buf []byte, offset, length, numGlyphs int) *gpos {
	start := max(min(offset, len(buf)), 0)
	end := start
	if length > 0 {
		end = int(min(int64(start)+int64(length), int64(len(buf))))
	}
	return &gpos{buf: buf, start: start, end: end, numGlyphs: numGlyphs}
}

// read reads the table, the first time it is called, and returns it.
func (g *gpos) read() *gpos {
	g.once.Do(func() {
		g.gposWork = maxGposWork
		g.markToMarkOffsets = make(map[int][2]int)
		g.markAnchors = make([]map[int][]int, 0)
		g.baseAnchors = make([]map[int][]int, 0)
		g.lookups()
	})
	return g
}

// lookups reads the mark lookups of the table.
func (g *gpos) lookups() {
	lookupList := g.start + g.uint16At(g.start+8)
	lookupCount := g.uint16At(lookupList)
	for i := 0; i < lookupCount && g.gposWork > 0; i++ {
		lookup := lookupList + g.uint16At(lookupList+2+2*i)
		lookupType := g.uint16At(lookup)
		subTableCount := g.uint16At(lookup + 4)
		for j := 0; j < subTableCount && g.gposWork > 0; j++ {
			g.gposWork--
			subTable := lookup + g.uint16At(lookup+6+2*j)
			lookupType2 := lookupType
			if lookupType2 == 9 {
				// An extension lookup holds the subtable, at an offset of 32
				// bits that is read unsigned, as Java and C# read it: an int
				// of 32 bits made one of 0x80000000 or more negative, into the
				// table; past the table, there is no subtable (review of 12
				// October 2026, S4)
				lookupType2 = g.uint16At(subTable + 2)
				extension := int64(g.uint16At(subTable+4))<<16 | int64(g.uint16At(subTable+6))
				if extension >= int64(g.end-subTable) {
					continue
				}
				subTable += int(extension)
			}
			if lookupType2 == 4 && g.uint16At(subTable) == 1 {
				g.getMarkToBaseAnchors(subTable, false)
			} else if lookupType2 == 5 && g.uint16At(subTable) == 1 {
				g.getMarkToBaseAnchors(subTable, true)
			} else if lookupType2 == 6 && g.uint16At(subTable) == 1 {
				g.getMarkToMarkOffsets(subTable)
			}
		}
	}
}

// getMarkToBaseAnchors keeps the anchors of a MarkToBase or MarkToLigature
// subtable, by glyph ID: the class and anchor of each mark, and an anchor of
// each letter for each class of marks, with 1 before an anchor that is there
// and 0 before one that is not. A ligature has anchors for each of the letters
// it joins, and keeps those of its first letter, like the lam of a lam-alef
// ligature.
func (g *gpos) getMarkToBaseAnchors(subTable int, ligature bool) {
	markGlyphs := g.coverageGlyphs(subTable + g.uint16At(subTable+2))
	baseGlyphs := g.coverageGlyphs(subTable + g.uint16At(subTable+4))
	classCount := g.uint16At(subTable + 6)
	markArray := subTable + g.uint16At(subTable+8)
	baseArray := subTable + g.uint16At(subTable+10)
	marks := make(map[int][]int)
	for m, glyph := range markGlyphs {
		if g.gposWork == 0 {
			break
		}
		g.gposWork--
		anchor := markArray + g.uint16At(markArray+4+4*m)
		marks[glyph] = []int{
			g.uint16At(markArray + 2 + 4*m), g.int16At(anchor + 2), g.int16At(anchor + 4)}
	}
	bases := make(map[int][]int)
	for b, glyph := range baseGlyphs {
		// A letter is work of its own, whatever the classes of its marks.
		work := max(classCount, 1)
		if g.gposWork < work {
			break
		}
		g.gposWork -= work
		// The anchor offsets are from the base array, or from the ligature
		// attach table of a ligature.
		table := baseArray
		record := baseArray + 2 + 2*b*classCount
		if ligature {
			table = baseArray + g.uint16At(baseArray+2+2*b)
			record = table + 2
			if g.uint16At(table) == 0 { // No letters
				continue
			}
		}
		anchors := make([]int, 3*classCount)
		for c := 0; c < classCount; c++ {
			anchorOffset := g.uint16At(record + 2*c)
			if anchorOffset != 0 {
				anchors[3*c] = 1
				anchors[3*c+1] = g.int16At(table + anchorOffset + 2)
				anchors[3*c+2] = g.int16At(table + anchorOffset + 4)
			}
		}
		bases[glyph] = anchors
	}
	g.markAnchors = append(g.markAnchors, marks)
	g.baseAnchors = append(g.baseAnchors, bases)
}

// getMarkToMarkOffsets keeps the offset of each mark from the mark it attaches
// to, in font units, by the glyph IDs of the two marks. The first lookup that
// has a pair of marks places them.
func (g *gpos) getMarkToMarkOffsets(subTable int) {
	mark1Glyphs := g.coverageGlyphs(subTable + g.uint16At(subTable+2))
	mark2Glyphs := g.coverageGlyphs(subTable + g.uint16At(subTable+4))
	classCount := g.uint16At(subTable + 6)
	mark1Array := subTable + g.uint16At(subTable+8)
	mark2Array := subTable + g.uint16At(subTable+10)
	for m1, glyph1 := range mark1Glyphs {
		work := max(len(mark2Glyphs), 1)
		if g.gposWork < work {
			break
		}
		g.gposWork -= work
		markClass := g.uint16At(mark1Array + 2 + 4*m1)
		anchor1 := mark1Array + g.uint16At(mark1Array+4+4*m1)
		for m2, glyph2 := range mark2Glyphs {
			anchorOffset := g.uint16At(mark2Array + 2 + 2*(m2*classCount+markClass))
			if anchorOffset == 0 {
				continue
			}
			anchor2 := mark2Array + anchorOffset
			key := (glyph2 << 16) | glyph1
			if _, ok := g.markToMarkOffsets[key]; !ok {
				g.markToMarkOffsets[key] = [2]int{
					g.int16At(anchor2+2) - g.int16At(anchor1+2),
					g.int16At(anchor2+4) - g.int16At(anchor1+4)}
			}
		}
	}
}

// coverageGlyphs returns the glyph IDs of a coverage table, in the order of
// their coverage indexes.
func (g *gpos) coverageGlyphs(offset int) []int {
	format := g.uint16At(offset)
	count := g.uint16At(offset + 2)
	if count > g.gposWork {
		count = g.gposWork
	}
	g.gposWork -= count
	if format == 1 {
		glyphs := make([]int, count)
		for i := 0; i < count; i++ {
			glyphs[i] = g.uint16At(offset + 4 + 2*i)
		}
		return glyphs
	}
	// Format 2 has ranges of glyphs, each with the coverage index of its first glyph.
	size := 0
	for i := 0; i < count; i++ {
		rangeOffset := offset + 4 + 6*i
		start := g.uint16At(rangeOffset)
		end := g.uint16At(rangeOffset + 2)
		if end >= start && g.uint16At(rangeOffset+4)+end-start+1 > size {
			size = g.uint16At(rangeOffset+4) + end - start + 1
		}
	}
	// A coverage table lists each glyph of the font once at most, and the
	// room its indexes make is counted as work, as each glyph of it is gone
	// through by the lookup it is of.
	limit := 0x10000 // A glyph ID is 16 bits.
	if g.numGlyphs > 0 {
		limit = g.numGlyphs
	}
	size = min(size, limit, g.gposWork)
	g.gposWork -= size
	glyphs := make([]int, size)
	for i := 0; i < count && g.gposWork > 0; i++ {
		rangeOffset := offset + 4 + 6*i
		start := g.uint16At(rangeOffset)
		end := g.uint16At(rangeOffset + 2)
		coverageIndex := g.uint16At(rangeOffset + 4)
		// The glyphs of the range past the room of the indexes are left out.
		end = min(end, start+size-1-coverageIndex)
		for glyph := start; glyph <= end && g.gposWork > 0; glyph++ {
			g.gposWork--
			glyphs[coverageIndex+glyph-start] = glyph
		}
	}
	return glyphs
}

// uint16At returns the 16 bits at the offset, or 0 outside the table.
func (g *gpos) uint16At(offset int) int {
	if offset < g.start || offset > g.end-2 {
		return 0
	}
	return int(g.buf[offset])<<8 | int(g.buf[offset+1])
}

func (g *gpos) int16At(offset int) int {
	return int(int16(g.uint16At(offset)))
}

