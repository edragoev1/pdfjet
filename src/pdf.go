// pdf.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package pdfjet is a library for creating PDF documents.
package pdfjet

import (
	"bufio"
	"encoding/hex"
	"log"
	"os"
	"slices"
	"sort"
	"strconv"
	"strings"
	"time"

	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/compressor"
	"github.com/edragoev1/pdfjet/v9/src/djb"
	"github.com/edragoev1/pdfjet/v9/src/encryption"
	"github.com/edragoev1/pdfjet/v9/src/fastfloat"
	"github.com/edragoev1/pdfjet/v9/src/token"
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
	importedXObjects          []string
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
	pdf.producer = "PDFjet v8.7.0"
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

// SetCompliance sets the PDF/UA or PDF/A compliance of this document. See the compliance package.
func (pdf *PDF) SetCompliance(compliance int) *PDF {
	pdf.compliance = compliance
	return pdf
}

// SetEncryption sets the encryption applied to this document. It does nothing when err is not nil.
func (pdf *PDF) SetEncryption(encryption *Encryption, err error) *PDF {
	if err == nil {
		pdf.encryption = encryption
	}
	return pdf
}

// NewPDFFile creates a PDF document that is written to the file at the specified path.
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

// setObjOffset records the offset of an object that carries its own number,
// growing the table with placeholders for any number that has no object yet.
func (pdf *PDF) setObjOffset(number, offset int) {
	if number <= 0 { // No number of its own - just append.
		pdf.objOffsets = append(pdf.objOffsets, offset)
		return
	}
	for len(pdf.objOffsets) < number {
		pdf.objOffsets = append(pdf.objOffsets, 0)
	}
	pdf.objOffsets[number-1] = offset
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
		sb.WriteString("    xmlns:dc=\"http://purl.org/dc/elements/1.1/\"\n")
		sb.WriteString("    xmlns:xmp=\"http://ns.adobe.com/xap/1.0/\"\n")
		sb.WriteString("    xmlns:xapMM=\"http://ns.adobe.com/xap/1.0/mm/\"\n")
		sb.WriteString("    xmlns:pdfaid=\"http://www.aiim.org/pdfa/ns/id/\"\n")
		sb.WriteString("    xmlns:pdfuaid=\"http://www.aiim.org/pdfua/ns/id/\">\n")

		sb.WriteString("    <dc:format>application/pdf</dc:format>\n")
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
		// Add the recommended 2000 bytes padding
		line := strings.Repeat(" ", 100) + "\n"
		sb.WriteString(strings.Repeat(line, 20))
	}

	sb.WriteString("</rdf:RDF>\n")
	sb.WriteString("</x:xmpmeta>\n")
	sb.WriteString("<?xpacket end=\"w\"?>")

	// The metadata is encrypted like every other stream, and the
	// encryption dictionary says so with /EncryptMetadata true. Readers do
	// not agree on which metadata streams to leave alone when it is false.
	xml := []byte(sb.String())
	if pdf.encryption != nil {
		xml, _ = encryption.Encrypt(xml, pdf.encryption.GetKey())
	}

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
	profile := ICCBlackScaledProfile
	if pdf.encryption != nil {
		profile, _ = encryption.Encrypt(profile, pdf.encryption.GetKey())
	}

	pdf.newobj()
	pdf.appendByteArray(token.BeginDictionary)
	pdf.appendString("/N 3\n")

	pdf.appendByteArray(token.Length)
	pdf.appendInteger(len(profile))
	pdf.appendByte(token.Newline)

	pdf.appendString("/Filter /FlateDecode\n")
	pdf.appendByteArray(token.EndDictionary)
	pdf.appendByteArray(token.Stream)
	pdf.appendByteArray(profile)
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

// appendImportedEntries writes the "/Name number 0 R" entries collected from a
// PDF that was read.
func (pdf *PDF) appendImportedEntries(tokens []string) {
	for _, token1 := range tokens {
		pdf.appendString(token1)
		if token1 == "R" {
			pdf.appendString("\n")
		} else {
			pdf.appendString(" ")
		}
	}
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
		pdf.appendImportedEntries(pdf.importedFonts)
		for _, font := range pdf.fonts {
			pdf.appendString("/F")
			pdf.appendInteger(font.objNumber)
			pdf.appendString(" ")
			pdf.appendInteger(font.objNumber)
			pdf.appendString(" 0 R\n")
		}
		pdf.appendByteArray(token.EndDictionary)
	}
	if len(pdf.images) > 0 || len(pdf.stamps) > 0 || len(pdf.importedXObjects) > 0 {
		pdf.appendString("/XObject\n")
		pdf.appendByteArray(token.BeginDictionary)
		pdf.appendImportedEntries(pdf.importedXObjects)
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
		keys := make([]string, 0, len(pdf.states))
		for key := range pdf.states {
			keys = append(keys, key)
		}
		sort.Slice(keys, func(i, j int) bool {
			return pdf.states[keys[i]] < pdf.states[keys[j]]
		})
		for _, key := range keys {
			pdf.appendString("/GS")
			pdf.appendInteger(pdf.states[key])
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

		// The actual text is written only with an alternate description, since
		// a text block and a text box pass the text they draw as the actual
		// text without one.
		hasAltDescription := element.altDescription != ""
		hasActualText := hasAltDescription && element.actualText != ""
		language := element.language
		if language == "" && hasAltDescription {
			language = pdf.language
		}

		if language != "" {
			languageBytes := []byte(language)
			if pdf.encryption != nil {
				languageBytes, _ = encryption.Encrypt(languageBytes, pdf.encryption.GetKey())
			}
			pdf.appendString("/Lang <")
			pdf.appendString(hex.EncodeToString(languageBytes))
			pdf.appendString(">\n")
		}

		if hasActualText {
			actualTextBytes := []byte(element.actualText)
			if pdf.encryption != nil {
				actualTextBytes, _ = encryption.Encrypt(actualTextBytes, pdf.encryption.GetKey())
			}
			pdf.appendString("/ActualText <")
			pdf.appendString(hex.EncodeToString(actualTextBytes))
			pdf.appendString(">\n")
		}

		if hasAltDescription {
			altDescriptionBytes := []byte(element.altDescription)
			if pdf.encryption != nil {
				altDescriptionBytes, _ = encryption.Encrypt(altDescriptionBytes, pdf.encryption.GetKey())
			}
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
	// The keys must be listed in increasing order, so the page entries - whose
	// keys are the /StructParents values 0 .. len(pdf.pages)-1 - come first.
	// Each value is the array of struct elements of that page, indexed by the
	// MCID they were marked with.
	for i, page := range pdf.pages {
		pdf.appendInteger(i)
		pdf.appendString(" [")
		for _, element := range page.structures {
			if element.annotation == nil {
				pdf.appendString(" ")
				pdf.appendInteger(element.objNumber)
				pdf.appendString(" 0 R")
			}
		}
		pdf.appendString("]\n")
	}
	// The annotations follow, keyed by the /StructParent values handed out by
	// addAnnotDictionaries, which continue where the pages left off.
	structParent := len(pdf.pages)
	for _, element := range pdf.structElements {
		if element.annotation != nil {
			pdf.appendInteger(structParent)
			structParent++
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
		pdf.appendInteger(len(buf))
		pdf.appendString("\n")
		pdf.appendString(">>\n")
		pdf.appendString("stream\n")
		pdf.appendByteArray(buf)
		pdf.appendString("\nendstream\n")
		pdf.endobj()
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
		// PDF/UA requires a link to carry an alternate description in its
		// Contents key.
		description := annot.contents
		if description == "" {
			description = annot.altDescription
		}
		if description == "" {
			description = annot.uri
		}
		if description == "" {
			description = annot.key
		}
		if description != "" {
			bytes := []byte(description)
			if pdf.encryption != nil {
				bytes, _ = encryption.Encrypt(bytes, pdf.encryption.GetKey())
			}
			pdf.appendString("/Contents <")
			pdf.appendString(hex.EncodeToString(bytes))
			pdf.appendString(">\n")
		}
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
			element.annotation.structParentWritten = true
		}
	}

	for _, page := range pdf.pages {
		for _, annot := range page.annots {
			// Skip the annotations that were already written above - writing
			// them twice would leave the page referencing a copy that has no
			// /StructParent key.
			if !annot.structParentWritten {
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

// AddPages adds the pages to this document.
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
		if offset == 0 { // A number that no object was written for.
			pdf.appendString("0000000000 65535 f \n")
			continue
		}
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
func (pdf *PDF) SetLanguage(language string) *PDF {
	pdf.language = language
	return pdf
}

// SetTitle sets the "Title" document property of the PDF file.
func (pdf *PDF) SetTitle(title string) *PDF {
	pdf.title = title
	return pdf
}

// SetAuthor sets the "Author" document property of the PDF file.
func (pdf *PDF) SetAuthor(author string) *PDF {
	pdf.author = author
	return pdf
}

// SetSubject sets the "Subject" document property of the PDF file.
func (pdf *PDF) SetSubject(subject string) *PDF {
	pdf.subject = subject
	return pdf
}

// SetKeywords sets the keywords.
func (pdf *PDF) SetKeywords(keywords string) *PDF {
	pdf.keywords = keywords
	return pdf
}

// SetCreator sets the creator field of the PDF.
func (pdf *PDF) SetCreator(creator string) *PDF {
	pdf.creator = creator
	return pdf
}

// SetPageLayout sets the page layout.
func (pdf *PDF) SetPageLayout(pageLayout string) *PDF {
	pdf.pageLayout = pageLayout
	return pdf
}

// SetPageMode sets the page mode.
func (pdf *PDF) SetPageMode(pageMode string) *PDF {
	pdf.pageMode = pageMode
	return pdf
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
// An encrypted PDF is decrypted when it opens without a password.
// @param inputStream the PDF input stream.
// @return List<PDFobj> the list of PDF objects.
func (pdf *PDF) Read(buf []byte) []*PDFobj {
	return pdf.ReadWithPassword(buf, "")
}

// ReadWithPassword returns a list of objects of type PDFobj read from the
// bytes of a PDF that is encrypted with the standard security handler. The
// PDF is decrypted with the password, which is its user or its owner
// password.
// @param buf the bytes of the PDF.
// @param password the user or owner password of the PDF.
// @return List<PDFobj> the list of PDF objects.
func (pdf *PDF) ReadWithPassword(buf []byte, password string) []*PDFobj {
	objects1 := make([]*PDFobj, 0)
	trailer := getObjects(buf, pdf.getStartXRef(buf), &objects1, 0)
	if trailer == nil || len(objects1) == 0 {
		// The cross-reference table is missing or wrong, like in a PDF
		// that was changed without updating it.
		objects1 = objects1[:0]
		trailer = getObjectsByScanning(buf, &objects1)
	}
	dec, err := getDecryptor(trailer, objects1, password)
	if err != nil {
		log.Fatal(err)
	}

	objects2 := make([]*PDFobj, 0)
	for _, obj := range objects1 {
		objType := obj.getValue("/Type")
		if objType == "/XRef" {
			continue // Skip the cross-reference streams.
		}
		if dec != nil {
			if obj.number == dec.objNumber {
				continue // Skip the encryption dictionary.
			}
			dec.decryptStrings(obj)
		}
		if contains(obj.dict, "stream") {
			length := obj.GetLength(objects1)
			obj.setStreamAndData(buf, length, dec)
		}

		if objType == "/ObjStm" {
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
		} else {
			objects2 = append(objects2, obj)
		}
	}

	return pdf.getSortedObjects(objects2)
}

func process(obj *PDFobj, sb *strings.Builder, buf []byte, off int) bool {
	str := trimToken(sb.String())
	if str != "" {
		obj.dict = append(obj.dict, str)
	}
	sb.Reset()
	if str == "endobj" {
		return true
	} else if str == "stream" {
		obj.streamOffset = off
		if off < len(buf) && buf[off] == byte('\n') {
			obj.streamOffset++
		}
		return true
	} else if str == "startxref" {
		return true
	}
	return false
}

// trimToken removes the bytes up to the space at both ends, like trim() in
// Java. strings.TrimSpace also removes Unicode spaces like the no-break space,
// whose UTF-8 bytes can end a name.
func trimToken(str string) string {
	start, end := 0, len(str)
	for start < end && str[start] <= ' ' {
		start++
	}
	for end > start && str[end-1] <= ' ' {
		end--
	}
	return str[start:end]
}

// getObjectAt returns the object at the offset, which has no tokens when the
// offset is outside of the PDF.
func getObjectAt(buf []byte, off int) *PDFobj {
	if off < 0 || off >= len(buf) {
		return NewPDFobj()
	}
	return getObject(buf, off, len(buf))
}

func getObject(buf []byte, off, length int) *PDFobj {
	obj := NewPDFobj()
	obj.offset = off

	var token1 strings.Builder
	p := 0 // The nesting level of the parentheses in a literal string
	done := false
	for !done && off < length {
		b2 := buf[off]
		off++
		if p > 0 {
			// A literal string is one token, with its white space and
			// delimiters. A backslash escapes the character after it.
			token1.WriteByte(b2)
			if b2 == '\\' {
				if off < length {
					token1.WriteByte(buf[off])
					off++
				}
			} else if b2 == '(' {
				p++
			} else if b2 == ')' {
				p--
				if p == 0 {
					done = process(obj, &token1, buf, off)
				}
			}
		} else if b2 == '(' {
			done = process(obj, &token1, buf, off)
			if !done {
				token1.WriteByte(b2)
				p = 1
			}
		} else if isWhiteSpace(int(b2)) {
			done = process(obj, &token1, buf, off)
		} else if b2 == '/' {
			done = process(obj, &token1, buf, off)
			if !done {
				token1.WriteByte(b2)
			}
		} else if b2 == '<' || b2 == '>' {
			done = process(obj, &token1, buf, off)
			if !done {
				if off < length && buf[off] == b2 {
					if b2 == '<' {
						obj.dict = append(obj.dict, "<<")
					} else {
						obj.dict = append(obj.dict, ">>")
					}
					off++
				} else if b2 == '<' {
					// A hexadecimal string is one token, without its white space.
					token1.WriteByte(b2)
					for off < length && buf[off] != '>' {
						if !isWhiteSpace(int(buf[off])) {
							token1.WriteByte(buf[off])
						}
						off++
					}
					token1.WriteByte('>')
					off++
					done = process(obj, &token1, buf, off)
				} else {
					obj.dict = append(obj.dict, ">")
				}
			}
		} else if b2 == '%' {
			// A comment ends at the end of the line.
			done = process(obj, &token1, buf, off)
			for !done && off < length && buf[off] != '\n' && buf[off] != '\r' {
				off++
			}
		} else if b2 == '[' || b2 == ']' || b2 == '{' || b2 == '}' {
			done = process(obj, &token1, buf, off)
			if !done {
				obj.dict = append(obj.dict, string(b2))
			}
		} else {
			token1.WriteByte(b2)
		}
	}
	if !done {
		process(obj, &token1, buf, off) // The last token, at the end of the data.
	}

	return obj
}

func isWhiteSpace(c int) bool {
	return c == 0x00 || // Null
		c == 0x09 || // Horizontal Tab
		c == 0x0A || // Line Feed (LF)
		c == 0x0C || // Form Feed
		c == 0x0D || // Carriage Return (CR)
		c == 0x20 // Space
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

// toInteger returns the value of the token, or -1 when it is not an integer.
func toInteger(token string) int {
	if !isInteger(token) {
		return -1
	}
	value, err := strconv.ParseInt(token, 10, 32)
	if err != nil {
		return -1
	}
	return int(value)
}

// isObject returns true when the tokens of the object start with "number
// generation obj", where the number is above 0, and is the number that is
// given unless that is -1.
func isObject(obj *PDFobj, number int) bool {
	if len(obj.dict) < 3 || obj.dict[2] != "obj" || !isInteger(obj.dict[1]) {
		return false
	}
	n := toInteger(obj.dict[0])
	return n > 0 && (number == -1 || n == number)
}

// getObjects adds the objects of the cross-reference section at the offset to
// the list, after the objects of the sections before it, so that the newest
// version of an object that was updated comes last. A section is a
// cross-reference table, which can have an /XRefStm stream for the objects in
// object streams, or a cross-reference stream. It returns the trailer of the
// section, which is the cross-reference stream object when there is no table,
// or nil when an offset in the section is not that of its object.
func getObjects(buf []byte, offset int, objects *[]*PDFobj, depth int) *PDFobj {
	xref := getObjectAt(buf, offset)
	table := len(xref.dict) > 0 && xref.dict[0] == "xref"
	if depth > 1000 || (!table && !isObject(xref, -1)) {
		return nil
	}
	prev := xref.getValue("/Prev")
	if prev != "" && getObjects(buf, toInteger(prev), objects, depth+1) == nil {
		return nil
	}
	if table {
		// The objects in the table replace those in the /XRefStm stream.
		xrefStm := xref.getValue("/XRefStm")
		if xrefStm != "" && !getStreamObjects(buf, getObjectAt(buf, toInteger(xrefStm)), objects) {
			return nil
		}
		if !getTableObjects(buf, xref, objects) {
			return nil
		}
	} else if !getStreamObjects(buf, xref, objects) {
		return nil
	}
	return xref
}

// getTableObjects adds the objects in use of a cross-reference table, and
// returns false when an offset is not that of its object.
func getTableObjects(buf []byte, xref *PDFobj, objects *[]*PDFobj) bool {
	dict := xref.dict
	i := 1
	// Each subsection starts with its first object number and the number of entries.
	for i+1 < len(dict) && isInteger(dict[i]) {
		number := toInteger(dict[i])
		count := toInteger(dict[i+1])
		i += 2
		for j := 0; j < count; j, number, i = j+1, number+1, i+3 {
			if i+2 >= len(dict) {
				return false
			}
			// The entry is the offset, the generation number and n for an object in use.
			if dict[i+2] == "n" {
				obj := getObjectAt(buf, toInteger(dict[i]))
				if !isObject(obj, number) {
					return false
				}
				obj.number = number
				*objects = append(*objects, obj)
			}
		}
	}
	return i < len(dict) && dict[i] == "trailer"
}

// getStreamObjects adds the objects of a cross-reference stream that are not
// in object streams, and returns false when an offset is not that of its object.
func getStreamObjects(buf []byte, xref *PDFobj, objects *[]*PDFobj) bool {
	if !isObject(xref, -1) || xref.getValue("/Type") != "/XRef" || !contains(xref.dict, "stream") {
		return false
	}
	// See page 50 in PDF32000_2008.pdf
	dict := xref.dict
	w := slices.Index(dict, "/W")
	if w == -1 || w+4 >= len(dict) {
		return false
	}
	n1 := toInteger(dict[w+2]) // Field 1 number of bytes
	n2 := toInteger(dict[w+3]) // Field 2 number of bytes
	n3 := toInteger(dict[w+4]) // Field 3 number of bytes
	length := toInteger(xref.getValue("/Length"))
	if n1 < 0 || n2 < 0 || n3 < 0 || n1+n2+n3 == 0 ||
		length < 0 || xref.streamOffset+length > len(buf) {
		return false
	}
	// The /Index array has the first object number and the number of entries
	// of each subsection, and is [0 /Size] when it is missing.
	index := make([]int, 0)
	k := slices.Index(dict, "/Index")
	if k != -1 && k+1 < len(dict) && dict[k+1] == "[" {
		for k += 2; k+1 < len(dict) && isInteger(dict[k]); k += 2 {
			index = append(index, toInteger(dict[k]), toInteger(dict[k+1]))
		}
	} else {
		index = append(index, 0, toInteger(xref.getValue("/Size")))
	}

	// setStreamAndData undoes the predictor, so each entry is a row of the data.
	xref.setStreamAndData(buf, length, nil)
	n := n1 + n2 + n3 // Number of bytes per entry
	offset := 0
	for s := 0; s+1 < len(index); s += 2 {
		number := index[s]
		for j := 0; j < index[s+1] && offset+n <= len(xref.data); j++ {
			// Process the entries in a cross-reference stream.
			// Page 51 in PDF32000_2008.pdf
			entryType := 1
			if n1 > 0 {
				entryType = toInt(xref.data, offset, n1)
			}
			if entryType == 1 {
				obj := getObjectAt(buf, toInt(xref.data, offset+n1, n2))
				if !isObject(obj, number) {
					return false
				}
				obj.number = number
				*objects = append(*objects, obj)
			}
			number++
			offset += n
		}
	}
	return true
}

// getObjectsByScanning adds the objects of the PDF to the list by looking for
// "number generation obj" in it, for when the cross-reference table is missing
// or wrong. The objects of incremental updates are later in the PDF, so the
// newest version of an object comes last. It returns the last trailer, or the
// last cross-reference stream object when there is no trailer, or nil when
// there is neither.
func getObjectsByScanning(buf []byte, objects *[]*PDFobj) *PDFobj {
	var trailer *PDFobj
	var xrefStream *PDFobj
	i := 0
	for i < len(buf) {
		if isObjectStart(buf, i) {
			obj := getObjectAt(buf, i)
			if isObject(obj, -1) {
				obj.number = toInteger(obj.dict[0])
				*objects = append(*objects, obj)
				if obj.getValue("/Type") == "/XRef" {
					xrefStream = obj
				}
				if contains(obj.dict, "stream") {
					// Skip the stream, as its bytes can look like an object.
					end := indexOf(buf, "endstream", obj.streamOffset)
					if end == -1 {
						i = len(buf)
					} else {
						i = end
					}
					continue
				}
			}
		} else if startsWith(buf, i, "trailer") {
			trailer = getObjectAt(buf, i)
		}
		i++
	}
	if trailer != nil {
		return trailer
	}
	return xrefStream
}

// isObjectStart returns true when "number generation obj" starts at the
// offset, after white space or at the start of the PDF.
func isObjectStart(buf []byte, off int) bool {
	if off > 0 && !isWhiteSpace(int(buf[off-1])) {
		return false
	}
	i := off
	for i < len(buf) && buf[i] >= '0' && buf[i] <= '9' {
		i++
	}
	j := i
	for j < len(buf) && isWhiteSpace(int(buf[j])) {
		j++
	}
	k := j
	for k < len(buf) && buf[k] >= '0' && buf[k] <= '9' {
		k++
	}
	m := k
	for m < len(buf) && isWhiteSpace(int(buf[m])) {
		m++
	}
	return i > off && j > i && k > j && m > k && startsWith(buf, m, "obj")
}

func startsWith(buf []byte, off int, str string) bool {
	if off+len(str) > len(buf) {
		return false
	}
	for i := 0; i < len(str); i++ {
		if buf[off+i] != str[i] {
			return false
		}
	}
	return true
}

func indexOf(buf []byte, str string, from int) int {
	for i := max(from, 0); i+len(str) <= len(buf); i++ {
		if startsWith(buf, i, str) {
			return i
		}
	}
	return -1
}

// getStartXRef returns the offset after the last startxref, or -1 when there
// is none.
func (pdf *PDF) getStartXRef(buf []byte) int {
	for i := len(buf) - 9; i >= 0; i-- {
		if startsWith(buf, i, "startxref") {
			j := i + 9
			for j < len(buf) && isWhiteSpace(int(buf[j])) {
				j++
			}
			offset := 0
			k := j
			for k < len(buf) && buf[k] >= '0' && buf[k] <= '9' && offset <= 2147483647 {
				offset = offset*10 + int(buf[k]-'0')
				k++
			}
			if k > j && offset <= 2147483647 {
				return offset
			}
			return -1
		}
	}
	return -1
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
		if token1 == "/Type" && i+1 < len(obj.dict) && obj.dict[i+1] == "/Page" {
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
	i := 0
	for i < len(dict) && dict[i] != "/Font" {
		i++
	}
	i += 2 // Skip over "/Font" and the "<<" that follows it.

	// The sub-dictionary holds one "/Name <number> 0 R" entry per font. Every
	// one of them is re-emitted in the resources object, so every one of them
	// has to be collected here - taking only the first left the rest of the
	// references dangling.
	for i < len(dict) && dict[i] != ">>" {
		token1 := dict[i]
		if strings.HasPrefix(token1, "/") && i+3 < len(dict) && dict[i+3] == "R" {
			// Pages can carry separate resource dictionaries that name the same
			// fonts. They are merged into one /Font dictionary here, so a name
			// that is already present must not be added twice.
			if slices.Contains(pdf.importedFonts, token1) {
				i += 4
				continue
			}
			pdf.importedFonts = append(pdf.importedFonts,
				token1, dict[i+1], dict[i+2], dict[i+3])
			objNumber, err := strconv.Atoi(dict[i+1])
			if err != nil {
				log.Fatal(err)
			} else if objNumber > 0 && objNumber <= len(objects) {
				fonts = append(fonts, objects[objNumber-1])
			}
			i += 4
			continue
		}
		pdf.importedFonts = append(pdf.importedFonts, token1)
		i++
	}

	if len(fonts) == 0 {
		return nil
	}
	return fonts
}

func (pdf *PDF) getDescendantFonts(font *PDFobj, objects []*PDFobj) []*PDFobj {
	descendantFonts := make([]*PDFobj, 0)
	dict := font.GetDict()
	for i, token1 := range dict {
		if token1 == "/DescendantFonts" && i+2 < len(dict) {
			token1 = dict[i+2]
			if token1 != "]" {
				objNumber, err := strconv.Atoi(token1)
				if err != nil {
					log.Fatal(err)
				} else {
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
		if token1 == name && i+1 < len(dict) {
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
// addFontDescriptor collects the font descriptor of the given font, together
// with whichever embedded font program it carries.
func (pdf *PDF) addFontDescriptor(
	font *PDFobj, objects []*PDFobj, resources []*PDFobj) []*PDFobj {
	descriptor := pdf.getObjectFromObjects("/FontDescriptor", font, objects)
	if descriptor == nil {
		return resources
	}
	resources = append(resources, descriptor)
	for _, key := range []string{"/FontFile", "/FontFile2", "/FontFile3"} {
		fontFile := pdf.getObjectFromObjects(key, descriptor, objects)
		if fontFile != nil {
			resources = append(resources, fontFile)
		}
	}
	return resources
}

// getResourceEntries returns the entries of a sub-dictionary of the resources,
// like /XObject, without the brackets around them. The sub-dictionary can also
// be an object of its own.
func (pdf *PDF) getResourceEntries(resources *PDFobj, name string, objects []*PDFobj) []string {
	entries := make([]string, 0)
	dict := resources.GetDict()
	i := slices.Index(dict, name) + 1
	if i == 0 || i >= len(dict) {
		return entries
	}
	if isInteger(dict[i]) { // "/XObject 12 0 R"
		objNumber, err := strconv.Atoi(dict[i])
		if err != nil || objNumber < 1 || objNumber > len(objects) {
			return entries
		}
		dict = objects[objNumber-1].GetDict()
		i = slices.Index(dict, "<<")
		if i == -1 {
			return entries
		}
	}
	if dict[i] != "<<" {
		return entries
	}
	level := 1
	for i++; i < len(dict); i++ {
		token1 := dict[i]
		if token1 == "<<" {
			level++
		} else if token1 == ">>" {
			level--
			if level == 0 {
				break
			}
		}
		entries = append(entries, token1)
	}
	return entries
}

func isInteger(token1 string) bool {
	if token1 == "" {
		return false
	}
	for _, c := range token1 {
		if c < '0' || c > '9' {
			return false
		}
	}
	return true
}

// getReferences returns the numbers of the objects that "number 0 R"
// references in the tokens refer to.
func (pdf *PDF) getReferences(tokens []string) []int {
	numbers := make([]int, 0)
	for i := 0; i+2 < len(tokens); i++ {
		if tokens[i+2] == "R" && isInteger(tokens[i]) && isInteger(tokens[i+1]) {
			if objNumber, err := strconv.Atoi(tokens[i]); err == nil {
				numbers = append(numbers, objNumber)
			}
			i += 2
		}
	}
	return numbers
}

// addObjectTree collects the object with the given number and every object it
// refers to, directly or through other objects, like the color space of an
// image or the resources of a form XObject. The page tree is not followed.
func (pdf *PDF) addObjectTree(
	objNumber int, objects []*PDFobj, numbers map[int]bool, resources []*PDFobj) []*PDFobj {
	if objNumber <= 0 || objNumber > len(objects) || numbers[objNumber] {
		return resources
	}
	numbers[objNumber] = true
	obj := objects[objNumber-1]
	objType := obj.getValue("/Type")
	if len(obj.dict) == 0 || objType == "/Page" || objType == "/Pages" || objType == "/Catalog" {
		return resources
	}
	resources = append(resources, obj)
	for _, reference := range pdf.getReferences(obj.dict) {
		resources = pdf.addObjectTree(reference, objects, numbers, resources)
	}
	return resources
}

// addXObjects collects the images and forms in the /XObject resources, with
// the objects they use, and adds their names to the resources object.
func (pdf *PDF) addXObjects(
	resObj *PDFobj, objects []*PDFobj, numbers map[int]bool, resources []*PDFobj) []*PDFobj {
	entries := pdf.getResourceEntries(resObj, "/XObject", objects)
	i := 0
	for i < len(entries) {
		token1 := entries[i]
		if strings.HasPrefix(token1, "/") && i+3 < len(entries) && entries[i+3] == "R" {
			// Like the fonts, a name that an earlier page added is kept.
			if !slices.Contains(pdf.importedXObjects, token1) {
				pdf.importedXObjects = append(pdf.importedXObjects, entries[i:i+4]...)
				if objNumber, err := strconv.Atoi(entries[i+1]); err == nil {
					resources = pdf.addObjectTree(objNumber, objects, numbers, resources)
				}
			}
			i += 4
		} else {
			i++
		}
	}
	return resources
}

// AddResourceObjects adds the fonts, images and graphics states used by the pages to this document.
func (pdf *PDF) AddResourceObjects(objects []*PDFobj) {
	resources := make([]*PDFobj, 0)
	numbers := make(map[int]bool)

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
			// A simple font carries its descriptor directly; only a composite
			// one puts it on the descendant.
			resources = pdf.addFontDescriptor(font, objects, resources)
			descendantFonts := pdf.getDescendantFonts(font, objects)
			for _, descendantFont := range descendantFonts {
				resources = append(resources, descendantFont)
				resources = pdf.addFontDescriptor(descendantFont, objects, resources)
			}
		}
		resources = pdf.addXObjects(resObj, objects, numbers, resources)
		pdf.extGState = pdf.getExtGState(resObj)
		// The /ExtGState dictionary is copied as it is, so the objects that its
		// entries refer to have to be copied too.
		for _, objNumber := range pdf.getReferences(pdf.getResourceEntries(resObj, "/ExtGState", objects)) {
			resources = pdf.addObjectTree(objNumber, objects, numbers, resources)
		}
	}
	sort.SliceStable(resources, func(i, j int) bool {
		return resources[i].number < resources[j].number
	})

	// An object can be collected twice, like a font that a form XObject uses
	// too, and must be written once.
	unique := make([]*PDFobj, 0, len(resources))
	for _, obj := range resources {
		if len(unique) == 0 || unique[len(unique)-1].number != obj.number {
			unique = append(unique, obj)
		}
	}
	pdf.addObjectsToPDF(&unique)
}

func (pdf *PDF) addObjectsToPDF(objects *[]*PDFobj) {
	for _, obj := range *objects {
		if obj.offset == 0 {
			// Create new object.
			pdf.setObjOffset(obj.number, pdf.byteCount)
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
			pdf.setObjOffset(obj.number, pdf.byteCount)
			// Uncomment to see the format of the objects.
			// log.Println(obj.dict)
			n := len(obj.dict)
			var token1 string
			for i := 0; i < n; i++ {
				token1 = obj.dict[i]
				pdf.appendString(token1)
				if i < (n - 1) {
					pdf.appendString(" ")
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
