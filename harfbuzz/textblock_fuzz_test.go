// textblock_fuzz_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package harfbuzz

import (
	"bufio"
	"io"
	"os"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// TextBlock and TextFrame shaped by PDFjet's HarfBuzz, go-text, the shaper
// this module makes the default by being imported: any mix of scripts, any
// width and size, drawn and completed with no error and no panic.
//
//	go test -run '^$' -fuzz FuzzHarfBuzzTextBlock -fuzztime 10m .

var fuzzPieces = []string{
	"Hello", "PDFjet", " ", " ", "\n", "नमस्ते", "क्षत्रिय", "्", "ि",
	"ភាសាខ្មែរ", "ព្រះ", "ៀ", "ภาษาไทย", "ั", "ພາສາລາວ", "မြန်မာ",
	"مرحبا", "שלום", "中文", "\u200B", "\u200C", "\u200D", "\uFEFF", "😀", "é",
}

func FuzzHarfBuzzTextBlock(f *testing.F) {
	f.Add([]byte{0, 2, 5, 2, 9, 2, 12, 2, 17}, uint16(200), uint8(12))
	f.Add([]byte{6, 7, 8, 21, 22, 10, 11}, uint16(20), uint8(30))
	root, err := os.Getwd()
	if err != nil {
		f.Fatal(err)
	}
	read := func(path string) []byte {
		data, err := os.ReadFile(root + "/../" + path)
		if err != nil {
			f.Fatal(err)
		}
		return data
	}
	fonts := [][]byte{
		read(world + "NotoSansDevanagari/NotoSansDevanagari-Regular.otf"),
		read(world + "NotoSansKhmer/NotoSansKhmer-Regular.otf"),
		read("resources/fonts/IBMPlexSans/IBMPlexSans-Regular.otf"),
	}
	f.Fuzz(func(t *testing.T, data []byte, width uint16, size uint8) {
		if len(data) > 200 {
			return
		}
		var b strings.Builder
		for _, c := range data {
			b.WriteString(fuzzPieces[int(c)%len(fuzzPieces)])
		}
		text := b.String()
		pdf := pdfjet.NewPDF(bufio.NewWriter(io.Discard))
		font := pdfjet.NewFontFromBytes(pdf, fonts[int(size)%2])
		fallback := pdfjet.NewFontFromBytes(pdf, fonts[2])
		font.SetSize(float32(size%40) + 1)
		page := pdfjet.NewPage(pdf, letter.Portrait())
		block := pdfjet.NewTextBlock(font, text).SetFallbackFont(fallback)
		block.SetWidth(float32(width%500) + 1)
		block.SetLocation(20, 20)
		block.DrawOn(nil)
		block.DrawOn(page)
		line := pdfjet.NewTextLine(font, strings.ReplaceAll(text, "\n", " "))
		line.SetFallbackFont(fallback)
		line.SetLocation(20, 600)
		line.DrawOn(page)
		frame := pdfjet.NewTextFrameFromParagraphs([]*pdfjet.Paragraph{pdfjet.NewParagraph().Add(line)}).
			SetWidth(float32(width%300) + 1)
		frame.SetHeight(2000)
		frame.SetLocation(20, 20)
		frame.DrawOn(pdfjet.NewPage(pdf, letter.Portrait()))
		if err := pdf.Complete(); err != nil {
			t.Fatalf("%q: %v", text, err)
		}
	})
}
