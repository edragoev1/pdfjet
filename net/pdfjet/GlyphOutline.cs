/*
 * GlyphOutline.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PDFjet.NET {
/// <summary>
/// The outlines of the glyphs of a font, read from the font file itself: the
/// glyf table of a TrueType font, its quadratic contours drawn exactly as
/// cubic curves, composite glyphs with the transforms of their parts; or the
/// Type 2 charstrings of the CFF table of an OpenType font, with their
/// subroutines, the hints left out. Page.DrawGlyphs draws them as paths.
/// </summary>
/// <remarks>
/// An outline is written as the path operators of PDF in the units of the
/// font, y up, filled with the nonzero winding rule; the matrix takes the
/// units to the em, so that the size of the font times the matrix, moved to
/// the glyph's place, draws it.
/// </remarks>
internal sealed class GlyphOutline {
    // The message of a font whose outlines cannot be read.
    private const String UNREADABLE = "The outlines of the font cannot be read: ";

    /// <summary>An outline: its path operators and the box of its points.</summary>
    internal sealed class Path {
        internal readonly String operators;     // Empty for a glyph that draws nothing, as the space
        internal readonly double xMin;
        internal readonly double yMin;
        internal readonly double xMax;
        internal readonly double yMax;

        internal Path(String operators, double xMin, double yMin, double xMax, double yMax) {
            this.operators = operators;
            this.xMin = xMin;
            this.yMin = yMin;
            this.xMax = xMax;
            this.yMax = yMax;
        }
    }

    // A glyph of the seac operator, which is refused with its own message.
    private sealed class SeacGlyph : Exception {
        internal SeacGlyph(String message) : base(message) {
        }
    }

    private readonly byte[] data;
    private readonly bool cff;
    // The units of the font to the em: a b c d e f
    internal readonly double[] matrix;
    private readonly int numGlyphs;

    // TrueType
    private int glyfOffset;
    private int glyfLength;
    private int locaOffset;
    private int locaLength;
    private bool longOffsets;

    // CFF
    internal CFFSubset.Index charStrings;    // Internal for the tests, which put a charstring of their own
    private CFFSubset.Index globalSubrs;
    private CFFSubset.Index[] localSubrs;   // Of each Font DICT, or of the font
    private int[] fdOf;                     // The Font DICT of each glyph, or null
    private double[][] fdMatrix;            // The FontMatrix of each Font DICT, or null

    /// <summary>
    /// Reads the tables of the outlines of the font program: a TrueType font,
    /// or the CFF table of an OpenType font.
    /// </summary>
    internal GlyphOutline(byte[] data, bool cff) {
        this.data = data;
        this.cff = cff;
        try {
            if (cff) {
                matrix = ReadCFF();
                numGlyphs = charStrings.count();
            } else {
                int unitsPerEm = ReadTrueType();
                matrix = new double[] {1.0 / unitsPerEm, 0, 0, 1.0 / unitsPerEm, 0, 0};
                numGlyphs = (locaLength - (longOffsets ? 4 : 2)) / (longOffsets ? 4 : 2);
            }
        } catch (Exception) {
            throw new ArgumentException(UNREADABLE + "its tables are damaged.");
        }
    }

    // Reads the head, loca and glyf tables, and returns the units per em.
    private int ReadTrueType() {
        int numTables = U16(4);
        int headOffset = -1;
        int locaAt = -1;
        int glyfAt = -1;
        for (int i = 0; i < numTables; i++) {
            int entry = 12 + 16 * i;
            String tag = Encoding.ASCII.GetString(data, entry, 4);
            int offset = U32(entry + 8);
            int length = U32(entry + 12);
            if ((long) offset + length > data.Length) {
                throw new Subset.NotSubset();
            }
            if (tag == "head") {
                headOffset = offset;
            } else if (tag == "loca") {
                locaAt = offset;
                locaLength = length;
            } else if (tag == "glyf") {
                glyfAt = offset;
                glyfLength = length;
            }
        }
        if (headOffset < 0 || locaAt < 0 || glyfAt < 0) {
            throw new Subset.NotSubset();
        }
        int unitsPerEm = U16(headOffset + 18);
        longOffsets = U16(headOffset + 50) == 1;
        locaOffset = locaAt;
        glyfOffset = glyfAt;
        if (unitsPerEm == 0) {
            throw new Subset.NotSubset();
        }
        return unitsPerEm;
    }

    // Reads the CFF table: its charstrings, its subroutines and the Font DICT
    // of each glyph, and returns the FontMatrix of the font.
    private double[] ReadCFF() {
        if (data.Length < 4 || data[0] != 1) {
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
        if (at == null || at.Length != 1) {
            throw new Subset.NotSubset();
        }
        charStrings = CFFSubset.readIndex(data, at[0]);
        double[] topMatrix = MatrixOf(top);
        if (CFFSubset.entryOf(top, CFFSubset.ROS) != null) {
            int[] fdArrayAt = CFFSubset.entryOf(top, CFFSubset.FD_ARRAY);
            int[] fdSelectAt = CFFSubset.entryOf(top, CFFSubset.FD_SELECT);
            if (fdArrayAt == null || fdArrayAt.Length != 1 || fdSelectAt == null || fdSelectAt.Length != 1) {
                throw new Subset.NotSubset();
            }
            CFFSubset.Index fdArray = CFFSubset.readIndex(data, fdArrayAt[0]);
            localSubrs = new CFFSubset.Index[fdArray.count()];
            fdMatrix = new double[fdArray.count()][];
            for (int i = 0; i < fdArray.count(); i++) {
                List<CFFSubset.Entry> fd = CFFSubset.readDict(data, fdArray.objects[i], fdArray.objects[i + 1]);
                localSubrs[i] = SubrsOf(fd);
                fdMatrix[i] = MatrixOf(fd);
            }
            byte[] fdSelect = CFFSubset.block(data, fdSelectAt[0],
                    CFFSubset.fdSelectLength(data, fdSelectAt[0], charStrings.count()));
            fdOf = CFFSubset.fdOf(fdSelect, charStrings.count(), fdArray.count());
            // The glyph space of a CIDFont: the FontMatrix of the glyph's Font
            // DICT and then the Top DICT's; a CIDFont whose Top DICT has none
            // has its Font DICTs' taken as the whole ones, as readers do.
            for (int i = 0; i < fdMatrix.Length; i++) {
                if (fdMatrix[i] == null) {
                    fdMatrix[i] = (topMatrix == null) ? THOUSANDTH : topMatrix;
                } else if (topMatrix != null) {
                    fdMatrix[i] = Multiply(fdMatrix[i], topMatrix);
                }
            }
            return THOUSANDTH;  // Of a glyph, its Font DICT's
        }
        localSubrs = new CFFSubset.Index[] {SubrsOf(top)};
        return (topMatrix == null) ? THOUSANDTH : topMatrix;
    }

    // The default FontMatrix of a CFF font.
    private static readonly double[] THOUSANDTH = {0.001, 0, 0, 0.001, 0, 0};

    // The local subroutines of the Private DICT of the Top DICT or Font DICT.
    private CFFSubset.Index SubrsOf(List<CFFSubset.Entry> dict) {
        int[] p = CFFSubset.entryOf(dict, CFFSubset.PRIVATE);
        if (p == null || p.Length != 2) {
            throw new Subset.NotSubset();
        }
        return CFFSubset.readPrivate(data, p[0], p[1]).subrs;
    }

    // The FontMatrix of the DICT, or null when it has none.
    private static double[] MatrixOf(List<CFFSubset.Entry> dict) {
        foreach (CFFSubset.Entry entry in dict) {
            if (entry.op == CFFSubset.FONT_MATRIX) {
                double[] m = Operands(entry.raw);
                if (m.Length != 6) {
                    throw new Subset.NotSubset();
                }
                foreach (double v in m) {
                    if (!(Math.Abs(v) <= 1000)) {   // NaN too
                        throw new Subset.NotSubset();
                    }
                }
                return m;
            }
        }
        return null;
    }

    // The product of the matrices: a, then b.
    private static double[] Multiply(double[] a, double[] b) {
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
    private static double[] Operands(byte[] raw) {
        List<double> values = new List<double>();
        int i = 0;
        while (i < raw.Length) {
            int b0 = raw[i] & 0xFF;
            if (b0 <= 21) {
                break;  // The operator
            } else if (b0 == 28) {
                values.Add((short) ((raw[i + 1] & 0xFF) << 8 | (raw[i + 2] & 0xFF)));
                i += 3;
            } else if (b0 == 29) {
                values.Add((raw[i + 1] & 0xFF) << 24 | (raw[i + 2] & 0xFF) << 16
                        | (raw[i + 3] & 0xFF) << 8 | (raw[i + 4] & 0xFF));
                i += 5;
            } else if (b0 == 30) {
                StringBuilder sb = new StringBuilder();
                i++;
                bool end = false;
                while (!end && i < raw.Length) {
                    int b = raw[i++] & 0xFF;
                    foreach (int nibble in new int[] {b >> 4, b & 0x0F}) {
                        if (nibble <= 9) {
                            sb.Append((char) ('0' + nibble));
                        } else if (nibble == 0xA) {
                            sb.Append('.');
                        } else if (nibble == 0xB) {
                            sb.Append('E');
                        } else if (nibble == 0xC) {
                            sb.Append("E-");
                        } else if (nibble == 0xE) {
                            sb.Append('-');
                        } else if (nibble == 0xF) {
                            end = true;
                            break;
                        }
                    }
                }
                double value;
                if (!Double.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)) {
                    throw new Subset.NotSubset();
                }
                values.Add(value);
            } else if (b0 >= 32 && b0 <= 246) {
                values.Add(b0 - 139);
                i++;
            } else if (b0 >= 247 && b0 <= 250) {
                values.Add((b0 - 247) * 256 + (raw[i + 1] & 0xFF) + 108);
                i += 2;
            } else if (b0 >= 251 && b0 <= 254) {
                values.Add(-(b0 - 251) * 256 - (raw[i + 1] & 0xFF) - 108);
                i += 2;
            } else {
                throw new Subset.NotSubset();
            }
        }
        return values.ToArray();
    }

    /// <summary>The number of glyphs of the font.</summary>
    internal int Count() {
        return numGlyphs;
    }

    /// <summary>
    /// The matrix that takes the units of the glyph to the em: of the font,
    /// or of the glyph's Font DICT in a CID-keyed CFF font.
    /// </summary>
    internal double[] MatrixOf(int gid) {
        if (fdMatrix != null && gid >= 0 && gid < fdOf.Length) {
            return fdMatrix[fdOf[gid]];
        }
        return matrix;
    }

    /// <summary>The outline of the glyph.</summary>
    internal Path Outline(int gid) {
        if (gid < 0 || gid >= numGlyphs) {
            throw new ArgumentException("The font has no glyph number " + gid + ".");
        }
        Pen pen = new Pen();
        try {
            if (cff) {
                new CharString(this, pen, gid).Run(charStrings.objects[gid], charStrings.objects[gid + 1], 0);
            } else {
                Points points = new Points();
                TrueTypePoints(gid, points, 0);
                points.Draw(pen);
            }
        } catch (SeacGlyph e) {
            throw new ArgumentException(e.Message);
        } catch (Exception) {
            throw new ArgumentException(UNREADABLE + "glyph number " + gid + " is damaged.");
        }
        if (pen.outOfRange) {
            throw new ArgumentException(UNREADABLE + "glyph number " + gid + " is damaged.");
        }
        return pen.ToPath();
    }

    // ------------------------------------------------------------------
    // The path: the operators of PDF, in the units of the font

    // The bound of the points of an outline, in the units of the font: a
    // font's are within ±32,768, and a charstring's arithmetic can make larger
    // ones, or ones not finite, which a path cannot have.
    private const double MAX_COORDINATE = 1e6;

    /// <summary>Writes the path operators and finds the box of the points.</summary>
    internal sealed class Pen {
        private readonly StringBuilder sb = new StringBuilder();
        private double xMin = Double.MaxValue;
        private double yMin = Double.MaxValue;
        private double xMax = -Double.MaxValue;
        private double yMax = -Double.MaxValue;
        private double startX;
        private double startY;
        internal double x;
        internal double y;
        private bool open;      // A contour is begun and has a segment
        internal bool outOfRange;   // A point past MAX_COORDINATE, or not finite

        private void Point(double px, double py) {
            if (!(Math.Abs(px) <= MAX_COORDINATE && Math.Abs(py) <= MAX_COORDINATE)) {
                outOfRange = true;  // NaN too
                px = 0;
                py = 0;
            }
            if (px < xMin) xMin = px;
            if (py < yMin) yMin = py;
            if (px > xMax) xMax = px;
            if (py > yMax) yMax = py;
            sb.Append(Number(px)).Append(' ').Append(Number(py));
        }

        internal void MoveTo(double px, double py) {
            startX = px;
            startY = py;
            x = px;
            y = py;
            open = false;
        }

        // Writes the m of the contour before its first segment, so that a
        // contour of a moveto alone writes nothing.
        private void Begin() {
            if (!open) {
                Point(startX, startY);
                sb.Append(" m\n");
                open = true;
            }
        }

        internal void LineTo(double px, double py) {
            Begin();
            Point(px, py);
            sb.Append(" l\n");
            x = px;
            y = py;
        }

        internal void CurveTo(double x1, double y1, double x2, double y2, double x3, double y3) {
            Begin();
            Point(x1, y1);
            sb.Append(' ');
            Point(x2, y2);
            sb.Append(' ');
            Point(x3, y3);
            sb.Append(" c\n");
            x = x3;
            y = y3;
        }

        // The quadratic curve from the current point drawn exactly as a cubic one.
        internal void QuadTo(double qx, double qy, double px, double py) {
            CurveTo(x + 2.0 / 3.0 * (qx - x), y + 2.0 / 3.0 * (qy - y),
                    px + 2.0 / 3.0 * (qx - px), py + 2.0 / 3.0 * (qy - py),
                    px, py);
        }

        internal Path ToPath() {
            if (sb.Length == 0) {
                return new Path("", 0, 0, 0, 0);
            }
            return new Path(sb.ToString(), xMin, yMin, xMax, yMax);
        }
    }

    /// <summary>
    /// A number of a path, in the units of the font: whole units, the same in
    /// the four ports, rounded half away from zero. The font's own points are
    /// whole units; the control points of a quadratic curve drawn as a cubic
    /// one are not, and move by at most half a unit, 0.05% of the em, unseen
    /// at any size, for outlines 15% smaller (9.2.0).
    /// </summary>
    internal static String Number(double value) {
        return Decimals(value, 0);
    }

    /// <summary>The value with at most the decimals given, rounded half away from zero.</summary>
    internal static String Decimals(double value, int decimals) {
        long scale = 1;
        for (int i = 0; i < decimals; i++) {
            scale *= 10;
        }
        long n = (long) Math.Floor(Math.Abs(value) * scale + 0.5);
        if (n == 0) {
            return "0";
        }
        StringBuilder sb = new StringBuilder();
        if (value < 0) {
            sb.Append('-');
        }
        sb.Append((n / scale).ToString(CultureInfo.InvariantCulture));
        long fraction = n % scale;
        if (fraction != 0) {
            String digits = (fraction + scale).ToString(CultureInfo.InvariantCulture).Substring(1);
            int end = digits.Length;
            while (digits[end - 1] == '0') {
                end--;
            }
            sb.Append('.').Append(digits, 0, end);
        }
        return sb.ToString();
    }

    // ------------------------------------------------------------------
    // TrueType

    /// <summary>The points of a glyph, with the last point of each contour.</summary>
    internal sealed class Points {
        internal double[] x = new double[64];
        internal double[] y = new double[64];
        internal bool[] on = new bool[64];
        internal int count;
        internal List<int> ends = new List<int>();
        internal int[] work = new int[1];   // The glyphs and points read for the outline, its parts' too

        internal void Add(double px, double py, bool onCurve) {
            if (count == x.Length) {
                Array.Resize(ref x, 2 * count);
                Array.Resize(ref y, 2 * count);
                Array.Resize(ref on, 2 * count);
            }
            x[count] = px;
            y[count] = py;
            on[count] = onCurve;
            count++;
        }

        // Draws the contours: the points on the curve joined by lines, and by
        // quadratic curves through the points off it, with a point on the
        // curve halfway between two off it.
        internal void Draw(Pen pen) {
            int first = 0;
            foreach (int end in ends) {
                int n = end - first + 1;
                if (n >= 2) {
                    Contour(pen, first, n);
                }
                first = end + 1;
            }
        }

        private void Contour(Pen pen, int first, int n) {
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
            pen.MoveTo(sx, sy);
            bool off = false;
            double qx = 0;
            double qy = 0;
            for (int k = 0; k < steps; k++) {
                int i = first + (from + k) % n;
                if (on[i]) {
                    if (off) {
                        pen.QuadTo(qx, qy, x[i], y[i]);
                    } else {
                        pen.LineTo(x[i], y[i]);
                    }
                    off = false;
                } else {
                    if (off) {
                        pen.QuadTo(qx, qy, (qx + x[i]) / 2, (qy + y[i]) / 2);
                    }
                    qx = x[i];
                    qy = y[i];
                    off = true;
                }
            }
            // Back to the start: a curve, or the line that filling draws
            if (off) {
                pen.QuadTo(qx, qy, sx, sy);
            }
        }
    }

    // Adds the points of the glyph, a simple or a composite one, to the points.
    private void TrueTypePoints(int gid, Points points, int depth) {
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
            start = U32(locaOffset + 4 * gid);
            end = U32(locaOffset + 4 * gid + 4);
        } else {
            start = 2 * U16(locaOffset + 2 * gid);
            end = 2 * U16(locaOffset + 2 * gid + 2);
        }
        if (start == end) {
            return;     // No outline, as the space
        }
        if (start > end || end > glyfLength || end - start < 10) {
            throw new Subset.NotSubset();
        }
        int at = glyfOffset + start;
        int limit = glyfOffset + end;
        int contours = (short) U16(at);
        if (contours >= 0) {
            SimplePoints(at, limit, contours, points);
        } else {
            CompositePoints(at + 10, limit, points, depth);
        }
    }

    private void SimplePoints(int at, int limit, int contours, Points points) {
        int p = at + 10;
        int[] ends = new int[contours];
        int n = 0;
        for (int i = 0; i < contours; i++) {
            ends[i] = U16(p + 2 * i);
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
        int instructions = U16(p);
        p += 2 + instructions;
        byte[] flags = new byte[n];
        for (int i = 0; i < n; ) {
            int flag = ByteAt(p++, limit);
            flags[i++] = (byte) flag;
            if ((flag & 0x08) != 0) {   // REPEAT_FLAG
                int repeat = ByteAt(p++, limit);
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
                int dx = ByteAt(p++, limit);
                value += (flag & 0x10) != 0 ? dx : -dx;
            } else if ((flag & 0x10) == 0) {    // Not X_IS_SAME
                value += (short) (ByteAt(p, limit) << 8 | ByteAt(p + 1, limit));
                p += 2;
            }
            xs[i] = value;
        }
        value = 0;
        int baseIndex = points.count;
        for (int i = 0; i < n; i++) {
            int flag = flags[i];
            if ((flag & 0x04) != 0) {           // Y_SHORT_VECTOR
                int dy = ByteAt(p++, limit);
                value += (flag & 0x20) != 0 ? dy : -dy;
            } else if ((flag & 0x20) == 0) {    // Not Y_IS_SAME
                value += (short) (ByteAt(p, limit) << 8 | ByteAt(p + 1, limit));
                p += 2;
            }
            points.Add(xs[i], value, (flag & 0x01) != 0);
        }
        foreach (int end in ends) {
            points.ends.Add(baseIndex + end);
        }
    }

    private void CompositePoints(int p, int limit, Points points, int depth) {
        while (true) {
            int flags = U16(p);
            int component = U16(p + 2);
            p += 4;
            if (component >= numGlyphs) {
                throw new Subset.NotSubset();
            }
            int arg1;
            int arg2;
            bool xy = (flags & 0x0002) != 0;        // ARGS_ARE_XY_VALUES
            if ((flags & 0x0001) != 0) {            // ARG_1_AND_2_ARE_WORDS
                arg1 = U16(p);
                arg2 = U16(p + 2);
                if (xy) {
                    arg1 = (short) arg1;
                    arg2 = (short) arg2;
                }
                p += 4;
            } else {
                arg1 = ByteAt(p, limit);
                arg2 = ByteAt(p + 1, limit);
                if (xy) {
                    arg1 = (sbyte) arg1;
                    arg2 = (sbyte) arg2;
                }
                p += 2;
            }
            double a = 1;
            double b = 0;
            double c = 0;
            double d = 1;
            if ((flags & 0x0008) != 0) {            // WE_HAVE_A_SCALE
                a = d = F2Dot14(p);
                p += 2;
            } else if ((flags & 0x0040) != 0) {     // WE_HAVE_AN_X_AND_Y_SCALE
                a = F2Dot14(p);
                d = F2Dot14(p + 2);
                p += 4;
            } else if ((flags & 0x0080) != 0) {     // WE_HAVE_A_TWO_BY_TWO
                a = F2Dot14(p);
                b = F2Dot14(p + 2);
                c = F2Dot14(p + 4);
                d = F2Dot14(p + 6);
                p += 8;
            }
            if (p > limit) {
                throw new Subset.NotSubset();
            }
            Points part = new Points();
            part.work = points.work;
            TrueTypePoints(component, part, depth + 1);
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
            int baseIndex = points.count;
            for (int i = 0; i < part.count; i++) {
                points.Add(part.x[i] + dx, part.y[i] + dy, part.on[i]);
            }
            foreach (int end in part.ends) {
                points.ends.Add(baseIndex + end);
            }
            if ((flags & 0x0020) == 0) {            // MORE_COMPONENTS
                break;
            }
        }
    }

    private double F2Dot14(int at) {
        return ((short) U16(at)) / 16384.0;
    }

    private int ByteAt(int at, int limit) {
        if (at < 0 || at >= limit) {
            throw new Subset.NotSubset();
        }
        return data[at] & 0xFF;
    }

    private int U16(int at) {
        if (at < 0 || at > data.Length - 2) {
            throw new Subset.NotSubset();
        }
        return (data[at] & 0xFF) << 8 | (data[at + 1] & 0xFF);
    }

    private int U32(int at) {
        if (at < 0 || at > data.Length - 4) {
            throw new Subset.NotSubset();
        }
        long value = (long) (data[at] & 0xFF) << 24 | (long) (data[at + 1] & 0xFF) << 16
                | (long) (data[at + 2] & 0xFF) << 8 | (long) (data[at + 3] & 0xFF);
        if (value > Int32.MaxValue) {
            throw new Subset.NotSubset();
        }
        return (int) value;
    }

    // ------------------------------------------------------------------
    // CFF: the Type 2 charstrings (Adobe Technical Note #5177)

    // The most bytes of charstrings a glyph runs, subroutines and all; and the
    // most glyphs and points a TrueType glyph reads, its parts and theirs.
    private const int MAX_WORK = 1 << 20;

    /// <summary>The interpreter of the charstring of one glyph.</summary>
    private sealed class CharString {
        private readonly GlyphOutline outline;
        private readonly byte[] data;
        private readonly Pen pen;
        private readonly CFFSubset.Index local;
        private readonly double[] stack = new double[48];
        private int sp;
        private readonly double[] storage = new double[32];
        private int stems;
        private bool widthDone;
        private bool ended;
        private double x;
        private double y;
        private int work;
        private readonly int gid;

        internal CharString(GlyphOutline outline, Pen pen, int gid) {
            this.outline = outline;
            this.data = outline.data;
            this.pen = pen;
            this.gid = gid;
            this.local = (outline.fdOf != null) ? outline.localSubrs[outline.fdOf[gid]] : outline.localSubrs[0];
        }

        private void Push(double value) {
            if (sp == stack.Length) {
                throw new Subset.NotSubset();
            }
            stack[sp++] = value;
        }

        private double Pop() {
            if (sp == 0) {
                throw new Subset.NotSubset();
            }
            return stack[--sp];
        }

        // The first operator that clears the stack may have the width of the
        // glyph before its operands, which is not drawn.
        private int Width(bool extra) {
            if (!widthDone) {
                widthDone = true;
                if (extra) {
                    return 1;
                }
            }
            return 0;
        }

        private void MoveTo(double dx, double dy) {
            x += dx;
            y += dy;
            pen.MoveTo(x, y);
        }

        private void LineTo(double dx, double dy) {
            x += dx;
            y += dy;
            pen.LineTo(x, y);
        }

        private void CurveTo(double dx1, double dy1, double dx2, double dy2, double dx3, double dy3) {
            double x1 = x + dx1;
            double y1 = y + dy1;
            double x2 = x1 + dx2;
            double y2 = y1 + dy2;
            x = x2 + dx3;
            y = y2 + dy3;
            pen.CurveTo(x1, y1, x2, y2, x, y);
        }

        private void Need(int count) {
            if (sp < count) {
                throw new Subset.NotSubset();
            }
        }

        internal void Run(int from, int to, int depth) {
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
                    Push((short) ((data[i + 1] & 0xFF) << 8 | (data[i + 2] & 0xFF)));
                    i += 3;
                    continue;
                } else if (b0 >= 32 && b0 <= 246) {
                    Push(b0 - 139);
                    i++;
                    continue;
                } else if (b0 >= 247 && b0 <= 250) {
                    if (i > to - 2) {
                        throw new Subset.NotSubset();
                    }
                    Push((b0 - 247) * 256 + (data[i + 1] & 0xFF) + 108);
                    i += 2;
                    continue;
                } else if (b0 >= 251 && b0 <= 254) {
                    if (i > to - 2) {
                        throw new Subset.NotSubset();
                    }
                    Push(-(b0 - 251) * 256 - (data[i + 1] & 0xFF) - 108);
                    i += 2;
                    continue;
                } else if (b0 == 255) {     // A 16.16 fixed number
                    if (i > to - 5) {
                        throw new Subset.NotSubset();
                    }
                    int fixedNumber = (data[i + 1] & 0xFF) << 24 | (data[i + 2] & 0xFF) << 16
                            | (data[i + 3] & 0xFF) << 8 | (data[i + 4] & 0xFF);
                    Push(fixedNumber / 65536.0);
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
                    Width(sp % 2 == 1);
                    stems += sp / 2;
                    sp = 0;
                    break;
                case 19:    // hintmask
                case 20:    // cntrmask
                    // The vstem hints before it, given with no operator
                    Width(sp % 2 == 1);
                    stems += sp / 2;
                    sp = 0;
                    i += (stems + 7) / 8;
                    if (i > to) {
                        throw new Subset.NotSubset();
                    }
                    break;
                case 21:    // rmoveto
                    k = Width(sp > 2);
                    Need(k + 2);
                    MoveTo(stack[k], stack[k + 1]);
                    sp = 0;
                    break;
                case 22:    // hmoveto
                    k = Width(sp > 1);
                    Need(k + 1);
                    MoveTo(stack[k], 0);
                    sp = 0;
                    break;
                case 4:     // vmoveto
                    k = Width(sp > 1);
                    Need(k + 1);
                    MoveTo(0, stack[k]);
                    sp = 0;
                    break;
                case 5:     // rlineto
                    for (k = 0; k + 1 < sp; k += 2) {
                        LineTo(stack[k], stack[k + 1]);
                    }
                    sp = 0;
                    break;
                case 6:     // hlineto
                case 7:     // vlineto
                    for (k = 0; k < sp; k++) {
                        if ((k % 2 == 0) == (b0 == 6)) {
                            LineTo(stack[k], 0);
                        } else {
                            LineTo(0, stack[k]);
                        }
                    }
                    sp = 0;
                    break;
                case 8:     // rrcurveto
                    for (k = 0; k + 5 < sp; k += 6) {
                        CurveTo(stack[k], stack[k + 1], stack[k + 2], stack[k + 3], stack[k + 4], stack[k + 5]);
                    }
                    sp = 0;
                    break;
                case 24:    // rcurveline
                    for (k = 0; k + 5 < sp - 2; k += 6) {
                        CurveTo(stack[k], stack[k + 1], stack[k + 2], stack[k + 3], stack[k + 4], stack[k + 5]);
                    }
                    Need(k + 2);
                    LineTo(stack[k], stack[k + 1]);
                    sp = 0;
                    break;
                case 25:    // rlinecurve
                    for (k = 0; k + 1 < sp - 6; k += 2) {
                        LineTo(stack[k], stack[k + 1]);
                    }
                    Need(k + 6);
                    CurveTo(stack[k], stack[k + 1], stack[k + 2], stack[k + 3], stack[k + 4], stack[k + 5]);
                    sp = 0;
                    break;
                case 26: {  // vvcurveto
                    k = 0;
                    double dx1 = 0;
                    if (sp % 2 == 1) {
                        dx1 = stack[0];
                        k = 1;
                    }
                    for (; k + 3 < sp; k += 4) {
                        CurveTo(dx1, stack[k], stack[k + 1], stack[k + 2], 0, stack[k + 3]);
                        dx1 = 0;
                    }
                    sp = 0;
                    break;
                }
                case 27: {  // hhcurveto
                    k = 0;
                    double dy1 = 0;
                    if (sp % 2 == 1) {
                        dy1 = stack[0];
                        k = 1;
                    }
                    for (; k + 3 < sp; k += 4) {
                        CurveTo(stack[k], dy1, stack[k + 1], stack[k + 2], stack[k + 3], 0);
                        dy1 = 0;
                    }
                    sp = 0;
                    break;
                }
                case 30:    // vhcurveto
                case 31: {  // hvcurveto
                    bool horizontal = (b0 == 31);
                    for (k = 0; k + 3 < sp; k += 4) {
                        double last = (sp - k == 5) ? stack[k + 4] : 0;
                        if (horizontal) {
                            CurveTo(stack[k], 0, stack[k + 1], stack[k + 2], last, stack[k + 3]);
                        } else {
                            CurveTo(0, stack[k], stack[k + 1], stack[k + 2], stack[k + 3], last);
                        }
                        horizontal = !horizontal;
                    }
                    sp = 0;
                    break;
                }
                case 10:    // callsubr
                case 29: {  // callgsubr
                    CFFSubset.Index subrs = (b0 == 29) ? outline.globalSubrs : local;
                    if (subrs == null) {
                        throw new Subset.NotSubset();
                    }
                    // Compared as a number before it is made an integer, as put and get
                    double n = Pop() + CFFSubset.bias(subrs.count());
                    if (!(n >= 0 && n < subrs.count())) {   // NaN too
                        throw new Subset.NotSubset();
                    }
                    int number = (int) n;
                    Run(subrs.objects[number], subrs.objects[number + 1], depth + 1);
                    break;
                }
                case 11:    // return
                    return;
                case 14:    // endchar
                    k = Width(sp == 1 || sp == 5);
                    if (sp - k >= 4) {
                        throw new SeacGlyph("The CFF glyph number " + gid
                                + " is an accented letter of the seac operator, which DrawGlyphs does not draw.");
                    }
                    sp = 0;
                    ended = true;
                    return;
                case 12:
                    if (i >= to) {
                        throw new Subset.NotSubset();
                    }
                    Escaped(data[i++] & 0xFF);
                    break;
                default:
                    throw new Subset.NotSubset();   // A reserved operator
                }
            }
        }

        // The operators of two bytes: the flex curves and the arithmetic.
        private void Escaped(int b1) {
            double a;
            double b;
            switch (b1) {
            case 35:    // flex
                Need(13);
                CurveTo(stack[0], stack[1], stack[2], stack[3], stack[4], stack[5]);
                CurveTo(stack[6], stack[7], stack[8], stack[9], stack[10], stack[11]);
                sp = 0;
                break;
            case 34: {  // hflex
                Need(7);
                double startY = y;
                CurveTo(stack[0], 0, stack[1], stack[2], stack[3], 0);
                CurveTo(stack[4], 0, stack[5], startY - y, stack[6], 0);
                sp = 0;
                break;
            }
            case 36: {  // hflex1
                Need(9);
                double startY = y;
                CurveTo(stack[0], stack[1], stack[2], stack[3], stack[4], 0);
                CurveTo(stack[5], 0, stack[6], stack[7], stack[8], startY - (y + stack[7]));
                sp = 0;
                break;
            }
            case 37: {  // flex1
                Need(11);
                double dx = stack[0] + stack[2] + stack[4] + stack[6] + stack[8];
                double dy = stack[1] + stack[3] + stack[5] + stack[7] + stack[9];
                CurveTo(stack[0], stack[1], stack[2], stack[3], stack[4], stack[5]);
                if (Math.Abs(dx) > Math.Abs(dy)) {
                    CurveTo(stack[6], stack[7], stack[8], stack[9], stack[10], -dy);
                } else {
                    CurveTo(stack[6], stack[7], stack[8], stack[9], -dx, stack[10]);
                }
                sp = 0;
                break;
            }
            case 3:     // and
                b = Pop();
                a = Pop();
                Push((a != 0 && b != 0) ? 1 : 0);
                break;
            case 4:     // or
                b = Pop();
                a = Pop();
                Push((a != 0 || b != 0) ? 1 : 0);
                break;
            case 5:     // not
                Push(Pop() == 0 ? 1 : 0);
                break;
            case 9:     // abs
                Push(Math.Abs(Pop()));
                break;
            case 10:    // add
                b = Pop();
                a = Pop();
                Push(a + b);
                break;
            case 11:    // sub
                b = Pop();
                a = Pop();
                Push(a - b);
                break;
            case 12:    // div
                b = Pop();
                a = Pop();
                if (b == 0) {
                    throw new Subset.NotSubset();
                }
                Push(a / b);
                break;
            case 14:    // neg
                Push(-Pop());
                break;
            case 15:    // eq
                b = Pop();
                a = Pop();
                Push(a == b ? 1 : 0);
                break;
            case 18:    // drop
                Pop();
                break;
            case 20: {  // put
                // Compared as a number before it is made an integer, as index
                double index = Pop();
                a = Pop();
                if (!(index >= 0 && index < storage.Length)) { // NaN too
                    throw new Subset.NotSubset();
                }
                storage[(int) index] = a;
                break;
            }
            case 21: {  // get
                double index = Pop();
                if (!(index >= 0 && index < storage.Length)) { // NaN too
                    throw new Subset.NotSubset();
                }
                Push(storage[(int) index]);
                break;
            }
            case 22: {  // ifelse
                double v2 = Pop();
                double v1 = Pop();
                double s2 = Pop();
                double s1 = Pop();
                Push(v1 <= v2 ? s1 : s2);
                break;
            }
            case 23:    // random, the same number in every port
                Push(0.5);
                break;
            case 24:    // mul
                b = Pop();
                a = Pop();
                Push(a * b);
                break;
            case 26:    // sqrt
                Push(Math.Sqrt(Math.Abs(Pop())));
                break;
            case 27:    // dup
                a = Pop();
                Push(a);
                Push(a);
                break;
            case 28:    // exch
                b = Pop();
                a = Pop();
                Push(b);
                Push(a);
                break;
            case 29: {  // index
                double v = Pop();
                // Compared as a number before it is made an integer, which
                // differs from language to language and from processor to
                // processor for one too large
                if (Double.IsNaN(v) || v >= sp) {
                    throw new Subset.NotSubset();
                }
                int index = 0;
                if (v > 0) {
                    index = (int) v;
                }
                Push(stack[sp - 1 - index]);
                break;
            }
            case 30: {  // roll
                double jv = Pop();
                double nv = Pop();
                // Compared as numbers before they are made integers, as index
                if (!(nv >= 1 && nv <= sp) || !(Math.Abs(jv) <= 1e9)) {
                    throw new Subset.NotSubset();
                }
                int j = (int) jv;
                int n = (int) nv;
                double[] part = new double[n];
                for (int m = 0; m < n; m++) {
                    part[((m + j) % n + n) % n] = stack[sp - n + m];
                }
                Array.Copy(part, 0, stack, sp - n, n);
                break;
            }
            default:
                // dotsection (0), deprecated, and the reserved ones: no drawing
                sp = 0;
                break;
            }
        }
    }
}
}   // End of GlyphOutline.cs
