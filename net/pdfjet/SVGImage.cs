/*
 * SVGImage.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace PDFjet.NET {
/// <summary>
/// Used to embed SVG images in the PDF document.
/// </summary>
public class SVGImage : IDrawable {
    float x = 0f;
    float y = 0f;
    float w = 0f;       // SVG width
    float h = 0f;       // SVG height
    String viewBox = null;
    String preserveAspectRatio = null;

    internal List<SVGPath> paths = null;    // Internal for the tests
    /// <summary>The URI opened when the image is clicked.</summary>
    protected String uri = null;
    /// <summary>The destination key used by the GoTo action.</summary>
    protected String key = null;
    private String language = null;
    private String actualText = null;
    private String altDescription = null;

    /// <summary>
    /// Used to embed SVG images in the PDF document.
    /// </summary>
    /// <param name="svgPath">the path to the SVG file.</param>
    public SVGImage(String svgPath) {
        using (FileStream stream = new FileStream(svgPath, FileMode.Open, FileAccess.Read)) {
            Read(stream);
        }
    }

    /// <summary>
    /// Used to embed SVG images in the PDF document.
    /// </summary>
    /// <param name="stream">the input stream.</param>
    public SVGImage(Stream stream) {
        Read(stream);
    }

    // The elements whose content is not drawn where it is, but used by other
    // elements, which PDFjet does not draw.
    private static readonly HashSet<String> TEMPLATES = new HashSet<String> {
        "defs", "clipPath", "mask", "pattern", "symbol",
        "marker", "linearGradient", "radialGradient",
    };

    // Draws the <path>, <rect>, <circle>, <ellipse>, <line>, <polyline> and
    // <polygon> elements, in groups or not, with their transforms, and the
    // fill, stroke, stroke-width, fill-rule, stroke-linecap, stroke-linejoin,
    // opacity, fill-opacity and stroke-opacity they have or inherit, as
    // attributes, in a style attribute or in rules for their classes in a
    // <style> element that comes before them. The width, height, viewBox and
    // preserveAspectRatio of the <svg> element give the size of the image.
    private void Read(Stream stream) {
        paths = new List<SVGPath>();
        List<SVGRule> rules = new List<SVGRule>();
        List<SVGState> stack = new List<SVGState>();
        stack.Add(new SVGState());
        List<String> names = new List<String>();
        StringBuilder styleSheet = new StringBuilder();
        bool root = true;

        XmlReaderSettings settings = new XmlReaderSettings();
        // Disable DTD and external entity processing to prevent XXE attacks.
        settings.DtdProcessing = DtdProcessing.Ignore;
        settings.XmlResolver = null;

        XmlReader reader = XmlReader.Create(stream, settings);
        try {
            while (reader.Read()) {
                if (reader.NodeType == XmlNodeType.Element) {
                    String name = reader.LocalName;
                    bool empty = reader.IsEmptyElement;
                    // Only the attributes of no namespace are presentation
                    // attributes, in the order of the element.
                    Dictionary<String, String> attributes = new Dictionary<String, String>();
                    List<String> order = new List<String>();
                    while (reader.MoveToNextAttribute()) {
                        if (reader.NamespaceURI.Equals("")) {
                            attributes[reader.LocalName] = reader.Value;
                            order.Add(reader.LocalName);
                        }
                    }
                    reader.MoveToElement();

                    SVGState state = ElementState(stack[stack.Count - 1], rules, attributes, order);
                    if (TEMPLATES.Contains(name)) {
                        state.hidden = true;
                    }
                    stack.Add(state);
                    names.Add(name);

                    if (name.Equals("svg") && root) {
                        root = false;
                        this.w = ParseLength(Attribute(attributes, "width"));
                        this.h = ParseLength(Attribute(attributes, "height"));
                        attributes.TryGetValue("viewBox", out this.viewBox);
                        attributes.TryGetValue("preserveAspectRatio", out this.preserveAspectRatio);
                    }
                    List<PathOp> operations = ShapeOperations(name, attributes);
                    if (name.Equals("line")) {
                        state.fill = Color.transparent;     // A line has no inside to fill
                        state.fillCurrent = false;
                    }
                    AddPath(state, operations);
                    if (empty) {
                        EndElement(stack, names, rules, styleSheet);
                    }
                } else if (reader.NodeType == XmlNodeType.EndElement) {
                    EndElement(stack, names, rules, styleSheet);
                } else if (reader.NodeType == XmlNodeType.Text ||
                        reader.NodeType == XmlNodeType.CDATA ||
                        reader.NodeType == XmlNodeType.Whitespace ||
                        reader.NodeType == XmlNodeType.SignificantWhitespace) {
                    if (names.Count > 0 && names[names.Count - 1].Equals("style")) {
                        styleSheet.Append(reader.Value);
                    }
                }
            }
        } finally {
            // Close only the reader we created. The caller remains
            // responsible for the underlying stream.
            reader.Close();
        }

        ProcessPaths(paths);
    }

    // Ends an element: the rules of a <style> element are added to those of
    // the style sheet, and the state of the element is left.
    private static void EndElement(
            List<SVGState> stack, List<String> names, List<SVGRule> rules, StringBuilder styleSheet) {
        if (names.Count > 0 && names[names.Count - 1].Equals("style")) {
            rules.AddRange(SVGRule.ParseStyleSheet(styleSheet.ToString()));
            styleSheet.Length = 0;
        }
        if (stack.Count > 1) {
            stack.RemoveAt(stack.Count - 1);
            names.RemoveAt(names.Count - 1);
        }
    }

    // Returns the value of the attribute, and "" for one the element does not have.
    private static String Attribute(Dictionary<String, String> attributes, String name) {
        String value;
        return attributes.TryGetValue(name, out value) ? value : "";
    }

    // Returns the state of an element whose parent has the given one: its
    // presentation attributes, in their order, then the rules of the style
    // sheet for its classes, in the order of the style sheet, then its style
    // attribute, and its transform.
    private static SVGState ElementState(SVGState parent, List<SVGRule> rules,
            Dictionary<String, String> attributes, List<String> order) {
        SVGState state = parent.Copy();
        state.ownOpacity = 1f;
        foreach (String name in order) {
            state.SetProperty(name, attributes[name]);
        }
        String[] classes = Attribute(attributes, "class").Split(
                default(char[]), StringSplitOptions.RemoveEmptyEntries);
        if (classes.Length > 0) {
            foreach (SVGRule rule in rules) {
                foreach (String name in classes) {
                    if (!name.Equals(rule.name)) {
                        continue;
                    }
                    foreach (String[] declaration in rule.declarations) {
                        state.SetProperty(declaration[0], declaration[1]);
                    }
                    break;
                }
            }
        }
        foreach (String[] declaration in SVGRule.ParseDeclarations(Attribute(attributes, "style"))) {
            state.SetProperty(declaration[0], declaration[1]);
        }
        state.opacity *= state.ownOpacity;
        String transform;
        if (attributes.TryGetValue("transform", out transform)) {
            double[] matrix = SVGState.ParseTransform(transform);
            if (matrix != null) {
                state.matrix = SVGState.Multiply(parent.matrix, matrix);
            }
        }
        return state;
    }

    // How far the control points of the cubic curve that draws a quarter of a
    // circle are from its ends, for a radius of 1.
    private const double KAPPA = 0.5522847498307936;

    // Builds the PDF path operations of a shape.
    private class Builder {
        internal List<PathOp> operations = new List<PathOp>();
        private double x0 = 0.0;    // The start of the subpath
        private double y0 = 0.0;

        internal void MoveTo(double x, double y) {
            x0 = x;
            y0 = y;
            operations.Add(new PathOp('M', (float) x, (float) y));
        }

        internal void LineTo(double x, double y) {
            operations.Add(new PathOp('L', (float) x, (float) y));
        }

        internal void CurveTo(double x1, double y1, double x2, double y2, double x, double y) {
            operations.Add(new PathOp('C').SetCubicPoints(
                    (float) x1, (float) y1, (float) x2, (float) y2, (float) x, (float) y));
        }

        internal void ClosePath() {
            operations.Add(new PathOp('Z', (float) x0, (float) y0));
        }

        // Adds the ellipse as four cubic curves, from its right end.
        internal void Ellipse(double cx, double cy, double rx, double ry) {
            double kx = KAPPA * rx;
            double ky = KAPPA * ry;
            MoveTo(cx + rx, cy);
            CurveTo(cx + rx, cy + ky, cx + kx, cy + ry, cx, cy + ry);
            CurveTo(cx - kx, cy + ry, cx - rx, cy + ky, cx - rx, cy);
            CurveTo(cx - rx, cy - ky, cx - kx, cy - ry, cx, cy - ry);
            CurveTo(cx + kx, cy - ry, cx + rx, cy - ky, cx + rx, cy);
            ClosePath();
        }
    }

    // Returns the PDF path operations of an element that draws a path or a
    // basic shape, in its user space, and none for another element or a shape
    // of no size. Throws for path data that is not numbers.
    private static List<PathOp> ShapeOperations(String name, Dictionary<String, String> attributes) {
        Builder b = new Builder();
        if (name.Equals("path")) {
            return SVG.ToPDF(SVG.GetOperations(Attribute(attributes, "d")));
        } else if (name.Equals("rect")) {
            double x = Number(attributes, "x");
            double y = Number(attributes, "y");
            double w = Number(attributes, "width");
            double h = Number(attributes, "height");
            if (w <= 0.0 || h <= 0.0) {
                return b.operations;
            }
            // A radius that is not given is the other one, and neither is more
            // than half the side.
            double rx;
            double ry;
            bool rxSet = Radius(Attribute(attributes, "rx"), out rx);
            bool rySet = Radius(Attribute(attributes, "ry"), out ry);
            if (!rxSet) {
                rx = ry;
            }
            if (!rySet) {
                ry = rx;
            }
            rx = Math.Min(rx, w/2.0);
            ry = Math.Min(ry, h/2.0);
            if (rx <= 0.0 || ry <= 0.0) {
                b.MoveTo(x, y);
                b.LineTo(x + w, y);
                b.LineTo(x + w, y + h);
                b.LineTo(x, y + h);
                b.ClosePath();
                return b.operations;
            }
            double kx = KAPPA * rx;
            double ky = KAPPA * ry;
            b.MoveTo(x + rx, y);
            b.LineTo(x + w - rx, y);
            b.CurveTo(x + w - rx + kx, y, x + w, y + ry - ky, x + w, y + ry);
            b.LineTo(x + w, y + h - ry);
            b.CurveTo(x + w, y + h - ry + ky, x + w - rx + kx, y + h, x + w - rx, y + h);
            b.LineTo(x + rx, y + h);
            b.CurveTo(x + rx - kx, y + h, x, y + h - ry + ky, x, y + h - ry);
            b.LineTo(x, y + ry);
            b.CurveTo(x, y + ry - ky, x + rx - kx, y, x + rx, y);
            b.ClosePath();
        } else if (name.Equals("circle")) {
            double r = Number(attributes, "r");
            if (r <= 0.0) {
                return b.operations;
            }
            b.Ellipse(Number(attributes, "cx"), Number(attributes, "cy"), r, r);
        } else if (name.Equals("ellipse")) {
            double rx = Number(attributes, "rx");
            double ry = Number(attributes, "ry");
            if (rx <= 0.0 || ry <= 0.0) {
                return b.operations;
            }
            b.Ellipse(Number(attributes, "cx"), Number(attributes, "cy"), rx, ry);
        } else if (name.Equals("line")) {
            b.MoveTo(Number(attributes, "x1"), Number(attributes, "y1"));
            b.LineTo(Number(attributes, "x2"), Number(attributes, "y2"));
        } else if (name.Equals("polyline") || name.Equals("polygon")) {
            List<double> points = SVGState.ParseNumbers(Attribute(attributes, "points"));
            if (points == null || points.Count < 4) {
                return b.operations;    // Drawn as far as it can be read, which is not a line
            }
            b.MoveTo(points[0], points[1]);
            for (int i = 2; i + 1 < points.Count; i += 2) {
                b.LineTo(points[i], points[i + 1]);
            }
            if (name.Equals("polygon")) {
                b.ClosePath();
            }
        }
        return b.operations;
    }

    // Returns the coordinate or the size of a shape in the attribute.
    private static double Number(Dictionary<String, String> attributes, String name) {
        return SVGState.ParseCoordinate(Attribute(attributes, name));
    }

    // Reads the rx or the ry of a rectangle; returns false when it is not
    // given, is auto or is not a length of zero or more.
    private static bool Radius(String value, out double radius) {
        radius = 0.0;
        value = value.Trim();
        if (value.Equals("") || value.Equals("auto")) {
            return false;
        }
        float length;
        if (!SVGState.ParseLength(value, out length) || length < 0f || value.EndsWith("%")) {
            return false;
        }
        radius = (double) length;
        return true;
    }

    // Adds the operations of a path or a shape to the image, in the space of
    // the svg element, with what the state draws them with, unless they draw
    // nothing: a shape in <defs> or of no size, or with neither a fill nor a
    // stroke.
    private void AddPath(SVGState state, List<PathOp> operations) {
        double[] m = state.matrix;
        double det = m[0]*m[3] - m[1]*m[2];
        if (state.hidden || operations.Count == 0 || det == 0.0) {
            return;
        }
        SVGPath path = new SVGPath();
        path.operations = operations;
        path.fill = state.fillCurrent ? state.color : state.fill;
        path.stroke = state.strokeCurrent ? state.color : state.stroke;
        path.fillAlpha = state.fillOpacity * state.opacity;
        path.strokeAlpha = state.strokeOpacity * state.opacity;
        if (path.fillAlpha == 0f) {
            path.fill = Color.transparent;
        }
        if (path.strokeAlpha == 0f || state.strokeWidth == 0f) {
            path.stroke = Color.transparent;
        }
        if (path.fill == Color.transparent && path.stroke == Color.transparent) {
            return;
        }
        path.strokeWidth = (float) (((double) state.strokeWidth) * Math.Sqrt(Math.Abs(det)));
        path.evenOdd = state.evenOdd;
        path.lineCap = state.lineCap;
        path.lineJoin = state.lineJoin;
        if (!SVGState.IsIdentity(m)) {
            foreach (PathOp op in operations) {
                float x = op.x;
                op.x = (float) (m[0]*x + m[2]*op.y + m[4]);
                op.y = (float) (m[1]*x + m[3]*op.y + m[5]);
                if (op.cmd == 'C') {
                    float x1 = op.x1;
                    op.x1 = (float) (m[0]*x1 + m[2]*op.y1 + m[4]);
                    op.y1 = (float) (m[1]*x1 + m[3]*op.y1 + m[5]);
                    float x2 = op.x2;
                    op.x2 = (float) (m[0]*x2 + m[2]*op.y2 + m[4]);
                    op.y2 = (float) (m[1]*x2 + m[3]*op.y2 + m[5]);
                }
            }
        }
        paths.Add(path);
    }

    // Returns the width or the height of the svg element in points, and 0 for
    // a length that PDFjet cannot read, a percentage among them, which leaves
    // the size to the viewBox.
    private static float ParseLength(String value) {
        String text = value.Trim();
        float scale = 1f;
        for (int i = 0; i < SVGState.UNITS.Length; i++) {
            if (text.EndsWith(SVGState.UNITS[i])) {
                text = text.Substring(0, text.Length - SVGState.UNITS[i].Length).Trim();
                scale = SVGState.POINTS[i];
                break;
            }
        }
        float number;
        if (!SVGState.ParseFloat(text, out number) || float.IsInfinity(number) || float.IsNaN(number)) {
            return 0f;
        }
        return number*scale;
    }

    private void ProcessPaths(List<SVGPath> paths) {
        if (viewBox == null || viewBox.Equals("")) {
            return;
        }
        float[] box = new float[4];
        List<double> list = SVGState.ParseNumbers(viewBox);
        if (list == null || list.Count != 4) {
            throw new Exception("Invalid SVG viewBox \"" + viewBox + "\": four numbers are needed.");
        }
        for (int i = 0; i < 4; i++) {
            box[i] = (float) list[i];
        }
        if (box[2] == 0f || box[3] == 0f) {
            throw new Exception(
                    "Invalid SVG viewBox \"" + viewBox + "\": its width and height cannot be zero.");
        }
        // A size the file does not give, or one in a unit PDFjet cannot read,
        // is the other one in the proportions of the viewBox, or the size of
        // the viewBox when both are missing, where scaling it by a width of
        // zero would draw every path at the origin.
        if (w == 0f && h == 0f) {
            w = box[2];
            h = box[3];
        } else if (w == 0f) {
            w = h * box[2] / box[3];
        } else if (h == 0f) {
            h = w * box[3] / box[2];
        }

        // The viewBox is scaled to the size, and, unless preserveAspectRatio
        // is none, by the same factor across and down, as large as it fits
        // (meet) or as small as it covers (slice), and placed as it says, in
        // the middle by default.
        float sx = w / box[2];
        float sy = h / box[3];
        float tx = 0f;
        float ty = 0f;
        String[] fields = (preserveAspectRatio == null) ? new String[0] : preserveAspectRatio.Split(
                default(char[]), StringSplitOptions.RemoveEmptyEntries);
        int first = (fields.Length > 0 && fields[0].Equals("defer")) ? 1 : 0;
        // The alignment is read in the bytes of UTF-8, one char for each
        // byte, as the Go port reads it.
        String align = (fields.Length > first) ?
                Encoding.Latin1.GetString(Encoding.UTF8.GetBytes(fields[first])) : "xMidYMid";
        bool uniform = !align.Equals("none") && sx != sy;
        if (uniform) {
            float scale = (float) Math.Min((double) sx, (double) sy);
            if (fields.Length > first + 1 && fields[first + 1].Equals("slice")) {
                scale = (float) Math.Max((double) sx, (double) sy);
            }
            sx = scale;
            sy = scale;
            if (align.Length != 8) {
                align = "xMidYMid";
            }
            tx = AlignOffset(align.Substring(1, 3), w - box[2]*scale);
            ty = AlignOffset(align.Substring(5, 3), h - box[3]*scale);
        }
        float strokeScale = (float) Math.Sqrt(((double) sx) * ((double) sy));
        foreach (SVGPath path in paths) {
            path.strokeWidth *= strokeScale;
            foreach (PathOp op in path.operations) {
                if (uniform) {
                    op.x = (op.x - box[0])*sx + tx;
                    op.y = (op.y - box[1])*sy + ty;
                    op.x1 = (op.x1 - box[0])*sx + tx;
                    op.y1 = (op.y1 - box[1])*sy + ty;
                    op.x2 = (op.x2 - box[0])*sx + tx;
                    op.y2 = (op.y2 - box[1])*sy + ty;
                } else {
                    op.x = (op.x - box[0]) * w / box[2];
                    op.y = (op.y - box[1]) * h / box[3];
                    op.x1 = (op.x1 - box[0]) * w / box[2];
                    op.y1 = (op.y1 - box[1]) * h / box[3];
                    op.x2 = (op.x2 - box[0]) * w / box[2];
                    op.y2 = (op.y2 - box[1]) * h / box[3];
                }
            }
        }
    }

    // Returns how far the viewBox is moved across or down in the space left
    // over by it: none for Min, half for Mid and all for Max.
    private static float AlignOffset(String align, float space) {
        if (align.Equals("Min")) {
            return 0f;
        }
        if (align.Equals("Max")) {
            return space;
        }
        return space / 2f;
    }

    /// <summary>Sets the location of the top left corner of this image on the page.</summary>
    public SVGImage SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>Scales this SVG image by the specified factor.</summary>
    public SVGImage ScaleBy(float factor) {
        foreach (SVGPath path in paths) {
            path.strokeWidth *= factor;
            foreach (PathOp op in path.operations) {
                op.x1 *= factor;
                op.y1 *= factor;
                op.x2 *= factor;
                op.y2 *= factor;
                op.x *= factor;
                op.y *= factor;
            }
        }
        this.w *= factor;
        this.h *= factor;
        return this;
    }

    /// <summary>Sets the URI for the "click box" action.</summary>
    public SVGImage SetURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    /// <summary>Sets the destination key for the action.</summary>
    public SVGImage SetGoToAction(String key) {
        this.key = key;
        return this;
    }

    /// <summary>Sets the alternate description of this image.</summary>
    public SVGImage SetAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /// <summary>Sets the actual text of this image.</summary>
    public SVGImage SetActualText(String actualText) {
        this.actualText = actualText;
        return this;
    }

    /// <summary>Sets the language of this image, for example "en-US".</summary>
    public SVGImage SetLanguage(String language) {
        this.language = language;
        return this;
    }

    /// <summary>Returns the width of this SVG image.</summary>
    public float GetWidth() {
        return this.w;
    }

    /// <summary>Returns the height of this SVG image.</summary>
    public float GetHeight() {
        return this.h;
    }

    // Whether a PDF can hold the points and the stroke width of the path
    // where it is drawn: a transform or a scale such as scale(1e30) can take
    // them out of its range, and such a path is not drawn.
    private bool IsWritable(SVGPath path, Page page) {
        if (!FastFloat.IsWritable(path.strokeWidth)) {
            return false;
        }
        foreach (PathOp op in path.operations) {
            if (!IsWritable(op.x, op.y, page) || (op.cmd == 'C' &&
                    (!IsWritable(op.x1, op.y1, page) || !IsWritable(op.x2, op.y2, page)))) {
                return false;
            }
        }
        return true;
    }

    private bool IsWritable(float x, float y, Page page) {
        x += this.x;
        y += this.y;
        return FastFloat.IsWritable(x) && FastFloat.IsWritable(y) && FastFloat.IsWritable(page.height - y);
    }

    // Draws a path, filled and then stroked. An opacity, a line cap and a
    // line join are set in a graphics state of the path's own.
    private void drawPath(SVGPath path, Page page) {
        bool fill = path.fill != Color.transparent;
        bool stroke = path.stroke != Color.transparent;
        if (path.operations.Count == 0 || (!fill && !stroke) || !IsWritable(path, page)) {
            return;
        }
        bool alpha = (fill && path.fillAlpha < 1f) || (stroke && path.strokeAlpha < 1f);
        bool lineCap = stroke && path.lineCap != CapStyle.BUTT;
        bool lineJoin = stroke && path.lineJoin != JoinStyle.MITER;
        bool state = alpha || lineCap || lineJoin;
        if (state) {
            page.SaveGraphicsState();
            if (alpha) {
                GraphicsState gs = new GraphicsState();
                gs.SetAlphaNonStroking(path.fillAlpha);
                gs.SetAlphaStroking(path.strokeAlpha);
                page.SetGraphicsState(gs);
            }
            if (lineCap) {
                page.SetLineCapStyle(path.lineCap);
            }
            if (lineJoin) {
                page.SetLineJoinStyle(path.lineJoin);
            }
        }

        if (fill) {
            page.SetBrushColor(path.fill);
            foreach (PathOp op in path.operations) {
                if (op.cmd == 'M') {
                    page.MoveTo(op.x + x, op.y + y);
                } else if (op.cmd == 'L') {
                    page.LineTo(op.x + x, op.y + y);
                } else if (op.cmd == 'C') {
                    page.CurveTo(
                        op.x1 + x, op.y1 + y,
                        op.x2 + x, op.y2 + y,
                        op.x + x, op.y + y);
                }
            }
            if (path.evenOdd) {
                page.Append("f*\n");
            } else {
                page.FillPath();
            }
        }

        if (stroke) {
            page.SetPenColor(path.stroke);
            page.SetPenWidth(path.strokeWidth);
            // Z closes and strokes a subpath; a path that is still open at the
            // end is stroked without closing it.
            bool open = false;
            foreach (PathOp op in path.operations) {
                if (op.cmd == 'M') {
                    page.MoveTo(op.x + x, op.y + y);
                    open = true;
                } else if (op.cmd == 'L') {
                    page.LineTo(op.x + x, op.y + y);
                    open = true;
                } else if (op.cmd == 'C') {
                    page.CurveTo(
                        op.x1 + x, op.y1 + y,
                        op.x2 + x, op.y2 + y,
                        op.x + x, op.y + y);
                    open = true;
                } else if (op.cmd == 'Z') {
                    page.ClosePath();
                    open = false;
                }
            }
            if (open) {
                page.StrokePath();
            }
        }

        if (state) {
            page.RestoreGraphicsState();
        }
    }

    /// <summary>Draws this SVG image on the specified page.</summary>
    public float[] DrawOn(Page page) {
        if (page == null) {
            return new float[] {x + w, y + h};  // Measured, not drawn
        }
        page.AddBDC(StructElem.FIGURE, language, actualText, altDescription);
        foreach (SVGPath path in paths) {
            drawPath(path, page);
        }
        page.AddEMC();
        if (uri != null || key != null) {
            page.AddAnnotation(new Annotation(
                    Annotation.Link,
                    x,
                    y,
                    x + w,
                    y + h,
                    null,   // Vertices
                    null,   // Fill Color
                    0f,     // Opacity
                    null,   // Title
                    null,   // Contents
                    uri,
                    key,    // The destination name
                    language,
                    actualText,
                    altDescription));
        }
        return new float[] {x + w, y + h};
    }
}   // End of SVGImage.cs
}   // End of PDFjet.NET namespace