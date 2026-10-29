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
    private let plexOTF = "tests/fonts/IBMPlexSans-Regular.otf"
    // Source Serif 4, whose kern feature has Extension lookups of its pairs
    // and a chained one, with LookupFlag 8, as Noto Sans, which these tests
    // read before 9.1.7, has; the lookups of IBM Plex Sans are pairs alone.
    private let serifTTF = "fonts/SourceSerif4/SourceSerif4-Regular.ttf"

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
        for path in [plexTTF, plexOTF, serifTTF] {
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

    // Returns how wide the text drawn in a core font is as the page draws it,
    // at the size: the widths of its characters, with the moves of the TJ
    // arrays before and between them, from the content of the page. It reads
    // the <([0-9A-F]*)> and (-?[0-9.]+) in each \[([^\]]*)\] TJ, as the other
    // ports do. The end of its last character is then where stringWidth says
    // the text ends.
    private func coreDrawnWidth(_ font: Font, _ content: String, _ size: Float) -> Float {
        var units: Float = 0
        let metrics = font.metrics!
        for piece in content.components(separatedBy: "] TJ").dropLast() {
            guard let open = piece.lastIndex(of: "[") else {
                continue
            }
            let array = Array(piece[piece.index(after: open)...].unicodeScalars)
            var i = 0
            while i < array.count {
                if array[i] == "<" {
                    var j = i + 1
                    while j + 2 <= array.count && array[j] != ">" {
                        let hex = String(String.UnicodeScalarView(array[j..<(j + 2)]))
                        units += Float(metrics[Int(hex, radix: 16)! - 32][1])
                        j += 2
                    }
                    i = j + 1
                } else {
                    var j = i
                    while j < array.count && array[j] != "<" {
                        j += 1
                    }
                    units -= Float(String(String.UnicodeScalarView(array[i..<j])))!
                    i = j
                }
            }
        }
        return units * size / 1000
    }

    // Colored text in a core font kerns the pairs across its words and its
    // color changes, as stringWidth measures them: Helvetica kerns "Y." (KPX Y
    // period), and a red "Y" before the period was drawn without it (the
    // review of 28 October 2026). A color changes only between words, so a
    // pair inside a word, as "AV", is never split by one.
    @Test func kerningOfColoredCoreFontTextIsDrawnAsItIsMeasured() throws {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        font.setKernPairs(true)
        let colors: [String : Int32] = ["Y": 0xFF0000, "To": 0x0000FF]
        for text in [
            "Y.",
            "AVATAR Y. To \"Wally\", LT: P.V. said.",
            "T\u{00ad}oo, Y\u{00ad}.",
        ] {
            let page = Page(pdf, Letter.PORTRAIT)
            let start = TestSupport.content(page).count
            page.drawColoredString(font, text, [0, 0, 0], colors)
            let content = String(TestSupport.content(page).dropFirst(start))
            TestSupport.expectNear(font.stringWidth(12, text), coreDrawnWidth(font, content, 12), 0.001, "\(text)")
        }
        // "Y." kerns by 140 units, the period moved as the TJ array it starts
        let page = Page(pdf, Letter.PORTRAIT)
        let start = TestSupport.content(page).count
        page.drawColoredString(font, "Y.", [0, 0, 0], colors)
        let content = String(TestSupport.content(page).dropFirst(start))
        #expect(content.contains("[140<2E>] TJ"), "the period is not kerned:\n\(content)")
    }

    // Text in a core font that is not colored is drawn as before the kerning
    // of colored text across its words: one TJ array, kerned inside it.
    @Test func kerningOfCoreFontTextNotColoredIsUnchanged() throws {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        font.setKernPairs(true)
        let page = Page(pdf, Letter.PORTRAIT)
        let start = TestSupport.content(page).count
        page.drawString(font, 12, "AV Y. To", 50, 50)
        #expect(String(TestSupport.content(page).dropFirst(start))
                == "BT\n/F1 12 Tf\n50 742 Td\n0 0 0 rg\n[<41>70<5620>90<59>140<2E>60<20>50<54>120<6F>] TJ\nET\n")
    }

    @Test func kerningIsOffUnlessAskedFor() throws {
        let pdf = TestSupport.newPDF()
        let font = try font(pdf, plexTTF, false)
        let content = String(decoding: draw(pdf, font, "AVATAR To"), as: UTF8.self)
        #expect(!content.contains("] TJ"), "a font not kerned moved its glyphs:\n\(content)")
    }

    @Test func kerningLeavesMarksAndRightToLeftTextAlone() throws {
        let pdf = TestSupport.newPDF()
        // IBM Plex Sans Hebrew kerns שלום עולם by 20 units without its
        // marks, as Noto Sans Hebrew, which this test read before 9.1.7,
        // kerned it by 10.
        for path in [
            "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Regular.ttf",
            "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Regular.ttf",
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
        // A pair kerned wider, "T after a quotation mark, takes its room only
        // with its second character: every start of the text fits in its width
        let wider = Array("\"Tea\" &co".unicodeScalars)
        for n in 1...wider.count {
            var start = String.UnicodeScalarView()
            start.append(contentsOf: wider[0..<n])
            let w = font.stringWidth(font.getSize(), String(start))
            let fit = font.getFitChars(String(String.UnicodeScalarView(wider)), w + 0.001)
            #expect(fit == n, "\(fit) characters fit in the width of \(n)")
        }
    }

    @Test func kerningOfABrokenTableIsNone() {
        let buf: [UInt8] = [0, 1, 0, 0, 0, 10, 0, 10, 0, 10, 0xFF, 0xFF, 0xFF, 0xFF]
        let gpos = GPOS.of(buf, 0, buf.count, 10, false)
        let kerning = Kerning.of(gpos)!
        let v = kerning.readTables(nil).pair(1, 2)
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
            // Source Serif 4, from its hmtx and kern lookups (Noto Sans, before
            // 9.1.7: 75.83 and -310, 56.79 and -150, 24.62 and -80)
            (serifTTF, "AVATAR To Wally", 80.43, -599),
            (serifTTF, "Тъй ТАТЯНА", 61.50, -215),
            (serifTTF, "ΑΥΤΟ", 26.34, -115),
        ]
        for c in cases {
            let font = try font(pdf, c.path, true)
            TestSupport.expectNear(c.kerned, font.stringWidth(10, c.text), 0.005, "\(c.path): \(c.text)")
            #expect(font.textKerning(c.text) == c.unitsOf, "\(c.path): \(c.text) in font units")
        }
    }

    // Returns a GPOS table whose kern feature has the lookups, all one lookup
    // of the subtables, all one PairPos subtable of the format, which moves
    // the glyph after itself by -50: the pair of the glyph, or the pair of its
    // class 1.
    private func kernTable(_ lookups: Int, _ subtables: Int, _ format: Int, _ glyph: Int) -> [UInt8] {
        var v = [1, 0, 10, 30, 42 + 2*lookups]
        v += [1, 0x4446, 0x4C54, 8]     // DFLT
        v += [4, 0, 0, 0xFFFF, 1, 0]
        v += [1, 0x6B65, 0x726E, 8]     // kern
        v += [0, lookups]
        v += Array(0..<lookups)
        v += [lookups]
        v += [Int](repeating: 2 + 2*lookups, count: lookups)
        v += [2, 0, subtables]
        v += [Int](repeating: 6 + 2*subtables, count: subtables)
        if format == 1 {
            v += [1, 12, 4, 0, 1, 18]       // The coverage at 12, the pair set at 18
            v += [1, 1, glyph]
            v += [1, glyph, -50]
        } else {
            v += [2, 24, 4, 0, 30, 30, 2, 2]    // The coverage at 24, the classes at 30
            v += [0, 0, 0, -50]
            v += [1, 1, glyph]
            v += [1, glyph, 1, 1]
        }
        var buf = [UInt8]()
        for value in v {
            buf.append(UInt8(truncatingIfNeeded: value >> 8))
            buf.append(UInt8(truncatingIfNeeded: value))
        }
        return buf
    }

    private func kerning(_ buf: [UInt8], _ numGlyphs: Int) -> Kerning {
        return Kerning.of(GPOS.of(buf, 0, buf.count, numGlyphs, false))!.readTables(nil)
    }

    // A table of a few kilobytes whose subtables named glyph 65535 again and
    // again made gigabytes of arrays by glyph; the room they make is work,
    // and a subtable a lookup names again is read once (the review of 28
    // October 2026).
    @Test func kerningOfATableThatNamesGlyph65535AgainIsBounded() {
        for (lookups, subtables, format) in [(1, 400, 1), (400, 1, 1), (400, 1, 2)] {
            let room = kerning(kernTable(lookups, subtables, format, 65535), 0).room()
            #expect(room <= GPOS.maxGposWork,
                    "\(lookups) lookups, \(subtables) subtables, format \(format): \(room) words")
        }
        let v = kerning(kernTable(1, 400, 1, 65535), 0).pair(65535, 65535)
        #expect(v == -50, "the subtable named again kerns \(v)")
        // A glyph past the glyphs of the font is left out; a glyph of the
        // font kerns as before.
        for format in [1, 2] {
            let k = kerning(kernTable(1, 1, format, 65535), 1000)
            #expect(k.room() == 0 && k.pair(65535, 65535) == 0, "format \(format): a glyph the font does not have")
            let v = kerning(kernTable(1, 1, format, 7), 1000).pair(7, 7)
            #expect(v == -50, "format \(format): glyph 7 kerns \(v)")
        }
    }
}
