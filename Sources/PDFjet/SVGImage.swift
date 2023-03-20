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
    public init(_ stream: InputStream) {
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
        for scalar in scalars {
            if !token && buf.hasSuffix(" width=") {
                token = true
                param = "width"
                buf = ""
            } else if !token && buf.hasSuffix(" height=") {
                token = true
                param = "height"
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
                } else if param == "data" {
                    path!.data = buf
                } else if param == "fill" {
                    if buf == "none" {
                        path!.fill = Color.transparent
                    } else {
                        path!.fill = mapColorNameToValue(buf)
                    }
                } else if param == "stroke" {
                    path!.stroke = mapColorNameToValue(buf)
                } else if param == "stroke-width" {
                    path!.strokeWidth = Float(buf)!
                }
                buf = ""
            } else {
                buf.append(String(scalar))
            }
        }
        if path != nil {
            paths!.append(path!)
        }


        var i = 0
        while i < paths!.count {
            path = paths![i]
            path!.operations = SVG.getOperations(path!.data!)
            path!.operations = SVG.toPDF(path!.operations!)
            i += 1
        }
    }

    func mapColorNameToValue(_ colorName: String) -> Int32 {
        var color = Color.black
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
