/*
 * IDrawable.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

namespace PDFjet.NET {
/// <summary>
/// Interface that is required for components that can be drawn on a PDF page as part of Optional Content Group.
/// </summary>
/// <remarks>Author: Mark Paxton</remarks>
public interface IDrawable {
    /// <summary>
    /// Draw the component implementing this interface on the PDF page.
    /// </summary>
    /// <param name="canvas">the page to draw on.</param>
    /// <returns>x and y coordinates of the bottom right corner of this component.</returns>
    float[] DrawOn(Page canvas);
    /// <summary>Sets the location of this drawable on the page.</summary>
    IDrawable SetLocation(float x, float y);
}
}   // End of namespace PDFjet.NET
