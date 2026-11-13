// readerrors_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"errors"
	"io/fs"
	"os"
	"testing"
)

// ReadImage and ReadFontFromFile return, as errors, what NewImage and
// NewFontFromFile panic with, in the same words, and add nothing to the PDF;
// a file they can read is the same image or font as theirs.
func TestReadImageReturnsTheErrorNewImagePanicsWith(t *testing.T) {
	jpeg, err := os.ReadFile("../resources/images/cmyk.jpg")
	if err != nil {
		t.Fatal(err)
	}
	png, err := os.ReadFile("../tests/data/PngSuite/BASN2C08.PNG")
	if err != nil {
		t.Fatal(err)
	}
	for _, c := range []struct {
		name string
		data []byte
		want string
	}{
		{"not an image", []byte("GIF89a"), "The image is not a PNG, JPEG or BMP file."},
		{"empty", nil, "The image is not a PNG, JPEG or BMP file."},
		{"a JPEG cut short", jpeg[:len(jpeg)/2], errJPEGCutShort.Error()},
		{"a PNG cut short", png[:40], "Unexpected end of the PNG stream."},
	} {
		pdf := testNewPDF()
		image, err := ReadImage(pdf, bytes.NewReader(c.data))
		if image != nil || err == nil || err.Error() != c.want {
			t.Errorf("%s: %v, %v, not %q", c.name, image, err, c.want)
		}
		if len(pdf.objOffsets) != 0 {
			t.Errorf("%s: %d objects added to the PDF", c.name, len(pdf.objOffsets))
		}
		func() {
			defer func() {
				if r := recover(); r == nil || panicError(r).Error() != c.want {
					t.Errorf("%s: NewImage panicked with %v", c.name, r)
				}
			}()
			NewImage(testNewPDF(), bytes.NewReader(c.data))
		}()
	}
	image, err := ReadImage(testNewPDF(), bytes.NewReader(png))
	if err != nil || image.GetWidth() != 32 || image.GetHeight() != 32 {
		t.Errorf("%v, %v", image, err)
	}
}

func TestReadFontFromFileReturnsTheErrorNewFontFromFilePanicsWith(t *testing.T) {
	for _, path := range []string{"", "fonts/NoSuchFont/NoSuchFont-Regular.ttf", "../tests/data/PngSuite/BASN2C08.PNG"} {
		pdf := testNewPDF()
		font, err := ReadFontFromFile(pdf, path)
		if font != nil || err == nil {
			t.Fatalf("%q: %v, %v", path, font, err)
		}
		if path != "../tests/data/PngSuite/BASN2C08.PNG" && !errors.Is(err, fs.ErrNotExist) {
			t.Errorf("%q: %v is not fs.ErrNotExist", path, err)
		}
		if len(pdf.fonts) != 0 {
			t.Errorf("%q: %d fonts added to the PDF", path, len(pdf.fonts))
		}
		func() {
			defer func() {
				if r := recover(); r == nil || panicError(r).Error() != err.Error() {
					t.Errorf("%q: NewFontFromFile panicked with %v, not %v", path, r, err)
				}
			}()
			NewFontFromFile(testNewPDF(), path)
		}()
	}
	testWant(t, "open : no such file or directory", func() string {
		_, err := ReadFontFromFile(testNewPDF(), "")
		return err.Error()
	}())
	font, err := ReadFontFromFile(testNewPDF(), "../tests/fonts/IBMPlexSans-Regular.otf")
	if err != nil || font.GetName() != NewFontFromFile(testNewPDF(), "../tests/fonts/IBMPlexSans-Regular.otf").GetName() {
		t.Errorf("%v, %v", font, err)
	}
}

// A runtime error is a mistake of PDFjet's own, not the input's: it panics
// again.
func TestPanicErrorPanicsAgainWithARuntimeError(t *testing.T) {
	defer func() {
		if r := recover(); r == nil {
			t.Error("no panic")
		}
	}()
	var m map[string]int
	func() {
		defer func() { panicError(recover()) }()
		m["x"] = 1
	}()
}
