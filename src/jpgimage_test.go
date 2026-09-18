// jpgimage_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"os"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// A CMYK JPEG that Adobe software wrote, as images/cmyk.jpg is, has an APP14
// marker and stores its inks inverted, so the image is written with a Decode
// array that inverts them back; one without the marker is written as it is.

// testWithoutAPP14 returns the JPEG without its APP14 segment.
func testWithoutAPP14(jpeg []byte) []byte {
	for i := 2; i+3 < len(jpeg); i++ {
		if jpeg[i] == 0xFF && jpeg[i+1] == 0xEE {
			end := i + 2 + (int(jpeg[i+2])<<8 | int(jpeg[i+3]))
			return append(append([]byte(nil), jpeg[:i]...), jpeg[end:]...)
		}
	}
	return jpeg
}

func testPDFWith(jpeg []byte) string {
	doc := testNewDoc()
	NewImage(doc.pdf, bytes.NewReader(jpeg)).DrawOn(NewPage(doc.pdf, letter.Portrait()))
	return string(doc.complete())
}

func TestJPGImageTheInksOfACmykJpegAreInvertedBackOnlyWhenAdobeSoftwareWroteIt(t *testing.T) {
	jpeg, err := os.ReadFile(testRepoPath(t, "images/cmyk.jpg"))
	if err != nil {
		t.Skip("the images directory is not here")
	}
	image, err := newJPGImage(bytes.NewReader(jpeg))
	if err != nil || !image.isAdobe() {
		t.Errorf("the Adobe marker is not found: %v", err)
	}
	if !strings.Contains(testPDFWith(jpeg), "/Decode [1.0 0.0 1.0 0.0 1.0 0.0 1.0 0.0]") {
		t.Error("the inks are not inverted back")
	}

	plain := testWithoutAPP14(jpeg)
	image, err = newJPGImage(bytes.NewReader(plain))
	if err != nil || image.isAdobe() {
		t.Errorf("an Adobe marker is found without one: %v", err)
	}
	raw := testPDFWith(plain)
	if !strings.Contains(raw, "/DeviceCMYK") || strings.Contains(raw, "/Decode [") {
		t.Error("an image without the marker has its inks inverted")
	}
}
