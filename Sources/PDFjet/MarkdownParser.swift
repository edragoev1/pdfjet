/**
 * MarkdownParser.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Reads the blocks of a Markdown text into a tree, for Markdown to draw:
/// headings, paragraphs, code, block quotes, lists, thematic breaks, tables and
/// images. The text of the headings, the paragraphs and the table cells keeps
/// its inline markup, which Markup reads. Containers nest to MAX_DEPTH levels;
/// deeper, their lines are text, so that no input makes the reading slower
/// than linear in its length times that depth.
///
/// The parser works on the UTF-16 code units of each line, as Markup does, so
/// that an index is found in constant time and is the index of a char in Java.
///
final class MarkdownParser {
    static let MAX_DEPTH = 32

    enum Kind { case HEADING, PARAGRAPH, CODE, QUOTE, LIST, ITEM, RULE, TABLE, IMAGE }

    final class Block {
        let kind: Kind
        var level = 0                   // Of a heading, 1 to 6
        var text: String?               // Of a heading or a paragraph, the code, or the alt text of an image
        var source: String?             // Of an image
        var ordered = false             // Of a list
        var start = 1                   // Of an ordered list
        var loose = false               // Of a list whose items are apart, with an empty line between them
        var children = [Block]()        // Of a quote, a list or an item
        var rows: [[String]]?           // Of a table, the header row first
        var alignments: [Alignment]?    // Of a table, of each column, or nil

        init(_ kind: Kind) {
            self.kind = kind
        }
    }

    // A line of the text, in UTF-16 code units.
    typealias Line = [UInt16]

    private static let tab: UInt16 = 0x09
    private static let lf: UInt16 = 0x0A
    private static let cr: UInt16 = 0x0D
    private static let space: UInt16 = 0x20
    private static let hash: UInt16 = 0x23          // #
    private static let openParen: UInt16 = 0x28     // (
    private static let closeParen: UInt16 = 0x29    // )
    private static let star: UInt16 = 0x2A          // *
    private static let plus: UInt16 = 0x2B          // +
    private static let dash: UInt16 = 0x2D          // -
    private static let dot: UInt16 = 0x2E           // .
    private static let zero: UInt16 = 0x30          // 0
    private static let nine: UInt16 = 0x39          // 9
    private static let colon: UInt16 = 0x3A         // :
    private static let equals: UInt16 = 0x3D        // =
    private static let greater: UInt16 = 0x3E       // >
    private static let bang: UInt16 = 0x21          // !
    private static let openBracket: UInt16 = 0x5B   // [
    private static let backslash: UInt16 = 0x5C     // \
    private static let closeBracket: UInt16 = 0x5D  // ]
    private static let underscore: UInt16 = 0x5F    // _
    private static let backtick: UInt16 = 0x60      // `
    private static let pipe: UInt16 = 0x7C          // |
    private static let tilde: UInt16 = 0x7E         // ~

    private init() {
    }

    // The blocks of the text.
    static func parse(_ text: String) -> [Block] {
        let units = Array(text.utf16)
        var lines = [Line]()
        var start = 0
        var i = 0
        while i < units.count {
            let ch = units[i]
            if ch == lf || ch == cr {
                lines.append(expandTabs(units[start..<i]))
                if ch == cr && i + 1 < units.count && units[i + 1] == lf {
                    i += 1
                }
                start = i + 1
            }
            i += 1
        }
        lines.append(expandTabs(units[start...]))
        return parseBlocks(lines, 0)
    }

    // A tab is spaces to the next column that is a multiple of 4.
    private static func expandTabs(_ line: ArraySlice<UInt16>) -> Line {
        if !line.contains(tab) {
            return Line(line)
        }
        var buf = Line()
        for ch in line {
            if ch == tab {
                repeat {
                    buf.append(space)
                } while buf.count % 4 != 0
            } else {
                buf.append(ch)
            }
        }
        return buf
    }

    private static func parseBlocks(_ lines: [Line], _ depth: Int) -> [Block] {
        var blocks = [Block]()
        var i = 0
        while i < lines.count {
            let line = lines[i]
            if isBlank(line) {
                i += 1
                continue
            }
            if let fence = fence(line) {
                i = parseFencedCode(lines, i, fence, &blocks)
            } else if atxLevel(line) > 0 {
                let heading = Block(.HEADING)
                heading.level = atxLevel(line)
                heading.text = string(atxText(line))
                blocks.append(heading)
                i += 1
            } else if isThematicBreak(line) {
                blocks.append(Block(.RULE))
                i += 1
            } else if depth < MAX_DEPTH && isQuote(line) {
                i = parseQuote(lines, i, depth, &blocks)
            } else if depth < MAX_DEPTH && listMarker(line) != nil {
                i = parseList(lines, i, depth, &blocks)
            } else if isTableStart(lines, i) {
                i = parseTable(lines, i, &blocks)
            } else if indent(line) >= 4 {
                i = parseIndentedCode(lines, i, &blocks)
            } else {
                i = parseParagraph(lines, i, &blocks)
            }
        }
        return blocks
    }

    // Code between two fences of ``` or ~~~, the closing one as long or longer.
    private static func parseFencedCode(_ lines: [Line], _ i: Int, _ fence: [Int], _ blocks: inout [Block]) -> Int {
        var code = Line()
        var j = i + 1
        while j < lines.count {
            let line = lines[j]
            if isClosingFence(line, fence) {
                j += 1
                break
            }
            if !code.isEmpty || j > i + 1 {
                code.append(lf)
            }
            // The content loses as much of its indent as the opening fence has.
            let strip = min(fence[2], indent(line))
            code.append(contentsOf: line[strip...])
            j += 1
        }
        let block = Block(.CODE)
        block.text = string(code)
        blocks.append(block)
        return j
    }

    // Lines indented 4 spaces or more, with the empty lines between them.
    private static func parseIndentedCode(_ lines: [Line], _ i: Int, _ blocks: inout [Block]) -> Int {
        var code = [Line]()
        var j = i
        while j < lines.count && (isBlank(lines[j]) || indent(lines[j]) >= 4) {
            let line = lines[j]
            code.append(isBlank(line) ? Line() : Line(line[4...]))
            j += 1
        }
        while !code.isEmpty && code[code.count - 1].isEmpty {
            code.removeLast()
        }
        let block = Block(.CODE)
        block.text = string(join(code, lf))
        blocks.append(block)
        return j
    }

    // The lines of a quote, without their >, and the lines that go on from
    // the text of the quote without one, as Markdown allows.
    private static func parseQuote(_ lines: [Line], _ i: Int, _ depth: Int, _ blocks: inout [Block]) -> Int {
        var quoted = [Line]()
        var j = i
        while j < lines.count {
            let line = lines[j]
            if isQuote(line) {
                quoted.append(stripQuote(line))
            } else if !isBlank(line) && !quoted.isEmpty && !isBlank(quoted[quoted.count - 1])
                    && !interrupts(line) {
                quoted.append(line)
            } else {
                break
            }
            j += 1
        }
        let quote = Block(.QUOTE)
        quote.children.append(contentsOf: parseBlocks(quoted, depth + 1))
        blocks.append(quote)
        return j
    }

    // The items of a list: each is its first line after the marker and the
    // lines indented to its text, with what goes on from its text without an
    // indent. A list ends at an item of another kind, or at a line that is not
    // indented after an empty one.
    private static func parseList(_ lines: [Line], _ start: Int, _ depth: Int, _ blocks: inout [Block]) -> Int {
        var i = start
        let first = listMarker(lines[i])!
        let list = Block(.LIST)
        list.ordered = first[1] < 0
        list.start = list.ordered ? first[3] : 1
        while i < lines.count {
            guard let marker = listMarker(lines[i]), marker[1] == first[1], !isThematicBreak(lines[i]) else {
                break
            }
            let column = marker[2]
            let line = lines[i]
            var itemLines = [Line]()
            itemLines.append(line.count > column ? Line(line[column...]) : Line())
            var j = i + 1
            while j < lines.count {
                let next = lines[j]
                let last = itemLines[itemLines.count - 1]
                if isBlank(next) {
                    itemLines.append(Line())
                } else if indent(next) >= column {
                    itemLines.append(Line(next[column...]))
                } else if !isBlank(last) && !interrupts(next) && listMarker(next) == nil {
                    itemLines.append(next)
                } else {
                    break
                }
                j += 1
            }
            // The empty lines after the item are between it and the next.
            var blankAfter = false
            while itemLines.count > 1 && itemLines[itemLines.count - 1].isEmpty {
                itemLines.removeLast()
                blankAfter = true
            }
            if itemLines.contains(Line()) || (blankAfter && j < lines.count && sameList(lines[j], first)) {
                list.loose = true
            }
            let item = Block(.ITEM)
            item.children.append(contentsOf: parseBlocks(itemLines, depth + 1))
            list.children.append(item)
            i = j
        }
        blocks.append(list)
        return i
    }

    private static func sameList(_ line: Line, _ first: [Int]) -> Bool {
        guard let marker = listMarker(line) else {
            return false
        }
        return marker[1] == first[1]
    }

    // A table of GitHub's Markdown: a header row, a row of dashes that sets the
    // alignment of each column, and the rows up to an empty line.
    private static func parseTable(_ lines: [Line], _ i: Int, _ blocks: inout [Block]) -> Int {
        let header = cells(lines[i]).map { string($0) }
        let delimiters = cells(lines[i + 1])
        let table = Block(.TABLE)
        var rows = [[String]]()
        var alignments = [Alignment]()
        for delimiter in delimiters {
            let d = trim(delimiter)
            let left = d.first == colon
            let right = d.last == colon
            alignments.append((left && right) ? Alignment.CENTER : right ? Alignment.RIGHT : Alignment.LEFT)
        }
        rows.append(header)
        var j = i + 2
        while j < lines.count && !isBlank(lines[j]) && lines[j].contains(pipe)
                && !interrupts(lines[j]) {
            var row = cells(lines[j]).map { string($0) }
            // Every row has a cell for each column.
            while row.count < header.count {
                row.append("")
            }
            rows.append(Array(row[0..<header.count]))
            j += 1
        }
        table.rows = rows
        table.alignments = alignments
        blocks.append(table)
        return j
    }

    private static func isTableStart(_ lines: [Line], _ i: Int) -> Bool {
        if i + 1 >= lines.count || !lines[i].contains(pipe) || indent(lines[i]) >= 4 {
            return false
        }
        let delimiterRow = lines[i + 1]
        if !delimiterRow.contains(dash) || indent(delimiterRow) >= 4 {
            return false
        }
        let delimiters = cells(delimiterRow)
        for delimiter in delimiters {
            let d = trim(delimiter)
            let from = (d.first == colon) ? 1 : 0
            let to = (d.last == colon && d.count > from) ? d.count - 1 : d.count
            if to <= from {
                return false
            }
            for k in from..<to where d[k] != dash {
                return false
            }
        }
        return cells(lines[i]).count == delimiters.count
    }

    // The cells of a row, split at the | that no backslash escapes, without
    // the | at the start and at the end of the row.
    private static func cells(_ line: Line) -> [Line] {
        var row = trim(line)
        if row.first == pipe {
            row = Line(row[1...])
        }
        if row.last == pipe && !(row.count >= 2 && row[row.count - 2] == backslash) {
            row = Line(row[0..<(row.count - 1)])
        }
        var cells = [Line]()
        var cell = Line()
        var k = 0
        while k < row.count {
            let ch = row[k]
            if ch == backslash && k + 1 < row.count {
                cell.append(ch)
                cell.append(row[k + 1])
                k += 1
            } else if ch == pipe {
                cells.append(trim(cell))
                cell.removeAll(keepingCapacity: true)
            } else {
                cell.append(ch)
            }
            k += 1
        }
        cells.append(trim(cell))
        return cells
    }

    // The lines of a paragraph, up to an empty line or a line that starts
    // another block. A line of = or - under them makes them a heading.
    private static func parseParagraph(_ lines: [Line], _ i: Int, _ blocks: inout [Block]) -> Int {
        var text = [Line]()
        var j = i
        while j < lines.count && !isBlank(lines[j]) {
            let line = lines[j]
            if j > i {
                let level = setextLevel(line)
                if level > 0 {
                    let heading = Block(.HEADING)
                    heading.level = level
                    heading.text = string(join(text, lf))
                    blocks.append(heading)
                    return j + 1
                }
                if interrupts(line) {
                    break
                }
            }
            text.append(trim(line))
            j += 1
        }
        let paragraph = join(text, lf)
        blocks.append(image(paragraph) ?? self.paragraph(paragraph))
        return j
    }

    private static func paragraph(_ text: Line) -> Block {
        let block = Block(.PARAGRAPH)
        block.text = string(text)
        return block
    }

    // An image alone in its paragraph: ![alt text](source), with no space in the source.
    private static func image(_ text: Line) -> Block? {
        if text.count < 2 || text[0] != bang || text[1] != openBracket || text.last != closeParen {
            return nil
        }
        var close = -1
        var k = 0
        while k + 1 < text.count {
            if text[k] == closeBracket && text[k + 1] == openParen {
                close = k
                break
            }
            k += 1
        }
        if close == -1 || indexOf(text, closeBracket, 2) != close {
            return nil
        }
        let source = text[(close + 2)..<(text.count - 1)]
        if source.isEmpty {
            return nil
        }
        for ch in source {
            if isWhitespace(ch) || ch == openParen || ch == closeParen {
                return nil
            }
        }
        let block = Block(.IMAGE)
        block.text = string(text[2..<close])
        block.source = string(source)
        return block
    }

    // A line that ends a paragraph and starts another block, with no empty
    // line before it: a fence, a heading, a thematic break, a quote, or a list
    // item with text, of a numbered list only when it starts at 1.
    private static func interrupts(_ line: Line) -> Bool {
        if fence(line) != nil || atxLevel(line) > 0 || isThematicBreak(line) || isQuote(line) {
            return true
        }
        guard let marker = listMarker(line) else {
            return false
        }
        return !isBlank(line[min(marker[2], line.count)...])
                && (marker[1] >= 0 || marker[3] == 1)
    }

    // True when the code units are all U+0020 or below, so that String.trim in
    // Java would leave nothing of them.
    private static func isBlank<C: Collection>(_ line: C) -> Bool where C.Element == UInt16 {
        for unit in line where unit > space {
            return false
        }
        return true
    }

    // The line without the code units of U+0020 or below at its start and at
    // its end, as String.trim in Java.
    private static func trim(_ line: Line) -> Line {
        var start = 0
        var end = line.count
        while start < end && line[start] <= space {
            start += 1
        }
        while end > start && line[end - 1] <= space {
            end -= 1
        }
        return (start == 0 && end == line.count) ? line : Line(line[start..<end])
    }

    // Character.isWhitespace in Java, of a UTF-16 code unit: a surrogate is
    // not whitespace.
    private static func isWhitespace(_ unit: UInt16) -> Bool {
        guard let scalar = Unicode.Scalar(unit) else {
            return false
        }
        return String.isJavaWhitespace(scalar)
    }

    // The index of the first code unit that is ch, at or after from, or -1.
    private static func indexOf(_ line: Line, _ ch: UInt16, _ from: Int) -> Int {
        var k = max(0, from)
        while k < line.count {
            if line[k] == ch {
                return k
            }
            k += 1
        }
        return -1
    }

    private static func indent(_ line: Line) -> Int {
        var n = 0
        while n < line.count && line[n] == space {
            n += 1
        }
        return n
    }

    // A fence of three or more ` or ~, indented 3 spaces at most, as
    // [the character, its count, the indent]; a ` fence has no ` after it.
    private static func fence(_ line: Line) -> [Int]? {
        let n = indent(line)
        if n > 3 || n >= line.count {
            return nil
        }
        let ch = line[n]
        if ch != backtick && ch != tilde {
            return nil
        }
        var count = 0
        while n + count < line.count && line[n + count] == ch {
            count += 1
        }
        if count < 3 || (ch == backtick && indexOf(line, backtick, n + count) != -1) {
            return nil
        }
        return [Int(ch), count, n]
    }

    private static func isClosingFence(_ line: Line, _ fence: [Int]) -> Bool {
        let n = indent(line)
        if n > 3 {
            return false
        }
        var count = 0
        while n + count < line.count && Int(line[n + count]) == fence[0] {
            count += 1
        }
        return count >= fence[1] && isBlank(line[(n + count)...])
    }

    // The level of a heading of # to ######, or 0.
    private static func atxLevel(_ line: Line) -> Int {
        let n = indent(line)
        if n > 3 {
            return 0
        }
        var level = 0
        while n + level < line.count && line[n + level] == hash {
            level += 1
        }
        if level < 1 || level > 6 {
            return 0
        }
        let after = n + level
        return (after == line.count || line[after] == space) ? level : 0
    }

    // The text of a heading of #, without the #s that close it.
    private static func atxText(_ line: Line) -> Line {
        var text = trim(line)
        var k = 0
        while k < text.count && text[k] == hash {
            k += 1
        }
        text = trim(Line(text[k...]))
        var end = text.count
        while end > 0 && text[end - 1] == hash {
            end -= 1
        }
        if end == 0 || text[end - 1] == space {
            text = trim(Line(text[0..<end]))
        }
        return text
    }

    // A line of three or more *, - or _, all the same, with spaces between them.
    private static func isThematicBreak(_ line: Line) -> Bool {
        if indent(line) > 3 {
            return false
        }
        var mark: UInt16 = 0
        var count = 0
        for ch in line {
            if ch == space {
                continue
            }
            if (ch != star && ch != dash && ch != underscore) || (mark != 0 && ch != mark) {
                return false
            }
            mark = ch
            count += 1
        }
        return count >= 3
    }

    // The level of the heading that a line of = (1) or of - (2) under a paragraph makes, or 0.
    private static func setextLevel(_ line: Line) -> Int {
        if indent(line) > 3 {
            return 0
        }
        let text = trim(line)
        if text.isEmpty {
            return 0
        }
        let ch = text[0]
        if ch != equals && ch != dash {
            return 0
        }
        for unit in text where unit != ch {
            return 0
        }
        return (ch == equals) ? 1 : 2
    }

    private static func isQuote(_ line: Line) -> Bool {
        let n = indent(line)
        return n <= 3 && n < line.count && line[n] == greater
    }

    // The line without its > and the space after it.
    private static func stripQuote(_ line: Line) -> Line {
        var n = indent(line) + 1
        if n < line.count && line[n] == space {
            n += 1
        }
        return Line(line[n...])
    }

    // The marker of a list item: -, + or * and a space, or 1 to 9 digits, . or
    // ) and a space, indented 3 spaces at most. Returns [the indent, the kind:
    // the bullet, or -1 after . and -2 after ), the column of the text, the
    // number], or nil. The text is 1 to 4 spaces after the marker; with more
    // the item starts with indented code, one space after it.
    static func listMarker(_ line: Line) -> [Int]? {
        let n = indent(line)
        if n > 3 || n >= line.count {
            return nil
        }
        let ch = line[n]
        var end: Int
        var kind: Int
        var number = 0
        if ch == dash || ch == plus || ch == star {
            end = n + 1
            kind = Int(ch)
        } else {
            var digits = 0
            while n + digits < line.count && digits < 10
                    && line[n + digits] >= zero && line[n + digits] <= nine {
                digits += 1
            }
            if digits == 0 || digits > 9 || n + digits >= line.count {
                return nil
            }
            let delimiter = line[n + digits]
            if delimiter != dot && delimiter != closeParen {
                return nil
            }
            for k in n..<(n + digits) {
                number = number * 10 + Int(line[k] - zero)
            }
            end = n + digits + 1
            kind = (delimiter == dot) ? -1 : -2
        }
        if end < line.count && line[end] != space {
            return nil
        }
        var spaces = 0
        while end + spaces < line.count && line[end + spaces] == space {
            spaces += 1
        }
        let column = (spaces >= 1 && spaces <= 4 && end + spaces < line.count) ? end + spaces : end + 1
        return [n, kind, column, number]
    }

    private static func join(_ list: [Line], _ separator: UInt16) -> Line {
        var buf = Line()
        for k in 0..<list.count {
            if k > 0 {
                buf.append(separator)
            }
            buf.append(contentsOf: list[k])
        }
        return buf
    }

    private static func string<C: Collection>(_ units: C) -> String where C.Element == UInt16 {
        return String(decoding: units, as: UTF16.self)
    }
}   // End of MarkdownParser.swift
