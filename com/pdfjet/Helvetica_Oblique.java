/*
 * Helvetica_Oblique.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * The metrics of Helvetica-Oblique, one of the 14 standard PDF fonts.
 * The values are in thousandths of the font size.
 */
final class Helvetica_Oblique {
    private Helvetica_Oblique() {
    }

    /** The PostScript name of the font */
    public static final String name = "Helvetica-Oblique";
    /** The x coordinate of the lower left corner of the font bounding box */
    public static final int bBoxLLx = -170;
    /** The y coordinate of the lower left corner of the font bounding box */
    public static final int bBoxLLy = -225;
    /** The x coordinate of the upper right corner of the font bounding box */
    public static final int bBoxURx = 1116;
    /** The y coordinate of the upper right corner of the font bounding box */
    public static final int bBoxURy = 931;
    /** The position of the underline relative to the baseline */
    public static final int underlinePosition = -100;
    /** The thickness of the underline */
    public static final int underlineThickness = 50;
    /** The copyright notice of the font */
    public static final String notice = "Copyright (c) 1985, 1987, 1989, 1990, 1997 Adobe Systems Incorporated. All Rights Reserved. Helvetica is a trademark of Linotype-Hell AG and/or its subsidiaries.";
    /** The same widths and kerning as Helvetica: one row for each character from 32 to 255 */
    public static final int[][] metrics = Helvetica.metrics;
}
