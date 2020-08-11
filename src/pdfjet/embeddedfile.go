package pdfjet

/**
 * embeddedfile.go
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice,
  this list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and / or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

import (
	"bytes"
	"compress/zlib"
	"io"
	"io/ioutil"
	"log"
)

// EmbeddedFile is used to embed file objects in the PDF.
// The file objects must added to the PDF before drawing on the first page.
type EmbeddedFile struct {
	objNumber int
	fileName  string
	content   []byte
}

// NewEmbeddedFile is the constructor.
func NewEmbeddedFile(pdf *PDF, fileName string, reader io.Reader, compress bool) *EmbeddedFile {
	file := new(EmbeddedFile)
	file.fileName = fileName

	buf, err := ioutil.ReadAll(reader)
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
	pdf.appendString("<<\n")
	pdf.appendString("/Type /EmbeddedFile\n")
	if compress {
		pdf.appendString("/Filter /FlateDecode\n")
	}
	pdf.appendString("/Length ")
	pdf.appendInteger(len(file.content))
	pdf.appendString("\n")
	pdf.appendString(">>\n")
	pdf.appendString("stream\n")
	pdf.appendByteArray(file.content)
	pdf.appendString("\nendstream\n")
	pdf.endobj()

	pdf.newobj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /Filespec\n")
	pdf.appendString("/F (")
	pdf.appendString(fileName)
	pdf.appendString(")\n")
	pdf.appendString("/EF <</F ")
	pdf.appendInteger(pdf.getObjNumber() - 1)
	pdf.appendString(" 0 R>>\n")
	pdf.appendString(">>\n")
	pdf.endobj()

	file.objNumber = pdf.getObjNumber()

	return file
}

// GetFileName returns the file name.
func (file *EmbeddedFile) GetFileName() string {
	return file.fileName
}
