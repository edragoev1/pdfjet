/**
 * A3.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

/**
 * Used to specify PDF page with size <strong>A3</strong>.
 * For more information about the page size classes - A3, A4, A5, B5, Executive, Letter, Legal and Tabloid - see the Page class.
 */
namespace PDFjet.NET {
public class A3 {
    public static readonly float[] PORTRAIT = new float[] {842.0f, 1191.0f};
    public static readonly float[] LANDSCAPE = new float[] {1191.0f, 842.0f};
}
}   // End of namespace PDFjet.NET
