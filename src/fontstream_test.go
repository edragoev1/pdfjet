// fontstream_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"bytes"
	"encoding/binary"
	"io"
	"os"
	"runtime"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// testFontStreamError returns the message that loading the stream font panics with.
func testFontStreamError(stream []byte) string {
	return testPanicMessage(func() {
		NewFontStream1(NewPDF(bufio.NewWriter(io.Discard)), bytes.NewReader(stream))
	})
}

// testFontStreamMetrics returns a stream font with the metrics, and the
// Thai font's name, license and font file.
func testFontStreamMetrics(t *testing.T, metrics []byte) []byte {
	stream, err := os.ReadFile(fuzzFontStreamSeeds[0])
	if err != nil {
		t.Fatal(err)
	}
	name, info, _, _, _, rest, flag, embedded := fuzzSplitFontStream(t, stream)
	return fuzzJoinFontStream(name, info, metrics, nil, 0, rest, flag, embedded)
}

// testMetrics returns the metrics of a stream font: the units per em, the
// first and the last character, the advance widths and the character map.
func testMetrics(unitsPerEm, firstChar, lastChar int32, widths, cmap int) []byte {
	var buf bytes.Buffer
	be := binary.BigEndian
	_ = binary.Write(&buf, be, []int32{unitsPerEm, 0, -200, 1000, 800, 800, -200, firstChar, lastChar, 700, -100, 50})
	_ = binary.Write(&buf, be, int32(widths))
	_ = binary.Write(&buf, be, make([]uint16, widths))
	_ = binary.Write(&buf, be, int32(cmap))
	_ = binary.Write(&buf, be, make([]uint16, cmap))
	return buf.Bytes()
}

func TestFontStreamAMarkAfterAGlyphPastTheAdvanceWidthsIsDrawn(t *testing.T) {
	// IBM Plex Sans JP maps ↺ and 14 other arrows to glyphs past the end of
	// its advance widths, which the offsets of the marks looked up.
	pdf := NewPDF(bufio.NewWriter(io.Discard))
	font := NewFontFromFile(pdf, testRepoPath(t, "fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf.stream"))
	NewTextLine(font, "↺́ x").SetLocation(50, 50).DrawOn(NewPage(pdf, letter.Portrait()))
	if err := pdf.Complete(); err != nil {
		t.Fatal(err)
	}
}

func TestFontStreamLengthsThatTheStreamDoesNotHaveTakeNoMemory(t *testing.T) {
	// The metrics say they are 4 GB long, and the stream ends.
	stream := []byte{1, 'A', 0, 0, 0, 0xFF, 0xFF, 0xFF, 0xFF}
	var before, after runtime.MemStats
	runtime.ReadMemStats(&before)
	testWant(t, "Unexpected end of stream: expected 4294967295 bytes", testFontStreamError(stream))
	runtime.ReadMemStats(&after)
	if allocated := after.TotalAlloc - before.TotalAlloc; allocated > 1024*1024 {
		t.Errorf("%d bytes allocated", allocated)
	}
}

func TestFontStreamRejectsMetricsItCannotDrawWith(t *testing.T) {
	cases := []struct {
		want    string
		metrics []byte
	}{
		{"Invalid font stream: the units per em.", testMetrics(0, 32, 126, 1, 0x10000)},
		{"Invalid font stream: the first or last character.", testMetrics(1000, -1, 126, 1, 0x10000)},
		{"Invalid font stream: the first or last character.", testMetrics(1000, 32, 0x10000, 1, 0x10000)},
		{"Invalid font stream: no advance widths.", testMetrics(1000, 32, 126, 0, 0x10000)},
		{"Invalid font stream: the character map.", testMetrics(1000, 32, 126, 1, 0x100)},
		{"Invalid font stream: the metrics end too soon.", testMetrics(1000, 32, 126, 1, 0x10000)[:100]},
	}
	for _, c := range cases {
		testWant(t, c.want, testFontStreamError(testFontStreamMetrics(t, c.metrics)))
	}
	if got := testFontStreamError(testFontStreamMetrics(t, testMetrics(1000, 32, 126, 1, 0x10000))); got != "(no panic)" {
		t.Errorf("valid metrics fail with %q", got)
	}
}

func TestFontStreamRejectsANameThatIsNotAPDFName(t *testing.T) {
	stream, err := os.ReadFile(fuzzFontStreamSeeds[0])
	if err != nil {
		t.Fatal(err)
	}
	_, info, metrics, marks, lineGap, rest, flag, embedded := fuzzSplitFontStream(t, stream)
	for _, name := range []string{"", "Noto Sans", "Noto/Sans", "Noto(Sans", "Noto#20Sans"} {
		stream := fuzzJoinFontStream([]byte(name), info, metrics, marks, lineGap, rest, flag, embedded)
		testWant(t, "Invalid font stream: the font name.", testFontStreamError(stream))
	}
}
