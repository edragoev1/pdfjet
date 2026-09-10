/*
 * Border.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// Used to control the visibility of cell borders.
/// See the Cell class for more information.
/// </summary>
public class Border {
    /// <summary>No border.</summary>
    public const uint NONE   = 0x00000000;
    /// <summary>The top border.</summary>
    public const uint TOP    = 0x00010000;
    /// <summary>The bottom border.</summary>
    public const uint BOTTOM = 0x00020000;
    /// <summary>The left border.</summary>
    public const uint LEFT   = 0x00040000;
    /// <summary>The right border.</summary>
    public const uint RIGHT  = 0x00080000;
    /// <summary>All four borders.</summary>
    public const uint ALL    = 0x000F0000;
}
}   // End of namespace PDFjet.NET
