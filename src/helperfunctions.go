// helperfunctions.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"fmt"
	"io"
	"strconv"
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

// readFully fills the buffer from the reader. It panics if the reader ends
// before the buffer is full, or cannot be read.
func readFully(r io.Reader, buf []byte) {
	_, err := io.ReadFull(r, buf)
	if err == io.EOF || err == io.ErrUnexpectedEOF {
		panic("Unexpected end of stream: expected " + strconv.Itoa(len(buf)) + " bytes")
	} else if err != nil {
		panic(err)
	}
}

func getUint8(r io.Reader) uint8 {
	buf := make([]byte, 1)
	readFully(r, buf)
	return buf[0]
}

func getUint16(r io.Reader) uint16 {
	buf := make([]byte, 2)
	readFully(r, buf)
	return uint16(buf[0])<<8 | uint16(buf[1])
}

func getUint24(r io.Reader) uint32 {
	buf := make([]byte, 3)
	readFully(r, buf)
	return uint32(buf[0])<<16 | uint32(buf[1])<<8 | uint32(buf[2])
}

func getUint32(r io.Reader) uint32 {
	buf := make([]byte, 4)
	readFully(r, buf)
	return uint32(buf[0])<<24 | uint32(buf[1])<<16 | uint32(buf[2])<<8 | uint32(buf[3])
}

func getInt32(r io.Reader) int32 {
	buf := make([]byte, 4)
	readFully(r, buf)
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
	readFully(r, buf)
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

// splitDelimited splits one line of a delimited data file into its fields, as
// RFC 4180 reads them: a field that starts with a quote runs to the closing
// quote, a doubled quote inside it stands for one quote, and a delimiter
// inside it is part of the text. A field that does not start with a quote
// keeps any quotes it holds. The quotes around a field are not part of it.
// A file that cannot be read this way is refused rather than guessed at.
//
// Every field is a slice of the line, as strings.Split makes them; the one
// copy is of a quoted field that holds a doubled quote. BigTable reads every
// line of its file through here.
func splitDelimited(line, delimiter string) []string {
	fields, _ := splitDelimitedRecord(line, delimiter, false)
	return fields
}

// maxLinesInRecord is the most lines a record may take, so that a quote that
// is never closed fails instead of reading the rest of the file into one field.
const maxLinesInRecord = 10000

// readDelimitedRecord returns the fields of the record of a delimited data
// file that starts with the line. When a quoted field holds line breaks, the
// record goes on over the lines that nextLine returns, and each line break in
// the field is a space, as a table cell is drawn on one line. Only a record of
// several lines is looked at for them, so a line costs nothing more to read.
// nextLine returns false at the end of the file.
func readDelimitedRecord(line, delimiter string, nextLine func() (string, bool)) []string {
	if fields, closed := splitDelimitedRecord(line, delimiter, true); closed {
		return fields // Nearly every line ends here
	}
	return readDelimitedLines(line, delimiter, nextLine)
}

// readDelimitedLines reads on from a line that ends inside a quoted field.
func readDelimitedLines(line, delimiter string, nextLine func() (string, bool)) []string {
	var record strings.Builder
	record.WriteString(line)
	lines := 1
	for {
		next, ok := nextLine()
		if !ok {
			panic("A quoted field is not closed by the end of the data file: " + excerptOfLine(record.String()))
		}
		lines++
		if lines > maxLinesInRecord {
			panic(fmt.Sprintf("A quoted field is not closed within %d lines of the data file: %s",
				maxLinesInRecord, excerptOfLine(record.String())))
		}
		record.WriteByte('\n')
		record.WriteString(next)
		// The quoted field goes on until a quote that is not doubled; only then
		// can the record end, so only then is it split again.
		if closesQuotedField(next) {
			if fields, closed := splitDelimitedRecord(record.String(), delimiter, true); closed {
				for i, field := range fields {
					fields[i] = lineBreaksToSpaces(field)
				}
				return fields
			}
		}
	}
}

// closesQuotedField returns true when the line, read inside a quoted field,
// holds the quote that closes it: a quote that is not one of a doubled pair.
func closesQuotedField(line string) bool {
	for i := 0; i < len(line); i++ {
		if line[i] == '"' {
			if i+1 < len(line) && line[i+1] == '"' {
				i++
			} else {
				return true
			}
		}
	}
	return false
}

// lineBreaksToSpaces returns the text with each line break, "\r\n", "\r" or
// "\n", replaced by a space, for a table cell that is drawn on one line.
func lineBreaksToSpaces(text string) string {
	if hasLineBreak(text) {
		return lineBreaks.Replace(text)
	}
	return text
}

// hasLineBreak returns true when the text holds a "\r" or a "\n". BigTable
// calls it for every field it measures and draws, and a field is short, so a
// loop small enough to be inlined is faster than calls to IndexByte.
func hasLineBreak(text string) bool {
	for i := 0; i < len(text); i++ {
		if isLineBreak[text[i]] {
			return true
		}
	}
	return false
}

var isLineBreak = [256]bool{'\n': true, '\r': true}

var lineBreaks = strings.NewReplacer("\r\n", " ", "\r", " ", "\n", " ")

// splitDelimitedRecord splits the line as splitDelimited does. With open, a
// line that ends inside a quoted field returns false, for readDelimitedRecord
// to read on; without it the line is refused.
func splitDelimitedRecord(line, delimiter string, open bool) ([]string, bool) {
	if delimiter == "" {
		return []string{line}, true
	}
	fields := make([]string, 0, strings.Count(line, delimiter)+1)
	i := 0
	for {
		if i < len(line) && line[i] == '"' {
			i++ // The quote that opens the field
			start := i
			for {
				quote := strings.IndexByte(line[i:], '"')
				if quote == -1 {
					if open {
						return nil, false
					}
					panic("A quoted field is not closed on this line of the data file: " + excerptOfLine(line))
				}
				i += quote + 1
				if i < len(line) && line[i] == '"' {
					i++ // Two quotes stand for one; the closing quote is the first single one
				} else {
					break // The quote that closes the field
				}
			}
			// The text between the quotes, where every quote is doubled.
			fields = append(fields, strings.ReplaceAll(line[start:i-1], `""`, `"`))
			if i < len(line) && !strings.HasPrefix(line[i:], delimiter) {
				panic("A quoted field is followed by text on this line of the data file: " + excerptOfLine(line))
			}
		} else {
			end := strings.Index(line[i:], delimiter)
			if end == -1 {
				end = len(line)
			} else {
				end += i
			}
			fields = append(fields, line[i:end])
			i = end
		}
		if i == len(line) {
			break
		}
		i += len(delimiter) // Step over the delimiter
		if i == len(line) { // The line ends on a delimiter
			fields = append(fields, "")
			break
		}
	}
	return fields, true
}

// excerptOfLine returns the start of the line, for the message of a file that
// cannot be read.
func excerptOfLine(line string) string {
	if len(line) <= 60 {
		return line
	}
	return line[:60] + "..."
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
