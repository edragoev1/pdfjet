// fontstream1.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"encoding/hex"
	"io"
	"math"
	"strconv"
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/internal/decompressor"
	"github.com/edragoev1/pdfjet/v9/src/internal/token"
)

// fontStream1 is used to add stream fonts to the PDF.
func fontStream1(pdf *PDF, font *Font, reader io.Reader) {
	getFontData(font, reader)
	embedFontFile(pdf, font, reader)
	addFontDescriptorObject(pdf, font)
	addCIDFontDictionaryObject(pdf, font)
	addToUnicodeCMapObject(pdf, font)

	// Type0 Font Dictionary
	pdf.newObj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /Font\n")
	pdf.appendString("/Subtype /Type0\n")
	pdf.appendString("/BaseFont /")
	pdf.appendString(font.name)
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

	font.objNumber = pdf.getObjNumber()
	pdf.fonts = append(pdf.fonts, font)
}

func embedFontFile(pdf *PDF, font *Font, reader io.Reader) {
	// Check if the font file is already embedded
	for _, f := range pdf.fonts {
		if f.fileObjNumber != 0 && f.name == font.name {
			font.fileObjNumber = f.fileObjNumber
			return
		}
	}

	metadataObjNumber := pdf.addMetadataObject(font.info, true)

	pdf.newObj()
	pdf.appendString("<<\n")

	pdf.appendString("/Metadata ")
	pdf.appendInteger(metadataObjNumber)
	pdf.appendString(" 0 R\n")

	if font.cff {
		pdf.appendString("/Subtype /CIDFontType0C\n")
	} else {
		pdf.appendString("/Length1 ")
		pdf.appendInteger(font.uncompressedSize)
		pdf.appendString("\n")
	}
	pdf.appendString("/Filter /FlateDecode\n")

	var encrypted []byte
	compressed := readBytes(reader, font.compressedSize)
	if pdf.encryption != nil {
		encrypted = pdf.encryption.encrypt(compressed)
	}

	pdf.appendString("/Length ")
	if pdf.encryption != nil {
		pdf.appendInteger(len(encrypted))
	} else {
		pdf.appendInteger(font.compressedSize)
	}
	pdf.appendString("\n")
	pdf.appendString(">>\n")
	pdf.appendString("stream\n")
	if pdf.encryption != nil {
		pdf.appendByteArray(encrypted)
	} else {
		pdf.appendByteArray(compressed)
	}
	pdf.appendString("\nendstream\n")
	pdf.endObj()

	font.fileObjNumber = pdf.getObjNumber()
}

func addFontDescriptorObject(pdf *PDF, font *Font) {
	for _, f := range pdf.fonts {
		if f.fontDescriptorObjNumber != 0 && f.name == font.name {
			font.fontDescriptorObjNumber = f.fontDescriptorObjNumber
			return
		}
	}

	pdf.newObj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /FontDescriptor\n")
	pdf.appendString("/FontName /")
	pdf.appendString(font.name)
	pdf.appendString("\n")
	if font.cff {
		pdf.appendString("/FontFile3 ")
	} else {
		pdf.appendString("/FontFile2 ")
	}
	pdf.appendInteger(font.fileObjNumber)
	pdf.appendString(" 0 R\n")
	pdf.appendString("/Flags 32\n")
	pdf.appendString("/FontBBox [")
	pdf.appendInteger(toGlyphSpace(font.bBoxLLx, font.unitsPerEm))
	pdf.appendString(" ")
	pdf.appendInteger(toGlyphSpace(font.bBoxLLy, font.unitsPerEm))
	pdf.appendString(" ")
	pdf.appendInteger(toGlyphSpace(font.bBoxURx, font.unitsPerEm))
	pdf.appendString(" ")
	pdf.appendInteger(toGlyphSpace(font.bBoxURy, font.unitsPerEm))
	pdf.appendString("]\n")
	pdf.appendString("/Ascent ")
	pdf.appendInteger(toGlyphSpace(font.fontAscent, font.unitsPerEm))
	pdf.appendString("\n")
	pdf.appendString("/Descent ")
	pdf.appendInteger(toGlyphSpace(font.fontDescent, font.unitsPerEm))
	pdf.appendString("\n")
	pdf.appendString("/ItalicAngle 0\n")
	pdf.appendString("/CapHeight ")
	pdf.appendInteger(toGlyphSpace(font.capHeight, font.unitsPerEm))
	pdf.appendString("\n")
	pdf.appendString("/StemV 79\n")
	pdf.appendString(">>\n")
	pdf.endObj()

	font.fontDescriptorObjNumber = pdf.getObjNumber()
}

func addToUnicodeCMapObject(pdf *PDF, font *Font) {
	for _, f := range pdf.fonts {
		if f.toUnicodeCMapObjNumber != 0 && f.name == font.name {
			font.toUnicodeCMapObjNumber = f.toUnicodeCMapObjNumber
			return
		}
	}

	var sb strings.Builder
	sb.WriteString("/CIDInit /ProcSet findresource begin\n")
	sb.WriteString("12 dict begin\n")
	sb.WriteString("begincmap\n")
	sb.WriteString("/CIDSystemInfo <</Registry (Adobe) /Ordering (Identity) /Supplement 0>> def\n")
	sb.WriteString("/CMapName /Adobe-Identity def\n")
	sb.WriteString("/CMapType 2 def\n")

	sb.WriteString("1 begincodespacerange\n")
	sb.WriteString("<0000> <FFFF>\n")
	sb.WriteString("endcodespacerange\n")

	list := make([]string, 0)
	// A character the font does not contain is drawn with the .notdef glyph.
	// PDF/UA requires every glyph to map to Unicode, so map it to the
	// replacement character.
	list = append(list, "<0000> <FFFD>\n")
	var buf strings.Builder
	unicodeOf := unicodeOfGlyphs(font.unicodeToGID)
	for cid := 0; cid <= 0xffff; cid++ {
		gid := font.unicodeToGID[cid]
		if gid > 0 && unicodeOf[gid] == cid {
			buf.WriteString("<")
			buf.WriteString(toHexString(gid))
			buf.WriteString("> <")
			// A presentation form that the Bidi class puts in maps to the letters it stands for.
			if letters := lettersOf(rune(cid)); letters != nil {
				for _, letter := range letters {
					buf.WriteString(toHexString(int(letter)))
				}
			} else {
				buf.WriteString(toHexString(cid))
			}
			buf.WriteString(">\n")
			list = append(list, buf.String())
			buf.Reset()
			if len(list) == 100 {
				writeListTo(&sb, list)
				list = nil
			}
		}
	}
	if len(list) > 0 {
		writeListTo(&sb, list)
		list = nil
	}

	sb.WriteString("endcmap\n")
	sb.WriteString("CMapName currentdict /CMap defineresource pop\n")
	sb.WriteString("end\nend")

	buf2 := []byte(sb.String())
	if pdf.encryption != nil {
		buf2 = pdf.encryption.encrypt(buf2)
	}

	pdf.newObj()
	pdf.appendString("<<\n")
	pdf.appendString("/Length ")
	pdf.appendInteger(len(buf2))
	pdf.appendString("\n")
	pdf.appendString(">>\n")
	pdf.appendString("stream\n")
	pdf.appendByteArray(buf2)
	pdf.appendString("\nendstream\n")
	pdf.endObj()

	font.toUnicodeCMapObjNumber = pdf.getObjNumber()
}

func addCIDFontDictionaryObject(pdf *PDF, font *Font) {
	for _, f := range pdf.fonts {
		if f.cidFontDictObjNumber != 0 && f.name == font.name {
			font.cidFontDictObjNumber = f.cidFontDictObjNumber
			return
		}
	}

	pdf.newObj()
	pdf.appendByteArray(token.BeginDictionary)
	pdf.appendString("/Type /Font\n")
	if font.cff {
		pdf.appendString("/Subtype /CIDFontType0\n")
	} else {
		pdf.appendString("/Subtype /CIDFontType2\n")
	}
	pdf.appendString("/BaseFont /")
	pdf.appendString(font.name)
	pdf.appendByte(token.Newline)

	registry := []byte("Adobe")
	ordering := []byte("Identity")
	if pdf.encryption != nil {
		registry = pdf.encryption.encrypt(registry)
		ordering = pdf.encryption.encrypt(ordering)
	}
	pdf.appendString("/CIDSystemInfo <</Registry <")
	pdf.appendString(hex.EncodeToString(registry))
	pdf.appendString("> /Ordering <")
	pdf.appendString(hex.EncodeToString(ordering))
	pdf.appendString("> /Supplement 0>>\n")

	pdf.appendString("/FontDescriptor ")
	pdf.appendInteger(font.fontDescriptorObjNumber)
	pdf.appendByteArray(token.ObjRef)

	k := float32(1000.0) / float32(font.unitsPerEm)

	pdf.appendString("/DW ")
	pdf.appendInteger(int(math.Round(float64(k * float32(font.advanceWidth[0])))))
	pdf.appendString("\n")

	pdf.appendString("/W [0[\n")
	for _, width := range font.advanceWidth {
		pdf.appendInteger(int(math.Round(float64(k * float32(width)))))
		pdf.appendString(" ")
	}
	pdf.appendString("]]\n")

	pdf.appendString("/CIDToGIDMap /Identity\n")
	pdf.appendByteArray(token.EndDictionary)
	pdf.endObj()

	font.cidFontDictObjNumber = pdf.getObjNumber()
}

// unicodeOfGlyphs returns the character that each glyph maps to in a ToUnicode
// CMap. A glyph can stand for several characters, like the space and the
// no-break space, but viewers read a CMap with more than one entry for a glyph
// differently. So a glyph maps to the first character that uses it, unless
// that is one text seldom has and another character uses the glyph too, like
// the modifier letter apostrophe and the right single quotation mark.
func unicodeOfGlyphs(unicodeToGID []int) []int {
	chars := make([]int, 0x10000)
	for i := range chars {
		chars[i] = -1
	}
	for cid := 0; cid <= 0xffff; cid++ {
		gid := unicodeToGID[cid]
		if gid > 0 && (chars[gid] == -1 ||
			(isSeldomText(chars[gid]) && !isSeldomText(cid))) {
			chars[gid] = cid
		}
	}
	return chars
}

// isSeldomText returns true for the soft hyphen, the spacing modifier letters,
// the combining marks, the figure dash, the deprecated angle brackets, the CJK
// and Kangxi radicals, which CJK fonts draw with the glyphs of the ideographs,
// and the private use characters.
func isSeldomText(ch int) bool {
	return ch == 0x00AD ||
		(ch >= 0x02B0 && ch <= 0x036F) ||
		(ch >= 0x1AB0 && ch <= 0x1AFF) ||
		(ch >= 0x1DC0 && ch <= 0x1DFF) ||
		ch == 0x2012 ||
		(ch >= 0x20D0 && ch <= 0x20FF) ||
		(ch >= 0x2329 && ch <= 0x232A) ||
		(ch >= 0x2E80 && ch <= 0x2FDF) ||
		(ch >= 0xE000 && ch <= 0xF8FF) ||
		(ch >= 0xFE20 && ch <= 0xFE2F)
}

func writeListTo(sb *strings.Builder, list []string) {
	sb.WriteString(strconv.Itoa(len(list)))
	sb.WriteString(" beginbfchar\n")
	for _, s := range list {
		sb.WriteString(s)
	}
	sb.WriteString("endbfchar\n")
}

// The most bytes the metrics of a stream font decode to, with its marks
// compressed in them. IBM Plex Sans JP has 180 KB.
const maxFontMetricsLength = 16 * 1024 * 1024

// fontStreamError panics with the message of a stream font that is not valid.
func fontStreamError(what string) {
	panic("Invalid font stream: " + what + ".")
}

// readBytes reads the next length bytes. It reads them as they come, so a
// length that the stream does not have takes no memory.
func readBytes(reader io.Reader, length int) []byte {
	buf, err := io.ReadAll(io.LimitReader(reader, int64(length)))
	if err != nil {
		panic(err)
	}
	if len(buf) != length {
		panic("Unexpected end of stream: expected " + strconv.Itoa(length) + " bytes")
	}
	return buf
}

// isFontName returns true if the name can be written as a PDF name as it is:
// printable ASCII, and none of the characters that end a name or start an
// escape.
func isFontName(name []byte) bool {
	if len(name) == 0 {
		return false
	}
	for _, b := range name {
		if b < 0x21 || b > 0x7E || strings.IndexByte("()<>[]{}/%#", b) != -1 {
			return false
		}
	}
	return true
}

func getFontData(font *Font, reader io.Reader) {
	fontName := readBytes(reader, int(getUint8(reader)))
	if !isFontName(fontName) {
		fontStreamError("the font name")
	}
	font.name = string(fontName)
	font.info = string(readBytes(reader, int(getUint24(reader))))

	inflated, err := decompressor.InflateWithMaxLength(
		readBytes(reader, int(getUint32(reader))), maxFontMetricsLength)
	if err != nil {
		panic(err)
	}

	// unicodeToGID and advanceWidth can each hold up to 0xFFFF entries for
	// a large CJK font, so reading them one uint16 at a time through a
	// bytes.Reader -- and the io.Reader interface indirection that goes
	// with it -- was showing up as real cost. inflated is already a plain
	// []byte in memory, so read straight out of it with a running offset,
	// after checking that the bytes are there.
	pos := 0
	need := func(count, size int) {
		if count < 0 || count > (len(inflated)-pos)/size {
			fontStreamError("the metrics end too soon")
		}
	}
	readInt32 := func() int32 {
		need(1, 4)
		v := int32(inflated[pos])<<24 | int32(inflated[pos+1])<<16 | int32(inflated[pos+2])<<8 | int32(inflated[pos+3])
		pos += 4
		return v
	}
	readLength := func(size int) int {
		v := readInt32()
		need(int(v), size)
		return int(v)
	}
	readUint16 := func() uint16 {
		v := uint16(inflated[pos])<<8 | uint16(inflated[pos+1])
		pos += 2
		return v
	}

	font.unitsPerEm = int(readInt32())
	font.bBoxLLx = int16(readInt32())
	font.bBoxLLy = int16(readInt32())
	font.bBoxURx = int16(readInt32())
	font.bBoxURy = int16(readInt32())
	font.fontAscent = int16(readInt32())
	font.fontDescent = int16(readInt32())
	font.firstChar = readInt32()
	font.lastChar = readInt32()
	font.capHeight = int16(readInt32())
	font.fontUnderlinePosition = int16(readInt32())
	font.fontUnderlineThickness = int16(readInt32())
	// The range OpenType allows; the sizes of the text are divided by it.
	if font.unitsPerEm < 16 || font.unitsPerEm > 16384 {
		fontStreamError("the units per em")
	}
	// A character in the range is looked up in unicodeToGID.
	if font.firstChar < 0 || font.lastChar > 0xFFFF {
		fontStreamError("the first or last character")
	}

	length := readLength(2)
	if length == 0 {
		fontStreamError("no advance widths")
	}
	font.advanceWidth = make([]uint16, length)
	for i := 0; i < length; i++ {
		font.advanceWidth[i] = readUint16()
	}

	length = readLength(2)
	if length != 0x10000 {
		fontStreamError("the character map")
	}
	font.unicodeToGID = make([]int, length)
	for i := 0; i < length; i++ {
		font.unicodeToGID[i] = int(readUint16())
	}

	// Where the GPOS table of the font puts the marks, compressed on its own
	// after the metrics of a stream that has them. It is kept as it is and
	// read when a mark is drawn in the font; see readMarks. A font with no
	// marks has none, or 0 bytes of them.
	if pos < len(inflated) {
		length := readLength(1)
		if length > 0 {
			font.markData = append([]byte(nil), inflated[pos:pos+length]...)
		}
		pos += length
	}
	// The line gap of a font that has one follows the marks, where a library
	// that does not read it stops.
	if pos < len(inflated) {
		font.fontLineGap = int16(readInt32())
	}

	flag := getUint8(reader)
	if flag == 'R' {
		// The tables of an OpenType font that are not in its CFF data,
		// which keep the font whole; they are not embedded.
		length := int64(getUint32(reader))
		if n, _ := io.CopyN(io.Discard, reader, length); n != length {
			panic("Unexpected end of stream: expected " + strconv.FormatInt(length, 10) + " bytes")
		}
		flag = getUint8(reader)
	}
	font.cff = flag == 'Y'

	font.uncompressedSize = int(getUint32(reader))
	font.compressedSize = int(getUint32(reader))
}

// readMarks reads where the marks go, which the stream font keeps compressed:
// for each MarkToBase and MarkToLigature subtable the class and anchor of each
// mark and the anchors of each letter, and then the offsets of the marks that
// go on other marks. It is done once, the first time a mark is drawn.
func readMarks(font *Font) {
	data, err := decompressor.InflateWithMaxLength(font.markData, maxFontMetricsLength)
	if err != nil {
		panic(err)
	}
	pos := 0
	readInt := func() int {
		if len(data)-pos < 4 {
			fontStreamError("the marks end too soon")
		}
		v := int32(data[pos])<<24 | int32(data[pos+1])<<16 | int32(data[pos+2])<<8 | int32(data[pos+3])
		pos += 4
		return int(v)
	}
	// readCount reads the number of entries of size ints that follow.
	readCount := func(size int) int {
		count := readInt()
		if count < 0 || count > (len(data)-pos)/(4*size) {
			fontStreamError("the marks end too soon")
		}
		return count
	}
	subTables := readCount(2)
	markAnchors := make([]map[int][]int, 0, subTables)
	baseAnchors := make([]map[int][]int, 0, subTables)
	for i := 0; i < subTables; i++ {
		count := readCount(4)
		marks := make(map[int][]int, count)
		for j := 0; j < count; j++ {
			gid := readInt()
			markClass := readInt()
			// The anchors of a letter are 3 ints for each mark class.
			if markClass < 0 || markClass > 0xFFFF {
				fontStreamError("a mark class")
			}
			x := readInt()
			marks[gid] = []int{markClass, x, readInt()}
		}
		count = readCount(2)
		bases := make(map[int][]int, count)
		for j := 0; j < count; j++ {
			gid := readInt()
			anchors := make([]int, readCount(1))
			for k := range anchors {
				anchors[k] = readInt()
			}
			bases[gid] = anchors
		}
		markAnchors = append(markAnchors, marks)
		baseAnchors = append(baseAnchors, bases)
	}
	pairs := readCount(4)
	markToMarkOffsets := make(map[int][2]int, pairs)
	for j := 0; j < pairs; j++ {
		other := readInt()
		mark := readInt()
		dx := readInt()
		markToMarkOffsets[(other<<16)|mark] = [2]int{dx, readInt()}
	}
	font.markAnchors = markAnchors
	font.baseAnchors = baseAnchors
	font.markToMarkOffsets = markToMarkOffsets
	font.markData = nil
}
