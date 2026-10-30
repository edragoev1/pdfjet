/**
 * SVGState.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// What an element of an SVG file draws with: the properties it inherits from
/// its parent, changed by its own attributes, the rules of the style sheet for
/// its classes and its style attribute, in that order, and the transform from
/// its user space to that of the svg element. It starts as the svg element
/// before its attributes: an SVG fills black, strokes nothing, and a stroke is
/// one unit wide.
struct SVGState {
    var fill: Int32 = Color.black           // Color.transparent is none
    var stroke: Int32 = Color.transparent
    var fillCurrent = false                 // currentColor: the color property, as it is where it is used
    var strokeCurrent = false
    var color: Int32 = Color.black          // The color property
    var fillAlpha: Float = 1.0              // The alpha of the fill color, as #f008 or rgba() has one
    var strokeAlpha: Float = 1.0
    var colorAlpha: Float = 1.0
    var strokeWidth: Float = 1.0            // In the user space of the element
    var evenOdd = false                     // fill-rule="evenodd"
    var clipEvenOdd = false                 // clip-rule="evenodd"
    var lineCap = CapStyle.BUTT
    var lineJoin = JoinStyle.MITER
    var miterLimit: Float = 4.0
    var fillOpacity: Float = 1.0
    var strokeOpacity: Float = 1.0
    var opacity: Float = 1.0                // The opacities of the element and its ancestors, multiplied
    var ownOpacity: Float = 1.0             // The opacity of the element itself, not inherited
    var matrix = SVGState.identity
    var viewport = [0.0, 0.0]               // The width and the height of the viewport, in user units
    var hidden = false                      // In <defs> and the like, or under display="none": not drawn
    var invisible = false                   // visibility: not painted, but its children may be
    var dashArray: [Float]? = nil           // stroke-dasharray, in the user space; nil is a solid line
    var dashOffset: Float = 0.0
    var strokeFirst = false                 // paint-order: the stroke drawn before the fill
    var fontSize: Float = 16.0              // In the user space; medium, as a browser has it
    var fontWeight = 400                    // normal 400, bold 700
    var italic = false                      // A font-style of italic or oblique
    var fontFamily = BundledFonts.SANS      // The family of the bundled fonts its names stand for
    var textAnchor = 0                      // 0 start, 1 middle, 2 end
    var baseline = 0                        // dominant-baseline: 0 the alphabetic, 1 the middle, 2 the hanging
    var markerStart: String? = nil          // The ids of the markers, inherited
    var markerMid: String? = nil
    var markerEnd: String? = nil
    var clipPath: String? = nil             // The id of the clip path of the element itself, not inherited
    var clips: [SVGPath]? = nil             // The clip paths of the element and of its ancestors
    var paints: [[UInt8]: Int32]? = nil     // The colors of the gradients, by the bytes of their ids

    // The matrices of transforms are [a b c d e f], which take x, y to
    // a*x + c*y + e, b*x + d*y + f, as SVG writes them.
    static let identity: [Double] = [1.0, 0.0, 0.0, 1.0, 0.0, 0.0]

    // Built once per process; avoids reflecting over ColorMap on every
    // color parsed.
    private static let colorMap: [String: Int32] = {
        var map: [String: Int32] = [:]
        let mirror = Mirror(reflecting: ColorMap())
        for child in mirror.children {
            if let label = child.label, let value = child.value as? Int32 {
                map[label] = value
            }
        }
        return map
    }()

    /// Sets a property of the state, of a presentation attribute, a rule or a
    /// style attribute. Properties that PDFjet does not draw, and values it
    /// cannot read, are left as they are inherited; it throws only for a color
    /// that starts with # and is not hexadecimal, as it always has.
    mutating func setProperty(_ name: String, _ value: String) throws {
        let value = SVGState.trimSpace(value)
        if value == "inherit" {
            return
        }
        switch name {
        case "fill", "stroke":
            let paint: (Int32, Float, Bool)?
            do {
                paint = try SVGState.parsePaint(value, paints)
            } catch let error as PDFjetError {
                throw PDFjetError(message: "invalid SVG " + name + ": " + error.description)
            }
            guard let (color, alpha, current) = paint else {
                return
            }
            if name == "fill" {
                fill = color
                fillAlpha = alpha
                fillCurrent = current
            } else {
                stroke = color
                strokeAlpha = alpha
                strokeCurrent = current
            }
        case "color":
            do {
                if let (c, alpha) = try SVGState.parseColor(value) {
                    color = c
                    colorAlpha = alpha
                }
            } catch let error as PDFjetError {
                throw PDFjetError(message: "invalid SVG color: " + error.description)
            }
        case "stroke-width":
            if SVGState.hasSuffix(value, "%") {
                return
            }
            if let width = SVGState.parseLength(value), width >= 0.0 {
                strokeWidth = width
            }
        case "fill-rule":
            if value == "evenodd" {
                evenOdd = true
            } else if value == "nonzero" {
                evenOdd = false
            }
        case "clip-rule":
            if value == "evenodd" {
                clipEvenOdd = true
            } else if value == "nonzero" {
                clipEvenOdd = false
            }
        case "stroke-linecap":
            switch value {
            case "butt": lineCap = CapStyle.BUTT
            case "round": lineCap = CapStyle.ROUND
            case "square": lineCap = CapStyle.PROJECTING_SQUARE
            default: break
            }
        case "stroke-linejoin":
            switch value {
            case "miter", "miter-clip", "arcs": lineJoin = JoinStyle.MITER
            case "round": lineJoin = JoinStyle.ROUND
            case "bevel": lineJoin = JoinStyle.BEVEL
            default: break
            }
        case "opacity", "fill-opacity", "stroke-opacity":
            guard let alpha = SVGState.parseOpacity(value) else {
                return
            }
            if name == "opacity" {
                ownOpacity = alpha
            } else if name == "fill-opacity" {
                fillOpacity = alpha
            } else {
                strokeOpacity = alpha
            }
        case "display":
            if value == "none" {
                hidden = true
            }
        case "stroke-miterlimit":
            // Below 1 is an error in SVG, and a PDF has to be able to hold it
            if let limit = SVGState.parseLength(value), limit >= 1.0, FastFloat.isWritable(limit) {
                miterLimit = limit
            }
        case "visibility":
            if value == "hidden" || value == "collapse" {
                invisible = true
            } else if value == "visible" {
                invisible = false
            }
        case "stroke-dasharray":
            dashArray = SVGState.parseDashArray(value)
        case "stroke-dashoffset":
            if !SVGState.hasSuffix(value, "%"), let offset = SVGState.parseLength(value) {
                dashOffset = offset
            }
        case "paint-order":
            // The stroke is drawn first when it comes before the fill, and
            // the markers, which PDFjet draws last, are left out.
            let order = SVGState.fields(value)
            if let stroke = order.firstIndex(of: "stroke") {
                let fill = order.firstIndex(of: "fill")
                strokeFirst = fill == nil || stroke < fill!
            } else {
                strokeFirst = false
            }
        case "font-size":
            if let size = SVGState.parseFontSize(value, fontSize) {
                fontSize = size
            }
        case "font-weight":
            if let weight = SVGState.parseFontWeight(value, fontWeight) {
                fontWeight = weight
            }
        case "font-style":
            if value == "italic" || SVGState.hasPrefix(value, "oblique") {
                italic = true
            } else if value == "normal" {
                italic = false
            }
        case "font-family":
            fontFamily = BundledFonts.familyOf(value)
        case "font":
            setFont(value)
        case "text-anchor":
            switch value {
            case "start": textAnchor = 0
            case "middle": textAnchor = 1
            case "end": textAnchor = 2
            default: break
            }
        case "dominant-baseline", "alignment-baseline":
            switch value {
            case "middle", "central": baseline = 1
            case "hanging", "text-before-edge": baseline = 2
            case "auto", "alphabetic", "baseline": baseline = 0
            default: break
            }
        case "marker-start":
            markerStart = SVGState.urlId(value)
        case "marker-mid":
            markerMid = SVGState.urlId(value)
        case "marker-end":
            markerEnd = SVGState.urlId(value)
        case "marker":
            markerStart = SVGState.urlId(value)
            markerMid = markerStart
            markerEnd = markerStart
        case "clip-path":
            clipPath = SVGState.urlId(value)
        default:
            break
        }
    }

    /// Returns the id of a reference, url(#id), with or without quotes, and
    /// nil for none or for a value of another kind.
    static func urlId(_ value: String) -> String? {
        let value = trimSpace(value)
        if !hasPrefix(value, "url(") {
            return nil
        }
        let scalars = Array(value.unicodeScalars)
        guard let end = scalars.firstIndex(of: ")") else {
            return nil
        }
        var id = Array(trimSpace(string(scalars[4..<end])).unicodeScalars)
        if id.count >= 2 && (id[0] == "\"" || id[0] == "'") && id[id.count - 1] == id[0] {
            id = Array(trimSpace(string(id[1..<(id.count - 1)])).unicodeScalars)
        }
        if id.count < 2 || id[0] != "#" {
            return nil
        }
        return string(id[1...])
    }

    /// Reads a stroke-dasharray: the lengths of the dashes and the gaps, as
    /// many again when they are an odd number. Returns nil, a solid line, for
    /// none, for a length below zero or one it cannot read, and for lengths
    /// that are all zero.
    static func parseDashArray(_ value: String) -> [Float]? {
        if value == "none" {
            return nil
        }
        var lengths = [Float]()
        for field in fields(string(value.unicodeScalars.map { $0 == "," ? " " : $0 })) {
            guard !hasSuffix(field, "%"), let length = parseLength(field),
                  length >= 0.0, length.isFinite else {
                return nil
            }
            lengths.append(length)
        }
        var sum: Float = 0.0
        for length in lengths {
            sum += length
        }
        if lengths.isEmpty || sum == 0.0 {
            return nil
        }
        let count = lengths.count
        var dashes = [Float]()
        for i in 0..<((count % 2 == 0) ? count : 2*count) {
            dashes.append(lengths[i % count])
        }
        return dashes
    }

    // The font sizes of the keywords, as a browser has them.
    private static let sizeNames = ["xx-small", "x-small", "small", "medium", "large", "x-large", "xx-large"]
    private static let sizes: [Float] = [9.0, 10.0, 13.0, 16.0, 18.0, 24.0, 32.0]

    /// Reads a font-size: a length, a number of em or a percentage of the
    /// font size of the parent, or a keyword. Returns nil for a size it
    /// cannot read or one below zero.
    static func parseFontSize(_ value: String, _ parentSize: Float) -> Float? {
        for (i, name) in sizeNames.enumerated() where value == name {
            return sizes[i]
        }
        var size: Float?
        if hasSuffix(value, "em") && !hasSuffix(value, "rem") {
            if let factor = parseFactor(string(value.unicodeScalars.dropLast(2))) {
                size = factor * parentSize
            }
        } else if hasSuffix(value, "%") {
            if let factor = parseFactor(string(value.unicodeScalars.dropLast(1))) {
                size = factor * parentSize / 100.0
            }
        } else {
            size = parseLength(value)
        }
        guard let fontSize = size, fontSize >= 0.0, fontSize.isFinite else {
            return nil
        }
        return fontSize
    }

    // Reads a number, or returns nil.
    private static func parseFactor(_ text: String) -> Float? {
        return parseFloat(trimSpace(text))
    }

    /// Reads a font-weight, from 1 to 1000: normal is 400 and bold 700, and
    /// bolder and lighter are those of CSS, from the weight of the parent.
    /// Returns nil for a weight it cannot read.
    static func parseFontWeight(_ value: String, _ parentWeight: Int) -> Int? {
        if value == "normal" {
            return 400
        } else if value == "bold" {
            return 700
        } else if value == "bolder" {
            return (parentWeight < 350) ? 400 : (parentWeight < 550) ? 700 : 900
        } else if value == "lighter" {
            return (parentWeight < 550) ? 100 : (parentWeight < 750) ? 400 : 700
        }
        guard let weight = parseFactor(value), weight >= 1.0, weight <= 1000.0 else {
            return nil
        }
        return Int((weight + 0.5).rounded(.down))     // Java's Math.round
    }

    // Sets the size, the weight, the style and the family of the font
    // shorthand, such as "bold 10px sans-serif": the words before the size,
    // then the size, which may have a line height after a slash, then the
    // family. A shorthand without a weight is normal.
    private mutating func setFont(_ value: String) {
        var weight = 400
        var italic = false
        let fields = SVGState.fields(value)
        for (i, field) in fields.enumerated() {
            let slash = field.unicodeScalars.contains("/")
            let size = SVGState.string(field.unicodeScalars.prefix(while: { $0 != "/" }))
            // A size has a unit or is a keyword; a number alone is a weight.
            // A digit is a decimal digit of one UTF-16 code unit, as Java's
            // Character.isDigit reads a char.
            if let last = size.unicodeScalars.last,
                    !(last.value <= 0xFFFF && last.properties.generalCategory == .decimalNumber),
                    let fontSize = SVGState.parseFontSize(size, self.fontSize) {
                self.fontSize = fontSize
                self.fontWeight = weight
                self.italic = italic
                // The family after the size, and a line height after a slash
                // written apart from it
                var from = i + 1
                if !slash && from < fields.count && SVGState.hasPrefix(fields[from], "/") {
                    from += (fields[from] == "/") ? 2 : 1
                }
                self.fontFamily = BundledFonts.familyOf(
                        (from < fields.count) ? fields[from...].joined(separator: " ") : "")
                return
            }
            if field == "italic" || field == "oblique" {
                italic = true
                continue
            }
            if let w = SVGState.parseFontWeight(field, self.fontWeight) {
                weight = w
            }
        }
    }

    // Reads the value of a fill or a stroke: none, currentColor, a color, or a
    // url of a gradient, which PDFjet draws in the mean color of its stops. A
    // url of anything else, a pattern or an element that is not there, is the
    // color after it, which SVG draws when it cannot draw the url, or none, as
    // a browser draws it. Returns the paint and whether it is currentColor, or
    // nil for a value that leaves the paint as it is inherited.
    private static func parsePaint(_ value: String, _ paints: [[UInt8]: Int32]?) throws -> (Int32, Float, Bool)? {
        var value = value
        if hasPrefix(value, "url(") {
            let scalars = Array(value.unicodeScalars)
            guard let end = scalars.firstIndex(of: ")") else {
                return nil
            }
            if let id = urlId(string(scalars[...end])), let gradient = paints?[Array(id.utf8)] {
                return (gradient, 1.0, false)
            }
            value = trimSpace(string(scalars[(end + 1)...]))
            if value.isEmpty {
                value = "none"
            }
        }
        let keyword = toLower(value)
        if keyword == "none" {
            return (Color.transparent, 1.0, false)
        }
        if keyword == "currentcolor" {
            return (0, 1.0, true)
        }
        guard let (color, alpha) = try parseColor(value) else {
            return nil
        }
        return (color, alpha, false)
    }

    // The alpha the fill is drawn with: its opacities and the alpha of its
    // color.
    func drawnFillAlpha() -> Float {
        return fillOpacity * opacity * (fillCurrent ? colorAlpha : fillAlpha)
    }

    // The alpha the stroke is drawn with.
    func drawnStrokeAlpha() -> Float {
        return strokeOpacity * opacity * (strokeCurrent ? colorAlpha : strokeAlpha)
    }

    // Reads a color and its alpha: #rgb, #rgba, #rrggbb, #rrggbbaa, rgb() of
    // three numbers or percentages, hsl(), with an alpha after them in either,
    // or a name. Returns nil for a color it does not know, and throws for one
    // that starts with # but is not hexadecimal.
    private static func parseColor(_ value: String) throws -> (Int32, Float)? {
        if hasPrefix(value, "#") {
            // The digits are counted in bytes, as the other ports count them.
            var hex = Array(value.utf8.dropFirst())
            if hex.count == 3 || hex.count == 4 {
                hex = hex.flatMap { [$0, $0] }
            }
            if hex.count != 6 && hex.count != 8 {
                return nil
            }
            guard let c = Int32(String(decoding: hex[0..<6], as: UTF8.self), radix: 16) else {
                throw PDFjetError(message: "invalid color \"" + value + "\"")
            }
            if hex.count == 6 {
                return (c, 1.0)
            }
            guard let a = Int32(String(decoding: hex[6...], as: UTF8.self), radix: 16) else {
                throw PDFjetError(message: "invalid color \"" + value + "\"")
            }
            return (c, Float(max(a, 0)) / 255.0)
        }
        let lower = toLower(value)
        let hsl = hasPrefix(lower, "hsl(") || hasPrefix(lower, "hsla(")
        if hsl || hasPrefix(lower, "rgb(") || hasPrefix(lower, "rgba(") {
            let scalars = Array(lower.unicodeScalars)
            let open = scalars.firstIndex(of: "(")!
            guard let end = scalars.firstIndex(of: ")"), end >= open else {
                return nil
            }
            let parts = scalars[(open + 1)..<end].split(whereSeparator: {
                $0 == "," || $0 == "/" || isSpace($0)
            })
            if parts.count < 3 {
                return nil
            }
            let alpha = (parts.count > 3) ? parseOpacity(string(parts[parts.startIndex + 3])) ?? 1.0 : 1.0
            if hsl {
                return parseHSL(parts.map { string($0) }).map { ($0, alpha) }
            }
            var rgb: Int32 = 0
            for part in parts[0..<3] {
                var digits = part
                var scale = 1.0
                if digits.last == "%" {
                    digits = digits.dropLast()
                    scale = 2.55
                }
                guard let number = parseDouble(string(digits)), number.isFinite else {
                    return nil
                }
                let channel = (number * scale).rounded()
                rgb = rgb << 8 | Int32(goMax(0.0, goMin(255.0, channel)))
            }
            return (rgb, alpha)
        }
        return colorMap[lower].map { ($0, 1.0) }
    }

    // Returns the color of the hue, the saturation and the lightness of
    // hsl(), by the formula of CSS Color 4, or nil for one it cannot read.
    private static func parseHSL(_ parts: [String]) -> Int32? {
        guard let hue = parseHue(parts[0]), let saturation = parsePercentage(parts[1]),
              let lightness = parsePercentage(parts[2]) else {
            return nil
        }
        let a = saturation * goMin(lightness, 1.0 - lightness)
        var rgb: Int32 = 0
        for n in [0.0, 8.0, 4.0] {
            let k = (n + hue / 30.0).truncatingRemainder(dividingBy: 12.0)
            let channel = lightness - a * goMax(-1.0, goMin(goMin(k - 3.0, 9.0 - k), 1.0))
            rgb = rgb << 8 | Int32(goMax(0.0, goMin(255.0, (channel * 255.0).rounded())))
        }
        return rgb
    }

    // Reads a hue, in degrees unless it has a unit, and returns it from 0 to
    // 360, or nil for one it cannot read.
    private static func parseHue(_ text: String) -> Double? {
        var scale = 1.0
        var digits = text
        for (unit, degrees) in [("deg", 1.0), ("grad", 0.9), ("rad", 57.29577951308232), ("turn", 360.0)]
                where hasSuffix(digits, unit) {
            digits = string(digits.unicodeScalars.dropLast(unit.unicodeScalars.count))
            scale = degrees
            break
        }
        guard let number = parseDouble(digits), number.isFinite else {
            return nil
        }
        let hue = (number * scale).truncatingRemainder(dividingBy: 360.0)
        if hue.isNaN {
            return nil      // Too large for its unit
        }
        return (hue < 0.0) ? hue + 360.0 : hue
    }

    // Reads a saturation or a lightness, with or without its percent sign,
    // and returns it between 0 and 1, or nil for one it cannot read.
    private static func parsePercentage(_ text: String) -> Double? {
        let digits = hasSuffix(text, "%") ? string(text.unicodeScalars.dropLast()) : text
        guard let number = parseDouble(digits), number.isFinite else {
            return nil
        }
        return goMax(0.0, goMin(1.0, number / 100.0))
    }

    // Reads an opacity, a number or a percentage, and returns it between 0 and 1.
    private static func parseOpacity(_ value: String) -> Float? {
        var value = value
        var scale = 1.0
        if hasSuffix(value, "%") {
            value = String(String.UnicodeScalarView(value.unicodeScalars.dropLast()))
            scale = 0.01
        }
        guard let number = parseDouble(trimSpace(value)), !number.isNaN else {
            return nil
        }
        return Float(goMax(0.0, goMin(1.0, number * scale)))
    }

    /// Reads a length in user units, which PDFjet draws as points, with or
    /// without a unit; an empty value is 0, as an omitted attribute. Returns
    /// nil for one that is not a number.
    static func parseLength(_ value: String) -> Float? {
        var text = trimSpace(value)
        var scale: Float = 1.0
        for (suffix, points) in SVGImage.units where hasSuffix(text, suffix) {
            text = trimSpace(String(String.UnicodeScalarView(
                    text.unicodeScalars.dropLast(suffix.unicodeScalars.count))))
            scale = points
            break
        }
        guard let number = SVGImage.parseFloatLenient(text), number.isFinite else {
            return nil
        }
        return number * scale
    }

    /// Reads a coordinate or a size of a shape, and returns 0 for one it
    /// cannot read, a percentage among them.
    static func parseCoordinate(_ value: String) -> Double {
        guard let length = parseLength(value), !hasSuffix(trimSpace(value), "%") else {
            return 0.0
        }
        return Double(length)
    }

    /// Reads a list of numbers, as the points of a polygon or the arguments of
    /// a transform are written: separated by white space or a comma, or by
    /// nothing where a sign or a second point starts the next one. Returns nil
    /// for a list with something that is not a finite number.
    static func parseNumbers(_ text: String) -> [Double]? {
        var numbers = [Double]()
        var buf = String.UnicodeScalarView()
        var failed = false
        func flush() {
            if buf.isEmpty {
                return
            }
            let number = parseDouble(String(buf))
            buf.removeAll()
            if let number = number, number.isFinite {
                numbers.append(number)
            } else {
                failed = true
            }
        }
        for ch in text.unicodeScalars {
            if isSpace(ch) || ch == "," {
                flush()
            } else if (ch == "-" || ch == "+") && !(buf.last == "e" || buf.last == "E") ||
                    ch == "." && buf.contains(".") {
                flush()
                buf.append(ch)
            } else {
                buf.append(ch)
            }
            if failed {
                return nil
            }
        }
        flush()
        return failed ? nil : numbers
    }

    /// Returns the transform that is n, then m.
    static func multiply(_ m: [Double], _ n: [Double]) -> [Double] {
        return [
            m[0]*n[0] + m[2]*n[1],
            m[1]*n[0] + m[3]*n[1],
            m[0]*n[2] + m[2]*n[3],
            m[1]*n[2] + m[3]*n[3],
            m[0]*n[4] + m[2]*n[5] + m[4],
            m[1]*n[4] + m[3]*n[5] + m[5],
        ]
    }

    /// Returns the matrix of a transform attribute, the list of its matrix,
    /// translate, scale, rotate, skewX and skewY, applied from the last to the
    /// first, or nil for a list PDFjet cannot read, which is drawn as if the
    /// element had none.
    static func parseTransform(_ text: String) -> [Double]? {
        var matrix = identity
        var rest = Array(trimSpace(text).unicodeScalars)[...]
        while !rest.isEmpty {
            guard let open = rest.firstIndex(of: "("),
                  let end = rest.firstIndex(of: ")"), end >= open else {
                return nil
            }
            let name = trimSpace(string(rest[..<open]))
            guard let args = parseNumbers(string(rest[(open + 1)..<end])) else {
                return nil
            }
            rest = rest[(end + 1)...].drop(while: {
                $0 == " " || $0 == "\t" || $0 == "\r" || $0 == "\n" || $0 == ","
            })
            var m: [Double]
            switch (name, args.count) {
            case ("matrix", 6):
                m = args
            case ("translate", 1):
                m = [1.0, 0.0, 0.0, 1.0, args[0], 0.0]
            case ("translate", 2):
                m = [1.0, 0.0, 0.0, 1.0, args[0], args[1]]
            case ("scale", 1):
                m = [args[0], 0.0, 0.0, args[0], 0.0, 0.0]
            case ("scale", 2):
                m = [args[0], 0.0, 0.0, args[1], 0.0, 0.0]
            case ("rotate", 1), ("rotate", 3):
                let angle = args[0] * Double.pi / 180.0
                let cosine = cos(angle)
                let sine = sin(angle)
                m = [cosine, sine, -sine, cosine, 0.0, 0.0]
                if args.count == 3 {
                    m = multiply([1.0, 0.0, 0.0, 1.0, args[1], args[2]],
                            multiply(m, [1.0, 0.0, 0.0, 1.0, -args[1], -args[2]]))
                }
            case (let skew, 1) where (skew == "skewX" || skew == "skewY") &&
                    abs(args[0]).truncatingRemainder(dividingBy: 180.0) == 90.0:
                return nil  // A skew of 90 degrees flattens everything: not a transform
            case ("skewX", 1):
                m = [1.0, 0.0, tan(args[0] * Double.pi / 180.0), 1.0, 0.0, 0.0]
            case ("skewY", 1):
                m = [1.0, tan(args[0] * Double.pi / 180.0), 0.0, 1.0, 0.0, 0.0]
            default:
                return nil
            }
            matrix = multiply(matrix, m)
        }
        return matrix
    }

    // The text below is read as the other ports read it: by code point, with
    // the white space of Unicode, and compared byte for byte, where Swift
    // compares characters, of one or more code points, and treats canonically
    // equivalent ones as equal.

    // Returns true for the white space that separates the numbers of a list:
    // the white space of XML.
    static func isSpace(_ ch: Unicode.Scalar) -> Bool {
        return ch == " " || ch == "\t" || ch == "\n" || ch == "\r"
    }

    static func string<S: Sequence>(_ scalars: S) -> String where S.Element == Unicode.Scalar {
        return String(String.UnicodeScalarView(scalars))
    }

    static func hasPrefix(_ text: String, _ prefix: String) -> Bool {
        return text.utf8.starts(with: prefix.utf8)
    }

    static func hasSuffix(_ text: String, _ suffix: String) -> Bool {
        return text.utf8.reversed().starts(with: suffix.utf8.reversed())
    }

    static func equal(_ a: String, _ b: String) -> Bool {
        return a.utf8.elementsEqual(b.utf8)
    }

    /// Returns the text without the Unicode white space at its ends.
    static func trimSpace(_ text: String) -> String {
        let scalars = text.unicodeScalars
        var start = scalars.startIndex
        var end = scalars.endIndex
        while start < end && scalars[start].properties.isWhitespace {
            start = scalars.index(after: start)
        }
        while end > start && scalars[scalars.index(before: end)].properties.isWhitespace {
            end = scalars.index(before: end)
        }
        return String(scalars[start..<end])
    }

    /// Returns the words of the text, between its Unicode white space.
    static func fields(_ text: String) -> [String] {
        return text.unicodeScalars
                .split(whereSeparator: { $0.properties.isWhitespace })
                .map { string($0) }
    }

    // Returns the text in lower case, a code point at a time.
    static func toLower(_ text: String) -> String {
        var lower = String.UnicodeScalarView()
        for scalar in text.unicodeScalars {
            if scalar == "\u{130}" {
                lower.append("i")   // The simple lower case of İ
            } else {
                lower.append(contentsOf: scalar.properties.lowercaseMapping.unicodeScalars)
            }
        }
        return String(lower)
    }

    /// Returns the number of the text, or nil for text that is not one: a
    /// number is written in decimal, a NaN is written nan, and a
    /// number too large for a Double is not read as an infinity.
    static func parseDouble(_ text: String) -> Double? {
        guard let number = Double(text), isNumber(text, number) else {
            return nil
        }
        return number
    }

    /// Returns the number of the text as a Float, as parseDouble reads it.
    static func parseFloat(_ text: String) -> Float? {
        guard let number = Float(text), isNumber(text, Double(number)) else {
            return nil
        }
        return number
    }

    private static func isNumber(_ text: String, _ number: Double) -> Bool {
        var digits = text.lowercased()
        if number.isNaN {
            return digits == "nan"
        }
        if digits.hasPrefix("+") || digits.hasPrefix("-") {
            digits.removeFirst()
        }
        if number.isInfinite {
            return digits == "inf" || digits == "infinity"
        }
        // Only a number written in decimal: a hexadecimal number, as 0x1p3,
        // is none of SVG's, and C# did not read one (review of
        // 17 October 2026)
        return digits.allSatisfy { "0123456789.e+-".contains($0) }
    }

    // The math.Min and math.Max of the other ports: NaN if either number is,
    // and -0 less than +0.
    static func goMin(_ x: Double, _ y: Double) -> Double {
        if x == -Double.infinity || y == -Double.infinity {
            return -Double.infinity
        }
        if x.isNaN || y.isNaN {
            return Double.nan
        }
        if x == 0.0 && y == 0.0 {
            return x.sign == .minus ? x : y
        }
        return x < y ? x : y
    }

    static func goMax(_ x: Double, _ y: Double) -> Double {
        if x == Double.infinity || y == Double.infinity {
            return Double.infinity
        }
        if x.isNaN || y.isNaN {
            return Double.nan
        }
        if x == 0.0 && y == 0.0 {
            return x.sign == .minus ? y : x
        }
        return x > y ? x : y
    }
}

/// The rules of the <style> elements of an SVG file, in their order, and where
/// the rules of each class are among them, so that an element finds the rules
/// of its classes without going through all of them. A class is its bytes, as
/// SVGState.equal compares names.
struct SVGStyleSheet {
    private var rules = [SVGRule]()
    private var byClass = [[UInt8]: [Int]]()
    // The declarations its rules have applied, to elements, maxApplied at
    // most: N copies of a rule for a class and N elements of it were N x N
    // declarations, 20 s for 416 KB (review of 17 October 2026)
    static let maxApplied = 1000000
    private var applied = 0

    /// Adds the rules of a <style> element after those before it.
    mutating func add(_ rules: [SVGRule]) {
        for rule in rules {
            byClass[Array(rule.name.utf8), default: []].append(self.rules.count)
            self.rules.append(rule)
        }
    }

    /// Returns the rules for any of the classes, in the order of the style
    /// sheet, each rule once.
    mutating func forClasses(_ classes: [String]) throws -> [SVGRule] {
        var indexes = [Int]()
        var seen = Set<[UInt8]>()
        for name in classes {
            let key = Array(name.utf8)
            if !seen.insert(key).inserted {
                continue    // A class named twice has its rules once.
            }
            indexes.append(contentsOf: byClass[key] ?? [])
        }
        var found = [SVGRule]()
        for index in indexes.sorted() {
            applied += rules[index].declarations.count
            if applied > SVGStyleSheet.maxApplied {
                throw PDFjetError(message:
                    "The style sheet of the SVG applies more than \(SVGStyleSheet.maxApplied) declarations.")
            }
            found.append(rules[index])
        }
        return found
    }
}

/// A rule of the style sheet of an SVG file for one class: .name followed by
/// its declarations. Selectors of other kinds are left out.
struct SVGRule {
    let name: String
    let declarations: [(String, String)]

    /// Returns the rules of the text of a <style> element that are for a
    /// class, in the order of the text.
    static func parseStyleSheet(_ text: String) -> [SVGRule] {
        // Comments are left out first; they may hold braces. A comment that
        // is not closed runs to the end of the text.
        let scalars = Array(text.unicodeScalars)
        var text = [Unicode.Scalar]()
        var i = 0
        while true {
            guard let start = index(scalars, "/*", i) else {
                text.append(contentsOf: scalars[i...])
                break
            }
            text.append(contentsOf: scalars[i..<start])
            guard let end = index(scalars, "*/", start + 2) else {
                break
            }
            text.append(" ")
            i = end + 2
        }
        var rules = [SVGRule]()
        for block in text.split(separator: "}", omittingEmptySubsequences: false) {
            guard let open = block.firstIndex(of: "{") else {
                continue
            }
            let declarations = parseDeclarations(SVGState.string(block[(open + 1)...]))
            for selector in block[..<open].split(separator: ",", omittingEmptySubsequences: false) {
                let selector = Array(SVGState.trimSpace(SVGState.string(selector)).unicodeScalars)
                if selector.count > 1 && selector[0] == "." && isName(selector[1...]) {
                    rules.append(SVGRule(name: SVGState.string(selector[1...]), declarations: declarations))
                }
            }
        }
        return rules
    }

    // Returns the index of the first occurrence of the text at or after i.
    private static func index(_ scalars: [Unicode.Scalar], _ text: String, _ i: Int) -> Int? {
        let pattern = Array(text.unicodeScalars)
        var j = i
        while j + pattern.count <= scalars.count {
            if scalars[j..<(j + pattern.count)].elementsEqual(pattern) {
                return j
            }
            j += 1
        }
        return nil
    }

    // Returns true when the text is a name of a class alone, without the
    // combinators, the pseudo-classes and the like of other selectors.
    private static func isName(_ text: ArraySlice<Unicode.Scalar>) -> Bool {
        for ch in text {
            let value = ch.value
            if !(ch == "-" || ch == "_" || (value >= 0x30 && value <= 0x39) ||
                    (value >= 0x61 && value <= 0x7A) || (value >= 0x41 && value <= 0x5A) || value > 127) {
                return false
            }
        }
        return true
    }

    /// Returns the declarations of a style attribute or of a rule,
    /// name: value; each, without !important.
    static func parseDeclarations(_ text: String) -> [(String, String)] {
        var declarations = [(String, String)]()
        for declaration in text.unicodeScalars.split(separator: ";", omittingEmptySubsequences: false) {
            guard let colon = declaration.firstIndex(of: ":") else {
                continue
            }
            let name = SVGState.trimSpace(SVGState.string(declaration[..<colon]))
            var value = SVGState.trimSpace(SVGState.string(declaration[declaration.index(after: colon)...]))
            if SVGState.hasSuffix(value, "!important") {
                value = SVGState.trimSpace(SVGState.string(value.unicodeScalars.dropLast(10)))
            }
            if !name.isEmpty && !value.isEmpty {
                declarations.append((name, value))
            }
        }
        return declarations
    }
}   // End of SVGState.swift
