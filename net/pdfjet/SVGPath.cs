/*
 * SVGPath.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
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
    /// <summary>True to fill the path, or to clip to it, with the even-odd rule.</summary>
    internal bool evenOdd = false;
    /// <summary>The line cap style.</summary>
    internal CapStyle lineCap = CapStyle.BUTT;
    /// <summary>The line join style.</summary>
    internal JoinStyle lineJoin = JoinStyle.MITER;
    /// <summary>The miter limit.</summary>
    internal float miterLimit = 4f;
    /// <summary>The alpha of the fill.</summary>
    internal float fillAlpha = 1f;
    /// <summary>The alpha of the stroke.</summary>
    internal float strokeAlpha = 1f;
    /// <summary>The dashes and the gaps, in the space of the svg element; null is solid.</summary>
    internal float[] dashArray = null;
    /// <summary>The dash offset, in the space of the svg element.</summary>
    internal float dashOffset = 0f;
    /// <summary>True to draw the stroke before the fill.</summary>
    internal bool strokeFirst = false;
    /// <summary>The clip paths it is drawn inside, all of them, or null.</summary>
    internal List<SVGPath> clips = null;
    /// <summary>The children of the switches it is in, or null.</summary>
    internal List<SVGImage.Choice> choices = null;
    /// <summary>A text, drawn instead of the operations, or null.</summary>
    internal SVGText text = null;
    /// <summary>Of a clip path that clips with texts: one for each, with its text, or null.</summary>
    internal List<SVGPath> parts = null;
    /// <summary>Of such a clip path: what is written for its texts, as operations.</summary>
    internal int glyphs = 0;

    /// <summary>True when it is in the children chosen of the switches it is in.</summary>
    internal bool IsChosen() {
        return AreChosen(choices);
    }

    // Returns true when each of the choices is the child chosen.
    internal static bool AreChosen(List<SVGImage.Choice> choices) {
        if (choices != null) {
            foreach (SVGImage.Choice choice in choices) {
                if (!choice.IsChosen()) {
                    return false;
                }
            }
        }
        return true;
    }
}
}
