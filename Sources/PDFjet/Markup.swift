/**
 * Markup.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Makes paragraphs of text with the inline markup of Markdown: **bold**,
/// *italic*, ***bold italic***, `code` and [links](https://pdfjet.com), in the
/// fonts given for each. A backslash before a punctuation character, as in \*,
/// makes it plain text, and so is a mark that has no match, such as the * of
/// 2 * 3. Emphasis can be inside a link, and neither inside code. A word keeps
/// its punctuation next to it in another style, as in **bold**, with no space
/// between them: the paragraph joins its text lines with Paragraph.addJoined.
///
/// Headings, lists, block quotes and the rest of Markdown's blocks are not
/// read: a line break is a space, and paragraphs splits a text into paragraphs
/// at its empty lines. The markup is read in time linear in the length of the
/// text, so it can come from anyone. Please see Example_53.
///
public class Markup {
    private static let BOLD = 1
    private static let ITALIC = 2
    private static let CODE = 4

    private let regular: Font
    private let bold: Font
    private let italic: Font
    private let boldItalic: Font
    private let code: Font
    private var linkColor: Int32 = Color.blue
    private var linkUnderline = true

    ///
    /// Creates the markup of paragraphs in the fonts, each at its size.
    ///
    /// - Parameter regular: the font of the text.
    /// - Parameter bold: the font of **bold** text.
    /// - Parameter italic: the font of *italic* text.
    /// - Parameter boldItalic: the font of ***bold italic*** text.
    /// - Parameter code: the font of `code`, usually a monospaced font.
    ///
    public init(_ regular: Font, _ bold: Font, _ italic: Font, _ boldItalic: Font, _ code: Font) {
        self.regular = regular
        self.bold = bold
        self.italic = italic
        self.boldItalic = boldItalic
        self.code = code
    }

    ///
    /// Sets the color of the text of links. The default is Color.blue.
    ///
    /// - Parameter color: the color as a 0xRRGGBB value.
    /// - Returns: this Markup.
    ///
    @discardableResult
    public func setLinkColor(_ color: Int32) -> Markup {
        self.linkColor = color
        return self
    }

    ///
    /// Sets whether the text of links is underlined, as it is by default.
    ///
    /// - Parameter underline: true to underline the links.
    /// - Returns: this Markup.
    ///
    @discardableResult
    public func setLinkUnderline(_ underline: Bool) -> Markup {
        self.linkUnderline = underline
        return self
    }

    ///
    /// Returns the paragraphs of the text, which empty lines separate.
    ///
    /// - Parameter text: the text with its markup.
    /// - Returns: the paragraphs, none for a text with no words.
    ///
    public func paragraphs(_ text: String) -> [Paragraph] {
        var paragraphs = [Paragraph]()
        var buf = [UInt16]()
        // The lines of the text, split at each \n as in Java's
        // text.split("\n", -1), in UTF-16 code units.
        let units = Array(text.utf16)
        var start = 0
        while start <= units.count {
            var end = start
            while end < units.count && units[end] != 0x0A {
                end += 1
            }
            var line = units[start..<end]
            if line.last == 0x0D {
                line = line.dropLast()
            }
            if Markup.isBlank(line) {
                addParagraph(&paragraphs, &buf)
            } else {
                buf.append(contentsOf: line)
                buf.append(0x0A)
            }
            start = end + 1
        }
        addParagraph(&paragraphs, &buf)
        return paragraphs
    }

    private func addParagraph(_ paragraphs: inout [Paragraph], _ buf: inout [UInt16]) {
        if !buf.isEmpty {
            let paragraph = self.paragraph(String(decoding: buf, as: UTF16.self))
            if !paragraph.lines.isEmpty {
                paragraphs.append(paragraph)
            }
            buf.removeAll(keepingCapacity: true)
        }
    }

    ///
    /// Returns the text as one paragraph: a text line for each run of text in
    /// one style, each joined to the text before it.
    ///
    /// - Parameter text: the text with its markup.
    /// - Returns: the paragraph.
    ///
    public func paragraph(_ text: String) -> Paragraph {
        let parser = Parser(text)
        var nodes = [Node]()
        parser.parse(0, parser.text.count, nil, &nodes)
        let paragraph = Paragraph()
        // The runs of one style and link, in order.
        var buf = ""
        var flags = 0
        var link: String? = nil
        var joined = false
        for node in nodes {
            if !buf.isEmpty && (node.flags != flags || node.link != link) {
                joined = addRun(paragraph, buf, flags, link, joined)
                buf = ""
            }
            flags = node.flags
            link = node.link
            buf += node.text!
        }
        if !buf.isEmpty {
            addRun(paragraph, buf, flags, link, joined)
        }
        return paragraph
    }

    // Adds a run of text in one style. A run of spaces only is not added: it
    // puts a space before the next run, as between two words. Returns whether
    // the next run is joined to this one.
    @discardableResult
    private func addRun(_ paragraph: Paragraph, _ text: String, _ flags: Int, _ link: String?, _ joined: Bool) -> Bool {
        if text.trim().isEmpty {
            return false
        }
        let font = ((flags & Markup.CODE) != 0) ? code
                : ((flags & Markup.BOLD) != 0 && (flags & Markup.ITALIC) != 0) ? boldItalic
                : ((flags & Markup.BOLD) != 0) ? bold
                : ((flags & Markup.ITALIC) != 0) ? italic : regular
        let textLine = TextLine(font, text)
        if let link {
            textLine.setURIAction(link)
            textLine.setTextColor(linkColor)
            textLine.setUnderline(linkUnderline)
        }
        if joined {
            paragraph.addJoined(textLine)
        } else {
            paragraph.add(textLine)
        }
        return true
    }

    // True when the code units are all U+0020 or below, so that String.trim in
    // Java would leave nothing of them.
    fileprivate static func isBlank(_ units: ArraySlice<UInt16>) -> Bool {
        for unit in units where unit > 0x20 {
            return false
        }
        return true
    }

    // Character.isWhitespace in Java, of a UTF-16 code unit: a surrogate is
    // not whitespace.
    fileprivate static func isWhitespace(_ unit: UInt16) -> Bool {
        guard let scalar = Unicode.Scalar(unit) else {
            return false
        }
        return String.isJavaWhitespace(scalar)
    }

    // Text in one style, or a run of * that may open or close emphasis, which
    // is text too as far as it does not.
    private final class Node {
        var text: String?
        var flags: Int
        var link: String?
        // Of a run of *: how many of them are not matched yet, whether they can
        // open and close emphasis, and the emphasis the run opens and closes,
        // counted in levels of the flags.
        var count = 0
        var canOpen = false
        var canClose = false
        var opens: [Int]?
        var closes: [Int]?

        init(_ text: String?, _ flags: Int, _ link: String?) {
            self.text = text
            self.flags = flags
            self.link = link
        }
    }

    // The parser works on the UTF-16 code units of the text, so that an index
    // is found in constant time and is the index of a char in Java.
    private final class Parser {
        let text: [UInt16]
        // Where each ] that closes a [ is, by the index of the [.
        private let closingBracket: [Int]
        // The index of the next ) and of the next space at or after each index.
        private let nextParenthesis: [Int]
        private let nextSpace: [Int]
        // The runs of backticks, by their length: where each starts, in order,
        // and the first one of each length that can still close a code span.
        private var backtickRuns = [Int: [Int]]()
        private var backtickCursor = [Int: Int]()

        init(_ string: String) {
            let text = Array(string.utf16)
            let n = text.count
            var closingBracket = [Int](repeating: -1, count: n)
            var open = [Int]()
            var i = 0
            while i < n {
                let ch = text[i]
                if ch == Parser.backslash && i + 1 < n {
                    i += 1
                } else if ch == Parser.openBracket {
                    open.append(i)
                } else if ch == Parser.closeBracket && !open.isEmpty {
                    closingBracket[open.removeLast()] = i
                }
                i += 1
            }
            var nextParenthesis = [Int](repeating: n, count: n + 1)
            var nextSpace = [Int](repeating: n, count: n + 1)
            i = n - 1
            while i >= 0 {
                let ch = text[i]
                nextParenthesis[i] = (ch == Parser.closeParenthesis) ? i : nextParenthesis[i + 1]
                nextSpace[i] = Markup.isWhitespace(ch) ? i : nextSpace[i + 1]
                i -= 1
            }
            self.text = text
            self.closingBracket = closingBracket
            self.nextParenthesis = nextParenthesis
            self.nextSpace = nextSpace
            i = 0
            while i < n {
                if text[i] == Parser.backslash && i + 1 < n {
                    i += 2
                } else if text[i] == Parser.backtick {
                    let start = i
                    while i < n && text[i] == Parser.backtick {
                        i += 1
                    }
                    let length = i - start
                    if backtickRuns[length] == nil {
                        backtickRuns[length] = [Int]()
                        backtickCursor[length] = 0
                    }
                    backtickRuns[length]!.append(start)
                } else {
                    i += 1
                }
            }
        }

        private static let backslash = UInt16(UInt8(ascii: "\\"))
        private static let backtick = UInt16(UInt8(ascii: "`"))
        private static let star = UInt16(UInt8(ascii: "*"))
        private static let openBracket = UInt16(UInt8(ascii: "["))
        private static let closeBracket = UInt16(UInt8(ascii: "]"))
        private static let openParenthesis = UInt16(UInt8(ascii: "("))
        private static let closeParenthesis = UInt16(UInt8(ascii: ")"))
        private static let space = UInt16(UInt8(ascii: " "))

        // Reads the text from start to end into nodes, all in the link when it
        // is not nil, with the emphasis of the runs of * matched.
        func parse(_ start: Int, _ end: Int, _ link: String?, _ nodes: inout [Node]) {
            let first = nodes.count
            var buf = [UInt16]()
            var i = start
            while i < end {
                let ch = text[i]
                if ch == Parser.backslash && i + 1 < end && Parser.isASCIIPunctuation(text[i + 1]) {
                    buf.append(text[i + 1])
                    i += 2
                } else if ch == Parser.backtick {
                    var length = 1
                    while i + length < end && text[i + length] == Parser.backtick {
                        length += 1
                    }
                    let close = closingBackticks(length, i + length, end)
                    if close == -1 {
                        buf.append(contentsOf: text[i..<(i + length)])
                    } else {
                        Parser.flush(&buf, link, &nodes)
                        nodes.append(Node(Parser.codeText(text[(i + length)..<close]), Markup.CODE, link))
                    }
                    i = (close == -1) ? i + length : close + length
                } else if ch == Parser.star {
                    var length = 1
                    while i + length < end && text[i + length] == Parser.star {
                        length += 1
                    }
                    Parser.flush(&buf, link, &nodes)
                    let run = Node(nil, 0, link)
                    run.count = length
                    run.canOpen = i + length < end && !Markup.isWhitespace(text[i + length])
                    run.canClose = i > start && !Markup.isWhitespace(text[i - 1])
                    nodes.append(run)
                    i += length
                } else if ch == Parser.openBracket && link == nil && isLink(i, end) {
                    let close = closingBracket[i]
                    let urlEnd = nextParenthesis[close + 2]
                    Parser.flush(&buf, link, &nodes)
                    parse(i + 1, close, Parser.string(text[(close + 2)..<urlEnd]), &nodes)
                    i = urlEnd + 1
                } else {
                    buf.append(Markup.isWhitespace(ch) ? Parser.space : ch)
                    i += 1
                }
            }
            Parser.flush(&buf, link, &nodes)
            Parser.matchEmphasis(&nodes, first)
        }

        // A [ starts a link when its ] is before the end, a ( follows the ]
        // at once, and a ) ends the URL before any space: [text](url).
        private func isLink(_ i: Int, _ end: Int) -> Bool {
            let close = closingBracket[i]
            if close == -1 || close + 1 >= end || text[close + 1] != Parser.openParenthesis {
                return false
            }
            let urlEnd = nextParenthesis[close + 2]
            return urlEnd < end && urlEnd > close + 2 && nextSpace[close + 2] > urlEnd
        }

        // Returns where the next run of backticks of the length starts, after
        // from and before end, or -1.
        private func closingBackticks(_ length: Int, _ from: Int, _ end: Int) -> Int {
            guard let runs = backtickRuns[length] else {
                return -1
            }
            var cursor = backtickCursor[length]!
            while cursor < runs.count && runs[cursor] < from {
                cursor += 1
            }
            backtickCursor[length] = cursor
            if cursor < runs.count && runs[cursor] + length <= end {
                return runs[cursor]
            }
            return -1
        }

        // The text of a code span: a line break is a space, and one space at
        // each end is taken away when both ends have one, as in `` `a` ``.
        private static func codeText(_ units: ArraySlice<UInt16>) -> String {
            var code = units.map { ($0 == 0x0A || $0 == 0x0D) ? space : $0 }[...]
            if code.count >= 2 && code.first == space && code.last == space
                    && !Markup.isBlank(code) {
                code = code.dropFirst().dropLast()
            }
            return string(code)
        }

        private static func string(_ units: ArraySlice<UInt16>) -> String {
            return String(decoding: units, as: UTF16.self)
        }

        private static func flush(_ buf: inout [UInt16], _ link: String?, _ nodes: inout [Node]) {
            if !buf.isEmpty {
                nodes.append(Node(String(decoding: buf, as: UTF16.self), 0, link))
                buf.removeAll(keepingCapacity: true)
            }
        }

        // Matches the runs of * from the index on, each closer with the nearest
        // opener before it: two of each make bold and one italic, and the runs
        // between them can no longer match. Then gives every node the emphasis
        // it is in, and turns what is left of each run into text.
        private static func matchEmphasis(_ nodes: inout [Node], _ first: Int) {
            var openers = [Node]()
            for i in first..<nodes.count {
                let node = nodes[i]
                if node.text != nil {
                    continue
                }
                if node.canClose {
                    while node.count > 0, let opener = openers.last {
                        let use = (node.count >= 2 && opener.count >= 2) ? 2 : 1
                        let flag = (use == 2) ? Markup.BOLD : Markup.ITALIC
                        opener.opens = add(opener.opens, flag)
                        node.closes = add(node.closes, flag)
                        opener.count -= use
                        node.count -= use
                        if opener.count == 0 {
                            openers.removeLast()
                        }
                    }
                }
                if node.count > 0 && node.canOpen {
                    openers.append(node)
                }
            }
            // The emphasis in effect, in levels, since bold can be in bold.
            var boldLevel = 0
            var italicLevel = 0
            for i in first..<nodes.count {
                let node = nodes[i]
                if node.text == nil {
                    boldLevel -= level(node.closes, Markup.BOLD)
                    italicLevel -= level(node.closes, Markup.ITALIC)
                    node.text = String(repeating: "*", count: node.count)
                    node.flags = emphasis(boldLevel, italicLevel)
                    boldLevel += level(node.opens, Markup.BOLD)
                    italicLevel += level(node.opens, Markup.ITALIC)
                } else {
                    node.flags |= emphasis(boldLevel, italicLevel)
                }
            }
        }

        private static func emphasis(_ boldLevel: Int, _ italicLevel: Int) -> Int {
            return ((boldLevel > 0) ? Markup.BOLD : 0) | ((italicLevel > 0) ? Markup.ITALIC : 0)
        }

        // The levels of bold and of italic, [bold, italic], with one more of the flag.
        private static func add(_ levels: [Int]?, _ flag: Int) -> [Int] {
            var levels = levels ?? [0, 0]
            levels[(flag == Markup.BOLD) ? 0 : 1] += 1
            return levels
        }

        private static func level(_ levels: [Int]?, _ flag: Int) -> Int {
            return (levels == nil) ? 0 : levels![(flag == Markup.BOLD) ? 0 : 1]
        }

        private static func isASCIIPunctuation(_ ch: UInt16) -> Bool {
            return (ch >= 0x21 && ch <= 0x2F) || (ch >= 0x3A && ch <= 0x40)
                    || (ch >= 0x5B && ch <= 0x60) || (ch >= 0x7B && ch <= 0x7E)
        }
    }
}   // End of Markup.swift
