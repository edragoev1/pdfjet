// DonutChart.cs
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.Linq;

namespace PDFjet.NET {
    /// <summary>
    /// A donut or pie chart: each slice is its value's share of the sum of the
    /// values, with a label next to it and its percentage inside it. A chart with
    /// an inner radius of 0 is a pie chart. See Example_25.
    /// </summary>
    public class DonutChart : IDrawable {
        private readonly Font f1;
        private readonly Font f2;
        private float x;
        private float y;
        private float r1;
        private float r2;
        private readonly List<Slice> slices;
        private String altDescription = null;

        /// <summary>
        /// Creates a donut chart. With an inner radius of 0 it is a pie chart.
        /// </summary>
        /// <param name="f1">the font for the slice labels.</param>
        /// <param name="f2">the font for the percentages drawn inside the slices.</param>
        public DonutChart(Font f1, Font f2) {
            this.f1 = f1;
            this.f2 = f2;
            this.slices = new List<Slice>();
        }

        /// <summary>
        /// Sets the top left corner of the outer circle of this chart. The center
        /// is one outer radius to the right of it and one below. The slice labels
        /// can extend past the circle.
        /// </summary>
        /// <param name="x">the x coordinate of the top left corner.</param>
        /// <param name="y">the y coordinate of the top left corner.</param>
        public DonutChart SetLocation(float x, float y) {
            this.x = x;
            this.y = y;
            return this;
        }

        IDrawable IDrawable.SetLocation(float x, float y) {
            return SetLocation(x, y);
        }

        /// <summary>Sets the outer and inner radius of this chart; an inner radius of 0 makes a pie chart.</summary>
        public DonutChart SetRadii(float outerRadius, float innerRadius) {
            this.r1 = outerRadius;
            this.r2 = innerRadius;
            return this;
        }

        /// <summary>Adds a slice to this chart.</summary>
        public DonutChart AddSlice(Slice slice) {
            slices.Add(slice);
            return this;
        }

        /// <summary>
        /// Sets the alternate description of the chart, which a screen reader reads
        /// in a PDF/UA document, where the chart is a figure. The default lists the
        /// label and the percentage of each slice.
        /// </summary>
        /// <param name="altDescription">the alternate description.</param>
        /// <returns>this DonutChart object.</returns>
        public DonutChart SetAltDescription(String altDescription) {
            this.altDescription = altDescription;
            return this;
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
            float cross = ax * by - ay * bx;
            // An arc of radius zero, the center of a pie chart, or of no angle has
            // its control points at its ends; the formula would divide 0 by 0.
            float k2 = (cross == 0f) ? 0f : (4.0f / 3.0f * ((float)Math.Sqrt(2.0 * q1 * q2) - q2)) / cross;

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
                page.DrawString(f1, f1.GetSize(), text,
                        onRightSide ? x2 + 2.0f : xEnd + 2.0f, yEnd - f1.GetAscent() / 3.0f,
                        Util.ToRGB(Color.black), null);
            } else {
                // No text — short horizontal stub
                bool onRightSide = (float)Math.Cos(midAngle * Math.PI / 180.0) >= 0;
                float xEnd = onRightSide ? x2 + 20.0f : x2 - 20.0f;
                page.LineTo(xEnd, y2);
                page.StrokePath();
            }
        }

        /// <summary>Draws this chart on the specified page.</summary>
        /// <param name="page">the page to draw on.</param>
        /// <returns>x and y coordinates of the bottom right corner of the outer circle
        /// of this chart. The slice labels can extend past it.</returns>
        public float[] DrawOn(Page page) {
            float xc = x + r1;      // the center of the chart
            float yc = y + r1;

            // The slices with a value above 0 share the circle
            float total = 0.0f;
            foreach (Slice slice in slices) {
                if (slice.value > 0.0f) {
                    total += slice.value;
                }
            }
            if (page == null || total <= 0.0f) {   // Measured, or nothing to draw
                return new float[] {xc + r1, yc + r1};
            }
            // The chart is one figure, described by its alternate description.
            page.AddBDC(StructElem.FIGURE, null, AltDescription(total));
            // The pen, the brush and the dash pattern of the caller are
            // kept, as a Stamp and a CalendarMonth keep them: the chart left
            // the page with the black pen of its pointers and the color of
            // its last slice.
            page.SaveGraphicsState();
            float angle = 0.0f;
            foreach (Slice slice in slices) {
                if (slice.value <= 0.0f) {
                    continue;
                }
                float sweep = slice.value * 360.0f / total;
                angle = DrawSlice(
                    page, slice.color,
                    xc, yc,
                    r1, r2,
                    angle, angle + sweep);
                DrawLinePointer(
                    page, slice.text,
                    xc, yc,
                    r1,
                    angle - sweep, angle);

                // The percentage fits inside a slice of 15 degrees or more
                if (f2 != null && sweep >= 15.0f) {
                    string pctStr = Percentage(slice, total);
                    float midAngle = angle - sweep / 2.0f - 90.0f;
                    float midR = (r1 + r2) / 2.0f;
                    var (posX, posY) = GetPoint(xc, yc, midR, midAngle);
                    page.DrawString(f2, f2.GetSize(), pctStr,
                        posX - f2.StringWidth(pctStr) / 2.0f,
                        posY + f2.GetAscent() / 3.0f,
                        Util.ToRGB(Color.white), null);
                }
            }
            page.RestoreGraphicsState();
            page.AddEMC();
            return new float[] {xc + r1, yc + r1};
        }

        // Returns the share of the slice in the total, as a whole percentage.
        private static String Percentage(Slice slice, float total) {
            return (int) Math.Round(slice.value * 100.0f / total, MidpointRounding.AwayFromZero) + "%";
        }

        // Returns the alternate description, or the label and the percentage of
        // each slice when none is set.
        private String AltDescription(float total) {
            if (!String.IsNullOrEmpty(altDescription)) {
                return altDescription;
            }
            System.Text.StringBuilder sb = new System.Text.StringBuilder(r2 > 0.0f ? "Donut chart:" : "Pie chart:");
            String separator = " ";
            foreach (Slice slice in slices) {
                if (slice.value > 0.0f) {
                    sb.Append(separator);
                    if (slice.text.Length > 0) {
                        sb.Append(slice.text).Append(' ');
                    }
                    sb.Append(Percentage(slice, total));
                    separator = ", ";
                }
            }
            return sb.ToString();
        }

        // Utility: append points from a control-point block into a list
        private static void AppendPoints(List<(float x, float y)> list, float[,] pts) {
            for (int i = 0; i < pts.GetLength(0); i++) {
                list.Add((pts[i, 0], pts[i, 1]));
            }
        }
    }
}
