/**
 * KerningTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

@Suite struct KerningTests {
    private let plexTTF = "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"
    private let plexOTF = "fonts/IBMPlexSans/IBMPlexSans-Regular.otf"
    private let notoSans = "fonts/NotoSans/NotoSans-Regular.ttf"

    // Opens a font of the repository, with its kerning on or off.
    private func font(_ pdf: PDF, _ path: String, _ kerned: Bool) throws -> Font {
        let font = try Font(pdf, TestSupport.open(path))
        font.setKernPairs(kerned)
        return font
    }

    // Returns how wide the text is as the page draws it, at the size: the
    // advances of its glyphs, with the moves of the TJ arrays between them,
    // from the content of the page. It reads what <([0-9A-F]*)> Tj and
    // \[(-?[0-9.]+)\] TJ match, as the other ports do.
    private func drawnWidth(_ font: Font, _ content: [UInt8], _ size: Float) -> Float {
        var units: Float = 0
        var moves: Float = 0
        let n = content.count
        func follows(_ i: Int, _ text: String) -> Bool {
            let bytes = Array(text.utf8)
            return i + bytes.count <= n && Array(content[i..<(i + bytes.count)]) == bytes
        }
        func isHex(_ b: UInt8) -> Bool {
            return (b >= UInt8(ascii: "0") && b <= UInt8(ascii: "9")) || (b >= UInt8(ascii: "A") && b <= UInt8(ascii: "F"))
        }
        func isNumber(_ b: UInt8) -> Bool {
            return (b >= UInt8(ascii: "0") && b <= UInt8(ascii: "9")) || b == UInt8(ascii: ".")
        }
        var i = 0
        while i < n {
            if content[i] == UInt8(ascii: "<") {
                var j = i + 1
                while j < n && isHex(content[j]) {
                    j += 1
                }
                if follows(j, "> Tj") {
                    var k = i + 1
                    while k + 4 <= j {
                        let gid = Int(String(decoding: content[k..<(k + 4)], as: UTF8.self), radix: 16)!
                        units += Float(font.glyphAdvance(gid))
                        k += 4
                    }
                    i = j + 4
                    continue
                }
            } else if content[i] == UInt8(ascii: "[") {
                var j = i + 1
                if j < n && content[j] == UInt8(ascii: "-") {
                    j += 1
                }
                while j < n && isNumber(content[j]) {
                    j += 1
                }
                if j > i + 1 && follows(j, "] TJ") {
                    moves += Float(String(decoding: content[(i + 1)..<j], as: UTF8.self))!
                    i = j + 4
                    continue
                }
            }
            i += 1
        }
        return units * size / Float(font.unitsPerEm) - moves * size / 1000
    }

    // Draws the text with the font on a page of its own, and returns what the
    // page wrote for it.
    private func draw(_ pdf: PDF, _ font: Font, _ text: String) -> [UInt8] {
        let page = Page(pdf, Letter.PORTRAIT)
        let start = page.buf.count
        page.drawString(font, nil, 10, text, 50, 50)
        return Array(page.buf[start...])
    }

    @Test func kerningNarrowsTheKernedPairsOfAnOpenTypeFont() throws {
        let pdf = TestSupport.newPDF()
        let kerned = try font(pdf, plexTTF, true)
        let plain = try font(pdf, plexTTF, false)
        for pair in ["AV", "To", "Ty", "LT"] {
            #expect(kerned.stringWidth(10, pair) < plain.stringWidth(10, pair),
                    "\(pair): kerned \(kerned.stringWidth(10, pair)), not kerned \(plain.stringWidth(10, pair))")
        }
        #expect(kerned.stringWidth(10, "HH") == plain.stringWidth(10, "HH"), "HH kerned")
    }

    @Test func kerningIsDrawnAsItIsMeasured() throws {
        let pdf = TestSupport.newPDF()
        for path in [plexTTF, plexOTF, notoSans] {
            let font = try font(pdf, path, true)
            for text in [
                "AVATAR. To Wally, Tyler and LT: \"Yes,\" P.V. said.",
                "Обява: Тъй, Уважаеми Г-н Петров, ТАТЯНА.",
                "Ταύτα, ΑΥΤΟ ΤΟ ΒΑΓΟΝΙ.",
                "A\u{00a0}V, then A V again",
                "soft\u{00ad}hy\u{200b}phen, T\u{feff}o",
            ] {
                let drawn = drawnWidth(font, draw(pdf, font, text), 10)
                TestSupport.expectNear(font.stringWidth(10, text), drawn, 0.01, "\(path): \(text)")
            }
        }
    }

    @Test func kerningOfColoredTextIsDrawnAsItIsMeasured() throws {
        let pdf = TestSupport.newPDF()
        let font = try font(pdf, plexTTF, true)
        let text = "AVATAR To \"Wally\" LT."
        let page = Page(pdf, Letter.PORTRAIT)
        let start = page.buf.count
        page.drawColoredString(font, text, [0, 0, 0], ["To": 0xFF0000])
        TestSupport.expectNear(font.stringWidth(12, text),
                drawnWidth(font, Array(page.buf[start...]), 12), 0.01, "colored")
        // A word with a mark, drawn on the shaping path, kerns with the last
        // glyph of the word before it, as stringWidth measures it
        let shaped = "A V. To Te\u{0301} LT"
        let shapedStart = page.buf.count
        page.drawColoredString(font, shaped, [0, 0, 0], ["To": 0xFF0000])
        TestSupport.expectNear(font.stringWidth(12, shaped),
                drawnWidth(font, Array(page.buf[shapedStart...]), 12), 0.01, "colored, shaped")
    }

    @Test func kerningIsOffUnlessAskedFor() throws {
        let pdf = TestSupport.newPDF()
        let font = try font(pdf, plexTTF, false)
        let content = String(decoding: draw(pdf, font, "AVATAR To"), as: UTF8.self)
        #expect(!content.contains("] TJ"), "a font not kerned moved its glyphs:\n\(content)")
    }

    @Test func kerningLeavesMarksAndRightToLeftTextAlone() throws {
        let pdf = TestSupport.newPDF()
        for path in [
            "fonts/NotoSansHebrew/NotoSansHebrew-Regular.ttf",
            "fonts/NotoSansArabic/NotoSansArabic-Regular.ttf",
        ] {
            let kerned = try font(pdf, path, true)
            let plain = try font(pdf, path, false)
            for text in ["שָׁלוֹם עוֹלָם", "مرحبا بالعالم"] {
                #expect(kerned.stringWidth(10, text) == plain.stringWidth(10, text), "\(path): \(text) kerned")
            }
        }
    }

    @Test func kerningFitsWhatIsMeasured() throws {
        let pdf = TestSupport.newPDF()
        let font = try font(pdf, plexTTF, true)
        let text = "AVATAR To Wally"
        let count = text.unicodeScalars.count
        // getFitChars measures at the font's own size
        let width = font.stringWidth(font.getSize(), text)
        let n = font.getFitChars(text, width + 0.001)
        #expect(n == count, "\(n) of \(count) characters fit in their own width")
        #expect(font.getFitChars(text, width - 0.1) != count, "all the characters fit in less than their width")
    }

    @Test func kerningOfABrokenTableIsNone() {
        let buf: [UInt8] = [0, 1, 0, 0, 0, 10, 0, 10, 0, 10, 0xFF, 0xFF, 0xFF, 0xFF]
        let gpos = GPOS.of(buf, 0, buf.count, 10, false)
        let kerning = Kerning.of(gpos, buf, FontTable(name: "kern", offset: 4, length: 900), 10, false)!
        let v = kerning.pair(1, 2)
        #expect(v == 0, "a broken table kerns: \(v)")
    }

    // The same widths in the four ports, at 10 points: what each port
    // measures, and so wraps and aligns, is the same to the hundredth of a
    // point.
    @Test func kerningWidthsAreThoseOfTheOtherPorts() throws {
        let pdf = TestSupport.newPDF()
        let cases: [(path: String, text: String, kerned: Float, unitsOf: Int)] = [
            (plexTTF, "AVATAR To Wally", 75.14, -302),
            (plexTTF, "Тъй ТАТЯНА", 55.94, -165),
            (plexTTF, "ΑΥΤΟ", 24.44, -70),
            (plexOTF, "AVATAR To Wally", 75.14, -302),
            (notoSans, "AVATAR To Wally", 75.83, -310),
            (notoSans, "Тъй ТАТЯНА", 56.79, -150),
            (notoSans, "ΑΥΤΟ", 24.62, -80),
        ]
        for c in cases {
            let font = try font(pdf, c.path, true)
            TestSupport.expectNear(c.kerned, font.stringWidth(10, c.text), 0.005, "\(c.path): \(c.text)")
            #expect(font.textKerning(c.text) == c.unitsOf, "\(c.path): \(c.text) in font units")
        }
    }
}
