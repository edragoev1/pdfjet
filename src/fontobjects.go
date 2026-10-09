// fontobjects.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// Floating point: every float multiplication in this file is wrapped in
// float32(...) or float64(...) on purpose, so that Go's arm64 compiler does
// not fuse it with an addition and round otherwise than amd64 and the other
// ports. Keep the wrapping; see "Floating point on ARM" in README.md.
// check-no-fma.sh fails if one is removed.

// The objects of a font added to an existing PDF. They are numbered when the
// font is added, so that pages can refer to it, and filled in when the objects
// are added to the PDF, after the pages are drawn: the font program a subset of
// the glyphs drawn, as a font of a new PDF is at Complete.

import (
	"math"
	"strconv"
	"strings"
)

// fontObjects are the objects of a font added to an existing PDF, empty until
// the objects are added to the PDF.
type fontObjects struct {
	metadata   int // The number of the font's metadata object
	file       *PDFobj
	descriptor *PDFobj
	cidFont    *PDFobj
	toUnicode  *PDFobj
	type0      *PDFobj
}

// addFontToObjects numbers the objects of the font, added to the objects of an
// existing PDF; the Type0 font, the one pages refer to, is the last.
func addFontToObjects(objects *[]*PDFobj, font *Font) {
	objs := &fontObjects{metadata: addMetadataObject2(objects, font)}
	objs.file = appendEmptyObject(objects)
	objs.descriptor = appendEmptyObject(objects)
	objs.cidFont = appendEmptyObject(objects)
	objs.toUnicode = appendEmptyObject(objects)
	objs.type0 = appendEmptyObject(objects)
	objs.type0.font = font
	font.fileObjNumber = objs.file.number
	font.fontDescriptorObjNumber = objs.descriptor.number
	font.cidFontDictObjNumber = objs.cidFont.number
	font.toUnicodeCMapObjNumber = objs.toUnicode.number
	font.objNumber = objs.type0.number
	font.objects = objs
}

func appendEmptyObject(objects *[]*PDFobj) *PDFobj {
	obj := newPDFobj()
	obj.number = len(*objects) + 1
	*objects = append(*objects, obj)
	return obj
}

// completeFontObjects fills in the objects of the fonts added to the objects,
// when the pages are drawn and the glyphs they use are known.
func completeFontObjects(objects []*PDFobj) {
	for _, obj := range objects {
		if obj.font != nil {
			obj.font.completeObjects()
			obj.font = nil
		}
	}
}

// completeObjects fills in the objects of the font: its program, a subset of
// the glyphs drawn unless it is to be whole, the descriptor and the CID font
// under the name of the subset, the widths and the ToUnicode map of the glyphs
// it keeps, and the Type0 font.
func (font *Font) completeObjects() {
	objs := font.objects
	program, kept, baseFont := embeddedProgram(font)
	font.baseFont = baseFont

	compressed := deflateFontProgram(program)
	obj := objs.file
	obj.add("<<")
	obj.add("/Metadata")
	obj.add(strconv.Itoa(objs.metadata))
	obj.add("0")
	obj.add("R")
	obj.add("/Filter")
	obj.add("/FlateDecode")
	obj.add("/Length")
	obj.add(strconv.Itoa(len(compressed)))
	if font.cff {
		obj.add("/Subtype")
		obj.add("/CIDFontType0C")
	} else {
		obj.add("/Length1")
		obj.add(strconv.Itoa(len(program)))
	}
	obj.add(">>")
	obj.setStream(compressed)

	completeFontDescriptor(objs.descriptor, font, baseFont)
	completeCIDFontDictionary(objs.cidFont, font, baseFont, kept)

	cmap := deflateFontProgram([]byte(toUnicodeCMap(font, kept)))
	obj = objs.toUnicode
	obj.add("<<")
	obj.add("/Filter")
	obj.add("/FlateDecode")
	obj.add("/Length")
	obj.add(strconv.Itoa(len(cmap)))
	obj.add(">>")
	obj.setStream(cmap)

	obj = objs.type0
	obj.add("<<")
	obj.add("/Type")
	obj.add("/Font")
	obj.add("/Subtype")
	obj.add("/Type0")
	obj.add("/BaseFont")
	obj.add("/" + baseFont)
	obj.add("/Encoding")
	obj.add("/Identity-H")
	obj.add("/DescendantFonts")
	obj.add("[")
	obj.add(strconv.Itoa(font.cidFontDictObjNumber))
	obj.add("0")
	obj.add("R")
	obj.add("]")
	obj.add("/ToUnicode")
	obj.add(strconv.Itoa(font.toUnicodeCMapObjNumber))
	obj.add("0")
	obj.add("R")
	obj.add(">>")

	// The program and the objects are no longer needed.
	font.program = nil
	font.objects = nil
}

func addMetadataObject2(objects *[]*PDFobj, font *Font) int {
	var sb strings.Builder
	sb.WriteString("<?xpacket id=\"W5M0MpCehiHzreSzNTczkc9d\"?>\n")
	sb.WriteString("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\">\n")
	sb.WriteString("<rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">\n")
	sb.WriteString("<rdf:Description rdf:about=\"\" xmlns:xmpRights=\"http://ns.adobe.com/xap/1.0/rights/\">\n")
	sb.WriteString("<xmpRights:UsageTerms>\n")
	sb.WriteString("<rdf:Alt>\n")
	sb.WriteString("<rdf:li xml:lang=\"x-default\">\n")
	sb.WriteString(escapeXML(font.info))
	sb.WriteString("</rdf:li>\n")
	sb.WriteString("</rdf:Alt>\n")
	sb.WriteString("</xmpRights:UsageTerms>\n")
	sb.WriteString("</rdf:Description>\n")
	sb.WriteString("</rdf:RDF>\n")
	sb.WriteString("</x:xmpmeta>\n")
	sb.WriteString("<?xpacket end=\"r\"?>")

	xml := []byte(sb.String())

	// This is the metadata object
	obj := newPDFobj()
	obj.add("<<")
	obj.add("/Type")
	obj.add("/Metadata")
	obj.add("/Subtype")
	obj.add("/XML")
	obj.add("/Length")
	obj.add(strconv.Itoa(len(xml)))
	obj.add(">>")
	obj.setStream(xml)
	obj.number = len(*objects) + 1
	*objects = append(*objects, obj)

	return obj.number
}

func completeFontDescriptor(obj *PDFobj, font *Font, fontName string) {
	obj.add("<<")
	obj.add("/Type")
	obj.add("/FontDescriptor")
	obj.add("/FontName")
	obj.add("/" + fontName)
	if font.cff {
		obj.add("/FontFile3")
	} else {
		obj.add("/FontFile2")
	}
	obj.add(strconv.Itoa(font.fileObjNumber))
	obj.add("0")
	obj.add("R")
	obj.add("/Flags")
	obj.add(strconv.Itoa(flagsOf(font.italicAngle)))
	obj.add("/FontBBox")
	obj.add("[")
	obj.add(strconv.Itoa(toGlyphSpace(font.bBoxLLx, font.unitsPerEm)))
	obj.add(strconv.Itoa(toGlyphSpace(font.bBoxLLy, font.unitsPerEm)))
	obj.add(strconv.Itoa(toGlyphSpace(font.bBoxURx, font.unitsPerEm)))
	obj.add(strconv.Itoa(toGlyphSpace(font.bBoxURy, font.unitsPerEm)))
	obj.add("]")
	obj.add("/Ascent")
	obj.add(strconv.Itoa(toGlyphSpace(font.fontAscent, font.unitsPerEm)))
	obj.add("/Descent")
	obj.add(strconv.Itoa(toGlyphSpace(font.fontDescent, font.unitsPerEm)))
	obj.add("/ItalicAngle")
	obj.add(italicAngleOf(font.italicAngle))
	obj.add("/CapHeight")
	obj.add(strconv.Itoa(toGlyphSpace(font.capHeight, font.unitsPerEm)))
	obj.add("/StemV")
	obj.add("79")
	obj.add(">>")
}

func completeCIDFontDictionary(obj *PDFobj, font *Font, baseFont string, kept []bool) {
	obj.add("<<")
	obj.add("/Type")
	obj.add("/Font")
	obj.add("/Subtype")
	if font.cff {
		obj.add("/CIDFontType0")
	} else {
		obj.add("/CIDFontType2")
	}
	obj.add("/BaseFont")
	obj.add("/" + baseFont)
	obj.add("/CIDSystemInfo")
	obj.add("<<")
	obj.add("/Registry")
	obj.add("(Adobe)")
	obj.add("/Ordering")
	obj.add("(Identity)")
	obj.add("/Supplement")
	obj.add("0")
	obj.add(">>")
	obj.add("/FontDescriptor")
	obj.add(strconv.Itoa(font.fontDescriptorObjNumber))
	obj.add("0")
	obj.add("R")

	k := float32(1000.0) / float32(font.unitsPerEm)
	// The width of the glyphs past the /W array: those past the advance
	// widths, which have the width of the last one.
	obj.add("/DW")
	obj.add(strconv.Itoa(int(math.Round(float64(float32(k * float32(font.advanceWidth[len(font.advanceWidth)-1])))))))
	obj.add("/W")
	obj.add(widthsArray(font, kept))
	obj.add("/CIDToGIDMap")
	obj.add("/Identity")
	obj.add(">>")
}
