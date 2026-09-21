// svgimage_fuzz_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"fmt"
	"io"
	"os"
	"path/filepath"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// The fuzz targets of the SVG images. Any input either makes an image that is
// drawn and written, or fails with a message; see fuzzRun. FuzzSVGImage
// fuzzes whole documents, and FuzzSVGPath the path data, which it writes out
// of numbers no larger than a page, so that a path of them must draw. The
// seeds are the icons of images/svg; go test runs them, and
//
//	go test ./src -run '^$' -fuzz '^FuzzSVGPath$' -fuzztime 5m -fuzzminimizetime 5s
//
// fuzzes. An input that fails is kept in testdata/fuzz, and go test runs it
// from then on.

// FuzzSVGImage fuzzes whole SVG documents.
func FuzzSVGImage(f *testing.F) {
	paths, err := filepath.Glob("../images/svg/*.svg")
	if err != nil || len(paths) == 0 {
		f.Fatal("no SVG icons")
	}
	for _, path := range paths {
		svg, err := os.ReadFile(path)
		if err != nil {
			f.Fatal(err)
		}
		f.Add(string(svg))
	}
	// The commands of a path, the colors and the stroke width, which the
	// icons do not use.
	f.Add(`<svg width="100" height="50" viewBox="0 0 100 50" fill="none" stroke="#ff0000" stroke-width="2">` +
		`<path d="M10 10 H40 V40 L20 30 Z" fill="red"/>` +
		`<path d="m10 10 c5 5 10 10 15 15 s5 5 10 10 q5 5 10 10 t5 5" stroke="none"/>` +
		`<path d="M10 10 A5 5 0 0 1 20 20 a5 5 0 1 0 10 10" fill="#0f0" stroke-width="1.5"/></svg>`)
	f.Fuzz(func(t *testing.T, svg string) {
		fuzzRun(t, len(svg), func() {
			fuzzDrawSVGDocument(t, svg, false)
		})
	})
}

// FuzzSVGPath fuzzes the path data, which it writes from the commands and the
// numbers of the input: every number is a whole number of at most 128, so the
// image is one a page holds and the document must be written. The numbers are
// written twice, plainly and as the formats of the input give them, and the
// two must draw the same picture: a number of path data is the same number
// written 12, 12.0, 1200e-2 or +12, with a comma, a space or nothing before
// it where its sign or its point separates it from the one before.
func FuzzSVGPath(f *testing.F) {
	f.Add("MLHVQTCSAZ", []byte{1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16}, []byte{0})
	f.Add("mlhvqtcsaz", []byte{0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15}, []byte{1, 2, 3})
	f.Add("MAAAA", []byte{1, 1, 0, 0, 1, 2, 2, 0, 1, 0, 3, 3}, []byte{2})
	f.Add("MLCQ", []byte{5, 6, 7, 8, 9, 10, 11, 12, 13, 14}, []byte{2, 4, 5, 3})
	f.Fuzz(func(t *testing.T, commands string, numbers, formats []byte) {
		plain := fuzzSVGPathData(commands, numbers, nil)
		written := fuzzSVGPathData(commands, numbers, formats)
		fuzzRun(t, len(plain)+len(written), func() {
			content := fuzzDrawSVG(t, plain, true)
			if other := fuzzDrawSVG(t, written, true); other != content {
				t.Fatalf("%q draws %q, and the same numbers written %q draw %q",
					plain, content, written, other)
			}
		})
	})
}

// fuzzSVGPathData returns the path data of the commands, with the arguments
// each one takes read from the numbers. A number is a whole number from -128
// to 127, and the flags of an arc are 0 or 1, so that the path data is always
// one PDFjet parses and every coordinate it makes fits on a page. The formats
// say how each number is written; without them every number is written
// plainly, separated by a space.
func fuzzSVGPathData(commands string, numbers, formats []byte) string {
	args := map[byte]int{
		'M': 2, 'L': 2, 'H': 1, 'V': 1, 'Q': 4, 'T': 2, 'C': 6, 'S': 4, 'A': 7, 'Z': 0,
	}
	var data strings.Builder
	next := 0
	number := func(flag bool) (int, byte) {
		value, format := 0, byte(0)
		if next < len(numbers) {
			value = int(int8(numbers[next]))
		}
		if len(formats) > 0 {
			format = formats[next%len(formats)]
		}
		next++
		if flag {
			return value & 1, format
		}
		return value, format
	}
	for i := 0; i < len(commands); i++ {
		command := commands[i]
		count, ok := args[command&^0x20] // The lower case commands are relative
		if !ok {
			continue
		}
		data.WriteByte(command)
		for j := 0; j < count; j++ {
			// The large arc and the sweep of an arc are flags, not numbers:
			// one character, 0 or 1, which no format of a number applies to.
			flag := command&^0x20 == 'A' && (j == 3 || j == 4)
			value, format := number(flag)
			if flag {
				fmt.Fprintf(&data, " %d", value)
				continue
			}
			text := fuzzSVGNumber(value, format)
			if !strings.HasPrefix(text, "-") && !strings.HasPrefix(text, "+") {
				// A number that does not begin with its sign needs a space or
				// a comma before it; the first one follows the command.
				if j == 0 || format%2 == 0 {
					data.WriteByte(' ')
				} else {
					data.WriteByte(',')
				}
			}
			data.WriteString(text)
		}
		data.WriteByte(' ')
	}
	return data.String()
}

// fuzzSVGNumber returns the whole number written as the format says: the same
// number every way SVG 1.1 section 8.3.9 allows, so that every way must draw
// the same picture.
func fuzzSVGNumber(value int, format byte) string {
	switch format % 6 {
	case 1:
		return fmt.Sprintf("%d.0", value)
	case 2:
		return fmt.Sprintf("%d00e-2", value)
	case 3:
		if value >= 0 {
			return fmt.Sprintf("+%d", value)
		}
		return fmt.Sprintf("%d", value)
	case 4:
		return fmt.Sprintf("%de0", value)
	case 5:
		return fmt.Sprintf("%de+0", value)
	}
	return fmt.Sprintf("%d", value)
}

// fuzzDrawSVG draws the path data on a page and returns what it drew.
func fuzzDrawSVG(t *testing.T, data string, mustDraw bool) string {
	return fuzzDrawSVGDocument(t,
		`<svg width="1000" height="1000" viewBox="0 0 1000 1000"><path d="`+data+`"/></svg>`,
		mustDraw)
}

// fuzzDrawSVGDocument draws the SVG on a page and writes the document, and
// returns what it drew. When the image is one of numbers a page holds, both
// must work: an image PDFjet cannot draw is one where it makes a coordinate
// of its own that a PDF cannot hold out of numbers that it can.
func fuzzDrawSVGDocument(t *testing.T, svg string, mustDraw bool) string {
	image, err := NewSVGImage(strings.NewReader(svg))
	if err != nil {
		if mustDraw {
			t.Fatalf("the path of numbers of a page is not read: %v", err)
		}
		return ""
	}
	pdf := NewPDF(bufio.NewWriter(io.Discard))
	page := NewPage(pdf, letter.Portrait())
	image.SetLocation(10, 10)
	image.DrawOn(page)
	content := string(page.GetContent())
	if err := pdf.Complete(); err != nil && mustDraw {
		t.Fatalf("the path of numbers of a page is not drawn: %v", err)
	}
	return content
}
