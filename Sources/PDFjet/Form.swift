/**
 * Form.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/**
 * Please see Example_42
 */
public class Form : Drawable {
    private var fields: [Field]
    private var x: Float = 0.0
    private var y: Float = 0.0
    private var f1: Font?
    private var labelFontSize: Float = 9.0
    private var f2: Font?
    private var valueFontSize: Float = 9.0
    private var formWidth: Float = 500.0
    private var lineWidth: Float = 0.0
    private var labelColor: [Float] = [0.0, 0.0, 0.0]
    private var valueColor: [Float] = [0.33, 0.33, 0.66]

    /// Creates a form with the specified fields.
    public init(_ fields: [Field]) {
        self.fields = fields
    }

    /// Sets the location of this form on the page.
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x = x
        self.y = y
        return self
    }

    /// Sets the width of this form.
    @discardableResult
    public func setFormWidth(_ formWidth: Float) -> Form {
        self.formWidth = formWidth
        return self
    }

    /// Sets the width of the lines.
    @discardableResult
    public func setLineWidth(_ lineWidth: Float) -> Form {
        self.lineWidth = lineWidth
        return self
    }

    /// Sets the font of the labels.
    @discardableResult
    public func setLabelFont(_ f1: Font) -> Form {
        self.f1 = f1
        return self
    }

    /// Sets the font size of the labels.
    @discardableResult
    public func setLabelFontSize(_ labelFontSize: Float) -> Form {
        self.labelFontSize = labelFontSize
        return self
    }

    /// Sets the font of the values.
    @discardableResult
    public func setValueFont(_ f2: Font) -> Form {
        self.f2 = f2
        return self
    }

    /// Sets the font size of the values.
    @discardableResult
    public func setValueFontSize(_ valueFontSize: Float) -> Form {
        self.valueFontSize = valueFontSize
        return self
    }

    /// Sets the color of the labels from an array of red, green and blue values.
    @discardableResult
    public func setLabelColor(_ labelColor: [Float]) -> Form {
        self.labelColor = labelColor
        return self
    }

    /// Sets the color of the labels as a 0xRRGGBB value, for example Color.black.
    @discardableResult
    public func setLabelColor(_ labelColor: Int32) -> Form {
        let r = Float((labelColor >> 16) & 0xff)/255.0
        let g = Float((labelColor >>  8) & 0xff)/255.0
        let b = Float((labelColor)       & 0xff)/255.0
        return setLabelColor([r, g, b])
    }

    /// Sets the color of the values from an array of red, green and blue values.
    @discardableResult
    public func setValueColor(_ valueColor: [Float]) -> Form {
        self.valueColor = valueColor
        return self
    }

    /// Sets the color of the values as a 0xRRGGBB value, for example Color.blue.
    @discardableResult
    public func setValueColor(_ valueColor: Int32) -> Form {
        let r = Float((valueColor >> 16) & 0xff)/255.0
        let g = Float((valueColor >>  8) & 0xff)/255.0
        let b = Float((valueColor)       & 0xff)/255.0
        return setValueColor([r, g, b])
    }

    /**
     * Draws this Form on the specified page.
     *
     * - Parameter page: the page to draw this form on.
     * - Returns: x and y coordinates of the bottom right corner of this component.
     */
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        if page == nil {
            fatalError("Page cannot be nil")
        }

        var yField: Float = 0.0
        let xOffset: Float = 3.0
        for i in 0..<fields.count {
            let field = fields[i]
            if field.x == 0.0 {
                if field.label != "" {
                    if i > 0 {
                        let hLine = Line(
                                x,
                                y + yField,
                                x + formWidth,
                                y + yField)
                        hLine.setStrokeWidth(lineWidth).drawOn(page)
                    }
                    yField += f1!.getAscent(labelFontSize) + 3.0*f1!.getDescent(labelFontSize)
                }
                yField += f2!.getAscent(valueFontSize) + f2!.getDescent(valueFontSize)
            }

            if field.label != "" {
                let yOffset = 2*f1!.getDescent(labelFontSize) +
                        f2!.getAscent(valueFontSize) + f2!.getDescent(valueFontSize)
                TextLine(f1!, field.label)
                        .setFontSize(labelFontSize)
                        .setTextColor(labelColor)
                        .setLocation(
                                x + field.x + xOffset,
                                y + yField - yOffset).drawOn(page)
            }

            TextLine(f2!, field.value)
                    .setFontSize(valueFontSize)
                    .setTextColor(valueColor)
                    .setLocation(xOffset + x + field.x, y + yField - f2!.getDescent(valueFontSize))
                    .drawOn(page)

            if field.x != 0.0 {
                var rowHeight = f1!.getAscent(labelFontSize) + 3.0*f1!.getDescent(labelFontSize)
                rowHeight += f2!.getAscent(valueFontSize) + f2!.getDescent(valueFontSize)
                let vLine = Line(
                        x + field.x,
                        (y + yField) - rowHeight,
                        x + field.x,
                        y + yField)
                vLine.setStrokeWidth(lineWidth).drawOn(page)
            }
        }

        let rect = Rect()
        rect.setLocation(x, y)
        rect.setBorderWidth(lineWidth)
        rect.setBorderColor(Color.black)
        rect.setSize(formWidth, yField)
        rect.drawOn(page)

        return [ x + formWidth, y + yField ]
    }
}   // End of Form.swift
