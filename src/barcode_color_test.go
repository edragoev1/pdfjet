// barcode_color_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// The pdf417, qrcode and datamatrix packages import this package, so this
// test is in a package of its own.
package pdfjet_test

import (
	"bufio"
	"bytes"
	"fmt"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/datamatrix"
	"github.com/edragoev1/pdfjet/v9/src/errorcorrectionlevel"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/pdf417"
	"github.com/edragoev1/pdfjet/v9/src/qrcode"
)

// The color of the barcodes, SetModuleColor, black unless set.

const (
	testNavy       = 0x1F3A5F
	testNavyStroke = "0.12 0.23 0.37 RG"
	testNavyFill   = "0.12 0.23 0.37 rg"
)

func testNewBarcodePage() *pdfjet.Page {
	pdf := pdfjet.NewPDF(bufio.NewWriter(new(bytes.Buffer)))
	return pdfjet.NewPage(pdf, letter.Portrait())
}

// testBarcodeFillColorBefore returns the fill color set last before the text,
// drawn in a core font, as its operator, for example "1 1 1 rg".
func testBarcodeFillColorBefore(content, text string) string {
	i := strings.Index(content, "<"+fmt.Sprintf("%X", []byte(text))+">")
	if i == -1 {
		return ""
	}
	rg := strings.LastIndex(content[:i], " rg\n")
	if rg == -1 {
		return ""
	}
	return content[strings.LastIndex(content[:rg], "\n")+1 : rg+3]
}

func TestBarcodeColorALinearBarcodeDrawsItsBarsAndItsTextInItsColor(t *testing.T) {
	pdf := pdfjet.NewPDF(bufio.NewWriter(new(bytes.Buffer)))
	page := pdfjet.NewPage(pdf, letter.Portrait())
	barcode := pdfjet.NewBarcode(pdfjet.CODE_128, "A1").SetFont(pdfjet.NewCoreFont(pdf, corefont.Helvetica()))
	barcode.SetLocation(50, 50)
	barcode.SetModuleColor(testNavy).DrawOn(page)
	content := string(page.GetContent())
	if !strings.Contains(content, testNavyStroke) {
		t.Errorf("no %q in %s", testNavyStroke, content)
	}
	if got := testBarcodeFillColorBefore(content, "A1"); got != testNavyFill {
		t.Errorf("want %q before the text, got %q", testNavyFill, got)
	}
}

func TestBarcodeColorALinearBarcodeIsBlackUnlessSet(t *testing.T) {
	pdf := pdfjet.NewPDF(bufio.NewWriter(new(bytes.Buffer)))
	page := pdfjet.NewPage(pdf, letter.Portrait())
	page.SetPenColor(color.Red)
	barcode := pdfjet.NewBarcode(pdfjet.CODE_128, "A1").SetFont(pdfjet.NewCoreFont(pdf, corefont.Helvetica()))
	barcode.SetLocation(50, 50)
	barcode.DrawOn(page)
	if content := string(page.GetContent()); !strings.Contains(content, "0 0 0 RG") {
		t.Errorf("no %q in %s", "0 0 0 RG", content)
	}
}

func TestBarcodeColorAPDF417DrawsInItsColor(t *testing.T) {
	page := testNewBarcodePage()
	pdf417.NewPDF417("A1").SetModuleColor(testNavy).SetLocation(50, 50).DrawOn(page)
	if content := string(page.GetContent()); !strings.Contains(content, testNavyStroke) {
		t.Errorf("no %q in %s", testNavyStroke, content)
	}
}

func TestBarcodeColorTheTwoDimensionalCodesDrawInTheirColor(t *testing.T) {
	page := testNewBarcodePage()
	qrcode.NewQRCode("A1", errorcorrectionlevel.M).SetModuleColor(testNavy).SetLocation(50, 50).DrawOn(page)
	if content := string(page.GetContent()); !strings.Contains(content, testNavyFill) {
		t.Errorf("QRCode: no %q in %s", testNavyFill, content)
	}
	page = testNewBarcodePage()
	datamatrix.NewDataMatrix("A1").SetModuleColor(testNavy).SetLocation(50, 50).DrawOn(page)
	if content := string(page.GetContent()); !strings.Contains(content, testNavyFill) {
		t.Errorf("DataMatrix: no %q in %s", testNavyFill, content)
	}
}

// The setters of the pdf417, qrcode and datamatrix packages, which the test of
// every color setter in package pdfjet cannot import, leave the color as it
// was for color.Transparent: the code set to navy, then to transparent, draws
// in navy.
func TestBarcodeColorTransparentLeavesTheColor(t *testing.T) {
	cases := []struct {
		name string
		draw func(page *pdfjet.Page)
		want string
	}{
		{"PDF417.SetModuleColor", func(page *pdfjet.Page) {
			pdf417.NewPDF417("A1").SetModuleColor(testNavy).SetModuleColor(color.Transparent).SetLocation(50, 50).DrawOn(page)
		}, testNavyStroke},
		{"QRCode.SetModuleColor", func(page *pdfjet.Page) {
			qrcode.NewQRCode("A1", errorcorrectionlevel.M).SetModuleColor(testNavy).SetModuleColor(color.Transparent).SetLocation(50, 50).DrawOn(page)
		}, testNavyFill},
		{"DataMatrix.SetModuleColor", func(page *pdfjet.Page) {
			datamatrix.NewDataMatrix("A1").SetModuleColor(testNavy).SetModuleColor(color.Transparent).SetLocation(50, 50).DrawOn(page)
		}, testNavyFill},
	}
	for _, c := range cases {
		page := testNewBarcodePage()
		c.draw(page)
		if !strings.Contains(string(page.GetContent()), c.want) {
			t.Errorf("%s: color.Transparent changed it", c.name)
		}
	}
}
