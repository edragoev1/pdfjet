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
    public static let NONE: UInt32   = 0x00000000
    public static let TOP: UInt32    = 0x00010000
    public static let BOTTOM: UInt32 = 0x00020000
    public static let LEFT: UInt32   = 0x00040000
    public static let RIGHT: UInt32  = 0x00080000
    public static let ALL: UInt32    = 0x000F0000
}
