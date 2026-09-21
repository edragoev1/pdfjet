// bmpimage_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"encoding/binary"
	"math"
	"os"
	"strings"
	"testing"
)

// Red, green on the top row; blue, white on the bottom row.
var testPixels = [2][2][3]byte{{{255, 0, 0}, {0, 255, 0}}, {{0, 0, 255}, {255, 255, 255}}}

var testRGB = []byte{255, 0, 0, 0, 255, 0, 0, 0, 255, 255, 255, 255}

func testBMP24(topDown bool) []byte {
	width, height := 2, 2
	rowSize := (width*3 + 3) &^ 3
	var buf bytes.Buffer
	le := binary.LittleEndian
	buf.WriteString("BM")
	_ = binary.Write(&buf, le, uint32(54+rowSize*height))
	_ = binary.Write(&buf, le, uint32(0))
	_ = binary.Write(&buf, le, uint32(54))
	_ = binary.Write(&buf, le, uint32(40))
	_ = binary.Write(&buf, le, int32(width))
	h := int32(height)
	if topDown {
		h = -h
	}
	_ = binary.Write(&buf, le, h)
	_ = binary.Write(&buf, le, uint16(1))
	_ = binary.Write(&buf, le, uint16(24))
	_ = binary.Write(&buf, le, []uint32{0, uint32(rowSize * height), 2835, 2835, 0, 0})
	for i := 0; i < height; i++ {
		row := testPixels[height-1-i]
		if topDown {
			row = testPixels[i]
		}
		for _, p := range row {
			buf.Write([]byte{p[2], p[1], p[0]})
		}
		for pad := width * 3; pad < rowSize; pad++ {
			buf.WriteByte(0)
		}
	}
	return buf.Bytes()
}

func TestBMPImageBottomUpRowsAreReadTopRowFirst(t *testing.T) {
	bmp := newBMPImage(bytes.NewReader(testBMP24(false)))
	if bmp.getWidth() != 2 || bmp.getHeight() != 2 {
		t.Errorf("size %v x %v", bmp.getWidth(), bmp.getHeight())
	}
	if got := testInflate(t, bmp.getData()); !bytes.Equal(got, testRGB) {
		t.Errorf("samples %v", got)
	}
}

func TestBMPImageTopDownRowsAreReadTheSame(t *testing.T) {
	if got := testInflate(t, newBMPImage(bytes.NewReader(testBMP24(true))).getData()); !bytes.Equal(got, testRGB) {
		t.Errorf("samples %v", got)
	}
}

func TestBMPImageATruncatedFileThrows(t *testing.T) {
	truncated := testBMP24(false)[:60]
	if _, panicked := testPanic(func() { newBMPImage(bytes.NewReader(truncated)) }); !panicked {
		t.Error("did not panic")
	}
}

// testBMPHeader returns the 54 byte header of a BMP file of the size, bits per
// pixel and palette colors.
func testBMPHeader(width, height int32, bitsPerPixel uint16, colors int32) []byte {
	var buf bytes.Buffer
	le := binary.LittleEndian
	buf.WriteString("BM")
	_ = binary.Write(&buf, le, []uint32{54, 0, 54, 40})
	_ = binary.Write(&buf, le, []int32{width, height})
	_ = binary.Write(&buf, le, []uint16{1, bitsPerPixel})
	_ = binary.Write(&buf, le, []uint32{0, 0, 2835, 2835})
	_ = binary.Write(&buf, le, []int32{colors, 0})
	return buf.Bytes()
}

func testBMPError(bmp []byte) string {
	return testPanicMessage(func() { newBMPImage(bytes.NewReader(bmp)) })
}

func TestBMPImageRejectsAnInvalidSize(t *testing.T) {
	testWant(t, "Invalid BMP image size.", testBMPError(testBMPHeader(0, 2, 24, 0)))
	testWant(t, "Invalid BMP image size.", testBMPError(testBMPHeader(2, 0, 24, 0)))
	testWant(t, "Invalid BMP image size.", testBMPError(testBMPHeader(-2, 2, 24, 0)))
	testWant(t, "Invalid BMP image size.", testBMPError(testBMPHeader(2, math.MinInt32, 24, 0)))
}

func TestBMPImageRejectsAnImageLargerThanTheLimitBeforeReadingIt(t *testing.T) {
	// 20000 x 20000 pixels are 1.2 GB of RGB; the file has only the header.
	testWant(t, "The BMP image is larger than 268435456 bytes.", testBMPError(testBMPHeader(20000, 20000, 24, 0)))
	// The largest size, where the sizes multiplied overflow an int64.
	const max = math.MaxInt32
	testWant(t, "The BMP image is larger than 268435456 bytes.", testBMPError(testBMPHeader(max, max, 32, 0)))
	testWant(t, "The BMP image is larger than 268435456 bytes.", testBMPError(testBMPHeader(max, 1, 32, 0)))
}

func TestBMPImageRejectsAnUnsupportedBitDepthOrALargePalette(t *testing.T) {
	testWant(t, "Can only parse 1 bit, 4bit, 8bit, 16bit, 24bit and 32bit images",
		testBMPError(testBMPHeader(2, 2, 2, 0)))
	testWant(t, "Invalid BMP palette size 2147483647.", testBMPError(testBMPHeader(2, 2, 8, 0x7FFFFFFF)))
}

// testBMP returns a BMP file of the 2 by 2 pixels with a header of the size,
// 40 bytes or more, the bits per pixel, the compression, the masks after the
// first 40 bytes of the header, the palette of 0xRRGGBB colors, and the pixels
// of the bottom row and then the top row, each padded to 4 bytes.
func testBMP(headerSize, bitsPerPixel, compression int, masks, palette []uint32, bottomRow, topRow []byte) []byte {
	afterHeader := 0
	if headerSize == 40 && masks != nil {
		afterHeader = 12
	}
	offset := 14 + headerSize + afterHeader + 4*len(palette)
	rowSize := (len(bottomRow) + 3) &^ 3
	var buf bytes.Buffer
	le := binary.LittleEndian
	buf.WriteString("BM")
	_ = binary.Write(&buf, le, []uint32{uint32(offset + 2*rowSize), 0, uint32(offset), uint32(headerSize), 2, 2})
	_ = binary.Write(&buf, le, []uint16{1, uint16(bitsPerPixel)})
	_ = binary.Write(&buf, le, []uint32{uint32(compression), uint32(2 * rowSize), 2835, 2835, uint32(len(palette)), 0})
	_ = binary.Write(&buf, le, masks)
	buf.Write(make([]byte, offset-4*len(palette)-buf.Len())) // The rest of a larger header is 0
	for _, color := range palette {
		buf.Write([]byte{byte(color), byte(color >> 8), byte(color >> 16), 0})
	}
	for _, row := range [][]byte{bottomRow, topRow} {
		buf.Write(row)
		buf.Write(make([]byte, rowSize-len(row)))
	}
	return buf.Bytes()
}

func testBMPDecode(t *testing.T, bmp []byte) []byte {
	t.Helper()
	return testInflate(t, newBMPImage(bytes.NewReader(bmp)).getData())
}

// testShorts returns the pixels as little endian 16 bit values.
func testShorts(pixels ...uint16) []byte {
	var buf bytes.Buffer
	_ = binary.Write(&buf, binary.LittleEndian, pixels)
	return buf.Bytes()
}

func TestBMPImageAThirtyTwoBitPixelIsBlueGreenRedAndAByteThatIsNotAColor(t *testing.T) {
	bmp := testBMP(40, 32, 0, nil, nil, []byte{255, 0, 0, 0, 255, 255, 255, 0}, []byte{0, 0, 255, 0, 0, 255, 0, 0})
	if got := testBMPDecode(t, bmp); !bytes.Equal(got, testRGB) {
		t.Errorf("got %v", got)
	}
}

func TestBMPImageTheMasksOfSixteenAndThirtyTwoBitPixelsAreRead(t *testing.T) {
	for name, bmp := range map[string][]byte{
		// 5 bits a color without masks; 31 of 5 bits is 255.
		"5 bits a color": testBMP(40, 16, 0, nil, nil, testShorts(0x001F, 0x7FFF), testShorts(0x7C00, 0x03E0)),
		"5, 6 and 5 bits": testBMP(40, 16, 3, []uint32{0xF800, 0x07E0, 0x001F}, nil,
			testShorts(0x001F, 0xFFFF), testShorts(0xF800, 0x07E0)),
		// Red in the low byte, in a 108 byte header.
		"red in the low byte": testBMP(108, 32, 3, []uint32{0x000000FF, 0x0000FF00, 0x00FF0000}, nil,
			[]byte{0, 0, 255, 0, 255, 255, 255, 0}, []byte{255, 0, 0, 0, 0, 255, 0, 0}),
	} {
		if got := testBMPDecode(t, bmp); !bytes.Equal(got, testRGB) {
			t.Errorf("%s: got %v", name, got)
		}
	}
}

func TestBMPImageThePaletteFollowsAHeaderOfAnySize(t *testing.T) {
	palette := []uint32{0xFF0000, 0x00FF00, 0x0000FF, 0xFFFFFF}
	for _, headerSize := range []int{40, 124} {
		if got := testBMPDecode(t, testBMP(headerSize, 8, 0, nil, palette, []byte{2, 3}, []byte{0, 1})); !bytes.Equal(got, testRGB) {
			t.Errorf("a %d byte header: got %v", headerSize, got)
		}
	}
}

func TestBMPImageRejectsACompressedImage(t *testing.T) {
	palette := []uint32{0xFF0000, 0x00FF00, 0x0000FF, 0xFFFFFF}
	// RLE8: a run of 1 pixel of color 2, 1 of color 3, the end of the bitmap
	bmp := testBMP(40, 8, 1, nil, palette, []byte{1, 2, 1, 3}, []byte{0, 1, 0, 0})
	if got := testBMPError(bmp); got != "Compressed BMP images are not supported." {
		t.Errorf("got %q", got)
	}
}

func TestBMPImageAPaletteIndexPastThePaletteIsBlack(t *testing.T) {
	// A palette of 2 colors, and the indexes 1 and 5 in the top row.
	bmp := testBMP(40, 8, 0, nil, []uint32{0x102030, 0x405060}, []byte{0, 0}, []byte{1, 5})
	want := []byte{0x40, 0x50, 0x60, 0, 0, 0, 0x10, 0x20, 0x30, 0x10, 0x20, 0x30}
	if got := testBMPDecode(t, bmp); !bytes.Equal(got, want) {
		t.Errorf("pixels %v", got)
	}
}

func TestBMPImageTheLastRowCanBeWithoutItsPadding(t *testing.T) {
	bmp := testBMP(40, 24, 0, nil, nil, []byte{1, 2, 3, 4, 5, 6}, []byte{7, 8, 9, 10, 11, 12})
	want := testBMPDecode(t, bmp)
	// The 2 bytes of padding of the top row, which is the last one.
	if got := testBMPDecode(t, bmp[:len(bmp)-2]); !bytes.Equal(got, want) {
		t.Errorf("pixels %v, not %v", got, want)
	}
	testWant(t, "Unexpected end of stream: expected 6 bytes", testBMPError(bmp[:len(bmp)-3]))
}

func TestBMPImageThePixelsPerMeterOfTheHeaderGiveTheSizeTheImageIsDrawnAt(t *testing.T) {
	// The header of a BMP holds the pixels per metre of each axis.
	// palette.bmp is 100 by 100 pixels at 4724 per metre, which is 120 dots
	// per inch and 60 by 60 points.
	path := testRepoPath(t, "images/palette.bmp")
	data, err := os.ReadFile(path)
	if err != nil {
		t.Fatal(err)
	}
	bmp := newBMPImage(bytes.NewReader(data))
	if bmp.getWidth() != 100 || bmp.getHeight() != 100 {
		t.Fatalf("pixels %v x %v", bmp.getWidth(), bmp.getHeight())
	}
	if math.Abs(float64(bmp.physicalWidth)-60.0) > 0.05 ||
		math.Abs(float64(bmp.physicalHeight)-60.0) > 0.05 {
		t.Errorf("physical size %v x %v", bmp.physicalWidth, bmp.physicalHeight)
	}
	file, err := os.Open(path)
	if err != nil {
		t.Fatal(err)
	}
	defer file.Close()
	image := NewImage(testNewPDF(), file)
	if math.Abs(float64(image.GetWidth())-60.0) > 0.05 ||
		math.Abs(float64(image.GetHeight())-60.0) > 0.05 {
		t.Errorf("the image is drawn %v x %v, not the size it asks for",
			image.GetWidth(), image.GetHeight())
	}
}

func TestBMPImageAHeaderWithNoPixelsPerMeterLeavesTheSizeOfThePixels(t *testing.T) {
	// Most writers leave the two fields at 0, which says nothing about how
	// large the image is, so it keeps one point for each of its pixels. The
	// header is built here because the files of the repository both carry a
	// resolution.
	data, err := os.ReadFile(testRepoPath(t, "images/palette.bmp"))
	if err != nil {
		t.Fatal(err)
	}
	for i := 38; i < 46; i++ {
		data[i] = 0
	}
	bmp := newBMPImage(bytes.NewReader(data))
	if bmp.physicalWidth != 0.0 || bmp.physicalHeight != 0.0 {
		t.Errorf("physical size %v x %v", bmp.physicalWidth, bmp.physicalHeight)
	}
}

func TestBMPImageRejectsAnOS2HeaderForItsSize(t *testing.T) {
	// The 12 byte header of OS/2 1.x has a width and a height of 2 bytes, so
	// the bit depth is not where a header of 40 bytes has it: the message
	// says that the header is not supported, and not that the bit depth is
	// not. Here the bit depth is 8, with a palette of 3 bytes a color.
	var buf bytes.Buffer
	le := binary.LittleEndian
	buf.WriteString("BM")
	_ = binary.Write(&buf, le, []uint32{26 + 6 + 4, 0, 26 + 6, 12})
	_ = binary.Write(&buf, le, []uint16{2, 2, 1, 8})
	buf.Write([]byte{0, 0, 0, 255, 255, 255})
	buf.Write([]byte{0, 1, 0, 0, 1, 0, 0, 0})
	testWant(t, "Unsupported BMP header of 12 bytes.", testBMPError(buf.Bytes()))
}

// The masks of 32 bit pixels of blue, green, red and alpha bytes.
var testMasksBGRA = []uint32{0x00FF0000, 0x0000FF00, 0x000000FF, 0xFF000000}

func TestBMPImageTheAlphaMaskOfAHeaderOf56BytesOrMoreIsRead(t *testing.T) {
	for name, bmp := range map[string][]byte{
		// Blue and white of alpha 0x80 and 0xFF, red and green of 0 and 0x40.
		"32 bits in a 124 byte header": testBMP(124, 32, 3, testMasksBGRA, nil,
			[]byte{255, 0, 0, 0x80, 255, 255, 255, 0xFF}, []byte{0, 0, 255, 0, 0, 255, 0, 0x40}),
		"32 bits in a 56 byte header": testBMP(56, 32, 3, testMasksBGRA, nil,
			[]byte{255, 0, 0, 0x80, 255, 255, 255, 0xFF}, []byte{0, 0, 255, 0, 0, 255, 0, 0x40}),
		// 4 bits of alpha, and of each color: 5 of 15 is 85.
		"4 bits a color and 4 of alpha": testBMP(108, 16, 3, []uint32{0x0F00, 0x00F0, 0x000F, 0xF000}, nil,
			testShorts(0x800F, 0xFFFF), testShorts(0x0F00, 0x40F0)),
	} {
		image := newBMPImage(bytes.NewReader(bmp))
		if got := testInflate(t, image.getData()); !bytes.Equal(got, testRGB) {
			t.Errorf("%s: samples %v", name, got)
		}
		want := []byte{0, 0x40, 0x80, 0xFF}
		if name == "4 bits a color and 4 of alpha" {
			want = []byte{0, 68, 136, 255}
		}
		if image.getAlpha() == nil {
			t.Errorf("%s: no alpha", name)
		} else if got := testInflate(t, image.getAlpha()); !bytes.Equal(got, want) {
			t.Errorf("%s: alpha %v", name, got)
		}
	}
}

func TestBMPImageHasNoAlphaWithoutAnAlphaMaskOrWhenEveryPixelHasAnAlphaOf0(t *testing.T) {
	top, bottom := []byte{255, 0, 0, 0x12, 255, 255, 255, 0x34}, []byte{0, 0, 255, 0x56, 0, 255, 0, 0x78}
	for name, bmp := range map[string][]byte{
		// The fourth byte of a pixel without masks is not a color, and the
		// masks of a BI_RGB image, in a larger header, are not read.
		"no masks":            testBMP(40, 32, 0, nil, nil, top, bottom),
		"the masks of BI_RGB": testBMP(124, 32, 0, testMasksBGRA, nil, top, bottom),
		"the masks after 40":  testBMP(40, 32, 3, testMasksBGRA[:3], nil, top, bottom),
		"the 52 byte header":  testBMP(52, 32, 3, testMasksBGRA[:3], nil, top, bottom),
		"an alpha mask of 0":  testBMP(124, 32, 3, []uint32{0xFF0000, 0xFF00, 0xFF, 0}, nil, top, bottom),
		// Browsers draw an image whose alpha is 0 in every pixel opaque:
		// writers that do not know of the alpha leave it at 0.
		"alpha 0 in every pixel": testBMP(124, 32, 3, testMasksBGRA, nil,
			[]byte{255, 0, 0, 0, 255, 255, 255, 0}, []byte{0, 0, 255, 0, 0, 255, 0, 0}),
	} {
		image := newBMPImage(bytes.NewReader(bmp))
		if got := testInflate(t, image.getData()); !bytes.Equal(got, testRGB) {
			t.Errorf("%s: samples %v", name, got)
		}
		if image.getAlpha() != nil {
			t.Errorf("%s: alpha %v", name, testInflate(t, image.getAlpha()))
		}
	}
}

func TestBMPImageTheAlphaIsTheSoftMaskOfTheImage(t *testing.T) {
	for _, alpha := range []byte{0x80, 0} {
		bmp := testBMP(124, 32, 3, testMasksBGRA, nil,
			[]byte{255, 0, 0, alpha, 255, 255, 255, 0}, []byte{0, 0, 255, 0, 0, 255, 0, 0})
		doc := testNewDoc()
		image := NewImage(doc.pdf, bytes.NewReader(bmp))
		image.DrawOn(NewPage(doc.pdf, testLetterPortrait()))
		raw := string(doc.complete())
		if strings.Contains(raw, "/SMask") != (alpha != 0) {
			t.Errorf("alpha %d: a soft mask is %v", alpha, strings.Contains(raw, "/SMask"))
		}
	}
}
