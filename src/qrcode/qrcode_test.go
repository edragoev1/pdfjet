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

func TestQRCodeTheSymbolIs33ModulesAtEveryLevel(t *testing.T) {
	levels := []ErrorCorrectionLevel{ErrorCorrectionLevelL, ErrorCorrectionLevelM, ErrorCorrectionLevelQ, ErrorCorrectionLevelH}
	for _, level := range levels {
		modules := NewQRCode("Hello", level).GetModules()
		if len(modules) != 33 || len(modules[0]) != 33 {
			t.Errorf("level %d: %d x %d", level, len(modules), len(modules[0]))
		}
	}
}

func TestQRCodeFinderPatternsAreInThreeCorners(t *testing.T) {
	modules := NewQRCode("Hello", ErrorCorrectionLevelL).GetModules()
	for i := 0; i < 7; i++ {
		if !testDark(modules, 0, i) || !testDark(modules, 0, 32-i) || !testDark(modules, 32, i) || !testDark(modules, i, 0) {
			t.Errorf("finder module %d is light", i)
		}
	}
	if testDark(modules, 0, 7) || testDark(modules, 7, 0) {
		t.Error("separator is dark")
	}
}

func TestQRCodeDataThatDoesNotFitThrows(t *testing.T) {
	// Java throws IllegalArgumentException; Go panics.
	if len(NewQRCode(strings.Repeat("a", 50), ErrorCorrectionLevelM).GetModules()) != 33 {
		t.Error("50 characters at M")
	}
	if !testOverflows(func() { NewQRCode(strings.Repeat("a", 80), ErrorCorrectionLevelL) }) {
		t.Error("80 characters at L did not panic")
	}
	if !testOverflows(func() { NewQRCode(strings.Repeat("a", 50), ErrorCorrectionLevelQ) }) {
		t.Error("50 characters at Q did not panic")
	}
}

func TestQRCodeDrawOnReturnsTheSameCornerEveryTime(t *testing.T) {
	page := testNewPage()
	qr := NewQRCode("Hello", ErrorCorrectionLevelL).SetModuleLength(2)
	qr.SetLocation(10, 10)
	testAssertXY(t, 76, 76, qr.DrawOn(page))
	testAssertXY(t, 76, 76, qr.DrawOn(page))
}
