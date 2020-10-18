package pdfjet

/**
 * fontstream1.go
 *
Copyright 2020 Innovatics Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

import (
	"bytes"
	"decompressor"
	"io"
	"strconv"
	"strings"
)

// FontStream1 is used to add stream fonts to the PDF.
func FontStream1(pdf *PDF, font *Font, reader io.Reader) {
	readFontData(font, reader)
	embedFontFile(pdf, font, reader)
	addFontDescriptorObject(pdf, font)
	addCIDFontDictionaryObject(pdf, font)
	addToUnicodeCMapObject(pdf, font)

	// Type0 Font Dictionary
	pdf.newobj()
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
	pdf.endobj()

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

	pdf.newobj()
	pdf.appendString("<<\n")

	pdf.appendString("/Metadata ")
	pdf.appendInteger(metadataObjNumber)
	pdf.appendString(" 0 R\n")

	if font.cff {
		pdf.appendString("/Subtype /CIDFontType0C\n")
	}
	pdf.appendString("/Filter /FlateDecode\n")
	pdf.appendString("/Length ")
	pdf.appendInteger(font.compressedSize)
	pdf.appendString("\n")

	if !font.cff {
		pdf.appendString("/Length1 ")
		pdf.appendInteger(font.uncompressedSize)
		pdf.appendString("\n")
	}

	pdf.appendString(">>\n")
	pdf.appendString("stream\n")

	buf := make([]byte, 4096) // We need this buffer to be non zero length!
	for {
		n, err := reader.Read(buf)
		pdf.appendByteArray(buf[:n])
		if err == io.EOF {
			break
		}
	}

	pdf.appendString("\nendstream\n")
	pdf.endobj()

	font.fileObjNumber = pdf.getObjNumber()
}

func addFontDescriptorObject(pdf *PDF, font *Font) {
	for _, f := range pdf.fonts {
		if f.fontDescriptorObjNumber != 0 && f.name == font.name {
			font.fontDescriptorObjNumber = f.fontDescriptorObjNumber
			return
		}
	}

	pdf.newobj()
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
	pdf.appendInteger(int(font.bBoxLLx))
	pdf.appendString(" ")
	pdf.appendInteger(int(font.bBoxLLy))
	pdf.appendString(" ")
	pdf.appendInteger(int(font.bBoxURx))
	pdf.appendString(" ")
	pdf.appendInteger(int(font.bBoxURy))
	pdf.appendString("]\n")
	pdf.appendString("/Ascent ")
	pdf.appendInteger(int(font.ascent))
	pdf.appendString("\n")
	pdf.appendString("/Descent ")
	pdf.appendInteger(int(font.descent))
	pdf.appendString("\n")
	pdf.appendString("/ItalicAngle 0\n")
	pdf.appendString("/CapHeight ")
	pdf.appendInteger(int(font.capHeight))
	pdf.appendString("\n")
	pdf.appendString("/StemV 79\n")
	pdf.appendString(">>\n")
	pdf.endobj()

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
	var buf strings.Builder
	for cid := 0; cid <= 0xffff; cid++ {
		gid := font.unicodeToGID[cid]
		if gid > 0 {
			buf.WriteString("<")
			buf.WriteString(toHexString(gid))
			buf.WriteString("> <")
			buf.WriteString(toHexString(cid))
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

	pdf.newobj()
	pdf.appendString("<<\n")
	pdf.appendString("/Length ")
	pdf.appendInteger(sb.Len())
	pdf.appendString("\n")
	pdf.appendString(">>\n")
	pdf.appendString("stream\n")
	pdf.appendString(sb.String())
	pdf.appendString("\nendstream\n")
	pdf.endobj()

	font.toUnicodeCMapObjNumber = pdf.getObjNumber()
}

func addCIDFontDictionaryObject(pdf *PDF, font *Font) {
	for _, f := range pdf.fonts {
		if f.cidFontDictObjNumber != 0 && f.name == font.name {
			font.cidFontDictObjNumber = f.cidFontDictObjNumber
			return
		}
	}

	pdf.newobj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /Font\n")
	if font.cff {
		pdf.appendString("/Subtype /CIDFontType0\n")
	} else {
		pdf.appendString("/Subtype /CIDFontType2\n")
	}
	pdf.appendString("/BaseFont /")
	pdf.appendString(font.name)
	pdf.appendString("\n")
	pdf.appendString("/CIDSystemInfo <</Registry (Adobe) /Ordering (Identity) /Supplement 0>>\n")
	pdf.appendString("/FontDescriptor ")
	pdf.appendInteger(font.fontDescriptorObjNumber)
	pdf.appendString(" 0 R\n")
	factor := float32(1000.0) / float32(font.unitsPerEm)
	if len(font.advanceWidth) < 2 {
		pdf.appendString("/DW ")
		pdf.appendInteger(int(factor * float32(font.advanceWidth[0])))
		pdf.appendString("\n")
	} else {
		pdf.appendString("/W [0[\n")
		for _, width := range font.advanceWidth {
			pdf.appendInteger(int(factor * float32(width)))
			pdf.appendString(" ")
		}
		pdf.appendString("]]\n")
	}
	pdf.appendString("/CIDToGIDMap /Identity\n")
	pdf.appendString(">>\n")
	pdf.endobj()

	font.cidFontDictObjNumber = pdf.getObjNumber()
}

func writeListTo(sb *strings.Builder, list []string) {
	sb.WriteString(strconv.Itoa(len(list)))
	sb.WriteString(" beginbfchar\n")
	for _, s := range list {
		sb.WriteString(s)
	}
	sb.WriteString("endbfchar\n")
}

func readFontData(font *Font, reader io.Reader) {
	length := int(getUint8(reader))
	fontName := make([]byte, length)
	io.ReadFull(reader, fontName)
	font.name = string(fontName)

	length = int(getUint24(reader))
	fontInfo := make([]byte, length)
	io.ReadFull(reader, fontInfo)
	font.info = string(fontInfo)

	length = int(getUint32(reader))
	buf := make([]byte, length)
	io.ReadFull(reader, buf)

	inflated := decompressor.Inflate(buf)
	r2 := bytes.NewReader(inflated)

	font.unitsPerEm = int(getInt32(r2))
	font.bBoxLLx = int16(getInt32(r2))
	font.bBoxLLy = int16(getInt32(r2))
	font.bBoxURx = int16(getInt32(r2))
	font.bBoxURy = int16(getInt32(r2))
	font.fontAscent = int16(getInt32(r2))
	font.fontDescent = int16(getInt32(r2))
	font.firstChar = rune(getInt32(r2))
	font.lastChar = rune(getInt32(r2))
	font.capHeight = int16(getInt32(r2))
	font.fontUnderlinePosition = int16(getInt32(r2))
	font.fontUnderlineThickness = int16(getInt32(r2))

	length = int(getUint32(r2))
	font.advanceWidth = make([]uint16, length)
	for i := 0; i < length; i++ {
		font.advanceWidth[i] = getUint16(r2)
	}

	length = int(getUint32(r2))
	font.glyphWidth = make([]uint16, length)
	for i := 0; i < length; i++ {
		font.glyphWidth[i] = getUint16(r2)
	}

	length = int(getUint32(r2))
	font.unicodeToGID = make([]int, length)
	for i := 0; i < length; i++ {
		font.unicodeToGID[i] = int(getUint16(r2))
	}

	font.cff = false
	if getUint8(reader) == 'Y' {
		font.cff = true
	}

	font.uncompressedSize = int(getUint32(reader))
	font.compressedSize = int(getUint32(reader))
}
