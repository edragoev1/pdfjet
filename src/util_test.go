// util_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
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
