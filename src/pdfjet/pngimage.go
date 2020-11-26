package pdfjet

/**
 * pngimage.go
 *
Copyright 2020 Innovatics Inc.

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
	"compressor"
	"crc32util"
	"decompressor"
	"fmt"
	"io"
	"log"
	"math"
)

// PNGImage is used to embed PNG images in the PDF document.
// <p>
// <strong>Please note:</strong>
// <p>
//   Interlaced images are not supported.
// <p>
//   To convert interlaced image to non-interlaced image use OptiPNG:
// <p>
//   optipng -i0 -o7 myimage.png
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
	image.iDAT = make([]byte, 0)

	image.validatePNG(reader)

	chunks := image.processPNG(reader)

	for _, chunk := range chunks {
		chunkType := string(chunk.ChunkType)
		if chunkType == "IHDR" {
			image.w = int(toUint32(chunk.ChunkData, 0)) // Width
			image.h = int(toUint32(chunk.ChunkData, 4)) // Height
			image.bitDepth = int(chunk.ChunkData[8])    // Bit Depth
			image.colorType = int(chunk.ChunkData[9])   // Color Type

			// fmt.Println(
			//         "Bit Depth == " + chunk.getData()[8])
			// fmt.Println(
			//         "Color Type == " + chunk.getData()[9])
			// fmt.Println(chunk.getData()[10])
			// fmt.Println(chunk.getData()[11])
			// fmt.Println(chunk.getData()[12])

			if chunk.ChunkData[12] == 1 {
				fmt.Println("Interlaced PNG images are not supported.")
				fmt.Println("Convert the image using OptiPNG:\noptipng -i0 -o7 myimage.png")
			}
		} else if chunkType == "IDAT" {
			image.iDAT = append(image.iDAT, chunk.ChunkData...)
		} else if chunkType == "PLTE" {
			image.pLTE = chunk.ChunkData
			if len(image.pLTE)%3 != 0 {
				log.Fatal("Incorrect palette length.")
			}
		} else if chunkType == "gAMA" {
			// fmt.Println("gAMA chunk found!")
		} else if chunkType == "tRNS" {
			if image.colorType == 3 {
				image.tRNS = chunk.ChunkData
			}
		} else if chunkType == "cHRM" {
			// fmt.Println("cHRM chunk found!")
		} else if chunkType == "sBIT" {
			// fmt.Println("sBIT chunk found!")
		} else if chunkType == "bKGD" {
			// fmt.Println("bKGD chunk found!")
		}
	}

	// Decompress the IDAT chunk data.
	inflatedIDAT := decompressor.Inflate(image.iDAT)

	var imageData []byte
	if image.colorType == 0 {
		// Grayscale Image
		if image.bitDepth == 16 {
			imageData = image.getImageColorType0BitDepth16(inflatedIDAT)
		} else if image.bitDepth == 8 {
			imageData = image.getImageColorType0BitDepth8(inflatedIDAT)
		} else if image.bitDepth == 4 {
			imageData = image.getImageColorType0BitDepth4(inflatedIDAT)
		} else if image.bitDepth == 2 {
			imageData = image.getImageColorType0BitDepth2(inflatedIDAT)
		} else if image.bitDepth == 1 {
			imageData = image.getImageColorType0BitDepth1(inflatedIDAT)
		} else {
			log.Fatal("Image with unsupported bit depth == " + fmt.Sprint(image.bitDepth))
		}
	} else if image.colorType == 6 {
		if image.bitDepth == 8 {
			imageData = image.getImageColorType6BitDepth8(inflatedIDAT)
		} else {
			log.Fatal("Image with unsupported bit depth == " + fmt.Sprint(image.bitDepth))
		}
	} else {
		// Color Image
		if image.pLTE == nil {
			// Trucolor Image
			if image.bitDepth == 16 {
				imageData = image.getImageColorType2BitDepth16(inflatedIDAT)
			} else {
				imageData = image.getImageColorType2BitDepth8(inflatedIDAT)
			}
		} else {
			// Indexed Image
			if image.bitDepth == 8 {
				imageData = image.getImageColorType3BitDepth8(inflatedIDAT)
			} else if image.bitDepth == 4 {
				imageData = image.getImageColorType3BitDepth4(inflatedIDAT)
			} else if image.bitDepth == 2 {
				imageData = image.getImageColorType3BitDepth2(inflatedIDAT)
			} else if image.bitDepth == 1 {
				imageData = image.getImageColorType3BitDepth1(inflatedIDAT)
			} else {
				log.Fatal("Image with unsupported bit depth == " + fmt.Sprint(image.bitDepth))
			}
		}
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

func (image *PNGImage) processPNG(reader io.Reader) []*Chunk {
	chunks := make([]*Chunk, 0)
	for {
		chunk := image.getChunk(reader)
		if string(chunk.ChunkType) == "IEND" {
			break
		}
		chunks = append(chunks, chunk)
	}
	return chunks
}

func (image *PNGImage) validatePNG(reader io.Reader) {
	buf := make([]byte, 8)
	if _, err := io.ReadFull(reader, buf); err != nil {
		log.Fatal("File is too short!")
	}

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
		log.Fatal("Wrong PNG signature.")
	}
}

func (image *PNGImage) getChunk(reader io.Reader) *Chunk {
	chunk := NewChunk()
	chunk.ChunkLength = getUint32(reader)                       // The length of the data chunk.
	chunk.ChunkType = getNBytes(reader, 4)                      // The chunk type.
	chunk.ChunkData = getNBytes(reader, int(chunk.ChunkLength)) // The chunk data.
	chunk.ChunkCRC = getUint32(reader)                          // CRC of the type and data chunks.

	crc32 := crc32util.NewCRC32()
	crc32.Update(chunk.ChunkType)
	crc32.Update(chunk.ChunkData)
	if crc32.GetValue() != chunk.ChunkCRC {
		log.Fatal("PNGImage chunk has bad CRC.")
	}
	return chunk
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

// getImageColorType3BitDepth8 indexed image with bit depth == 8
// Each value is a palette index; a PLTE chunk shall appear.
func (image *PNGImage) getImageColorType3BitDepth8(buf []byte) []byte {
	image2 := make([]byte, 3*(image.w*image.h))

	filters := make([]byte, image.h)
	var alpha []byte
	if image.tRNS != nil {
		alpha = make([]byte, image.w*image.h)
		for i := 0; i < len(alpha); i++ {
			alpha[i] = 0xff
		}
	}

	bytesPerLine := image.w + 1
	n := 0
	j := 0
	for i := 0; i < len(buf); i++ {
		if i%bytesPerLine == 0 {
			filters = append(filters, buf[i])
		} else {
			k := int(buf[i] & 0xff)
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
	applyFilters(filters, image2, image.w, image.h, 3)

	if image.tRNS != nil {
		image.deflatedAlphaData = compressor.Deflate(alpha)
	}

	return image2
}

// Indexed Image with Bit Depth == 4
func (image *PNGImage) getImageColorType3BitDepth4(buf []byte) []byte {
	image2 := make([]byte, 6*(len(buf)-image.h))

	bytesPerLine := image.w/2 + 1
	if image.w%2 > 0 {
		bytesPerLine++
	}

	j := 0
	k := 0
	for i := 0; i < len(buf); i++ {
		if i%bytesPerLine == 0 {
			// Skip the filter byte.
			continue
		}

		l := int(buf[i])

		k = 3 * ((l >> 4) & 0x0000000f)
		image2[j] = image.pLTE[k]
		j++
		image2[j] = image.pLTE[k+1]
		j++
		image2[j] = image.pLTE[k+2]
		j++

		if (j % (3 * image.w)) == 0 {
			continue
		}

		k = 3 * (l & 0x0000000f)
		image2[j] = image.pLTE[k]
		j++
		image2[j] = image.pLTE[k+1]
		j++
		image2[j] = image.pLTE[k+2]
		j++
	}

	return image2
}

// Indexed Image with Bit Depth == 2
func (image *PNGImage) getImageColorType3BitDepth2(buf []byte) []byte {
	image2 := make([]byte, 12*(len(buf)-image.h))

	bytesPerLine := image.w/4 + 1
	if image.w%4 > 0 {
		bytesPerLine++
	}

	j := 0
	k := 0
	for i := 0; i < len(buf); i++ {
		if (i % bytesPerLine) == 0 {
			// Skip the filter byte.
			continue
		}

		l := int(buf[i])

		k = 3 * ((l >> 6) & 0x00000003)
		image2[j] = image.pLTE[k]
		j++
		image2[j] = image.pLTE[k+1]
		j++
		image2[j] = image.pLTE[k+2]
		j++

		if j%(3*image.w) == 0 {
			continue
		}

		k = 3 * ((l >> 4) & 0x00000003)
		image2[j] = image.pLTE[k]
		j++
		image2[j] = image.pLTE[k+1]
		j++
		image2[j] = image.pLTE[k+2]
		j++

		if j%(3*image.w) == 0 {
			continue
		}

		k = 3 * ((l >> 2) & 0x00000003)
		image2[j] = image.pLTE[k]
		j++
		image2[j] = image.pLTE[k+1]
		j++
		image2[j] = image.pLTE[k+2]
		j++

		if j%(3*image.w) == 0 {
			continue
		}

		k = 3 * (l & 0x00000003)
		image2[j] = image.pLTE[k]
		j++
		image2[j] = image.pLTE[k+1]
		j++
		image2[j] = image.pLTE[k+2]
		j++
	}

	return image2
}

// Indexed Image with Bit Depth == 1
func (image *PNGImage) getImageColorType3BitDepth1(buf []byte) []byte {
	image2 := make([]byte, 24*(len(buf)-image.h))

	bytesPerLine := image.w/8 + 1
	if image.w%8 > 0 {
		bytesPerLine++
	}

	k := 0
	j := 0
	for i := 0; i < len(buf); i++ {
		if i%bytesPerLine == 0 {
			// Skip the filter byte.
			continue
		}

		l := int(buf[i])

		k = 3 * ((l >> 7) & 0x00000001)
		image2[j] = image.pLTE[k]
		j++
		image2[j] = image.pLTE[k+1]
		j++
		image2[j] = image.pLTE[k+2]
		j++

		if j%(3*image.w) == 0 {
			continue
		}

		k = 3 * ((l >> 6) & 0x00000001)
		image2[j] = image.pLTE[k]
		j++
		image2[j] = image.pLTE[k+1]
		j++
		image2[j] = image.pLTE[k+2]
		j++

		if j%(3*image.w) == 0 {
			continue
		}

		k = 3 * ((l >> 5) & 0x00000001)
		image2[j] = image.pLTE[k]
		j++
		image2[j] = image.pLTE[k+1]
		j++
		image2[j] = image.pLTE[k+2]
		j++

		if j%(3*image.w) == 0 {
			continue
		}

		k = 3 * ((l >> 4) & 0x00000001)
		image2[j] = image.pLTE[k]
		j++
		image2[j] = image.pLTE[k+1]
		j++
		image2[j] = image.pLTE[k+2]
		j++

		if j%(3*image.w) == 0 {
			continue
		}

		k = 3 * ((l >> 3) & 0x00000001)
		image2[j] = image.pLTE[k]
		j++
		image2[j] = image.pLTE[k+1]
		j++
		image2[j] = image.pLTE[k+2]
		j++

		if j%(3*image.w) == 0 {
			continue
		}

		k = 3 * ((l >> 2) & 0x00000001)
		image2[j] = image.pLTE[k]
		j++
		image2[j] = image.pLTE[k+1]
		j++
		image2[j] = image.pLTE[k+2]
		j++

		if j%(3*image.w) == 0 {
			continue
		}

		k = 3 * ((l >> 1) & 0x00000001)
		image2[j] = image.pLTE[k]
		j++
		image2[j] = image.pLTE[k+1]
		j++
		image2[j] = image.pLTE[k+2]
		j++

		if j%(3*image.w) == 0 {
			continue
		}

		k = 3 * (l & 0x00000001)
		image2[j] = image.pLTE[k]
		j++
		image2[j] = image.pLTE[k+1]
		j++
		image2[j] = image.pLTE[k+2]
		j++
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

	bytesPerLine := image.w/2 + 1
	if image.w%2 > 0 {
		bytesPerLine++
	}

	j := 0
	for i := 0; i < len(buf); i++ {
		if i%bytesPerLine == 0 {
			continue
		}
		image2[j] = buf[i]
		j++
	}

	return image2
}

// Grayscale Image with Bit Depth == 2
func (image *PNGImage) getImageColorType0BitDepth2(buf []byte) []byte {
	image2 := make([]byte, len(buf)-image.h)

	bytesPerLine := image.w/4 + 1
	if image.w%4 > 0 {
		bytesPerLine++
	}

	j := 0
	for i := 0; i < len(buf); i++ {
		if i%bytesPerLine == 0 {
			continue
		}
		image2[j] = buf[i]
		j++
	}

	return image2
}

// Grayscale Image with Bit Depth == 1
func (image *PNGImage) getImageColorType0BitDepth1(buf []byte) []byte {
	image2 := make([]byte, len(buf)-image.h)

	bytesPerLine := image.w/8 + 1
	if image.w%8 > 0 {
		bytesPerLine++
	}

	j := 0
	for i := 0; i < len(buf); i++ {
		if i%bytesPerLine != 0 {
			image2[j] = buf[i]
			j++
		}
	}

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
			if filter == 0x01 { // Sub
				image[index] += byte(a)
			} else if filter == 0x02 { // Up
				image[index] += byte(b)
			} else if filter == 0x03 { // Average
				image[index] += byte(math.Floor(float64(a+b) / 2.0))
			} else if filter == 0x04 { // Paeth
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

func (image *PNGImage) appendIdatChunk(array1, array2 []byte) []byte {
	if array1 == nil {
		return array2
	} else if array2 == nil {
		return array1
	}
	return append(array1, array2...)
}

/*
func (image *PNGImage) static void main(String[] args) {
    FileInputStream fis = new FileInputStream(args[0])
    PNGImage png = new PNGImage(fis)
    byte[] image = png.getData()
    byte[] alpha = png.getAlpha()
    int w = png.getWidth()
    int h = png.getHeight()
    int c = png.getColorType()
    fis.close()

    String fileName = args[0].substring(0, args[0].lastIndexOf("."))
    FileOutputStream fos = new FileOutputStream(fileName + ".jet")
    BufferedOutputStream bos = new BufferedOutputStream(fos)
    writeInt(w, bos);   // Width
    writeInt(h, bos);   // Height
    bos.write(c);       // Color Space
    if alpha != null {
        bos.write(1)
        writeInt(alpha.length, bos)
        bos.write(alpha)
    } else {
        bos.write(0)
    }
    writeInt(image.length, bos)
    bos.write(image)
    bos.flush()
    bos.close()
}

func (image *PNGImage) static void writeInt(int i, OutputStream os) throws IOException {
    os.write((i >> 24) & 0xff)
    os.write((i >> 16) & 0xff)
    os.write((i >>  8) & 0xff)
    os.write((i >>  0) & 0xff)
}
*/
