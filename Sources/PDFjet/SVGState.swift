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
    var strokeWidth: Float = 1.0            // In the user space of the element
    var evenOdd = false                     // fill-rule="evenodd"
    var lineCap = CapStyle.BUTT
    var lineJoin = JoinStyle.MITER
    var fillOpacity: Float = 1.0
    var strokeOpacity: Float = 1.0
    var opacity: Float = 1.0                // The opacities of the element and its ancestors, multiplied
    var ownOpacity: Float = 1.0             // The opacity of the element itself, not inherited
    var matrix = SVGState.identity
    var hidden = false                      // In <defs> and the like, or under display="none": not drawn

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
            let paint: (Int32, Bool)?
            do {
                paint = try SVGState.parsePaint(value)
            } catch let error as PDFjetError {
                throw PDFjetError(message: "invalid SVG " + name + ": " + error.description)
            }
            guard let (color, current) = paint else {
                return
            }
            if name == "fill" {
                fill = color
                fillCurrent = current
            } else {
                stroke = color
                strokeCurrent = current
            }
        case "color":
            do {
                if let c = try SVGState.parseColor(value) {
                    color = c
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
        default:
            break
        }
    }

    // Reads the value of a fill or a stroke: none, currentColor, a color, or a
    // url of a gradient or a pattern, which PDFjet does not draw, and the
    // color after it, if any, which SVG draws when it cannot. Returns the
    // paint and whether it is currentColor, or nil for a value that leaves
    // the paint as it is inherited.
    private static func parsePaint(_ value: String) throws -> (Int32, Bool)? {
        var value = value
        if hasPrefix(value, "url(") {
            let scalars = Array(value.unicodeScalars)
            guard let end = scalars.firstIndex(of: ")") else {
                return nil
            }
            value = trimSpace(string(scalars[(end + 1)...]))
            if value.isEmpty {
                return nil
            }
        }
        if value == "none" {
            return (Color.transparent, false)
        }
        if value == "currentColor" {
            return (0, true)
        }
        guard let color = try parseColor(value) else {
            return nil
        }
        return (color, false)
    }

    // Reads a color: #rgb, #rrggbb, rgb() of three numbers or percentages, or
    // a name. Returns nil for a color it does not know, and throws for one
    // that starts with # but is not hexadecimal.
    private static func parseColor(_ value: String) throws -> Int32? {
        if hasPrefix(value, "#") {
            // The digits are counted in bytes, as the other ports count them.
            var hex = Array(value.utf8.dropFirst())
            if hex.count == 3 {
                hex = [hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]]
            }
            if hex.count != 6 {
                return nil
            }
            guard let c = Int32(String(decoding: hex, as: UTF8.self), radix: 16) else {
                throw PDFjetError(message: "invalid color \"" + value + "\"")
            }
            return c
        }
        let lower = toLower(value)
        if hasPrefix(lower, "rgb(") || hasPrefix(lower, "rgba(") {
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
            return rgb
        }
        return colorMap[lower]
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
    private static func toLower(_ text: String) -> String {
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
    /// hexadecimal number needs an exponent, a NaN is written nan, and a
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
        return !digits.hasPrefix("0x") || digits.contains("p")
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

/// A rule of the style sheet of an SVG file for one class: .name followed by
/// its declarations. Selectors of other kinds are left out.
struct SVGRule {
    let name: String
    let declarations: [(String, String)]

    /// Returns the rules of the text of a <style> element that are for a
    /// class, in the order of the text.
    static func parseStyleSheet(_ text: String) -> [SVGRule] {
        // Comments are left out first; they may hold braces.
        var text = Array(text.unicodeScalars)
        while let start = index(text, "/*", 0) {
            guard let end = index(text, "*/", start + 2) else {
                text = Array(text[..<start])
                break
            }
            text = Array(text[..<start]) + [" "] + Array(text[(end + 2)...])
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
