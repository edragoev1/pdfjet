package pdfjet

/**
 * bmpimage.go
 *
Copyright 2020 Jonas Krogsböll

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

import (
	"io"
	"log"
	"math"
	"pdfjet/compressor"
)

// BMPImage describes BMP image object.
type BMPImage struct {
	w        int    // Image width in pixels
	h        int    // Image height in pixels
	image    []byte // The reconstructed image data
	deflated []byte // The deflated reconstructed image data
	bpp      int
	palette  [][]byte
	r5g6b5   bool // If 16 bit image two encodings can occur
}

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

// NewBMPImage constructs bitmap image objects.
func NewBMPImage(reader io.Reader) *BMPImage {
	image := new(BMPImage)

	bm := getNBytes(reader, 2)
	// From Wikipedia
	if (bm[0] == 'B' && bm[1] == 'M') ||
		(bm[0] == 'B' && bm[1] == 'A') ||
		(bm[0] == 'C' && bm[1] == 'I') ||
		(bm[0] == 'C' && bm[1] == 'P') ||
		(bm[0] == 'I' && bm[1] == 'C') ||
		(bm[0] == 'P' && bm[1] == 'T') {
		skipNBytes(reader, 8)
		offset := readSignedInt(reader)
		readSignedInt(reader) // skip sizeOfHeader
		image.w = readSignedInt(reader)
		image.h = readSignedInt(reader)
		skipNBytes(reader, 2)
		image.bpp = read2BytesLE(reader)
		compression := readSignedInt(reader)
		if image.bpp > 8 {
			image.r5g6b5 = (compression == 3)
			skipNBytes(reader, 20)
			if offset > 54 {
				skipNBytes(reader, offset-54)
			}
		} else {
			skipNBytes(reader, 12)
			numpalcol := readSignedInt(reader)
			if numpalcol == 0 {
				numpalcol = int(math.Pow(2, float64(image.bpp)))
			}
			skipNBytes(reader, 4)
			image.parsePalette(reader, numpalcol)
		}
		image.parseData(reader)
	} else {
		log.Fatal("BMP data could not be parsed!")
	}

	return image
}

func (image *BMPImage) parseData(reader io.Reader) []byte {
	// rowsize is 4 * ceil (bpp*width/32.0)
	bmpImage := make([]byte, 3*image.w*image.h)

	rowsize := 4 * int(math.Ceil(float64(image.bpp)*float64(image.w)/float64(32.0))) // 4 byte alignment
	row := make([]byte, 0)
	index := 0
	for i := 0; i < image.h; i++ {
		row = getNBytes(reader, rowsize)
		switch image.bpp {
		case 1:
			row = image.bit1to8(row, image.w)
			break // opslag i palette
		case 4:
			row = image.bit4to8(row, image.w)
			break // opslag i palette
		case 8:
			break // opslag i palette
		case 16:
			if image.r5g6b5 {
				row = image.bit16to24(row, image.w) // 5,6,5 bit
			} else {
				row = image.bit16to24b(row, image.w)
			}
			break
		case 24:
			break // bytes are correct
		case 32:
			row = image.bit32to24(row, image.w)
			break
		default:
			log.Fatal("Can only parse 1 bit, 4bit, 8bit, 16bit, 24bit and 32bit images.")
		}

		index = image.w * (image.h - i - 1) * 3
		if image.palette != nil { // indexed
			for j := 0; j < image.w; j++ {
				if row[j] < 0 {
					bmpImage[index] = image.palette[int(row[j])+256][2]
				} else {
					bmpImage[index] = image.palette[row[j]][2]
				}
				index++

				if row[j] < 0 {
					bmpImage[index] = image.palette[int(row[j])+256][1]
				} else {
					bmpImage[index] = image.palette[row[j]][1]
				}
				index++

				if row[j] < 0 {
					bmpImage[index] = image.palette[int(row[j])+256][0]
				} else {
					bmpImage[index] = image.palette[row[j]][0]
				}
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

// 5 + 6 + 5 in B G R format 2 bytes to 3 bytes
func (image *BMPImage) bit16to24(row []byte, width int) []byte {
	ret := make([]byte, 3*width)
	j := 0
	for i := 0; i < 2*width; i += 2 {
		ret[j] = (byte)((row[i] & 0x1F) << 3)
		j++
		ret[j] = (byte)(((row[i+1] & 0x07) << 5) + ((row[i] & 0xE0) >> 3))
		j++
		ret[j] = (byte)((row[i+1] & 0xF8))
		j++
	}
	return ret
}

// 5 + 5 + 5 in B G R format 2 bytes to 3 bytes
func (image *BMPImage) bit16to24b(row []byte, width int) []byte {
	ret := make([]byte, 3*width)
	j := 0
	for i := 0; i < 2*width; i += 2 {
		ret[j] = byte((row[i] & 0x1F) << 3)
		j++
		ret[j] = byte(((row[i+1] & 0x03) << 6) + ((row[i] & 0xE0) >> 2))
		j++
		ret[j] = byte((row[i+1] & 0x7C) << 1)
		j++
	}
	return ret
}

/* alpha first? */
func (image *BMPImage) bit32to24(row []byte, width int) []byte {
	ret := make([]byte, 3*width)
	j := 0
	for i := 0; i < width*4; i += 4 {
		ret[j] = row[i+1]
		j++
		ret[j] = row[i+2]
		j++
		ret[j] = row[i+3]
		j++
	}
	return ret
}

func (image *BMPImage) bit4to8(row []byte, width int) []byte {
	ret := make([]byte, width)
	for i := 0; i < width; i++ {
		if i%2 == 0 {
			ret[i] = (byte)((row[i/2] & m11110000) >> 4)
		} else {
			ret[i] = (byte)((row[i/2] & m00001111))
		}
	}
	return ret
}

func (image *BMPImage) bit1to8(row []byte, width int) []byte {
	ret := make([]byte, width)
	for i := 0; i < width; i++ {
		switch i % 8 {
		case 0:
			ret[i] = byte((row[i/8] & m10000000) >> 7)
			break
		case 1:
			ret[i] = byte((row[i/8] & m01000000) >> 6)
			break
		case 2:
			ret[i] = byte((row[i/8] & m00100000) >> 5)
			break
		case 3:
			ret[i] = byte((row[i/8] & m00010000) >> 4)
			break
		case 4:
			ret[i] = byte((row[i/8] & m00001000) >> 3)
			break
		case 5:
			ret[i] = byte((row[i/8] & m00000100) >> 2)
			break
		case 6:
			ret[i] = byte((row[i/8] & m00000010) >> 1)
			break
		case 7:
			ret[i] = byte((row[i/8] & m00000001))
			break
		}
	}
	return ret
}

func (image *BMPImage) parsePalette(reader io.Reader, size int) {
	image.palette = make([][]byte, size)
	for i := 0; i < size; i++ {
		image.palette[i] = getNBytes(reader, 4)
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
	return int(val)
}

// GetWidth returns the image width.
func (image *BMPImage) GetWidth() float32 {
	return float32(image.w)
}

// GetHeight returns the image height.
func (image *BMPImage) GetHeight() float32 {
	return float32(image.h)
}

// GetData returns the compressed image data.
func (image *BMPImage) GetData() []byte {
	return image.deflated
}
