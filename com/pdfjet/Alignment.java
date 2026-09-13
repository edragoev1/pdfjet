/*
 * Alignment.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Used to specify the horizontal and vertical alignment, for example of the
 * text in a Cell, TextBox, TextBlock, Paragraph or TextColumn.
 */
public enum Alignment {
    /** Aligns to the left. */
    LEFT,
    /** Aligns to the right. */
    RIGHT,
    /** Centers horizontally or vertically. */
    CENTER,
    /** Justifies the text. */
    JUSTIFY,
    /** Aligns to the top. */
    TOP,
    /** Aligns to the bottom. */
    BOTTOM;
}
