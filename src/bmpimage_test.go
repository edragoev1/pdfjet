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
