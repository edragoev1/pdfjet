/**
 * PageLayout.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

/**
 *  Used to specify the PDF page layout.
 *
 */
public enum PageLayout: String {
    /// Displays one page at a time.
    case SINGLE_PAGE = "SinglePage"
    /// Displays the pages in one column.
    case ONE_COLUMN = "OneColumn"
    /// Displays the pages in two columns, with odd-numbered pages on the left.
    case TWO_COLUMN_LEFT = "TwoColumnLeft"
    /// Displays the pages in two columns, with odd-numbered pages on the right.
    case TWO_COLUMN_RIGHT = "TwoColumnRight"
    /// Displays two pages at a time, with odd-numbered pages on the left.
    case TWO_PAGE_LEFT = "TwoPageLeft"
    /// Displays two pages at a time, with odd-numbered pages on the right.
    case TWO_PAGE_RIGHT = "TwoPageRight"
}
