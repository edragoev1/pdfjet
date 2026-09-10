/*
 * CapStyle.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// Used to specify the cap style of a line.
/// See the Line class for more information.
/// </summary>
public enum CapStyle : Int32 {
    /// <summary>The line ends squarely at the end point.</summary>
    BUTT = 0,
    /// <summary>The line ends with a semicircle.</summary>
    ROUND,
    /// <summary>The line ends squarely, half the line width past the end point.</summary>
    PROJECTING_SQUARE
}
}   // End of namespace PDFjet.NET
