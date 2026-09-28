// review_codes_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdf417

import (
	"bufio"
	"bytes"
	"strings"
	"testing"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

func TestPDF417AControlCharacterIsShiftedToByteCompaction(t *testing.T) {
	// G S in alpha, then the pad and GS after the shift 913, then s e p after
	// the latch to lower case
	want := []int{30*6 + 18, 30*26 + 29, 913, 0x1D, 30*27 + 18, 30*4 + 15}
	got := NewPDF417("GS \x1dsep").dataCodewords()
	if len(got) != len(want) {
		t.Fatalf("codewords %v", got)
	}
	for i := range want {
		if got[i] != want[i] {
			t.Fatalf("codewords %v, not %v", got, want)
		}
	}
	// HT, LF and CR are in text compaction
	for _, codeword := range NewPDF417("a\tb\nc\r").dataCodewords() {
		if codeword == 913 {
			t.Error("HT, LF or CR was shifted to byte compaction")
		}
	}
}

func TestPDF417TheBarsAreOneBlackArtifact(t *testing.T) {
	pdf := pdfjet.NewPDF(bufio.NewWriter(new(bytes.Buffer)))
	pdf.SetCompliance(compliance.PDF_UA_1)
	page := pdfjet.NewPage(pdf, letter.Portrait())
	page.SetPenColor(color.Red)
	NewPDF417("Hello, World!").DrawOn(page)
	content := string(page.GetContent())
	if !strings.HasPrefix(content, "1 0 0 RG\n/Artifact BMC\nq\n0 0 0 RG\n") || !strings.HasSuffix(content, "Q\nEMC\n") ||
		strings.Count(content, "BMC") != 1 {
		t.Errorf("the bars are not one black artifact: %.80q", content)
	}
	// The page knows the pen is red again
	page.SetPenColor(color.Red)
	if string(page.GetContent()) != content {
		t.Error("the pen was set again after the barcode")
	}
}

func TestPDF417ADescribedBarcodeIsAFigure(t *testing.T) {
	pdf := pdfjet.NewPDF(bufio.NewWriter(new(bytes.Buffer)))
	pdf.SetCompliance(compliance.PDF_UA_1)
	page := pdfjet.NewPage(pdf, letter.Portrait())
	barcode := NewPDF417("Hello, World!").SetAltDescription("Hello, World!")
	barcode.SetLocation(50, 50)
	barcode.DrawOn(page)
	content := string(page.GetContent())
	if !strings.HasPrefix(content, "/Figure <</MCID 0>>\nBDC\nq\n") || strings.Contains(content, "/Artifact") {
		t.Errorf("the barcode is not a figure: %.80q", content)
	}
}
