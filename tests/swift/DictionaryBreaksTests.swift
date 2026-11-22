/**
 * DictionaryBreaksTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

@Suite struct DictionaryBreaksTests {
    // Every run of Thai, Lao, Khmer and Burmese in the UDHR is broken into the
    // words ICU breaks it into (tests/data/dictionarybreaks/expected.tsv): the
    // same dictionaries, ICU's engines ported.
    @Test func wordEndsAreICUs() throws {
        let rows = try String(contentsOfFile: TestSupport.path("tests/data/dictionarybreaks/expected.tsv"),
                encoding: .utf8).split(separator: "\n")
        var runs = [String: Int]()
        var differ = [String: Int]()
        var examples = [String]()
        for row in rows where !row.hasPrefix("#") {
            let fields = row.split(separator: "\t", omittingEmptySubsequences: false).map(String.init)
            let want = fields[2].split(separator: ",").compactMap { Int($0) }
            let got = DictionaryBreaks.wordEnds(DictionaryBreaks.codePoints(fields[1]))
            runs[fields[0], default: 0] += 1
            if got != want {
                differ[fields[0], default: 0] += 1
                if examples.count < 3 {
                    examples.append("\(fields[0]) \(fields[1]): \(got), ICU \(want)")
                }
            }
        }
        for code in ["tha", "lao", "khm", "mya"] {
            #expect((runs[code] ?? 0) >= 300, "\(code)")
            #expect(differ[code] == nil, "\(code) \(examples)")
        }
    }

    // Thai text without zero width spaces breaks between its words: the Thai
    // UDHR, each paragraph 300 points wide in IBM Plex Sans Thai 12, in the
    // lines Chrome breaks it into (tests/data/dictionarybreaks/thai-textblock-300.txt).
    @Test func thaiTextBlockBreaksBetweenWordsAsChrome() throws {
        let pdf = TestSupport.newPDF()
        let font = try Font(pdf, TestSupport.path("resources/fonts/world/IBMPlexSansThai/IBMPlexSansThai-Regular.otf"))
        _ = font.setSize(12)
        var got = [String]()
        for paragraph in try String(contentsOfFile: TestSupport.path("resources/data/udhr/udhr_tha.txt"),
                encoding: .utf8).split(separator: "\n") {
            let block = TextBlock(font, String(paragraph))
            _ = block.setWidth(300)
            for line in block.getTextLines() {
                var text = line.text ?? ""
                while text.hasSuffix(" ") {
                    text.removeLast()
                }
                got.append(text)
            }
            got.append("")
        }
        let want = try String(contentsOfFile: TestSupport.path("tests/data/dictionarybreaks/thai-textblock-300.txt"),
                encoding: .utf8).trimmingCharacters(in: .whitespacesAndNewlines)
                .split(separator: "\n", omittingEmptySubsequences: false).map(String.init)
        while got.last == "" {
            got.removeLast()
        }
        for i in 0..<min(got.count, want.count) {
            #expect(got[i] == want[i], "line \(i + 1)")
            if got[i] != want[i] {
                break
            }
        }
        #expect(got.count == want.count)
    }
}
