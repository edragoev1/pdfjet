// repairscan_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"
	"testing"
)

// testPDFWithAnObjectInAStream returns a PDF whose cross-reference table is
// broken, so that it is read by looking for its objects, and the content of
// its page: an unfiltered stream with a wrong /Length that holds an object of
// an embedded PDF, with a stream of its own.
func testPDFWithAnObjectInAStream() ([]byte, string) {
	inner := "BT /F1 12 Tf 72 720 Td (x) Tj ET\n" +
		"5 0 obj\n<< /Length 3 >>\nstream\nabc\nendstream\nendobj\n" +
		"q Q"
	var sb strings.Builder
	sb.WriteString("%PDF-1.7\n")
	sb.WriteString("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
	sb.WriteString("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
	sb.WriteString("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj\n")
	sb.WriteString("4 0 obj\n<< /Length 10 >>\nstream\n" + inner + "\nendstream\nendobj\n")
	sb.WriteString("trailer\n<< /Size 5 /Root 1 0 R >>\nstartxref\n999999\n%%EOF\n")
	return []byte(sb.String()), inner
}

func TestRepairScanAStreamThatHoldsAnObjectIsReadWhole(t *testing.T) {
	// An object ended at the next "number generation obj": one in the bytes
	// of a stream, as an embedded PDF has, unfiltered, cut the stream short
	// and was read as an object of the PDF, and the first endstream after the
	// stream, that of the embedded object, was taken for the stream's own.
	pdf, inner := testPDFWithAnObjectInAStream()
	objects := testReadQuickly(t, pdf)
	var content *PDFobj
	for _, obj := range objects {
		if obj.number == 5 {
			t.Error("the object in the stream is read as an object of the PDF")
		}
		if obj.number == 4 {
			content = obj
		}
	}
	if content == nil {
		t.Fatal("no object 4")
	}
	if got := string(content.GetData()); got != inner {
		t.Errorf("the stream is %q", got)
	}
}
