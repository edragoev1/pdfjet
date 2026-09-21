// pngimage_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"encoding/binary"
	"fmt"
	"hash/crc32"
	"math"
	"os"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/internal/compressor"
	"github.com/edragoev1/pdfjet/v9/src/internal/decompressor"
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

func testDecodePNG(t *testing.T, name string) *pngImage {
	t.Helper()
	file, err := os.Open(testRepoPath(t, "PngSuite/"+name+".PNG"))
	if err != nil {
		t.Fatal(err)
	}
	defer file.Close()
	return newPNGImage(file)
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
	if _, panicked := testPanic(func() { newPNGImage(strings.NewReader("not a png file")) }); !panicked {
		t.Error("did not panic")
	}
}

func TestPNGImageAnImageFromAPngHasItsSize(t *testing.T) {
	file, err := os.Open(testRepoPath(t, "PngSuite/BASN2C08.PNG"))
	if err != nil {
		t.Fatal(err)
	}
	defer file.Close()
	image := NewImage(testNewPDF(), file)
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
	image := NewImage(doc.pdf, file)
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

// testPNG returns a PNG file with the IHDR of the size, bit depth and color
// type, a PLTE chunk when there is a palette, and one IDAT chunk when there is
// image data.
func testPNG(width, height int32, bitDepth, colorType byte, palette, idat []byte) []byte {
	return testPNGOf(width, height, bitDepth, colorType, 0, 0, palette, idat)
}

// testPNGOf returns the same file with the compression and the filter method
// of its IHDR chunk, which are 0 in every PNG file that is defined.
func testPNGOf(width, height int32, bitDepth, colorType, compression, filter byte,
	palette, idat []byte) []byte {
	var buf bytes.Buffer
	buf.Write([]byte{0x89, 'P', 'N', 'G', '\r', '\n', 0x1A, '\n'})
	ihdr := make([]byte, 13)
	binary.BigEndian.PutUint32(ihdr[0:], uint32(width))
	binary.BigEndian.PutUint32(ihdr[4:], uint32(height))
	ihdr[8] = bitDepth
	ihdr[9] = colorType
	ihdr[10] = compression
	ihdr[11] = filter
	testPNGChunk(&buf, "IHDR", ihdr)
	if palette != nil {
		testPNGChunk(&buf, "PLTE", palette)
	}
	if idat != nil {
		testPNGChunk(&buf, "IDAT", idat)
	}
	testPNGChunk(&buf, "IEND", []byte{})
	return buf.Bytes()
}

func testPNGChunk(buf *bytes.Buffer, name string, data []byte) {
	crc := crc32.NewIEEE()
	crc.Write([]byte(name))
	crc.Write(data)
	_ = binary.Write(buf, binary.BigEndian, uint32(len(data)))
	buf.WriteString(name)
	buf.Write(data)
	_ = binary.Write(buf, binary.BigEndian, crc.Sum32())
}

// testPanicMessage returns the message that the function panics with.
func testPanicMessage(fn func()) string {
	message, panicked := testPanic(fn)
	if !panicked {
		return "(no panic)"
	}
	return message
}

func testPNGError(png []byte) string {
	return testPanicMessage(func() { newPNGImage(bytes.NewReader(png)) })
}

func testWant(t *testing.T, want, got string) {
	t.Helper()
	if got != want {
		t.Errorf("want %q, got %q", want, got)
	}
}

func TestPNGImageDecodesTheRowsOfTheImageAndIgnoresDataAfterThem(t *testing.T) {
	rgb := []byte{1, 2, 3, 4, 5, 6}
	for _, rows := range [][]byte{{0, 1, 2, 3, 4, 5, 6}, {0, 1, 2, 3, 4, 5, 6, 9, 9, 9}} {
		png := newPNGImage(bytes.NewReader(testPNG(2, 1, 8, 2, nil, compressor.Deflate(rows))))
		if got := testInflate(t, png.GetData()); !bytes.Equal(got, rgb) {
			t.Errorf("samples %v", got)
		}
	}
}

func TestPNGImageRejectsImageDataShorterThanTheImage(t *testing.T) {
	testWant(t, "The PNG image data is shorter than the image.",
		testPNGError(testPNG(2, 1, 8, 2, nil, compressor.Deflate([]byte{0, 1, 2, 3}))))
}

func TestPNGImageRejectsAnImageLargerThanTheLimitBeforeDecodingIt(t *testing.T) {
	// 20000 x 20000 RGB samples are 1.2 GB.
	testWant(t, "The PNG image is larger than 268435456 bytes.",
		testPNGError(testPNG(20000, 20000, 8, 2, nil, compressor.Deflate([]byte{0}))))
	// 10000 x 10000 palette indexes of 1 bit are 12.5 MB, but 400 MB of RGB and alpha.
	testWant(t, "The PNG image is larger than 268435456 bytes.",
		testPNGError(testPNG(10000, 10000, 1, 3, make([]byte, 6), compressor.Deflate([]byte{0}))))
	// The largest size, where the sizes multiplied overflow an int64.
	const max = math.MaxInt32
	testWant(t, "The PNG image is larger than 268435456 bytes.",
		testPNGError(testPNG(max, max, 16, 6, nil, compressor.Deflate([]byte{0}))))
	testWant(t, "The PNG image is larger than 268435456 bytes.",
		testPNGError(testPNG(max, max, 1, 3, make([]byte, 6), compressor.Deflate([]byte{0}))))
	testWant(t, "The PNG image is larger than 268435456 bytes.",
		testPNGError(testPNG(max, 1, 16, 6, nil, compressor.Deflate([]byte{0}))))
}

func TestPNGImageRejectsAnInvalidSizeBitDepthColorTypeOrPalette(t *testing.T) {
	idat := compressor.Deflate([]byte{0, 0, 0, 0})
	testWant(t, "Invalid PNG image size.", testPNGError(testPNG(0, 1, 8, 2, nil, idat)))
	testWant(t, "Invalid PNG image size.", testPNGError(testPNG(-1, 1, 8, 2, nil, idat)))
	testWant(t, "Invalid PNG bit depth 4 for color type 2.", testPNGError(testPNG(1, 1, 4, 2, nil, idat)))
	testWant(t, "Invalid PNG color type 5.", testPNGError(testPNG(1, 1, 8, 5, nil, idat)))
	testWant(t, "Invalid PNG color type 200.", testPNGError(testPNG(1, 1, 8, 200, nil, idat)))
	testWant(t, "Invalid PNG bit depth 200 for color type 2.", testPNGError(testPNG(1, 1, 200, 2, nil, idat)))
	testWant(t, "The PNG palette image has no PLTE chunk.", testPNGError(testPNG(1, 1, 8, 3, nil, idat)))
	testWant(t, "The PNG image has no image data.", testPNGError(testPNG(1, 1, 8, 2, nil, nil)))
}

func TestPNGImageRejectsAnUnknownCompressionOrFilterMethod(t *testing.T) {
	// Only the deflate compression method and the adaptive filter method are
	// defined. libpng refuses a file of another one, and so does Pillow for
	// the filter method, where the rows of this one would be read as if it
	// were 0.
	idat := compressor.Deflate([]byte{0, 0})
	testWant(t, "Unknown PNG compression method.",
		testPNGError(testPNGOf(1, 1, 8, 0, 1, 0, nil, idat)))
	testWant(t, "Unknown PNG filter method.",
		testPNGError(testPNGOf(1, 1, 8, 0, 0, 1, nil, idat)))
	if _, panicked := testPanic(func() {
		newPNGImage(bytes.NewReader(testPNGOf(1, 1, 8, 0, 0, 0, nil, idat)))
	}); panicked {
		t.Error("a file of the methods that are defined is refused")
	}
}

func TestPNGImageRejectsAChunkLengthThatTheFileDoesNotHave(t *testing.T) {
	valid := testPNG(1, 1, 8, 2, nil, compressor.Deflate([]byte{0, 0, 0, 0}))
	// The IDAT chunk starts after the signature and the 25 bytes of the IHDR chunk.
	lying := append([]byte{}, valid[:33+18]...)
	lying[33], lying[34], lying[35], lying[36] = 0x7F, 0xFF, 0xFF, 0xF0
	testWant(t, "Unexpected end of the PNG stream.", testPNGError(lying))
}

func TestPNGImageReadsAStreamThatReturnsFewBytesAtATime(t *testing.T) {
	file, err := os.Open(testRepoPath(t, "PngSuite/BASN2C08.PNG"))
	if err != nil {
		t.Fatal(err)
	}
	defer file.Close()
	png := newPNGImage(testSlowReader{file})
	if got := testCRC(testInflate(t, png.GetData())); got != "7855b9bf" {
		t.Errorf("samples %s", got)
	}
}

func TestPNGImageATruecolorImageWithASuggestedPaletteIsDecodedAsTruecolor(t *testing.T) {
	png := testDecodePNG(t, "PS1N2C16")
	if png.GetColorType() != 2 {
		t.Errorf("color type %d", png.GetColorType())
	}
	if got := len(testInflate(t, png.GetData())); got != 6144 {
		t.Errorf("samples %d", got)
	}
}

func TestPNGImageAPaletteIndexPastThePaletteIsBlack(t *testing.T) {
	// A palette of 2 colors, and the indexes 1, 2 and 255.
	palette := []byte{10, 20, 30, 40, 50, 60}
	png := newPNGImage(bytes.NewReader(testPNG(3, 1, 8, 3, palette, compressor.Deflate([]byte{0, 1, 2, 255}))))
	if got := testInflate(t, png.GetData()); !bytes.Equal(got, []byte{40, 50, 60, 0, 0, 0, 0, 0, 0}) {
		t.Errorf("samples %v", got)
	}
}

func TestPNGImageRejectsAPaletteOfNoColorsOrMoreThan256(t *testing.T) {
	idat := compressor.Deflate([]byte{0, 0})
	testWant(t, "Incorrect palette length.", testPNGError(testPNG(1, 1, 8, 3, []byte{}, idat)))
	testWant(t, "Incorrect palette length.", testPNGError(testPNG(1, 1, 8, 3, make([]byte, 3*257), idat)))
	testWant(t, "Incorrect palette length.", testPNGError(testPNG(1, 1, 8, 3, make([]byte, 4), idat)))
}

// testPNGWithPhys returns a truecolor PNG with a pHYs chunk of the given
// pixels per unit and unit: 1 is the metre and 0 is a ratio of the axes with
// no size.
func testPNGWithPhys(width, height int32, x, y uint32, unit byte) []byte {
	var buf bytes.Buffer
	buf.Write([]byte{0x89, 'P', 'N', 'G', '\r', '\n', 0x1A, '\n'})
	ihdr := make([]byte, 13)
	binary.BigEndian.PutUint32(ihdr[0:], uint32(width))
	binary.BigEndian.PutUint32(ihdr[4:], uint32(height))
	ihdr[8] = 8
	ihdr[9] = 2
	testPNGChunk(&buf, "IHDR", ihdr)
	phys := make([]byte, 9)
	binary.BigEndian.PutUint32(phys[0:], x)
	binary.BigEndian.PutUint32(phys[4:], y)
	phys[8] = unit
	testPNGChunk(&buf, "pHYs", phys)
	testPNGChunk(&buf, "IDAT", compressor.Deflate(make([]byte, int(height)*(1+3*int(width)))))
	testPNGChunk(&buf, "IEND", []byte{})
	return buf.Bytes()
}

func TestPNGImageThePhysicalSizeChunkGivesTheSizeTheImageIsDrawnAt(t *testing.T) {
	// A pHYs chunk whose unit is the metre says how large the image is meant
	// to be, so it is drawn that size rather than one point for each of its
	// pixels. 11811 pixels per metre is 300 dots per inch, and 380 by 100 of
	// them are 91.2 by 24 points.
	path := testRepoPath(t, "images/rgba-8bit-chunks.png")
	file, err := os.Open(path)
	if err != nil {
		t.Fatal(err)
	}
	png := newPNGImage(file)
	file.Close()
	if png.GetWidth() != 380 || png.GetHeight() != 100 {
		t.Fatalf("pixels %v x %v", png.GetWidth(), png.GetHeight())
	}
	if math.Abs(float64(png.physicalWidth)-91.2) > 0.01 ||
		math.Abs(float64(png.physicalHeight)-24.0) > 0.01 {
		t.Errorf("physical size %v x %v", png.physicalWidth, png.physicalHeight)
	}
	file, err = os.Open(path)
	if err != nil {
		t.Fatal(err)
	}
	defer file.Close()
	image := NewImage(testNewPDF(), file)
	if math.Abs(float64(image.GetWidth())-91.2) > 0.01 ||
		math.Abs(float64(image.GetHeight())-24.0) > 0.01 {
		t.Errorf("the image is drawn %v x %v, not the size it asks for",
			image.GetWidth(), image.GetHeight())
	}
}

func TestPNGImageAnImageWithNoPhysicalSizeIsDrawnAtOnePointForEachPixel(t *testing.T) {
	// Most PNG files carry no pHYs chunk, and are drawn as they were.
	png := testDecodePNG(t, "BASN2C08")
	if png.physicalWidth != 0.0 || png.physicalHeight != 0.0 {
		t.Errorf("physical size %v x %v", png.physicalWidth, png.physicalHeight)
	}
}

func TestPNGImageAPhysicalSizeChunkThatGivesNoSizeIsPassedOver(t *testing.T) {
	// Unit 0 is the ratio of the two axes and says nothing about how large the
	// image is, and a count of zero pixels gives no size either.
	for _, c := range []struct {
		what string
		x, y uint32
		unit byte
	}{
		{"a ratio is not a size", 1, 4, 0},
		{"no pixels per metre across", 0, 4724, 1},
		{"no pixels per metre down", 4724, 0, 1},
	} {
		png := newPNGImage(bytes.NewReader(testPNGWithPhys(8, 8, c.x, c.y, c.unit)))
		if png.physicalWidth != 0.0 || png.physicalHeight != 0.0 {
			t.Errorf("%s: physical size %v x %v", c.what, png.physicalWidth, png.physicalHeight)
		}
	}
}

func TestPNGImageAnImageOfPixelsThatAreNotSquareIsDrawnWithEachAxisOfItsOwnSize(t *testing.T) {
	// The chunk gives the pixels per metre of each axis on its own, so an
	// image of 4724 across and 2362 down is twice as tall as it is wide for
	// the same count of pixels.
	png := newPNGImage(bytes.NewReader(testPNGWithPhys(20, 20, 4724, 2362, 1)))
	if math.Abs(float64(png.physicalWidth)-12.0) > 0.01 ||
		math.Abs(float64(png.physicalHeight)-24.0) > 0.01 {
		t.Errorf("physical size %v x %v", png.physicalWidth, png.physicalHeight)
	}
}
