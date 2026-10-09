/**
 * PDFjetXMLNode.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

// This is the XMLNode of the Java, the C# and the Go ports, under another
// name: Foundation, and FoundationXML on Linux, already has XMLNode. Please
// see PDFjetXMLParser.swift.

///
/// An element of an XML document that PDFjetXMLParser read: its name, its
/// attributes, its text and the elements in it.
///
/// The elements are found by a path of the names without their prefix, such as
/// "SupplyChainTradeTransaction/ApplicableHeaderTradeAgreement/BuyerReference",
/// because the prefixes of an invoice are its own: what one writes as ram:ID
/// another writes as a:ID. A * matches an element of any name.
///
public final class PDFjetXMLNode {
    private let name: String
    private let localName: String
    private let namespace: String
    // The names in the order the document writes them, and their values.
    private var attributeNames = [String]()
    private var attributeValues = [String: String]()
    private var children = [PDFjetXMLNode]()
    private var text = [UInt16]()

    init(_ name: String, _ namespace: String) {
        self.name = name
        let utf8 = name.utf8
        let colon = utf8.firstIndex(of: 0x3A)   // :
        self.localName = (colon == nil) ? name : String(name[utf8.index(after: colon!)...])
        self.namespace = namespace
    }

    func addAttribute(_ attributeName: String, _ value: String) {
        if attributeValues[attributeName] == nil {
            attributeNames.append(attributeName)
        }
        attributeValues[attributeName] = value
    }

    func addChild(_ child: PDFjetXMLNode) {
        children.append(child)
    }

    func addText<S: Sequence>(_ characters: S) where S.Element == UInt16 {
        text.append(contentsOf: characters)
    }

    ///
    /// Returns the name of the element as the document writes it, with its
    /// prefix when it has one, such as ram:ID.
    ///
    /// - Returns: the name.
    ///
    public func getName() -> String {
        return name
    }

    ///
    /// Returns the name of the element without its prefix, such as ID.
    ///
    /// - Returns: the name without its prefix.
    ///
    public func getLocalName() -> String {
        return localName
    }

    ///
    /// Returns the namespace of the element, the URI its prefix stands for, or
    /// an empty string when it is in no namespace.
    ///
    /// - Returns: the namespace.
    ///
    public func getNamespace() -> String {
        return namespace
    }

    ///
    /// Returns the text of the element, the characters in it that are not in
    /// the elements it holds, as they are written.
    ///
    /// - Returns: the text.
    ///
    public func getText() -> String {
        return PDFjetXMLNode.string(text[...])
    }

    ///
    /// Returns the elements in this one, in their order.
    ///
    /// - Returns: the elements.
    ///
    public func getChildren() -> [PDFjetXMLNode] {
        return children
    }

    ///
    /// Returns the value of the attribute of that name, by its name with its
    /// prefix, such as unitCode or ram:unitCode, or nil when it has none.
    ///
    /// - Parameter attributeName: the name of the attribute.
    /// - Returns: the value, or nil.
    ///
    public func getAttribute(_ attributeName: String) -> String? {
        if let value = attributeValues[attributeName] {
            return value
        }
        for key in attributeNames {
            let units = key.utf16
            if let colon = units.firstIndex(of: 0x3A),   // :
                    String(decoding: units[units.index(after: colon)...], as: UTF16.self) == attributeName {
                return attributeValues[key]
            }
        }
        return nil
    }

    ///
    /// Returns the attributes of the element, by their names as the document
    /// writes them, in the order the document writes them.
    ///
    /// - Returns: the attributes.
    ///
    public func getAttributes() -> [(name: String, value: String)] {
        return attributeNames.map { (name: $0, value: attributeValues[$0]!) }
    }

    ///
    /// Returns the elements at the path under this one, none when there are
    /// none: the names of the path are the names without their prefix, and a *
    /// matches an element of any name.
    ///
    /// - Parameter path: the path, such as "IncludedSupplyChainTradeLineItem/SpecifiedTradeProduct/Name".
    /// - Returns: the elements.
    ///
    public func findAll(_ path: String) -> [PDFjetXMLNode] {
        var found = [PDFjetXMLNode]()
        found.append(self)
        for step in PDFjetXMLNode.steps(path) {
            var next = [PDFjetXMLNode]()
            for node in found {
                for child in node.children where PDFjetXMLNode.matches(step, child.localName) {
                    next.append(child)
                }
            }
            found = next
        }
        return found
    }

    // The names of the path, which are compared with the names of the
    // elements byte by byte: a split of the path into Strings took most of the
    // time a large invoice took to read.
    private static func steps(_ path: String) -> [Substring] {
        var steps = [Substring]()
        let utf8 = path.utf8
        var start = utf8.startIndex
        var i = start
        while i != utf8.endIndex {
            if utf8[i] == 0x2F {                // /
                if start != i {
                    steps.append(path[start..<i])
                }
                start = utf8.index(after: i)
            }
            i = utf8.index(after: i)
        }
        if start != i {
            steps.append(path[start..<i])
        }
        return steps
    }

    private static func matches(_ step: Substring, _ name: String) -> Bool {
        return (step.utf8.count == 1 && step.utf8.first == 0x2A)   // *
                || step.utf8.elementsEqual(name.utf8)
    }

    // The first element at the steps under this one, from the step at the
    // index on, or nil: the elements in the order findAll returns them, which
    // is the order of the document, without the ones after the first.
    private func first(_ steps: [Substring], _ at: Int) -> PDFjetXMLNode? {
        if at == steps.count {
            return self
        }
        let step = steps[at]
        for child in children where PDFjetXMLNode.matches(step, child.localName) {
            if let found = child.first(steps, at + 1) {
                return found
            }
        }
        return nil
    }

    ///
    /// Returns the first element at the path under this one, or nil.
    ///
    /// - Parameter path: the path.
    /// - Returns: the element, or nil.
    ///
    public func find(_ path: String) -> PDFjetXMLNode? {
        return first(PDFjetXMLNode.steps(path), 0)
    }

    ///
    /// Returns the text of the first element at the path under this one,
    /// without the spaces and line breaks at its ends, or nil when the path
    /// has no element.
    ///
    /// - Parameter path: the path.
    /// - Returns: the text, or nil.
    ///
    public func getValue(_ path: String) -> String? {
        guard let node = find(path) else {
            return nil
        }
        return PDFjetXMLNode.trimmed(node.text)
    }

    // The characters without those of a space or less at their ends, as
    // Java's String.trim leaves them.
    public static func trimmed(_ units: [UInt16]) -> String {
        var start = 0
        var end = units.count
        while start < end && units[start] <= 0x20 {
            start += 1
        }
        while start < end && units[end - 1] <= 0x20 {
            end -= 1
        }
        return string(units[start..<end])
    }

    // The String of the code units. The names and most of the text of an
    // invoice are ASCII, which is made a String of directly, a byte a
    // character: String(decoding:) takes some twenty times as long over them,
    // which was most of the time a large invoice took to read.
    static func string(_ units: ArraySlice<UInt16>) -> String {
        for unit in units where unit >= 0x80 {
            return String(decoding: units, as: UTF16.self)
        }
        // String(unsafeUninitializedCapacity:) is of macOS 11 and the systems
        // of its year; on older ones the ASCII bytes are decoded as UTF-8.
        if #available(macOS 11.0, iOS 14.0, tvOS 14.0, watchOS 7.0, *) {
            return String(unsafeUninitializedCapacity: units.count) { buffer in
                var i = 0
                for unit in units {
                    buffer[i] = UInt8(truncatingIfNeeded: unit)
                    i += 1
                }
                return i
            }
        }
        return String(decoding: units.map { UInt8(truncatingIfNeeded: $0) }, as: UTF8.self)
    }
}   // End of PDFjetXMLNode.swift
