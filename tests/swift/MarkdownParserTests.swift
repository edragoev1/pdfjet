/**
 * MarkdownParserTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

@Suite struct MarkdownParserTests {
    // The blocks as text: H1(text), P(text), CODE(text), RULE, IMG(alt|source),
    // QUOTE[...], UL[...] or OL3[...] with its start, * after a list that is
    // loose, I[...] for an item, T[...] for a task and X[...] for a task that is
    // done, and TABLE(a,b;c,d|L,C) with its rows and alignments.
    static func describe(_ blocks: [MarkdownParser.Block]) -> String {
        var buf = ""
        for block in blocks {
            if !buf.isEmpty {
                buf += " "
            }
            switch block.kind {
            case .HEADING:
                buf += "H\(block.level)(\(block.text!))"
            case .PARAGRAPH:
                buf += "P(\(block.text!))"
            case .CODE:
                buf += "CODE(\(block.text!))"
            case .RULE:
                buf += "RULE"
            case .IMAGE:
                buf += "IMG(\(block.text!)|\(block.source!))"
            case .QUOTE:
                buf += "QUOTE[\(describe(block.children))]"
            case .LIST:
                buf += (block.ordered ? "OL\(block.start)" : "UL") + (block.loose ? "*" : "")
                        + "[\(describe(block.children))]"
            case .ITEM:
                buf += ["I", "T", "X"][block.task.rawValue] + "[\(describe(block.children))]"
            case .TABLE:
                let rows = block.rows!.map { $0.joined(separator: ",") }.joined(separator: ";")
                var alignments = ""
                for alignment in block.alignments! {
                    alignments += String(String(describing: alignment).prefix(1))
                }
                buf += "TABLE(\(rows)|\(alignments))"
            }
        }
        return buf
    }

    private func parse(_ text: String) -> String {
        return MarkdownParserTests.describe(MarkdownParser.parse(text))
    }

    @Test func headingsOfHashesAndOfUnderlines() {
        #expect(parse("# Title\n### Three\n## Closed ##") == "H1(Title) H3(Three) H2(Closed)")
        #expect(parse("#NoSpace\n\n####### Seven") == "P(#NoSpace) P(####### Seven)")
        #expect(parse("Big\nTitle\n===\n\nSmall\n---") == "H1(Big\nTitle) H2(Small)")
    }

    @Test func paragraphsAreSeparatedByEmptyLines() {
        #expect(parse("one\ntwo\n\n  \nthree") == "P(one\ntwo) P(three)")
        #expect(parse("a\r\n\r\nb") == "P(a) P(b)")
        #expect(parse("") == "")
        #expect(parse("\n \n") == "")
    }

    @Test func fencedAndIndentedCode() {
        #expect(parse("```java\nint x = 1;\n  y();\n```") == "CODE(int x = 1;\n  y();)")
        #expect(parse("~~~~\na\n```\nb\n~~~~") == "CODE(a\n```\nb)")
        #expect(parse("```\nunclosed") == "CODE(unclosed)")
        #expect(parse("text\n\n    code\n\n      more\n\n") == "P(text) CODE(code\n\n  more)")
        // A paragraph goes on over an indented line.
        #expect(parse("text\n    continued") == "P(text\ncontinued)")
    }

    @Test func thematicBreaks() {
        #expect(parse("a\n\n***\nb\n\n- - -\n___") == "P(a) RULE P(b) RULE RULE")
        #expect(parse("-- not") == "P(-- not)")
    }

    @Test func quotesNestAndGoOnWithoutTheirMark() {
        #expect(parse("> a\n> b") == "QUOTE[P(a\nb)]")
        #expect(parse("> a\nlazy") == "QUOTE[P(a\nlazy)]")
        #expect(parse("> # T\n> > deep") == "QUOTE[H1(T) QUOTE[P(deep)]]")
        #expect(parse("> a\n\nb") == "QUOTE[P(a)] P(b)")
    }

    @Test func bulletAndNumberedLists() {
        #expect(parse("- a\n- b") == "UL[I[P(a)] I[P(b)]]")
        #expect(parse("3. x\n4. y") == "OL3[I[P(x)] I[P(y)]]")
        #expect(parse("- a\n+ b") == "UL[I[P(a)]] UL[I[P(b)]]")
        #expect(parse("- a\nlazy") == "UL[I[P(a\nlazy)]]")
        #expect(parse("- a\n  - b\n- c") == "UL[I[P(a) UL[I[P(b)]]] I[P(c)]]")
        #expect(parse("- a\n\n- b") == "UL*[I[P(a)] I[P(b)]]")
        #expect(parse("- a\n\n  more") == "UL*[I[P(a) P(more)]]")
        #expect(parse("- a\n\nafter") == "UL[I[P(a)]] P(after)")
    }

    @Test func taskLists() {
        #expect(parse("- [x] done\n- [ ] open\n- plain") == "UL[X[P(done)] T[P(open)] I[P(plain)]]")
        #expect(parse("* [X] Done") == "UL[X[P(Done)]]")
        #expect(parse("1. [ ] first") == "OL1[T[P(first)]]")
        #expect(parse("- [ ] a\n  - [x] b") == "UL[T[P(a) UL[X[P(b)]]]]")
        #expect(parse("- [ ]\ttab") == "UL[T[P(tab)]]")
        // Not tasks: no space after the box, no text, another letter
        #expect(parse("- [ ]no space") == "UL[I[P([ ]no space)]]")
        #expect(parse("- [ ]") == "UL[I[P([ ])]]")
        #expect(parse("- [ ]   ") == "UL[I[P([ ])]]")
        #expect(parse("- [y] other") == "UL[I[P([y] other)]]")
        #expect(parse("[x] not in a list") == "P([x] not in a list)")
    }

    @Test func aNumberedListInterruptsAParagraphOnlyWhenItStartsAtOne() {
        #expect(parse("The year\n2026. was good") == "P(The year\n2026. was good)")
        #expect(parse("Steps:\n1. go") == "P(Steps:) OL1[I[P(go)]]")
    }

    @Test func tablesOfPipes() {
        #expect(parse("| A | B |\n|---|--:|\n| 1 | 2 |\n| 3 |") == "TABLE(A,B;1,2;3,|LR)")
        #expect(parse("a | b\n:-:|---\nx\\|y | z") == "TABLE(a,b;x\\|y,z|CL)")
        // The header and the delimiter row need the same number of cells.
        #expect(parse("a | b\n--- | --- | ---") == "P(a | b\n--- | --- | ---)")
    }

    @Test func anImageAloneInItsParagraph() {
        #expect(parse("![A map](images/map.png)") == "IMG(A map|images/map.png)")
        #expect(parse("See ![a](b.png) here") == "P(See ![a](b.png) here)")
        #expect(parse("![a](b c.png)") == "P(![a](b c.png))")
    }

    @Test func tabsAreSpacesToTheNextStopOfFour() {
        #expect(parse("\tx") == "CODE(x)")
        #expect(parse("-\ta\n\n\t\tb") == "UL*[I[P(a) CODE(b)]]")
    }

    @Test func containersNestAtMostMaxDepthLevels() {
        let text = String(repeating: ">", count: 1000) + " deep"
        let tree = parse(text)
        let quotes = tree.components(separatedBy: "QUOTE[").count - 1
        #expect(quotes == MarkdownParser.MAX_DEPTH)
    }

    // A table of more than 64 columns is marked too wide, and its rows are not
    // padded: thousands of columns over thousands of rows made cells as the
    // square of the text (review of 11 October 2026, #33); drawn, it is an
    // error (review of 12 October 2026, R33).
    @Test func aTableHasAtMost64Columns() {
        func table(_ columns: Int) -> String {
            return String(repeating: "| a ", count: columns) + "|\n"
                    + String(repeating: "| - ", count: columns) + "|\n|\n|\n"
        }
        let table64 = parse(table(64))
        #expect(table64.hasPrefix("TABLE("), "64 columns: \(table64.prefix(60))")
        let blocks = MarkdownParser.parse(table(65))
        #expect(blocks.count == 1 && blocks[0].tooWide && blocks[0].rows?.count == 1,
                "65 columns: \(parse(table(65)).prefix(60))")
    }

    // Eight times as long a text is read in about eight times the time, not
    // sixty-four; the two times are measured in the same run, the shortest of
    // three each, so that a busy computer slows both (a limit of 3 s failed
    // under a fuzzer's load, 9 October 2026, and a limit of 10 for four times
    // the text, too near both, failed in CI at 10.1 on 12 October).
    @Test func longInputsAreReadQuickly() {
        let small = parseTime(2500)
        let large = parseTime(20000)
        #expect(large <= 24 * small, "20000 repeats took \(large / 1_000_000) ms, and 2500 \(small / 1_000_000) ms")
    }

    // The shortest of three times the text of the repeats is read, in nanoseconds.
    private func parseTime(_ count: Int) -> UInt64 {
        let text = String(repeating: "> - a | b\n>   ---|---\n>   ```\n\n    x\n- [ ]\n", count: count)
        var shortest = UInt64.max
        for _ in 0..<3 {
            let time0 = DispatchTime.now().uptimeNanoseconds
            let blocks = MarkdownParser.parse(text)
            shortest = min(shortest, DispatchTime.now().uptimeNanoseconds - time0)
            #expect(!blocks.isEmpty)
        }
        return shortest
    }
}
