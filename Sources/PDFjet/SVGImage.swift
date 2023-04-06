/**
 *  SVGImage.swift
 *
Copyright 2023 Innovatics Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
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


    /**
     * Used to embed SVG images in the PDF document.
     *
     * @param stream the input stream.
     * @throws Exception  if exception occurred.
     */
    public convenience init(fileAtPath: String) {
        self.init(stream: InputStream(fileAtPath: fileAtPath)!)
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
        stream.open()
        defer {
            stream.close()
        }
        let buffer = UnsafeMutablePointer<UInt8>.allocate(capacity: 1)
        defer {
            buffer.deallocate()
        }
        var scalars = [UnicodeScalar]()
        while stream.hasBytesAvailable {
            let read = stream.read(buffer, maxLength: 1)
            if read > 0 {
                scalars.append(UnicodeScalar(buffer[0]))
            }
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
                if path != nil {
                    paths!.append(path!)
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
                    w = Float(buf)!
                } else if param == "height" {
                    h = Float(buf)!
                } else if param == "viewBox" {
                    viewBox = buf
                } else if param == "data" {
                    path!.data = buf
                } else if param == "fill" {
                    let fillColor = getColor(buf)
                    if header {
                        self.fill = fillColor
                    } else {
                        path!.fill = fillColor
                    }
                } else if param == "stroke" {
                    let strokeColor = getColor(buf)
                    if header {
                        self.stroke = strokeColor
                    } else {
                        path!.stroke = strokeColor
                    }
                } else if param == "stroke-width" {
                    let strokeWidth = Float(buf)!
                    if (header) {
                        self.strokeWidth = strokeWidth
                    } else {
                        path!.strokeWidth = strokeWidth
                    }
                }
                buf = ""
            } else {
                buf.append(String(scalar))
            }
        }
        if path != nil {
            paths!.append(path!)
        }
        processPaths(paths!)
    }
    
    func processPaths(_ paths: [SVGPath]) {
        var box: [Float] = Array(repeating: 0.0, count: 4)
        if viewBox != nil {
            let list = viewBox!.trim().components(separatedBy: .whitespaces)
            box[0] = Float(list[0])!
            box[1] = Float(list[1])!
            box[2] = Float(list[2])!
            box[3] = Float(list[3])!
        }
        for path in paths {
            path.operations = SVG.getOperations(path.data!)
            path.operations = SVG.toPDF(path.operations!)
            if viewBox != nil {
                for op in path.operations! {
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
                return Int32(colorName[index...], radix: 16)!
            } else if colorName.count == 4 {
                let index1 = colorName.index(colorName.startIndex, offsetBy: 1)
                let index2 = colorName.index(colorName.startIndex, offsetBy: 2)
                let index3 = colorName.index(colorName.startIndex, offsetBy: 3)
                let str1 = colorName[index1..<index2]
                let str2 = colorName[index2..<index3]
                let str3 = colorName[index3...]
                let str = String(str1 + str1 + str2 + str2 + str3 + str3)
                return Int32(str, radix: 16)!
            } else {
                return Color.transparent
            }
        }
        var color = Color.transparent
        let mirror = Mirror(reflecting: ColorMap())
        mirror.children.forEach { child in
            if child.label! == colorName {
                color = child.value as! Int32
            }
        }
        return color
    }

    /**
     *  Sets the location of this SVG on the page.
     *
     *  @param x the x coordinate of the top left corner of this box when drawn on the page.
     *  @param y the y coordinate of the top left corner of this box when drawn on the page.
     *  @return this SVG object.
     */
    public func setLocation(_ x: Float, _ y: Float) {
        self.x = x
        self.y = y
    }

    public func setScale(_ factor: Float) {
        // TODO:
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

        if fillColor != Color.transparent {
            for op in path.operations! {
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
            for op in path.operations! {
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

    public func drawOn(_ page: Page) -> [Float] {
        page.addBMC(StructElem.P, language, actualText, altDescription)
        for path in paths! {
            drawPath(path, page)
        }
        page.addEMC()
        if (uri != nil || key != nil) {
            page.addAnnotation(Annotation(
                    uri,
                    key,    // The destination name
                    x,
                    y,
                    x + w,
                    y + h,
                    language,
                    actualText,
                    altDescription))
        }
        return [self.x + self.w, self.y + self.h]
    }
}   // End of SVGImage.java
