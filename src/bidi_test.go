// bidi_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import "testing"

// Right to left text in visual order: the Unicode Bidirectional Algorithm and Arabic shaping.

func testReorder(t *testing.T, want, input string) {
	t.Helper()
	if got := ReorderVisually(input); got != want {
		t.Errorf("%q: want %q, got %q", input, want, got)
	}
}

func TestBidiLeftToRightTextIsUnchanged(t *testing.T) {
	testReorder(t, "abc", "abc")
}

func TestBidiHebrewIsReversed(t *testing.T) {
	testReorder(t, "םולש", "שלום")
}

// Text pasted from a PDF often has presentation forms, and Persian and Urdu
// text the letters of Arabic Supplement.
func TestBidiTheOtherLettersOfRightToLeftScriptsAreRightToLeft(t *testing.T) {
	testReorder(t, "\uFB2C\uFB2A abc", "abc \uFB2A\uFB2C")                 // Hebrew presentation forms
	testReorder(t, "\uFEE2\uFE8D abc", "abc \uFE8D\uFEE2")                 // Arabic presentation forms B
	testReorder(t, "\uFB51\uFB50 abc", "abc \uFB50\uFB51")                 // Arabic presentation forms A
	testReorder(t, "\u0751\u0750 abc", "abc \u0750\u0751")                 // Arabic Supplement
	testReorder(t, "\u08A1\u08A0 abc", "abc \u08A0\u08A1")                 // Arabic Extended-A
	testReorder(t, "\u07CB\u07CA abc", "abc \u07CA\u07CB")                 // NKo
	testReorder(t, "\U00010801\U00010800 abc", "abc \U00010800\U00010801") // Cypriot
	for _, c := range []struct {
		ch   rune
		want int
	}{
		{0xFB2A, bidiR}, {0xFB29, bidiES}, {0xFE8D, bidiAL}, {0xFD3E, bidiON}, {0xFDFD, bidiON},
		{0xFEFF, bidiON}, {0x0750, bidiAL}, {0x0710, bidiAL}, {0x07CA, bidiR}, {0x1EE00, bidiAL},
	} {
		if got := bidiType(c.ch); got != c.want {
			t.Errorf("U+%04X: type %d, want %d", c.ch, got, c.want)
		}
	}
}

func TestBidiDigitsKeepTheirOrder(t *testing.T) {
	testReorder(t, "123 םולש", "שלום 123")
}

func TestBidiALineWithRightToLeftTextIsLaidOutRightToLeft(t *testing.T) {
	// The README: a line that has right to left text is a right to left line.
	testReorder(t, "םולש abc", "abc שלום")
}

func TestBidiBracketsAroundLeftToRightTextAreKeptWithItBetweenMarks(t *testing.T) {
	testReorder(t, "‎(abc)‎ םולש", "שלום (abc)")
	testReorder(t, "‏(ש‏)", "(ש)")
}

func TestBidiArabicIsShapedWithLigaturesAndReversed(t *testing.T) {
	// seen initial, lam alef final ligature, meem isolated
	testReorder(t, "ﻡﻼﺳ", "سلام")
}

func TestBidiZeroWidthNonJoinerIsKept(t *testing.T) {
	testReorder(t, "ﻢﻫﺍﻮﺧ‌ﯽﻣ", "می‌خواهم")
}

func TestBidiARangeIsReorderedInTheContextOfTheWholeString(t *testing.T) {
	// Go counts the range in bytes, where Java counts UTF-16 units (README,
	// Port differences): each Hebrew letter is two bytes.
	if got := ReorderVisuallyPart("שלום", 2, 6); got != "ול" {
		t.Errorf("got %q", got)
	}
}

func TestBidiIsArabicTellsArabicLettersApart(t *testing.T) {
	if !IsArabic(0x0633) || IsArabic('a') || IsArabic(0x05E9) {
		t.Error("wrong IsArabic")
	}
}
