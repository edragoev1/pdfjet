/**
 * Border.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

/**
 * Used to control the visibility of cell borders.
 * See the Cell class for more information.
 */
namespace PDFjet.NET {
public class Border {
    public const uint NONE   = 0x00000000;
    public const uint TOP    = 0x00010000;
    public const uint BOTTOM = 0x00020000;
    public const uint LEFT   = 0x00040000;
    public const uint RIGHT  = 0x00080000;
    public const uint ALL    = 0x000F0000;
}
}   // End of namespace PDFjet.NET
