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
    public static let LEFT: UInt32    = 0x00000000
    public static let CENTER: UInt32  = 0x00100000
    public static let RIGHT: UInt32   = 0x00200000
    public static let JUSTIFY: UInt32 = 0x00300000

    public static let TOP: UInt32     = 0x00400000
    public static let BOTTOM: UInt32  = 0x00500000
}
