/**
 * SimpleLineChart.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// A chart of a few lines over time, or over any categories in their order:
/// the categories left to right, each line labeled at its right end with its
/// name and its last value, so that it needs no legend; one line highlighted in
/// navy, its label bold, the others gray. A light horizontal grid alone, no
/// border, on a light panel, with a title and a subtitle at the top left, in
/// the look of SimpleBarChart and SimpleScatterChart. A value that is NaN
/// leaves a gap in its line.
///
/// In a PDF/UA document the chart is one figure, described from its data: the
/// lines, their first and last values and their highest, unless an alternate
/// description is set. It draws with the page alone, and needs no other chart.
/// Please see Example_25.
///
public class SimpleLineChart : Drawable {
    // The color of the lines that are not highlighted, unless set
    static let LINE: Int32 = 0x8C96A3

    // A line of the chart: its name and a value for each category, NaN where
    // it has none.
    private struct Line {
        let name: String
        let values: [Float]

        // Whether the line has a value at the category.
        func has(_ i: Int) -> Bool {
            return i < values.count && ChartScale.isValue(values[i])
        }
    }

    // A label at the end of a line: its text, the index of its line and the
    // baseline it is drawn at.
    private struct EndLabel {
        let text: String
        let index: Int
        var y: Float
    }

    private let f1: Font    // The title and the highlighted label
    private let f2: Font    // The rest
    private var x: Float = 0.0
    private var y: Float = 0.0
    private var w: Float = 400.0
    private var h: Float = 300.0
    private var title = ""
    private var subtitle = ""
    private var xAxisTitle = ""
    private var yAxisTitle = ""
    private var altDescription = ""
    private var categories = [String]()
    private var lines = [Line]()
    private var highlight = -1      // The index of the highlighted line, or -1
    private var markers = false
    private var yMin: Float = 0.0
    private var yMax: Float = 0.0
    private var yRangeSet = false
    private var panelColor = SimpleBarChart.PANEL
    private var lineColor = SimpleLineChart.LINE
    private var highlightColor = SimpleBarChart.HIGHLIGHT
    private let maxFractionDigits = 2

    ///
    /// Creates a simple line chart, 400 by 300 points.
    ///
    /// - Parameter f1: the font of the title and of the highlighted line's
    ///   label, a bold one.
    /// - Parameter f2: the font of the subtitle, the axis titles, the numbers,
    ///   the categories and the other labels.
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
    public func setTitle(_ title: String) -> SimpleLineChart {
        self.title = title
        return self
    }

    ///
    /// Sets the subtitle, drawn small and gray under the title.
    ///
    /// - Parameter subtitle: the subtitle.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setSubtitle(_ subtitle: String) -> SimpleLineChart {
        self.subtitle = subtitle
        return self
    }

    ///
    /// Sets the title of the categories, drawn under them.
    ///
    /// - Parameter title: the title.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setXAxisTitle(_ title: String) -> SimpleLineChart {
        self.xAxisTitle = title
        return self
    }

    ///
    /// Sets the title of the vertical axis, drawn up its left side.
    ///
    /// - Parameter title: the title.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setYAxisTitle(_ title: String) -> SimpleLineChart {
        self.yAxisTitle = title
        return self
    }

    ///
    /// Sets the alternate description of the chart in a PDF/UA document.
    /// Without one the chart is described from its data.
    ///
    /// - Parameter altDescription: the alternate description.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setAltDescription(_ altDescription: String) -> SimpleLineChart {
        self.altDescription = altDescription
        return self
    }

    ///
    /// Sets the categories, the years or the months, in their order left to
    /// right.
    ///
    /// - Parameter categories: the categories.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setCategories(_ categories: String...) -> SimpleLineChart {
        return setCategories(categories)
    }

    ///
    /// Sets the categories, the years or the months, in their order left to
    /// right, from an array.
    ///
    /// - Parameter categories: the categories.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setCategories(_ categories: [String]) -> SimpleLineChart {
        self.categories = categories
        return self
    }

    ///
    /// Adds a line: its name, and a value for each category, NaN where it has
    /// none.
    ///
    /// - Parameter name: the name of the line.
    /// - Parameter values: its values.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func addLine(_ name: String, _ values: [Float]) -> SimpleLineChart {
        lines.append(Line(name: name, values: values))
        return self
    }

    ///
    /// Highlights the line of the index, the first line 0: navy and a little
    /// wider, its label bold.
    ///
    /// - Parameter index: the index of the line.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setHighlight(_ index: Int) -> SimpleLineChart {
        self.highlight = index
        return self
    }

    ///
    /// Draws a dot at each value of the lines.
    ///
    /// - Parameter markers: true to draw them.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setMarkers(_ markers: Bool) -> SimpleLineChart {
        self.markers = markers
        return self
    }

    ///
    /// Sets the range of the vertical axis. Without it the range is that of
    /// the values, rounded to steps.
    ///
    /// - Parameter min: the first value of the axis.
    /// - Parameter max: the last value of the axis.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setYAxisRange(_ min: Float, _ max: Float) -> SimpleLineChart {
        self.yMin = min
        self.yMax = max
        self.yRangeSet = max > min
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
    public func setPanelColor(_ color: Int32) -> SimpleLineChart {
        self.panelColor = color
        return self
    }

    ///
    /// Sets the color of the lines that are not highlighted. It is gray,
    /// 0x8C96A3, unless set.
    ///
    /// - Parameter color: the color.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setLineColor(_ color: Int32) -> SimpleLineChart {
        self.lineColor = color
        return self
    }

    ///
    /// Sets the color of the highlighted line and of its label. It is navy,
    /// 0x1F3A5F, unless set.
    ///
    /// - Parameter color: the color.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setHighlightColor(_ color: Int32) -> SimpleLineChart {
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
    /// Sets the width and the height of the chart.
    ///
    /// - Parameter w: the width.
    /// - Parameter h: the height.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setSize(_ w: Float, _ h: Float) -> SimpleLineChart {
        self.w = w
        self.h = h
        return self
    }

    // The space around the chart's content, a size of its numbers.
    private func pad() -> Float {
        return f2.getSize()
    }

    // The size of the subtitle.
    private func subtitleSize() -> Float {
        return 0.8 * f2.getSize()
    }

    // The height above the plot: the padding, the title and the subtitle.
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

    // The number of categories the chart draws: the categories set, or the
    // longest line when there are none.
    private func count() -> Int {
        var n = categories.count
        if n == 0 {
            for line in lines {
                n = Swift.max(n, line.values.count)
            }
        }
        return n
    }

    // A number of the axis, with the decimals its step needs, its digits
    // grouped.
    private func label(_ value: Float, _ step: Float) -> String {
        return ChartScale.groupDigits(
                ChartScale.format(value, ChartScale.fractionDigitsOf(step, maxFractionDigits), maxFractionDigits))
    }

    // A value of a line, as its label writes it.
    private func value(_ v: Float) -> String {
        return ChartScale.groupDigits(ChartScale.format(v, 0, maxFractionDigits))
    }

    // The name of the category of the index.
    private func category(_ i: Int) -> String {
        return i < categories.count ? categories[i] : ""
    }

    // The font of the label of the line of the index.
    private func fontOf(_ index: Int) -> Font {
        return index == highlight ? f1 : f2
    }

    // The category of the last value of the line, or -1 when it has none.
    private func lastOf(_ line: Line) -> Int {
        var i = count() - 1
        while i >= 0 {
            if line.has(i) {
                return i
            }
            i -= 1
        }
        return -1
    }

    // The alternate description: the one set, or the title, the categories
    // and, for each line, its first and last values and its highest.
    func getAltDescription() -> String {
        if !altDescription.isEmpty {
            return altDescription
        }
        var sb = title
        if !subtitle.isEmpty {
            if !sb.isEmpty {
                sb += ", "
            }
            sb += subtitle
        }
        if sb.isEmpty {
            sb = "Line chart"
        }
        let n = count()
        if n > 0 && !category(0).isEmpty {
            sb += ", " + category(0) + " to " + category(n - 1)
        }
        sb += ":"
        for (i, line) in lines.enumerated() {
            var first = -1
            var last = -1
            var high = -1
            for j in 0..<n where line.has(j) {
                if first < 0 {
                    first = j
                }
                last = j
                if high < 0 || line.values[j] > line.values[high] {
                    high = j
                }
            }
            if i > 0 {
                sb += ";"
            }
            sb += " " + line.name
            if first < 0 {
                sb += " has no values"
                continue
            }
            sb += " from " + value(line.values[first]) + " to " + value(line.values[last])
            if high != first && high != last {
                sb += ", highest " + value(line.values[high])
                if !category(high).isEmpty {
                    sb += " in " + category(high)
                }
            }
        }
        return sb + "."
    }

    ///
    /// Draws the chart on the page.
    ///
    /// - Parameter page: the page to draw on.
    /// - Returns: the x and y coordinates of the bottom right corner of the chart.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        let n = count()
        var all = [Float]()
        for line in lines {
            for j in 0..<n where line.has(j) {
                all.append(line.values[j])
            }
        }
        guard let page = page, !all.isEmpty else {   // Measured, or nothing to draw
            return [x + w, y + h]
        }
        let size = f2.getSize()
        let pad = pad()
        let ascent = f2.getAscent(size)

        let axis = ChartScale.axisOf(all, yMin, yMax, yRangeSet)
        let step = (axis.max - axis.min) / Float(axis.lines)

        // The plot: right of the y title and the widest number, left of the
        // widest label at the end of a line, above the categories and the x
        // title
        var widest: Float = 0.0
        for i in 0...axis.lines {
            widest = Swift.max(widest, f2.stringWidth(size, label(axis.min + step * Float(i), step)))
        }
        var endWidth: Float = 0.0
        for (i, line) in lines.enumerated() {
            let last = lastOf(line)
            if last >= 0 {
                endWidth = Swift.max(endWidth, fontOf(i).stringWidth(size, line.name + " " + value(line.values[last])))
            }
        }
        var left = x + pad + widest + size / 2.0
        if !yAxisTitle.isEmpty {
            left += 1.8 * size      // The title, as far from the numbers as the x title is
        }
        let right = x + w - pad - endWidth - size / 2.0
        let top = y + self.top() + ascent / 2.0     // Room for the top number
        var bottom = y + h - pad - 1.4 * size
        if !xAxisTitle.isEmpty {
            bottom -= 1.4 * size
        }
        if right <= left || bottom <= top || !ChartScale.drawable(axis) {
            return [x + w, y + h]
        }
        let plotLeft = left
        let plotBottom = bottom
        func xAt(_ i: Int) -> Float {
            if n == 1 {
                return (plotLeft + right) / 2.0
            }
            return plotLeft + Float(i) * (right - plotLeft) / Float(n - 1)
        }
        func yAt(_ v: Float) -> Float {     // Held near the plot
            let v = ChartScale.held(v, axis)
            return plotBottom - (v - axis.min) * (plotBottom - top) / (axis.max - axis.min)
        }

        page.addBDC(StructElem.FIGURE, nil, getAltDescription())
        page.saveGraphicsState()

        if panelColor >= 0 {
            page.setBrushColor(panelColor)
            page.fillRect(x, y, w, h)
        }
        var baseline = y + pad
        if !title.isEmpty {
            baseline += f1.getAscent(f1.getSize())
            page.drawString(f1, f1.getSize(), title, x + pad, baseline, Util.toRGB(SimpleBarChart.INK), nil)
            baseline += f1.getDescent(f1.getSize())
        }
        if !subtitle.isEmpty {
            baseline += 1.4 * subtitleSize()
            page.drawString(f2, subtitleSize(), subtitle, x + pad, baseline, Util.toRGB(SimpleBarChart.MUTED), nil)
        }

        // The horizontal grid and its numbers, and the categories under the
        // plot, each where it leaves the one before it room
        page.setPenColor(SimpleScatterChart.GRID)
        page.setPenWidth(0.5)
        var every = ChartScale.axisLabelStep((plotBottom - top) / Float(axis.lines), f2.getBodyHeight(), axis.lines)
        for i in 0...axis.lines {
            let v = axis.min + step * Float(i)
            let gy = yAt(v)
            page.drawLine(plotLeft, gy, right, gy)
            if i % every == 0 {
                let label = self.label(v, step)
                page.drawString(f2, size, label, plotLeft - size / 2.0 - f2.stringWidth(size, label),
                        gy + ascent / 2.0, Util.toRGB(SimpleBarChart.MUTED), nil)
            }
        }
        var widestCategory: Float = 0.0
        for i in 0..<n {
            widestCategory = Swift.max(widestCategory, f2.stringWidth(size, category(i)))
        }
        every = 1
        if n > 1 {
            every = ChartScale.axisLabelStep((right - plotLeft) / Float(n - 1), widestCategory + pad, n - 1)
        }
        for i in stride(from: 0, to: n, by: every) {
            let label = category(i)
            page.drawString(f2, size, label, xAt(i) - f2.stringWidth(size, label) / 2.0,
                    plotBottom + 1.4 * size, Util.toRGB(SimpleBarChart.MUTED), nil)
        }
        if !xAxisTitle.isEmpty {
            page.drawString(f2, size, xAxisTitle, plotLeft + (right - plotLeft - f2.stringWidth(size, xAxisTitle)) / 2.0,
                    plotBottom + 2.8 * size, Util.toRGB(SimpleBarChart.INK), nil)
        }
        if !yAxisTitle.isEmpty {
            page.setTextRotation(-90)
            page.drawString(f2, size, yAxisTitle, x + pad + ascent,
                    plotBottom - (plotBottom - top - f2.stringWidth(size, yAxisTitle)) / 2.0,
                    Util.toRGB(SimpleBarChart.INK), nil)
            page.setTextRotation(0)
        }

        // The lines, the highlighted one last, over the others, broken where a
        // value is missing
        var order = [Int]()
        for i in 0..<lines.count where i != highlight {
            order.append(i)
        }
        if highlight >= 0 && highlight < lines.count {
            order.append(highlight)
        }
        // A value outside a range that is set is drawn past the plot, and the
        // lines are clipped at its edges, and as far past them as a dot is wide
        let outside = all.contains { !ChartScale.onAxis($0, axis) }
        if outside {
            let edge = 0.25 * size + 1.0
            page.saveGraphicsState()
            page.clipRect(plotLeft - edge, top - edge, right - plotLeft + 2.0 * edge, plotBottom - top + 2.0 * edge)
        }
        for i in order {
            let line = lines[i]
            let color = (i == highlight) ? highlightColor : lineColor
            page.setPenColor(color)
            page.setPenWidth((i == highlight) ? 2.0 : 1.5)
            page.setLineJoinStyle(JoinStyle.ROUND)  // No spike at a sharp peak
            var drawing = false
            for j in 0..<n {
                if !line.has(j) {
                    if drawing {
                        page.strokePath()
                    }
                    drawing = false
                    continue
                }
                if drawing {
                    page.lineTo(xAt(j), yAt(line.values[j]))
                } else {
                    page.moveTo(xAt(j), yAt(line.values[j]))
                    drawing = true
                }
            }
            if drawing {
                page.strokePath()
            }
            if markers {
                page.setBrushColor(color)
                for j in 0..<n where line.has(j) {
                    page.fillCircle(xAt(j), yAt(line.values[j]), 0.25 * size)
                }
            }
        }
        if outside {
            page.restoreGraphicsState()
        }

        // The labels at the ends of the lines, moved apart where they would
        // meet
        var labels = [EndLabel]()
        for (i, line) in lines.enumerated() {
            let last = lastOf(line)
            if last >= 0 {
                // At the end of its line, or at the edge of the plot it is past
                let end = Swift.max(top, Swift.min(plotBottom, yAt(line.values[last])))
                labels.append(EndLabel(text: line.name + " " + value(line.values[last]), index: i,
                        y: end + ascent / 2.0))
            }
        }
        // By the baseline, and labels on one baseline in the order of their
        // lines, as a stable sort leaves them
        labels.sort { $0.y != $1.y ? $0.y < $1.y : $0.index < $1.index }
        if labels.count > 1 {
            for k in 1..<labels.count {
                labels[k].y = Swift.max(labels[k].y, labels[k - 1].y + f2.getBodyHeight())
            }
        }
        for label in labels {
            let color = (label.index == highlight) ? highlightColor : SimpleBarChart.MUTED
            page.drawString(fontOf(label.index), size, label.text, right + size / 2.0, label.y, Util.toRGB(color), nil)
        }

        page.restoreGraphicsState()
        page.setFigureBoundingBox(x, y, w, h)
        page.addEMC()
        return [x + w, y + h]
    }
}   // End of SimpleLineChart.swift
