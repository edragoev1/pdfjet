// review_codes_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package datamatrix

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

func TestDataMatrixADescribedBarcodeIsAFigure(t *testing.T) {
	pdf := pdfjet.NewPDF(bufio.NewWriter(new(bytes.Buffer)))
	pdf.SetCompliance(compliance.PDF_UA_1)
	page := pdfjet.NewPage(pdf, letter.Portrait())
	dm := NewDataMatrix("Hello, World!").SetAltDescription("Hello, World!")
	dm.SetLocation(50, 50)
	dm.DrawOn(page)
	content := string(page.GetContent())
	if !strings.HasPrefix(content, "/Figure <</MCID 0>>\nBDC\nq\n") || strings.Contains(content, "/Artifact") {
		t.Errorf("the barcode is not a figure: %.80q", content)
	}
	// Not described, it is decoration, as before
	plain := pdfjet.NewPage(pdf, letter.Portrait())
	NewDataMatrix("Hello, World!").DrawOn(plain)
	if content := string(plain.GetContent()); !strings.HasPrefix(content, "/Artifact BMC\nq\n") {
		t.Errorf("a barcode with no description is not decoration: %.80q", content)
	}
}

func TestDataMatrixTheBrushOfThePageIsKept(t *testing.T) {
	page := pdfjet.NewPage(pdfjet.NewPDF(bufio.NewWriter(new(bytes.Buffer))), letter.Portrait())
	page.SetBrushColor(color.Blue)
	NewDataMatrix("Hello").SetModuleColor(color.Red).DrawOn(page)
	content := string(page.GetContent())
	if !strings.HasSuffix(content, "Q\n") {
		t.Errorf("the modules are not in q and Q: %q", content[len(content)-20:])
	}
	// The page knows the brush is blue again, and sets red when asked
	page.SetBrushColor(color.Blue)
	page.SetBrushColor(color.Red)
	if got := string(page.GetContent())[len(content):]; got != "1 0 0 rg\n" {
		t.Errorf("after the barcode: %q", got)
	}
}
