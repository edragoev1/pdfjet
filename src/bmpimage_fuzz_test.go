// bmpimage_fuzz_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"bytes"
	"io"
	"os"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// The fuzz target of the BMP images. Any input either makes an image that is
// drawn and written, or panics with a message; see fuzzRun. A BMP file has no
// checksum and no compression that PDFjet reads, so whole files are fuzzed.
// The seeds are two of the BMP files of the examples and the BMPs of the unit
// tests; go test runs them, and
//
//	go test ./src -run '^$' -fuzz '^FuzzBMPImage$' -fuzztime 5m -fuzzminimizetime 5s
//
// fuzzes. An input that fails is kept in testdata/fuzz, and go test runs it
// from then on.
func FuzzBMPImage(f *testing.F) {
	for _, path := range []string{"../images/palette.bmp", "../images/rgb24pal.bmp"} {
		bmp, err := os.ReadFile(path)
		if err != nil {
			f.Fatal(err)
		}
		f.Add(bmp)
	}
	// 2 by 2 pixels of every bit depth, with the masks of 16 and 32 bit pixels
	// or without, in a header of 40 bytes and in the larger one of 124 bytes,
	// bottom-up and top-down.
	palette := []uint32{0xFF0000, 0x00FF00, 0x0000FF, 0xFFFFFF}
	masks16 := []uint32{0xF800, 0x07E0, 0x001F}
	masks32 := []uint32{0xFF0000, 0xFF00, 0xFF}
	f.Add(testBMP(40, 1, 0, nil, palette[:2], []byte{0x40}, []byte{0x80}))
	f.Add(testBMP(40, 4, 0, nil, palette, []byte{0x01}, []byte{0x23}))
	f.Add(testBMP(40, 8, 0, nil, palette, []byte{0, 1}, []byte{2, 3}))
	f.Add(testBMP(40, 16, 0, nil, nil, testShorts(0x7C00, 0x03E0), testShorts(0x001F, 0x7FFF)))
	f.Add(testBMP(40, 16, 3, masks16, nil, testShorts(0xF800, 0x07E0), testShorts(0x001F, 0xFFFF)))
	f.Add(testBMP(124, 16, 3, masks16, nil, testShorts(0xF800, 0x07E0), testShorts(0x001F, 0xFFFF)))
	f.Add(testBMP(40, 24, 0, nil, nil, []byte{0, 0, 255, 0, 255, 0}, []byte{255, 0, 0, 255, 255, 255}))
	f.Add(testBMP(40, 32, 0, nil, nil, []byte{0, 0, 255, 0, 0, 255, 0, 0}, []byte{255, 0, 0, 0, 1, 2, 3, 4}))
	f.Add(testBMP(124, 32, 3, masks32, nil, []byte{0, 0, 255, 0, 0, 255, 0, 0}, []byte{255, 0, 0, 0, 1, 2, 3, 4}))
	// An alpha mask, of 32 and 16 bit pixels, in the headers of 56 bytes and
	// more that have one.
	f.Add(testBMP(124, 32, 3, testMasksBGRA, nil, []byte{0, 0, 255, 0x80, 0, 255, 0, 0}, []byte{255, 0, 0, 0xFF, 1, 2, 3, 4}))
	f.Add(testBMP(56, 16, 3, []uint32{0x0F00, 0x00F0, 0x000F, 0xF000}, nil, testShorts(0x0F00, 0x40F0), testShorts(0x800F, 0xFFFF)))
	f.Add(testBMP24(true))
	f.Add(testBMP24(false))
	f.Fuzz(func(t *testing.T, bmp []byte) {
		fuzzRun(t, len(bmp), func() {
			objects := make([]*PDFobj, 0)
			NewImageForObjects(&objects, bytes.NewReader(bmp))

			pdf := NewPDF(bufio.NewWriter(io.Discard))
			image := NewImage(pdf, bytes.NewReader(bmp))
			image.SetLocation(10, 10)
			image.DrawOn(NewPage(pdf, letter.Portrait()))
			if err := pdf.Complete(); err != nil {
				t.Fatal(err)
			}
		})
	})
}
