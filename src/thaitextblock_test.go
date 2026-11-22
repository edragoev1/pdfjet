// thaitextblock_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"io"
	"os"
	"strings"
	"testing"
)

// Thai text without zero width spaces breaks between its words, where ICU's
// dictionary puts their ends: the Thai UDHR, each paragraph 300 points wide
// in IBM Plex Sans Thai 12, in the lines Chrome breaks it into
// (tests/data/dictionarybreaks/thai-textblock-300.txt). Before the
// dictionary, 23 of its 165 breaks were inside a word.
func TestThaiTextBlockBreaksBetweenWordsAsChrome(t *testing.T) {
	t.Chdir("..")
	pdf := NewPDF(bufio.NewWriter(io.Discard))
	font := NewFontFromFile(pdf, "resources/fonts/world/IBMPlexSansThai/IBMPlexSansThai-Regular.otf")
	font.SetSize(12)
	text, err := os.ReadFile("resources/data/udhr/udhr_tha.txt")
	if err != nil {
		t.Fatal(err)
	}
	expected, err := os.ReadFile("tests/data/dictionarybreaks/thai-textblock-300.txt")
	if err != nil {
		t.Fatal(err)
	}
	var got []string
	for _, paragraph := range strings.Split(strings.TrimSpace(string(text)), "\n") {
		for _, line := range NewTextBlock(font, paragraph).SetWidth(300).getTextLines() {
			got = append(got, strings.TrimRight(line.text, " "))
		}
		got = append(got, "")
	}
	want := strings.Split(strings.TrimSpace(string(expected)), "\n")
	gotText := strings.TrimSpace(strings.Join(got, "\n"))
	if gotText != strings.Join(want, "\n") {
		gotLines := strings.Split(gotText, "\n")
		for i := 0; i < len(want) && i < len(gotLines); i++ {
			if gotLines[i] != want[i] {
				t.Fatalf("line %d: %q, Chrome %q", i+1, gotLines[i], want[i])
			}
		}
		t.Fatalf("%d lines, Chrome %d", len(gotLines), len(want))
	}
}
