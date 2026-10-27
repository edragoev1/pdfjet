// DonutChart.cs
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

using System;
using System.Globalization;
using System.Collections.Generic;

namespace PDFjet.NET {
    /// <summary>
    /// A donut or pie chart: each slice is its value's share of the sum of the
    /// values, with a label next to it and its percentage inside it. A chart with
    /// an inner radius of 0 is a pie chart. In the look of the other charts: a
    /// title and a subtitle at the top left, thin white lines between the slices,
    /// gray leader lines, the labels in ink, and each percentage white or ink by
    /// its slice's color. See Example_28.
    /// </summary>
    public class DonutChart : IDrawable {
        private readonly Font f1;      // The regular font: the labels, the subtitle and the legend
        private readonly Font f2;      // The bold font: the percentages, the title and the text in the hole
        private float x;
        private float y;
        private float r1;
        private float r2;
        private readonly List<Slice> slices;
        private String altDescription = null;
        private String title = "";
        private String subtitle = "";
        private String centerText = "";
        private bool legend = false;

        // The look of the simple charts
        private const int INK = SimpleBarChart.INK;
        private const int MUTED = SimpleBarChart.MUTED;
        private const int LEADER = 0x8C96A3;

        /// <summary>
        /// Creates a donut chart. With an inner radius of 0 it is a pie chart. Its
        /// fonts are in the order of the other charts': the bold one first.
        /// </summary>
        /// <param name="bold">the bold font: the percentages inside the slices, the
        /// title and the text in the hole.</param>
        /// <param name="regular">the regular font: the slice labels, the subtitle and
        /// the legend, and the size of the title.</param>
        public DonutChart(Font bold, Font regular) {
            this.f1 = regular;
            this.f2 = bold;
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
            // The larger is the outer radius: an inner radius past the outer one
            // drew a ring inside out
            this.r1 = Math.Max(Math.Abs(outerRadius), Math.Abs(innerRadius));
            this.r2 = Math.Min(Math.Abs(outerRadius), Math.Abs(innerRadius));
            return this;
        }

        /// <summary>Adds a slice to this chart.</summary>
        public DonutChart AddSlice(Slice slice) {
            if (slice != null) {
                slices.Add(slice);
            }
            return this;
        }

        /// <summary>
        /// Sets the title, drawn at the top left of the chart's location in the bold
        /// font at the size of the labels; the circle is drawn under it. A chart
        /// without the bold font, which draws no percentages, draws its title in the
        /// regular one. A chart without a title has the circle at its location.
        /// </summary>
        /// <param name="title">the title.</param>
        /// <returns>this DonutChart object.</returns>
        public DonutChart SetTitle(String title) {
            this.title = title ?? "";
            return this;
        }

        /// <summary>Sets the subtitle, drawn under the title in a muted gray.</summary>
        /// <param name="subtitle">the subtitle.</param>
        /// <returns>this DonutChart object.</returns>
        public DonutChart SetSubtitle(String subtitle) {
            this.subtitle = subtitle ?? "";
            return this;
        }

        /// <summary>
        /// Sets the text written in the hole of a donut chart, in the font of the
        /// title, as large as one and a half labels or as fits: the total, "100
        /// crates". A pie chart, with no hole, does not write it.
        /// </summary>
        /// <param name="centerText">the text.</param>
        /// <returns>this DonutChart object.</returns>
        public DonutChart SetCenterText(String centerText) {
            this.centerText = centerText ?? "";
            return this;
        }

        /// <summary>
        /// Draws a legend under the circle, a swatch, the label and the percentage
        /// of each slice, in rows as wide as the circle, in place of the leader
        /// lines and the labels beside the circle: for many slices, or small ones
        /// whose labels would crowd.
        /// </summary>
        /// <param name="legend">true to draw the legend.</param>
        /// <returns>this DonutChart object.</returns>
        public DonutChart SetLegend(bool legend) {
            this.legend = legend;
            return this;
        }

        /// <summary>
        /// Sets the alternate description of the chart, which a screen reader reads
        /// in a PDF/UA document, where the chart is a figure. The default is the
        /// title, the subtitle and the text in the hole, as sentences, and the
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
            // In single precision, the square root of a float, as Go computes
            // it, for the same chart in every port
            float k2 = (cross == 0f) ? 0f
                    : (4.0f / 3.0f * ((float) Math.Sqrt((float) (2f * q1 * q2)) - q2)) / cross;

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

            // No arc of angles that are not numbers, whose loop never ended
            if (!IsValue(a1) || !IsValue(a2)) {
                return a1;
            }
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

        // The leader line and the label of a slice: the point on the edge of the
        // circle, the elbow 15 points past it, the y of the label's line, moved
        // down from the elbow's when it would meet the label above it, and the
        // side of the circle it is on.
        private sealed class Pointer {
            internal readonly String text;
            internal readonly float x1;
            internal readonly float y1;
            internal readonly float x2;
            internal readonly float y2;
            internal readonly bool right;
            internal float xElbow;
            internal float yEnd;

            internal Pointer(String text, float x1, float y1, float x2, float y2, bool right) {
                this.text = text;
                this.x1 = x1;
                this.y1 = y1;
                this.x2 = x2;
                this.y2 = y2;
                this.right = right;
                this.xElbow = x2;
                this.yEnd = y2;
            }
        }

        // Returns the pointer of a slice from the angle a1 to a2.
        private Pointer PointerOf(String text, float xc, float yc, float r1, float a1, float a2) {
            float midAngle = (a1 + a2) / 2.0f - 90.0f;
            var (x1, y1) = GetPoint(xc, yc, r1, midAngle);              // On the outer edge
            var (x2, y2) = GetPoint(xc, yc, r1 + 15.0f, midAngle);      // The elbow
            // A label wider than the radius, or than six of its letters, is
            // shortened: a long one at the left ran off the page
            if (f1 != null) {
                text = ChartScale.Shorten(f1, f1.GetSize(), text, Math.Max(r1, 6f * f1.GetSize()));
            }
            return new Pointer(text, x1, y1, x2, y2, Math.Cos(midAngle * Math.PI / 180.0) >= 0);
        }

        // Moves the labels on each side of the circle apart, top to bottom, where
        // one would meet the one above it, and each moved label's elbow out to
        // where its height leaves the circle, 15 points past its edge: thin slices
        // side by side stacked their labels down a column across the ring.
        private void MoveApart(List<Pointer> pointers, float xc, float yc) {
            if (f1 == null) {
                return;
            }
            for (int side = 0; side < 2; side++) {
                List<Pointer> labels = new List<Pointer>();
                foreach (Pointer pointer in pointers) {
                    if (pointer.right == (side == 0) && pointer.text.Length > 0) {
                        labels.Add(pointer);
                    }
                }
                // Top to bottom, and labels at one height in the order of their
                // slices: List.Sort is not stable, the order is kept by the index
                labels.Sort((a, b) => a.yEnd != b.yEnd
                        ? a.yEnd.CompareTo(b.yEnd) : pointers.IndexOf(a).CompareTo(pointers.IndexOf(b)));
                for (int k = 1; k < labels.Count; k++) {
                    labels[k].yEnd = Math.Max(labels[k].yEnd, labels[k - 1].yEnd + f1.GetBodyHeight());
                }
                float r3 = r1 + 15.0f;
                foreach (Pointer label in labels) {
                    if (label.yEnd == label.y2) {
                        continue;   // Where it was
                    }
                    float dy = label.yEnd - yc;
                    float out_ = (Math.Abs(dy) < r3) ? (float) Math.Sqrt((double) r3 * r3 - (double) dy * dy) : 0f;
                    label.xElbow = label.right ? Math.Max(label.x2, xc + out_) : Math.Min(label.x2, xc - out_);
                }
            }
        }

        // Returns the box of the leader line and the label of the pointer: its
        // left, top, right and bottom; and draws them when there is a page.
        private float[] DrawPointer(Page page, Pointer pointer) {
            float x1 = pointer.x1;
            float y1 = pointer.y1;
            float x2 = pointer.xElbow;
            float yEnd = pointer.yEnd;
            if (page != null) {
                // The line: edge, elbow, and along to the end, thin and gray
                page.SetPenColor(LEADER);
                page.SetPenWidth(0.75f);
                page.MoveTo(x1, y1);
                page.LineTo(x2, yEnd);
            }
            if (f1 != null && pointer.text.Length > 0) {
                float textWidth = f1.StringWidth(pointer.text);
                float lineLength = textWidth + 4.0f;
                float xEnd = pointer.right ? x2 + lineLength : x2 - lineLength;
                // The label just above the line
                float x = pointer.right ? x2 + 2.0f : xEnd + 2.0f;
                float baseline = yEnd - f1.GetAscent() / 3.0f;
                if (page != null) {
                    page.LineTo(xEnd, yEnd);
                    page.StrokePath();
                    page.DrawString(f1, f1.GetSize(), pointer.text, x, baseline, Util.ToRGB(INK), null);
                }
                return new float[] {
                        Math.Min(Math.Min(x1, x2), Math.Min(xEnd, x)),
                        Math.Min(Math.Min(y1, yEnd), baseline - f1.GetAscent()),
                        Math.Max(Math.Max(x1, x2), Math.Max(xEnd, x + textWidth)),
                        Math.Max(Math.Max(y1, yEnd), baseline + f1.GetDescent())};
            }
            // No text: a short line along
            float end = pointer.right ? x2 + 20.0f : x2 - 20.0f;
            if (page != null) {
                page.LineTo(end, yEnd);
                page.StrokePath();
            }
            return new float[] {
                    Math.Min(Math.Min(x1, x2), end), Math.Min(y1, yEnd),
                    Math.Max(Math.Max(x1, x2), end), Math.Max(y1, yEnd)};
        }

        /// <summary>Draws this chart on the specified page.</summary>
        /// <param name="page">the page to draw on, or null to measure the chart.</param>
        /// <returns>x and y coordinates of the bottom right corner of the chart: of
        /// its title, its circle, and its leader lines and labels, which can reach
        /// past the circle, or its legend. A chart with no slice above 0 draws
        /// nothing, not its title either, and returns the corner of its
        /// circle.</returns>
        public float[] DrawOn(Page page) {
            // The title and the subtitle above the circle, when there are any
            Font titleFont = f2 ?? f1;
            float titleSize = (f1 != null) ? f1.GetSize() : (titleFont != null ? titleFont.GetSize() : 0f);
            float top = y;
            float titleBaseline = 0f;
            float subtitleBaseline = 0f;
            if (titleFont != null && title.Length > 0) {
                titleBaseline = top + titleFont.GetAscent(titleSize);
                top = titleBaseline + titleFont.GetDescent(titleSize);
            }
            if (f1 != null && subtitle.Length > 0) {
                subtitleBaseline = top + f1.GetAscent();
                top = subtitleBaseline + f1.GetDescent();
            }
            if (top > y) {
                // Room for a label at the top of the circle: its leader line
                // goes 15 points up, the label sits above its end; and half a line
                float label = (f1 != null && !legend) ? f1.GetAscent() / 3f + f1.GetAscent() : 0f;
                top += (legend ? 0f : 15f) + label + titleSize / 2f;
            }
            float xc = x + r1;      // the center of the chart
            float yc = top + r1;

            // The slices with a value above 0 share the circle
            float[] values = Values();
            float total = 0.0f;
            foreach (float value in values) {
                total += value;
            }
            if (total <= 0.0f) {   // Nothing to draw
                return new float[] {xc + r1, yc + r1};
            }
            int[] percentages = Percentages(values, total);

            // The leader lines and the labels, moved apart, and the box of the
            // figure: the title, the outer circle, the leader lines and the labels
            List<Pointer> pointers = new List<Pointer>();
            int[] colors = new int[slices.Count];
            float angle = 0.0f;
            int count = 0;
            for (int i = 0; i < slices.Count; i++) {
                if (values[i] > 0.0f) {
                    float sweep = values[i] * 360.0f / total;
                    if (!legend) {
                        pointers.Add(PointerOf(slices[i].text, xc, yc, r1, angle, angle + sweep));
                    }
                    angle += sweep;
                    int color = slices[i].color;
                    colors[i] = (color == Slice.PALETTE) ? BarChart.PALETTE[count % BarChart.PALETTE.Length] : color;
                    count++;
                }
            }
            // The last slice, next to the first around the circle, in the palette's
            // next color when it would have the first's: the ninth of nine was navy
            // by the navy first
            int first = -1;
            int last = -1;
            for (int i = 0; i < slices.Count; i++) {
                if (values[i] > 0.0f) {
                    first = (first < 0) ? i : first;
                    last = i;
                }
            }
            if (count > 1 && slices[last].color == Slice.PALETTE && colors[last] == colors[first]) {
                colors[last] = BarChart.PALETTE[count % BarChart.PALETTE.Length];
            }
            MoveApart(pointers, xc, yc);
            float[] box = {x, y, x + 2f * r1, yc + r1};
            if (titleBaseline > 0f) {
                box[2] = Math.Max(box[2], x + titleFont.StringWidth(titleSize, title));
            }
            if (subtitleBaseline > 0f) {
                box[2] = Math.Max(box[2], x + f1.StringWidth(subtitle));
            }
            foreach (Pointer pointer in pointers) {
                box = Union(box, DrawPointer(null, pointer));
            }
            if (legend) {
                box = Union(box, DrawLegend(null, values, percentages, colors, yc + r1));
            }
            if (page == null) {     // Measured
                return new float[] {box[2], box[3]};
            }

            // The chart is one figure, described by its alternate description.
            page.AddBDC(StructElem.FIGURE, null, AltDescription(values, percentages));
            // The pen, the brush and the dash pattern of the caller are
            // kept, as a Stamp keeps them: the chart left
            // the page with the black pen of its pointers and the color of
            // its last slice.
            page.SaveGraphicsState();
            page.SetDefaultStrokeDashPattern();     // Not the caller's, which dashed the leader lines
            if (titleBaseline > 0f) {
                page.DrawString(titleFont, titleSize, title, x, titleBaseline, Util.ToRGB(INK), null);
            }
            if (subtitleBaseline > 0f) {
                page.DrawString(f1, f1.GetSize(), subtitle, x, subtitleBaseline, Util.ToRGB(MUTED), null);
            }
            angle = 0.0f;
            int drawn = 0;
            List<float> edges = new List<float>();
            List<float> sweeps = new List<float>();
            for (int i = 0; i < slices.Count; i++) {
                if (values[i] <= 0.0f) {
                    continue;
                }
                int color = colors[i];
                float sweep = values[i] * 360.0f / total;
                angle = DrawSlice(
                    page, color,
                    xc, yc,
                    r1, r2,
                    angle, angle + sweep);
                edges.Add(angle);
                sweeps.Add(sweep);
                if (!legend) {
                    DrawPointer(page, pointers[drawn]);
                }
                drawn++;

                // The percentage fits inside a slice of 15 degrees or more
                if (f2 != null && sweep >= 15.0f) {
                    string pctStr = percentages[i].ToString(CultureInfo.InvariantCulture) + "%";
                    float midAngle = angle - sweep / 2.0f - 90.0f;
                    float midR = (r1 + r2) / 2.0f;
                    var (posX, posY) = GetPoint(xc, yc, midR, midAngle);
                    page.DrawString(f2, f2.GetSize(), pctStr,
                        posX - f2.StringWidth(pctStr) / 2.0f,
                        posY + f2.GetAscent() / 3.0f,
                        Util.ToRGB(BarChart.LabelColorOn(color)), null);
                }
            }
            // A thin white line between the slices, from the hole to the edge, at
            // most a quarter as wide as the thinner of the two slices is across
            // the middle of the ring, so that it does not cover a thin slice
            if (drawn > 1) {
                page.SetPenColor(Color.white);
                float midR = (r1 + r2) / 2.0f;
                for (int k = 0; k < drawn; k++) {
                    float thinner = Math.Min(sweeps[k], sweeps[(k + 1) % drawn]);
                    float across = midR * ((float) (thinner * Math.PI / 180.0));
                    page.SetPenWidth(Math.Min(1.5f, 0.25f * across));
                    float edge = edges[k];
                    var (innerX, innerY) = GetPoint(xc, yc, r2, edge - 90.0f);
                    var (outerX, outerY) = GetPoint(xc, yc, r1, edge - 90.0f);
                    page.DrawLine(innerX, innerY, outerX, outerY);
                }
            }
            if (legend) {
                DrawLegend(page, values, percentages, colors, yc + r1);
            }
            // The text in the hole, as large as one and a half labels or as fits
            if (r2 > 0.0f && titleFont != null && centerText.Length > 0) {
                // At three quarters of a label at least, shortened to fit there:
                // a long text was drawn as small as it took, too small to read
                float size = 1.5f * titleSize;
                String text = centerText;
                float width = titleFont.StringWidth(size, text);
                if (width > 1.6f * r2) {
                    size = Math.Max(size * (1.6f * r2) / width, 0.75f * titleSize);
                    text = ChartScale.Shorten(titleFont, size, text, 1.6f * r2);
                    width = titleFont.StringWidth(size, text);
                }
                float baseline = yc + (titleFont.GetAscent(size) - titleFont.GetDescent(size)) / 2f;
                page.DrawString(titleFont, size, text, xc - width / 2f, baseline, Util.ToRGB(INK), null);
            }
            page.RestoreGraphicsState();
            page.SetFigureBoundingBox(box[0], box[1], box[2] - box[0], box[3] - box[1]);
            page.AddEMC();
            return new float[] {box[2], box[3]};
        }

        // Returns the box of the legend under the circle, whose bottom is at the
        // y: a swatch, the label and the percentage of each slice, in rows as wide
        // as the circle; and draws it when there is a page.
        private float[] DrawLegend(Page page, float[] values, int[] percentages, int[] colors, float y) {
            float[] box = {x, y, x, y};
            if (f1 == null) {
                return box;
            }
            float swatch = f1.GetAscent();
            float gap = swatch / 2f;
            float left = x;
            float baseline = y + 1.5f * f1.GetBodyHeight();
            for (int i = 0; i < slices.Count; i++) {
                if (values[i] <= 0.0f) {
                    continue;
                }
                String percentage = percentages[i].ToString(CultureInfo.InvariantCulture) + "%";
                // A label too long for a row is shortened, its percentage kept
                String text = percentage;
                if (slices[i].text.Length > 0) {
                    float room = 2f * r1 - swatch - gap - f1.StringWidth(" " + percentage);
                    text = ChartScale.Shorten(f1, f1.GetSize(), slices[i].text, room) + " " + percentage;
                }
                float width = f1.StringWidth(text);
                if (left > x && left + swatch + gap + width > x + 2f * r1) {
                    left = x;
                    baseline += 1.5f * f1.GetBodyHeight();
                }
                if (page != null) {
                    page.SetBrushColor(colors[i]);
                    page.FillRect(left, baseline - swatch, swatch, swatch);
                    page.DrawString(f1, f1.GetSize(), text, left + swatch + gap, baseline, Util.ToRGB(INK), null);
                }
                box[2] = Math.Max(box[2], left + swatch + gap + width);
                box[3] = baseline + f1.GetDescent();
                left += swatch + gap + width + f1.GetBodyHeight();
            }
            return box;
        }

        // Returns the box that holds the two boxes, each left, top, right, bottom.
        private static float[] Union(float[] a, float[] b) {
            return new float[] {Math.Min(a[0], b[0]), Math.Min(a[1], b[1]),
                    Math.Max(a[2], b[2]), Math.Max(a[3], b[3])};
        }

        // Returns the value of each slice that is drawn, and 0 for the others: a
        // slice is drawn when its value is a number above 0. A NaN, from 0/0 in
        // an empty category of real data, or an infinity made the arc's loop
        // endless, until memory ran out (review of 11 October 2026, #2). Values
        // whose 360ths or whose sum are past the largest float are divided by the
        // largest of them, so that they are drawn as they compare.
        private float[] Values() {
            float[] values = new float[slices.Count];
            float largest = 0.0f;
            float total = 0.0f;
            for (int i = 0; i < values.Length; i++) {
                float value = slices[i].value;
                if (value > 0.0f && IsValue(value)) {
                    values[i] = value;
                    largest = Math.Max(largest, value);
                    total += value;
                }
            }
            if (largest > 0.0f && (!IsValue(largest * 360.0f) || !IsValue(total))) {
                for (int i = 0; i < values.Length; i++) {
                    values[i] = values[i] / largest;
                }
            }
            return values;
        }

        // Returns the whole percentage of each slice that is drawn, by the largest
        // remainder, so that they add up to 100: each rounded alone, 90 and six of
        // 1.5 were 91 and six of 2.
        private static int[] Percentages(float[] values, float total) {
            int[] percentages = new int[values.Length];
            double[] remainders = new double[values.Length];
            int sum = 0;
            for (int i = 0; i < values.Length; i++) {
                remainders[i] = -1.0;
                if (values[i] > 0.0f) {
                    double exact = (double) values[i] * 100.0 / (double) total;
                    percentages[i] = (int) Math.Floor(exact);
                    remainders[i] = exact - percentages[i];
                    sum += percentages[i];
                }
            }
            // The points left over to the largest remainders, the first of equal ones
            for (; sum < 100; sum++) {
                int largest = -1;
                for (int i = 0; i < values.Length; i++) {
                    if (remainders[i] >= 0.0 && (largest < 0 || remainders[i] > remainders[largest])) {
                        largest = i;
                    }
                }
                if (largest < 0) {
                    break;
                }
                percentages[largest]++;
                remainders[largest] = -1.0;
            }
            return percentages;
        }

        // Tells if the value is a number: not NaN and not infinite.
        private static bool IsValue(float value) {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        // Returns the alternate description, or the title, the subtitle, and the
        // label and the percentage of each slice when none is set.
        private String AltDescription(float[] values, int[] percentages) {
            if (!String.IsNullOrEmpty(altDescription)) {
                return altDescription;
            }
            String name = ChartScale.Join(title, subtitle);
            if (r2 > 0.0f && (f1 != null || f2 != null)) {
                name = ChartScale.Join(name, centerText);   // Drawn in the hole
            }
            if (name.Length == 0) {
                name = (r2 > 0.0f) ? "Donut chart" : "Pie chart";
            }
            System.Text.StringBuilder sb = new System.Text.StringBuilder(name).Append(':');
            String separator = " ";
            for (int i = 0; i < slices.Count; i++) {
                if (values[i] > 0.0f) {
                    sb.Append(separator);
                    if (slices[i].text.Length > 0) {
                        sb.Append(slices[i].text).Append(' ');
                    }
                    sb.Append(percentages[i].ToString(CultureInfo.InvariantCulture)).Append('%');
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
