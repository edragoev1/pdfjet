// barcode_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"fmt"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/direction"
)

var testBarcodeCorners = []struct {
	barcodeType int
	text        string
	direction   direction.Direction
	withFont    bool
	x, y        float32
}{
	{EAN_13, "012345678901", direction.LeftToRight, false, 171.25, 145.5},
	{EAN_13, "012345678901", direction.LeftToRight, true, 171.25, 151.31},
	{EAN_13, "012345678901", direction.BottomToTop, false, 145.5, 171.25},
	{EAN_13, "012345678901", direction.BottomToTop, true, 151.31, 179.59},
	{EAN_13, "012345678901", direction.TopToBottom, false, 145.5, 171.25},
	{EAN_13, "012345678901", direction.TopToBottom, true, 145.5, 171.25},
	{UPC_A, "01234567890", direction.LeftToRight, false, 171.25, 145.5},
	{UPC_A, "01234567890", direction.LeftToRight, true, 179.59, 151.31},
	{UPC_A, "01234567890", direction.BottomToTop, false, 145.5, 171.25},
	{UPC_A, "01234567890", direction.BottomToTop, true, 151.31, 179.59},
	{UPC_A, "01234567890", direction.TopToBottom, false, 145.5, 171.25},
	{UPC_A, "01234567890", direction.TopToBottom, true, 145.5, 179.59},
	{CODE_128, "Hello", direction.LeftToRight, false, 167.5, 137.5},
	{CODE_128, "Hello", direction.LeftToRight, true, 167.5, 154.072},
	{CODE_128, "Hello", direction.BottomToTop, false, 137.5, 167.5},
	{CODE_128, "Hello", direction.BottomToTop, true, 154.072, 167.5},
	{CODE_128, "Hello", direction.TopToBottom, false, 137.5, 167.5},
	{CODE_128, "Hello", direction.TopToBottom, true, 137.5, 167.5},
	{CODE_39, "HELLO-39", direction.LeftToRight, false, 219.25, 137.5},
	{CODE_39, "HELLO-39", direction.LeftToRight, true, 219.25, 154.072},
	{CODE_39, "HELLO-39", direction.BottomToTop, false, 137.5, 219.25},
	{CODE_39, "HELLO-39", direction.BottomToTop, true, 154.072, 219.25},
	{CODE_39, "HELLO-39", direction.TopToBottom, false, 137.5, 219.25},
	{CODE_39, "HELLO-39", direction.TopToBottom, true, 137.5, 219.25},
}

func TestBarcodeDrawOnReturnsTheCornerOfTheBarsAndTheTextInEveryDirection(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	font := testHelvetica(pdf)
	for _, row := range testBarcodeCorners {
		barcode := NewBarcode(row.barcodeType, row.text).SetDirection(row.direction)
		if row.withFont {
			barcode.SetFont(font)
		}
		barcode.SetLocation(100, 100)
		name := fmt.Sprintf("%d %v font %v", row.barcodeType, row.direction, row.withFont)
		first := barcode.DrawOn(page)
		testNear(t, name+" x", row.x, first[0], testDelta)
		testNear(t, name+" y", row.y, first[1], testDelta)
		if second := barcode.DrawOn(page); second != first {
			t.Errorf("%s drawn again: %v", name, second)
		}
		testNear(t, name+" height", row.y-100, barcode.GetHeight(), testDelta)
	}
}

func TestBarcodeCode39RejectsCharactersItCannotEncode(t *testing.T) {
	page := testNewPage()
	message, panicked := testPanic(func() { NewBarcode(CODE_39, "hello").DrawOn(page) })
	if !panicked || message != "The input string '*hello*' contains characters that are invalid in a Code39 barcode." {
		t.Errorf("panicked %v with %q", panicked, message)
	}
}

func TestBarcodeUpcAndEanNeedTheirNumberOfDigits(t *testing.T) {
	if message, _ := testPanic(func() { NewBarcode(UPC_A, "123") }); message != "UPC-A barcodes must have exactly 11 digits!" {
		t.Errorf("UPC-A: %q", message)
	}
	if message, _ := testPanic(func() { NewBarcode(EAN_13, "0123456789012") }); message != "EAN-13 barcodes must have exactly 12 digits!" {
		t.Errorf("EAN-13: %q", message)
	}
}

func TestBarcodeCode128RefusesATextItCannotHold(t *testing.T) {
	const tooLong = "Code 128 barcodes hold at most 48 codewords, and a character below 32 or from 128 to 255 takes two!"
	for text, want := range map[string]string{
		strings.Repeat("7", 49): tooLong,
		strings.Repeat("é", 25): tooLong, // Two codewords each
		"A€":                    "Code 128 barcodes can only hold characters up to U+00FF!",
	} {
		if message, _ := testPanic(func() { NewBarcode(CODE_128, text) }); message != want {
			t.Errorf("%q: %q", text, message)
		}
	}
	// The most a barcode holds is drawn whole: 48 codewords, and the start,
	// the check digit and the stop, of 11 modules each but the stop of 13
	for _, text := range []string{strings.Repeat("7", 48), strings.Repeat("é", 24)} {
		if width := NewBarcode(CODE_128, text).DrawOn(nil)[0]; width != (11*50+13)*0.75 {
			t.Errorf("%q is %v wide", text, width)
		}
	}
}

func TestBarcodeIsBlackWhateverPenColorThePageHas(t *testing.T) {
	page := testNewPage()
	page.SetPenColor(color.Blue)
	NewBarcode(CODE_128, "AB").DrawOn(page)
	content := string(page.buf)
	if !strings.Contains(content, "q\n0 0 0 RG\n") || !strings.HasSuffix(content, "Q\n") {
		t.Errorf("the barcode is not drawn in black in a state of its own:\n%s", content)
	}
}
