/*
 * JISB5.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// Used to specify PDF page with size <strong>JIS B5</strong>, the Japanese B5 of 182 by 257 mm.
/// For more information about the page size classes - A3, A4, A5, JISB5, Executive, Letter, Legal and Tabloid - see the Page class.
/// </summary>
public class JISB5 {
    /// <summary>The JISB5 page size in portrait orientation.</summary>
    public static readonly PageSize PORTRAIT = new PageSize(516.0f, 729.0f);
    /// <summary>The JISB5 page size in landscape orientation.</summary>
    public static readonly PageSize LANDSCAPE = new PageSize(729.0f, 516.0f);
}
}   // End of namespace PDFjet.NET
