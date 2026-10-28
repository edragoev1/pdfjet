// kerning.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"sort"
	"sync"
)

// kerning is how far apart pairs of glyphs go, from the font: the PairPos
// lookups of the kern feature of its GPOS table, pairs of glyphs (format 1)
// and pairs of classes of glyphs (format 2). Font.SetKernPairs(true) turns
// it on; off, which it is unless asked for, nothing is read and nothing is
// slower. The kern table of an older TrueType font with no GPOS table is not
// read: none of the fonts PDFjet ships, nor any of a Linux system's, has one
// without GPOS, and such a font draws its text unkerned.
//
// The table is read when the font's kerning is turned on, once, under a
// sync.Once, so that text is measured with it from any number of goroutines,
// and within the same limit of work as the marks of the GPOS table, so that a
// table that claims more than a font holds is stopped rather than read to an
// end it does not have.
type kerning struct {
	gpos    gpos // The GPOS table, read with its reader
	once    sync.Once
	lookups [][]pairSubtable // The kern feature's lookups, in their order
	// Made with the lookups, for the font: a bit for each glyph kerned, not a
	// mark, a right to left letter or a glyph of no character, and the pairs
	// of the printable ASCII characters, looked up once, by asciiIndex
	kernable   []uint64
	asciiIndex []int8 // By glyph, the ASCII character - 0x20 drawn with it, or -1
	asciiKern  []int16
}

// pairSubtable is a PairPos subtable: the first glyphs it covers, and how far
// each pair moves the second glyph, in font units, by the pair or by the
// classes of the pair. Each pair drawn of a kerned font is looked up in them,
// so they are arrays by glyph number, not maps: a map lookup for each
// subtable that a pair goes through made Go's kerned text four times as slow
// as unkerned text (25 October 2026).
type pairSubtable struct {
	covered      []uint64  // A bit for each first glyph covered
	pairSets     []int32   // Format 1, by first glyph: its pair set + 1, or 0
	seconds      [][]int32 // Format 1, the second glyphs of each pair set, in order
	values       [][]int16 // Format 1, the moves of the pairs of each pair set
	firstClass   []uint16  // Format 2, the class of each first glyph, 0 past the end
	secondClass  []uint16  // Format 2, the class of each second glyph, 0 past the end
	secondCount  int       // Format 2, the classes of the second glyphs
	classAdvance []int16   // Format 2, by first class * secondCount + second class
}

// covers returns true if the subtable covers the glyph as a first glyph.
func (s *pairSubtable) covers(gid int) bool {
	return gid>>6 < len(s.covered) && s.covered[gid>>6]&(1<<(gid&63)) != 0
}

// cover marks the glyph covered, and returns false, with nothing marked, when
// there is not the work left for the words it adds to the bits.
func (s *pairSubtable) cover(g *gpos, gid int) bool {
	if gid>>6 >= len(s.covered) {
		if !g.grow(len(s.covered), gid>>6+1, 1) {
			return false
		}
		s.covered = append(s.covered, make([]uint64, gid>>6+1-len(s.covered))...)
	}
	s.covered[gid>>6] |= 1 << (gid & 63)
	return true
}

// grow takes the work of an array grown from its length to the new one, with
// so many of its elements to a word of 64 bits, a word each, and returns
// true, or, when there is not that much work left, stops the reading, with no
// work left, and returns false. The arrays of the kerning, by glyph number,
// grow as far as the glyphs its subtables name, and the room they make is
// work: a table of a few kilobytes whose subtables all named glyph 65535 made
// gigabytes of them (the review of 28 October 2026). Of the 261 fonts PDFjet
// ships the kerning of IBM Plex Sans SC takes the most work, 192,235.
func (g *gpos) grow(length, grown, perWord int) bool {
	work := (grown+perWord-1)/perWord - (length+perWord-1)/perWord
	if work > g.gposWork {
		g.gposWork = 0
		return false
	}
	g.gposWork -= work
	return true
}

// isGlyph returns true if the glyph is one of the font's, or the font does not
// say how many glyphs it has.
func (g *gpos) isGlyph(gid int) bool {
	return g.numGlyphs <= 0 || gid < g.numGlyphs
}

// classOf returns the class of the glyph, 0 for a glyph the classes do not
// reach.
func classOf(classes []uint16, gid int) int {
	if gid < len(classes) {
		return int(classes[gid])
	}
	return 0
}

// newKerning returns the kerning of the font data, from its GPOS table, at its
// offset and of its length; nil when the font has none.
func newKerning(buf []byte, gposTable *fontTable, numGlyphs int) *kerning {
	if gposTable == nil {
		return nil
	}
	return &kerning{gpos: *newGpos(buf, gposTable.offset, gposTable.length, numGlyphs)}
}

// read reads the table, the first time it is called, and makes the glyphs
// kerned and the ASCII pairs of the font, when there is one, and returns the
// kerning.
func (k *kerning) read(font *Font) *kerning {
	k.once.Do(func() {
		k.gpos.gposWork = maxGposWork
		k.readKernFeature()
		if font != nil {
			k.prepare(font)
		}
	})
	return k
}

// prepare makes the glyphs the font kerns and the pairs of its printable
// ASCII characters, so that most pairs drawn are an array read.
func (k *kerning) prepare(font *Font) {
	k.kernable = make([]uint64, (len(font.unicodeOf)+63)/64)
	for gid := 1; gid < len(font.unicodeOf); gid++ {
		c := rune(font.unicodeOf[gid])
		if c >= 0 && !isMark(c) && !isRightToLeft(c) {
			k.kernable[gid>>6] |= 1 << (gid & 63)
		}
	}
	k.asciiIndex = make([]int8, len(font.unicodeOf))
	for gid := range k.asciiIndex {
		k.asciiIndex[gid] = -1
	}
	for i, gid := range font.asciiGID {
		if gid != 0 && int(gid) < len(k.asciiIndex) && k.asciiIndex[gid] < 0 {
			k.asciiIndex[gid] = int8(i)
		}
	}
	k.asciiKern = make([]int16, 95*95)
	for i, first := range font.asciiGID {
		for j, second := range font.asciiGID {
			if first != 0 && second != 0 && k.isKernable(int(first)) && k.isKernable(int(second)) {
				k.asciiKern[i*95+j] = int16(k.pair(int(first), int(second)))
			}
		}
	}
}

// isKernable returns true if the glyph is kerned: not a mark, a right to left
// letter or a glyph of no character, as .notdef, which shaping places.
func (k *kerning) isKernable(gid int) bool {
	return gid > 0 && gid>>6 < len(k.kernable) && k.kernable[gid>>6]&(1<<(gid&63)) != 0
}

// glyphPair returns how far the second glyph moves after the first, in font
// units: 0 unless both are kerned, from the ASCII pairs when both are
// printable ASCII characters.
func (k *kerning) glyphPair(first, second int) int {
	if !k.isKernable(first) || !k.isKernable(second) {
		return 0
	}
	if a, b := k.asciiIndex[first], k.asciiIndex[second]; a >= 0 && b >= 0 {
		return int(k.asciiKern[int(a)*95+int(b)])
	}
	return k.pair(first, second)
}

// pair returns how far the second glyph moves after the first one, in font
// units: the advance of the first glyph, made shorter or longer. Each lookup
// of the kern feature adds its own, from the first of its subtables that
// covers the first glyph and has the pair, as HarfBuzz applies them.
func (k *kerning) pair(first, second int) int {
	if first < 0 || second < 0 {
		return 0
	}
	total := 0
	for _, subtables := range k.lookups {
		for i := range subtables {
			s := &subtables[i]
			if !s.covers(first) {
				continue
			}
			if s.pairSets != nil {
				if v, ok := s.pairValue(first, second); ok {
					total += v
					break
				}
				continue // A pair set without the second glyph: the next subtable
			}
			c1 := classOf(s.firstClass, first)
			c2 := classOf(s.secondClass, second)
			if c2 >= s.secondCount || c1*s.secondCount+c2 >= len(s.classAdvance) {
				continue // A class the subtable has no values for: the next subtable
			}
			total += int(s.classAdvance[c1*s.secondCount+c2])
			break
		}
	}
	return total
}

// pairValue returns the move of the pair of a subtable of format 1, and
// whether the pair set of the first glyph has the second, found by halves in
// its second glyphs, which are in order.
func (s *pairSubtable) pairValue(first, second int) (int, bool) {
	if first >= len(s.pairSets) || s.pairSets[first] == 0 {
		return 0, false
	}
	set := s.pairSets[first] - 1
	seconds := s.seconds[set]
	lo, hi := 0, len(seconds)
	for lo < hi {
		mid := int(uint(lo+hi) >> 1)
		if int(seconds[mid]) < second {
			lo = mid + 1
		} else {
			hi = mid
		}
	}
	if lo < len(seconds) && int(seconds[lo]) == second {
		return int(s.values[set][lo]), true
	}
	return 0, false
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
		// A subtable the lookup names again is read once: the second time it
		// kerns no pair the first did not (the review of 28 October 2026).
		offsetsRead := make(map[int]bool)
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
			if subTableType != 2 || offsetsRead[subTable] {
				continue
			}
			offsetsRead[subTable] = true
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
// covers, a set of second glyphs, each with how far it moves. The second
// glyphs of a set are put in order, the first record of a pair kept, as it is
// the one applied. A glyph past the glyphs of the font is left out.
func (k *kerning) pairsSubtable(subTable int) pairSubtable {
	g := &k.gpos
	firstGlyphs := g.coverageGlyphs(subTable + g.uint16At(subTable+2))
	size1, xAdvance := valueSize(g.uint16At(subTable + 4))
	size2, _ := valueSize(g.uint16At(subTable + 6))
	pairSetCount := g.uint16At(subTable + 8)
	var s pairSubtable
	for i, first := range firstGlyphs {
		if i >= pairSetCount || g.gposWork == 0 {
			break
		}
		if !g.isGlyph(first) {
			continue
		}
		if !s.cover(g, first) {
			break
		}
		if first >= len(s.pairSets) {
			if !g.grow(len(s.pairSets), first+1, 2) {
				break
			}
			s.pairSets = append(s.pairSets, make([]int32, first+1-len(s.pairSets))...)
		}
		pairSet := subTable + g.uint16At(subTable+10+2*i)
		pairCount := min(g.uint16At(pairSet), g.gposWork)
		g.gposWork -= pairCount
		seconds := make([]int32, 0, pairCount)
		values := make([]int16, 0, pairCount)
		for j := 0; j < pairCount; j++ {
			record := pairSet + 2 + j*(2+size1+size2)
			v := 0
			if xAdvance >= 0 {
				v = g.int16At(record + 2 + xAdvance)
			}
			seconds = append(seconds, int32(g.uint16At(record)))
			values = append(values, int16(v))
		}
		seconds, values = sortedPairs(seconds, values)
		if s.pairSets[first] == 0 { // A glyph covered twice: its first pair set
			s.seconds = append(s.seconds, seconds)
			s.values = append(s.values, values)
			s.pairSets[first] = int32(len(s.seconds))
		}
	}
	if s.pairSets == nil {
		s.pairSets = []int32{} // Format 1, though it has no pairs
	}
	return s
}

// sortedPairs returns the second glyphs of a pair set in order, with their
// values, each glyph once, with the value of its first record. A font's are in
// order already, which is looked at first.
func sortedPairs(seconds []int32, values []int16) ([]int32, []int16) {
	ordered := true
	for j := 1; j < len(seconds); j++ {
		if seconds[j] <= seconds[j-1] {
			ordered = false
			break
		}
	}
	if ordered {
		return seconds, values
	}
	index := make([]int, len(seconds))
	for j := range index {
		index[j] = j
	}
	sort.SliceStable(index, func(a, b int) bool { return seconds[index[a]] < seconds[index[b]] })
	sortedSeconds := make([]int32, 0, len(seconds))
	sortedValues := make([]int16, 0, len(seconds))
	for _, j := range index {
		if n := len(sortedSeconds); n > 0 && sortedSeconds[n-1] == seconds[j] {
			continue // The first record of a pair is the one applied
		}
		sortedSeconds = append(sortedSeconds, seconds[j])
		sortedValues = append(sortedValues, values[j])
	}
	return sortedSeconds, sortedValues
}

// classesSubtable reads a PairPos subtable of format 2: the first glyphs it
// covers, the class of each first and second glyph, and how far each pair of
// classes moves the second glyph. A glyph past the glyphs of the font is
// left out.
func (k *kerning) classesSubtable(subTable int) pairSubtable {
	g := &k.gpos
	firstGlyphs := g.coverageGlyphs(subTable + g.uint16At(subTable+2))
	size1, xAdvance := valueSize(g.uint16At(subTable + 4))
	size2, _ := valueSize(g.uint16At(subTable + 6))
	firstClasses := g.uint16At(subTable + 12)
	secondClasses := g.uint16At(subTable + 14)
	s := pairSubtable{secondCount: secondClasses}
	for _, first := range firstGlyphs {
		if g.isGlyph(first) && !s.cover(g, first) {
			break
		}
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
// one, by glyph number, as far as the last glyph of a class other than 0;
// every other glyph is of class 0. The room the classes take is work, as the
// glyphs of a coverage table are.
func (k *kerning) classDef(offset int) []uint16 {
	g := &k.gpos
	var classes []uint16
	set := func(glyph, class int) {
		if class == 0 {
			return
		}
		if glyph >= len(classes) {
			if !g.grow(len(classes), glyph+1, 4) {
				return
			}
			classes = append(classes, make([]uint16, glyph+1-len(classes))...)
		}
		classes[glyph] = uint16(class)
	}
	limit := 0x10000
	if g.numGlyphs > 0 {
		limit = g.numGlyphs
	}
	switch g.uint16At(offset) {
	case 1:
		start := g.uint16At(offset + 2)
		count := min(g.uint16At(offset+4), g.gposWork, limit)
		g.gposWork -= count
		for i := 0; i < count && start+i < limit && g.gposWork > 0; i++ {
			set(start+i, g.uint16At(offset+6+2*i))
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
				set(glyph, class)
			}
		}
	}
	return classes
}
