// decompressor_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package decompressor

import (
	"bytes"
	"math/rand"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/compressor"
)

// The stream filters and predictors that reading a PDF needs, and the compressor.

func testEqual(t *testing.T, name string, want, got []byte) {
	t.Helper()
	if !bytes.Equal(want, got) {
		t.Errorf("%s: want %v, got %v", name, want, got)
	}
}

func TestDecompressorLzwDecodesTheExampleOfTheStandard(t *testing.T) {
	// ISO 32000-1, 7.4.4.2: the encoding of "-----A---B".
	encoded := []byte{0x80, 0x0B, 0x60, 0x50, 0x22, 0x0C, 0x0C, 0x85, 0x01}
	decoded, err := LZWDecode(encoded)
	if err != nil {
		t.Fatal(err)
	}
	testEqual(t, "lzw", []byte("-----A---B"), decoded)
}

func TestDecompressorAsciiHexDecodeSkipsWhitespaceAndStopsAtTheEndMarker(t *testing.T) {
	testEqual(t, "hex", []byte("Hello"), ASCIIHexDecode([]byte("48 65 6C6C6F>")))
}

func TestDecompressorAsciiHexDecodeReadsAMissingLastDigitAsZero(t *testing.T) {
	testEqual(t, "hex", []byte{0x70}, ASCIIHexDecode([]byte("7>")))
}

func TestDecompressorAscii85DecodesWithAndWithoutThePrefix(t *testing.T) {
	// base64.a85encode(b"Hello world") in Python.
	testEqual(t, "without prefix", []byte("Hello world"), ASCII85Decode([]byte("87cURD]j7BEbo7~>")))
	testEqual(t, "with prefix", []byte("Hello world"), ASCII85Decode([]byte("<~87cURD]j7BEbo7~>")))
}

func TestDecompressorAscii85DecodesZAsFourZeroBytesAndAPartialLastGroup(t *testing.T) {
	testEqual(t, "z", []byte{0, 0, 0, 0}, ASCII85Decode([]byte("z~>")))
	testEqual(t, "z and partial", []byte{0, 0, 0, 0, 'a', 'b'}, ASCII85Decode([]byte("z@:B~>")))
}

func TestDecompressorRunLengthDecodeCopiesLiteralsAndRepeatsRuns(t *testing.T) {
	decoded, err := RunLengthDecode([]byte{2, 'a', 'b', 'c', 254, 'x', 128})
	if err != nil {
		t.Fatal(err)
	}
	testEqual(t, "run length", []byte("abcxxx"), decoded)
}

func TestDecompressorPngPredictorsUndoEachRowFilter(t *testing.T) {
	// Each row is the filter type and three one byte samples.
	testEqual(t, "sub", []byte{1, 2, 3}, ApplyPredictor([]byte{1, 1, 1, 1}, 11, 1, 8, 3))
	testEqual(t, "up", []byte{1, 2, 3, 2, 3, 4}, ApplyPredictor([]byte{0, 1, 2, 3, 2, 1, 1, 1}, 12, 1, 8, 3))
	testEqual(t, "average", []byte{2, 5, 8}, ApplyPredictor([]byte{3, 2, 4, 6}, 13, 1, 8, 3))
	testEqual(t, "paeth", []byte{1, 2, 3}, ApplyPredictor([]byte{4, 1, 1, 1}, 14, 1, 8, 3))
}

func TestDecompressorTiffPredictorAddsTheSampleToTheLeft(t *testing.T) {
	testEqual(t, "tiff", []byte{1, 2, 3, 5, 5, 5}, ApplyPredictor([]byte{1, 1, 1, 5, 0, 0}, 2, 1, 8, 3))
}

func TestDecompressorPredictorOneAndInvalidParametersLeaveTheDataAsItIs(t *testing.T) {
	data := []byte{9, 8, 7}
	testEqual(t, "predictor 1", data, ApplyPredictor(data, 1, 1, 8, 3))
	testEqual(t, "no colors", data, ApplyPredictor(data, 12, 0, 8, 3))
}

func TestDecompressorInflateUndoesDeflate(t *testing.T) {
	data := make([]byte, 100000)
	rand.New(rand.NewSource(42)).Read(data)
	for i := 50000; i < 90000; i++ {
		data[i] = 'x'
	}
	inflated, err := Inflate(compressor.Deflate(data))
	if err != nil {
		t.Fatal(err)
	}
	testEqual(t, "round trip", data, inflated)
}

func TestDecompressorInflateRejectsATruncatedStream(t *testing.T) {
	deflated := compressor.Deflate([]byte("hello hello hello hello"))
	if _, err := Inflate(deflated[:6]); err == nil {
		t.Error("no error")
	}
}

func TestDecompressorInflateIgnoresBytesAfterTheEndOfAStream(t *testing.T) {
	data := []byte("hello hello hello hello")
	padded := append(compressor.Deflate(data), '\r', '\n', 0x42)
	inflated, err := Inflate(padded)
	if err != nil || !bytes.Equal(data, inflated) {
		t.Errorf("inflated %q, error %v", inflated, err)
	}
}

func TestDecompressorInflateRejectsEveryTruncationOfAStream(t *testing.T) {
	data := make([]byte, 2000)
	rand.New(rand.NewSource(3)).Read(data[:1000])
	for i := 1000; i < 2000; i++ {
		data[i] = 'a'
	}
	deflated := compressor.Deflate(data)
	inflated, err := Inflate(deflated)
	if err != nil || !bytes.Equal(data, inflated) {
		t.Fatalf("round trip: %v", err)
	}
	// The last 4 bytes are the Adler-32 checksum, which not every port checks.
	for length := 0; length < len(deflated)-4; length++ {
		if _, err := Inflate(deflated[:length]); err == nil {
			t.Errorf("length %d: no error", length)
		}
	}
}

func TestDecompressorTheDecodedLengthLimitIs256MiB(t *testing.T) {
	if MaxDecodedLength != 256*1024*1024 {
		t.Errorf("MaxDecodedLength %d", MaxDecodedLength)
	}
}

func TestDecompressorInflateRejectsDataThatDecodesToMoreThanTheLimit(t *testing.T) {
	deflated := compressor.Deflate(make([]byte, 1000))
	if data, err := InflateWithMaxLength(deflated, 1000); err != nil || len(data) != 1000 {
		t.Errorf("decoded %d bytes, error %v", len(data), err)
	}
	_, err := InflateWithMaxLength(deflated, 999)
	if err == nil || err.Error() != "Flate data decodes to more than 999 bytes" {
		t.Errorf("error %v", err)
	}
}

func TestDecompressorInflatePrefixReturnsTheFirstBytesAndIgnoresTheRest(t *testing.T) {
	data := []byte("hello hello hello hello")
	deflated := compressor.Deflate(data)
	prefix, err := InflatePrefix(deflated, 5)
	if err != nil {
		t.Fatal(err)
	}
	testEqual(t, "prefix", data[:5], prefix)
	all, err := InflatePrefix(deflated, 100)
	if err != nil {
		t.Fatal(err)
	}
	testEqual(t, "all", data, all)
	if _, err := InflatePrefix(deflated[:6], 20); err == nil {
		t.Error("no error for a truncated stream")
	}
}

func TestDecompressorLzwDecodeRejectsDataThatDecodesToMoreThanTheLimit(t *testing.T) {
	encoded := []byte{0x80, 0x0B, 0x60, 0x50, 0x22, 0x0C, 0x0C, 0x85, 0x01}
	if data, err := LZWDecodeWithMaxLength(encoded, 10); err != nil || len(data) != 10 {
		t.Errorf("decoded %d bytes, error %v", len(data), err)
	}
	_, err := LZWDecodeWithMaxLength(encoded, 9)
	if err == nil || err.Error() != "LZW data decodes to more than 9 bytes" {
		t.Errorf("error %v", err)
	}
}

func TestDecompressorRunLengthDecodeRejectsDataThatDecodesToMoreThanTheLimit(t *testing.T) {
	encoded := []byte{2, 'a', 'b', 'c', 254, 'x', 128}
	if data, err := RunLengthDecodeWithMaxLength(encoded, 6); err != nil || len(data) != 6 {
		t.Errorf("decoded %d bytes, error %v", len(data), err)
	}
	_, err := RunLengthDecodeWithMaxLength(encoded, 5)
	if err == nil || err.Error() != "RunLength data decodes to more than 5 bytes" {
		t.Errorf("error %v", err)
	}
}

func TestDecompressorDeflateOfNoBytesIsAnEmptyZlibStream(t *testing.T) {
	deflated := compressor.Deflate([]byte{})
	if len(deflated) != 8 || deflated[0] != 0x78 {
		t.Errorf("deflated %v", deflated)
	}
}

func TestDecompressorInflateAcceptsAnEmptyStream(t *testing.T) {
	inflated, err := Inflate(compressor.Deflate([]byte{}))
	if err != nil || len(inflated) != 0 {
		t.Errorf("inflated %v, error %v", inflated, err)
	}
}
