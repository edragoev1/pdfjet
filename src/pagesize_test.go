// pagesize_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/a3"
	"github.com/edragoev1/pdfjet/v9/src/a4"
	"github.com/edragoev1/pdfjet/v9/src/a5"
	"github.com/edragoev1/pdfjet/v9/src/b5"
	"github.com/edragoev1/pdfjet/v9/src/executive"
	"github.com/edragoev1/pdfjet/v9/src/jisb5"
	"github.com/edragoev1/pdfjet/v9/src/legal"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/pagesize"
	"github.com/edragoev1/pdfjet/v9/src/tabloid"
)

func testAssertSize(t *testing.T, name string, width, height float32, portrait, landscape pagesize.PageSize) {
	t.Helper()
	if portrait.GetWidth() != width || portrait.GetHeight() != height {
		t.Errorf("%s portrait: %v x %v", name, portrait.GetWidth(), portrait.GetHeight())
	}
	if landscape.GetWidth() != height || landscape.GetHeight() != width {
		t.Errorf("%s landscape: %v x %v", name, landscape.GetWidth(), landscape.GetHeight())
	}
}

func TestPageSizeIsoSizesInPoints(t *testing.T) {
	testAssertSize(t, "A3", 842, 1191, a3.Portrait(), a3.Landscape())
	testAssertSize(t, "A4", 595, 842, a4.Portrait(), a4.Landscape())
	testAssertSize(t, "A5", 420, 595, a5.Portrait(), a5.Landscape())
	testAssertSize(t, "B5", 499, 709, b5.Portrait(), b5.Landscape())
}

func TestPageSizeJapaneseAndNorthAmericanSizesInPoints(t *testing.T) {
	testAssertSize(t, "JISB5", 516, 729, jisb5.Portrait(), jisb5.Landscape())
	testAssertSize(t, "Letter", 612, 792, letter.Portrait(), letter.Landscape())
	testAssertSize(t, "Legal", 612, 1008, legal.Portrait(), legal.Landscape())
	testAssertSize(t, "Executive", 522, 756, executive.Portrait(), executive.Landscape())
	testAssertSize(t, "Tabloid", 792, 1224, tabloid.Portrait(), tabloid.Landscape())
}

func TestPageSizeAPageTakesItsSizeFromThePageSize(t *testing.T) {
	page := NewPage(testNewPDF(), a4.Landscape())
	if page.GetWidth() != 842 || page.GetHeight() != 595 {
		t.Errorf("page: %v x %v", page.GetWidth(), page.GetHeight())
	}
	custom := pagesize.NewPageSize(100, 200)
	if custom.GetWidth() != 100 || custom.GetHeight() != 200 {
		t.Errorf("custom: %v x %v", custom.GetWidth(), custom.GetHeight())
	}
}
