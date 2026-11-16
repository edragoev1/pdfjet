/*
 * TableLines.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {

// The borders of the cells that a table draws on a page, kept while the cells
// are drawn and written as continuous lines when the table is done with the
// page: each cell stroked its own top and left, one short line for each edge,
// three or four times the lines of one line across each row. Lines on the
// same y, or the same x, that touch or overlap and have the same pen become
// one line, which covers the same part of the page as they did.
//
// A line is drawn later than its cell drew it, so what was drawn over it in
// between must be drawn over it still: before a background is filled over a
// line that is kept, or next to it, the lines are written; and before a
// drawable or a point is drawn, as either can draw anything anywhere, with
// the pen the cell left. Lines of different pens that overlap are written in
// the order the cells drew them, so the one on top is on top still.
internal sealed class TableLines {
    // The margin around a line, in points, within which another line may
    // paint a pixel of its own: a line covers whole pixels of a screen or a
    // printer, the hairline of width 0 one at least, so two lines that only
    // meet, end to end or edge to edge, may paint one pixel.
    private const float TABLE_LINE_MARGIN = 1;

    // A line: horizontal at y = at, from x = from to x = to, or vertical at
    // x = at, from y = from to y = to, with the pen of the page when the cell
    // drew it, and how far its caps reach past its ends.
    internal sealed class Segment {
        internal readonly bool vertical;
        internal readonly float at;
        internal float from;
        internal float to;
        internal readonly float r;
        internal readonly float g;
        internal readonly float b;
        internal readonly float width;
        internal readonly float cap;

        internal Segment(bool vertical, float at, float from, float to,
                float r, float g, float b, float width, float cap) {
            this.vertical = vertical;
            this.at = at;
            this.from = from;
            this.to = to;
            this.r = r;
            this.g = g;
            this.b = b;
            this.width = width;
            this.cap = cap;
        }

        internal bool SamePen(Segment other) {
            return r == other.r && g == other.g && b == other.b && width == other.width;
        }

        // The part of the page the line covers: left, top, right, bottom.
        internal float X0() { return vertical ? at - width/2 : from - cap; }
        internal float Y0() { return vertical ? from - cap : at - width/2; }
        internal float X1() { return vertical ? at + width/2 : to + cap; }
        internal float Y1() { return vertical ? to + cap : at + width/2; }

        // True when the lines may paint a pixel in common: they are then
        // stroked in the order the cells drew them.
        internal bool Overlaps(Segment other) {
            const float m = 2 * TABLE_LINE_MARGIN;
            return X0() < other.X1() + m && other.X0() < X1() + m &&
                    Y0() < other.Y1() + m && other.Y0() < Y1() + m;
        }
    }

    // The lines in the order the cells drew them.
    private readonly List<Segment> pending = new List<Segment>();

    // The pen of the borders of the last cell, which a cell drawing its
    // borders itself would have left the page with, and the count of the
    // page's pen changes then: when the pen was not set after it, the lines
    // leave the page with this pen, as the cell would have. The cells do not
    // set the pen of the page themselves, which would write it once for the
    // cell and again for its lines.
    private bool hasCellPen = false;
    private float[] cellPenColor;
    private float cellPenWidth;
    private int penChangesAtCell;

    // Keeps the borders of a cell that drawBorders would stroke, with the
    // pen of the cell, color and width: its top, bottom, left and right, as
    // it draws them.
    internal void AddBorders(Page page, uint properties, int color, float width,
            float x, float y, float cellW, float cellH) {
        // As SetPenColor(int) makes the color
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        cellPenColor = new float[] {r, g, b};
        cellPenWidth = width;
        hasCellPen = true;
        penChangesAtCell = page.penChanges;
        // Half the pen width, so that the corners of the borders close.
        float hWidth = width / 2;
        if ((properties & Border.TOP) != 0) {
            Add(page, false, y, x - hWidth, x + cellW);
        }
        if ((properties & Border.BOTTOM) != 0) {
            Add(page, false, y + cellH, x - hWidth, x + cellW);
        }
        if ((properties & Border.LEFT) != 0) {
            Add(page, true, x, y - hWidth, y + cellH + hWidth);
        }
        if ((properties & Border.RIGHT) != 0) {
            Add(page, true, x + cellW, y - hWidth, y + cellH + hWidth);
        }
    }

    private void Add(Page page, bool vertical, float at, float from, float to) {
        float width = cellPenWidth;
        float cap = (page.lineCapStyle == CapStyle.BUTT) ? 0f : width/2;
        pending.Add(new Segment(vertical, at, from, to,
                cellPenColor[0], cellPenColor[1], cellPenColor[2], width, cap));
    }

    // Writes the lines before a background is filled over the rectangle
    // left, top, right, bottom, when it covers a line, or meets it along an
    // edge: drawn over the background, the line would look different where it
    // is not a whole pixel wide. A line that only touches the rectangle at a
    // corner, as the lines of the row above meet the background of a cell
    // where they end, is kept; one that goes into it, however little, as the
    // end of a wider line does, is written first, so the background covers
    // it as it did.
    internal void BeforeFill(Page page, float left, float top, float right, float bottom) {
        foreach (Segment line in pending) {
            float x0 = Math.Max(line.X0(), left);
            float y0 = Math.Max(line.Y0(), top);
            float x1 = Math.Min(line.X1(), right);
            float y1 = Math.Min(line.Y1(), bottom);
            if (x0 > x1 || y0 > y1) {
                continue;   // Apart
            }
            if ((x0 == x1 && y1 - y0 <= line.width) || (y0 == y1 && x1 - x0 <= line.width)) {
                continue;   // At a corner
            }
            Flush(page);
            return;
        }
    }

    // Writes the lines kept so far, joined, and leaves the page with the pen
    // it would have had, had each cell drawn its own borders: what is drawn
    // next finds the pen the last cell left, or the one set after it.
    internal void Flush(Page page) {
        if (pending.Count == 0) {
            return;
        }
        float[] color;
        float width;
        if (hasCellPen && page.penChanges == penChangesAtCell) {
            color = cellPenColor;
            width = cellPenWidth;
        } else {
            color = page.GetPenColor();
            width = page.GetPenWidth();
        }
        hasCellPen = false;
        // The lines of a pen go together, unless a line of another pen that
        // a cell drew after them overlaps the line: it starts a new group,
        // after the line of the other pen.
        List<List<Segment>> groups = new List<List<Segment>>();
        foreach (Segment line in pending) {
            List<Segment> target = null;
            for (int i = groups.Count - 1; i >= 0; i--) {
                List<Segment> group = groups[i];
                if (group[0].SamePen(line)) {
                    target = group;
                    break;
                }
                if (OverlapsAny(group, line)) {
                    break;
                }
            }
            if (target == null) {
                target = new List<Segment>();
                groups.Add(target);
            }
            target.Add(line);
        }
        pending.Clear();

        page.AddArtifactBMC();
        foreach (List<Segment> group in groups) {
            Segment first = group[0];
            page.SetPenColor(new float[] {first.r, first.g, first.b});
            page.SetPenWidth(first.width);
            // The horizontal lines from the top down, each from the left,
            // then the vertical lines from the left, each from the top.
            List<Segment> lines = new List<Segment>(group);
            lines.Sort(Compare);
            // A line that two cells drew, such as the line under the header,
            // which the first row draws over, was stroked twice, darker where
            // it is not a whole pixel wide: it is stroked twice still, once
            // in each of two layers. The ends of the lines of two cells next
            // to each other overlap by no more than the width of the pen.
            List<List<Segment>> layers = new List<List<Segment>>();
            foreach (Segment next in lines) {
                List<Segment> layer = null;
                Segment last = null;
                foreach (List<Segment> candidate in layers) {
                    last = candidate[candidate.Count - 1];
                    if (last.vertical != next.vertical || last.at != next.at ||
                            last.to - next.from <= 1.5f*next.width) {
                        layer = candidate;
                        break;
                    }
                }
                if (layer == null) {
                    layer = new List<Segment>();
                    layers.Add(layer);
                    last = null;
                }
                if (last != null && last.vertical == next.vertical &&
                        last.at == next.at && next.from <= last.to) {
                    if (next.to > last.to) {
                        last.to = next.to;
                    }
                } else {
                    layer.Add(new Segment(next.vertical, next.at, next.from, next.to,
                            next.r, next.g, next.b, next.width, next.cap));
                }
            }
            foreach (List<Segment> layer in layers) {
                foreach (Segment line in layer) {
                    Draw(page, line);
                }
                page.StrokePath();
            }
        }
        page.SetPenColor(color);
        page.SetPenWidth(width);
        page.AddEMC();
    }

    private static bool OverlapsAny(List<Segment> group, Segment line) {
        foreach (Segment other in group) {
            if (other.Overlaps(line)) {
                return true;
            }
        }
        return false;
    }

    private static void Draw(Page page, Segment line) {
        if (line.vertical) {
            page.MoveTo(line.at, line.from);
            page.LineTo(line.at, line.to);
        } else {
            page.MoveTo(line.from, line.at);
            page.LineTo(line.to, line.at);
        }
    }

    // Horizontal before vertical, then by where the line is and where it
    // starts and ends: the same order in the four ports.
    private static int Compare(Segment a, Segment b) {
        if (a.vertical != b.vertical) {
            return a.vertical ? 1 : -1;
        }
        int c = Compare(a.at, b.at);
        if (c == 0) {
            c = Compare(a.from, b.from);
        }
        if (c == 0) {
            c = Compare(a.to, b.to);
        }
        return c;
    }

    private static int Compare(float a, float b) {
        return (a < b) ? -1 : ((a > b) ? 1 : 0);
    }
}
}   // End of TableLines.cs
