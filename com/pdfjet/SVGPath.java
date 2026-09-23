/*
 * SVGPath.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.List;

/**
 * A path or a shape of an SVG file, as PDF path operations in the space of
 * the svg element, and what it is drawn with.
 */
class SVGPath {
    /** The default constructor */
    SVGPath() {
    }

    List<PathOp> operations;        // The PDF path operations
    int fill = Color.transparent;   // The fill color, or transparent for none
    int stroke = Color.transparent; // The stroke color, or transparent for none
    float strokeWidth = 0f;         // The stroke width, in the space of the svg element
    boolean evenOdd = false;        // Filled with the even-odd rule
    CapStyle lineCap = CapStyle.BUTT;
    JoinStyle lineJoin = JoinStyle.MITER;
    float fillAlpha = 1f;
    float strokeAlpha = 1f;
}
