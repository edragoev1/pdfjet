// decompressor.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package decompressor decompresses Deflate and LZW data.
package decompressor

import (
	"bytes"
	"compress/zlib"
	"fmt"
	"io"
)

// LZWDecode decodes the data of an LZWDecode stream, with the default
// EarlyChange of 1: the codes get one bit longer one code before the table
// needs it. Data that ends without the end code, or with an invalid code,
// returns what was decoded up to there, as a missing end is common in real
// files.
func LZWDecode(buf []byte) []byte {
	decoded := make([]byte, 0, len(buf)*2)
	table := make([][]byte, 4096)
	for i := 0; i < 256; i++ {
		table[i] = []byte{byte(i)}
	}
	next := 258 // 256 clears the table and 257 ends the data.
	codeLength := 9
	bits := 0 // Only the low bitCount bits are still unread.
	bitCount := 0
	var previous []byte
	for _, b := range buf {
		bits = (bits << 8) | int(b)
		bitCount += 8
		for bitCount >= codeLength {
			bitCount -= codeLength
			code := (bits >> bitCount) & ((1 << codeLength) - 1)
			if code == 256 {
				next = 258
				codeLength = 9
				previous = nil
				continue
			}
			var entry []byte
			if code < next && code != 257 && table[code] != nil {
				entry = table[code]
			} else if code == next && previous != nil {
				entry = append(append([]byte{}, previous...), previous[0])
			} else {
				return decoded // The end code or an invalid one.
			}
			decoded = append(decoded, entry...)
			if previous != nil && next < 4096 {
				table[next] = append(append([]byte{}, previous...), entry[0])
				next++
			}
			previous = entry
			if next+1 >= 1<<codeLength && codeLength < 12 {
				codeLength++
			}
		}
	}
	return decoded
}

// Inflate decompresses zlib-compressed data (RFC 1950).
// Returns an error if the data is not valid zlib format.
func Inflate(buf []byte) (result []byte, err error) {
	reader, err := zlib.NewReader(bytes.NewReader(buf))
	if err != nil {
		return nil, fmt.Errorf("invalid zlib data: %w", err)
	}
	defer func() {
		if closeErr := reader.Close(); closeErr != nil && err == nil {
			err = fmt.Errorf("failed to close reader: %w", closeErr)
		}
	}()

	var inflated bytes.Buffer
	inflated.Grow(len(buf))
	if _, err := io.Copy(&inflated, reader); err != nil {
		return nil, fmt.Errorf("decompression failed: %w", err)
	}

	return inflated.Bytes(), nil
}
