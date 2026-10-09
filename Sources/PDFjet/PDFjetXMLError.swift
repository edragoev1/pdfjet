/**
 * PDFjetXMLError.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

// This is the XMLException of the Java, the C# and the Go ports, as an error
// of Swift and under another name: the names of XML are Foundation's. Please
// see PDFjetXMLParser.swift.

///
/// The document is not the XML that PDFjetXMLParser reads; the message says where.
///
public struct PDFjetXMLError: Error, Equatable, CustomStringConvertible {
    public let message: String

    public init(message: String) {
        self.message = message
    }

    public var description: String {
        return message
    }
}
