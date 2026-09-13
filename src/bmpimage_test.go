// bmpimage_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"encoding/binary"
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
	if bmp.GetWidth() != 2 || bmp.GetHeight() != 2 {
		t.Errorf("size %v x %v", bmp.GetWidth(), bmp.GetHeight())
	}
	if got := testInflate(t, bmp.GetData()); !bytes.Equal(got, testRGB) {
		t.Errorf("samples %v", got)
	}
}

func TestBMPImageTopDownRowsAreReadTheSame(t *testing.T) {
	if got := testInflate(t, newBMPImage(bytes.NewReader(testBMP24(true))).GetData()); !bytes.Equal(got, testRGB) {
		t.Errorf("samples %v", got)
	}
}

func TestBMPImageATruncatedFileThrows(t *testing.T) {
	truncated := testBMP24(false)[:60]
	if _, panicked := testPanic(func() { newBMPImage(bytes.NewReader(truncated)) }); !panicked {
		t.Error("did not panic")
	}
}
