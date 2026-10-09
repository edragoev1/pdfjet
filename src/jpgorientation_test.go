// jpgorientation_test.go
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

// A JPEG with an Exif orientation is drawn as it is meant to be seen:
// tests/data/jpeg/orientation-N.jpg is stored turned so that it is seen as
// 32 by 16 pixels, as orientation-1.jpg is; 6 and 8 are 16 by 32 as stored.

func testOrientationJPEG(t *testing.T, orientation string) []byte {
	t.Helper()
	data, err := os.ReadFile(testRepoPath(t, "tests/data/jpeg/orientation-"+orientation+".jpg"))
	if err != nil {
		t.Fatal(err)
	}
	return data
}

// testDrawnContent returns the content stream of a page the JPEG is drawn on,
// and the image.
func testDrawnContent(jpeg []byte) (string, *Image) {
	pdf := testNewPDF()
	page := NewPage(pdf, letter.Portrait())
	image := NewImage(pdf, bytes.NewReader(jpeg))
	image.SetLocation(10, 20)
	image.DrawOn(page)
	return testContent(page), image
}

func TestJPGOrientationTheSizeAndTheMatrixAreAsSeen(t *testing.T) {
	for _, test := range []struct {
		orientation   string
		width, height int // As stored
		matrix        string
	}{
		{"1", 32, 16, ""},
		{"3", 32, 16, "-1 0 0 -1 1 1 cm\n"},
		{"6", 16, 32, "0 -1 1 0 0 1 cm\n"},
		{"8", 16, 32, "0 1 -1 0 1 0 cm\n"},
	} {
		content, image := testDrawnContent(testOrientationJPEG(t, test.orientation))
		if image.GetWidth() != 32 || image.GetHeight() != 16 {
			t.Errorf("orientation %s: seen as %g by %g", test.orientation,
				image.GetWidth(), image.GetHeight())
		}
		if image.pixelWidth != test.width || image.pixelHeight != test.height {
			t.Errorf("orientation %s: the image object is %d by %d", test.orientation,
				image.pixelWidth, image.pixelHeight)
		}
		box := "32 0 0 16 10 756 cm\n" + test.matrix + "/Im"
		if !strings.Contains(content, box) {
			t.Errorf("orientation %s: no %q in\n%s", test.orientation, box, content)
		}
	}
}

// Orientation 1, upright as stored, draws the page as a JPEG without Exif.
func TestJPGOrientationOneIsDrawnAsWithoutExif(t *testing.T) {
	data := testOrientationJPEG(t, "1")
	withExif, _ := testDrawnContent(data)
	withoutExif, _ := testDrawnContent(testWithoutSegment(data, 0xE1))
	if withExif != withoutExif {
		t.Errorf("orientation 1:\n%s\nwithout Exif:\n%s", withExif, withoutExif)
	}
}

// testWithoutSegment returns the JPEG without its first segment of the marker.
func testWithoutSegment(jpeg []byte, marker byte) []byte {
	for i := 2; i+3 < len(jpeg); i++ {
		if jpeg[i] == 0xFF && jpeg[i+1] == marker {
			end := i + 2 + (int(jpeg[i+2])<<8 | int(jpeg[i+3]))
			return append(append([]byte(nil), jpeg[:i]...), jpeg[end:]...)
		}
	}
	return jpeg
}

// testExif returns an Exif segment, without its marker and length, with one
// entry in IFD0, in either byte order.
func testExif(bigEndian bool, tag, kind, count, value int) []byte {
	u16 := func(v int) []byte {
		if bigEndian {
			return []byte{byte(v >> 8), byte(v)}
		}
		return []byte{byte(v), byte(v >> 8)}
	}
	u32 := func(v int) []byte {
		if bigEndian {
			return append(u16(v>>16), u16(v)...)
		}
		return append(u16(v), u16(v>>16)...)
	}
	segment := []byte("Exif\x00\x00")
	if bigEndian {
		segment = append(segment, "MM"...)
	} else {
		segment = append(segment, "II"...)
	}
	segment = append(segment, u16(42)...)
	segment = append(segment, u32(8)...)
	segment = append(segment, u16(1)...)
	segment = append(segment, u16(tag)...)
	segment = append(segment, u16(kind)...)
	segment = append(segment, u32(count)...)
	segment = append(segment, u16(value)...)
	segment = append(segment, 0, 0)
	return append(segment, 0, 0, 0, 0)
}

func TestJPGOrientationIsReadInBothByteOrders(t *testing.T) {
	for _, bigEndian := range []bool{false, true} {
		for value := 1; value <= 8; value++ {
			if got := exifOrientation(testExif(bigEndian, 0x0112, 3, 1, value)); got != uint8(value) {
				t.Errorf("big endian %v, orientation %d: %d", bigEndian, value, got)
			}
		}
	}
}

// A malformed Exif segment is passed over, never read out of its bounds.
func TestJPGOrientationOfAMalformedExifIsPassedOver(t *testing.T) {
	for name, segment := range map[string][]byte{
		"orientation 0":        testExif(false, 0x0112, 3, 1, 0),
		"orientation 9":        testExif(true, 0x0112, 3, 1, 9),
		"a LONG":               testExif(false, 0x0112, 4, 1, 6),
		"a count of 2":         testExif(true, 0x0112, 3, 2, 6),
		"another tag":          testExif(false, 0x0110, 3, 1, 6),
		"not Exif":             append([]byte("Exiv"), testExif(false, 0x0112, 3, 1, 6)[4:]...),
		"another byte order":   append([]byte("Exif\x00\x00IM"), testExif(false, 0x0112, 3, 1, 6)[8:]...),
		"an IFD0 before 8":     append(testExif(false, 0x0112, 3, 1, 6)[:10], 4, 0, 0, 0),
		"an IFD0 past the end": append(testExif(false, 0x0112, 3, 1, 6)[:10], 0xFF, 0xFF, 0xFF, 0x7F),
	} {
		if got := exifOrientation(segment); got != 0 {
			t.Errorf("%s: %d", name, got)
		}
	}
	whole := testExif(true, 0x0112, 3, 1, 6)
	for end := 0; end < len(whole)-4; end++ {
		if got := exifOrientation(whole[:end]); got != 0 {
			t.Errorf("cut at %d: %d", end, got)
		}
	}
	// The JPEG with a malformed Exif is drawn as stored
	data := testOrientationJPEG(t, "6")
	broken := bytes.Replace(data, []byte("MM\x00\x2A"), []byte("MM\x00\x2B"), 1)
	if _, image := testDrawnContent(broken); image.GetWidth() != 16 || image.GetHeight() != 32 {
		t.Errorf("a malformed Exif: %g by %g", image.GetWidth(), image.GetHeight())
	}
}
