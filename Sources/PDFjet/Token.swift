/**
 * Token.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// Please see PDF.swift
///
public class Token {
    // Fundamental structural tokens
    public static let space: UInt8 = 32     // ASCII space
    public static let newline: UInt8 = 10   // ASCII LF

    public static let beginDictionary = [UInt8]("<<\n".utf8)
    public static let endDictionary = [UInt8](">>\n".utf8)
    public static let stream = [UInt8]("stream\n".utf8)
    public static let endStream = [UInt8]("\nendstream\n".utf8)

    // Object management tokens
    public static let newObj = [UInt8](" 0 obj\n".utf8)
    public static let endObj = [UInt8]("endobj\n".utf8)
    public static let objRef = [UInt8](" 0 R\n".utf8)

    // Text and content tokens
    public static let beginText = [UInt8]("BT\n".utf8)
    public static let endText = [UInt8]("ET\n".utf8)

    // Essential property tokens (used everywhere)
    public static let length = [UInt8]("/Length ".utf8)
    public static let type = [UInt8]("/Type ".utf8)
    public static let resources = [UInt8]("/Resources ".utf8)
}
