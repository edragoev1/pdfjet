/*
 * TableLines.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

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
final class TableLines {
    // The margin around a line, in points, within which another line may
    // paint a pixel of its own: a line covers whole pixels of a screen or a
    // printer, the hairline of width 0 one at least, so two lines that only
    // meet, end to end or edge to edge, may paint one pixel.
    private static final float TABLE_LINE_MARGIN = 1;

    // A line: horizontal at y = at, from x = from to x = to, or vertical at
    // x = at, from y = from to y = to, with the pen of the page when the cell
    // drew it, and how far its caps reach past its ends.
    static final class Segment {
        final boolean vertical;
        final float at;
        float from;
        float to;
        final float r;
        final float g;
        final float b;
        final float width;
        final float cap;

        Segment(boolean vertical, float at, float from, float to,
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

        boolean samePen(Segment other) {
            return r == other.r && g == other.g && b == other.b && width == other.width;
        }

        // The part of the page the line covers: left, top, right, bottom.
        float x0() { return vertical ? at - width/2 : from - cap; }
        float y0() { return vertical ? from - cap : at - width/2; }
        float x1() { return vertical ? at + width/2 : to + cap; }
        float y1() { return vertical ? to + cap : at + width/2; }

        // True when the lines may paint a pixel in common: they are then
        // stroked in the order the cells drew them.
        boolean overlaps(Segment other) {
            final float m = 2 * TABLE_LINE_MARGIN;
            return x0() < other.x1() + m && other.x0() < x1() + m
                    && y0() < other.y1() + m && other.y0() < y1() + m;
        }
    }

    // The lines in the order the cells drew them.
    private final List<Segment> pending = new ArrayList<Segment>();

    // The pen of the borders of the last cell, which a cell drawing its
    // borders itself would have left the page with, and the count of the
    // page's pen changes then: when the pen was not set after it, the lines
    // leave the page with this pen, as the cell would have. The cells do not
    // set the pen of the page themselves, which would write it once for the
    // cell and again for its lines.
    private boolean hasCellPen = false;
    private float[] cellPenColor;
    private float cellPenWidth;
    private int penChangesAtCell;

    // Keeps the borders of a cell that drawBorders would stroke, with the
    // pen of the cell, color and width: its top, bottom, left and right, as
    // it draws them.
    void addBorders(Page page, int properties, int color, float width,
            float x, float y, float cellW, float cellH) {
        // As setPenColor(int) makes the color
        cellPenColor = Util.toRGB(color);
        cellPenWidth = width;
        hasCellPen = true;
        penChangesAtCell = page.penChanges;
        // Half the pen width, so that the corners of the borders close.
        float hWidth = width / 2;
        if ((properties & Border.TOP) != 0) {
            add(page, false, y, x - hWidth, x + cellW);
        }
        if ((properties & Border.BOTTOM) != 0) {
            add(page, false, y + cellH, x - hWidth, x + cellW);
        }
        if ((properties & Border.LEFT) != 0) {
            add(page, true, x, y - hWidth, y + cellH + hWidth);
        }
        if ((properties & Border.RIGHT) != 0) {
            add(page, true, x + cellW, y - hWidth, y + cellH + hWidth);
        }
    }

    private void add(Page page, boolean vertical, float at, float from, float to) {
        float width = cellPenWidth;
        float cap = (page.lineCapStyle == CapStyle.BUTT) ? 0f : width/2;
        pending.add(new Segment(vertical, at, from, to,
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
    void beforeFill(Page page, float left, float top, float right, float bottom) {
        for (Segment line : pending) {
            float x0 = Math.max(line.x0(), left);
            float y0 = Math.max(line.y0(), top);
            float x1 = Math.min(line.x1(), right);
            float y1 = Math.min(line.y1(), bottom);
            if (x0 > x1 || y0 > y1) {
                continue;   // Apart
            }
            if ((x0 == x1 && y1 - y0 <= line.width) || (y0 == y1 && x1 - x0 <= line.width)) {
                continue;   // At a corner
            }
            flush(page);
            return;
        }
    }

    // Writes the lines kept so far, joined, and leaves the page with the pen
    // it would have had, had each cell drawn its own borders: what is drawn
    // next finds the pen the last cell left, or the one set after it.
    void flush(Page page) {
        if (pending.isEmpty()) {
            return;
        }
        float[] color;
        float width;
        if (hasCellPen && page.penChanges == penChangesAtCell) {
            color = cellPenColor;
            width = cellPenWidth;
        } else {
            color = page.getPenColor();
            width = page.getPenWidth();
        }
        hasCellPen = false;
        // The lines of a pen go together, unless a line of another pen that
        // a cell drew after them overlaps the line: it starts a new group,
        // after the line of the other pen.
        List<List<Segment>> groups = new ArrayList<List<Segment>>();
        for (Segment line : pending) {
            List<Segment> target = null;
            for (int i = groups.size() - 1; i >= 0; i--) {
                List<Segment> group = groups.get(i);
                if (group.get(0).samePen(line)) {
                    target = group;
                    break;
                }
                if (overlapsAny(group, line)) {
                    break;
                }
            }
            if (target == null) {
                target = new ArrayList<Segment>();
                groups.add(target);
            }
            target.add(line);
        }
        pending.clear();

        page.addArtifactBMC();
        for (List<Segment> group : groups) {
            Segment first = group.get(0);
            page.setPenColor(new float[] {first.r, first.g, first.b});
            page.setPenWidth(first.width);
            // The horizontal lines from the top down, each from the left,
            // then the vertical lines from the left, each from the top.
            List<Segment> lines = new ArrayList<Segment>(group);
            Collections.sort(lines, ORDER);
            // A line that two cells drew, such as the line under the header,
            // which the first row draws over, was stroked twice, darker where
            // it is not a whole pixel wide: it is stroked twice still, once
            // in each of two layers. The ends of the lines of two cells next
            // to each other overlap by no more than the width of the pen.
            List<List<Segment>> layers = new ArrayList<List<Segment>>();
            for (Segment next : lines) {
                List<Segment> layer = null;
                Segment last = null;
                for (List<Segment> candidate : layers) {
                    last = candidate.get(candidate.size() - 1);
                    if (last.vertical != next.vertical || last.at != next.at ||
                            last.to - next.from <= 1.5f*next.width) {
                        layer = candidate;
                        break;
                    }
                }
                if (layer == null) {
                    layer = new ArrayList<Segment>();
                    layers.add(layer);
                    last = null;
                }
                if (last != null && last.vertical == next.vertical &&
                        last.at == next.at && next.from <= last.to) {
                    if (next.to > last.to) {
                        last.to = next.to;
                    }
                } else {
                    layer.add(new Segment(next.vertical, next.at, next.from, next.to,
                            next.r, next.g, next.b, next.width, next.cap));
                }
            }
            for (List<Segment> layer : layers) {
                for (Segment line : layer) {
                    draw(page, line);
                }
                page.strokePath();
            }
        }
        page.setPenColor(color);
        page.setPenWidth(width);
        page.addEMC();
    }

    private static boolean overlapsAny(List<Segment> group, Segment line) {
        for (Segment other : group) {
            if (other.overlaps(line)) {
                return true;
            }
        }
        return false;
    }

    private static void draw(Page page, Segment line) {
        if (line.vertical) {
            page.moveTo(line.at, line.from);
            page.lineTo(line.at, line.to);
        } else {
            page.moveTo(line.from, line.at);
            page.lineTo(line.to, line.at);
        }
    }

    // Horizontal before vertical, then by where the line is and where it
    // starts and ends: the same order in the four ports.
    private static final Comparator<Segment> ORDER = new Comparator<Segment>() {
        @Override
        public int compare(Segment a, Segment b) {
            if (a.vertical != b.vertical) {
                return a.vertical ? 1 : -1;
            }
            int c = compare(a.at, b.at);
            if (c == 0) {
                c = compare(a.from, b.from);
            }
            if (c == 0) {
                c = compare(a.to, b.to);
            }
            return c;
        }

        private int compare(float a, float b) {
            return (a < b) ? -1 : ((a > b) ? 1 : 0);
        }
    };
}   // End of TableLines.java
