// decompressor_fuzz_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"encoding/binary"
	"fmt"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/internal/compressor"
	"github.com/edragoev1/pdfjet/v9/src/internal/decompressor"
)

// The fuzz target of the stream filters, which PDFjet decodes a stream of a
// PDF it reads with: Flate, LZW, ASCIIHex, ASCII85 and RunLength, and the
// predictor of a /DecodeParms. Any input either decodes or fails with an
// error of PDFjet's own: an index out of range, a hang or gigabytes of memory
// is a bug. go test runs the seeds, and
//
//	go test ./src -run '^$' -fuzz '^FuzzDecompressor$' -fuzztime 5m -fuzzminimizetime 5s
//
// fuzzes. An input that fails is kept in testdata/fuzz, and go test runs it
// from then on.

// fuzzDecompress runs every decoder on the same bytes, and the predictor on
// what Flate decodes to, and returns what each one gave, so that the four
// ports can be compared on the same input.
func fuzzDecompress(data []byte, predictor, colors, bits, columns int) string {
	var report strings.Builder
	say := func(filter string, decoded []byte, err error) {
		if err != nil {
			fmt.Fprintf(&report, " %s=error", filter)
			return
		}
		fmt.Fprintf(&report, " %s=%d,%s", filter, len(decoded), fuzzDigest(decoded))
	}
	inflated, err := decompressor.Inflate(data)
	say("flate", inflated, err)
	if err == nil {
		say("flate-predictor", decompressor.ApplyPredictor(
			inflated, predictor, colors, bits, columns), nil)
	}
	prefix, err := decompressor.InflatePrefix(data, 64)
	say("prefix", prefix, err)
	lzw, err := decompressor.LZWDecode(data)
	say("lzw", lzw, err)
	say("hex", decompressor.ASCIIHexDecode(data), nil)
	say("a85", decompressor.ASCII85Decode(data), nil)
	runLength, err := decompressor.RunLengthDecode(data)
	say("runlength", runLength, err)
	say("predictor", decompressor.ApplyPredictor(
		data, predictor, colors, bits, columns), nil)
	return strings.TrimSpace(report.String())
}

// fuzzDigest is the FNV-1a 64 bit hash, as 16 hexadecimal digits. The ports
// are compared on it rather than on the megabytes a stream decodes to.
func fuzzDigest(data []byte) string {
	hash := uint64(0xcbf29ce484222325)
	for i := 0; i < len(data); i++ {
		hash ^= uint64(data[i])
		hash *= 0x100000001b3
	}
	return fmt.Sprintf("%016x", hash)
}

// FuzzDecompressor fuzzes the bytes of a stream and the four values of the
// predictor of its /DecodeParms.
func FuzzDecompressor(f *testing.F) {
	// The encoding of "-----A---B" of ISO 32000-1, 7.4.4.2, ASCII85 and
	// ASCIIHex of "Hello world", a run length stream, and Flate of text that
	// repeats, of one byte and of nothing.
	for _, seed := range [][]byte{
		{0x80, 0x0B, 0x60, 0x50, 0x22, 0x0C, 0x0C, 0x85, 0x01},
		[]byte("<~87cURD]j7BEbo7~>"),
		[]byte("48 65 6C6C6F>"),
		{2, 'a', 'b', 'c', 254, 'x', 128},
		compressor.Deflate(bytes.Repeat([]byte("PDFjet "), 64)),
		compressor.Deflate([]byte{0}),
		compressor.Deflate(nil),
		{},
	} {
		f.Add(seed, 12, 3, 8, 16)
		f.Add(seed, 2, 1, 4, 7)
	}
	f.Fuzz(func(t *testing.T, data []byte, predictor, colors, bits, columns int) {
		fuzzRun(t, len(data), func() {
			fuzzDecompress(data, predictor, colors, bits, columns)
		})
	})
}

// FuzzDeflateRoundTrip fuzzes the bytes PDFjet writes compressed into a PDF
// and reads back: Inflate must read what Deflate writes as the same bytes,
// and a prefix of it must be its first bytes. Its corpus is the streams the
// four ports are replayed on, which a fuzzer that changes bytes at random
// hardly ever writes: one of them reaches the dynamic Huffman codes of a
// hand-written decoder, where random bytes stop at the zlib header.
func FuzzDeflateRoundTrip(f *testing.F) {
	f.Add([]byte(nil))
	f.Add([]byte{0})
	f.Add(bytes.Repeat([]byte("PDFjet "), 64))
	f.Add([]byte("The quick brown fox jumps over the lazy dog."))
	data := make([]byte, 4096)
	for i := range data {
		data[i] = byte(i * i / 7)
	}
	f.Add(data)
	f.Fuzz(func(t *testing.T, data []byte) {
		fuzzRun(t, len(data), func() {
			deflated := compressor.Deflate(data)
			inflated, err := decompressor.Inflate(deflated)
			if err != nil {
				t.Fatalf("%d bytes deflate to a stream that does not inflate: %v", len(data), err)
			}
			if !bytes.Equal(data, inflated) {
				t.Fatalf("%d bytes inflate to %d other bytes", len(data), len(inflated))
			}
			length := min(len(data), 64)
			prefix, err := decompressor.InflatePrefix(deflated, 64)
			if err != nil {
				t.Fatalf("the prefix of %d bytes fails: %v", len(data), err)
			}
			if !bytes.Equal(data[:length], prefix) {
				t.Fatalf("the prefix of %d bytes is %d other bytes", len(data), len(prefix))
			}
		})
	})
}

// fuzzJoinDecompressorInput returns the bytes of a corpus input as the four
// ports replay it: the four values of the predictor, then the data.
func fuzzJoinDecompressorInput(data []byte, predictor, colors, bits, columns int) []byte {
	var buf bytes.Buffer
	for _, value := range []int{predictor, colors, bits, columns} {
		_ = binary.Write(&buf, binary.BigEndian, int32(value))
	}
	buf.Write(data)
	return buf.Bytes()
}
