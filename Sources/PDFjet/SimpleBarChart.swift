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
/// highlighted, in navy, its name and value bold, as the best of the values:
/// the bar of an index, or the lowest or the highest value, which the chart
/// finds. Each bar is as long as its share of the largest value. A dashed line
/// can mark a value across the bars, a target or an average, and the bars can
/// have rounded ends.
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
    // The bar the chart highlights itself: 0 none, -1 the lowest value, 1 the
    // highest
    private var highlightValue = 0
    private var reference = Float.nan   // The value of the reference line, or NaN
    private var referenceLabel = ""
    private var roundedBars = false
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
    /// bar, with its value written. A value that is NaN or infinite has no
    /// bar and no value, as in the other charts, and the description leaves
    /// it out.
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
        self.highlightValue = 0
        return self
    }

    ///
    /// Highlights the bar of the lowest value, the best of times or prices;
    /// the first of them when two are lowest. Bars added later count too, as
    /// the chart finds the bar when it is drawn.
    ///
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setHighlightLowest() -> SimpleBarChart {
        self.highlightValue = -1
        return self
    }

    ///
    /// Highlights the bar of the highest value, the best of sales or scores;
    /// the first of them when two are highest. Bars added later count too, as
    /// the chart finds the bar when it is drawn.
    ///
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setHighlightHighest() -> SimpleBarChart {
        self.highlightValue = 1
        return self
    }

    ///
    /// Draws a thin dashed line across the bars at a value, a target or an
    /// average, with its label and value under the bars: "Target 50". The
    /// bars are scaled so that the line fits, should it be past the largest
    /// value. A value that is NaN or infinite draws no line.
    ///
    /// - Parameter value: the value of the line.
    /// - Parameter label: the label written before the value, or "".
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setReferenceLine(_ value: Float, _ label: String) -> SimpleBarChart {
        self.reference = value
        self.referenceLabel = label
        return self
    }

    ///
    /// Draws the bars with slightly rounded ends. They are square unless set.
    ///
    /// - Parameter roundedBars: true for rounded ends.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setRoundedBars(_ roundedBars: Bool) -> SimpleBarChart {
        self.roundedBars = roundedBars
        return self
    }

    // The index of the highlighted bar: the one set, or the one of the lowest
    // or highest value, or -1.
    func highlighted() -> Int {
        if highlightValue == 0 {
            return highlight
        }
        var best = -1
        for (i, value) in values.enumerated() where ChartScale.isValue(value) {
            if best < 0 || (highlightValue < 0 ? value < values[best] : value > values[best]) {
                best = i
            }
        }
        return best
    }

    // True when a reference line is drawn.
    private func hasReference() -> Bool {
        return ChartScale.isValue(reference)
    }

    // The text under the reference line: its label and its value, "Target 50",
    // or its value alone.
    func referenceText() -> String {
        return referenceLabel.isEmpty ? label(reference) : referenceLabel + " " + label(reference)
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
    /// Sets the color of the panel behind the chart. It is light gray,
    /// 0xF1F3F6, unless set. Color.transparent, -1, draws no panel: the one
    /// color setter where it does not leave the color as it was.
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
        var h = top() + Float(values.count) * rowHeight() + pad()
        if hasReference() {
            h += 1.4 * subtitleSize()       // The line's label under the bars
        }
        return h
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
        if !ChartScale.isValue(value) {
            return ""       // No value, as in the other charts: it was written NaN or In,fin,ity
        }
        // Its digits grouped in thousands, 23,507, as in the other charts
        return ChartScale.groupDigits(ChartScale.format(value, minFractionDigits, maxFractionDigits))
    }

    // The font of the name and the value of the bar of the index.
    private func fontOf(_ index: Int) -> Font {
        return index == highlighted() ? f1 : f2
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
        var separator = SimpleBarChart.punctuationAfter(text, ":") + " "
        for (i, name) in names.enumerated() where ChartScale.isValue(values[i]) {
            text += separator + name + " " + label(values[i])
            separator = ", "
        }
        if hasReference() {
            text += "; " + (referenceLabel.isEmpty ? "reference line at " + label(reference) : referenceText())
        }
        return text + SimpleBarChart.punctuationAfter(text, ".")
    }

    // The mark to write after the text of a description, or nothing when the
    // text ends a sentence already, in ".", "?" or "!": a title "Sales." was
    // described as "Sales.: a 5", and in a line chart as "Sales., 2019 to
    // 2024:" (the review of 28 October 2026).
    static func punctuationAfter(_ text: String, _ mark: String) -> String {
        if let end = text.unicodeScalars.last, end == "." || end == "?" || end == "!" {
            return ""
        }
        return mark
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
        if hasReference() {
            largest = Swift.max(largest, reference)     // The line within the bars' room
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
        let highlight = highlighted()
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
                let barW = Swift.max(length, 1.5)
                // The radius a fifth of the bar's height, or half a short bar
                let r = Swift.min(barHeight / 5.0, barW / 2.0)
                if roundedBars && r > 0.25 {
                    page.fillRoundedRect(barX, top, barW, barHeight, r, r)
                } else {
                    page.fillRect(barX, top, barW, barHeight)
                }
            }
            page.drawString(font, size, label(value), barX + length + size / 2.0, textBaseline,
                    Util.toRGB(highlighted ? SimpleBarChart.INK : SimpleBarChart.MUTED), nil)
        }

        // The reference line, dashed, over the bars, from a little above the
        // first to a little below the last, and its label under it. It leaves
        // out the rows where it would cross a bar's value.
        if hasReference() && largest > 0.0 && reference >= 0.0 {
            let lineX = barX + barRoom * (reference / largest)
            let lineBottom = rowTop + Float(names.count - 1) * rowHeight() + barHeight + size / 4.0
            page.setPenColor(SimpleBarChart.INK)
            page.setPenWidth(0.75)
            page.setStrokeDashPattern("[2 2] 0")
            var from = rowTop - size / 4.0
            for i in 0..<names.count {
                let value = values[i]
                let length: Float = (ChartScale.isValue(value) && value > 0.0) ? barRoom * (value / largest) : 0.0
                let labelX = barX + length + size / 2.0
                let labelW = fontOf(i).stringWidth(size, label(value))
                if labelW > 0.0 && lineX > labelX - size / 2.0 && lineX < labelX + labelW + size / 2.0 {
                    let top = rowTop + Float(i) * rowHeight()
                    if top > from {
                        page.drawLine(lineX, from, lineX, top)
                    }
                    from = top + barHeight
                }
            }
            if lineBottom > from {
                page.drawLine(lineX, from, lineX, lineBottom)
            }
            page.setDefaultStrokeDashPattern()
            let text = referenceText()
            let width = f2.stringWidth(subtitleSize(), text)
            // Centered under the line, within the chart
            let textX = Swift.max(x + pad, Swift.min(lineX - width / 2.0, x + w - pad - width))
            page.drawString(f2, subtitleSize(), text, textX, lineBottom + 1.2 * subtitleSize(),
                    Util.toRGB(SimpleBarChart.INK), nil)
        }

        page.restoreGraphicsState()
        page.setFigureBoundingBox(x, y, w, h)
        page.addEMC()
        return [x + w, y + h]
    }
}   // End of SimpleBarChart.swift
