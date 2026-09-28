// review_writer_bench_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"io"
	"strconv"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// BenchmarkWriteTaggedDocument writes a tagged document of 200 pages of 50
// lines each, whose structure elements, page objects and cross-reference
// table the writer appends number by number.
func BenchmarkWriteTaggedDocument(b *testing.B) {
	for b.Loop() {
		pdf := NewPDF(bufio.NewWriter(io.Discard))
		pdf.SetCompliance(compliance.PDF_UA_1)
		pdf.SetTitle("Benchmark")
		font := NewCoreFont(pdf, corefont.Helvetica())
		for i := 0; i < 200; i++ {
			page := NewPage(pdf, letter.Portrait())
			for j := 0; j < 50; j++ {
				NewTextLine(font, "Line "+strconv.Itoa(j)).SetLocation(50, float32(20+j*14)).DrawOn(page)
			}
		}
		if err := pdf.Complete(); err != nil {
			b.Fatal(err)
		}
	}
}
