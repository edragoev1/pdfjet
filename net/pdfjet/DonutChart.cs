// DonutChart.cs
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.Linq;

namespace PDFjet.NET {
    /// <summary>
    /// Used to create Donut chart objects and draw them on a page.
    ///
    /// Please see Example_25.cs
    /// </summary>
    public class DonutChart {
        private readonly Font f1;
        private readonly Font f2;
        private float xc;
        private float yc;
        private float r1;
        private float r2;
        private readonly List<Slice> slices;
        private readonly bool isDonutChart;

        public DonutChart(Font f1, Font f2, bool isDonutChart) {
            this.f1 = f1;
            this.f2 = f2;
            this.isDonutChart = isDonutChart;
            this.slices = new List<Slice>();
        }

        public void SetLocation(float xc, float yc) {
            this.xc = xc;
            this.yc = yc;
        }

        public void SetR1AndR2(float r1, float r2) {
            this.r1 = r1;
            this.r2 = r2;
        }

        public void AddSlice(Slice slice) {
            slices.Add(slice);
        }

        private static float[,] GetControlPoints(
            float xc, float yc,
            float x0, float y0,
            float x3, float y3) {
            float ax = x0 - xc;
            float ay = y0 - yc;
            float bx = x3 - xc;
            float by = y3 - yc;
            float q1 = ax * ax + ay * ay;
            float q2 = q1 + ax * bx + ay * by;
            float k2 = (4.0f / 3.0f * ((float)Math.Sqrt(2.0 * q1 * q2) - q2)) / (ax * by - ay * bx);

            // Control points coordinates
            float x1 = xc + ax - k2 * ay;
            float y1 = yc + ay + k2 * ax;
            float x2 = xc + bx + k2 * by;
            float y2 = yc + by - k2 * bx;

            // Order: p0, cp1, cp2, p3 (same as the Java version)
            return new float[,]
            {
                { x0, y0 },
                { x1, y1 },
                { x2, y2 },
                { x3, y3 }
            };
        }

        private static (float x, float y) GetPoint(float xc, float yc, float radius, float angle) {
            float x = xc + radius * (float)Math.Cos(angle * Math.PI / 180.0);
            float y = yc + radius * (float)Math.Sin(angle * Math.PI / 180.0);
            return (x, y);
        }

        private float DrawSlice(
                Page page,
                int fillColor,
                float xc, float yc,
                float r1, float r2,         // r1 > r2
                float a1, float a2) {       // a1 > a2
            page.SetBrushColor(fillColor);

            float angle1 = a1 - 90.0f;
            float angle2 = a2 - 90.0f;

            List<(float x, float y)> points1 = new List<(float, float)>();
            List<(float x, float y)> points2 = new List<(float, float)>();
            while (true) {
                if (angle2 - angle1 <= 90.0f) {
                    var (px0, py0) = GetPoint(xc, yc, r1, angle1);   // Start point
                    var (px3, py3) = GetPoint(xc, yc, r1, angle2);   // End point
                    AppendPoints(points1, GetControlPoints(xc, yc, px0, py0, px3, py3));
                    (px0, py0) = GetPoint(xc, yc, r2, angle1);       // Start point
                    (px3, py3) = GetPoint(xc, yc, r2, angle2);      // End point
                    AppendPoints(points2, GetControlPoints(xc, yc, px0, py0, px3, py3));
                    break;
                } else {
                    var (px0, py0) = GetPoint(xc, yc, r1, angle1);
                    var (px3, py3) = GetPoint(xc, yc, r1, angle1 + 90.0f);
                    AppendPoints(points1, GetControlPoints(xc, yc, px0, py0, px3, py3));
                    (px0, py0) = GetPoint(xc, yc, r2, angle1);
                    (px3, py3) = GetPoint(xc, yc, r2, angle1 + 90.0f);
                    AppendPoints(points2, GetControlPoints(xc, yc, px0, py0, px3, py3));
                    angle1 += 90.0f;
                }
            }
            points2.Reverse();

            page.MoveTo(points1[0].x, points1[0].y);
            int i = 0;
            while (i <= points1.Count - 4) {
                page.CurveTo(
                    points1[i + 1].x, points1[i + 1].y,
                    points1[i + 2].x, points1[i + 2].y,
                    points1[i + 3].x, points1[i + 3].y);
                i += 4;
            }
            page.LineTo(points2[0].x, points2[0].y);
            i = 0;
            while (i <= points2.Count - 4) {
                page.CurveTo(
                    points2[i + 1].x, points2[i + 1].y,
                    points2[i + 2].x, points2[i + 2].y,
                    points2[i + 3].x, points2[i + 3].y);
                i += 4;
            }
            page.FillPath();

            return a2;
        }

        private void DrawLinePointer(
                Page page,
                string text,
                float xc, float yc,
                float r1,
                float a1, float a2) {
            float midAngle = (a1 + a2) / 2.0f - 90.0f;

            // Point on the outer edge of the donut
            var (x1, y1) = GetPoint(xc, yc, r1, midAngle);

            // Elbow point — 15pt beyond the outer edge
            float r3 = r1 + 15.0f;
            var (x2, y2) = GetPoint(xc, yc, r3, midAngle);

            // Draw the pointer line: edge → elbow → horizontal end
            page.SetPenColor(Color.black);
            page.SetPenWidth(1.0f);
            page.MoveTo(x1, y1);
            page.LineTo(x2, y2);

            if (f1 != null && !string.IsNullOrEmpty(text)) {
                float textWidth = f1.StringWidth(text);
                bool onRightSide = (float)Math.Cos(midAngle * Math.PI / 180.0) >= 0;

                float padding = 4.0f;
                float lineLength = textWidth + padding;

                float xEnd = onRightSide ? x2 + lineLength : x2 - lineLength;
                float yEnd = y2;

                // Continue the path to the horizontal end
                page.LineTo(xEnd, yEnd);
                page.StrokePath();

                // Draw the label text just above the horizontal line
                TextLine label = new TextLine(f1, text);
                label.SetTextColor(Color.black);
                if (onRightSide) {
                    label.SetLocation(x2 + 2.0f, yEnd - f1.GetAscent() / 3.0f);
                } else {
                    label.SetLocation(xEnd + 2.0f, yEnd - f1.GetAscent() / 3.0f);
                }
                label.DrawOn(page);
            } else {
                // No text — short horizontal stub
                bool onRightSide = (float)Math.Cos(midAngle * Math.PI / 180.0) >= 0;
                float xEnd = onRightSide ? x2 + 20.0f : x2 - 20.0f;
                page.LineTo(xEnd, y2);
                page.StrokePath();
            }
        }

        public void DrawOn(Page page) {
            if (slices == null || slices.Count == 0) {
                return;
            }
            float innerR = isDonutChart ? r2 : 0.0f;
            float angle = 0.0f;
            foreach (Slice slice in slices) {
                angle = DrawSlice(
                    page, slice.color,
                    xc, yc,
                    r1, innerR,
                    angle, angle + slice.angle);
                DrawLinePointer(
                    page, slice.text,
                    xc, yc,
                    r1,
                    angle - slice.angle, angle);

                // Percent label inside the slice
                if (f2 != null && slice.angle >= 15.0f) {
                    int pct = (int)(slice.angle / 360.0f * 100.0f);
                    string pctStr = pct + "%";
                    TextLine label = new TextLine(f2, pctStr);
                    label.SetTextColor(Color.white);
                    float midAngle = angle - slice.angle / 2.0f - 90.0f;
                    float midR = (r1 + innerR) / 2.0f;
                    var (posX, posY) = GetPoint(xc, yc, midR, midAngle);
                    label.SetLocation(
                        posX - f2.StringWidth(pctStr) / 2.0f,
                        posY + f2.GetAscent() / 3.0f);
                    label.DrawOn(page);
                }
            }
        }

        // Utility: append points from a control-point block into a list
        private static void AppendPoints(List<(float x, float y)> list, float[,] pts) {
            for (int i = 0; i < pts.GetLength(0); i++) {
                list.Add((pts[i, 0], pts[i, 1]));
            }
        }
    }
}
