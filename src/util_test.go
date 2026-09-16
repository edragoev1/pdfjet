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
