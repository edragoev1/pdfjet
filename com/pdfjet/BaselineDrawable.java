/*
 * BaselineDrawable.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * A drawable whose location is the baseline of its text, not the top left
 * corner of a box: a line of text, such as TextLine and CompositeTextLine.
 *
 * A Cell draws such a drawable where it draws its own text, on the baseline
 * that its vertical alignment asks for, and makes room for the ascent and the
 * descent below. A drawable that is not one of these is placed by its top left
 * corner, at the padding of the cell.
 */
public interface BaselineDrawable extends Drawable {
    /**
     * Returns how far above its baseline this drawable reaches.
     *
     * @return the ascent, in points.
     */
    public float getAscent();

    /**
     * Returns how far below its baseline this drawable reaches.
     *
     * @return the descent, in points.
     */
    public float getDescent();

    /**
     * Returns the width of this drawable.
     *
     * @return the width, in points.
     */
    public float getWidth();
}
