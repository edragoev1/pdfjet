// bitbuffer.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.
//
// Original author: Kazuhiko Arase, 2009
// URL: http://www.d-project.com/
// Licensed under MIT: http://www.opensource.org/licenses/mit-license.php
//
// The word "QR Code" is a registered trademark of
// DENSO WAVE INCORPORATED
// http://www.denso-wave.com/qrcode/faqpatent-e.html
//
// Modified and adapted for use in PDFjet by PDFjet Software

package qrcode

// bitBuffer describes the bitBuffer structure.
type bitBuffer struct {
	buffer     []byte
	length     int
	increments int
}

// newBitBuffer constructs bitBuffer object.
func newBitBuffer() *bitBuffer {
	bitBuffer := new(bitBuffer)
	bitBuffer.length = 0
	bitBuffer.increments = 32
	bitBuffer.buffer = make([]byte, bitBuffer.increments)
	return bitBuffer
}

func (bitBuffer *bitBuffer) getBuffer() []byte {
	return bitBuffer.buffer
}

func (bitBuffer *bitBuffer) getLengthInBits() int {
	return bitBuffer.length
}

func (bitBuffer *bitBuffer) put(num, length int) {
	for i := 0; i < length; i++ {
		bitBuffer.putBit(((num >> (length - i - 1)) & 1) == 1)
	}
}

func (bitBuffer *bitBuffer) putBit(bit bool) {
	if bitBuffer.length == len(bitBuffer.buffer)*8 {
		newBuffer := make([]byte, len(bitBuffer.buffer)+bitBuffer.increments)
		for i := 0; i < len(bitBuffer.buffer); i++ {
			newBuffer[i] = bitBuffer.buffer[i]
		}
		bitBuffer.buffer = newBuffer
	}
	if bit {
		bitBuffer.buffer[bitBuffer.length/8] |= (0x80 >> (bitBuffer.length % 8))
	}
	bitBuffer.length++
}
