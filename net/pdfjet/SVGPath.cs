/*
 * SVGPath.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>One SVG path with its PDF path operations, colors and stroke width.</summary>
public class SVGPath {
    /// <summary>The SVG path data.</summary>
    public String data;                     // The SVG path data
    /// <summary>The PDF path operations.</summary>
    public List<PathOp> operations;         // The PDF path operations
    /// <summary>The fill color, or Color.transparent to not fill the path.</summary>
    public int fill = Color.transparent;    // The fill color or -1 (don't fill)
    /// <summary>The stroke color, or Color.transparent to not stroke the path.</summary>
    public int stroke = Color.transparent;  // The stroke color or -1 (don't stroke)
    /// <summary>The stroke width.</summary>
    public float strokeWidth = 0f;          // The stroke width
}
}
