/**
 * Embed.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Used to specify if TrueType or OpenType font should be embedded in the PDF document.
 * See the Font class for more details.
 */
public class Embed {
    /** The default constructor */
    public Embed() {
    }

    /** Embed the file */
    public static final boolean YES = true;

    /** Do not embed the file */
    public static final boolean NO = false;
}
