/*
 * Executive.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// Used to specify PDF page with size <strong>Executive</strong>.
/// For more information about the page size classes - A3, A4, A5, B5, JISB5, Executive, Letter, Legal and Tabloid - see the Page class.
/// </summary>
public class Executive {
    /// <summary>The executive page size in portrait orientation.</summary>
    public static readonly PageSize PORTRAIT = new PageSize(522.0f, 756.0f);
    /// <summary>The executive page size in landscape orientation.</summary>
    public static readonly PageSize LANDSCAPE = new PageSize(756.0f, 522.0f);
}
}   // End of namespace PDFjet.NET
