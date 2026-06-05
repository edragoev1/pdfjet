/**
 * IDrawable.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

/**
 * Interface that is required for components that can be drawn on a PDF page as part of Optional Content Group.
 *
 * @author Mark Paxton
 */
namespace PDFjet.NET {
public interface IDrawable {
    /**
     * Draw the component implementing this interface on the PDF page.
     *
     * @param page the page to draw on.
     * @return x and y coordinates of the bottom right corner of this component.
     * @throws Exception
     */
    float[] DrawOn(Page canvas);
    void SetPosition(float x, float y);
}
}   // End of namespace PDFjet.NET
