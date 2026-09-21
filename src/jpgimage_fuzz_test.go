// jpgimage_fuzz_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"bytes"
	"encoding/hex"
	"errors"
	"fmt"
	goimage "image"
	"image/color"
	"image/jpeg"
	"io"
	"os"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// The fuzz target of the JPEG images. Any input either makes an image that is
// drawn and written, or panics with a message; see fuzzRun. PDFjet writes the
// JPEG data as it is, so what it reads from the file is the width, the height,
// the number of color components and the Adobe marker, and those are what Go's
// image/jpeg is asked for too: a header image/jpeg reads, PDFjet reads the same
// way. A JPEG has no checksum, so whole files are fuzzed. The seeds are the
// progressive image of the examples and the small images below; go test runs
// them, and
//
//	go test ./src -run '^$' -fuzz '^FuzzJPGImage$' -fuzztime 5m -fuzzminimizetime 5s
//
// fuzzes. An input that fails is kept in testdata/fuzz, and go test runs it
// from then on.
func FuzzJPGImage(f *testing.F) {
	for _, seed := range fuzzJPEGSeeds(f) {
		f.Add(seed)
	}
	f.Fuzz(func(t *testing.T, data []byte) {
		fuzzRun(t, len(data), func() {
			if err := fuzzCompareJPEG(data); err != nil {
				t.Fatal(err)
			}
			objects := make([]*PDFobj, 0)
			NewImageForObjects(&objects, bytes.NewReader(data))

			pdf := NewPDF(bufio.NewWriter(io.Discard))
			image := NewImage(pdf, bytes.NewReader(data))
			image.SetLocation(10, 10)
			image.DrawOn(NewPage(pdf, letter.Portrait()))
			if err := pdf.Complete(); err != nil {
				t.Fatal(err)
			}
		})
	})
}

// fuzzJPEGSeeds returns a JPEG of each kind PDFjet reads: the progressive,
// three component image of the examples; a gray and a three component image
// that image/jpeg writes; the three component one with the Adobe APP14 segment
// before its other segments; and a four component CMYK image.
func fuzzJPEGSeeds(f *testing.F) [][]byte {
	progressive, err := os.ReadFile("../images/610-30x30.jpg")
	if err != nil {
		f.Fatal(err)
	}
	gray := goimage.NewGray(goimage.Rect(0, 0, 8, 8))
	rgb := goimage.NewRGBA(goimage.Rect(0, 0, 8, 8))
	for y := 0; y < 8; y++ {
		for x := 0; x < 8; x++ {
			gray.SetGray(x, y, color.Gray{Y: uint8(16 * (x + y))})
			rgb.SetRGBA(x, y, color.RGBA{R: uint8(32 * x), G: uint8(32 * y), B: 128, A: 255})
		}
	}
	return [][]byte{
		progressive,
		fuzzEncodeJPEG(f, gray),
		fuzzEncodeJPEG(f, rgb),
		fuzzWithAPP14(fuzzEncodeJPEG(f, rgb)),
		fuzzCMYKJPEG(f),
	}
}

// fuzzEncodeJPEG returns the image as a baseline JPEG.
func fuzzEncodeJPEG(f *testing.F, img goimage.Image) []byte {
	var buf bytes.Buffer
	if err := jpeg.Encode(&buf, img, nil); err != nil {
		f.Fatal(err)
	}
	return buf.Bytes()
}

// fuzzWithAPP14 returns the JPEG with an Adobe APP14 segment, of the color
// transform 1, YCbCr, after its SOI marker.
func fuzzWithAPP14(jpeg []byte) []byte {
	app14 := []byte{0xFF, 0xEE, 0x00, 0x0E, 'A', 'd', 'o', 'b', 'e', 0x00, 0x64, 0, 0, 0, 0, 1}
	return append(append(append([]byte(nil), jpeg[:2]...), app14...), jpeg[2:]...)
}

// fuzzCMYKJPEG returns 8 by 8 pixels of CMYK, as Pillow writes them: four
// components, an Adobe APP14 segment and the inks stored inverted.
func fuzzCMYKJPEG(f *testing.F) []byte {
	cmyk, err := hex.DecodeString(
		"ffd8ffee000e41646f626500640000000000ffdb004300080606070605080707070909080a0c140d0c0b0b0c" +
			"1912130f141d1a1f1e1d1a1c1c20242e2720222c231c1c2837292c30313434341f27393d38323c2e333432" +
			"ffc000140800080008044311004d11005911004b1100ffc4001f00000105010101010101000000000000" +
			"00000102030405060708090a0bffc400b5100002010303020403050504040000017d0102030004110512" +
			"2131410613516107227114328191a1082342b1c11552d1f02433627282090a161718191a252627282920" +
			"3435363738393a434445464748494a535455565758595a636465666768696a737475767778797a838485" +
			"868788898a92939495969798999aa2a3a4a5a6a7a8a9aab2b3b4b5b6b7b8b9bac2c3c4c5c6c7c8c9cad2" +
			"d3d4d5d6d7d8d9dae1e2e3e4e5e6e7e8e9eaf1f2f3f4f5f6f7f8f9faffda000e0443004d0059004b0000" +
			"3f00d6f8b7ff002dff001ad6ff0085b7ff004f1fad6b78bbc5dfeb3f79fad78de8ff00c35fffd9")
	if err != nil {
		f.Fatal(err)
	}
	return cmyk
}

// fuzzCompareJPEG returns an error when Go's image/jpeg reads the header of
// the JPEG and PDFjet does not read the same width, height, color components
// and Adobe marker from it, or fails on it. An image image/jpeg rejects is not
// compared: PDFjet reads JPEGs image/jpeg has no decoder for, the lossless and
// the arithmetic ones among them.
func fuzzCompareJPEG(data []byte) error {
	config, err := jpeg.DecodeConfig(bytes.NewReader(data))
	if err != nil {
		return nil
	}
	if config.Width == 0 || config.Height == 0 {
		// image/jpeg reads a frame header of no pixels, which PDFjet rejects:
		// such an image cannot be drawn.
		return nil
	}
	components := 0
	switch config.ColorModel {
	case color.GrayModel:
		components = 1
	case color.YCbCrModel, color.RGBAModel:
		components = 3
	case color.CMYKModel:
		components = 4
	default:
		return nil
	}
	image, err := newJPGImage(bytes.NewReader(data))
	if err != nil {
		return fmt.Errorf("image/jpeg reads %d by %d pixels of %d components, and PDFjet fails: %v",
			config.Width, config.Height, components, err)
	}
	if int(image.getWidth()) != config.Width || int(image.getHeight()) != config.Height {
		return fmt.Errorf("%g by %g pixels, not %d by %d",
			image.getWidth(), image.getHeight(), config.Width, config.Height)
	}
	if int(image.getColorComponents()) != components {
		return fmt.Errorf("%d color components, not %d", image.getColorComponents(), components)
	}
	if adobe := fuzzAdobeInHeader(data); adobe != image.isAdobe() {
		return fmt.Errorf("the Adobe APP14 segment of the header is %v, and PDFjet reads %v",
			adobe, image.isAdobe())
	}
	if components == 4 && config.Width*config.Height <= 1<<16 {
		// image/jpeg decodes a four component image only when an APP14
		// segment says that Adobe software wrote it, which is what PDFjet
		// reads the segment for: the inks of such an image are inverted.
		// image/jpeg reads the markers after the scan too, where libjpeg and
		// PDFjet read a header to the scan, so only what it does not find
		// says that PDFjet finds one too many.
		_, err := jpeg.Decode(bytes.NewReader(data))
		var unsupported jpeg.UnsupportedError
		if errors.As(err, &unsupported) && strings.Contains(string(unsupported), "APP14") && image.isAdobe() {
			return errors.New("PDFjet finds an Adobe APP14 segment, and image/jpeg does not")
		}
	}
	return nil
}

// fuzzAdobeInHeader returns whether an Adobe APP14 segment stands in the
// header of the JPEG, before its scan, which is where libjpeg's
// jpeg_read_header and PDFjet look for one. It walks the segments on its own:
// the marker of a segment, the fill bytes and the stuffed 0xFF bytes, the
// markers that stand alone, and the length of everything else.
func fuzzAdobeInHeader(data []byte) bool {
	for i := 2; i+1 < len(data); {
		if data[i] != 0xFF || data[i+1] == 0xFF || data[i+1] == 0x00 {
			i++
			continue
		}
		marker := data[i+1]
		if marker == 0x01 || (marker >= 0xD0 && marker <= 0xD8) {
			i += 2
			continue
		}
		if marker == 0xD9 || marker == 0xDA { // The end of the image, or the scan
			return false
		}
		if i+3 >= len(data) {
			return false
		}
		length := int(data[i+2])<<8 | int(data[i+3])
		if length < 2 || i+2+length > len(data) {
			return false
		}
		if marker == 0xEE && length-2 >= 12 && string(data[i+4:i+9]) == "Adobe" {
			return true
		}
		i += 2 + length
	}
	return false
}
