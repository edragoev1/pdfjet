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
        guard let fileStream = InputStream(fileAtPath: fileAtPath) else {
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
     * the fill, stroke, stroke-width, fill-rule, stroke-linecap,
     * stroke-linejoin, opacity, fill-opacity and stroke-opacity they have or
     * inherit, as attributes, in a style attribute or in rules for their
     * classes in a <style> element that comes before them. The width, height,
     * viewBox and preserveAspectRatio of the <svg> element give the size of
     * the image.
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

        var rules = [SVGRule]()
        var stack = [SVGState()]
        var names = [String]()
        var styleSheet = String.UnicodeScalarView()
        var root = true
        let xml = Array(String(decoding: bytes, as: UTF8.self).unicodeScalars)
        for token in SVGImage.getTokens(xml) {
            switch token {
            case .start(let name, let attributes, let empty):
                var values = [String: String]()
                var order = [String]()
                for (attributeName, value) in attributes {
                    values[attributeName] = value
                    order.append(attributeName)
                }
                var state = try SVGImage.elementState(stack[stack.count - 1], rules, values, order)
                if SVGImage.templates.contains(name) {
                    state.hidden = true
                }
                stack.append(state)
                names.append(name)

                if name == "svg" && root {
                    root = false
                    w = SVGImage.parseLength(values["width"] ?? "")
                    h = SVGImage.parseLength(values["height"] ?? "")
                    viewBox = values["viewBox"] ?? ""
                    preserveAspectRatio = values["preserveAspectRatio"] ?? ""
                }
                let operations = try SVGImage.shapeOperations(name, values)
                if name == "line" {
                    state.fill = Color.transparent  // A line has no inside to fill
                    state.fillCurrent = false
                }
                addPath(state, operations)
                if !empty {
                    break
                }
                fallthrough
            case .end:
                if names.last == "style" {
                    rules.append(contentsOf: SVGRule.parseStyleSheet(String(styleSheet)))
                    styleSheet.removeAll()
                }
                if stack.count > 1 {
                    stack.removeLast()
                    names.removeLast()
                }
            case .text(let text):
                if names.last == "style" {
                    styleSheet.append(contentsOf: text.unicodeScalars)
                }
            }
        }
        try processPaths(paths)
    }

    // Returns the state of an element whose parent has the given one: its
    // presentation attributes, in their order, then the rules of the style
    // sheet for its classes, in the order of the style sheet, then its style
    // attribute, and its transform.
    private static func elementState(_ parent: SVGState, _ rules: [SVGRule],
            _ attributes: [String: String], _ order: [String]) throws -> SVGState {
        var state = parent
        state.ownOpacity = 1.0
        for name in order {
            try state.setProperty(name, attributes[name]!)
        }
        let classes = SVGState.fields(attributes["class"] ?? "")
        if !classes.isEmpty {
            for rule in rules where classes.contains(where: { SVGState.equal($0, rule.name) }) {
                for (name, value) in rule.declarations {
                    try state.setProperty(name, value)
                }
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
            return SVGState.parseCoordinate(attributes[name] ?? "")
        }
        var b = Builder()
        switch name {
        case "path":
            return try SVG.toPDF(SVG.getOperations(attributes["d"] ?? ""))
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
        if state.hidden || operations.isEmpty || det == 0.0 {
            return
        }
        let path = SVGPath()
        path.operations = operations
        path.fill = state.fillCurrent ? state.color : state.fill
        path.stroke = state.strokeCurrent ? state.color : state.stroke
        path.fillAlpha = state.fillOpacity * state.opacity
        path.strokeAlpha = state.strokeOpacity * state.opacity
        if path.fillAlpha == 0.0 {
            path.fill = Color.transparent
        }
        if path.strokeAlpha == 0.0 || state.strokeWidth == 0.0 {
            path.stroke = Color.transparent
        }
        if path.fill == Color.transparent && path.stroke == Color.transparent {
            return
        }
        path.strokeWidth = Float(Double(state.strokeWidth) * abs(det).squareRoot())
        path.evenOdd = state.evenOdd
        path.lineCap = state.lineCap
        path.lineJoin = state.lineJoin
        if m != SVGState.identity {
            for op in operations {
                (op.x, op.y) = SVGImage.transform(m, op.x, op.y)
                if op.cmd == "C" {
                    (op.x1, op.y1) = SVGImage.transform(m, op.x1, op.y1)
                    (op.x2, op.y2) = SVGImage.transform(m, op.x2, op.y2)
                }
            }
        }
        paths.append(path)
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

    // The parts of an XML document that SVGImage reads, in document order.
    private enum Token {
        // The local name of an element, its attributes without a namespace,
        // in their order, and whether it is empty, written <name/>.
        case start(String, [(String, String)], Bool)
        case end
        case text(String)   // Character data, and the content of a CDATA section
    }

    // Returns the start tags, the end tags and the text of the document, as
    // an XML parser reports them: the attribute values may be in single or
    // double quotes and span lines, and comments, processing instructions and
    // the document type declaration are skipped.
    private static func getTokens(_ xml: [Unicode.Scalar]) -> [Token] {
        var tokens = [Token]()
        var i = 0
        while i < xml.count {
            if xml[i] != "<" {
                let start = i
                while i < xml.count && xml[i] != "<" {
                    i += 1
                }
                tokens.append(.text(getText(xml[start..<i], false)))
            } else if startsWith(xml, i, "<!--") {
                i = indexAfter(xml, i + 4, "-->")
            } else if startsWith(xml, i, "<![CDATA[") {
                let start = i + 9
                i = indexAfter(xml, start, "]]>")
                let end = (i >= start + 3 && startsWith(xml, i - 3, "]]>")) ? i - 3 : i
                tokens.append(.text(getLines(xml[start..<end])))
            } else if startsWith(xml, i, "<?") {
                i = indexAfter(xml, i + 2, "?>")
            } else if startsWith(xml, i, "<!") {
                // The document type declaration may have an internal subset in brackets.
                var depth = 0
                i += 2
                while i < xml.count && (xml[i] != ">" || depth > 0) {
                    if xml[i] == "[" {
                        depth += 1
                    } else if xml[i] == "]" {
                        depth -= 1
                    }
                    i += 1
                }
                i += 1
            } else if startsWith(xml, i, "</") {
                i = indexAfter(xml, i + 2, ">")
                tokens.append(.end)
            } else {
                i += 1
                let name = localName(getName(xml, &i))
                var attributes = [(String, String)]()
                while true {
                    skipSpaces(xml, &i)
                    if i >= xml.count || xml[i] == ">" || xml[i] == "/" {
                        break
                    }
                    let attributeName = getName(xml, &i)
                    skipSpaces(xml, &i)
                    if i >= xml.count || xml[i] != "=" {
                        break
                    }
                    i += 1
                    skipSpaces(xml, &i)
                    if i >= xml.count || (xml[i] != "\"" && xml[i] != "'") {
                        break
                    }
                    let quote = xml[i]
                    i += 1
                    let start = i
                    while i < xml.count && xml[i] != quote {
                        i += 1
                    }
                    // An attribute with a prefix, xmlns:x or inkscape:label,
                    // is of another namespace.
                    if !attributeName.unicodeScalars.contains(":") {
                        attributes.append((attributeName, getText(xml[start..<i], true)))
                    }
                    i += 1
                }
                let empty = i < xml.count && xml[i] == "/"
                tokens.append(.start(name, attributes, empty))
                i = indexAfter(xml, i, ">")
            }
        }
        return tokens
    }

    private static func startsWith(
            _ xml: [Unicode.Scalar], _ i: Int, _ prefix: String) -> Bool {
        var j = i
        for scalar in prefix.unicodeScalars {
            if j >= xml.count || xml[j] != scalar {
                return false
            }
            j += 1
        }
        return true
    }

    // Returns the index after the first occurrence of the text at or after i.
    private static func indexAfter(
            _ xml: [Unicode.Scalar], _ i: Int, _ text: String) -> Int {
        var j = i
        while j < xml.count {
            if startsWith(xml, j, text) {
                return j + text.unicodeScalars.count
            }
            j += 1
        }
        return xml.count
    }

    private static func skipSpaces(_ xml: [Unicode.Scalar], _ i: inout Int) {
        while i < xml.count &&
                (xml[i] == " " || xml[i] == "\t" || xml[i] == "\n" || xml[i] == "\r") {
            i += 1
        }
    }

    // Reads a name, with its prefix, if any.
    private static func getName(_ xml: [Unicode.Scalar], _ i: inout Int) -> String {
        var name = String.UnicodeScalarView()
        while i < xml.count {
            let ch = xml[i]
            if ch == " " || ch == "\t" || ch == "\n" || ch == "\r" ||
                    ch == "=" || ch == ">" || ch == "/" {
                break
            }
            name.append(ch)
            i += 1
        }
        return String(name)
    }

    // Returns the local name of an element: the part after the colon of a
    // name with a prefix, and a name that has no prefix or no local part as
    // it is.
    private static func localName(_ name: String) -> String {
        let parts = name.unicodeScalars.split(separator: ":", omittingEmptySubsequences: false)
        if parts.count == 2 && (parts[0].isEmpty || parts[1].isEmpty) {
            return name
        }
        return SVGState.string(parts[parts.count - 1])
    }

    // Normalizes the text or the attribute value as an XML parser does: a
    // line end is a newline in text, and every line end, tab and newline is a
    // space in an attribute value; the references are replaced.
    private static func getText(_ value: ArraySlice<Unicode.Scalar>, _ attribute: Bool) -> String {
        var buf = String.UnicodeScalarView()
        var i = value.startIndex
        while i < value.endIndex {
            let ch = value[i]
            if ch == "\r" {
                buf.append(attribute ? " " : "\n")
                if i + 1 < value.endIndex && value[i + 1] == "\n" {
                    i += 1
                }
            } else if attribute && (ch == "\n" || ch == "\t") {
                buf.append(" ")
            } else if ch == "&",
                    let end = value[i...].firstIndex(of: ";"),
                    let scalar = getReference(String(String.UnicodeScalarView(value[(i + 1)..<end]))) {
                buf.append(scalar)
                i = end
            } else {
                buf.append(ch)
            }
            i += 1
        }
        return String(buf)
    }

    // Returns the content of a CDATA section, where a line end is a newline.
    private static func getLines(_ value: ArraySlice<Unicode.Scalar>) -> String {
        var buf = String.UnicodeScalarView()
        var i = value.startIndex
        while i < value.endIndex {
            if value[i] == "\r" {
                buf.append("\n")
                if i + 1 < value.endIndex && value[i + 1] == "\n" {
                    i += 1
                }
            } else {
                buf.append(value[i])
            }
            i += 1
        }
        return String(buf)
    }

    private static func getReference(_ name: String) -> Unicode.Scalar? {
        switch name {
        case "amp": return "&"
        case "lt": return "<"
        case "gt": return ">"
        case "quot": return "\""
        case "apos": return "'"
        default:
            var code: UInt32?
            if name.hasPrefix("#x") {
                code = UInt32(name.dropFirst(2), radix: 16)
            } else if name.hasPrefix("#") {
                code = UInt32(name.dropFirst())
            }
            return code.flatMap { Unicode.Scalar($0) }
        }
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
        for path in paths {
            path.strokeWidth *= strokeScale
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
        for path in paths {
            path.strokeWidth *= factor
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
        let alpha = fill && path.fillAlpha < 1.0 || stroke && path.strokeAlpha < 1.0
        let lineCap = stroke && path.lineCap != CapStyle.BUTT
        let lineJoin = stroke && path.lineJoin != JoinStyle.MITER
        let state = alpha || lineCap || lineJoin
        if state {
            page.saveGraphicsState()
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
            // Z closes and strokes a subpath; a path that is still open at the
            // end is stroked without closing it.
            var open = false
            for op in path.operations {
                if op.cmd == "M" {
                    page.moveTo(op.x + x, op.y + y)
                    open = true
                } else if op.cmd == "L" {
                    page.lineTo(op.x + x, op.y + y)
                    open = true
                } else if op.cmd == "C" {
                    page.curveTo(
                        op.x1 + x, op.y1 + y,
                        op.x2 + x, op.y2 + y,
                        op.x + x, op.y + y)
                    open = true
                } else if op.cmd == "Z" {
                    page.closePath()
                    open = false
                }
            }
            if open {
                page.strokePath()
            }
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

    /// Draws this SVG image on the specified page.
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        guard let page = page else {
            return [self.x + self.w, self.y + self.h]   // Measured, not drawn
        }
        page.addBDC(StructElem.FIGURE, language, actualText, altDescription)
        for path in paths {
            drawPath(path, page)
        }
        page.addEMC()
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
                    altDescription))
        }
        return [self.x + self.w, self.y + self.h]
    }
}   // End of SVGImage.swift