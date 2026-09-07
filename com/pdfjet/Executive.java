/*
 * Executive.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Used to specify PDF page with size <strong>Executive</strong>.
 * For more information about the page size classes - A3, A4, A5, B5, Executive, Letter, Legal and Tabloid - see the Page class.
 */
public class Executive {
    /** Default Constructor */
    public Executive() {
    }

    /** PORTRAIT orientation */
    public static final float[] PORTRAIT = new float[] {522.0f, 756.0f};

    /** LANDSCAPE orientation */
    public static final float[] LANDSCAPE = new float[] {756.0f, 522.0f};
}
