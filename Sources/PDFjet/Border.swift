/**
 * Border.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

/**
 * Used to control the visibility of cell borders.
 * See the Cell class for more information.
 */
public class Border {
    /// No border.
    public static let NONE: UInt32   = 0x00000000
    /// The top border.
    public static let TOP: UInt32    = 0x00010000
    /// The bottom border.
    public static let BOTTOM: UInt32 = 0x00020000
    /// The left border.
    public static let LEFT: UInt32   = 0x00040000
    /// The right border.
    public static let RIGHT: UInt32  = 0x00080000
    /// All four borders.
    public static let ALL: UInt32    = 0x000F0000
}
