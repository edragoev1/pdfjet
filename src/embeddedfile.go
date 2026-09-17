// embeddedfile.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"bytes"
	"compress/zlib"
	"io"
	"os"
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/internal/token"
)

// EmbeddedFile is used to embed file objects in the PDF.
// The file objects must be added to the PDF before drawing on the first page.
type EmbeddedFile struct {
	objNumber int
	fileName  string
	content   []byte
	pdf       *PDF // The PDF the file is embedded in
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
	file := new(EmbeddedFile)
	file.pdf = pdf
	file.fileName = fileName

	buf, err := io.ReadAll(reader)
	if err != nil {
		panic(err)
	}

	if compress {
		var compressed bytes.Buffer
		writer := zlib.NewWriter(&compressed)
		_, err := writer.Write(buf)
		if err != nil {
			panic(err)
		}
		err = writer.Close()
		if err != nil {
			panic(err)
		}
		file.content = compressed.Bytes()
	} else {
		file.content = buf
	}

	if pdf.encryption != nil {
		file.content = pdf.encryption.encrypt(file.content)
	}

	pdf.newObj()
	pdf.appendByteArray(token.BeginDictionary)
	pdf.appendString("/Type /EmbeddedFile\n")
	if compress {
		pdf.appendString("/Filter /FlateDecode\n")
	}
	pdf.appendString("/Length ")
	pdf.appendInteger(len(file.content))
	pdf.appendByte('\n')
	pdf.appendByteArray(token.EndDictionary)
	pdf.appendByteArray(token.Stream)
	pdf.appendByteArray(file.content)
	pdf.appendByteArray(token.EndStream)
	pdf.endObj()

	pdf.newObj()
	pdf.appendByteArray(token.BeginDictionary)
	pdf.appendString("/Type /Filespec\n")

	// The file name as a text string, which every reader decodes the same way.
	// /UF is the name that readers of PDF 1.7 look for first, and /F is the one
	// that older readers know; PDF/A-3 requires both.
	pdf.appendString("/F ")
	pdf.appendTextString(fileName)
	pdf.appendString("\n")
	pdf.appendString("/UF ")
	pdf.appendTextString(fileName)
	pdf.appendString("\n")

	pdf.appendString("/EF <</F ")
	pdf.appendInteger(pdf.getObjNumber() - 1)
	pdf.appendString(" 0 R>>\n")
	pdf.appendByteArray(token.EndDictionary)
	pdf.endObj()

	file.objNumber = pdf.getObjNumber()

	return file
}

// GetFileName returns the file name.
func (file *EmbeddedFile) GetFileName() string {
	return file.fileName
}
