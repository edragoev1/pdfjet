/**
 * SVGXMLError.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

// This is the XMLException of the Java, the C# and the Go ports, as an error
// of Swift and under another name: the names of XML are Foundation's. Please
// see SVGXMLParser.swift.

///
/// The document is not the XML that SVGXMLParser reads; the message says where.
///
struct SVGXMLError: Error, Equatable, CustomStringConvertible {
    let message: String

    init(message: String) {
        self.message = message
    }

    var description: String {
        return message
    }
}
