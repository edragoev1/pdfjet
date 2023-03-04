/**
 *  Page.swift
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


///
/// Used to create PDF page objects.
///
/// Please note:
/// <pre>
///   The coordinate (0.0, 0.0) is the top left corner of the page.
///   The size of the pages are represented in points.
///   1 point is 1/72 inches.
/// </pre>
///
public class Page {

    var pdf: PDF?
    var pageObj: PDFobj?
    var objNumber = 0
    var buf = [UInt8]()
    var tm: [Float] = [1.0, 0.0, 0.0, 1.0]
    var renderingMode = 0
    var width: Float = 0.0
    var height: Float = 0.0
    var contents = [Int]()
    var annots: [Annotation]?
    var destinations: [Destination]?
    var cropBox: [Float]?
    var bleedBox: [Float]?
    var trimBox: [Float]?
    var artBox: [Float]?
    var structures = [StructElem]()

    private var pen: [Float] = [0.0, 0.0, 0.0]
    private var brush: [Float] = [0.0, 0.0, 0.0]

    private var penCMYK: [Float] = [0.0, 0.0, 0.0, 1.0]
    private var brushCMYK: [Float] = [0.0, 0.0, 0.0, 1.0]

    private var penWidth: Float = -1.0
    private var lineCapStyle = CapStyle.BUTT
    private var lineJoinStyle = JoinStyle.MITER
    private var linePattern: String = "[] 0"
    private var font: Font?
    private var savedStates = [State]()
    private var mcid = 0


    ///
    /// Creates page object and add it to the PDF document.
    ///
    /// Please note:
    /// <pre>
    ///   The coordinate (0.0, 0.0) is the top left corner of the page.
    ///   The size of the pages are represented in points.
    ///   1 point is 1/72 inches.
    /// </pre>
    ///
    /// - Parameter pdf the pdf object.
    /// - Parameter pageSize the page size of this page.
    /// - Parameter addPageToPDF Bool flag.
    ///
    public init(
            _ pdf: PDF,
            _ pageSize: [Float],
            _ addPageToPDF: Bool) {
        self.pdf = pdf
        self.annots = [Annotation]()
        self.destinations = [Destination]()
        self.width = pageSize[0]
        self.height = pageSize[1]
        if addPageToPDF {
            pdf.addPage(self)
        }
    }


    public init(_ pdf: inout PDF, _ pageObj: inout PDFobj) {
        self.pdf = pdf
        self.pageObj = pageObj
        self.width = pageObj.getPageSize()[0]
        self.height = pageObj.getPageSize()[1]
        append("q\n")
        if pageObj.gsNumber != -1 {
            append("/GS")
            append(pageObj.gsNumber + 1)
            append(" gs\n")
        }
    }


    ///
    /// Creates page object and add it to the PDF document.
    ///
    /// Please note:
    /// <pre>
    ///   The coordinate (0.0, 0.0) is the top left corner of the page.
    ///   The size of the pages are represented in points.
    ///   1 point is 1/72 inches.
    /// </pre>
    ///
    /// - Parameter pdf the pdf object.
    /// - Parameter pageSize the page size of this page.
    ///
    public convenience init(_ pdf: PDF, _ pageSize: [Float]) {
        self.init(pdf, pageSize, true)
    }


    public func addResource(_ coreFont: CoreFont, _ objects: inout [PDFobj]) -> Font {
        return pageObj!.addResource(coreFont, &objects)
    }


    public func addResource(_ image: Image, _ objects: inout [PDFobj]) {
        pageObj!.addResource(image, &objects)
    }


    public func addResource(_ font: Font, _ objects: inout [PDFobj]) {
        pageObj!.addResource(font, &objects)
    }


    public func complete(_ objects: inout [PDFobj]) {
        append("Q\n")
        pageObj!.addContent(&self.buf, &objects)
    }


    public func getContent() -> [UInt8] {
        return self.buf
    }


    ///
    /// Adds destination to this page.
    ///
    /// - Parameter name The destination name.
    /// - Parameter yPosition The vertical position of the destination on this page.
    ///
    /// - Returns: the destination.
    ///
    @discardableResult
    public func addDestination(
            _ name: String,
            _ yPosition: Float) -> Destination {
        let dest = Destination(name, height - yPosition)
        destinations!.append(dest)
        return dest
    }


    ///
    /// Returns the width of this page.
    ///
    /// - Returns: the width of the page.
    ///
    public func getWidth() -> Float {
        return self.width
    }


    ///
    /// Returns the height of this page.
    ///
    /// - Returns: the height of the page.
    ///
    public func getHeight() -> Float {
        return self.height
    }


    ///
    /// Draws a line on the page, using the current color, between the points (x1, y1) and (x2, y2).
    ///
    /// - Parameter x1 the first point's x coordinate.
    /// - Parameter y1 the first point's y coordinate.
    /// - Parameter x2 the second point's x coordinate.
    /// - Parameter y2 the second point's y coordinate.
    ///
    public func drawLine(
            _ x1: Float,
            _ y1: Float,
            _ x2: Float,
            _ y2: Float) {
        moveTo(x1, y1)
        lineTo(x2, y2)
        strokePath()
    }


    public final func drawString(
            _ font1: Font,
            _ font2: Font?,
            _ str: String?,
            _ xOrig: Float,
            _ yOrig: Float) {
        drawString(font1, font2, str, xOrig, yOrig, nil)
    }

    ///
    /// Draws the text given by the specified string,
    /// using the specified main font and the current brush color.
    /// If the main font is missing some glyphs - the fallback font is used.
    /// The baseline of the leftmost character is at position (x, y) on the page.
    ///
    /// - Parameter font the main font.
    /// - Parameter fallbackFont the fallback font.
    /// - Parameter str the string to be drawn.
    /// - Parameter x the x coordinate.
    /// - Parameter y the y coordinate.
    ///
    public final func drawString(
            _ font: Font,
            _ fallbackFont: Font?,
            _ str: String?,
            _ xOrig: Float,
            _ yOrig: Float,
            _ colors: [String : UInt32]?) {
        var x = xOrig
        let y = yOrig
        if (font.isCoreFont || font.isCJK || fallbackFont == nil || fallbackFont!.isCoreFont || fallbackFont!.isCJK) {
            drawString(font, str, x, y, colors)
        }
        else {
            var activeFont = font
            var buf = String()
            for scalar in str!.unicodeScalars {
                if activeFont.unicodeToGID![Int(scalar.value)] == 0 {
                    drawString(activeFont, buf, x, y, colors)
                    x += activeFont.stringWidth(buf)
                    buf = ""
                    // Switch the font
                    if activeFont === font {
                        activeFont = fallbackFont!
                    }
                    else {
                        activeFont = font
                    }
                }
                buf.append(String(scalar))
            }
            drawString(activeFont, buf, x, y, colors)
        }
    }


    public final func drawString(
            _ font: Font,
            _ text: String?,
            _ x: Float,
            _ y: Float) {
        drawString(font, text, x, y, nil)                
    }


    ///
    /// Draws the text given by the specified string,
    /// using the specified font and the current brush color.
    /// The baseline of the leftmost character is at position (x, y) on the page.
    ///
    /// - Parameter font the font to use.
    /// - Parameter str the string to be drawn.
    /// - Parameter x the x coordinate.
    /// - Parameter y the y coordinate.
    ///
    public final func drawString(
            _ font: Font,
            _ text: String?,
            _ x: Float,
            _ y: Float,
            _ colors: [String : UInt32]?) {

        if text == nil || text! == "" {
            return
        }

        append("BT\n")

        if font.fontID == nil {
            setTextFont(font)
        }
        else {
            append("/")
            append(font.fontID!)
            append(" ")
            append(font.size)
            append(" Tf\n")
        }

        if self.renderingMode != 0 {
            append(renderingMode)
            append(" Tr\n")
        }

        var skew: Float = 0.0
        if font.skew15 &&
                self.tm[0] == 1.0 &&
                self.tm[1] == 0.0 &&
                self.tm[2] == 0.0 &&
                self.tm[3] == 1.0 {
            skew = 0.26
        }

        append(self.tm[0])
        append(" ")
        append(self.tm[1])
        append(" ")
        append(self.tm[2] + skew)
        append(" ")
        append(self.tm[3])
        append(" ")
        append(x)
        append(" ")
        append(self.height - y)
        append(" Tm\n")

        if (colors == nil) {
            append("[<")
            drawString(font, text!)
            append(">] TJ\n")
        }
        else {
            drawColoredString(font, text!, colors!);
        }

        append("ET\n")
    }


    private final func drawString(
            _ font: Font,
            _ text: String) {
        let scalars = Array(text.unicodeScalars)
        if font.isCoreFont {
            drawAsciiString(font, scalars)
        }
        else {
            drawUnicodeString(font, scalars)
        }
    }


    private final func drawAsciiString(_ font: Font, _ scalars: [Unicode.Scalar]) {
        for i in 0..<scalars.count {
            let c1 = scalars[i]
            if c1 < Unicode.Scalar(font.firstChar)! ||
                    c1 > Unicode.Scalar(font.lastChar)! {
                appendTwoHexDigits(0x20, &self.buf)
                return
            }
            appendTwoHexDigits(Int(c1.value), &self.buf)
            if font.isCoreFont && font.kernPairs && i < (scalars.count - 1) {
                var c2 = scalars[i + 1]
                if c2 < Unicode.Scalar(font.firstChar)! ||
                        c2 > Unicode.Scalar(font.lastChar)! {
                    c2 = Unicode.Scalar(32)
                }
                let index = Int(c1.value - 32)
                var j = 2
                while j < font.metrics![index].count {
                    if Unicode.Scalar(Int(font.metrics![index][j])) == c2 {
                        append(">")
                        append(Int(-font.metrics![index][j + 1]))
                        append("<")
                        break
                    }
                    j += 2
                }
            }
        }
    }


    private final func drawUnicodeString(_ font: Font, _ scalars: [Unicode.Scalar]) {
        if font.isCJK {
            for i in 0..<scalars.count {
                let c1 = scalars[i]
                if c1 < Unicode.Scalar(font.firstChar)! ||
                        c1 > Unicode.Scalar(font.lastChar)! {
                    appendFourHexDigits(0x0020, &self.buf)
                }
                else {
                    appendFourHexDigits(Int(c1.value), &self.buf)
                }
            }
        }
        else {
            for i in 0..<scalars.count {
                let c1 = scalars[i]
                if c1 < Unicode.Scalar(font.firstChar)! ||
                        c1 > Unicode.Scalar(font.lastChar)! {
                    appendFourHexDigits(font.unicodeToGID![0x0020], &self.buf)
                }
                else {
                    appendFourHexDigits(font.unicodeToGID![Int(c1.value)], &self.buf)
                }
            }
        }
    }


    ///
    /// Sets the graphics state. Please see Example_31.
    ///
    /// - Parameter gs the graphics state to use.
    ///
    public final func setGraphicsState(_ gs: GraphicsState) {
        var buf = String()
        buf.append("/CA ")
        buf.append(String(gs.getAlphaStroking()))
        buf.append(" ")
        buf.append("/ca ")
        buf.append(String(gs.getAlphaNonStroking()))
        var n = pdf!.states[buf]
        if n == nil {
            n = pdf!.states.count + 1
            pdf!.states[buf] = n
        }
        append("/GS")
        append(n!)
        append(" gs\n")
    }


    ///
    /// Sets the color for stroking operations.
    /// The pen color is used when drawing lines and splines.
    ///
    /// - Parameter r the red component is Float value from 0.0 to 1.0.
    /// - Parameter g the green component is Float value from 0.0 to 1.0.
    /// - Parameter b the blue component is Float value from 0.0 to 1.0.
    ///
    public final func setPenColor(_ r: Float, _ g: Float, _ b: Float) {
        if pen[0] != r || pen[1] != g || pen[2] != b {
            setColor(r, g, b)
            append(" RG\n")
            pen[0] = r
            pen[1] = g
            pen[2] = b
        }
    }


    ///
    /// Sets the color for stroking operations using CMYK.
    /// The pen color is used when drawing lines and splines.
    ///
    /// - Parameter c the cyan component is Float value from 0.0 to 1.0.
    /// - Parameter m the magenta component is Float value from 0.0 to 1.0.
    /// - Parameter y the yellow component is Float value from 0.0 to 1.0.
    /// - Parameter k the black component is Float value from 0.0 to 1.0.
    ///
    public final func setPenColorCMYK(_ c: Float, _ m: Float, _ y: Float, _ k: Float) {
        if penCMYK[0] != c || penCMYK[1] != m || penCMYK[2] != y || penCMYK[3] != k {
            setColorCMYK(c, m, y, k)
            append(" K\n")
            penCMYK[0] = c
            penCMYK[1] = m
            penCMYK[2] = y
            penCMYK[3] = k
        }
    }


    ///
    /// Sets the color for brush operations.
    /// This is the color used when drawing regular text and filling shapes.
    ///
    /// - Parameter r the red component is Float value from 0.0 to 1.0.
    /// - Parameter g the green component is Float value from 0.0 to 1.0.
    /// - Parameter b the blue component is Float value from 0.0 to 1.0.
    ///
    public final func setBrushColor(_ r: Float, _ g: Float, _ b: Float) {
        if brush[0] != r || brush[1] != g || brush[2] != b {
            setColor(r, g, b)
            append(" rg\n")
            brush[0] = r
            brush[1] = g
            brush[2] = b
        }
    }


    ///
    /// Sets the color for brush operations using CMYK.
    /// This is the color used when drawing regular text and filling shapes.
    ///
    /// - Parameter c the cyan component is Float value from 0.0 to 1.0.
    /// - Parameter m the magenta component is Float value from 0.0 to 1.0.
    /// - Parameter y the yellow component is Float value from 0.0 to 1.0.
    /// - Parameter k the black component is Float value from 0.0 to 1.0.
    ///
    public final func setBrushColorCMYK(_ c: Float, _ m: Float, _ y: Float, _ k: Float) {
        if brushCMYK[0] != c || brushCMYK[1] != m || brushCMYK[2] != y || brushCMYK[3] != k {
            setColorCMYK(c, m, y, k)
            append(" k\n")
            brushCMYK[0] = c
            brushCMYK[1] = m
            brushCMYK[2] = y
            brushCMYK[3] = k
        }
    }


    ///
    /// Sets the color for brush operations.
    ///
    /// - Parameter color the color.
    /// @throws IOException
    ///
    public func setBrushColor(_ color: [Float]) {
        setBrushColor(color[0], color[1], color[2])
    }


    ///
    /// Returns the brush color.
    ///
    /// - Returns: the brush color.
    ///
    public func getBrushColor() -> [Float] {
        return brush
    }


    private func setColor(_ r: Float, _ g: Float, _ b: Float) {
        append(r)
        append(" ")
        append(g)
        append(" ")
        append(b)
    }


    private func setColorCMYK(_ c: Float, _ m: Float, _ y: Float, _ k: Float) {
        append(c)
        append(" ")
        append(m)
        append(" ")
        append(y)
        append(" ")
        append(k)
    }


    ///
    /// Sets the pen color.
    ///
    /// - Parameter color the color.
    /// See the Color class for predefined values or define your own using 0x00RRGGBB packed integers.
    /// @throws IOException
    ///
    public func setPenColor(_ color: UInt32) {
        let r = Float((color >> 16) & 0xff)/255.0
        let g = Float((color >>  8) & 0xff)/255.0
        let b = Float((color)       & 0xff)/255.0
        setPenColor(r, g, b)
    }


    ///
    /// Sets the brush color.
    ///
    /// - Parameter color the color.
    /// See the Color class for predefined values or define your own using 0x00RRGGBB packed integers.
    /// @throws IOException
    ///
    public func setBrushColor(_ color: UInt32) {
        let r = Float((color >> 16) & 0xff)/255.0
        let g = Float((color >>  8) & 0xff)/255.0
        let b = Float((color)       & 0xff)/255.0
        setBrushColor(r, g, b)
    }


    ///
    /// Sets the line width to the default.
    /// The default is the finest line width.
    ///
    public func setDefaultLineWidth() {
        if self.penWidth != 0.0 {
            self.penWidth = 0.0
            append(self.penWidth)
            append(" w\n")
        }
    }


    ///
    /// The line dash pattern controls the pattern of dashes and gaps used to stroke paths.
    /// It is specified by a dash array and a dash phase.
    /// The elements of the dash array are positive numbers that specify the lengths of
    /// alternating dashes and gaps.
    /// The dash phase specifies the distance into the dash pattern at which to start the dash.
    /// The elements of both the dash array and the dash phase are expressed in user space units.
    /// <pre>
    ///   Examples of line dash patterns:
    ///
    ///   "[Array] Phase"     Appearance          Description
    ///   _______________     _________________   ____________________________________
    ///
    ///   "[] 0"              -----------------   Solid line
    ///   "[3] 0"             ---   ---   ---     3 units on, 3 units off, ...
    ///   "[2] 1"             -  --  --  --  --   1 on, 2 off, 2 on, 2 off, ...
    ///   "[2 1] 0"           -- -- -- -- -- --   2 on, 1 off, 2 on, 1 off, ...
    ///   "[3 5] 6"             ---     ---       2 off, 3 on, 5 off, 3 on, 5 off, ...
    ///   "[2 3] 11"          -   --   --   --    1 on, 3 off, 2 on, 3 off, 2 on, ...
    /// </pre>
    ///
    /// - Parameter pattern the line dash pattern.
    ///
    public func setLinePattern(_ pattern: String) {
        if pattern != linePattern {
            self.linePattern = pattern
            append(self.linePattern)
            append(" d\n")
        }
    }


    ///
    /// Sets the default line dash pattern - solid line.
    ///
    public func setDefaultLinePattern() {
        append("[] 0")
        append(" d\n")
    }


    ///
    /// Sets the pen width that will be used to draw lines and splines on this page.
    ///
    /// - Parameter width the pen width.
    ///
    public func setPenWidth(_ width: Float) {
        if self.penWidth != width {
            self.penWidth = width
            append(self.penWidth)
            append(" w\n")
        }
    }


    ///
    /// Sets the current line cap style.
    ///
    /// - Parameter style the cap style of the current line.
    /// Supported values: CapStyle.BUTT, CapStyle.ROUND and CapStyle.PROJECTING_SQUARE
    ///
    public func setLineCapStyle(_ style: CapStyle) {
        if self.lineCapStyle != style {
            self.lineCapStyle = style
            append(self.lineCapStyle.rawValue)
            append(" J\n")
        }
    }


    ///
    /// Sets the line join style.
    ///
    /// - Parameter style the line join style code. Supported values: Join.MITER, Join.ROUND and Join.BEVEL
    ///
    public func setLineJoinStyle(_ style: JoinStyle) {
        if self.lineJoinStyle != style {
            self.lineJoinStyle = style
            append(self.lineJoinStyle.rawValue)
            append(" j\n")
        }
    }


    ///
    /// Moves the pen to the point with coordinates (x, y) on the page.
    ///
    /// - Parameter x the x coordinate of new pen position.
    /// - Parameter y the y coordinate of new pen position.
    ///
    public func moveTo(_ x: Float, _ y: Float) {
        append(x)
        append(" ")
        append(height - y)
        append(" m\n")
    }


    ///
    /// Draws a line from the current pen position to the point with coordinates (x, y),
    /// using the current pen width and stroke color.
    /// Make sure you call strokePath(), closePath() or fillPath() after the last call to this method.
    ///
    public func lineTo(_ x: Float, _ y: Float) {
        append(x)
        append(" ")
        append(height - y)
        append(" l\n")
    }


    ///
    /// Draws the path using the current pen color.
    ///
    public func strokePath() {
        append("S\n")
    }


    ///
    /// Closes the path and draws it using the current pen color.
    ///
    public func closePath() {
        append("s\n")
    }


    ///
    /// Closes and fills the path with the current brush color.
    ///
    public func fillPath() {
        append("f\n")
    }


    ///
    /// Draws the outline of the specified rectangle on the page.
    /// The left and right edges of the rectangle are at x and x + w.
    /// The top and bottom edges are at y and y + h.
    /// The rectangle is drawn using the current pen color.
    ///
    /// - Parameter x the x coordinate of the rectangle to be drawn.
    /// - Parameter y the y coordinate of the rectangle to be drawn.
    /// - Parameter w the width of the rectangle to be drawn.
    /// - Parameter h the height of the rectangle to be drawn.
    ///
    public func drawRect(
            _ x: Float,
            _ y: Float,
            _ w: Float,
            _ h: Float) {
        moveTo(x, y)
        lineTo(x + w, y)
        lineTo(x + w, y + h)
        lineTo(x, y + h)
        closePath()
    }


    ///
    /// Fills the specified rectangle on the page.
    /// The left and right edges of the rectangle are at x and x + w.
    /// The top and bottom edges are at y and y + h.
    /// The rectangle is drawn using the current pen color.
    ///
    /// - Parameter x the x coordinate of the rectangle to be drawn.
    /// - Parameter y the y coordinate of the rectangle to be drawn.
    /// - Parameter w the width of the rectangle to be drawn.
    /// - Parameter h the height of the rectangle to be drawn.
    ///
    public func fillRect(
            _ x: Float,
            _ y: Float,
            _ w: Float,
            _ h: Float) {
        moveTo(x, y)
        lineTo(x + w, y)
        lineTo(x + w, y + h)
        lineTo(x, y + h)
        fillPath()
    }


    ///
    /// Draws or fills the specified path using the current pen or brush.
    ///
    /// - Parameter path the path.
    /// - Parameter operation specifies 'stroke' or 'fill' operation.
    ///
    public func drawPath(
            _ path: [Point],
            _ operation: String) {
        if path.count < 2 {
            // Swift.print("The Path object must contain at least 2 points")
        }
        var point = path[0]
        moveTo(point.x, point.y)
        var curve: Bool = false
        for i in 1..<path.count {
            point = path[i]
            if point.isControlPoint {
                curve = true
                append(point)
            }
            else {
                if curve {
                    curve = false
                    append(point)
                    append("c\n")
                }
                else {
                    lineTo(point.x, point.y)
                }
            }
        }

        append(operation)
        append("\n")
    }


    ///
    /// Draws a circle on the page.
    ///
    /// The outline of the circle is drawn using the current pen color.
    ///
    /// - Parameter x the x coordinate of the center of the circle to be drawn.
    /// - Parameter y the y coordinate of the center of the circle to be drawn.
    /// - Parameter r the radius of the circle to be drawn.
    ///
    public func drawCircle(
            _ x: Float,
            _ y: Float,
            _ r: Float) {
        drawEllipse(x, y, r, r, Operation.STROKE)
    }


    ///
    /// Draws the specified circle on the page and fills it with the current brush color.
    ///
    /// - Parameter x the x coordinate of the center of the circle to be drawn.
    /// - Parameter y the y coordinate of the center of the circle to be drawn.
    /// - Parameter r the radius of the circle to be drawn.
    /// - Parameter operation must be Operation.STROKE, Operation.CLOSE or Operation.FILL.
    ///
    public func drawCircle(
            _ x: Float,
            _ y: Float,
            _ r: Float,
            _ operation: String) {
        drawEllipse(x, y, r, r, operation)
    }


    ///
    /// Draws an ellipse on the page using the current pen color.
    ///
    /// - Parameter x the x coordinate of the center of the ellipse to be drawn.
    /// - Parameter y the y coordinate of the center of the ellipse to be drawn.
    /// - Parameter r1 the horizontal radius of the ellipse to be drawn.
    /// - Parameter r2 the vertical radius of the ellipse to be drawn.
    ///
    public func drawEllipse(
            _ x: Float,
            _ y: Float,
            _ r1: Float,
            _ r2: Float) {
        drawEllipse(x, y, r1, r2, Operation.STROKE)
    }


    ///
    /// Fills an ellipse on the page using the current pen color.
    ///
    /// - Parameter x the x coordinate of the center of the ellipse to be drawn.
    /// - Parameter y the y coordinate of the center of the ellipse to be drawn.
    /// - Parameter r1 the horizontal radius of the ellipse to be drawn.
    /// - Parameter r2 the vertical radius of the ellipse to be drawn.
    ///
    public func fillEllipse(
            _ x: Float,
            _ y: Float,
            _ r1: Float,
            _ r2: Float) {
        drawEllipse(x, y, r1, r2, Operation.FILL)
    }


    ///
    /// Draws an ellipse on the page and fills it using the current brush color.
    ///
    /// - Parameter x the x coordinate of the center of the ellipse to be drawn.
    /// - Parameter y the y coordinate of the center of the ellipse to be drawn.
    /// - Parameter r1 the horizontal radius of the ellipse to be drawn.
    /// - Parameter r2 the vertical radius of the ellipse to be drawn.
    /// - Parameter operation the operation.
    ///
    private func drawEllipse(
            _ x: Float,
            _ y: Float,
            _ r1: Float,
            _ r2: Float,
            _ operation: String) {
        // The best 4-spline magic number
        let m4: Float = 0.551784

        // Starting point
        moveTo(x, y - r2)

        appendPointXY(x + m4*r1, y - r2)
        appendPointXY(x + r1, y - m4*r2)
        appendPointXY(x + r1, y)
        append("c\n")

        appendPointXY(x + r1, y + m4*r2)
        appendPointXY(x + m4*r1, y + r2)
        appendPointXY(x, y + r2)
        append("c\n")

        appendPointXY(x - m4*r1, y + r2)
        appendPointXY(x - r1, y + m4*r2)
        appendPointXY(x - r1, y)
        append("c\n")

        appendPointXY(x - r1, y - m4*r2)
        appendPointXY(x - m4*r1, y - r2)
        appendPointXY(x, y - r2)
        append("c\n")

        append(operation)
        append("\n")
    }


    ///
    /// Draws a point on the page using the current pen color.
    ///
    /// - Parameter p the point.
    ///
    public func drawPoint(_ p: Point) {
        if p.shape != Point.INVISIBLE  {
            var list: [Point]
            if p.shape == Point.CIRCLE {
                if p.fillShape {
                    drawCircle(p.x, p.y, p.r, "f")
                }
                else {
                    drawCircle(p.x, p.y, p.r, "S")
                }
            }
            else if p.shape == Point.DIAMOND {
                list = [Point]()
                list.append(Point(p.x, p.y - p.r))
                list.append(Point(p.x + p.r, p.y))
                list.append(Point(p.x, p.y + p.r))
                list.append(Point(p.x - p.r, p.y))
                if p.fillShape {
                    drawPath(list, "f")
                }
                else {
                    drawPath(list, "s")
                }
            }
            else if p.shape == Point.BOX {
                list = [Point]()
                list.append(Point(p.x - p.r, p.y - p.r))
                list.append(Point(p.x + p.r, p.y - p.r))
                list.append(Point(p.x + p.r, p.y + p.r))
                list.append(Point(p.x - p.r, p.y + p.r))
                if p.fillShape {
                    drawPath(list, "f")
                }
                else {
                    drawPath(list, "s")
                }
            }
            else if p.shape == Point.PLUS {
                drawLine(p.x - p.r, p.y, p.x + p.r, p.y)
                drawLine(p.x, p.y - p.r, p.x, p.y + p.r)
            }
            else if p.shape == Point.UP_ARROW {
                list = [Point]()
                list.append(Point(p.x, p.y - p.r))
                list.append(Point(p.x + p.r, p.y + p.r))
                list.append(Point(p.x - p.r, p.y + p.r))
                if p.fillShape {
                    drawPath(list, "f")
                }
                else {
                    drawPath(list, "s")
                }
            }
            else if p.shape == Point.DOWN_ARROW {
                list = [Point]()
                list.append(Point(p.x - p.r, p.y - p.r))
                list.append(Point(p.x + p.r, p.y - p.r))
                list.append(Point(p.x, p.y + p.r))
                if p.fillShape {
                    drawPath(list, "f")
                }
                else {
                    drawPath(list, "s")
                }
            }
            else if p.shape == Point.LEFT_ARROW {
                list = [Point]()
                list.append(Point(p.x + p.r, p.y + p.r))
                list.append(Point(p.x - p.r, p.y))
                list.append(Point(p.x + p.r, p.y - p.r))
                if p.fillShape {
                    drawPath(list, "f")
                }
                else {
                    drawPath(list, "s")
                }
            }
            else if p.shape == Point.RIGHT_ARROW {
                list = [Point]()
                list.append(Point(p.x - p.r, p.y - p.r))
                list.append(Point(p.x + p.r, p.y))
                list.append(Point(p.x - p.r, p.y + p.r))
                if p.fillShape {
                    drawPath(list, "f")
                }
                else {
                    drawPath(list, "s")
                }
            }
            else if p.shape == Point.H_DASH {
                drawLine(p.x - p.r, p.y, p.x + p.r, p.y)
            }
            else if p.shape == Point.V_DASH {
                drawLine(p.x, p.y - p.r, p.x, p.y + p.r)
            }
            else if p.shape == Point.X_MARK {
                drawLine(p.x - p.r, p.y - p.r, p.x + p.r, p.y + p.r)
                drawLine(p.x - p.r, p.y + p.r, p.x + p.r, p.y - p.r)
            }
            else if p.shape == Point.MULTIPLY {
                drawLine(p.x - p.r, p.y - p.r, p.x + p.r, p.y + p.r)
                drawLine(p.x - p.r, p.y + p.r, p.x + p.r, p.y - p.r)
                drawLine(p.x - p.r, p.y, p.x + p.r, p.y)
                drawLine(p.x, p.y - p.r, p.x, p.y + p.r)
            }
            else if p.shape == Point.STAR {
                let angle = Float.pi / 10.0
                let sin18 = Float(sin(angle))
                let cos18 = Float(cos(angle))
                let a = p.r * cos18
                let b = p.r * sin18
                let c = 2 * a * sin18
                let d = 2 * a * cos18 - p.r
                list = [Point]()
                list.append(Point(p.x, p.y - p.r))
                list.append(Point(p.x + c, p.y + d))
                list.append(Point(p.x - a, p.y - b))
                list.append(Point(p.x + a, p.y - b))
                list.append(Point(p.x - c, p.y + d))
                if p.fillShape {
                    drawPath(list, "f")
                }
                else {
                    drawPath(list, "s")
                }
            }
        }
    }


    ///
    /// Sets the text rendering mode.
    ///
    /// - Parameter mode the rendering mode.
    ///
    public func setTextRenderingMode(_ mode: Int) throws {
        if mode >= 0 && mode <= 7 {
            self.renderingMode = mode
        }
        else {
            // TODO:
            Swift.print("Invalid text rendering mode: \(mode)")
        }
    }


    ///
    /// Sets the text direction.
    ///
    /// - Parameter degrees the angle.
    ///
    public func setTextDirection(_ angleInDegrees: Int) {
        var degrees: Int = angleInDegrees
        if degrees > 360 {
            degrees %= 360
        }
        if degrees == 0 {
            self.tm = [ 1.0,  0.0,  0.0,  1.0 ]
        }
        else if degrees == 90 {
            self.tm = [ 0.0,  1.0, -1.0,  0.0 ]
        }
        else if degrees == 180 {
            self.tm = [-1.0,  0.0,  0.0, -1.0 ]
        }
        else if degrees == 270 {
            self.tm = [ 0.0, -1.0,  1.0,  0.0 ]
        }
        else if degrees == 360 {
            self.tm = [ 1.0,  0.0,  0.0,  1.0 ]
        }
        else {
            let sinOfAngle = Float(sin(Float(degrees) * (Float.pi / 180.0)))
            let cosOfAngle = Float(cos(Float(degrees) * (Float.pi / 180.0)))
            self.tm = [cosOfAngle, sinOfAngle, -sinOfAngle, cosOfAngle]
        }
    }


    ///
    /// Draws a bezier curve starting from the current point.
    /// <strong>Please note:</strong> You must call the fillPath,
    /// closePath or strokePath method after the last bezierCurveTo call.
    /// <p><i>Author:</i> <strong>Pieter Libin</strong>, pieter@emweb.be</p>
    ///
    /// - Parameter p1 first control point
    /// - Parameter p2 second control point
    /// - Parameter p3 end point
    ///
    public func bezierCurveTo(
            _ p1: Point,
            _ p2: Point,
            _ p3: Point) {
        append(p1)
        append(p2)
        append(p3)
        append("c\n")
    }


    ///
    /// Sets the start of text block.
    /// Please see Example_32. This method must have matching call to setTextEnd().
    ///
    public func setTextStart() {
        append("BT\n")
    }


    ///
    /// Sets the text location.
    /// Please see Example_32.
    ///
    /// - Parameter x the x coordinate of new text location.
    /// - Parameter y the y coordinate of new text location.
    ///
    public func setTextLocation(_ x: Float, _ y: Float) {
        append(x)
        append(" ")
        append(height - y)
        append(" Td\n")
    }


    public func setTextBegin(_ x: Float, _ y: Float) {
        append("BT\n")
        append(x)
        append(" ")
        append(height - y)
        append(" Td\n")
    }


    ///
    /// Sets the text leading.
    /// Please see Example_32.
    ///
    /// - Parameter leading the leading.
    ///
    public func setTextLeading(_ leading: Float) {
        append(leading)
        append(" TL\n")
    }


    public func setCharSpacing(_ spacing: Float) {
        append(spacing)
        append(" Tc\n")
    }


    public func setWordSpacing(_ spacing: Float) {
        append(spacing)
        append(" Tw\n")
    }


    public func setTextScaling(_ scaling: Float) {
        append(scaling)
        append(" Tz\n")
    }


    public func setTextRise(_ rise: Float) {
        append(rise)
        append(" Ts\n")
    }


    public func setTextFont(_ font: Font) {
        self.font = font
        append("/F")
        append(font.objNumber)
        append(" ")
        append(font.size)
        append(" Tf\n")
    }


    ///
    /// Prints a line of text and moves to the next line.
    /// Please see Example_32.
    ///
    // public func println(_ str: String) {
    //     printString(str)
    //     newLine()
    // }


    ///
    /// Prints a line of text.
    /// Please see Example_32.
    ///
    public func printString(_ str: String) {
        if self.font != nil {
            append("[<")
            drawString(self.font!, str)
            append(">] TJ\n")
        }
    }


    ///
    /// Move to the next line.
    /// Please see Example_32.
    ///
    public func newLine() {
        append("T*\n")
    }


    ///
    /// Sets the end of text block.
    /// Please see Example_32.
    ///
    public func setTextEnd() {
        append("ET\n")
    }


    // Original code provided by:
    // Dominique Andre Gunia <contact@dgunia.de>
    // <<
    public func drawRectRoundCorners(
            _ x: Float,
            _ y: Float,
            _ w: Float,
            _ h: Float,
            _ r1: Float,
            _ r2: Float,
            _ operation: String) {

        // The best 4-spline magic number
        let m4: Float = 0.551784

        var list = [Point]()

        // Starting point
        list.append(Point(x + w - r1, y))
        list.append(Point(x + w - r1 + m4*r1, y, Point.CONTROL_POINT))
        list.append(Point(x + w, y + r2 - m4*r2, Point.CONTROL_POINT))
        list.append(Point(x + w, y + r2))

        list.append(Point(x + w, y + h - r2))
        list.append(Point(x + w, y + h - r2 + m4*r2, Point.CONTROL_POINT))
        list.append(Point(x + w - m4*r1, y + h, Point.CONTROL_POINT))
        list.append(Point(x + w - r1, y + h))

        list.append(Point(x + r1, y + h))
        list.append(Point(x + r1 - m4*r1, y + h, Point.CONTROL_POINT))
        list.append(Point(x, y + h - m4*r2, Point.CONTROL_POINT))
        list.append(Point(x, y + h - r2))

        list.append(Point(x, y + r2))
        list.append(Point(x, y + r2 - m4*r2, Point.CONTROL_POINT))
        list.append(Point(x + m4*r1, y, Point.CONTROL_POINT))
        list.append(Point(x + r1, y))
        list.append(Point(x + w - r1, y))

        drawPath(list, operation)
    }


    ///
    /// Clips the path.
    ///
    public func clipPath() {
        append("W\n")
        append("n\n")   // Close the path without painting it.
    }


    public func clipRect(
            _ x: Float,
            _ y: Float,
            _ w: Float,
            _ h: Float) {
        moveTo(x, y)
        lineTo(x + w, y)
        lineTo(x + w, y + h)
        lineTo(x, y + h)
        clipPath()
    }


    public func save() {
        append("q\n")
        savedStates.append(State(
                self.pen,
                self.brush,
                self.penWidth,
                self.lineCapStyle,
                self.lineJoinStyle,
                self.linePattern))
    }


    public func restore() {
        append("Q\n")
        if savedStates.count > 0 {
            let savedState = savedStates.remove(at: savedStates.count - 1)
            self.pen = savedState.getPen()
            self.brush = savedState.getBrush()
            self.penWidth = savedState.getPenWidth()
            self.lineCapStyle = savedState.getLineCapStyle()
            self.lineJoinStyle = savedState.getLineJoinStyle()
            self.linePattern = savedState.getLinePattern()
        }
    }
    // <<


    ///
    /// Sets the page CropBox.
    /// See page 77 of the PDF32000_2008.pdf specification.
    ///
    /// - Parameter upperLeftX the top left X coordinate of the CropBox.
    /// - Parameter upperLeftY the top left Y coordinate of the CropBox.
    /// - Parameter lowerRightX the bottom right X coordinate of the CropBox.
    /// - Parameter lowerRightY the bottom right Y coordinate of the CropBox.
    ///
    public func setCropBox(
            _ upperLeftX: Float,
            _ upperLeftY: Float,
            _ lowerRightX: Float,
            _ lowerRightY: Float) {
        self.cropBox = [upperLeftX, upperLeftY, lowerRightX, lowerRightY]
    }


    ///
    /// Sets the page BleedBox.
    /// See page 77 of the PDF32000_2008.pdf specification.
    ///
    /// - Parameter upperLeftX the top left X coordinate of the BleedBox.
    /// - Parameter upperLeftY the top left Y coordinate of the BleedBox.
    /// - Parameter lowerRightX the bottom right X coordinate of the BleedBox.
    /// - Parameter lowerRightY the bottom right Y coordinate of the BleedBox.
    ///
    public func setBleedBox(
            _ upperLeftX: Float,
            _ upperLeftY: Float,
            _ lowerRightX: Float,
            _ lowerRightY: Float) {
        self.bleedBox = [upperLeftX, upperLeftY, lowerRightX, lowerRightY]
    }


    ///
    /// Sets the page TrimBox.
    /// See page 77 of the PDF32000_2008.pdf specification.
    ///
    /// - Parameter upperLeftX the top left X coordinate of the TrimBox.
    /// - Parameter upperLeftY the top left Y coordinate of the TrimBox.
    /// - Parameter lowerRightX the bottom right X coordinate of the TrimBox.
    /// - Parameter lowerRightY the bottom right Y coordinate of the TrimBox.
    ///
    public func setTrimBox(
            _ upperLeftX: Float,
            _ upperLeftY: Float,
            _ lowerRightX: Float,
            _ lowerRightY: Float) {
        self.trimBox = [upperLeftX, upperLeftY, lowerRightX, lowerRightY]
    }


    ///
    /// Sets the page ArtBox.
    /// See page 77 of the PDF32000_2008.pdf specification.
    ///
    /// - Parameter upperLeftX the top left X coordinate of the ArtBox.
    /// - Parameter upperLeftY the top left Y coordinate of the ArtBox.
    /// - Parameter lowerRightX the bottom right X coordinate of the ArtBox.
    /// - Parameter lowerRightY the bottom right Y coordinate of the ArtBox.
    ///
    public func setArtBox(
            _ upperLeftX: Float,
            _ upperLeftY: Float,
            _ lowerRightX: Float,
            _ lowerRightY: Float) {
        self.artBox = [upperLeftX, upperLeftY, lowerRightX, lowerRightY]
    }


    private func appendPointXY(_ x: Float, _ y: Float) {
        append(x)
        append(" ")
        append(height - y)
        append(" ")
    }


    private func append(_ point: Point) {
        append(point.x)
        append(" ")
        append(height - point.y)
        append(" ")
    }


    func append(_ str: String) {
        self.buf.append(contentsOf: Array(str.utf8))
    }


    func append(_ num: UInt32) {
        append(String(num))
    }


    func append(_ num: Int) {
        append(String(num))
    }


    func append(_ val: Float) {
        append(String(val))
    }


    func append(_ byte: UInt8) {
        self.buf.append(byte)
    }


    public func append(_ buffer: [UInt8]) {
        self.buf.append(contentsOf: buffer)
    }


    private func drawWord(
            _ font: Font,
            _ str: inout String,
            _ colors: [String : UInt32]) {
        if str != "" {
            if colors[str] != nil {
                setBrushColor(colors[str]!)
            }
            else {
                setBrushColor(Color.black)
            }
            append("[<");
            drawString(font, str);
            append(">] TJ\n");
            str = ""
        }
    }


    func drawColoredString(
            _ font: Font,
            _ text: String,
            _ colors: [String : UInt32]) {
        var buf1 = String()
        var buf2 = String()
        for scalar in text.unicodeScalars {
            if isLetterOrDigit(scalar) {
                drawWord(font, &buf2, colors)
                buf1.append(String(scalar))
            }
            else {
                drawWord(font, &buf1, colors)
                buf2.append(String(scalar))
            }
        }
        drawWord(font, &buf1, colors)
        drawWord(font, &buf2, colors)
    }


    func setStructElementsPageObjNumber(
            _ pageObjNumber: Int) {
        for element in structures {
            element.pageObjNumber = pageObjNumber
        }
    }


    public func addBMC(
            _ structure: String,
            _ actualText: String,
            _ altDescription: String) {
        addBMC(structure, nil, actualText, altDescription)
    }


    public func addBMC(
            _ structure: String,
            _ language: String?,
            _ actualText: String,
            _ altDescription: String) {

        if pdf != nil && pdf!.compliance == Compliance.PDF_UA {
            let element = StructElem()
            element.structure = structure
            element.mcid = mcid
            element.language = language
            element.actualText = actualText
            element.altDescription = altDescription
            structures.append(element)

            append("/")
            append(structure)
            append(" <</MCID ")
            append(mcid)
            append(">>\n")
            append("BDC\n")
            mcid += 1
        }
    }


    public func addEMC() {
        if pdf != nil && pdf!.compliance == Compliance.PDF_UA {
            append("EMC\n")
        }
    }


    func addAnnotation(_ annotation: Annotation) {
        annotation.y1 = self.height - annotation.y1
        annotation.y2 = self.height - annotation.y2
        self.annots!.append(annotation)
        if pdf != nil && pdf!.compliance == Compliance.PDF_UA {
            let element = StructElem()
            element.structure = StructElem.LINK
            element.language = annotation.language
            element.actualText = annotation.actualText
            element.altDescription = annotation.altDescription
            element.annotation = annotation
            self.structures.append(element)
        }
    }


    func beginTransform(
            _ x: Float,
            _ y: Float,
            _ xScale: Float,
            _ yScale: Float) {
        append("q\n")

        append(xScale)
        append(" 0 0 ")
        append(yScale)
        append(" ")
        append(x)
        append(" ")
        append(y)
        append(" cm\n")

        append(xScale)
        append(" 0 0 ")
        append(yScale)
        append(" ")
        append(x)
        append(" ")
        append(y)
        append(" Tm\n")
    }


    func endTransform() {
        append("Q\n")
    }


    public func drawContents(
            _ content: [UInt8],
            _ h: Float,     // The height of the graphics object in points.
            _ x: Float,
            _ y: Float,
            _ xScale: Float,
            _ yScale: Float) {
        beginTransform(x, (self.height - yScale * h) - y, xScale, yScale)
        append(content)
        endTransform()
    }


    public func drawString(
            _ font: Font,
            _ str: String,
            _ x: Float,
            _ y: Float,
            _ dx: Float) {
        let scalars = Array(str.unicodeScalars)
        var x1 = x
        for scalar in scalars {
            drawString(font, String(scalar), x1, y)
            x1 += dx
        }
    }


    private func isLetterOrDigit(_ scalar: UnicodeScalar) -> Bool {
        if (scalar.value >= 65 && scalar.value <= 90) ||
            (scalar.value >= 97 && scalar.value <= 122) ||
            (scalar.value >= 48 && scalar.value <= 57) {
            return true
        }
        return false
    }


    private func isLetterOrDigit(_ value: Int) -> Bool {
        if (value >= 65 && value <= 90) ||
            (value >= 97 && value <= 122) ||
            (value >= 48 && value <= 57) {
            return true
        }
        return false
    }


    func appendTwoHexDigits(_ number: Int, _ buffer: inout [UInt8]) {
        let index = 2 * (number & 0xFF)
        buffer.append(Hexadecimal.instance.digits[index])
        buffer.append(Hexadecimal.instance.digits[index + 1])
    }


    func appendFourHexDigits(_ number: Int, _ buffer: inout [UInt8]) {
        var index = 2 * ((number >> 8) & 0xFF)
        buffer.append(Hexadecimal.instance.digits[index])
        buffer.append(Hexadecimal.instance.digits[index + 1])
        index = 2 * (number & 0xFF)
        buffer.append(Hexadecimal.instance.digits[index])
        buffer.append(Hexadecimal.instance.digits[index + 1])
    }


    public func addWatermark(
            _ font: Font,
            _ text: String) throws {
        let hypotenuse: Float =
                sqrt(self.height * self.height + self.width * self.width)
        let stringWidth = font.stringWidth(text)
        let offset = (hypotenuse - stringWidth) / 2.0
        let angle = atan(self.height / self.width)
        let watermark = TextLine(font)
        watermark.setColor(Color.lightgrey)
        watermark.setText(text)
        watermark.setLocation(
                Float(offset * cos(angle)),
                (self.height - Float(offset * sin(angle))))
        watermark.setTextDirection(Int((angle * (180.0 / Float.pi))))
        watermark.drawOn(self)
    }


    public func invertYAxis() {
        append("1 0 0 -1 0 ")
        append(self.height)
        append(" cm\n")
    }


    @discardableResult
    public func addHeader(_ textLine: TextLine) throws -> [Float] {
        return try addHeader(textLine, 1.5*textLine.font!.ascent)
    }


    @discardableResult
    public func addHeader(_ textLine: TextLine, _ offset: Float) throws -> [Float] {
        textLine.setLocation((getWidth() - textLine.getWidth())/2, offset)
        var xy = textLine.drawOn(self)
        xy[1] += font!.descent
        return xy
    }


    @discardableResult
    public func addFooter(_ textLine: TextLine) throws -> [Float] {
        return try addFooter(textLine, textLine.font!.ascent);
    }


    @discardableResult
    public func addFooter(_ textLine: TextLine, _ offset: Float) throws -> [Float] {
        textLine.setLocation((getWidth() - textLine.getWidth())/2, getHeight() - offset);
        return textLine.drawOn(self);
    }

}   // End of Page.swift
