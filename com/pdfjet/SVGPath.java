package com.pdfjet;

import java.util.List;

public class SVGPath {
    String data;                    // The SVG path data
    List<PathOp> operations;        // The PDF path operations
    int fill = Color.transparent;   // The fill color or -1 (don't fill)
    int stroke = Color.transparent; // The stroke color or -1 (don't stroke)
    float strokeWidth = 2f;         // The stroke width
}
