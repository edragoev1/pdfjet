/**
 * Legal.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Used to specify PDF page with size <strong>Legal</strong>.
 * For more information about the page size classes - A3, A4, A5, B5, Executive, Letter, Legal and Tabloid - see the Page class.
 */
public class Legal {
    /** Default Constructor */
    public Legal() {
    }

    /** Portrait orientation */
    public static final float[] PORTRAIT = new float[] {612.0f, 1008.0f};
    /** Landscape orientation */
    public static final float[] LANDSCAPE = new float[] {1008.0f, 612.0f};
}
