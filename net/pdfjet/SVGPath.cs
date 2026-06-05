/**
 * SVGPath.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
public class SVGPath {
    public String data;                     // The SVG path data
    public List<PathOp> operations;         // The PDF path operations
    public int fill = Color.transparent;    // The fill color or -1 (don't fill)
    public int stroke = Color.transparent;  // The stroke color or -1 (don't stroke)
    public float strokeWidth = 0f;          // The stroke width
}
}
