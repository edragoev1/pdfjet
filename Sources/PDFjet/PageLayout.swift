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
public class PageLayout {
    /// Displays one page at a time.
    public static let SINGLE_PAGE = "SinglePage"           // Display one page at a time
    /// Displays the pages in one column.
    public static let ONE_COLUMN = "OneColumn"             // Display the pages in one column
    /// Displays the pages in two columns, with odd-numbered pages on the left.
    public static let TWO_COLUMN_LEFT = "TwoColumnLeft"    // Odd-numbered pages on the left
    /// Displays the pages in two columns, with odd-numbered pages on the right.
    public static let TWO_COLUMN_RIGHT = "TwoColumnRight"  // Odd-numbered pages on the right
    /// Displays two pages at a time, with odd-numbered pages on the left.
    public static let TWO_PAGE_LEFT = "TwoPageLeft"        // Odd-numbered pages on the left
    /// Displays two pages at a time, with odd-numbered pages on the right.
    public static let TWO_PAGE_RIGHT = "TwoPageRight"      // Odd-numbered pages on the right
}
