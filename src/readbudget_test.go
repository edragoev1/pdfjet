// readbudget_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"compress/zlib"
	"fmt"
	"strings"
	"testing"
)

// testDeflate returns the data compressed with Flate.
func testDeflate(data []byte) []byte {
	var buf bytes.Buffer
	w := zlib.NewWriter(&buf)
	w.Write(data)
	w.Close()
	return buf.Bytes()
}

// testPDFOfStreams returns a PDF, without a cross-reference table, so that it
// is read by scanning, of a catalog, an object stream that holds one object,
// and a content stream: each Flate, decoding to the given number of bytes.
func testPDFOfStreams(objectStreamBytes, contentBytes int) []byte {
	var pdf bytes.Buffer
	pdf.WriteString("%PDF-1.5\n")
	pdf.WriteString("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
	pdf.WriteString("2 0 obj\n<< /Type /Pages /Kids [] /Count 0 >>\nendobj\n")
	// Object 5 in the object stream, padded to the length asked for
	body := "5 0 << /Padding /"
	body = body + strings.Repeat("x", max(0, objectStreamBytes-len(body)-3)) + " >>"
	first := len("5 0 ")
	stream := testDeflate([]byte(body))
	fmt.Fprintf(&pdf, "3 0 obj\n<< /Type /ObjStm /N 1 /First %d /Length %d /Filter /FlateDecode >>\nstream\n", first, len(stream))
	pdf.Write(stream)
	pdf.WriteString("\nendstream\nendobj\n")
	content := testDeflate([]byte(strings.Repeat("q Q\n", contentBytes/4)))
	fmt.Fprintf(&pdf, "4 0 obj\n<< /Length %d /Filter /FlateDecode >>\nstream\n", len(content))
	pdf.Write(content)
	pdf.WriteString("\nendstream\nendobj\n")
	pdf.WriteString("trailer\n<< /Root 1 0 R /Size 6 >>\n%%EOF\n")
	return pdf.Bytes()
}

func testObjectOfNumber(objects []*PDFobj, number int) *PDFobj {
	for _, obj := range objects {
		if obj.GetNumber() == number {
			return obj
		}
	}
	return nil
}

func TestReadAStreamIsDecodedWhenItsDataIsFirstAskedFor(t *testing.T) {
	objects, err := testNewPDF().Read(testPDFOfStreams(100, 4000))
	if err != nil {
		t.Fatal(err)
	}
	content := testObjectOfNumber(objects, 4)
	if content == nil || !content.undecoded || content.data != nil {
		t.Fatalf("the content stream is decoded before it is asked for: %+v", content)
	}
	if data := content.GetData(); len(data) != 4000 || content.undecoded {
		t.Errorf("the content stream decodes to %d bytes, not 4000", len(data))
	}
	// The object stream was read when the PDF was: its object is there.
	if padded := testObjectOfNumber(objects, 5); padded == nil || padded.GetValue("/Padding") == "" {
		t.Errorf("the object of the object stream was not read: %+v", padded)
	}
}

func TestReadTheStreamsOfAPDFDecodeToNoMoreThanTheBudgetTogether(t *testing.T) {
	defer func(was int) { maxDecodedTotal = was }(maxDecodedTotal)
	maxDecodedTotal = 5000

	// The object stream, read with the PDF, is within the budget, and the
	// content stream is decoded up to what is left of it: 4000 of 5000 is
	// left after the object stream of 1000, so a content of 4000 is decoded
	// and one of 4001 is not.
	objects, err := testNewPDF().Read(testPDFOfStreams(1000, 4000))
	if err != nil {
		t.Fatal(err)
	}
	if data := testObjectOfNumber(objects, 4).GetData(); len(data) != 4000 {
		t.Errorf("a content within the budget decodes to %d bytes, not 4000", len(data))
	}
	objects, err = testNewPDF().Read(testPDFOfStreams(1000, 4004))
	if err != nil {
		t.Fatal(err)
	}
	if data := testObjectOfNumber(objects, 4).GetData(); data != nil {
		t.Errorf("a content past the budget decodes to %d bytes, not to nothing", len(data))
	}

	// An object stream past the budget is an error, as the PDF cannot be
	// read without its objects.
	_, err = testNewPDF().Read(testPDFOfStreams(6000, 0))
	if err == nil || err.Error() != "the streams of the PDF decode to more than 5000 bytes together" {
		t.Errorf("an object stream past the budget: %v", err)
	}
}

func TestReadAStreamWhoseDecoderFailsHasNoDataAndDoesNotPanic(t *testing.T) {
	// No PDF is known to make a decoder fail with a runtime error, which
	// 1,218 corrupted files did not, so one is made to here: a RunLength
	// stream, with a budget below zero that no PDF can have, makes a slice
	// of a negative capacity. GetData is called after Read, which recovers
	// nothing then, and it has no data, as the other ports have.
	objects, err := testNewPDF().Read(testPDFOfStreams(100, 4000))
	if err != nil {
		t.Fatal(err)
	}
	content := testObjectOfNumber(objects, 4)
	for i, token := range content.dict {
		if token == "/FlateDecode" {
			content.dict[i] = "/RunLengthDecode"
		}
	}
	content.budget = &decodeBudget{left: -1}
	defer func() {
		if r := recover(); r != nil {
			t.Fatalf("GetData panics: %v", r)
		}
	}()
	if data := content.GetData(); data != nil {
		t.Errorf("the stream has %d bytes of data, not none", len(data))
	}
	if content.GetData() != nil {
		t.Error("the stream has data the second time it is asked for")
	}
}
