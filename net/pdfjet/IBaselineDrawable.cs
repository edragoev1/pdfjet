/*
 * IBaselineDrawable.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

namespace PDFjet.NET {
/// <summary>
/// A drawable whose location is the baseline of its text, not the top left
/// corner of a box: a line of text, such as TextLine and CompositeTextLine.
///
/// A Cell draws such a drawable where it draws its own text, on the baseline
/// that its vertical alignment asks for, and makes room for the ascent and the
/// descent below. A drawable that is not one of these is placed by its top
/// left corner, at the padding of the cell.
/// </summary>
public interface IBaselineDrawable : IDrawable {
    /// <summary>Returns how far above its baseline this drawable reaches, in points.</summary>
    float GetAscent();
    /// <summary>Returns how far below its baseline this drawable reaches, in points.</summary>
    float GetDescent();
    /// <summary>Returns the width of this drawable, in points.</summary>
    float GetWidth();
}
}   // End of namespace PDFjet.NET
