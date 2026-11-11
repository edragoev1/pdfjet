/**
 * BigTableWrapTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

@Suite struct BigTableWrapTests {
    private let header = ["Name", "City", "Total"]

    /// A hundred rows, with a delimiter inside a field, and a short row near the
    /// end, which is skipped: the rows of BigTableTests.
    private func rows() -> [[String]] {
        var rows = [[String]]()
        for i in 0..<100 {
            if i == 90 {
                rows.append(["short"])
            }
            rows.append(["n\(i)", "City, \(i)", "\(i).5"])
        }
        return rows
    }

    /// Draws the rows in a table of the header's columns, with wrapping on or
    /// off, and returns its pages.
    private func table(_ pdf: PDF, _ font: Font, _ wrap: Bool, _ header: [String], _ rows: [[String]],
            _ set: ((BigTable) -> Void)? = nil) throws -> [Page] {
        let table = BigTable(pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(header.count)
        table.setTableData(header, rows)
        table.setWrapText(wrap)
        set?(table)
        table.setLocation(20, 20)
        try table.complete()
        return table.getPages()
    }

    /// A generator of the same numbers on every run, SplitMix64.
    private struct Numbers {
        var state: UInt64

        mutating func next(_ below: Int) -> Int {
            state &+= 0x9E37_79B9_7F4A_7C15
            var z = state
            z = (z ^ (z >> 30)) &* 0xBF58_476D_1CE4_E5B9
            z = (z ^ (z >> 27)) &* 0x94D0_49BB_1331_11EB
            z ^= z >> 31
            return Int(z % UInt64(below))
        }
    }

    /// A text of n words of the generator's lengths.
    private func words(_ numbers: inout Numbers, _ n: Int) -> String {
        var words = [String]()
        for _ in 0..<n {
            let letter = Character(Unicode.Scalar(UInt8(97 + numbers.next(26))))
            words.append(String(repeating: letter, count: 1 + numbers.next(9)))
        }
        return words.joined(separator: " ")
    }

    /// The strings the page draws, in the order they are drawn. A core font
    /// draws a character as the one byte of its code.
    private func drawnText(_ page: Page) -> [String] {
        let content = TestSupport.latin1(page.getContent())
        let regex = try! NSRegularExpression(pattern: "\\[<([0-9A-Fa-f]*)>\\] TJ")
        var list = [String]()
        for m in regex.matches(in: content, range: NSRange(content.startIndex..., in: content)) {
            let hex = (content as NSString).substring(with: m.range(at: 1))
            var text = ""
            var i = hex.startIndex
            while let next = hex.index(i, offsetBy: 2, limitedBy: hex.endIndex) {
                text.append(Character(UnicodeScalar(UInt8(hex[i..<next], radix: 16)!)))
                i = next
            }
            list.append(text)
        }
        return list
    }

    // A table whose columns fit its page draws the same with wrapping on as
    // off, byte for byte: nothing wraps, and the rows and pages are where they
    // were.
    @Test func onATableThatFitsDrawsTheSameAsOff() throws {
        func draw(_ wrap: Bool) throws -> [String] {
            let pdf = TestSupport.newPDF()
            return try table(pdf, TestSupport.helvetica(pdf), wrap, header, rows()).map { TestSupport.content($0) }
        }
        let off = try draw(false)
        let on = try draw(true)
        #expect(off == on, "wrapping a table that fits changed it: \(off.count) pages off, \(on.count) on")
    }

    // A field too long for its column wraps onto more lines, its words whole
    // and in their order, no line wider than the column, and nothing cut.
    @Test func aLongFieldWrapsWholeWithinItsColumn() throws {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let text = "The quick brown fox jumps over the lazy dog, again and again, until the page "
                + "is far too narrow for the whole of the sentence on one line of the table."
        let pages = try table(pdf, font, true, ["No", "Description", "Total"], [["1", text, "2.5"]])
        let drawn = drawnText(pages[0])
        // The header, then the row's three fields, the description on lines
        try #require(drawn.count >= 6, "the page drew \(drawn)")
        let lines = Array(drawn[4..<(drawn.count - 2)])     // Before the total and the footer
        #expect(lines.joined(separator: " ") == text)
        for line in lines {
            #expect(!line.hasSuffix(" ..."), "a line was cut: \(line)")
            #expect(font.stringWidth(line) <= Letter.PORTRAIT.getWidth() - 40, "a line is wider than the table: \(line)")
        }
    }

    // A word longer than its column is broken between its characters, the
    // pieces in their order.
    @Test func aWordLongerThanItsColumnIsBroken() throws {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let word = String(repeating: "abcdefghij", count: 12)
        let lines = BigTable.wrapText(word, BigTable.TextMeasure(font: font, fallback: nil), 60)
        #expect(lines.count >= 2 && lines.joined() == word, "the word broke as \(lines)")
        for line in lines {
            #expect(font.stringWidth(line) <= 60, "a piece is wider than the column: \(line)")
        }
        // A column too narrow for one character still draws one on each line
        #expect(BigTable.wrapText("abc", BigTable.TextMeasure(font: font, fallback: nil), 0.5) == ["a", "b", "c"])
        // Spaces are where lines break, not text: an empty field is one line
        #expect(BigTable.wrapText("  ", BigTable.TextMeasure(font: font, fallback: nil), 60) == [""])
        // A character is a code point, as in the other ports: an "e" and its
        // combining accent can go on two lines.
        let accents = String(repeating: "e\u{0301}", count: 3)
        #expect(BigTable.wrapText(accents, BigTable.TextMeasure(font: font, fallback: nil), 0.5).count == 6)
    }

    // The narrow columns keep their widths, and the wide ones share the rest of
    // the page equally.
    @Test func theNarrowColumnsKeepTheirWidths() throws {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let long = String(repeating: "word ", count: 60)
        let table = BigTable(pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(4)
        table.setTableData(["No", "A", "B", "Total"], [["1", long, long, "12.50"]])
        table.setWrapText(true)
        table.setLocation(20, 20)
        let natural = table.widths
        #expect(table.textWidths[0] == natural[0] && table.textWidths[3] == natural[3],
                "a narrow column lost its width: \(table.textWidths) of \(natural)")
        #expect(table.textWidths[1] == table.textWidths[2] && table.textWidths[1] < natural[1],
                "the wide columns do not share the rest equally: \(table.textWidths)")
        #expect(table.vertLines[4] <= Letter.PORTRAIT.getWidth() - 20 + 0.01,
                "the table is wider than its page: \(table.vertLines[4])")
    }

    // The footer of every page says how many pages there are, however the rows
    // wrap: the pages are counted as they are drawn.
    @Test func thePagesAreCountedAsTheyAreDrawn() throws {
        for seed in UInt64(1)...20 {
            var numbers = Numbers(state: seed)
            var rows = [[String]]()
            for i in 0..<(40 + numbers.next(120)) {
                rows.append(["\(i)", words(&numbers, numbers.next(60)), words(&numbers, numbers.next(12))])
            }
            let padding = Float(numbers.next(4))
            let pdf = TestSupport.newPDF()
            let pages = try table(pdf, TestSupport.helvetica(pdf), true, ["No", "Notes", "Name"], rows) {
                $0.setVerticalPadding(padding)
            }
            let last = "Page \(pages.count) of \(pages.count)"
            #expect(TestSupport.content(pages[pages.count - 1]).contains(TestSupport.hex(last)),
                    "seed \(seed): \(pages.count) pages, and the last does not say \(last)")
        }
    }

    // A row that does not fit what is left of a page starts the next page whole.
    @Test func aRowThatDoesNotFitStartsTheNextPage() throws {
        var rows = [[String]]()
        for i in 0..<50 {
            rows.append(["\(i)", "short"])
        }
        rows.append(["tall", String(repeating: "several words of a long note ", count: 40)])
        let pdf = TestSupport.newPDF()
        let pages = try table(pdf, TestSupport.helvetica(pdf), true, ["No", "Notes"], rows)
        try #require(pages.count >= 2, "\(pages.count) pages")
        #expect(!drawnText(pages[0]).joined(separator: "|").contains("|tall|"), "the tall row began on the first page")
        #expect(drawnText(pages[pages.count - 1]).joined(separator: " ").contains("tall"),
                "the tall row is not on the last page")
    }

    // A row taller than a whole page is cut at the lines that fit it, its last
    // line ending in the mark, and the cell of a PDF/UA document keeps the
    // whole of its text.
    @Test func aRowTallerThanAPageIsCutAndItsCellKeepsTheText() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.coreFontsInCompliance = true    // Draws in a core font
        memory.pdf.setTitle("Test")
        let font = try Font(memory.pdf, CoreFont.HELVETICA).setSize(12)
        let text = String(repeating: "a note that runs on and on ", count: 400)
                .trimmingCharacters(in: .whitespaces)
        let pages = try table(memory.pdf, font, true, ["No", "Notes"], [["1", text]])
        try #require(pages.count == 1, "\(pages.count) pages")
        let drawn = drawnText(pages[0])
        #expect(drawn[drawn.count - 2].hasSuffix(" ..."),
                "the last line drawn does not end in the mark: \(drawn[drawn.count - 2])")
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        var alt = "feff"
        for unit in text.utf16 {
            alt += String(format: "%04x", unit)
        }
        #expect(raw.contains("/Alt <" + alt + ">"), "the cell does not keep the whole of the text")
    }

    // A header too long for its column wraps too, and the rows start under it.
    @Test func theHeaderWrapsToo() throws {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let header = ["No", "A heading of many words for a narrow column of short values"]
        let table = BigTable(pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(2)
        table.setTableData(header, [["1", "x"]]).setWrapText(true).setColumnWidthsInPercent(80, 20)
        table.setLocation(20, 20)
        try #require(table.headerCount >= 2, "the header did not wrap: \(table.headerLines)")
        try table.complete()
        let drawn = drawnText(table.getPages()[0])
        #expect(drawn[drawn.count - 2] == "x", "the page drew \(drawn)")
    }

    // Wrapping reads the rows once more, to count the pages: three times in
    // all, and none of them kept.
    @Test func readsTheRowsThreeTimes() throws {
        let rows = self.rows()
        final class Counter {
            var opened = 0
        }
        let counter = Counter()
        let source = AnySequence { () -> IndexingIterator<[[String]]> in
            counter.opened += 1
            return rows.makeIterator()
        }
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let table = BigTable(pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(3)
                .setTableData(header, source).setWrapText(true)
        table.setLocation(10, 10)
        try table.complete()
        #expect(counter.opened == 3)
    }

    // Wrapping turned off again draws the table as it was never on.
    @Test func turnedOffAgainIsAsNeverOn() throws {
        let long = String(repeating: "word ", count: 80)
        func draw(_ set: ((BigTable) -> Void)?) throws -> String {
            let pdf = TestSupport.newPDF()
            let pages = try table(pdf, TestSupport.helvetica(pdf), false, ["No", "Notes"],
                    [["1", long], ["2", "short"]], set)
            return TestSupport.content(pages[0])
        }
        let never = try draw(nil)
        let again = try draw { $0.setWrapText(true).setWrapText(false) }
        #expect(never == again, "turned on and off, the table is not drawn as before")
    }

    // The fallback fonts measure and draw the characters the fonts of the
    // table do not have: a Korean answer in a table of a Latin font is in the
    // Korean font, its column as wide as it is drawn; without them, nothing
    // changes.
    @Test func theFallbackFontsDrawWhatTheFontsDoNotHave() throws {
        let text = "안녕하세요 반갑습니다"
        func table(_ fallback: Bool) throws -> BigTable {
            let pdf = TestSupport.newPDF()
            let f1 = try Font(pdf, IBMPlexSans.SemiBold)
            let f2 = try Font(pdf, IBMPlexSans.Regular)
            let table = BigTable(pdf, f1, f2, Letter.PORTRAIT).setNumberOfColumns(2)
            if fallback {
                table.setFallbackFonts(try Font(pdf, IBMPlexSansKR.SemiBold), try Font(pdf, IBMPlexSansKR.Regular))
            }
            table.setTableData(["No", "Answer"], [["1", text]])
            table.setLocation(20, 20)
            try table.complete()
            return table
        }
        let with = try table(true)
        let without = try table(false)
        #expect(with.widths[1] > without.widths[1],
                "the answer is not measured in the Korean font: \(with.widths[1]), without it \(without.widths[1])")
        let content = TestSupport.content(with.getPages()[0])
        #expect(content.contains("Tf") && content.components(separatedBy: " Tf").count - 1 >= 3,
                "the Korean font is not drawn with: \(content)")
    }
}
