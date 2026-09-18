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
    var viewBox: String?
    var fill: Int32 = Color.transparent
    var stroke: Int32 = Color.transparent
    var fillNone = false    // fill="none" on the svg element
    var strokeNone = false  // stroke="none" on the svg element
    var strokeWidth: Float = 0.0
    var paths: [SVGPath]?
    var uri: String?
    var key: String?
    var language: String?
    var actualText: String?
    var altDescription: String?

    // Built once per process; avoids reflecting over ColorMap on every
    // color attribute parsed.
    private static let colorCache: [String: Int32] = {
        var cache: [String: Int32] = [:]
        let mirror = Mirror(reflecting: ColorMap())
        for child in mirror.children {
            if let label = child.label, let value = child.value as? Int32 {
                cache[label] = value
            }
        }
        return cache
    }()

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
     * - Parameter stream: the input stream.
     */
    public init(stream: InputStream) throws {
        paths = [SVGPath]()
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

        let xml = Array(String(decoding: bytes, as: UTF8.self).unicodeScalars)
        for (name, attributes) in SVGImage.getStartTags(xml) {
            if name == "svg" {
                try readSVGAttributes(attributes)
            } else if name == "path" {
                try readPathAttributes(attributes)
            }
        }
        processPaths(paths ?? [])
    }

    private func readSVGAttributes(_ attributes: [(String, String)]) throws {
        for (name, value) in attributes {
            if name == "width" {
                self.w = Float(value.trim()) ?? 0.0
            } else if name == "height" {
                self.h = Float(value.trim()) ?? 0.0
            } else if name == "viewBox" {
                self.viewBox = value
            } else if name == "fill" {
                self.fill = try getColor(value)
                self.fillNone = SVGImage.isNone(value)
            } else if name == "stroke" {
                self.stroke = try getColor(value)
                self.strokeNone = SVGImage.isNone(value)
            } else if name == "stroke-width" {
                self.strokeWidth = Float(value.trim()) ?? 0.0
            }
        }
    }

    private func readPathAttributes(_ attributes: [(String, String)]) throws {
        let path = SVGPath()
        for (name, value) in attributes {
            if name == "d" {
                path.data = value
            } else if name == "fill" {
                path.fill = try getColor(value)
                path.fillNone = SVGImage.isNone(value)
            } else if name == "stroke" {
                path.stroke = try getColor(value)
                path.strokeNone = SVGImage.isNone(value)
            } else if name == "stroke-width" {
                path.strokeWidth = Float(value.trim()) ?? 0.0
            }
        }
        paths?.append(path)
    }

    // Returns the local names of the elements and the local names and values
    // of their attributes, in document order, as an XML parser reports them:
    // the attribute values may be in single or double quotes and span lines,
    // and comments, CDATA sections, processing instructions, the document
    // type declaration and end tags are skipped.
    private static func getStartTags(
            _ xml: [Unicode.Scalar]) -> [(String, [(String, String)])] {
        var tags = [(String, [(String, String)])]()
        var i = 0
        while i < xml.count {
            if xml[i] != "<" {
                i += 1
            } else if startsWith(xml, i, "<!--") {
                i = indexAfter(xml, i + 4, "-->")
            } else if startsWith(xml, i, "<![CDATA[") {
                i = indexAfter(xml, i + 9, "]]>")
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
            } else {
                i += 1
                let name = getName(xml, &i)
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
                    attributes.append((attributeName, getAttributeValue(xml[start..<i])))
                    i += 1
                }
                tags.append((name, attributes))
                i = indexAfter(xml, i, ">")
            }
        }
        return tags
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

    // Reads a name and returns its local part, without the namespace prefix.
    private static func getName(_ xml: [Unicode.Scalar], _ i: inout Int) -> String {
        var name = String.UnicodeScalarView()
        while i < xml.count {
            let ch = xml[i]
            if ch == " " || ch == "\t" || ch == "\n" || ch == "\r" ||
                    ch == "=" || ch == ">" || ch == "/" {
                break
            }
            if ch == ":" {
                name.removeAll()
            } else {
                name.append(ch)
            }
            i += 1
        }
        return String(name)
    }

    // Normalizes the attribute value as an XML parser does: every line end,
    // tab and newline becomes a space, and the references are replaced.
    private static func getAttributeValue(_ value: ArraySlice<Unicode.Scalar>) -> String {
        var buf = String.UnicodeScalarView()
        var i = value.startIndex
        while i < value.endIndex {
            let ch = value[i]
            if ch == "\r" {
                buf.append(" ")
                if i + 1 < value.endIndex && value[i + 1] == "\n" {
                    i += 1
                }
            } else if ch == "\n" || ch == "\t" {
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

    func processPaths(_ paths: [SVGPath]) {
        var box: [Float] = Array(repeating: 0.0, count: 4)
        if let viewBox = viewBox {
            let list = viewBox.trim()
                .components(separatedBy: .whitespaces)
                .filter { !$0.isEmpty }
            guard list.count == 4 else {
                return
            }
            guard let bx0 = Float(list[0]),
                  let bx1 = Float(list[1]),
                  let bx2 = Float(list[2]),
                  let bx3 = Float(list[3]) else {
                return
            }
            guard bx2 != 0.0, bx3 != 0.0 else {
                return  // degenerate viewBox: division would produce NaN
            }
            box[0] = bx0
            box[1] = bx1
            box[2] = bx2
            box[3] = bx3
        }
        for path in paths {
            guard let data = path.data else { continue }
            path.operations = SVG.getOperations(data)
            path.operations = SVG.toPDF(path.operations ?? [])
            if viewBox != nil {
                for op in path.operations ?? [] {
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

    func getColor(_ colorName: String) throws -> Int32 {
        if colorName.hasPrefix("#") {
            if colorName.count == 7 {
                let index = colorName.index(colorName.startIndex, offsetBy: 1)
                guard let value = Int32(colorName[index...], radix: 16) else {
                    throw PDFjetError(message: "Invalid color: " + colorName)
                }
                return value
            } else if colorName.count == 4 {
                let index1 = colorName.index(colorName.startIndex, offsetBy: 1)
                let index2 = colorName.index(colorName.startIndex, offsetBy: 2)
                let index3 = colorName.index(colorName.startIndex, offsetBy: 3)
                let str1 = colorName[index1..<index2]
                let str2 = colorName[index2..<index3]
                let str3 = colorName[index3...]
                let str = String(str1 + str1 + str2 + str2 + str3 + str3)
                guard let value = Int32(str, radix: 16) else {
                    throw PDFjetError(message: "Invalid color: " + colorName)
                }
                return value
            } else {
                return Color.transparent
            }
        }
        return SVGImage.colorCache[colorName] ?? Color.transparent
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
        w *= factor
        h *= factor
        guard let paths = paths else {
            return self
        }
        for path in paths {
            guard let operations = path.operations else {
                continue
            }
            for op in operations {
                op.x1 *= factor
                op.y1 *= factor
                op.x2 *= factor
                op.y2 *= factor
                op.x *= factor
                op.y *= factor
            }
        }
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

    // Returns true for the value none, which turns the fill or the stroke off.
    private static func isNone(_ value: String) -> Bool {
        return value.trim() == "none"
    }

    private func drawPath(_ path: SVGPath, _ page: Page) {
        // none on the path wins over the color of the svg element; a color
        // that is not set, or not understood, is taken from the svg element.
        let noFill = path.fillNone || (path.fill == Color.transparent && self.fillNone)
        var fillColor = noFill ? Color.transparent : path.fill
        if !noFill && fillColor == Color.transparent {
            fillColor = self.fill
        }
        let noStroke = path.strokeNone || (path.stroke == Color.transparent && self.strokeNone)
        var strokeColor = noStroke ? Color.transparent : path.stroke
        if !noStroke && strokeColor == Color.transparent {
            strokeColor = self.stroke
        }
        var strokeWidth = self.strokeWidth
        if path.strokeWidth > strokeWidth {
            strokeWidth = path.strokeWidth
        }

        // A path whose fill is not none, with no fill and no stroke color, is
        // filled black, as SVG fills a path black by default.
        if !noFill && fillColor == Color.transparent &&
                strokeColor == Color.transparent {
            fillColor = Color.black
        }
        if fillColor == Color.transparent && strokeColor == Color.transparent {
            return  // fill="none" and no stroke: nothing to draw
        }

        page.setBrushColor(fillColor)
        page.setPenColor(strokeColor)
        page.setPenWidth(strokeWidth)

        guard let operations = path.operations else {
            return
        }

        if fillColor != Color.transparent {
            for op in operations {
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
                }
            }
            page.fillPath()
        }

        if strokeColor != Color.transparent {
            // Z closes and strokes a subpath; a path that is still open at the
            // end is stroked without closing it.
            var open = false
            for op in operations {
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
    }

    /// Draws this SVG image on the specified page.
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        guard let page = page else {
            return [self.x + self.w, self.y + self.h]   // Measured, not drawn
        }
        page.addBDC(StructElem.P, language, actualText, altDescription)
        for path in paths ?? [] {
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