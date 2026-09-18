// datamatrix_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package datamatrix

import (
	"bufio"
	"bytes"
	"math"
	"strings"
	"testing"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

func TestDataMatrixSixDigitsFitTheSmallestSquareWithItsFinderPattern(t *testing.T) {
	modules := NewDataMatrix("123456").GetModules()
	if len(modules) != 10 || len(modules[0]) != 10 {
		t.Fatalf("size %d x %d", len(modules), len(modules[0]))
	}
	for i := 0; i < 10; i++ {
		if modules[0][i] != (i%2 == 0) {
			t.Errorf("top row module %d", i)
		}
		if !modules[9][i] || !modules[i][0] {
			t.Errorf("bottom row or left column module %d is light", i)
		}
	}
}

func TestDataMatrixLongerDataGetsALargerSymbol(t *testing.T) {
	if got := len(NewDataMatrix(strings.Repeat("Z", 60)).GetModules()); got != 32 {
		t.Errorf("size %d", got)
	}
}

func TestDataMatrixTheRectangleShapeIsWiderThanTall(t *testing.T) {
	modules := NewDataMatrixWithShape("Hello, World!", Rectangle).GetModules()
	if len(modules) != 12 || len(modules[0]) != 26 {
		t.Errorf("size %d x %d", len(modules), len(modules[0]))
	}
}

func TestDataMatrixDrawOnReturnsTheCornerOfTheModules(t *testing.T) {
	page := pdfjet.NewPage(pdfjet.NewPDF(bufio.NewWriter(new(bytes.Buffer))), letter.Portrait())
	dm := NewDataMatrix("123456").SetModuleLength(3)
	dm.SetLocation(5, 5)
	xy := dm.DrawOn(page)
	if math.Abs(float64(xy[0]-35)) > 0.01 || math.Abs(float64(xy[1]-35)) > 0.01 {
		t.Errorf("corner %v", xy)
	}
}

func TestDataMatrixInAPDFUADocumentTheModulesAreAnArtifact(t *testing.T) {
	pdf := pdfjet.NewPDF(bufio.NewWriter(new(bytes.Buffer)))
	pdf.SetCompliance(compliance.PDF_UA_1)
	page := pdfjet.NewPage(pdf, letter.Portrait())
	NewDataMatrix("PDFjet").DrawOn(page)
	content := string(page.GetContent())
	if !strings.HasPrefix(content, "/Artifact BMC\n") || !strings.HasSuffix(content, "EMC\n") {
		t.Errorf("the modules are not an artifact: %q", content)
	}
}
