/**
 * Align.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/**
 * Used to specify the text alignment in paragraphs.
 * See the Paragraph class for more details.
 */
public class Align {
    public const uint LEFT    = 0x00000000;
    public const uint CENTER  = 0x00100000;
    public const uint RIGHT   = 0x00200000;
    public const uint JUSTIFY = 0x00300000;

    public const uint TOP     = 0x00400000;
    public const uint BOTTOM  = 0x00500000;
}
}   // End of namespace PDFjet.NET
