// imagesize_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"bytes"
	"encoding/binary"
	"io"
	"io/fs"
	"os"
	"path/filepath"
	"strings"
	"testing"
)

// testImageSizeAgrees checks the size read from the header against NewImage:
// an image NewImage takes has the size it draws it at, and an image whose
// header is refused NewImage refuses too. It returns whether NewImage took it.
func testImageSizeAgrees(t *testing.T, name string, data []byte) bool {
	t.Helper()
	size, err := ReadImageSize(bytes.NewReader(data))
	var image *Image
	refused := func() (refused any) {
		defer func() { refused = recover() }()
		image = NewImage(NewPDF(bufio.NewWriter(io.Discard)), bytes.NewReader(data))
		return nil
	}()
	switch {
	case refused == nil && err != nil:
		t.Errorf("%s: NewImage takes it, ReadImageSize refuses it: %v", name, err)
	case refused == nil:
		if size.GetWidth() != image.GetWidth() || size.GetHeight() != image.GetHeight() ||
			size.GetPixelWidth() != image.pixelWidth || size.GetPixelHeight() != image.pixelHeight {
			t.Errorf("%s: %v by %v, %d by %d pixels; NewImage %v by %v, %d by %d", name,
				size.GetWidth(), size.GetHeight(), size.GetPixelWidth(), size.GetPixelHeight(),
				image.GetWidth(), image.GetHeight(), image.pixelWidth, image.pixelHeight)
		}
	}
	return refused == nil
}

func TestImageSizeIsTheSizeNewImageDrawsAt(t *testing.T) {
	// The images of the examples, of the tests, and the 1,440 of the
	// references when they are fetched (tests/references/images)
	var paths []string
	for _, dir := range []string{"images", "tests/data", ".images"} {
		root := testRepoPath(t, dir)
		_ = filepath.WalkDir(root, func(path string, d fs.DirEntry, err error) error {
			if err == nil && !d.IsDir() {
				switch strings.ToLower(filepath.Ext(path)) {
				case ".png", ".jpg", ".jpeg", ".bmp":
					paths = append(paths, path)
				}
			}
			return nil
		})
	}
	taken := 0
	for _, path := range paths {
		data, err := os.ReadFile(path)
		if err != nil {
			t.Fatal(err)
		}
		if testImageSizeAgrees(t, path, data) {
			taken++
		}
	}
	if taken < 20 {
		t.Errorf("%d images taken, of %d", taken, len(paths))
	}
	t.Logf("%d images, %d taken by NewImage", len(paths), taken)
}

func TestImageSizeRefusesEarly(t *testing.T) {
	// A PNG of 100,000 by 100,000 pixels, refused from its header, before
	// its image data, which this one does not even have
	ihdr := []byte{0, 1, 0x86, 0xA0, 0, 1, 0x86, 0xA0, 8, 6, 0, 0, 0}
	var png bytes.Buffer
	png.WriteString("\x89PNG\r\n\x1a\n")
	testPNGChunk(&png, "IHDR", ihdr)
	testPNGChunk(&png, "IEND", nil)
	if _, err := ReadImageSize(bytes.NewReader(png.Bytes())); err == nil || !strings.Contains(err.Error(), "larger than") {
		t.Errorf("a PNG of 100,000 by 100,000: %v", err)
	}
	// Not an image at all
	if _, err := ReadImageSize(strings.NewReader("GIF89a")); err == nil {
		t.Error("a GIF taken")
	}
	// A JPEG turned a quarter of the way is its height by its width
	for _, orientation := range []string{"1", "3", "6", "8"} {
		data := testOrientationJPEG(t, orientation)
		testImageSizeAgrees(t, "orientation "+orientation, data)
		// Seen as 32 by 16, whatever it is stored as
		if size, err := ReadImageSize(bytes.NewReader(data)); err != nil || size.GetWidth() != 32 || size.GetHeight() != 16 {
			t.Errorf("orientation %s: %v, %v", orientation, size, err)
		}
	}
}

// FuzzReadImageSize checks that the header of any bytes is read or refused
// without a panic, and as NewImage reads or refuses them.
//
//	go test -run '^$' -fuzz FuzzReadImageSize -fuzztime 60s .
func FuzzReadImageSize(f *testing.F) {
	for _, name := range []string{"images/ee-map.png", "images/gr-map.jpg", "images/cmyk.jpg", "images/610-30x30.jpg"} {
		if data, err := os.ReadFile("../" + name); err == nil {
			f.Add(data)
		}
	}
	f.Fuzz(func(t *testing.T, data []byte) {
		testImageSizeAgrees(t, "fuzzed", data)
	})
}

func TestImageSizeRefusesWhatNewImageRefusesInTheHeader(t *testing.T) {
	// The review of 9 October 2026: an 8-bit BMP of 1,000 colors and a PNG
	// whose one IDAT is empty were sizes, and NewImage refused them.
	bmp := make([]byte, 54)
	copy(bmp, "BM")
	le32 := func(at, v int) { binary.LittleEndian.PutUint32(bmp[at:], uint32(v)) }
	le32(10, 54)
	le32(14, 40)
	le32(18, 1)
	le32(22, 1)
	binary.LittleEndian.PutUint16(bmp[26:], 1)
	binary.LittleEndian.PutUint16(bmp[28:], 8)
	le32(46, 1000)
	if _, err := ReadImageSize(bytes.NewReader(bmp)); err == nil || !strings.Contains(err.Error(), "palette") {
		t.Errorf("a BMP of 1,000 colors: %v", err)
	}

	var png bytes.Buffer
	png.WriteString("\x89PNG\r\n\x1a\n")
	testPNGChunk(&png, "IHDR", []byte{0, 0, 0, 1, 0, 0, 0, 1, 8, 0, 0, 0, 0})
	testPNGChunk(&png, "IDAT", nil)
	testPNGChunk(&png, "IEND", nil)
	if _, err := ReadImageSize(bytes.NewReader(png.Bytes())); err == nil {
		t.Error("a PNG of no image data taken")
	}

	// A JPEG that ends inside its frame header, after the size, is a size,
	// as in the other ports: its data is NewImage's to refuse.
	jpg := []byte{0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x11, 0x08, 0x00, 0x10, 0x00, 0x20, 0x03}
	if size, err := ReadImageSize(bytes.NewReader(jpg)); err != nil || size.GetPixelWidth() != 32 || size.GetPixelHeight() != 16 {
		t.Errorf("a JPEG cut in its frame header: %v, %v", size, err)
	}
}
