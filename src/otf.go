// otf.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"io"
	"strings"
	"unicode/utf16"

	"github.com/edragoev1/pdfjet/v9/src/content"
	"github.com/edragoev1/pdfjet/v9/src/internal/utf8text"
)

// fontTable is used to construct font table objects.
type fontTable struct {
	name     string
	checkSum uint32
	offset   int
	length   int
}

// openTypeFont is used to construct TTF and OTF font objects.
type openTypeFont struct {
	fontName           string
	fontInfo           string
	buf                []byte
	index              int
	unitsPerEm         int
	bBoxLLx            int16
	bBoxLLy            int16
	bBoxURx            int16
	bBoxURy            int16
	ascent             int16
	descent            int16
	lineGap            int16
	firstChar          rune
	lastChar           rune
	capHeight          int16
	hasCapHeight       bool
	indexToLocFormat   int
	loca               *fontTable
	glyf               *fontTable
	postVersion        uint32
	italicAngle        uint32
	underlinePosition  int16
	underlineThickness int16
	advanceWidth       []uint16
	unicodeToGID       *glyphMap
	gpos               *fontTable // Read when a mark is first drawn, by gpos
	cff                bool
	cffOff             int
	cffLen             int
	format             int
	count              int
	stringOffset       int
	numGlyphs          int // Of the maxp table, or 0 when the font has none
	fsType             int // Of the OS/2 table: what the license allows
}

// fontError panics with the message of a font file that is not valid.
func fontError(what string) {
	panic("Invalid font file: " + what + ".")
}

// readFontData returns the bytes of a font from the reader. A reader that
// says how many bytes it has left, as a bytes.Reader does, is read into a
// buffer of that size, once: io.ReadAll grows its buffer as it reads, which
// copied a font of 200 KB several times, about half of what a document of
// three embedded fonts allocated in all.
func readFontData(reader io.Reader) []byte {
	if sized, ok := reader.(interface{ Len() int }); ok {
		var buf bytes.Buffer
		buf.Grow(sized.Len() + bytes.MinRead)
		if _, err := buf.ReadFrom(reader); err != nil {
			panic(err)
		}
		return buf.Bytes()
	}
	return content.GetFromStream(reader)
}

// newOpenTypeFont is the constructor for TTF and OTF fonts.
func newOpenTypeFont(buf []byte) *openTypeFont {
	otf := new(openTypeFont)
	otf.buf = buf
	otf.unicodeToGID = new(glyphMap)
	// A font without an OS/2 table does not say which characters it holds,
	// so its character map is read for all of them.
	otf.firstChar = 0
	otf.lastChar = 0xFFFF

	// Extract the OTF metadata
	version := readUint32(otf)
	if version == 0x00010000 || // Win OTF
		version == 0x74727565 || // Mac TTF
		version == 0x4F54544F { // CFF OTF
		// We should be able to read this font.
	} else {
		fontError("not an OpenType or TrueType font: PDFjet reads .otf and .ttf fonts")
	}

	numOfTables := int(readUint16(otf))
	readUint16(otf) // Skip the search range.
	readUint16(otf) // Skip the entry selector.
	readUint16(otf) // Skip the range shift.

	var cmapTable *fontTable
	var hmtxTable *fontTable
	for i := 0; i < numOfTables; i++ {
		table := new(fontTable)
		table.name = string(readNBytes(otf, 4))
		table.checkSum = readUint32(otf)
		table.offset = int(readUint32(otf))
		table.length = int(readUint32(otf))

		k := otf.index // Save the current index
		switch table.name {
		case "head":
			getHeadTable(otf, table)
		case "hhea":
			getHheaTable(otf, table)
		case "OS/2":
			getOs2Table(otf, table)
		case "name":
			getNameTable(otf, table)
		case "hmtx":
			hmtxTable = table
		case "post":
			getPostTable(otf, table)
		case "CFF ":
			getCffTable(otf, table)
		case "GPOS":
			otf.gpos = table
		case "maxp":
			otf.numGlyphs, _ = otf.tableUint16(table, 4)
		case "cmap":
			cmapTable = table
		case "loca":
			otf.loca = table
		case "glyf":
			otf.glyf = table
		}
		otf.index = k // Restore the index
	}

	// The hmtx table is read after the hhea table, which says how many
	// advance widths it has, whatever their order in the directory: listed
	// first, its widths were left unread, every glyph 0 wide.
	if hmtxTable != nil {
		getHmtxTable(otf, hmtxTable)
	}

	// This table must be processed last
	if cmapTable == nil {
		fontError("no character map")
	}
	getCmapTable(otf, cmapTable)

	// A font without the cap height of a version 2 OS/2 table has the top of
	// its H, as the glyph is drawn, or else its ascent: the cap height goes
	// into the font descriptor, where a reader fits text in a box with it.
	if !otf.hasCapHeight {
		otf.capHeight = otf.ascent
		if top, ok := otf.glyphTop(otf.unicodeToGID.get('H')); ok {
			otf.capHeight = top
		}
	}

	// The sizes of the text are divided by the units per em, and the width of
	// every glyph past the advance widths is that of the last one.
	if otf.unitsPerEm < 16 || otf.unitsPerEm > 16384 {
		fontError("the units per em")
	}
	if len(otf.advanceWidth) == 0 {
		fontError("no advance widths")
	}
	// The name goes into the PDF as the name of the font.
	if !isFontName([]byte(otf.fontName)) {
		fontError("the font name")
	}

	return otf
}

// compress returns the font program as it is embedded, compressed: the CFF
// table of a font with CFF outlines, or else the whole font. It is compressed
// only for a font the PDF does not hold yet.
func getHeadTable(otf *openTypeFont, table *fontTable) {
	otf.index = table.offset + 16
	_ = readUint16(otf) // Skip the flags
	otf.unitsPerEm = int(readUint16(otf))
	otf.index += 16
	otf.bBoxLLx = readInt16(otf)
	otf.bBoxLLy = readInt16(otf)
	otf.bBoxURx = readInt16(otf)
	otf.bBoxURy = readInt16(otf)
	otf.indexToLocFormat, _ = otf.tableUint16(table, 50)
}

func getHheaTable(otf *openTypeFont, table *fontTable) {
	otf.index = table.offset + 4
	otf.ascent = readInt16(otf)
	otf.descent = readInt16(otf)
	otf.lineGap = readInt16(otf)
	otf.index += 24
	otf.advanceWidth = make([]uint16, readUint16(otf))
}

func getOs2Table(otf *openTypeFont, table *fontTable) {
	otf.fsType, _ = otf.tableUint16(table, 8)
	otf.index = table.offset + 64
	otf.firstChar = rune(readUint16(otf))
	otf.lastChar = rune(readUint16(otf))
	// sCapHeight is in the table from its version 2 on. Where it would be in
	// a version 0 or 1 table are the bytes after the table, often those of
	// the next table.
	if version, ok := otf.tableUint16(table, 0); ok && version >= 2 {
		if capHeight, ok := otf.tableUint16(table, 88); ok {
			otf.capHeight = int16(capHeight)
			otf.hasCapHeight = true
		}
	}
}

// glyphTop returns the top of the bounding box of the glyph, the yMax of its
// header in the glyf table, at the offset the loca table gives it. It returns
// false for a font with CFF outlines, which has no glyf table, for glyph 0,
// and for a glyph with no outline, which has no header.
func (otf *openTypeFont) glyphTop(gid int) (int16, bool) {
	if gid == 0 || otf.loca == nil || otf.glyf == nil {
		return 0, false
	}
	var start, end int
	var ok1, ok2 bool
	if otf.indexToLocFormat == 0 {
		// The short offsets are half the offsets.
		start, ok1 = otf.tableUint16(otf.loca, 2*gid)
		end, ok2 = otf.tableUint16(otf.loca, 2*gid+2)
		start, end = 2*start, 2*end
	} else {
		start, ok1 = otf.tableUint32(otf.loca, 4*gid)
		end, ok2 = otf.tableUint32(otf.loca, 4*gid+4)
	}
	// The header of a glyph is 10 bytes: the number of contours, then xMin,
	// yMin, xMax and yMax.
	if !ok1 || !ok2 || end-start < 10 {
		return 0, false
	}
	yMax, ok := otf.tableUint16(otf.glyf, start+8)
	return int16(yMax), ok
}

// maxFontInfoBytes is the most of the name records' bytes kept as the font's information, of
// each platform: a font of 593 KB whose 40,000 records name one string of
// 65,000 bytes was gigabytes of text, and a smaller one put 65 million
// characters in the XMP of every PDF that used it (review of 17 October
// 2026). A font's copyright and license are a few KB.
const maxFontInfoBytes = 65536

func getNameTable(otf *openTypeFont, table *fontTable) {
	otf.index = table.offset
	otf.format = int(readUint16(otf))
	otf.count = int(readUint16(otf))
	otf.stringOffset = int(readUint16(otf))
	var macFontInfo strings.Builder
	var winFontInfo strings.Builder
	// The bytes of the records of each, up to maxFontInfoBytes
	macBytes, winBytes := 0, 0

	for r := 0; r < otf.count; r++ {
		platformID := readUint16(otf)
		encodingID := readUint16(otf)
		languageID := readUint16(otf)
		nameID := readUint16(otf)
		length := int(readUint16(otf))
		offset := int(readUint16(otf))
		index1 := table.offset + otf.stringOffset + offset
		if index1 < 0 || length > len(otf.buf)-index1 {
			continue // A name record outside the font is left out.
		}
		buffer := otf.buf[index1 : index1+length]

		if platformID == 1 && encodingID == 0 && languageID == 0 {
			// Macintosh
			if nameID == 6 {
				otf.fontName = utf8text.Decode(buffer)
			} else if macBytes+length <= maxFontInfoBytes {
				macBytes += length
				// This record's own decoded text, not otf.fontName (which
				// may not even be set yet - name records aren't guaranteed
				// to arrive in nameID order).
				macFontInfo.WriteString(utf8text.Decode(buffer))
				macFontInfo.WriteString("\n")
			}
		} else if platformID == 3 && encodingID == 1 && languageID == 0x409 {
			// Windows
			w := make([]uint16, len(buffer)/2)
			for i := 0; i < len(w); i++ {
				w[i] = uint16(buffer[2*i])<<8 | uint16(buffer[2*i+1])
			}
			str := string(utf16.Decode(w))
			if nameID == 6 {
				otf.fontName = str
			} else if winBytes+length <= maxFontInfoBytes {
				winBytes += length
				winFontInfo.WriteString(str)
				winFontInfo.WriteString("\n")
			}
		}
	}
	otf.fontInfo = winFontInfo.String()
	if otf.fontInfo == "" {
		otf.fontInfo = macFontInfo.String()
	}
}

func getCmapTable(otf *openTypeFont, table *fontTable) {
	otf.index = table.offset
	tableOffset := otf.index
	otf.index += 2
	numRecords := int(readUint16(otf))

	// Process the encoding records: the format 4 subtable of the Windows
	// platform, and its format 12 subtable, which maps the characters past the
	// Basic Multilingual Plane too.
	format4subtable := false
	subtableOffset := 0
	format12Offset := -1
	for i := 0; i < numRecords; i++ {
		platformID := readUint16(otf)
		encodingID := readUint16(otf)
		offset := int(readUint32(otf))
		if platformID == 3 && encodingID == 1 && !format4subtable {
			format4subtable = true
			subtableOffset = offset
		} else if platformID == 3 && encodingID == 10 && format12Offset == -1 {
			format12Offset = offset
		}
	}
	if !format4subtable && format12Offset == -1 {
		panic("Format 4 subtable not found in this font.")
	}
	if format4subtable {
		getCmapFormat4(otf, tableOffset+subtableOffset)
	}
	if format12Offset != -1 {
		getCmapFormat12(otf, table, format12Offset)
	}
}

// getCmapFormat12 maps the characters of the Basic Multilingual Plane that the
// format 4 subtable left without a glyph, from the format 12 subtable at the
// offset in the table. IBM Plex Sans TC, as a .ttf, has an empty format 4
// subtable and maps every character in its format 12 subtable. The groups go
// up and do not overlap, and a group that goes back is passed over, so that
// the map is read once at most.
func getCmapFormat12(otf *openTypeFont, table *fontTable, offset int) {
	if format, ok := otf.tableUint16(table, offset); !ok || format != 12 {
		return
	}
	numGroups, ok := otf.tableUint32(table, offset+12)
	if !ok || numGroups > (table.length-offset-16)/12 {
		return
	}
	next := int(otf.firstChar)
	for i := 0; i < numGroups; i++ {
		group := offset + 16 + 12*i
		first, ok1 := otf.tableUint32(table, group)
		end, ok2 := otf.tableUint32(table, group+4)
		startGlyph, ok3 := otf.tableUint32(table, group+8)
		if !ok1 || !ok2 || !ok3 {
			continue // Past the end of the font
		}
		// The glyphs are counted from the group's first character, even
		// where the characters before next are passed over (the review of
		// 9 October 2026: they were counted from next)
		start := max(first, next)
		if end > int(otf.lastChar) {
			end = int(otf.lastChar)
		}
		for ch := start; ch <= end; ch++ {
			if gid := startGlyph + (ch - first); gid < 0x10000 {
				otf.unicodeToGID.setIfAbsent(rune(ch), gid)
			}
		}
		if end+1 > next {
			next = end + 1
		}
	}
}

// getCmapFormat4 reads the format 4 subtable that begins at subtable.
func getCmapFormat4(otf *openTypeFont, subtable int) {
	otf.index = subtable

	if format := readUint16(otf); format != 4 {
		fontError("the character map is not format 4")
	}
	tableLen := readUint16(otf)
	readUint16(otf) // Skip the language
	segCount := int(readUint16(otf) / 2)

	otf.index += 6 // Skip to the endCount[]
	endCount := make([]uint16, segCount)
	for i := 0; i < segCount; i++ {
		endCount[i] = readUint16(otf)
	}

	otf.index += 2 // Skip the reservedPad
	startCount := make([]uint16, segCount)
	for i := 0; i < segCount; i++ {
		startCount[i] = readUint16(otf)
	}

	idDelta := make([]uint16, segCount)
	for i := 0; i < segCount; i++ {
		idDelta[i] = readUint16(otf)
	}

	idRangeOffset := make([]uint16, segCount)
	for i := 0; i < segCount; i++ {
		idRangeOffset[i] = readUint16(otf)
	}

	// The glyph ID array is the rest of the subtable, after its header and
	// the four arrays of the segments. A length that leaves none of it is
	// read as none, not as an array of a negative size.
	glyphIDLen := (int(tableLen) - (16 + 8*segCount)) / 2
	if glyphIDLen < 0 {
		glyphIDLen = 0
	}
	glyphIDArray := make([]uint16, glyphIDLen)
	for i := 0; i < len(glyphIDArray); i++ {
		glyphIDArray[i] = readUint16(otf)
	}

	// The segments are in the order of their end codes, so the segment of a
	// character is the first that ends at it or after it, if it starts at it
	// or before it. The characters go up, and so does the segment: each
	// segment has the characters after those of the segments before it, up to
	// its end, and the characters between segments are passed over, not looked
	// at one by one.
	next := int(otf.firstChar) // The first character no segment has had yet
	for seg := 0; seg < segCount && next <= int(otf.lastChar); seg++ {
		if int(endCount[seg]) < next {
			continue
		}
		end := min(int(endCount[seg]), int(otf.lastChar))
		for ch := max(next, int(startCount[seg])); ch <= end; ch++ {
			gid := 0
			offset := int(idRangeOffset[seg])
			if offset == 0 {
				// Per spec this is unsigned 16-bit modulo arithmetic.
				// idDelta is read as unsigned here (readUint16), so the sum
				// is always non-negative and % 65536 already gives the
				// right answer, but use & 0xFFFF to spell out the intended
				// unsigned-16-bit wraparound explicitly (matches the other
				// language ports and doesn't rely on that coincidence).
				gid = (int(idDelta[seg]) + int(ch)) & 0xFFFF
			} else {
				offset /= 2
				offset -= segCount - seg
				index := offset + (int(ch) - int(startCount[seg]))
				if index < 0 || index >= len(glyphIDArray) {
					// The segment points outside the glyph ID array, so it
					// gives this character no glyph.
					continue
				}
				gid = int(glyphIDArray[index])
				if gid != 0 {
					// idDelta[seg] % 65536 alone is a no-op (idDelta is
					// already < 65536) and never wraps the actual sum, so a
					// large gid + idDelta could overflow past the valid
					// 16-bit glyph ID range uncorrected. Wrap the *sum*
					// instead.
					gid = (gid + int(idDelta[seg])) & 0xFFFF
				}
			}
			otf.unicodeToGID.set(rune(ch), gid)
		}
		next = int(endCount[seg]) + 1
	}
}

func getHmtxTable(otf *openTypeFont, table *fontTable) {
	otf.index = table.offset
	for i := 0; i < len(otf.advanceWidth); i++ {
		otf.advanceWidth[i] = readUint16(otf)
		otf.index += 2
	}
}

func getPostTable(otf *openTypeFont, table *fontTable) {
	otf.index = table.offset
	otf.postVersion = readUint32(otf)
	otf.italicAngle = readUint32(otf)
	otf.underlinePosition = readInt16(otf)
	otf.underlineThickness = readInt16(otf)
}

func getCffTable(otf *openTypeFont, table *fontTable) {
	if table.offset < 0 || table.length < 0 || table.length > len(otf.buf)-table.offset {
		fontError("the CFF table is not in the font")
	}
	otf.cff = true
	otf.cffOff = table.offset
	otf.cffLen = table.length
}

// uint16At returns the 16 bits at the offset, or 0 outside the font data.
func (otf *openTypeFont) uint16At(offset int) int {
	if offset < 0 || offset+2 > len(otf.buf) {
		return 0
	}
	return int(otf.buf[offset])<<8 | int(otf.buf[offset+1])
}

// tableUint16 returns the 16 bits at the offset in the table, and false when
// the table or the font ends before them: a table can be shorter than its
// version says, and the directory can point past the end of the font.
func (otf *openTypeFont) tableUint16(table *fontTable, at int) (int, bool) {
	if at < 0 || table.offset < 0 || table.offset > len(otf.buf) || table.length < 0 ||
		at > table.length-2 || at > len(otf.buf)-table.offset-2 {
		return 0, false
	}
	return otf.uint16At(table.offset + at), true
}

// tableUint32 returns the 32 bits at the offset in the table, as tableUint16
// returns 16.
func (otf *openTypeFont) tableUint32(table *fontTable, at int) (int, bool) {
	high, ok1 := otf.tableUint16(table, at)
	low, ok2 := otf.tableUint16(table, at+2)
	return high<<16 | low, ok1 && ok2
}

// need panics unless the next count bytes of the font are there. The index
// runs past the end when a table of the directory points outside the font.
func need(otf *openTypeFont, count int) {
	if otf.index < 0 || count > len(otf.buf)-otf.index {
		fontError("the font ends too soon")
	}
}

func readInt16(otf *openTypeFont) int16 {
	need(otf, 2)
	value := int16(otf.buf[otf.index]) << 8
	otf.index++
	value |= int16(otf.buf[otf.index])
	otf.index++
	return value
}

func readUint16(otf *openTypeFont) uint16 {
	need(otf, 2)
	value := uint16(otf.buf[otf.index]) << 8
	otf.index++
	value |= uint16(otf.buf[otf.index])
	otf.index++
	return value
}

func readUint32(otf *openTypeFont) uint32 {
	need(otf, 4)
	value := uint32(otf.buf[otf.index]) << 24
	otf.index++
	value |= uint32(otf.buf[otf.index]) << 16
	otf.index++
	value |= uint32(otf.buf[otf.index]) << 8
	otf.index++
	value |= uint32(otf.buf[otf.index])
	otf.index++
	return value
}

func readNBytes(otf *openTypeFont, n int) []byte {
	need(otf, n)
	buf := make([]byte, 0)
	for i := 0; i < n; i++ {
		buf = append(buf, otf.buf[otf.index])
		otf.index++
	}
	return buf
}
