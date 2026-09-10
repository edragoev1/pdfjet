/*
 * JoinStyle.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// Used to specify the join style when joining two lines.
/// See the Page and Line classes for more details.
/// </summary>
public enum JoinStyle : Int32 {
    /// <summary>Joins the lines with a sharp corner.</summary>
    MITER = 0,
    /// <summary>Joins the lines with a rounded corner.</summary>
    ROUND,
    /// <summary>Joins the lines with a beveled corner.</summary>
    BEVEL
}
}   // End of namespace PDFjet.NET
