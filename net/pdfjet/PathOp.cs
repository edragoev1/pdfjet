/*
 * PathOp.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>A single path operation: a command and its points.</summary>
internal class PathOp {
    /// <summary>The path command, for example 'M' or 'C'.</summary>
    public char cmd;

    /// <summary>The x coordinate of the original quadratic control point.</summary>
    public float x1q;   // Original quadratic control
    /// <summary>The y coordinate of the original quadratic control point.</summary>
    public float y1q;   // point coordinates

    /// <summary>The x coordinate of the first control point.</summary>
    public float x1;    // Control point x1
    /// <summary>The y coordinate of the first control point.</summary>
    public float y1;    // Control point y1
    /// <summary>The x coordinate of the second control point.</summary>
    public float x2;    // Control point x2
    /// <summary>The y coordinate of the second control point.</summary>
    public float y2;    // Control point y2
    /// <summary>The x coordinate of the point.</summary>
    public float x;     // Initial point x
    /// <summary>The y coordinate of the point.</summary>
    public float y;     // Initial point y
    /// <summary>The arguments of the command.</summary>
    public List<String> args;

    /// <summary>Creates a path operation with the specified command.</summary>
    public PathOp(char cmd) {
        this.cmd = cmd;
        this.args = new List<String>();
    }

    /// <summary>Creates a path operation with the specified command and point.</summary>
    public PathOp(char cmd, float x, float y) {
        this.cmd = cmd;
        this.x = x;
        this.y = y;
        this.args = new List<String>();
    }

    /// <summary>Sets the two control points and the end point of a cubic curve.</summary>
    public PathOp SetCubicPoints(
            float x1, float y1,
            float x2, float y2,
            float x, float y) {
        this.x1 = x1;
        this.y1 = y1;
        this.x2 = x2;
        this.y2 = y2;
        this.x = x;
        this.y = y;
        return this;
    }
}
}
