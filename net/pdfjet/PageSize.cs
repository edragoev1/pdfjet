/*
 * PageSize.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
namespace PDFjet.NET {
/// <summary>
/// The width and height of a page in points; 1 point is 1/72 inch.
/// A page size cannot be changed once it is created, so the PORTRAIT and LANDSCAPE
/// constants of A3, A4, A5, B5, JISB5, Executive, Legal, Letter and Tabloid are the same
/// for every page made with them. Use the constructor for any other page size.
/// </summary>
public sealed class PageSize {
    private readonly float width;
    private readonly float height;

    /// <summary>Creates a page size.</summary>
    /// <param name="width">the width of the page in points.</param>
    /// <param name="height">the height of the page in points.</param>
    public PageSize(float width, float height) {
        this.width = width;
        this.height = height;
    }

    /// <summary>Returns the width of the page in points.</summary>
    public float GetWidth() {
        return width;
    }

    /// <summary>Returns the height of the page in points.</summary>
    public float GetHeight() {
        return height;
    }
}
}   // End of namespace PDFjet.NET
