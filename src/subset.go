// subset.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"errors"
	"math"
	"sort"

	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/internal/compressor"
)

// fontProgram is the font program of a TrueType font, or the CFF table of an
// OpenType font with CFF outlines, kept until Complete, when it is embedded
// with the outlines of the glyphs the document did not draw emptied. The glyph
// numbers stay, so the widths, the character map and the ToUnicode map are
// those of the whole font. Fonts read from one file share one program, and the
// glyphs any of them drew.
type fontProgram struct {
	font      []byte // The TrueType font, or the CFF table
	cff       bool
	forbidden bool      // The fsType of a CFF font's OS/2 table forbids subsetting
	used      *glyphSet // The glyphs drawn, by glyph number
	// The character first drawn with each glyph, by glyph number, for the
	// ToUnicode map of a glyph of several characters, as U+3000 and U+2003
	chars map[int]rune
	whole bool // Set by SetSubset(false): the font is embedded whole
}

// newFontProgram returns the program of a font of the number of glyphs given,
// the count of its maxp table, or 0 when it has none.
func newFontProgram(glyphs int) *fontProgram {
	program := &fontProgram{used: newGlyphSet(glyphs)}
	program.used.add(0) // .notdef, drawn for a character the font lacks
	return program
}

// glyphSet is the set of the glyphs drawn with a font, a bit for each glyph
// number: 128 bytes for the 1,019 glyphs of IBM Plex Sans, where a bool for
// each of the 65,536 glyph numbers was 64 KB for every font of every
// document (the owner's idea, 10 October 2026). It grows for a glyph past
// the font's count, up to the 65,536 a glyph number can be, so that it keeps
// what the array kept.
type glyphSet struct {
	words []uint64
}

func newGlyphSet(glyphs int) *glyphSet {
	if glyphs <= 0 || glyphs > 0x10000 {
		glyphs = 0x10000 // A glyph number is written in four hexadecimal digits
	}
	return &glyphSet{words: make([]uint64, (glyphs+63)/64)}
}

// add adds the glyph number to the set.
func (set *glyphSet) add(gid int) {
	if gid < 0 || gid >= 0x10000 {
		return
	}
	i := gid >> 6
	if i >= len(set.words) {
		words := make([]uint64, i+1)
		copy(words, set.words)
		set.words = words
	}
	set.words[i] |= 1 << uint(gid&63)
}

// bools returns the set as a bool for each glyph number up to the end of its
// last word, for the subsetters, which read a glyph number's bool.
func (set *glyphSet) bools() []bool {
	used := make([]bool, len(set.words)*64)
	for gid := range used {
		used[gid] = set.words[gid>>6]&(1<<uint(gid&63)) != 0
	}
	return used
}

// useGlyph records that the glyph is drawn with the font.
func (font *Font) useGlyph(gid int) {
	if font.program != nil {
		font.program.used.add(gid)
	}
}

// useChar records that the glyph is drawn for the character, so that the
// ToUnicode map gives the glyph the character the document drew, where the
// font draws several characters with one glyph: it gave the glyph the lowest
// of them, and a Persian ۱ was copied as an Arabic ١ (review of 11 October
// 2026, #8). The first character drawn with the glyph is kept; the others
// drawn with it are given their own as actual text, see drawnAsAnother.
func (font *Font) useChar(c rune, gid int) {
	if font.program == nil || gid <= 0 || font.unicodeToGID.get(c) != gid {
		return
	}
	if font.program.chars == nil {
		font.program.chars = make(map[int]rune)
	}
	if _, ok := font.program.chars[gid]; !ok {
		font.program.chars[gid] = c
	}
}

// drawnAsAnother returns true if the glyph of the character is drawn for
// another character too, which the ToUnicode map gives the glyph: the
// character is then drawn in a span with itself as its actual text. A
// document that drew "١٢٣" and then "۱۲۳" copied the second as "١۲٣"
// (review of 12 October 2026, R15).
func (font *Font) drawnAsAnother(c rune) bool {
	if font.program == nil || font.program.chars == nil {
		return false
	}
	first, ok := font.program.chars[font.unicodeToGID.get(c)]
	return ok && first != c
}

// SetSubset sets whether this font is embedded as a subset, the outlines of
// the glyphs the document does not draw left out, which is the default for a
// .ttf or a .otf. A font whose license does not allow subsetting, by the
// fsType of its OS/2 table, is embedded whole. Fonts read from one file are
// one font program in the PDF: kept whole for one, the program is whole for
// all of them. It must be called before Complete, and for a font added to the
// objects of an existing PDF, before the objects are added to the PDF.
func (font *Font) SetSubset(subset bool) *Font {
	if font.program != nil {
		font.program.whole = !subset
	}
	return font
}

// shareFontProgram gives the font the program of a font the PDF already has
// from the same file, if any, or else a new one of the program given.
func shareFontProgram(pdf *PDF, font *Font, program []byte, cff, forbidden bool, glyphs int) {
	for _, f := range pdf.fonts {
		if f.program != nil && f.name == font.name && f.checksum == font.checksum {
			font.program = f.program
			return
		}
	}
	font.program = newFontProgram(glyphs)
	font.program.font = program
	font.program.cff = cff
	font.program.forbidden = forbidden
}

// addEmbeddedFonts writes the embedded fonts at Complete, when every glyph
// they draw is known: each font program, its descriptor, its CID font and its
// ToUnicode map once, and the Type0 font of each Font under the number its
// pages refer to.
func (pdf *PDF) addEmbeddedFonts() {
	for _, font := range pdf.fonts {
		if font.program == nil {
			continue
		}
		baseFont := font.name
		var kept []bool
		embedFontProgram(pdf, font, &baseFont, &kept)
		addCIDSetObject(pdf, font, kept)
		addFontDescriptorObject(pdf, font, baseFont)
		addCIDFontDictionaryObject(pdf, font, baseFont, kept)
		addToUnicodeCMapObject(pdf, font, kept)

		pdf.setObjOffset(font.objNumber, pdf.byteCount)
		pdf.appendInteger(font.objNumber)
		pdf.appendString(" 0 obj\n")
		pdf.appendString("<<\n")
		pdf.appendString("/Type /Font\n")
		pdf.appendString("/Subtype /Type0\n")
		pdf.appendString("/BaseFont /")
		pdf.appendString(baseFont)
		pdf.appendString("\n")
		pdf.appendString("/Encoding /Identity-H\n")
		pdf.appendString("/DescendantFonts [")
		pdf.appendInteger(font.cidFontDictObjNumber)
		pdf.appendString(" 0 R]\n")
		pdf.appendString("/ToUnicode ")
		pdf.appendInteger(font.toUnicodeCMapObjNumber)
		pdf.appendString(" 0 R\n")
		pdf.appendString(">>\n")
		pdf.endObj()
	}
	// The programs are no longer needed.
	for _, font := range pdf.fonts {
		font.program = nil
	}
}

// embedFontProgram writes the font program, a subset unless it is to be
// whole, and gives the name of the font, with the tag of a subset, and the
// glyphs the subset keeps. A font of the same program embedded before is not
// written again: its name and object numbers are used.
func embedFontProgram(pdf *PDF, font *Font, baseFont *string, kept *[]bool) {
	for _, f := range pdf.fonts {
		if f == font {
			break
		}
		if f.fileObjNumber != 0 && f.name == font.name && f.checksum == font.checksum {
			font.fileObjNumber = f.fileObjNumber
			font.cidSetObjNumber = f.cidSetObjNumber
			*baseFont = f.baseFont
			font.baseFont = f.baseFont
			return
		}
	}

	program := font.program
	data, glyphs, name := embeddedProgram(font)
	compressed := deflateFontProgram(data)
	length := len(data)
	*kept = glyphs
	*baseFont = name
	font.baseFont = name

	metadataObjNumber := pdf.addMetadataObject(font.info, true)
	if pdf.encryption != nil {
		compressed = pdf.encryption.encrypt(compressed)
	}
	pdf.newObj()
	pdf.appendString("<<\n")
	if program.cff {
		pdf.appendString("/Subtype /CIDFontType0C\n")
	}
	pdf.appendString("/Filter /FlateDecode\n")
	if !program.cff {
		pdf.appendString("/Length1 ")
		pdf.appendInteger(length)
		pdf.appendString("\n")
	}
	if metadataObjNumber != 0 {
		pdf.appendString("/Metadata ")
		pdf.appendInteger(metadataObjNumber)
		pdf.appendString(" 0 R\n")
	}
	pdf.appendString("/Length ")
	pdf.appendInteger(len(compressed))
	pdf.appendString("\n")
	pdf.appendString(">>\n")
	pdf.appendString("stream\n")
	pdf.appendByteArray(compressed)
	pdf.appendString("\nendstream\n")
	pdf.endObj()
	font.fileObjNumber = pdf.getObjNumber()
}

// embeddedProgram returns the font program to embed: a subset, unless it is to
// be whole, with the glyphs it keeps and the name of the font with the tag of
// the subset, or else the program whole, with nil and the font's name.
func embeddedProgram(font *Font) ([]byte, []bool, string) {
	program := font.program
	data := program.font
	if !program.whole && !program.forbidden {
		var subset []byte
		var glyphs []bool
		var err error
		if program.cff {
			subset, glyphs, err = subsetCFF(data, program.used.bools())
		} else {
			subset, glyphs, err = subsetTrueType(data, program.used.bools())
		}
		if err == nil {
			return subset, glyphs, subsetTag(font.checksum, glyphs) + "+" + font.name
		}
	}
	if program.cff {
		// Whole, written again for the identity charset of a CID-keyed font,
		// or as it is when it cannot be.
		if whole, _, err := subsetCFF(data, nil); err == nil {
			data = whole
		}
	}
	return data, nil, font.name
}

// addCIDSetObject writes, for PDF/A-1, which asks it of a subset, the CIDSet
// of the glyphs the subset keeps: a bit for each glyph number, the first in
// the high bit of the first byte.
func addCIDSetObject(pdf *PDF, font *Font, kept []bool) {
	if kept == nil || font.cidSetObjNumber != 0 ||
		(pdf.compliance != compliance.PDF_A_1A && pdf.compliance != compliance.PDF_A_1B) {
		return
	}
	bits := make([]byte, (len(kept)+7)/8)
	for gid, keep := range kept {
		if keep {
			bits[gid/8] |= 0x80 >> (gid % 8)
		}
	}
	compressed := deflateFontProgram(bits)
	if pdf.encryption != nil {
		compressed = pdf.encryption.encrypt(compressed)
	}
	pdf.newObj()
	pdf.appendString("<<\n")
	pdf.appendString("/Filter /FlateDecode\n")
	pdf.appendString("/Length ")
	pdf.appendInteger(len(compressed))
	pdf.appendString("\n")
	pdf.appendString(">>\n")
	pdf.appendString("stream\n")
	pdf.appendByteArray(compressed)
	pdf.appendString("\nendstream\n")
	pdf.endObj()
	font.cidSetObjNumber = pdf.getObjNumber()
}

// deflateFontProgram compresses a font program or a ToUnicode CMap with a
// writer of the pool that page content uses, at the same level, rather than
// a new writer of about a megabyte for each: 38% of what a page in three
// fonts allocated (10 October 2026).
func deflateFontProgram(buf []byte) []byte {
	return compressor.Deflate(buf)
}

// subsetTag returns the six capitals that begin the name of a subset, from
// the font and the glyphs it keeps, so that another subset of the font has
// another tag, and the same document the same one, in the four ports.
func subsetTag(checksum uint64, kept []bool) string {
	hash := checksum
	for gid, keep := range kept {
		if keep {
			hash = foldChecksum(hash, gid)
		}
	}
	tag := make([]byte, 6)
	for i := range tag {
		tag[i] = byte('A' + hash%26)
		hash /= 26
	}
	return string(tag)
}

var errNotSubset = errors.New("the font cannot be subset")

// subsetTables are the tables a subset keeps: those a PDF reader draws a
// TrueType font with (ISO 32000-1, 9.9), and the cmap, OS/2, name and post
// tables, which some readers and checkers look at. The tables of shaping and
// of vertical text, which PDFjet reads from the font and the PDF does not
// need, are left out, and so is the DSIG table, the signature of the whole
// font.
var subsetTables = map[string]bool{
	"head": true, "hhea": true, "hmtx": true, "maxp": true, "loca": true, "glyf": true,
	"cvt ": true, "fpgm": true, "prep": true, "gasp": true,
	"cmap": true, "OS/2": true, "name": true, "post": true,
}

// subsetTrueType returns the TrueType font with the outlines of the glyphs
// not used emptied, and the glyphs kept: those used, glyph 0 and the parts of
// the composite glyphs kept. Every glyph keeps its number, so the tables kept,
// subsetTables, are copied as they are, but for the glyf and loca tables made
// again, the head table's checksum adjustment, and the post table without the
// names of the glyphs. A font that forbids subsetting in the fsType of
// its OS/2 table, or whose tables cannot be read, gives an error, and is
// embedded whole.
func subsetTrueType(ttf []byte, used []bool) ([]byte, []bool, error) {
	u16 := func(at int) (int, bool) {
		if at < 0 || at+2 > len(ttf) {
			return 0, false
		}
		return int(ttf[at])<<8 | int(ttf[at+1]), true
	}
	// A value past 2^31 - 1 is refused, as an int of 32 bits could not hold
	// it (the review of 9 October 2026: a 32-bit build panicked on one)
	u32 := func(at int) (int, bool) {
		if at < 0 || at+4 > len(ttf) {
			return 0, false
		}
		v := uint32(ttf[at])<<24 | uint32(ttf[at+1])<<16 | uint32(ttf[at+2])<<8 | uint32(ttf[at+3])
		if v > math.MaxInt32 {
			return 0, false
		}
		return int(v), true
	}

	type table struct {
		tag            string
		offset, length int
	}
	numTables, ok := u16(4)
	if !ok {
		return nil, nil, errNotSubset
	}
	tables := make([]table, 0, numTables)
	var head, loca, glyf, maxp, os2 table
	seen := make(map[string]bool, numTables)
	for i := 0; i < numTables; i++ {
		entry := 12 + 16*i
		offset, ok1 := u32(entry + 8)
		length, ok2 := u32(entry + 12)
		if !ok1 || !ok2 || offset+length > len(ttf) {
			return nil, nil, errNotSubset
		}
		// A tag twice, as two head tables, one of them too short to copy,
		// is not a font to subset (the review of 9 October 2026)
		tag := string(ttf[entry : entry+4])
		if seen[tag] {
			return nil, nil, errNotSubset
		}
		seen[tag] = true
		tables = append(tables, table{tag, offset, length})
	}
	for _, t := range tables {
		switch t.tag {
		case "head":
			head = t
		case "loca":
			loca = t
		case "glyf":
			glyf = t
		case "maxp":
			maxp = t
		case "OS/2":
			os2 = t
		}
	}
	// A table the font lacks is of length 0.
	if head.length < 54 || maxp.length < 6 || loca.length == 0 || glyf.length == 0 {
		return nil, nil, errNotSubset
	}
	if os2.length >= 10 {
		if fsType, _ := u16(os2.offset + 8); fsType&0x0100 != 0 {
			return nil, nil, errNotSubset // No subsetting
		}
	}
	numGlyphs, _ := u16(maxp.offset + 4)
	longOffsets, _ := u16(head.offset + 50)
	if numGlyphs == 0 || longOffsets > 1 || loca.length < (numGlyphs+1)*(2+2*longOffsets) {
		return nil, nil, errNotSubset
	}
	glyphAt := func(gid int) (int, int, bool) {
		var start, end int
		if longOffsets == 1 {
			var ok1, ok2 bool
			start, ok1 = u32(loca.offset + 4*gid)
			end, ok2 = u32(loca.offset + 4*gid + 4)
			if !ok1 || !ok2 {
				return 0, 0, false
			}
		} else {
			start, _ = u16(loca.offset + 2*gid)
			end, _ = u16(loca.offset + 2*gid + 2)
			start, end = 2*start, 2*end
		}
		if start > end || end > glyf.length {
			return 0, 0, false
		}
		return glyf.offset + start, glyf.offset + end, true
	}

	// The glyphs kept, with the parts of the composite glyphs.
	kept := make([]bool, numGlyphs)
	stack := []int{}
	for gid := 0; gid < numGlyphs && gid < len(used); gid++ {
		if used[gid] || gid == 0 {
			kept[gid] = true
			stack = append(stack, gid)
		}
	}
	for len(stack) > 0 {
		gid := stack[len(stack)-1]
		stack = stack[:len(stack)-1]
		start, end, ok := glyphAt(gid)
		if !ok {
			return nil, nil, errNotSubset
		}
		if end-start < 10 {
			continue // No outline
		}
		contours, _ := u16(start)
		if contours < 0x8000 {
			continue // A simple glyph
		}
		at := start + 10
		for {
			flags, ok1 := u16(at)
			component, ok2 := u16(at + 2)
			if !ok1 || !ok2 || at+4 > end || component >= numGlyphs {
				return nil, nil, errNotSubset
			}
			if !kept[component] {
				kept[component] = true
				stack = append(stack, component)
			}
			at += 4
			if flags&0x0001 != 0 { // ARG_1_AND_2_ARE_WORDS
				at += 4
			} else {
				at += 2
			}
			if flags&0x0008 != 0 { // WE_HAVE_A_SCALE
				at += 2
			} else if flags&0x0040 != 0 { // WE_HAVE_AN_X_AND_Y_SCALE
				at += 4
			} else if flags&0x0080 != 0 { // WE_HAVE_A_TWO_BY_TWO
				at += 8
			}
			if flags&0x0020 == 0 { // MORE_COMPONENTS
				break
			}
		}
	}

	// The glyf and loca tables, the kept glyphs copied, each padded to a
	// multiple of four bytes, the others empty.
	var newGlyf []byte
	newLoca := make([]byte, 0, (numGlyphs+1)*(2+2*longOffsets))
	appendOffset := func(offset int) {
		if longOffsets == 1 {
			newLoca = append(newLoca, byte(offset>>24), byte(offset>>16), byte(offset>>8), byte(offset))
		} else {
			newLoca = append(newLoca, byte(offset>>9), byte(offset>>1))
		}
	}
	for gid := 0; gid < numGlyphs; gid++ {
		appendOffset(len(newGlyf))
		if kept[gid] {
			start, end, _ := glyphAt(gid)
			newGlyf = append(newGlyf, ttf[start:end]...)
			for len(newGlyf)%4 != 0 {
				newGlyf = append(newGlyf, 0)
			}
			// Glyphs whose outlines overlap, each copied again, could make
			// the subset larger than the whole font many times over (the
			// review of 9 October 2026: 112 KB into 384 MB); a subset is never
			// larger than the glyf table and the padding of each glyph.
			if len(newGlyf) > glyf.length+3*numGlyphs {
				return nil, nil, errNotSubset
			}
		}
	}
	appendOffset(len(newGlyf))
	if longOffsets == 0 && len(newGlyf) > 0x1FFFE {
		return nil, nil, errNotSubset
	}

	// The font again, its tables in the order of their tags.
	sort.Slice(tables, func(i, j int) bool { return tables[i].tag < tables[j].tag })
	data := make([][]byte, 0, len(tables))
	tags := make([]string, 0, len(tables))
	for _, t := range tables {
		switch t.tag {
		case "glyf":
			data = append(data, newGlyf)
		case "loca":
			data = append(data, newLoca)
		case "head":
			h := append([]byte(nil), ttf[t.offset:t.offset+t.length]...)
			h[8], h[9], h[10], h[11] = 0, 0, 0, 0 // checkSumAdjustment, set below
			data = append(data, h)
		case "post":
			if t.length < 32 {
				continue
			}
			// Version 3, without the names of the glyphs.
			p := append([]byte(nil), ttf[t.offset:t.offset+32]...)
			p[0], p[1], p[2], p[3] = 0, 3, 0, 0
			data = append(data, p)
		default:
			if !subsetTables[t.tag] {
				continue
			}
			data = append(data, ttf[t.offset:t.offset+t.length])
		}
		tags = append(tags, t.tag)
	}
	count := len(data)
	searchRange, entrySelector := 1, 0
	for searchRange*2 <= count {
		searchRange *= 2
		entrySelector++
	}
	out := make([]byte, 0, 12+16*count+len(ttf))
	be16 := func(v int) { out = append(out, byte(v>>8), byte(v)) }
	be32 := func(v uint32) { out = append(out, byte(v>>24), byte(v>>16), byte(v>>8), byte(v)) }
	out = append(out, ttf[0:4]...)
	be16(count)
	be16(16 * searchRange)
	be16(entrySelector)
	be16(16*count - 16*searchRange)
	offset := 12 + 16*count
	headAt := 0
	for i, d := range data {
		out = append(out, tags[i]...)
		be32(tableChecksum(d))
		be32(uint32(offset))
		be32(uint32(len(d)))
		if tags[i] == "head" {
			headAt = offset
		}
		offset += (len(d) + 3) &^ 3
	}
	for _, d := range data {
		out = append(out, d...)
		for len(out)%4 != 0 {
			out = append(out, 0)
		}
	}
	adjustment := 0xB1B0AFBA - tableChecksum(out)
	out[headAt+8] = byte(adjustment >> 24)
	out[headAt+9] = byte(adjustment >> 16)
	out[headAt+10] = byte(adjustment >> 8)
	out[headAt+11] = byte(adjustment)
	return out, kept, nil
}

// tableChecksum is the sum of the big-endian 32-bit words of the data, the
// last one padded with zeros.
func tableChecksum(data []byte) uint32 {
	var sum uint32
	for i := 0; i < len(data); i += 4 {
		var word uint32
		for j := 0; j < 4; j++ {
			word <<= 8
			if i+j < len(data) {
				word |= uint32(data[i+j])
			}
		}
		sum += word
	}
	return sum
}
