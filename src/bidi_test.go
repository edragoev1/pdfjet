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
