/**
 * BarChart.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Bar chart renderer for PDF pages: one slot per category, the bars of the
/// series grouped inside each slot. See Example_39 (horizontal bars) and
/// Example_40 (vertical bars).
///
public class BarChart : Drawable {
    /// One series of the chart: a name, a value per category and a color.
    private struct Series {
        let name: String
        let values: [Float]
        let color: Int32
    }

    private static let NO_COLOR: Int32 = -1

    private var x1: Float = 0.0
    private var y1: Float = 0.0
    private var w: Float = 300.0
    private var h: Float = 200.0

    private var title = ""
    private var xAxisTitle = ""
    private var yAxisTitle = ""

    private var categories = [String]()
    private var series = [Series]()

    private var horizontal = false
    private var groupGap: Float = 0.3
    private var barGap: Float = 0.0

    private var drawGridLines = true
    private var drawValueLabels = false
    private var drawLegend = true

    private var gridLineWidth: Float = 0.0
    private var gridLineDashPattern = "[1 1] 0"
    private var axisLineWidth: Float = 0.5
    private var chartBorderWidth: Float = 0.0
    private var innerBorderWidth: Float = 0.0

    private var minFractionDigits = 0
    private var maxFractionDigits = 2

    // Value axis range; auto-computed from the data unless gridLines > 0
    private var min: Float = 0.0
    private var max: Float = 0.0
    private var gridLines = 0

    // f1 = chart title font, f2 = axis titles, labels and legend font
    private let f1: Font
    private let f2: Font

    ///
    /// Creates a bar chart.
    ///
    /// - Parameter f1: the font for the chart title.
    /// - Parameter f2: the font for the axis titles, the labels and the legend.
    ///
    public init(_ f1: Font, _ f2: Font) {
        self.f1 = f1
        self.f2 = f2
    }

    ///
    /// Sets the chart title.
    ///
    /// - Parameter title: the title.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setTitle(_ title: String) -> BarChart {
        self.title = title
        return self
    }

    ///
    /// Sets the X axis title.
    ///
    /// - Parameter title: the title.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setXAxisTitle(_ title: String) -> BarChart {
        self.xAxisTitle = title
        return self
    }

    ///
    /// Sets the Y axis title.
    ///
    /// - Parameter title: the title.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setYAxisTitle(_ title: String) -> BarChart {
        self.yAxisTitle = title
        return self
    }

    ///
    /// Sets the categories, one per group of bars, in the order they are drawn:
    /// left to right in a vertical chart, top to bottom in a horizontal one.
    ///
    /// - Parameter categories: the category labels.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setCategories(_ categories: String...) -> BarChart {
        self.categories = categories
        return self
    }

    ///
    /// Adds a series drawn in the next color of the default palette.
    ///
    /// - Parameter name: the series name, shown in the legend; empty for none.
    /// - Parameter values: one value per category.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func addSeries(_ name: String, _ values: [Float]) -> BarChart {
        return addSeries(name, values, BarChart.NO_COLOR)
    }

    ///
    /// Adds a series drawn in the specified color.
    ///
    /// - Parameter name: the series name, shown in the legend; empty for none.
    /// - Parameter values: one value per category.
    /// - Parameter color: the bar color as a 0xRRGGBB value, for example Color.blue.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func addSeries(_ name: String, _ values: [Float], _ color: Int32) -> BarChart {
        series.append(Series(name: name, values: values, color: color))
        return self
    }

    ///
    /// Sets the location of the top left corner of this chart.
    ///
    /// - Parameter x: the x coordinate.
    /// - Parameter y: the y coordinate.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x1 = x
        self.y1 = y
        return self
    }

    ///
    /// Sets the size of this chart.
    ///
    /// - Parameter w: the width.
    /// - Parameter h: the height.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setSize(_ w: Float, _ h: Float) -> BarChart {
        self.w = w
        self.h = h
        return self
    }

    ///
    /// Sets whether the bars are horizontal. The default is vertical bars.
    ///
    /// - Parameter horizontal: true for horizontal bars.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setHorizontal(_ horizontal: Bool) -> BarChart {
        self.horizontal = horizontal
        return self
    }

    ///
    /// Sets the gap between the groups of bars as a fraction of the category
    /// slot, from 0.0 to below 1.0. The default is 0.3.
    ///
    /// - Parameter gap: the gap.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setGroupGap(_ gap: Float) -> BarChart {
        self.groupGap = gap
        return self
    }

    ///
    /// Sets the gap between the bars of a group as a fraction of the bar width.
    /// The default is 0.0, so the bars of a group touch.
    ///
    /// - Parameter gap: the gap.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setBarGap(_ gap: Float) -> BarChart {
        self.barGap = gap
        return self
    }

    ///
    /// Sets whether the grid lines of the value axis are drawn.
    ///
    /// - Parameter drawGridLines: true to draw them.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setDrawGridLines(_ drawGridLines: Bool) -> BarChart {
        self.drawGridLines = drawGridLines
        return self
    }

    ///
    /// Sets whether the value of each bar is written at its end.
    ///
    /// - Parameter drawValueLabels: true to write the values.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setDrawValueLabels(_ drawValueLabels: Bool) -> BarChart {
        self.drawValueLabels = drawValueLabels
        return self
    }

    ///
    /// Sets whether the legend is drawn. The legend lists the series that have
    /// a name, under the title.
    ///
    /// - Parameter drawLegend: true to draw the legend.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setDrawLegend(_ drawLegend: Bool) -> BarChart {
        self.drawLegend = drawLegend
        return self
    }

    ///
    /// Sets the width of the grid lines. A width of 0 draws the thinnest line
    /// a viewer shows; setDrawGridLines(false) hides them.
    ///
    /// - Parameter width: the line width.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setGridLineWidth(_ width: Float) -> BarChart {
        self.gridLineWidth = width
        return self
    }

    ///
    /// Sets the dash pattern of the grid lines, for example "[1 1] 0".
    ///
    /// - Parameter pattern: the dash pattern.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setGridLineDashPattern(_ pattern: String) -> BarChart {
        self.gridLineDashPattern = pattern
        return self
    }

    ///
    /// Sets the width of the axis lines. The default is 0.5.
    ///
    /// - Parameter width: the line width.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setAxisLineWidth(_ width: Float) -> BarChart {
        self.axisLineWidth = width
        return self
    }

    ///
    /// Sets the width of the outer chart border. A width of 0, the default,
    /// hides it.
    ///
    /// - Parameter width: the border width.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setChartBorderWidth(_ width: Float) -> BarChart {
        self.chartBorderWidth = width
        return self
    }

    ///
    /// Sets the width of the plot area border. A width of 0, the default,
    /// hides it.
    ///
    /// - Parameter width: the border width.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setInnerBorderWidth(_ width: Float) -> BarChart {
        self.innerBorderWidth = width
        return self
    }

    ///
    /// Sets the minimum number of decimal places in the value labels. The axis
    /// labels have at least the decimal places of the axis step. The default is 0.
    ///
    /// - Parameter minFractionDigits: the minimum number of decimal places.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setMinimumFractionDigits(_ minFractionDigits: Int) -> BarChart {
        self.minFractionDigits = minFractionDigits
        return self
    }

    ///
    /// Sets the maximum number of decimal places in the value labels. The
    /// default is 2.
    ///
    /// - Parameter maxFractionDigits: the maximum number of decimal places.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setMaximumFractionDigits(_ maxFractionDigits: Int) -> BarChart {
        self.maxFractionDigits = maxFractionDigits
        return self
    }

    ///
    /// Sets the range and the number of grid lines of the value axis. Without
    /// it the range is computed from the data and always includes 0.
    ///
    /// - Parameter min: the value at the start of the axis.
    /// - Parameter max: the value at the end of the axis.
    /// - Parameter gridLines: the number of grid lines, at least 1.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setValueAxisMinMax(_ min: Float, _ max: Float, _ gridLines: Int) -> BarChart {
        self.min = min
        self.max = max
        self.gridLines = gridLines
        return self
    }

    ///
    /// Draws this chart on the specified page.
    ///
    /// - Parameter page: the page to draw on.
    /// - Returns: the bottom right corner coordinates [x, y].
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        let n = numberOfCategories()
        if n == 0 || page == nil {
            return [x1 + w, y1 + h]
        }
        let page = page!

        var vMin: Float
        var vMax: Float
        var lines: Int
        if gridLines > 0 {
            vMin = min
            vMax = max
            lines = gridLines
        } else {
            var lo: Float = 0.0
            var hi: Float = 0.0
            for s in series {
                for v in s.values {
                    lo = Swift.min(lo, v)
                    hi = Swift.max(hi, v)
                }
            }
            if hi == lo { hi = lo + 1.0 }
            let round = Chart.roundMaxAndMinValues(hi, lo)
            vMin = round.minValue
            vMax = round.maxValue
            lines = round.numOfGridLines
        }
        if vMax == vMin { vMax = vMin + 1.0 }
        let step = (vMax - vMin) / Float(lines)
        let axisDigits = Swift.max(minFractionDigits, Chart.fractionDigitsOf(step, maxFractionDigits))
        let base = Swift.min(Swift.max(0.0, vMin), vMax)

        let x2 = x1 + w
        let y2 = y1 + h
        let bodyHeight = f2.getBodyHeight()
        let ascent = f2.getAscent()
        let pad = bodyHeight / 2.0
        let legend = drawLegend && hasSeriesNames()

        // Widest labels on the value axis and next to the bars
        var widestAxisLabel: Float = 0.0
        for i in 0...lines {
            let label = Chart.format(vMin + step * Float(i), axisDigits, maxFractionDigits)
            widestAxisLabel = Swift.max(widestAxisLabel, f2.stringWidth(label))
        }
        var widestValueLabel: Float = 0.0
        if drawValueLabels {
            for s in series {
                for v in s.values {
                    widestValueLabel = Swift.max(widestValueLabel, f2.stringWidth(valueLabel(v)))
                }
            }
        }
        var widestCategory: Float = 0.0
        for category in categories {
            widestCategory = Swift.max(widestCategory, f2.stringWidth(category))
        }

        // Margins and the plot area
        var topMargin = 2.5 * f1.getBodyHeight() + (legend ? 1.5 * bodyHeight : 0.0)
        let leftMargin = 1.5 * bodyHeight + pad + (horizontal ? widestCategory : widestAxisLabel)
        var rightMargin = pad
        let bottomMargin = 2.5 * bodyHeight
        if horizontal {
            rightMargin += Swift.max(widestAxisLabel / 2.0, widestValueLabel + pad)
        } else if drawValueLabels {
            topMargin += bodyHeight
        }
        let x5 = x1 + leftMargin
        let y5 = y1 + topMargin
        let x6 = x2 - rightMargin
        let y8 = y2 - bottomMargin

        // Title, then the legend under it
        page.setBrushColor(Color.black)
        page.drawString(f1, f1.getSize(), title, x1 + (w - f1.stringWidth(title)) / 2.0, y1 + 1.5 * f1.getBodyHeight())
        if legend {
            drawLegend(page, y1 + 1.5 * f1.getBodyHeight() + 1.5 * bodyHeight)
        }

        if chartBorderWidth > 0.0 {
            page.setPenColor(Color.black)
            page.setPenWidth(chartBorderWidth)
            page.setDefaultStrokeDashPattern()
            page.drawRect(x1, y1, w, h)
        }

        // Grid lines and value axis labels
        for i in 0...lines {
            let v = vMin + step * Float(i)
            let label = Chart.format(v, axisDigits, maxFractionDigits)
            if horizontal {
                let x = x5 + (v - vMin) * (x6 - x5) / (vMax - vMin)
                if drawGridLines {
                    gridLine(page, x, y5, x, y8)
                }
                page.drawString(f2, f2.getSize(), label, x - f2.stringWidth(label) / 2.0, y8 + bodyHeight)
            } else {
                let y = y8 - (v - vMin) * (y8 - y5) / (vMax - vMin)
                if drawGridLines {
                    gridLine(page, x5, y, x6, y)
                }
                page.drawString(f2, f2.getSize(), label, x5 - pad - f2.stringWidth(label), y + ascent / 2.0)
            }
        }

        // Bars, category labels and value labels
        let m = series.count
        let slot = (horizontal ? (y8 - y5) : (x6 - x5)) / Float(n)
        let groupWidth = slot * (1.0 - groupGap)
        let barWidth = m == 0 ? groupWidth : groupWidth / (Float(m) + Float(m - 1) * barGap)
        for i in 0..<n {
            let slotStart = (horizontal ? y5 : x5) + Float(i) * slot
            let groupStart = slotStart + slot * groupGap / 2.0
            let category = i < categories.count ? categories[i] : ""
            page.setBrushColor(Color.black)
            if horizontal {
                page.drawString(f2, f2.getSize(), category, x5 - pad - f2.stringWidth(category),
                        slotStart + slot / 2.0 + ascent / 2.0)
            } else {
                page.drawString(f2, f2.getSize(), category, slotStart + (slot - f2.stringWidth(category)) / 2.0,
                        y8 + bodyHeight)
            }
            for j in 0..<m {
                let s = series[j]
                if i >= s.values.count {
                    continue
                }
                let v = Swift.min(Swift.max(s.values[i], vMin), vMax)
                let barStart = groupStart + Float(j) * barWidth * (1.0 + barGap)
                page.setBrushColor(s.color == BarChart.NO_COLOR ? Chart.DEFAULT_PALETTE[j % Chart.DEFAULT_PALETTE.count] : s.color)
                if horizontal {
                    let x0 = x5 + (base - vMin) * (x6 - x5) / (vMax - vMin)
                    let x = x5 + (v - vMin) * (x6 - x5) / (vMax - vMin)
                    page.fillRect(Swift.min(x0, x), barStart, abs(x - x0), barWidth)
                    if drawValueLabels {
                        let label = valueLabel(s.values[i])
                        page.setBrushColor(Color.black)
                        page.drawString(f2, f2.getSize(), label,
                                v >= base ? x + pad / 2.0 : x - pad / 2.0 - f2.stringWidth(label),
                                barStart + barWidth / 2.0 + ascent / 2.0)
                    }
                } else {
                    let y0 = y8 - (base - vMin) * (y8 - y5) / (vMax - vMin)
                    let y = y8 - (v - vMin) * (y8 - y5) / (vMax - vMin)
                    page.fillRect(barStart, Swift.min(y0, y), barWidth, abs(y - y0))
                    if drawValueLabels {
                        let label = valueLabel(s.values[i])
                        page.setBrushColor(Color.black)
                        page.drawString(f2, f2.getSize(), label, barStart + (barWidth - f2.stringWidth(label)) / 2.0,
                                v >= base ? y - pad / 2.0 : y + ascent + pad / 2.0)
                    }
                }
            }
        }

        // Axis lines: along the labels and at the base of the bars
        page.setPenColor(Color.black)
        page.setPenWidth(axisLineWidth)
        page.setDefaultStrokeDashPattern()
        if horizontal {
            let x0 = x5 + (base - vMin) * (x6 - x5) / (vMax - vMin)
            page.drawLine(x5, y8, x6, y8)
            page.drawLine(x0, y5, x0, y8)
        } else {
            let y0 = y8 - (base - vMin) * (y8 - y5) / (vMax - vMin)
            page.drawLine(x5, y5, x5, y8)
            page.drawLine(x5, y0, x6, y0)
        }
        if innerBorderWidth > 0.0 {
            page.setPenWidth(innerBorderWidth)
            page.drawRect(x5, y5, x6 - x5, y8 - y5)
        }

        // Axis titles
        page.setBrushColor(Color.black)
        page.setTextRotation(90)
        page.drawString(f2, f2.getSize(), yAxisTitle, x1 + bodyHeight, y8 - ((y8 - y5) - f2.stringWidth(yAxisTitle)) / 2.0)
        page.setTextRotation(0)
        page.drawString(f2, f2.getSize(), xAxisTitle, x5 + ((x6 - x5) - f2.stringWidth(xAxisTitle)) / 2.0, y2 - bodyHeight / 2.0)

        page.setDefaultPenWidth()
        page.setDefaultStrokeDashPattern()
        page.setPenColor(Color.black)

        return [x1 + w, y1 + h]
    }

    /// Returns the number of category slots: the categories or the longest series.
    private func numberOfCategories() -> Int {
        var n = categories.count
        for s in series {
            n = Swift.max(n, s.values.count)
        }
        return n
    }

    /// Returns true if a series has a name to list in the legend.
    private func hasSeriesNames() -> Bool {
        for s in series {
            if !s.name.isEmpty {
                return true
            }
        }
        return false
    }

    /// Formats the value written at the end of a bar.
    private func valueLabel(_ value: Float) -> String {
        return Chart.format(value, minFractionDigits, maxFractionDigits)
    }

    /// Draws one dotted grid line.
    private func gridLine(_ page: Page, _ xa: Float, _ ya: Float, _ xb: Float, _ yb: Float) {
        page.setPenColor(Color.black)
        page.setPenWidth(gridLineWidth)
        page.setStrokeDashPattern(gridLineDashPattern)
        page.drawLine(xa, ya, xb, yb)
    }

    /// Draws the legend centered on the chart, with a swatch before each series name.
    private func drawLegend(_ page: Page, _ baseline: Float) {
        let swatch = f2.getAscent()
        let gap = swatch / 2.0
        var width: Float = 0.0
        var entries = 0
        for s in series {
            if !s.name.isEmpty {
                width += swatch + gap + f2.stringWidth(s.name)
                entries += 1
            }
        }
        width += Float(entries - 1) * f2.getBodyHeight()
        var x = x1 + (w - width) / 2.0
        for j in 0..<series.count {
            let s = series[j]
            if s.name.isEmpty {
                continue
            }
            page.setBrushColor(s.color == BarChart.NO_COLOR ? Chart.DEFAULT_PALETTE[j % Chart.DEFAULT_PALETTE.count] : s.color)
            page.fillRect(x, baseline - swatch, swatch, swatch)
            x += swatch + gap
            page.setBrushColor(Color.black)
            page.drawString(f2, f2.getSize(), s.name, x, baseline)
            x += f2.stringWidth(s.name) + f2.getBodyHeight()
        }
    }
}   // End of BarChart.swift
