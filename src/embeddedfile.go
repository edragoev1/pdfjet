// embeddedfile.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"fmt"
	"io"
	"os"
	"strconv"
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/internal/compressor"
	"github.com/edragoev1/pdfjet/v9/src/internal/token"
	"github.com/edragoev1/pdfjet/v9/src/relationship"
)

// EmbeddedFile is used to embed file objects in the PDF.
// The file objects must be added to the PDF before drawing on the first page.
type EmbeddedFile struct {
	objNumber int
	fileName  string
	pdf       *PDF // The PDF the file is embedded in
	// How the file relates to the document, for a file of a document of
	// PDF/A-3, and empty for a file that is only attached to a page.
	relationship relationship.Relationship
	// What the file holds, like "text/xml", or empty.
	mediaType string
}

// NewEmbeddedFileAtPath embeds the file at the specified path into the PDF,
// compressing it with Flate when compress is true. It panics if the file cannot be opened.
func NewEmbeddedFileAtPath(pdf *PDF, filePath string, compress bool) *EmbeddedFile {
	fileName := filePath[strings.LastIndex(filePath, "/")+1:]
	file, err := os.Open(filePath)
	if err != nil {
		panic(err)
	}
	defer func(file *os.File) {
		err := file.Close()
		if err != nil {
			panic("Error closing file: " + err.Error())
		}
	}(file)
	return NewEmbeddedFile(pdf, fileName, bufio.NewReader(file), compress)
}

// NewEmbeddedFile is the constructor.
func NewEmbeddedFile(pdf *PDF, fileName string, reader io.Reader, compress bool) *EmbeddedFile {
	return NewEmbeddedFileWithRelationship(pdf, fileName, reader, compress, "", "", "")
}

// NewEmbeddedFileWithRelationship embeds the file and says what it holds and
// how it relates to the document. A document of PDF/A-3 needs all three of
// them for each file it carries, and so does a document that carries the XML
// of an invoice. PDF.AddAssociatedFile adds the embedded file to the document.
// The media type is what the file holds, such as "text/xml", the relation is
// how the file relates to the document and the description is what the file
// is, in the words of a person; each of them may be empty.
func NewEmbeddedFileWithRelationship(pdf *PDF, fileName string, reader io.Reader, compress bool,
	mediaType string, relation relationship.Relationship, description string) *EmbeddedFile {
	file := new(EmbeddedFile)
	file.pdf = pdf
	file.fileName = fileName
	// A relationship that is not one of relationship's was written raw, as
	// any name or text in the dictionary (review of 17 October 2026)
	switch relation {
	case "", relationship.Source, relationship.Data, relationship.Alternative,
		relationship.Supplement, relationship.Unspecified:
	default:
		pdf.fail("The relationship " + strconv.Quote(string(relation)) +
			" is not one: use relationship.Source, Data, Alternative, Supplement or Unspecified.")
		relation = ""
	}
	file.relationship = relation
	file.mediaType = mediaType

	buf, err := io.ReadAll(reader)
	if err != nil {
		panic(err)
	}
	// The file is the PDF's once it is read: one that could not be read was
	// left in the list, with no object, for the check of PDF/A (review of
	// 17 October 2026)
	pdf.embeddedFiles = append(pdf.embeddedFiles, file)
	size := len(buf)

	// The content is written at once, and not kept: the PDF keeps each file
	// for the check of PDF/A, which reads its name, and each was kept whole
	// until the PDF was freed (review of 12 October 2026, R25)
	content := buf
	if compress {
		content = compressor.Deflate(buf)
	}
	if pdf.encryption != nil {
		content = pdf.encryption.encrypt(content)
	}

	pdf.newObj()
	pdf.appendByteArray(token.BeginDictionary)
	pdf.appendString("/Type /EmbeddedFile\n")
	if mediaType != "" {
		// What the file holds, as a name: /text#2Fxml for "text/xml".
		pdf.appendString("/Subtype ")
		pdf.appendString(toName(mediaType))
		pdf.appendByte(token.Newline)
		// The size before compression and the date, which PDF/A-3 asks for.
		// The date is the one the document itself carries, since the file is
		// written as the document is.
		pdf.appendString("/Params <</Size ")
		pdf.appendInteger(size)
		pdf.appendString(" /ModDate ")
		pdf.appendByteString([]byte(pdf.getDate()))
		pdf.appendString(">>\n")
	}
	if compress {
		pdf.appendString("/Filter /FlateDecode\n")
	}
	pdf.appendString("/Length ")
	pdf.appendInteger(len(content))
	pdf.appendByte('\n')
	pdf.appendByteArray(token.EndDictionary)
	pdf.appendByteArray(token.Stream)
	pdf.appendByteArray(content)
	pdf.appendByteArray(token.EndStream)
	pdf.endObj()

	pdf.newObj()
	pdf.appendByteArray(token.BeginDictionary)
	pdf.appendString("/Type /Filespec\n")

	// /UF is the file name as a text string, the one that readers of PDF 1.7
	// use, and /F is the file specification string that older readers know,
	// a string of bytes (ISO 32000-2, 7.11.3, Table 43); PDF/A-3 requires both.
	pdf.appendString("/F ")
	pdf.appendByteString(fileSpecBytes(fileName))
	pdf.appendString("\n")
	pdf.appendString("/UF ")
	pdf.appendTextString(fileName)
	pdf.appendString("\n")

	if relation != "" {
		pdf.appendString("/AFRelationship ")
		pdf.appendString(string(relation))
		pdf.appendByte(token.Newline)
	}
	if description != "" {
		pdf.appendString("/Desc ")
		pdf.appendTextString(description)
		pdf.appendString("\n")
	}

	pdf.appendString("/EF <</F ")
	pdf.appendInteger(pdf.getObjNumber() - 1)
	pdf.appendString(" 0 R /UF ")
	pdf.appendInteger(pdf.getObjNumber() - 1)
	pdf.appendString(" 0 R>>\n")
	pdf.appendByteArray(token.EndDictionary)
	pdf.endObj()

	file.objNumber = pdf.getObjNumber()

	return file
}

// fileSpecBytes returns the file name as the bytes of /F: each character
// from space to tilde (0x20 to 0x7E) as it is, and each other character, one
// Unicode code point, as an underscore, so that "résumé.txt" is
// "r_sum_.txt". The full name is in /UF.
func fileSpecBytes(fileName string) []byte {
	bytes := make([]byte, 0, len(fileName))
	for _, ch := range fileName {
		if ch >= 0x20 && ch <= 0x7E {
			bytes = append(bytes, byte(ch))
		} else {
			bytes = append(bytes, '_')
		}
	}
	return bytes
}

// toName returns the media type as a name of PDF: the characters a name
// cannot hold written as a number sign and two hexadecimal digits, so that
// "text/xml" is /text#2Fxml.
func toName(mediaType string) string {
	var sb strings.Builder
	sb.WriteByte('/')
	for _, ch := range []byte(mediaType) {
		if ch > 0x20 && ch < 0x7F && !strings.ContainsRune("()<>[]{}/%#", rune(ch)) {
			sb.WriteByte(ch)
		} else {
			fmt.Fprintf(&sb, "#%02X", ch)
		}
	}
	return sb.String()
}

// GetFileName returns the file name.
func (file *EmbeddedFile) GetFileName() string {
	return file.fileName
}
