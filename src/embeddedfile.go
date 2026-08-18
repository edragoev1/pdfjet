// embeddedfile.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"bytes"
	"compress/zlib"
	"encoding/hex"
	"io"
	"log"
	"os"
	"strings"

	"github.com/edragoev1/pdfjet/src/encryption"
	"github.com/edragoev1/pdfjet/src/token"
)

// EmbeddedFile is used to embed file objects in the PDF.
// The file objects must be added to the PDF before drawing on the first page.
type EmbeddedFile struct {
	objNumber int
	fileName  string
	content   []byte
}

func NewEmbeddedFileAtPath(pdf *PDF, filePath string, compress bool) *EmbeddedFile {
	fileName := filePath[strings.LastIndex(filePath, "/")+1:]
	file, err := os.Open(filePath)
	if err != nil {
		log.Fatal(err)
	}
	defer func(file *os.File) {
		_ = file.Close()
	}(file)
	return NewEmbeddedFile(pdf, fileName, bufio.NewReader(file), compress)
}

// NewEmbeddedFile is the constructor.
func NewEmbeddedFile(pdf *PDF, fileName string, reader io.Reader, compress bool) *EmbeddedFile {
	file := new(EmbeddedFile)
	file.fileName = fileName

	buf, err := io.ReadAll(reader)
	if err != nil {
		log.Fatal(err)
	}

	if compress {
		var compressed bytes.Buffer
		writer := zlib.NewWriter(&compressed)
		_, err := writer.Write(buf)
		if err != nil {
			return nil
		}
		err = writer.Close()
		if err != nil {
			return nil
		}
		file.content = compressed.Bytes()
	} else {
		file.content = buf
	}

	if pdf.encryption != nil {
		file.content, _ = encryption.Encrypt(file.content, pdf.encryption.GetKey())
	}

	pdf.newobj()
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
	pdf.endobj()

	pdf.newobj()
	pdf.appendByteArray(token.BeginDictionary)
	pdf.appendString("/Type /Filespec\n")

	fileNameBytes := []byte(fileName)
	if pdf.encryption != nil {
		fileNameBytes, _ = encryption.Encrypt(fileNameBytes, pdf.encryption.GetKey())
	}
	pdf.appendString("/F <")
	pdf.appendString(hex.EncodeToString(fileNameBytes))
	pdf.appendString(">\n")

	pdf.appendString("/EF <</F ")
	pdf.appendInteger(pdf.getObjNumber() - 1)
	pdf.appendString(" 0 R>>\n")
	pdf.appendByteArray(token.EndDictionary)
	pdf.endobj()

	file.objNumber = pdf.getObjNumber()

	return file
}

// GetFileName returns the file name.
func (file *EmbeddedFile) GetFileName() string {
	return file.fileName
}
