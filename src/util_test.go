// util_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"encoding/hex"
	"io"
	"math/rand"
	"os"
	"path/filepath"
	"reflect"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/content"
)

// content and util: reading text and binary files and readers.

func testWriteFile(t *testing.T, name string, data []byte) string {
	t.Helper()
	path := filepath.Join(t.TempDir(), name)
	if err := os.WriteFile(path, data, 0o644); err != nil {
		t.Fatal(err)
	}
	return path
}

func TestUtilOfTextFileReadsUtf8AndDropsAByteOrderMark(t *testing.T) {
	path := testWriteFile(t, "bom.txt", []byte("\uFEFFhello\nwörld"))
	if got := content.OfTextFile(path); got != "hello\nwörld" {
		t.Errorf("got %q", got)
	}
}

func TestUtilReadLinesDropsAByteOrderMarkAndKeepsEmptyLines(t *testing.T) {
	path := testWriteFile(t, "lines.txt", []byte("\uFEFFa\n\nb"))
	if got := content.LinesOfTextFile(path); !reflect.DeepEqual(got, []string{"a", "", "b"}) {
		t.Errorf("got %q", got)
	}
}

func TestUtilReadingAMissingFileThrows(t *testing.T) {
	missing := filepath.Join(t.TempDir(), "missing.txt")
	if _, panicked := testPanic(func() { content.OfTextFile(missing) }); !panicked {
		t.Error("OfTextFile did not panic")
	}
	if _, panicked := testPanic(func() { content.OfBinaryFile(missing) }); !panicked {
		t.Error("OfBinaryFile did not panic")
	}
}

// testSlowReader returns at most three bytes per read.
type testSlowReader struct {
	reader io.Reader
}

func (r testSlowReader) Read(p []byte) (int, error) {
	if len(p) > 3 {
		p = p[:3]
	}
	return r.reader.Read(p)
}

func TestUtilGetFromStreamReadsAStreamThatReturnsFewBytesAtATime(t *testing.T) {
	// Go has GetFromStream only, with no buffer size argument.
	data := make([]byte, 10000)
	rand.New(rand.NewSource(7)).Read(data)
	if got := content.GetFromStream(testSlowReader{bytes.NewReader(data)}); !bytes.Equal(got, data) {
		t.Error("the bytes differ")
	}
}

func TestUtilOfBinaryFileReadsTheBytes(t *testing.T) {
	data := []byte{0, 1, 2, 0xFF}
	if got := content.OfBinaryFile(testWriteFile(t, "data.bin", data)); !bytes.Equal(got, data) {
		t.Errorf("got %v", got)
	}
}

func testFields(t *testing.T, want []string, line, delimiter string) {
	t.Helper()
	got := splitDelimited(line, delimiter)
	if !reflect.DeepEqual(got, want) {
		t.Errorf("%q split on %q: want %q, got %q", line, delimiter, want, got)
	}
}

func TestUtilSplitCutsTheLineAtTheDelimiterAndKeepsTheEmptyFields(t *testing.T) {
	testFields(t, []string{"a", "b", "c"}, "a,b,c", ",")
	testFields(t, []string{"", "a", ""}, ",a,", ",")
	testFields(t, []string{""}, "", ",")
	testFields(t, []string{"a", "b"}, "a||b", "||")
	testFields(t, []string{"a,b"}, "a,b", "")
}

func TestUtilSplitReadsAQuotedFieldAsRfc4180Does(t *testing.T) {
	testFields(t, []string{"Smith, John", "42"}, `"Smith, John",42`, ",")
	testFields(t, []string{`a"b`}, `"a""b"`, ",")
	testFields(t, []string{"", "x", ""}, `"",x,""`, ",")
	testFields(t, []string{"one\ttwo", "three"}, "\"one\ttwo\"\tthree", "\t")
}

func TestUtilSplitLeavesTheQuotesOfAFieldThatDoesNotStartWithOne(t *testing.T) {
	testFields(t, []string{`5" pipe`, "b"}, `5" pipe,b`, ",")
	testFields(t, []string{`a"b"c`}, `a"b"c`, ",")
}

func TestUtilSplitRefusesALineItCannotRead(t *testing.T) {
	for _, line := range []string{`a,"b,c`, `"a"b,c`} {
		func() {
			defer func() {
				if recover() == nil {
					t.Errorf("%q was read without complaint", line)
				}
			}()
			splitDelimited(line, ",")
		}()
	}
}

// testFirstRecord reads the first record of the text, as the data file readers do.
func testFirstRecord(text string) []string {
	lines := strings.Split(text, "\n")
	i := 1
	return readDelimitedRecord(lines[0], ",", func() (string, bool) {
		if i < len(lines) {
			i++
			return lines[i-1], true
		}
		return "", false
	})
}

func TestUtilAQuotedFieldGoesOnOverItsLineBreaksAsSpaces(t *testing.T) {
	for _, c := range []struct {
		want []string
		text string
	}{
		{[]string{"a", "12 Main St Apt 4", "b"}, "a,\"12 Main St\nApt 4\",b\nnext,line"},
		{[]string{"x\" y"}, "\"x\"\"\ny\""},
		{[]string{"a b", "c d"}, "\"a\nb\",\"c\nd\""},
		{[]string{"", " ", ""}, ",\"\n\",\nnext"},
		{[]string{"a", "b"}, "a,b\n\"c\nd\""},
	} {
		if got := testFirstRecord(c.text); !reflect.DeepEqual(got, c.want) {
			t.Errorf("%q: want %q, got %q", c.text, c.want, got)
		}
	}
}

func TestUtilAQuotedFieldThatIsNeverClosedIsRefused(t *testing.T) {
	message, _ := testPanic(func() { testFirstRecord("a,\"b\nc\nd") })
	if message != "A quoted field is not closed by the end of the data file: a,\"b\nc\nd" {
		t.Errorf("end: %q", message)
	}
	text := "\"a" + strings.Repeat("\nb", maxLinesInRecord)
	message, _ = testPanic(func() { testFirstRecord(text) })
	if message != "A quoted field is not closed within 10000 lines of the data file: "+text[:60]+"..." {
		t.Errorf("limit: %q", message)
	}
}

func TestUtilLineBreaksAreDrawnAsSpaces(t *testing.T) {
	if got := lineBreaksToSpaces("a\r\nb\rc\nd"); got != "a b c d" {
		t.Errorf("got %q", got)
	}
	if got := lineBreaksToSpaces("no breaks"); got != "no breaks" {
		t.Errorf("got %q", got)
	}
}

func TestUtilNoLineOfChineseOrJapaneseTextStartsWithAClosingMarkOrEndsWithAnOpeningOne(t *testing.T) {
	for _, c := range []struct {
		line string
		next rune
		want int
	}{
		{"あいう", 'え', 3},
		{"あいう", '。', 2}, // う moves down with 。
		{"あい」", '。', 1}, // and so does 」
		{"あい「", 'う', 2}, // 「 moves down
		{"中文", ',', 1},
		{"」", '。', 1}, // no other place to break
	} {
		if got := cjkLineEnd([]rune(c.line), c.next); got != c.want {
			t.Errorf("cjkLineEnd(%q, %q) = %d, want %d", c.line, c.next, got, c.want)
		}
	}
}

func TestUtilOfTextFileReplacesWhatIsNotUtf8(t *testing.T) {
	// The bytes and the text they read as: the four ports replace the maximal
	// subparts of an ill formed UTF-8 sequence with U+FFFD, the substitution
	// the Unicode Standard recommends in section 3.9. The same table is in
	// the tests of the other three ports.
	for _, item := range []struct {
		bytes string // In hexadecimal, so that the case reads as the bytes it is
		want  string
	}{
		{"68656C6C6F", "hello"},                                  // Hello
		{"77C3B6726C64", "w\u00F6rld"},                           // Wörld
		{"E697A5E69CAC", "\u65E5\u672C"},                         // 日本, Japanese
		{"F09F9880", "\U0001F600"},                               // A code point outside the plane
		{"EFBFBD", "\uFFFD"},                                     // The replacement character itself
		{"EFBFBE", "\uFFFE"},                                     // A noncharacter is well formed
		{"E28282", "\u2082"},                                     // Well formed, subscript two
		{"EDA080", "\uFFFD\uFFFD\uFFFD"},                         // An encoded surrogate, U+D800
		{"EDBFBF", "\uFFFD\uFFFD\uFFFD"},                         // U+DFFF
		{"EDA080EDB080", "\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD"}, // An encoded surrogate pair
		{"EDA0", "\uFFFD\uFFFD"},                                 // Two bytes of an encoded surrogate
		{"ED", "\uFFFD"},
		{"C080", "\uFFFD\uFFFD"}, // The overlong encodings
		{"E08080", "\uFFFD\uFFFD\uFFFD"},
		{"F0828282", "\uFFFD\uFFFD\uFFFD\uFFFD"},
		{"F4908080", "\uFFFD\uFFFD\uFFFD\uFFFD"}, // Past U+10FFFF
		{"F5808080", "\uFFFD\uFFFD\uFFFD\uFFFD"},
		{"FE", "\uFFFD"},
		{"FF", "\uFFFD"},
		{"80", "\uFFFD"}, // A byte of a sequence, alone
		{"BF", "\uFFFD"},
		{"C2", "\uFFFD"},         // A sequence cut short is one replacement,
		{"C2C2", "\uFFFD\uFFFD"}, // However many of its bytes are there
		{"E282", "\uFFFD"},
		{"E0A0", "\uFFFD"},
		{"F09080", "\uFFFD"},
		{"41C2", "A\uFFFD"},
		{"61EDA08062", "a\uFFFD\uFFFD\uFFFDb"}, // Between well formed text
	} {
		data, err := hex.DecodeString(item.bytes)
		if err != nil {
			t.Fatal(err)
		}
		path := testWriteFile(t, "utf8.txt", data)
		if got := content.OfTextFile(path); got != item.want {
			t.Errorf("%s reads %q, not %q", item.bytes, got, item.want)
		}
	}
}
