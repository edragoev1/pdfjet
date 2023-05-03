/**
 *  CheckBox.swift
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
    /// @param fontSize the fontSize to use.
    /// @return this CheckBox.
    ///
    @discardableResult
    public func setFontSize(_ fontSize: Float) -> CheckBox {
        self.font.setSize(fontSize)
        return self
    }


    ///
    /// Sets the color of the check box.
    ///
    /// @param boxColor the check box color specified as an 0xRRGGBB integer.
    /// @return this CheckBox.
    ///
    @discardableResult
    public func setBoxColor(_ boxColor: Int32) -> CheckBox {
        self.boxColor = boxColor
        return self
    }


    ///
    /// Sets the color of the check mark.
    ///
    /// @param checkColor the check mark color specified as an 0xRRGGBB integer.
    /// @return this CheckBox.
    ///
    @discardableResult
    public func setCheckmark(_ checkColor: Int32) -> CheckBox {
        self.checkColor = checkColor
        return self
    }


    public func setPosition(_ x: Float, _ y: Float) {
        setLocation(x, y)
    }


    ///
    /// Set the x,y location on the Page.
    ///
    /// @param x the x coordinate on the Page.
    /// @param y the y coordinate on the Page.
    /// @return this CheckBox.
    ///
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> CheckBox {
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
    /// @return this CheckBox.
    ///
    @discardableResult
    public func check(_ mark: Int) -> CheckBox {
        self.mark = mark
        return self
    }


    ///
    /// Sets the URI for the "click text line" action.
    ///
    /// @param uri the URI.
    /// @return this CheckBox.
    ///
    @discardableResult
    public func setURIAction(_ uri: String) -> CheckBox{
        self.uri = uri
        return self
    }


    ///
    /// Sets the alternate description of this check box.
    ///
    /// @param altDescription the alternate description of the check box.
    /// @return this Checkbox.
    ///
    @discardableResult
    public func setAltDescription(_ altDescription: String) -> CheckBox {
        self.altDescription = altDescription
        return self
    }


    ///
    /// Sets the actual text for this check box.
    ///
    /// @param actualText the actual text for the check box.
    /// @return this CheckBox.
    ///
    @discardableResult
    public func setActualText(_ actualText: String)-> CheckBox {
        self.actualText = actualText
        return self
    }


    ///
    /// Draws this CheckBox on the specified Page.
    ///
    /// @param page the Page where the CheckBox is to be drawn.
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
        page!.setLinePattern("[] 0")
        page!.drawRect(self.x, yBox, self.w, self.h)

        if mark == Mark.CHECK || mark == Mark.X {
            page!.setPenWidth(self.checkWidth!)
            page!.setPenColor(self.checkColor)
            if mark == Mark.CHECK {
                // Draw check mark
                page!.moveTo(x + checkWidth!, yBox + h/2)
                page!.lineTo(x + w/6 + checkWidth!, (yBox + h) - 4.0*checkWidth!/3.0)
                page!.lineTo((x + w) - checkWidth!, yBox + checkWidth!)
                page!.strokePath()
            } else if mark == Mark.X {
                // Draw 'X' mark
                page!.moveTo(self.x + checkWidth!, yBox + checkWidth!)
                page!.lineTo((self.x + self.w) - checkWidth!, (yBox + self.h) - checkWidth!)
                page!.moveTo((self.x + self.w) - checkWidth!, yBox + checkWidth!)
                page!.lineTo(self.x + checkWidth!, (yBox + h) - checkWidth!)
                page!.strokePath()
            }
        }

        if uri != nil {
            page!.setBrushColor(Color.blue)
        }
        page!.drawString(font, label, x + 3.0*w/2.0, y + font.ascent)
        page!.setPenWidth(0.0)
        page!.setPenColor(Color.black)
        page!.setBrushColor(Color.black)

        page!.addEMC()

        if uri != nil {
            page!.addAnnotation(Annotation(
                    uri,
                    nil,
                    x + 3.0*w/2.0,
                    y,
                    x + 3.0*w/2.0 + font.stringWidth(label),
                    y + font.bodyHeight,
                    language,
                    actualText,
                    altDescription))
        }

        return [x + 3.0*w + font.stringWidth(label), y + font.bodyHeight]
    }

}   // End of CheckBox.swift
