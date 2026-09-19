// pngimage_fuzz_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"bytes"
	"compress/zlib"
	"encoding/binary"
	"fmt"
	"hash/crc32"
	goimage "image"
	"image/color"
	"image/png"
	"io"
	"os"
	"path/filepath"
	"runtime"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/internal/decompressor"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// The fuzz targets of the PNG images. Any input either makes an image that is
// drawn and written, or panics with a message; see fuzzRun. The seeds are the
// images of PngSuite; go test runs them, and
//
//	go test ./src -run '^$' -fuzz '^FuzzPNGImagePixels$' -fuzztime 5m -fuzzminimizetime 5s
//
// fuzzes. An input that fails is kept in testdata/fuzz, and go test runs it
// from then on.

// fuzzPNGSeeds returns the images of PngSuite.
func fuzzPNGSeeds(f *testing.F) [][]byte {
	paths, err := filepath.Glob("../PngSuite/*.[Pp][Nn][Gg]")
	if err != nil || len(paths) == 0 {
		f.Fatal("no PngSuite images")
	}
	seeds := make([][]byte, 0, len(paths))
	for _, path := range paths {
		png, err := os.ReadFile(path)
		if err != nil {
			f.Fatal(err)
		}
		seeds = append(seeds, png)
	}
	return seeds
}

// fuzzPNG makes an image of the PNG in both ways, and draws it. When Go's
// image/png decodes the PNG too, the samples must be the ones it decodes.
func fuzzPNG(t *testing.T, pngData []byte) {
	fuzzRun(t, len(pngData), func() {
		if err := fuzzComparePNG(pngData); err != nil {
			t.Fatal(err)
		}
		objects := make([]*PDFobj, 0)
		NewImageForObjects(&objects, bytes.NewReader(pngData))

		pdf := NewPDF(bufio.NewWriter(io.Discard))
		image := NewImage(pdf, bytes.NewReader(pngData))
		image.SetLocation(10, 10)
		image.DrawOn(NewPage(pdf, letter.Portrait()))
		if err := pdf.Complete(); err != nil {
			t.Fatal(err)
		}
	})
}

// FuzzPNGImage fuzzes whole PNG files. The CRC of each chunk is made right
// first, or almost every change would fail the CRC check.
func FuzzPNGImage(f *testing.F) {
	for _, seed := range fuzzPNGSeeds(f) {
		f.Add(seed)
	}
	f.Fuzz(func(t *testing.T, data []byte) {
		fuzzPNG(t, fuzzFixPNGCRCs(data))
	})
}

// FuzzPNGImagePixels fuzzes the header, the palette, the alpha of the palette
// and the rows of a PNG image, which the target compresses into its IDAT
// chunk, so that the fuzzer changes what the image data decodes to.
func FuzzPNGImagePixels(f *testing.F) {
	for _, seed := range fuzzPNGSeeds(f) {
		if header, palette, alpha, rows, ok := fuzzSplitPNG(seed); ok {
			f.Add(header, palette, alpha, rows)
		}
	}
	f.Fuzz(func(t *testing.T, header, palette, alpha, rows []byte) {
		fuzzPNG(t, fuzzJoinPNG(header, palette, alpha, rows))
	})
}

// fuzzFixPNGCRCs returns the PNG with the CRC of each whole chunk after the
// signature made right.
func fuzzFixPNGCRCs(data []byte) []byte {
	png := append([]byte(nil), data...)
	pos := 8
	for pos+12 <= len(png) {
		length := int(binary.BigEndian.Uint32(png[pos:]))
		if length < 0 || length > len(png)-pos-12 {
			break
		}
		crc := crc32.ChecksumIEEE(png[pos+4 : pos+8+length])
		binary.BigEndian.PutUint32(png[pos+8+length:], crc)
		pos += 12 + length
	}
	return png
}

// fuzzSplitPNG returns the IHDR data, the PLTE and tRNS data, which are nil
// when the image has none, and the rows the IDAT data decodes to, of a PNG
// that has them all and is not interlaced.
func fuzzSplitPNG(data []byte) (header, palette, alpha, rows []byte, ok bool) {
	var idat []byte
	pos := 8
	for pos+12 <= len(data) {
		length := int(binary.BigEndian.Uint32(data[pos:]))
		if length > len(data)-pos-12 {
			return nil, nil, nil, nil, false
		}
		chunk := data[pos+8 : pos+8+length]
		switch string(data[pos+4 : pos+8]) {
		case "IHDR":
			header = chunk
		case "PLTE":
			palette = chunk
		case "tRNS":
			alpha = chunk
		case "IDAT":
			idat = append(idat, chunk...)
		}
		pos += 12 + length
	}
	if len(header) != 13 || header[12] != 0 || idat == nil {
		return nil, nil, nil, nil, false
	}
	r, err := zlib.NewReader(bytes.NewReader(idat))
	if err != nil {
		return nil, nil, nil, nil, false
	}
	rows, err = io.ReadAll(r)
	return header, palette, alpha, rows, err == nil
}

// fuzzJoinPNG returns the PNG of the parts that fuzzSplitPNG returns.
func fuzzJoinPNG(header, palette, alpha, rows []byte) []byte {
	var buf bytes.Buffer
	buf.Write([]byte{0x89, 'P', 'N', 'G', '\r', '\n', 0x1A, '\n'})
	chunk := func(chunkType string, data []byte) {
		_ = binary.Write(&buf, binary.BigEndian, uint32(len(data)))
		typeAndData := append([]byte(chunkType), data...)
		buf.Write(typeAndData)
		_ = binary.Write(&buf, binary.BigEndian, crc32.ChecksumIEEE(typeAndData))
	}
	chunk("IHDR", header)
	if palette != nil {
		chunk("PLTE", palette)
	}
	if alpha != nil {
		chunk("tRNS", alpha)
	}
	chunk("IDAT", fuzzCompress(rows))
	chunk("IEND", nil)
	return buf.Bytes()
}

// fuzzComparePNG returns an error when PDFjet and Go's image/png both decode
// the PNG, and a sample or an alpha value of PDFjet's is not the one of
// image/png. The samples are compared unpacked, so the bits that pad a row of
// a bit depth below 8 do not count.
func fuzzComparePNG(data []byte) error {
	var pdfjet *pngImage
	func() {
		// A PNG that PDFjet rejects is not compared, and fuzzRun sees a
		// runtime error.
		defer func() {
			if r := recover(); r != nil {
				if _, ok := r.(runtime.Error); ok {
					panic(r)
				}
			}
		}()
		pdfjet = newPNGImage(bytes.NewReader(data))
	}()
	if pdfjet == nil {
		return nil
	}
	decoded, err := png.Decode(bytes.NewReader(data))
	if err != nil {
		return nil
	}
	samples, err := decompressor.Inflate(pdfjet.GetData())
	if err != nil {
		return err
	}
	var alpha []byte
	if pdfjet.GetAlpha() != nil {
		if alpha, err = decompressor.Inflate(pdfjet.GetAlpha()); err != nil {
			return err
		}
	}

	w, h := pdfjet.w, pdfjet.h
	depth := pdfjet.bitDepth
	channels := 3
	switch pdfjet.colorType {
	case 0, 4:
		channels = 1
	case 3:
		depth = 8
	}
	if pdfjet.colorType == 4 || pdfjet.colorType == 6 {
		depth = 8
	}
	rowBytes := (w*channels*depth + 7) / 8
	if len(samples) != rowBytes*h {
		return fmt.Errorf("%d bytes of samples, not %d", len(samples), rowBytes*h)
	}
	hasAlpha := pdfjet.colorType == 4 || pdfjet.colorType == 6 ||
		(pdfjet.colorType == 3 && pdfjet.tRNS != nil)
	if hasAlpha != (alpha != nil) || (alpha != nil && len(alpha) != w*h) {
		return fmt.Errorf("alpha of %d bytes for color type %d", len(alpha), pdfjet.colorType)
	}
	bounds := decoded.Bounds()
	for y := 0; y < h; y++ {
		for x := 0; x < w; x++ {
			c := fuzzNRGBA64(decoded, bounds.Min.X+x, bounds.Min.Y+y)
			want := []uint16{c.R, c.G, c.B}
			for i := 0; i < channels; i++ {
				bit := (x*channels + i) * depth
				got := fuzzSample(samples[y*rowBytes:], bit, depth)
				if expected := int(want[i] >> (16 - depth)); got != expected {
					return fmt.Errorf("color type %d, bit depth %d: sample %d of pixel %d, %d is %d, not %d",
						pdfjet.colorType, pdfjet.bitDepth, i, x, y, got, expected)
				}
			}
			if alpha != nil {
				if got, expected := alpha[y*w+x], byte(c.A>>8); got != expected {
					return fmt.Errorf("color type %d: alpha of pixel %d, %d is %d, not %d",
						pdfjet.colorType, x, y, got, expected)
				}
			}
		}
	}
	return nil
}

// fuzzNRGBA64 returns the color of the pixel as image/png decoded it, without
// the alpha multiplied in, as PDFjet keeps the alpha apart: the conversions of
// the color package go through the premultiplied colors, which lose the color
// of a pixel that is not opaque.
func fuzzNRGBA64(img goimage.Image, x, y int) color.NRGBA64 {
	c := img.At(x, y)
	if p, ok := img.(*goimage.Paletted); ok {
		c = p.Palette[p.ColorIndexAt(x, y)]
	}
	switch c := c.(type) {
	case color.NRGBA:
		return color.NRGBA64{uint16(c.R) * 0x101, uint16(c.G) * 0x101, uint16(c.B) * 0x101, uint16(c.A) * 0x101}
	case color.NRGBA64:
		return c
	}
	return color.NRGBA64Model.Convert(c).(color.NRGBA64)
}

// fuzzSample returns the sample of the bit depth at the bit of the row.
func fuzzSample(row []byte, bit, depth int) int {
	switch depth {
	case 16:
		return int(row[bit/8])<<8 | int(row[bit/8+1])
	case 8:
		return int(row[bit/8])
	}
	return int(row[bit/8]>>(8-depth-bit%8)) & (1<<depth - 1)
}
