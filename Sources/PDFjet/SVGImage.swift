/**
 * SVGImage.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/**
 * Used to embed SVG images in the PDF document.
 */
public class SVGImage : Drawable {
    var x: Float = 0.0  // location x
    var y: Float = 0.0  // location y
    var w: Float = 0.0  // SVG width
    var h: Float = 0.0  // SVG height
    var viewBox = ""
    var preserveAspectRatio = ""
    var paths = [SVGPath]()
    var uri: String?
    var key: String?
    var language: String?
    var actualText: String?
    var altDescription: String?
    private var font: Font?
    private var boldFont: Font?

    // While the file is read: the elements by the bytes of their ids, the
    // clip paths of the image, and the elements drawn again by a use element
    // or as a marker, which are not drawn inside themselves; the rules of its
    // style sheet, the states of the elements open, and whether the svg
    // element of the document is still to come.
    private var ids = [[UInt8]: PDFjetXMLNode]()
    private var clipShapes = [SVGPath]()
    private var reusing = Set<ObjectIdentifier>()
    private var reused = 0
    private var counted = 0
    private var useSize: (String, String)?  // The width and the height of the use element of the element drawn next
    private var rules = SVGStyleSheet()
    private var stack = [SVGState]()
    private var root = true
    // The elements drawn again, by use elements and as markers, at most: a
    // use of a group of uses, and so on, draws ten to the power of the
    // number of groups, as an XML of entities in entities does.
    static let MAX_REUSED = 1000000
    // The path operations drawn again, by use elements and as markers, and
    // written for the clip paths of what is drawn, at most: a use of a path of N
    // operations was N operations for each element counted by the limit above,
    // 506 MB for a file of 200 KB, and a clip path of N operations is read for
    // every element it clips and written for every path in it, 800 MB of page
    // for 320 KB (review of 30 October 2026). The operations of a file drawn
    // once are not counted: they grow with the file.
    static let MAX_OPERATIONS = 4000000
    // How deep the elements being drawn may nest, by groups, use elements and
    // markers, a state on the stack for each: a chain of use elements is flat in
    // the file, so the limit of the XML parser does not see it, and each level
    // is a call. The number is the same in the four ports and leaves every one
    // of them room on a stack of 512 KB, where a level takes 6.4 KB of a debug
    // build of Swift (review of 30 October 2026).
    static let MAX_DEPTH = 64

    // The elements whose content is not drawn where it is, but used by other
    // elements, which PDFjet does not draw.
    private static let templates: Set<String> = [
        "defs", "clipPath", "mask", "pattern", "symbol",
        "marker", "linearGradient", "radialGradient",
    ]

    /**
     * Used to embed SVG images in the PDF document.
     *
     * - Parameter fileAtPath: the path to the SVG file.
     */
    public convenience init?(fileAtPath: String) throws {
        guard FileManager.default.fileExists(atPath: fileAtPath),
                let fileStream = InputStream(fileAtPath: fileAtPath) else {
            return nil  // cannot open file
        }
        // This constructor owns the stream it created, so it closes it
        // once reading is done. (Deferred until after self.init returns,
        // since reading happens inside the designated initializer.)
        defer { fileStream.close() }
        try self.init(stream: fileStream)
    }

    /**
     * Used to embed SVG images in the PDF document.
     *
     * It draws the <path>, <rect>, <circle>, <ellipse>, <line>, <polyline>
     * and <polygon> elements, in groups or not, with their transforms, and
     * the fill, stroke, stroke-width, stroke-dasharray, stroke-dashoffset,
     * fill-rule, stroke-linecap, stroke-linejoin, opacity, fill-opacity,
     * stroke-opacity, paint-order, clip-path and markers they have or
     * inherit, as attributes, in a style attribute or in rules for their
     * classes in a <style> element that comes before them. It draws the
     * elements a <use> element names, and <text> and <tspan> elements with
     * their x, y, dx, dy, font-size, font-weight, text-anchor and
     * dominant-baseline, in the font set with setFont and setBoldFont, or
     * else in the fonts PDFjet carries, embedded: IBM
     * Plex Sans for sans-serif, Helvetica, Arial and the like, IBM Plex Serif
     * for serif, Times and the like, Cascadia Mono for monospace, Courier and
     * the like, regular, semibold or bold, upright or italic, as the
     * font-family, font-weight and font-style of the text ask. A gradient is
     * drawn in the mean color of its stops. The width, height, viewBox and preserveAspectRatio of the <svg> element
     * give the size of the image, and what is drawn outside it is cut off, as
     * a browser does.
     *
     * - Parameter stream: the input stream.
     */
    public init(stream: InputStream) throws {
        // The caller retains ownership of the stream — we read from it
        // but do not close it, consistent with the Java and .NET editions.
        stream.open()
        var bytes = [UInt8]()
        var buffer = [UInt8](repeating: 0, count: 4096)
        while stream.hasBytesAvailable {
            let read = stream.read(&buffer, maxLength: buffer.count)
            if read <= 0 {
                break
            }
            bytes.append(contentsOf: buffer[0..<read])
        }
        // A stream that cannot be read, as one of a missing file, was an
        // empty SVG (review of 17 October 2026)
        if stream.streamStatus == .error {
            throw stream.streamError ?? PDFjetError(message: "Cannot read the stream.")
        }

        let document: PDFjetXMLNode
        do {
            document = try PDFjetXMLParser.parseSkippingDoctype(bytes)
        } catch let error as PDFjetXMLError {
            throw PDFjetError(message: "parsing SVG: " + error.message)
        }
        var elements = [document]
        var i = 0
        while i < elements.count {
            let element = elements[i]
            if let id = element.getAttribute("id"), ids[Array(id.utf8)] == nil {
                ids[Array(id.utf8)] = element   // The first element of an id is the one it names
            }
            elements.append(contentsOf: element.getChildren())
            i += 1
        }
        var svg = SVGState()
        svg.paints = try gradientColors()
        stack = [svg]
        try draw(document, false)
        try processPaths(paths)
        ids = [:]
        reusing = []
        rules = SVGStyleSheet()
        stack = []
    }

    // Returns the colors the gradients are drawn in, by the bytes of their
    // ids: the mean of the colors of their stops, or of the stops of the
    // gradient they name with href when they have none. A gradient without
    // stops is none, and is left out.
    private func gradientColors() throws -> [[UInt8]: Int32] {
        var colors = [[UInt8]: Int32]()
        var read = [ObjectIdentifier: Stops]()
        for (id, element) in ids {
            var gradient: PDFjetXMLNode? = element
            var i = 0
            // Up to ten gradients along the hrefs, which may go round in a circle.
            while i < 10, let node = gradient, SVGImage.isGradient(node) {
                var stops = read[ObjectIdentifier(node)]
                if stops == nil {
                    stops = try gradientStops(node)
                    read[ObjectIdentifier(node)] = stops
                }
                if let color = stops!.color {
                    colors[id] = color
                    break
                }
                gradient = stops!.next
                i += 1
            }
        }
        return colors
    }

    // What a gradient itself says of its color: the mean of its stops, if it
    // has any, and the gradient its href names.
    private struct Stops {
        var color: Int32?
        var next: PDFjetXMLNode?
    }

    // Reads the gradient, once however many gradients name it: N gradients
    // that name one of N stops were N x N stops read, 50 s for 3.4 MB (review
    // of 30 October 2026).
    private func gradientStops(_ node: PDFjetXMLNode) throws -> Stops {
        var red: Int64 = 0
        var green: Int64 = 0
        var blue: Int64 = 0
        var count: Int64 = 0
        for stop in node.getChildren() where stop.getLocalName() == "stop" {
            var state = SVGState()
            state.fill = Color.black    // stop-color is black unless given
            if let color = stop.getAttribute("stop-color") {
                try state.setProperty("fill", color)
            }
            for (name, value) in SVGRule.parseDeclarations(stop.getAttribute("style") ?? "")
                    where name == "stop-color" {
                try state.setProperty("fill", value)
            }
            let c = (state.fill == Color.transparent) ? Color.black : state.fill
            red += Int64((c >> 16) & 0xFF)
            green += Int64((c >> 8) & 0xFF)
            blue += Int64(c & 0xFF)
            count += 1
        }
        var stops = Stops()
        if count > 0 {
            let half = count / 2
            stops.color = Int32(truncatingIfNeeded: (((red + half) / count) << 16)
                    | (((green + half) / count) << 8) | ((blue + half) / count))
        }
        stops.next = SVGImage.href(node).flatMap { ids[Array($0.utf8)] }
        return stops
    }

    private static func isGradient(_ node: PDFjetXMLNode) -> Bool {
        let name = node.getLocalName()
        return name == "linearGradient" || name == "radialGradient"
    }

    // Returns the id that the href of an element names, #id, with or without
    // the xlink prefix, or nil.
    private static func href(_ node: PDFjetXMLNode) -> String? {
        var href: String?
        for (name, value) in node.getAttributes() {
            if name == "href" || href == nil && localName(name) == "href" {
                href = value
            }
        }
        guard let reference = href else {
            return nil
        }
        let scalars = Array(SVGState.trimSpace(reference).unicodeScalars)
        return (scalars.count > 1 && scalars[0] == "#") ? SVGState.string(scalars[1...]) : nil
    }

    // The name without its prefix, the part after its colon.
    private static func localName(_ name: String) -> String {
        let scalars = name.unicodeScalars
        guard let colon = scalars.firstIndex(of: ":") else {
            return name
        }
        return String(scalars[scalars.index(after: colon)...])
    }

    // Counts an element drawn again, and throws when there are too many.
    private func countReused() throws {
        reused += 1
        if reused > SVGImage.MAX_REUSED {
            throw PDFjetError(message:
                "The SVG draws more than \(SVGImage.MAX_REUSED) elements again, with use elements or markers.")
        }
    }

    // The attributes of no namespace of the element, and their order:
    // xlink:href, xml:space and the namespaces' own declarations, xmlns:...,
    // are not read.
    private static func attributes(_ node: PDFjetXMLNode, _ order: inout [String]) -> [String: String] {
        var attributes = [String: String]()
        for (name, value) in node.getAttributes() where !name.unicodeScalars.contains(":") {
            attributes[name] = value
            order.append(name)
        }
        return attributes
    }

    // Counts operations drawn again or written again, and throws when there
    // are too many.
    private func countOperations(_ count: Int) throws {
        counted += count
        if counted > SVGImage.MAX_OPERATIONS {
            throw PDFjetError(message: "The SVG draws more than \(SVGImage.MAX_OPERATIONS)"
                    + " path operations again, with use elements, markers or clip paths.")
        }
    }

    // Returns the operations written for the clip paths of one path, and of
    // a run of a text.
    private static func clipOperations(_ clips: [SVGPath]?) -> Int {
        return (clips ?? []).reduce(0) { $0 + $1.operations.count }
    }

    // The error of elements nested too deep.
    private static func depthError() -> PDFjetError {
        return PDFjetError(message:
            "The SVG nests elements, use elements and markers more than \(MAX_DEPTH) deep.")
    }

    // Draws the element, then the ones in it, in the order of the document;
    // the rules of a <style> element apply from its end. An element that is
    // used, by a use element or as a marker, is drawn even where it is a
    // template, as a symbol or a marker is.
    private func draw(_ node: PDFjetXMLNode, _ used: Bool) throws {
        if stack.count > SVGImage.MAX_DEPTH {
            throw SVGImage.depthError()
        }
        let name = node.getLocalName()
        if !reusing.isEmpty {
            try countReused()
        }
        var order = [String]()
        let values = SVGImage.attributes(node, &order)
        var state = try elementState(stack[stack.count - 1], values, order)
        if SVGImage.templates.contains(name) && !used {
            state.hidden = true
        }
        if state.clipPath != nil && !state.hidden {
            try clip(&state)
        }
        let size = useSize
        useSize = nil
        if name == "svg" && root {
            root = false
            w = SVGImage.parseLength(values["width"] ?? "")
            h = SVGImage.parseLength(values["height"] ?? "")
            // viewbox in lower case when there is no viewBox, as an
            // HTML page's parser reads an inline svg, whose files keep it
            viewBox = values["viewBox"] ?? values["viewbox"] ?? ""
            preserveAspectRatio = values["preserveAspectRatio"] ?? ""
            if let box = SVGState.parseNumbers(viewBox), box.count == 4 {
                state.viewport = [box[2], box[3]]
            } else {
                state.viewport = [Double(w), Double(h)]
            }
        } else if name == "svg" || name == "symbol" && used {
            SVGImage.enterViewport(&state, values, size)
        }
        stack.append(state)
        defer {
            stack.removeLast()
        }

        if name == "text" {
            if !state.hidden {
                try addText(node, state, values)
            }
            return
        }
        if name == "use" {
            if let target = SVGImage.href(node).flatMap({ ids[Array($0.utf8)] }), !state.hidden,
                    !reusing.contains(ObjectIdentifier(target)) {
                // The element is drawn as a child of the use element, moved
                // by its x and y.
                var parent = state
                parent.matrix = SVGState.multiply(state.matrix, [
                        1.0, 0.0, 0.0, 1.0, SVGImage.number(values, "x"), SVGImage.number(values, "y")])
                stack.append(parent)
                reusing.insert(ObjectIdentifier(target))
                useSize = (values["width"] ?? "", values["height"] ?? "")
                try draw(target, true)
                reusing.remove(ObjectIdentifier(target))
                stack.removeLast()
            }
            return
        }
        let operations = try SVGImage.shapeOperations(name, values)
        var count = reusing.isEmpty ? 0 : operations.count
        if !state.hidden && !operations.isEmpty {
            count += SVGImage.clipOperations(state.clips) + (state.dashArray?.count ?? 0)
        }
        try countOperations(count)
        if name == "line" {
            state.fill = Color.transparent  // A line has no inside to fill
            state.fillCurrent = false
        }
        var vertices: [[Double]]?
        if !state.hidden && !state.invisible && !operations.isEmpty
                && (state.markerStart != nil || state.markerMid != nil || state.markerEnd != nil)
                && (name == "path" || name == "line" || name == "polyline" || name == "polygon") {
            vertices = SVGImage.vertices(operations)    // Before addPath moves the points
        }
        addPath(state, operations)
        if let vertices = vertices {
            try drawMarkers(state, vertices)
        }

        for child in node.getChildren() {
            if name == "switch" && !SVGImage.isSwitchable(child) {
                continue
            }
            try draw(child, false)
            if name == "switch" {
                break   // A switch draws the first it can
            }
        }
        if name == "style" {
            rules.add(SVGRule.parseStyleSheet(node.getText()))
        }
    }

    // The elements PDFjet draws, and a switch can choose.
    private static let drawn: Set<String> = [
        "a", "circle", "ellipse", "g", "line", "path", "polygon",
        "polyline", "rect", "svg", "switch", "text", "use",
    ]

    // Returns true when a switch can draw the element: one PDFjet draws,
    // which asks for no extension, as PDFjet has none. The system language
    // is not tested: a PDF is read by anyone.
    private static func isSwitchable(_ node: PDFjetXMLNode) -> Bool {
        return drawn.contains(node.getLocalName()) && node.getAttribute("requiredExtensions") == nil
    }

    // Returns the state of an element whose parent has the given one: its
    // presentation attributes, in their order, then the rules of the style
    // sheet for its classes, in the order of the style sheet, then its style
    // attribute, and its transform.
    private func elementState(_ parent: SVGState,
            _ attributes: [String: String], _ order: [String]) throws -> SVGState {
        var state = parent
        state.ownOpacity = 1.0
        state.clipPath = nil
        for name in order {
            try state.setProperty(name, attributes[name]!)
        }
        for rule in try rules.forClasses(SVGState.fields(attributes["class"] ?? "")) {
            for (name, value) in rule.declarations {
                try state.setProperty(name, value)
            }
        }
        for (name, value) in SVGRule.parseDeclarations(attributes["style"] ?? "") {
            try state.setProperty(name, value)
        }
        state.opacity *= state.ownOpacity
        if let transform = attributes["transform"], let matrix = SVGState.parseTransform(transform) {
            state.matrix = SVGState.multiply(parent.matrix, matrix)
        }
        return state
    }

    // How far the control points of the cubic curve that draws a quarter of a
    // circle are from its ends, for a radius of 1.
    private static let kappa = 0.5522847498307936

    // Builds the PDF path operations of a shape.
    private struct Builder {
        var operations = [PathOp]()
        var x0 = 0.0    // The start of the subpath
        var y0 = 0.0

        mutating func moveTo(_ x: Double, _ y: Double) {
            x0 = x
            y0 = y
            operations.append(PathOp("M", Float(x), Float(y)))
        }

        mutating func lineTo(_ x: Double, _ y: Double) {
            operations.append(PathOp("L", Float(x), Float(y)))
        }

        mutating func curveTo(_ x1: Double, _ y1: Double,
                _ x2: Double, _ y2: Double, _ x: Double, _ y: Double) {
            let op = PathOp("C")
            op.setCubicPoints(Float(x1), Float(y1), Float(x2), Float(y2), Float(x), Float(y))
            operations.append(op)
        }

        mutating func closePath() {
            operations.append(PathOp("Z", Float(x0), Float(y0)))
        }

        // Adds the ellipse as four cubic curves, from its right end.
        mutating func ellipse(_ cx: Double, _ cy: Double, _ rx: Double, _ ry: Double) {
            let kx = kappa * rx
            let ky = kappa * ry
            moveTo(cx + rx, cy)
            curveTo(cx + rx, cy + ky, cx + kx, cy + ry, cx, cy + ry)
            curveTo(cx - kx, cy + ry, cx - rx, cy + ky, cx - rx, cy)
            curveTo(cx - rx, cy - ky, cx - kx, cy - ry, cx, cy - ry)
            curveTo(cx + kx, cy - ry, cx + rx, cy - ky, cx + rx, cy)
            closePath()
        }
    }

    // Returns the PDF path operations of an element that draws a path or a
    // basic shape, in its user space, and none for another element or a shape
    // of no size. It throws for path data that is not numbers.
    private static func shapeOperations(
            _ name: String, _ attributes: [String: String]) throws -> [PathOp] {
        func number(_ name: String) -> Double {
            return SVGImage.number(attributes, name)
        }
        var b = Builder()
        switch name {
        case "path":
            let operations = try SVG.toPDF(SVG.getOperations(attributes["d"] ?? ""))
            // Path data starts with a moveto; data that does not is in error
            // from its start, and SVG 1.1 section 8.3.2 draws it up to there.
            if let first = operations.first, first.cmd != "M" {
                return []
            }
            return operations
        case "rect":
            let x = number("x")
            let y = number("y")
            let w = number("width")
            let h = number("height")
            if w <= 0.0 || h <= 0.0 {
                return []
            }
            // A radius that is not given is the other one, and neither is
            // more than half the side.
            var rx = radius(attributes["rx"] ?? "")
            var ry = radius(attributes["ry"] ?? "")
            if rx == nil {
                rx = ry
            }
            if ry == nil {
                ry = rx
            }
            let rw = SVGState.goMin(rx ?? 0.0, w/2.0)
            let rh = SVGState.goMin(ry ?? 0.0, h/2.0)
            if rw <= 0.0 || rh <= 0.0 {
                b.moveTo(x, y)
                b.lineTo(x + w, y)
                b.lineTo(x + w, y + h)
                b.lineTo(x, y + h)
                b.closePath()
                return b.operations
            }
            let kx = kappa * rw
            let ky = kappa * rh
            b.moveTo(x + rw, y)
            b.lineTo(x + w - rw, y)
            b.curveTo(x + w - rw + kx, y, x + w, y + rh - ky, x + w, y + rh)
            b.lineTo(x + w, y + h - rh)
            b.curveTo(x + w, y + h - rh + ky, x + w - rw + kx, y + h, x + w - rw, y + h)
            b.lineTo(x + rw, y + h)
            b.curveTo(x + rw - kx, y + h, x, y + h - rh + ky, x, y + h - rh)
            b.lineTo(x, y + rh)
            b.curveTo(x, y + rh - ky, x + rw - kx, y, x + rw, y)
            b.closePath()
        case "circle":
            let r = number("r")
            if r <= 0.0 {
                return []
            }
            b.ellipse(number("cx"), number("cy"), r, r)
        case "ellipse":
            let rx = number("rx")
            let ry = number("ry")
            if rx <= 0.0 || ry <= 0.0 {
                return []
            }
            b.ellipse(number("cx"), number("cy"), rx, ry)
        case "line":
            b.moveTo(number("x1"), number("y1"))
            b.lineTo(number("x2"), number("y2"))
        case "polyline", "polygon":
            guard let points = SVGState.parseNumbers(attributes["points"] ?? ""), points.count >= 4 else {
                return []   // Drawn as far as it can be read, which is not a line
            }
            b.moveTo(points[0], points[1])
            var i = 2
            while i + 1 < points.count {
                b.lineTo(points[i], points[i + 1])
                i += 2
            }
            if name == "polygon" {
                b.closePath()
            }
        default:
            break
        }
        return b.operations
    }

    // Returns a coordinate or a size of a shape.
    private static func number(_ attributes: [String: String], _ name: String) -> Double {
        return SVGState.parseCoordinate(attributes[name] ?? "")
    }

    // Reads the rx or the ry of a rectangle; nil when it is not given, is
    // auto or is not a length of zero or more.
    private static func radius(_ value: String) -> Double? {
        let value = SVGState.trimSpace(value)
        if value.isEmpty || value == "auto" {
            return nil
        }
        guard let length = SVGState.parseLength(value), length >= 0.0,
              !SVGState.hasSuffix(value, "%") else {
            return nil
        }
        return Double(length)
    }

    // Adds the operations of a path or a shape to the image, in the space of
    // the svg element, with what the state draws them with, unless they draw
    // nothing: a shape in <defs> or of no size, or with neither a fill nor a
    // stroke.
    private func addPath(_ state: SVGState, _ operations: [PathOp]) {
        let m = state.matrix
        let det = m[0]*m[3] - m[1]*m[2]
        if state.hidden || state.invisible || operations.isEmpty || det == 0.0 {
            return
        }
        let path = SVGPath()
        path.operations = operations
        path.fill = state.fillCurrent ? state.color : state.fill
        path.stroke = state.strokeCurrent ? state.color : state.stroke
        path.fillAlpha = state.drawnFillAlpha()
        path.strokeAlpha = state.drawnStrokeAlpha()
        if path.fillAlpha == 0.0 {
            path.fill = Color.transparent
        }
        if path.strokeAlpha == 0.0 || state.strokeWidth == 0.0 {
            path.stroke = Color.transparent
        }
        if path.fill == Color.transparent && path.stroke == Color.transparent {
            return
        }
        let scale = abs(det).squareRoot()
        path.strokeWidth = Float(Double(state.strokeWidth) * scale)
        path.evenOdd = state.evenOdd
        path.lineCap = state.lineCap
        path.lineJoin = state.lineJoin
        if let dashArray = state.dashArray {
            path.dashArray = dashArray.map { Float(Double($0) * scale) }
            path.dashOffset = Float(Double(state.dashOffset) * scale)
        }
        path.clips = state.clips
        SVGImage.transform(m, operations)
        paths.append(path)
    }

    // Moves the points of the operations by the matrix.
    private static func transform(_ m: [Double], _ operations: [PathOp]) {
        if m == SVGState.identity {
            return
        }
        for op in operations {
            (op.x, op.y) = SVGImage.transform(m, op.x, op.y)
            if op.cmd == "C" {
                (op.x1, op.y1) = SVGImage.transform(m, op.x1, op.y1)
                (op.x2, op.y2) = SVGImage.transform(m, op.x2, op.y2)
            }
        }
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
    private func clip(_ state: inout SVGState) throws {
        guard let id = state.clipPath, let clipPath = ids[Array(id.utf8)],
              clipPath.getLocalName() == "clipPath",
              SVGState.trimSpace(clipPath.getAttribute("clipPathUnits") ?? "") != "objectBoundingBox" else {
            return
        }
        var svg = SVGState()
        svg.matrix = state.matrix
        var order = [String]()
        var attributes = SVGImage.attributes(clipPath, &order)
        var base = try elementState(svg, attributes, order)
        base.hidden = false     // A clipPath element is not displayed anyway
        let clip = SVGPath()
        var shapes = [[PathOp]]()
        for child in clipPath.getChildren() {
            order = []
            attributes = SVGImage.attributes(child, &order)
            var shape = try elementState(base, attributes, order)
            var name = child.getLocalName()
            if name == "use" {
                guard let target = SVGImage.href(child).flatMap({ ids[Array($0.utf8)] }) else {
                    continue
                }
                shape.matrix = SVGState.multiply(shape.matrix, [
                        1.0, 0.0, 0.0, 1.0, SVGImage.number(attributes, "x"), SVGImage.number(attributes, "y")])
                order = []
                attributes = SVGImage.attributes(target, &order)
                shape = try elementState(shape, attributes, order)
                name = target.getLocalName()
            }
            if shape.hidden || shape.invisible {
                continue
            }
            let operations = try SVGImage.shapeOperations(name, attributes)
            if operations.isEmpty {
                continue
            }
            SVGImage.transform(shape.matrix, operations)
            shapes.append(operations)
            clip.evenOdd = shape.clipEvenOdd
        }
        for operations in shapes {
            if shapes.count > 1 {
                clip.evenOdd = false
                clip.operations += (SVGImage.area(operations) < 0.0) ? SVGImage.reversed(operations) : operations
            } else {
                clip.operations += operations
            }
        }
        try countOperations(clipPath.getChildren().count + clip.operations.count)
        if clip.operations.isEmpty {
            state.hidden = true
            return
        }
        clipShapes.append(clip)
        state.clips = (state.clips ?? []) + [clip]
    }

    // Sets the user space of a nested svg element, or of a symbol a use
    // element draws: moved by its x and y, and its viewBox fitted to its width
    // and height, which the use element may give, and which are those of the
    // viewport it is in unless given; a percentage is of that viewport. A
    // viewBox or a size of nothing draws nothing.
    private static func enterViewport(_ state: inout SVGState, _ attributes: [String: String],
            _ size: (String, String)?) {
        let width = (size != nil && !size!.0.isEmpty) ? size!.0 : attributes["width"] ?? ""
        let height = (size != nil && !size!.1.isEmpty) ? size!.1 : attributes["height"] ?? ""
        var w = viewportLength(width, state.viewport[0], state.viewport[0])
        var h = viewportLength(height, state.viewport[1], state.viewport[1])
        var m = [1.0, 0.0, 0.0, 1.0,
                viewportLength(attributes["x"] ?? "", state.viewport[0], 0.0),
                viewportLength(attributes["y"] ?? "", state.viewport[1], 0.0)]
        guard let box = SVGState.parseNumbers(attributes["viewBox"] ?? ""), box.count == 4 else {
            state.viewport = [w, h]
            state.matrix = SVGState.multiply(state.matrix, m)
            return
        }
        // A viewport of no known size, of an svg element with neither a size
        // nor a viewBox, leaves the viewBox as it is.
        if w == 0.0 && width.isEmpty {
            w = box[2]
        }
        if h == 0.0 && height.isEmpty {
            h = box[3]
        }
        if box[2] <= 0.0 || box[3] <= 0.0 || w <= 0.0 || h <= 0.0 {
            state.hidden = true
            return
        }
        var sx = w / box[2]
        var sy = h / box[3]
        var tx = 0.0
        var ty = 0.0
        var fields = SVGState.fields(attributes["preserveAspectRatio"] ?? "")
        if !fields.isEmpty && fields[0] == "defer" {
            fields.removeFirst()
        }
        var align = Array((fields.first ?? "xMidYMid").utf8)
        if !SVGState.equal(fields.first ?? "", "none") {
            sx = SVGState.goMin(sx, sy)
            if fields.count > 1 && fields[1] == "slice" {
                sx = SVGState.goMax(w / box[2], h / box[3])
            }
            sy = sx
            if align.count != 8 {
                align = Array("xMidYMid".utf8)
            }
            tx = Double(alignOffset(align[1..<4], 1.0)) * (w - box[2]*sx)
            ty = Double(alignOffset(align[5..<8], 1.0)) * (h - box[3]*sy)
        }
        m = SVGState.multiply(m, [sx, 0.0, 0.0, sy, tx, ty])
        m = SVGState.multiply(m, [1.0, 0.0, 0.0, 1.0, -box[0], -box[1]])
        state.viewport = [box[2], box[3]]
        state.matrix = SVGState.multiply(state.matrix, m)
    }

    // Reads an x, a y, a width or a height of a viewport, which may be a
    // percentage of the size of the one it is in, the given one if it has none.
    private static func viewportLength(_ value: String, _ whole: Double, _ given: Double) -> Double {
        let value = SVGState.trimSpace(value)
        if value.isEmpty {
            return given
        }
        if SVGState.hasSuffix(value, "%") {
            guard let number = SVGState.parseDouble(SVGState.string(value.unicodeScalars.dropLast())),
                  number.isFinite else {
                return given
            }
            return number*whole/100.0
        }
        return SVGState.parseCoordinate(value)
    }

    // Returns twice the area of the path, its sign the way it goes round, by
    // the points of its lines and curves, and closed.
    private static func area(_ operations: [PathOp]) -> Double {
        var area = 0.0
        var x0 = 0.0
        var y0 = 0.0
        var x = 0.0
        var y = 0.0
        func line(_ px: Double, _ py: Double) {
            area += x*py - px*y
            x = px
            y = py
        }
        for op in operations {
            if op.cmd == "M" {
                line(x0, y0)
                x0 = Double(op.x)
                y0 = Double(op.y)
                x = x0
                y = y0
                continue
            }
            if op.cmd == "C" {
                line(Double(op.x1), Double(op.y1))
                line(Double(op.x2), Double(op.y2))
            }
            line(Double(op.x), Double(op.y))
        }
        line(x0, y0)
        return area
    }

    // Returns the path going the other way round, each subpath from its end.
    // Only a clip path needs this, whose subpaths close of themselves.
    private static func reversed(_ operations: [PathOp]) -> [PathOp] {
        var reversed = [PathOp]()
        var start = 0
        for end in stride(from: 1, through: operations.count, by: 1) {
            if end < operations.count && operations[end].cmd != "M" {
                continue
            }
            let last = operations[end - 1]
            reversed.append(PathOp("M", last.x, last.y))
            for i in stride(from: end - 1, to: start, by: -1) {
                let op = operations[i]
                let before = operations[i - 1]
                if op.cmd == "C" {
                    let curve = PathOp("C")
                    curve.setCubicPoints(op.x2, op.y2, op.x1, op.y1, before.x, before.y)
                    reversed.append(curve)
                } else {
                    reversed.append(PathOp("L", before.x, before.y))
                }
            }
            start = end
        }
        return reversed
    }

    // The degrees of a radian, and the radians of a degree, as Java's
    // Math.toDegrees and Math.toRadians multiply by them.
    private static let degreesPerRadian = 57.29577951308232
    private static let radiansPerDegree = 0.017453292519943295

    // Returns the vertices of the path, where its markers are drawn, in its
    // user space: x, y and the angle of the path there, in degrees, which is
    // halfway between the directions in and out of a vertex in the middle.
    private static func vertices(_ operations: [PathOp]) -> [[Double]] {
        var vertices = [[Double]]()     // x, y, in x, in y, out x, out y
        var x = 0.0
        var y = 0.0
        var start = 0       // The vertex where the subpath starts
        for op in operations {
            let inward: [Double]
            let out: [Double]
            if op.cmd == "M" {
                start = vertices.count
                vertices.append([Double(op.x), Double(op.y), 0.0, 0.0, 0.0, 0.0])
                x = Double(op.x)
                y = Double(op.y)
                continue
            } else if op.cmd == "C" {
                out = direction(x, y, [Double(op.x1), Double(op.y1), Double(op.x2), Double(op.y2),
                        Double(op.x), Double(op.y)])
                let back = direction(Double(op.x), Double(op.y), [Double(op.x2), Double(op.y2),
                        Double(op.x1), Double(op.y1), x, y])
                inward = [-back[0], -back[1]]
            } else {
                // A line, or Z, the line back to the start of the subpath.
                out = [Double(op.x) - x, Double(op.y) - y]
                inward = out
            }
            if vertices.isEmpty {
                continue
            }
            let last = vertices.count - 1
            if vertices[last][4] == 0.0 && vertices[last][5] == 0.0 {
                vertices[last][4] = out[0]
                vertices[last][5] = out[1]
            }
            vertices.append([Double(op.x), Double(op.y), inward[0], inward[1], 0.0, 0.0])
            if op.cmd == "Z" {
                // The vertex that closes the subpath goes on as its start does.
                let closing = vertices.count - 1
                vertices[closing][4] = vertices[start][4]
                vertices[closing][5] = vertices[start][5]
            }
            x = Double(op.x)
            y = Double(op.y)
        }
        var result = [[Double]]()
        for v in vertices {
            let inLength = hypot(v[2], v[3])
            let outLength = hypot(v[4], v[5])
            var dx = 0.0
            var dy = 0.0
            if inLength > 0.0 {
                dx += v[2] / inLength
                dy += v[3] / inLength
            }
            if outLength > 0.0 {
                dx += v[4] / outLength
                dy += v[5] / outLength
            }
            if dx == 0.0 && dy == 0.0 && inLength > 0.0 {
                dx = v[2]       // A path that turns back: the direction in
                dy = v[3]
            }
            result.append([v[0], v[1], atan2(dy, dx) * degreesPerRadian])
        }
        return result
    }

    // Returns the direction from x, y to the first of the points that is not
    // there, as a curve leaves its start.
    private static func direction(_ x: Double, _ y: Double, _ points: [Double]) -> [Double] {
        var i = 0
        while i + 1 < points.count {
            if points[i] != x || points[i + 1] != y {
                return [points[i] - x, points[i + 1] - y]
            }
            i += 2
        }
        return [0.0, 0.0]
    }

    // Draws the markers of a path at its vertices: the start marker at the
    // first, the end marker at the last and the mid marker at the others.
    private func drawMarkers(_ state: SVGState, _ vertices: [[Double]]) throws {
        let last = vertices.count - 1
        for i in 0..<vertices.count {
            if i == 0 {
                try drawMarker(state.markerStart, state, vertices[i], true)
            }
            if i > 0 && i < last {
                try drawMarker(state.markerMid, state, vertices[i], false)
            }
            if i == last {
                try drawMarker(state.markerEnd, state, vertices[i], false)
            }
        }
    }

    // Draws the marker at the vertex: its refX, refY there, turned as its
    // orient says, and its viewBox fitted to its markerWidth and
    // markerHeight, which are in units of the stroke width unless its
    // markerUnits are userSpaceOnUse.
    private func drawMarker(_ id: String?, _ state: SVGState, _ vertex: [Double], _ start: Bool) throws {
        guard let id = id, let marker = ids[Array(id.utf8)], marker.getLocalName() == "marker",
              !reusing.contains(ObjectIdentifier(marker)) else {
            return
        }
        var orient = SVGState.trimSpace(marker.getAttribute("orient") ?? "")
        var angle = 0.0
        if orient == "auto" {
            angle = vertex[2]
        } else if orient == "auto-start-reverse" {
            angle = start ? vertex[2] + 180.0 : vertex[2]
        } else {
            if SVGState.hasSuffix(orient, "deg") {
                orient = SVGState.string(orient.unicodeScalars.dropLast(3))
            }
            angle = SVGState.parseDouble(SVGState.trimSpace(orient)) ?? 0.0
            if !angle.isFinite {
                angle = 0.0
            }
        }
        let units = SVGState.trimSpace(marker.getAttribute("markerUnits") ?? "") == "userSpaceOnUse"
                ? 1.0 : Double(state.strokeWidth)
        let width = SVGImage.markerSize(marker.getAttribute("markerWidth") ?? "")
        let height = SVGImage.markerSize(marker.getAttribute("markerHeight") ?? "")
        var sx = 1.0
        var sy = 1.0
        if let box = SVGState.parseNumbers(marker.getAttribute("viewBox") ?? ""), box.count == 4 {
            if box[2] <= 0.0 || box[3] <= 0.0 {
                return      // A viewBox of no size draws nothing
            }
            sx = width / box[2]
            sy = height / box[3]
            if SVGState.trimSpace(marker.getAttribute("preserveAspectRatio") ?? "") != "none" {
                sx = SVGState.goMin(sx, sy)
                sy = sx
            }
        }
        let radians = angle * SVGImage.radiansPerDegree
        let cosine = cos(radians)
        let sine = sin(radians)
        var m = SVGState.multiply(state.matrix, [cosine, sine, -sine, cosine, vertex[0], vertex[1]])
        m = SVGState.multiply(m, [units * sx, 0.0, 0.0, units * sy, 0.0, 0.0])
        m = SVGState.multiply(m, [
                1.0, 0.0, 0.0, 1.0, -SVGState.parseCoordinate(marker.getAttribute("refX") ?? ""),
                -SVGState.parseCoordinate(marker.getAttribute("refY") ?? "")])
        // The marker inherits from the svg element, not from the path.
        var base = SVGState()
        base.paints = state.paints
        base.clips = state.clips
        base.matrix = m
        stack.append(base)
        reusing.insert(ObjectIdentifier(marker))
        try draw(marker, true)
        reusing.remove(ObjectIdentifier(marker))
        stack.removeLast()
    }

    // Reads the markerWidth or the markerHeight: 3 unless given.
    private static func markerSize(_ value: String) -> Double {
        if SVGState.trimSpace(value).isEmpty {
            return 3.0
        }
        let size = SVGState.parseCoordinate(value)
        return (size > 0.0) ? size : 0.0
    }

    // Adds a text element, with its tspan elements, as the runs of its
    // characters.
    private func addText(_ node: PDFjetXMLNode, _ state: SVGState, _ attributes: [String: String]) throws {
        let text = SVGText()
        text.matrix = state.matrix
        var pending = [Double.nan, Double.nan, 0.0, 0.0]
        try addRuns(text, node, state, attributes, &pending, stack.count)
        text.collapseSpaces()
        let empty = text.runs.allSatisfy { $0.text.isEmpty }
        var count = text.runs.count * SVGImage.clipOperations(state.clips)
        if !reusing.isEmpty {
            count += text.runs.reduce(0) { $0 + 1 + $1.text.unicodeScalars.count }
        }
        try countOperations(count)
        let m = state.matrix
        if empty || m[0]*m[3] - m[1]*m[2] == 0.0 {
            return
        }
        let path = SVGPath()
        path.text = text
        path.clips = state.clips
        paths.append(path)
    }

    // Adds the runs of a text or a tspan element. Its x, y, dx and dy apply to
    // its first character, which may be in a tspan in it; they are kept in
    // pending until then. A tspan counts as a level of the depth of the
    // elements.
    private func addRuns(_ text: SVGText, _ node: PDFjetXMLNode, _ state: SVGState,
            _ attributes: [String: String], _ pending: inout [Double], _ depth: Int) throws {
        if depth > SVGImage.MAX_DEPTH {
            throw SVGImage.depthError()
        }
        let x = SVGImage.textLength(attributes["x"] ?? "", state.fontSize)
        let y = SVGImage.textLength(attributes["y"] ?? "", state.fontSize)
        let dx = SVGImage.textLength(attributes["dx"] ?? "", state.fontSize)
        let dy = SVGImage.textLength(attributes["dy"] ?? "", state.fontSize)
        if !x.isNaN {
            pending[0] = x
            pending[2] = 0.0
        }
        if !y.isNaN {
            pending[1] = y
            pending[3] = 0.0
        }
        if !dx.isNaN {
            pending[2] += dx
        }
        if !dy.isNaN {
            pending[3] += dy
        }
        for item in node.getContent() {
            switch item {
            case .text(let characters):
                let run = SVGText.Run()
                run.text = characters
                run.x = pending[0]
                run.y = pending[1]
                run.dx = pending[2]
                run.dy = pending[3]
                pending = [Double.nan, Double.nan, 0.0, 0.0]
                run.fontSize = state.fontSize
                run.weight = state.fontWeight
                run.italic = state.italic
                run.family = state.fontFamily
                run.anchor = state.textAnchor
                run.baseline = state.baseline
                run.fill = state.fillCurrent ? state.color : state.fill
                run.stroke = state.strokeCurrent ? state.color : state.stroke
                run.fillAlpha = state.drawnFillAlpha()
                run.strokeAlpha = state.drawnStrokeAlpha()
                if state.invisible {
                    // It keeps its place in the text
                    run.fill = Color.transparent
                    run.stroke = Color.transparent
                }
                run.strokeWidth = state.strokeWidth
                run.strokeFirst = state.strokeFirst
                run.lineJoin = state.lineJoin
                text.runs.append(run)
            case .element(let child):
                let name = child.getLocalName()
                if name != "tspan" && name != "a" {
                    continue    // A title or a desc is not drawn
                }
                var childOrder = [String]()
                let childAttributes = SVGImage.attributes(child, &childOrder)
                var childState = try elementState(state, childAttributes, childOrder)
                childState.matrix = state.matrix    // A tspan has no transform
                if !childState.hidden {
                    try addRuns(text, child, childState, childAttributes, &pending, depth + 1)
                }
            }
        }
    }

    // Reads the first length of an x, y, dx or dy of a text, which may be in
    // em of its font size, and returns NaN for none.
    private static func textLength(_ value: String, _ fontSize: Float) -> Double {
        let fields = SVGState.fields(SVGState.string(value.unicodeScalars.map { $0 == "," ? " " : $0 }))
        guard let first = fields.first else {
            return Double.nan
        }
        let length: Double
        if SVGState.hasSuffix(first, "em") {
            guard let number = SVGState.parseDouble(SVGState.string(first.unicodeScalars.dropLast(2))) else {
                return Double.nan
            }
            length = number * Double(fontSize)
        } else if SVGState.hasSuffix(first, "%") {
            return Double.nan
        } else {
            guard let points = SVGState.parseLength(first) else {
                return Double.nan
            }
            length = Double(points)
        }
        return length.isInfinite ? Double.nan : length
    }

    // Returns the point that the matrix takes the point to.
    private static func transform(_ m: [Double], _ x: Float, _ y: Float) -> (Float, Float) {
        return (Float(m[0]*Double(x) + m[2]*Double(y) + m[4]),
                Float(m[1]*Double(x) + m[3]*Double(y) + m[5]))
    }

    // The units of a length of an SVG file, in points. A number without a
    // unit is in the user unit of the file, which PDFjet draws as a point,
    // and so is a number in px.
    static let units: [(String, Float)] = [
        ("px", 1.0), ("pt", 1.0), ("pc", 12.0),
        ("in", 72.0), ("mm", 72.0/25.4), ("cm", 72.0/2.54),
    ]

    // Returns the width or the height of the svg element in points, and 0 for
    // a length that PDFjet cannot read, a percentage among them, which leaves
    // the size to the viewBox.
    private static func parseLength(_ value: String) -> Float {
        var text = SVGState.trimSpace(value)
        var scale: Float = 1.0
        for (suffix, points) in units where SVGState.hasSuffix(text, suffix) {
            text = SVGState.trimSpace(SVGState.string(text.unicodeScalars.dropLast(suffix.count)))
            scale = points
            break
        }
        guard let number = SVGState.parseFloat(text), number.isFinite else {
            return 0.0
        }
        return number*scale
    }

    // Parses a numeric attribute value, treating an empty value as 0 —
    // consistent with how an omitted attribute is handled. Returns nil for a
    // value that is not a number.
    static func parseFloatLenient(_ value: String) -> Float? {
        let text = SVGState.trimSpace(value)
        if text.isEmpty {
            return 0.0
        }
        return SVGState.parseFloat(text)
    }

    func processPaths(_ paths: [SVGPath]) throws {
        if viewBox.isEmpty {
            return
        }
        guard let list = SVGState.parseNumbers(viewBox), list.count == 4 else {
            throw PDFjetError(message: "Invalid SVG viewBox \"\(viewBox)\": four numbers are needed.")
        }
        let box = list.map { Float($0) }
        guard box[2] != 0.0, box[3] != 0.0 else {
            throw PDFjetError(
                message: "Invalid SVG viewBox \"\(viewBox)\": its width and height cannot be zero.")
        }
        // A size the file does not give, or one in a unit PDFjet cannot read,
        // is the other one in the proportions of the viewBox, or the size of
        // the viewBox when both are missing, where scaling it by a width of
        // zero would draw every path at the origin.
        if w == 0.0 && h == 0.0 {
            w = box[2]
            h = box[3]
        } else if w == 0.0 {
            w = h * box[2] / box[3]
        } else if h == 0.0 {
            h = w * box[3] / box[2]
        }

        // The viewBox is scaled to the size, and, unless preserveAspectRatio
        // is none, by the same factor across and down, as large as it fits
        // (meet) or as small as it covers (slice), and placed as it says, in
        // the middle by default.
        var sx = w / box[2]
        var sy = h / box[3]
        var tx: Float = 0.0
        var ty: Float = 0.0
        var fields = SVGState.fields(preserveAspectRatio)
        if !fields.isEmpty && fields[0] == "defer" {
            fields.removeFirst()
        }
        var align = Array((fields.first ?? "xMidYMid").utf8)
        let uniform = !SVGState.equal(fields.first ?? "", "none") && sx != sy
        if uniform {
            var scale = Float(SVGState.goMin(Double(sx), Double(sy)))
            if fields.count > 1 && fields[1] == "slice" {
                scale = Float(SVGState.goMax(Double(sx), Double(sy)))
            }
            sx = scale
            sy = scale
            if align.count != 8 {
                align = Array("xMidYMid".utf8)
            }
            tx = SVGImage.alignOffset(align[1..<4], w - box[2]*scale)
            ty = SVGImage.alignOffset(align[5..<8], h - box[3]*scale)
        }
        let strokeScale = Float((Double(sx) * Double(sy)).squareRoot())
        for path in paths + clipShapes {
            path.strokeWidth *= strokeScale
            if let dashArray = path.dashArray {
                path.dashArray = dashArray.map { $0 * strokeScale }
                path.dashOffset *= strokeScale
            }
            if let text = path.text {
                let m = text.matrix
                let ax = Double(uniform ? sx : w / box[2])
                let ay = Double(uniform ? sy : h / box[3])
                let ox = uniform ? Double(tx) : 0.0
                let oy = uniform ? Double(ty) : 0.0
                text.matrix = [
                        m[0]*ax, m[1]*ay, m[2]*ax, m[3]*ay,
                        (m[4] - Double(box[0]))*ax + ox, (m[5] - Double(box[1]))*ay + oy]
            }
            for op in path.operations {
                if uniform {
                    op.x = (op.x - box[0])*sx + tx
                    op.y = (op.y - box[1])*sy + ty
                    op.x1 = (op.x1 - box[0])*sx + tx
                    op.y1 = (op.y1 - box[1])*sy + ty
                    op.x2 = (op.x2 - box[0])*sx + tx
                    op.y2 = (op.y2 - box[1])*sy + ty
                } else {
                    op.x = (op.x - box[0]) * w / box[2]
                    op.y = (op.y - box[1]) * h / box[3]
                    op.x1 = (op.x1 - box[0]) * w / box[2]
                    op.y1 = (op.y1 - box[1]) * h / box[3]
                    op.x2 = (op.x2 - box[0]) * w / box[2]
                    op.y2 = (op.y2 - box[1]) * h / box[3]
                }
            }
        }
    }

    // Returns how far the viewBox is moved across or down in the space left
    // over by it: none for Min, half for Mid and all for Max.
    private static func alignOffset(_ align: ArraySlice<UInt8>, _ space: Float) -> Float {
        if align.elementsEqual("Min".utf8) {
            return 0.0
        }
        if align.elementsEqual("Max".utf8) {
            return space
        }
        return space / 2.0
    }

    /**
     *  Sets the location of this SVG on the page.
     *
     *  - Parameter x: the x coordinate of the top left corner of this box when drawn on the page.
     *  - Parameter y: the y coordinate of the top left corner of this box when drawn on the page.
     *  - Returns: this SVG object, to allow method chaining.
     */
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x = x
        self.y = y
        return self
    }

    /// Scales this SVG image by the specified factor.
    @discardableResult
    public func scaleBy(_ factor: Float) -> SVGImage {
        for path in paths + clipShapes {
            path.strokeWidth *= factor
            if let dashArray = path.dashArray {
                path.dashArray = dashArray.map { $0 * factor }
                path.dashOffset *= factor
            }
            if let text = path.text {
                let m = text.matrix
                let f = Double(factor)
                text.matrix = [m[0]*f, m[1]*f, m[2]*f, m[3]*f, m[4]*f, m[5]*f]
            }
            for op in path.operations {
                op.x1 *= factor
                op.y1 *= factor
                op.x2 *= factor
                op.y2 *= factor
                op.x *= factor
                op.y *= factor
            }
        }
        w *= factor
        h *= factor
        return self
    }

    /// Sets the URI for the "click box" action.
    @discardableResult
    public func setURIAction(_ uri: String) -> SVGImage {
        self.uri = uri
        return self
    }

    /// Sets the destination key for the action.
    @discardableResult
    public func setGoToAction(_ key: String) -> SVGImage {
        self.key = key
        return self
    }

    /// Sets the alternate description of this image.
    @discardableResult
    public func setAltDescription(_ altDescription: String) -> SVGImage {
        self.altDescription = altDescription
        return self
    }

    /// Sets the actual text of this image.
    @discardableResult
    public func setActualText(_ actualText: String) -> SVGImage {
        self.actualText = actualText
        return self
    }

    /// Sets the language of this image, for example "en-US".
    @discardableResult
    public func setLanguage(_ language: String) -> SVGImage {
        self.language = language
        return self
    }

    ///
    /// Sets the font of the text of the image, in place of the fonts PDFjet
    /// carries: for the text of a language they do not have, such as Chinese,
    /// or for a font of one's own.
    ///
    /// - Parameter font: the font.
    /// - Returns: this SVGImage object.
    ///
    @discardableResult
    public func setFont(_ font: Font) -> SVGImage {
        self.font = font
        return self
    }

    ///
    /// Sets the font of the bold text of the image, whose font-weight is bold
    /// or 600 or more, when a font is set with setFont; unless set, that font
    /// draws the bold text too.
    ///
    /// - Parameter font: the bold font.
    /// - Returns: this SVGImage object.
    ///
    @discardableResult
    public func setBoldFont(_ font: Font) -> SVGImage {
        self.boldFont = font
        return self
    }

    // Returns the font of a run: the one set, the bold one for a weight of 600
    // or more when it is set, or else the face of the fonts PDFjet carries of
    // its family, weight and style. A face that is neither in resources/fonts
    // nor in the bundle of PDFjetFonts, which PDFjet depends on, is refused:
    // nil, and the document records why, as no core font is drawn in its
    // place.
    private func fontOf(_ run: SVGText.Run, _ pdf: PDF) -> Font? {
        if let font = font {
            return (run.weight >= 600 && boldFont != nil) ? boldFont! : font
        }
        if let bundled = BundledFonts.get(pdf, run.family, run.weight, run.italic) {
            return bundled
        }
        pdf.fail("The font " + BundledFonts.fileName(run.family, run.weight, run.italic)
                + " is neither in resources/fonts nor in the bundle of PDFjetFonts: "
                + "set a font with setFont.")
        return nil
    }

    /// Returns the width of this SVG image.
    public func getWidth() -> Float {
        return self.w
    }

    /// Returns the height of this SVG image.
    public func getHeight() -> Float {
        return self.h
    }

    // Draws a path, filled and then stroked. An opacity, a line cap and a
    // line join are set in a graphics state of the path's own.
    private func drawPath(_ path: SVGPath, _ page: Page) {
        let fill = path.fill != Color.transparent
        let stroke = path.stroke != Color.transparent
        if path.operations.isEmpty || !fill && !stroke || !isWritable(path, page) {
            return
        }
        // PDF/A-1 has no transparency, so there the path is drawn opaque, as
        // an annotation is (review of 12 October 2026, R9).
        let alpha = !page.pdf.isPDFA1() && (fill && path.fillAlpha < 1.0 || stroke && path.strokeAlpha < 1.0)
        let lineCap = stroke && path.lineCap != CapStyle.BUTT
        let lineJoin = stroke && path.lineJoin != JoinStyle.MITER
        let dash = stroke && path.dashArray != nil
        let state = alpha || lineCap || lineJoin || dash || path.clips != nil
        if state {
            page.saveGraphicsState()
            clip(path.clips, page)
            if dash {
                page.append("[")
                for (i, length) in path.dashArray!.enumerated() {
                    if i > 0 {
                        page.append(Token.space)
                    }
                    page.append(length)
                }
                page.append("] ")
                page.append(path.dashOffset)
                page.append(" d\n")
            }
            if alpha {
                let gs = GraphicsState()
                gs.setAlphaNonStroking(path.fillAlpha)
                gs.setAlphaStroking(path.strokeAlpha)
                page.setGraphicsState(gs)
            }
            if lineCap {
                page.setLineCapStyle(path.lineCap)
            }
            if lineJoin {
                page.setLineJoinStyle(path.lineJoin)
            }
        }

        if fill {
            page.setBrushColor(path.fill)
            for op in path.operations {
                if op.cmd == "M" {
                    page.moveTo(op.x + x, op.y + y)
                } else if op.cmd == "L" {
                    page.lineTo(op.x + x, op.y + y)
                } else if op.cmd == "C" {
                    page.curveTo(
                        op.x1 + x, op.y1 + y,
                        op.x2 + x, op.y2 + y,
                        op.x + x, op.y + y)
                }
            }
            if path.evenOdd {
                page.append("f*\n")
            } else {
                page.fillPath()
            }
        }

        if stroke {
            page.setPenColor(path.stroke)
            page.setPenWidth(path.strokeWidth)
            // Z closes a subpath, and the path is stroked once, all of its
            // subpaths together, as SVG strokes it.
            for op in path.operations {
                if op.cmd == "M" {
                    page.moveTo(op.x + x, op.y + y)
                } else if op.cmd == "L" {
                    page.lineTo(op.x + x, op.y + y)
                } else if op.cmd == "C" {
                    page.curveTo(
                        op.x1 + x, op.y1 + y,
                        op.x2 + x, op.y2 + y,
                        op.x + x, op.y + y)
                } else if op.cmd == "Z" {
                    page.append("h\n")
                }
            }
            page.strokePath()
        }

        if state {
            page.restoreGraphicsState()
        }
    }

    // Returns true when a PDF can hold the points and the stroke width of the
    // path where it is drawn: a transform or a scale such as scale(1e30) can
    // take them out of its range, and such a path is not drawn.
    private func isWritable(_ path: SVGPath, _ page: Page) -> Bool {
        func point(_ px: Float, _ py: Float) -> Bool {
            let px = px + x
            let py = py + y
            return FastFloat.isWritable(px) && FastFloat.isWritable(py) &&
                    FastFloat.isWritable(page.height - py)
        }
        if !FastFloat.isWritable(path.strokeWidth) {
            return false
        }
        for op in path.operations {
            if !point(op.x, op.y) || op.cmd == "C" && (!point(op.x1, op.y1) || !point(op.x2, op.y2)) {
                return false
            }
        }
        return true
    }

    // Clips what is drawn next to the clip paths, each in turn.
    private func clip(_ clips: [SVGPath]?, _ page: Page) {
        guard let clips = clips else {
            return
        }
        for clip in clips {
            for op in clip.operations {
                if op.cmd == "M" {
                    page.moveTo(op.x + x, op.y + y)
                } else if op.cmd == "L" {
                    page.lineTo(op.x + x, op.y + y)
                } else if op.cmd == "C" {
                    page.curveTo(op.x1 + x, op.y1 + y, op.x2 + x, op.y2 + y, op.x + x, op.y + y)
                } else if op.cmd == "Z" {
                    page.append("h\n")
                }
            }
            if clip.evenOdd {
                page.append("W*\nn\n")
            } else {
                page.clipPath()
            }
        }
    }

    // Draws a text: each run filled and then stroked, or stroked first when
    // its paint-order says, with the text matrix that takes its glyphs, whose
    // y goes up, to where the run is on the page.
    private func drawText(_ text: SVGText, _ clips: [SVGPath]?, _ page: Page) {
        let m = text.matrix
        let scale = abs(m[0]*m[3] - m[1]*m[2]).squareRoot()
        for run in text.runs {
            let str = SVGText.text(run)
            if str.isEmpty || !(run.fontSize > 0.0) {
                continue
            }
            let px = Double(run.px)
            let py = Double(run.py)
            let tm: [Float] = [
                Float(m[0]),
                Float(-m[1]),
                Float(-m[2]),
                Float(m[3]),
                Float(m[0]*px + m[2]*py + m[4]) + x,
                page.height - (Float(m[1]*px + m[3]*py + m[5]) + y)]
            let writable = tm.allSatisfy { FastFloat.isWritable($0) }
            let strokeWidth = Float(Double(run.strokeWidth) * scale)
            let fill = run.fill != Color.transparent && run.fillAlpha > 0.0
            let stroke = run.stroke != Color.transparent && run.strokeAlpha > 0.0
                    && strokeWidth > 0.0 && FastFloat.isWritable(strokeWidth)
            if !writable || !fill && !stroke {
                continue
            }
            let alpha = !page.pdf.isPDFA1() && (fill && run.fillAlpha < 1.0 || stroke && run.strokeAlpha < 1.0)
            let lineJoin = stroke && run.lineJoin != JoinStyle.MITER
            let state = alpha || lineJoin || clips != nil
            if state {
                page.saveGraphicsState()
                clip(clips, page)
                if alpha {
                    let gs = GraphicsState()
                    gs.setAlphaNonStroking(run.fillAlpha)
                    gs.setAlphaStroking(run.strokeAlpha)
                    page.setGraphicsState(gs)
                }
                if lineJoin {
                    page.setLineJoinStyle(run.lineJoin)
                }
            }
            for pass in 0..<2 {
                let strokePass = (pass == 0) == run.strokeFirst
                if strokePass && stroke {
                    page.setPenColor(run.stroke)
                    page.setPenWidth(strokeWidth)
                    page.drawSVGText(run.font!, run.fontSize, str, tm, 1)
                } else if !strokePass && fill {
                    page.setBrushColor(run.fill)
                    page.drawSVGText(run.font!, run.fontSize, str, tm, 0)
                }
            }
            if state {
                page.restoreGraphicsState()
            }
        }
    }

    // Returns true when something of the image is drawn outside its size,
    // which is cut off then: a point of a path, wider by half its stroke, or
    // the box of a run of a text, from its ascent to its descent.
    private func overflows(_ page: Page) -> Bool {
        if w <= 0.0 || h <= 0.0 {
            return false
        }
        for path in paths {
            if let text = path.text {
                let m = text.matrix
                for run in text.runs {
                    let corners: [(Float, Float)] = [
                        (run.px, run.py - run.fontSize),
                        (run.px + run.width, run.py - run.fontSize),
                        (run.px, run.py + 0.3 * run.fontSize),
                        (run.px + run.width, run.py + 0.3 * run.fontSize)]
                    for (cx, cy) in corners {
                        let (px, py) = SVGImage.transform(m, cx, cy)
                        if outside(px, py, 0.0) {
                            return true
                        }
                    }
                }
                continue
            }
            if !isWritable(path, page) {
                continue    // Not drawn
            }
            let margin: Float = (path.stroke != Color.transparent) ? path.strokeWidth / 2.0 : 0.0
            for op in path.operations {
                if outside(op.x, op.y, margin) || op.cmd == "C"
                        && (outside(op.x1, op.y1, margin) || outside(op.x2, op.y2, margin)) {
                    return true
                }
            }
        }
        return false
    }

    // Returns true for a point outside the size of the image, by more than a
    // hundredth of a point and the margin.
    private func outside(_ px: Float, _ py: Float, _ margin: Float) -> Bool {
        let e: Float = 0.01 + margin
        return px < -e || py < -e || px > w + e || py > h + e
    }

    /// Draws this SVG image on the specified page.
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        guard let page = page else {
            return [self.x + self.w, self.y + self.h]   // Measured, not drawn
        }
        // A linked image is a Figure in the Link its annotation joins
        var link: StructElement?
        if uri != nil || key != nil {
            link = page.beginLink()
        }
        page.addBDC(StructElem.FIGURE, language, actualText, altDescription)
        // A font that is missing is refused, and nothing is drawn, as the
        // other ports throw; the figure is closed, empty
        for path in paths {
            if let text = path.text {
                for run in text.runs {
                    guard let font = fontOf(run, page.pdf) else {
                        page.addEMC()
                        page.endLink(link)
                        return [self.x + self.w, self.y + self.h]
                    }
                    run.font = font
                }
                text.place()
            }
        }
        let clip = overflows(page)
        if clip {
            page.saveGraphicsState()
            page.clipRect(x, y, w, h)
        }
        for path in paths {
            if let text = path.text {
                drawText(text, path.clips, page)
            } else {
                drawPath(path, page)
            }
        }
        if clip {
            page.restoreGraphicsState()
        }
        page.setFigureBoundingBox(x, y, w, h)
        page.addEMC()
        page.endLink(link)
        if (uri != nil || key != nil) {
            page.addAnnotation(Annotation(
                    Annotation.Link,
                    x,
                    y,
                    x + w,
                    y + h,
                    nil,    // Vertices
                    nil,    // Fill Color
                    0.0,    // Opacity
                    nil,    // Title
                    nil,    // Contents
                    uri,
                    key,    // The destination name
                    language,
                    actualText,
                    altDescription).joining(link))
        }
        return [self.x + self.w, self.y + self.h]
    }
}   // End of SVGImage.swift