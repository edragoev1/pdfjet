/*
 * PageLayout.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Used to specify the PDF page layout.
 */
public class PageLayout {
    /**
     * Creates page layout object.
     */
    public PageLayout() {
    }

    /** Display one page at a time */
    public static final String SINGLE_PAGE = "SinglePage";

    /** Display the pages in one column */
    public static final String ONE_COLUMN = "OneColumn";

    /** Odd-numbered pages on the left */
    public static final String TWO_COLUMN_LEFT = "TwoColumnLeft";

    /** Odd-numbered pages on the right */
    public static final String TWO_COLUMN_RIGTH = "TwoColumnRight";

    /** Odd-numbered pages on the left */
    public static final String TWO_PAGE_LEFT = "TwoPageLeft";

    /** Odd-numbered pages on the right */
    public static final String TWO_PAGE_RIGTH = "TwoPageRight";
}
