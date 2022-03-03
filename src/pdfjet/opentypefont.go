package pdfjet

/**
 * opentypefont.go
 *
Copyright 2020 Innovatics Inc.
*/

import (
	"io"
    "math"
	"strings"
)

func registerOpenTypeFont(pdf *PDF, font *Font, reader io.Reader) {
	otf := NewOTF(reader)

	font.name = otf.fontName
	font.firstChar = otf.firstChar
	font.lastChar = otf.lastChar
	font.unicodeToGID = otf.unicodeToGID
	font.unitsPerEm = otf.unitsPerEm
	font.bBoxLLx = otf.bBoxLLx
	font.bBoxLLy = otf.bBoxLLy
	font.bBoxURx = otf.bBoxURx
	font.bBoxURy = otf.bBoxURy
	font.advanceWidth = otf.advanceWidth
	font.glyphWidth = otf.glyphWidth
	font.fontAscent = otf.ascent
	font.fontDescent = otf.descent
	font.fontUnderlinePosition = otf.underlinePosition
	font.fontUnderlineThickness = otf.underlineThickness
	font.SetSize(font.size)

	embedOpenTypeFontFile(pdf, font, otf)
	addOpenTypeFontDescriptorObject(pdf, font, otf)
	addOpenTypeFontCIDFontDictionaryObject(pdf, font, otf)
	addOpenTypeFontToUnicodeCMapObject(pdf, font, otf)

	// Type0 Font Dictionary
	pdf.newobj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /Font\n")
	pdf.appendString("/Subtype /Type0\n")
	pdf.appendString("/BaseFont /")
	pdf.appendString(otf.fontName)
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

func embedOpenTypeFontFile(pdf *PDF, font *Font, otf *OTF) {
	// Check if the font file is already embedded
	for _, f := range pdf.fonts {
		if f.fileObjNumber != 0 && f.name == otf.fontName {
			font.fileObjNumber = f.fileObjNumber
			return
		}
	}

	metadataObjNumber := pdf.addMetadataObject(otf.fontInfo, true)

	pdf.newobj()
	pdf.appendString("<<\n")
	if otf.cff {
		pdf.appendString("/Subtype /CIDFontType0C\n")
	}
	pdf.appendString("/Filter /FlateDecode\n")

	pdf.appendString("/Length ")
	pdf.appendInteger(otf.compressed.Len()) // The compressed size
	pdf.appendString("\n")

	if !otf.cff {
		pdf.appendString("/Length1 ")
		pdf.appendInteger(len(otf.buf)) // The uncompressed size
		pdf.appendString("\n")
	}

	if metadataObjNumber != 0 {
		pdf.appendString("/Metadata ")
		pdf.appendInteger(metadataObjNumber)
		pdf.appendString(" 0 R\n")
	}

	pdf.appendString(">>\n")
	pdf.appendString("stream\n")
	pdf.appendByteArray(otf.compressed.Bytes())
	pdf.appendString("\nendstream\n")
	pdf.endobj()

	font.fileObjNumber = pdf.getObjNumber()
}

func addOpenTypeFontDescriptorObject(pdf *PDF, font *Font, otf *OTF) {
	for _, f := range pdf.fonts {
		if f.fontDescriptorObjNumber != 0 && f.name == otf.fontName {
			font.fontDescriptorObjNumber = f.fontDescriptorObjNumber
			return
		}
	}

	pdf.newobj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /FontDescriptor\n")
	pdf.appendString("/FontName /")
	pdf.appendString(otf.fontName)
	pdf.appendString("\n")
	if otf.cff {
		pdf.appendString("/FontFile3 ")
	} else {
		pdf.appendString("/FontFile2 ")
	}
	pdf.appendInteger(font.fileObjNumber)
	pdf.appendString(" 0 R\n")
	pdf.appendString("/Flags 32\n")
	pdf.appendString("/FontBBox [")
	pdf.appendInteger(int(otf.bBoxLLx))
	pdf.appendString(" ")
	pdf.appendInteger(int(otf.bBoxLLy))
	pdf.appendString(" ")
	pdf.appendInteger(int(otf.bBoxURx))
	pdf.appendString(" ")
	pdf.appendInteger(int(otf.bBoxURy))
	pdf.appendString("]\n")
	pdf.appendString("/Ascent ")
	pdf.appendInteger(int(otf.ascent))
	pdf.appendString("\n")
	pdf.appendString("/Descent ")
	pdf.appendInteger(int(otf.descent))
	pdf.appendString("\n")
	pdf.appendString("/ItalicAngle 0\n")
	pdf.appendString("/CapHeight ")
	pdf.appendInteger(int(otf.capHeight))
	pdf.appendString("\n")
	pdf.appendString("/StemV 79\n")
	pdf.appendString(">>\n")
	pdf.endobj()

	font.fontDescriptorObjNumber = pdf.getObjNumber()
}

func addOpenTypeFontToUnicodeCMapObject(pdf *PDF, font *Font, otf *OTF) {
	for _, f := range pdf.fonts {
		if f.toUnicodeCMapObjNumber != 0 && f.name == otf.fontName {
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
		gid := otf.unicodeToGID[cid]
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

func addOpenTypeFontCIDFontDictionaryObject(pdf *PDF, font *Font, otf *OTF) {
	for _, f := range pdf.fonts {
		if f.cidFontDictObjNumber != 0 && f.name == otf.fontName {
			font.cidFontDictObjNumber = f.cidFontDictObjNumber
			return
		}
	}

	pdf.newobj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /Font\n")
	if otf.cff {
		pdf.appendString("/Subtype /CIDFontType0\n")
	} else {
		pdf.appendString("/Subtype /CIDFontType2\n")
	}
	pdf.appendString("/BaseFont /")
	pdf.appendString(otf.fontName)
	pdf.appendString("\n")
	pdf.appendString("/CIDSystemInfo <</Registry (Adobe) /Ordering (Identity) /Supplement 0>>\n")
	pdf.appendString("/FontDescriptor ")
	pdf.appendInteger(font.fontDescriptorObjNumber)
	pdf.appendString(" 0 R\n")

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
	pdf.appendString(">>\n")
	pdf.endobj()

	font.cidFontDictObjNumber = pdf.getObjNumber()
}
