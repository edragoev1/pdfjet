package pdfjet

/**
 * fontstream2.go
 *
©2025 PDFjet Software

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
	"io"
	"math"
	"strconv"
	"strings"
)

// FontStream2 constructs font object and adds it to the PDF objects slice.
func FontStream2(objects *[]*PDFobj, font *Font, reader io.Reader) {
	getFontData(font, reader)
	embedFontFile2(objects, font, reader)
	addFontDescriptorObject2(objects, font)
	addCIDFontDictionaryObject2(objects, font)
	addToUnicodeCMapObject2(objects, font)

	// Type0 Font Dictionary
	obj := NewPDFobj()
	obj.add("<<")
	obj.add("/Type")
	obj.add("/Font")
	obj.add("/Subtype")
	obj.add("/Type0")
	obj.add("/BaseFont")
	obj.add("/" + font.name)
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
	obj.number = len(*objects) + 1
	*objects = append(*objects, obj)
	font.objNumber = obj.number
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
	sb.WriteString(string([]byte(font.info)))
	sb.WriteString("</rdf:li>\n")
	sb.WriteString("</rdf:Alt>\n")
	sb.WriteString("</xmpRights:UsageTerms>\n")
	sb.WriteString("</rdf:Description>\n")
	sb.WriteString("</rdf:RDF>\n")
	sb.WriteString("</x:xmpmeta>\n")
	sb.WriteString("<?xpacket end=\"w\"?>")

	xml := []byte(sb.String())

	// This is the metadata object
	obj := NewPDFobj()
	obj.add("<<")
	obj.add("/Type")
	obj.add("/Metadata")
	obj.add("/Subtype")
	obj.add("/XML")
	obj.add("/Length")
	obj.add(strconv.Itoa(len(xml)))
	obj.add(">>")
	obj.SetStream(xml)
	obj.number = len(*objects) + 1
	*objects = append(*objects, obj)

	return obj.number
}

func embedFontFile2(objects *[]*PDFobj, font *Font, reader io.Reader) {
	metadataObjNumber := addMetadataObject2(objects, font)

	obj := NewPDFobj()
	obj.add("<<")
	obj.add("/Metadata")
	obj.add(strconv.Itoa(metadataObjNumber))
	obj.add("0")
	obj.add("R")
	obj.add("/Filter")
	obj.add("/FlateDecode")
	obj.add("/Length")
	obj.add(strconv.Itoa(font.compressedSize))
	if font.cff {
		obj.add("/Subtype")
		obj.add("/CIDFontType0C")
	} else {
		obj.add("/Length1")
		obj.add(strconv.Itoa(font.uncompressedSize))
	}
	obj.add(">>")

	buf1 := make([]byte, 0)
	buf2 := make([]byte, 4096) // We need this buffer to be non zero length!
	for {
		n, err := reader.Read(buf2)
		buf1 = append(buf1, buf2[:n]...)
		if err == io.EOF {
			break
		}
	}

	obj.SetStream(buf1)
	obj.number = len(*objects) + 1
	*objects = append(*objects, obj)
	font.fileObjNumber = obj.number
}

func addFontDescriptorObject2(objects *[]*PDFobj, font *Font) {
	obj := NewPDFobj()
	obj.add("<<")
	obj.add("/Type")
	obj.add("/FontDescriptor")
	obj.add("/FontName")
	obj.add("/" + font.name)
	if font.cff {
		obj.add("/FontFile3")
	} else {
		obj.add("/FontFile2")
	}
	obj.add(strconv.Itoa(font.fileObjNumber))
	obj.add("0")
	obj.add("R")
	obj.add("/Flags")
	obj.add("32")
	obj.add("/FontBBox")
	obj.add("[")
	obj.add(strconv.Itoa(int(font.bBoxLLx)))
	obj.add(strconv.Itoa(int(font.bBoxLLy)))
	obj.add(strconv.Itoa(int(font.bBoxURx)))
	obj.add(strconv.Itoa(int(font.bBoxURy)))
	obj.add("]")
	obj.add("/Ascent")
	obj.add(strconv.Itoa(int(font.fontAscent)))
	obj.add("/Descent")
	obj.add(strconv.Itoa(int(font.fontDescent)))
	obj.add("/ItalicAngle")
	obj.add("0")
	obj.add("/CapHeight")
	obj.add(strconv.Itoa(int(font.capHeight)))
	obj.add("/StemV")
	obj.add("79")
	obj.add(">>")
	obj.number = len(*objects) + 1
	*objects = append(*objects, obj)
	font.fontDescriptorObjNumber = obj.number
}

func addToUnicodeCMapObject2(objects *[]*PDFobj, font *Font) {
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

	obj := NewPDFobj()
	obj.add("<<")
	obj.add("/Length")
	obj.add(strconv.Itoa(sb.Len()))
	obj.add(">>")
	obj.SetStream([]byte(sb.String()))
	obj.number = len(*objects) + 1
	*objects = append(*objects, obj)
	font.toUnicodeCMapObjNumber = obj.number
}

func addCIDFontDictionaryObject2(objects *[]*PDFobj, font *Font) {
	obj := NewPDFobj()
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
	obj.add("/" + font.name)
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
	obj.add("/DW")
	obj.add(strconv.Itoa(int(math.Round(float64(k * float32(font.advanceWidth[0]))))))
	obj.add("/W")
	obj.add("[")
	obj.add("0")
	obj.add("[")
	for i := 0; i < len(font.advanceWidth); i++ {
		obj.add(strconv.Itoa(int(math.Round(float64(k * float32(font.advanceWidth[i]))))))
	}
	obj.add("]")
	obj.add("]")

	obj.add("/CIDToGIDMap")
	obj.add("/Identity")
	obj.add(">>")
	obj.number = len(*objects) + 1
	*objects = append(*objects, obj)
	font.cidFontDictObjNumber = obj.number
}
