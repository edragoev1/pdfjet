/**
 * B5.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Used to specify PDF page with size <strong>B5</strong>.
 * For more information about the page size classes - A3, A4, A5, B5, Executive, Letter, Legal and Tabloid - see the Page class.
 */
public class B5 {
    /** The default constructor */
    public B5() {
    }
    /**
     * This is a public static variable that specifies that page size in portrait orientation.
     */
    public static final float[] PORTRAIT = new float[] {516.0f, 729.0f};
    /**
     * This is a public static variable that specifies that page size in landscape orientation.
     */
    public static final float[] LANDSCAPE = new float[] {729.0f, 516.0f};
}
