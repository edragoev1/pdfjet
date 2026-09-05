// pdf.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"encoding/hex"
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
	"github.com/edragoev1/pdfjet/src/encryption"
	"github.com/edragoev1/pdfjet/src/fastfloat"
	"github.com/edragoev1/pdfjet/src/token"
)

// PDF is used to create PDF objects.
type PDF struct {
	writer                    *bufio.Writer
	byteCount                 int
	objOffsets                []int
	fonts                     []*Font
	images                    []*Image
	pages                     []*Page
	destinations              map[string]*Destination
	groups                    []*OptionalContentGroup
	states                    map[string]int
	stamps                    []*Stamp
	metadataObjNumber         int
	outputIntentObjNumber     int
	compliance                int
	encryption                *Encryption
	title                     string
	author                    string
	subject                   string
	keywords                  string
	producer                  string
	creator                   string
	createDate                string
	creationDate              string
	pagesObjNumber            int
	pageLayout                string
	pageMode                  string
	language                  string
	toc                       *Bookmark
	importedFonts             []string
	extGState                 string
	uuid                      string
	prevPage                  *Page
	structElements            []*StructElem
	contentStreamsCompression bool
}

// OCG holds an object number and a name.
type OCG struct {
	objNumber int
	name      string
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
// Root
// xref table
// Trailer
/**
 *  Creates a PDF object that represents a PDF document.
 *  Use this constructor to create PDF/A compliant PDF documents.
 *  Please note: PDF/A compliance requires all fonts to be embedded in the PDF.
 *
 *  @param os the associated output stream.
 *  @param compliance must be: compliance.PDF_UA_1 or compliance.PDF_A_1A to compliance.PDF_A_3B
 */
func NewPDF(w *bufio.Writer) *PDF {
	pdf := new(PDF)
	pdf.contentStreamsCompression = true
	pdf.writer = w
	pdf.producer = "PDFjet v8.6.0"
	pdf.language = "en-US"

	pdf.destinations = make(map[string]*Destination)
	pdf.uuid = djb.Salsa20()

	// createDate format: "yyyy-MM-ddTHH:mm:ss"
	pdf.createDate = time.Now().Format(time.RFC3339)[0:19]

	pdf.states = make(map[string]int)
	pdf.stamps = make([]*Stamp, 0)

	pdf.appendString("%PDF-1.7\n")
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

func (pdf *PDF) SetEncryption(encryption *Encryption, err error) {
	if err == nil {
		pdf.encryption = encryption
	}
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
		sb.WriteString(notice)
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
		if pdf.compliance == compliance.PDF_UA_1 {
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

		if pdf.title != "" {
			sb.WriteString("  <dc:title><rdf:Alt><rdf:li xml:lang=\"x-default\">")
			sb.WriteString(pdf.title)
			sb.WriteString("</rdf:li></rdf:Alt></dc:title>\n")
		}

		if pdf.author != "" {
			sb.WriteString("  <dc:creator><rdf:Seq><rdf:li>")
			sb.WriteString(pdf.author)
			sb.WriteString("</rdf:li></rdf:Seq></dc:creator>\n")
		}

		if pdf.subject != "" {
			sb.WriteString("  <dc:description><rdf:Alt><rdf:li xml:lang=\"x-default\">")
			sb.WriteString(pdf.subject)
			sb.WriteString("</rdf:li></rdf:Alt></dc:description>\n")
		}

		if pdf.keywords != "" {
			sb.WriteString("  <pdf:Keywords>")
			sb.WriteString(pdf.keywords)
			sb.WriteString("</pdf:Keywords>\n")
		}

		if pdf.creator != "" {
			sb.WriteString("  <xmp:CreatorTool>")
			sb.WriteString(pdf.creator)
			sb.WriteString("</xmp:CreatorTool>\n")
		}

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
		// Add the recommended 2000 bytes padding. 20 lines × 100 bytes (99 spaces + newline).
		line := strings.Repeat(" ", 99) + "\n"
		sb.WriteString(strings.Repeat(line, 20))
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
	pdf.appendByte(token.Newline)
	pdf.appendByteArray(token.EndDictionary)
	pdf.appendByteArray(token.Stream)
	pdf.appendByteArray(xml)
	pdf.appendByteArray(token.EndStream)
	pdf.endobj()

	return pdf.getObjNumber()
}

func (pdf *PDF) addOutputIntentObject() int {
	pdf.newobj()
	pdf.appendByteArray(token.BeginDictionary)
	pdf.appendString("/N 3\n")

	pdf.appendByteArray(token.Length)
	pdf.appendInteger(len(ICCBlackScaledProfile))
	pdf.appendByte(token.Newline)

	pdf.appendString("/Filter /FlateDecode\n")
	pdf.appendByteArray(token.EndDictionary)
	pdf.appendByteArray(token.Stream)
	pdf.appendByteArray(ICCBlackScaledProfile)
	pdf.appendByteArray(token.EndStream)
	pdf.endobj()

	identifierBytes := []byte("sRGB IEC61966-2.1")
	if pdf.encryption != nil {
		identifierBytes, _ = encryption.Encrypt(identifierBytes, pdf.encryption.GetKey())
	}
	// OutputIntent object
	pdf.newobj()
	pdf.appendByteArray(token.BeginDictionary)
	pdf.appendString("/Type /OutputIntent\n")
	pdf.appendString("/S /GTS_PDFA1\n")

	pdf.appendString("/OutputCondition <")
	pdf.appendString(hex.EncodeToString(identifierBytes))
	pdf.appendString(">\n")

	pdf.appendString("/OutputConditionIdentifier <")
	pdf.appendString(hex.EncodeToString(identifierBytes))
	pdf.appendString(">\n")

	pdf.appendString("/Info <")
	pdf.appendString(hex.EncodeToString(identifierBytes))
	pdf.appendString(">\n")

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
		for _, token1 := range pdf.importedFonts {
			pdf.appendString(token1)
			if token1 == "R" {
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
	if len(pdf.images) > 0 || len(pdf.stamps) > 0 {
		pdf.appendString("/XObject\n")
		pdf.appendByteArray(token.BeginDictionary)
		for _, image := range pdf.images {
			pdf.appendString("/Im")
			pdf.appendInteger(image.objNumber)
			pdf.appendString(" ")
			pdf.appendInteger(image.objNumber)
			pdf.appendString(" 0 R\n")
		}
		for _, stamp := range pdf.stamps {
			pdf.appendString("/Fm")
			pdf.appendInteger(stamp.objNumber)
			pdf.appendString(" ")
			pdf.appendInteger(stamp.objNumber)
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

func (pdf *PDF) addPagesObject() {
	pdf.newobj()
	pdf.appendByteArray(token.BeginDictionary)
	pdf.appendString("/Type /Pages\n")
	pdf.appendString("/Kids [\n")
	for _, page := range pdf.pages {
		if pdf.compliance != compliance.PDF_17 {
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
	for _, structElement := range pdf.structElements {
		pdf.appendInteger(structElement.objNumber)
		pdf.appendByteArray(token.ObjRef)
	}
	pdf.appendString("]\n")
	pdf.appendByteArray(token.EndDictionary)
	pdf.endobj()
	return pdf.getObjNumber()
}

func (pdf *PDF) addStructElementObjects() {
	structTreeRootObjNumber := pdf.getObjNumber() + 1
	structTreeRootObjNumber += len(pdf.structElements)
	for _, element := range pdf.structElements {
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
			pdf.appendString(" 0 R>>\n")
		} else {
			pdf.appendString("/K ")
			pdf.appendInteger(element.mcid)
			pdf.appendString("\n")
		}

		if element.actualText != "" && element.altDescription != "" {
			language := element.language
			if language == "" {
				language = pdf.language
			}
			languageBytes := []byte(language)
			actualTextBytes := []byte(element.actualText)
			altDescriptionBytes := []byte(element.altDescription)
			if pdf.encryption != nil {
				languageBytes, _ = encryption.Encrypt(languageBytes, pdf.encryption.GetKey())
				actualTextBytes, _ = encryption.Encrypt(actualTextBytes, pdf.encryption.GetKey())
				altDescriptionBytes, _ = encryption.Encrypt(altDescriptionBytes, pdf.encryption.GetKey())
			}

			pdf.appendString("/Lang <")
			pdf.appendString(hex.EncodeToString(languageBytes))
			pdf.appendString(">\n")

			pdf.appendString("/ActualText <")
			pdf.appendString(hex.EncodeToString(actualTextBytes))
			pdf.appendString(">\n")

			pdf.appendString("/Alt <")
			pdf.appendString(hex.EncodeToString(altDescriptionBytes))
			pdf.appendString(">\n")
		}

		pdf.appendString(">>\n")
		pdf.endobj()
	}
}

func (pdf *PDF) addNumsParentTree() {
	pdf.newobj()
	pdf.appendString("<<\n")
	pdf.appendString("/Nums [\n")
	for i, element := range pdf.structElements {
		if element.annotation != nil {
			pdf.appendInteger(i)
			pdf.appendString(" ")
			pdf.appendInteger(element.objNumber)
			pdf.appendString(" 0 R\n")
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

	if pdf.compliance != compliance.PDF_17 {
		languageBytes := []byte(pdf.language)
		if pdf.encryption != nil {
			languageBytes, _ = encryption.Encrypt(languageBytes, pdf.encryption.GetKey())
		}

		pdf.appendString("/Lang <")
		pdf.appendString(hex.EncodeToString(languageBytes))
		pdf.appendString(">\n")

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

	if pdf.compliance != compliance.PDF_17 {
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
			pdf.destinations[destination.name] = destination
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

		if pdf.compliance != compliance.PDF_17 {
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
	if pdf.contentStreamsCompression {
		compressed := compressor.Deflate(page.buf)
		if pdf.encryption != nil {
			compressed, _ = encryption.Encrypt(compressed, pdf.encryption.GetKey())
		}
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
	} else { // No compression. Used for diagnostics
		buf := page.buf
		if pdf.encryption != nil {
			buf, _ = encryption.Encrypt(buf, pdf.encryption.GetKey())
		}
		page.buf = nil // Release the page content memory!

		pdf.newobj()
		pdf.appendString("<<\n")
		pdf.appendString("/Length ")
		pdf.appendInteger(len(page.buf))
		pdf.appendString("\n")
		pdf.appendString(">>\n")
		pdf.appendString("stream\n")
		pdf.appendByteArray(buf)
		pdf.appendString("\nendstream\n")
		pdf.endobj()
		page.buf = nil // Release the page content memory!
		page.contents = append(page.contents, pdf.getObjNumber())
	}
}

func (pdf *PDF) addAnnotationObject(annot *Annotation, index int) int {
	pdf.newobj()
	annot.objNumber = pdf.getObjNumber()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /Annot\n")
	pdf.appendString("/Subtype /")
	pdf.appendString(annot.annotationType)
	pdf.appendString("\n")

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

	if annot.annotationType == AnnotationFileAttachment {
		pdf.appendString("/FS ")
		pdf.appendString(strconv.Itoa(annot.fileAttachment.embeddedFile.objNumber))
		pdf.appendString(" 0 R\n")
		pdf.appendString("/Name /")
		pdf.appendString(annot.fileAttachment.icon)
		pdf.appendString("\n")

		if annot.fileAttachment.title != "" {
			title := []byte(annot.fileAttachment.title)
			if pdf.encryption != nil {
				title, _ = encryption.Encrypt(title, pdf.encryption.GetKey())
			}
			pdf.appendString("/T <")
			pdf.appendString(hex.EncodeToString(title))
			pdf.appendString(">\n")
		}

		if annot.fileAttachment.contents != "" {
			contents := []byte(annot.fileAttachment.contents)
			if pdf.encryption != nil {
				contents, _ = encryption.Encrypt(contents, pdf.encryption.GetKey())
			}
			pdf.appendString("/Contents <")
			pdf.appendString(hex.EncodeToString(contents))
			pdf.appendString(">\n")
		}
	} else if annot.annotationType == AnnotationLink {
		if annot.uri != "" {
			pdf.appendString("/F 4\n")
			pdf.appendString("/A <<\n")
			pdf.appendString("/S /URI\n")
			uri := []byte(annot.uri)
			if pdf.encryption != nil {
				uri, _ = encryption.Encrypt(uri, pdf.encryption.GetKey())
			}
			pdf.appendString("/URI <")
			pdf.appendString(hex.EncodeToString(uri))
			pdf.appendString(">\n")
			pdf.appendString(">>\n")
		} else if annot.key != "" {
			destination := pdf.destinations[annot.key]
			if destination != nil {
				pdf.appendString("/F 4\n")
				pdf.appendString("/Dest [")
				pdf.appendString(strconv.Itoa(destination.pageObjNumber))
				pdf.appendString(" 0 R /XYZ ")
				pdf.appendFloat32(destination.xPosition)
				pdf.appendString(" ")
				pdf.appendFloat32(destination.yPosition)
				pdf.appendString(" 0]\n")
			}
		}
	} else if annot.annotationType == AnnotationPolygon {
		pdf.appendString("/Vertices [ ")
		for i := 0; i < len(annot.vertices); i += 2 {
			pdf.appendFloat32(annot.x1 + annot.vertices[i])
			pdf.appendString(" ")
			pdf.appendFloat32(annot.y1 - annot.vertices[i+1])
			pdf.appendString(" ")
		}
		pdf.appendString("]\n")

		pdf.appendString("/IC [")
		pdf.appendFloat32(annot.fillColor[0])
		pdf.appendString(" ")
		pdf.appendFloat32(annot.fillColor[1])
		pdf.appendString(" ")
		pdf.appendFloat32(annot.fillColor[2])
		pdf.appendString("]\n")

		pdf.appendString("/CA ")
		pdf.appendFloat32(annot.transparency)
		pdf.appendString("\n")

		if annot.title != "" {
			title := []byte(annot.title)
			if pdf.encryption != nil {
				title, _ = encryption.Encrypt(title, pdf.encryption.GetKey())
			}
			pdf.appendString("/T <")
			pdf.appendString(hex.EncodeToString(title))
			pdf.appendString(">\n")
		}

		if annot.contents != "" {
			contents := []byte(annot.contents)
			if pdf.encryption != nil {
				contents, _ = encryption.Encrypt(contents, pdf.encryption.GetKey())
			}
			pdf.appendString("/Contents <")
			pdf.appendString(hex.EncodeToString(contents))
			pdf.appendString(">\n")
		}
	} else if annot.annotationType == AnnotationSquare ||
		annot.annotationType == AnnotationCircle {
		pdf.appendString("/IC [")
		pdf.appendFloat32(annot.fillColor[0])
		pdf.appendString(" ")
		pdf.appendFloat32(annot.fillColor[1])
		pdf.appendString(" ")
		pdf.appendFloat32(annot.fillColor[2])
		pdf.appendString("]\n")

		pdf.appendString("/CA ")
		pdf.appendFloat32(annot.transparency)
		pdf.appendString("\n")

		if annot.title != "" {
			title := []byte(annot.title)
			if pdf.encryption != nil {
				title, _ = encryption.Encrypt(title, pdf.encryption.GetKey())
			}
			pdf.appendString("/T <")
			pdf.appendString(hex.EncodeToString(title))
			pdf.appendString(">\n")
		}

		if annot.contents != "" {
			contents := []byte(annot.contents)
			if pdf.encryption != nil {
				contents, _ = encryption.Encrypt(contents, pdf.encryption.GetKey())
			}
			pdf.appendString("/Contents <")
			pdf.appendString(hex.EncodeToString(contents))
			pdf.appendString(">\n")
		}
	} else if annot.annotationType == AnnotationText {
		pdf.appendString("/Name /Comment\n")

		if annot.title != "" {
			title := []byte(annot.title)
			if pdf.encryption != nil {
				title, _ = encryption.Encrypt(title, pdf.encryption.GetKey())
			}
			pdf.appendString("/T <")
			pdf.appendString(hex.EncodeToString(title))
			pdf.appendString(">\n")
		}

		if annot.contents != "" {
			contents := []byte(annot.contents)
			if pdf.encryption != nil {
				contents, _ = encryption.Encrypt(contents, pdf.encryption.GetKey())
			}
			pdf.appendString("/Contents <")
			pdf.appendString(hex.EncodeToString(contents))
			pdf.appendString(">\n")
		}
	}

	if index != -1 {
		pdf.appendString("/StructParent ")
		pdf.appendInteger(index)
		pdf.appendString("\n")
		index++
	}
	pdf.appendString(">>\n")
	pdf.endobj()

	return index
}

func (pdf *PDF) addAnnotDictionaries() {
	index := len(pdf.pages)
	for _, element := range pdf.structElements {
		if element.annotation != nil {
			index = pdf.addAnnotationObject(element.annotation, index)
		}
	}

	for _, page := range pdf.pages {
		if len(page.annots) > 0 {
			for _, annot := range page.annots {
				pdf.addAnnotationObject(annot, -1)
			}
		}
	}
}

func (pdf *PDF) addOCProperties() {
	if len(pdf.groups) > 0 {
		var list []OCG
		var buf strings.Builder
		for _, ocg := range pdf.groups {
			buf.WriteString(" ")
			buf.WriteString(strconv.Itoa(ocg.objNumber))
			buf.WriteString(" 0 R")
			list = append(list, OCG{
				objNumber: ocg.objNumber,
				name:      ocg.name,
			})
		}
		sort.Slice(list, func(i, j int) bool {
			return list[i].name < list[j].name
		})

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

		pdf.appendString("/Order [")
		for _, ocg := range list {
			pdf.appendString(" ")
			pdf.appendInteger(ocg.objNumber)
			pdf.appendString(" 0 R ")
		}
		pdf.appendString("]\n")

		pdf.appendString(">>\n")
		pdf.appendString(">>\n")
	}
}

// AddPage adds page to the PDF.
func (pdf *PDF) AddPage(page *Page) {
	if page == nil {
		return
	}
	pdf.pages = append(pdf.pages, page)
	if pdf.prevPage != nil {
		pdf.addPageContent(pdf.prevPage)
	}
	pdf.prevPage = page
}

func (pdf *PDF) AddPages(pages []*Page) {
	for _, page := range pages {
		pdf.AddPage(page)
	}
}

// Complete writes the PDF to the bufio.Writer and calls the Flush method.
func (pdf *PDF) Complete() {
	if pdf.prevPage != nil {
		pdf.addPageContent(pdf.prevPage)
	}
	if pdf.compliance != compliance.PDF_17 {
		pdf.metadataObjNumber = pdf.addMetadataObject("", false)
		pdf.outputIntentObjNumber = pdf.addOutputIntentObject()
	}

	if pdf.pagesObjNumber == 0 {
		pdf.addAllPages(pdf.addResourcesObject())
		pdf.addPagesObject()
	}

	structTreeRootObjNumber := 0
	if pdf.compliance != compliance.PDF_17 {
		pdf.addStructElementObjects()
		structTreeRootObjNumber = pdf.addStructTreeRootObject()
		pdf.addNumsParentTree()
		pdf.addStructDocumentObject(structTreeRootObjNumber)
	}

	var outlineDictNum = 0
	if pdf.toc != nil && pdf.toc.getChildren() != nil {
		list := pdf.toc.toArrayList()
		outlineDictNum = pdf.addOutlineDict(pdf.toc)
		for i := 1; i < len(list); i++ {
			pdf.addOutlineItem(outlineDictNum, i, list[i])
		}
	}

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

	if pdf.encryption != nil {
		pdf.appendString("/Encrypt ")
		pdf.appendInteger(pdf.encryption.GetObjNumber())
		pdf.appendString(" 0 R\n")
	}

	pdf.appendString("/Root ")
	pdf.appendInteger(rootObjNumber)
	pdf.appendString(" 0 R\n")

	pdf.appendString(">>\n")
	pdf.appendString("startxref\n")
	pdf.appendInteger(startxref)
	pdf.appendString("\n")
	pdf.appendString("%%EOF\n")

	err := pdf.writer.Flush()
	if err != nil {
		log.Printf("failed to flush PDF writer: %v\n", err)
		return
	}
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

	var token1 strings.Builder
	p := 0
	b1 := byte(' ')
	done := false
	for !done && off < length {
		b2 := buf[off]
		off++
		if b1 == byte('\\') {
			token1.WriteByte(b2)
			b1 = b2
			continue
		}

		if b2 == byte('(') {
			if p == 0 {
				done = process(obj, &token1, buf, off)
			}
			if !done {
				token1.WriteByte(b2)
				b1 = b2
				p++
			}
		} else if b2 == byte(')') {
			token1.WriteByte(b2)
			b1 = b2
			p--
			if p == 0 {
				done = process(obj, &token1, buf, off)
			}
		} else if b2 == 0x00 || // Null
			b2 == 0x09 || // Horizontal Tab
			b2 == 0x0A || // Line Feed (LF)
			b2 == 0x0C || // Form Feed
			b2 == 0x0D || // Carriage Return (CR)
			b2 == 0x20 { // Space
			done = process(obj, &token1, buf, off)
			if !done {
				b1 = byte(' ')
			}
		} else if b2 == byte('/') {
			done = process(obj, &token1, buf, off)
			if !done {
				token1.WriteByte(b2)
				b1 = b2
			}
		} else if b2 == byte('<') || b2 == byte('>') || b2 == byte('%') {
			if p > 0 {
				token1.WriteByte(b2)
				b1 = b2
			} else {
				if b2 != b1 {
					done = process(obj, &token1, buf, off)
					if !done {
						token1.WriteByte(b2)
						b1 = b2
					}
				} else {
					token1.WriteByte(b2)
					done = process(obj, &token1, buf, off)
					if !done {
						b1 = byte(' ')
					}
				}
			}
		} else if b2 == byte('[') || b2 == byte(']') ||
			b2 == byte('{') || b2 == byte('}') {
			if p > 0 {
				token1.WriteByte(b2)
				b1 = b2
			} else {
				done = process(obj, &token1, buf, off)
				if !done {
					obj.dict = append(obj.dict, string(b2))
					b1 = b2
				}
			}
		} else {
			token1.WriteByte(b2)
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
		token1 := obj.dict[i]
		i++
		if token1 == "trailer" {
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
		token1 := obj.dict[i]
		if token1 == "/Predictor" {
			val, err := strconv.Atoi(obj.dict[i+1])
			if err != nil {
				log.Fatal(err)
			}
			predictor = val
		} else if token1 == "/Length" {
			len1, err := strconv.Atoi(obj.dict[i+1])
			if err != nil {
				log.Fatal(err)
			}
			length = len1
		} else if token1 == "/W" {
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
	for i := len(buf) - 10; i > 10; i-- {
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

	title := []byte(bm1.GetTitle())
	if pdf.encryption != nil {
		title, _ = encryption.Encrypt(title, pdf.encryption.GetKey())
	}

	pdf.newobj()
	pdf.appendString("<<\n")
	pdf.appendString("/Title <")
	pdf.appendString(hex.EncodeToString(title))
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
	if pagesObject != nil {
		var number = pagesObject.dict[0]
		objNumber, err := strconv.Atoi(number)
		if err != nil {
			log.Fatal(err)
		} else {
			pdf.pagesObjNumber = objNumber
			pdf.addObjectsToPDF(objects)
		}
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
	for i, token1 := range obj.dict {
		if token1 == "/Type" && obj.dict[i+1] == "/Page" {
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
				token1 := dict[i]
				if token1 == "<<" {
					level++
				} else if token1 == ">>" {
					level--
				}
				buf.WriteString(token1)
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
	for i, token1 := range dict {
		if token1 == "/Font" {
			if dict[i+2] != ">>" {
				token1 := dict[i+3]
				objNumber, err := strconv.Atoi(token1)
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
	for i, token1 := range dict {
		if token1 == "/DescendantFonts" {
			token1 = dict[i+2]
			objNumber, err := strconv.Atoi(token1)
			if err != nil {
				log.Fatal(err)
			} else {
				if token1 != "]" {
					descendantFonts = append(descendantFonts, objects[objNumber-1])
				}
			}
		}
	}
	return descendantFonts
}

func (pdf *PDF) getObjectFromObjects(name string, obj *PDFobj, objects []*PDFobj) *PDFobj {
	dict := obj.GetDict()
	for i, token1 := range dict {
		if token1 == name {
			token1 = dict[i+1]
			objNumber, err := strconv.Atoi(token1)
			if err != nil {
				log.Println("NumberFormatException: " + token1)
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
				for _, token1 := range obj.dict {
					pdf.appendString(token1)
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
			// log.Println(obj.dict)
			var link = false
			n := len(obj.dict)
			var token1 string
			for i := 0; i < n; i++ {
				token1 = obj.dict[i]
				pdf.appendString(token1)
				if strings.HasPrefix(token1, "(http:") {
					link = true
				} else if link && strings.HasSuffix(token1, ")") {
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
			if token1 != "endobj" {
				pdf.appendString("endobj\n")
			}
		}
	}
}

func (pdf *PDF) appendInteger(value int) {
	pdf.appendString(strconv.Itoa(value))
}

func (pdf *PDF) appendFloat32(f float32) {
	pdf.appendByteArray(fastfloat.ToByteArray(f))
}

func (pdf *PDF) appendString(s string) {
	buf := []byte(s)
	_, err := pdf.writer.Write(buf)
	if err != nil {
		return
	}
	pdf.byteCount += len(buf)
}

func (pdf *PDF) appendByte(b byte) {
	err := pdf.writer.WriteByte(b)
	if err != nil {
		return
	}
	pdf.byteCount++
}

func (pdf *PDF) appendByteArray(buf []byte) {
	_, err := pdf.writer.Write(buf)
	if err != nil {
		return
	}
	pdf.byteCount += len(buf)
}
