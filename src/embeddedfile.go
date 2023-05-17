package pdfjet

/**
 * embeddedfile.go
 *
Copyright 2023 Innovatics Inc.

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
	"bytes"
	"compress/zlib"
	"io"
	"log"
	"os"
	"strings"

	"github.com/edragoev1/pdfjet/src/token"
)

// EmbeddedFile is used to embed file objects in the PDF.
// The file objects must added to the PDF before drawing on the first page.
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
	defer file.Close()
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
		writer.Write(buf)
		writer.Close()
		file.content = compressed.Bytes()
	} else {
		file.content = buf
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
	pdf.appendByteArray(token.Endstream)
	pdf.endobj()

	pdf.newobj()
	pdf.appendByteArray(token.BeginDictionary)
	pdf.appendString("/Type /Filespec\n")
	pdf.appendString("/F (")
	pdf.appendString(fileName)
	pdf.appendString(")\n")
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
