// imagesize.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"errors"
	"fmt"
	"hash/crc32"
	"io"
	"math"
	"os"

	"github.com/edragoev1/pdfjet/v9/src/internal/decompressor"
	"github.com/edragoev1/pdfjet/v9/src/internal/imagetype"
)

// ImageSize is the size an image is drawn at, and its pixels, read from the
// header of its file alone, without its image data in memory: the size of a
// page laid out before its images are drawn, and an image refused early, for
// its size or for a header that NewImage refuses, before a byte of its image
// data is decoded (9 October 2026).
//
// The size is the one NewImage gives the image: its pixels, or the physical
// size the file asks for, the pHYs chunk of a PNG, the JFIF density of a JPEG
// or the pixels per metre of a BMP; a JPEG turned a quarter of the way by its
// Exif orientation is its height by its width. What only the image data
// shows, a JPEG cut short or the rows of a PNG, is NewImage's to refuse.
type ImageSize struct {
	imageType   imagetype.ImageType
	width       float32
	height      float32
	pixelWidth  int
	pixelHeight int
}

// ReadImageSizeFromFile reads the size of the PNG, JPEG or BMP file at the
// path, as ReadImageSize does.
func ReadImageSizeFromFile(filePath string) (*ImageSize, error) {
	file, err := os.Open(filePath)
	if err != nil {
		return nil, err
	}
	defer file.Close()
	return ReadImageSize(file)
}

// ReadImageSize reads the size of the PNG, JPEG or BMP image of the reader
// from its header, or returns why NewImage would refuse the image, as far as
// its header shows. A JPEG is read to its frame header, a BMP to its pixels
// per metre, and a PNG to its end, a chunk at a time, its image data checked
// and passed over, as a pHYs chunk may come after it.
func ReadImageSize(reader io.Reader) (*ImageSize, error) {
	r := bufio.NewReader(reader)
	head, err := r.Peek(4)
	if err != nil {
		return nil, errors.New("The image is not a PNG, JPEG or BMP file.")
	}
	switch {
	case head[0] == 0x89 && head[1] == 'P' && head[2] == 'N' && head[3] == 'G':
		return readPNGSize(r)
	case head[0] == 0xFF && head[1] == 0xD8:
		return readJPGSize(r)
	case head[0] == 'B' && head[1] == 'M':
		return readBMPSize(r)
	}
	return nil, errors.New("The image is not a PNG, JPEG or BMP file.")
}

// GetWidth returns the width the image is drawn at, in points, as Image's
// GetWidth before it is scaled.
func (size *ImageSize) GetWidth() float32 {
	return size.width
}

// GetHeight returns the height the image is drawn at, in points, as Image's
// GetHeight before it is scaled.
func (size *ImageSize) GetHeight() float32 {
	return size.height
}

// GetPixelWidth returns the width of the image in pixels, as stored.
func (size *ImageSize) GetPixelWidth() int {
	return size.pixelWidth
}

// GetPixelHeight returns the height of the image in pixels, as stored.
func (size *ImageSize) GetPixelHeight() int {
	return size.pixelHeight
}

// newImageSize returns the size of an image of the pixels, drawn at the
// physical size when it has one, as Image's setPhysicalSize draws it.
func newImageSize(imageType imagetype.ImageType, pixelWidth, pixelHeight int, physicalWidth, physicalHeight float32) *ImageSize {
	size := &ImageSize{imageType: imageType, pixelWidth: pixelWidth, pixelHeight: pixelHeight,
		width: float32(pixelWidth), height: float32(pixelHeight)}
	if physicalWidth > 0.0 && physicalHeight > 0.0 {
		size.width = physicalWidth
		size.height = physicalHeight
	}
	return size
}

// readPNGSize reads the chunks of a PNG as newPNGImage does, each with its
// CRC, its image data passed over rather than kept, and checks its header as
// getImageDataLength does.
func readPNGSize(r *bufio.Reader) (*ImageSize, error) {
	signature := make([]byte, 8)
	if _, err := io.ReadFull(r, signature); err != nil ||
		string(signature) != "\x89PNG\r\n\x1a\n" {
		return nil, errors.New("Wrong PNG signature.")
	}
	image := &pngImage{bitDepth: 8}
	hasIDAT, hasPLTE := false, false
	for {
		var head [8]byte
		if _, err := io.ReadFull(r, head[:]); err != nil {
			return nil, errors.New("Unexpected end of the PNG stream.")
		}
		length := toUint32(head[:], 0)
		if length > math.MaxInt32 {
			return nil, fmt.Errorf("Invalid PNG chunk length %d.", length)
		}
		chunkType := string(head[4:])
		crc := crc32.NewIEEE()
		crc.Write(head[4:])
		// The IHDR chunk and a pHYs chunk of their length are read; the
		// image data and the others are passed over, through the CRC alone,
		// so that a chunk of any length takes no memory
		if chunkType == "IHDR" && length != 13 {
			return nil, errors.New("Invalid PNG IHDR chunk.")
		}
		var data []byte
		if chunkType == "IHDR" || (chunkType == "pHYs" && length == 9) {
			data = make([]byte, length)
			if _, err := io.ReadFull(r, data); err != nil {
				return nil, errors.New("Unexpected end of the PNG stream.")
			}
			crc.Write(data)
		} else if n, err := io.CopyN(crc, r, int64(length)); err != nil || n != int64(length) {
			return nil, errors.New("Unexpected end of the PNG stream.")
		}
		var tail [4]byte
		if _, err := io.ReadFull(r, tail[:]); err != nil {
			return nil, errors.New("Unexpected end of the PNG stream.")
		}
		if crc.Sum32() != toUint32(tail[:], 0) {
			return nil, errors.New("pngImage chunk has bad CRC.")
		}
		switch chunkType {
		case "IEND":
			return pngSizeOf(image, hasIDAT, hasPLTE)
		case "IHDR":
			image.w = int(toUint32(data, 0))
			image.h = int(toUint32(data, 4))
			image.bitDepth = int(data[8])
			image.colorType = int(data[9])
			if data[10] != 0 {
				return nil, errors.New("Unknown PNG compression method.")
			}
			if data[11] != 0 {
				return nil, errors.New("Unknown PNG filter method.")
			}
			if data[12] == 1 {
				return nil, errors.New("Interlaced PNG images are not supported.")
			}
		case "IDAT":
			// An empty one holds none of the image data (the review of
			// 9 October 2026)
			hasIDAT = hasIDAT || length > 0
		case "PLTE":
			if length%3 != 0 || length == 0 || length > 3*256 {
				return nil, errors.New("Incorrect palette length.")
			}
			hasPLTE = true
		case "pHYs":
			image.readPhysicalSize(data)
		}
	}
}

// pngSizeOf checks the header of the PNG read, as getImageDataLength does,
// and returns its size.
func pngSizeOf(image *pngImage, hasIDAT, hasPLTE bool) (size *ImageSize, err error) {
	if hasPLTE {
		image.pLTE = []byte{}
	}
	defer func() {
		if e := recover(); e != nil {
			size, err = nil, fmt.Errorf("%v", e)
		}
	}()
	image.getImageDataLength()
	if !hasIDAT {
		return nil, errors.New("The PNG image has no image data.")
	}
	return newImageSize(imagetype.PNG, image.w, image.h, image.physicalWidth, image.physicalHeight), nil
}

// readJPGSize reads the segments of a JPEG to its frame header, as
// readJPGImage does: the density of its JFIF segment, the orientation of its
// Exif segment, and its frame header, checked as readJPGImage checks it. A
// segment is held at most, 64 KB.
func readJPGSize(r *bufio.Reader) (*ImageSize, error) {
	var soi [2]byte
	if _, err := io.ReadFull(r, soi[:]); err != nil {
		return nil, errors.New("Error: Invalid JPEG header.")
	}
	image := new(jpgImage)
	for {
		ch, err := nextJPGMarker(r)
		if err != nil {
			return nil, err
		}
		if ch == mTEM || ch == mSOI || (ch >= mRST0 && ch <= mRST7) {
			continue
		}
		if ch == mEOI {
			return nil, errors.New("Error: The JPEG ends before its frame header.")
		}
		switch ch {
		case mSOF3, mSOF5, mSOF6, mSOF7, mSOF9, mSOF10, mSOF11, mSOF13, mSOF14, mSOF15:
			return nil, fmt.Errorf(
				"Error: The JPEG is lossless, hierarchical or arithmetic coded (SOF%d), "+
					"which a PDF reader cannot decode.", ch-mSOF0)
		case mSOF0, mSOF1, mSOF2:
			// The length, then the precision, the height, the width and
			// the components, as readJPGImage reads them; the rest of the
			// frame header, the components' specifications, is not read, as
			// in the other ports (the review of 9 October 2026)
			segment := make([]byte, 2+6)
			if _, err := io.ReadFull(r, segment); err != nil {
				return nil, io.ErrUnexpectedEOF
			}
			length := int(segment[0])<<8 | int(segment[1])
			if precision := segment[2]; precision != 8 {
				return nil, fmt.Errorf("Error: The JPEG has %d bits per color component, not 8.", precision)
			}
			image.height = uint16(segment[3])<<8 | uint16(segment[4])
			image.width = uint16(segment[5])<<8 | uint16(segment[6])
			components := segment[7]
			if image.width == 0 || image.height == 0 || (components != 1 && components != 3 && components != 4) {
				return nil, errors.New("Error: Invalid JPEG dimensions or component count.")
			}
			if length != 3*int(components)+8 {
				return nil, fmt.Errorf(
					"Error: The JPEG frame header is %d bytes, not the %d of its %d color components.",
					length, 3*int(components)+8, components)
			}
			size := newImageSize(imagetype.JPG, int(image.width), int(image.height),
				image.GetPhysicalWidth(), image.GetPhysicalHeight())
			// Turned a quarter of the way, as Image's setOrientation turns it
			if image.orientation >= 5 && image.orientation <= 8 {
				size.width, size.height = size.height, size.width
			}
			return size, nil
		case mAPP0, mAPP1:
			segment, err := readJPGSegment(r)
			if err != nil {
				return nil, err
			}
			image.index = 0
			if ch == mAPP0 {
				err = image.readAPP0(segment)
			} else {
				err = image.readAPP1(segment)
			}
			if err != nil {
				return nil, err
			}
		default:
			if _, err := readJPGSegment(r); err != nil {
				return nil, err
			}
		}
	}
}

// nextJPGMarker finds the next marker of the stream, as jpgImage's nextMarker
// finds it in a buffer: bytes that are not 0xFF passed over, 0xFF bytes of
// padding swallowed, and a 0xFF that a zero byte follows taken as data.
func nextJPGMarker(r *bufio.Reader) (uint8, error) {
	for {
		ch, err := r.ReadByte()
		for err == nil && ch != 0xFF {
			ch, err = r.ReadByte()
		}
		for err == nil && ch == 0xFF {
			ch, err = r.ReadByte()
		}
		if err != nil {
			return 0, io.ErrUnexpectedEOF
		}
		if ch != 0x00 {
			return ch, nil
		}
	}
}

// readJPGSegment reads a segment of a JPEG: its length, which counts itself,
// and the rest of it, returned with the length first.
func readJPGSegment(r *bufio.Reader) ([]byte, error) {
	var head [2]byte
	if _, err := io.ReadFull(r, head[:]); err != nil {
		return nil, io.ErrUnexpectedEOF
	}
	length := int(head[0])<<8 | int(head[1])
	if length < 2 {
		return nil, errors.New("Error: Length includes itself, so must be at least 2.")
	}
	segment := make([]byte, length)
	copy(segment, head[:])
	if _, err := io.ReadFull(r, segment[2:]); err != nil {
		return nil, io.ErrUnexpectedEOF
	}
	return segment, nil
}

// readBMPSize reads the header of a BMP to its pixels per metre, as
// newBMPImage reads it, with its checks.
func readBMPSize(r *bufio.Reader) (size *ImageSize, err error) {
	defer func() {
		if e := recover(); e != nil {
			size, err = nil, fmt.Errorf("%v", e)
		}
	}()
	header := make([]byte, 50) // To the colors used, which the palette's size checks
	if _, err := io.ReadFull(r, header); err != nil {
		return nil, errors.New("Unexpected end of the BMP stream.")
	}
	le := func(at int) int {
		return int(int32(uint32(header[at]) | uint32(header[at+1])<<8 | uint32(header[at+2])<<16 | uint32(header[at+3])<<24))
	}
	if headerSize := le(14); headerSize < 40 {
		return nil, fmt.Errorf("Unsupported BMP header of %d bytes.", headerSize)
	}
	image := new(bmpImage)
	image.w = le(18)
	image.h = le(22)
	if image.h < 0 {
		image.h = -image.h
	}
	image.bpp = int(header[28]) | int(header[29])<<8
	compression := le(30)
	if image.w <= 0 || image.h <= 0 || image.h > math.MaxInt32 {
		return nil, errors.New("Invalid BMP image size.")
	}
	switch image.bpp {
	case 1, 4, 8, 16, 24, 32:
	default:
		return nil, errors.New("Can only parse 1 bit, 4bit, 8bit, 16bit, 24bit and 32bit images")
	}
	if compression != biRGB && !(compression == biBitfields && (image.bpp == 16 || image.bpp == 32)) {
		return nil, errors.New("Compressed BMP images are not supported.")
	}
	rowSize := 4 * ((int64(image.bpp)*int64(image.w) + 31) / 32)
	if int64(image.h) > decompressor.MaxDecodedLength ||
		3*int64(image.w)*int64(image.h) > decompressor.MaxDecodedLength ||
		rowSize*int64(image.h) > decompressor.MaxDecodedLength {
		return nil, fmt.Errorf("The BMP image is larger than %d bytes.", decompressor.MaxDecodedLength)
	}
	// The size of the palette, refused by NewImage as here (the review of
	// 9 October 2026: a palette of 1,000 colors was a size)
	if image.bpp <= 8 {
		colors := le(46)
		if colors == 0 {
			colors = 1 << image.bpp
		}
		if colors < 0 || colors > 256 {
			return nil, fmt.Errorf("Invalid BMP palette size %d.", colors)
		}
	}
	image.setPhysicalSize(le(38), le(42))
	return newImageSize(imagetype.BMP, image.w, image.h, image.physicalWidth, image.physicalHeight), nil
}
