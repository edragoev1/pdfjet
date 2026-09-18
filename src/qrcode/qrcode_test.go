// qrcode_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package qrcode

import (
	"bufio"
	"bytes"
	"math"
	"strings"
	"testing"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/errorcorrectionlevel"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

func testNewPage() *pdfjet.Page {
	return pdfjet.NewPage(pdfjet.NewPDF(bufio.NewWriter(new(bytes.Buffer))), letter.Portrait())
}

func testAssertXY(t *testing.T, x, y float32, xy [2]float32) {
	t.Helper()
	if math.Abs(float64(x-xy[0])) > 0.01 || math.Abs(float64(y-xy[1])) > 0.01 {
		t.Errorf("want (%v, %v), got %v", x, y, xy)
	}
}

func testDark(modules [][]*bool, row, column int) bool {
	return modules[row][column] != nil && *modules[row][column]
}

func testOverflows(fn func()) (overflows bool) {
	defer func() {
		if r := recover(); r != nil {
			overflows = true
		}
	}()
	fn()
	return false
}

func TestQRCodeShortDataKeepsTheSymbolAt33ModulesAtEveryLevel(t *testing.T) {
	levels := []errorcorrectionlevel.ErrorCorrectionLevel{errorcorrectionlevel.L, errorcorrectionlevel.M, errorcorrectionlevel.Q, errorcorrectionlevel.H}
	for _, level := range levels {
		modules := NewQRCode("Hello", level).GetModules()
		if len(modules) != 33 || len(modules[0]) != 33 {
			t.Errorf("level %d: %d x %d", level, len(modules), len(modules[0]))
		}
	}
}

func TestQRCodeLongerDataMakesALargerSymbol(t *testing.T) {
	cases := []struct {
		length  int
		level   errorcorrectionlevel.ErrorCorrectionLevel
		modules int
	}{
		{78, errorcorrectionlevel.L, 33},    // version 4
		{79, errorcorrectionlevel.L, 37},    // version 5
		{2953, errorcorrectionlevel.L, 177}, // version 40
		{1273, errorcorrectionlevel.H, 177}, // version 40
	}
	for _, c := range cases {
		if n := len(NewQRCode(strings.Repeat("a", c.length), c.level).GetModules()); n != c.modules {
			t.Errorf("%d bytes at level %d: %d modules, want %d", c.length, c.level, n, c.modules)
		}
	}
}

func TestQRCodeVersionsFrom7CarryTheirVersionNumber(t *testing.T) {
	// 120 bytes at level M need version 7, 45 modules, whose version information is 0x07C94.
	modules := NewQRCode(strings.Repeat("a", 120), errorcorrectionlevel.M).GetModules()
	if len(modules) != 45 {
		t.Fatalf("%d modules, want 45", len(modules))
	}
	for i := 0; i < 18; i++ {
		bit := ((0x07C94 >> i) & 1) == 1
		if testDark(modules, i/3, i%3+45-11) != bit {
			t.Errorf("top right, bit %d", i)
		}
		if testDark(modules, i%3+45-11, i/3) != bit {
			t.Errorf("bottom left, bit %d", i)
		}
	}
}

func TestQRCodeFinderPatternsAreInThreeCorners(t *testing.T) {
	modules := NewQRCode("Hello", errorcorrectionlevel.L).GetModules()
	for i := 0; i < 7; i++ {
		if !testDark(modules, 0, i) || !testDark(modules, 0, 32-i) || !testDark(modules, 32, i) || !testDark(modules, i, 0) {
			t.Errorf("finder module %d is light", i)
		}
	}
	if testDark(modules, 0, 7) || testDark(modules, 7, 0) {
		t.Error("separator is dark")
	}
}

func TestQRCodeDataThatDoesNotFitVersion40Panics(t *testing.T) {
	// Java throws IllegalArgumentException; Go panics.
	if !testOverflows(func() { NewQRCode(strings.Repeat("a", 2954), errorcorrectionlevel.L) }) {
		t.Error("2954 bytes at L did not panic")
	}
	if !testOverflows(func() { NewQRCode(strings.Repeat("a", 1274), errorcorrectionlevel.H) }) {
		t.Error("1274 bytes at H did not panic")
	}
}

func TestQRCodeDrawOnReturnsTheSameCornerEveryTime(t *testing.T) {
	page := testNewPage()
	qr := NewQRCode("Hello", errorcorrectionlevel.L).SetModuleLength(2)
	qr.SetLocation(10, 10)
	testAssertXY(t, 76, 76, qr.DrawOn(page))
	testAssertXY(t, 76, 76, qr.DrawOn(page))
}

func TestQRCodeInAPDFUADocumentTheModulesAreAnArtifact(t *testing.T) {
	pdf := pdfjet.NewPDF(bufio.NewWriter(new(bytes.Buffer)))
	pdf.SetCompliance(compliance.PDF_UA_1)
	page := pdfjet.NewPage(pdf, letter.Portrait())
	NewQRCode("https://pdfjet.com", errorcorrectionlevel.M).DrawOn(page)
	content := string(page.GetContent())
	if !strings.HasPrefix(content, "/Artifact BMC\n") || !strings.HasSuffix(content, "EMC\n") {
		t.Errorf("the modules are not an artifact: %q", content)
	}
}
