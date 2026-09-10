/**
 * CheckBox.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// Creates a CheckBox, which can be set checked or unchecked.
/// By default the check box is unchecked.
///
/// Portions of the code was provided by Shirley C. Christenson
/// Shirley Christenson Consulting
public class CheckBox : Drawable {
    private var x: Float = 0.0
    private var y: Float = 0.0
    private var w: Float = 0.0
    private var h: Float = 0.0
    private var boxColor = Color.black
    private var checkColor = Color.black
    private var penWidth: Float?
    private var checkWidth: Float?
    private var mark = 0
    private var font: Font
    private var fontSize: Float = 12.0
    private var label: String = ""
    private var uri: String?
    private var language: String?
    private var altDescription: String = Single.space
    private var actualText: String = Single.space

    ///
    /// Creates a CheckBox with black check mark.
    ///
    public init(_ font: Font, _ label: String) {
        self.font = font
        self.label = label
    }

    ///
    /// Sets the font size to use for this text line.
    ///
    /// - Parameter fontSize: the fontSize to use.
    /// - Returns: this CheckBox.
    ///
    @discardableResult
    public func setFontSize(_ fontSize: Float) -> CheckBox {
        self.fontSize = fontSize
        return self
    }

    ///
    /// Sets the color of the check box.
    ///
    /// - Parameter boxColor: the check box color specified as an 0xRRGGBB integer.
    /// - Returns: this CheckBox.
    ///
    @discardableResult
    public func setBoxColor(_ boxColor: Int32) -> CheckBox {
        self.boxColor = boxColor
        return self
    }

    ///
    /// Sets the color of the check mark.
    ///
    /// - Parameter checkColor: the check mark color specified as an 0xRRGGBB integer.
    /// - Returns: this CheckBox.
    ///
    @discardableResult
    public func setCheckmark(_ checkColor: Int32) -> CheckBox {
        self.checkColor = checkColor
        return self
    }

    ///
    /// Set the x,y location on the Page.
    ///
    /// - Parameter x: the x coordinate on the Page.
    /// - Parameter y: the y coordinate on the Page.
    /// - Returns: this CheckBox.
    ///
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x = x
        self.y = y
        return self
    }

    ///
    /// Gets the height of the CheckBox.
    ///
    public func getHeight() -> Float {
        return self.h
    }

    ///
    /// Gets the width of the CheckBox.
    ///
    public func getWidth() -> Float {
        return self.w
    }

    ///
    /// Checks or unchecks this check box. See the Mark class for available options.
    ///
    /// - Returns: this CheckBox.
    ///
    @discardableResult
    public func check(_ mark: Int) -> CheckBox {
        self.mark = mark
        return self
    }

    ///
    /// Sets the URI for the "click text line" action.
    ///
    /// - Parameter uri: the URI.
    /// - Returns: this CheckBox.
    ///
    @discardableResult
    public func setURIAction(_ uri: String) -> CheckBox{
        self.uri = uri
        return self
    }

    ///
    /// Sets the alternate description of this check box.
    ///
    /// - Parameter altDescription: the alternate description of the check box.
    /// - Returns: this Checkbox.
    ///
    @discardableResult
    public func setAltDescription(_ altDescription: String) -> CheckBox {
        self.altDescription = altDescription
        return self
    }

    ///
    /// Sets the actual text for this check box.
    ///
    /// - Parameter actualText: the actual text for the check box.
    /// - Returns: this CheckBox.
    ///
    @discardableResult
    public func setActualText(_ actualText: String)-> CheckBox {
        self.actualText = actualText
        return self
    }

    /// Draws a blue X mark of the specified size at the specified location.
    public static func xMark(_ page: Page, _ x: Float, _ y: Float, _ size: Float) {
        page.setPenColor(Color.blue)
        page.setPenWidth(size / 5)
        page.moveTo(x, y)
        page.lineTo(x + size, y + size)
        page.moveTo(x, y + size)
        page.lineTo(x + size, y)
        page.strokePath()
    }

    ///
    /// Draws this CheckBox on the specified Page.
    ///
    /// - Parameter page: the Page where the CheckBox is to be drawn.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        page!.addBMC(StructElem.P, language, actualText, altDescription)

        self.w = self.font.getAscent()
        self.h = self.w
        self.penWidth = self.w/15
        self.checkWidth = self.w/5

        let yBox: Float = y
        page!.setPenWidth(self.penWidth!)
        page!.setPenColor(self.boxColor)
        page!.setStrokeDashPattern("[] 0")
        page!.drawRect(self.x + self.penWidth!, yBox + self.penWidth!, self.w, self.h)

        if mark == Mark.CHECK || mark == Mark.X {
            page!.setPenWidth(self.checkWidth!)
            page!.setPenColor(self.checkColor)
            if mark == Mark.CHECK {
                // Draw check mark
                page!.moveTo(x + checkWidth! + penWidth!, yBox + h/2 + penWidth!)
                page!.lineTo((x + w/6 + checkWidth!) + penWidth!, ((yBox + h) - 4.0*checkWidth!/3.0) + penWidth!)
                page!.lineTo(((x + w) - checkWidth!) + penWidth!, (yBox + checkWidth!) + penWidth!)
                page!.strokePath()
            } else if mark == Mark.X {
                // Draw 'X' mark
                page!.moveTo(x + checkWidth! + penWidth!, yBox + checkWidth! + penWidth!)
                page!.lineTo(((x + w) - checkWidth!) + penWidth!, ((yBox + h) - checkWidth!) + penWidth!)
                page!.moveTo(((x + w) - checkWidth!) + penWidth!, (yBox + checkWidth!) + penWidth!)
                page!.lineTo((x + checkWidth!) + penWidth!, ((yBox + h) - checkWidth!) + penWidth!)
                page!.strokePath()
            }
        }

        if uri != nil {
            page!.setBrushColor(Color.blue)
        }
        page!.drawString(font, fontSize, label, x + 3.0*w/2.0, y + font.ascent)
        page!.setPenWidth(0.0)
        page!.setPenColor(Color.black)
        page!.setBrushColor(Color.black)
        page!.addEMC()

        if uri != nil {     // TODO: BMC and EMC here!
            page!.addAnnotation(Annotation(
                    Annotation.Link,
                    x + 3.0*w/2.0,
                    y,
                    x + 3.0*w/2.0 + font.stringWidth(label),
                    y + font.getBodyHeight(),   // TODO: Use fontSize
                    nil,    // Vertices
                    nil,    // Fill Color
                    0.0,    // Transparency
                    nil,    // Title
                    nil,    // Contents
                    uri,
                    nil,
                    language,
                    actualText,
                    altDescription))
        }

        return [x + 3.0*w + font.stringWidth(label), y + font.bodyHeight]
    }
}   // End of CheckBox.swift
