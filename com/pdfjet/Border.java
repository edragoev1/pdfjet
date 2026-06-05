/**
 * Border.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Used to control the visibility of cell borders.
 * See the Cell class for more information.
 */
public class Border {
    /** The default constructor */
    public Border() {
    }

    /** Specifies no borders. */
    public static final int NONE   = 0x00000000;
    /** Specifies top border. */
    public static final int TOP    = 0x00010000;
    /** Specifies bottom border. */
    public static final int BOTTOM = 0x00020000;
    /** Specifies left border. */
    public static final int LEFT   = 0x00040000;
    /** Specifies right border. */
    public static final int RIGHT  = 0x00080000;
    /** Specifies all borders. */
    public static final int ALL    = 0x000F0000;
}
