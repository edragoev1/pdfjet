package pdfjet

/**
 * jpgimage.go
 *
 * The authors make NO WARRANTY or representation, either express or implied,
 * with respect to this software, its quality, accuracy, merchantability, or
 * fitness for a particular purpose.  This software is provided "AS IS", and you,
 * its user, assume the entire risk as to its quality and accuracy.
 *
 * This software is copyright (C) 1991-1998, Thomas G. Lane.
 * All Rights Reserved except as specified below.
 *
 * Permission is hereby granted to use, copy, modify, and distribute this
 * software (or portions thereof) for any purpose, without fee, subject to these
 * conditions:
 * (1) If any part of the source code for this software is distributed, then this
 * README file must be included, with this copyright and no-warranty notice
 * unaltered; and any additions, deletions, or changes to the original files
 * must be clearly indicated in accompanying documentation.
 * (2) If only executable code is distributed, then the accompanying
 * documentation must state that "this software is based in part on the work of
 * the Independent JPEG Group".
 * (3) Permission for use of this software is granted only if the user accepts
 * full responsibility for any undesirable consequences; the authors accept
 * NO LIABILITY for damages of any kind.
 *
 * These conditions apply to any software derived from or based on the IJG code,
 * not just to the unmodified library.  If you use our work, you ought to
 * acknowledge us.
 *
 * Permission is NOT granted for the use of any IJG author's name or company name
 * in advertising or publicity relating to this software or products derived from
 * it.  This software may be referred to only as "the Independent JPEG Group's
 * software".
 *
 * We specifically permit and encourage the use of this software as the basis of
 * commercial products, provided that all warranty or liability claims are
 * assumed by the product vendor.
 */

import (
	"io"
	"io/ioutil"
	"log"
)

// JPGImage describes JPG image object.
type JPGImage struct {
	width           uint16
	height          uint16
	colorComponents uint8
	data            []byte
	index           int
}

// Constants
const (
	mSOF0  = uint8(0xC0) // Start Of Frame N
	mSOF1  = uint8(0xC1) // N indicates which compression process
	mSOF2  = uint8(0xC2) // Only SOF0-SOF2 are now in common use
	mSOF3  = uint8(0xC3)
	mSOF5  = uint8(0xC5) // NB: codes C4 and CC are NOT SOF markers
	mSOF6  = uint8(0xC6)
	mSOF7  = uint8(0xC7)
	mSOF9  = uint8(0xC9)
	mSOF10 = uint8(0xCA)
	mSOF11 = uint8(0xCB)
	mSOF13 = uint8(0xCD)
	mSOF14 = uint8(0xCE)
	mSOF15 = uint8(0xCF)
)

// NewJPGImage is the constructor.
func NewJPGImage(reader io.Reader) *JPGImage {
	image := new(JPGImage)
	buf, err := ioutil.ReadAll(reader)
	if err != nil {
		log.Fatal(err)
	}
	image.data = buf
	return image.readJPGImage(image.data)
}

// GetWidth returns the width of the image.
func (image *JPGImage) GetWidth() float32 {
	return float32(image.width)
}

// GetHeight returns the height of the image.
func (image *JPGImage) GetHeight() float32 {
	return float32(image.height)
}

// GetFileSize returns the file size of the image.
func (image *JPGImage) GetFileSize() uint64 {
	return uint64(len(image.data))
}

// GetColorComponents returns the color components of the image.
func (image *JPGImage) GetColorComponents() uint8 {
	return image.colorComponents
}

// GetData returns the image data.
func (image *JPGImage) GetData() []byte {
	return image.data
}

func (image *JPGImage) readJPGImage(buffer []byte) *JPGImage {
	if buffer[image.index] != 0xFF || buffer[image.index+1] != 0xD8 {
		log.Fatal("Error: Invalid JPEG header.")
	}

	image.index += 2
	for {
		ch := image.nextMarker(buffer)
		// Note that marker codes 0xC4, 0xC8, 0xCC are not,
		// and must not be treated as SOFn. C4 in particular
		// is actually DHT.
		if ch == mSOF0 || // Baseline
			ch == mSOF1 || // Extended sequential, Huffman
			ch == mSOF2 || // Progressive, Huffman
			ch == mSOF3 || // Lossless, Huffman
			ch == mSOF5 || // Differential sequential, Huffman
			ch == mSOF6 || // Differential progressive, Huffman
			ch == mSOF7 || // Differential lossless, Huffman
			ch == mSOF9 || // Extended sequential, arithmetic
			ch == mSOF10 || // Progressive, arithmetic
			ch == mSOF11 || // Lossless, arithmetic
			ch == mSOF13 || // Differential sequential, arithmetic
			ch == mSOF14 || // Differential progressive, arithmetic
			ch == mSOF15 { // Differential lossless, arithmetic
			// Skip 3 bytes to get to the image height and width
			image.index += 3
			image.height = image.getUint16(buffer)
			image.index += 2
			image.width = image.getUint16(buffer)
			image.index += 2
			image.colorComponents = buffer[image.index]
			break
		} else {
			image.skipVariable(buffer)
		}
	}

	return image
}

// Find the next JPEG marker and return its marker code.
// We expect at least one FF byte, possibly more if the compressor
// used FFs to pad the file.
// There could also be non-FF garbage between markers. The treatment
// of such garbage is unspecified; we choose to skip over it but
// emit a warning msg.
// NB: this routine must not be used after seeing SOS marker, since
// it will not deal correctly with FF/00 sequences in the compressed
// image data...
func (image *JPGImage) nextMarker(buffer []byte) uint8 {
	// Find 0xFF byte; count and skip any non-FFs.
	ch := buffer[image.index]
	image.index++
	if ch != 0xFF {
		log.Fatal("0xFF byte expected.")
	}

	// Get marker code byte, swallowing any duplicate FF bytes.
	// Extra FFs are legal as pad bytes, so don't count them in discardedBytes.
	for {
		ch = buffer[image.index]
		image.index++
		if ch != 0xFF {
			break
		}
	}
	return ch
}

// Most types of marker are followed by a variable-length parameter
// segment. This routine skips over the parameters for any marker we
// don't otherwise want to process.
// Note that we MUST skip the parameter segment explicitly in order
// not to be fooled by 0xFF bytes that might appear within the
// parameter segment such bytes do NOT introduce new markers.
func (image *JPGImage) skipVariable(buffer []byte) {
	// Get the marker parameter length
	length := image.getUint16(buffer)
	image.index += 2

	if length < 2 {
		log.Fatal("Length includes itself, so must be at least 2.")
	}
	length -= 2

	// Skip over the remaining bytes
	for length > 0 {
		image.index++
		length--
	}
}

func (image *JPGImage) getUint16(buffer []byte) uint16 {
	return uint16(buffer[image.index])<<8 | uint16(buffer[image.index+1])
}
