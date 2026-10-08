/**
 * SVGXMLParser.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

// This is the XMLParser of the Java, the C# and the Go ports, under another
// name: Foundation, and FoundationXML on Linux, already has XMLParser,
// XMLNode, XMLElement and XMLDocument, and a type of ours by one of those
// names would be ambiguous, or would hide theirs, wherever a program imports
// Foundation as well, so the Swift names are SVGXMLParser, SVGXMLNode and
// SVGXMLError.

///
/// Reads the XML of an SVG image into elements: their names, their
/// namespaces, their attributes in the order they are written, and their
/// text. It is the one parser of the four ports, so that they read an SVG
/// alike, and it came from the parser of the electronic invoices of PDFjet
/// Pro. It reads what such a document is made of and no more, and what it
/// leaves out is what makes XML dangerous to read:
///
/// - A document type declaration, a DOCTYPE, is skipped, as the files of the
///   drawing programs have one, and its entities are not read: an entity
///   that names a file or a URL reads what it should not, and the ones that
///   stand for each other grow a short document into gigabytes.
/// - The entities are the five of XML, &lt;, &gt;, &amp;, &quot; and &apos;,
///   and the numeric ones, such as &#160; and &#xA0;. Any other is an error,
///   the ones a DOCTYPE declares among them.
/// - The elements nest MAX_DEPTH levels at most, and the reading does not
///   recurse, so no document overflows the stack.
/// - The document is 20 MB at most.
/// - The namespaces are looked up in the elements that declare them, from
///   the innermost out, and not copied into each element, so that no number
///   of declarations makes the reading slower than the length of the document
///   times its depth.
/// - Nothing is fetched, and no schema is read.
///
/// What is read is XML, and what is not is an error: a character that XML
/// does not have, such as U+0000 or U+FFFF, written or as an entity; an
/// attribute written twice, or not after a space; and a prefix that no
/// element declares. A line break is a line feed, as XML reads a carriage
/// return and the line feed after it, and the tabs and line breaks in the
/// value of an attribute are spaces, as XML normalizes them.
///
/// The document is UTF-8, or UTF-16 when it starts with a byte order mark;
/// another encoding in the declaration is an error. The bytes that are not
/// valid UTF-8 are the character U+FFFD, one for each part of a sequence that
/// is as much of a character as it is valid for, which is what the Unicode
/// Standard recommends and the decoders of the other ports do. A code unit of
/// UTF-16 that is a surrogate without the other one of its pair is U+FFFD as
/// well.
///
final class SVGXMLParser {
    /// How deep the elements of a document may nest.
    static let MAX_DEPTH = 256
    // How many bytes a document may be, 20 MB.
    static let MAX_SIZE = 20 << 20
    // The error of a document of more than 20 MB.
    static let TOO_LARGE = SVGXMLError(message: "The document is more than 20 MB.")

    private init() {
    }

    ///
    /// Reads the document and returns its root element.
    ///
    /// - Parameter bytes: the document.
    /// - Returns: the root element.
    /// - Throws: SVGXMLError if the document is not the XML this reads, or
    ///   is more than 20 MB.
    ///
    static func parse(_ bytes: [UInt8]) throws -> SVGXMLNode {
        if bytes.count > MAX_SIZE {
            throw TOO_LARGE
        }
        var reader = Reader(decode(bytes))
        return try reader.document()
    }

    ///
    /// Reads the document and returns its root element.
    ///
    /// - Parameter data: the document.
    /// - Returns: the root element.
    /// - Throws: SVGXMLError if the document is not the XML this reads, or
    ///   is more than 20 MB.
    ///
    static func parse(_ data: Data) throws -> SVGXMLNode {
        return try parse([UInt8](data))
    }

    ///
    /// Reads the document of the stream and returns its root element. The
    /// stream is opened when it is not open, is read to its end, or to the
    /// 20 MB a document may be, and is not closed.
    ///
    /// - Parameter stream: the stream.
    /// - Returns: the root element.
    /// - Throws: SVGXMLError if the stream cannot be read, or if the document is
    ///   not the XML this reads, or is more than 20 MB.
    ///
    static func parse(_ stream: InputStream) throws -> SVGXMLNode {
        var bytes = [UInt8]()
        var buffer = [UInt8](repeating: 0, count: 8192)
        if stream.streamStatus == .notOpen {
            stream.open()
        }
        while bytes.count <= MAX_SIZE {
            // Fewer than none of the bytes is the IOException of Java's read,
            // and none of them is its end of the stream.
            let count = stream.read(&buffer, maxLength: buffer.count)
            if count < 0 {
                throw SVGXMLError(message: "The stream cannot be read")
            }
            if count == 0 {
                break
            }
            bytes.append(contentsOf: buffer[0..<count])
        }
        return try parse(bytes)
    }

    // The characters of the document: UTF-8, or the UTF-16 of a byte order
    // mark, without the mark. The decoder of Swift makes one U+FFFD of each
    // maximal part of a sequence that is not valid UTF-8.
    private static func decode(_ bytes: [UInt8]) -> [UInt16] {
        if bytes.count >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF {
            return Array(decodeUTF16(bytes, 2, true).utf16)
        }
        if bytes.count >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE {
            return Array(decodeUTF16(bytes, 2, false).utf16)
        }
        if bytes.count >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF {
            return Array(String(decoding: bytes[3...], as: UTF8.self).utf16)
        }
        return Array(String(decoding: bytes, as: UTF8.self).utf16)
    }

    // The characters of UTF-16, from the index on. A half of a character
    // that has no other half, and a last byte with no pair, is the
    // replacement character, and what follows is read as it stands. Facturx
    // reads the name of an embedded file with this, so a document is read
    // the same way wherever its UTF-16 stands.
    static func decodeUTF16(_ bytes: [UInt8], _ from: Int, _ bigEndian: Bool) -> String {
        var units = [UInt16]()
        units.reserveCapacity(max(bytes.count - from, 0) / 2 + 1)
        var i = from
        while i + 1 < bytes.count {
            let first = UInt16(bytes[i])
            let second = UInt16(bytes[i + 1])
            units.append(bigEndian ? (first << 8 | second) : (second << 8 | first))
            i += 2
        }
        if i < bytes.count {
            units.append(0xFFFD)    // A byte with no other, as Java's decoder leaves it.
        }
        // A surrogate with no pair becomes U+FFFD, again as Java's decoder leaves it.
        return String(decoding: units, as: UTF16.self)
    }

    // Whether the code unit is one of a character XML has: all but the control
    // characters other than the tab and the line breaks, and U+FFFE and U+FFFF.
    // The surrogates are the halves of the characters above U+FFFF, which the
    // decoder leaves in pairs, as it makes U+FFFD of a half with no other half.
    static func isXMLCharacter(_ ch: UInt16) -> Bool {
        return ch >= 0x20 ? (ch != 0xFFFE && ch != 0xFFFF)
                : (ch == 0x09 || ch == 0x0A || ch == 0x0D)
    }
}

// The reading of one document: the characters, as the UTF-16 code units Java's
// chars are, so that every index is the index of Java's and nothing is
// quadratic, where the reading is, and the elements that are open. A struct,
// so that its state is its own and is read and changed without the checks a
// class makes of each of its properties.
private struct Reader {
    private var xml: [UInt16]
    private var index = 0
    private var line = 1
    private var column = 1
    // The elements that are open, the innermost last, and the namespaces each
    // of them declares, nil where it declares none. They are read one after
    // another and not by recursion, so they are a stack of this reader and not
    // of the calls.
    private var open = [SVGXMLNode]()
    private var scopes = [[String: String]?]()
    // The first character that is not one of XML, or -1
    private var bad = -1

    private static let tab: UInt16 = 0x09           // \t
    private static let newline: UInt16 = 0x0A       // \n
    private static let carriageReturn: UInt16 = 0x0D
    private static let space: UInt16 = 0x20
    private static let exclamation: UInt16 = 0x21   // !
    private static let quotation: UInt16 = 0x22     // "
    private static let hash: UInt16 = 0x23          // #
    private static let ampersand: UInt16 = 0x26     // &
    private static let apostrophe: UInt16 = 0x27    // '
    private static let slash: UInt16 = 0x2F         // /
    private static let semicolon: UInt16 = 0x3B     // ;
    private static let less: UInt16 = 0x3C          // <
    private static let equal: UInt16 = 0x3D         // =
    private static let greater: UInt16 = 0x3E       // >
    private static let question: UInt16 = 0x3F     // ?
    private static let openBracket: UInt16 = 0x5B   // [
    private static let closeBracket: UInt16 = 0x5D  // ]
    private static let capitalX: UInt16 = 0x58      // X
    private static let smallX: UInt16 = 0x78        // x

    private static let commentStart = Array("<!--".utf16)
    private static let commentEnd = Array("-->".utf16)
    private static let cdataStart = Array("<![CDATA[".utf16)
    private static let cdataEnd = Array("]]>".utf16)
    private static let instructionStart = Array("<?".utf16)
    private static let instructionEnd = Array("?>".utf16)
    private static let declarationStart = Array("<?xml".utf16)
    private static let doctypeStart = Array("<!DOCTYPE".utf16)
    private static let encodingWord = Array("encoding".utf16)
    // The namespace of the prefix xml, which every document has without
    // declaring it.
    private static let namespaceXML = "http://www.w3.org/XML/1998/namespace"

    // Makes each line break of the document a line feed, as XML reads a
    // carriage return and the line feed after it, and a carriage return on
    // its own, and finds the first character that is not one of XML.
    init(_ decoded: [UInt16]) {
        var xml = decoded
        var written = 0
        var i = 0
        var bad = -1
        let count = xml.count
        xml.withUnsafeMutableBufferPointer { units in
            while i < count {
                var ch = units[i]
                if ch == Reader.carriageReturn {
                    ch = Reader.newline
                    if i + 1 < count && units[i + 1] == Reader.newline {
                        i += 1
                    }
                } else if bad == -1 && !SVGXMLParser.isXMLCharacter(ch) {
                    bad = written
                }
                units[written] = ch
                written += 1
                i += 1
            }
        }
        xml.removeSubrange(written...)
        self.xml = xml
        self.bad = bad
    }

    // --- The document -------------------------------------------------------

    mutating func document() throws -> SVGXMLNode {
        if bad != -1 {
            skip(bad)
            var hex = String(xml[bad], radix: 16, uppercase: true)
            while hex.count < 4 {
                hex = "0" + hex
            }
            throw error("The character U+\(hex) is not a character of XML")
        }
        try prolog()
        let root = try element()
        while index < xml.count {
            if Reader.isWhitespace(peek()) {
                next()
            } else if startsWith(Reader.commentStart) {
                try comment()
            } else if startsWith(Reader.instructionStart) {
                try processingInstruction()
            } else {
                throw error("There is more than the one element of the document")
            }
        }
        return root
    }

    // The declaration, the comments, the processing instructions and the
    // DOCTYPE before the root element.
    private mutating func prolog() throws {
        while index < xml.count {
            if Reader.isWhitespace(peek()) {
                next()
            } else if startsWith(Reader.declarationStart) && index == 0 {
                try declaration()
            } else if startsWith(Reader.commentStart) {
                try comment()
            } else if startsWith(Reader.instructionStart) {
                try processingInstruction()
            } else if startsWith(Reader.doctypeStart) {
                try doctype()
            } else if peek() == Reader.less {
                return
            } else {
                throw error("The document starts with text")
            }
        }
        throw error("The document has no element")
    }

    // <!DOCTYPE svg PUBLIC "..." "..." [ ... ]>: skipped to its end, its
    // internal subset too, past the > of its declarations, of its comments and
    // of its quoted strings. Its entities are not read.
    private mutating func doctype() throws {
        skip(Reader.doctypeStart.count)
        var depth = 0
        while index < xml.count {
            if startsWith(Reader.commentStart) {
                let end = Reader.indexOf(xml, Reader.commentEnd, index + 4)
                if end == -1 {
                    throw error("A comment of the document type declaration does not end")
                }
                skip(end + 3 - index)
                continue
            }
            let ch = peek()
            if ch == Reader.quotation || ch == Reader.apostrophe {
                let quote = next()
                while index < xml.count && peek() != quote {
                    next()
                }
                if index >= xml.count {
                    throw error("A string of the document type declaration does not end")
                }
            } else if ch == Reader.openBracket {
                depth += 1
            } else if ch == Reader.closeBracket {
                depth -= 1
            } else if ch == Reader.greater && depth <= 0 {
                next()
                return
            }
            next()
        }
        throw error("The document type declaration does not end")
    }

    // <?xml version="1.0" encoding="UTF-8"?>: the encoding is UTF-8 or the
    // UTF-16 of the byte order mark.
    private mutating func declaration() throws {
        let end = indexOf(Reader.instructionEnd)
        if end == -1 {
            throw error("The declaration does not end")
        }
        let declaration = Array(xml[index..<end])
        let encoding = Reader.indexOf(declaration, Reader.encodingWord, 0)
        if encoding != -1 {
            var quote = -1
            for i in (encoding + Reader.encodingWord.count)..<declaration.count {
                let ch = declaration[i]
                if ch == Reader.quotation || ch == Reader.apostrophe {
                    quote = i
                    break
                }
            }
            let close = (quote == -1) ? -1 : Reader.indexOf(declaration, declaration[quote], quote + 1)
            let name = (close == -1) ? "" : String(decoding: declaration[(quote + 1)..<close], as: UTF16.self)
            let lowercased = name.lowercased()
            if lowercased != "utf-8" && lowercased != "utf-16"
                    && lowercased != "utf-16be" && lowercased != "utf-16le" {
                throw error("The encoding \(name) is not read, only UTF-8 and UTF-16")
            }
        }
        skip(end + 2 - index)
    }

    private mutating func comment() throws {
        let end = indexOf(Reader.commentEnd)
        if end == -1 {
            throw error("The comment does not end")
        }
        skip(end + 3 - index)
    }

    private mutating func processingInstruction() throws {
        let end = indexOf(Reader.instructionEnd)
        if end == -1 {
            throw error("The processing instruction does not end")
        }
        skip(end + 2 - index)
    }

    // --- The elements -------------------------------------------------------

    // The root element and the elements in it, which are read one after
    // another with a stack and not by recursion, so that a document of any
    // depth is read in the stack of this call alone.
    private mutating func element() throws -> SVGXMLNode {
        var root: SVGXMLNode? = nil
        while true {
            if index >= xml.count {
                throw error("The element \(open.last?.getName() ?? "") does not end")
            }
            if peek() != Reader.less {
                try text()
                continue
            }
            let after = (index + 1 < xml.count) ? xml[index + 1] : 0
            if after == Reader.exclamation && startsWith(Reader.commentStart) {
                try comment()
            } else if after == Reader.exclamation && startsWith(Reader.cdataStart) {
                try characterData()
            } else if after == Reader.question {
                try processingInstruction()
            } else if after == Reader.slash {
                try closeTag()
                if open.isEmpty {
                    return root!    // A closing tag closes an element, so there is a root.
                }
            } else {
                if open.count >= SVGXMLParser.MAX_DEPTH {
                    throw error("The elements nest more than \(SVGXMLParser.MAX_DEPTH) deep")
                }
                if root != nil && open.isEmpty {
                    throw error("There is more than the one element of the document")
                }
                let node = try openTag()
                if root == nil {
                    root = node
                }
                if open.isEmpty {
                    return root!    // An empty root element, <svg/>
                }
            }
        }
    }

    // <name attribute="value"> or <name/>: the element, added to the one it is
    // in, and open until its closing tag unless it closes itself.
    private mutating func openTag() throws -> SVGXMLNode {
        next()      // The <
        let name = self.name()
        if name.isEmpty {
            throw error("A tag has no name")
        }
        var declared: [String: String]? = nil
        var attributes = [(String, String)]()
        var written = Set<String>()
        var first = true
        while true {
            let spaced = index < xml.count && Reader.isWhitespace(peek())
            skipWhitespace()
            let ch = peek()
            if ch == Reader.greater || ch == Reader.slash {
                break
            }
            // An attribute is after a space: XML has no name="1"other="2".
            if !spaced {
                if first {
                    throw error("The name of the tag \(name) is not followed by a space")
                }
                throw error("The attributes of \(name) are not separated by a space")
            }
            first = false
            let attributeName = self.name()
            if attributeName.isEmpty {
                throw error("The attributes of \(name) are not a name and a value")
            }
            skipWhitespace()
            if peek() != Reader.equal {
                throw error("The attribute \(attributeName) of \(name) has no value")
            }
            next()
            skipWhitespace()
            let value = try attributeValue()
            if !written.insert(attributeName).inserted {
                throw error("The attribute \(attributeName) of \(name) is written twice")
            }
            if attributeName == "xmlns" || attributeName.hasPrefix("xmlns:") {
                if declared == nil {
                    declared = [String: String]()
                }
                var prefix = ""
                if attributeName != "xmlns" {
                    prefix = String(attributeName.dropFirst(6))
                    // A prefix stands for a namespace, and the default
                    // namespace is the only one that may be none.
                    if value.isEmpty {
                        throw error("The prefix \(prefix) of \(name) is declared as no namespace")
                    }
                }
                declared![prefix] = value
            }
            attributes.append((attributeName, value))
        }
        // The namespace of the element is the one its prefix stands for in the
        // element itself or in the nearest element it is in that declares it.
        let prefix = Reader.prefixOf(name)
        guard let namespace = namespaceOf(prefix, declared) else {
            throw error("The prefix \(prefix) of \(name) is not declared")
        }
        let node = SVGXMLNode(name, namespace)
        for attribute in attributes {
            let attributePrefix = Reader.prefixOf(attribute.0)
            if !attributePrefix.isEmpty && attributePrefix != "xmlns"
                    && namespaceOf(attributePrefix, declared) == nil {
                throw error("The prefix \(attributePrefix) of the attribute \(attribute.0) of \(name) is not declared")
            }
            node.addAttribute(attribute.0, attribute.1)
        }
        if !open.isEmpty {
            open[open.count - 1].addChild(node)
        }
        let empty = peek() == Reader.slash
        if empty {
            next()
        }
        if peek() != Reader.greater {
            throw error("The tag \(name) does not end with >")
        }
        next()
        if !empty {
            open.append(node)
            scopes.append(declared)
        }
        return node
    }

    // The prefix of the name, the part before its colon, or an empty string
    // when it has none.
    private static func prefixOf(_ name: String) -> String {
        guard let colon = name.utf8.firstIndex(of: 0x3A) else {    // :
            return ""
        }
        return String(name[..<colon])
    }

    // The namespace the prefix stands for, in the namespaces the element
    // declares and then in the ones of the elements it is in, from the
    // innermost out, or nil when it stands for none. No prefix is the default
    // namespace, which is none where no element declares one, and xml is the
    // namespace of XML itself.
    private func namespaceOf(_ prefix: String, _ declared: [String: String]?) -> String? {
        if let namespace = declared?[prefix] {
            return namespace
        }
        var i = scopes.count - 1
        while i >= 0 {
            if let namespace = scopes[i]?[prefix] {
                return namespace
            }
            i -= 1
        }
        if prefix.isEmpty {
            return ""
        }
        if prefix == "xml" {
            return Reader.namespaceXML
        }
        return nil
    }

    // </name>, which ends the element that is open.
    private mutating func closeTag() throws {
        skip(2)     // The </
        let name = self.name()
        skipWhitespace()
        if peek() != Reader.greater {
            throw error("The closing tag of \(name) does not end with >")
        }
        next()
        if open.isEmpty {
            throw error("The closing tag of \(name) closes no element")
        }
        let node = open.removeLast()
        scopes.removeLast()
        if node.getName() != name {
            throw error("The element \(node.getName()) is closed by the tag of \(name)")
        }
    }

    // <![CDATA[ the text as it is ]]>
    private mutating func characterData() throws {
        let end = indexOf(Reader.cdataEnd)
        if end == -1 {
            throw error("The character data does not end")
        }
        if open.isEmpty {
            throw error("There is text outside the element of the document")
        }
        open[open.count - 1].addText(xml[(index + Reader.cdataStart.count)..<end])
        skip(end + 3 - index)
    }

    // The text up to the next <, with its entities read. The characters
    // between the entities are added as they stand, a run at a time.
    private mutating func text() throws {
        var buf = [UInt16]()
        var start = index
        while index < xml.count {
            let ch = xml[index]
            if ch == Reader.less {
                break
            }
            if ch == Reader.ampersand {
                buf.append(contentsOf: xml[start..<index])
                next()
                buf.append(contentsOf: try entity())
                start = index
                continue
            }
            index += 1
            if ch == Reader.newline {
                line += 1
                column = 1
            } else {
                column += 1
            }
        }
        buf.append(contentsOf: xml[start..<index])
        if open.isEmpty {
            if SVGXMLNode.trimmed(buf).isEmpty {
                return
            }
            throw error("There is text outside the element of the document")
        }
        open[open.count - 1].addText(buf)
    }

    // The &lt;, &gt;, &amp;, &quot; and &apos; of XML, and the numeric ones,
    // such as &#160; and &#xA0;. The & is read.
    private mutating func entity() throws -> [UInt16] {
        let end = indexOf(Reader.semicolon)
        if end == -1 || end - index > 16 {
            throw error("An & that is not an entity, such as &amp;")
        }
        let chars = Array(xml[index..<end])
        skip(end + 1 - index)
        let name = String(decoding: chars, as: UTF16.self)
        if name == "lt" {
            return [Reader.less]
        } else if name == "gt" {
            return [Reader.greater]
        } else if name == "amp" {
            return [Reader.ampersand]
        } else if name == "quot" {
            return [Reader.quotation]
        } else if name == "apos" {
            return [Reader.apostrophe]
        } else if chars.first == Reader.hash {
            let hex = chars.count > 1 && (chars[1] == Reader.smallX || chars[1] == Reader.capitalX)
            let digits = Array(chars[(hex ? 2 : 1)...])
            // The digits of XML and no others: Int32 takes a sign, as
            // Integer.parseInt does, so it reads &#-1; and &#+65;, which are
            // not numbers of XML. An Int32, as Java's int is: a number that
            // does not fit in one is not a number to either of them.
            guard Reader.isNumber(digits, hex),
                    let code = Int32(String(decoding: digits, as: UTF16.self),
                            radix: hex ? 16 : 10) else {
                throw error("The character &\(name); is not a number")
            }
            // A character XML has, which a surrogate on its own is not
            guard code <= 0x10FFFF, !(code >= 0xD800 && code <= 0xDFFF),
                    code > 0xFFFF || SVGXMLParser.isXMLCharacter(UInt16(code)),
                    let scalar = UnicodeScalar(UInt32(code)) else {
                throw error("The character &\(name); is not a character of XML")
            }
            return Array(String(scalar).utf16)
        }
        throw error("The entity &\(name); is not one of XML, and this reads no others")
    }

    // --- The characters -----------------------------------------------------

    // A name of an element or of an attribute: what stands before the
    // whitespace, the =, the / or the > after it.
    private mutating func name() -> String {
        let start = index
        while index < xml.count {
            let ch = xml[index]
            if Reader.isWhitespace(ch) || ch == Reader.equal || ch == Reader.slash
                    || ch == Reader.greater || ch == Reader.less {
                break
            }
            next()
        }
        return SVGXMLNode.string(xml[start..<index])
    }

    // "value" or 'value', with its entities read.
    private mutating func attributeValue() throws -> String {
        let quote = peek()
        if quote != Reader.quotation && quote != Reader.apostrophe {
            throw error("The value of an attribute is not in quotes")
        }
        next()
        var buf = [UInt16]()
        while true {
            if index >= xml.count {
                throw error("The value of an attribute does not end")
            }
            let ch = next()
            if ch == quote {
                return String(decoding: buf, as: UTF16.self)
            } else if ch == Reader.less {
                throw error("The value of an attribute holds a <")
            } else if ch == Reader.ampersand {
                buf.append(contentsOf: try entity())
            } else if ch == Reader.tab || ch == Reader.newline {
                // XML makes each tab and line break of a value a space, and
                // keeps the ones written as entities, such as &#10;.
                buf.append(Reader.space)
            } else {
                buf.append(ch)
            }
        }
    }

    private func peek() -> UInt16 {
        return (index < xml.count) ? xml[index] : 0
    }

    @discardableResult
    private mutating func next() -> UInt16 {
        let ch = xml[index]
        index += 1
        if ch == Reader.newline {
            line += 1
            column = 1
        } else {
            column += 1
        }
        return ch
    }

    private mutating func skip(_ count: Int) {
        var i = 0
        while i < count && index < xml.count {
            next()
            i += 1
        }
    }

    private mutating func skipWhitespace() {
        while index < xml.count && Reader.isWhitespace(xml[index]) {
            next()
        }
    }

    // A number of a numeric entity: the digits 0 to 9, and a to f as well when
    // the entity is hexadecimal, in ASCII and in no other script.
    private static func isNumber(_ digits: [UInt16], _ hex: Bool) -> Bool {
        if digits.isEmpty {
            return false
        }
        for ch in digits {
            if ch >= 0x30 && ch <= 0x39 {           // 0 to 9
                continue
            }
            if hex && ((ch >= 0x61 && ch <= 0x66) || (ch >= 0x41 && ch <= 0x46)) {   // a to f, A to F
                continue
            }
            return false
        }
        return true
    }

    private static func isWhitespace(_ ch: UInt16) -> Bool {
        return ch == space || ch == tab || ch == newline || ch == carriageReturn
    }

    // The characters at the index of the document, as String.startsWith is.
    private func startsWith(_ needle: [UInt16]) -> Bool {
        guard index + needle.count <= xml.count else {
            return false
        }
        var i = 0
        while i < needle.count {
            if xml[index + i] != needle[i] {
                return false
            }
            i += 1
        }
        return true
    }

    // Where the characters are from the index of the document, or -1, as
    // String.indexOf is.
    private func indexOf(_ needle: [UInt16]) -> Int {
        return Reader.indexOf(xml, needle, index)
    }

    private func indexOf(_ needle: UInt16) -> Int {
        return Reader.indexOf(xml, needle, index)
    }

    private static func indexOf(_ haystack: [UInt16], _ needle: [UInt16], _ from: Int) -> Int {
        var start = (from < 0) ? 0 : from
        while start + needle.count <= haystack.count {
            var i = 0
            while i < needle.count && haystack[start + i] == needle[i] {
                i += 1
            }
            if i == needle.count {
                return start
            }
            start += 1
        }
        return -1
    }

    private static func indexOf(_ haystack: [UInt16], _ needle: UInt16, _ from: Int) -> Int {
        var i = from
        while i < haystack.count {
            if haystack[i] == needle {
                return i
            }
            i += 1
        }
        return -1
    }

    private func error(_ message: String) -> SVGXMLError {
        return SVGXMLError(message: message + ", at line \(line) column \(column)")
    }
}   // End of SVGXMLParser.swift
