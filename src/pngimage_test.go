// pngimage_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"fmt"
	"hash/crc32"
	"os"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/decompressor"
	"github.com/edragoev1/pdfjet/v9/src/imagetype"
)

// PNG decoding against the PngSuite images. The samples are compared as the
// CRC-32 of the decompressed data, with the values of the Java tests. MuPDF and
// Pillow decode the 8-bit, filtered, palette and gray with alpha images to the
// same samples.

var testPngSuite = []struct {
	name                         string
	width, height                float32
	colorType, bitDepth, samples int
	samplesCRC                   string
	alpha                        int
	alphaCRC                     string
}{
	{"BASN0G01", 32, 32, 0, 1, 128, "b71a0667", 0, ""},
	{"BASN0G02", 32, 32, 0, 2, 256, "c429db1d", 0, ""},
	{"BASN0G04", 32, 32, 0, 4, 512, "8089a6e9", 0, ""},
	{"BASN0G08", 32, 32, 0, 8, 1024, "784b4a4e", 0, ""},
	{"BASN0G16", 32, 32, 0, 16, 2048, "9362f0f0", 0, ""},
	{"BASN2C08", 32, 32, 2, 8, 3072, "7855b9bf", 0, ""},
	{"BASN2C16", 32, 32, 2, 16, 6144, "c278125a", 0, ""},
	{"BASN3P01", 32, 32, 3, 1, 3072, "31ec284b", 0, ""},
	{"BASN3P02", 32, 32, 3, 2, 3072, "279a463a", 0, ""},
	{"BASN3P04", 32, 32, 3, 4, 3072, "3a9e038e", 0, ""},
	{"BASN3P08", 32, 32, 3, 8, 3072, "ff6e2940", 0, ""},
	{"BASN6A08", 32, 32, 6, 8, 3072, "a9b0c6b5", 1024, "fa6029ad"},
	{"TP1N3P08", 32, 32, 3, 8, 3072, "8b0a6c2c", 1024, "f83b2838"},
	{"F00N2C08", 32, 32, 2, 8, 3072, "3f1d66ad", 0, ""},
	{"F01N2C08", 32, 32, 2, 8, 3072, "11c1b27e", 0, ""},
	{"F02N2C08", 32, 32, 2, 8, 3072, "7f1ca785", 0, ""},
	{"F03N2C08", 32, 32, 2, 8, 3072, "31645d89", 0, ""},
	{"F04N2C08", 32, 32, 2, 8, 3072, "77056a6f", 0, ""},
	{"F00N0G08", 32, 32, 0, 8, 1024, "1f18265f", 0, ""},
	{"F04N0G08", 32, 32, 0, 8, 1024, "b8006228", 0, ""},
	{"S01N3P01", 1, 1, 3, 1, 3, "d243369f", 0, ""},
	{"S05N3P02", 5, 5, 3, 2, 75, "1242b6fb", 0, ""},
}

func testCRC(data []byte) string {
	return fmt.Sprintf("%08x", crc32.ChecksumIEEE(data))
}

func testInflate(t *testing.T, data []byte) []byte {
	t.Helper()
	inflated, err := decompressor.Inflate(data)
	if err != nil {
		t.Fatal(err)
	}
	return inflated
}

func testDecodePNG(t *testing.T, name string) *PNGImage {
	t.Helper()
	file, err := os.Open(testRepoPath(t, "PngSuite/"+name+".PNG"))
	if err != nil {
		t.Fatal(err)
	}
	defer file.Close()
	return NewPNGImage(file)
}

func TestPNGImageDecodesThePngSuiteImages(t *testing.T) {
	for _, row := range testPngSuite {
		png := testDecodePNG(t, row.name)
		if png.GetWidth() != row.width || png.GetHeight() != row.height {
			t.Errorf("%s: size %v x %v", row.name, png.GetWidth(), png.GetHeight())
		}
		if png.GetColorType() != row.colorType || png.GetBitDepth() != row.bitDepth {
			t.Errorf("%s: color type %d, bit depth %d", row.name, png.GetColorType(), png.GetBitDepth())
		}
		samples := testInflate(t, png.GetData())
		if len(samples) != row.samples || testCRC(samples) != row.samplesCRC {
			t.Errorf("%s: samples %d:%s", row.name, len(samples), testCRC(samples))
		}
		if row.alpha == 0 {
			if png.GetAlpha() != nil {
				t.Errorf("%s: has alpha", row.name)
			}
		} else {
			alpha := testInflate(t, png.GetAlpha())
			if len(alpha) != row.alpha || testCRC(alpha) != row.alphaCRC {
				t.Errorf("%s: alpha %d:%s", row.name, len(alpha), testCRC(alpha))
			}
		}
	}
}

func TestPNGImageTruecolorTransparencyIsIgnored(t *testing.T) {
	// tRNS applies to palette images only; TBRN2C08 has the samples of TP1N3P08.
	png := testDecodePNG(t, "TBRN2C08")
	if got := testCRC(testInflate(t, png.GetData())); got != "8b0a6c2c" {
		t.Errorf("samples %s", got)
	}
	if png.GetAlpha() != nil {
		t.Error("has alpha")
	}
}

func TestPNGImageRejects16BitRgbaWithAMessage(t *testing.T) {
	message, panicked := testPanic(func() { testDecodePNG(t, "BASN6A16") })
	if !panicked || message != "Image with unsupported bit depth == 16" {
		t.Errorf("panicked %v with %q", panicked, message)
	}
}

func TestPNGImageRejectsDataThatIsNotAPng(t *testing.T) {
	if _, panicked := testPanic(func() { NewPNGImage(strings.NewReader("not a png file")) }); !panicked {
		t.Error("did not panic")
	}
}

func TestPNGImageAnImageFromAPngHasItsSize(t *testing.T) {
	file, err := os.Open(testRepoPath(t, "PngSuite/BASN2C08.PNG"))
	if err != nil {
		t.Fatal(err)
	}
	defer file.Close()
	image := NewImage(testNewPDF(), file, imagetype.PNG)
	if image.GetWidth() != 32 || image.GetHeight() != 32 {
		t.Errorf("size %v x %v", image.GetWidth(), image.GetHeight())
	}
}

func TestPNGImageDecodesGrayscaleWithAlpha(t *testing.T) {
	png := testDecodePNG(t, "BASN4A08")
	if png.GetWidth() != 32 || png.GetHeight() != 32 || png.GetColorType() != 4 || png.GetBitDepth() != 8 {
		t.Errorf("size %v x %v, color type %d, bit depth %d",
			png.GetWidth(), png.GetHeight(), png.GetColorType(), png.GetBitDepth())
	}
	gray := testInflate(t, png.GetData())
	if len(gray) != 1024 || testCRC(gray) != "bfc7e22b" {
		t.Errorf("gray %d:%s", len(gray), testCRC(gray))
	}
	alpha := testInflate(t, png.GetAlpha())
	if len(alpha) != 1024 || testCRC(alpha) != "fa6029ad" {
		t.Errorf("alpha %d:%s", len(alpha), testCRC(alpha))
	}
}

func TestPNGImageAnImageFromAGrayscalePngWithAlphaIsGrayWithASoftMask(t *testing.T) {
	file, err := os.Open(testRepoPath(t, "PngSuite/BASN4A08.PNG"))
	if err != nil {
		t.Fatal(err)
	}
	defer file.Close()
	doc := testNewDoc()
	image := NewImage(doc.pdf, file, imagetype.PNG)
	image.DrawOn(NewPage(doc.pdf, testLetterPortrait()))
	raw := string(doc.complete())
	if !strings.Contains(raw, "DeviceGray") || !strings.Contains(raw, "/SMask") || strings.Contains(raw, "DeviceRGB") {
		t.Error("the image is not gray with a soft mask")
	}
}

func TestPNGImageRejects16BitGrayscaleWithAlphaWithAMessage(t *testing.T) {
	message, panicked := testPanic(func() { testDecodePNG(t, "BASN4A16") })
	if !panicked || message != "Image with unsupported bit depth == 16" {
		t.Errorf("panicked %v with %q", panicked, message)
	}
}

func TestPNGImageRejectsInterlacedImagesWithAClearError(t *testing.T) {
	message, panicked := testPanic(func() { testDecodePNG(t, "BASI0G08") })
	want := "Interlaced PNG images are not supported.\n" +
		"Convert the image using OptiPNG:\noptipng -i0 -o7 myimage.png"
	if !panicked || message != want {
		t.Errorf("panicked %v with %q", panicked, message)
	}
}
