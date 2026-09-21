// jpgimage.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

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
	"fmt"
	"io"

	"github.com/edragoev1/pdfjet/v9/src/content"
)

// jpgImage describes JPG image object.
type jpgImage struct {
	width           uint16
	height          uint16
	colorComponents uint8
	// adobe is true when an APP14 segment says that Adobe software wrote the
	// image, which stores the inks of a CMYK image inverted, 255 for no ink.
	adobe bool
	data  []byte
	index int
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
	mAPP14 = uint8(0xEE)
	// The markers that stand alone, with no parameter segment to skip.
	mTEM  = uint8(0x01) // Temporary, for arithmetic coding
	mRST0 = uint8(0xD0) // ReSTart 0 to 7
	mRST7 = uint8(0xD7)
	mSOI  = uint8(0xD8) // Start Of Image
	mEOI  = uint8(0xD9) // End Of Image
	mSOS  = uint8(0xDA) // Start Of Scan, the end of the header
)

// newJPGImage is the constructor.
func newJPGImage(reader io.Reader) (*jpgImage, error) {
	image := new(jpgImage)
	image.data = content.GetFromStream(reader)
	return image.readJPGImage(image.data)
}

// GetWidth returns the width of the image.
func (image *jpgImage) getWidth() float32 {
	return float32(image.width)
}

// GetHeight returns the height of the image.
func (image *jpgImage) getHeight() float32 {
	return float32(image.height)
}

// GetFileSize returns the file size of the image.
func (image *jpgImage) getFileSize() uint64 {
	return uint64(len(image.data))
}

// GetColorComponents returns the color components of the image.
func (image *jpgImage) getColorComponents() uint8 {
	return image.colorComponents
}

// GetData returns the image data.
func (image *jpgImage) isAdobe() bool {
	return image.adobe
}

func (image *jpgImage) getData() []byte {
	return image.data
}

func (image *jpgImage) readJPGImage(buffer []byte) (*jpgImage, error) {
	if len(buffer) < 2 || buffer[0] != 0xFF || buffer[1] != 0xD8 {
		return nil, errors.New("Error: Invalid JPEG header.")
	}
	image.index = 2

	for {
		ch, err := image.nextMarker(buffer)
		if err != nil {
			return nil, err
		}

		// The standalone markers carry no parameter segment, so there is
		// nothing to skip after them; reading two bytes of one as a length
		// would skip over the frame header that follows.
		if ch == mTEM || ch == mSOI || (ch >= mRST0 && ch <= mRST7) {
			continue
		}
		if ch == mEOI {
			return nil, errors.New("Error: The JPEG ends before its frame header.")
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

			// The length of the frame header, then the sample precision: a
			// PDF image stream of DCTDecode data delivers eight bits per
			// color component, so a JPEG of another precision, a 12-bit one
			// among them, cannot be embedded as it is.
			segment := image.index
			length, err := image.getUint16(buffer)
			if err != nil {
				return nil, err
			}
			precision, err := image.getByte(buffer)
			if err != nil {
				return nil, err
			}
			if precision != 8 {
				return nil, fmt.Errorf(
					"Error: The JPEG has %d bits per color component, not 8.", precision)
			}
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
			// The frame header holds three bytes for each component after
			// its eight, as libjpeg checks: a header of another length is
			// the "Bogus SOF length" of libjpeg and the "Bogus marker
			// length" of MuPDF, so the readers a PDF is drawn with refuse
			// the image where PDFjet embedded it.
			if int(length) != 3*int(colorComponents)+8 {
				return nil, fmt.Errorf(
					"Error: The JPEG frame header is %d bytes, not the %d of its %d color components.",
					length, 3*int(colorComponents)+8, colorComponents)
			}

			// The component specifications fill the rest of the frame
			// header; they can hold any bytes, the 0xFF of a marker among
			// them, so the markers are read after the whole segment.
			if end := segment + int(length); end > image.index && end <= len(buffer) {
				image.index = end
				image.readAdobeMarker(buffer)
			}
			return image, nil

		case mAPP14:
			if err := image.readAPP14(buffer); err != nil {
				return nil, err
			}

		default:
			if err := image.skipVariable(buffer); err != nil {
				return nil, err
			}
		}
	}
}

// readAdobeMarker reads the markers from the frame header to the scan, for an
// APP14 segment: libjpeg reads a header to the scan too, so a segment that
// follows the frame header says that Adobe software wrote the image as one
// before it does. The frame header is all the image needs, so whatever cannot
// be read after it leaves the image as it is.
func (image *jpgImage) readAdobeMarker(buffer []byte) {
	for {
		ch, err := image.nextMarker(buffer)
		if err != nil || ch == mSOS || ch == mEOI {
			return
		}
		if ch == mTEM || ch == mSOI || (ch >= mRST0 && ch <= mRST7) {
			continue
		}
		if ch == mAPP14 {
			if image.readAPP14(buffer) != nil {
				return
			}
			continue
		}
		if image.skipVariable(buffer) != nil {
			return
		}
	}
}

// getByte reads one byte, advancing the index.
// It returns io.ErrUnexpectedEOF if the buffer is exhausted.
func (image *jpgImage) getByte(buffer []byte) (uint8, error) {
	if image.index >= len(buffer) {
		return 0, io.ErrUnexpectedEOF
	}
	b := buffer[image.index]
	image.index++
	return b, nil
}

// getUint16 reads two bytes as a big-endian unsigned integer,
// advancing the index by two.
func (image *jpgImage) getUint16(buffer []byte) (uint16, error) {
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
// are legal padding and are swallowed, and an FF byte that a zero byte
// follows is data and not a marker, so the search goes on.
// NB: this routine must not be used after the SOS marker, since it
// does not deal correctly with FF/00 sequences in compressed data.
func (image *jpgImage) nextMarker(buffer []byte) (uint8, error) {
	for {
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
		for ch == 0xFF {
			if ch, err = image.getByte(buffer); err != nil {
				return 0, err
			}
		}
		if ch != 0x00 {
			return ch, nil
		}
	}
}

// readAPP14 reads an APP14 segment, which Adobe software writes starting with
// "Adobe".
func (image *jpgImage) readAPP14(buffer []byte) error {
	length, err := image.getUint16(buffer)
	if err != nil {
		return err
	}
	if length < 2 {
		return errors.New("Error: Length includes itself, so must be at least 2.")
	}
	end := image.index + int(length) - 2
	if end > len(buffer) {
		return io.ErrUnexpectedEOF
	}
	// Adobe's segment is twelve bytes: "Adobe", the version, two flags and
	// the color transform. A shorter one is not Adobe's, as in libjpeg. An
	// APP14 segment of another kind after it does not unmark the image.
	if end-image.index >= 12 && string(buffer[image.index:image.index+5]) == "Adobe" {
		image.adobe = true
	}
	image.index = end
	return nil
}

// skipVariable skips over the parameter segment of any marker
// we don't otherwise want to process.
// Note that we MUST skip the parameter segment explicitly in order
// not to be fooled by 0xFF bytes that might appear within the
// parameter segment - such bytes do NOT introduce new markers.
func (image *jpgImage) skipVariable(buffer []byte) error {
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
