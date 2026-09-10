/*
 * Align.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// Used to specify the text alignment in paragraphs.
/// See the Paragraph class for more details.
/// </summary>
public class Align {
    /// <summary>Aligns the text to the left.</summary>
    public const uint LEFT    = 0x00000000;
    /// <summary>Centers the text.</summary>
    public const uint CENTER  = 0x00100000;
    /// <summary>Aligns the text to the right.</summary>
    public const uint RIGHT   = 0x00200000;
    /// <summary>Justifies the text.</summary>
    public const uint JUSTIFY = 0x00300000;

    /// <summary>Aligns the text to the top.</summary>
    public const uint TOP     = 0x00400000;
    /// <summary>Aligns the text to the bottom.</summary>
    public const uint BOTTOM  = 0x00500000;
}
}   // End of namespace PDFjet.NET
