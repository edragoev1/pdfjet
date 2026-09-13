/**
 * Token.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// Byte sequences of the PDF syntax, internal as in the other ports.
/// Please see PDF.swift
///
class Token {
    // Fundamental structural tokens
    /// A space.
    static let space: UInt8 = 32     // ASCII space
    /// A line feed.
    static let newline: UInt8 = 10   // ASCII LF

    /// The start of a dictionary: <<
    static let beginDictionary = [UInt8]("<<\n".utf8)
    /// The end of a dictionary: >>
    static let endDictionary = [UInt8](">>\n".utf8)
    /// The stream keyword.
    static let stream = [UInt8]("stream\n".utf8)
    /// The endstream keyword.
    static let endStream = [UInt8]("\nendstream\n".utf8)

    // Object management tokens
    /// The " 0 obj" that follows an object number.
    static let newObj = [UInt8](" 0 obj\n".utf8)
    /// The endobj keyword.
    static let endObj = [UInt8]("endobj\n".utf8)
    /// The " 0 R" that follows the number of a referenced object.
    static let objRef = [UInt8](" 0 R\n".utf8)

    // Text and content tokens
    /// The BT operator, which begins a text object.
    static let beginText = [UInt8]("BT\n".utf8)
    /// The ET operator, which ends a text object.
    static let endText = [UInt8]("ET\n".utf8)

    // Essential property tokens (used everywhere)
    /// The /Length key.
    static let length = [UInt8]("/Length ".utf8)
    /// The /Type key.
    static let type = [UInt8]("/Type ".utf8)
    /// The /Resources key.
    static let resources = [UInt8]("/Resources ".utf8)
}
