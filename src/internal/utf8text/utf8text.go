// utf8text.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package utf8text reads UTF-8 text, replacing what is not UTF-8.
package utf8text

import (
	"strings"
	"unicode/utf8"
)

// Decode returns the text of the bytes, with the bytes that are not a well
// formed UTF-8 sequence replaced by U+FFFD. One replacement stands for each
// maximal subpart of an ill formed sequence, the substitution the Unicode
// Standard recommends in section 3.9, so that the four ports of PDFjet read
// the same text from the same bytes: the three bytes ED A0 80, an encoded
// surrogate, are three replacements, and a sequence cut short at the end of
// the text is one, however many of its bytes are there.
//
// Go's own decoder replaces a byte at a time, which reads a sequence cut
// short as one replacement per byte.
func Decode(data []byte) string {
	if utf8.Valid(data) {
		return string(data)
	}
	var text strings.Builder
	text.Grow(len(data))
	for i := 0; i < len(data); {
		length, wellFormed := sequenceAt(data, i)
		if wellFormed {
			text.Write(data[i : i+length])
		} else {
			text.WriteRune(utf8.RuneError)
		}
		i += length
	}
	return text.String()
}

// sequenceAt returns the length of the UTF-8 sequence that starts at i and
// whether it is well formed, as the table of section 3.9 of the Unicode
// Standard gives them. The length of one that is not is the length of its
// maximal subpart: the bytes that could still have started a sequence, at
// least one.
func sequenceAt(data []byte, i int) (int, bool) {
	first := data[i]
	if first < 0x80 {
		return 1, true
	}
	var length int
	var low, high byte = 0x80, 0xBF // The range of the byte after the first
	switch {
	case first >= 0xC2 && first <= 0xDF:
		length = 2
	case first == 0xE0:
		length, low = 3, 0xA0 // Not the overlong encodings
	case first >= 0xE1 && first <= 0xEC:
		length = 3
	case first == 0xED:
		length, high = 3, 0x9F // Not the surrogates
	case first >= 0xEE && first <= 0xEF:
		length = 3
	case first == 0xF0:
		length, low = 4, 0x90 // Not the overlong encodings
	case first >= 0xF1 && first <= 0xF3:
		length = 4
	case first == 0xF4:
		length, high = 4, 0x8F // Not past U+10FFFF
	default:
		return 1, false // A byte of a sequence, or C0, C1 or F5 to FF
	}
	for next := 1; next < length; next++ {
		if i+next == len(data) || data[i+next] < low || data[i+next] > high {
			return next, false
		}
		low, high = 0x80, 0xBF
	}
	return length, true
}
