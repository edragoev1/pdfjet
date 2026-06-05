/**
 * Legal.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

/**
 * Used to specify PDF page with size <strong>Legal</strong>.
 * For more information about the page size classes - A3, A4, A5, B5, Executive, Letter, Legal and Tabloid - see the Page class.
 */
namespace PDFjet.NET {
public class Legal {
    public static readonly float[] PORTRAIT = new float[] {612.0f, 1008.0f};
    public static readonly float[] LANDSCAPE = new float[] {1008.0f, 612.0f};
}
}   // End of namespace PDFjet.NET
