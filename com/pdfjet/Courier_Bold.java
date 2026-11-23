/*
 * Courier_Bold.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * The metrics of Courier-Bold, one of the 14 standard PDF fonts.
 * The values are in thousandths of the font size.
 */
final class Courier_Bold {
    private Courier_Bold() {
    }

    /** The PostScript name of the font */
    public static final String name = "Courier-Bold";
    /** The x coordinate of the lower left corner of the font bounding box */
    public static final int bBoxLLx = -113;
    /** The y coordinate of the lower left corner of the font bounding box */
    public static final int bBoxLLy = -250;
    /** The x coordinate of the upper right corner of the font bounding box */
    public static final int bBoxURx = 749;
    /** The y coordinate of the upper right corner of the font bounding box */
    public static final int bBoxURy = 801;
    /** The position of the underline relative to the baseline */
    public static final int underlinePosition = -100;
    /** The thickness of the underline */
    public static final int underlineThickness = 50;
    /** The copyright notice of the font */
    public static final String notice = "Copyright (c) 1989, 1990, 1991, 1993, 1997 Adobe Systems Incorporated. All Rights Reserved.";
    /** The same widths and kerning as Courier: one row for each character from 32 to 255 */
    public static final int[][] metrics = Courier.metrics;
}
