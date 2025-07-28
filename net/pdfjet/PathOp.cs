/**
 *  PathOp.cs
 *
©2025 PDFjet Software

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
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
