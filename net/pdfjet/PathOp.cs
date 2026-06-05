/**
 * PathOp.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
public class PathOp {
    public char cmd;

    public float x1q;   // Original quadratic control
    public float y1q;   // point coordinates

    public float x1;    // Control point x1
    public float y1;    // Control point y1
    public float x2;    // Control point x2
    public float y2;    // Control point y2
    public float x;     // Initial point x
    public float y;     // Initial point y
    public List<String> args;

    public PathOp(char cmd) {
        this.cmd = cmd;
        this.args = new List<String>();
    }

    public PathOp(char cmd, float x, float y) {
        this.cmd = cmd;
        this.x = x;
        this.y = y;
        this.args = new List<String>();
    }

    public void SetCubicPoints(
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
}
