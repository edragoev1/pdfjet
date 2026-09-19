// fontstream_fuzz_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"bytes"
	"compress/zlib"
	"encoding/binary"
	"io"
	"os"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// The fuzz targets of the stream fonts, which fontStream1 adds to a PDF and
// fontStream2 to the objects of a PDF that was read. Any input either makes a
// font that draws text, or panics with a message: an index out of range, a
// nil pointer, a hang or gigabytes of memory is a bug. The seeds are fonts
// PDFjet ships; go test runs them, and
//
//	go test ./src -run '^$' -fuzz '^FuzzFontStreamMetrics$' -fuzztime 5m -fuzzminimizetime 5s
//
// fuzzes. An input that fails is kept in testdata/fuzz, and go test runs it
// from then on.

// fuzzFontStreamSeeds are shipped fonts: with marks that go on letters (Thai),
// with the CFF tables of an OpenType font (IBM Plex Sans), and TrueType (JetBrains Mono).
var fuzzFontStreamSeeds = []string{
	"../fonts/NotoSansThai/NotoSansThai-Regular.ttf.stream",
	"../fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream",
	"../fonts/JetBrainsMono/JetBrainsMono-Regular.ttf.stream",
}

// fuzzFontText has letters, marks that go on letters and on other marks,
// right to left text, ideographs, a character past the Basic Multilingual
// Plane and ones that are not drawn.
const fuzzFontText = "Ab1 กิ่ ñ é\u0301 שָׁלוֹם مَرْحَبًا 日本 😀 \u200D\uFEFF"

// fuzzFontStream loads the stream font in both ways and draws with it.
func fuzzFontStream(t *testing.T, stream []byte) {
	fuzzRun(t, len(stream), func() {
		fuzzUseFontStream(t, stream)
	})
}

func fuzzUseFontStream(t *testing.T, stream []byte) {
	objects := make([]*PDFobj, 0)
	NewFontStream2(&objects, bytes.NewReader(stream))

	pdf := NewPDF(bufio.NewWriter(io.Discard))
	font := NewFontStream1(pdf, bytes.NewReader(stream))
	font.SetSize(12)
	font.StringWidth(12, fuzzFontText)
	page := NewPage(pdf, letter.Portrait())
	NewTextLine(font, fuzzFontText).SetLocation(50, 50).DrawOn(page)
	if err := pdf.Complete(); err != nil {
		t.Fatal(err)
	}
}

// FuzzFontStream fuzzes a whole .stream file. Most changes to it break the
// checksum of the compressed metrics, which FuzzFontStreamMetrics gets past.
func FuzzFontStream(f *testing.F) {
	for _, path := range fuzzFontStreamSeeds {
		stream, err := os.ReadFile(path)
		if err != nil {
			f.Fatal(err)
		}
		f.Add(stream)
		f.Add(stream[:len(stream)/2])
	}
	f.Add([]byte{})
	f.Fuzz(fuzzFontStream)
}

// FuzzFontStreamMetrics fuzzes the metrics of a stream font, which the stream
// holds compressed, and the marks, which the metrics hold compressed again:
// the target compresses them, so the fuzzer changes what they decode to. The
// name, the license, the tables that are not embedded and the font file are
// fuzzed as they are.
func FuzzFontStreamMetrics(f *testing.F) {
	for _, path := range fuzzFontStreamSeeds {
		stream, err := os.ReadFile(path)
		if err != nil {
			f.Fatal(err)
		}
		name, info, metrics, marks, lineGap, rest, flag, embedded := fuzzSplitFontStream(f, stream)
		f.Add(name, info, metrics, marks, lineGap, rest, flag, embedded)
	}
	f.Fuzz(func(t *testing.T, name, info, metrics, marks []byte, lineGap int32, rest, flag byte, embedded []byte) {
		fuzzFontStream(t, fuzzJoinFontStream(name, info, metrics, marks, lineGap, rest, flag, embedded))
	})
}

// fuzzJoinFontStream returns the stream font of the parts that
// fuzzSplitFontStream returns. The rest are the tables that are not embedded,
// as many bytes as it says, when the flag is R.
func fuzzJoinFontStream(name, info, metrics, marks []byte, lineGap int32, rest, flag byte, embedded []byte) []byte {
	var buf bytes.Buffer
	buf.WriteByte(byte(len(name)))
	buf.Write(name[:min(len(name), 255)])
	buf.Write([]byte{byte(len(info) >> 16), byte(len(info) >> 8), byte(len(info))})
	buf.Write(info[:min(len(info), 0xFFFFFF)])

	all := bytes.NewBuffer(append([]byte(nil), metrics...))
	if marks != nil {
		compressed := fuzzCompress(marks)
		_ = binary.Write(all, binary.BigEndian, uint32(len(compressed)))
		all.Write(compressed)
		_ = binary.Write(all, binary.BigEndian, lineGap)
	}
	compressed := fuzzCompress(all.Bytes())
	_ = binary.Write(&buf, binary.BigEndian, uint32(len(compressed)))
	buf.Write(compressed)

	if flag == 'R' {
		buf.WriteByte('R')
		_ = binary.Write(&buf, binary.BigEndian, uint32(rest))
		buf.Write(make([]byte, rest))
		buf.WriteByte('Y')
	} else {
		buf.WriteByte(flag)
	}
	_ = binary.Write(&buf, binary.BigEndian, uint32(2*len(embedded)))
	_ = binary.Write(&buf, binary.BigEndian, uint32(len(embedded)))
	buf.Write(embedded)
	return buf.Bytes()
}

// fuzzSplitFontStream returns the parts of a stream font that
// fuzzJoinFontStream joins, with the font file cut short.
func fuzzSplitFontStream(f testing.TB, stream []byte) (name, info, metrics, marks []byte, lineGap int32, rest, flag byte, embedded []byte) {
	r := bytes.NewReader(stream)
	next := func(n int) []byte {
		b := make([]byte, n)
		if _, err := io.ReadFull(r, b); err != nil {
			f.Fatal(err)
		}
		return b
	}
	name = next(int(next(1)[0]))
	b := next(3)
	info = next(int(b[0])<<16 | int(b[1])<<8 | int(b[2]))
	all := fuzzInflate(f, next(int(binary.BigEndian.Uint32(next(4)))))
	pos := 48
	pos += 4 + 2*int(binary.BigEndian.Uint32(all[pos:]))
	pos += 4 + 2*int(binary.BigEndian.Uint32(all[pos:]))
	metrics = all[:pos]
	if pos < len(all) {
		length := int(binary.BigEndian.Uint32(all[pos:]))
		marks = fuzzInflate(f, all[pos+4:pos+4+length])
		if pos+4+length+4 <= len(all) {
			lineGap = int32(binary.BigEndian.Uint32(all[pos+4+length:]))
		}
	}
	flag = next(1)[0]
	if flag == 'R' {
		next(int(binary.BigEndian.Uint32(next(4))))
		rest = 16
		next(1)
	}
	next(8)
	embedded = next(min(r.Len(), 4096))
	return
}

func fuzzCompress(data []byte) []byte {
	var buf bytes.Buffer
	w := zlib.NewWriter(&buf)
	_, _ = w.Write(data)
	_ = w.Close()
	return buf.Bytes()
}

func fuzzInflate(f testing.TB, data []byte) []byte {
	r, err := zlib.NewReader(bytes.NewReader(data))
	if err != nil {
		f.Fatal(err)
	}
	inflated, err := io.ReadAll(r)
	if err != nil {
		f.Fatal(err)
	}
	return inflated
}
