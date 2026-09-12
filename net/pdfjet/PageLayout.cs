/*
 * PageLayout.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// Used to specify the PDF page layout.
/// </summary>
public class PageLayout {
    /// <summary>Displays one page at a time.</summary>
    public const String SINGLE_PAGE = "SinglePage";          // Display one page at a time
    /// <summary>Displays the pages in one column.</summary>
    public const String ONE_COLUMN = "OneColumn";            // Display the pages in one column
    /// <summary>Displays the pages in two columns, with odd-numbered pages on the left.</summary>
    public const String TWO_COLUMN_LEFT = "TwoColumnLeft";   // Odd-numbered pages on the left
    /// <summary>Displays the pages in two columns, with odd-numbered pages on the right.</summary>
    public const String TWO_COLUMN_RIGHT = "TwoColumnRight"; // Odd-numbered pages on the right
    /// <summary>Displays two pages at a time, with odd-numbered pages on the left.</summary>
    public const String TWO_PAGE_LEFT = "TwoPageLeft";       // Odd-numbered pages on the left
    /// <summary>Displays two pages at a time, with odd-numbered pages on the right.</summary>
    public const String TWO_PAGE_RIGHT = "TwoPageRight";     // Odd-numbered pages on the right
}
}   // End of namespace PDFjet.NET
