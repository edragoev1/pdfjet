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
public class SVGImage {
    var x: Float = 0.0  // location x
    var y: Float = 0.0  // location y
    var w: Float = 0.0  // SVG width
    var h: Float = 0.0  // SVG height
    var viewBox: String?
    var fill: Int32 = Color.transparent
    var stroke: Int32 = Color.transparent
    var strokeWidth: Float = 0.0
    var paths: [SVGPath]?
    var uri: String?
    var key: String?
    var language: String?
    var actualText: String = Single.space
    var altDescription: String = Single.space

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
     * @param fileAtPath the path to the SVG file.
     */
    public convenience init?(fileAtPath: String) {
        guard let fileStream = InputStream(fileAtPath: fileAtPath) else {
            return nil  // cannot open file
        }
        // This constructor owns the stream it created, so it closes it
        // once reading is done. (Deferred until after self.init returns,
        // since reading happens inside the designated initializer.)
        defer { fileStream.close() }
        self.init(stream: fileStream)
    }

    /**
     * Used to embed SVG images in the PDF document.
     *
     * @param stream the input stream.
     * @throws Exception  if exception occurred.
     */
    public init(stream: InputStream) {
        paths = [SVGPath]()
        var path: SVGPath?
        // The caller retains ownership of the stream — we read from it
        // but do not close it, consistent with the Java and .NET editions.
        stream.open()
        var scalars = [UnicodeScalar]()
        var buffer = [UInt8](repeating: 0, count: 1)
        while stream.hasBytesAvailable {
            let read = stream.read(&buffer, maxLength: 1)
            if (read == 0) {
                break
            }
            scalars.append(UnicodeScalar(buffer[0]))
        }

        var buf = String()
        var token = false
        var param: String?
        var header: Bool = false
        for scalar in scalars {
            if buf.hasSuffix("<svg") {
                header = true
                buf = ""
            } else if header && scalar == ">" {
                header = false
                buf = ""
            } else if !token && buf.hasSuffix(" width=") {
                token = true
                param = "width"
                buf = ""
            } else if !token && buf.hasSuffix(" height=") {
                token = true
                param = "height"
                buf = ""
            } else if !token && buf.hasSuffix(" viewBox=") {
                token = true
                param = "viewBox"
                buf = ""
            } else if !token && buf.hasSuffix(" d=") {
                token = true
                if let pending = path {
                    paths?.append(pending)
                }
                path = SVGPath()
                param = "data"
                buf = ""
            } else if !token && buf.hasSuffix(" fill=") {
                token = true
                param = "fill"
                buf = ""
            } else if !token && buf.hasSuffix(" stroke=") {
                token = true
                param = "stroke"
                buf = ""
            } else if !token && buf.hasSuffix(" stroke-width=") {
                token = true
                param = "stroke-width"
                buf = ""
            } else if token && scalar == UnicodeScalar("\"") {
                token = false
                if param == "width" {
                    if let value = Float(buf) {
                        w = value
                    }
                } else if param == "height" {
                    if let value = Float(buf) {
                        h = value
                    }
                } else if param == "viewBox" {
                    viewBox = buf
                } else if param == "data" {
                    path?.data = buf
                } else if param == "fill" {
                    let fillColor = getColor(buf)
                    if header {
                        self.fill = fillColor
                    } else {
                        path?.fill = fillColor
                    }
                } else if param == "stroke" {
                    let strokeColor = getColor(buf)
                    if header {
                        self.stroke = strokeColor
                    } else {
                        path?.stroke = strokeColor
                    }
                } else if param == "stroke-width" {
                    let strokeWidth = Float(buf) ?? 0.0
                    if (header) {
                        self.strokeWidth = strokeWidth
                    } else {
                        path?.strokeWidth = strokeWidth
                    }
                }
                buf = ""
            } else {
                buf.append(String(scalar))
            }
        }
        if let last = path {
            paths?.append(last)
        }
        processPaths(paths ?? [])
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

    func getColor(_ colorName: String) -> Int32 {
        if colorName.hasPrefix("#") {
            if colorName.count == 7 {
                let index = colorName.index(colorName.startIndex, offsetBy: 1)
                guard let value = Int32(colorName[index...], radix: 16) else {
                    return Color.transparent
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
                if let value = Int32(str, radix: 16) {
                    return value
                }
                return Color.transparent
            } else {
                return Color.transparent
            }
        }
        return SVGImage.colorCache[colorName] ?? Color.transparent
    }

    /**
     *  Sets the location of this SVG on the page.
     *
     *  @param x the x coordinate of the top left corner of this box when drawn on the page.
     *  @param y the y coordinate of the top left corner of this box when drawn on the page.
     *  @return this SVG object, to allow method chaining.
     */
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> SVGImage {
        self.x = x
        self.y = y
        return self
    }

    public func scaleBy(_ factor: Float) {
        guard let paths = paths else {
            return
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
    }

    public func getPenWidth() -> Float {
        return self.w
    }

    public func getHeight() -> Float {
        return self.h
    }

    private func drawPath(_ path: SVGPath, _ page: Page) {
        var fillColor = path.fill
        if fillColor == Color.transparent {
            fillColor = self.fill
        }
        var strokeColor = path.stroke
        if strokeColor == Color.transparent {
            strokeColor = self.stroke
        }
        var strokeWidth = self.strokeWidth
        if path.strokeWidth > strokeWidth {
            strokeWidth = path.strokeWidth
        }

        if fillColor == Color.transparent &&
                strokeColor == Color.transparent {
            fillColor = Color.black
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
                    page.closePath()
                }
            }
        }
    }

    @discardableResult
    public func drawOn(_ page: Page) -> [Float] {
        page.addBMC(StructElem.P, language, actualText, altDescription)
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
                    0.0,    // Transparency
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