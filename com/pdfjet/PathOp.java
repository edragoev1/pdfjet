/**
 * PathOp.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

class PathOp {
    char cmd;

    float x1q;  // Original quadratic control
    float y1q;  // point coordinates

    float x1;   // Control point x1
    float y1;   // Control point y1
    float x2;   // Control point x2
    float y2;   // Control point y2
    float x;    // Initial point x
    float y;    // Initial point y
    List<String> args;

    PathOp(char cmd) {
        this.cmd = cmd;
        this.args = new ArrayList<String>();
    }

    PathOp(char cmd, float x, float y) {
        this.cmd = cmd;
        this.x = x;
        this.y = y;
        this.args = new ArrayList<String>();
    }

    void setCubicPoints(
            float x1, float y1,
            float x2, float y2,
            float x, float y) {
        this.x1 = x1;
        this.y1 = y1;
        this.x2 = x2;
        this.y2 = y2;
        this.x = x;
        this.y = y;
    }
}
