package com.pdfjet;

import java.util.List;

/**
 * The SVGPath class.
 */
class SVGPath {
    /** The default constructor */
    SVGPath() {
    }

    String data;                    // The SVG path data
    List<PathOp> operations;        // The PDF path operations
    int fill = Color.transparent;   // The fill color or transparent don't fill
    int stroke = Color.transparent; // The stroke color or transparent don't stroke
    boolean fillNone = false;       // fill="none": not filled, whatever the svg element says
    boolean strokeNone = false;     // stroke="none": not stroked, whatever the svg element says
    float strokeWidth = 0f;         // The stroke width
}
