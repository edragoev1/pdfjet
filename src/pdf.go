package pdfjet

/**
 * pdf.go
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
	"bufio"
	"fmt"
	"log"
	"os"
	"sort"
	"strconv"
	"strings"
	"time"
	"unicode"

	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/compressor"
	"github.com/edragoev1/pdfjet/src/djb"
	"github.com/edragoev1/pdfjet/src/token"
)

// PDF is used to create PDF objects.
type PDF struct {
	writer                *bufio.Writer
	byteCount             int
	objOffsets            []int
	fonts                 []*Font
	images                []*Image
	pages                 []*Page
	destinations          map[string]*Destination
	groups                []*OptionalContentGroup
	states                map[string]int
	metadataObjNumber     int
	outputIntentObjNumber int
	compliance            int
	title                 string
	author                string
	subject               string
	keywords              string
	producer              string
	creator               string
	createDate            string
	creationDate          string
	pagesObjNumber        int
	pageLayout            string
	pageMode              string
	language              string
	toc                   *Bookmark
	importedFonts         []string
	extGState             string
	uuid                  string
	prevPage              *Page
}

// NewPDF the constructor.
// Here is the layout of the PDF document:
//
// Metadata Object
// Output Intent Object
// Fonts
// Images
// Resources Object
// Content1
// Content2
// ...
// ContentN
// Annot1
// Annot2
// ...
// AnnotN
// Page1
// Page2
// ...
// PageN
// Pages
// StructElem1
// StructElem2
// ...
// StructElemN
// StructTreeRoot
// Info
// Root
// xref table
// Trailer
/**
 *  Creates a PDF object that represents a PDF document.
 *  Use this constructor to create PDF/A compliant PDF documents.
 *  Please note: PDF/A compliance requires all fonts to be embedded in the PDF.
 *
 *  @param os the associated output stream.
 *  @param compliance must be: compliance.PDF_UA or compliance.PDF_A_1A to compliance.PDF_A_3B
 */
func NewPDF(w *bufio.Writer) *PDF {
	pdf := new(PDF)
	pdf.writer = w
	pdf.producer = "PDFjet v8.0.4"
	pdf.creator = pdf.producer
	pdf.language = "en-US"

	pdf.destinations = make(map[string]*Destination)
	pdf.uuid = djb.Salsa20()

	// createDate format: "yyyy-MM-ddTHH:mm:ss"
	pdf.createDate = time.Now().Format(time.RFC3339)[0:19]

	// creationDate format: "yyyyMMddHHmmss"
	pdf.creationDate =
		strings.ReplaceAll(pdf.createDate[:10], "-", "") +
			strings.ReplaceAll(pdf.createDate[11:], ":", "")

	pdf.states = make(map[string]int)

	pdf.appendString("%PDF-1.5\n")
	pdf.appendString("%")
	pdf.appendByte(0xF2)
	pdf.appendByte(0xF3)
	pdf.appendByte(0xF4)
	pdf.appendByte(0xF5)
	pdf.appendByte(0xF6)
	pdf.appendString("\n")

	return pdf
}

func (pdf *PDF) SetCompliance(compliance int) {
	pdf.compliance = compliance
}

func NewPDFFile(filePath string) *PDF {
	file, err := os.Create(filePath)
	if err != nil {
		log.Fatal(err)
	}
	return NewPDF(bufio.NewWriter(file))
}

func (pdf *PDF) newobj() {
	pdf.objOffsets = append(pdf.objOffsets, pdf.byteCount)
	pdf.appendInteger(len(pdf.objOffsets))
	pdf.appendString(" 0 obj\n")
}

func (pdf *PDF) endobj() {
	pdf.appendString("endobj\n")
}

func (pdf *PDF) getObjNumber() int {
	return len(pdf.objOffsets)
}

func (pdf *PDF) addMetadataObject(notice string, fontMetadataObject bool) int {
	var sb strings.Builder
	sb.WriteString("<?xpacket id=\"W5M0MpCehiHzreSzNTczkc9d\"?>\n")
	sb.WriteString("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"\n")
	sb.WriteString("    x:xmptk=\"Adobe XMP Core 5.4-c005 78.147326, 2012/08/23-13:03:03\">\n")
	sb.WriteString("<rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">\n")

	if fontMetadataObject {
		sb.WriteString("<rdf:Description rdf:about=\"\" xmlns:xmpRights=\"http://ns.adobe.com/xap/1.0/rights/\">\n")
		sb.WriteString("<xmpRights:UsageTerms>\n")
		sb.WriteString("<rdf:Alt>\n")
		sb.WriteString("<rdf:li xml:lang=\"x-default\">\n")
		sb.WriteString(string([]byte(notice)))
		sb.WriteString("</rdf:li>\n")
		sb.WriteString("</rdf:Alt>\n")
		sb.WriteString("</xmpRights:UsageTerms>\n")
		sb.WriteString("</rdf:Description>\n")
	} else {
		sb.WriteString("<rdf:Description rdf:about=\"\"\n")
		sb.WriteString("    xmlns:pdf=\"http://ns.adobe.com/pdf/1.3/\"\n")
		sb.WriteString("    xmlns:pdfaid=\"http://www.aiim.org/pdfa/ns/id/\"\n")
		sb.WriteString("    xmlns:dc=\"http://purl.org/dc/elements/1.1/\"\n")
		sb.WriteString("    xmlns:xmp=\"http://ns.adobe.com/xap/1.0/\"\n")
		sb.WriteString("    xmlns:xapMM=\"http://ns.adobe.com/xap/1.0/mm/\"\n")
		sb.WriteString("    xmlns:pdfuaid=\"http://www.aiim.org/pdfua/ns/id/\">\n")

		sb.WriteString("  <dc:format>application/pdf</dc:format>\n")
		if pdf.compliance == compliance.PDF_UA {
			sb.WriteString("  <pdfuaid:part>1</pdfuaid:part>\n")
		} else if pdf.compliance == compliance.PDF_A_1A {
			sb.WriteString("  <pdfaid:part>1</pdfaid:part>\n")
			sb.WriteString("  <pdfaid:conformance>A</pdfaid:conformance>\n")
		} else if pdf.compliance == compliance.PDF_A_1B {
			sb.WriteString("  <pdfaid:part>1</pdfaid:part>\n")
			sb.WriteString("  <pdfaid:conformance>B</pdfaid:conformance>\n")
		} else if pdf.compliance == compliance.PDF_A_2A {
			sb.WriteString("  <pdfaid:part>2</pdfaid:part>\n")
			sb.WriteString("  <pdfaid:conformance>A</pdfaid:conformance>\n")
		} else if pdf.compliance == compliance.PDF_A_2B {
			sb.WriteString("  <pdfaid:part>2</pdfaid:part>\n")
			sb.WriteString("  <pdfaid:conformance>B</pdfaid:conformance>\n")
		} else if pdf.compliance == compliance.PDF_A_3A {
			sb.WriteString("  <pdfaid:part>3</pdfaid:part>\n")
			sb.WriteString("  <pdfaid:conformance>A</pdfaid:conformance>\n")
		} else if pdf.compliance == compliance.PDF_A_3B {
			sb.WriteString("  <pdfaid:part>3</pdfaid:part>\n")
			sb.WriteString("  <pdfaid:conformance>B</pdfaid:conformance>\n")
		}

		sb.WriteString("  <pdf:Producer>")
		sb.WriteString(pdf.producer)
		sb.WriteString("</pdf:Producer>\n")

		sb.WriteString("  <pdf:Keywords>")
		sb.WriteString(pdf.keywords)
		sb.WriteString("</pdf:Keywords>\n")

		sb.WriteString("  <dc:title><rdf:Alt><rdf:li xml:lang=\"x-default\">")
		sb.WriteString(pdf.title)
		sb.WriteString("</rdf:li></rdf:Alt></dc:title>\n")

		sb.WriteString("  <dc:creator><rdf:Seq><rdf:li>")
		sb.WriteString(pdf.author)
		sb.WriteString("</rdf:li></rdf:Seq></dc:creator>\n")

		sb.WriteString("  <dc:description><rdf:Alt><rdf:li xml:lang=\"x-default\">")
		sb.WriteString(pdf.subject)
		sb.WriteString("</rdf:li></rdf:Alt></dc:description>\n")

		sb.WriteString("  <xmp:CreatorTool>")
		sb.WriteString(pdf.creator)
		sb.WriteString("</xmp:CreatorTool>\n")

		sb.WriteString("  <xmp:CreateDate>")
		sb.WriteString(pdf.createDate + "-05:00") // Append the time zone.
		sb.WriteString("</xmp:CreateDate>\n")

		sb.WriteString("  <xapMM:DocumentID>uuid:")
		sb.WriteString(pdf.uuid)
		sb.WriteString("</xapMM:DocumentID>\n")

		sb.WriteString("  <xapMM:InstanceID>uuid:")
		sb.WriteString(pdf.uuid)
		sb.WriteString("</xapMM:InstanceID>\n")

		sb.WriteString("</rdf:Description>\n")
	}

	if !fontMetadataObject {
		// Add the recommended 2000 bytes padding
		for i := 0; i < 20; i++ {
			for j := 0; j < 10; j++ {
				sb.WriteString("          ")
			}
			sb.WriteString("\n")
		}
	}

	sb.WriteString("</rdf:RDF>\n")
	sb.WriteString("</x:xmpmeta>\n")
	sb.WriteString("<?xpacket end=\"w\"?>")

	xml := []byte(sb.String())
	// This is the metadata object
	pdf.newobj()
	pdf.appendByteArray(token.BeginDictionary)
	pdf.appendString("/Type /Metadata\n")
	pdf.appendString("/Subtype /XML\n")
	pdf.appendByteArray(token.Length)
	pdf.appendInteger(len(xml))
	pdf.appendByteArray(token.Newline)
	pdf.appendByteArray(token.EndDictionary)
	pdf.appendByteArray(token.Stream)
	pdf.appendByteArray(xml)
	pdf.appendByteArray(token.Endstream)
	pdf.endobj()

	return pdf.getObjNumber()
}

func (pdf *PDF) addOutputIntentObject() int {
	pdf.newobj()
	pdf.appendByteArray(token.BeginDictionary)
	pdf.appendString("/N 3\n")

	pdf.appendByteArray(token.Length)
	pdf.appendInteger(len(ICCBlackScaledProfile))
	pdf.appendByteArray(token.Newline)

	pdf.appendString("/Filter /FlateDecode\n")
	pdf.appendByteArray(token.EndDictionary)
	pdf.appendByteArray(token.Stream)
	pdf.appendByteArray(ICCBlackScaledProfile)
	pdf.appendByteArray(token.Endstream)
	pdf.endobj()

	// OutputIntent object
	pdf.newobj()
	pdf.appendByteArray(token.BeginDictionary)
	pdf.appendString("/Type /OutputIntent\n")
	pdf.appendString("/S /GTS_PDFA1\n")
	pdf.appendString("/OutputCondition (sRGB IEC61966-2.1)\n")
	pdf.appendString("/OutputConditionIdentifier (sRGB IEC61966-2.1)\n")
	pdf.appendString("/Info (sRGB IEC61966-2.1)\n")
	pdf.appendString("/DestOutputProfile ")
	pdf.appendInteger(pdf.getObjNumber() - 1)
	pdf.appendByteArray(token.ObjRef)
	pdf.appendByteArray(token.EndDictionary)
	pdf.endobj()

	return pdf.getObjNumber()
}

func (pdf *PDF) addResourcesObject() int {
	pdf.newobj()
	pdf.appendByteArray(token.BeginDictionary)
	if pdf.extGState != "" {
		pdf.appendString(pdf.extGState)
	}
	if len(pdf.fonts) > 0 || len(pdf.importedFonts) > 0 {
		pdf.appendString("/Font\n")
		pdf.appendByteArray(token.BeginDictionary)
		for _, token := range pdf.importedFonts {
			pdf.appendString(token)
			if token == "R" {
				pdf.appendString("\n")
			} else {
				pdf.appendString(" ")
			}
		}
		for _, font := range pdf.fonts {
			pdf.appendString("/F")
			pdf.appendInteger(font.objNumber)
			pdf.appendString(" ")
			pdf.appendInteger(font.objNumber)
			pdf.appendString(" 0 R\n")
		}
		pdf.appendByteArray(token.EndDictionary)
	}
	if len(pdf.images) > 0 {
		pdf.appendString("/XObject\n")
		pdf.appendByteArray(token.BeginDictionary)
		for _, image := range pdf.images {
			pdf.appendString("/Im")
			pdf.appendInteger(image.objNumber)
			pdf.appendString(" ")
			pdf.appendInteger(image.objNumber)
			pdf.appendString(" 0 R\n")
		}
		pdf.appendByteArray(token.EndDictionary)
	}
	if len(pdf.groups) > 0 {
		pdf.appendString("/Properties\n")
		pdf.appendByteArray(token.BeginDictionary)
		for i, ocg := range pdf.groups {
			pdf.appendString("/OC")
			pdf.appendInteger(i + 1)
			pdf.appendString(" ")
			pdf.appendInteger(ocg.objNumber)
			pdf.appendString(" 0 R\n")
		}
		pdf.appendByteArray(token.EndDictionary)
	}
	// String state = "/CA 0.5 /ca 0.5"
	if len(pdf.states) > 0 {
		pdf.appendString("/ExtGState <<\n")
		for key, value := range pdf.states {
			pdf.appendString("/GS")
			pdf.appendInteger(value)
			pdf.appendString(" <<")
			pdf.appendString(key)
			pdf.appendByteArray(token.EndDictionary)
		}
		pdf.appendByteArray(token.EndDictionary)
	}
	pdf.appendByteArray(token.EndDictionary)
	pdf.endobj()
	return pdf.getObjNumber()
}

func (pdf *PDF) addPagesObject() int {
	pdf.newobj()
	pdf.appendByteArray(token.BeginDictionary)
	pdf.appendString("/Type /Pages\n")
	pdf.appendString("/Kids [\n")
	for _, page := range pdf.pages {
		if pdf.compliance == compliance.PDF_UA ||
			pdf.compliance == compliance.PDF_A_1A ||
			pdf.compliance == compliance.PDF_A_1B ||
			pdf.compliance == compliance.PDF_A_2A ||
			pdf.compliance == compliance.PDF_A_2B ||
			pdf.compliance == compliance.PDF_A_3A ||
			pdf.compliance == compliance.PDF_A_3B {
			page.setStructElementsPageObjNumber(page.objNumber)
		}
		pdf.appendInteger(page.objNumber)
		pdf.appendString(" 0 R\n")
	}
	pdf.appendString("]\n")
	pdf.appendString("/Count ")
	pdf.appendInteger(len(pdf.pages))
	pdf.appendByte('\n')
	pdf.appendByteArray(token.EndDictionary)
	pdf.endobj()
	return pdf.getObjNumber()
}

func (pdf *PDF) addInfoObject() int {
	// Add the info object
	pdf.newobj()
	pdf.appendString("<<\n")
	pdf.appendString("/Title (")
	pdf.appendString(pdf.title)
	pdf.appendString(")\n")
	pdf.appendString("/Author (")
	pdf.appendString(pdf.author)
	pdf.appendString(")\n")
	pdf.appendString("/Subject (")
	pdf.appendString(pdf.subject)
	pdf.appendString(")\n")
	pdf.appendString("/Producer (")
	pdf.appendString(pdf.producer)
	pdf.appendString(")\n")
	pdf.appendString("/Creator (")
	pdf.appendString(pdf.creator)
	pdf.appendString(")\n")
	pdf.appendString("/CreationDate (D:")
	pdf.appendString(pdf.creationDate)
	pdf.appendString("-05'00')\n")
	pdf.appendString(">>\n")
	pdf.endobj()
	return pdf.getObjNumber()
}

func (pdf *PDF) addStructTreeRootObject() int {
	pdf.newobj()
	pdf.appendByteArray(token.BeginDictionary)
	pdf.appendString("/Type /StructTreeRoot\n")
	pdf.appendString("/ParentTree ")
	pdf.appendInteger(pdf.getObjNumber() + 1)
	pdf.appendString(" 0 R\n")
	pdf.appendString("/K [\n")
	pdf.appendInteger(pdf.getObjNumber() + 2)
	pdf.appendString(" 0 R\n")
	pdf.appendString("]\n")
	pdf.appendByteArray(token.EndDictionary)
	pdf.endobj()
	return pdf.getObjNumber()
}

func (pdf *PDF) addStructDocumentObject(parent int) int {
	pdf.newobj()
	pdf.appendByteArray(token.BeginDictionary)
	pdf.appendString("/Type /StructElem\n")
	pdf.appendString("/S /Document\n")
	pdf.appendString("/P ")
	pdf.appendInteger(parent)
	pdf.appendByteArray(token.ObjRef)
	pdf.appendString("/K [\n")
	for _, page := range pdf.pages {
		for _, structElement := range page.structures {
			pdf.appendInteger(structElement.objNumber)
			pdf.appendByteArray(token.ObjRef)
		}
	}
	pdf.appendString("]\n")
	pdf.appendByteArray(token.EndDictionary)
	pdf.endobj()
	return pdf.getObjNumber()
}

func (pdf *PDF) addStructElementObjects() {
	structTreeRootObjNumber := pdf.getObjNumber() + 1
	for _, page := range pdf.pages {
		structTreeRootObjNumber += len(page.structures)
	}
	for _, page := range pdf.pages {
		for _, element := range page.structures {
			pdf.newobj()
			element.objNumber = pdf.getObjNumber()
			pdf.appendString("<<\n/Type /StructElem /S /")
			pdf.appendString(element.structure)
			pdf.appendString("\n/P ")
			pdf.appendInteger(structTreeRootObjNumber + 2) // Use the document struct as parent!
			pdf.appendString(" 0 R\n/Pg ")
			pdf.appendInteger(element.pageObjNumber)
			pdf.appendString(" 0 R\n")
			if element.annotation != nil {
				pdf.appendString("/K <</Type /OBJR /Obj ")
				pdf.appendInteger(element.annotation.objNumber)
				pdf.appendString(" 0 R>>")
			} else {
				pdf.appendString("/K ")
				pdf.appendInteger(element.mcid)
			}
			pdf.appendString("\n/Lang (")
			if element.language != "" {
				pdf.appendString(element.language)
			} else {
				pdf.appendString(pdf.language)
			}
			pdf.appendString(")\n/Alt <")
			pdf.appendString(encodeToHex(element.altDescription))
			pdf.appendString(">\n/ActualText <")
			pdf.appendString(encodeToHex(element.actualText))
			pdf.appendString(">\n>>\n")
			pdf.endobj()
		}
	}
}

func encodeToHex(text string) string {
	var buf strings.Builder
	runes := []rune(text)
	buf.WriteString("FEFF")
	if len(runes) > 0 {
		for _, rune := range runes {
			buf.WriteString(fmt.Sprintf("%04X", rune))
		}
	}
	return buf.String()
}

func (pdf *PDF) addNumsParentTree() {
	pdf.newobj()
	pdf.appendString("<<\n")
	pdf.appendString("/Nums [\n")
	for i, page := range pdf.pages {
		pdf.appendInteger(i)
		pdf.appendString(" [\n")
		for _, element := range page.structures {
			if element.annotation == nil {
				pdf.appendInteger(element.objNumber)
				pdf.appendString(" 0 R\n")
			}
		}
		pdf.appendString("]\n")
	}

	index := len(pdf.pages)
	for _, page := range pdf.pages {
		for _, element := range page.structures {
			if element.annotation != nil {
				pdf.appendInteger(index)
				pdf.appendString(" ")
				pdf.appendInteger(element.objNumber)
				pdf.appendString(" 0 R\n")
				index++
			}
		}
	}
	pdf.appendString("]\n")
	pdf.appendString(">>\n")
	pdf.endobj()
}

func (pdf *PDF) addRootObject(structTreeRootObjNumber, outlineDictNumber int) int {
	// Add the root object
	pdf.newobj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /Catalog\n")

	if pdf.compliance == compliance.PDF_UA ||
		pdf.compliance == compliance.PDF_A_1A ||
		pdf.compliance == compliance.PDF_A_1B ||
		pdf.compliance == compliance.PDF_A_2A ||
		pdf.compliance == compliance.PDF_A_2B ||
		pdf.compliance == compliance.PDF_A_3A ||
		pdf.compliance == compliance.PDF_A_3B {
		pdf.appendString("/Lang (")
		pdf.appendString(pdf.language)
		pdf.appendString(")\n")

		pdf.appendString("/StructTreeRoot ")
		pdf.appendInteger(structTreeRootObjNumber)
		pdf.appendString(" 0 R\n")

		pdf.appendString("/MarkInfo <</Marked true>>\n")
		pdf.appendString("/ViewerPreferences <</DisplayDocTitle true>>\n")
	}

	if pdf.pageLayout != "" {
		pdf.appendString("/PageLayout /")
		pdf.appendString(pdf.pageLayout)
		pdf.appendString("\n")
	}

	if pdf.pageMode != "" {
		pdf.appendString("/PageMode /")
		pdf.appendString(pdf.pageMode)
		pdf.appendString("\n")
	}

	pdf.addOCProperties()

	pdf.appendString("/Pages ")
	pdf.appendInteger(pdf.pagesObjNumber)
	pdf.appendString(" 0 R\n")

	if pdf.compliance == compliance.PDF_UA ||
		pdf.compliance == compliance.PDF_A_1A ||
		pdf.compliance == compliance.PDF_A_1B ||
		pdf.compliance == compliance.PDF_A_2A ||
		pdf.compliance == compliance.PDF_A_2B ||
		pdf.compliance == compliance.PDF_A_3A ||
		pdf.compliance == compliance.PDF_A_3B {
		pdf.appendString("/Metadata ")
		pdf.appendInteger(pdf.metadataObjNumber)
		pdf.appendString(" 0 R\n")

		pdf.appendString("/OutputIntents [")
		pdf.appendInteger(pdf.outputIntentObjNumber)
		pdf.appendString(" 0 R]\n")
	}

	if outlineDictNumber > 0 {
		pdf.appendString("/Outlines ")
		pdf.appendInteger(outlineDictNumber)
		pdf.appendString(" 0 R\n")
	}

	pdf.appendString(">>\n")
	pdf.endobj()
	return pdf.getObjNumber()
}

func (pdf *PDF) addPageBox(boxName string, page *Page, rect []float32) {
	pdf.appendString("/")
	pdf.appendString(boxName)
	pdf.appendString(" [")
	pdf.appendFloat32(rect[0])
	pdf.appendString(" ")
	pdf.appendFloat32(page.height - rect[3])
	pdf.appendString(" ")
	pdf.appendFloat32(rect[2])
	pdf.appendString(" ")
	pdf.appendFloat32(page.height - rect[1])
	pdf.appendString("]\n")
}

func (pdf *PDF) setDestinationObjNumbers() {
	numberOfAnnotations := 0
	for _, page := range pdf.pages {
		numberOfAnnotations += len(page.annots)
	}
	for i, page := range pdf.pages {
		for _, destination := range page.destinations {
			destination.pageObjNumber =
				pdf.getObjNumber() + numberOfAnnotations + i + 1
			pdf.destinations[*destination.name] = destination
		}
	}
}

func (pdf *PDF) addAllPages(resObjNumber int) {
	pdf.setDestinationObjNumbers()
	pdf.addAnnotDictionaries()

	// Calculate the object number of the Pages object
	pdf.pagesObjNumber = pdf.getObjNumber() + len(pdf.pages) + 1

	for i, page := range pdf.pages {
		// Page object
		pdf.newobj()
		page.objNumber = pdf.getObjNumber()
		pdf.appendString("<<\n")
		pdf.appendString("/Type /Page\n")
		pdf.appendString("/Parent ")
		pdf.appendInteger(pdf.pagesObjNumber)
		pdf.appendString(" 0 R\n")
		pdf.appendString("/MediaBox [0 0 ")
		pdf.appendFloat32(page.width)
		pdf.appendString(" ")
		pdf.appendFloat32(page.height)
		pdf.appendString("]\n")

		if page.cropBox != nil {
			pdf.addPageBox("CropBox", page, page.cropBox)
		}
		if page.bleedBox != nil {
			pdf.addPageBox("BleedBox", page, page.bleedBox)
		}
		if page.trimBox != nil {
			pdf.addPageBox("TrimBox", page, page.trimBox)
		}
		if page.artBox != nil {
			pdf.addPageBox("ArtBox", page, page.artBox)
		}

		pdf.appendString("/Resources ")
		pdf.appendInteger(resObjNumber)
		pdf.appendString(" 0 R\n")

		pdf.appendString("/Contents [ ")
		for _, n := range page.contents {
			pdf.appendInteger(n)
			pdf.appendString(" 0 R ")
		}
		pdf.appendString("]\n")

		if len(page.annots) > 0 {
			pdf.appendString("/Annots [ ")
			for _, annot := range page.annots {
				pdf.appendInteger(annot.objNumber)
				pdf.appendString(" 0 R ")
			}
			pdf.appendString("]\n")
		}

		if pdf.compliance == compliance.PDF_UA ||
			pdf.compliance == compliance.PDF_A_1A ||
			pdf.compliance == compliance.PDF_A_1B ||
			pdf.compliance == compliance.PDF_A_2A ||
			pdf.compliance == compliance.PDF_A_2B ||
			pdf.compliance == compliance.PDF_A_3A ||
			pdf.compliance == compliance.PDF_A_3B {
			pdf.appendString("/Tabs /S\n")
			pdf.appendString("/StructParents ")
			pdf.appendInteger(i)
			pdf.appendString("\n")
		}

		pdf.appendString(">>\n")
		pdf.endobj()
	}
}

func (pdf *PDF) addPageContent(page *Page) {
	compressed := compressor.Deflate(page.buf)
	page.buf = nil // Release the page content memory!

	pdf.newobj()
	pdf.appendString("<<\n")
	pdf.appendString("/Filter /FlateDecode\n")
	pdf.appendString("/Length ")
	pdf.appendInteger(len(compressed))
	pdf.appendString("\n")
	pdf.appendString(">>\n")
	pdf.appendString("stream\n")
	pdf.appendByteArray(compressed)
	pdf.appendString("\nendstream\n")
	pdf.endobj()
	page.contents = append(page.contents, pdf.getObjNumber())
}

/*
// Use this method on systems that don't have Deflater stream or when troubleshooting.
func (pdf *PDF) addPageContent(page *Page) {
	pdf.newobj()
	pdf.appendString("<<\n")
	pdf.appendString("/Length ")
	pdf.appendInteger(len(page.buf))
	pdf.appendString("\n")
	pdf.appendString(">>\n")
	pdf.appendString("stream\n")
	pdf.appendByteArray(page.buf)
	pdf.appendString("\nendstream\n")
	pdf.endobj()
	page.buf = nil // Release the page content memory!
	page.contents = append(page.contents, pdf.getObjNumber())
}

func (pdf *PDF) addPageContent(Page page) {
    ByteArrayOutputStream baos = new ByteArrayOutputStream()
    new LZWEncode(page.buf.toByteArray(), baos)
    page.buf = nil  // Release the page content memory!

    newobj()
    appendString("<<\n")
    appendString("/Filter /LZWDecode\n")
    appendString("/Length ")
    append(baos.size())
    appendString("\n")
    appendString(">>\n")
    appendString("stream\n")
    append(baos)
    appendString("\nendstream\n")
    endobj()
    page.contents.add(getObjNumber())
}
*/

func (pdf *PDF) addAnnotationObject(annot *Annotation, index int) int {
	pdf.newobj()
	annot.objNumber = pdf.getObjNumber()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /Annot\n")
	if annot.fileAttachment != nil {
		pdf.appendString("/Subtype /FileAttachment\n")
		pdf.appendString("/T (")
		pdf.appendString(annot.fileAttachment.title)
		pdf.appendString(")\n")
		pdf.appendString("/Contents (")
		pdf.appendString(annot.fileAttachment.contents)
		pdf.appendString(")\n")
		pdf.appendString("/FS ")
		pdf.appendInteger(annot.fileAttachment.embeddedFile.objNumber)
		pdf.appendString(" 0 R\n")
		pdf.appendString("/Name /")
		pdf.appendString(annot.fileAttachment.icon)
		pdf.appendString("\n")
	} else {
		pdf.appendString("/Subtype /Link\n")
	}
	pdf.appendString("/Rect [")
	pdf.appendFloat32(annot.x1)
	pdf.appendString(" ")
	pdf.appendFloat32(annot.y1)
	pdf.appendString(" ")
	pdf.appendFloat32(annot.x2)
	pdf.appendString(" ")
	pdf.appendFloat32(annot.y2)
	pdf.appendString("]\n")
	pdf.appendString("/Border [0 0 0]\n")
	if annot.uri != nil {
		pdf.appendString("/F 4\n")
		pdf.appendString("/A <<\n")
		pdf.appendString("/S /URI\n")
		pdf.appendString("/URI (")
		pdf.appendString(*annot.uri)
		pdf.appendString(")\n")
		pdf.appendString(">>\n")
	} else if annot.key != nil {
		destination := pdf.destinations[*annot.key]
		if destination != nil {
			pdf.appendString("/F 4\n") // No Zoom
			pdf.appendString("/Dest [")
			pdf.appendInteger(destination.pageObjNumber)
			pdf.appendString(" 0 R /XYZ ")
			pdf.appendFloat32(destination.xPosition)
			pdf.appendString(" ")
			pdf.appendFloat32(destination.yPosition)
			pdf.appendString(" 0]\n")
		}
	}
	if index != 0 {
		pdf.appendString("/StructParent ")
		pdf.appendInteger(index)
		index++
		pdf.appendString("\n")
	}
	pdf.appendString(">>\n")
	pdf.endobj()

	return index
}

func (pdf *PDF) addAnnotDictionaries() {
	index := len(pdf.pages)
	for _, page := range pdf.pages {
		if len(page.structures) > 0 {
			for _, element := range page.structures {
				if element.annotation != nil {
					index = pdf.addAnnotationObject(element.annotation, index)
				}
			}
		} else if len(page.annots) > 0 {
			for _, annotation := range page.annots {
				if annotation != nil {
					pdf.addAnnotationObject(annotation, 0)
				}
			}
		}
	}
}

func (pdf *PDF) addOCProperties() {
	if len(pdf.groups) > 0 {
		var buf strings.Builder
		for _, ocg := range pdf.groups {
			buf.WriteString(" ")
			buf.WriteString(strconv.Itoa(ocg.objNumber))
			buf.WriteString(" 0 R")
		}

		pdf.appendString("/OCProperties\n")
		pdf.appendString("<<\n")
		pdf.appendString("/OCGs [")
		pdf.appendString(buf.String())
		pdf.appendString(" ]\n")
		pdf.appendString("/D <<\n")

		pdf.appendString("/AS [\n")
		pdf.appendString("<< /Event /View /Category [/View] /OCGs [")
		pdf.appendString(buf.String())
		pdf.appendString(" ] >>\n")
		pdf.appendString("<< /Event /Print /Category [/Print] /OCGs [")
		pdf.appendString(buf.String())
		pdf.appendString(" ] >>\n")
		pdf.appendString("<< /Event /Export /Category [/Export] /OCGs [")
		pdf.appendString(buf.String())
		pdf.appendString(" ] >>\n")
		pdf.appendString("]\n")

		pdf.appendString("/Order [[ ()")
		pdf.appendString(buf.String())
		pdf.appendString(" ]]\n")

		pdf.appendString(">>\n")
		pdf.appendString(">>\n")
	}
}

// AddPage adds page to the PDF.
func (pdf *PDF) AddPage(page *Page) {
	pdf.pages = append(pdf.pages, page)
	if pdf.prevPage != nil {
		pdf.addPageContent(pdf.prevPage)
	}
	pdf.prevPage = page
}

// Complete writes the PDF to the bufio.Writer and calls the Flush method.
func (pdf *PDF) Complete() {
	if pdf.prevPage != nil {
		pdf.addPageContent(pdf.prevPage)
	}
	if pdf.compliance == compliance.PDF_UA ||
		pdf.compliance == compliance.PDF_A_1A ||
		pdf.compliance == compliance.PDF_A_1B ||
		pdf.compliance == compliance.PDF_A_2A ||
		pdf.compliance == compliance.PDF_A_2B ||
		pdf.compliance == compliance.PDF_A_3A ||
		pdf.compliance == compliance.PDF_A_3B {
		pdf.metadataObjNumber = pdf.addMetadataObject("", false)
		pdf.outputIntentObjNumber = pdf.addOutputIntentObject()
	}

	if pdf.pagesObjNumber == 0 {
		pdf.addAllPages(pdf.addResourcesObject())
		pdf.addPagesObject()
	}

	structTreeRootObjNumber := 0
	if pdf.compliance == compliance.PDF_UA ||
		pdf.compliance == compliance.PDF_A_1A ||
		pdf.compliance == compliance.PDF_A_1B ||
		pdf.compliance == compliance.PDF_A_2A ||
		pdf.compliance == compliance.PDF_A_2B ||
		pdf.compliance == compliance.PDF_A_3A ||
		pdf.compliance == compliance.PDF_A_3B {
		pdf.addStructElementObjects()
		structTreeRootObjNumber = pdf.addStructTreeRootObject()
		pdf.addNumsParentTree()
		pdf.addStructDocumentObject(structTreeRootObjNumber)
	}

	var outlineDictNum int = 0
	if pdf.toc != nil && pdf.toc.getChildren() != nil {
		list := pdf.toc.toArrayList()
		outlineDictNum = pdf.addOutlineDict(pdf.toc)
		for i := 1; i < len(list); i++ {
			pdf.addOutlineItem(outlineDictNum, i, list[i])
		}
	}

	infoObjNumber := pdf.addInfoObject()
	rootObjNumber := pdf.addRootObject(structTreeRootObjNumber, outlineDictNum)

	startxref := pdf.byteCount

	// Create the xref table
	pdf.appendString("xref\n")
	pdf.appendString("0 ")
	pdf.appendInteger(rootObjNumber + 1)
	pdf.appendString("\n")
	pdf.appendString("0000000000 65535 f \n")
	for _, offset := range pdf.objOffsets {
		str := strconv.Itoa(offset)
		for i := 0; i < 10-len(str); i++ {
			pdf.appendString("0")
		}
		pdf.appendString(str)
		pdf.appendString(" 00000 n \n")
	}
	pdf.appendString("trailer\n")
	pdf.appendString("<<\n")
	pdf.appendString("/Size ")
	pdf.appendInteger(rootObjNumber + 1)
	pdf.appendString("\n")

	pdf.appendString("/ID[<")
	pdf.appendString(pdf.uuid)
	pdf.appendString("><")
	pdf.appendString(pdf.uuid)
	pdf.appendString(">]\n")

	pdf.appendString("/Info ")
	pdf.appendInteger(infoObjNumber)
	pdf.appendString(" 0 R\n")

	pdf.appendString("/Root ")
	pdf.appendInteger(rootObjNumber)
	pdf.appendString(" 0 R\n")

	pdf.appendString(">>\n")
	pdf.appendString("startxref\n")
	pdf.appendInteger(startxref)
	pdf.appendString("\n")
	pdf.appendString("%%EOF\n")

	pdf.writer.Flush()
}

// SetLanguage sets the "Language" document property of the PDF file.
func (pdf *PDF) SetLanguage(language string) {
	pdf.language = language
}

// SetTitle sets the "Title" document property of the PDF file.
func (pdf *PDF) SetTitle(title string) {
	pdf.title = title
}

// SetAuthor sets the "Author" document property of the PDF file.
func (pdf *PDF) SetAuthor(author string) {
	pdf.author = author
}

// SetSubject sets the "Subject" document property of the PDF file.
func (pdf *PDF) SetSubject(subject string) {
	pdf.subject = subject
}

// SetKeywords sets the keywords.
func (pdf *PDF) SetKeywords(keywords string) {
	pdf.keywords = keywords
}

// SetCreator sets the creator field of the PDF.
func (pdf *PDF) SetCreator(creator string) {
	pdf.creator = creator
}

// SetPageLayout sets the page layout.
func (pdf *PDF) SetPageLayout(pageLayout string) {
	pdf.pageLayout = pageLayout
}

// SetPageMode sets the page mode.
func (pdf *PDF) SetPageMode(pageMode string) {
	pdf.pageMode = pageMode
}

func (pdf *PDF) getSortedObjects(objects []*PDFobj) []*PDFobj {
	sorted := make([]*PDFobj, 0)

	maxObjNumber := 0
	for _, obj := range objects {
		if obj.number > maxObjNumber {
			maxObjNumber = obj.number
		}
	}

	for number := 1; number <= maxObjNumber; number++ {
		obj := NewPDFobj()
		obj.SetNumber(number)
		sorted = append(sorted, obj)
	}

	for _, obj := range objects {
		sorted[obj.number-1] = obj
	}

	return sorted
}

func contains(slice []string, text string) bool {
	for _, str := range slice {
		if str == text {
			return true
		}
	}
	return false
}

// Read returns a list of objects of type PDFobj read from input stream.
// @param inputStream the PDF input stream.
// @return List<PDFobj> the list of PDF objects.
func (pdf *PDF) Read(buf []byte) []*PDFobj {
	objects1 := make([]*PDFobj, 0)
	xref := pdf.getStartXRef(buf)

	obj1 := getObject(buf, xref, len(buf))
	if obj1.dict[0] == "xref" {
		// Get the objects using xref table
		getObjects1(buf, obj1, &objects1)
	} else {
		// Get the objects using XRef stream
		getObjects2(buf, obj1, &objects1)
	}

	objects2 := make([]*PDFobj, 0)
	for _, obj := range objects1 {
		if contains(obj.dict, "stream") {
			length := obj.GetLength(objects1)
			obj.SetStreamAndData(buf, length)
		}

		if obj.getValue("/Type") == "/ObjStm" {
			first, err := strconv.Atoi(obj.getValue("/First"))
			if err != nil {
				log.Fatal(err)
			}
			o2 := getObject(obj.GetData(), 0, first)
			count := len(o2.dict)
			for i := 0; i < count; i += 2 {
				num, err := strconv.Atoi(o2.dict[i])
				if err != nil {
					log.Fatal(err)
				}
				off, err := strconv.Atoi(o2.dict[i+1])
				if err != nil {
					log.Fatal(err)
				}
				end := len(obj.GetData())
				if i <= count-4 {
					tmp, err := strconv.Atoi(o2.dict[i+3])
					if err != nil {
						log.Fatal(err)
					}
					end = first + tmp
				}
				o3 := getObject(obj.GetData(), first+off, end)
				o3.SetNumber(num)
				o3.dict = insertStringAt(o3.dict, "obj", 0)
				o3.dict = insertStringAt(o3.dict, "0", 0)
				o3.dict = insertStringAt(o3.dict, strconv.Itoa(num), 0)
				objects2 = append(objects2, o3)
			}
		} else if obj.getValue("/Type") == "/XRef" {
			// Skip the stream XRef object.
		} else {
			objects2 = append(objects2, obj)
		}
	}

	return pdf.getSortedObjects(objects2)
}

func process(obj *PDFobj, sb *strings.Builder, buf []byte, off int) bool {
	str := strings.TrimSpace(sb.String())
	if str != "" {
		obj.dict = append(obj.dict, str)
	}
	sb.Reset()
	if str == "endobj" {
		return true
	} else if str == "stream" {
		obj.streamOffset = off
		if buf[off] == byte('\n') {
			obj.streamOffset++
		}
		return true
	} else if str == "startxref" {
		return true
	}
	return false
}

func getObject(buf []byte, off, length int) *PDFobj {
	obj := NewPDFobj()
	obj.offset = off

	var token strings.Builder
	p := 0
	b1 := byte(' ')
	done := false
	for !done && off < length {
		b2 := buf[off]
		off++
		if b1 == byte('\\') {
			token.WriteByte(b2)
			b1 = b2
			continue
		}

		if b2 == byte('(') {
			if p == 0 {
				done = process(obj, &token, buf, off)
			}
			if !done {
				token.WriteByte(b2)
				b1 = b2
				p++
			}
		} else if b2 == byte(')') {
			token.WriteByte(b2)
			b1 = b2
			p--
			if p == 0 {
				done = process(obj, &token, buf, off)
			}
		} else if b2 == 0x00 || // Null
			b2 == 0x09 || // Horizontal Tab
			b2 == 0x0A || // Line Feed (LF)
			b2 == 0x0C || // Form Feed
			b2 == 0x0D || // Carriage Return (CR)
			b2 == 0x20 { // Space
			done = process(obj, &token, buf, off)
			if !done {
				b1 = byte(' ')
			}
		} else if b2 == byte('/') {
			done = process(obj, &token, buf, off)
			if !done {
				token.WriteByte(b2)
				b1 = b2
			}
		} else if b2 == byte('<') || b2 == byte('>') || b2 == byte('%') {
			if p > 0 {
				token.WriteByte(b2)
				b1 = b2
			} else {
				if b2 != b1 {
					done = process(obj, &token, buf, off)
					if !done {
						token.WriteByte(b2)
						b1 = b2
					}
				} else {
					token.WriteByte(b2)
					done = process(obj, &token, buf, off)
					if !done {
						b1 = byte(' ')
					}
				}
			}
		} else if b2 == byte('[') || b2 == byte(']') ||
			b2 == byte('{') || b2 == byte('}') {
			if p > 0 {
				token.WriteByte(b2)
				b1 = b2
			} else {
				done = process(obj, &token, buf, off)
				if !done {
					obj.dict = append(obj.dict, string(b2))
					b1 = b2
				}
			}
		} else {
			token.WriteByte(b2)
			b1 = b2
		}
	}

	return obj
}

// toInt converts an array of bytes to an integer.
func toInt(buf []byte, off, length int) int {
	i := 0
	for j := 0; j < length; j++ {
		i |= int(buf[off+j]) & int(0xFF)
		if j < length-1 {
			i = i << 8
		}
	}
	return i
}

func getObjects1(buf []byte, obj *PDFobj, objects *[]*PDFobj) {
	xref := obj.getValue("/Prev")
	if xref != "" {
		num, err := strconv.Atoi(xref)
		if err != nil {
			log.Fatal(err)
		}
		getObjects1(
			buf,
			getObject(buf, num, len(buf)),
			objects)
	}

	i := 1
	for {
		token := obj.dict[i]
		i++
		if token == "trailer" {
			break
		}

		n, err := strconv.Atoi(obj.dict[i]) // Number of entries
		if err != nil {
			log.Fatal(err)
		}
		i++
		for j := 0; j < n; j++ {
			offset := obj.dict[i] // Object offset
			i++
			i++                   // Skip the generation number
			status := obj.dict[i] // Status keyword
			i++
			if status != "f" {
				off, err := strconv.Atoi(offset)
				if err != nil {
					log.Fatal(err)
				}
				o2 := getObject(buf, off, len(buf))
				num, err := strconv.Atoi(o2.dict[0])
				if err != nil {
					log.Fatal(err)
				}
				o2.number = num
				*objects = append(*objects, o2)
			}
		}
	}
}

func getObjects2(buf []byte, obj *PDFobj, objects *[]*PDFobj) {
	prev := obj.getValue("/Prev")
	if prev != "" {
		off, err := strconv.Atoi(prev)
		if err != nil {
			log.Fatal(err)
		}
		getObjects2(
			buf,
			getObject(buf, off, len(buf)),
			objects)
	}

	// See page 50 in PDF32000_2008.pdf
	predictor := 0 // The predictor
	n1 := 0        // Field 1 number of bytes
	n2 := 0        // Field 2 number of bytes
	n3 := 0        // Field 3 number of bytes
	length := 0
	for i := 0; i < len(obj.dict); i++ {
		token := obj.dict[i]
		if token == "/Predictor" {
			val, err := strconv.Atoi(obj.dict[i+1])
			if err != nil {
				log.Fatal(err)
			}
			predictor = val
		} else if token == "/Length" {
			len1, err := strconv.Atoi(obj.dict[i+1])
			if err != nil {
				log.Fatal(err)
			}
			length = len1
		} else if token == "/W" {
			// "/W [ 1 3 1 ]"
			num, err := strconv.Atoi(obj.dict[i+2])
			if err != nil {
				log.Fatal(err)
			}
			n1 = num
			num, err = strconv.Atoi(obj.dict[i+3])
			if err != nil {
				log.Fatal(err)
			}
			n2 = num
			num, err = strconv.Atoi(obj.dict[i+4])
			if err != nil {
				log.Fatal(err)
			}
			n3 = num
		}
	}

	obj.SetStreamAndData(buf, length)
	n := n1 + n2 + n3 // Number of bytes per entry
	if predictor > 0 {
		n++
	}

	entry := make([]byte, n)
	for i := 0; i < len(obj.data); i += n {
		if predictor == 12 {
			// Apply the 'Up' filter.
			for j := 1; j < n; j++ {
				entry[j] += obj.data[i+j]
			}
		} else {
			for j := 0; j < n; j++ {
				entry[j] = obj.data[i+j]
			}
		}
		// Process the entries in a cross-reference stream.
		// Page 51 in PDF32000_2008.pdf
		if predictor > 0 {
			if entry[1] == 1 { // Type 1 entry
				o2 := getObject(buf, toInt(entry, 1+n1, n2), len(buf))
				num, err := strconv.Atoi(o2.dict[0])
				if err != nil {
					log.Fatal(err)
				}
				o2.number = num
				*objects = append(*objects, o2)
			}
		} else {
			if entry[0] == 1 { // Type 1 entry
				o2 := getObject(buf, toInt(entry, n1, n2), len(buf))
				num, err := strconv.Atoi(o2.dict[0])
				if err != nil {
					log.Fatal(err)
				}
				o2.number = num
				*objects = append(*objects, o2)
			}
		}
	}
}

func (pdf *PDF) getStartXRef(buf []byte) int {
	var sb strings.Builder
	for i := (len(buf) - 10); i > 10; i-- {
		if buf[i] == 's' &&
			buf[i+1] == 't' &&
			buf[i+2] == 'a' &&
			buf[i+3] == 'r' &&
			buf[i+4] == 't' &&
			buf[i+5] == 'x' &&
			buf[i+6] == 'r' &&
			buf[i+7] == 'e' &&
			buf[i+8] == 'f' {
			i += 10             // Skip over "startxref" and the first EOL character
			for buf[i] < 0x30 { // Skip over possible second EOL character and spaces
				i++
			}
			for unicode.IsDigit(rune(buf[i])) {
				sb.WriteByte(buf[i])
				i++
			}
			break
		}
	}

	objNumber, err := strconv.Atoi(sb.String())
	if err != nil {
		log.Fatal(err)
	}
	return objNumber
}

func (pdf *PDF) addOutlineDict(toc *Bookmark) int {
	numOfChildren := getNumOfChildren(0, toc)
	pdf.newobj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /Outlines\n")
	pdf.appendString("/First ")
	pdf.appendInteger(pdf.getObjNumber() + 1)
	pdf.appendString(" 0 R\n")
	pdf.appendString("/Last ")
	pdf.appendInteger(pdf.getObjNumber() + numOfChildren)
	pdf.appendString(" 0 R\n")
	pdf.appendString("/Count ")
	pdf.appendInteger(numOfChildren)
	pdf.appendString("\n")
	pdf.appendString(">>\n")
	pdf.endobj()
	return pdf.getObjNumber()
}

func (pdf *PDF) addOutlineItem(parent, i int, bm1 *Bookmark) {
	prev := 0
	if bm1.getPrevBookmark() != nil {
		prev = parent + (i - 1)
	}
	next := 0
	if bm1.getNextBookmark() != nil {
		next = parent + (i + 1)
	}

	first := 0
	last := 0
	count := 0
	if bm1.getChildren() != nil && len(bm1.getChildren()) > 0 {
		first = parent + bm1.getFirstChild().objNumber
		last = parent + bm1.getLastChild().objNumber
		count = (-1) * getNumOfChildren(0, bm1)
	}

	pdf.newobj()
	pdf.appendString("<<\n")
	pdf.appendString("/Title <")
	pdf.appendString(encodeToHex(bm1.GetTitle()))
	pdf.appendString(">\n")
	pdf.appendString("/Parent ")
	pdf.appendInteger(parent)
	pdf.appendString(" 0 R\n")
	if prev > 0 {
		pdf.appendString("/Prev ")
		pdf.appendInteger(prev)
		pdf.appendString(" 0 R\n")
	}
	if next > 0 {
		pdf.appendString("/Next ")
		pdf.appendInteger(next)
		pdf.appendString(" 0 R\n")
	}
	if first > 0 {
		pdf.appendString("/First ")
		pdf.appendInteger(first)
		pdf.appendString(" 0 R\n")
	}
	if last > 0 {
		pdf.appendString("/Last ")
		pdf.appendInteger(last)
		pdf.appendString(" 0 R\n")
	}
	if count != 0 {
		pdf.appendString("/Count ")
		pdf.appendInteger(count)
		pdf.appendString("\n")
	}
	pdf.appendString("/F 4\n") // No Zoom
	pdf.appendString("/Dest [")
	pdf.appendInteger(bm1.getDestination().pageObjNumber)
	pdf.appendString(" 0 R /XYZ ")
	pdf.appendFloat32(bm1.getDestination().xPosition)
	pdf.appendString(" ")
	pdf.appendFloat32(bm1.getDestination().yPosition)
	pdf.appendString(" 0]\n")
	pdf.appendString(">>\n")
	pdf.endobj()
}

func getNumOfChildren(numOfChildren int, bm1 *Bookmark) int {
	children := bm1.getChildren()
	for _, bm2 := range children {
		numOfChildren++
		numOfChildren = getNumOfChildren(numOfChildren, bm2)
	}
	return numOfChildren
}

// AddObjects adds the specified objects to the PDF.
func (pdf *PDF) AddObjects(objects *[]*PDFobj) {
	pagesObject := pdf.getPagesObject(*objects)
	number := pagesObject.dict[0]
	objNumber, err := strconv.Atoi(number)
	if err != nil {
		log.Fatal(err)
	} else {
		pdf.pagesObjNumber = objNumber
		pdf.addObjectsToPDF(objects)
	}
}

func (pdf *PDF) getPagesObject(objects []*PDFobj) *PDFobj {
	for _, obj := range objects {
		if obj.getValue("/Type") == "/Pages" && obj.getValue("/Parent") == "" {
			return obj
		}
	}
	return nil
}

// GetPageObjects returns all page objects.
func (pdf *PDF) GetPageObjects(objects []*PDFobj) []*PDFobj {
	pages := make([]*PDFobj, 0)
	pdf.getPageObjects(pdf.getPagesObject(objects), objects, &pages)
	return pages
}

func (pdf *PDF) getPageObjects(pdfObj *PDFobj, objects []*PDFobj, pages *[]*PDFobj) {
	kids := pdfObj.GetObjectNumbers("/Kids")
	for _, number := range kids {
		obj := objects[number-1]
		if isPageObject(obj) {
			*pages = append(*pages, obj)
		} else {
			pdf.getPageObjects(obj, objects, pages)
		}
	}
}

func isPageObject(obj *PDFobj) bool {
	isPage := false
	for i, token := range obj.dict {
		if token == "/Type" && obj.dict[i+1] == "/Page" {
			isPage = true
		}
	}
	return isPage
}

func (pdf *PDF) getExtGState(resources *PDFobj) string {
	var buf strings.Builder
	dict := resources.GetDict()
	level := 0
	for i := 0; i < len(dict); i++ {
		if dict[i] == "/ExtGState" {
			buf.WriteString("/ExtGState << ")
			i++
			level++
			for level > 0 {
				i++
				token := dict[i]
				if token == "<<" {
					level++
				} else if token == ">>" {
					level--
				}
				buf.WriteString(token)
				if level > 0 {
					buf.WriteString(" ")
				} else {
					buf.WriteString("\n")
				}
			}
			break
		}
	}
	return buf.String()
}

func (pdf *PDF) getFontObjects(resources *PDFobj, objects []*PDFobj) []*PDFobj {
	fonts := make([]*PDFobj, 0)

	dict := resources.GetDict()
	for i, token := range dict {
		if token == "/Font" {
			if dict[i+2] != ">>" {
				token := dict[i+3]
				objNumber, err := strconv.Atoi(token)
				if err != nil {
					log.Fatal(err)
				} else {
					fonts = append(fonts, objects[objNumber-1])
				}
			}
		}
	}

	if len(fonts) == 0 {
		return nil
	}

	i := 4
	for {
		if dict[i] == "/Font" {
			i += 2
			break
		}
		i++
	}
	for dict[i] != ">>" {
		pdf.importedFonts = append(pdf.importedFonts, dict[i])
		i++
	}

	return fonts
}

func (pdf *PDF) getDescendantFonts(font *PDFobj, objects []*PDFobj) []*PDFobj {
	descendantFonts := make([]*PDFobj, 0)
	dict := font.GetDict()
	for i, token := range dict {
		if token == "/DescendantFonts" {
			token = dict[i+2]
			objNumber, err := strconv.Atoi(token)
			if err != nil {
				log.Fatal(err)
			} else {
				if token != "]" {
					descendantFonts = append(descendantFonts, objects[objNumber-1])
				}
			}
		}
	}
	return descendantFonts
}

func (pdf *PDF) getObjectFromObjects(name string, obj *PDFobj, objects []*PDFobj) *PDFobj {
	dict := obj.GetDict()
	for i, token := range dict {
		if token == name {
			token = dict[i+1]
			objNumber, err := strconv.Atoi(token)
			if err != nil {
				fmt.Println("NumberFormatException: " + token)
			} else {
				return objects[objNumber-1]
			}
		}
	}
	return nil
}

// AddResourceObjects adds the resource objects to the PDF.
func (pdf *PDF) AddResourceObjects(objects []*PDFobj) {
	resources := make([]*PDFobj, 0)

	pages := pdf.GetPageObjects(objects)
	for _, page := range pages {
		resObj := page.getResourcesObject(objects)
		fonts := pdf.getFontObjects(resObj, objects)
		for _, font := range fonts {
			resources = append(resources, font)
			obj := pdf.getObjectFromObjects("/ToUnicode", font, objects)
			if obj != nil {
				resources = append(resources, obj)
			}
			descendantFonts := pdf.getDescendantFonts(font, objects)
			for _, descendantFont := range descendantFonts {
				resources = append(resources, descendantFont)
				obj = pdf.getObjectFromObjects("/FontDescriptor", descendantFont, objects)
				if obj != nil {
					resources = append(resources, obj)
					obj = pdf.getObjectFromObjects("/FontFile2", obj, objects)
					if obj != nil {
						resources = append(resources, obj)
					}
				}
			}
		}
		pdf.extGState = pdf.getExtGState(resObj)
	}
	sort.SliceStable(resources, func(i, j int) bool {
		return resources[i].number < resources[j].number
	})

	pdf.addObjectsToPDF(&resources)
}

func (pdf *PDF) addObjectsToPDF(objects *[]*PDFobj) {
	for _, obj := range *objects {
		if obj.offset == 0 {
			// Create new object.
			pdf.objOffsets = append(pdf.objOffsets, pdf.byteCount)
			pdf.appendInteger(obj.number)
			pdf.appendString(" 0 obj\n")
			if obj.dict != nil {
				for _, token := range obj.dict {
					pdf.appendString(token)
					pdf.appendString(" ")
				}
			}
			if obj.stream != nil {
				if len(obj.dict) == 0 {
					pdf.appendString("<< /Length ")
					pdf.appendInteger(len(obj.stream))
					pdf.appendString(" >>")
				}
				pdf.appendString("\nstream\n")
				pdf.appendByteArray(obj.stream)
				pdf.appendString("\nendstream\n")
			}
			pdf.appendString("endobj\n")
		} else {
			pdf.objOffsets = append(pdf.objOffsets, pdf.byteCount)
			// Uncomment to see the format of the objects.
			// fmt.Println(obj.dict)
			var link bool = false
			n := len(obj.dict)
			var token string
			for i := 0; i < n; i++ {
				token = obj.dict[i]
				pdf.appendString(token)
				if strings.HasPrefix(token, "(http:") {
					link = true
				} else if link && strings.HasSuffix(token, ")") {
					link = false
				}
				if i < (n - 1) {
					if !link {
						pdf.appendString(" ")
					}
				} else {
					pdf.appendString("\n")
				}
			}
			if obj.stream != nil {
				pdf.appendByteArray(obj.stream)
				pdf.appendString("\nendstream\n")
			}
			if token != "endobj" {
				pdf.appendString("endobj\n")
			}
		}
	}
}

func (pdf *PDF) appendInteger(value int) {
	pdf.appendString(strconv.Itoa(value))
}

func (pdf *PDF) appendFloat32(f float32) {
	p := formatFloat32(f)
	pdf.writer.Write(p)
	pdf.byteCount += len(p)
}

func (pdf *PDF) appendString(text string) {
	buf := []byte(text)
	pdf.writer.Write(buf)
	pdf.byteCount += len(buf)
}

func (pdf *PDF) appendByte(b byte) {
	pdf.writer.WriteByte(b)
	pdf.byteCount++
}

func (pdf *PDF) appendByteArraySlice(buf []byte, off, length int) {
	pdf.writer.Write(buf[off : off+length])
	pdf.byteCount += length
}

func (pdf *PDF) appendByteArray(buf []byte) {
	pdf.writer.Write(buf)
	pdf.byteCount += len(buf)
}
