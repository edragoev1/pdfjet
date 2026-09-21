/**
 * Alignment.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

/**
 * Used to specify the horizontal and vertical alignment, for example of the
 * text in a Cell, TextBlock, Paragraph or TextColumn.
 */
/// The values are the same as the ordinals of the other three ports, so that
/// a Cell can keep three alignments in the bits of one integer.
public enum Alignment: UInt32 {
    /// Aligns to the left.
    case LEFT = 0
    /// Aligns to the right.
    case RIGHT = 1
    /// Centers horizontally or vertically.
    case CENTER = 2
    /// Justifies the text.
    case JUSTIFY = 3
    /// Aligns to the top.
    case TOP = 4
    /// Aligns to the bottom.
    case BOTTOM = 5
}
