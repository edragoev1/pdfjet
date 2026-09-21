// bmpimage.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.
//
// Written by Jonas Krogsböll, who contributed it to PDFjet.

package pdfjet

import (
	"fmt"
	"io"
	"math"
	"math/bits"

	"github.com/edragoev1/pdfjet/v9/src/internal/compressor"
	"github.com/edragoev1/pdfjet/v9/src/internal/decompressor"
	"github.com/edragoev1/pdfjet/v9/src/internal/fastfloat"
)

// bmpImage describes BMP image object.
type bmpImage struct {
	w        int    // Image width in pixels
	h        int    // Image height in pixels
	deflated []byte // The deflated reconstructed image data
	bpp      int
	palette  [][]byte
	masks    []uint32 // The red, green and blue masks of a 16 or 32 bit pixel
	topDown  bool     // If the first row is the top row

	// The size the header gives the image, in points, or 0 when it gives
	// none: the pixels per metre of each axis, which most writers leave at 0.
	physicalWidth  float32
	physicalHeight float32
}

// bmpPointsPerMeter is the points a metre is: 72/0.0254.
const bmpPointsPerMeter = 72.0 / 0.0254

// setPhysicalSize works out the size the image asks to be drawn at from the
// pixels per metre of the header. Most writers leave the two at 0, and a size
// too large for a PDF number is passed over; the image keeps the size of its
// pixels for both.
func (image *bmpImage) setPhysicalSize(pixelsPerMeterX, pixelsPerMeterY int) {
	if pixelsPerMeterX <= 0 || pixelsPerMeterY <= 0 {
		return
	}
	width := float32(float64(image.w) * bmpPointsPerMeter / float64(pixelsPerMeterX))
	height := float32(float64(image.h) * bmpPointsPerMeter / float64(pixelsPerMeterY))
	if fastfloat.IsWritable(width) && fastfloat.IsWritable(height) {
		image.physicalWidth = width
		image.physicalHeight = height
	}
}

const (
	biRGB       = 0
	biBitfields = 3
)

const (
	m10000000 = 0x80
	m01000000 = 0x40
	m00100000 = 0x20
	m00010000 = 0x10
	m00001000 = 0x08
	m00000100 = 0x04
	m00000010 = 0x02
	m00000001 = 0x01
	m11110000 = 0xF0
	m00001111 = 0x0F
)

// newBMPImage constructs bitmap image objects.
func newBMPImage(reader io.Reader) *bmpImage {
	image := new(bmpImage)

	bm := getNBytes(reader, 2)
	// From Wikipedia
	if (bm[0] == 'B' && bm[1] == 'M') ||
		(bm[0] == 'B' && bm[1] == 'A') ||
		(bm[0] == 'C' && bm[1] == 'I') ||
		(bm[0] == 'C' && bm[1] == 'P') ||
		(bm[0] == 'I' && bm[1] == 'C') ||
		(bm[0] == 'P' && bm[1] == 'T') {
		skipNBytes(reader, 8)
		offset := readSignedInt(reader) // Where the pixels start
		headerSize := readSignedInt(reader)
		image.w = readSignedInt(reader)
		image.h = readSignedInt(reader)
		if image.h < 0 {
			// A negative height is that of a top-down bitmap.
			image.h = -image.h
			image.topDown = true
		}
		skipNBytes(reader, 2)
		image.bpp = read2BytesLE(reader)
		compression := readSignedInt(reader)
		// The size and the bit depth come from the file, so they are checked
		// before any buffer is allocated for the image.
		if image.w <= 0 || image.h <= 0 || image.h > math.MaxInt32 { // -math.MinInt32
			panic("Invalid BMP image size.")
		}
		switch image.bpp {
		case 1, 4, 8, 16, 24, 32:
		default:
			panic("Can only parse 1 bit, 4bit, 8bit, 16bit, 24bit and 32bit images")
		}
		// RLE and the JPEG and PNG compressions are not supported, and their
		// data is not read as pixels.
		if compression != biRGB && !(compression == biBitfields && (image.bpp == 16 || image.bpp == 32)) {
			panic("Compressed BMP images are not supported.")
		}
		// The older OS/2 header is 12 bytes; the others start with the 40
		// bytes of the BITMAPINFOHEADER.
		if headerSize < 40 {
			panic(fmt.Sprintf("Unsupported BMP header of %d bytes.", headerSize))
		}
		rowSize := 4 * ((int64(image.bpp)*int64(image.w) + 31) / 32)
		// A height of at most the limit keeps the products in an int64.
		if int64(image.h) > decompressor.MaxDecodedLength ||
			3*int64(image.w)*int64(image.h) > decompressor.MaxDecodedLength ||
			rowSize*int64(image.h) > decompressor.MaxDecodedLength {
			panic(fmt.Sprintf("The BMP image is larger than %d bytes.", decompressor.MaxDecodedLength))
		}
		skipNBytes(reader, 4) // The size of the pixels
		pixelsPerMeterX := readSignedInt(reader)
		pixelsPerMeterY := readSignedInt(reader)
		image.setPhysicalSize(pixelsPerMeterX, pixelsPerMeterY)
		colorsUsed := readSignedInt(reader)
		skipNBytes(reader, 4)
		read := 54 // The bytes read so far

		// The masks follow the first 40 bytes of the header, in the larger
		// headers or after the BITMAPINFOHEADER. Without them the pixels have
		// the masks of the format: 5 bits a color in 16 bits, and 8 bits a
		// color in 32 bits.
		if compression == biBitfields {
			image.masks = []uint32{
				uint32(readSignedInt(reader)), uint32(readSignedInt(reader)), uint32(readSignedInt(reader))}
			read += 12
		} else if image.bpp == 16 {
			image.masks = []uint32{0x7C00, 0x03E0, 0x001F}
		} else if image.bpp == 32 {
			image.masks = []uint32{0x00FF0000, 0x0000FF00, 0x000000FF}
		}
		if 14+headerSize > read {
			skipNBytes(reader, 14+headerSize-read) // The rest of a larger header
			read = 14 + headerSize
		}

		if image.bpp <= 8 {
			numpalcol := colorsUsed
			if numpalcol == 0 {
				numpalcol = 1 << image.bpp
			}
			if numpalcol < 0 || numpalcol > 256 {
				panic(fmt.Sprintf("Invalid BMP palette size %d.", numpalcol))
			}
			image.parsePalette(reader, numpalcol)
			read += 4 * numpalcol
		}
		if offset > read {
			skipNBytes(reader, offset-read) // The pixels start at the offset
		}
		image.parseData(reader)
	} else {
		panic("BMP data could not be parsed!")
	}

	return image
}

func (image *bmpImage) parseData(reader io.Reader) []byte {
	// rowsize is 4 * ceil (bpp*width/32.0)
	rowsize := int(4 * ((int64(image.bpp)*int64(image.w) + 31) / 32)) // 4 byte alignment
	// The rows are read before the image is allocated, so a size that the
	// stream does not have takes no memory.
	rows := make([][]byte, 0, min(image.h, 4096))
	for i := 0; i < image.h-1; i++ {
		rows = append(rows, getNBytes(reader, rowsize))
	}
	// Some files end without the padding of the last row, which holds no
	// pixels, as Pillow and browsers read them.
	last := getNBytes(reader, int((int64(image.bpp)*int64(image.w)+7)/8))
	_, _ = io.CopyN(io.Discard, reader, int64(rowsize-len(last)))
	rows = append(rows, last)
	bmpImage := make([]byte, 3*image.w*image.h)
	index := 0
	for i := 0; i < image.h; i++ {
		row := rows[i]
		switch image.bpp {
		case 1:
			row = image.bit1to8(row, image.w) // opslag i palette
		case 4:
			row = image.bit4to8(row, image.w) // opslag i palette
		case 8:
		case 16:
			row = masksTo24(row, image.w, 2, image.masks)
		case 24:
			// bytes are correct
		case 32:
			row = masksTo24(row, image.w, 4, image.masks)
		default:
			panic("Can only parse 1 bit, 4bit, 8bit, 16bit, 24bit and 32bit images")
		}

		if image.topDown {
			index = image.w * i * 3
		} else {
			index = image.w * (image.h - i - 1) * 3
		}
		if image.palette != nil { // indexed
			for j := 0; j < image.w; j++ {
				bmpImage[index] = image.palette[row[j]][2]
				index++
				bmpImage[index] = image.palette[row[j]][1]
				index++
				bmpImage[index] = image.palette[row[j]][0]
				index++
			}
		} else { // not indexed
			for j := 0; j < 3*image.w; j += 3 {
				bmpImage[index] = row[j+2]
				index++
				bmpImage[index] = row[j+1]
				index++
				bmpImage[index] = row[j]
				index++
			}
		}
	}
	image.deflated = compressor.Deflate(bmpImage)

	return bmpImage
}

// masksTo24 converts a row of 16 or 32 bit little endian pixels to blue, green
// and red bytes, with the red, green and blue masks. A color of fewer than 8
// bits is scaled to the full range, so that 31 of 5 bits is 255.
func masksTo24(row []byte, width, bytesPerPixel int, masks []uint32) []byte {
	ret := make([]byte, 3*width)
	j := 0
	for i := 0; i < width*bytesPerPixel; i += bytesPerPixel {
		pixel := uint32(row[i]) | uint32(row[i+1])<<8
		if bytesPerPixel == 4 {
			pixel |= uint32(row[i+2])<<16 | uint32(row[i+3])<<24
		}
		ret[j] = colorOf(pixel, masks[2])
		ret[j+1] = colorOf(pixel, masks[1])
		ret[j+2] = colorOf(pixel, masks[0])
		j += 3
	}
	return ret
}

// colorOf returns the color of the pixel under the mask, from 0 to 255.
func colorOf(pixel, mask uint32) byte {
	if mask == 0 {
		return 0
	}
	shift := bits.TrailingZeros32(mask)
	max := uint64(mask >> shift)
	value := uint64((pixel & mask) >> shift)
	return byte(value * 255 / max)
}

func (image *bmpImage) bit4to8(row []byte, width int) []byte {
	ret := make([]byte, width)
	for i := 0; i < width; i++ {
		if i%2 == 0 {
			ret[i] = (row[i/2] & m11110000) >> 4
		} else {
			ret[i] = row[i/2] & m00001111
		}
	}
	return ret
}

func (image *bmpImage) bit1to8(row []byte, width int) []byte {
	ret := make([]byte, width)
	for i := 0; i < width; i++ {
		switch i % 8 {
		case 0:
			ret[i] = (row[i/8] & m10000000) >> 7
		case 1:
			ret[i] = (row[i/8] & m01000000) >> 6
		case 2:
			ret[i] = (row[i/8] & m00100000) >> 5
		case 3:
			ret[i] = (row[i/8] & m00010000) >> 4
		case 4:
			ret[i] = (row[i/8] & m00001000) >> 3
		case 5:
			ret[i] = (row[i/8] & m00000100) >> 2
		case 6:
			ret[i] = (row[i/8] & m00000010) >> 1
		case 7:
			ret[i] = row[i/8] & m00000001
		}
	}
	return ret
}

// parsePalette reads the colors of the palette. An index past them is drawn
// black, as browsers draw it, so the palette has the 256 colors an index of 8
// bits can take.
func (image *bmpImage) parsePalette(reader io.Reader, size int) {
	image.palette = make([][]byte, 256)
	for i := range image.palette {
		if i < size {
			image.palette[i] = getNBytes(reader, 4)
		} else {
			image.palette[i] = []byte{0, 0, 0, 0}
		}
	}
}

func read2BytesLE(reader io.Reader) int {
	buf := getNBytes(reader, 2)
	val := 0
	val |= int(buf[1]) & 0xff
	val <<= 8
	val |= int(buf[0]) & 0xff
	return val
}

func readSignedInt(reader io.Reader) int {
	buf := getNBytes(reader, 4)
	var val uint32
	val |= uint32(buf[3]) & uint32(0xff)
	val <<= 8
	val |= uint32(buf[2]) & uint32(0xff)
	val <<= 8
	val |= uint32(buf[1]) & uint32(0xff)
	val <<= 8
	val |= uint32(buf[0]) & uint32(0xff)
	return int(int32(val))
}

// GetWidth returns the image width.
func (image *bmpImage) getWidth() float32 {
	return float32(image.w)
}

// GetHeight returns the image height.
func (image *bmpImage) getHeight() float32 {
	return float32(image.h)
}

// GetData returns the compressed image data.
func (image *bmpImage) getData() []byte {
	return image.deflated
}
