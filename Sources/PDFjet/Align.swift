/**
 * Align.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

/**
 *  Used to specify the text alignment in paragraphs.
 *  See the Paragraph class for more details.
 *
 */
public class Align {
    /// Aligns the text to the left.
    public static let LEFT: UInt32    = 0x00000000
    /// Centers the text.
    public static let CENTER: UInt32  = 0x00100000
    /// Aligns the text to the right.
    public static let RIGHT: UInt32   = 0x00200000
    /// Justifies the text.
    public static let JUSTIFY: UInt32 = 0x00300000

    /// Aligns the text to the top.
    public static let TOP: UInt32     = 0x00400000
    /// Aligns the text to the bottom.
    public static let BOTTOM: UInt32  = 0x00500000
}
