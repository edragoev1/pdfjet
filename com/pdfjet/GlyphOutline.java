/*
 * GlyphOutline.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.ArrayList;
import java.util.List;

/**
 * The outlines of the glyphs of a font, read from the font file itself: the
 * glyf table of a TrueType font, its quadratic contours drawn exactly as
 * cubic curves, composite glyphs with the transforms of their parts; or the
 * Type 2 charstrings of the CFF table of an OpenType font, with their
 * subroutines, the hints left out. Page.drawGlyphs draws them as paths.
 * <p>
 * An outline is written as the path operators of PDF in the units of the
 * font, y up, filled with the nonzero winding rule; the matrix takes the
 * units to the em, so that the size of the font times the matrix, moved to
 * the glyph's place, draws it.
 */
final class GlyphOutline {
    // The message of a font whose outlines cannot be read.
    private static final String UNREADABLE = "The outlines of the font cannot be read: ";

    /** An outline: its path operators and the box of its points. */
    static final class Path {
        final String operators;     // Empty for a glyph that draws nothing, as the space
        final double xMin;
        final double yMin;
        final double xMax;
        final double yMax;

        Path(String operators, double xMin, double yMin, double xMax, double yMax) {
            this.operators = operators;
            this.xMin = xMin;
            this.yMin = yMin;
            this.xMax = xMax;
            this.yMax = yMax;
        }
    }

    private final byte[] data;
    private final boolean cff;
    // The units of the font to the em: a b c d e f
    final double[] matrix;
    private final int numGlyphs;

    // TrueType
    private int glyfOffset;
    private int glyfLength;
    private int locaOffset;
    private boolean longOffsets;

    // CFF
    private CFFSubset.Index charStrings;
    private CFFSubset.Index globalSubrs;
    private CFFSubset.Index[] localSubrs;   // Of each Font DICT, or of the font
    private int[] fdOf;                     // The Font DICT of each glyph, or null
    private double[][] fdMatrix;            // The FontMatrix of each Font DICT, or null

    /**
     * Reads the tables of the outlines of the font program: a TrueType font,
     * or the CFF table of an OpenType font.
     */
    GlyphOutline(byte[] data, boolean cff) {
        this.data = data;
        this.cff = cff;
        try {
            if (cff) {
                matrix = readCFF();
                numGlyphs = charStrings.count();
            } else {
                int unitsPerEm = readTrueType();
                matrix = new double[] {1.0 / unitsPerEm, 0, 0, 1.0 / unitsPerEm, 0, 0};
                numGlyphs = (locaLength - (longOffsets ? 4 : 2)) / (longOffsets ? 4 : 2);
            }
        } catch (Subset.NotSubset | RuntimeException e) {
            throw new IllegalArgumentException(UNREADABLE + "its tables are damaged.");
        }
    }

    private int locaLength;

    // Reads the head, loca and glyf tables, and returns the units per em.
    private int readTrueType() throws Subset.NotSubset {
        int numTables = u16(4);
        int headOffset = -1;
        int locaAt = -1;
        int glyfAt = -1;
        for (int i = 0; i < numTables; i++) {
            int entry = 12 + 16 * i;
            String tag = new String(data, entry, 4, java.nio.charset.StandardCharsets.ISO_8859_1);
            int offset = u32(entry + 8);
            int length = u32(entry + 12);
            if ((long) offset + length > data.length) {
                throw new Subset.NotSubset();
            }
            if (tag.equals("head")) {
                headOffset = offset;
            } else if (tag.equals("loca")) {
                locaAt = offset;
                locaLength = length;
            } else if (tag.equals("glyf")) {
                glyfAt = offset;
                glyfLength = length;
            }
        }
        if (headOffset < 0 || locaAt < 0 || glyfAt < 0) {
            throw new Subset.NotSubset();
        }
        int unitsPerEm = u16(headOffset + 18);
        longOffsets = u16(headOffset + 50) == 1;
        locaOffset = locaAt;
        glyfOffset = glyfAt;
        if (unitsPerEm == 0) {
            throw new Subset.NotSubset();
        }
        return unitsPerEm;
    }

    // Reads the CFF table: its charstrings, its subroutines and the Font DICT
    // of each glyph, and returns the FontMatrix of the font.
    private double[] readCFF() throws Subset.NotSubset {
        if (data.length < 4 || data[0] != 1) {
            throw new Subset.NotSubset();   // Not CFF version 1
        }
        CFFSubset.Index names = CFFSubset.readIndex(data, data[2] & 0xFF);
        CFFSubset.Index topDicts = CFFSubset.readIndex(data, names.end);
        if (topDicts.count() != 1) {
            throw new Subset.NotSubset();
        }
        CFFSubset.Index strings = CFFSubset.readIndex(data, topDicts.end);
        globalSubrs = CFFSubset.readIndex(data, strings.end);
        List<CFFSubset.Entry> top = CFFSubset.readDict(data, topDicts.objects[0], topDicts.objects[1]);
        int[] at = CFFSubset.entryOf(top, CFFSubset.CHAR_STRINGS);
        if (at == null || at.length != 1) {
            throw new Subset.NotSubset();
        }
        charStrings = CFFSubset.readIndex(data, at[0]);
        double[] topMatrix = matrixOf(top);
        if (CFFSubset.entryOf(top, CFFSubset.ROS) != null) {
            int[] fdArrayAt = CFFSubset.entryOf(top, CFFSubset.FD_ARRAY);
            int[] fdSelectAt = CFFSubset.entryOf(top, CFFSubset.FD_SELECT);
            if (fdArrayAt == null || fdArrayAt.length != 1 || fdSelectAt == null || fdSelectAt.length != 1) {
                throw new Subset.NotSubset();
            }
            CFFSubset.Index fdArray = CFFSubset.readIndex(data, fdArrayAt[0]);
            localSubrs = new CFFSubset.Index[fdArray.count()];
            fdMatrix = new double[fdArray.count()][];
            for (int i = 0; i < fdArray.count(); i++) {
                List<CFFSubset.Entry> fd = CFFSubset.readDict(data, fdArray.objects[i], fdArray.objects[i + 1]);
                localSubrs[i] = subrsOf(fd);
                fdMatrix[i] = matrixOf(fd);
            }
            byte[] fdSelect = CFFSubset.block(data, fdSelectAt[0],
                    CFFSubset.fdSelectLength(data, fdSelectAt[0], charStrings.count()));
            fdOf = CFFSubset.fdOf(fdSelect, charStrings.count(), fdArray.count());
            // The glyph space of a CIDFont: the FontMatrix of the glyph's Font
            // DICT and then the Top DICT's; a CIDFont whose Top DICT has none
            // has its Font DICTs' taken as the whole ones, as readers do.
            for (int i = 0; i < fdMatrix.length; i++) {
                if (fdMatrix[i] == null) {
                    fdMatrix[i] = (topMatrix == null) ? THOUSANDTH : topMatrix;
                } else if (topMatrix != null) {
                    fdMatrix[i] = multiply(fdMatrix[i], topMatrix);
                }
            }
            return THOUSANDTH;  // Of a glyph, its Font DICT's
        }
        localSubrs = new CFFSubset.Index[] {subrsOf(top)};
        return (topMatrix == null) ? THOUSANDTH : topMatrix;
    }

    // The default FontMatrix of a CFF font.
    private static final double[] THOUSANDTH = {0.001, 0, 0, 0.001, 0, 0};

    // The local subroutines of the Private DICT of the Top DICT or Font DICT.
    private CFFSubset.Index subrsOf(List<CFFSubset.Entry> dict) throws Subset.NotSubset {
        int[] p = CFFSubset.entryOf(dict, CFFSubset.PRIVATE);
        if (p == null || p.length != 2) {
            throw new Subset.NotSubset();
        }
        return CFFSubset.readPrivate(data, p[0], p[1]).subrs;
    }

    // The FontMatrix of the DICT, or null when it has none.
    private static double[] matrixOf(List<CFFSubset.Entry> dict) throws Subset.NotSubset {
        for (CFFSubset.Entry entry : dict) {
            if (entry.op == CFFSubset.FONT_MATRIX) {
                double[] m = operands(entry.raw);
                if (m.length != 6) {
                    throw new Subset.NotSubset();
                }
                for (double v : m) {
                    if (!(Math.abs(v) <= 1000)) {   // NaN too
                        throw new Subset.NotSubset();
                    }
                }
                return m;
            }
        }
        return null;
    }

    // The product of the matrices: a, then b.
    private static double[] multiply(double[] a, double[] b) {
        return new double[] {
            a[0] * b[0] + a[1] * b[2],
            a[0] * b[1] + a[1] * b[3],
            a[2] * b[0] + a[3] * b[2],
            a[2] * b[1] + a[3] * b[3],
            a[4] * b[0] + a[5] * b[2] + b[4],
            a[4] * b[1] + a[5] * b[3] + b[5]
        };
    }

    // The operands of an entry of a DICT, integers and reals, before its operator.
    private static double[] operands(byte[] raw) throws Subset.NotSubset {
        List<Double> values = new ArrayList<Double>();
        int i = 0;
        while (i < raw.length) {
            int b0 = raw[i] & 0xFF;
            if (b0 <= 21) {
                break;  // The operator
            } else if (b0 == 28) {
                values.add((double) (short) ((raw[i + 1] & 0xFF) << 8 | (raw[i + 2] & 0xFF)));
                i += 3;
            } else if (b0 == 29) {
                values.add((double) ((raw[i + 1] & 0xFF) << 24 | (raw[i + 2] & 0xFF) << 16
                        | (raw[i + 3] & 0xFF) << 8 | (raw[i + 4] & 0xFF)));
                i += 5;
            } else if (b0 == 30) {
                StringBuilder sb = new StringBuilder();
                i++;
                boolean end = false;
                while (!end && i < raw.length) {
                    int b = raw[i++] & 0xFF;
                    for (int nibble : new int[] {b >> 4, b & 0x0F}) {
                        if (nibble <= 9) {
                            sb.append((char) ('0' + nibble));
                        } else if (nibble == 0xA) {
                            sb.append('.');
                        } else if (nibble == 0xB) {
                            sb.append('E');
                        } else if (nibble == 0xC) {
                            sb.append("E-");
                        } else if (nibble == 0xE) {
                            sb.append('-');
                        } else if (nibble == 0xF) {
                            end = true;
                            break;
                        }
                    }
                }
                try {
                    values.add(Double.parseDouble(sb.toString()));
                } catch (NumberFormatException e) {
                    throw new Subset.NotSubset();
                }
            } else if (b0 >= 32 && b0 <= 246) {
                values.add((double) (b0 - 139));
                i++;
            } else if (b0 >= 247 && b0 <= 250) {
                values.add((double) ((b0 - 247) * 256 + (raw[i + 1] & 0xFF) + 108));
                i += 2;
            } else if (b0 >= 251 && b0 <= 254) {
                values.add((double) (-(b0 - 251) * 256 - (raw[i + 1] & 0xFF) - 108));
                i += 2;
            } else {
                throw new Subset.NotSubset();
            }
        }
        double[] result = new double[values.size()];
        for (int k = 0; k < result.length; k++) {
            result[k] = values.get(k);
        }
        return result;
    }

    /** The number of glyphs of the font. */
    int count() {
        return numGlyphs;
    }

    /**
     * The matrix that takes the units of the glyph to the em: of the font,
     * or of the glyph's Font DICT in a CID-keyed CFF font.
     */
    double[] matrixOf(int gid) {
        if (fdMatrix != null && gid >= 0 && gid < fdOf.length) {
            return fdMatrix[fdOf[gid]];
        }
        return matrix;
    }

    /** The outline of the glyph. */
    Path outline(int gid) {
        if (gid < 0 || gid >= numGlyphs) {
            throw new IllegalArgumentException("The font has no glyph number " + gid + ".");
        }
        Pen pen = new Pen();
        try {
            if (cff) {
                new CharString(pen, gid).run(charStrings.objects[gid], charStrings.objects[gid + 1], 0);
            } else {
                Points points = new Points(new int[1]);
                trueTypePoints(gid, points, 0);
                points.draw(pen);
            }
            if (pen.outOfRange) {
                throw new Subset.NotSubset();
            }
        } catch (Subset.NotSubset | RuntimeException e) {
            if (e instanceof IllegalArgumentException && e.getMessage() != null
                    && e.getMessage().startsWith("The CFF glyph")) {
                throw (IllegalArgumentException) e;
            }
            throw new IllegalArgumentException(UNREADABLE + "glyph number " + gid + " is damaged.");
        }
        return pen.path();
    }

    // ------------------------------------------------------------------
    // The path: the operators of PDF, in the units of the font

    // The bound of the points of an outline, in the units of the font: a
    // font's are within ±32,768, and a charstring's arithmetic can make larger
    // ones, or ones not finite, which a path cannot have.
    private static final double MAX_COORDINATE = 1e6;

    /** Writes the path operators and finds the box of the points. */
    static final class Pen {
        private final StringBuilder sb = new StringBuilder();
        private double xMin = Double.MAX_VALUE;
        private double yMin = Double.MAX_VALUE;
        private double xMax = -Double.MAX_VALUE;
        private double yMax = -Double.MAX_VALUE;
        private double startX;
        private double startY;
        private double x;
        private double y;
        private boolean open;       // A contour is begun and has a segment
        private boolean outOfRange; // A point past MAX_COORDINATE, or not finite

        private void point(double px, double py) {
            if (!(Math.abs(px) <= MAX_COORDINATE && Math.abs(py) <= MAX_COORDINATE)) {
                outOfRange = true;  // NaN too
                px = 0;
                py = 0;
            }
            if (px < xMin) xMin = px;
            if (py < yMin) yMin = py;
            if (px > xMax) xMax = px;
            if (py > yMax) yMax = py;
            sb.append(number(px)).append(' ').append(number(py));
        }

        void moveTo(double px, double py) {
            startX = px;
            startY = py;
            x = px;
            y = py;
            open = false;
        }

        // Writes the m of the contour before its first segment, so that a
        // contour of a moveto alone writes nothing.
        private void begin() {
            if (!open) {
                point(startX, startY);
                sb.append(" m\n");
                open = true;
            }
        }

        void lineTo(double px, double py) {
            begin();
            point(px, py);
            sb.append(" l\n");
            x = px;
            y = py;
        }

        void curveTo(double x1, double y1, double x2, double y2, double x3, double y3) {
            begin();
            point(x1, y1);
            sb.append(' ');
            point(x2, y2);
            sb.append(' ');
            point(x3, y3);
            sb.append(" c\n");
            x = x3;
            y = y3;
        }

        // The quadratic curve from the current point drawn exactly as a cubic one.
        void quadTo(double qx, double qy, double px, double py) {
            curveTo(x + 2.0 / 3.0 * (qx - x), y + 2.0 / 3.0 * (qy - y),
                    px + 2.0 / 3.0 * (qx - px), py + 2.0 / 3.0 * (qy - py),
                    px, py);
        }

        Path path() {
            if (sb.length() == 0) {
                return new Path("", 0, 0, 0, 0);
            }
            return new Path(sb.toString(), xMin, yMin, xMax, yMax);
        }
    }

    /**
     * A number of a path, in the units of the font: in hundredths, trailing
     * zeros left out, the same in the four ports. Rounded half away from zero.
     */
    static String number(double value) {
        return decimals(value, 2);
    }

    /** The value with at most the decimals given, rounded half away from zero. */
    static String decimals(double value, int decimals) {
        long scale = 1;
        for (int i = 0; i < decimals; i++) {
            scale *= 10;
        }
        long n = (long) Math.floor(Math.abs(value) * scale + 0.5);
        if (n == 0) {
            return "0";
        }
        StringBuilder sb = new StringBuilder();
        if (value < 0) {
            sb.append('-');
        }
        sb.append(n / scale);
        long fraction = n % scale;
        if (fraction != 0) {
            String digits = Long.toString(fraction + scale).substring(1);
            int end = digits.length();
            while (digits.charAt(end - 1) == '0') {
                end--;
            }
            sb.append('.').append(digits, 0, end);
        }
        return sb.toString();
    }

    // ------------------------------------------------------------------
    // TrueType

    /** The points of a glyph, with the last point of each contour. */
    static final class Points {
        double[] x = new double[64];
        double[] y = new double[64];
        boolean[] on = new boolean[64];
        int count;
        List<Integer> ends = new ArrayList<Integer>();
        final int[] work;   // The glyphs and points read for the outline, its parts' too

        Points(int[] work) {
            this.work = work;
        }

        void add(double px, double py, boolean onCurve) {
            if (count == x.length) {
                x = java.util.Arrays.copyOf(x, 2 * count);
                y = java.util.Arrays.copyOf(y, 2 * count);
                on = java.util.Arrays.copyOf(on, 2 * count);
            }
            x[count] = px;
            y[count] = py;
            on[count] = onCurve;
            count++;
        }

        // Draws the contours: the points on the curve joined by lines, and by
        // quadratic curves through the points off it, with a point on the
        // curve halfway between two off it.
        void draw(Pen pen) {
            int first = 0;
            for (int end : ends) {
                int n = end - first + 1;
                if (n >= 2) {
                    contour(pen, first, n);
                }
                first = end + 1;
            }
        }

        private void contour(Pen pen, int first, int n) {
            double sx;
            double sy;
            int from;   // The first point after the start
            int steps;  // The points after the start, the start not again
            if (on[first]) {
                sx = x[first];
                sy = y[first];
                from = 1;
                steps = n - 1;
            } else if (on[first + n - 1]) {
                sx = x[first + n - 1];
                sy = y[first + n - 1];
                from = 0;
                steps = n - 1;
            } else {
                sx = (x[first] + x[first + n - 1]) / 2;
                sy = (y[first] + y[first + n - 1]) / 2;
                from = 0;
                steps = n;
            }
            pen.moveTo(sx, sy);
            boolean off = false;
            double qx = 0;
            double qy = 0;
            for (int k = 0; k < steps; k++) {
                int i = first + (from + k) % n;
                if (on[i]) {
                    if (off) {
                        pen.quadTo(qx, qy, x[i], y[i]);
                    } else {
                        pen.lineTo(x[i], y[i]);
                    }
                    off = false;
                } else {
                    if (off) {
                        pen.quadTo(qx, qy, (qx + x[i]) / 2, (qy + y[i]) / 2);
                    }
                    qx = x[i];
                    qy = y[i];
                    off = true;
                }
            }
            // Back to the start: a curve, or the line that filling draws
            if (off) {
                pen.quadTo(qx, qy, sx, sy);
            }
        }
    }

    // Adds the points of the glyph, a simple or a composite one, to the points.
    private void trueTypePoints(int gid, Points points, int depth) throws Subset.NotSubset {
        if (depth > 16) {
            throw new Subset.NotSubset();   // Composite glyphs nested too deep, or in a loop
        }
        // A composite glyph of many parts, each of many parts, 16 deep, would
        // take time and memory that grow exponentially: the work of a glyph is
        // bounded, as that of a charstring is.
        points.work[0]++;
        if (points.work[0] > MAX_WORK) {
            throw new Subset.NotSubset();
        }
        int start;
        int end;
        if (longOffsets) {
            start = u32(locaOffset + 4 * gid);
            end = u32(locaOffset + 4 * gid + 4);
        } else {
            start = 2 * u16(locaOffset + 2 * gid);
            end = 2 * u16(locaOffset + 2 * gid + 2);
        }
        if (start == end) {
            return;     // No outline, as the space
        }
        if (start > end || end > glyfLength || end - start < 10) {
            throw new Subset.NotSubset();
        }
        int at = glyfOffset + start;
        int limit = glyfOffset + end;
        int contours = (short) u16(at);
        if (contours >= 0) {
            simplePoints(at, limit, contours, points);
        } else {
            compositePoints(at + 10, limit, points, depth);
        }
    }

    private void simplePoints(int at, int limit, int contours, Points points) throws Subset.NotSubset {
        int p = at + 10;
        int[] ends = new int[contours];
        int n = 0;
        for (int i = 0; i < contours; i++) {
            ends[i] = u16(p + 2 * i);
            if (ends[i] < n - 1 || (i > 0 && ends[i] <= ends[i - 1])) {
                throw new Subset.NotSubset();
            }
            n = ends[i] + 1;
        }
        points.work[0] += n;
        if (points.work[0] > MAX_WORK) {
            throw new Subset.NotSubset();
        }
        p += 2 * contours;
        int instructions = u16(p);
        p += 2 + instructions;
        byte[] flags = new byte[n];
        for (int i = 0; i < n; ) {
            int flag = byteAt(p++, limit);
            flags[i++] = (byte) flag;
            if ((flag & 0x08) != 0) {   // REPEAT_FLAG
                int repeat = byteAt(p++, limit);
                for (int r = 0; r < repeat; r++) {
                    if (i >= n) {
                        throw new Subset.NotSubset();
                    }
                    flags[i++] = (byte) flag;
                }
            }
        }
        int[] xs = new int[n];
        int value = 0;
        for (int i = 0; i < n; i++) {
            int flag = flags[i];
            if ((flag & 0x02) != 0) {           // X_SHORT_VECTOR
                int dx = byteAt(p++, limit);
                value += (flag & 0x10) != 0 ? dx : -dx;
            } else if ((flag & 0x10) == 0) {    // Not X_IS_SAME
                value += (short) (byteAt(p, limit) << 8 | byteAt(p + 1, limit));
                p += 2;
            }
            xs[i] = value;
        }
        value = 0;
        int base = points.count;
        for (int i = 0; i < n; i++) {
            int flag = flags[i];
            if ((flag & 0x04) != 0) {           // Y_SHORT_VECTOR
                int dy = byteAt(p++, limit);
                value += (flag & 0x20) != 0 ? dy : -dy;
            } else if ((flag & 0x20) == 0) {    // Not Y_IS_SAME
                value += (short) (byteAt(p, limit) << 8 | byteAt(p + 1, limit));
                p += 2;
            }
            points.add(xs[i], value, (flag & 0x01) != 0);
        }
        for (int end : ends) {
            points.ends.add(base + end);
        }
    }

    private void compositePoints(int p, int limit, Points points, int depth) throws Subset.NotSubset {
        while (true) {
            int flags = u16(p);
            int component = u16(p + 2);
            p += 4;
            if (component >= numGlyphs) {
                throw new Subset.NotSubset();
            }
            int arg1;
            int arg2;
            boolean xy = (flags & 0x0002) != 0;     // ARGS_ARE_XY_VALUES
            if ((flags & 0x0001) != 0) {            // ARG_1_AND_2_ARE_WORDS
                arg1 = u16(p);
                arg2 = u16(p + 2);
                if (xy) {
                    arg1 = (short) arg1;
                    arg2 = (short) arg2;
                }
                p += 4;
            } else {
                arg1 = byteAt(p, limit);
                arg2 = byteAt(p + 1, limit);
                if (xy) {
                    arg1 = (byte) arg1;
                    arg2 = (byte) arg2;
                }
                p += 2;
            }
            double a = 1;
            double b = 0;
            double c = 0;
            double d = 1;
            if ((flags & 0x0008) != 0) {            // WE_HAVE_A_SCALE
                a = d = f2dot14(p);
                p += 2;
            } else if ((flags & 0x0040) != 0) {     // WE_HAVE_AN_X_AND_Y_SCALE
                a = f2dot14(p);
                d = f2dot14(p + 2);
                p += 4;
            } else if ((flags & 0x0080) != 0) {     // WE_HAVE_A_TWO_BY_TWO
                a = f2dot14(p);
                b = f2dot14(p + 2);
                c = f2dot14(p + 4);
                d = f2dot14(p + 6);
                p += 8;
            }
            if (p > limit) {
                throw new Subset.NotSubset();
            }
            Points part = new Points(points.work);
            trueTypePoints(component, part, depth + 1);
            for (int i = 0; i < part.count; i++) {
                double px = part.x[i];
                double py = part.y[i];
                part.x[i] = a * px + c * py;
                part.y[i] = b * px + d * py;
            }
            double dx;
            double dy;
            if (xy) {
                dx = arg1;
                dy = arg2;
                // SCALED_COMPONENT_OFFSET, and not UNSCALED_COMPONENT_OFFSET
                if ((flags & 0x0800) != 0 && (flags & 0x1000) == 0) {
                    double tx = a * dx + c * dy;
                    double ty = b * dx + d * dy;
                    dx = tx;
                    dy = ty;
                }
            } else {
                // The point of the part placed on the point of the glyph
                if (arg1 >= points.count || arg2 >= part.count) {
                    throw new Subset.NotSubset();
                }
                dx = points.x[arg1] - part.x[arg2];
                dy = points.y[arg1] - part.y[arg2];
            }
            int base = points.count;
            for (int i = 0; i < part.count; i++) {
                points.add(part.x[i] + dx, part.y[i] + dy, part.on[i]);
            }
            for (int end : part.ends) {
                points.ends.add(base + end);
            }
            if ((flags & 0x0020) == 0) {            // MORE_COMPONENTS
                break;
            }
        }
    }

    private double f2dot14(int at) throws Subset.NotSubset {
        return ((short) u16(at)) / 16384.0;
    }

    private int byteAt(int at, int limit) throws Subset.NotSubset {
        if (at < 0 || at >= limit) {
            throw new Subset.NotSubset();
        }
        return data[at] & 0xFF;
    }

    private int u16(int at) throws Subset.NotSubset {
        if (at < 0 || at > data.length - 2) {
            throw new Subset.NotSubset();
        }
        return (data[at] & 0xFF) << 8 | (data[at + 1] & 0xFF);
    }

    private int u32(int at) throws Subset.NotSubset {
        if (at < 0 || at > data.length - 4) {
            throw new Subset.NotSubset();
        }
        long value = (data[at] & 0xFFL) << 24 | (data[at + 1] & 0xFF) << 16
                | (data[at + 2] & 0xFF) << 8 | (data[at + 3] & 0xFF);
        if (value > Integer.MAX_VALUE) {
            throw new Subset.NotSubset();
        }
        return (int) value;
    }

    // ------------------------------------------------------------------
    // CFF: the Type 2 charstrings (Adobe Technical Note #5177)

    // The most bytes of charstrings a glyph runs, subroutines and all; and the
    // most glyphs and points a TrueType glyph reads, its parts and theirs.
    private static final int MAX_WORK = 1 << 20;

    /** The interpreter of the charstring of one glyph. */
    private final class CharString {
        private final Pen pen;
        private final CFFSubset.Index local;
        private final double[] stack = new double[48];
        private int sp;
        private final double[] storage = new double[32];
        private int stems;
        private boolean widthDone;
        private boolean ended;
        private double x;
        private double y;
        private int work;
        private final int gid;

        CharString(Pen pen, int gid) {
            this.pen = pen;
            this.gid = gid;
            this.local = (fdOf != null) ? localSubrs[fdOf[gid]] : localSubrs[0];
        }

        private void push(double value) throws Subset.NotSubset {
            if (sp == stack.length) {
                throw new Subset.NotSubset();
            }
            stack[sp++] = value;
        }

        private double pop() throws Subset.NotSubset {
            if (sp == 0) {
                throw new Subset.NotSubset();
            }
            return stack[--sp];
        }

        // The first operator that clears the stack may have the width of the
        // glyph before its operands, which is not drawn.
        private int width(boolean extra) {
            if (!widthDone) {
                widthDone = true;
                if (extra) {
                    return 1;
                }
            }
            return 0;
        }

        private void moveTo(double dx, double dy) {
            x += dx;
            y += dy;
            pen.moveTo(x, y);
        }

        private void lineTo(double dx, double dy) {
            x += dx;
            y += dy;
            pen.lineTo(x, y);
        }

        private void curveTo(double dx1, double dy1, double dx2, double dy2, double dx3, double dy3) {
            double x1 = x + dx1;
            double y1 = y + dy1;
            double x2 = x1 + dx2;
            double y2 = y1 + dy2;
            x = x2 + dx3;
            y = y2 + dy3;
            pen.curveTo(x1, y1, x2, y2, x, y);
        }

        private void need(int count) throws Subset.NotSubset {
            if (sp < count) {
                throw new Subset.NotSubset();
            }
        }

        void run(int from, int to, int depth) throws Subset.NotSubset {
            if (depth > 10) {   // Type 2 nests subroutines 10 deep at most
                throw new Subset.NotSubset();
            }
            work += to - from;
            if (work > MAX_WORK) {
                throw new Subset.NotSubset();
            }
            int i = from;
            while (i < to && !ended) {
                int b0 = data[i] & 0xFF;
                if (b0 == 28) {
                    if (i > to - 3) {
                        throw new Subset.NotSubset();
                    }
                    push((short) ((data[i + 1] & 0xFF) << 8 | (data[i + 2] & 0xFF)));
                    i += 3;
                    continue;
                } else if (b0 >= 32 && b0 <= 246) {
                    push(b0 - 139);
                    i++;
                    continue;
                } else if (b0 >= 247 && b0 <= 250) {
                    if (i > to - 2) {
                        throw new Subset.NotSubset();
                    }
                    push((b0 - 247) * 256 + (data[i + 1] & 0xFF) + 108);
                    i += 2;
                    continue;
                } else if (b0 >= 251 && b0 <= 254) {
                    if (i > to - 2) {
                        throw new Subset.NotSubset();
                    }
                    push(-(b0 - 251) * 256 - (data[i + 1] & 0xFF) - 108);
                    i += 2;
                    continue;
                } else if (b0 == 255) {     // A 16.16 fixed number
                    if (i > to - 5) {
                        throw new Subset.NotSubset();
                    }
                    int fixed = (data[i + 1] & 0xFF) << 24 | (data[i + 2] & 0xFF) << 16
                            | (data[i + 3] & 0xFF) << 8 | (data[i + 4] & 0xFF);
                    push(fixed / 65536.0);
                    i += 5;
                    continue;
                }
                i++;
                int k;
                switch (b0) {
                case 1:     // hstem
                case 3:     // vstem
                case 18:    // hstemhm
                case 23:    // vstemhm
                    width(sp % 2 == 1);
                    stems += sp / 2;
                    sp = 0;
                    break;
                case 19:    // hintmask
                case 20:    // cntrmask
                    // The vstem hints before it, given with no operator
                    width(sp % 2 == 1);
                    stems += sp / 2;
                    sp = 0;
                    i += (stems + 7) / 8;
                    if (i > to) {
                        throw new Subset.NotSubset();
                    }
                    break;
                case 21:    // rmoveto
                    k = width(sp > 2);
                    need(k + 2);
                    moveTo(stack[k], stack[k + 1]);
                    sp = 0;
                    break;
                case 22:    // hmoveto
                    k = width(sp > 1);
                    need(k + 1);
                    moveTo(stack[k], 0);
                    sp = 0;
                    break;
                case 4:     // vmoveto
                    k = width(sp > 1);
                    need(k + 1);
                    moveTo(0, stack[k]);
                    sp = 0;
                    break;
                case 5:     // rlineto
                    for (k = 0; k + 1 < sp; k += 2) {
                        lineTo(stack[k], stack[k + 1]);
                    }
                    sp = 0;
                    break;
                case 6:     // hlineto
                case 7:     // vlineto
                    for (k = 0; k < sp; k++) {
                        if ((k % 2 == 0) == (b0 == 6)) {
                            lineTo(stack[k], 0);
                        } else {
                            lineTo(0, stack[k]);
                        }
                    }
                    sp = 0;
                    break;
                case 8:     // rrcurveto
                    for (k = 0; k + 5 < sp; k += 6) {
                        curveTo(stack[k], stack[k + 1], stack[k + 2], stack[k + 3], stack[k + 4], stack[k + 5]);
                    }
                    sp = 0;
                    break;
                case 24:    // rcurveline
                    for (k = 0; k + 5 < sp - 2; k += 6) {
                        curveTo(stack[k], stack[k + 1], stack[k + 2], stack[k + 3], stack[k + 4], stack[k + 5]);
                    }
                    need(k + 2);
                    lineTo(stack[k], stack[k + 1]);
                    sp = 0;
                    break;
                case 25:    // rlinecurve
                    for (k = 0; k + 1 < sp - 6; k += 2) {
                        lineTo(stack[k], stack[k + 1]);
                    }
                    need(k + 6);
                    curveTo(stack[k], stack[k + 1], stack[k + 2], stack[k + 3], stack[k + 4], stack[k + 5]);
                    sp = 0;
                    break;
                case 26:    // vvcurveto
                    k = 0;
                    double dx1 = 0;
                    if (sp % 2 == 1) {
                        dx1 = stack[0];
                        k = 1;
                    }
                    for (; k + 3 < sp; k += 4) {
                        curveTo(dx1, stack[k], stack[k + 1], stack[k + 2], 0, stack[k + 3]);
                        dx1 = 0;
                    }
                    sp = 0;
                    break;
                case 27:    // hhcurveto
                    k = 0;
                    double dy1 = 0;
                    if (sp % 2 == 1) {
                        dy1 = stack[0];
                        k = 1;
                    }
                    for (; k + 3 < sp; k += 4) {
                        curveTo(stack[k], dy1, stack[k + 1], stack[k + 2], stack[k + 3], 0);
                        dy1 = 0;
                    }
                    sp = 0;
                    break;
                case 30:    // vhcurveto
                case 31: {  // hvcurveto
                    boolean horizontal = (b0 == 31);
                    for (k = 0; k + 3 < sp; k += 4) {
                        double last = (sp - k == 5) ? stack[k + 4] : 0;
                        if (horizontal) {
                            curveTo(stack[k], 0, stack[k + 1], stack[k + 2], last, stack[k + 3]);
                        } else {
                            curveTo(0, stack[k], stack[k + 1], stack[k + 2], stack[k + 3], last);
                        }
                        horizontal = !horizontal;
                    }
                    sp = 0;
                    break;
                }
                case 10:    // callsubr
                case 29: {  // callgsubr
                    CFFSubset.Index subrs = (b0 == 29) ? globalSubrs : local;
                    if (subrs == null) {
                        throw new Subset.NotSubset();
                    }
                    int number = (int) pop() + CFFSubset.bias(subrs.count());
                    if (number < 0 || number >= subrs.count()) {
                        throw new Subset.NotSubset();
                    }
                    run(subrs.objects[number], subrs.objects[number + 1], depth + 1);
                    break;
                }
                case 11:    // return
                    return;
                case 14:    // endchar
                    k = width(sp == 1 || sp == 5);
                    if (sp - k >= 4) {
                        throw new IllegalArgumentException("The CFF glyph number " + gid
                                + " is an accented letter of the seac operator, which drawGlyphs does not draw.");
                    }
                    sp = 0;
                    ended = true;
                    return;
                case 12:
                    if (i >= to) {
                        throw new Subset.NotSubset();
                    }
                    escaped(data[i++] & 0xFF);
                    break;
                default:
                    throw new Subset.NotSubset();   // A reserved operator
                }
            }
        }

        // The operators of two bytes: the flex curves and the arithmetic.
        private void escaped(int b1) throws Subset.NotSubset {
            double a;
            double b;
            switch (b1) {
            case 35:    // flex
                need(13);
                curveTo(stack[0], stack[1], stack[2], stack[3], stack[4], stack[5]);
                curveTo(stack[6], stack[7], stack[8], stack[9], stack[10], stack[11]);
                sp = 0;
                break;
            case 34: {  // hflex
                need(7);
                double startY = y;
                curveTo(stack[0], 0, stack[1], stack[2], stack[3], 0);
                curveTo(stack[4], 0, stack[5], startY - y, stack[6], 0);
                sp = 0;
                break;
            }
            case 36: {  // hflex1
                need(9);
                double startY = y;
                curveTo(stack[0], stack[1], stack[2], stack[3], stack[4], 0);
                curveTo(stack[5], 0, stack[6], stack[7], stack[8], startY - (y + stack[7]));
                sp = 0;
                break;
            }
            case 37: {  // flex1
                need(11);
                double dx = stack[0] + stack[2] + stack[4] + stack[6] + stack[8];
                double dy = stack[1] + stack[3] + stack[5] + stack[7] + stack[9];
                curveTo(stack[0], stack[1], stack[2], stack[3], stack[4], stack[5]);
                if (Math.abs(dx) > Math.abs(dy)) {
                    curveTo(stack[6], stack[7], stack[8], stack[9], stack[10], -dy);
                } else {
                    curveTo(stack[6], stack[7], stack[8], stack[9], -dx, stack[10]);
                }
                sp = 0;
                break;
            }
            case 3:     // and
                b = pop();
                a = pop();
                push((a != 0 && b != 0) ? 1 : 0);
                break;
            case 4:     // or
                b = pop();
                a = pop();
                push((a != 0 || b != 0) ? 1 : 0);
                break;
            case 5:     // not
                push(pop() == 0 ? 1 : 0);
                break;
            case 9:     // abs
                push(Math.abs(pop()));
                break;
            case 10:    // add
                b = pop();
                a = pop();
                push(a + b);
                break;
            case 11:    // sub
                b = pop();
                a = pop();
                push(a - b);
                break;
            case 12:    // div
                b = pop();
                a = pop();
                if (b == 0) {
                    throw new Subset.NotSubset();
                }
                push(a / b);
                break;
            case 14:    // neg
                push(-pop());
                break;
            case 15:    // eq
                b = pop();
                a = pop();
                push(a == b ? 1 : 0);
                break;
            case 18:    // drop
                pop();
                break;
            case 20: {  // put
                int index = (int) pop();
                a = pop();
                if (index < 0 || index >= storage.length) {
                    throw new Subset.NotSubset();
                }
                storage[index] = a;
                break;
            }
            case 21: {  // get
                int index = (int) pop();
                if (index < 0 || index >= storage.length) {
                    throw new Subset.NotSubset();
                }
                push(storage[index]);
                break;
            }
            case 22: {  // ifelse
                double v2 = pop();
                double v1 = pop();
                double s2 = pop();
                double s1 = pop();
                push(v1 <= v2 ? s1 : s2);
                break;
            }
            case 23:    // random, the same number in every port
                push(0.5);
                break;
            case 24:    // mul
                b = pop();
                a = pop();
                push(a * b);
                break;
            case 26:    // sqrt
                push(Math.sqrt(Math.abs(pop())));
                break;
            case 27:    // dup
                a = pop();
                push(a);
                push(a);
                break;
            case 28:    // exch
                b = pop();
                a = pop();
                push(b);
                push(a);
                break;
            case 29: {  // index
                double v = pop();
                // Compared as a number before it is made an integer, which
                // differs from language to language and from processor to
                // processor for one too large
                if (Double.isNaN(v) || v >= sp) {
                    throw new Subset.NotSubset();
                }
                int index = 0;
                if (v > 0) {
                    index = (int) v;
                }
                push(stack[sp - 1 - index]);
                break;
            }
            case 30: {  // roll
                double jv = pop();
                double nv = pop();
                // Compared as numbers before they are made integers, as index
                if (!(nv >= 1 && nv <= sp) || !(Math.abs(jv) <= 1e9)) {
                    throw new Subset.NotSubset();
                }
                int j = (int) jv;
                int n = (int) nv;
                double[] part = new double[n];
                for (int m = 0; m < n; m++) {
                    part[((m + j) % n + n) % n] = stack[sp - n + m];
                }
                System.arraycopy(part, 0, stack, sp - n, n);
                break;
            }
            default:
                // dotsection (0), deprecated, and the reserved ones: no drawing
                sp = 0;
                break;
            }
        }
    }
}   // End of GlyphOutline.java
