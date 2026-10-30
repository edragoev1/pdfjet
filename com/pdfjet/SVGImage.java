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

/**
 * Used to embed SVG images in the PDF document.
 *
 * It draws the path, rect, circle, ellipse, line, polyline and polygon
 * elements, in groups or not, with their transforms, and the fill, stroke,
 * stroke-width, stroke-dasharray, stroke-dashoffset, fill-rule,
 * stroke-linecap, stroke-linejoin, opacity, fill-opacity, stroke-opacity,
 * paint-order, clip-path and markers they have or inherit, as attributes, in a
 * style attribute or in rules for their classes in a style element that comes
 * before them. It draws the elements a use element names, and text and tspan
 * elements with their x, y, dx, dy, font-size, font-weight, text-anchor and
 * dominant-baseline, in the font set with setFont and setBoldFont, or else
 * in the fonts PDFjet carries, embedded: IBM Plex
 * Sans for sans-serif, Helvetica, Arial and the like, IBM Plex Serif for
 * serif, Times and the like, Cascadia Mono for monospace, Courier and the
 * like, regular, semibold or bold, upright or italic, as the font-family,
 * font-weight and font-style of the text ask. A gradient is drawn in the
 * mean color of its stops. The width,
 * height, viewBox and preserveAspectRatio of the svg element give the size of
 * the image, and what is drawn outside it is cut off, as a browser does.
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
    private Font font = null;
    private Font boldFont = null;

    // While the file is read: the elements by their ids, the clip paths of
    // the image, and the elements drawn again by a use element or as a
    // marker, which are not drawn inside themselves.
    private Map<String, XMLNode> ids;
    private List<SVGPath> clipShapes = new ArrayList<SVGPath>();
    private Set<XMLNode> reusing;
    private long reused = 0;
    private long counted = 0;
    private String[] useSize = null;    // The width and the height of the use element of the element drawn next
    // The elements drawn again, by use elements and as markers, at most: a
    // use of a group of uses, and so on, draws ten to the power of the
    // number of groups, as an XML of entities in entities does.
    static final long MAX_REUSED = 1000000;
    // The path operations drawn again, by use elements and as markers, and
    // written for the clip paths of what is drawn, at most: a use of a path of N
    // operations was N operations for each element counted by the limit above,
    // 506 MB for a file of 200 KB, and a clip path of N operations is read for
    // every element it clips and written for every path in it, 800 MB of page
    // for 320 KB (review of 30 October 2026). The operations of a file drawn
    // once are not counted: they grow with the file.
    static final long MAX_OPERATIONS = 4000000;
    // How deep the elements being drawn may nest, by groups, use elements and
    // markers, a state on the stack for each: a chain of use elements is flat in
    // the file, so the limit of the XML parser does not see it, and each level
    // is a call. The number is the same in the four ports and leaves every one
    // of them room on a stack of 512 KB, where a level takes 6.4 KB of a debug
    // build of Swift (review of 30 October 2026).
    static final int MAX_DEPTH = 64;

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
        paths = new ArrayList<SVGPath>();
        XMLNode document;
        try {
            document = XMLParser.parseSkippingDoctype(stream);
        } catch (XMLException e) {
            throw new Exception("parsing SVG: " + e.getMessage(), e);
        }
        ids = new HashMap<String, XMLNode>();
        reusing = new HashSet<XMLNode>();
        List<XMLNode> elements = new ArrayList<XMLNode>();
        elements.add(document);
        for (int i = 0; i < elements.size(); i++) {
            XMLNode element = elements.get(i);
            String id = element.getAttribute("id");
            if (id != null && !ids.containsKey(id)) {
                ids.put(id, element);   // The first element of an id is the one it names
            }
            elements.addAll(element.getChildren());
        }
        SVGState svg = new SVGState();
        svg.paints = gradientColors(ColorMap.SVG_COLORS);
        List<SVGState> stack = new ArrayList<SVGState>();
        stack.add(svg);
        boolean[] root = {true};
        draw(document, ColorMap.SVG_COLORS, new SVGState.StyleSheet(), stack, root, false);
        processPaths(paths);
        ids = null;
        reusing = null;
    }

    // Returns the colors the gradients are drawn in, by their ids: the mean
    // of the colors of their stops, or of the stops of the gradient they name
    // with href when they have none. A gradient without stops is none, and
    // is left out.
    private Map<String, Integer> gradientColors(ColorMap colorMap) {
        Map<String, Integer> colors = new HashMap<String, Integer>();
        Map<XMLNode, Stops> read = new HashMap<XMLNode, Stops>();
        for (Map.Entry<String, XMLNode> entry : ids.entrySet()) {
            XMLNode gradient = entry.getValue();
            // Up to ten gradients along the hrefs, which may go round in a circle.
            for (int i = 0; i < 10 && gradient != null && isGradient(gradient); i++) {
                Stops stops = read.get(gradient);
                if (stops == null) {
                    stops = gradientStops(gradient, colorMap);
                    read.put(gradient, stops);
                }
                if (stops.found) {
                    colors.put(entry.getKey(), stops.color);
                    break;
                }
                gradient = stops.next;
            }
        }
        return colors;
    }

    // What a gradient itself says of its color: the mean of its stops, if it
    // has any, and the gradient its href names.
    private static class Stops {
        int color = 0;
        boolean found = false;
        XMLNode next = null;
    }

    // Reads the gradient, once however many gradients name it: N gradients
    // that name one of N stops were N x N stops read, 50 s for 3.4 MB (review
    // of 30 October 2026).
    private Stops gradientStops(XMLNode gradient, ColorMap colorMap) {
        long r = 0;
        long g = 0;
        long b = 0;
        int count = 0;
        for (XMLNode stop : gradient.getChildren()) {
            if (!stop.getLocalName().equals("stop")) {
                continue;
            }
            SVGState state = new SVGState();
            state.fill = Color.black;   // stop-color is black unless given
            String color = stop.getAttribute("stop-color");
            if (color != null) {
                state.setProperty(colorMap, "fill", color);
            }
            for (String[] declaration : SVGState.parseDeclarations(attribute(stop, "style"))) {
                if (declaration[0].equals("stop-color")) {
                    state.setProperty(colorMap, "fill", declaration[1]);
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
        stops.next = ids.get(href(gradient));
        return stops;
    }

    private static boolean isGradient(XMLNode node) {
        String name = node.getLocalName();
        return name.equals("linearGradient") || name.equals("radialGradient");
    }

    // Returns the attribute of the element, or an empty value.
    private static String attribute(XMLNode node, String name) {
        String value = node.getAttribute(name);
        return (value == null) ? "" : value;
    }

    // Returns the id that the href of an element names, #id, with or without
    // the xlink prefix, or null.
    private static String href(XMLNode node) {
        String href = null;
        for (Map.Entry<String, String> attribute : node.getAttributes().entrySet()) {
            String name = attribute.getKey();
            if (name.equals("href") || href == null && XMLNode.localNameOf(name).equals("href")) {
                href = attribute.getValue();
            }
        }
        if (href == null) {
            return null;
        }
        href = SVGState.trim(href);
        return (href.length() > 1 && href.charAt(0) == '#') ? href.substring(1) : null;
    }

    // Counts an element drawn again, and throws when there are too many.
    private void countReused() {
        if (++reused > MAX_REUSED) {
            throw new IllegalArgumentException(
                    "The SVG draws more than " + MAX_REUSED + " elements again, with use elements or markers.");
        }
    }

    // Counts operations drawn again or written again, and throws when there
    // are too many.
    private void countOperations(int count) {
        counted += count;
        if (counted > MAX_OPERATIONS) {
            throw new IllegalArgumentException("The SVG draws more than " + MAX_OPERATIONS
                    + " path operations again, with use elements, markers or clip paths.");
        }
    }

    // Returns the operations written for the clip paths of one path, and of
    // a run of a text.
    private static int clipOperations(List<SVGPath> clips) {
        int count = 0;
        if (clips != null) {
            for (SVGPath clip : clips) {
                count += clip.operations.size();
            }
        }
        return count;
    }

    // The error of elements nested too deep.
    private static IllegalArgumentException depthError() {
        return new IllegalArgumentException(
                "The SVG nests elements, use elements and markers more than " + MAX_DEPTH + " deep.");
    }

    // Draws the element, then the ones in it, in the order of the document;
    // the rules of a <style> element apply from its end. An element that is
    // used, by a use element or as a marker, is drawn even where it is a
    // template, as a symbol or a marker is.
    private void draw(XMLNode node, ColorMap colorMap, SVGState.StyleSheet rules,
            List<SVGState> stack, boolean[] root, boolean used) throws Exception {
        if (stack.size() > MAX_DEPTH) {
            throw depthError();
        }
        String name = node.getLocalName();
        if (!reusing.isEmpty()) {
            countReused();
        }
        Map<String, String> attributes = new HashMap<String, String>();
        List<String> order = new ArrayList<String>();
        for (Map.Entry<String, String> attribute : node.getAttributes().entrySet()) {
            // The attributes of no namespace: xlink:href, xml:space and the
            // namespaces' own declarations, xmlns:..., are not read.
            if (attribute.getKey().indexOf(':') == -1) {
                attributes.put(attribute.getKey(), attribute.getValue());
                order.add(attribute.getKey());
            }
        }
        SVGState state = elementState(stack.get(stack.size() - 1), colorMap, rules, attributes, order);
        if (TEMPLATES.contains(name) && !used) {
            state.hidden = true;
        }
        if (state.clipPath != null && !state.hidden) {
            clip(state, colorMap, rules);
        }
        String[] size = useSize;
        useSize = null;
        if (name.equals("svg") && root[0]) {
            root[0] = false;
            this.w = parseLength(attribute(attributes, "width"));
            this.h = parseLength(attribute(attributes, "height"));
            // viewbox in lower case when there is no viewBox, as an HTML
            // page's parser reads an inline svg, whose files keep it
            this.viewBox = attributes.containsKey("viewBox")
                    ? attribute(attributes, "viewBox") : attribute(attributes, "viewbox");
            this.preserveAspectRatio = attribute(attributes, "preserveAspectRatio");
            double[] box = SVGState.parseNumbers(this.viewBox);
            state.viewport = (box != null && box.length == 4)
                    ? new double[] {box[2], box[3]} : new double[] {this.w, this.h};
        } else if (name.equals("svg") || name.equals("symbol") && used) {
            enterViewport(state, attributes, size);
        }
        stack.add(state);

        if (name.equals("text")) {
            if (!state.hidden) {
                addText(node, colorMap, rules, state, attributes, stack.size());
            }
            stack.remove(stack.size() - 1);
            return;
        }
        if (name.equals("use")) {
            XMLNode target = ids.get(href(node));
            if (target != null && !state.hidden && !reusing.contains(target)) {
                // The element is drawn as a child of the use element, moved
                // by its x and y.
                SVGState parent = new SVGState(state);
                parent.matrix = SVGState.multiply(state.matrix, new double[] {
                        1, 0, 0, 1, number(attributes, "x"), number(attributes, "y")});
                stack.add(parent);
                reusing.add(target);
                useSize = new String[] {attribute(attributes, "width"), attribute(attributes, "height")};
                draw(target, colorMap, rules, stack, root, true);
                reusing.remove(target);
                stack.remove(stack.size() - 1);
            }
            stack.remove(stack.size() - 1);
            return;
        }
        List<PathOp> operations = shapeOperations(name, attributes);
        int count = reusing.isEmpty() ? 0 : operations.size();
        if (!state.hidden && !operations.isEmpty()) {
            count += clipOperations(state.clips) + ((state.dashArray != null) ? state.dashArray.length : 0);
        }
        countOperations(count);
        if (name.equals("line")) {
            state = new SVGState(state);
            state.fill = Color.transparent;     // A line has no inside to fill
            state.fillCurrent = false;
        }
        List<double[]> vertices = null;
        if (!state.hidden && !state.invisible && !operations.isEmpty()
                && (state.markerStart != null || state.markerMid != null || state.markerEnd != null)
                && (name.equals("path") || name.equals("line") || name.equals("polyline") || name.equals("polygon"))) {
            vertices = vertices(operations);    // Before addPath moves the points
        }
        addPath(state, operations);
        if (vertices != null) {
            drawMarkers(state, vertices, colorMap, rules, stack, root);
        }

        for (XMLNode child : node.getChildren()) {
            if (name.equals("switch") && !isSwitchable(child)) {
                continue;
            }
            draw(child, colorMap, rules, stack, root, false);
            if (name.equals("switch")) {
                break;  // A switch draws the first it can
            }
        }
        if (name.equals("style")) {
            rules.add(SVGState.parseStyleSheet(node.getText()));
        }
        stack.remove(stack.size() - 1);
    }

    // Returns the value of the attribute, or an empty one for an attribute
    // that is not given.
    private static String attribute(Map<String, String> attributes, String name) {
        String value = attributes.get(name);
        return (value == null) ? "" : value;
    }

    // The elements PDFjet draws, and a switch can choose.
    private static final Set<String> DRAWN = new HashSet<String>(Arrays.asList(
            "a", "circle", "ellipse", "g", "line", "path", "polygon",
            "polyline", "rect", "svg", "switch", "text", "use"));

    // Returns true when a switch can draw the element: one PDFjet draws,
    // which asks for no extension, as PDFjet has none. The system language
    // is not tested: a PDF is read by anyone.
    private static boolean isSwitchable(XMLNode node) {
        return DRAWN.contains(node.getLocalName()) && node.getAttribute("requiredExtensions") == null;
    }

    // Returns the state of an element whose parent has the given one: its
    // presentation attributes, in their order, then the rules of the style
    // sheet for its classes, in the order of the style sheet, then its style
    // attribute, and its transform.
    private static SVGState elementState(SVGState parent, ColorMap colorMap, SVGState.StyleSheet rules,
            Map<String, String> attributes, List<String> order) {
        SVGState state = new SVGState(parent);
        state.ownOpacity = 1f;
        state.clipPath = null;
        for (String name : order) {
            state.setProperty(colorMap, name, attributes.get(name));
        }
        for (SVGState.Rule rule : rules.forClasses(SVGState.fields(attribute(attributes, "class")))) {
            for (String[] declaration : rule.declarations) {
                state.setProperty(colorMap, declaration[0], declaration[1]);
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
            List<PathOp> operations = SVG.toPDF(SVG.getOperations(attribute(attributes, "d")));
            // Path data starts with a moveto; data that does not is in error
            // from its start, and SVG 1.1 section 8.3.2 draws it up to there.
            if (!operations.isEmpty() && operations.get(0).cmd != 'M') {
                return new ArrayList<PathOp>();
            }
            return operations;
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
        if (state.hidden || state.invisible || operations.isEmpty() || det == 0.0) {
            return;
        }
        SVGPath path = new SVGPath();
        path.operations = operations;
        path.fill = state.fillCurrent ? state.color : state.fill;
        path.stroke = state.strokeCurrent ? state.color : state.stroke;
        path.fillAlpha = state.drawnFillAlpha();
        path.strokeAlpha = state.drawnStrokeAlpha();
        if (path.fillAlpha == 0f) {
            path.fill = Color.transparent;
        }
        if (path.strokeAlpha == 0f || state.strokeWidth == 0f) {
            path.stroke = Color.transparent;
        }
        if (path.fill == Color.transparent && path.stroke == Color.transparent) {
            return;
        }
        double scale = Math.sqrt(Math.abs(det));
        path.strokeWidth = (float) (((double) state.strokeWidth)*scale);
        path.evenOdd = state.evenOdd;
        path.lineCap = state.lineCap;
        path.lineJoin = state.lineJoin;
        path.strokeFirst = state.strokeFirst;
        if (state.dashArray != null) {
            path.dashArray = new float[state.dashArray.length];
            for (int i = 0; i < path.dashArray.length; i++) {
                path.dashArray[i] = (float) (((double) state.dashArray[i])*scale);
            }
            path.dashOffset = (float) (((double) state.dashOffset)*scale);
        }
        path.clips = state.clips;
        transform(m, operations);
        paths.add(path);
    }

    // Moves the points of the operations by the matrix.
    private static void transform(double[] m, List<PathOp> operations) {
        if (SVGState.isIdentity(m)) {
            return;
        }
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

    // The attributes of no namespace of the element, and their order.
    private static Map<String, String> attributes(XMLNode node, List<String> order) {
        Map<String, String> attributes = new HashMap<String, String>();
        for (Map.Entry<String, String> attribute : node.getAttributes().entrySet()) {
            if (attribute.getKey().indexOf(':') == -1) {
                attributes.put(attribute.getKey(), attribute.getValue());
                order.add(attribute.getKey());
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
    private void clip(SVGState state, ColorMap colorMap, SVGState.StyleSheet rules) throws Exception {
        XMLNode clipPath = ids.get(state.clipPath);
        if (clipPath == null || !clipPath.getLocalName().equals("clipPath")
                || SVGState.trim(attribute(clipPath, "clipPathUnits")).equals("objectBoundingBox")) {
            return;
        }
        SVGState base = new SVGState();
        base.matrix = state.matrix;
        List<String> order = new ArrayList<String>();
        Map<String, String> attributes = attributes(clipPath, order);
        base = elementState(base, colorMap, rules, attributes, order);
        base.hidden = false;    // A clipPath element is not displayed anyway
        SVGPath clip = new SVGPath();
        clip.operations = new ArrayList<PathOp>();
        List<List<PathOp>> shapes = new ArrayList<List<PathOp>>();
        for (XMLNode child : clipPath.getChildren()) {
            order = new ArrayList<String>();
            attributes = attributes(child, order);
            SVGState shape = elementState(base, colorMap, rules, attributes, order);
            String name = child.getLocalName();
            if (name.equals("use")) {
                XMLNode target = ids.get(href(child));
                if (target == null) {
                    continue;
                }
                shape.matrix = SVGState.multiply(shape.matrix, new double[] {
                        1, 0, 0, 1, number(attributes, "x"), number(attributes, "y")});
                order = new ArrayList<String>();
                attributes = attributes(target, order);
                shape = elementState(shape, colorMap, rules, attributes, order);
                name = target.getLocalName();
            }
            if (shape.hidden || shape.invisible) {
                continue;
            }
            List<PathOp> operations = shapeOperations(name, attributes);
            if (operations.isEmpty()) {
                continue;
            }
            transform(shape.matrix, operations);
            shapes.add(operations);
            clip.evenOdd = shape.clipEvenOdd;
        }
        for (List<PathOp> operations : shapes) {
            if (shapes.size() > 1) {
                clip.evenOdd = false;
                if (area(operations) < 0.0) {
                    operations = reversed(operations);
                }
            }
            clip.operations.addAll(operations);
        }
        countOperations(clipPath.getChildren().size() + clip.operations.size());
        if (clip.operations.isEmpty()) {
            state.hidden = true;
            return;
        }
        clipShapes.add(clip);
        List<SVGPath> clips = (state.clips == null) ? new ArrayList<SVGPath>() : new ArrayList<SVGPath>(state.clips);
        clips.add(clip);
        state.clips = clips;
    }

    // Sets the user space of a nested svg element, or of a symbol a use
    // element draws: moved by its x and y, and its viewBox fitted to its width
    // and height, which the use element may give, and which are those of the
    // viewport it is in unless given; a percentage is of that viewport. A
    // viewBox or a size of nothing draws nothing.
    private static void enterViewport(SVGState state, Map<String, String> attributes, String[] size) {
        String width = (size != null && !size[0].isEmpty()) ? size[0] : attribute(attributes, "width");
        String height = (size != null && !size[1].isEmpty()) ? size[1] : attribute(attributes, "height");
        double w = viewportLength(width, state.viewport[0], state.viewport[0]);
        double h = viewportLength(height, state.viewport[1], state.viewport[1]);
        double[] m = {1, 0, 0, 1,
                viewportLength(attribute(attributes, "x"), state.viewport[0], 0.0),
                viewportLength(attribute(attributes, "y"), state.viewport[1], 0.0)};
        double[] box = SVGState.parseNumbers(attribute(attributes, "viewBox"));
        if (box == null || box.length != 4) {
            state.viewport = new double[] {w, h};
            state.matrix = SVGState.multiply(state.matrix, m);
            return;
        }
        // A viewport of no known size, of an svg element with neither a size
        // nor a viewBox, leaves the viewBox as it is.
        if (w == 0.0 && width.isEmpty()) {
            w = box[2];
        }
        if (h == 0.0 && height.isEmpty()) {
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
        List<String> fields = SVGState.fields(attribute(attributes, "preserveAspectRatio"));
        if (!fields.isEmpty() && fields.get(0).equals("defer")) {
            fields = fields.subList(1, fields.size());
        }
        String align = fields.isEmpty() ? "xMidYMid" : fields.get(0);
        if (!align.equals("none")) {
            sx = Math.min(sx, sy);
            if (fields.size() > 1 && fields.get(1).equals("slice")) {
                sx = Math.max(w/box[2], h/box[3]);
            }
            sy = sx;
            // The value is read in bytes of UTF-8, as the Go port reads it.
            byte[] bytes = Util.utf8(align);
            if (bytes.length != 8) {
                bytes = "xMidYMid".getBytes(StandardCharsets.US_ASCII);
            }
            tx = alignOffset(new String(bytes, 1, 3, StandardCharsets.ISO_8859_1), 1f)*(w - box[2]*sx);
            ty = alignOffset(new String(bytes, 5, 3, StandardCharsets.ISO_8859_1), 1f)*(h - box[3]*sy);
        }
        m = SVGState.multiply(m, new double[] {sx, 0, 0, sy, tx, ty});
        m = SVGState.multiply(m, new double[] {1, 0, 0, 1, -box[0], -box[1]});
        state.viewport = new double[] {box[2], box[3]};
        state.matrix = SVGState.multiply(state.matrix, m);
    }

    // Reads an x, a y, a width or a height of a viewport, which may be a
    // percentage of the size of the one it is in, the given one if it has none.
    private static double viewportLength(String value, double whole, double given) {
        value = SVGState.trim(value);
        if (value.isEmpty()) {
            return given;
        }
        if (value.endsWith("%")) {
            double number;
            try {
                number = SVGState.parseNumber(value.substring(0, value.length() - 1), false);
            } catch (NumberFormatException e) {
                return given;
            }
            if (Double.isNaN(number) || Double.isInfinite(number)) {
                return given;
            }
            return number*whole/100.0;
        }
        return SVGState.parseCoordinate(value);
    }

    // Returns twice the area of the path, its sign the way it goes round, by
    // the points of its lines and curves, and closed.
    private static double area(List<PathOp> operations) {
        double area = 0.0;
        double x0 = 0.0;
        double y0 = 0.0;
        double x = 0.0;
        double y = 0.0;
        for (PathOp op : operations) {
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
    private static List<PathOp> reversed(List<PathOp> operations) {
        List<PathOp> reversed = new ArrayList<PathOp>(operations.size());
        int start = 0;
        for (int end = 1; end <= operations.size(); end++) {
            if (end < operations.size() && operations.get(end).cmd != 'M') {
                continue;
            }
            PathOp last = operations.get(end - 1);
            reversed.add(new PathOp('M', last.x, last.y));
            for (int i = end - 1; i > start; i--) {
                PathOp op = operations.get(i);
                PathOp before = operations.get(i - 1);
                if (op.cmd == 'C') {
                    PathOp curve = new PathOp('C');
                    curve.setCubicPoints(op.x2, op.y2, op.x1, op.y1, before.x, before.y);
                    reversed.add(curve);
                } else {
                    reversed.add(new PathOp('L', before.x, before.y));
                }
            }
            start = end;
        }
        return reversed;
    }

    // Returns the vertices of the path, where its markers are drawn, in its
    // user space: x, y and the angle of the path there, in degrees, which is
    // halfway between the directions in and out of a vertex in the middle.
    private static List<double[]> vertices(List<PathOp> operations) {
        List<double[]> vertices = new ArrayList<double[]>();  // x, y, in x, in y, out x, out y
        double x = 0.0;
        double y = 0.0;
        int start = 0;      // The vertex where the subpath starts
        for (PathOp op : operations) {
            double[] in;
            double[] out;
            if (op.cmd == 'M') {
                start = vertices.size();
                vertices.add(new double[] {op.x, op.y, 0, 0, 0, 0});
                x = op.x;
                y = op.y;
                continue;
            } else if (op.cmd == 'C') {
                out = direction(x, y, op.x1, op.y1, op.x2, op.y2, op.x, op.y);
                in = direction(op.x, op.y, op.x2, op.y2, op.x1, op.y1, x, y);
                in = new double[] {-in[0], -in[1]};
            } else {
                // A line, or Z, the line back to the start of the subpath.
                out = new double[] {op.x - x, op.y - y};
                in = out;
            }
            if (vertices.isEmpty()) {
                continue;
            }
            double[] last = vertices.get(vertices.size() - 1);
            if (last[4] == 0.0 && last[5] == 0.0) {
                last[4] = out[0];
                last[5] = out[1];
            }
            vertices.add(new double[] {op.x, op.y, in[0], in[1], 0, 0});
            if (op.cmd == 'Z') {
                // The vertex that closes the subpath goes on as its start does.
                double[] first = vertices.get(start);
                double[] closing = vertices.get(vertices.size() - 1);
                closing[4] = first[4];
                closing[5] = first[5];
            }
            x = op.x;
            y = op.y;
        }
        List<double[]> result = new ArrayList<double[]>();
        for (double[] v : vertices) {
            double inLength = Math.hypot(v[2], v[3]);
            double outLength = Math.hypot(v[4], v[5]);
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
            result.add(new double[] {v[0], v[1], Math.atan2(dy, dx)*DEGREES});
        }
        return result;
    }

    // The degrees of a radian and the radians of a degree, which Java 8's
    // Math.toDegrees and toRadians computed in another order, and so in
    // another last bit than the other ports.
    private static final double DEGREES = 57.29577951308232;
    private static final double RADIANS = 0.017453292519943295;

    // Returns the direction from x, y to the first of the points that is not
    // there, as a curve leaves its start.
    private static double[] direction(double x, double y, double... points) {
        for (int i = 0; i + 1 < points.length; i += 2) {
            if (points[i] != x || points[i + 1] != y) {
                return new double[] {points[i] - x, points[i + 1] - y};
            }
        }
        return new double[] {0, 0};
    }

    // Draws the markers of a path at its vertices: the start marker at the
    // first, the end marker at the last and the mid marker at the others.
    private void drawMarkers(SVGState state, List<double[]> vertices, ColorMap colorMap,
            SVGState.StyleSheet rules, List<SVGState> stack, boolean[] root) throws Exception {
        int last = vertices.size() - 1;
        for (int i = 0; i <= last; i++) {
            if (i == 0) {
                drawMarker(state.markerStart, state, vertices.get(i), true, colorMap, rules, stack, root);
            }
            if (i > 0 && i < last) {
                drawMarker(state.markerMid, state, vertices.get(i), false, colorMap, rules, stack, root);
            }
            if (i == last) {
                drawMarker(state.markerEnd, state, vertices.get(i), false, colorMap, rules, stack, root);
            }
        }
    }

    // Draws the marker at the vertex: its refX, refY there, turned as its
    // orient says, and its viewBox fitted to its markerWidth and
    // markerHeight, which are in units of the stroke width unless its
    // markerUnits are userSpaceOnUse.
    private void drawMarker(String id, SVGState state, double[] vertex, boolean start, ColorMap colorMap,
            SVGState.StyleSheet rules, List<SVGState> stack, boolean[] root) throws Exception {
        XMLNode marker = (id == null) ? null : ids.get(id);
        if (marker == null || !marker.getLocalName().equals("marker") || reusing.contains(marker)) {
            return;
        }
        String orient = SVGState.trim(attribute(marker, "orient"));
        double angle = 0.0;
        if (orient.equals("auto")) {
            angle = vertex[2];
        } else if (orient.equals("auto-start-reverse")) {
            angle = start ? vertex[2] + 180.0 : vertex[2];
        } else {
            if (orient.endsWith("deg")) {
                orient = orient.substring(0, orient.length() - 3);
            }
            try {
                angle = SVGState.parseNumber(SVGState.trim(orient), false);
            } catch (NumberFormatException e) {
                angle = 0.0;
            }
            if (Double.isNaN(angle) || Double.isInfinite(angle)) {
                angle = 0.0;
            }
        }
        double units = SVGState.trim(attribute(marker, "markerUnits")).equals("userSpaceOnUse")
                ? 1.0 : state.strokeWidth;
        double width = markerSize(attribute(marker, "markerWidth"));
        double height = markerSize(attribute(marker, "markerHeight"));
        double sx = 1.0;
        double sy = 1.0;
        double[] box = SVGState.parseNumbers(attribute(marker, "viewBox"));
        if (box != null && box.length == 4) {
            if (box[2] <= 0.0 || box[3] <= 0.0) {
                return;     // A viewBox of no size draws nothing
            }
            sx = width/box[2];
            sy = height/box[3];
            if (!SVGState.trim(attribute(marker, "preserveAspectRatio")).equals("none")) {
                sx = Math.min(sx, sy);
                sy = sx;
            }
        }
        double radians = angle*RADIANS;
        double cos = Math.cos(radians);
        double sin = Math.sin(radians);
        double[] m = SVGState.multiply(state.matrix, new double[] {cos, sin, -sin, cos, vertex[0], vertex[1]});
        m = SVGState.multiply(m, new double[] {units*sx, 0, 0, units*sy, 0, 0});
        m = SVGState.multiply(m, new double[] {
                1, 0, 0, 1, -SVGState.parseCoordinate(attribute(marker, "refX")),
                -SVGState.parseCoordinate(attribute(marker, "refY"))});
        // The marker inherits from the svg element, not from the path.
        SVGState base = new SVGState();
        base.paints = state.paints;
        base.clips = state.clips;
        base.matrix = m;
        stack.add(base);
        reusing.add(marker);
        draw(marker, colorMap, rules, stack, root, true);
        reusing.remove(marker);
        stack.remove(stack.size() - 1);
    }

    // Reads the markerWidth or the markerHeight: 3 unless given.
    private static double markerSize(String value) {
        if (SVGState.trim(value).isEmpty()) {
            return 3.0;
        }
        double size = SVGState.parseCoordinate(value);
        return (size > 0.0) ? size : 0.0;
    }

    // Adds a text element, with its tspan elements, as the runs of its
    // characters.
    private void addText(XMLNode node, ColorMap colorMap, SVGState.StyleSheet rules,
            SVGState state, Map<String, String> attributes, int depth) {
        SVGText text = new SVGText();
        text.matrix = state.matrix;
        addRuns(text, node, colorMap, rules, state, attributes, new double[] {Double.NaN, Double.NaN, 0, 0}, depth);
        text.collapseSpaces();
        boolean empty = true;
        int count = 0;
        for (SVGText.Run run : text.runs) {
            if (!run.text.isEmpty()) {
                empty = false;
            }
            count += clipOperations(state.clips);
            if (!reusing.isEmpty()) {
                count += 1 + run.text.length();
            }
        }
        countOperations(count);
        double[] m = state.matrix;
        if (empty || m[0]*m[3] - m[1]*m[2] == 0.0) {
            return;
        }
        SVGPath path = new SVGPath();
        path.operations = new ArrayList<PathOp>();
        path.text = text;
        path.clips = state.clips;
        paths.add(path);
    }

    // Adds the runs of a text or a tspan element. Its x, y, dx and dy apply to
    // its first character, which may be in a tspan in it; they are kept in
    // pending until then. A tspan counts as a level of the depth of the
    // elements.
    private void addRuns(SVGText text, XMLNode node, ColorMap colorMap, SVGState.StyleSheet rules,
            SVGState state, Map<String, String> attributes, double[] pending, int depth) {
        if (depth > MAX_DEPTH) {
            throw depthError();
        }
        double x = textLength(attribute(attributes, "x"), state.fontSize);
        double y = textLength(attribute(attributes, "y"), state.fontSize);
        double dx = textLength(attribute(attributes, "dx"), state.fontSize);
        double dy = textLength(attribute(attributes, "dy"), state.fontSize);
        if (!Double.isNaN(x)) {
            pending[0] = x;
            pending[2] = 0.0;
        }
        if (!Double.isNaN(y)) {
            pending[1] = y;
            pending[3] = 0.0;
        }
        if (!Double.isNaN(dx)) {
            pending[2] += dx;
        }
        if (!Double.isNaN(dy)) {
            pending[3] += dy;
        }
        for (Object item : node.getContent()) {
            if (item instanceof String) {
                SVGText.Run run = new SVGText.Run();
                run.text = (String) item;
                run.x = pending[0];
                run.y = pending[1];
                run.dx = pending[2];
                run.dy = pending[3];
                pending[0] = Double.NaN;
                pending[1] = Double.NaN;
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
                run.fillAlpha = state.drawnFillAlpha();
                run.strokeAlpha = state.drawnStrokeAlpha();
                if (state.invisible) {
                    // It keeps its place in the text
                    run.fill = Color.transparent;
                    run.stroke = Color.transparent;
                }
                run.strokeWidth = state.strokeWidth;
                run.strokeFirst = state.strokeFirst;
                run.lineJoin = state.lineJoin;
                text.runs.add(run);
            } else {
                XMLNode child = (XMLNode) item;
                String name = child.getLocalName();
                if (!name.equals("tspan") && !name.equals("a")) {
                    continue;   // A title or a desc is not drawn
                }
                List<String> childOrder = new ArrayList<String>();
                Map<String, String> childAttributes = attributes(child, childOrder);
                SVGState childState = elementState(state, colorMap, rules, childAttributes, childOrder);
                childState.matrix = state.matrix;   // A tspan has no transform
                if (!childState.hidden) {
                    addRuns(text, child, colorMap, rules, childState, childAttributes, pending, depth + 1);
                }
            }
        }
    }

    // Reads the first length of an x, y, dx or dy of a text, which may be in
    // em of its font size, and returns NaN for none.
    private static double textLength(String value, float fontSize) {
        List<String> fields = SVGState.fields(value.replace(',', ' '));
        if (fields.isEmpty()) {
            return Double.NaN;
        }
        String first = fields.get(0);
        try {
            double length;
            if (first.endsWith("em")) {
                length = SVGState.parseNumber(first.substring(0, first.length() - 2), false)*fontSize;
            } else if (first.endsWith("%")) {
                return Double.NaN;
            } else {
                Float points = SVGState.parseLength(first);
                if (points == null) {
                    return Double.NaN;
                }
                length = points;
            }
            return (Double.isInfinite(length)) ? Double.NaN : length;
        } catch (NumberFormatException e) {
            return Double.NaN;
        }
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
            byte[] bytes = Util.utf8(align);
            if (bytes.length != 8) {
                bytes = "xMidYMid".getBytes(StandardCharsets.US_ASCII);
            }
            tx = alignOffset(new String(bytes, 1, 3, StandardCharsets.ISO_8859_1), w - box[2]*scale);
            ty = alignOffset(new String(bytes, 5, 3, StandardCharsets.ISO_8859_1), h - box[3]*scale);
        }
        float strokeScale = (float) Math.sqrt(((double) sx)*((double) sy));
        List<SVGPath> all = new ArrayList<SVGPath>(paths);
        all.addAll(clipShapes);
        for (SVGPath path : all) {
            path.strokeWidth *= strokeScale;
            if (path.dashArray != null) {
                for (int i = 0; i < path.dashArray.length; i++) {
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
        List<SVGPath> all = new ArrayList<SVGPath>(paths);
        all.addAll(clipShapes);
        for (SVGPath path : all) {
            path.strokeWidth *= factor;
            if (path.dashArray != null) {
                for (int i = 0; i < path.dashArray.length; i++) {
                    path.dashArray[i] *= factor;
                }
                path.dashOffset *= factor;
            }
            if (path.text != null) {
                double[] m = path.text.matrix;
                path.text.matrix = new double[] {
                        m[0]*factor, m[1]*factor, m[2]*factor, m[3]*factor, m[4]*factor, m[5]*factor};
            }
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
     * Sets the font of the text of the image, in place of the fonts PDFjet
     * carries: for the text of a language they do not have, such as Chinese,
     * or for a font of one's own.
     *
     * @param font the font.
     * @return this SVGImage object.
     */
    public SVGImage setFont(Font font) {
        this.font = font;
        return this;
    }

    /**
     * Sets the font of the bold text of the image, whose font-weight is bold
     * or 600 or more, when a font is set with setFont; unless set, that font
     * draws the bold text too.
     *
     * @param font the bold font.
     * @return this SVGImage object.
     */
    public SVGImage setBoldFont(Font font) {
        this.boldFont = font;
        return this;
    }

    // Returns the font of a run: the one set, the bold one for a weight of 600
    // or more when it is set, or else the face of the fonts PDFjet carries of
    // its family, weight and style, from resources/fonts or the jar; when it
    // is in neither, it throws: the text is never drawn in a core font.
    private Font fontOf(SVGText.Run run, PDF pdf) {
        if (font != null) {
            return (run.weight >= 600 && boldFont != null) ? boldFont : font;
        }
        Font bundled = BundledFonts.get(pdf, run.family, run.weight, run.italic);
        if (bundled == null) {
            pdf.fail(new IllegalStateException("The font "
                    + BundledFonts.fileName(run.family, run.weight, run.italic)
                    + " is not one of the fonts PDFjet carries, in resources/fonts or in PDFjet.jar: "
                    + "set a font with setFont."));
        }
        return bundled;
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

    // Draws a path, filled and then stroked, or stroked first when its
    // paint-order says. An opacity, a line cap and a line join are set in a
    // graphics state of the path's own.
    private void drawPath(SVGPath path, Page page) {
        boolean fill = path.fill != Color.transparent;
        boolean stroke = path.stroke != Color.transparent;
        if (path.operations.isEmpty() || !fill && !stroke || !isWritable(path, page)) {
            return;
        }
        // PDF/A-1 has no transparency, so there the path is drawn opaque, as
        // an annotation is (review of 12 October 2026, R9).
        boolean alpha = !page.pdf.isPDFA1() && (fill && path.fillAlpha < 1f || stroke && path.strokeAlpha < 1f);
        boolean lineCap = stroke && path.lineCap != CapStyle.BUTT;
        boolean lineJoin = stroke && path.lineJoin != JoinStyle.MITER;
        boolean dash = stroke && path.dashArray != null;
        boolean state = alpha || lineCap || lineJoin || dash || path.clips != null;
        if (state) {
            page.saveGraphicsState();
            clip(path, page);
            if (dash) {
                page.append('[');
                for (int i = 0; i < path.dashArray.length; i++) {
                    if (i > 0) {
                        page.append(' ');
                    }
                    page.append(path.dashArray[i]);
                }
                page.append("] ");
                page.append(path.dashOffset);
                page.append(" d\n");
            }
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

        for (int pass = 0; pass < 2; pass++) {
            boolean strokePass = (pass == 0) == path.strokeFirst;
            if (!strokePass && fill) {
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

            if (strokePass && stroke) {
                page.setPenColor(path.stroke);
                page.setPenWidth(path.strokeWidth);
                // Z closes a subpath, and the path is stroked once, all of its
                // subpaths together, as SVG strokes it.
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
                    } else if (op.cmd == 'Z') {
                        page.append("h\n");
                    }
                }
                page.strokePath();
            }
        }

        if (state) {
            page.restoreGraphicsState();
        }
    }

    // Clips what is drawn next to the clip paths of the path, each in turn.
    private void clip(SVGPath path, Page page) {
        if (path.clips == null) {
            return;
        }
        for (SVGPath clip : path.clips) {
            for (PathOp op : clip.operations) {
                if (op.cmd == 'M') {
                    page.moveTo(op.x + x, op.y + y);
                } else if (op.cmd == 'L') {
                    page.lineTo(op.x + x, op.y + y);
                } else if (op.cmd == 'C') {
                    page.curveTo(op.x1 + x, op.y1 + y, op.x2 + x, op.y2 + y, op.x + x, op.y + y);
                } else if (op.cmd == 'Z') {
                    page.append("h\n");
                }
            }
            if (clip.evenOdd) {
                page.append("W*\nn\n");
            } else {
                page.clipPath();
            }
        }
    }

    // Draws a text: each run filled and then stroked, or stroked first when
    // its paint-order says, with the text matrix that takes its glyphs, whose
    // y goes up, to where the run is on the page.
    private void drawText(SVGText text, List<SVGPath> clips, Page page) {
        double[] m = text.matrix;
        double scale = Math.sqrt(Math.abs(m[0]*m[3] - m[1]*m[2]));
        for (SVGText.Run run : text.runs) {
            String str = SVGText.text(run);
            if (str.isEmpty() || !(run.fontSize > 0f)) {
                continue;
            }
            float[] tm = {
                (float) m[0],
                (float) -m[1],
                (float) -m[2],
                (float) m[3],
                (float) (m[0]*run.px + m[2]*run.py + m[4]) + x,
                page.height - ((float) (m[1]*run.px + m[3]*run.py + m[5]) + y)};
            boolean writable = true;
            for (float value : tm) {
                writable = writable && FastFloat.isWritable(value);
            }
            float strokeWidth = (float) (run.strokeWidth*scale);
            boolean fill = run.fill != Color.transparent && run.fillAlpha > 0f;
            boolean stroke = run.stroke != Color.transparent && run.strokeAlpha > 0f
                    && strokeWidth > 0f && FastFloat.isWritable(strokeWidth);
            if (!writable || !fill && !stroke) {
                continue;
            }
            boolean alpha = !page.pdf.isPDFA1() && (fill && run.fillAlpha < 1f || stroke && run.strokeAlpha < 1f);
            boolean lineJoin = stroke && run.lineJoin != JoinStyle.MITER;
            boolean state = alpha || lineJoin || clips != null;
            if (state) {
                page.saveGraphicsState();
                SVGPath clipped = new SVGPath();
                clipped.clips = clips;
                clip(clipped, page);
                if (alpha) {
                    GraphicsState gs = new GraphicsState();
                    gs.setAlphaNonStroking(run.fillAlpha);
                    gs.setAlphaStroking(run.strokeAlpha);
                    page.setGraphicsState(gs);
                }
                if (lineJoin) {
                    page.setLineJoinStyle(run.lineJoin);
                }
            }
            for (int pass = 0; pass < 2; pass++) {
                boolean strokePass = (pass == 0) == run.strokeFirst;
                if (strokePass && stroke) {
                    page.setPenColor(run.stroke);
                    page.setPenWidth(strokeWidth);
                    page.drawSVGText(run.font, run.fontSize, str, tm, 1);
                } else if (!strokePass && fill) {
                    page.setBrushColor(run.fill);
                    page.drawSVGText(run.font, run.fontSize, str, tm, 0);
                }
            }
            if (state) {
                page.restoreGraphicsState();
            }
        }
    }

    // Returns true when something of the image is drawn outside its size,
    // which is cut off then: a point of a path, wider by half its stroke, or
    // the box of a run of a text, from its ascent to its descent.
    private boolean overflows(Page page) {
        if (w <= 0f || h <= 0f) {
            return false;
        }
        for (SVGPath path : paths) {
            if (path.text != null) {
                double[] m = path.text.matrix;
                for (SVGText.Run run : path.text.runs) {
                    float[][] corners = {
                        {run.px, run.py - run.fontSize},
                        {run.px + run.width, run.py - run.fontSize},
                        {run.px, run.py + 0.3f*run.fontSize},
                        {run.px + run.width, run.py + 0.3f*run.fontSize}};
                    for (float[] corner : corners) {
                        float[] point = transform(m, corner[0], corner[1]);
                        if (outside(point[0], point[1], 0f)) {
                            return true;
                        }
                    }
                }
                continue;
            }
            if (!isWritable(path, page)) {
                continue;   // Not drawn
            }
            float margin = (path.stroke != Color.transparent) ? path.strokeWidth/2f : 0f;
            for (PathOp op : path.operations) {
                if (outside(op.x, op.y, margin) || op.cmd == 'C'
                        && (outside(op.x1, op.y1, margin) || outside(op.x2, op.y2, margin))) {
                    return true;
                }
            }
        }
        return false;
    }

    // Returns true for a point outside the size of the image, by more than a
    // hundredth of a point and the margin.
    private boolean outside(float px, float py, float margin) {
        float e = 0.01f + margin;
        return px < -e || py < -e || px > w + e || py > h + e;
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
        // A linked image is a Figure in the Link its annotation joins.
        StructElement link = null;
        if (uri != null || key != null) {
            link = page.beginLink();
        }
        page.addBDC(StructElem.FIGURE, language, actualText, altDescription);
        for (SVGPath path : paths) {
            if (path.text != null) {
                for (SVGText.Run run : path.text.runs) {
                    run.font = fontOf(run, page.pdf);
                }
                path.text.place();
            }
        }
        boolean clip = overflows(page);
        if (clip) {
            page.saveGraphicsState();
            page.clipRect(x, y, w, h);
        }
        for (SVGPath path : paths) {
            if (path.text != null) {
                drawText(path.text, path.clips, page);
            } else {
                drawPath(path, page);
            }
        }
        if (clip) {
            page.restoreGraphicsState();
        }
        page.setFigureBoundingBox(x, y, w, h);
        page.addEMC();
        page.endLink(link);
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
            page.addAnnotation(linkAnnotation);
        }
        return new float[] {x + w, y + h};
    }
}   // End of SVGImage.java