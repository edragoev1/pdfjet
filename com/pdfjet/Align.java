/**
 * Align.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Used to specify the text alignment in paragraphs.
 * See the Paragraph class for more details.
 */
public class Align {
    /** The default constructor */
    public Align() {
    }

    /** Specifies left alignment */
    public static final int LEFT    = 0x00000000;
    /** Specifies centered paragraph */
    public static final int CENTER  = 0x00100000;
    /** Specifies right alignment */
    public static final int RIGHT   = 0x00200000;
    /** Specifies justified paragraph */
    public static final int JUSTIFY = 0x00300000;

    /** Specifies top alignment */
    public static final int TOP     = 0x00400000;
    /** Specifies bottom alignment */
    public static final int BOTTOM  = 0x00500000;
}
