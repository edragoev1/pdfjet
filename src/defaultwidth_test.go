// defaultwidth_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"slices"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Every line PDFjet draws by default is 0.5 points wide (9.1.9), as Word's
// table borders and TextBlock's and TextFrame's are: 0, the thinnest line of
// the device, is a pixel on a screen and faint or gone in print, and the PDF
// standard advises against it (ISO 32000, 8.4.3.2). 0 is still there when set.
func TestEveryLineIsHalfAPointUnlessSet(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	if w := NewCell(font, "x").GetBorderWidth(); w != 0.5 {
		t.Errorf("a cell's border: %v", w)
	}
	if w := NewRect(0, 0, 10, 10).borderWidth; w != 0.5 {
		t.Errorf("a rect's border: %v", w)
	}
	if w := NewLine(0, 0, 10, 10).width; w != 0.5 {
		t.Errorf("a line: %v", w)
	}
	if w := NewPath().width; w != 0.5 {
		t.Errorf("a path: %v", w)
	}
	page := NewPage(pdf, letter.Portrait())
	page.SetDefaultPenWidth()
	if !strings.Contains(testContent(page), "0.5 w\n") {
		t.Errorf("the default pen: %q", testContent(page))
	}
	table := NewBigTable(pdf, font, font, letter.Portrait()).SetNumberOfColumns(1)
	table.SetTableRows([]string{"Name"}, slices.Values([][]string{{"a"}}))
	table.SetLocation(20, 20)
	if err := table.Complete(); err != nil {
		t.Fatal(err)
	}
	if content := testContent(table.GetPages()[0]); !strings.Contains(content, "0.5 w\n") || strings.Contains(content, "\n0 w\n") {
		t.Errorf("a big table's lines: %q", content)
	}
	// Set to 0, the thinnest line, as before
	zero := NewBigTable(pdf, font, font, letter.Portrait()).SetNumberOfColumns(1).SetBorderWidth(0)
	zero.SetTableRows([]string{"Name"}, slices.Values([][]string{{"a"}}))
	zero.SetLocation(20, 20)
	if err := zero.Complete(); err != nil {
		t.Fatal(err)
	}
	if content := testContent(zero.GetPages()[0]); !strings.Contains(content, "0 w\n") {
		t.Errorf("a big table set to 0: %q", content)
	}
}
