// pdf.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package pdfjet is a library for creating PDF documents.
package pdfjet

import (
	"bufio"
	"crypto/rand"
	"encoding/hex"
	"errors"
	"fmt"
	"os"
	"slices"
	"sort"
	"strconv"
	"strings"
	"time"
	"unicode/utf16"
	"unicode/utf8"

	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/internal/compressor"
	"github.com/edragoev1/pdfjet/v9/src/internal/fastfloat"
	"github.com/edragoev1/pdfjet/v9/src/internal/token"
	"github.com/edragoev1/pdfjet/v9/src/pagelayout"
	"github.com/edragoev1/pdfjet/v9/src/pagemode"
)

// PDF is used to create PDF objects.
type PDF struct {
	writer                    *bufio.Writer
	byteCount                 int64
	objOffsets                []int64
	fonts                     []*Font
	images                    []*Image
	pages                     []*Page
	destinations              map[string]*Destination
	groups                    []*OptionalContentGroup
	states                    map[string]int
	stamps                    []*Stamp
	metadataObjNumber         int
	outputIntentObjNumber     int
	compliance                compliance.Compliance
	encryption                *Encryption
	title                     string
	author                    string
	subject                   string
	keywords                  string
	producer                  string
	creator                   string
	createDate                string
	pagesObjNumber            int
	pageLayout                pagelayout.PageLayout
	pageMode                  pagemode.PageMode
	language                  string
	toc                       *Bookmark
	importedFonts             []string
	importedXObjects          []string
	importedExtGStates        []string
	uuid                      string
	prevPage                  *Page
	err                       error            // The first misuse of the API or error writing to the writer; Complete returns it.
	completed                 bool             // True after Complete.
	pagesCreated              int              // The pages made for this document, added or detached.
	annotElements             []*structElement // The elements of the annotations
	documentKids              []int            // The elements of the Document element, by number
	structTreeRootNumber      int              // Reserved with the two numbers after it,
	parentTreeNumber          int              // the parent tree and the Document element,
	documentElementNumber     int              // before the first element is written
	contentStreamsCompression bool
	file                      *os.File
}

// ocgObject holds an object number and a name.
type ocgObject struct {
	objNumber int
	name      string
}

// NewPDF creates a PDF document that is written to the writer.
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
//
// SetCompliance makes it a PDF/UA or PDF/A document; PDF/A requires all fonts
// to be embedded.
func NewPDF(w *bufio.Writer) *PDF {
	pdf := new(PDF)
	pdf.contentStreamsCompression = true
	pdf.writer = w
	pdf.producer = "PDFjet v9.0.1"
	pdf.language = "en-US"

	pdf.destinations = make(map[string]*Destination)
	// The document ID for the trailer and the XMP metadata: 16 random bytes as
	// 32 hexadecimal digits, so documents made at the same time get different IDs.
	id := make([]byte, 16)
	if _, err := rand.Read(id); err != nil {
		panic(err)
	}
	pdf.uuid = hex.EncodeToString(id)

	// The creation date is in UTC, so the XMP metadata says so with a Z.
	pdf.createDate = time.Now().UTC().Format("2006-01-02T15:04:05") + "Z"

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
func (pdf *PDF) SetCompliance(level compliance.Compliance) *PDF {
	// The fonts and the page content are written for the compliance.
	if level != pdf.compliance && (pdf.getObjNumber() > 0 || pdf.pagesCreated > 0) {
		pdf.fail("Set the compliance before adding fonts, images or pages to the PDF.")
		return pdf
	}
	pdf.compliance = level
	return pdf
}

// GetCompliance returns the PDF/UA or PDF/A compliance of this document. See the compliance package.
func (pdf *PDF) GetCompliance() compliance.Compliance {
	return pdf.compliance
}

// SetEncryption sets the encryption applied to this document.
func (pdf *PDF) SetEncryption(encryption *Encryption) *PDF {
	// Every object after the encryption dictionary is encrypted.
	if encryption != nil && encryption.getObjNumber() != pdf.getObjNumber() {
		pdf.fail("Set the encryption before adding fonts, images or pages to the PDF.")
		return pdf
	}
	// ISO 19005 does not allow a PDF/A document to be encrypted.
	if encryption != nil && pdf.compliance != compliance.PDF_1_7 && pdf.compliance != compliance.PDF_UA_1 {
		pdf.fail("A PDF/A document cannot be encrypted.")
		return pdf
	}
	pdf.encryption = encryption
	return pdf
}

// NewPDFFile creates a PDF document that is written to the file at the
// specified path. Complete closes the file.
func NewPDFFile(filePath string) (*PDF, error) {
	file, err := os.Create(filePath)
	if err != nil {
		return nil, err
	}
	pdf := NewPDF(bufio.NewWriter(file))
	pdf.file = file
	return pdf, nil
}

// NewPDFReader creates a PDF that only reads existing documents with Read and
// ReadWithPassword, like the PDF() constructor of the other ports. Use NewPDF
// or NewPDFFile to write a document.
func NewPDFReader() *PDF {
	pdf := new(PDF)
	pdf.destinations = make(map[string]*Destination)
	pdf.states = make(map[string]int)
	pdf.stamps = make([]*Stamp, 0)
	return pdf
}

// fail records the first misuse of the API and returns it as an error. The
// call that finds the misuse writes nothing broken where it can, and Complete
// refuses to finish the document, as the file would be broken.
func (pdf *PDF) fail(message string) error {
	err := errors.New(message)
	if pdf.err == nil {
		pdf.err = err
	}
	return err
}

func (pdf *PDF) newObj() {
	pdf.objOffsets = append(pdf.objOffsets, pdf.byteCount)
	pdf.appendInteger(len(pdf.objOffsets))
	pdf.appendString(" 0 obj\n")
}

func (pdf *PDF) endObj() {
	pdf.appendString("endobj\n")
}

func (pdf *PDF) getObjNumber() int {
	return len(pdf.objOffsets)
}

// setObjOffset records the offset of an object that carries its own number,
// growing the table with placeholders for any number that has no object yet.
func (pdf *PDF) setObjOffset(number int, offset int64) {
	if number <= 0 { // No number of its own - just append.
		pdf.objOffsets = append(pdf.objOffsets, offset)
		return
	}
	for len(pdf.objOffsets) < number {
		pdf.objOffsets = append(pdf.objOffsets, 0)
	}
	pdf.objOffsets[number-1] = offset
}

// xrefOffset returns the offset as the 10 digits of an entry of the
// cross-reference table, which cannot hold an offset of more than 10 digits:
// it returns an error for one, where the other ports throw.
func xrefOffset(offset int64) (string, error) {
	digits := strconv.FormatInt(offset, 10)
	if len(digits) > 10 {
		return "", fmt.Errorf("The PDF is too large for a cross-reference table: an object starts at byte %d.", offset)
	}
	return "0000000000"[len(digits):] + digits, nil
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
			sb.WriteString(escapeXML(pdf.title))
			sb.WriteString("</rdf:li></rdf:Alt></dc:title>\n")
		}

		if pdf.author != "" {
			sb.WriteString("  <dc:creator><rdf:Seq><rdf:li>")
			sb.WriteString(escapeXML(pdf.author))
			sb.WriteString("</rdf:li></rdf:Seq></dc:creator>\n")
		}

		if pdf.subject != "" {
			sb.WriteString("  <dc:description><rdf:Alt><rdf:li xml:lang=\"x-default\">")
			sb.WriteString(escapeXML(pdf.subject))
			sb.WriteString("</rdf:li></rdf:Alt></dc:description>\n")
		}

		if pdf.keywords != "" {
			sb.WriteString("  <pdf:Keywords>")
			sb.WriteString(escapeXML(pdf.keywords))
			sb.WriteString("</pdf:Keywords>\n")
		}

		if pdf.creator != "" {
			sb.WriteString("  <xmp:CreatorTool>")
			sb.WriteString(escapeXML(pdf.creator))
			sb.WriteString("</xmp:CreatorTool>\n")
		}

		sb.WriteString("  <xmp:CreateDate>")
		sb.WriteString(pdf.createDate)
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
		xml = pdf.encryption.encrypt(xml)
	}

	// This is the metadata object
	pdf.newObj()
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
	pdf.endObj()

	return pdf.getObjNumber()
}

// escapeXML returns the text with the characters that have a meaning in XML
// escaped, and without what XML does not allow: invalid UTF-8, the control
// characters other than tab, line feed and carriage return, U+FFFE and
// U+FFFF, which would make the metadata unreadable.
func escapeXML(text string) string {
	var sb strings.Builder
	for i, r := range text {
		switch {
		case r == utf8.RuneError && strings.HasPrefix(text[i:], "\xef\xbf\xbd") == false:
			// Invalid UTF-8
		case r == '&':
			sb.WriteString("&amp;")
		case r == '<':
			sb.WriteString("&lt;")
		case r == '>':
			sb.WriteString("&gt;")
		case r == '\t' || r == '\n' || r == '\r' || (r >= 0x20 && r != 0xFFFE && r != 0xFFFF):
			sb.WriteRune(r)
		}
	}
	return sb.String()
}

func (pdf *PDF) addOutputIntentObject() int {
	profile := iccBlackScaledProfile
	if pdf.encryption != nil {
		profile = pdf.encryption.encrypt(profile)
	}

	pdf.newObj()
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
	pdf.endObj()

	identifierBytes := []byte("sRGB IEC61966-2.1")
	if pdf.encryption != nil {
		identifierBytes = pdf.encryption.encrypt(identifierBytes)
	}
	// OutputIntent object
	pdf.newObj()
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
	pdf.endObj()

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
	pdf.newObj()
	pdf.appendByteArray(token.BeginDictionary)
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
	// The graphics states of a PDF that was read and those of the pages go in
	// the same dictionary.
	if len(pdf.states) > 0 || len(pdf.importedExtGStates) > 0 {
		pdf.appendString("/ExtGState <<\n")
		pdf.appendImportedEntries(pdf.importedExtGStates)
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
	pdf.endObj()
	return pdf.getObjNumber()
}

func (pdf *PDF) addPagesObject() {
	pdf.setObjOffset(pdf.pagesObjNumber, pdf.byteCount)
	pdf.appendInteger(pdf.pagesObjNumber)
	pdf.appendString(" 0 obj\n")
	pdf.appendByteArray(token.BeginDictionary)
	pdf.appendString("/Type /Pages\n")
	pdf.appendString("/Kids [\n")
	for _, page := range pdf.pages {
		pdf.appendInteger(page.objNumber)
		pdf.appendString(" 0 R\n")
	}
	pdf.appendString("]\n")
	pdf.appendString("/Count ")
	pdf.appendInteger(len(pdf.pages))
	pdf.appendByte('\n')
	pdf.appendByteArray(token.EndDictionary)
	pdf.endObj()
}

func (pdf *PDF) addStructTreeRootObject() int {
	pdf.setObjOffset(pdf.structTreeRootNumber, pdf.byteCount)
	pdf.appendInteger(pdf.structTreeRootNumber)
	pdf.appendString(" 0 obj\n")
	pdf.appendByteArray(token.BeginDictionary)
	pdf.appendString("/Type /StructTreeRoot\n")
	pdf.appendString("/ParentTree ")
	pdf.appendInteger(pdf.parentTreeNumber)
	pdf.appendString(" 0 R\n")
	pdf.appendString("/K [\n")
	pdf.appendInteger(pdf.documentElementNumber)
	pdf.appendString(" 0 R\n")
	pdf.appendString("]\n")
	pdf.appendByteArray(token.EndDictionary)
	pdf.endObj()
	return pdf.structTreeRootNumber
}

func (pdf *PDF) addStructDocumentObject(parent int) int {
	pdf.setObjOffset(pdf.documentElementNumber, pdf.byteCount)
	pdf.appendInteger(pdf.documentElementNumber)
	pdf.appendString(" 0 obj\n")
	pdf.appendByteArray(token.BeginDictionary)
	pdf.appendString("/Type /StructElem\n")
	pdf.appendString("/S /Document\n")
	pdf.appendString("/P ")
	pdf.appendInteger(parent)
	pdf.appendByteArray(token.ObjRef)
	pdf.appendString("/K [\n")
	for _, number := range pdf.documentKids {
		pdf.appendInteger(number)
		pdf.appendByteArray(token.ObjRef)
	}
	pdf.appendString("]\n")
	pdf.appendByteArray(token.EndDictionary)
	pdf.endObj()
	return pdf.documentElementNumber
}

// reserveStructTreeNumbers reserves the numbers of the structure tree root,
// the parent tree and the Document element, which the elements written with
// their pages refer to before the three are written.
func (pdf *PDF) reserveStructTreeNumbers() {
	if pdf.structTreeRootNumber == 0 {
		pdf.structTreeRootNumber = pdf.reserveObjNumber()
		pdf.parentTreeNumber = pdf.reserveObjNumber()
		pdf.documentElementNumber = pdf.reserveObjNumber()
	}
}

// addStructElementObject writes one structure element, under the number it was
// given when it was made.
func (pdf *PDF) addStructElementObject(element *structElement) {
	{
		pdf.setObjOffset(element.objNumber, pdf.byteCount)
		pdf.appendInteger(element.objNumber)
		pdf.appendString(" 0 obj\n")
		pdf.appendString("<<\n/Type /StructElem /S /")
		pdf.appendString(element.structure)
		pdf.appendString("\n/P ")
		if element.parent != nil {
			pdf.appendInteger(element.parent.objNumber)
		} else {
			pdf.appendInteger(pdf.documentElementNumber)
		}
		pdf.appendString(" 0 R /Pg ")
		pdf.appendInteger(element.pageObjNumber)
		pdf.appendString(" 0 R\n")

		if element.annotation != nil {
			pdf.appendString("/K <</Type /OBJR /Obj ")
			pdf.appendInteger(element.annotation.objNumber)
			pdf.appendString(" 0 R>>\n")
		} else if element.mcid >= 0 {
			pdf.appendString("/K ")
			pdf.appendInteger(element.mcid)
			pdf.appendString("\n")
		} else if len(element.mcids) > 0 {
			// The marked contents of a paragraph drawn word by word.
			pdf.appendString("/K [")
			for i, mcid := range element.mcids {
				if i > 0 {
					pdf.appendString(" ")
				}
				pdf.appendInteger(mcid)
			}
			pdf.appendString("]\n")
		} else if len(element.kids) > 0 {
			pdf.appendString("/K [")
			for _, kid := range element.kids {
				pdf.appendInteger(kid)
				pdf.appendString(" 0 R ")
			}
			pdf.appendString("]\n")
		}

		if element.attributes != "" {
			pdf.appendString("/A ")
			pdf.appendString(element.attributes)
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
				languageBytes = pdf.encryption.encrypt(languageBytes)
			}
			pdf.appendString("/Lang <")
			pdf.appendString(hex.EncodeToString(languageBytes))
			pdf.appendString(">\n")
		}

		if hasActualText {
			pdf.appendString("/ActualText ")
			pdf.appendTextString(element.actualText)
			pdf.appendString("\n")
		}

		if hasAltDescription {
			pdf.appendString("/Alt ")
			pdf.appendTextString(element.altDescription)
			pdf.appendString("\n")
		}

		pdf.appendString(">>\n")
		pdf.endObj()
	}
}

func (pdf *PDF) addNumsParentTree() {
	pdf.setObjOffset(pdf.parentTreeNumber, pdf.byteCount)
	pdf.appendInteger(pdf.parentTreeNumber)
	pdf.appendString(" 0 obj\n")
	pdf.appendString("<<\n")
	pdf.appendString("/Nums [\n")
	// The keys must be listed in increasing order, so the page entries - whose
	// keys are the /StructParents values 0 .. len(pdf.pages)-1 - come first.
	// Each value is the array of struct elements of that page, indexed by the
	// MCID they were marked with.
	for i, page := range pdf.pages {
		pdf.appendInteger(i)
		pdf.appendString(" [")
		for _, number := range page.mcidNumbers {
			pdf.appendString(" ")
			pdf.appendInteger(number)
			pdf.appendString(" 0 R")
		}
		pdf.appendString("]\n")
		page.mcidNumbers = nil
	}
	// The annotations follow, keyed by the /StructParent values handed out by
	// addAnnotDictionaries, which continue where the pages left off.
	structParent := len(pdf.pages)
	for _, element := range pdf.annotElements {
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
	pdf.endObj()
}

// addInfoObject adds the document information dictionary, which readers like
// pdfinfo show. It says what the XMP metadata of PDF/A and PDF/UA documents says.
func (pdf *PDF) addInfoObject() int {
	pdf.newObj()
	pdf.appendString("<<\n")
	pdf.appendInfoText("/Title", pdf.title)
	pdf.appendInfoText("/Author", pdf.author)
	pdf.appendInfoText("/Subject", pdf.subject)
	pdf.appendInfoText("/Keywords", pdf.keywords)
	pdf.appendInfoText("/Creator", pdf.creator)
	pdf.appendInfoText("/Producer", pdf.producer)
	// The XMP creation date 2026-01-31T12:00:00Z is D:20260131120000Z.
	date := "D:" + strings.NewReplacer("-", "", "T", "", ":", "").Replace(pdf.createDate)
	pdf.appendInfoString("/CreationDate", []byte(date))
	pdf.appendString(">>\n")
	pdf.endObj()
	return pdf.getObjNumber()
}

// appendInfoText appends an entry of the information dictionary with the text,
// unless the text is empty.
func (pdf *PDF) appendInfoText(key, text string) {
	if text == "" {
		return
	}
	pdf.appendString(key)
	pdf.appendString(" ")
	pdf.appendTextString(text)
	pdf.appendString("\n")
}

// appendTextString appends a text string, like a bookmark title or an alternate
// description: UTF-16BE with a byte order mark in hexadecimal, encrypted if the
// document is encrypted. A text string without the mark is in PDFDocEncoding,
// so UTF-8 bytes would show as two or three wrong characters each.
func (pdf *PDF) appendTextString(text string) {
	bytes := []byte{0xFE, 0xFF}
	for _, unit := range utf16.Encode([]rune(text)) {
		bytes = append(bytes, byte(unit>>8), byte(unit))
	}
	if pdf.encryption != nil {
		bytes = pdf.encryption.encrypt(bytes)
	}
	pdf.appendString("<")
	pdf.appendString(hex.EncodeToString(bytes))
	pdf.appendString(">")
}

// appendInfoString appends an entry of the information dictionary with the
// bytes of a string, encrypted if the document is encrypted.
func (pdf *PDF) appendInfoString(key string, bytes []byte) {
	if pdf.encryption != nil {
		bytes = pdf.encryption.encrypt(bytes)
	}
	pdf.appendString(key)
	pdf.appendString(" <")
	pdf.appendString(hex.EncodeToString(bytes))
	pdf.appendString(">\n")
}

func (pdf *PDF) addRootObject(structTreeRootObjNumber, outlineDictNumber int) int {
	// Add the root object
	pdf.newObj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /Catalog\n")

	if pdf.compliance != compliance.PDF_1_7 {
		languageBytes := []byte(pdf.language)
		if pdf.encryption != nil {
			languageBytes = pdf.encryption.encrypt(languageBytes)
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
		pdf.appendString(string(pdf.pageLayout))
		pdf.appendString("\n")
	}

	if pdf.pageMode != "" {
		pdf.appendString("/PageMode /")
		pdf.appendString(string(pdf.pageMode))
		pdf.appendString("\n")
	}

	pdf.addOCProperties()

	pdf.appendString("/Pages ")
	pdf.appendInteger(pdf.pagesObjNumber)
	pdf.appendString(" 0 R\n")

	if pdf.compliance != compliance.PDF_1_7 {
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
	pdf.endObj()
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

// setDestinationObjNumbers gives every destination the object number of the
// page it is on, which the page was given when it was added.
func (pdf *PDF) setDestinationObjNumbers() {
	for _, page := range pdf.pages {
		for _, destination := range page.destinations {
			destination.pageObjNumber = page.objNumber
			pdf.destinations[destination.name] = destination
		}
	}
}

func (pdf *PDF) addAllPages(resObjNumber int) {
	pdf.setDestinationObjNumbers()
	pdf.addAnnotDictionaries()

	// Calculate the object number of the Pages object, which comes after the
	// objects of the pages drawn with PDFjet.
	pdf.pagesObjNumber = pdf.reserveObjNumber()

	for i, page := range pdf.pages {
		if page.mergedDict != nil {
			dict := setDictEntry(append([]string(nil), page.mergedDict...),
				"/Parent", []string{strconv.Itoa(pdf.pagesObjNumber), "0", "R"})
			pdf.setObjOffset(page.objNumber, pdf.byteCount)
			pdf.appendInteger(page.objNumber)
			pdf.appendString(" 0 obj\n")
			pdf.appendTokens(dict)
			pdf.appendString("\n")
			pdf.appendString("endobj\n")
			continue
		}
		// Page object, under the number it was given when it was added.
		pdf.setObjOffset(page.objNumber, pdf.byteCount)
		pdf.appendInteger(page.objNumber)
		pdf.appendString(" 0 obj\n")
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

		if page.rotateDegrees != 0.0 {
			pdf.appendString("/Rotate ")
			pdf.appendFloat32(page.rotateDegrees)
			pdf.appendString("\n")
		}

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

		if pdf.compliance != compliance.PDF_1_7 {
			pdf.appendString("/Tabs /S\n")
			pdf.appendString("/StructParents ")
			pdf.appendInteger(i)
			pdf.appendString("\n")
		}

		pdf.appendString(">>\n")
		pdf.endObj()
	}
}

func (pdf *PDF) addPageContent(page *Page) {
	page.checkBalanced()
	page.written = true
	if pdf.contentStreamsCompression {
		compressed := compressor.Deflate(page.buf)
		if pdf.encryption != nil {
			compressed = pdf.encryption.encrypt(compressed)
		}
		page.buf = nil // Release the page content memory!

		pdf.newObj()
		pdf.appendString("<<\n")
		pdf.appendString("/Filter /FlateDecode\n")
		pdf.appendString("/Length ")
		pdf.appendInteger(len(compressed))
		pdf.appendString("\n")
		pdf.appendString(">>\n")
		pdf.appendString("stream\n")
		pdf.appendByteArray(compressed)
		pdf.appendString("\nendstream\n")
		pdf.endObj()
		page.contents = append(page.contents, pdf.getObjNumber())
	} else { // No compression. Used for diagnostics
		buf := page.buf
		if pdf.encryption != nil {
			buf = pdf.encryption.encrypt(buf)
		}
		page.buf = nil // Release the page content memory!

		pdf.newObj()
		pdf.appendString("<<\n")
		pdf.appendString("/Length ")
		pdf.appendInteger(len(buf))
		pdf.appendString("\n")
		pdf.appendString(">>\n")
		pdf.appendString("stream\n")
		pdf.appendByteArray(buf)
		pdf.appendString("\nendstream\n")
		pdf.endObj()
		page.contents = append(page.contents, pdf.getObjNumber())
	}
	pdf.addPageStructElements(page)
}

// addPageStructElements writes the structure elements of a page that is
// written, so that a document of many pages holds no more of them than the
// page it is drawing. What it keeps is the elements that are still open, the
// ones of an annotation, whose object is written when the document is
// completed, and the number of each element of the page by its marked
// content, which the parent tree is written from.
func (pdf *PDF) addPageStructElements(page *Page) {
	if pdf.compliance == compliance.PDF_1_7 || len(page.structures) == 0 {
		return
	}
	elements := page.structures
	kept := elements[:0]
	for _, element := range elements {
		setMcidNumber := func(mcid int) {
			if mcid < 0 {
				return
			}
			for len(page.mcidNumbers) <= mcid {
				page.mcidNumbers = append(page.mcidNumbers, 0)
			}
			page.mcidNumbers[mcid] = element.objNumber
		}
		setMcidNumber(element.mcid)
		for _, mcid := range element.mcids {
			setMcidNumber(mcid)
		}
		if element.parent == nil {
			pdf.documentKids = append(pdf.documentKids, element.objNumber)
		}
		if element.annotation != nil {
			pdf.annotElements = append(pdf.annotElements, element)
		}
		if element.open || element.annotation != nil {
			kept = append(kept, element)
			continue
		}
		pdf.addStructElementObject(element)
	}
	// The elements that were written are let go of: the tail of the array
	// that is left over still points at them.
	for i := len(kept); i < len(elements); i++ {
		elements[i] = nil
	}
	page.structures = kept
}

func (pdf *PDF) addAnnotationObject(annot *annotationObject, index int) int {
	pdf.newObj()
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

	if annot.annotationType == annotationFileAttachment {
		pdf.appendString("/FS ")
		pdf.appendString(strconv.Itoa(annot.fileAttachment.embeddedFile.objNumber))
		pdf.appendString(" 0 R\n")
		pdf.appendString("/Name /")
		pdf.appendString(annot.fileAttachment.icon)
		pdf.appendString("\n")

		if annot.fileAttachment.title != "" {
			pdf.appendString("/T ")
			pdf.appendTextString(annot.fileAttachment.title)
			pdf.appendString("\n")
		}

		if annot.fileAttachment.contents != "" {
			pdf.appendString("/Contents ")
			pdf.appendTextString(annot.fileAttachment.contents)
			pdf.appendString("\n")
		}
	} else if annot.annotationType == annotationLink {
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
			pdf.appendString("/Contents ")
			pdf.appendTextString(description)
			pdf.appendString("\n")
		}
		if annot.uri != "" {
			pdf.appendString("/F 4\n")
			pdf.appendString("/A <<\n")
			pdf.appendString("/S /URI\n")
			uri := []byte(annot.uri)
			if pdf.encryption != nil {
				uri = pdf.encryption.encrypt(uri)
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
	} else if annot.annotationType == annotationPolygon {
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
		pdf.appendFloat32(annot.opacity)
		pdf.appendString("\n")

		if annot.title != "" {
			pdf.appendString("/T ")
			pdf.appendTextString(annot.title)
			pdf.appendString("\n")
		}

		if annot.contents != "" {
			pdf.appendString("/Contents ")
			pdf.appendTextString(annot.contents)
			pdf.appendString("\n")
		}
	} else if annot.annotationType == annotationSquare ||
		annot.annotationType == annotationCircle {
		pdf.appendString("/IC [")
		pdf.appendFloat32(annot.fillColor[0])
		pdf.appendString(" ")
		pdf.appendFloat32(annot.fillColor[1])
		pdf.appendString(" ")
		pdf.appendFloat32(annot.fillColor[2])
		pdf.appendString("]\n")

		pdf.appendString("/CA ")
		pdf.appendFloat32(annot.opacity)
		pdf.appendString("\n")

		if annot.title != "" {
			pdf.appendString("/T ")
			pdf.appendTextString(annot.title)
			pdf.appendString("\n")
		}

		if annot.contents != "" {
			pdf.appendString("/Contents ")
			pdf.appendTextString(annot.contents)
			pdf.appendString("\n")
		}
	} else if annot.annotationType == annotationText {
		pdf.appendString("/Name /Comment\n")

		if annot.title != "" {
			pdf.appendString("/T ")
			pdf.appendTextString(annot.title)
			pdf.appendString("\n")
		}

		if annot.contents != "" {
			pdf.appendString("/Contents ")
			pdf.appendTextString(annot.contents)
			pdf.appendString("\n")
		}
	}

	if index != -1 {
		pdf.appendString("/StructParent ")
		pdf.appendInteger(index)
		pdf.appendString("\n")
		index++
	}
	pdf.appendString(">>\n")
	pdf.endObj()

	return index
}

func (pdf *PDF) addAnnotDictionaries() {
	index := len(pdf.pages)
	for _, element := range pdf.annotElements {
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
		var list []ocgObject
		var buf strings.Builder
		for _, ocg := range pdf.groups {
			buf.WriteString(" ")
			buf.WriteString(strconv.Itoa(ocg.objNumber))
			buf.WriteString(" 0 R")
			list = append(list, ocgObject{
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

		// The groups hidden by default, for the viewers that read the
		// configuration and not the usage of each group
		var off strings.Builder
		for _, ocg := range pdf.groups {
			if !ocg.visible {
				off.WriteString(" ")
				off.WriteString(strconv.Itoa(ocg.objNumber))
				off.WriteString(" 0 R")
			}
		}
		if off.Len() > 0 {
			pdf.appendString("/OFF [")
			pdf.appendString(off.String())
			pdf.appendString(" ]\n")
		}

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
	if pdf.completed {
		pdf.fail("The PDF was already completed.")
		return
	}
	if page.pdf != pdf {
		pdf.fail("The page belongs to another PDF.")
		return
	}
	if page.added {
		pdf.fail("The page was already added to the PDF.")
		return
	}
	page.added = true
	if page.objNumber == 0 {
		page.objNumber = pdf.reserveObjNumber()
	}
	// A page that was drawn before it was added has elements of its own.
	if pdf.compliance != compliance.PDF_1_7 {
		page.setStructElementsPageObjNumber(page.objNumber)
	}
	pdf.pages = append(pdf.pages, page)
	if pdf.prevPage != nil {
		pdf.addPageContent(pdf.prevPage)
	}
	pdf.prevPage = page
}

// Merge adds all the pages of a document that was read with Read after the
// pages of this document, in their order. A PDF can merge several documents
// and draw pages of its own before, between and after them.
//
// The merged pages keep their content, resources, annotations and links. The
// parts of the read document that belong to the whole document are left out:
// its bookmarks, form fields, tagging, named destinations and optional content
// settings. The objects that the pages use are written at once, so the objects
// are not needed after the call.
//
// A PDF/UA or PDF/A document cannot merge pages, which were not made for its
// compliance, and Merge cannot be used with AddObjects: Merge records the
// mistake and returns it as an error.
func (pdf *PDF) Merge(objects []*PDFobj) error {
	if err := pdf.checkMerge(objects); err != nil {
		return err
	}
	pdf.mergePages(objects, pdf.GetPageObjects(objects))
	return nil
}

// MergePages adds the listed pages of a document that was read with Read after
// the pages of this document, in the order they are listed. A document is split
// by merging each part of it into a PDF of its own: the objects that Read
// returned can be merged into any number of PDFs.
//
// The pages are merged as Merge merges all of them, and a link to a page that
// is not merged leads nowhere. A page number that the document does not have,
// or one that is listed twice, is refused: MergePages records the mistake and
// returns it as an error.
func (pdf *PDF) MergePages(objects []*PDFobj, pageNumbers ...int) error {
	if err := pdf.checkMerge(objects); err != nil {
		return err
	}
	pageObjects := pdf.GetPageObjects(objects)
	listed := make([]*PDFobj, 0, len(pageNumbers))
	seen := make(map[int]bool)
	for _, number := range pageNumbers {
		if number < 1 || number > len(pageObjects) {
			return pdf.fail("The document has no page " + strconv.Itoa(number) + ".")
		}
		if seen[number] {
			return pdf.fail("Page " + strconv.Itoa(number) + " is listed twice.")
		}
		seen[number] = true
		listed = append(listed, pageObjects[number-1])
	}
	pdf.mergePages(objects, listed)
	return nil
}

// checkMerge refuses a merge that would break this document.
func (pdf *PDF) checkMerge(objects []*PDFobj) error {
	if pdf.completed {
		return pdf.fail("The PDF was already completed.")
	}
	if pdf.compliance != compliance.PDF_1_7 {
		return pdf.fail("Pages of an existing PDF cannot be merged into a PDF/UA or PDF/A document.")
	}
	if pdf.pagesObjNumber != 0 {
		return pdf.fail("Merge and AddObjects cannot be used on the same PDF.")
	}
	if pdf.getPagesObject(objects) == nil {
		return pdf.fail("The objects have no root /Pages object.")
	}
	return nil
}

// mergePages adds the pages, in their order, and every object that they use.
func (pdf *PDF) mergePages(objects []*PDFobj, pageObjects []*PDFobj) {
	mergedPages := make(map[int]bool)
	for _, page := range pageObjects {
		mergedPages[page.number] = true
	}

	// Each page and every object that it uses, found through the references,
	// gets a number of this document before anything is written, as the
	// objects refer to each other: a page to its annotations, and a link
	// annotation to the page it points at.
	numbers := make(map[int]int)
	values := make(map[int][]string)
	queue := make([]*PDFobj, 0)
	for _, page := range pageObjects {
		if _, ok := numbers[page.number]; !ok {
			numbers[page.number] = pdf.reserveObjNumber()
			queue = append(queue, page)
		}
	}
	for i := 0; i < len(queue); i++ {
		obj := queue[i]
		value := mergedValue(obj, mergedPages[obj.number], objects)
		values[obj.number] = value
		for j := 0; j < len(value); j++ {
			if isObjectReference(value, j) {
				number, _ := strconv.Atoi(value[j])
				if _, ok := numbers[number]; !ok && isMergedObject(number, objects, mergedPages) {
					numbers[number] = pdf.reserveObjNumber()
					queue = append(queue, objects[number-1])
				}
				j += 2
			}
		}
	}

	for _, obj := range queue {
		if !mergedPages[obj.number] {
			pdf.addMergedObject(obj, numbers[obj.number], pdf.renumbered(values[obj.number], numbers))
		}
	}
	for _, obj := range queue {
		if mergedPages[obj.number] {
			pdf.pages = append(pdf.pages,
				newMergedPage(pdf, numbers[obj.number], pdf.renumbered(values[obj.number], numbers)))
		}
	}
}

// inheritedKeys are the entries of a page that it can inherit from the page tree.
var inheritedKeys = []string{"/Resources", "/MediaBox", "/CropBox", "/Rotate"}

// reserveObjNumber reserves the next object number for an object written later.
func (pdf *PDF) reserveObjNumber() int {
	pdf.objOffsets = append(pdf.objOffsets, 0)
	return len(pdf.objOffsets)
}

// isObjectReference returns true when the tokens at index i are a reference: "n g R".
func isObjectReference(tokens []string, i int) bool {
	return i+2 < len(tokens) &&
		tokens[i+2] == "R" &&
		isObjectNumberToken(tokens[i]) &&
		isObjectNumberToken(tokens[i+1])
}

func isObjectNumberToken(token string) bool {
	if token == "" || len(token) > 9 {
		return false
	}
	for i := 0; i < len(token); i++ {
		if token[i] < '0' || token[i] > '9' {
			return false
		}
	}
	return true
}

// isMergedObject returns true for an object that the merged pages can use:
// not the page tree, the catalog, a page that is not merged or an object that
// is missing.
func isMergedObject(number int, objects []*PDFobj, mergedPages map[int]bool) bool {
	if number < 1 || number > len(objects) {
		return false
	}
	obj := objects[number-1]
	if obj == nil || len(obj.dict) == 0 {
		return false
	}
	objType := obj.GetValue("/Type")
	if objType == "/Pages" || objType == "/Catalog" {
		return false
	}
	return !isPageObject(obj) || mergedPages[number]
}

// mergedValue returns the value of an object that was read, without its
// "n g obj" and its "stream" and "endobj" keywords, with a direct /Length for a
// stream, and for a page with the entries it inherits and without its /Parent.
func mergedValue(obj *PDFobj, isPage bool, objects []*PDFobj) []string {
	value := objectValue(obj)
	if obj.stream != nil {
		value = setDictEntry(value, "/Length", []string{strconv.Itoa(len(obj.stream))})
	}
	if isPage {
		for _, key := range inheritedKeys {
			if dictEntryIndex(value, key) == -1 {
				inherited := inheritedPageValue(obj, key, objects)
				if inherited == nil && key == "/MediaBox" {
					inherited = []string{"[", "0", "0", "612", "792", "]"} // Letter
				}
				if inherited != nil {
					value = setDictEntry(value, key, inherited)
				}
			}
		}
		value = removeDictEntry(value, "/Parent")
	}
	return value
}

func objectValue(obj *PDFobj) []string {
	dict := obj.dict
	start := 0
	if len(dict) >= 3 && dict[2] == "obj" {
		start = 3
	}
	end := len(dict)
	if end > start && dict[end-1] == "endobj" {
		end--
	}
	if end > start && dict[end-1] == "stream" {
		end--
	}
	return append([]string(nil), dict[start:end]...)
}

// inheritedPageValue returns the value of the entry from the nearest node of
// the page tree above the page that has it, or nil.
func inheritedPageValue(page *PDFobj, key string, objects []*PDFobj) []string {
	node := page
	for depth := 0; depth < 64; depth++ { // A loop in a broken tree ends here.
		tokens := objectValue(node)
		i := dictEntryIndex(tokens, "/Parent")
		if i == -1 || !isObjectReference(tokens, i+1) {
			return nil
		}
		number, _ := strconv.Atoi(tokens[i+1])
		if number < 1 || number > len(objects) || objects[number-1] == nil || len(objects[number-1].dict) == 0 {
			return nil
		}
		node = objects[number-1]
		parent := objectValue(node)
		k := dictEntryIndex(parent, key)
		if k != -1 {
			return append([]string(nil), parent[k+1:dictValueEnd(parent, k+1)]...)
		}
	}
	return nil
}

// dictValueEnd returns the index after the value that starts at index i.
func dictValueEnd(tokens []string, i int) int {
	if i >= len(tokens) {
		return len(tokens)
	}
	token := tokens[i]
	if token == "<<" || token == "[" {
		depth := 0
		for j := i; j < len(tokens); j++ {
			t := tokens[j]
			if t == "<<" || t == "[" {
				depth++
			} else if t == ">>" || t == "]" {
				depth--
				if depth == 0 {
					return j + 1
				}
			}
		}
		return len(tokens)
	}
	if isObjectReference(tokens, i) {
		return i + 3
	}
	return i + 1
}

// dictEntryIndex returns the index of the key of an entry of the dictionary,
// not of a dictionary inside it, or -1.
func dictEntryIndex(tokens []string, key string) int {
	if len(tokens) == 0 || tokens[0] != "<<" {
		return -1
	}
	i := 1
	for i < len(tokens) && tokens[i] != ">>" {
		if tokens[i] == key {
			return i
		}
		i = dictValueEnd(tokens, i+1)
	}
	return -1
}

// setDictEntry sets the value of an entry of the dictionary, adding the entry
// at its end, and returns the tokens.
func setDictEntry(tokens []string, key string, value []string) []string {
	if len(tokens) == 0 || tokens[0] != "<<" {
		return tokens
	}
	i := dictEntryIndex(tokens, key)
	if i != -1 {
		end := dictValueEnd(tokens, i+1)
		result := append([]string(nil), tokens[:i+1]...)
		result = append(result, value...)
		return append(result, tokens[end:]...)
	}
	end := dictValueEnd(tokens, 0) - 1 // The index of the closing >>
	result := append([]string(nil), tokens[:end]...)
	result = append(result, key)
	result = append(result, value...)
	return append(result, tokens[end:]...)
}

func removeDictEntry(tokens []string, key string) []string {
	i := dictEntryIndex(tokens, key)
	if i == -1 {
		return tokens
	}
	end := dictValueEnd(tokens, i+1)
	return append(append([]string(nil), tokens[:i]...), tokens[end:]...)
}

// renumbered returns the tokens with the references renumbered for this
// document, a reference to an object that is not merged replaced with null,
// and the strings encrypted when this document is encrypted.
func (pdf *PDF) renumbered(tokens []string, numbers map[int]int) []string {
	result := make([]string, 0, len(tokens))
	for i := 0; i < len(tokens); i++ {
		token := tokens[i]
		if isObjectReference(tokens, i) {
			old, _ := strconv.Atoi(token)
			if number, ok := numbers[old]; ok {
				result = append(result, strconv.Itoa(number), "0", "R")
			} else {
				result = append(result, "null")
			}
			i += 2
		} else if pdf.encryption != nil &&
			(strings.HasPrefix(token, "(") || (strings.HasPrefix(token, "<") && token != "<<")) {
			encrypted := pdf.encryption.encrypt(toBytes(token))
			result = append(result, "<"+hex.EncodeToString(encrypted)+">")
		} else {
			result = append(result, token)
		}
	}
	return result
}

func (pdf *PDF) addMergedObject(obj *PDFobj, number int, value []string) {
	stream := obj.stream
	if stream != nil && pdf.encryption != nil {
		stream = pdf.encryption.encrypt(stream)
		value = setDictEntry(value, "/Length", []string{strconv.Itoa(len(stream))})
	}
	pdf.setObjOffset(number, pdf.byteCount)
	pdf.appendInteger(number)
	pdf.appendString(" 0 obj\n")
	pdf.appendTokens(value)
	pdf.appendString("\n")
	if stream != nil {
		pdf.appendString("stream\n")
		pdf.appendByteArray(stream)
		pdf.appendString("\nendstream\n")
	}
	pdf.appendString("endobj\n")
}

func (pdf *PDF) appendTokens(tokens []string) {
	for i, token := range tokens {
		if i > 0 {
			pdf.appendString(" ")
		}
		pdf.appendString(token)
	}
}

// AddPages adds the pages to this document.
func (pdf *PDF) AddPages(pages []*Page) {
	for _, page := range pages {
		pdf.AddPage(page)
	}
}

// Complete writes the rest of the PDF, flushes the bufio.Writer and closes the
// file that NewPDFFile opened. It returns the first error writing the document.
func (pdf *PDF) Complete() error {
	if pdf.completed {
		return pdf.fail("Complete was already called.")
	}
	if pdf.err != nil {
		return errors.New("The PDF was not completed because of an earlier error: " + pdf.err.Error())
	}
	if len(pdf.pages) == 0 && pdf.pagesObjNumber == 0 {
		return pdf.fail("A PDF needs at least one page.")
	}
	if pdf.prevPage != nil {
		pdf.addPageContent(pdf.prevPage)
		if pdf.err != nil {
			return pdf.err
		}
	}
	pdf.completed = true
	if pdf.compliance != compliance.PDF_1_7 {
		pdf.metadataObjNumber = pdf.addMetadataObject("", false)
		pdf.outputIntentObjNumber = pdf.addOutputIntentObject()
	}

	if pdf.pagesObjNumber == 0 {
		pdf.addAllPages(pdf.addResourcesObject())
		pdf.addPagesObject()
	}

	structTreeRootObjNumber := 0
	if pdf.compliance != compliance.PDF_1_7 {
		// The elements of every page are written with it; the ones still open
		// and the ones of the annotations are what is left.
		for _, page := range pdf.pages {
			for _, element := range page.structures {
				pdf.addStructElementObject(element)
			}
			page.structures = nil
		}
		pdf.reserveStructTreeNumbers()
		structTreeRootObjNumber = pdf.addStructTreeRootObject()
		pdf.addNumsParentTree()
		pdf.addStructDocumentObject(structTreeRootObjNumber)
	}

	var outlineDictNum = 0
	if pdf.toc != nil && pdf.toc.getChildren() != nil {
		list := pdf.toc.toArrayList()
		outlineDictNum = pdf.addOutlineDict(pdf.toc)
		for i := 1; i < len(list); i++ {
			pdf.addOutlineItem(outlineDictNum, list[i])
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
		if offset == 0 { // A number that no object was written for.
			pdf.appendString("0000000000 65535 f \n")
			continue
		}
		entry, err := xrefOffset(offset)
		if err != nil {
			return err
		}
		pdf.appendString(entry)
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
		pdf.appendInteger(pdf.encryption.getObjNumber())
		pdf.appendString(" 0 R\n")
	}

	pdf.appendString("/Info ")
	pdf.appendInteger(infoObjNumber)
	pdf.appendString(" 0 R\n")

	pdf.appendString("/Root ")
	pdf.appendInteger(rootObjNumber)
	pdf.appendString(" 0 R\n")

	pdf.appendString(">>\n")
	pdf.appendString("startxref\n")
	pdf.appendString(strconv.FormatInt(startxref, 10))
	pdf.appendString("\n")
	pdf.appendString("%%EOF\n")

	if pdf.err != nil {
		return pdf.err
	}
	if err := pdf.writer.Flush(); err != nil {
		return err
	}
	// The file that NewPDFFile created is closed, as the other ports close
	// their output stream.
	if pdf.file != nil {
		return pdf.file.Close()
	}
	return nil
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
func (pdf *PDF) SetPageLayout(pageLayout pagelayout.PageLayout) *PDF {
	pdf.pageLayout = pageLayout
	return pdf
}

// SetPageMode sets the page mode.
func (pdf *PDF) SetPageMode(pageMode pagemode.PageMode) *PDF {
	pdf.pageMode = pageMode
	return pdf
}

// getSortedObjects returns the objects by their number, with an empty object
// at the number of every one the PDF does not have, so that the object a
// reference names is the one at its number. Every object of a PDF takes bytes
// of its file, so a number larger than the file has bytes is one no PDF can
// hold, and the empty objects up to it would take the memory a file of a few
// bytes never names.
func (pdf *PDF) getSortedObjects(objects []*PDFobj, size int) ([]*PDFobj, error) {
	sorted := make([]*PDFobj, 0)

	maxObjNumber := 0
	for _, obj := range objects {
		if obj.number > maxObjNumber {
			maxObjNumber = obj.number
		}
	}
	if maxObjNumber > size {
		return nil, fmt.Errorf(
			"The PDF of %d bytes cannot hold an object numbered %d.", size, maxObjNumber)
	}

	for number := 1; number <= maxObjNumber; number++ {
		obj := newPDFobj()
		obj.setNumber(number)
		sorted = append(sorted, obj)
	}

	for _, obj := range objects {
		if obj.number > 0 {
			sorted[obj.number-1] = obj
		}
	}

	return sorted, nil
}

func contains(slice []string, text string) bool {
	for _, str := range slice {
		if str == text {
			return true
		}
	}
	return false
}

// Read returns the objects of the PDF in buf. An encrypted PDF is decrypted
// when it opens without a password. It returns an error if the PDF needs a
// password or cannot be read.
func (pdf *PDF) Read(buf []byte) ([]*PDFobj, error) {
	return pdf.ReadWithPassword(buf, "")
}

// ReadWithPassword returns a list of objects of type PDFobj read from the
// bytes of a PDF that is encrypted with the standard security handler. The
// PDF is decrypted with the password, which is its user or its owner
// password. It returns an error if the password is not correct or the PDF
// cannot be read.
func (pdf *PDF) ReadWithPassword(buf []byte, password string) (objects []*PDFobj, err error) {
	// The code that reads a malformed PDF panics where the Java port throws,
	// like a Flate stream that cannot be inflated; the panic is the error.
	defer func() {
		if r := recover(); r != nil {
			objects = nil
			if e, ok := r.(error); ok {
				err = e
			} else {
				err = fmt.Errorf("%v", r)
			}
		}
	}()
	objects1 := make([]*PDFobj, 0)
	trailer := func() (trailer *PDFobj) {
		defer func() {
			if recover() != nil {
				trailer = nil // A cross-reference stream that cannot be decoded.
			}
		}()
		return getObjects(buf, pdf.getStartXRef(buf), &objects1, 0)
	}()
	if trailer == nil || len(objects1) == 0 {
		// The cross-reference table is missing or wrong, like in a PDF
		// that was changed without updating it.
		objects1 = objects1[:0]
		trailer = getObjectsByScanning(buf, &objects1)
	}
	dec, err := getDecryptor(trailer, objects1, password)
	if err != nil {
		return nil, err
	}

	objects2 := make([]*PDFobj, 0)
	for _, obj := range objects1 {
		objType := obj.GetValue("/Type")
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
			length := obj.getLength(objects1)
			obj.setStreamAndData(buf, length, dec)
		}

		if objType == "/ObjStm" {
			// A malformed object stream is an error, as in the other ports,
			// and not a reason to stop the program.
			first, err := objectStreamNumber(obj.GetValue("/First"))
			if err != nil {
				return nil, err
			}
			data := obj.GetData()
			o2 := getObject(data, 0, min(first, len(data)))
			for i := 0; i+1 < len(o2.dict); i += 2 {
				num := o2.dict[i]
				number, err := objectStreamNumber(num)
				if err != nil {
					return nil, err
				}
				off, err := objectStreamNumber(o2.dict[i+1])
				if err != nil {
					return nil, err
				}
				end := len(data)
				if i <= len(o2.dict)-4 {
					tmp, err := objectStreamNumber(o2.dict[i+3])
					if err != nil {
						return nil, err
					}
					end = min(first+tmp, len(data))
				}
				o3 := getObject(data, first+off, end)
				o3.setNumber(number)
				o3.dict = insertStringAt(o3.dict, "obj", 0)
				o3.dict = insertStringAt(o3.dict, "0", 0)
				o3.dict = insertStringAt(o3.dict, num, 0)
				objects2 = append(objects2, o3)
			}
		} else {
			objects2 = append(objects2, obj)
		}
	}

	return pdf.getSortedObjects(objects2, len(buf))
}

// objectStreamNumber returns the number in the header of an object stream.
func objectStreamNumber(token string) (int, error) {
	number, err := strconv.Atoi(token)
	if err != nil || number < 0 {
		return 0, fmt.Errorf(
			"The object stream of the PDF is malformed: %q is not a number.", token)
	}
	return number, nil
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
		// The keyword ends with CRLF or LF, and the tokenizer consumed the
		// first of those bytes, so only the LF of a CRLF is left to skip. A
		// data byte that is a line feed, like the first byte of the IV of an
		// encrypted stream, stays.
		obj.streamOffset = off
		if off > 0 && buf[off-1] == byte('\r') && off < len(buf) && buf[off] == byte('\n') {
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
		return newPDFobj()
	}
	return getObject(buf, off, len(buf))
}

func getObject(buf []byte, off, length int) *PDFobj {
	obj := newPDFobj()
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
	prev := xref.GetValue("/Prev")
	if prev != "" && getObjects(buf, toInteger(prev), objects, depth+1) == nil {
		return nil
	}
	if table {
		// The objects in the table replace those in the /XRefStm stream.
		xrefStm := xref.GetValue("/XRefStm")
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
	if !isObject(xref, -1) || xref.GetValue("/Type") != "/XRef" || !contains(xref.dict, "stream") {
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
	length := toInteger(xref.GetValue("/Length"))
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
		index = append(index, 0, toInteger(xref.GetValue("/Size")))
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
				if obj.GetValue("/Type") == "/XRef" {
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

// addOutlineDict adds the outline dictionary and returns its object number.
// The bookmarks were numbered by toArrayList, level by level, and the object of
// a bookmark is that many objects after the outline dictionary.
func (pdf *PDF) addOutlineDict(toc *Bookmark) int {
	pdf.newObj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /Outlines\n")
	pdf.appendString("/First ")
	pdf.appendInteger(pdf.getObjNumber() + toc.getFirstChild().objNumber)
	pdf.appendString(" 0 R\n")
	pdf.appendString("/Last ")
	pdf.appendInteger(pdf.getObjNumber() + toc.getLastChild().objNumber)
	pdf.appendString(" 0 R\n")
	// The items that are visible: those of the first level, as the items with
	// children are closed.
	pdf.appendString("/Count ")
	pdf.appendInteger(len(toc.getChildren()))
	pdf.appendString("\n")
	pdf.appendString(">>\n")
	pdf.endObj()
	return pdf.getObjNumber()
}

// addOutlineItem adds an outline item for the bookmark, under the outline
// dictionary with the given object number.
func (pdf *PDF) addOutlineItem(outlines int, bm1 *Bookmark) {
	prev := 0
	if bm1.getPrevBookmark() != nil {
		prev = outlines + bm1.getPrevBookmark().objNumber
	}
	next := 0
	if bm1.getNextBookmark() != nil {
		next = outlines + bm1.getNextBookmark().objNumber
	}

	first := 0
	last := 0
	count := 0
	if len(bm1.getChildren()) > 0 {
		first = outlines + bm1.getFirstChild().objNumber
		last = outlines + bm1.getLastChild().objNumber
		// A closed item: the items that opening it would show.
		count = (-1) * len(bm1.getChildren())
	}

	pdf.newObj()
	pdf.appendString("<<\n")
	pdf.appendString("/Title ")
	pdf.appendTextString(bm1.GetTitle())
	pdf.appendString("\n")
	// The root of the bookmarks is the outline dictionary itself.
	pdf.appendString("/Parent ")
	pdf.appendInteger(outlines + bm1.GetParent().objNumber)
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
	pdf.appendString("/Dest [")
	pdf.appendInteger(bm1.getDestination().pageObjNumber)
	pdf.appendString(" 0 R /XYZ ")
	pdf.appendFloat32(bm1.getDestination().xPosition)
	pdf.appendString(" ")
	pdf.appendFloat32(bm1.getDestination().yPosition)
	pdf.appendString(" 0]\n")
	pdf.appendString(">>\n")
	pdf.endObj()
}

// AddObjects adds the specified objects to the PDF. The objects keep their
// numbers and are written as they are, so they are added before any font,
// image or page of this document, and an encrypted PDF cannot take them. It
// records the mistake and returns it as an error, as it does when the objects
// have no root /Pages object.
func (pdf *PDF) AddObjects(objects []*PDFobj) error {
	for _, page := range pdf.pages {
		if page.mergedDict != nil {
			return pdf.fail("Merge and AddObjects cannot be used on the same PDF.")
		}
	}
	pagesObject := pdf.getPagesObject(objects)
	if pagesObject == nil {
		return pdf.fail("The objects have no root /Pages object.")
	}
	if err := pdf.checkObjects(objects); err != nil {
		return err
	}
	objNumber, err := strconv.Atoi(pagesObject.dict[0])
	if err != nil {
		return err
	}
	pdf.pagesObjNumber = objNumber
	pdf.addObjectsToPDF(objects)
	return nil
}

// checkObjects refuses objects of a PDF that was read when writing them would
// break this document. They keep their numbers and are written as they are, so
// they have to come before the objects that this document numbers itself, and
// an encrypted document cannot take them.
func (pdf *PDF) checkObjects(objects []*PDFobj) error {
	if pdf.completed {
		return pdf.fail("The PDF was already completed.")
	}
	if pdf.encryption != nil {
		return pdf.fail("The objects of an existing PDF cannot be added to an encrypted PDF.")
	}
	for _, obj := range objects {
		if obj.number > 0 && obj.number <= len(pdf.objOffsets) && pdf.objOffsets[obj.number-1] != 0 {
			return pdf.fail("Add the objects of an existing PDF before fonts, images or pages " +
				"are added to the PDF: object " + strconv.Itoa(obj.number) + " is already written.")
		}
	}
	return nil
}

func (pdf *PDF) getPagesObject(objects []*PDFobj) *PDFobj {
	for _, obj := range objects {
		if obj.GetValue("/Type") == "/Pages" && obj.GetValue("/Parent") == "" {
			return obj
		}
	}
	return nil
}

// GetPageObjects returns all page objects.
func (pdf *PDF) GetPageObjects(objects []*PDFobj) []*PDFobj {
	pages := make([]*PDFobj, 0)
	if pagesObject := pdf.getPagesObject(objects); pagesObject != nil {
		pdf.getPageObjects(pagesObject, objects, &pages, make(map[int]bool))
	}
	return pages
}

// The nodes of the page tree that were visited are skipped, as a node of a
// broken tree can list itself or a node above it as a kid.
func (pdf *PDF) getPageObjects(pdfObj *PDFobj, objects []*PDFobj, pages *[]*PDFobj, visited map[int]bool) {
	if visited[pdfObj.number] {
		return
	}
	visited[pdfObj.number] = true
	kids := pdfObj.getObjectNumbers("/Kids")
	for _, number := range kids {
		if number < 1 || number > len(objects) {
			continue // A kid that the document does not have.
		}
		obj := objects[number-1]
		if isPageObject(obj) {
			*pages = append(*pages, obj)
		} else {
			pdf.getPageObjects(obj, objects, pages, visited)
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

// addExtGStates adds the entries of the /ExtGState dictionary of the
// resources, which can be an object of its own, with the names that an earlier
// page did not add.
func (pdf *PDF) addExtGStates(resources *PDFobj, objects []*PDFobj) {
	entries := pdf.getResourceEntries(resources, "/ExtGState", objects)
	i := 0
	for i < len(entries) {
		// The value after the name is a dictionary, a reference or one token.
		end := i + 1
		if end < len(entries) && entries[end] == "<<" {
			level := 0
			for {
				token1 := entries[end]
				end++
				if token1 == "<<" {
					level++
				} else if token1 == ">>" {
					level--
				}
				if level <= 0 || end >= len(entries) {
					break
				}
			}
		} else if end+2 < len(entries) && entries[end+2] == "R" {
			end += 3
		} else {
			end = min(end+1, len(entries))
		}
		if !slices.Contains(pdf.importedExtGStates, entries[i]) {
			pdf.importedExtGStates = append(pdf.importedExtGStates, entries[i:end]...)
		}
		i = end
	}
}

func (pdf *PDF) getFontObjects(resources *PDFobj, objects []*PDFobj) []*PDFobj {
	fonts := make([]*PDFobj, 0)
	// The /Font dictionary holds one "/Name number 0 R" entry per font, and can
	// be an object of its own. Every entry is written to the resources object,
	// so every font it names is collected here.
	entries := pdf.getResourceEntries(resources, "/Font", objects)
	i := 0
	for i < len(entries) {
		token1 := entries[i]
		if strings.HasPrefix(token1, "/") && i+3 < len(entries) && entries[i+3] == "R" {
			// Pages can carry separate resource dictionaries that name the same
			// fonts. They are merged into one /Font dictionary here, so a name
			// that is already present must not be added twice.
			if !slices.Contains(pdf.importedFonts, token1) {
				pdf.importedFonts = append(pdf.importedFonts, entries[i:i+4]...)
				if objNumber := toInteger(entries[i+1]); objNumber > 0 && objNumber <= len(objects) {
					fonts = append(fonts, objects[objNumber-1])
				}
			}
			i += 4
			continue
		}
		pdf.importedFonts = append(pdf.importedFonts, token1)
		i++
	}
	return fonts
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
	objType := obj.GetValue("/Type")
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

// AddResourceObjects adds the fonts, images and graphics states used by the
// pages to this document. The objects keep their numbers and are written as
// they are, so they are added before any font, image or page of this document,
// and an encrypted PDF cannot take them: the mistake is recorded, and Complete
// returns it.
func (pdf *PDF) AddResourceObjects(objects []*PDFobj) {
	if pdf.checkObjects(objects) != nil {
		return
	}
	resources := make([]*PDFobj, 0)
	numbers := make(map[int]bool)

	pages := pdf.GetPageObjects(objects)
	for _, page := range pages {
		resObj := page.GetResourcesObject(objects)
		if resObj == nil {
			continue // A page without resources of its own.
		}
		// A font is copied with every object that it refers to: its descriptor
		// and font file, and also the widths, the encoding and the other
		// entries that can be objects of their own.
		for _, font := range pdf.getFontObjects(resObj, objects) {
			resources = pdf.addObjectTree(font.number, objects, numbers, resources)
		}
		resources = pdf.addXObjects(resObj, objects, numbers, resources)
		pdf.addExtGStates(resObj, objects)
		// The /ExtGState entries are copied as they are, so the objects that
		// they refer to have to be copied too.
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
	pdf.addObjectsToPDF(unique)
}

func (pdf *PDF) addObjectsToPDF(objects []*PDFobj) {
	for _, obj := range objects {
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
	if !fastfloat.IsWritable(f) {
		// The misuse is recorded, and the number is written all the same:
		// fastfloat writes 0 for it, so the object keeps its syntax, as in
		// the other ports.
		pdf.fail(notWritable)
	}
	pdf.appendByteArray(fastfloat.ToByteArray(f))
}

// The append functions keep the first error of the writer for Complete to
// return, as the other ports let the exception of their stream propagate.
func (pdf *PDF) appendString(s string) {
	pdf.appendByteArray([]byte(s))
}

func (pdf *PDF) appendByte(b byte) {
	if err := pdf.writer.WriteByte(b); err != nil && pdf.err == nil {
		pdf.err = err
	}
	pdf.byteCount++
}

func (pdf *PDF) appendByteArray(buf []byte) {
	if _, err := pdf.writer.Write(buf); err != nil && pdf.err == nil {
		pdf.err = err
	}
	pdf.byteCount += int64(len(buf))
}
