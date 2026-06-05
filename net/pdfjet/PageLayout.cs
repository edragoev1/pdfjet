/**
 * PageLayout.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

/**
 * Used to specify the PDF page layout.
 */
namespace PDFjet.NET {
public class PageLayout {
    public const String SINGLE_PAGE = "SinglePage";          // Display one page at a time
    public const String ONE_COLUMN = "OneColumn";            // Display the pages in one column
    public const String TWO_COLUMN_LEFT = "TwoColumnLeft";   // Odd-numbered pages on the left
    public const String TWO_COLUMN_RIGTH = "TwoColumnRight"; // Odd-numbered pages on the right
    public const String TWO_PAGE_LEFT = "TwoPageLeft";       // Odd-numbered pages on the left
    public const String TWO_PAGE_RIGTH = "TwoPageRight";     // Odd-numbered pages on the right
}
}   // End of namespace PDFjet.NET
