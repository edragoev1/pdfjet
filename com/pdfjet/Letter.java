/*
 * Letter.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Used to specify PDF page with size <strong>Letter</strong>.
 * For more information about the page size classes - A3, A4, A5, B5, Executive, Letter, Legal and Tabloid - see the Page class.
 */
public class Letter {
    /** Default Constructor */
    public Letter() {
    }

    /** Portrait orientation */
    public static final float[] PORTRAIT = new float[] {612.0f, 792.0f};
    /** Landscape orientation */
    public static final float[] LANDSCAPE = new float[] {792.0f, 612.0f};
}
