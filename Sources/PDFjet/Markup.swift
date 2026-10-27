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
/// fonts given for each, and as GitHub's Markdown has them, __bold__ and
/// _italic_, ~~strikethrough~~ of two ~, one being text here, as in ~5, and
/// links that are their address,
/// <https://pdfjet.com> or https://pdfjet.com alone. An _ emphasizes only at
/// the edge of a word, so that snake_case is text; an address alone ends
/// before the punctuation at its end, as the period of a sentence, and before
/// a ) that closes no ( of its own. A backslash before a punctuation character,
/// as in \*, makes it plain text, and so is a mark that has no match, such as
/// the * of 2 * 3. Emphasis can be inside a link, and neither inside code. A word keeps
/// its punctuation next to it in another style, as in **bold**, with no space
/// between them: the paragraph joins its text lines with Paragraph.addJoined.
///
/// A line that ends in two spaces or a backslash, and a <br>, are hard line
/// breaks, which TextFrame draws as the end of a row; any other line break is
/// a space. The entities &amp;, &lt;, &gt;, &quot;, &apos;, &nbsp;, &copy;,
/// &reg;, &trade;, &mdash;, &ndash;, &hellip;, the quotes and a few more, and
/// the numbers &#8212; and &#x2014;, are the characters they name.
///
/// Headings, lists, block quotes and the rest of Markdown's blocks are not
/// read: paragraphs splits a text into paragraphs at its empty lines. The
/// markup is read in time linear in the length of the text, so it can come
/// from anyone. Please see Example_18.
///
public class Markup {
    private static let BOLD = 1
    private static let ITALIC = 2
    private static let CODE = 4
    private static let STRIKE = 8
    private static let BREAK = 16       // A hard line break

    // The named entities that are read, as GitHub's Markdown reads them: the
    // markup's own characters, the spaces and the punctuation of typeset text.
    private static let ENTITIES: [String: UInt32] = [
        "amp": 0x26, "lt": 0x3C, "gt": 0x3E, "quot": 0x22, "apos": 0x27,
        "nbsp": 0xA0, "copy": 0xA9, "reg": 0xAE, "trade": 0x2122,
        "mdash": 0x2014, "ndash": 0x2013, "hellip": 0x2026, "lsquo": 0x2018,
        "rsquo": 0x2019, "ldquo": 0x201C, "rdquo": 0x201D,
        "laquo": 0xAB, "raquo": 0xBB, "bull": 0x2022, "middot": 0xB7,
        "deg": 0xB0, "times": 0xD7, "divide": 0xF7,
        "plusmn": 0xB1, "sect": 0xA7, "para": 0xB6, "euro": 0x20AC,
        "pound": 0xA3, "yen": 0xA5, "cent": 0xA2, "shy": 0xAD]

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
    /// Color.transparent leaves it unchanged.
    ///
    /// - Parameter color: the color as a 0xRRGGBB value.
    /// - Returns: this Markup.
    ///
    @discardableResult
    public func setLinkColor(_ color: Int32) -> Markup {
        if color == Color.transparent {
            return self
        }
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
            if (node.flags & Markup.BREAK) != 0 {
                // A hard line break: the row ends after the text before it,
                // and the text after it is not joined to it
                if !buf.isEmpty {
                    addRun(paragraph, buf, flags, link, joined)
                    buf = ""
                }
                if let last = paragraph.lines.last {
                    last.lineBreak = true
                }
                joined = false
                continue
            }
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
        if (flags & Markup.STRIKE) != 0 {
            textLine.setStrikeout(true)
        }
        if let link {
            textLine.setURIAction(link)
            textLine.setTextColor(linkColor)
            // The underline in the color of the link: it was black under the
            // blue (review of 15 October 2026)
            textLine.setDecorationColor(linkColor)
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

    // Text in one style, or a run of *, of _ or of ~~ that may open or close
    // emphasis or a strikethrough, which is text too as far as it does not.
    // The text is nil while the run's emphasis is not matched yet.
    private final class Node {
        var text: String?
        var flags: Int
        var link: String?
        // Of a run: its character, *, _ or ~, where it is among the nodes, how
        // many of its characters are not matched yet, whether they can open
        // and close emphasis, and the emphasis the run opens and closes,
        // counted in levels of bold, italic and strikethrough.
        var char: UInt16 = 0
        var index = 0
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
                if text[i] == Parser.backslash && i + 1 < n && text[i + 1] != Parser.backtick {
                    i += 2
                } else if text[i] == Parser.backtick {
                    // A backtick after a backslash is text where it would open
                    // code, as parse reads it, but closes code, in which a
                    // backslash is text, as in GitHub's Markdown: `foo\` was
                    // not closed
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
        private static let underscore = UInt16(UInt8(ascii: "_"))
        private static let tilde = UInt16(UInt8(ascii: "~"))
        private static let lessThan = UInt16(UInt8(ascii: "<"))
        private static let greaterThan = UInt16(UInt8(ascii: ">"))
        private static let letterH = UInt16(UInt8(ascii: "h"))
        private static let capitalH = UInt16(UInt8(ascii: "H"))
        private static let openBracket = UInt16(UInt8(ascii: "["))
        private static let closeBracket = UInt16(UInt8(ascii: "]"))
        private static let openParenthesis = UInt16(UInt8(ascii: "("))
        private static let closeParenthesis = UInt16(UInt8(ascii: ")"))
        private static let space = UInt16(UInt8(ascii: " "))
        private static let newline = UInt16(UInt8(ascii: "\n"))
        private static let slash = UInt16(UInt8(ascii: "/"))
        private static let ampersand = UInt16(UInt8(ascii: "&"))
        private static let semicolon = UInt16(UInt8(ascii: ";"))
        private static let numberSign = UInt16(UInt8(ascii: "#"))
        private static let letterB = UInt16(UInt8(ascii: "b"))
        private static let capitalB = UInt16(UInt8(ascii: "B"))
        private static let letterR = UInt16(UInt8(ascii: "r"))
        private static let capitalR = UInt16(UInt8(ascii: "R"))
        private static let letterX = UInt16(UInt8(ascii: "x"))
        private static let capitalX = UInt16(UInt8(ascii: "X"))

        // Reads the text from start to end into nodes, all in the link when it
        // is not nil, with the emphasis of the runs of * matched.
        func parse(_ start: Int, _ end: Int, _ link: String?, _ nodes: inout [Node]) {
            let first = nodes.count
            var buf = [UInt16]()
            var i = start
            while i < end {
                let ch = text[i]
                let breakEnd = self.breakEnd(i, end)
                if breakEnd > i {
                    Parser.flush(&buf, link, &nodes)
                    nodes.append(Node("", Markup.BREAK, link))
                    i = breakEnd
                } else if let entity = entity(i, end) {
                    buf.append(contentsOf: Unicode.Scalar(entity.code)!.utf16)
                    i = entity.end
                } else if ch == Parser.backslash && i + 1 < end && Parser.isASCIIPunctuation(text[i + 1]) {
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
                } else if ch == Parser.star || ch == Parser.underscore || ch == Parser.tilde {
                    var length = 1
                    while i + length < end && text[i + length] == ch {
                        length += 1
                    }
                    if ch == Parser.tilde && length != 2 {
                        // A strikethrough is two ~, and one or three are text, as in ~5
                        buf.append(contentsOf: text[i..<(i + length)])
                        i += length
                        continue
                    }
                    Parser.flush(&buf, link, &nodes)
                    let run = Node(nil, 0, link)
                    run.char = ch
                    run.index = nodes.count
                    run.count = length
                    // Before the run, or after it, a space, or the start or the end
                    let left = i + length < end && !Markup.isWhitespace(text[i + length])
                    let right = i > start && !Markup.isWhitespace(text[i - 1])
                    run.canOpen = left
                    run.canClose = right
                    if ch == Parser.underscore {
                        // Inside a word, as in snake_case, an _ is text
                        run.canOpen = left && (!right || Parser.isASCIIPunctuation(text[i - 1]))
                        run.canClose = right && (!left || Parser.isASCIIPunctuation(text[i + length]))
                    }
                    nodes.append(run)
                    i += length
                } else if case let urlEnd = autolinkEnd(i, start, end, link), urlEnd > i {
                    Parser.flush(&buf, link, &nodes)
                    let url = (ch == Parser.lessThan)
                            ? Parser.string(text[(i + 1)..<(urlEnd - 1)])
                            : Parser.string(text[i..<urlEnd])
                    nodes.append(Node(url, 0, url))
                    i = urlEnd
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

        // Returns where a hard line break at i ends, or i when there is none:
        // a line break after two spaces, a backslash and a line break, or a
        // <br>, <br/> or <br /> in capitals or not.
        private func breakEnd(_ i: Int, _ end: Int) -> Int {
            let ch = text[i]
            if ch == Parser.newline && i >= 2 && text[i - 1] == Parser.space && text[i - 2] == Parser.space {
                return i + 1
            }
            if ch == Parser.backslash && i + 1 < end && text[i + 1] == Parser.newline {
                return i + 2
            }
            if ch == Parser.lessThan && i + 3 < end && (text[i + 1] == Parser.letterB || text[i + 1] == Parser.capitalB)
                    && (text[i + 2] == Parser.letterR || text[i + 2] == Parser.capitalR) {
                var j = i + 3
                while j < end && text[j] == Parser.space {
                    j += 1
                }
                if j < end && text[j] == Parser.slash {
                    j += 1
                }
                if j < end && text[j] == Parser.greaterThan {
                    return j + 1
                }
            }
            return i
        }

        // Returns the character of the entity at i and where the entity ends,
        // or nil when there is none: a name of ENTITIES, or a number of up to
        // seven digits, or of six in hexadecimal after an x, that is a
        // character, between & and ;. The indexes are of UTF-16 code units,
        // as in Java, so that the ; is looked for in the same ten.
        private func entity(_ i: Int, _ end: Int) -> (code: UInt32, end: Int)? {
            if text[i] != Parser.ampersand {
                return nil
            }
            var semicolon = -1
            var j = i + 1
            while j < end && j <= i + 10 {
                if text[j] == Parser.semicolon {
                    semicolon = j
                    break
                }
                j += 1
            }
            if semicolon == -1 {
                return nil
            }
            let name = text[(i + 1)..<semicolon]
            if let code = Markup.ENTITIES[Parser.string(name)] {
                return (code, semicolon + 1)
            }
            if name.count < 2 || name.first != Parser.numberSign {
                return nil
            }
            let second = name[name.startIndex + 1]
            let hex = second == Parser.letterX || second == Parser.capitalX
            let digits = name.dropFirst(hex ? 2 : 1)
            if digits.isEmpty || digits.count > (hex ? 6 : 7) {
                return nil
            }
            var value: UInt32 = 0
            for unit in digits {
                let digit = Parser.asciiDigit(unit, hex)
                if digit < 0 {
                    return nil
                }
                value = value * (hex ? 16 : 10) + UInt32(digit)
            }
            if value == 0 || value > 0x10FFFF || (value >= 0xD800 && value <= 0xDFFF) {
                return nil
            }
            return (value, semicolon + 1)
        }

        // The value of an ASCII digit, of 0 to 9, or of a to f and A to F in
        // hexadecimal, or -1.
        private static func asciiDigit(_ ch: UInt16, _ hex: Bool) -> Int {
            if ch >= 0x30 && ch <= 0x39 {
                return Int(ch) - 0x30
            }
            if hex && ch >= 0x61 && ch <= 0x66 {
                return Int(ch) - 0x61 + 10
            }
            if hex && ch >= 0x41 && ch <= 0x46 {
                return Int(ch) - 0x41 + 10
            }
            return -1
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

        // Returns where a link that is its address ends, after its > when it
        // is in < and >, or i when there is none at i, or the text is in a link
        // already: an address of http:// or https:// and a host, in < and >
        // with no space in it, or alone, after a space, the start, a (, or a *,
        // _ or ~ of emphasis. An address alone ends at a space or a <, and then
        // before the punctuation at its end, ? ! . , : * _ and ~, and before
        // each ) at its end that closes no ( in it, as GitHub's Markdown reads it.
        private func autolinkEnd(_ i: Int, _ start: Int, _ end: Int, _ link: String?) -> Int {
            if link != nil || (text[i] != Parser.lessThan && text[i] != Parser.letterH
                    && text[i] != Parser.capitalH) {
                return i
            }
            var from = i
            if text[i] == Parser.lessThan {
                from = i + 1
            } else if i > start && !Markup.isWhitespace(text[i - 1]) && !Parser.isAutolinkStart(text[i - 1]) {
                return i
            }
            let host = schemeEnd(from, end)
            if host == -1 || host >= end || !Parser.isHostChar(text[host]) {
                return i
            }
            var j = host
            while j < end && !Markup.isWhitespace(text[j])
                    && text[j] != Parser.lessThan && text[j] != Parser.greaterThan {
                j += 1
            }
            if text[i] == Parser.lessThan {
                return (j < end && text[j] == Parser.greaterThan) ? j + 1 : i
            }
            var opening = 0
            var closing = 0
            for k in from..<j {
                if text[k] == Parser.openParenthesis {
                    opening += 1
                } else if text[k] == Parser.closeParenthesis {
                    closing += 1
                }
            }
            while j > host {
                let last = text[j - 1]
                if Parser.isAutolinkTrailing(last) {
                    j -= 1
                } else if last == Parser.closeParenthesis && opening < closing {
                    j -= 1
                    closing -= 1
                } else {
                    break
                }
            }
            return (j == host) ? i : j
        }

        // Returns where the host starts after http:// or https:// at the
        // index, in capitals or not, or -1. Only the ASCII letters are folded,
        // so that the four ports agree.
        private func schemeEnd(_ i: Int, _ end: Int) -> Int {
            for scheme in Parser.schemes {
                let n = scheme.count
                if i + n > end {
                    continue
                }
                var k = 0
                while k < n {
                    var ch = text[i + k]
                    if ch >= 0x41 && ch <= 0x5A {
                        ch += 0x20
                    }
                    if ch != scheme[k] {
                        break
                    }
                    k += 1
                }
                if k == n {
                    return i + n
                }
            }
            return -1
        }

        private static let schemes = [Array("https://".utf16), Array("http://".utf16)]

        // True for a letter, a digit or a character that is not ASCII, as the
        // first character of a host is.
        private static func isHostChar(_ ch: UInt16) -> Bool {
            return (ch >= 0x61 && ch <= 0x7A) || (ch >= 0x41 && ch <= 0x5A)
                    || (ch >= 0x30 && ch <= 0x39) || ch > 0x7F
        }

        // True for (, *, _ and ~, after which an address alone can start.
        private static func isAutolinkStart(_ ch: UInt16) -> Bool {
            return ch == openParenthesis || ch == star || ch == underscore || ch == tilde
        }

        // True for ? ! . , : * _ and ~, which an address alone ends before.
        private static func isAutolinkTrailing(_ ch: UInt16) -> Bool {
            switch ch {
            case 0x3F, 0x21, 0x2E, 0x2C, 0x3A, star, underscore, tilde:
                return true
            default:
                return false
            }
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

        // Matches the runs from the index on, each closer with the nearest
        // opener of its character before it: two * or _ of each make bold and
        // one italic, and ~~ a strikethrough. The runs of the other characters
        // between a closer and its opener can no longer match, so that emphasis
        // and strikethrough nest and do not cross. Then gives every node the
        // emphasis it is in, and turns what is left of each run into text.
        private static func matchEmphasis(_ nodes: inout [Node], _ first: Int) {
            // The runs that can still open, of *, of _ and of ~
            var stars = [Node]()
            var underscores = [Node]()
            var tildes = [Node]()
            for i in first..<nodes.count {
                let node = nodes[i]
                if node.text != nil {
                    continue
                }
                if node.char == tilde {
                    if node.canClose, let opener = tildes.popLast() {
                        dropAfter(&stars, opener.index)
                        dropAfter(&underscores, opener.index)
                        opener.opens = add(opener.opens, Markup.STRIKE)
                        node.closes = add(node.closes, Markup.STRIKE)
                        opener.count = 0
                        node.count = 0
                    }
                    if node.count > 0 && node.canOpen {
                        tildes.append(node)
                    }
                    continue
                }
                if node.char == star {
                    match(node, &stars, &underscores, &tildes)
                } else {
                    match(node, &underscores, &stars, &tildes)
                }
            }
            // The emphasis in effect, in levels, since bold can be in bold.
            var levels = [0, 0, 0]
            for i in first..<nodes.count {
                let node = nodes[i]
                if node.text == nil {
                    if let closes = node.closes {
                        for k in 0..<3 {
                            levels[k] -= closes[k]
                        }
                    }
                    node.text = String(repeating: Character(Unicode.Scalar(node.char)!), count: node.count)
                    node.flags = emphasis(levels)
                    if let opens = node.opens {
                        for k in 0..<3 {
                            levels[k] += opens[k]
                        }
                    }
                } else {
                    node.flags |= emphasis(levels)
                }
            }
        }

        // Matches a run of * or _ that can close with the openers of its own
        // character, and drops the openers of the other character and of ~
        // that are after the opener it matches. Then the run opens what is
        // left of it.
        private static func match(_ node: Node, _ own: inout [Node], _ other: inout [Node],
                _ tildes: inout [Node]) {
            if node.canClose {
                while node.count > 0, let opener = own.last {
                    dropAfter(&other, opener.index)
                    dropAfter(&tildes, opener.index)
                    let use = (node.count >= 2 && opener.count >= 2) ? 2 : 1
                    let flag = (use == 2) ? Markup.BOLD : Markup.ITALIC
                    opener.opens = add(opener.opens, flag)
                    node.closes = add(node.closes, flag)
                    opener.count -= use
                    node.count -= use
                    if opener.count == 0 {
                        own.removeLast()
                    }
                }
            }
            if node.count > 0 && node.canOpen {
                own.append(node)
            }
        }

        // Drops the openers after the index from the stack.
        private static func dropAfter(_ openers: inout [Node], _ index: Int) {
            while let after = openers.last, after.index > index {
                openers.removeLast()
            }
        }

        // The flags of the levels of bold, italic and strikethrough in effect.
        private static func emphasis(_ levels: [Int]) -> Int {
            return ((levels[0] > 0) ? Markup.BOLD : 0) | ((levels[1] > 0) ? Markup.ITALIC : 0)
                    | ((levels[2] > 0) ? Markup.STRIKE : 0)
        }

        // The levels of bold, italic and strikethrough, [bold, italic, strike],
        // with one more of the flag.
        private static func add(_ levels: [Int]?, _ flag: Int) -> [Int] {
            var levels = levels ?? [0, 0, 0]
            levels[(flag == Markup.BOLD) ? 0 : (flag == Markup.ITALIC) ? 1 : 2] += 1
            return levels
        }

        private static func isASCIIPunctuation(_ ch: UInt16) -> Bool {
            return (ch >= 0x21 && ch <= 0x2F) || (ch >= 0x3A && ch <= 0x40)
                    || (ch >= 0x5B && ch <= 0x60) || (ch >= 0x7B && ch <= 0x7E)
        }
    }
}   // End of Markup.swift
