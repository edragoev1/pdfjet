/*
 * SVGPath.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// A path or a shape of an SVG file, as PDF path operations in the space of
/// the svg element, and what it is drawn with.
/// </summary>
internal class SVGPath {
    /// <summary>The PDF path operations.</summary>
    internal List<PathOp> operations;         // The PDF path operations
    /// <summary>The fill color, or Color.transparent for none.</summary>
    internal int fill = Color.transparent;
    /// <summary>The stroke color, or Color.transparent for none.</summary>
    internal int stroke = Color.transparent;
    /// <summary>The stroke width, in the space of the svg element.</summary>
    internal float strokeWidth = 0f;
    /// <summary>True to fill the path with the even-odd rule.</summary>
    internal bool evenOdd = false;
    /// <summary>The line cap style.</summary>
    internal CapStyle lineCap = CapStyle.BUTT;
    /// <summary>The line join style.</summary>
    internal JoinStyle lineJoin = JoinStyle.MITER;
    /// <summary>The alpha of the fill.</summary>
    internal float fillAlpha = 1f;
    /// <summary>The alpha of the stroke.</summary>
    internal float strokeAlpha = 1f;
}
}
