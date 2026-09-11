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

// ApplyPredictor undoes the predictor of the /DecodeParms of a stream: 2 is
// the TIFF predictor, and 10 to 15 are the PNG predictors, where each row
// starts with the type of the PNG filter of that row.
func ApplyPredictor(buf []byte, predictor, colors, bitsPerComponent, columns int) []byte {
	// Larger values are not in real files, and would overflow the row length.
	if colors < 1 || colors > 256 ||
		bitsPerComponent < 1 || bitsPerComponent > 16 ||
		columns < 1 || columns > 1<<18 {
		return buf
	}
	if predictor == 2 {
		return applyTIFFPredictor(buf, colors, bitsPerComponent, columns)
	} else if predictor >= 10 {
		return applyPNGPredictor(
			buf,
			(colors*bitsPerComponent+7)/8,
			(colors*bitsPerComponent*columns+7)/8)
	}
	return buf
}

// applyTIFFPredictor adds to each sample the sample of the same color to its left.
func applyTIFFPredictor(buf []byte, colors, bitsPerComponent, columns int) []byte {
	decoded := append([]byte{}, buf...)
	rowLength := (colors*bitsPerComponent*columns + 7) / 8
	for row := 0; row < len(decoded); row += rowLength {
		if bitsPerComponent == 8 {
			end := min(row+rowLength, len(decoded))
			for i := row + colors; i < end; i++ {
				decoded[i] += decoded[i-colors]
			}
		} else {
			samples := min(colors*columns, (len(decoded)-row)*8/bitsPerComponent)
			for i := colors; i < samples; i++ {
				left := getSample(decoded, row, (i-colors)*bitsPerComponent, bitsPerComponent)
				sample := getSample(decoded, row, i*bitsPerComponent, bitsPerComponent)
				setSample(decoded, row, i*bitsPerComponent, bitsPerComponent, left+sample)
			}
		}
	}
	return decoded
}

func getSample(buf []byte, row, bit, bitsPerComponent int) int {
	sample := 0
	for i := bit; i < bit+bitsPerComponent; i++ {
		sample = (sample << 1) | int((buf[row+(i>>3)]>>(7-(i&7)))&1)
	}
	return sample
}

// setSample sets the low bitsPerComponent bits of the sample.
func setSample(buf []byte, row, bit, bitsPerComponent, sample int) {
	for i := bit + bitsPerComponent - 1; i >= bit; i-- {
		k := row + (i >> 3)
		mask := byte(1 << (7 - (i & 7)))
		if sample&1 != 0 {
			buf[k] |= mask
		} else {
			buf[k] &^= mask
		}
		sample >>= 1
	}
}

// applyPNGPredictor undoes the PNG filters. Only the last row can be shorter
// than rowLength.
func applyPNGPredictor(buf []byte, bytesPerPixel, rowLength int) []byte {
	decoded := make([]byte, 0, len(buf))
	for i := 0; i < len(buf); i += rowLength + 1 {
		filter := buf[i]
		n := min(rowLength, len(buf)-i-1)
		for x := 0; x < n; x++ {
			j := len(decoded)
			left, up, upLeft := 0, 0, 0
			if x >= bytesPerPixel {
				left = int(decoded[j-bytesPerPixel])
			}
			if j >= rowLength {
				up = int(decoded[j-rowLength])
				if x >= bytesPerPixel {
					upLeft = int(decoded[j-rowLength-bytesPerPixel])
				}
			}
			value := int(buf[i+1+x])
			switch filter {
			case 1: // Sub
				value += left
			case 2: // Up
				value += up
			case 3: // Average
				value += (left + up) / 2
			case 4: // Paeth
				value += paeth(left, up, upLeft)
			} // 0 is None, and so are unknown types.
			decoded = append(decoded, byte(value))
		}
	}
	return decoded
}

func paeth(left, up, upLeft int) int {
	p := left + up - upLeft
	pLeft := abs(p - left)
	pUp := abs(p - up)
	pUpLeft := abs(p - upLeft)
	if pLeft <= pUp && pLeft <= pUpLeft {
		return left
	}
	if pUp <= pUpLeft {
		return up
	}
	return upLeft
}

func abs(x int) int {
	if x < 0 {
		return -x
	}
	return x
}

// ASCIIHexDecode decodes the data of an ASCIIHexDecode stream. White space is
// skipped, > ends the data, and a last digit without a pair is followed by 0.
func ASCIIHexDecode(buf []byte) []byte {
	decoded := make([]byte, 0, len(buf)/2)
	high := -1
	for _, b := range buf {
		if b == '>' {
			break
		}
		digit := HexValue(int(b))
		if digit == -1 {
			continue // White space, or a character that is not valid.
		}
		if high == -1 {
			high = digit
		} else {
			decoded = append(decoded, byte(high<<4|digit))
			high = -1
		}
	}
	if high != -1 {
		decoded = append(decoded, byte(high<<4))
	}
	return decoded
}

// HexValue returns the value of a hexadecimal digit, or -1 when it is not one.
func HexValue(c int) int {
	if c >= '0' && c <= '9' {
		return c - '0'
	} else if c >= 'a' && c <= 'f' {
		return c - 'a' + 10
	} else if c >= 'A' && c <= 'F' {
		return c - 'A' + 10
	}
	return -1
}

// ASCII85Decode decodes the data of an ASCII85Decode stream. Each group of
// five characters from ! to u is four bytes, z is four zero bytes, and ~>
// ends the data. White space is skipped, and a last group of n characters is
// n - 1 bytes.
func ASCII85Decode(buf []byte) []byte {
	decoded := make([]byte, 0, len(buf)*4/5+4)
	var value int64
	count := 0
	i := 0
	if len(buf) >= 2 && buf[0] == '<' && buf[1] == '~' {
		i = 2 // The start of the data in PostScript.
	}
	for ; i < len(buf); i++ {
		c := int64(buf[i])
		if c == '~' {
			break
		} else if c == 'z' && count == 0 {
			decoded = append(decoded, 0, 0, 0, 0)
		} else if c >= '!' && c <= 'u' {
			value = value*85 + (c - '!')
			count++
			if count == 5 {
				for j := 24; j >= 0; j -= 8 {
					decoded = append(decoded, byte(value>>j))
				}
				value = 0
				count = 0
			}
		} // White space, or a character that is not valid.
	}
	if count > 1 {
		for j := count; j < 5; j++ {
			value = value*85 + 84
		}
		for j := 0; j < count-1; j++ {
			decoded = append(decoded, byte(value>>(24-8*j)))
		}
	}
	return decoded
}

// RunLengthDecode decodes the data of a RunLengthDecode stream. A length byte
// from 0 to 127 is followed by that many plus one bytes to copy, one from 129
// to 255 by a byte to repeat 257 minus that many times, and 128 ends the data.
func RunLengthDecode(buf []byte) []byte {
	decoded := make([]byte, 0, len(buf)*2)
	i := 0
	for i < len(buf) {
		length := int(buf[i])
		i++
		if length < 128 {
			n := length + 1
			if n > len(buf)-i {
				n = len(buf) - i
			}
			decoded = append(decoded, buf[i:i+n]...)
			i += n
		} else if length > 128 && i < len(buf) {
			b := buf[i]
			i++
			for j := 0; j < 257-length; j++ {
				decoded = append(decoded, b)
			}
		} else {
			break
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
