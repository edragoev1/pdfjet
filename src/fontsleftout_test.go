// fontsleftout_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

//go:build pdfjet_nofonts

package pdfjet

import (
	"bufio"
	"bytes"
	"errors"
	"fmt"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Built with pdfjet_nofonts, the program has none of the fonts PDFjet carries,
// and says so: of a constant, and of the text of an SVG image with no font set.
func TestNoFontsTheFontsAreLeftOutAndSaySo(t *testing.T) {
	if bundledFontHas(IBMPlexSans.Regular) {
		t.Fatal("IBM Plex Sans is built in")
	}
	_, err := readFontFile(IBMPlexSans.Regular)
	var notFound *fontNotFoundError
	if !errors.As(err, &notFound) || !strings.Contains(err.Error(), "pdfjet_nofonts") {
		t.Errorf("the error is %v", err)
	}
	// A font file of the program's own that is missing: the file is what is
	// missing, and the tag is not named (the review of 2 November 2026)
	_, err = readFontFile("fonts/MyFamily/MyFamily-Regular.ttf")
	if !errors.As(err, &notFound) || strings.Contains(err.Error(), "pdfjet_nofonts") {
		t.Errorf("of a font of the program's own, the error is %v", err)
	}
	svg, err := NewSVGImage(strings.NewReader(`<svg xmlns="http://www.w3.org/2000/svg" width="100" height="20"><text x="0" y="15">PDFjet</text></svg>`))
	if err != nil {
		t.Fatal(err)
	}
	pdf := NewPDF(bufio.NewWriter(new(bytes.Buffer)))
	page := NewPage(pdf, letter.Portrait())
	func() {
		defer func() {
			if r := recover(); r == nil || !strings.Contains(fmt.Sprint(r), "SetFont") {
				t.Errorf("recovered %v", r)
			}
		}()
		svg.DrawOn(page)
	}()
}
