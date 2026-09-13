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
public enum PageLayout {
    /// <summary>Displays one page at a time.</summary>
    SINGLE_PAGE,
    /// <summary>Displays the pages in one column.</summary>
    ONE_COLUMN,
    /// <summary>Displays the pages in two columns, with odd-numbered pages on the left.</summary>
    TWO_COLUMN_LEFT,
    /// <summary>Displays the pages in two columns, with odd-numbered pages on the right.</summary>
    TWO_COLUMN_RIGHT,
    /// <summary>Displays two pages at a time, with odd-numbered pages on the left.</summary>
    TWO_PAGE_LEFT,
    /// <summary>Displays two pages at a time, with odd-numbered pages on the right.</summary>
    TWO_PAGE_RIGHT
}

internal static class PageLayoutExtensions {
    // The name written to the /PageLayout entry.
    internal static String ToPDFName(this PageLayout pageLayout) {
        switch (pageLayout) {
            case PageLayout.SINGLE_PAGE: return "SinglePage";
            case PageLayout.ONE_COLUMN: return "OneColumn";
            case PageLayout.TWO_COLUMN_LEFT: return "TwoColumnLeft";
            case PageLayout.TWO_COLUMN_RIGHT: return "TwoColumnRight";
            case PageLayout.TWO_PAGE_LEFT: return "TwoPageLeft";
            case PageLayout.TWO_PAGE_RIGHT: return "TwoPageRight";
            default: throw new ArgumentException("Invalid page layout: " + pageLayout);
        }
    }
}
}   // End of namespace PDFjet.NET
