/*
 * JISB5.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Used to specify PDF page with size <strong>JIS B5</strong>, the Japanese B5 of 182 by 257 mm.
 * For more information about the page size classes - A3, A4, A5, JISB5, Executive, Letter, Legal and Tabloid - see the Page class.
 */
public class JISB5 {
    private JISB5() {
    }
    /**
     * This is a public static variable that specifies that page size in portrait orientation.
     */
    public static final PageSize PORTRAIT = new PageSize(516.0f, 729.0f);
    /**
     * This is a public static variable that specifies that page size in landscape orientation.
     */
    public static final PageSize LANDSCAPE = new PageSize(729.0f, 516.0f);
}
