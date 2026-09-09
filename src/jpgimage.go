// jpgimage.go
//
// The authors make NO WARRANTY or representation, either express or implied,
// with respect to this software, its quality, accuracy, merchantability, or
// fitness for a particular purpose. This software is provided "AS IS", and you,
// its user, assume the entire risk as to its quality and accuracy.
//
// This software is copyright (C) 1991-1998, Thomas G. Lane.
// All Rights Reserved except as specified below.
//
// Permission is hereby granted to use, copy, modify, and distribute this
// software (or portions thereof) for any purpose, without fee, subject to these
// conditions:
// (1) If any part of the source code for this software is distributed, then this
// README file must be included, with this copyright and no-warranty notice
// unaltered; and any additions, deletions, or changes to the original files
// must be clearly indicated in accompanying documentation.
// (2) If only executable code is distributed, then the accompanying
// documentation must state that "this software is based in part on the work of
// the Independent JPEG Group".
// (3) Permission for use of this software is granted only if the user accepts
// full responsibility for any undesirable consequences; the authors accept
// NO LIABILITY for damages of any kind.
//
// These conditions apply to any software derived from or based on the IJG code,
// not just to the unmodified library.  If you use our work, you ought to
// acknowledge us.
//
// Permission is NOT granted for the use of any IJG author's name or company name
// in advertising or publicity relating to this software or products derived from
// it.  This software may be referred to only as "the Independent JPEG Group's
// software".
//
// We specifically permit and encourage the use of this software as the basis of
// commercial products, provided that all warranty or liability claims are
// assumed by the product vendor.

package pdfjet

import (
	"errors"
	"io"

	"github.com/edragoev1/pdfjet/src/content"
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
func NewJPGImage(reader io.Reader) (*JPGImage, error) {
	image := new(JPGImage)
	image.data = content.GetFromReader(reader)
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

func (image *JPGImage) readJPGImage(buffer []byte) (*JPGImage, error) {
	if len(buffer) < 2 || buffer[0] != 0xFF || buffer[1] != 0xD8 {
		return nil, errors.New("Error: Invalid JPEG header.")
	}
	image.index = 2

	for {
		ch, err := image.nextMarker(buffer)
		if err != nil {
			return nil, err
		}

		// Note that marker codes 0xC4, 0xC8, 0xCC are not,
		// and must not be treated as SOFn. C4 in particular
		// is actually DHT.
		switch ch {
		case mSOF0, // Baseline
			mSOF1,  // Extended sequential, Huffman
			mSOF2,  // Progressive, Huffman
			mSOF3,  // Lossless, Huffman
			mSOF5,  // Differential sequential, Huffman
			mSOF6,  // Differential progressive, Huffman
			mSOF7,  // Differential lossless, Huffman
			mSOF9,  // Extended sequential, arithmetic
			mSOF10, // Progressive, arithmetic
			mSOF11, // Lossless, arithmetic
			mSOF13, // Differential sequential, arithmetic
			mSOF14, // Differential progressive, arithmetic
			mSOF15: // Differential lossless, arithmetic

			// Skip 3 bytes to get to the image height and width
			image.index += 3
			height, err := image.getUint16(buffer)
			if err != nil {
				return nil, err
			}
			image.height = height
			width, err := image.getUint16(buffer)
			if err != nil {
				return nil, err
			}
			image.width = width
			colorComponents, err := image.getByte(buffer)
			if err != nil {
				return nil, err
			}
			image.colorComponents = colorComponents

			if width == 0 || height == 0 ||
				(colorComponents != 1 && colorComponents != 3 && colorComponents != 4) {
				return nil, errors.New("Error: Invalid JPEG dimensions or component count.")
			}

			return image, nil

		default:
			if err := image.skipVariable(buffer); err != nil {
				return nil, err
			}
		}
	}
}

// getByte reads one byte, advancing the index.
// It returns io.ErrUnexpectedEOF if the buffer is exhausted.
func (image *JPGImage) getByte(buffer []byte) (uint8, error) {
	if image.index >= len(buffer) {
		return 0, io.ErrUnexpectedEOF
	}
	b := buffer[image.index]
	image.index++
	return b, nil
}

// getUint16 reads two bytes as a big-endian unsigned integer,
// advancing the index by two.
func (image *JPGImage) getUint16(buffer []byte) (uint16, error) {
	b1, err := image.getByte(buffer)
	if err != nil {
		return 0, err
	}
	b2, err := image.getByte(buffer)
	if err != nil {
		return 0, err
	}
	return uint16(b1)<<8 | uint16(b2), nil
}

// nextMarker finds the next JPEG marker and returns its marker code.
// Non-FF garbage between markers is skipped over. Duplicate FF bytes
// are legal padding and are swallowed.
// NB: this routine must not be used after the SOS marker, since it
// does not deal correctly with FF/00 sequences in compressed data.
func (image *JPGImage) nextMarker(buffer []byte) (uint8, error) {
	// Find 0xFF byte; skip any non-FF garbage.
	ch, err := image.getByte(buffer)
	if err != nil {
		return 0, err
	}
	for ch != 0xFF {
		if ch, err = image.getByte(buffer); err != nil {
			return 0, err
		}
	}

	// Get the marker code byte, swallowing any duplicate FF bytes.
	// Extra FFs are legal as pad bytes.
	for {
		if ch, err = image.getByte(buffer); err != nil || ch != 0xFF {
			return ch, err
		}
	}
}

// skipVariable skips over the parameter segment of any marker
// we don't otherwise want to process.
// Note that we MUST skip the parameter segment explicitly in order
// not to be fooled by 0xFF bytes that might appear within the
// parameter segment - such bytes do NOT introduce new markers.
func (image *JPGImage) skipVariable(buffer []byte) error {
	// Get the marker parameter length count
	length, err := image.getUint16(buffer)
	if err != nil {
		return err
	}
	if length < 2 {
		// Length includes itself, so must be at least 2
		return errors.New("Error: Length includes itself, so must be at least 2.")
	}

	// Skip over the remaining bytes
	for i := uint16(2); i < length; i++ {
		if _, err := image.getByte(buffer); err != nil {
			return err
		}
	}
	return nil
}
