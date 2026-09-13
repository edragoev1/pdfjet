/**
 * FontTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

@Suite struct FontTests {
    @Test func coreFontWidthsComeFromTheAfmMetrics() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        #expect(font.getName() == "Helvetica")
        #expect(font.getSize() == 12)
        // H 722, e 556, l 222, l 222, o 556 in 1/1000 em.
        TestSupport.expectNear(27.336, font.stringWidth(12, "Hello"), 0.001)
        TestSupport.expectNear(27.336, font.stringWidth("Hello"), 0.001)
        _ = font.setSize(24)
        TestSupport.expectNear(54.672, font.stringWidth("Hello"), 0.001)
    }

    @Test func theWidthOfNoTextIsZero() {
        #expect(TestSupport.helvetica(TestSupport.newPDF()).stringWidth(nil) == 0)
    }

    @Test func kerningPairsNarrowTheText() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        TestSupport.expectNear(16.008, font.stringWidth(12, "AV"), 0.001)
        _ = font.setKernPairs(true)
        // KPX A V -70
        TestSupport.expectNear(15.168, font.stringWidth(12, "AV"), 0.001)
    }

    @Test func coreFontVerticalMetrics() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        TestSupport.expectNear(11.172, font.getAscent(12), 0.001)
        TestSupport.expectNear(2.7, font.getDescent(12), 0.001)
        TestSupport.expectNear(13.872, font.getBodyHeight(12), 0.001)
    }

    @Test func getFitCharsCountsTheCharactersThatFit() {
        #expect(TestSupport.helvetica(TestSupport.newPDF()).getFitChars("Hello world", 30) == 5)
    }

    @Test func everyCjkCharacterIsOneEmWideAndSurrogatePairsCountOnce() {
        let font = Font(TestSupport.newPDF(), CJKFont.ADOBE_MING_STD_LIGHT)
        #expect(font.stringWidth(10, "日本") == 20)
        #expect(font.stringWidth(10, "\u{2000B}") == 10)
    }

    @Test(.enabled(if: TestSupport.exists("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"),
            "the fonts directory is not here"))
    func readsAStreamFont() throws {
        let stream = TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream")
        let font = try Font(TestSupport.newPDF(), stream)
        #expect(font.getName() == "IBMPlexSans")
        TestSupport.expectNear(28.32, font.stringWidth(12, "Hello"), 0.001)
    }
}
