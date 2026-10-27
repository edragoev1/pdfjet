/**
 * SimpleBarChart.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// A small chart of horizontal bars that compares a few values: a name at the
/// left of each bar, the value at its end, and no axis, grid or legend, on a
/// light panel, with a title and a subtitle at the top. One bar can be
/// highlighted, in navy, its name and value bold, as the best of the values.
/// Each bar is as long as its share of the largest value.
///
/// It is meant to stand beside a Table of the same numbers: the chart for the
/// picture, the table for the record and for a screen reader. In a PDF/UA
/// document the chart is one figure, described from its title and its values
/// unless an alternate description is set. For grouped, stacked or vertical
/// bars with an axis, use BarChart. Please see Example_20.
///
public class SimpleBarChart : Drawable {
    // The colors of a simple bar chart unless set: a light panel, gray bars,
    // the highlighted bar navy, the names dark and the values gray.
    static let PANEL: Int32 = 0xF1F3F6
    static let BAR: Int32 = 0xA7AFB8
    static let HIGHLIGHT: Int32 = 0x1F3A5F
    static let INK: Int32 = 0x22262B
    static let MUTED: Int32 = 0x5F6873

    private let f1: Font    // The title, and the bold name and value
    private let f2: Font    // The rest
    private var x: Float = 0.0
    private var y: Float = 0.0
    private var w: Float = 300.0
    private var title = ""
    private var subtitle = ""
    private var altDescription = ""
    private var names = [String]()
    private var values = [Float]()
    private var highlight = -1      // The index of the highlighted bar, or -1
    private var minFractionDigits = 0
    private var maxFractionDigits = 2
    private var panelColor = SimpleBarChart.PANEL
    private var barColor = SimpleBarChart.BAR
    private var highlightColor = SimpleBarChart.HIGHLIGHT

    ///
    /// Creates a simple bar chart, 300 points wide.
    ///
    /// - Parameter f1: the font of the title and of the highlighted bar's name
    ///   and value, a bold one.
    /// - Parameter f2: the font of the subtitle and of the other names and values.
    ///
    public init(_ f1: Font, _ f2: Font) {
        self.f1 = f1
        self.f2 = f2
    }

    ///
    /// Sets the title, drawn at the top left of the chart.
    ///
    /// - Parameter title: the title.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setTitle(_ title: String) -> SimpleBarChart {
        self.title = title
        return self
    }

    ///
    /// Sets the subtitle, drawn small and gray under the title: the unit of
    /// the values, say.
    ///
    /// - Parameter subtitle: the subtitle.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setSubtitle(_ subtitle: String) -> SimpleBarChart {
        self.subtitle = subtitle
        return self
    }

    ///
    /// Sets the alternate description of the chart in a PDF/UA document.
    /// Without one the chart is described from its title, its subtitle and its
    /// values.
    ///
    /// - Parameter altDescription: the alternate description.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setAltDescription(_ altDescription: String) -> SimpleBarChart {
        self.altDescription = altDescription
        return self
    }

    ///
    /// Adds a bar: its name and its value. A value below zero is drawn as no
    /// bar, with its value written.
    ///
    /// - Parameter name: the name of the bar.
    /// - Parameter value: its value.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func add(_ name: String, _ value: Float) -> SimpleBarChart {
        names.append(name)
        values.append(value)
        return self
    }

    ///
    /// Highlights the bar of the index, the first bar 0: it is drawn in the
    /// highlight color, its name and value in the bold font. Which bar is the
    /// best, the lowest value or the highest, is the caller's to say.
    ///
    /// - Parameter index: the index of the bar.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setHighlight(_ index: Int) -> SimpleBarChart {
        self.highlight = index
        return self
    }

    ///
    /// Sets the number of digits after the decimal point of the values, all of
    /// them alike. Without it a value has as many as it needs, two at most.
    ///
    /// - Parameter digits: the number of digits.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setFractionDigits(_ digits: Int) -> SimpleBarChart {
        self.minFractionDigits = digits
        self.maxFractionDigits = digits
        return self
    }

    ///
    /// Sets the color of the panel behind the chart, or -1 for none. It is
    /// light gray, 0xF1F3F6, unless set.
    ///
    /// - Parameter color: the color.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setPanelColor(_ color: Int32) -> SimpleBarChart {
        self.panelColor = color
        return self
    }

    ///
    /// Sets the color of the bars that are not highlighted. It is gray,
    /// 0xA7AFB8, unless set.
    ///
    /// - Parameter color: the color.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setBarColor(_ color: Int32) -> SimpleBarChart {
        self.barColor = color
        return self
    }

    ///
    /// Sets the color of the highlighted bar and of its name. It is navy,
    /// 0x1F3A5F, unless set.
    ///
    /// - Parameter color: the color.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setHighlightColor(_ color: Int32) -> SimpleBarChart {
        self.highlightColor = color
        return self
    }

    ///
    /// Sets the top left corner of the chart.
    ///
    /// - Parameter x: the x coordinate.
    /// - Parameter y: the y coordinate.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x = x
        self.y = y
        return self
    }

    ///
    /// Sets the width of the chart. Its height follows from the fonts and the
    /// number of bars; see getHeight.
    ///
    /// - Parameter w: the width.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setWidth(_ w: Float) -> SimpleBarChart {
        self.w = w
        return self
    }

    ///
    /// Returns the height of the chart: its padding, its title and subtitle,
    /// and a row for each bar.
    ///
    /// - Returns: the height of the chart.
    ///
    public func getHeight() -> Float {
        return top() + Float(values.count) * rowHeight() + pad()
    }

    // The space around the chart's content, a size of the text of the bars.
    private func pad() -> Float {
        return f2.getSize()
    }

    // The height of a row, a bar and the space under it.
    func rowHeight() -> Float {
        return 2.4 * f2.getSize()
    }

    // The size of the subtitle.
    private func subtitleSize() -> Float {
        return 0.8 * f2.getSize()
    }

    // The height above the first row: the padding, the title and the subtitle.
    private func top() -> Float {
        var top = pad()
        if !title.isEmpty {
            top += f1.getAscent(f1.getSize()) + f1.getDescent(f1.getSize())
        }
        if !subtitle.isEmpty {
            top += 1.4 * subtitleSize()
        }
        if !title.isEmpty || !subtitle.isEmpty {
            top += f2.getSize()
        }
        return top
    }

    // The value written at the end of a bar.
    func label(_ value: Float) -> String {
        // Its digits grouped in thousands, 23,507, as in the other charts
        return ChartScale.groupDigits(ChartScale.format(value, minFractionDigits, maxFractionDigits))
    }

    // The font of the name and the value of the bar of the index.
    private func fontOf(_ index: Int) -> Font {
        return index == highlight ? f1 : f2
    }

    // The alternate description: the one set, or the title, the subtitle and
    // the values.
    func getAltDescription() -> String {
        if !altDescription.isEmpty {
            return altDescription
        }
        var text = ChartScale.join(title, subtitle)
        if text.isEmpty {
            text = "Bar chart"
        }
        for (i, name) in names.enumerated() {
            text += (i == 0) ? ": " : ", "
            text += name + " " + label(values[i])
        }
        return text + "."
    }

    ///
    /// Draws the chart on the page.
    ///
    /// - Parameter page: the page to draw on.
    /// - Returns: the x and y coordinates of the bottom right corner of the chart.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        let h = getHeight()
        guard let page = page, !values.isEmpty else {   // Measured, or nothing to draw
            return [x + w, y + h]
        }
        let size = f2.getSize()
        let pad = pad()

        // The widest name and value, and the room left for the bars
        // A name wider than two fifths of the chart is shortened with an ellipsis
        var shown = [String]()
        var nameWidth: Float = 0.0
        var valueWidth: Float = 0.0
        var largest: Float = 0.0
        for i in 0..<names.count {
            valueWidth = Swift.max(valueWidth, fontOf(i).stringWidth(size, label(values[i])))
            if ChartScale.isValue(values[i]) {
                largest = Swift.max(largest, values[i])
            }
        }
        // The names in what the values leave of the width, two fifths of it at
        // most: in a chart narrower than its values they were written past it
        let nameRoom = Swift.max(Swift.min(0.4 * (w - 2.0 * pad), w - 3.0 * pad - valueWidth - size / 2.0), 0.0)
        for (i, name) in names.enumerated() {
            let font = fontOf(i)
            // No name where not even an ellipsis fits; the description has it
            shown.append(nameRoom < font.stringWidth(size, "\u{2026}")
                    ? "" : ChartScale.shorten(font, size, name, nameRoom))
            nameWidth = Swift.max(nameWidth, font.stringWidth(size, shown[i]))
        }
        let barX = x + pad + nameWidth + pad
        let barRoom = Swift.max(x + w - pad - (valueWidth + size / 2.0) - barX, 0.0)

        page.addBDC(StructElem.FIGURE, nil, getAltDescription())
        page.saveGraphicsState()

        if panelColor >= 0 {
            page.setBrushColor(panelColor)
            page.fillRect(x, y, w, h)
        }
        var baseline = y + pad
        if !title.isEmpty {
            baseline += f1.getAscent(f1.getSize())
            page.drawString(f1, f1.getSize(), ChartScale.shorten(f1, f1.getSize(), title, w - 2.0 * pad), x + pad, baseline, Util.toRGB(SimpleBarChart.INK), nil)
            baseline += f1.getDescent(f1.getSize())
        }
        if !subtitle.isEmpty {
            baseline += 1.4 * subtitleSize()
            page.drawString(f2, subtitleSize(), ChartScale.shorten(f2, subtitleSize(), subtitle, w - 2.0 * pad),
                    x + pad, baseline, Util.toRGB(SimpleBarChart.MUTED), nil)
        }

        let rowTop = y + top()
        let barHeight = 1.6 * size
        let ascent = f2.getAscent(size)
        for (i, name) in shown.enumerated() {
            let font = fontOf(i)
            let value = values[i]
            let top = rowTop + Float(i) * rowHeight()
            let textBaseline = top + (barHeight + ascent - f2.getDescent(size)) / 2.0
            let highlighted = (i == highlight)
            page.drawString(font, size, name, x + pad, textBaseline,
                    Util.toRGB(highlighted ? highlightColor : SimpleBarChart.INK), nil)
            var length: Float = 0.0
            if largest > 0.0 && ChartScale.isValue(value) && value > 0.0 {
                length = barRoom * (value / largest)
            }
            if length > 0.0 {
                page.setBrushColor(highlighted ? highlightColor : barColor)
                page.fillRect(barX, top, Swift.max(length, 1.5), barHeight)
            }
            page.drawString(font, size, label(value), barX + length + size / 2.0, textBaseline,
                    Util.toRGB(highlighted ? SimpleBarChart.INK : SimpleBarChart.MUTED), nil)
        }

        page.restoreGraphicsState()
        page.setFigureBoundingBox(x, y, w, h)
        page.addEMC()
        return [x + w, y + h]
    }
}   // End of SimpleBarChart.swift
