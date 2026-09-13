/*
 * Letter.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// Used to specify PDF page with size <strong>Letter</strong>.
/// For more information about the page size classes - A3, A4, A5, B5, JISB5, Executive, Letter, Legal and Tabloid - see the Page class.
/// </summary>
public class Letter {
    /// <summary>The letter page size in portrait orientation.</summary>
    public static readonly PageSize PORTRAIT = new PageSize(612.0f, 792.0f);
    /// <summary>The letter page size in landscape orientation.</summary>
    public static readonly PageSize LANDSCAPE = new PageSize(792.0f, 612.0f);
}
}   // End of namespace PDFjet.NET
