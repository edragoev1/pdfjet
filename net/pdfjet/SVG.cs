/*
 * SVG.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace PDFjet.NET {
    /// <summary>Converts SVG path data to PDF path operations.</summary>
    internal class SVG {
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

        /// <summary>Parses SVG path data into a list of path operations.</summary>
        internal static List<PathOp> GetOperations(String path) {
            List<PathOp> operations = new List<PathOp>();
            PathOp op = null;
            StringBuilder buf = new StringBuilder();
            bool token = false;
            foreach (char ch in path) {
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
            if (token) {    // The last number of a path that does not end with Z
                op.args.Add(buf.ToString());
            }
            return operations;
        }

        /// <summary>Converts SVG path operations to PDF path operations.</summary>
        internal static List<PathOp> ToPDF(List<PathOp> list) {
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
                    foreach (String arg in op.args) {
                        float x = float.Parse(arg);
                        if (op.cmd == 'h' && lastOp != null) {
                            x += lastOp.x;
                        }
                        pathOp = new PathOp('L', x, lastOp.y);
                        operations.Add(pathOp);
                        lastOp = pathOp;
                    }
                } else if (op.cmd == 'V' || op.cmd == 'v') {
                    foreach (String arg in op.args) {
                        float y = float.Parse(arg);
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
                    for (int i = 0; i <= op.args.Count - 7; i += 7) {
                        float rx = float.Parse(op.args[i]);
                        float ry = float.Parse(op.args[i + 1]);
                        float rotation = float.Parse(op.args[i + 2]);
                        bool largeArc = op.args[i + 3] != "0";
                        bool sweep = op.args[i + 4] != "0";
                        float x = float.Parse(op.args[i + 5]);
                        float y = float.Parse(op.args[i + 6]);
                        if (op.cmd == 'a') {
                            x += lastOp.x;
                            y += lastOp.y;
                        }
                        lastOp = AddArc(operations, lastOp, rx, ry, rotation, largeArc, sweep, x, y);
                    }
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

        /// <summary>
        /// Appends the cubic curves that draw the elliptical arc from the current
        /// point to (x, y), as SVG 1.1 section F.6.5 describes: the arc is split
        /// into pieces of at most a quarter turn, each approximated by one curve.
        /// </summary>
        /// <returns>the last operation appended, or lastOp when the arc is empty.</returns>
        private static PathOp AddArc(List<PathOp> operations, PathOp lastOp,
                float rx, float ry, float rotation, bool largeArc, bool sweep,
                float x, float y) {
            float x1 = lastOp.x;
            float y1 = lastOp.y;
            if (x1 == x && y1 == y) {
                return lastOp;
            }
            rx = Math.Abs(rx);
            ry = Math.Abs(ry);
            if (rx == 0f || ry == 0f) {
                PathOp line = new PathOp('L', x, y);
                operations.Add(line);
                return line;
            }
            double phi = rotation * Math.PI / 180.0;
            double cosPhi = Math.Cos(phi);
            double sinPhi = Math.Sin(phi);
            double dx = (x1 - x) / 2.0;
            double dy = (y1 - y) / 2.0;
            double x1p = cosPhi * dx + sinPhi * dy;
            double y1p = -sinPhi * dx + cosPhi * dy;
            double lambda = (x1p * x1p) / ((double) rx * rx) + (y1p * y1p) / ((double) ry * ry);
            if (lambda > 1.0) {
                rx *= (float) Math.Sqrt(lambda);
                ry *= (float) Math.Sqrt(lambda);
            }
            double rx2 = (double) rx * rx;
            double ry2 = (double) ry * ry;
            double num = rx2 * ry2 - rx2 * y1p * y1p - ry2 * x1p * x1p;
            double den = rx2 * y1p * y1p + ry2 * x1p * x1p;
            double coef = Math.Sqrt(Math.Max(0.0, num / den));
            if (largeArc == sweep) {
                coef = -coef;
            }
            double cxp = coef * rx * y1p / ry;
            double cyp = -coef * ry * x1p / rx;
            double cx = cosPhi * cxp - sinPhi * cyp + (x1 + x) / 2.0;
            double cy = sinPhi * cxp + cosPhi * cyp + (y1 + y) / 2.0;
            double ux = (x1p - cxp) / rx;
            double uy = (y1p - cyp) / ry;
            double vx = (-x1p - cxp) / rx;
            double vy = (-y1p - cyp) / ry;
            double theta = Math.Atan2(uy, ux);
            double delta = Math.Atan2(ux * vy - uy * vx, ux * vx + uy * vy);
            if (!sweep && delta > 0.0) {
                delta -= 2.0 * Math.PI;
            } else if (sweep && delta < 0.0) {
                delta += 2.0 * Math.PI;
            }
            int segments = (int) Math.Ceiling(Math.Abs(delta) / (Math.PI / 2.0));
            if (segments == 0) {
                return lastOp;
            }
            double step = delta / segments;
            double t = 4.0 / 3.0 * Math.Tan(step / 4.0);
            PathOp pathOp = lastOp;
            for (int i = 0; i < segments; i++) {
                double a1 = theta + i * step;
                double a2 = a1 + step;
                double p1x = Math.Cos(a1);
                double p1y = Math.Sin(a1);
                double p2x = Math.Cos(a2);
                double p2y = Math.Sin(a2);
                float[] c1 = OnEllipse(cx, cy, rx, ry, cosPhi, sinPhi, p1x - t * p1y, p1y + t * p1x);
                float[] c2 = OnEllipse(cx, cy, rx, ry, cosPhi, sinPhi, p2x + t * p2y, p2y - t * p2x);
                float[] p2 = (i == segments - 1) ?
                        new float[] {x, y} : OnEllipse(cx, cy, rx, ry, cosPhi, sinPhi, p2x, p2y);
                pathOp = new PathOp('C');
                pathOp.SetCubicPoints(c1[0], c1[1], c2[0], c2[1], p2[0], p2[1]);
                operations.Add(pathOp);
            }
            return pathOp;
        }

        // Maps a point of the unit circle to the ellipse with the center
        // (cx, cy), the radii rx and ry and the rotation with the given cosine and sine.
        private static float[] OnEllipse(double cx, double cy, double rx, double ry,
                double cosPhi, double sinPhi, double u, double v) {
            return new float[] {
                    (float) (cx + rx * u * cosPhi - ry * v * sinPhi),
                    (float) (cy + rx * u * sinPhi + ry * v * cosPhi)};
        }
    }
}   // End of SVG.cs
