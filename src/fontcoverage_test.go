// fontcoverage_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"os"
	"strings"
	"testing"
	"unicode"
)

// Every face of the fonts PDFjet carries for Latin text has every letter of
// modern Greek and of the Cyrillic of Russian, Ukrainian, Belarusian,
// Bulgarian, Serbian and Macedonian: a document in one of them never shows an
// empty box (the owner, 29 October 2026, of ѝ, the Bulgarian "her", which
// JetBrains Mono does not have).

var testCoverageFamilies = []string{"IBMPlexSans", "IBMPlexSerif", "CascadiaMono"}

// testCoverageLetters returns the letters of modern Greek, U+0386 to U+03CE,
// and of Cyrillic, U+0400 to U+045F; what the code points between them are
// not, as U+0387, a punctuation mark, is skipped by the test.
func testCoverageLetters() []rune {
	var letters []rune
	for c := rune(0x0386); c <= 0x03CE; c++ {
		if unicode.IsLetter(c) {
			letters = append(letters, c)
		}
	}
	for c := rune(0x0400); c <= 0x045F; c++ {
		if unicode.IsLetter(c) {
			letters = append(letters, c)
		}
	}
	return letters
}

func TestFontCoverageEveryFaceForLatinTextHasGreekAndCyrillic(t *testing.T) {
	letters := testCoverageLetters()
	if len(letters) != 69+96 {
		t.Fatalf("%d letters", len(letters))
	}
	pdf := testNewPDF()
	faces := 0
	for _, family := range testCoverageFamilies {
		files, err := os.ReadDir(testRepoPath(t, "resources/fonts/"+family))
		if err != nil {
			t.Fatal(err)
		}
		for _, file := range files {
			if !strings.HasSuffix(file.Name(), ".ttf") {
				continue
			}
			buf, err := os.ReadFile(testRepoPath(t, "resources/fonts/"+family+"/"+file.Name()))
			if err != nil {
				t.Fatal(err)
			}
			font := NewFont(pdf, bytes.NewReader(buf))
			var missing []rune
			for _, c := range letters {
				if font.unicodeToGID.get(c) == 0 {
					missing = append(missing, c)
				}
			}
			if len(missing) != 0 {
				t.Errorf("%s has no %s", file.Name(), string(missing))
			}
			faces++
		}
	}
	if faces != 14+14+10 {
		t.Errorf("%d faces", faces)
	}
}
