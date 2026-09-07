/*
 * A5.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Used to specify PDF page with size <strong>A5</strong>.
 * For more information about the page size classes - A3, A4, A5, B5, Executive, Letter, Legal and Tabloid - see the Page class.
 */
public class A5 {
    /**
     * The default constructor.
     */
    public A5() {
    }
    /**
     * This is a public static variable that specifies that page size in portrait orientation.
     */
    public static final float[] PORTRAIT = new float[] {420.0f, 595.0f};
    /**
     * This is a public static variable that specifies that page size in landscape orientation.
     */
    public static final float[] LANDSCAPE = new float[] {595.0f, 420.0f};
}
