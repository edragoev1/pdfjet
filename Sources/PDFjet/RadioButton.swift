/**
 *  RadioButton.swift
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice,
      this list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above copyright notice,
      this list of conditions and the following disclaimer in the documentation
      and / or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
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

        page!.addBMC(StructElem.SPAN, language, altDescription, actualText)

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
                    altDescription,
                    actualText))
        }

        return [x + 6*r1 + font.stringWidth(label), y + font.bodyHeight]
    }

}   // End of RadioButton.swift
