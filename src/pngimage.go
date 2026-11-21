// pngimage.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// Floating point: every float multiplication in this file is wrapped in
// float32(...) or float64(...) on purpose, so that Go's arm64 compiler does
// not fuse it with an addition and round otherwise than amd64 and the other
// ports. Keep the wrapping; see "Floating point on ARM" in README.md.
// check-no-fma.sh fails if one is removed.

import (
	"bytes"
	"fmt"
	"hash/crc32"
	"io"
	"math"

	"github.com/edragoev1/pdfjet/v9/src/internal/compressor"
	"github.com/edragoev1/pdfjet/v9/src/internal/decompressor"
	"github.com/edragoev1/pdfjet/v9/src/internal/device"
	"github.com/edragoev1/pdfjet/v9/src/internal/fastfloat"
)

// pngImage is used to embed PNG images in the PDF document.
//
// Please note: interlaced images are not supported.
// To convert an interlaced image to a non-interlaced image, use OptiPNG:
//
//	optipng -i0 -o7 myimage.png
type pngImage struct {
	w int // Image width in pixels
	h int // Image height in pixels

	iDAT []byte // The compressed data in the IDAT chunk
	pLTE []byte // The palette data
	tRNS []byte // The alpha for the palette data
	// The transparent color the tRNS chunk of a grayscale or truecolor image
	// names, as the ranges of a /Mask: the minimum and the maximum of each
	// component, both the value of the chunk, in the bits of the samples.
	colorKeyMask []int

	deflatedImageData []byte // The deflated image data
	deflatedAlphaData []byte // The deflated alpha channel data

	// The stream of the image object: the IDAT data as it is, whose PNG
	// filters the /DecodeParms of the image undo, or the deflated image data.
	stream []byte
	// The colors of a pixel of the IDAT data that the stream is, for the
	// /DecodeParms, or 0 when the stream is the deflated image data.
	decodeColors int
	// The colors of the /Indexed color space of a palette image whose stream
	// is its IDAT data, one for each index the bit depth has, the ones past
	// the palette black; nil for an image of RGB samples.
	palette []byte

	bitDepth  int
	colorType int

	// The size the pHYs chunk gives the image, in points, or 0 when it gives
	// none: the chunk holds the pixels per unit of each axis, and only unit 1,
	// the metre, is a physical size. Unit 0 is the ratio of the two axes with
	// no size to it, so the image keeps the size of its pixels.
	physicalWidth  float32
	physicalHeight float32
}

// pointsPerMeter is the points a metre is: 72/0.0254.
const pointsPerMeter = 72.0 / 0.0254

// readPhysicalSize reads the size the image asks to be drawn at from the pHYs
// chunk: the pixels per unit of each axis and the unit they are in. A chunk of
// another length, another unit or no pixels at all gives no size, and the image
// keeps the size of its pixels; so does one whose size is too large for a PDF
// number.
func (image *pngImage) readPhysicalSize(data []byte) {
	if len(data) != 9 || data[8] != 1 {
		return
	}
	pixelsPerMeterX := toUint32(data, 0)
	pixelsPerMeterY := toUint32(data, 4)
	// The PNG specification limits the pixels per unit to 2^31 - 1, as the
	// four byte numbers of all of its chunks.
	if pixelsPerMeterX == 0 || pixelsPerMeterY == 0 ||
		pixelsPerMeterX > math.MaxInt32 || pixelsPerMeterY > math.MaxInt32 {
		return
	}
	width := float32(float64(float64(image.w)*pointsPerMeter) / float64(pixelsPerMeterX))
	height := float32(float64(float64(image.h)*pointsPerMeter) / float64(pixelsPerMeterY))
	if fastfloat.IsWritable(width) && fastfloat.IsWritable(height) {
		image.physicalWidth = width
		image.physicalHeight = height
	}
}

// newPNGImage is used to embed PNG images in a PDF document.
func newPNGImage(reader io.Reader) *pngImage {
	image := new(pngImage)
	image.bitDepth = 8
	image.colorType = 0

	image.validatePNG(reader)

	chunks := image.processPNG(reader)

	for i, chunk := range chunks {
		chunkType := string(chunk.chunkType)
		// IHDR first, and once, as the PNG specification asks: a tRNS before
		// it was read for a gray image and written as the /Mask of one
		// (review of 11 October 2026, #38)
		if (i == 0) != (chunkType == "IHDR") {
			panic("Invalid PNG: the IHDR chunk must be the first chunk, and only the first.")
		}
		switch chunkType {
		case "IHDR":
			if len(chunk.chunkData) != 13 {
				panic("Invalid PNG IHDR chunk.")
			}
			image.w = int(toUint32(chunk.chunkData, 0)) // Width
			image.h = int(toUint32(chunk.chunkData, 4)) // Height
			image.bitDepth = int(chunk.chunkData[8])    // BitDepth
			image.colorType = int(chunk.chunkData[9])   // Color Type

			// Only the deflate compression method and the adaptive filter
			// method are defined, and libpng refuses a file of another one,
			// where the rows of this one would be read as if it were 0.
			if chunk.chunkData[10] != 0 {
				panic("Unknown PNG compression method.")
			}
			if chunk.chunkData[11] != 0 {
				panic("Unknown PNG filter method.")
			}

			if chunk.chunkData[12] == 1 {
				panic("Interlaced PNG images are not supported.\n" +
					"Convert the image using OptiPNG:\noptipng -i0 -o7 myimage.png")
			}
			// Only the interlace methods 0 and 1 are defined (ISO/IEC 15948,
			// 11.2.2); the rows of another one were read as if it were 0.
			if chunk.chunkData[12] != 0 {
				panic("Unknown PNG interlace method.")
			}
		case "IDAT":
			image.iDAT = append(image.iDAT, chunk.chunkData...)
		case "PLTE":
			// 1 to 256 colors of 3 bytes each.
			if len(chunk.chunkData)%3 != 0 || len(chunk.chunkData) == 0 || len(chunk.chunkData) > 3*256 {
				panic("Incorrect palette length.")
			}
			// An index past the colors of the palette is drawn black, as
			// libpng and browsers draw it.
			image.pLTE = make([]byte, 3*256)
			copy(image.pLTE, chunk.chunkData)
		case "tRNS":
			if image.colorType == 3 {
				image.tRNS = chunk.chunkData
			} else if image.colorType == 0 || image.colorType == 2 {
				image.colorKeyMask = image.colorKeyMaskOf(chunk.chunkData)
			}
		case "pHYs":
			image.readPhysicalSize(chunk.chunkData)
		default:
			// A critical chunk, of an uppercase first letter, that is not
			// known cannot be passed over (ISO/IEC 15948, 5.4).
			if chunk.chunkType[0]&0x20 == 0 {
				panic("Unknown critical PNG chunk " + chunkType + ".")
			}
		}
		// The gAMA, cHRM, sBIT and bKGD chunks are ignored, in all four
		// ports: the samples are embedded as they are.
	}

	// The size and the bit depth of the color type checked, as their image
	// data must be no more than decompressor.MaxDecodedLength
	image.getImageDataLength()
	if image.iDAT == nil {
		panic("The PNG image has no image data.")
	}
	// The image data is read a row at a time as it inflates, a row and the
	// one before it in memory, never the whole of it: an image of 8000 by
	// 8000 with alpha took 1.3 GB read whole (21 November 2026). The IDAT
	// data that is the rows of the image and nothing more is embedded as it
	// is, checked as it inflates, and the reader undoes the filters of the
	// rows, as a PNG decoder does. The IDAT data of an image with alpha, whose
	// alpha goes in a soft mask of its own, and any other IDAT data are
	// decoded and compressed again, as each row is undone. Data after the
	// last row is ignored.
	if image.colorType == 0 || image.colorType == 2 || image.colorType == 3 {
		colors := 1
		if image.colorType == 2 {
			colors = 3
		}
		if image.pngRows((image.w*colors*image.bitDepth+7)/8, 1, nil) {
			image.embedIDAT()
			return image
		}
	}
	image.decode()
	image.stream = image.deflatedImageData

	return image
}

// pngRows reads the rows of the image data as it inflates, the row and the
// one before it alone in memory: each row's filter undone and the row
// handed to each, or, with no each, its filter type checked alone. It tells
// whether the data is the image exactly. It fails as InflateExact does, then
// on data shorter than the image, then on the first filter type that PNG
// does not define, the order in which the image read whole failed.
func (image *pngImage) pngRows(bytesPerRow, bytesPerPixel int, each func(line []byte)) bool {
	raw := make([]byte, bytesPerRow+1)
	lines := [2][]byte{make([]byte, bytesPerRow), make([]byte, bytesPerRow)}
	var prior []byte
	first := min(bytesPerPixel, bytesPerRow)
	fill, row, badFilter := 0, 0, -1
	length := image.h * (bytesPerRow + 1)
	n, exact, err := decompressor.InflateExactTo(image.iDAT, length, func(p []byte) {
		for len(p) > 0 && row < image.h {
			k := copy(raw[fill:], p)
			fill += k
			p = p[k:]
			if fill < len(raw) {
				return
			}
			if filter := raw[0]; badFilter < 0 && filter > 4 {
				badFilter = int(filter)
			} else if badFilter < 0 && each != nil {
				line := lines[row&1]
				copy(line, raw[1:])
				unfilterLine(line, prior, filter, bytesPerPixel, first)
				each(line)
				prior = line
			}
			row++
			fill = 0
		}
	})
	if err != nil {
		panic(err)
	}
	if n < length {
		panic("The PNG image data is shorter than the image.")
	}
	if badFilter >= 0 {
		panic(fmt.Sprintf("Invalid PNG filter type %d.", badFilter))
	}
	return exact
}

// embedIDAT makes the IDAT data of a grayscale, truecolor or palette image
// the stream of the image object, which is faster than decoding the samples
// and compressing them again; pngRows checked the filter type of each row,
// as decoding checks it. The alpha of a palette image with a tRNS chunk is
// decoded for its soft mask, a row at a time.
func (image *pngImage) embedIDAT() {
	image.stream = image.iDAT
	image.decodeColors = 1
	if image.colorType == 2 {
		image.decodeColors = 3
	}
	if image.colorType == 3 {
		image.palette = image.pLTE[:3<<image.bitDepth]
		// A tRNS chunk of no alpha but 255 needs no decoding for its soft mask.
		if image.tRNS != nil && !allBytesAre(image.tRNS, 255) {
			alpha := compressor.NewImageWriter()
			indexes := make([]byte, image.w)
			a := make([]byte, image.w)
			defer func() {
				if r := recover(); r != nil {
					alpha.Discard()
					panic(r)
				}
			}()
			image.pngRows((image.w*image.bitDepth+7)/8, 1, func(line []byte) {
				image.paletteIndexesOf(line, indexes)
				image.paletteAlphaOf(indexes, a)
				alpha.Write(a)
			})
			image.setAlpha(alpha)
		}
	}
}

// setAlpha keeps the deflated alpha of the pixels for the soft mask of the
// image, unless the image has none or every pixel is opaque: an image with
// no soft mask is drawn the same, is smaller, and is one that PDF/A-1 can
// hold.
func (image *pngImage) setAlpha(alpha *compressor.ImageWriter) {
	if alpha.Empty() || alpha.AllAre(255) {
		alpha.Discard()
		return
	}
	image.deflatedAlphaData = alpha.Bytes()
}

// decode decodes the samples of the rows of the image as they inflate, a
// row at a time, and deflates them as each row is undone; and the alpha, of
// a grayscale or truecolor image with alpha or of a palette image's tRNS
// chunk, for its soft mask.
func (image *pngImage) decode() {
	w := image.w
	size := 1 // The bytes of a sample
	if image.bitDepth == 16 {
		size = 2
	}
	color := compressor.NewImageWriter()
	alpha := compressor.NewImageWriter()
	var bytesPerRow, bytesPerPixel int
	var each func(line []byte)
	switch image.colorType {
	case 0:
		// Grayscale Image
		switch image.bitDepth {
		case 16:
			bytesPerRow, bytesPerPixel = 2*w, 2
		case 8:
			bytesPerRow, bytesPerPixel = w, 1
		case 4, 2, 1:
			bytesPerRow, bytesPerPixel = (w*image.bitDepth+7)/8, 1
		default:
			panic("Image with unsupported bit depth == " + fmt.Sprint(image.bitDepth))
		}
		each = color.Write
	case 4:
		// Grayscale image with alpha, the alpha's first byte in the soft mask
		bytesPerRow, bytesPerPixel = 2*size*w, 2*size
		gray := make([]byte, size*w)
		a := make([]byte, w)
		each = func(line []byte) {
			for i := 0; i < w; i++ {
				copy(gray[size*i:size*i+size], line[2*size*i:2*size*i+size])
				a[i] = line[2*size*i+size]
			}
			color.Write(gray)
			alpha.Write(a)
		}
	case 6:
		bytesPerRow, bytesPerPixel = 4*size*w, 4*size
		rgb := make([]byte, 3*size*w)
		a := make([]byte, w)
		each = func(line []byte) {
			for i := 0; i < w; i++ {
				copy(rgb[3*size*i:3*size*i+3*size], line[4*size*i:4*size*i+3*size])
				a[i] = line[4*size*i+3*size]
			}
			color.Write(rgb)
			alpha.Write(a)
		}
	case 2:
		// True color image; a PLTE chunk in it is only a suggested palette
		bytesPerRow, bytesPerPixel = 3*size*w, 3*size
		each = color.Write
	default:
		// Indexed image
		bytesPerRow, bytesPerPixel = (w*image.bitDepth+7)/8, 1
		indexes := make([]byte, w)
		rgb := make([]byte, 3*w)
		a := make([]byte, w)
		each = func(line []byte) {
			image.paletteIndexesOf(line, indexes)
			for i, k := range indexes {
				copy(rgb[3*i:3*i+3], image.pLTE[3*int(k):3*int(k)+3])
			}
			color.Write(rgb)
			if image.tRNS != nil {
				image.paletteAlphaOf(indexes, a)
				alpha.Write(a)
			}
		}
	}
	defer func() {
		if r := recover(); r != nil {
			color.Discard()
			alpha.Discard()
			panic(r)
		}
	}()
	image.pngRows(bytesPerRow, bytesPerPixel, each)
	image.deflatedImageData = color.Bytes()
	image.setAlpha(alpha)
}

// colorSpace returns the color space and the bits per component of the image
// object: RGB for a palette image, which is /Indexed on RGB when its stream is
// its IDAT data.
func (image *pngImage) colorSpace() (string, int) {
	switch image.colorType {
	case 0:
		return device.Gray, image.bitDepth
	case 4:
		return device.Gray, image.bitDepth
	case 3:
		if image.palette != nil {
			return device.RGB, image.bitDepth
		}
		return device.RGB, 8
	}
	if image.bitDepth == 16 {
		return device.RGB, 16
	}
	return device.RGB, 8
}

// GetWidth returns the width of the image.
func (image *pngImage) GetWidth() float32 {
	return float32(image.w)
}

// GetHeight returns the height of the image.
func (image *pngImage) GetHeight() float32 {
	return float32(image.h)
}

// GetColorType returns the color type of the image.
func (image *pngImage) GetColorType() int {
	return image.colorType
}

// GetBitDepth returns the bit depth of the image.
func (image *pngImage) GetBitDepth() int {
	return image.bitDepth
}

// GetData returns the image data: the samples, deflated. For an image whose
// stream is its IDAT data they are decoded on the first call.
func (image *pngImage) GetData() []byte {
	if image.deflatedImageData == nil {
		image.decode()
	}
	return image.deflatedImageData
}

// GetAlpha returns the image alpha data.
// colorKeyMaskOf returns the /Mask of the transparent color the tRNS chunk of
// a grayscale or truecolor image names: a sample of 2 bytes for each
// component, of which the bits of the image are read, as libpng reads it, so
// that 255 in an image of 1 bit is white. A chunk of another length gives
// none.
func (image *pngImage) colorKeyMaskOf(data []byte) []int {
	components := 3
	if image.colorType == 0 {
		components = 1
	}
	if len(data) != 2*components {
		return nil
	}
	maxSample := (1 << image.bitDepth) - 1
	mask := make([]int, 2*components)
	for i := 0; i < components; i++ {
		sample := (int(data[2*i])<<8 | int(data[2*i+1])) & maxSample
		mask[2*i] = sample
		mask[2*i+1] = sample
	}
	return mask
}

// GetColorKeyMask returns the /Mask of the transparent color of a grayscale
// or truecolor image, the ranges of its components, or nil when it has none.
func (image *pngImage) GetColorKeyMask() []int {
	return image.colorKeyMask
}

func (image *pngImage) GetAlpha() []byte {
	return image.deflatedAlphaData
}

func (image *pngImage) processPNG(reader io.Reader) []*pngChunk {
	chunks := make([]*pngChunk, 0)
	for {
		chunk := image.getChunk(reader)
		if string(chunk.chunkType) == "IEND" {
			break
		}
		chunks = append(chunks, chunk)
	}
	return chunks
}

// getImageDataLength checks the size, the bit depth and the color type of the
// IHDR chunk, and returns the length of the decompressed image data: each row
// is a filter type byte and the packed samples. The size comes from the file,
// so it is checked before any buffer is allocated for the image.
func (image *pngImage) getImageDataLength() int {
	if image.w <= 0 || image.h <= 0 || image.w > math.MaxInt32 || image.h > math.MaxInt32 {
		panic("Invalid PNG image size.")
	}
	// Each row has a filter type byte, so a taller image is too large; a height
	// of at most the limit also keeps the products below in an int64.
	if image.h > decompressor.MaxDecodedLength {
		panic(fmt.Sprintf("The PNG image is larger than %d bytes.", decompressor.MaxDecodedLength))
	}
	depth := image.bitDepth
	var channels int
	var validBitDepth bool
	switch image.colorType {
	case 0:
		channels = 1
		validBitDepth = depth == 1 || depth == 2 || depth == 4 || depth == 8 || depth == 16
	case 2:
		channels = 3
		validBitDepth = depth == 8 || depth == 16
	case 3:
		channels = 1
		validBitDepth = depth == 1 || depth == 2 || depth == 4 || depth == 8
	case 4:
		channels = 2
		validBitDepth = depth == 8 || depth == 16
	case 6:
		channels = 4
		validBitDepth = depth == 8 || depth == 16
	default:
		panic(fmt.Sprintf("Invalid PNG color type %d.", image.colorType))
	}
	if !validBitDepth {
		panic(fmt.Sprintf("Invalid PNG bit depth %d for color type %d.", depth, image.colorType))
	}
	if image.colorType == 3 && image.pLTE == nil {
		panic("The PNG palette image has no PLTE chunk.")
	}
	bytesPerRow := (int64(image.w)*int64(channels)*int64(depth) + 7) / 8
	length := int64(image.h) * (1 + bytesPerRow)
	// A palette image becomes 3 bytes of RGB and 1 byte of alpha per pixel.
	decodedLength := length
	if image.colorType == 3 {
		decodedLength = 4 * int64(image.w) * int64(image.h)
	}
	if length > decompressor.MaxDecodedLength || decodedLength > decompressor.MaxDecodedLength {
		panic(fmt.Sprintf("The PNG image is larger than %d bytes.", decompressor.MaxDecodedLength))
	}
	return int(length)
}

func (image *pngImage) validatePNG(reader io.Reader) {
	buf := getPNGBytes(reader, 8)
	if ((buf[0] & 0xFF) == 0x89) &&
		buf[1] == 0x50 &&
		buf[2] == 0x4E &&
		buf[3] == 0x47 &&
		buf[4] == 0x0D &&
		buf[5] == 0x0A &&
		buf[6] == 0x1A &&
		buf[7] == 0x0A {
		// The PNG signature is correct.
	} else {
		panic("Wrong PNG signature.")
	}
}

func (image *pngImage) getChunk(reader io.Reader) *pngChunk {
	chunk := newPNGChunk()
	chunk.chunkLength = getPNGUint32(reader) // The length of the data chunk.
	if chunk.chunkLength > math.MaxInt32 {
		panic(fmt.Sprintf("Invalid PNG chunk length %d.", chunk.chunkLength))
	}
	chunk.chunkType = getPNGBytes(reader, 4)                      // The chunk type.
	chunk.chunkData = getPNGBytes(reader, int(chunk.chunkLength)) // The chunk data.
	chunk.chunkCRC = getPNGUint32(reader)                         // CRC of the type and data chunks.

	crc := crc32.NewIEEE()
	crc.Write(chunk.chunkType)
	crc.Write(chunk.chunkData)
	if crc.Sum32() != chunk.chunkCRC {
		panic("pngImage chunk has bad CRC.")
	}
	return chunk
}

// getPNGBytes reads the bytes in pieces, so that a chunk length that the file
// does not have fails at the end of the stream instead of allocating the length.
func getPNGBytes(reader io.Reader, length int) []byte {
	var buf bytes.Buffer
	if n, err := io.CopyN(&buf, reader, int64(length)); err != nil || n != int64(length) {
		panic("Unexpected end of the PNG stream.")
	}
	return buf.Bytes()
}

func getPNGUint32(reader io.Reader) uint32 {
	return toUint32(getPNGBytes(reader, 4), 0)
}

func toUint32(buf []byte, off int) uint32 {
	return uint32(buf[off])<<24 | uint32(buf[off+1])<<16 | uint32(buf[off+2])<<8 | uint32(buf[off+3])
}

// unfilterLine undoes the filter of a row, the row above it its prior, none
// for the first row: a row holds the bytes after its filter type, and the
// byte of the pixel on the left is bytesPerPixel bytes before, or the byte
// before for pixels smaller than a byte; the bytes of the first pixel, first
// of them, have no byte on the left, and the first row no row above, so they
// are undone apart from the rest, with the bytes they do not have taken as 0.
// It panics on a filter type that PNG does not define, as libpng does.
func unfilterLine(line, prior []byte, filter byte, bytesPerPixel, first int) {
	switch {
	case filter == 0x00: // None
	case filter == 0x01 || filter == 0x04 && prior == nil:
		// Sub, and Paeth of the first row, which is the byte on the left.
		for i := bytesPerPixel; i < len(line); i++ {
			line[i] += line[i-bytesPerPixel]
		}
	case filter == 0x02: // Up
		if prior != nil {
			for i := range line {
				line[i] += prior[i]
			}
		}
	case filter == 0x03 && prior == nil: // Average of the first row
		for i := bytesPerPixel; i < len(line); i++ {
			line[i] += line[i-bytesPerPixel] / 2
		}
	case filter == 0x03: // Average
		prior := prior[:len(line)]
		for i := 0; i < first; i++ {
			line[i] += prior[i] / 2
		}
		for i := bytesPerPixel; i < len(line); i++ {
			line[i] += byte((int(line[i-bytesPerPixel]) + int(prior[i])) / 2)
		}
	case filter == 0x04: // Paeth, whose first pixel is the byte above
		prior := prior[:len(line)]
		for i := 0; i < first; i++ {
			line[i] += prior[i]
		}
		for i := bytesPerPixel; i < len(line); i++ {
			line[i] += byte(paeth(int(line[i-bytesPerPixel]), int(prior[i]), int(prior[i-bytesPerPixel])))
		}
	default:
		panic(fmt.Sprintf("Invalid PNG filter type %d.", filter))
	}
}

// paeth returns whichever of the bytes on the left, above and above on the
// left is nearest to their sum less the one above on the left.
func paeth(a, b, c int) int {
	pa := b - c // p - a, where p = a + b - c
	pb := a - c // p - b
	pc := pa + pb
	if pa < 0 {
		pa = -pa
	}
	if pb < 0 {
		pb = -pb
	}
	if pc < 0 {
		pc = -pc
	}
	if pa <= pb && pa <= pc {
		return a
	} else if pb <= pc {
		return b
	}
	return c
}

// paletteIndexesOf reads the palette's index of each pixel of a row.
func (image *pngImage) paletteIndexesOf(row, indexes []byte) {
	mask := (1 << image.bitDepth) - 1
	for col := 0; col < image.w; col++ {
		bit := col * image.bitDepth
		b := int(row[bit/8])
		indexes[col] = byte((b >> (8 - image.bitDepth - bit%8)) & mask)
	}
}

// paletteAlphaOf gives each index of a row its alpha in the tRNS chunk, and
// 255 to an index past its end.
func (image *pngImage) paletteAlphaOf(indexes, alpha []byte) {
	for i, k := range indexes {
		if int(k) < len(image.tRNS) {
			alpha[i] = image.tRNS[k]
		} else {
			alpha[i] = 0xff
		}
	}
}
