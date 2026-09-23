/*
 * SVGImage.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

import javax.xml.stream.XMLInputFactory;
import javax.xml.stream.XMLStreamConstants;
import javax.xml.stream.XMLStreamException;
import javax.xml.stream.XMLStreamReader;

/**
 * Used to embed SVG images in the PDF document.
 *
 * It draws the path, rect, circle, ellipse, line, polyline and polygon
 * elements, in groups or not, with their transforms, and the fill, stroke,
 * stroke-width, fill-rule, stroke-linecap, stroke-linejoin, opacity,
 * fill-opacity and stroke-opacity they have or inherit, as attributes, in a
 * style attribute or in rules for their classes in a style element that comes
 * before them. The width, height, viewBox and preserveAspectRatio of the svg
 * element give the size of the image.
 */
public class SVGImage implements Drawable {
    float x = 0f;
    float y = 0f;
    float w = 0f;       // SVG width
    float h = 0f;       // SVG height
    String viewBox = "";
    String preserveAspectRatio = "";

    List<SVGPath> paths = null;
    /** The URI opened when the image is clicked. */
    protected String uri = null;
    /** The destination key used by the GoTo action. */
    protected String key = null;
    private String language = null;
    private String actualText = null;
    private String altDescription = null;

    /**
     * Used to embed SVG images in the PDF document.
     *
     * @param filePath the file path.
     * @throws Exception if exception occurred.
     */
    public SVGImage(String filePath) throws Exception {
        try (InputStream stream = new BufferedInputStream(new FileInputStream(filePath))) {
            read(stream);
        }
    }

    /**
     * Used to embed SVG images in the PDF document.
     *
     * @param stream the input stream.
     * @throws Exception if exception occurred.
     */
    public SVGImage(InputStream stream) throws Exception {
        read(stream);
    }

    // The elements whose content is not drawn where it is, but used by other
    // elements, which PDFjet does not draw.
    private static final Set<String> TEMPLATES = new HashSet<String>(Arrays.asList(
            "defs", "clipPath", "mask", "pattern", "symbol",
            "marker", "linearGradient", "radialGradient"));

    private void read(InputStream stream) throws Exception {
        ColorMap colorMap = new ColorMap();
        paths = new ArrayList<SVGPath>();
        List<SVGState.Rule> rules = new ArrayList<SVGState.Rule>();
        List<SVGState> stack = new ArrayList<SVGState>();
        stack.add(new SVGState());
        List<String> names = new ArrayList<String>();
        StringBuilder styleSheet = new StringBuilder();
        boolean root = true;

        XMLInputFactory factory = XMLInputFactory.newFactory();
        // Disable DTD and external entity processing to prevent XXE attacks.
        factory.setProperty(XMLInputFactory.SUPPORT_DTD, false);
        factory.setProperty(
                "javax.xml.stream.isSupportingExternalEntities", Boolean.FALSE);

        XMLStreamReader reader = factory.createXMLStreamReader(stream, "UTF-8");
        try {
            while (reader.hasNext()) {
                int event = reader.next();
                if (event == XMLStreamConstants.START_ELEMENT) {
                    String name = reader.getLocalName();
                    // Only the attributes of no namespace are SVG's.
                    Map<String, String> attributes = new HashMap<String, String>();
                    List<String> order = new ArrayList<String>();
                    for (int i = 0; i < reader.getAttributeCount(); i++) {
                        String namespace = reader.getAttributeNamespace(i);
                        if (namespace == null || namespace.isEmpty()) {
                            attributes.put(reader.getAttributeLocalName(i), reader.getAttributeValue(i));
                            order.add(reader.getAttributeLocalName(i));
                        }
                    }
                    SVGState state = elementState(
                            stack.get(stack.size() - 1), colorMap, rules, attributes, order);
                    if (TEMPLATES.contains(name)) {
                        state.hidden = true;
                    }
                    stack.add(state);
                    names.add(name);

                    if (name.equals("svg") && root) {
                        root = false;
                        this.w = parseLength(attribute(attributes, "width"));
                        this.h = parseLength(attribute(attributes, "height"));
                        this.viewBox = attribute(attributes, "viewBox");
                        this.preserveAspectRatio = attribute(attributes, "preserveAspectRatio");
                    }
                    List<PathOp> operations = shapeOperations(name, attributes);
                    if (name.equals("line")) {
                        state = new SVGState(state);
                        state.fill = Color.transparent;     // A line has no inside to fill
                        state.fillCurrent = false;
                    }
                    addPath(state, operations);
                } else if (event == XMLStreamConstants.END_ELEMENT) {
                    if (!names.isEmpty() && names.get(names.size() - 1).equals("style")) {
                        rules.addAll(SVGState.parseStyleSheet(styleSheet.toString()));
                        styleSheet.setLength(0);
                    }
                    if (stack.size() > 1) {
                        stack.remove(stack.size() - 1);
                        names.remove(names.size() - 1);
                    }
                } else if (event == XMLStreamConstants.CHARACTERS ||
                        event == XMLStreamConstants.CDATA ||
                        event == XMLStreamConstants.SPACE) {
                    if (!names.isEmpty() && names.get(names.size() - 1).equals("style")) {
                        styleSheet.append(reader.getText());
                    }
                }
            }
        } catch (XMLStreamException e) {
            throw new Exception("Failed to parse SVG: " + e.getMessage(), e);
        } finally {
            try {
                reader.close();
            } catch (XMLStreamException e) {
                // Nothing actionable here.
            }
        }

        processPaths(paths);
    }

    // Returns the value of the attribute, or an empty one for an attribute
    // that is not given.
    private static String attribute(Map<String, String> attributes, String name) {
        String value = attributes.get(name);
        return (value == null) ? "" : value;
    }

    // Returns the state of an element whose parent has the given one: its
    // presentation attributes, in their order, then the rules of the style
    // sheet for its classes, in the order of the style sheet, then its style
    // attribute, and its transform.
    private static SVGState elementState(SVGState parent, ColorMap colorMap, List<SVGState.Rule> rules,
            Map<String, String> attributes, List<String> order) {
        SVGState state = new SVGState(parent);
        state.ownOpacity = 1f;
        for (String name : order) {
            state.setProperty(colorMap, name, attributes.get(name));
        }
        List<String> classes = SVGState.fields(attribute(attributes, "class"));
        if (!classes.isEmpty()) {
            for (SVGState.Rule rule : rules) {
                if (classes.contains(rule.className)) {
                    for (String[] declaration : rule.declarations) {
                        state.setProperty(colorMap, declaration[0], declaration[1]);
                    }
                }
            }
        }
        for (String[] declaration : SVGState.parseDeclarations(attribute(attributes, "style"))) {
            state.setProperty(colorMap, declaration[0], declaration[1]);
        }
        state.opacity *= state.ownOpacity;
        String transform = attributes.get("transform");
        if (transform != null) {
            double[] matrix = SVGState.parseTransform(transform);
            if (matrix != null) {
                state.matrix = SVGState.multiply(parent.matrix, matrix);
            }
        }
        return state;
    }

    // How far the control points of the cubic curve that draws a quarter of a
    // circle are from its ends, for a radius of 1.
    private static final double KAPPA = 0.5522847498307936;

    // Builds the PDF path operations of a shape.
    private static class ShapeBuilder {
        List<PathOp> operations = new ArrayList<PathOp>();
        double x0;  // The start of the subpath
        double y0;

        void moveTo(double x, double y) {
            x0 = x;
            y0 = y;
            operations.add(new PathOp('M', (float) x, (float) y));
        }

        void lineTo(double x, double y) {
            operations.add(new PathOp('L', (float) x, (float) y));
        }

        void curveTo(double x1, double y1, double x2, double y2, double x, double y) {
            PathOp op = new PathOp('C');
            op.setCubicPoints((float) x1, (float) y1, (float) x2, (float) y2, (float) x, (float) y);
            operations.add(op);
        }

        void closePath() {
            operations.add(new PathOp('Z', (float) x0, (float) y0));
        }

        // Adds the ellipse as four cubic curves, from its right end.
        void ellipse(double cx, double cy, double rx, double ry) {
            double kx = KAPPA*rx;
            double ky = KAPPA*ry;
            moveTo(cx + rx, cy);
            curveTo(cx + rx, cy + ky, cx + kx, cy + ry, cx, cy + ry);
            curveTo(cx - kx, cy + ry, cx - rx, cy + ky, cx - rx, cy);
            curveTo(cx - rx, cy - ky, cx - kx, cy - ry, cx, cy - ry);
            curveTo(cx + kx, cy - ry, cx + rx, cy - ky, cx + rx, cy);
            closePath();
        }
    }

    // Returns the PDF path operations of an element that draws a path or a
    // basic shape, in its user space, and none for another element or a shape
    // of no size.
    private static List<PathOp> shapeOperations(String name, Map<String, String> attributes) {
        ShapeBuilder b = new ShapeBuilder();
        if (name.equals("path")) {
            return SVG.toPDF(SVG.getOperations(attribute(attributes, "d")));
        } else if (name.equals("rect")) {
            double x = number(attributes, "x");
            double y = number(attributes, "y");
            double w = number(attributes, "width");
            double h = number(attributes, "height");
            if (w <= 0.0 || h <= 0.0) {
                return b.operations;
            }
            // A radius that is not given is the other one, and neither is
            // more than half the side.
            double[] rx = radius(attribute(attributes, "rx"));
            double[] ry = radius(attribute(attributes, "ry"));
            if (rx == null) {
                rx = ry;
            }
            if (ry == null) {
                ry = rx;
            }
            double rx0 = Math.min((rx == null) ? 0.0 : rx[0], w/2.0);
            double ry0 = Math.min((ry == null) ? 0.0 : ry[0], h/2.0);
            if (rx0 <= 0.0 || ry0 <= 0.0) {
                b.moveTo(x, y);
                b.lineTo(x + w, y);
                b.lineTo(x + w, y + h);
                b.lineTo(x, y + h);
                b.closePath();
                return b.operations;
            }
            double kx = KAPPA*rx0;
            double ky = KAPPA*ry0;
            b.moveTo(x + rx0, y);
            b.lineTo(x + w - rx0, y);
            b.curveTo(x + w - rx0 + kx, y, x + w, y + ry0 - ky, x + w, y + ry0);
            b.lineTo(x + w, y + h - ry0);
            b.curveTo(x + w, y + h - ry0 + ky, x + w - rx0 + kx, y + h, x + w - rx0, y + h);
            b.lineTo(x + rx0, y + h);
            b.curveTo(x + rx0 - kx, y + h, x, y + h - ry0 + ky, x, y + h - ry0);
            b.lineTo(x, y + ry0);
            b.curveTo(x, y + ry0 - ky, x + rx0 - kx, y, x + rx0, y);
            b.closePath();
        } else if (name.equals("circle")) {
            double r = number(attributes, "r");
            if (r <= 0.0) {
                return b.operations;
            }
            b.ellipse(number(attributes, "cx"), number(attributes, "cy"), r, r);
        } else if (name.equals("ellipse")) {
            double rx = number(attributes, "rx");
            double ry = number(attributes, "ry");
            if (rx <= 0.0 || ry <= 0.0) {
                return b.operations;
            }
            b.ellipse(number(attributes, "cx"), number(attributes, "cy"), rx, ry);
        } else if (name.equals("line")) {
            b.moveTo(number(attributes, "x1"), number(attributes, "y1"));
            b.lineTo(number(attributes, "x2"), number(attributes, "y2"));
        } else if (name.equals("polyline") || name.equals("polygon")) {
            double[] points = SVGState.parseNumbers(attribute(attributes, "points"));
            if (points == null || points.length < 4) {
                return b.operations;    // Drawn as far as it can be read, which is not a line
            }
            b.moveTo(points[0], points[1]);
            for (int i = 2; i + 1 < points.length; i += 2) {
                b.lineTo(points[i], points[i + 1]);
            }
            if (name.equals("polygon")) {
                b.closePath();
            }
        }
        return b.operations;
    }

    // Returns a coordinate or a size of a shape.
    private static double number(Map<String, String> attributes, String name) {
        return SVGState.parseCoordinate(attribute(attributes, name));
    }

    // Reads the rx or the ry of a rectangle; it returns null when it is not
    // given, is auto or is not a length of zero or more.
    private static double[] radius(String value) {
        value = SVGState.trim(value);
        if (value.isEmpty() || value.equals("auto")) {
            return null;
        }
        Float length = SVGState.parseLength(value);
        if (length == null || length < 0f || value.endsWith("%")) {
            return null;
        }
        return new double[] {(double) length};
    }

    // Adds the operations of a path or a shape to the image, in the space of
    // the svg element, with what the state draws them with, unless they draw
    // nothing: a shape in <defs> or of no size, or with neither a fill nor a
    // stroke.
    private void addPath(SVGState state, List<PathOp> operations) {
        double[] m = state.matrix;
        double det = m[0]*m[3] - m[1]*m[2];
        if (state.hidden || operations.isEmpty() || det == 0.0) {
            return;
        }
        SVGPath path = new SVGPath();
        path.operations = operations;
        path.fill = state.fillCurrent ? state.color : state.fill;
        path.stroke = state.strokeCurrent ? state.color : state.stroke;
        path.fillAlpha = state.fillOpacity*state.opacity;
        path.strokeAlpha = state.strokeOpacity*state.opacity;
        if (path.fillAlpha == 0f) {
            path.fill = Color.transparent;
        }
        if (path.strokeAlpha == 0f || state.strokeWidth == 0f) {
            path.stroke = Color.transparent;
        }
        if (path.fill == Color.transparent && path.stroke == Color.transparent) {
            return;
        }
        path.strokeWidth = (float) (((double) state.strokeWidth)*Math.sqrt(Math.abs(det)));
        path.evenOdd = state.evenOdd;
        path.lineCap = state.lineCap;
        path.lineJoin = state.lineJoin;
        if (!SVGState.isIdentity(m)) {
            for (PathOp op : operations) {
                float[] point = transform(m, op.x, op.y);
                op.x = point[0];
                op.y = point[1];
                if (op.cmd == 'C') {
                    point = transform(m, op.x1, op.y1);
                    op.x1 = point[0];
                    op.y1 = point[1];
                    point = transform(m, op.x2, op.y2);
                    op.x2 = point[0];
                    op.y2 = point[1];
                }
            }
        }
        paths.add(path);
    }

    // Returns the point that the matrix takes the point to.
    private static float[] transform(double[] m, float x, float y) {
        return new float[] {
            (float) (m[0]*x + m[2]*y + m[4]),
            (float) (m[1]*x + m[3]*y + m[5]),
        };
    }

    // Returns the width or the height of the svg element in points, and 0 for
    // a length that PDFjet cannot read, a percentage among them, which leaves
    // the size to the viewBox.
    private static float parseLength(String value) {
        String text = SVGState.trim(value);
        float scale = 1f;
        for (int i = 0; i < SVGState.UNITS.length; i++) {
            if (text.endsWith(SVGState.UNITS[i])) {
                text = SVGState.trim(text.substring(0, text.length() - SVGState.UNITS[i].length()));
                scale = SVGState.POINTS[i];
                break;
            }
        }
        try {
            double number = SVGState.parseNumber(text, true);
            if (Double.isInfinite(number) || Double.isNaN(number)) {
                return 0f;
            }
            return ((float) number)*scale;
        } catch (NumberFormatException e) {
            return 0f;
        }
    }

    private void processPaths(List<SVGPath> paths) throws Exception {
        if (viewBox.isEmpty()) {
            return;
        }
        float[] box = new float[4];
        double[] list = SVGState.parseNumbers(viewBox);
        if (list == null || list.length != 4) {
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
            w = h*box[2]/box[3];
        } else if (h == 0f) {
            h = w*box[3]/box[2];
        }

        // The viewBox is scaled to the size, and, unless preserveAspectRatio
        // is none, by the same factor across and down, as large as it fits
        // (meet) or as small as it covers (slice), and placed as it says, in
        // the middle by default.
        float sx = w/box[2];
        float sy = h/box[3];
        float tx = 0f;
        float ty = 0f;
        List<String> fields = SVGState.fields(preserveAspectRatio);
        if (!fields.isEmpty() && fields.get(0).equals("defer")) {
            fields.remove(0);
        }
        String align = "xMidYMid";
        if (!fields.isEmpty()) {
            align = fields.get(0);
        }
        boolean uniform = !align.equals("none") && sx != sy;
        if (uniform) {
            float scale = Math.min(sx, sy);
            if (fields.size() > 1 && fields.get(1).equals("slice")) {
                scale = Math.max(sx, sy);
            }
            sx = scale;
            sy = scale;
            // The value is read in bytes of UTF-8, as the Go port reads it.
            byte[] bytes = align.getBytes(StandardCharsets.UTF_8);
            if (bytes.length != 8) {
                bytes = "xMidYMid".getBytes(StandardCharsets.US_ASCII);
            }
            tx = alignOffset(new String(bytes, 1, 3, StandardCharsets.ISO_8859_1), w - box[2]*scale);
            ty = alignOffset(new String(bytes, 5, 3, StandardCharsets.ISO_8859_1), h - box[3]*scale);
        }
        float strokeScale = (float) Math.sqrt(((double) sx)*((double) sy));
        for (SVGPath path : paths) {
            path.strokeWidth *= strokeScale;
            for (PathOp op : path.operations) {
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
    private static float alignOffset(String align, float space) {
        if (align.equals("Min")) {
            return 0f;
        } else if (align.equals("Max")) {
            return space;
        }
        return space/2f;
    }

    /**
     * Sets the location of this SVG on the page.
     *
     * @param x the x coordinate of the top left corner of this box when drawn on the page.
     * @param y the y coordinate of the top left corner of this box when drawn on the page.
     * @return this SVG object.
     */
    public SVGImage setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     * Scales the SVG image.
     *
     * @param factor the scale factor.
     * @return this SVGImage object.
     */
    public SVGImage scaleBy(float factor) {
        for (SVGPath path : paths) {
            path.strokeWidth *= factor;
            for (PathOp op : path.operations) {
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

    /**
     * Sets the URI for the "click box" action.
     *
     * @param uri the URI.
     * @return this SVGImage object.
     */
    public SVGImage setURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    /**
     * Sets the destination key for the action.
     *
     * @param key the destination name.
     * @return this SVGImage object.
     */
    public SVGImage setGoToAction(String key) {
        this.key = key;
        return this;
    }

    /**
     * Sets the alternate description of this image.
     *
     * @param altDescription the alternate description.
     * @return this SVGImage object.
     */
    public SVGImage setAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /**
     * Sets the actual text of this image.
     *
     * @param actualText the actual text.
     * @return this SVGImage object.
     */
    public SVGImage setActualText(String actualText) {
        this.actualText = actualText;
        return this;
    }

    /**
     * Sets the language of this image.
     *
     * @param language the language, for example "en-US".
     * @return this SVGImage object.
     */
    public SVGImage setLanguage(String language) {
        this.language = language;
        return this;
    }

    /**
     * Returns the width of the SVG image.
     *
     * @return the width of the SVG image.
     */
    public float getWidth() {
        return this.w;
    }

    // Whether a PDF can hold the points and the stroke width of the path
    // where it is drawn: a transform or a scale such as scale(1e30) can take
    // them out of its range, and such a path is not drawn.
    private boolean isWritable(SVGPath path, Page page) {
        if (!FastFloat.isWritable(path.strokeWidth)) {
            return false;
        }
        for (PathOp op : path.operations) {
            if (!isWritable(op.x, op.y, page) || op.cmd == 'C' &&
                    (!isWritable(op.x1, op.y1, page) || !isWritable(op.x2, op.y2, page))) {
                return false;
            }
        }
        return true;
    }

    private boolean isWritable(float x, float y, Page page) {
        x += this.x;
        y += this.y;
        return FastFloat.isWritable(x) && FastFloat.isWritable(y) && FastFloat.isWritable(page.height - y);
    }

    /**
     * Returns the height of the SVG image.
     *
     * @return the height of the SVG image.
     */
    public float getHeight() {
        return this.h;
    }

    // Draws a path, filled and then stroked. An opacity, a line cap and a
    // line join are set in a graphics state of the path's own.
    private void drawPath(SVGPath path, Page page) {
        boolean fill = path.fill != Color.transparent;
        boolean stroke = path.stroke != Color.transparent;
        if (path.operations.isEmpty() || !fill && !stroke || !isWritable(path, page)) {
            return;
        }
        boolean alpha = fill && path.fillAlpha < 1f || stroke && path.strokeAlpha < 1f;
        boolean lineCap = stroke && path.lineCap != CapStyle.BUTT;
        boolean lineJoin = stroke && path.lineJoin != JoinStyle.MITER;
        boolean state = alpha || lineCap || lineJoin;
        if (state) {
            page.saveGraphicsState();
            if (alpha) {
                GraphicsState gs = new GraphicsState();
                gs.setAlphaNonStroking(path.fillAlpha);
                gs.setAlphaStroking(path.strokeAlpha);
                page.setGraphicsState(gs);
            }
            if (lineCap) {
                page.setLineCapStyle(path.lineCap);
            }
            if (lineJoin) {
                page.setLineJoinStyle(path.lineJoin);
            }
        }

        if (fill) {
            page.setBrushColor(path.fill);
            for (PathOp op : path.operations) {
                if (op.cmd == 'M') {
                    page.moveTo(op.x + x, op.y + y);
                } else if (op.cmd == 'L') {
                    page.lineTo(op.x + x, op.y + y);
                } else if (op.cmd == 'C') {
                    page.curveTo(
                        op.x1 + x, op.y1 + y,
                        op.x2 + x, op.y2 + y,
                        op.x + x, op.y + y);
                }
            }
            if (path.evenOdd) {
                page.append("f*\n");
            } else {
                page.fillPath();
            }
        }

        if (stroke) {
            page.setPenColor(path.stroke);
            page.setPenWidth(path.strokeWidth);
            // Z closes and strokes a subpath; a path that is still open at the
            // end is stroked without closing it.
            boolean open = false;
            for (PathOp op : path.operations) {
                if (op.cmd == 'M') {
                    page.moveTo(op.x + x, op.y + y);
                    open = true;
                } else if (op.cmd == 'L') {
                    page.lineTo(op.x + x, op.y + y);
                    open = true;
                } else if (op.cmd == 'C') {
                    page.curveTo(
                        op.x1 + x, op.y1 + y,
                        op.x2 + x, op.y2 + y,
                        op.x + x, op.y + y);
                    open = true;
                } else if (op.cmd == 'Z') {
                    page.closePath();
                    open = false;
                }
            }
            if (open) {
                page.strokePath();
            }
        }

        if (state) {
            page.restoreGraphicsState();
        }
    }

    /**
     * Draws the SVGImage on the page.
     *
     * @param page the page.
     * @return the array containing the x and y of the SVG image.
     */
    public float[] drawOn(Page page) {
        if (page == null) {
            return new float[] {x + w, y + h};  // Measured, not drawn
        }
        page.addBDC(StructElem.FIGURE, language, actualText, altDescription);
        for (SVGPath path : paths) {
            drawPath(path, page);
        }
        page.addEMC();
        if (uri != null || key != null) {
            page.addAnnotation(new Annotation(
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
}   // End of SVGImage.java