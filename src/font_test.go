// font_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"os"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/cjkfont"
)

func TestFontCoreFontWidthsComeFromTheAfmMetrics(t *testing.T) {
	font := testHelvetica(testNewPDF())
	if font.GetName() != "Helvetica" {
		t.Errorf("name %q", font.GetName())
	}
	testNear(t, "size", 12, font.GetSize(), 0)
	// H 722, e 556, l 222, l 222, o 556 in 1/1000 em.
	testNear(t, "Hello at 12", 27.336, font.StringWidth(12, "Hello"), 0.001)
	font.SetSize(24)
	testNear(t, "Hello at 24", 54.672, font.StringWidth(font.GetSize(), "Hello"), 0.001)
}

func TestFontTheWidthOfNoTextIsZero(t *testing.T) {
	// A Go string cannot be nil; the empty string is no text.
	testNear(t, "width", 0, testHelvetica(testNewPDF()).StringWidth(12, ""), 0)
}

func TestFontKerningPairsNarrowTheText(t *testing.T) {
	font := testHelvetica(testNewPDF())
	testNear(t, "without kerning", 16.008, font.StringWidth(12, "AV"), 0.001)
	font.SetKernPairs(true)
	// KPX A V -70
	testNear(t, "with kerning", 15.168, font.StringWidth(12, "AV"), 0.001)
}

func TestFontCoreFontVerticalMetrics(t *testing.T) {
	font := testHelvetica(testNewPDF())
	testNear(t, "ascent", 11.172, font.GetAscentAt(12), 0.001)
	testNear(t, "descent", 2.7, font.GetDescentAt(12), 0.001)
	testNear(t, "body height", 13.872, font.GetBodyHeightAt(12), 0.001)
}

func TestFontGetFitCharsCountsTheCharactersThatFit(t *testing.T) {
	if got := testHelvetica(testNewPDF()).GetFitChars("Hello world", 30); got != 5 {
		t.Errorf("got %d", got)
	}
}

func TestFontEveryCjkCharacterIsOneEmWideAndSurrogatePairsCountOnce(t *testing.T) {
	font := NewCJKFont(testNewPDF(), cjkfont.AdobeMingStdLight)
	testNear(t, "two characters", 20, font.StringWidth(10, "日本"), 0)
	testNear(t, "one supplementary character", 10, font.StringWidth(10, "𠀋"), 0)
}

func TestFontReadsAStreamFont(t *testing.T) {
	path := testRepoPath(t, "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream")
	file, err := os.Open(path)
	if err != nil {
		t.Skip("the fonts directory is not here")
	}
	defer file.Close()
	font := NewFont(testNewPDF(), file)
	if font.GetName() != "IBMPlexSans" {
		t.Errorf("name %q", font.GetName())
	}
	testNear(t, "Hello at 12", 28.32, font.StringWidth(12, "Hello"), 0.001)
}
