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

namespace PDFjet.NET {
/// <summary>
/// Used to embed SVG images in the PDF document.
/// <para>
/// It draws the path, rect, circle, ellipse, line, polyline and polygon
/// elements, in groups or not, with their transforms, and the fill, stroke,
/// stroke-width, stroke-dasharray, stroke-dashoffset, fill-rule,
/// stroke-linecap, stroke-linejoin, opacity, fill-opacity, stroke-opacity,
/// paint-order, clip-path and markers they have or inherit, as attributes, in a
/// style attribute or in rules for their classes in a style element that comes
/// before them. It draws the elements a use element names, and text and tspan
/// elements with their x, y, dx, dy, font-size, font-weight, text-anchor and
/// dominant-baseline, in the font set with SetFont and SetBoldFont, or else
/// in the fonts PDFjet carries, embedded: IBM Plex
/// Sans for sans-serif, Helvetica, Arial and the like, IBM Plex Serif for
/// serif, Times and the like, Cascadia Mono for monospace, Courier and the
/// like, regular, semibold or bold, upright or italic, as the font-family,
/// font-weight and font-style of the text ask. A gradient is drawn in the
/// mean color of its stops. The width,
/// height, viewBox and preserveAspectRatio of the svg element give the size of
/// the image, and what is drawn outside it is cut off, as a browser does.
/// </para>
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
    private Font font = null;
    private Font boldFont = null;

    // While the file is read: the elements by their ids, the clip paths of
    // the image, and the elements drawn again by a use element or as a
    // marker, which are not drawn inside themselves.
    private Dictionary<String, XMLNode> ids;
    private List<SVGPath> clipShapes = new List<SVGPath>();
    private HashSet<XMLNode> reusing;
    private long reused = 0;
    private long counted = 0;
    private String[] useSize = null;    // The width and the height of the use element of the element drawn next
    // The elements drawn again, by use elements and as markers, at most: a
    // use of a group of uses, and so on, draws ten to the power of the
    // number of groups, as an XML of entities in entities does.
    internal const long MAX_REUSED = 1000000;
    // The path operations drawn again, by use elements and as markers, and
    // written for the clip paths of what is drawn, at most: a use of a path of N
    // operations was N operations for each element counted by the limit above,
    // 506 MB for a file of 200 KB, and a clip path of N operations is read for
    // every element it clips and written for every path in it, 800 MB of page
    // for 320 KB (review of 30 October 2026). The operations of a file drawn
    // once are not counted: they grow with the file.
    internal const long MAX_OPERATIONS = 4000000;
    // How deep the elements being drawn may nest, by groups, use elements and
    // markers, a state on the stack for each: a chain of use elements is flat in
    // the file, so the limit of the XML parser does not see it, and each level
    // is a call. The number is the same in the four ports and leaves every one
    // of them room on a stack of 512 KB, where a level takes 6.4 KB of a debug
    // build of Swift (review of 30 October 2026).
    internal const int MAX_DEPTH = 64;

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

    private void Read(Stream stream) {
        paths = new List<SVGPath>();
        SVGStyleSheet rules = new SVGStyleSheet();
        XMLNode document;
        try {
            document = XMLParser.ParseSkippingDoctype(stream);
        } catch (XMLException e) {
            throw new InvalidDataException("parsing SVG: " + e.Message, e);
        }
        ids = new Dictionary<String, XMLNode>();
        reusing = new HashSet<XMLNode>();
        List<XMLNode> elements = new List<XMLNode>();
        elements.Add(document);
        for (int i = 0; i < elements.Count; i++) {
            XMLNode element = elements[i];
            String id = element.GetAttribute("id");
            if (id != null && !ids.ContainsKey(id)) {
                ids[id] = element;  // The first element of an id is the one it names
            }
            elements.AddRange(element.GetChildren());
        }
        SVGState svg = new SVGState();
        svg.paints = GradientColors();
        List<SVGState> stack = new List<SVGState>();
        stack.Add(svg);
        bool root = true;
        Draw(document, stack, rules, ref root, false);
        ProcessPaths(paths);
        ids = null;
        reusing = null;
    }

    // Returns the element of the id, or null for none.
    private XMLNode ElementOf(String id) {
        XMLNode element;
        return (id != null && ids.TryGetValue(id, out element)) ? element : null;
    }

    // Returns the colors the gradients are drawn in, by their ids: the mean
    // of the colors of their stops, or of the stops of the gradient they name
    // with href when they have none. A gradient without stops is none, and
    // is left out.
    private Dictionary<String, int> GradientColors() {
        Dictionary<String, int> colors = new Dictionary<String, int>();
        Dictionary<XMLNode, Stops> read = new Dictionary<XMLNode, Stops>();
        foreach (KeyValuePair<String, XMLNode> entry in ids) {
            XMLNode gradient = entry.Value;
            // Up to ten gradients along the hrefs, which may go round in a circle.
            for (int i = 0; i < 10 && gradient != null && IsGradient(gradient); i++) {
                Stops stops;
                if (!read.TryGetValue(gradient, out stops)) {
                    stops = GradientStops(gradient);
                    read[gradient] = stops;
                }
                if (stops.found) {
                    colors[entry.Key] = stops.color;
                    break;
                }
                gradient = stops.next;
            }
        }
        return colors;
    }

    // What a gradient itself says of its color: the mean of its stops, if it
    // has any, and the gradient its href names.
    private class Stops {
        internal int color = 0;
        internal bool found = false;
        internal XMLNode next = null;
    }

    // Reads the gradient, once however many gradients name it: N gradients
    // that name one of N stops were N x N stops read, 50 s for 3.4 MB (review
    // of 30 October 2026).
    private Stops GradientStops(XMLNode gradient) {
        long r = 0;
        long g = 0;
        long b = 0;
        int count = 0;
        foreach (XMLNode stop in gradient.GetChildren()) {
            if (!stop.GetLocalName().Equals("stop")) {
                continue;
            }
            SVGState state = new SVGState();
            state.fill = Color.black;   // stop-color is black unless given
            String color = stop.GetAttribute("stop-color");
            if (color != null) {
                state.SetProperty("fill", color);
            }
            foreach (String[] declaration in SVGRule.ParseDeclarations(Attribute(stop, "style"))) {
                if (declaration[0].Equals("stop-color")) {
                    state.SetProperty("fill", declaration[1]);
                }
            }
            int c = (state.fill == Color.transparent) ? Color.black : state.fill;
            r += (c >> 16) & 0xFF;
            g += (c >> 8) & 0xFF;
            b += c & 0xFF;
            count++;
        }
        Stops stops = new Stops();
        if (count > 0) {
            stops.color = (int) ((r + count/2)/count << 16 | (g + count/2)/count << 8 | (b + count/2)/count);
            stops.found = true;
        }
        stops.next = ElementOf(Href(gradient));
        return stops;
    }

    private static bool IsGradient(XMLNode node) {
        String name = node.GetLocalName();
        return name.Equals("linearGradient") || name.Equals("radialGradient");
    }

    // Returns the attribute of the element, or an empty value.
    private static String Attribute(XMLNode node, String name) {
        String value = node.GetAttribute(name);
        return (value == null) ? "" : value;
    }

    // Returns the id that the href of an element names, #id, with or without
    // the xlink prefix, or null.
    private static String Href(XMLNode node) {
        String href = null;
        foreach (KeyValuePair<String, String> attribute in node.GetAttributes()) {
            String name = attribute.Key;
            if (name.Equals("href") || (href == null && XMLNode.LocalNameOf(name).Equals("href"))) {
                href = attribute.Value;
            }
        }
        if (href == null) {
            return null;
        }
        href = SVGState.Trim(href);
        return (href.Length > 1 && href[0] == '#') ? href.Substring(1) : null;
    }

    // Counts an element drawn again, and throws when there are too many.
    private void CountReused() {
        if (++reused > MAX_REUSED) {
            throw new ArgumentException(
                    "The SVG draws more than " + MAX_REUSED + " elements again, with use elements or markers.");
        }
    }

    // Counts operations drawn again or written again, and throws when there
    // are too many.
    private void CountOperations(int count) {
        counted += count;
        if (counted > MAX_OPERATIONS) {
            throw new ArgumentException("The SVG draws more than " + MAX_OPERATIONS
                    + " path operations again, with use elements, markers or clip paths.");
        }
    }

    // Returns the operations written for the clip paths of one path, and of
    // a run of a text.
    private static int ClipOperations(List<SVGPath> clips) {
        int count = 0;
        if (clips != null) {
            foreach (SVGPath clip in clips) {
                count += clip.operations.Count;
            }
        }
        return count;
    }

    // The error of elements nested too deep.
    private static ArgumentException DepthError() {
        return new ArgumentException(
                "The SVG nests elements, use elements and markers more than " + MAX_DEPTH + " deep.");
    }

    // Draws the element, then the ones in it, in the order of the document;
    // the rules of a <style> element apply from its end. An element that is
    // used, by a use element or as a marker, is drawn even where it is a
    // template, as a symbol or a marker is.
    private void Draw(XMLNode node, List<SVGState> stack, SVGStyleSheet rules, ref bool root, bool used) {
        if (stack.Count > MAX_DEPTH) {
            throw DepthError();
        }
        String name = node.GetLocalName();
        if (reusing.Count > 0) {
            CountReused();
        }
        // The attributes of no namespace, in the order of the element:
        // xlink:href, xml:space and the namespaces' own declarations,
        // xmlns:..., are not read.
        Dictionary<String, String> attributes = new Dictionary<String, String>();
        List<String> order = new List<String>();
        foreach (KeyValuePair<String, String> attribute in node.GetAttributes()) {
            if (attribute.Key.IndexOf(':') == -1) {
                attributes[attribute.Key] = attribute.Value;
                order.Add(attribute.Key);
            }
        }
        SVGState state = ElementState(stack[stack.Count - 1], rules, attributes, order);
        if (TEMPLATES.Contains(name) && !used) {
            state.hidden = true;
        }
        if (state.clipPath != null && !state.hidden) {
            Clip(state, rules);
        }
        String[] size = useSize;
        useSize = null;
        if (name.Equals("svg") && root) {
            root = false;
            this.w = ParseLength(Attribute(attributes, "width"));
            this.h = ParseLength(Attribute(attributes, "height"));
            // viewbox in lower case when there is no viewBox, as an
            // HTML page's parser reads an inline svg, whose files keep it
            if (!attributes.TryGetValue("viewBox", out this.viewBox)) {
                attributes.TryGetValue("viewbox", out this.viewBox);
            }
            attributes.TryGetValue("preserveAspectRatio", out this.preserveAspectRatio);
            List<double> box = SVGState.ParseNumbers(this.viewBox ?? "");
            state.viewport = (box != null && box.Count == 4)
                    ? new double[] {box[2], box[3]} : new double[] {this.w, this.h};
        } else if (name.Equals("svg") || (name.Equals("symbol") && used)) {
            EnterViewport(state, attributes, size);
        }
        stack.Add(state);

        if (name.Equals("text")) {
            if (!state.hidden) {
                AddText(node, rules, state, attributes, stack.Count);
            }
            stack.RemoveAt(stack.Count - 1);
            return;
        }
        if (name.Equals("use")) {
            XMLNode target = ElementOf(Href(node));
            if (target != null && !state.hidden && !reusing.Contains(target)) {
                // The element is drawn as a child of the use element, moved
                // by its x and y.
                SVGState parent = state.Copy();
                parent.matrix = SVGState.Multiply(state.matrix, new double[] {
                    1.0, 0.0, 0.0, 1.0, Number(attributes, "x"), Number(attributes, "y")});
                stack.Add(parent);
                reusing.Add(target);
                useSize = new String[] {Attribute(attributes, "width"), Attribute(attributes, "height")};
                Draw(target, stack, rules, ref root, true);
                reusing.Remove(target);
                stack.RemoveAt(stack.Count - 1);
            }
            stack.RemoveAt(stack.Count - 1);
            return;
        }
        List<PathOp> operations = ShapeOperations(name, attributes);
        int count = (reusing.Count > 0) ? operations.Count : 0;
        if (!state.hidden && operations.Count > 0) {
            count += ClipOperations(state.clips) + ((state.dashArray != null) ? state.dashArray.Length : 0);
        }
        CountOperations(count);
        if (name.Equals("line")) {
            state = state.Copy();
            state.fill = Color.transparent;     // A line has no inside to fill
            state.fillCurrent = false;
        }
        List<double[]> vertices = null;
        if (!state.hidden && !state.invisible && operations.Count > 0
                && (state.markerStart != null || state.markerMid != null || state.markerEnd != null)
                && (name.Equals("path") || name.Equals("line") || name.Equals("polyline") || name.Equals("polygon"))) {
            vertices = Vertices(operations);    // Before AddPath moves the points
        }
        AddPath(state, operations);
        if (vertices != null) {
            DrawMarkers(state, vertices, rules, stack, ref root);
        }

        foreach (XMLNode child in node.GetChildren()) {
            if (name.Equals("switch") && !IsSwitchable(child)) {
                continue;
            }
            Draw(child, stack, rules, ref root, false);
            if (name.Equals("switch")) {
                break;  // A switch draws the first it can
            }
        }
        if (name.Equals("style")) {
            rules.Add(SVGRule.ParseStyleSheet(node.GetText()));
        }
        stack.RemoveAt(stack.Count - 1);
    }

    // Returns the value of the attribute, and "" for one the element does not have.
    private static String Attribute(Dictionary<String, String> attributes, String name) {
        String value;
        return attributes.TryGetValue(name, out value) ? value : "";
    }

    // The elements PDFjet draws, and a switch can choose.
    private static readonly HashSet<String> DRAWN = new HashSet<String> {
        "a", "circle", "ellipse", "g", "line", "path", "polygon",
        "polyline", "rect", "svg", "switch", "text", "use",
    };

    // Returns true when a switch can draw the element: one PDFjet draws,
    // which asks for no extension, as PDFjet has none. The system language
    // is not tested: a PDF is read by anyone.
    private static bool IsSwitchable(XMLNode node) {
        return DRAWN.Contains(node.GetLocalName()) && node.GetAttribute("requiredExtensions") == null;
    }

    // Returns the state of an element whose parent has the given one: its
    // presentation attributes, in their order, then the rules of the style
    // sheet for its classes, in the order of the style sheet, then its style
    // attribute, and its transform.
    private static SVGState ElementState(SVGState parent, SVGStyleSheet rules,
            Dictionary<String, String> attributes, List<String> order) {
        SVGState state = parent.Copy();
        state.ownOpacity = 1f;
        state.clipPath = null;
        foreach (String name in order) {
            state.SetProperty(name, attributes[name]);
        }
        String[] classes = Attribute(attributes, "class").Split(
                default(char[]), StringSplitOptions.RemoveEmptyEntries);
        foreach (SVGRule rule in rules.ForClasses(classes)) {
            foreach (String[] declaration in rule.declarations) {
                state.SetProperty(declaration[0], declaration[1]);
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
            List<PathOp> operations = SVG.ToPDF(SVG.GetOperations(Attribute(attributes, "d")));
            // Path data starts with a moveto; data that does not is in error
            // from its start, and SVG 1.1 section 8.3.2 draws it up to there.
            if (operations.Count > 0 && operations[0].cmd != 'M') {
                return new List<PathOp>();
            }
            return operations;
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
        if (!SVGState.ParseLength(value, out length) || length < 0f || value.EndsWith("%", StringComparison.Ordinal)) {
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
        if (state.hidden || state.invisible || operations.Count == 0 || det == 0.0) {
            return;
        }
        SVGPath path = new SVGPath();
        path.operations = operations;
        path.fill = state.fillCurrent ? state.color : state.fill;
        path.stroke = state.strokeCurrent ? state.color : state.stroke;
        path.fillAlpha = state.DrawnFillAlpha();
        path.strokeAlpha = state.DrawnStrokeAlpha();
        if (path.fillAlpha == 0f) {
            path.fill = Color.transparent;
        }
        if (path.strokeAlpha == 0f || state.strokeWidth == 0f) {
            path.stroke = Color.transparent;
        }
        if (path.fill == Color.transparent && path.stroke == Color.transparent) {
            return;
        }
        double scale = Math.Sqrt(Math.Abs(det));
        path.strokeWidth = (float) (((double) state.strokeWidth) * scale);
        path.evenOdd = state.evenOdd;
        path.lineCap = state.lineCap;
        path.lineJoin = state.lineJoin;
        if (state.dashArray != null) {
            path.dashArray = new float[state.dashArray.Length];
            for (int i = 0; i < path.dashArray.Length; i++) {
                path.dashArray[i] = (float) (((double) state.dashArray[i]) * scale);
            }
            path.dashOffset = (float) (((double) state.dashOffset) * scale);
        }
        path.clips = state.clips;
        Transform(m, operations);
        paths.Add(path);
    }

    // Moves the points of the operations by the matrix.
    private static void Transform(double[] m, List<PathOp> operations) {
        if (SVGState.IsIdentity(m)) {
            return;
        }
        foreach (PathOp op in operations) {
            float[] point = Transform(m, op.x, op.y);
            op.x = point[0];
            op.y = point[1];
            if (op.cmd == 'C') {
                point = Transform(m, op.x1, op.y1);
                op.x1 = point[0];
                op.y1 = point[1];
                point = Transform(m, op.x2, op.y2);
                op.x2 = point[0];
                op.y2 = point[1];
            }
        }
    }

    // Returns the point that the matrix takes the point to.
    private static float[] Transform(double[] m, float x, float y) {
        return new float[] {
            (float) (m[0]*x + m[2]*y + m[4]),
            (float) (m[1]*x + m[3]*y + m[5]),
        };
    }

    // The attributes of no namespace of the element, and their order.
    private static Dictionary<String, String> Attributes(XMLNode node, List<String> order) {
        Dictionary<String, String> attributes = new Dictionary<String, String>();
        foreach (KeyValuePair<String, String> attribute in node.GetAttributes()) {
            if (attribute.Key.IndexOf(':') == -1) {
                attributes[attribute.Key] = attribute.Value;
                order.Add(attribute.Key);
            }
        }
        return attributes;
    }

    // Sets the clip paths of the element: those of its ancestors and the one
    // its clip-path names, the shapes of the clipPath element in the user
    // space of the element. A use in it names a shape, moved by its x and y. A
    // clip path of one shape keeps the clip-rule of the shape; several are
    // one path whose nonzero rule draws their union, each turned the same way
    // round, as opposite ways would cancel out where they overlap. A clip path
    // that is not there, or one in objectBoundingBox units, which PDFjet does
    // not draw, is left out; one with no shape in it hides the element, as SVG
    // clips all of it away.
    private void Clip(SVGState state, SVGStyleSheet rules) {
        XMLNode clipPath = ElementOf(state.clipPath);
        if (clipPath == null || !clipPath.GetLocalName().Equals("clipPath")
                || SVGState.Trim(Attribute(clipPath, "clipPathUnits")).Equals("objectBoundingBox")) {
            return;
        }
        SVGState svg = new SVGState();
        svg.matrix = state.matrix;
        List<String> order = new List<String>();
        Dictionary<String, String> attributes = Attributes(clipPath, order);
        SVGState parent = ElementState(svg, rules, attributes, order);
        parent.hidden = false;  // A clipPath element is not displayed anyway
        SVGPath clip = new SVGPath();
        clip.operations = new List<PathOp>();
        List<List<PathOp>> shapes = new List<List<PathOp>>();
        foreach (XMLNode child in clipPath.GetChildren()) {
            order = new List<String>();
            attributes = Attributes(child, order);
            SVGState shape = ElementState(parent, rules, attributes, order);
            String name = child.GetLocalName();
            if (name.Equals("use")) {
                XMLNode target = ElementOf(Href(child));
                if (target == null) {
                    continue;
                }
                shape.matrix = SVGState.Multiply(shape.matrix, new double[] {
                    1.0, 0.0, 0.0, 1.0, Number(attributes, "x"), Number(attributes, "y")});
                order = new List<String>();
                attributes = Attributes(target, order);
                shape = ElementState(shape, rules, attributes, order);
                name = target.GetLocalName();
            }
            if (shape.hidden || shape.invisible) {
                continue;
            }
            List<PathOp> operations = ShapeOperations(name, attributes);
            if (operations.Count == 0) {
                continue;
            }
            Transform(shape.matrix, operations);
            shapes.Add(operations);
            clip.evenOdd = shape.clipEvenOdd;
        }
        foreach (List<PathOp> operations in shapes) {
            List<PathOp> added = operations;
            if (shapes.Count > 1) {
                clip.evenOdd = false;
                if (Area(operations) < 0.0) {
                    added = Reversed(operations);
                }
            }
            clip.operations.AddRange(added);
        }
        CountOperations(clipPath.GetChildren().Count + clip.operations.Count);
        if (clip.operations.Count == 0) {
            state.hidden = true;
            return;
        }
        clipShapes.Add(clip);
        List<SVGPath> clips = (state.clips == null) ? new List<SVGPath>() : new List<SVGPath>(state.clips);
        clips.Add(clip);
        state.clips = clips;
    }

    // Sets the user space of a nested svg element, or of a symbol a use
    // element draws: moved by its x and y, and its viewBox fitted to its width
    // and height, which the use element may give, and which are those of the
    // viewport it is in unless given; a percentage is of that viewport. A
    // viewBox or a size of nothing draws nothing.
    private static void EnterViewport(SVGState state, Dictionary<String, String> attributes, String[] size) {
        String width = (size != null && size[0].Length > 0) ? size[0] : Attribute(attributes, "width");
        String height = (size != null && size[1].Length > 0) ? size[1] : Attribute(attributes, "height");
        double w = ViewportLength(width, state.viewport[0], state.viewport[0]);
        double h = ViewportLength(height, state.viewport[1], state.viewport[1]);
        double[] m = new double[] {1.0, 0.0, 0.0, 1.0,
            ViewportLength(Attribute(attributes, "x"), state.viewport[0], 0.0),
            ViewportLength(Attribute(attributes, "y"), state.viewport[1], 0.0)};
        List<double> box = SVGState.ParseNumbers(Attribute(attributes, "viewBox"));
        if (box == null || box.Count != 4) {
            state.viewport = new double[] {w, h};
            state.matrix = SVGState.Multiply(state.matrix, m);
            return;
        }
        // A viewport of no known size, of an svg element with neither a size
        // nor a viewBox, leaves the viewBox as it is.
        if (w == 0.0 && width.Length == 0) {
            w = box[2];
        }
        if (h == 0.0 && height.Length == 0) {
            h = box[3];
        }
        if (box[2] <= 0.0 || box[3] <= 0.0 || w <= 0.0 || h <= 0.0) {
            state.hidden = true;
            return;
        }
        double sx = w/box[2];
        double sy = h/box[3];
        double tx = 0.0;
        double ty = 0.0;
        List<String> fields = SVGState.Fields(Attribute(attributes, "preserveAspectRatio"));
        if (fields.Count > 0 && fields[0].Equals("defer")) {
            fields.RemoveAt(0);
        }
        // The alignment is read in the bytes of UTF-8, one char for each
        // byte, as the Go port reads it.
        String align = (fields.Count > 0) ? Encoding.Latin1.GetString(Encoding.UTF8.GetBytes(fields[0])) : "xMidYMid";
        if (!align.Equals("none")) {
            sx = Math.Min(sx, sy);
            if (fields.Count > 1 && fields[1].Equals("slice")) {
                sx = Math.Max(w/box[2], h/box[3]);
            }
            sy = sx;
            if (align.Length != 8) {
                align = "xMidYMid";
            }
            tx = AlignOffset(align.Substring(1, 3), 1f)*(w - box[2]*sx);
            ty = AlignOffset(align.Substring(5, 3), 1f)*(h - box[3]*sy);
        }
        m = SVGState.Multiply(m, new double[] {sx, 0.0, 0.0, sy, tx, ty});
        m = SVGState.Multiply(m, new double[] {1.0, 0.0, 0.0, 1.0, -box[0], -box[1]});
        state.viewport = new double[] {box[2], box[3]};
        state.matrix = SVGState.Multiply(state.matrix, m);
    }

    // Reads an x, a y, a width or a height of a viewport, which may be a
    // percentage of the size of the one it is in, the given one if it has none.
    private static double ViewportLength(String value, double whole, double given) {
        value = SVGState.Trim(value);
        if (value.Length == 0) {
            return given;
        }
        if (value.EndsWith("%", StringComparison.Ordinal)) {
            double number;
            if (!SVGState.ParseDouble(value.Substring(0, value.Length - 1), out number)
                    || double.IsNaN(number) || double.IsInfinity(number)) {
                return given;
            }
            return number*whole/100.0;
        }
        return SVGState.ParseCoordinate(value);
    }

    // Returns twice the area of the path, its sign the way it goes round, by
    // the points of its lines and curves, and closed.
    private static double Area(List<PathOp> operations) {
        double area = 0.0;
        double x0 = 0.0;
        double y0 = 0.0;
        double x = 0.0;
        double y = 0.0;
        foreach (PathOp op in operations) {
            if (op.cmd == 'M') {
                area += x*y0 - x0*y;
                x0 = op.x;
                y0 = op.y;
                x = x0;
                y = y0;
                continue;
            }
            if (op.cmd == 'C') {
                area += x*op.y1 - ((double) op.x1)*y;
                area += ((double) op.x1)*op.y2 - ((double) op.x2)*op.y1;
                x = op.x2;
                y = op.y2;
            }
            area += x*op.y - ((double) op.x)*y;
            x = op.x;
            y = op.y;
        }
        return area + x*y0 - x0*y;
    }

    // Returns the path going the other way round, each subpath from its end.
    // Only a clip path needs this, whose subpaths close of themselves.
    private static List<PathOp> Reversed(List<PathOp> operations) {
        List<PathOp> reversed = new List<PathOp>(operations.Count);
        int start = 0;
        for (int end = 1; end <= operations.Count; end++) {
            if (end < operations.Count && operations[end].cmd != 'M') {
                continue;
            }
            PathOp last = operations[end - 1];
            reversed.Add(new PathOp('M', last.x, last.y));
            for (int i = end - 1; i > start; i--) {
                PathOp op = operations[i];
                PathOp before = operations[i - 1];
                if (op.cmd == 'C') {
                    PathOp curve = new PathOp('C');
                    curve.SetCubicPoints(op.x2, op.y2, op.x1, op.y1, before.x, before.y);
                    reversed.Add(curve);
                } else {
                    reversed.Add(new PathOp('L', before.x, before.y));
                }
            }
            start = end;
        }
        return reversed;
    }

    // The length of the vector x, y. Its coordinates come from floats, whose
    // squares a double holds without overflow or underflow.
    private static double Hypot(double x, double y) {
        return Math.Sqrt(x*x + y*y);
    }

    // An angle in radians in degrees, and one in degrees in radians, as the
    // Java port's Math.toDegrees and Math.toRadians have them.
    private static double ToDegrees(double radians) {
        return radians * 57.29577951308232;
    }

    private static double ToRadians(double degrees) {
        return degrees * 0.017453292519943295;
    }

    // Returns the vertices of the path, where its markers are drawn, in its
    // user space: x, y and the angle of the path there, in degrees, which is
    // halfway between the directions in and out of a vertex in the middle.
    private static List<double[]> Vertices(List<PathOp> operations) {
        List<double[]> vertices = new List<double[]>();     // x, y, in x, in y, out x, out y
        double x = 0.0;
        double y = 0.0;
        int start = 0;      // The vertex where the subpath starts
        foreach (PathOp op in operations) {
            double[] into;
            double[] outOf;
            if (op.cmd == 'M') {
                start = vertices.Count;
                vertices.Add(new double[] {op.x, op.y, 0.0, 0.0, 0.0, 0.0});
                x = op.x;
                y = op.y;
                continue;
            } else if (op.cmd == 'C') {
                outOf = Direction(x, y, op.x1, op.y1, op.x2, op.y2, op.x, op.y);
                into = Direction(op.x, op.y, op.x2, op.y2, op.x1, op.y1, x, y);
                into = new double[] {-into[0], -into[1]};
            } else {
                // A line, or Z, the line back to the start of the subpath.
                outOf = new double[] {op.x - x, op.y - y};
                into = outOf;
            }
            if (vertices.Count == 0) {
                continue;
            }
            double[] last = vertices[vertices.Count - 1];
            if (last[4] == 0.0 && last[5] == 0.0) {
                last[4] = outOf[0];
                last[5] = outOf[1];
            }
            vertices.Add(new double[] {op.x, op.y, into[0], into[1], 0.0, 0.0});
            if (op.cmd == 'Z') {
                // The vertex that closes the subpath goes on as its start does.
                double[] first = vertices[start];
                double[] closing = vertices[vertices.Count - 1];
                closing[4] = first[4];
                closing[5] = first[5];
            }
            x = op.x;
            y = op.y;
        }
        List<double[]> result = new List<double[]>();
        foreach (double[] v in vertices) {
            double inLength = Hypot(v[2], v[3]);
            double outLength = Hypot(v[4], v[5]);
            double dx = 0.0;
            double dy = 0.0;
            if (inLength > 0.0) {
                dx += v[2]/inLength;
                dy += v[3]/inLength;
            }
            if (outLength > 0.0) {
                dx += v[4]/outLength;
                dy += v[5]/outLength;
            }
            if (dx == 0.0 && dy == 0.0 && inLength > 0.0) {
                dx = v[2];      // A path that turns back: the direction in
                dy = v[3];
            }
            result.Add(new double[] {v[0], v[1], ToDegrees(Math.Atan2(dy, dx))});
        }
        return result;
    }

    // Returns the direction from x, y to the first of the points that is not
    // there, as a curve leaves its start.
    private static double[] Direction(double x, double y, params double[] points) {
        for (int i = 0; i + 1 < points.Length; i += 2) {
            if (points[i] != x || points[i + 1] != y) {
                return new double[] {points[i] - x, points[i + 1] - y};
            }
        }
        return new double[] {0.0, 0.0};
    }

    // Draws the markers of a path at its vertices: the start marker at the
    // first, the end marker at the last and the mid marker at the others.
    private void DrawMarkers(SVGState state, List<double[]> vertices, SVGStyleSheet rules,
            List<SVGState> stack, ref bool root) {
        int last = vertices.Count - 1;
        for (int i = 0; i <= last; i++) {
            if (i == 0) {
                DrawMarker(state.markerStart, state, vertices[i], true, rules, stack, ref root);
            }
            if (i > 0 && i < last) {
                DrawMarker(state.markerMid, state, vertices[i], false, rules, stack, ref root);
            }
            if (i == last) {
                DrawMarker(state.markerEnd, state, vertices[i], false, rules, stack, ref root);
            }
        }
    }

    // Draws the marker at the vertex: its refX, refY there, turned as its
    // orient says, and its viewBox fitted to its markerWidth and
    // markerHeight, which are in units of the stroke width unless its
    // markerUnits are userSpaceOnUse.
    private void DrawMarker(String id, SVGState state, double[] vertex, bool start,
            SVGStyleSheet rules, List<SVGState> stack, ref bool root) {
        XMLNode marker = ElementOf(id);
        if (marker == null || !marker.GetLocalName().Equals("marker") || reusing.Contains(marker)) {
            return;
        }
        String orient = SVGState.Trim(Attribute(marker, "orient"));
        double angle = 0.0;
        if (orient.Equals("auto")) {
            angle = vertex[2];
        } else if (orient.Equals("auto-start-reverse")) {
            angle = start ? vertex[2] + 180.0 : vertex[2];
        } else {
            if (orient.EndsWith("deg", StringComparison.Ordinal)) {
                orient = orient.Substring(0, orient.Length - 3);
            }
            if (!SVGState.ParseNumber(SVGState.Trim(orient), false, out angle)) {
                angle = 0.0;
            }
            if (double.IsNaN(angle) || double.IsInfinity(angle)) {
                angle = 0.0;
            }
        }
        double units = SVGState.Trim(Attribute(marker, "markerUnits")).Equals("userSpaceOnUse")
                ? 1.0 : state.strokeWidth;
        double width = MarkerSize(Attribute(marker, "markerWidth"));
        double height = MarkerSize(Attribute(marker, "markerHeight"));
        double sx = 1.0;
        double sy = 1.0;
        List<double> box = SVGState.ParseNumbers(Attribute(marker, "viewBox"));
        if (box != null && box.Count == 4) {
            if (box[2] <= 0.0 || box[3] <= 0.0) {
                return;     // A viewBox of no size draws nothing
            }
            sx = width/box[2];
            sy = height/box[3];
            if (!SVGState.Trim(Attribute(marker, "preserveAspectRatio")).Equals("none")) {
                sx = Math.Min(sx, sy);
                sy = sx;
            }
        }
        double radians = ToRadians(angle);
        double cos = Math.Cos(radians);
        double sin = Math.Sin(radians);
        double[] m = SVGState.Multiply(state.matrix, new double[] {cos, sin, -sin, cos, vertex[0], vertex[1]});
        m = SVGState.Multiply(m, new double[] {units*sx, 0.0, 0.0, units*sy, 0.0, 0.0});
        m = SVGState.Multiply(m, new double[] {
            1.0, 0.0, 0.0, 1.0, -SVGState.ParseCoordinate(Attribute(marker, "refX")),
            -SVGState.ParseCoordinate(Attribute(marker, "refY"))});
        // The marker inherits from the svg element, not from the path.
        SVGState svg = new SVGState();
        svg.paints = state.paints;
        svg.clips = state.clips;
        svg.matrix = m;
        stack.Add(svg);
        reusing.Add(marker);
        Draw(marker, stack, rules, ref root, true);
        reusing.Remove(marker);
        stack.RemoveAt(stack.Count - 1);
    }

    // Reads the markerWidth or the markerHeight: 3 unless given.
    private static double MarkerSize(String value) {
        if (SVGState.Trim(value).Length == 0) {
            return 3.0;
        }
        double size = SVGState.ParseCoordinate(value);
        return (size > 0.0) ? size : 0.0;
    }

    // Adds a text element, with its tspan elements, as the runs of its
    // characters.
    private void AddText(XMLNode node, SVGStyleSheet rules, SVGState state,
            Dictionary<String, String> attributes, int depth) {
        SVGText text = new SVGText();
        text.matrix = state.matrix;
        AddRuns(text, node, rules, state, attributes, new double[] {double.NaN, double.NaN, 0.0, 0.0}, depth);
        text.CollapseSpaces();
        bool empty = true;
        int count = 0;
        foreach (SVGText.Run run in text.runs) {
            if (run.text.Length > 0) {
                empty = false;
            }
            count += ClipOperations(state.clips);
            if (reusing.Count > 0) {
                count += 1 + run.text.Length;
            }
        }
        CountOperations(count);
        double[] m = state.matrix;
        if (empty || m[0]*m[3] - m[1]*m[2] == 0.0) {
            return;
        }
        SVGPath path = new SVGPath();
        path.operations = new List<PathOp>();
        path.text = text;
        path.clips = state.clips;
        paths.Add(path);
    }

    // Adds the runs of a text or a tspan element. Its x, y, dx and dy apply to
    // its first character, which may be in a tspan in it; they are kept in
    // pending until then. A tspan counts as a level of the depth of the
    // elements.
    private void AddRuns(SVGText text, XMLNode node, SVGStyleSheet rules,
            SVGState state, Dictionary<String, String> attributes, double[] pending, int depth) {
        if (depth > MAX_DEPTH) {
            throw DepthError();
        }
        double x = TextLength(Attribute(attributes, "x"), state.fontSize);
        double y = TextLength(Attribute(attributes, "y"), state.fontSize);
        double dx = TextLength(Attribute(attributes, "dx"), state.fontSize);
        double dy = TextLength(Attribute(attributes, "dy"), state.fontSize);
        if (!double.IsNaN(x)) {
            pending[0] = x;
            pending[2] = 0.0;
        }
        if (!double.IsNaN(y)) {
            pending[1] = y;
            pending[3] = 0.0;
        }
        if (!double.IsNaN(dx)) {
            pending[2] += dx;
        }
        if (!double.IsNaN(dy)) {
            pending[3] += dy;
        }
        foreach (object item in node.GetContent()) {
            String characters = item as String;
            if (characters != null) {
                SVGText.Run run = new SVGText.Run();
                run.text = characters;
                run.x = pending[0];
                run.y = pending[1];
                run.dx = pending[2];
                run.dy = pending[3];
                pending[0] = double.NaN;
                pending[1] = double.NaN;
                pending[2] = 0.0;
                pending[3] = 0.0;
                run.fontSize = state.fontSize;
                run.weight = state.fontWeight;
                run.italic = state.italic;
                run.family = state.fontFamily;
                run.anchor = state.textAnchor;
                run.baseline = state.baseline;
                run.fill = state.fillCurrent ? state.color : state.fill;
                run.stroke = state.strokeCurrent ? state.color : state.stroke;
                run.fillAlpha = state.DrawnFillAlpha();
                run.strokeAlpha = state.DrawnStrokeAlpha();
                if (state.invisible) {
                    // It keeps its place in the text
                    run.fill = Color.transparent;
                    run.stroke = Color.transparent;
                }
                run.strokeWidth = state.strokeWidth;
                run.strokeFirst = state.strokeFirst;
                run.lineJoin = state.lineJoin;
                text.runs.Add(run);
            } else {
                XMLNode child = (XMLNode) item;
                String name = child.GetLocalName();
                if (!name.Equals("tspan") && !name.Equals("a")) {
                    continue;   // A title or a desc is not drawn
                }
                List<String> childOrder = new List<String>();
                Dictionary<String, String> childAttributes = Attributes(child, childOrder);
                SVGState childState = ElementState(state, rules, childAttributes, childOrder);
                childState.matrix = state.matrix;   // A tspan has no transform
                if (!childState.hidden) {
                    AddRuns(text, child, rules, childState, childAttributes, pending, depth + 1);
                }
            }
        }
    }

    // Reads the first length of an x, y, dx or dy of a text, which may be in
    // em of its font size, and returns NaN for none.
    private static double TextLength(String value, float fontSize) {
        List<String> fields = SVGState.Fields(value.Replace(',', ' '));
        if (fields.Count == 0) {
            return double.NaN;
        }
        String first = fields[0];
        double length;
        if (first.EndsWith("em", StringComparison.Ordinal)) {
            double number;
            if (!SVGState.ParseNumber(first.Substring(0, first.Length - 2), false, out number)) {
                return double.NaN;
            }
            length = number*fontSize;
        } else if (first.EndsWith("%", StringComparison.Ordinal)) {
            return double.NaN;
        } else {
            float points;
            if (!SVGState.ParseLength(first, out points)) {
                return double.NaN;
            }
            length = points;
        }
        return double.IsInfinity(length) ? double.NaN : length;
    }

    // Returns the width or the height of the svg element in points, and 0 for
    // a length that PDFjet cannot read, a percentage among them, which leaves
    // the size to the viewBox.
    private static float ParseLength(String value) {
        String text = value.Trim();
        float scale = 1f;
        for (int i = 0; i < SVGState.UNITS.Length; i++) {
            if (text.EndsWith(SVGState.UNITS[i], StringComparison.Ordinal)) {
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
            throw new InvalidDataException("Invalid SVG viewBox \"" + viewBox + "\": four numbers are needed.");
        }
        for (int i = 0; i < 4; i++) {
            box[i] = (float) list[i];
        }
        if (box[2] == 0f || box[3] == 0f) {
            throw new InvalidDataException(
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
        List<SVGPath> all = new List<SVGPath>(paths);
        all.AddRange(clipShapes);
        foreach (SVGPath path in all) {
            path.strokeWidth *= strokeScale;
            if (path.dashArray != null) {
                for (int i = 0; i < path.dashArray.Length; i++) {
                    path.dashArray[i] *= strokeScale;
                }
                path.dashOffset *= strokeScale;
            }
            if (path.text != null) {
                double[] m = path.text.matrix;
                double ax = uniform ? sx : w/box[2];
                double ay = uniform ? sy : h/box[3];
                double ox = uniform ? tx : 0.0;
                double oy = uniform ? ty : 0.0;
                path.text.matrix = new double[] {
                    m[0]*ax, m[1]*ay, m[2]*ax, m[3]*ay, (m[4] - box[0])*ax + ox, (m[5] - box[1])*ay + oy};
            }
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
        List<SVGPath> all = new List<SVGPath>(paths);
        all.AddRange(clipShapes);
        foreach (SVGPath path in all) {
            path.strokeWidth *= factor;
            if (path.dashArray != null) {
                for (int i = 0; i < path.dashArray.Length; i++) {
                    path.dashArray[i] *= factor;
                }
                path.dashOffset *= factor;
            }
            if (path.text != null) {
                double[] m = path.text.matrix;
                path.text.matrix = new double[] {
                    m[0]*factor, m[1]*factor, m[2]*factor, m[3]*factor, m[4]*factor, m[5]*factor};
            }
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

    /// <summary>
    /// Sets the font of the text of the image, in place of the fonts PDFjet
    /// carries: for the text of a language they do not have, such as Chinese,
    /// or for a font of one's own.
    /// </summary>
    /// <param name="font">the font.</param>
    /// <returns>this SVGImage object.</returns>
    public SVGImage SetFont(Font font) {
        this.font = font;
        return this;
    }

    /// <summary>
    /// Sets the font of the bold text of the image, whose font-weight is bold
    /// or 600 or more, when a font is set with SetFont; unless set, that font
    /// draws the bold text too.
    /// </summary>
    /// <param name="font">the bold font.</param>
    /// <returns>this SVGImage object.</returns>
    public SVGImage SetBoldFont(Font font) {
        this.boldFont = font;
        return this;
    }

    // Returns the font of a run: the one set, the bold one for a weight of 600
    // or more when it is set, or else the face of the fonts PDFjet carries of
    // its family, weight and style, from resources/fonts or the library. With
    // neither, as the small DLL run away from resources/fonts, it throws: the
    // text is never drawn in a core font.
    private Font FontOf(SVGText.Run run, PDF pdf) {
        if (font != null) {
            return (run.weight >= 600 && boldFont != null) ? boldFont : font;
        }
        Font bundled = BundledFonts.Get(pdf, run.family, run.weight, run.italic);
        if (bundled == null) {
            pdf.Fail(new InvalidOperationException("The font "
                    + BundledFonts.FileName(run.family, run.weight, run.italic)
                    + " is not one of the fonts PDFjet carries, in resources/fonts or in PDFjet.dll: "
                    + "set a font with SetFont."));
        }
        return bundled;
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
        // PDF/A-1 has no transparency, so there the path is drawn opaque, as
        // an annotation is (review of 12 October 2026, R9).
        bool alpha = !page.pdf.IsPDFA1() && ((fill && path.fillAlpha < 1f) || (stroke && path.strokeAlpha < 1f));
        bool lineCap = stroke && path.lineCap != CapStyle.BUTT;
        bool lineJoin = stroke && path.lineJoin != JoinStyle.MITER;
        bool dash = stroke && path.dashArray != null;
        bool state = alpha || lineCap || lineJoin || dash || path.clips != null;
        if (state) {
            page.SaveGraphicsState();
            Clip(path, page);
            if (dash) {
                page.Append('[');
                for (int i = 0; i < path.dashArray.Length; i++) {
                    if (i > 0) {
                        page.Append(' ');
                    }
                    page.Append(path.dashArray[i]);
                }
                page.Append("] ");
                page.Append(path.dashOffset);
                page.Append(" d\n");
            }
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
            // Z closes a subpath, and the path is stroked once, all of its
            // subpaths together, as SVG strokes it.
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
                } else if (op.cmd == 'Z') {
                    page.Append("h\n");
                }
            }
            page.StrokePath();
        }

        if (state) {
            page.RestoreGraphicsState();
        }
    }

    // Clips what is drawn next to the clip paths of the path, each in turn.
    private void Clip(SVGPath path, Page page) {
        if (path.clips == null) {
            return;
        }
        foreach (SVGPath clip in path.clips) {
            foreach (PathOp op in clip.operations) {
                if (op.cmd == 'M') {
                    page.MoveTo(op.x + x, op.y + y);
                } else if (op.cmd == 'L') {
                    page.LineTo(op.x + x, op.y + y);
                } else if (op.cmd == 'C') {
                    page.CurveTo(op.x1 + x, op.y1 + y, op.x2 + x, op.y2 + y, op.x + x, op.y + y);
                } else if (op.cmd == 'Z') {
                    page.Append("h\n");
                }
            }
            if (clip.evenOdd) {
                page.Append("W*\nn\n");
            } else {
                page.ClipPath();
            }
        }
    }

    // Draws a text: each run filled and then stroked, or stroked first when
    // its paint-order says, with the text matrix that takes its glyphs, whose
    // y goes up, to where the run is on the page.
    private void DrawText(SVGText text, List<SVGPath> clips, Page page) {
        double[] m = text.matrix;
        double scale = Math.Sqrt(Math.Abs(m[0]*m[3] - m[1]*m[2]));
        foreach (SVGText.Run run in text.runs) {
            String str = SVGText.Text(run);
            if (str.Length == 0 || !(run.fontSize > 0f)) {
                continue;
            }
            float[] tm = {
                (float) m[0],
                (float) -m[1],
                (float) -m[2],
                (float) m[3],
                (float) (m[0]*run.px + m[2]*run.py + m[4]) + x,
                page.height - ((float) (m[1]*run.px + m[3]*run.py + m[5]) + y)};
            bool writable = true;
            foreach (float value in tm) {
                writable = writable && FastFloat.IsWritable(value);
            }
            float strokeWidth = (float) (run.strokeWidth*scale);
            bool fill = run.fill != Color.transparent && run.fillAlpha > 0f;
            bool stroke = run.stroke != Color.transparent && run.strokeAlpha > 0f
                    && strokeWidth > 0f && FastFloat.IsWritable(strokeWidth);
            if (!writable || (!fill && !stroke)) {
                continue;
            }
            bool alpha = !page.pdf.IsPDFA1() && ((fill && run.fillAlpha < 1f) || (stroke && run.strokeAlpha < 1f));
            bool lineJoin = stroke && run.lineJoin != JoinStyle.MITER;
            bool state = alpha || lineJoin || clips != null;
            if (state) {
                page.SaveGraphicsState();
                SVGPath clipped = new SVGPath();
                clipped.clips = clips;
                Clip(clipped, page);
                if (alpha) {
                    GraphicsState gs = new GraphicsState();
                    gs.SetAlphaNonStroking(run.fillAlpha);
                    gs.SetAlphaStroking(run.strokeAlpha);
                    page.SetGraphicsState(gs);
                }
                if (lineJoin) {
                    page.SetLineJoinStyle(run.lineJoin);
                }
            }
            for (int pass = 0; pass < 2; pass++) {
                bool strokePass = (pass == 0) == run.strokeFirst;
                if (strokePass && stroke) {
                    page.SetPenColor(run.stroke);
                    page.SetPenWidth(strokeWidth);
                    page.DrawSVGText(run.font, run.fontSize, str, tm, 1);
                } else if (!strokePass && fill) {
                    page.SetBrushColor(run.fill);
                    page.DrawSVGText(run.font, run.fontSize, str, tm, 0);
                }
            }
            if (state) {
                page.RestoreGraphicsState();
            }
        }
    }

    // Returns true when something of the image is drawn outside its size,
    // which is cut off then: a point of a path, wider by half its stroke, or
    // the box of a run of a text, from its ascent to its descent.
    private bool Overflows(Page page) {
        if (w <= 0f || h <= 0f) {
            return false;
        }
        foreach (SVGPath path in paths) {
            if (path.text != null) {
                double[] m = path.text.matrix;
                foreach (SVGText.Run run in path.text.runs) {
                    float[][] corners = {
                        new float[] {run.px, run.py - run.fontSize},
                        new float[] {run.px + run.width, run.py - run.fontSize},
                        new float[] {run.px, run.py + 0.3f*run.fontSize},
                        new float[] {run.px + run.width, run.py + 0.3f*run.fontSize}};
                    foreach (float[] corner in corners) {
                        float[] point = Transform(m, corner[0], corner[1]);
                        if (Outside(point[0], point[1], 0f)) {
                            return true;
                        }
                    }
                }
                continue;
            }
            if (!IsWritable(path, page)) {
                continue;   // Not drawn
            }
            float margin = (path.stroke != Color.transparent) ? path.strokeWidth/2f : 0f;
            foreach (PathOp op in path.operations) {
                if (Outside(op.x, op.y, margin) || (op.cmd == 'C'
                        && (Outside(op.x1, op.y1, margin) || Outside(op.x2, op.y2, margin)))) {
                    return true;
                }
            }
        }
        return false;
    }

    // Returns true for a point outside the size of the image, by more than a
    // hundredth of a point and the margin.
    private bool Outside(float px, float py, float margin) {
        float e = 0.01f + margin;
        return px < -e || py < -e || px > w + e || py > h + e;
    }

    /// <summary>Draws this SVG image on the specified page.</summary>
    public float[] DrawOn(Page page) {
        if (page == null) {
            return new float[] {x + w, y + h};  // Measured, not drawn
        }
        // A linked image is a Figure in the Link its annotation joins
        StructElement link = null;
        if (uri != null || key != null) {
            link = page.BeginLink();
        }
        page.AddBDC(StructElem.FIGURE, language, actualText, altDescription);
        foreach (SVGPath path in paths) {
            if (path.text != null) {
                foreach (SVGText.Run run in path.text.runs) {
                    run.font = FontOf(run, page.pdf);
                }
                path.text.Place();
            }
        }
        bool clip = Overflows(page);
        if (clip) {
            page.SaveGraphicsState();
            page.ClipRect(x, y, w, h);
        }
        foreach (SVGPath path in paths) {
            if (path.text != null) {
                DrawText(path.text, path.clips, page);
            } else {
                drawPath(path, page);
            }
        }
        if (clip) {
            page.RestoreGraphicsState();
        }
        page.SetFigureBoundingBox(x, y, w, h);
        page.AddEMC();
        page.EndLink(link);
        if (uri != null || key != null) {
            Annotation linkAnnotation = new Annotation(
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
                    altDescription);
            linkAnnotation.linkElement = link;
            page.AddAnnotation(linkAnnotation);
        }
        return new float[] {x + w, y + h};
    }
}   // End of SVGImage.cs
}   // End of PDFjet.NET namespace