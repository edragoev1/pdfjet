/**
 *  SVG.cs
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
using System.Text;

namespace PDFjet.NET {
    public class SVG {
        private static bool isCommand(char ch) {
            // Capital letter commands use absolute coordinates
            // Small letter commands use relative coordinates
            switch (ch) {
                case 'M':  // moveto
                case 'm':  // moveto (lowercase)
                case 'L':  // lineto
                case 'l':  // lineto (lowercase)
                case 'H':  // horizontal lineto
                case 'h':  // horizontal lineto (lowercase)
                case 'V':  // vertical lineto
                case 'v':  // vertical lineto (lowercase)
                case 'Q':  // quadratic curveto
                case 'q':  // quadratic curveto (lowercase)
                case 'T':  // smooth quadratic curveto
                case 't':  // smooth quadratic curveto (lowercase)
                case 'C':  // cubic curveto
                case 'c':  // cubic curveto (lowercase)
                case 'S':  // smooth cubic curveto
                case 's':  // smooth cubic curveto (lowercase)
                case 'A':  // elliptical arc
                case 'a':  // elliptical arc (lowercase)
                case 'Z':  // close path
                case 'z':  // close path (lowercase)
                    return true;
                default:
                    return false;
            }
        }

        public static List<PathOp> GetOperations(String path) {
            List<PathOp> operations = new List<PathOp>();
            PathOp op = null;
            StringBuilder buf = new StringBuilder();
            bool token = false;
            for (int i = 0; i < path.Length; i++) {
                char ch = path[i];
                if (isCommand(ch)) {                    // open path
                    if (token) {
                        op.args.Add(buf.ToString());
                        buf.Length = 0;
                    }
                    token = false;
                    op = new PathOp(ch);
                    operations.Add(op);
                } else if (ch == ' ' || ch == ',') {
                    if (token) {
                        op.args.Add(buf.ToString());
                        buf.Length = 0;
                    }
                    token = false;
                } else if (ch == '-') {
                    if (token) {
                        op.args.Add(buf.ToString());
                        buf.Length = 0;
                    }
                    token = true;
                    buf.Append(ch);
                } else if (ch == '.') {
                    if (buf.ToString().Contains(".")) {
                        op.args.Add(buf.ToString());
                        buf.Length = 0;
                    }
                    token = true;
                    buf.Append(ch);
                } else {
                    token = true;
                    buf.Append(ch);
                }
            }
            return operations;
        }

        public static List<PathOp> ToPDF(List<PathOp> list) {
            List<PathOp> operations = new List<PathOp>();
            PathOp lastOp = null;
            PathOp pathOp = null;
            float x0 = 0f;  // Start of subpath
            float y0 = 0f;
            foreach (PathOp op in list) {
                if (op.cmd == 'M' || op.cmd == 'm') {
                    for (int i = 0; i <= op.args.Count - 2; i += 2) {
                        float x = float.Parse(op.args[i]);
                        float y = float.Parse(op.args[i + 1]);
                        if (op.cmd == 'm' && lastOp != null) {
                            x += lastOp.x;
                            y += lastOp.y;
                        }
                        if (i == 0) {
                            x0 = x;
                            y0 = y;
                            pathOp = new PathOp('M', x, y);
                        } else {
                            pathOp = new PathOp('L', x, y);
                        }
                        operations.Add(pathOp);
                        lastOp = pathOp;
                    }
                } else if (op.cmd == 'L' || op.cmd == 'l') {
                    for (int i = 0; i <= op.args.Count - 2; i += 2) {
                        float x = float.Parse(op.args[i]);
                        float y = float.Parse(op.args[i + 1]);
                        if (op.cmd == 'l' && lastOp != null) {
                            x += lastOp.x;
                            y += lastOp.y;
                        }
                        pathOp = new PathOp('L', x, y);
                        operations.Add(pathOp);
                        lastOp = pathOp;
                    }
                } else if (op.cmd == 'H' || op.cmd == 'h') {
                    for (int i = 0; i < op.args.Count; i++) {
                        float x = float.Parse(op.args[i]);
                        if (op.cmd == 'h' && lastOp != null) {
                            x += lastOp.x;
                        }
                        pathOp = new PathOp('L', x, lastOp.y);
                        operations.Add(pathOp);
                        lastOp = pathOp;
                    }
                } else if (op.cmd == 'V' || op.cmd == 'v') {
                    for (int i = 0; i < op.args.Count; i++) {
                        float y = float.Parse(op.args[i]);
                        if (op.cmd == 'v' && lastOp != null) {
                            y += lastOp.y;
                        }
                        pathOp = new PathOp('L', lastOp.x, y);
                        operations.Add(pathOp);
                        lastOp = pathOp;
                    }
                } else if (op.cmd == 'Q' || op.cmd == 'q') {
                    for (int i = 0; i <= op.args.Count - 4; i += 4) {
                        pathOp = new PathOp('C');
                        float x1 = float.Parse(op.args[i]);
                        float y1 = float.Parse(op.args[i + 1]);
                        float x = float.Parse(op.args[i + 2]);
                        float y = float.Parse(op.args[i + 3]);
                        if (op.cmd == 'q') {
                            x1 += lastOp.x;
                            y1 += lastOp.y;
                            x += lastOp.x;
                            y += lastOp.y;
                        }
                        // Save the original control point
                        pathOp.x1q = x1;
                        pathOp.y1q = y1;
                        // Calculate the coordinates of the cubic control points
                        float x1c = lastOp.x + (2f / 3f) * (x1 - lastOp.x);
                        float y1c = lastOp.y + (2f / 3f) * (y1 - lastOp.y);
                        float x2c = x + (2f / 3f) * (x1 - x);
                        float y2c = y + (2f / 3f) * (y1 - y);
                        pathOp.SetCubicPoints(x1c, y1c, x2c, y2c, x, y);
                        operations.Add(pathOp);
                        lastOp = pathOp;
                    }
                } else if (op.cmd == 'T' || op.cmd == 't') {
                    for (int i = 0; i <= op.args.Count - 2; i += 2) {
                        pathOp = new PathOp('C');
                        float x1 = lastOp.x;
                        float y1 = lastOp.y;
                        if (lastOp.cmd == 'C') {
                            // Find the reflection control point
                            x1 = 2 * lastOp.x - lastOp.x1q;
                            y1 = 2 * lastOp.y - lastOp.y1q;
                        }
                        float x = float.Parse(op.args[i]);
                        float y = float.Parse(op.args[i + 1]);
                        if (op.cmd == 't') {
                            x += lastOp.x;
                            y += lastOp.y;
                        }
                        // Calculate the coordinates of the cubic control points
                        float x1c = lastOp.x + (2f / 3f) * (x1 - lastOp.x);
                        float y1c = lastOp.y + (2f / 3f) * (y1 - lastOp.y);
                        float x2c = x + (2f / 3f) * (x1 - x);
                        float y2c = y + (2f / 3f) * (y1 - y);
                        pathOp.SetCubicPoints(x1c, y1c, x2c, y2c, x, y);
                        operations.Add(pathOp);
                        lastOp = pathOp;
                    }
                } else if (op.cmd == 'C' || op.cmd == 'c') {
                    for (int i = 0; i <= op.args.Count - 6; i += 6) {
                        pathOp = new PathOp('C');
                        float x1 = float.Parse(op.args[i]);
                        float y1 = float.Parse(op.args[i + 1]);
                        float x2 = float.Parse(op.args[i + 2]);
                        float y2 = float.Parse(op.args[i + 3]);
                        float x = float.Parse(op.args[i + 4]);
                        float y = float.Parse(op.args[i + 5]);
                        if (op.cmd == 'c') {
                            x1 += lastOp.x;
                            y1 += lastOp.y;
                            x2 += lastOp.x;
                            y2 += lastOp.y;
                            x += lastOp.x;
                            y += lastOp.y;
                        }
                        pathOp.SetCubicPoints(x1, y1, x2, y2, x, y);
                        operations.Add(pathOp);
                        lastOp = pathOp;
                    }
                } else if (op.cmd == 'S' || op.cmd == 's') {
                    for (int i = 0; i <= op.args.Count - 4; i += 4) {
                        pathOp = new PathOp('C');
                        float x1 = lastOp.x;
                        float y1 = lastOp.y;
                        if (lastOp.cmd == 'C') {
                            // Find the reflection control point
                            x1 = 2 * lastOp.x - lastOp.x2;
                            y1 = 2 * lastOp.y - lastOp.y2;
                        }
                        float x2 = float.Parse(op.args[i]);
                        float y2 = float.Parse(op.args[i + 1]);
                        float x = float.Parse(op.args[i + 2]);
                        float y = float.Parse(op.args[i + 3]);
                        if (op.cmd == 's') {
                            x2 += lastOp.x;
                            y2 += lastOp.y;
                            x += lastOp.x;
                            y += lastOp.y;
                        }
                        pathOp.SetCubicPoints(x1, y1, x2, y2, x, y);
                        operations.Add(pathOp);
                        lastOp = pathOp;
                    }
                } else if (op.cmd == 'A' || op.cmd == 'a') {
                    // TODO: Elliptical Arc
                } else if (op.cmd == 'Z' || op.cmd == 'z') {
                    pathOp = new PathOp('Z');
                    pathOp.x = x0;
                    pathOp.y = y0;
                    operations.Add(pathOp);
                    lastOp = pathOp;
                }
            }
            return operations;
        }
    }
}   // End of SVG.cs
