// pngimage.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

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
	width := float32(float64(image.w) * pointsPerMeter / float64(pixelsPerMeterX))
	height := float32(float64(image.h) * pointsPerMeter / float64(pixelsPerMeterY))
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
		}
		// The gAMA, cHRM, sBIT and bKGD chunks are ignored, in all four
		// ports: the samples are embedded as they are.
	}

	imageDataLength := image.getImageDataLength()
	if image.iDAT == nil {
		panic("The PNG image has no image data.")
	}
	// The rows of the image; data after the last row is ignored.
	inflatedIDAT, exact, err := decompressor.InflateExact(image.iDAT, imageDataLength)
	if err != nil {
		panic(err)
	}
	if len(inflatedIDAT) < imageDataLength {
		panic("The PNG image data is shorter than the image.")
	}

	// The IDAT data that is the rows of the image and nothing more is embedded
	// as it is, and the reader undoes the filters of the rows, as a PNG
	// decoder does. The IDAT data of an image with alpha, whose alpha goes in
	// a soft mask of its own, and any other IDAT data are decoded and
	// compressed again.
	if exact && (image.colorType == 0 || image.colorType == 2 || image.colorType == 3) {
		image.embedIDAT(inflatedIDAT)
	} else {
		image.decode(inflatedIDAT)
		image.stream = image.deflatedImageData
	}

	return image
}

// embedIDAT makes the IDAT data of a grayscale, truecolor or palette image
// the stream of the image object, which is faster than decoding the samples
// and compressing them again. The filter type of each row is checked, as
// decoding checks it; the alpha of a palette image with a tRNS chunk is
// decoded for its soft mask.
func (image *pngImage) embedIDAT(buf []byte) {
	colors := 1
	if image.colorType == 2 {
		colors = 3
	}
	bytesPerRow := (image.w*colors*image.bitDepth + 7) / 8
	for row := 0; row < image.h; row++ {
		if filter := buf[row*(bytesPerRow+1)]; filter > 4 {
			panic(fmt.Sprintf("Invalid PNG filter type %d.", filter))
		}
	}
	image.stream = image.iDAT
	image.decodeColors = colors
	if image.colorType == 3 {
		image.palette = image.pLTE[:3<<image.bitDepth]
		if image.tRNS != nil {
			indexes := image.paletteIndexes(unfilter(buf, image.h, bytesPerRow, 1))
			image.deflatedAlphaData = compressor.Deflate(image.paletteAlpha(indexes))
		}
	}
}

// decode decodes the samples of the rows of the image, and deflates them.
func (image *pngImage) decode(inflatedIDAT []byte) {
	var imageData []byte
	switch image.colorType {
	case 0:
		// Grayscale Image
		switch image.bitDepth {
		case 16:
			imageData = image.getImageColorType0BitDepth16(inflatedIDAT)
		case 8:
			imageData = image.getImageColorType0BitDepth8(inflatedIDAT)
		case 4, 2, 1:
			imageData = image.getImageColorType0BitDepthBelow8(inflatedIDAT)
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
}

// colorSpace returns the color space and the bits per component of the image
// object: RGB for a palette image, which is /Indexed on RGB when its stream is
// its IDAT data.
func (image *pngImage) colorSpace() (string, int) {
	switch image.colorType {
	case 0:
		return device.Gray, image.bitDepth
	case 4:
		return device.Gray, 8
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
		inflatedIDAT, err := decompressor.InflatePrefix(image.iDAT, image.getImageDataLength())
		if err != nil {
			panic(err)
		}
		image.decode(inflatedIDAT)
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

// unfilter returns the rows of the image data without the filter type byte
// each begins with, and with their filters undone. A row holds bytesPerRow
// bytes after the filter type, and the byte of the pixel on the left is
// bytesPerPixel bytes before, or the byte before for pixels smaller than a
// byte. It panics on a filter type that PNG does not define, as libpng does.
func unfilter(buf []byte, rows, bytesPerRow, bytesPerPixel int) []byte {
	image := make([]byte, rows*bytesPerRow)
	var prior []byte // The row above, none for the first row
	for row := 0; row < rows; row++ {
		offset := row * (bytesPerRow + 1)
		filter := buf[offset]
		line := image[row*bytesPerRow : (row+1)*bytesPerRow]
		copy(line, buf[offset+1:offset+1+bytesPerRow])
		switch filter {
		case 0x00: // None
		case 0x01: // Sub
			for i := bytesPerPixel; i < bytesPerRow; i++ {
				line[i] += line[i-bytesPerPixel]
			}
		case 0x02: // Up
			if prior != nil {
				for i := 0; i < bytesPerRow; i++ {
					line[i] += prior[i]
				}
			}
		case 0x03: // Average
			for i := 0; i < bytesPerRow; i++ {
				a := 0 // The byte on the left
				if i >= bytesPerPixel {
					a = int(line[i-bytesPerPixel])
				}
				b := 0 // The byte above
				if prior != nil {
					b = int(prior[i])
				}
				line[i] += byte((a + b) / 2)
			}
		case 0x04: // Paeth
			for i := 0; i < bytesPerRow; i++ {
				a, b, c := 0, 0, 0 // Left, above and above on the left
				if i >= bytesPerPixel {
					a = int(line[i-bytesPerPixel])
				}
				if prior != nil {
					b = int(prior[i])
					if i >= bytesPerPixel {
						c = int(prior[i-bytesPerPixel])
					}
				}
				line[i] += byte(paeth(a, b, c))
			}
		default:
			panic(fmt.Sprintf("Invalid PNG filter type %d.", filter))
		}
		prior = line
	}
	return image
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

// Truecolor Image with Bit Depth == 16
func (image *pngImage) getImageColorType2BitDepth16(buf []byte) []byte {
	return unfilter(buf, image.h, 6*image.w, 6)
}

// Truecolor Image with Bit Depth == 8
func (image *pngImage) getImageColorType2BitDepth8(buf []byte) []byte {
	return unfilter(buf, image.h, 3*image.w, 3)
}

// Truecolor Image with Alpha Transparency
// getImageColorType4BitDepth8 returns the gray samples; the alpha samples go
// in the soft mask.
func (image *pngImage) getImageColorType4BitDepth8(buf []byte) []byte {
	image2 := unfilter(buf, image.h, 2*image.w, 2)
	gray := make([]byte, image.w*image.h)
	alpha := make([]byte, image.w*image.h)
	for i := range gray {
		gray[i] = image2[2*i]
		alpha[i] = image2[2*i+1]
	}
	image.deflatedAlphaData = compressor.Deflate(alpha)

	return gray
}

func (image *pngImage) getImageColorType6BitDepth8(buf []byte) []byte {
	image2 := unfilter(buf, image.h, 4*image.w, 4)
	idata := make([]byte, 3*image.w*image.h) // Image data
	alpha := make([]byte, image.w*image.h)   // Alpha values
	for i := range alpha {
		idata[3*i] = image2[4*i]
		idata[3*i+1] = image2[4*i+1]
		idata[3*i+2] = image2[4*i+2]
		alpha[i] = image2[4*i+3]
	}
	image.deflatedAlphaData = compressor.Deflate(alpha)

	return idata
}

// getImageColorType3 indexed-color image with bit depth == 1, 2, 4 or 8
// Each value is a palette index; a PLTE chunk shall appear.
// The filters are undone on the packed indexes, one byte per pixel whatever
// the bit depth, before the indexes are looked up in the palette.
func (image *pngImage) getImageColorType3(buf []byte) []byte {
	bytesPerLine := (image.w*image.bitDepth + 7) / 8
	indexes := image.paletteIndexes(unfilter(buf, image.h, bytesPerLine, 1))

	image2 := make([]byte, 3*len(indexes))
	for i, k := range indexes {
		copy(image2[3*i:3*i+3], image.pLTE[3*int(k):3*int(k)+3])
	}

	if image.tRNS != nil {
		image.deflatedAlphaData = compressor.Deflate(image.paletteAlpha(indexes))
	}

	return image2
}

// paletteIndexes returns the palette index of each pixel, a byte each, of the
// unfiltered rows of a palette image.
func (image *pngImage) paletteIndexes(rows []byte) []byte {
	bytesPerLine := (image.w*image.bitDepth + 7) / 8
	indexes := make([]byte, image.w*image.h)
	mask := (1 << image.bitDepth) - 1
	n := 0
	for row := 0; row < image.h; row++ {
		for col := 0; col < image.w; col++ {
			bit := col * image.bitDepth
			b := int(rows[row*bytesPerLine+bit/8])
			indexes[n] = byte((b >> (8 - image.bitDepth - bit%8)) & mask)
			n++
		}
	}
	return indexes
}

// paletteAlpha returns the alpha of each pixel of a palette image with a tRNS
// chunk: the alpha of its index, or opaque for an index the chunk has none for.
func (image *pngImage) paletteAlpha(indexes []byte) []byte {
	alpha := make([]byte, len(indexes))
	for i, k := range indexes {
		if int(k) < len(image.tRNS) {
			alpha[i] = image.tRNS[k]
		} else {
			alpha[i] = 0xff
		}
	}
	return alpha
}

// Grayscale Image with Bit Depth == 16
func (image *pngImage) getImageColorType0BitDepth16(buf []byte) []byte {
	return unfilter(buf, image.h, 2*image.w, 2)
}

// Grayscale Image with Bit Depth == 8
func (image *pngImage) getImageColorType0BitDepth8(buf []byte) []byte {
	return unfilter(buf, image.h, image.w, 1)
}

// getImageColorType0BitDepthBelow8 returns a grayscale image of 1, 2 or 4
// bits per pixel. The filters work on bytes, one byte per pixel whatever the
// bit depth.
func (image *pngImage) getImageColorType0BitDepthBelow8(buf []byte) []byte {
	return unfilter(buf, image.h, (image.w*image.bitDepth+7)/8, 1)
}
