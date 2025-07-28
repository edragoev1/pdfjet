/**
 *  RadioButton.swift
 *
©2025 PDFjet Software

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
/// Creates a RadioButton, which can be set selected or unselected.
///
public class RadioButton : Drawable {
    private var selected: Bool = false
    private var x: Float = 0.0
    private var y: Float = 0.0
    private var r1: Float = 0.0
    private var r2: Float = 0.0
    private var penWidth: Float = 0.0
    private var font: Font
    private var label: String = ""
    private var uri: String?

    private var language: String?
    private var altDescription: String = Single.space
    private var actualText: String = Single.space

    ///
    /// Creates a RadioButton that is not selected.
    ///
    public init(_ font: Font, _ label: String) {
        self.font = font
        self.label = label
    }

    ///
    /// Sets the font size to use for this text line.
    ///
    /// @param fontSize the fontSize to use.
    /// @return this RadioButton.
    ///
    public func setFontSize(_ fontSize: Float) -> RadioButton {
        self.font.setSize(fontSize)
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
    /// @return this RadioButton.
    ///
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> RadioButton {
        self.x = x
        self.y = y
        return self
    }

    ///
    /// Sets the URI for the "click text line" action.
    ///
    /// @param uri the URI.
    /// @return this RadioButton.
    ///
    @discardableResult
    public func setURIAction(_ uri: String) -> RadioButton {
        self.uri = uri
        return self
    }

    ///
    /// Selects or deselects this radio button.
    ///
    /// @param selected the selection flag.
    /// @return this RadioButton.
    ///
    @discardableResult
    public func select(_ selected: Bool) -> RadioButton {
        self.selected = selected
        return self
    }

    ///
    /// Sets the alternate description of this radio button.
    ///
    /// @param altDescription the alternate description of the radio button.
    /// @return this RadioButton.
    ///
    public func setAltDescription(_ altDescription: String) -> RadioButton {
        self.altDescription = altDescription
        return self
    }

    ///
    /// Sets the actual text for this radio button.
    ///
    /// @param actualText the actual text for the radio button.
    /// @return this RadioButton.
    ///
    public func setActualText(_ actualText: String) -> RadioButton {
        self.actualText = actualText
        return self
    }

    ///
    /// Draws this RadioButton on the specified Page.
    ///
    /// @param page the Page where the RadioButton is to be drawn.
    /// @return x and y coordinates of the bottom right corner of this component.
    /// @throws Exception
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        page!.addBMC(StructElem.P, language, actualText, altDescription)

        self.r1 = font.getAscent()/2
        self.r2 = r1/2
        self.penWidth = r1/10

        let yBox = y
        page!.setPenWidth(1.0)
        page!.setPenColor(Color.black)
        page!.setLinePattern("[] 0")
        page!.setBrushColor(Color.black)
        page!.drawCircle(x + r1 + penWidth, yBox + r1 + penWidth, r1)

        if self.selected {
            page!.drawCircle(x + r1 + penWidth, yBox + r1 + penWidth, r2, Operation.FILL)
        }

        if self.uri != nil {
            page!.setBrushColor(Color.blue)
        }
        page!.drawString(font, label, x + 3*r1, y + font.ascent)
        page!.setPenWidth(0.0)
        page!.setBrushColor(Color.black)

        page!.addEMC()

        if uri != nil {
            page!.addAnnotation(Annotation(
                    uri,
                    nil,
                    x + 3*r1,
                    y,
                    x + 3*r1 + font.stringWidth(label),
                    y + font.bodyHeight,
                    language,
                    actualText,
                    altDescription))
        }

        return [x + 6*r1 + font.stringWidth(label), y + font.bodyHeight]
    }
}   // End of RadioButton.swift
