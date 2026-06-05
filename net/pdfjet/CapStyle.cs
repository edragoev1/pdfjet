/**
 * CapStyle.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

/**
 * Used to specify the cap style of a line.
 * See the Line class for more information.
 */
namespace PDFjet.NET {
public enum CapStyle : Int32 {
    BUTT = 0,
    ROUND,
    PROJECTING_SQUARE
}
}   // End of namespace PDFjet.NET
