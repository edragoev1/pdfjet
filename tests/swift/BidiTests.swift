/**
 * BidiTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

/// Right to left text in visual order: the Unicode Bidirectional Algorithm and Arabic shaping.
@Suite struct BidiTests {
    private func scalars(_ text: String) -> [UInt32] {
        return text.unicodeScalars.map { $0.value }
    }

    @Test func leftToRightTextIsUnchanged() {
        #expect(Bidi.reorderVisually("abc") == "abc")
    }

    @Test func hebrewIsReversed() {
        #expect(scalars(Bidi.reorderVisually("\u{05E9}\u{05DC}\u{05D5}\u{05DD}")) == [0x05DD, 0x05D5, 0x05DC, 0x05E9])
    }

    @Test func digitsKeepTheirOrder() {
        #expect(scalars(Bidi.reorderVisually("\u{05E9}\u{05DC}\u{05D5}\u{05DD} 123"))
                == [0x31, 0x32, 0x33, 0x20, 0x05DD, 0x05D5, 0x05DC, 0x05E9])
    }

    @Test func aLineWithRightToLeftTextIsLaidOutRightToLeft() {
        // The README: a line that has right to left text is a right to left line.
        #expect(scalars(Bidi.reorderVisually("abc \u{05E9}\u{05DC}\u{05D5}\u{05DD}"))
                == [0x05DD, 0x05D5, 0x05DC, 0x05E9, 0x20, 0x61, 0x62, 0x63])
    }

    @Test func bracketsAroundLeftToRightTextAreKeptWithItBetweenMarks() {
        #expect(scalars(Bidi.reorderVisually("\u{05E9}\u{05DC}\u{05D5}\u{05DD} (abc)"))
                == [0x200E, 0x28, 0x61, 0x62, 0x63, 0x29, 0x200E, 0x20, 0x05DD, 0x05D5, 0x05DC, 0x05E9])
        #expect(scalars(Bidi.reorderVisually("(\u{05E9})")) == [0x200F, 0x28, 0x05E9, 0x200F, 0x29])
    }

    @Test func arabicIsShapedWithLigaturesAndReversed() {
        // seen initial, lam alef final ligature, meem isolated
        #expect(scalars(Bidi.reorderVisually("\u{0633}\u{0644}\u{0627}\u{0645}")) == [0xFEE1, 0xFEFC, 0xFEB3])
    }

    @Test func zeroWidthNonJoinerIsKept() {
        let shaped = Bidi.reorderVisually("\u{0645}\u{06CC}\u{200C}\u{062E}\u{0648}\u{0627}\u{0647}\u{0645}")
        #expect(scalars(shaped) == [0xFEE2, 0xFEEB, 0xFE8D, 0xFEEE, 0xFEA7, 0x200C, 0xFBFD, 0xFEE3])
    }

    @Test func aRangeIsReorderedInTheContextOfTheWholeString() {
        #expect(scalars(Bidi.reorderVisually("\u{05E9}\u{05DC}\u{05D5}\u{05DD}", 1, 3)) == [0x05D5, 0x05DC])
    }

    @Test func isArabicTellsArabicLettersApart() {
        #expect(Bidi.isArabic(Character("\u{0633}")))
        #expect(!Bidi.isArabic(Character("a")))
        #expect(!Bidi.isArabic(Character("\u{05E9}")))
    }
}
