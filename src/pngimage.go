// pngimage.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"fmt"
	"io"
	"math"

	"github.com/edragoev1/pdfjet/v9/src/internal/compressor"
	"github.com/edragoev1/pdfjet/v9/src/internal/crc32util"
	"github.com/edragoev1/pdfjet/v9/src/internal/decompressor"
)

// PNGImage is used to embed PNG images in the PDF document.
//
// Please note: interlaced images are not supported.
// To convert an interlaced image to a non-interlaced image, use OptiPNG:
//
//	optipng -i0 -o7 myimage.png
type PNGImage struct {
	w int // Image width in pixels
	h int // Image height in pixels

	iDAT []byte // The compressed data in the IDAT chunk
	pLTE []byte // The palette data
	tRNS []byte // The alpha for the palette data

	deflatedImageData []byte // The deflated image data
	deflatedAlphaData []byte // The deflated alpha channel data

	bitDepth  int
	colorType int
}

// NewPNGImage is used to embed PNG images in a PDF document.
func NewPNGImage(reader io.Reader) *PNGImage {
	image := new(PNGImage)
	image.bitDepth = 8
	image.colorType = 0

	image.validatePNG(reader)

	chunks := image.processPNG(reader)

	for _, chunk := range chunks {
		chunkType := string(chunk.chunkType)
		switch chunkType {
		case "IHDR":
			if len(chunk.chunkData) != 13 {
				panic("Invalid PNG IHDR chunk.")
			}
			image.w = int(toUint32(chunk.chunkData, 0)) // Width
			image.h = int(toUint32(chunk.chunkData, 4)) // Height
			image.bitDepth = int(chunk.chunkData[8])    // BitDepth
			image.colorType = int(chunk.chunkData[9])   // Color Type

			if chunk.chunkData[12] == 1 {
				panic("Interlaced PNG images are not supported.\n" +
					"Convert the image using OptiPNG:\noptipng -i0 -o7 myimage.png")
			}
		case "IDAT":
			image.iDAT = append(image.iDAT, chunk.chunkData...)
		case "PLTE":
			image.pLTE = chunk.chunkData
			if len(image.pLTE)%3 != 0 {
				panic("Incorrect palette length.")
			}
		case "tRNS":
			if image.colorType == 3 {
				image.tRNS = chunk.chunkData
			}
		}
		// The gAMA, cHRM, sBIT and bKGD chunks are ignored, in all four
		// ports: the samples are embedded as they are.
	}

	imageDataLength := image.getImageDataLength()
	if image.iDAT == nil {
		panic("The PNG image has no image data.")
	}
	// The rows of the image; data after the last row is ignored.
	inflatedIDAT, err := decompressor.InflatePrefix(image.iDAT, imageDataLength)
	if err != nil {
		panic(err)
	}
	if len(inflatedIDAT) < imageDataLength {
		panic("The PNG image data is shorter than the image.")
	}

	var imageData []byte
	switch image.colorType {
	case 0:
		// Grayscale Image
		switch image.bitDepth {
		case 16:
			imageData = image.getImageColorType0BitDepth16(inflatedIDAT)
		case 8:
			imageData = image.getImageColorType0BitDepth8(inflatedIDAT)
		case 4:
			imageData = image.getImageColorType0BitDepth4(inflatedIDAT)
		case 2:
			imageData = image.getImageColorType0BitDepth2(inflatedIDAT)
		case 1:
			imageData = image.getImageColorType0BitDepth1(inflatedIDAT)
		default:
			panic("Image with unsupported bit depth == " + fmt.Sprint(image.bitDepth))
		}
	case 4:
		// Grayscale image with alpha
		if image.bitDepth == 8 {
			imageData = image.getImageColorType4BitDepth8(inflatedIDAT)
		} else {
			panic("Image with unsupported bit depth == " + fmt.Sprint(image.bitDepth))
		}
	case 6:
		if image.bitDepth == 8 {
			imageData = image.getImageColorType6BitDepth8(inflatedIDAT)
		} else {
			panic("Image with unsupported bit depth == " + fmt.Sprint(image.bitDepth))
		}
	case 2:
		// True color image; a PLTE chunk in it is only a suggested palette
		if image.bitDepth == 16 {
			imageData = image.getImageColorType2BitDepth16(inflatedIDAT)
		} else {
			imageData = image.getImageColorType2BitDepth8(inflatedIDAT)
		}
	default:
		// Indexed image
		imageData = image.getImageColorType3(inflatedIDAT)
	}

	// Compress the reconstructed image data.
	image.deflatedImageData = compressor.Deflate(imageData)

	return image
}

// GetWidth returns the width of the image.
func (image *PNGImage) GetWidth() float32 {
	return float32(image.w)
}

// GetHeight returns the height of the image.
func (image *PNGImage) GetHeight() float32 {
	return float32(image.h)
}

// GetColorType returns the color type of the image.
func (image *PNGImage) GetColorType() int {
	return image.colorType
}

// GetBitDepth returns the bit depth of the image.
func (image *PNGImage) GetBitDepth() int {
	return image.bitDepth
}

// GetData returns the image data.
func (image *PNGImage) GetData() []byte {
	return image.deflatedImageData
}

// GetAlpha returns the image alpha data.
func (image *PNGImage) GetAlpha() []byte {
	return image.deflatedAlphaData
}

func (image *PNGImage) processPNG(reader io.Reader) []*pngChunk {
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
func (image *PNGImage) getImageDataLength() int {
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

func (image *PNGImage) validatePNG(reader io.Reader) {
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

func (image *PNGImage) getChunk(reader io.Reader) *pngChunk {
	chunk := newPNGChunk()
	chunk.chunkLength = getPNGUint32(reader) // The length of the data chunk.
	if chunk.chunkLength > math.MaxInt32 {
		panic(fmt.Sprintf("Invalid PNG chunk length %d.", chunk.chunkLength))
	}
	chunk.chunkType = getPNGBytes(reader, 4)                      // The chunk type.
	chunk.chunkData = getPNGBytes(reader, int(chunk.chunkLength)) // The chunk data.
	chunk.chunkCRC = getPNGUint32(reader)                         // CRC of the type and data chunks.

	crc32 := crc32util.NewCRC32()
	crc32.Update(chunk.chunkType)
	crc32.Update(chunk.chunkData)
	if crc32.GetValue() != chunk.chunkCRC {
		panic("PNGImage chunk has bad CRC.")
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

// Truecolor Image with Bit Depth == 16
func (image *PNGImage) getImageColorType2BitDepth16(buf []byte) []byte {
	image2 := make([]byte, len(buf)-image.h)

	filters := make([]byte, image.h)
	bytesPerLine := 6*image.w + 1
	k := 0
	j := 0
	for i := 0; i < len(buf); i++ {
		if i%bytesPerLine == 0 {
			filters[j] = buf[i]
			j++
		} else {
			image2[k] = buf[i]
			k++
		}
	}
	applyFilters(filters, image2, image.w, image.h, 6)

	return image2
}

// Truecolor Image with Bit Depth == 8
func (image *PNGImage) getImageColorType2BitDepth8(buf []byte) []byte {
	image2 := make([]byte, len(buf)-image.h)

	filters := make([]byte, image.h)
	bytesPerLine := 3*image.w + 1
	k := 0
	j := 0
	for i := 0; i < len(buf); i++ {
		if i%bytesPerLine == 0 {
			filters[j] = buf[i]
			j++
		} else {
			image2[k] = buf[i]
			k++
		}
	}
	applyFilters(filters, image2, image.w, image.h, 3)

	return image2
}

// Truecolor Image with Alpha Transparency
// getImageColorType4BitDepth8 returns the gray samples; the alpha samples go
// in the soft mask.
func (image *PNGImage) getImageColorType4BitDepth8(buf []byte) []byte {
	image2 := make([]byte, 2*image.w*image.h)
	filters := make([]byte, image.h)
	bytesPerLine := 2*image.w + 1
	k := 0
	j := 0
	for i := 0; i < len(buf); i++ {
		if i%bytesPerLine == 0 {
			filters[j] = buf[i]
			j++
		} else {
			image2[k] = buf[i]
			k++
		}
	}
	applyFilters(filters, image2, image.w, image.h, 2)

	gray := make([]byte, image.w*image.h)
	alpha := make([]byte, image.w*image.h)
	for i := range gray {
		gray[i] = image2[2*i]
		alpha[i] = image2[2*i+1]
	}
	image.deflatedAlphaData = compressor.Deflate(alpha)

	return gray
}

func (image *PNGImage) getImageColorType6BitDepth8(buf []byte) []byte {
	image2 := make([]byte, 4*image.w*image.h) // Image data

	filters := make([]byte, image.h)
	bytesPerLine := 4*image.w + 1
	k := 0
	j := 0
	for i := 0; i < len(buf); i++ {
		if i%bytesPerLine == 0 {
			filters[j] = buf[i]
			j++
		} else {
			image2[k] = buf[i]
			k++
		}
	}
	applyFilters(filters, image2, image.w, image.h, 4)

	idata := make([]byte, 3*image.w*image.h) // Image data
	alpha := make([]byte, image.w*image.h)   // Alpha values

	k = 0
	j = 0
	i := 0
	for i < len(image2) {
		idata[j] = image2[i]
		j++
		i++
		idata[j] = image2[i]
		j++
		i++
		idata[j] = image2[i]
		j++
		i++
		alpha[k] = image2[i]
		k++
		i++
	}
	image.deflatedAlphaData = compressor.Deflate(alpha)

	return idata
}

// getImageColorType3 indexed-color image with bit depth == 1, 2, 4 or 8
// Each value is a palette index; a PLTE chunk shall appear.
// The filters are undone on the packed indexes, one byte per pixel whatever
// the bit depth, before the indexes are looked up in the palette.
func (image *PNGImage) getImageColorType3(buf []byte) []byte {
	bytesPerLine := (image.w*image.bitDepth + 7) / 8
	indexes := make([]byte, bytesPerLine*image.h)
	filters := make([]byte, image.h)
	for row := 0; row < image.h; row++ {
		offset := row * (bytesPerLine + 1)
		filters[row] = buf[offset]
		copy(indexes[row*bytesPerLine:], buf[offset+1:offset+1+bytesPerLine])
	}
	applyFilters(filters, indexes, bytesPerLine, image.h, 1)

	image2 := make([]byte, 3*(image.w*image.h))
	var alpha []byte
	if image.tRNS != nil {
		alpha = make([]byte, image.w*image.h)
		for i := 0; i < len(alpha); i++ {
			alpha[i] = 0xff
		}
	}
	mask := (1 << image.bitDepth) - 1
	n := 0
	j := 0
	for row := 0; row < image.h; row++ {
		for col := 0; col < image.w; col++ {
			bit := col * image.bitDepth
			b := int(indexes[row*bytesPerLine+bit/8])
			k := (b >> (8 - image.bitDepth - bit%8)) & mask
			if image.tRNS != nil && k < len(image.tRNS) {
				alpha[n] = image.tRNS[k]
			}
			n++
			image2[j] = image.pLTE[3*k]
			j++
			image2[j] = image.pLTE[3*k+1]
			j++
			image2[j] = image.pLTE[3*k+2]
			j++
		}
	}

	if image.tRNS != nil {
		image.deflatedAlphaData = compressor.Deflate(alpha)
	}

	return image2
}

// Grayscale Image with Bit Depth == 16
func (image *PNGImage) getImageColorType0BitDepth16(buf []byte) []byte {
	image2 := make([]byte, len(buf)-image.h)

	filters := make([]byte, image.h)
	bytesPerLine := 2*image.w + 1
	k := 0
	j := 0
	for i := 0; i < len(buf); i++ {
		if i%bytesPerLine == 0 {
			filters[j] = buf[i]
			j++
		} else {
			image2[k] = buf[i]
			k++
		}
	}
	applyFilters(filters, image2, image.w, image.h, 2)

	return image2
}

// Grayscale Image with Bit Depth == 8
func (image *PNGImage) getImageColorType0BitDepth8(buf []byte) []byte {
	image2 := make([]byte, len(buf)-image.h)

	filters := make([]byte, image.h)
	bytesPerLine := image.w + 1
	k := 0
	j := 0
	for i := 0; i < len(buf); i++ {
		if i%bytesPerLine == 0 {
			filters[j] = buf[i]
			j++
		} else {
			image2[k] = buf[i]
			k++
		}
	}
	applyFilters(filters, image2, image.w, image.h, 1)

	return image2
}

// Grayscale Image with Bit Depth == 4
func (image *PNGImage) getImageColorType0BitDepth4(buf []byte) []byte {
	image2 := make([]byte, len(buf)-image.h)

	filters := make([]byte, image.h)
	bytesPerLine := image.w/2 + 1
	if image.w%2 > 0 {
		bytesPerLine++
	}

	k := 0
	j := 0
	for i := 0; i < len(buf); i++ {
		if i%bytesPerLine == 0 {
			filters[k] = buf[i]
			k++
		} else {
			image2[j] = buf[i]
			j++
		}
	}
	// The filters work on bytes, one byte per pixel whatever the bit depth.
	applyFilters(filters, image2, bytesPerLine-1, image.h, 1)

	return image2
}

// Grayscale Image with Bit Depth == 2
func (image *PNGImage) getImageColorType0BitDepth2(buf []byte) []byte {
	image2 := make([]byte, len(buf)-image.h)

	filters := make([]byte, image.h)
	bytesPerLine := image.w/4 + 1
	if image.w%4 > 0 {
		bytesPerLine++
	}

	k := 0
	j := 0
	for i := 0; i < len(buf); i++ {
		if i%bytesPerLine == 0 {
			filters[k] = buf[i]
			k++
		} else {
			image2[j] = buf[i]
			j++
		}
	}
	// The filters work on bytes, one byte per pixel whatever the bit depth.
	applyFilters(filters, image2, bytesPerLine-1, image.h, 1)

	return image2
}

// Grayscale Image with Bit Depth == 1
func (image *PNGImage) getImageColorType0BitDepth1(buf []byte) []byte {
	image2 := make([]byte, len(buf)-image.h)

	filters := make([]byte, image.h)
	bytesPerLine := image.w/8 + 1
	if image.w%8 > 0 {
		bytesPerLine++
	}

	k := 0
	j := 0
	for i := 0; i < len(buf); i++ {
		if i%bytesPerLine == 0 {
			filters[k] = buf[i]
			k++
		} else {
			image2[j] = buf[i]
			j++
		}
	}
	// The filters work on bytes, one byte per pixel whatever the bit depth.
	applyFilters(filters, image2, bytesPerLine-1, image.h, 1)

	return image2
}

func applyFilters(
	filters []byte,
	image []byte,
	width, height, bytesPerPixel int) {
	bytesPerLine := width * bytesPerPixel
	filter := byte(0x00)
	for row := 0; row < height; row++ {
		for col := 0; col < bytesPerLine; col++ {
			if col == 0 {
				filter = filters[row]
			}
			if filter == 0x00 { // None
				continue
			}

			a := 0 // The pixel on the left
			if col >= bytesPerPixel {
				a = int(image[(bytesPerLine*row+col)-bytesPerPixel] & 0xff)
			}
			b := 0 // The pixel above
			if row > 0 {
				b = int(image[bytesPerLine*(row-1)+col] & 0xff)
			}
			c := 0 // The pixel diagonally left above
			if col >= bytesPerPixel && row > 0 {
				c = int(image[(bytesPerLine*(row-1)+col)-bytesPerPixel] & 0xff)
			}

			index := bytesPerLine*row + col
			switch filter {
			case 0x01: // Sub
				image[index] += byte(a)
			case 0x02: // Up
				image[index] += byte(b)
			case 0x03: // Average
				image[index] += byte(math.Floor(float64(a+b) / 2.0))
			case 0x04: // Paeth
				p := a + b - c
				pa := math.Abs(float64(p - a))
				pb := math.Abs(float64(p - b))
				pc := math.Abs(float64(p - c))
				if pa <= pb && pa <= pc {
					image[index] += byte(a)
				} else if pb <= pc {
					image[index] += byte(b)
				} else {
					image[index] += byte(c)
				}
			}
		}
	}
}
