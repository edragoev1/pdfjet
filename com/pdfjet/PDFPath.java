package com.pdfjet;

import java.util.*;

public class PDFPath {
    List<PathOp> data;      // The path operations
    int fill = Color.black; // The fill color or -1 (don't fill)
    int stroke = -1;        // The stroke color or -1 (don't stroke)
    float strokeWidth;      // The stroke width

    public PDFPath() {
    }
}
