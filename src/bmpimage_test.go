// bmpimage_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"encoding/binary"
	"math"
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
