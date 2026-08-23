/**
 * Page.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
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
    public static let DETACHED = false

    internal var pdf: PDF
    internal var pageObj: PDFobj?
    internal var objNumber = 0
    internal var renderingMode = 0
    internal var width: Float = 0.0
    internal var height: Float = 0.0
    internal var contents = [Int]()
    internal var annots: [Annotation] = []
    internal var destinations: [Destination] = []
    internal var structures = [StructElem]()
    internal var buf = [UInt8]()

    internal var cropBox: [Float]?
    internal var bleedBox: [Float]?
    internal var trimBox: [Float]?
    internal var artBox: [Float]?

    internal var tmx: [Float] = [1.0, 0.0, 0.0, 1.0]
    internal var tm0: [UInt8]
    internal var tm1: [UInt8]
    internal var tm2: [UInt8]
    internal var tm3: [UInt8]

    private var penColor: [Float] = [0.0, 0.0, 0.0]
    private var brushColor: [Float] = [0.0, 0.0, 0.0]

    private var penWidth: Float = 0.5

    private var lineCapStyle = CapStyle.BUTT
    private var lineJoinStyle = JoinStyle.MITER
    private var linePattern: String = "[] 0"
    private var font: Font?
    private var mcid = 0
    private let hexadecimal = Hexadecimal()

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
        self.tm0 = FastFloat.toByteArray(tmx[0])
        self.tm1 = FastFloat.toByteArray(tmx[1])
        self.tm2 = FastFloat.toByteArray(tmx[2])
        self.tm3 = FastFloat.toByteArray(tmx[3])
        if addPageToPDF {
            pdf.addPage(self)
        }
    }

    public init(_ pdf: PDF, _ pageObj: PDFobj) {
        self.pdf = pdf
        self.pageObj = pageObj
        self.width = pageObj.getPageSize()[0]
        self.height = pageObj.getPageSize()[1]
        self.tm0 = FastFloat.toByteArray(tmx[0])
        self.tm1 = FastFloat.toByteArray(tmx[1])
        self.tm2 = FastFloat.toByteArray(tmx[2])
        self.tm3 = FastFloat.toByteArray(tmx[3])
        self.pageObj = removeComments(self.pageObj!)
        append("q\n")
        if pageObj.gsNumber != -1 {
            append("/GS")
            append(pageObj.gsNumber + 1)
            append(" gs\n")
        }
    }

    private func removeComments(_ obj: PDFobj) -> PDFobj {
        var list = [String]()
        var comment: Bool = false
        for token in obj.dict {
            if token == "%" {
                comment = true
            } else {
                if token.hasPrefix("/") {
                    comment = false
                    list.append(token)
                } else {
                    if !comment {
                        list.append(token)
                    }
                }
            }
        }
        obj.dict = list
        return obj
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

    public func addResource(_ coreFont: Int, _ objects: inout [PDFobj]) -> Font {
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
    /// - Parameter xPosition The horizontal position of the destination on this page.
    /// - Parameter yPosition The vertical position of the destination on this page.
    ///
    /// - Returns: the destination.
    ///
    @discardableResult
    public func addDestination(
            _ name: String,
            _ xPosition: Float,
            _ yPosition: Float) -> Destination {
        let dest = Destination(name, xPosition, height - yPosition)
        destinations.append(dest)
        return dest
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
        destinations.append(dest)
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
            _ font: Font,
            _ fallbackFont: Font?,
            _ fontSize: Float,
            _ str: String?,
            _ xOrig: Float,
            _ yOrig: Float) {
        drawString(font, fallbackFont, fontSize, str, xOrig, yOrig, [0.0, 0.0, 0.0], nil)
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
            _ fontSize: Float,
            _ str: String?,
            _ xOrig: Float,
            _ yOrig: Float,
            _ textColor: [Float],
            _ highlightColors: [String : Int32]?) {
        var x = xOrig
        let y = yOrig
        if (font.isCoreFont ||
                font.isCJK ||
                fallbackFont == nil ||
                fallbackFont!.isCoreFont ||
                fallbackFont!.isCJK) {
            drawString(font, fontSize, str, x, y, textColor, highlightColors)
        } else {
            var activeFont = font
            var buf = String()
            for scalar in str!.unicodeScalars {
                if activeFont.unicodeToGID[Int(scalar.value)] == 0 {
                    drawString(activeFont, fontSize, buf, x, y, textColor, highlightColors)
                    x += activeFont.stringWidth(buf)
                    buf = ""
                    // Switch the font
                    if activeFont === font {
                        activeFont = fallbackFont!
                    } else {
                        activeFont = font
                    }
                }
                buf.append(String(scalar))
            }
            drawString(activeFont, fontSize, buf, x, y, textColor, highlightColors)
        }
    }

    public final func drawString(
            _ font: Font,
            _ text: String?,
            _ x: Float,
            _ y: Float) {
        drawString(font, font.size, text, x, y, [0.0, 0.0, 0.0], nil)
    }

    public final func drawString(
            _ font: Font,
            _ fontSize: Float,
            _ text: String?,
            _ x: Float,
            _ y: Float) {
        drawString(font, fontSize, text, x, y, [0.0, 0.0, 0.0], nil)
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
            _ fontSize: Float,
            _ text: String?,
            _ x: Float,
            _ y: Float,
            _ textColor: [Float],
            _ highlightColors: [String : Int32]?) {
        if text == nil || text! == "" {
            return
        }

        append(Token.beginText)
        setTextFont(font, fontSize)
        if self.renderingMode != 0 {
            append(renderingMode)
            append(" Tr\n")
        }

        if font.skew15 &&
                self.tmx[0] == 1.0 &&
                self.tmx[1] == 0.0 &&
                self.tmx[2] == 0.0 &&
                self.tmx[3] == 1.0 {
            let skew = Float(0.26)
            append(self.tmx[0])
            append(Token.space)
            append(self.tmx[1])
            append(Token.space)
            append(self.tmx[2] + skew)
            append(Token.space)
            append(self.tmx[3])
        } else {
            append(self.tm0)
            append(Token.space)
            append(self.tm1)
            append(Token.space)
            append(self.tm2)
            append(Token.space)
            append(self.tm3)
        }
        append(Token.space)
        append(x)
        append(Token.space)
        append(self.height - y)
        append(" Tm\n")

        if highlightColors == nil {
            setBrushColor(textColor)
            if font.isCoreFont {
                append("[<")
                drawASCIIString(font, text!)
                append(">] TJ\n")
            } else {
                append("<")
                drawUnicodeString(font, text!)
                append("> Tj\n")
            }
        } else {
            drawColoredString(font, text!, textColor, highlightColors!)
        }
        append(Token.endText)
    }

    private final func drawASCIIString(_ font: Font, _ text: String) {
        let scalars = Array(text.unicodeScalars)
        for i in 0..<scalars.count {
            let c1 = scalars[i]
            if c1 < Unicode.Scalar(font.firstChar)! ||
                    c1 > Unicode.Scalar(font.lastChar)! {
                appendTwoHexDigits(0x20, &self.buf)
                continue
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

    private final func drawUnicodeString(_ font: Font, _ text: String) {
        let scalars = Array(text.unicodeScalars)
        if font.isCJK {
            for scalar in scalars {
                if scalar.value != 0xFEFF {     // BOM
                    if scalar < Unicode.Scalar(font.firstChar)! ||
                            scalar > Unicode.Scalar(font.lastChar)! {
                        Page.appendCodePointAsHex(0x0020, &self.buf)
                    } else {
                        Page.appendCodePointAsHex(Int(scalar.value), &self.buf)
                    }
                }
            }
        } else {
            for scalar in scalars {
                if scalar.value != 0xFEFF {     // BOM
                    if scalar < Unicode.Scalar(font.firstChar)! ||
                            scalar > Unicode.Scalar(font.lastChar)! {
                        Page.appendCodePointAsHex(font.unicodeToGID[0x0020], &self.buf)
                    } else {
                        Page.appendCodePointAsHex(font.unicodeToGID[Int(scalar.value)], &self.buf)
                    }
                }
            }
        }
    }

    public func saveGraphicsState() {
        append("q\n")
    }

    ///
    /// Sets the graphics state. Please see Example_31.
    ///
    /// - Parameter gs the graphics state to use.
    ///
    public final func setGraphicsState(_ gs: GraphicsState) {
        var sb = String()
        sb.append("/CA ")
        sb.append(String(gs.getAlphaStroking()))
        sb.append(" ")
        sb.append("/ca ")
        sb.append(String(gs.getAlphaNonStroking()))
        var n = pdf.states[sb]
        if n == nil {
            n = pdf.states.count + 1
            pdf.states[sb] = n
        }
        append("/GS")
        append(n!)
        append(" gs\n")
    }

    public func restoreGraphicsState() {
        append("Q\n")
    }

    // setPenColor sets the pen color using a 24-bit RGB color integer.
    // - The integer color is expected in the format 0xRRGGBB,
    //   where R, G, and B are the red, green, and blue components respectively.
    // - The method converts the integer color to normalized float values
    //   between 0 and 1 for each RGB component and appends the color
    //   to the drawing context.
    public func setPenColor(_ color: Int32) {
        let r = Float((color >> 16) & 0xff)/255.0
        let g = Float((color >>  8) & 0xff)/255.0
        let b = Float((color)       & 0xff)/255.0
        append(r)
        append(Token.space)
        append(g)
        append(Token.space)
        append(b)
        append(" RG\n")
    }

    // setPenColor sets the pen color using an RGB color array.
    // Each element in the array represents the red, green, and blue components
    // of the color as floating-point values between 0.0 and 1.0.
    //
    // Parameters:
    //   rgbColor: An optional array of 3 Float values representing the
    //   red, green, and blue color components respectively. Each value should
    //   be between 0.0 (no intensity) and 1.0 (full intensity). If the value is
    //   nil, a warning is printed and the method exits early without modifying the color.
    //
    // Notes:
    //   - The method performs a range check to ensure that each color component
    //     is within the valid range [0.0, 1.0]. If any component is out of range,
    //     the method prints a warning and exits early without modifying the color.
    //   - The method then sets the penColor property and appends the color values
    //     to the output stream (e.g., for a PDF or graphics context).
    func setPenColor(_ rgbColor: [Float]?) {
        if rgbColor == nil {
            print("Warning: RGB color is null. Ignoring request.")
            return // Early exit if null
        }

        if rgbColor![0] < 0.0 || rgbColor![0] > 1.0 ||
           rgbColor![1] < 0.0 || rgbColor![1] > 1.0 ||
           rgbColor![2] < 0.0 || rgbColor![2] > 1.0 {
            print("Warning: RGB color values must be between 0f and 1f. Ignoring request.")
            return // Early exit if out of range
        }

        // Now set the pen color
        penColor = rgbColor!

        // Proceed with setting the color (example)
        append(rgbColor![0])
        append(Token.space)
        append(rgbColor![1])
        append(Token.space)
        append(rgbColor![2])
        append(" RG\n")
    }

    // getPenColor retrieves the current pen color as an array of float values.
    // - The returned array contains the normalized RGB values (in the range 0.0 to 1.0)
    //   representing the current pen color.
    // - The array format is [r, g, b], where r, g, and b are the red, green, and blue
    //   components of the pen color respectively.
    public final func getPenColor() -> [Float] {
        return penColor
    }

    // setBrushColor sets the brush color using a 24-bit RGB color integer.
    // - The integer color is expected in the format 0xRRGGBB,
    //   where R, G, and B are the red, green, and blue components respectively.
    // - The method converts the integer color to normalized float values
    //   between 0 and 1 for each RGB component and appends the color
    //   to the drawing context for brush-related operations.
    public func setBrushColor(_ color: Int32) {
        let r = Float((color >> 16) & 0xff)/255.0
        let g = Float((color >>  8) & 0xff)/255.0
        let b = Float((color)       & 0xff)/255.0
        append(r)
        append(Token.space)
        append(g)
        append(Token.space)
        append(b)
        append(" rg\n")
    }

    // setBrushColor sets the brush color using an RGB color array.
    // Each element in the array represents the red, green, and blue components
    // of the color as floating-point values between 0.0 and 1.0.
    //
    // Parameters:
    //   rgbColor: An optional array of 3 Float values representing the
    //   red, green, and blue color components respectively. Each value should
    //   be between 0.0 (no intensity) and 1.0 (full intensity). If the value is
    //   nil, a warning is printed and the method exits early without modifying the color.
    //
    // Notes:
    //   - The method performs a range check to ensure that each color component
    //     is within the valid range [0.0, 1.0]. If any component is out of range,
    //     the method prints a warning and exits early without modifying the color.
    //   - The method then sets the brushColor property and appends the color values
    //     to the output stream (e.g., for a PDF or graphics context).
    func setBrushColor(_ rgbColor: [Float]?) {
        if rgbColor == nil {
            print("Warning: RGB color is null. Ignoring request.")
            return // Early exit if null
        }

        if rgbColor![0] < 0.0 || rgbColor![0] > 1.0 ||
           rgbColor![1] < 0.0 || rgbColor![1] > 1.0 ||
           rgbColor![2] < 0.0 || rgbColor![2] > 1.0 {
            print("Warning: RGB color values must be between 0f and 1f. Ignoring request.")
            return // Early exit if out of range
        }

        // Now set the brush color
        brushColor = rgbColor!

        // Proceed with setting the color (example)
        append(rgbColor![0])
        append(Token.space)
        append(rgbColor![1])
        append(Token.space)
        append(rgbColor![2])
        append(" rg\n")
    }

    // getBrushColor retrieves the current brush color as an array of float values.
    // - The returned array contains the normalized RGB values (in the range 0.0 to 1.0)
    //   representing the current brush color.
    // - The array format is [r, g, b], where r, g, and b are the red, green, and blue
    //   components of the brush color respectively.
    public func getBrushColor() -> [Float] {
        return brushColor
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
        append(c)
        append(Token.space)
        append(m)
        append(Token.space)
        append(y)
        append(Token.space)
        append(k)
        append(" K\n")
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
        append(c)
        append(Token.space)
        append(m)
        append(Token.space)
        append(y)
        append(Token.space)
        append(k)
        append(" k\n")
    }

    ///
    /// Sets the line width to the default.
    /// The default is the finest line width.
    ///
    public func setDefaultLineWidth() {
        append("0 w\n")
    }

    ///
    /// The stroke dash pattern controls the pattern of dashes and gaps used to stroke paths.
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
    public func setStrokeDashPattern(_ pattern: String) {
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
        if self.linePattern != "[] 0" {
            self.linePattern = "[] 0"
            append(self.linePattern)
            append(" d\n")
        }
    }

    ///
    /// Sets the pen width that will be used to draw lines and splines on this page.
    ///
    /// - Parameter width the pen width.
    ///
    public func setPenWidth(_ width: Float) {
        self.penWidth = width
        append(width)
        append(" w\n")
    }

    public func getPenWidth() -> Float {
        return self.penWidth
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
        append(Token.space)
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
        append(Token.space)
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
            _ pathOperator: PathOperator) {
        if path.count < 2 {
            fatalError("The Path object must contain at least 2 points")
        }
        var point = path[0]
        moveTo(point.x, point.y)
        var controlPoint: String = ""
        for i in 1..<path.count {
            point = path[i]
            if point.controlPoint != "" {
    			controlPoint = point.controlPoint
                append(point)
            } else {
                if controlPoint != "" {
    				append(point)
	    			append(controlPoint)
    				append("\n")
	    			controlPoint = ""
                } else {
                    lineTo(point.x, point.y)
                }
            }
        }
        append(pathOperator.rawValue)
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
        drawEllipse(x, y, r, r, PathOperator.stroke)
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
            _ pathOperator: PathOperator) {
        drawEllipse(x, y, r, r, pathOperator)
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
        drawEllipse(x, y, r1, r2, PathOperator.stroke)
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
        drawEllipse(x, y, r1, r2, PathOperator.fill)
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
            _ pathOperator: PathOperator) {
        // The best 4-spline magic number
        let m4: Float = 0.55228

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

        append(pathOperator.rawValue)
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
                    drawCircle(p.x, p.y, p.r, PathOperator.fill)
                } else {
                    drawCircle(p.x, p.y, p.r, PathOperator.stroke)
                }
            } else if p.shape == Point.DIAMOND {
                list = [Point]()
                list.append(Point(p.x, p.y - p.r))
                list.append(Point(p.x + p.r, p.y))
                list.append(Point(p.x, p.y + p.r))
                list.append(Point(p.x - p.r, p.y))
                if p.fillShape {
                    drawPath(list, PathOperator.fill)
                } else {
                    drawPath(list, PathOperator.closeAndStroke)
                }
            } else if p.shape == Point.BOX {
                list = [Point]()
                list.append(Point(p.x - p.r, p.y - p.r))
                list.append(Point(p.x + p.r, p.y - p.r))
                list.append(Point(p.x + p.r, p.y + p.r))
                list.append(Point(p.x - p.r, p.y + p.r))
                if p.fillShape {
                    drawPath(list, PathOperator.fill)
                } else {
                    drawPath(list, PathOperator.closeAndStroke)
                }
            } else if p.shape == Point.PLUS {
                drawLine(p.x - p.r, p.y, p.x + p.r, p.y)
                drawLine(p.x, p.y - p.r, p.x, p.y + p.r)
            } else if p.shape == Point.UP_ARROW {
                list = [Point]()
                list.append(Point(p.x, p.y - p.r))
                list.append(Point(p.x + p.r, p.y + p.r))
                list.append(Point(p.x - p.r, p.y + p.r))
                if p.fillShape {
                    drawPath(list, PathOperator.fill)
                } else {
                    drawPath(list, PathOperator.closeAndStroke)
                }
            } else if p.shape == Point.DOWN_ARROW {
                list = [Point]()
                list.append(Point(p.x - p.r, p.y - p.r))
                list.append(Point(p.x + p.r, p.y - p.r))
                list.append(Point(p.x, p.y + p.r))
                if p.fillShape {
                    drawPath(list, PathOperator.fill)
                } else {
                    drawPath(list, PathOperator.closeAndStroke)
                }
            } else if p.shape == Point.LEFT_ARROW {
                list = [Point]()
                list.append(Point(p.x + p.r, p.y + p.r))
                list.append(Point(p.x - p.r, p.y))
                list.append(Point(p.x + p.r, p.y - p.r))
                if p.fillShape {
                    drawPath(list, PathOperator.fill)
                } else {
                    drawPath(list, PathOperator.closeAndStroke)
                }
            } else if p.shape == Point.RIGHT_ARROW {
                list = [Point]()
                list.append(Point(p.x - p.r, p.y - p.r))
                list.append(Point(p.x + p.r, p.y))
                list.append(Point(p.x - p.r, p.y + p.r))
                if p.fillShape {
                    drawPath(list, PathOperator.fill)
                } else {
                    drawPath(list, PathOperator.closeAndStroke)
                }
            } else if p.shape == Point.H_DASH {
                drawLine(p.x - p.r, p.y, p.x + p.r, p.y)
            } else if p.shape == Point.V_DASH {
                drawLine(p.x, p.y - p.r, p.x, p.y + p.r)
            } else if p.shape == Point.X_MARK {
                drawLine(p.x - p.r, p.y - p.r, p.x + p.r, p.y + p.r)
                drawLine(p.x - p.r, p.y + p.r, p.x + p.r, p.y - p.r)
            } else if p.shape == Point.MULTIPLY {
                drawLine(p.x - p.r, p.y - p.r, p.x + p.r, p.y + p.r)
                drawLine(p.x - p.r, p.y + p.r, p.x + p.r, p.y - p.r)
                drawLine(p.x - p.r, p.y, p.x + p.r, p.y)
                drawLine(p.x, p.y - p.r, p.x, p.y + p.r)
            } else if p.shape == Point.STAR {
                list = [Point]()
                for i in 0..<10 {
                    let theta = Double(i) * 36.0 * (Double.pi / 180.0)
                    let radius = (i % 2 == 0)
                    ? Double(p.r) * 1.147
                    : Double(p.r) * 0.38196 * 1.147
                    let x = p.x + Float(radius * sin(theta))
                    let y = p.y - Float(radius * cos(theta))  // minus because y grows down
                    list.append(Point(x, y))
                }
                if p.fillShape {
                    drawPath(list, PathOperator.fill)
                } else {
                    drawPath(list, PathOperator.closeAndStroke)
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
        } else {
            throw NSError(domain: "PDFjet", code: -1,
                          userInfo: [NSLocalizedDescriptionKey: "Invalid text rendering mode: \(mode)"])
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
            self.tmx = [ 1.0,  0.0,  0.0,  1.0 ]
        } else if degrees == 90 {
            self.tmx = [ 0.0,  1.0, -1.0,  0.0 ]
        } else if degrees == 180 {
            self.tmx = [-1.0,  0.0,  0.0, -1.0 ]
        } else if degrees == 270 {
            self.tmx = [ 0.0, -1.0,  1.0,  0.0 ]
        } else if degrees == 360 {
            self.tmx = [ 1.0,  0.0,  0.0,  1.0 ]
        } else {
            let sinOfAngle = Float(sin(Float(degrees) * (Float.pi / 180.0)))
            let cosOfAngle = Float(cos(Float(degrees) * (Float.pi / 180.0)))
            self.tmx = [cosOfAngle, sinOfAngle, -sinOfAngle, cosOfAngle]
        }
        self.tm0 = Array(String(format: pdf.floatFormat, tmx[0]).utf8)
        self.tm1 = Array(String(format: pdf.floatFormat, tmx[1]).utf8)
        self.tm2 = Array(String(format: pdf.floatFormat, tmx[2]).utf8)
        self.tm3 = Array(String(format: pdf.floatFormat, tmx[3]).utf8)
    }

    ///
    /// Draws a cubic bezier curve starting from the current point to the end point p3
    ///
    /// @param x1 first control point x
    /// @param y1 first control point y
    /// @param x2 second control point x
    /// @param y2 second control point y
    /// @param x3 end point x
    /// @param y3 end point y
    ///
    public func curveTo(
        _ x1: Float, _ y1: Float,
        _ x2: Float, _ y2: Float,
        _ x3: Float, _ y3: Float) {
        append(x1)
        append(Token.space)
        append(height - y1)
        append(Token.space)
        append(x2)
        append(Token.space)
        append(height - y2)
        append(Token.space)
        append(x3)
        append(Token.space)
        append(height - y3)
        append(" c\n")
    }

    public func drawCircularArc(
        _ x: Float, _ y: Float, _ r: Float, _ startAngle: Float, _ sweepDegrees: Float) -> [Float] {
        return drawArc(x, y, r, r, startAngle, sweepDegrees)
    }

    public func drawArc(
            _ x: Float,
            _ y: Float,
            _ rx: Float,
            _ ry: Float,
            _ startAngle: Float,
            _ sweepDegrees: Float) -> [Float] {
        var x1: Float = 0.0
        var y1: Float = 0.0
        var x2: Float = 0.0
        var y2: Float = 0.0
        var x3: Float = 0.0
        var y3: Float = 0.0

        let numSegments = Int(ceil(abs(sweepDegrees) / 90.0))
        var angleRad = Double(startAngle) * .pi / 180.0
        let deltaPerSeg = Double(sweepDegrees / Float(numSegments)) * .pi / 180.0

        for i in 0..<numSegments {
            let segStart = angleRad
            let segEnd = angleRad + deltaPerSeg
            let deltaRad = segEnd - segStart // guaranteed ≤ ±π/2

            // Calculate safe κ
            let k = Float(4.0 / 3.0 * tan(deltaRad / 4.0))

            let cosStart = Float(cos(segStart))
            let sinStart = Float(sin(segStart))
            let cosEnd = Float(cos(segEnd))
            let sinEnd = Float(sin(segEnd))

            // End points
            let x0 = x + rx * cosStart
            let y0 = y + ry * sinStart
            x3 = x + rx * cosEnd
            y3 = y + ry * sinEnd

            // Control points
            x1 = x0 - (k * rx * sinStart)
            y1 = y0 + (k * ry * cosStart)
            x2 = x3 + (k * rx * sinEnd)
            y2 = y3 - (k * ry * cosEnd)

            if i == 0 {
                moveTo(x0, y0)
            }
            curveTo(x1, y1, x2, y2, x3, y3)

            angleRad = segEnd
        }

        return [x1, y1, x2, y2, x3, y3]
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

    internal func setTextFont(_ font: Font, _ fontSize: Float) {
        if font.fontID != nil {
            append("/")
            append(font.fontID!)
        } else {
            append("/F")
            append(font.objNumber)
        }
        append(Token.space)
        append(fontSize)
        append(" Tf\n")
    }

    // Original code provided by:
    // Dominique Andre Gunia <contact@dgunia.de>
    // >>
    public func drawRectRoundCorners(
            _ x: Float,
            _ y: Float,
            _ w: Float,
            _ h: Float,
            _ r1: Float,
            _ r2: Float,
            _ pathOperator: PathOperator) {
        // The best 4-spline magic number
        let m4: Float = 0.55228
        var list = [Point]()
        // Starting point
        list.append(Point(x + w - r1, y))
        list.append(Point(x + w - r1 + m4*r1, y, Point.controlPointC))
        list.append(Point(x + w, y + r2 - m4*r2, Point.controlPointC))
        list.append(Point(x + w, y + r2))

        list.append(Point(x + w, y + h - r2))
        list.append(Point(x + w, y + h - r2 + m4*r2, Point.controlPointC))
        list.append(Point(x + w - m4*r1, y + h, Point.controlPointC))
        list.append(Point(x + w - r1, y + h))

        list.append(Point(x + r1, y + h))
        list.append(Point(x + r1 - m4*r1, y + h, Point.controlPointC))
        list.append(Point(x, y + h - m4*r2, Point.controlPointC))
        list.append(Point(x, y + h - r2))

        list.append(Point(x, y + r2))
        list.append(Point(x, y + r2 - m4*r2, Point.controlPointC))
        list.append(Point(x + m4*r1, y, Point.controlPointC))
        list.append(Point(x + r1, y))
        list.append(Point(x + w - r1, y))

        drawPath(list, pathOperator)
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
        append(Token.space)
        append(height - y)
        append(Token.space)
    }

    private func append(_ point: Point) {
        append(point.x)
        append(Token.space)
        append(height - point.y)
        append(Token.space)
    }

    func append(_ str: String) {
        self.buf.append(contentsOf: str.utf8)
    }

    func append(_ num: UInt32) {
        append(String(num))
    }

    func append(_ num: Int) {
        append(String(num))
    }

    func append(_ val: Float) {
        append(FastFloat.toByteArray(val))
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
            _ textColor: [Float],
            _ highlightColors: [String : Int32]) {
        if str != "" {
            if highlightColors[str] != nil {
                setBrushColor(highlightColors[str]!)
            } else {
                setBrushColor(textColor)
            }

            if font.isCoreFont {
                append("[<")
                drawASCIIString(font, str)
                append(">] TJ\n")
            } else {
                append("<")
                drawUnicodeString(font, str)
                append("> Tj\n")
            }
            str = ""
        }
    }

    func drawColoredString(
            _ font: Font,
            _ text: String,
            _ color: [Float],
            _ highlightColors: [String : Int32]) {
        var buf1 = String()
        var buf2 = String()
        for scalar in text.unicodeScalars {
            if isLetterOrDigit(scalar) {
                drawWord(font, &buf2, color, highlightColors)
                buf1.append(String(scalar))
            } else {
                drawWord(font, &buf1, color, highlightColors)
                buf2.append(String(scalar))
            }
        }
        drawWord(font, &buf1, color, highlightColors)
        drawWord(font, &buf2, color, highlightColors)
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
        if pdf.compliance == Compliance.PDF_UA_1 {
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
            append(Token.endDictionary)
            append("BDC\n")
            mcid += 1
        }
    }

    public func addArtifactBMC() {
        if pdf.compliance == Compliance.PDF_UA_1 {
            append("/Artifact BMC\n")
        }
    }

    public func addEMC() {
        if pdf.compliance == Compliance.PDF_UA_1 {
            append("EMC\n")
        }
    }

    func addAnnotation(_ annotation: Annotation) {
        annotation.y1 = self.height - annotation.y1
        annotation.y2 = self.height - annotation.y2
        self.annots.append(annotation)
        if pdf.compliance == Compliance.PDF_UA_1 {
            let element = StructElem()
            element.structure = StructElem.Link
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
        append(Token.space)
        append(x)
        append(Token.space)
        append(y)
        append(" cm\n")

        append(xScale)
        append(" 0 0 ")
        append(yScale)
        append(Token.space)
        append(x)
        append(Token.space)
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

    private func appendTwoHexDigits(_ number: Int, _ buffer: inout [UInt8]) {
        let index = (number & 0xFF) << 1
        buffer.append(hexadecimal.digits[index])
        buffer.append(hexadecimal.digits[index + 1])
    }

    public static let HEX: [UInt8] = [
        UInt8(ascii: "0"), UInt8(ascii: "1"), UInt8(ascii: "2"), UInt8(ascii: "3"),
        UInt8(ascii: "4"), UInt8(ascii: "5"), UInt8(ascii: "6"), UInt8(ascii: "7"),
        UInt8(ascii: "8"), UInt8(ascii: "9"), UInt8(ascii: "A"), UInt8(ascii: "B"),
        UInt8(ascii: "C"), UInt8(ascii: "D"), UInt8(ascii: "E"), UInt8(ascii: "F")
    ]

    private static func appendCodePointAsHex(_ codePoint: Int, _ buf: inout [UInt8]) {
        if codePoint <= 0xFFFF {
            // Basic Multilingual Plane (BMP) character
            buf.append(HEX[(codePoint >> 12) & 0xF])
            buf.append(HEX[(codePoint >> 8)  & 0xF])
            buf.append(HEX[(codePoint >> 4)  & 0xF])
            buf.append(HEX[(codePoint)       & 0xF])
        } else {
            // Supplementary character (needs surrogate pair in UTF-16)
            // Write as 6 hex digits (max Unicode code point is 0x10FFFF)
            buf.append(HEX[(codePoint >> 20) & 0xF])
            buf.append(HEX[(codePoint >> 16) & 0xF])
            buf.append(HEX[(codePoint >> 12) & 0xF])
            buf.append(HEX[(codePoint >> 8)  & 0xF])
            buf.append(HEX[(codePoint >> 4)  & 0xF])
            buf.append(HEX[(codePoint)       & 0xF])
        }
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
        watermark.setTextColor(Color.lightgrey)
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
        xy[1] += textLine.font!.descent
        return xy
    }

    @discardableResult
    public func addFooter(_ textLine: TextLine) throws -> [Float] {
        return try addFooter(textLine, textLine.font!.ascent)
    }

    @discardableResult
    public func addFooter(_ textLine: TextLine, _ offset: Float) throws -> [Float] {
        textLine.setLocation((getWidth() - textLine.getWidth())/2, getHeight() - offset)
        return textLine.drawOn(self)
    }

    /**
     * Begin text block.
     */
    func beginText() {
        append(Token.beginText)
    }

    /**
     * End the text block.
     */
    func endText() {
        append(Token.endText)
    }

    /**
     * Sets the text location.
     *
     * @param x the x coordinate of new text location.
     * @param y the y coordinate of new text location.
     */
    func setTextLocation(_ x: Float, _ y: Float) {
        append(x)
        append(Token.space)
        append(height - y)
        append(" Td\n")
    }

    /**
     * Sets the text leading.
     * @param leading the leading.
     */
    func setTextLeading(_ leading: Float) {
        append(leading)
        append(" TL\n")
    }

    /**
     * Advance to the next line.
     */
    func nextLine() {
        append("T*\n")
    }

    func setTextScaling(_ scaling: Float) {
        append(scaling)
        append(" Tz\n")
    }

    func setTextRise(_ rise: Float) {
        append(rise)
        append(" Ts\n")
    }

    /**
     * Draws a string at the correct location.
     * @param str the string.
     */
    internal func drawTextLine(_ font: Font, _ str: String, _ x: Float, _ y: Float) {
        append(Token.beginText)
        setTextLocation(x, y)
        setTextFont(font, font.size)
        if font.isCoreFont {
            append("[<")
            drawASCIIString(font, str)
            append(">] TJ\n")
        } else {
            append("<")
            drawUnicodeString(font, str)
            append("> Tj\n")
        }
        append(Token.endText)
    }

    func scaleAndRotate(_ x: Float, _ y: Float, _ w: Float, _ h: Float, _ degrees: Float) {
        // PDF transformations apply LAST-TO-FIRST (like a stack: last command = first applied)

        // [FINAL POSITIONING - Applied First]
        // Moves rotated/scaled image to target (x,y) on page
        append("1 0 0 1 ")
        append(x + w/2)
        append(Token.space)
        append((height - y) - h/2)
        append(" cm\n")

        // [ROTATION - Applied Second]
        // Rotates around current origin (0,0) by 'degrees'
        let radians = degrees * (Float.pi / 180)
        let cosValue = Float(cos(radians))
        let sinValue = Float(sin(radians))
        append(FastFloat.toByteArray(cosValue))
        append(Token.space)
        append(FastFloat.toByteArray(sinValue))
        append(Token.space)
        append(FastFloat.toByteArray(-sinValue))
        append(Token.space)
        append(FastFloat.toByteArray(cosValue))
        append(" 0 0 cm\n")

        // [ORIGIN SETUP - Applied Last]
        // Centers image at (0,0) and sets scale
        append(w)
        append(" 0 0 ")
        append(h)
        append(Token.space)
        append(-w/2)
        append(Token.space)
        append(-h/2)
        append(" cm\n")
    }

    func rotateAroundCenter(_ centerX: Float, _ centerY: Float, _ degrees: Float) {
        append("1 0 0 1 ")
        append(centerX)
        append(Token.space)
        append(centerY)
        append(" cm\n")

        let radians = degrees * Float.pi / 180
        let cosValue = Float(cos(radians))
        let sinValue = Float(sin(radians))
        append(FastFloat.toByteArray(cosValue))
        append(Token.space)
        append(FastFloat.toByteArray(sinValue))
        append(Token.space)
        append(FastFloat.toByteArray(-sinValue))
        append(Token.space)
        append(FastFloat.toByteArray(cosValue))
        append(" 0 0 cm\n")

        append("1 0 0 1 ")
        append(-centerX)
        append(Token.space)
        append(-centerY)
        append(" cm\n")
    }

    func drawTextBlock(
            _ font: Font,
            _ fontSize: Float,
            _ textLines: [TextLine],
            _ x: Float,
            _ y: Float,
            _ leading: Float,
            _ color: [Float],
            _ highlightColors: Dictionary<String, Int32>?) {
        if textLines.count == 0 {
            return
        }

        append("BT\n")
        setBrushColor(color)
        setTextFont(font, fontSize)
        var yText: Float = y
        for textLine in textLines {
            append("1 0 0 1 ")
            append(x + textLine.xOffset)
            append(" ")
            append(height - (yText + font.getAscent(fontSize)))
            append(" Tm\n")
            if highlightColors == nil {
                if font.isCoreFont {
                    append("[<")
                    drawASCIIString(font, textLine.text!)
                    append(">] TJ\n")
                } else {
                    append("<")
                    drawUnicodeString(font, textLine.text!)
                    append("> Tj\n")
                }
            } else {
                drawColoredString(font, textLine.text!, color, highlightColors!)
            }
            yText += leading
        }
        append("ET\n")

        var yLine = y + font.getBodyHeight(fontSize)
        for textLine in textLines {
            if textLine.underline {
                moveTo(x + textLine.xOffset, yLine)
                lineTo(x + textLine.xOffset + font.stringWidth(fontSize, textLine.text), yLine)
                strokePath()
            }
            yLine += leading
        }
    }
}   // End of Page.swift
