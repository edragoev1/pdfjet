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
public enum PageLayout {
    /** Display one page at a time */
    SINGLE_PAGE("SinglePage"),
    /** Display the pages in one column */
    ONE_COLUMN("OneColumn"),
    /** Odd-numbered pages on the left */
    TWO_COLUMN_LEFT("TwoColumnLeft"),
    /** Odd-numbered pages on the right */
    TWO_COLUMN_RIGHT("TwoColumnRight"),
    /** Odd-numbered pages on the left */
    TWO_PAGE_LEFT("TwoPageLeft"),
    /** Odd-numbered pages on the right */
    TWO_PAGE_RIGHT("TwoPageRight");

    /** The name written to the /PageLayout entry. */
    final String value;

    PageLayout(String value) {
        this.value = value;
    }
}
