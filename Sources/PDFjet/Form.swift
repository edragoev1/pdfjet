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
    private var labelFontSize: Float = 8.0
    private var f2: Font?
    private var valueFontSize: Float = 10.0
    private var formWidth: Float = 500.0
    private var lineWidth: Float = 0.0
    private var labelColor: [Float] = [0.0, 0.0, 0.0]
    private var valueColor: [Float] = [0.0, 0.0, 1.0]

    public init(_ fields: [Field]) {
        self.fields = fields
    }

    public func setPosition(_ x: Float, _ y: Float) {
        setLocation(x, y)
    }

    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Form {
        self.x = x
        self.y = y
        return self
    }

    @discardableResult
    public func setFormWidth(_ formWidth: Float) -> Form {
        self.formWidth = formWidth
        return self
    }

    @discardableResult
    public func setLineWidth(_ lineWidth: Float) -> Form {
        self.lineWidth = lineWidth
        return self
    }

    @discardableResult
    public func setLabelFont(_ f1: Font) -> Form {
        self.f1 = f1
        return self
    }

    @discardableResult
    public func setLabelFontSize(_ labelFontSize: Float) -> Form {
        self.labelFontSize = labelFontSize
        return self
    }

    @discardableResult
    public func setValueFont(_ f2: Font) -> Form {
        self.f2 = f2
        return self
    }

    @discardableResult
    public func setValueFontSize(_ valueFontSize: Float) -> Form {
        self.valueFontSize = valueFontSize
        return self
    }

    @discardableResult
    public func setLabelColor(_ labelColor: [Float]) -> Form {
        self.labelColor = labelColor
        return self
    }

    @discardableResult
    public func setValueColor(_ valueColor: [Float]) -> Form {
        self.valueColor = valueColor
        return self
    }

    /**
     * Draws this Form on the specified page.
     *
     * @param page the page to draw this form on.
     * @return x and y coordinates of the bottom right corner of this component.
     * @throws Exception
     */
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        if page == nil {
            print("[ERROR] drawOn called with nil page")
            return []
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
                        hLine.setWidth(lineWidth).drawOn(page)
                    }
                    yField += f1!.getAscent(labelFontSize) + 4.0*f1!.getDescent(labelFontSize)
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
                let vLine = Line(
                        x + field.x,
                        y + yField - (f2!.getAscent(valueFontSize) + f2!.getDescent(valueFontSize)),
                        x + field.x,
                        y + yField)
                vLine.setWidth(lineWidth).drawOn(page)
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
