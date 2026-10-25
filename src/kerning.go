// kerning.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import "sync"

// kerning is how far apart pairs of glyphs go, from the font: the PairPos
// lookups of the kern feature of its GPOS table, pairs of glyphs (format 1)
// and pairs of classes of glyphs (format 2), or, for an older TrueType font
// with no kern feature, the pairs of its kern table. Font.SetKernPairs(true)
// turns it on; off, which it is unless asked for, nothing is read and nothing
// is slower.
//
// The tables are read the first time a kerned font measures or draws text,
// within the same limit of work as the marks of the GPOS table, so that a
// table that claims more than a font holds is stopped rather than read to an
// end it does not have.
type kerning struct {
	gpos       gpos // The GPOS table, read with its reader
	kernTable  gpos // The kern table, read with the same reader
	once       sync.Once
	lookups    [][]pairSubtable // The kern feature's lookups, in their order
	legacyKern map[uint32]int16 // The pairs of the kern table, by glyph IDs
}

// pairSubtable is a PairPos subtable: the first glyphs it covers, and how far
// each pair moves the second glyph, in font units, by the pair or by the
// classes of the pair.
type pairSubtable struct {
	covered      map[int]bool
	pairs        map[uint32]int16 // Format 1, by (first << 16) | second
	firstClass   map[int]int      // Format 2, the class of each first glyph
	secondClass  map[int]int      // Format 2, the class of each second glyph
	secondCount  int              // Format 2, the classes of the second glyphs
	classAdvance []int16          // Format 2, by first class * secondCount + second class
}

// newKerning returns the kerning of the font data, from its GPOS table and its
// kern table, either of which can be missing, each at its offset and of its
// length; nil when the font has neither.
func newKerning(buf []byte, gposTable, kernTable *fontTable, numGlyphs int) *kerning {
	if gposTable == nil && kernTable == nil {
		return nil
	}
	k := new(kerning)
	if gposTable != nil {
		k.gpos = *newGpos(buf, gposTable.offset, gposTable.length, numGlyphs)
	}
	if kernTable != nil {
		k.kernTable = *newGpos(buf, kernTable.offset, kernTable.length, numGlyphs)
	}
	return k
}

// read reads the tables, the first time it is called, and returns the kerning.
func (k *kerning) read() *kerning {
	k.once.Do(func() {
		k.gpos.gposWork = maxGposWork
		k.readKernFeature()
		if len(k.lookups) == 0 {
			k.kernTable.gposWork = maxGposWork
			k.readKernTable()
		}
	})
	return k
}

// pair returns how far the second glyph moves after the first one, in font
// units: the advance of the first glyph, made shorter or longer. Each lookup
// of the kern feature adds its own, from the first of its subtables that
// covers the first glyph and has the pair, as HarfBuzz applies them.
func (k *kerning) pair(first, second int) int {
	k.read()
	if len(k.lookups) == 0 {
		return int(k.legacyKern[uint32(first)<<16|uint32(second)])
	}
	total := 0
	for _, subtables := range k.lookups {
		for i := range subtables {
			s := &subtables[i]
			if !s.covered[first] {
				continue
			}
			if s.pairs != nil {
				if v, ok := s.pairs[uint32(first)<<16|uint32(second)]; ok {
					total += int(v)
					break
				}
				continue // A pair set without the second glyph: the next subtable
			}
			c1 := s.firstClass[first]
			c2 := s.secondClass[second]
			if c2 >= s.secondCount || c1*s.secondCount+c2 >= len(s.classAdvance) {
				continue // A class the subtable has no values for: the next subtable
			}
			total += int(s.classAdvance[c1*s.secondCount+c2])
			break
		}
	}
	return total
}

// readKernFeature reads the lookups of the kern feature of the GPOS table,
// from the default language of every script that has the feature, each
// lookup once and in the order of the lookup list. The kern features of other
// languages are left out, as HarfBuzz leaves them out of text of no language.
func (k *kerning) readKernFeature() {
	g := &k.gpos
	if g.end-g.start < 10 {
		return
	}
	featureList := g.start + g.uint16At(g.start+6)
	lookupList := g.start + g.uint16At(g.start+8)
	featureCount := g.uint16At(featureList)
	lookupCount := g.uint16At(lookupList)
	defaults := k.defaultFeatures(featureCount)
	kern := make([]bool, lookupCount)
	for i := 0; i < featureCount && g.gposWork > 0; i++ {
		g.gposWork--
		if !defaults[i] {
			continue
		}
		record := featureList + 2 + 6*i
		if g.uint16At(record) != 0x6B65 || g.uint16At(record+2) != 0x726E { // "kern"
			continue
		}
		feature := featureList + g.uint16At(record+4)
		indexCount := min(g.uint16At(feature+2), g.gposWork)
		g.gposWork -= indexCount
		for j := 0; j < indexCount; j++ {
			if index := g.uint16At(feature + 4 + 2*j); index < lookupCount {
				kern[index] = true
			}
		}
	}
	for i := 0; i < lookupCount && g.gposWork > 0; i++ {
		if !kern[i] {
			continue
		}
		lookup := lookupList + g.uint16At(lookupList+2+2*i)
		lookupType := g.uint16At(lookup)
		subTableCount := g.uint16At(lookup + 4)
		var subtables []pairSubtable
		for j := 0; j < subTableCount && g.gposWork > 0; j++ {
			g.gposWork--
			subTable := lookup + g.uint16At(lookup+6+2*j)
			subTableType := lookupType
			if subTableType == 9 {
				// An extension lookup holds the subtable at an offset of 32
				// bits, read unsigned, as the marks' lookups read it.
				subTableType = g.uint16At(subTable + 2)
				extension := int64(g.uint16At(subTable+4))<<16 | int64(g.uint16At(subTable+6))
				if extension >= int64(g.end-subTable) {
					continue
				}
				subTable += int(extension)
			}
			if subTableType != 2 {
				continue
			}
			switch g.uint16At(subTable) {
			case 1:
				subtables = append(subtables, k.pairsSubtable(subTable))
			case 2:
				subtables = append(subtables, k.classesSubtable(subTable))
			}
		}
		if len(subtables) > 0 {
			k.lookups = append(k.lookups, subtables)
		}
	}
}

// defaultFeatures returns the features of the default languages of the
// scripts of the GPOS table, by their index in the feature list.
func (k *kerning) defaultFeatures(featureCount int) []bool {
	g := &k.gpos
	features := make([]bool, featureCount)
	scriptList := g.start + g.uint16At(g.start+4)
	scriptCount := g.uint16At(scriptList)
	for i := 0; i < scriptCount && g.gposWork > 0; i++ {
		g.gposWork--
		script := scriptList + g.uint16At(scriptList+2+6*i+4)
		defaultLangSys := g.uint16At(script)
		if defaultLangSys == 0 {
			continue
		}
		langSys := script + defaultLangSys
		if required := g.uint16At(langSys + 2); required < featureCount {
			features[required] = true
		}
		indexCount := min(g.uint16At(langSys+4), g.gposWork)
		g.gposWork -= indexCount
		for j := 0; j < indexCount; j++ {
			if index := g.uint16At(langSys + 6 + 2*j); index < featureCount {
				features[index] = true
			}
		}
	}
	return features
}

// valueSize returns the size of a value record of the format, in bytes, and
// the offset of its XAdvance in it, or -1 when it has none.
func valueSize(format int) (size, xAdvance int) {
	for bit := 0; bit < 8; bit++ {
		if format&(1<<bit) != 0 {
			if bit == 2 {
				xAdvance = size
			}
			size += 2
		}
	}
	if format&0x0004 == 0 {
		xAdvance = -1
	}
	return size, xAdvance
}

// pairsSubtable reads a PairPos subtable of format 1: for each first glyph it
// covers, a set of second glyphs, each with how far it moves.
func (k *kerning) pairsSubtable(subTable int) pairSubtable {
	g := &k.gpos
	firstGlyphs := g.coverageGlyphs(subTable + g.uint16At(subTable+2))
	size1, xAdvance := valueSize(g.uint16At(subTable + 4))
	size2, _ := valueSize(g.uint16At(subTable + 6))
	pairSetCount := g.uint16At(subTable + 8)
	s := pairSubtable{covered: make(map[int]bool), pairs: make(map[uint32]int16)}
	for i, first := range firstGlyphs {
		if i >= pairSetCount || g.gposWork == 0 {
			break
		}
		s.covered[first] = true
		pairSet := subTable + g.uint16At(subTable+10+2*i)
		pairCount := min(g.uint16At(pairSet), g.gposWork)
		g.gposWork -= pairCount
		for j := 0; j < pairCount; j++ {
			record := pairSet + 2 + j*(2+size1+size2)
			second := g.uint16At(record)
			key := uint32(first)<<16 | uint32(second)
			if _, ok := s.pairs[key]; ok {
				continue // The first record of a pair is the one applied
			}
			v := 0
			if xAdvance >= 0 {
				v = g.int16At(record + 2 + xAdvance)
			}
			s.pairs[key] = int16(v)
		}
	}
	return s
}

// classesSubtable reads a PairPos subtable of format 2: the first glyphs it
// covers, the class of each first and second glyph, and how far each pair of
// classes moves the second glyph.
func (k *kerning) classesSubtable(subTable int) pairSubtable {
	g := &k.gpos
	firstGlyphs := g.coverageGlyphs(subTable + g.uint16At(subTable+2))
	size1, xAdvance := valueSize(g.uint16At(subTable + 4))
	size2, _ := valueSize(g.uint16At(subTable + 6))
	firstClasses := g.uint16At(subTable + 12)
	secondClasses := g.uint16At(subTable + 14)
	s := pairSubtable{covered: make(map[int]bool, len(firstGlyphs)), secondCount: secondClasses}
	for _, first := range firstGlyphs {
		s.covered[first] = true
	}
	s.firstClass = k.classDef(subTable + g.uint16At(subTable+8))
	s.secondClass = k.classDef(subTable + g.uint16At(subTable+10))
	count := firstClasses * secondClasses
	if count > g.gposWork {
		return pairSubtable{} // Covers nothing
	}
	g.gposWork -= count
	s.classAdvance = make([]int16, count)
	if xAdvance >= 0 {
		record := subTable + 16
		for c := 0; c < count; c++ {
			s.classAdvance[c] = int16(g.int16At(record + c*(size1+size2) + xAdvance))
		}
	}
	return s
}

// classDef reads a class definition table: the class of each glyph it gives
// one, every other glyph of class 0.
func (k *kerning) classDef(offset int) map[int]int {
	g := &k.gpos
	classes := make(map[int]int)
	limit := 0x10000
	if g.numGlyphs > 0 {
		limit = g.numGlyphs
	}
	switch g.uint16At(offset) {
	case 1:
		start := g.uint16At(offset + 2)
		count := min(g.uint16At(offset+4), g.gposWork, limit)
		g.gposWork -= count
		for i := 0; i < count; i++ {
			if class := g.uint16At(offset + 6 + 2*i); class != 0 {
				classes[start+i] = class
			}
		}
	case 2:
		rangeCount := min(g.uint16At(offset+2), g.gposWork)
		g.gposWork -= rangeCount
		for i := 0; i < rangeCount && g.gposWork > 0; i++ {
			record := offset + 4 + 6*i
			start := g.uint16At(record)
			end := min(g.uint16At(record+2), limit-1)
			class := g.uint16At(record + 4)
			for glyph := start; glyph <= end && g.gposWork > 0; glyph++ {
				g.gposWork--
				if class != 0 {
					classes[glyph] = class
				}
			}
		}
	}
	return classes
}

// readKernTable reads the horizontal pairs of format 0 of the kern table of
// an older TrueType font, version 0, which Windows reads; the version 1 table
// of Apple's fonts is left out.
func (k *kerning) readKernTable() {
	t := &k.kernTable
	if t.end-t.start < 4 || t.uint16At(t.start) != 0 {
		return
	}
	k.legacyKern = make(map[uint32]int16)
	tableCount := t.uint16At(t.start + 2)
	subTable := t.start + 4
	for i := 0; i < tableCount && t.gposWork > 0 && subTable+6 <= t.end; i++ {
		t.gposWork--
		length := t.uint16At(subTable + 2)
		coverage := t.uint16At(subTable + 4)
		// Format 0, horizontal, kerning and not minimum values, not cross-stream
		if coverage>>8 == 0 {
			// The 16 bits of the length of a table of many pairs overflow:
			// the pairs say where the next table is
			length = 14 + 6*t.uint16At(subTable+6)
		}
		if coverage>>8 == 0 && coverage&0x000F == 0x0001 {
			pairCount := min(t.uint16At(subTable+6), t.gposWork)
			t.gposWork -= pairCount
			for j := 0; j < pairCount; j++ {
				record := subTable + 14 + 6*j
				key := uint32(t.uint16At(record))<<16 | uint32(t.uint16At(record+2))
				if _, ok := k.legacyKern[key]; !ok {
					k.legacyKern[key] = int16(t.int16At(record + 4))
				}
			}
		}
		if length < 6 {
			break
		}
		subTable += length
	}
}
