// helperfunctions.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"io"
	"strings"
	"unicode"
)

// insertStringAt inserts the string s1 into a1 at the specified index
func insertStringAt(a1 []string, s1 string, index int) []string {
	a2 := make([]string, 0)
	a2 = append(a2, a1[:index]...)
	a2 = append(a2, s1)
	a2 = append(a2, a1[index:]...)
	return a2
}

// insertArrayAt inserts the array a2 into a1 at the specified index
func insertArrayAt(a1, a2 []string, index int) []string {
	a3 := make([]string, 0)
	a3 = append(a3, a1[:index]...)
	a3 = append(a3, a2...)
	a3 = append(a3, a1[index:]...)
	return a3
}

func getUint8(r io.Reader) uint8 {
	buf := make([]byte, 1)
	io.ReadFull(r, buf)
	return buf[0]
}

func getUint16(r io.Reader) uint16 {
	buf := make([]byte, 2)
	io.ReadFull(r, buf)
	return uint16(buf[0])<<8 | uint16(buf[1])
}

func getUint24(r io.Reader) uint32 {
	buf := make([]byte, 3)
	io.ReadFull(r, buf)
	return uint32(buf[0])<<16 | uint32(buf[1])<<8 | uint32(buf[2])
}

func getUint32(r io.Reader) uint32 {
	buf := make([]byte, 4)
	io.ReadFull(r, buf)
	return uint32(buf[0])<<24 | uint32(buf[1])<<16 | uint32(buf[2])<<8 | uint32(buf[3])
}

func getInt32(r io.Reader) int32 {
	buf := make([]byte, 4)
	io.ReadFull(r, buf)
	return int32(buf[0])<<24 | int32(buf[1])<<16 | int32(buf[2])<<8 | int32(buf[3])
}

// Pre-allocated lowercase hex digits. The ToUnicode CMap is written in
// lowercase, matching Java's Integer.toHexString. This is deliberately not
// the uppercase hexDigits table used for the page content streams.
var lowerHexDigits = [16]byte{
	'0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
	'a', 'b', 'c', 'd', 'e', 'f',
}

// toHexString formats code as 4 lowercase hex digits, zero-padded.
// Callers only ever pass 16-bit CIDs/GIDs (0 - 0xFFFF).
// This used to go through fmt.Sprintf("%04x", code), which is fine
// for occasional use but far too slow when called tens of thousands
// of times while building a CJK font's ToUnicode CMap.
func toHexString(code int) string {
	b := [4]byte{
		lowerHexDigits[(code>>12)&0xF],
		lowerHexDigits[(code>>8)&0xF],
		lowerHexDigits[(code>>4)&0xF],
		lowerHexDigits[code&0xF],
	}
	return string(b[:])
}

func skipNBytes(reader io.Reader, n int) {
	getNBytes(reader, n)
}

func getNBytes(r io.Reader, n int) []byte {
	buf := make([]byte, n)
	io.ReadFull(r, buf)
	return buf
}

// isASCIIWhitespace reports the ASCII whitespace that Java's \s matches; a
// no-break space does not break a line.
func isASCIIWhitespace(r rune) bool {
	return r == ' ' || r == '\t' || r == '\n' || r == '\v' || r == '\f' || r == '\r'
}

// splitOnWhitespace splits the string into the words between its runs of
// ASCII whitespace, like split("\\s+") in Java, with no empty words.
func splitOnWhitespace(s string) []string {
	return strings.FieldsFunc(s, isASCIIWhitespace)
}

// trimSpace removes the characters up to the space at both ends of the
// string, like String.trim in Java. strings.TrimSpace also removes Unicode
// spaces like the no-break space, which Java's does not.
func trimSpace(s string) string {
	return strings.TrimFunc(s, func(r rune) bool { return r <= ' ' })
}

// isJavaWhitespace reports whether the rune is whitespace as
// Character.isWhitespace in Java defines it: the space separators, line
// separators and paragraph separators, except the no-break spaces U+00A0,
// U+2007 and U+202F, and the control characters U+0009 - U+000D and
// U+001C - U+001F.
func isJavaWhitespace(r rune) bool {
	if (r >= 0x09 && r <= 0x0D) || (r >= 0x1C && r <= 0x1F) {
		return true
	}
	if r == 0x00A0 || r == 0x2007 || r == 0x202F {
		return false
	}
	return unicode.In(r, unicode.Zs, unicode.Zl, unicode.Zp)
}

// isCJK returns true if more than half of the characters of the string are
// CJK Unified Ideographs (4E00 - 9FD5), Hiragana (3040 - 309F), Katakana
// (30A0 - 30FF) or Hangul Jamo (1100 - 11FF).
func isCJK(s string) bool {
	numOfCJK := 0
	count := 0
	for _, ch := range s {
		count++
		if (ch >= 0x4E00 && ch <= 0x9FD5) ||
			(ch >= 0x3040 && ch <= 0x309F) ||
			(ch >= 0x30A0 && ch <= 0x30FF) ||
			(ch >= 0x1100 && ch <= 0x11FF) {
			numOfCJK++
		}
	}
	return numOfCJK > (count / 2)
}
