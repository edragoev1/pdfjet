// fontstream1.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"encoding/hex"
	"fmt"
	"io"
	"math"
	"strconv"
	"strings"

	"github.com/edragoev1/pdfjet/src/decompressor"
	"github.com/edragoev1/pdfjet/src/encryption"
	"github.com/edragoev1/pdfjet/src/token"
)

// FontStream1 is used to add stream fonts to the PDF.
func FontStream1(pdf *PDF, font *Font, reader io.Reader) {
	getFontData(font, reader)
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
	} else {
		pdf.appendString("/Length1 ")
		pdf.appendInteger(font.uncompressedSize)
		pdf.appendString("\n")
	}
	pdf.appendString("/Filter /FlateDecode\n")

	var compressed []byte
	var encrypted []byte
	compressed, err := io.ReadAll(reader)
	if err != nil {
		fmt.Println("failed to read input:", err)
		return
	}
	if pdf.encryption != nil {
		encrypted, err = encryption.Encrypt(compressed, pdf.encryption.GetKey())
		if err != nil {
			fmt.Println("encryption failed:", err)
			return
		}
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
	pdf.appendInteger(int(font.fontAscent))
	pdf.appendString("\n")
	pdf.appendString("/Descent ")
	pdf.appendInteger(int(font.fontDescent))
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
	// A character the font does not contain is drawn with the .notdef glyph.
	// PDF/UA requires every glyph to map to Unicode, so map it to the
	// replacement character.
	list = append(list, "<0000> <FFFD>\n")
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

	buf2 := []byte(sb.String())
	if pdf.encryption != nil {
		buf2, _ = encryption.Encrypt(buf2, pdf.encryption.GetKey())
	}

	pdf.newobj()
	pdf.appendString("<<\n")
	pdf.appendString("/Length ")
	pdf.appendInteger(len(buf2))
	pdf.appendString("\n")
	pdf.appendString(">>\n")
	pdf.appendString("stream\n")
	pdf.appendByteArray(buf2)
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
		registry, _ = encryption.Encrypt(registry, pdf.encryption.GetKey())
		ordering, _ = encryption.Encrypt(ordering, pdf.encryption.GetKey())
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

func getFontData(font *Font, reader io.Reader) {
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

	inflated, _ := decompressor.Inflate(buf)

	// unicodeToGID and advanceWidth can each hold up to 0xFFFF entries for
	// a large CJK font, so reading them one uint16 at a time through a
	// bytes.Reader -- and the io.Reader interface indirection that goes
	// with it -- was showing up as real cost. inflated is already a plain
	// []byte in memory, so read straight out of it with a running offset.
	pos := 0
	readInt32 := func() int32 {
		v := int32(inflated[pos])<<24 | int32(inflated[pos+1])<<16 | int32(inflated[pos+2])<<8 | int32(inflated[pos+3])
		pos += 4
		return v
	}
	readUint32 := func() uint32 {
		v := uint32(inflated[pos])<<24 | uint32(inflated[pos+1])<<16 | uint32(inflated[pos+2])<<8 | uint32(inflated[pos+3])
		pos += 4
		return v
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

	length = int(readUint32())
	font.advanceWidth = make([]uint16, length)
	for i := 0; i < length; i++ {
		font.advanceWidth[i] = readUint16()
	}

	length = int(readUint32())
	font.unicodeToGID = make([]int, length)
	for i := 0; i < length; i++ {
		font.unicodeToGID[i] = int(readUint16())
	}

	font.cff = false
	if getUint8(reader) == 'Y' {
		font.cff = true
	}

	font.uncompressedSize = int(getUint32(reader))
	font.compressedSize = int(getUint32(reader))
}
