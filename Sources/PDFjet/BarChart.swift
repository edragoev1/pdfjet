/**
 * BarChart.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Bar chart renderer for PDF pages: one slot per category, the bars of the
/// series grouped inside each slot. See Example_27, of horizontal and
/// vertical bars.
///
public class BarChart : Drawable {
    /// One series of the chart: a name, a value per category and a color, or
    /// a color per category.
    private struct Series {
        let name: String
        let values: [Float]
        let color: Int32
        let colors: [Int32]?    // nil unless each bar has its own color
    }

    private static let NO_COLOR: Int32 = -1

    // The look of the simple charts: ink for the title and the values, a
    // muted gray for the rest of the text, light grid lines, and a palette
    // whose colors differ in lightness, so that they stay apart in grayscale
    // and for a reader who does not tell red from green.
    private static let INK = SimpleBarChart.INK
    private static let MUTED = SimpleBarChart.MUTED
    private static let GRID: Int32 = 0xE1E4E8
    private static let BASELINE: Int32 = 0x8C96A3
    static let PALETTE: [Int32] = [
        0x1F3A5F,   // navy
        0x6F95C2,   // steel blue
        0xD08C3C,   // amber
        0x76987C,   // sage
        0xA7AFB8,   // gray
        0x9C5B5B,   // brick
        0x4F7A8C,   // slate
        0xC9B458    // ochre
    ]

    private var x1: Float = 0.0
    private var y1: Float = 0.0
    private var w: Float = 300.0
    private var h: Float = 200.0

    private var title = ""
    private var subtitle = ""
    private var altDescription: String?
    private var xAxisTitle = ""
    private var yAxisTitle = ""

    private var categories = [String]()
    private var series = [Series]()
    private var lineSeries = [Series]()     // Drawn as lines over the bars

    private var horizontal = false
    private var stacked = false
    private var groupGap: Float = 0.3
    private var barGap: Float = 0.0

    private var drawGridLines = true
    private var drawValueLabels = false
    private var valueLabelsInside = false
    private var drawLegend = true
    private var groupingUsed = true

    private var gridLineColor: Int32 = BarChart.GRID
    private var gridLineWidth: Float = 0.5
    private var gridLineDashPattern = "[] 0"
    private var axisLineWidth: Float = 0.75
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
    /// Sets the subtitle, written in gray under the title in the second font.
    ///
    /// - Parameter subtitle: the subtitle.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setSubtitle(_ subtitle: String) -> BarChart {
        self.subtitle = subtitle
        return self
    }

    ///
    /// Sets the alternate description of the chart, which a screen reader reads
    /// in a PDF/UA document, where the chart is a figure. The default is the
    /// title and the subtitle, or "Bar chart" without them, and the values of
    /// each series by category. Describe what the chart shows.
    ///
    /// - Parameter altDescription: the alternate description.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setAltDescription(_ altDescription: String) -> BarChart {
        self.altDescription = altDescription
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
        return setCategories(categories)
    }

    ///
    /// Sets the categories, one per group of bars, in the order they are drawn:
    /// left to right in a vertical chart, top to bottom in a horizontal one.
    ///
    /// - Parameter categories: the category labels.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setCategories(_ categories: [String]) -> BarChart {
        self.categories = categories
        return self
    }

    ///
    /// Adds a series drawn in the next color of the default palette. A value
    /// that is NaN or infinite has no bar.
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
        series.append(Series(name: name, values: values, color: color, colors: nil))
        return self
    }

    ///
    /// Adds a series with a color per category, for a chart whose bars each
    /// have their own color. A bar past the end of the colors has the next
    /// color of the default palette.
    ///
    /// - Parameter name: the series name, shown in the legend; empty for none.
    /// - Parameter values: one value per category.
    /// - Parameter colors: one 0xRRGGBB color per category.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func addSeries(_ name: String, _ values: [Float], _ colors: [Int32]) -> BarChart {
        series.append(Series(name: name, values: values, color: BarChart.NO_COLOR, colors: colors))
        return self
    }

    ///
    /// Adds a series drawn as a line over the bars, in ink, a dot at each
    /// category: a target, the average, or last year. It shares the value
    /// axis with the bars, is not stacked, and is in the legend and the
    /// description. A value that is NaN or infinite leaves a gap in the line.
    ///
    /// - Parameter name: the series name, shown in the legend; empty for none.
    /// - Parameter values: one value per category.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func addLineSeries(_ name: String, _ values: [Float]) -> BarChart {
        return addLineSeries(name, values, BarChart.INK)
    }

    ///
    /// Adds a series drawn as a line over the bars, in the specified color.
    ///
    /// - Parameter name: the series name, shown in the legend; empty for none.
    /// - Parameter values: one value per category.
    /// - Parameter color: the line color as a 0xRRGGBB value.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func addLineSeries(_ name: String, _ values: [Float], _ color: Int32) -> BarChart {
        lineSeries.append(Series(name: name, values: values, color: color, colors: nil))
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
    /// Sets whether the series are stacked: one bar per category, with the
    /// value of each series as a segment of it. The values above 0 stack up
    /// from 0 and the values below 0 stack down. The default is grouped bars.
    ///
    /// - Parameter stacked: true for stacked bars.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setStacked(_ stacked: Bool) -> BarChart {
        self.stacked = stacked
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
    public func setGridLines(_ drawGridLines: Bool) -> BarChart {
        self.drawGridLines = drawGridLines
        return self
    }

    ///
    /// Sets whether the grid lines of the value axis are drawn.
    ///
    /// - Parameter drawGridLines: true to draw them.
    /// - Returns: this BarChart object.
    ///
    @available(*, deprecated, renamed: "setGridLines",
            message: "As of 9.1.5, use setGridLines, the name the other charts use for what they draw.")
    @discardableResult
    public func setDrawGridLines(_ drawGridLines: Bool) -> BarChart {
        return setGridLines(drawGridLines)
    }

    ///
    /// Sets whether the value of each bar is written at its end, or, in a
    /// stacked chart, inside each segment that has room for it.
    ///
    /// - Parameter drawValueLabels: true to write the values.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setValueLabels(_ drawValueLabels: Bool) -> BarChart {
        self.drawValueLabels = drawValueLabels
        return self
    }

    ///
    /// Sets whether the value of each bar is written at its end, or, in a
    /// stacked chart, inside each segment that has room for it.
    ///
    /// - Parameter drawValueLabels: true to write the values.
    /// - Returns: this BarChart object.
    ///
    @available(*, deprecated, renamed: "setValueLabels",
            message: "As of 9.1.5, use setValueLabels, as SimpleLineChart's.")
    @discardableResult
    public func setDrawValueLabels(_ drawValueLabels: Bool) -> BarChart {
        return setValueLabels(drawValueLabels)
    }

    ///
    /// Sets whether the value labels are written inside the bars, at the end
    /// of each bar, in white or in ink, whichever reads on the bar's color,
    /// instead of next to the bar ends. A bar too short for its label gets it
    /// next to its end. The default is false.
    ///
    /// - Parameter valueLabelsInside: true to write the values inside the bars.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setValueLabelsInside(_ valueLabelsInside: Bool) -> BarChart {
        self.valueLabelsInside = valueLabelsInside
        return self
    }

    ///
    /// Sets whether the labels group the digits in thousands with a comma, as
    /// in 6,650. The default is true.
    ///
    /// - Parameter groupingUsed: true to group the digits.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setGroupingUsed(_ groupingUsed: Bool) -> BarChart {
        self.groupingUsed = groupingUsed
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
    public func setLegend(_ drawLegend: Bool) -> BarChart {
        self.drawLegend = drawLegend
        return self
    }

    ///
    /// Sets whether the legend is drawn. The legend lists the series that have
    /// a name, under the title.
    ///
    /// - Parameter drawLegend: true to draw the legend.
    /// - Returns: this BarChart object.
    ///
    @available(*, deprecated, renamed: "setLegend",
            message: "As of 9.1.5, use setLegend, as DonutChart's.")
    @discardableResult
    public func setDrawLegend(_ drawLegend: Bool) -> BarChart {
        return setLegend(drawLegend)
    }

    ///
    /// Sets the width of the grid lines. The default is 0.5; a width of 0
    /// draws the thinnest line a viewer shows, and setDrawGridLines(false)
    /// hides them.
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
    /// Sets the color of the grid lines. The default is light gray, 0xE1E4E8.
    /// Color.transparent leaves it unchanged.
    ///
    /// - Parameter color: the color as a 0xRRGGBB value, for example Color.lightgray.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setGridLineColor(_ color: Int32) -> BarChart {
        if color == Color.transparent {
            return self
        }
        self.gridLineColor = color
        return self
    }

    ///
    /// Sets the dash pattern of the grid lines, for example "[1 1] 0" for a
    /// dotted line. The default is "[] 0", a solid line.
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
    /// Sets the width of the axis line, the line at the base of the bars: at 0
    /// on the value axis, or at its lowest value when the axis does not reach
    /// 0. The default is 0.75; 0 hides it.
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
    /// Draws this chart on the specified page. A category name wider than
    /// two fifths of a horizontal chart is shortened with an ellipsis, and a
    /// chart too small for its plot draws nothing.
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
            if stacked {
                // The sums of the values above and below 0 in each category
                for i in 0..<n {
                    var up: Float = 0.0
                    var down: Float = 0.0
                    for s in series {
                        if i < s.values.count && ChartScale.isValue(s.values[i]) {
                            if s.values[i] >= 0.0 { up += s.values[i] } else { down += s.values[i] }
                        }
                    }
                    lo = Swift.min(lo, down)
                    hi = Swift.max(hi, up)
                }
                for s in lineSeries {
                    for v in s.values where ChartScale.isValue(v) {
                        lo = Swift.min(lo, v)
                        hi = Swift.max(hi, v)
                    }
                }
            } else {
                for s in allSeries() {
                    for v in s.values where ChartScale.isValue(v) {
                        lo = Swift.min(lo, v)
                        hi = Swift.max(hi, v)
                    }
                }
            }
            if hi == lo { hi = lo + ChartScale.flatDataSpan(lo) }
            let round = ChartScale.roundMaxAndMinValues(hi, lo)
            vMin = round.minValue
            vMax = round.maxValue
            lines = round.numOfGridLines
        }
        if vMax == vMin { vMax = vMin + ChartScale.flatDataSpan(vMin) }
        let step = (vMax - vMin) / Float(lines)
        let axisDigits = Swift.max(minFractionDigits, ChartScale.fractionDigitsOf(step, maxFractionDigits))
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
            let label = axisLabel(vMin + step * Float(i), axisDigits)
            widestAxisLabel = Swift.max(widestAxisLabel, f2.stringWidth(label))
        }
        var widestValueLabel: Float = 0.0
        if drawValueLabels {
            for s in series {
                for v in s.values where ChartScale.isValue(v) {
                    widestValueLabel = Swift.max(widestValueLabel, f2.stringWidth(valueLabel(v)))
                }
            }
        }
        // The totals of the stacks, written past their ends
        let totals = drawValueLabels && stacked && series.count > 1
        var widestTotal: Float = 0.0
        if totals {
            for i in 0..<n {
                var up: Float = 0.0
                for s in series where i < s.values.count && ChartScale.isValue(s.values[i]) && s.values[i] >= 0.0 {
                    up += s.values[i]
                }
                if up > 0.0 {
                    widestTotal = Swift.max(widestTotal, f2.stringWidth(valueLabel(up)))
                }
            }
        }
        // The names of the categories, shortened at the left of horizontal bars
        var names = [String]()
        var widestCategory: Float = 0.0
        for category in categories {
            // Two fifths of a horizontal chart at most, a quarter under vertical bars
            let name = ChartScale.shorten(f2, f2.getSize(), category, (horizontal ? 0.4 : 0.25) * w)
            names.append(name)
            widestCategory = Swift.max(widestCategory, f2.stringWidth(name))
        }

        // Margins and the plot area
        // The title, the subtitle and the legend, its rows wrapped at the
        // right edge; no room for a title the chart has not
        let titleBaseline = y1 + (title.isEmpty ? 0.0 : 1.5 * f1.getBodyHeight())
        let subtitleHeight: Float = subtitle.isEmpty ? 0.0 : bodyHeight
        let legendRows = legend ? drawLegend(nil, 0.0) : 0
        var topMargin = (title.isEmpty ? f1.getBodyHeight() : 2.5 * f1.getBodyHeight()) + subtitleHeight
                + Float(legendRows) * (1.5 * bodyHeight)
        let leftMargin = 1.5 * bodyHeight + pad + (horizontal ? widestCategory : widestAxisLabel)
        var rightMargin = pad
        let bottomMargin = 2.5 * bodyHeight
        if horizontal {
            let past: Float = totals ? widestTotal + pad : (valueLabelsInside ? 0.0 : widestValueLabel + pad)
            rightMargin += Swift.max(widestAxisLabel / 2.0, past)
        } else if totals || (drawValueLabels && !stacked && !valueLabelsInside) {
            topMargin += bodyHeight
        }
        let x5 = x1 + leftMargin
        let y5 = y1 + topMargin
        let x6 = x2 - rightMargin
        let y8 = y2 - bottomMargin
        if x6 <= x5 || y8 <= y5 {   // No room for the plot
            return [x1 + w, y1 + h]
        }

        // The chart is one figure, described by its alternate description.
        page.addBDC(StructElem.FIGURE, nil, getAltDescription())
        // The pen, the brush and the dash pattern of the caller are kept,
        // as a Stamp keeps them.
        page.saveGraphicsState()
        page.setDefaultStrokeDashPattern()      // Not the caller's, which dashed the legend's lines

        // Title, the subtitle and then the legend under it, at the left
        // The title and the subtitle shortened to the width of the chart
        BarChart.drawText(page, f1, ChartScale.shorten(f1, f1.getSize(), title, w), x1, titleBaseline, BarChart.INK)
        if !subtitle.isEmpty {
            BarChart.drawText(page, f2, ChartScale.shorten(f2, f2.getSize(), subtitle, w),
                    x1, titleBaseline + subtitleHeight, BarChart.MUTED)
        }
        if legend {
            drawLegend(page, titleBaseline + subtitleHeight + 1.5 * bodyHeight)
        }

        if chartBorderWidth > 0.0 {
            page.setPenColor(BarChart.BASELINE)
            page.setPenWidth(chartBorderWidth)
            page.setDefaultStrokeDashPattern()
            page.drawRect(x1, y1, w, h)
        }

        // Grid lines and value axis labels. A label is drawn at every grid line
        // that leaves it room, the widest label and a pad across, or a line high
        // up the vertical axis: at every second or third line, or more, when
        // the labels would run into each other, the lines all drawn.
        let every = horizontal
                ? ChartScale.axisLabelStep((x6 - x5) / Float(lines), widestAxisLabel + pad, lines)
                : ChartScale.axisLabelStep((y8 - y5) / Float(lines), bodyHeight, lines)
        for i in 0...lines {
            let v = vMin + step * Float(i)
            let label = axisLabel(v, axisDigits)
            let labeled = i % every == 0
            if horizontal {
                let x = x5 + ChartScale.scaled(v - vMin, x6 - x5, vMax - vMin)
                if drawGridLines {
                    gridLine(page, x, y5, x, y8)
                }
                if labeled {
                    BarChart.drawText(page, f2, label, x - f2.stringWidth(label) / 2.0, y8 + bodyHeight, BarChart.MUTED)
                }
            } else {
                let y = y8 - ChartScale.scaled(v - vMin, y8 - y5, vMax - vMin)
                if drawGridLines {
                    gridLine(page, x5, y, x6, y)
                }
                if labeled {
                    BarChart.drawText(page, f2, label, x5 - pad - f2.stringWidth(label), y + ascent / 2.0, BarChart.MUTED)
                }
            }
        }

        // Bars, category labels and value labels
        let m = series.count
        let slot = (horizontal ? (y8 - y5) : (x6 - x5)) / Float(n)
        let groupWidth = slot * (1.0 - groupGap)
        let barWidth = (m == 0 || stacked) ? groupWidth : groupWidth / (Float(m) + Float(m - 1) * barGap)
        // Under vertical bars a category is written where it leaves the one
        // before it room: at every second or third bar, or more
        let everyCategory = horizontal ? 1 : ChartScale.axisLabelStep(slot, widestCategory + pad, n)
        for i in 0..<n {
            let slotStart = (horizontal ? y5 : x5) + Float(i) * slot
            let groupStart = slotStart + slot * groupGap / 2.0
            let category = i < names.count ? names[i] : ""
            if horizontal {
                BarChart.drawText(page, f2, category, x5 - pad - f2.stringWidth(category),
                        slotStart + slot / 2.0 + ascent / 2.0, BarChart.MUTED)
            } else if i % everyCategory == 0 {
                BarChart.drawText(page, f2, category, slotStart + (slot - f2.stringWidth(category)) / 2.0,
                        y8 + bodyHeight, BarChart.MUTED)
            }
            var up: Float = 0.0      // the stacked values above 0 so far
            var down: Float = 0.0    // the stacked values below 0 so far
            for j in 0..<m {
                let s = series[j]
                if i >= s.values.count || !ChartScale.isValue(s.values[i]) {
                    continue
                }
                // A bar runs from the base to its value; a segment of a stack
                // from the sum of the segments before it to that sum plus its value
                var from = base
                var to = s.values[i]
                if stacked {
                    if to >= 0.0 { from = up; up += to } else { from = down; down += to }
                    to = from + s.values[i]
                }
                from = Swift.min(Swift.max(from, vMin), vMax)
                to = Swift.min(Swift.max(to, vMin), vMax)
                let barStart = stacked ? groupStart : groupStart + Float(j) * barWidth * (1.0 + barGap)
                let color = barColor(s, j, i)
                page.setBrushColor(color)
                let label: String? = drawValueLabels ? valueLabel(s.values[i]) : nil
                let on = BarChart.labelColorOn(color)
                if horizontal {
                    let x0 = x5 + ChartScale.scaled(from - vMin, x6 - x5, vMax - vMin)
                    let x = x5 + ChartScale.scaled(to - vMin, x6 - x5, vMax - vMin)
                    page.fillRect(Swift.min(x0, x), barStart, abs(x - x0), barWidth)
                    let baseline = barStart + barWidth / 2.0 + ascent / 2.0
                    if let label = label, stacked {
                        if abs(x - x0) >= f2.stringWidth(label) + pad {
                            BarChart.drawText(page, f2, label,
                                    Swift.min(x0, x) + (abs(x - x0) - f2.stringWidth(label)) / 2.0, baseline, on)
                        }
                    } else if let label = label, valueLabelsInside && abs(x - x0) >= f2.stringWidth(label) + pad {
                        BarChart.drawText(page, f2, label,
                                to >= base ? x - pad / 2.0 - f2.stringWidth(label) : x + pad / 2.0, baseline, on)
                    } else if let label = label {
                        BarChart.drawText(page, f2, label,
                                to >= base ? x + pad / 2.0 : x - pad / 2.0 - f2.stringWidth(label), baseline, BarChart.INK)
                    }
                } else {
                    let y0 = y8 - ChartScale.scaled(from - vMin, y8 - y5, vMax - vMin)
                    let y = y8 - ChartScale.scaled(to - vMin, y8 - y5, vMax - vMin)
                    page.fillRect(barStart, Swift.min(y0, y), barWidth, abs(y - y0))
                    let x = barStart + (barWidth - (label != nil ? f2.stringWidth(label!) : 0.0)) / 2.0
                    if let label = label, stacked {
                        if abs(y - y0) >= bodyHeight {
                            BarChart.drawText(page, f2, label, x, (y + y0) / 2.0 + ascent / 2.0, on)
                        }
                    } else if let label = label, valueLabelsInside && abs(y - y0) >= bodyHeight + pad {
                        BarChart.drawText(page, f2, label, x, to >= base ? y + ascent + pad / 2.0 : y - pad / 2.0, on)
                    } else if let label = label {
                        BarChart.drawText(page, f2, label, x, to >= base ? y - pad / 2.0 : y + ascent + pad / 2.0,
                                BarChart.INK)
                    }
                }
            }
            // The total of a stack above it, or past its end
            if totals && up > 0.0 {
                let total = valueLabel(up)
                let groupMiddle = groupStart + groupWidth / 2.0
                if horizontal {
                    let x = x5 + ChartScale.scaled(Swift.min(up, vMax) - vMin, x6 - x5, vMax - vMin)
                    BarChart.drawText(page, f2, total, x + pad / 2.0, groupMiddle + ascent / 2.0, BarChart.INK)
                } else {
                    let y = y8 - ChartScale.scaled(Swift.min(up, vMax) - vMin, y8 - y5, vMax - vMin)
                    BarChart.drawText(page, f2, total, groupMiddle - f2.stringWidth(total) / 2.0, y - pad / 2.0,
                            BarChart.INK)
                }
            }
        }

        // The axis line at the base of the bars, which is the zero line of
        // negative values
        page.setDefaultStrokeDashPattern()
        if axisLineWidth > 0.0 {
            page.setPenColor(BarChart.BASELINE)
            page.setPenWidth(axisLineWidth)
            if horizontal {
                let x0 = x5 + ChartScale.scaled(base - vMin, x6 - x5, vMax - vMin)
                page.drawLine(x0, y5, x0, y8)
            } else {
                let y0 = y8 - ChartScale.scaled(base - vMin, y8 - y5, vMax - vMin)
                page.drawLine(x5, y0, x6, y0)
            }
        }
        if innerBorderWidth > 0.0 {
            page.setPenColor(BarChart.BASELINE)
            page.setPenWidth(innerBorderWidth)
            page.drawRect(x5, y5, x6 - x5, y8 - y5)
        }

        // The lines over the bars, through the middles of the categories, a
        // dot at each value, broken where a value is missing; each on a white
        // line a little wider, so that it stays clear where it crosses a bar
        for s in lineSeries {
            var runs = [[[Float]]]()
            var inRun = false
            for i in 0..<n {
                if i >= s.values.count || !ChartScale.isValue(s.values[i]) {
                    inRun = false
                    continue
                }
                let v = Swift.min(Swift.max(s.values[i], vMin), vMax)
                let middle = (horizontal ? y5 : x5) + Float(i) * slot + slot / 2.0
                let at = horizontal
                        ? x5 + ChartScale.scaled(v - vMin, x6 - x5, vMax - vMin)
                        : y8 - ChartScale.scaled(v - vMin, y8 - y5, vMax - vMin)
                if !inRun {
                    runs.append([[Float]]())
                    inRun = true
                }
                runs[runs.count - 1].append(horizontal ? [at, middle] : [middle, at])
            }
            page.setLineJoinStyle(JoinStyle.ROUND)
            let dot = 0.2 * bodyHeight
            for pass in 0..<2 {
                let color = (pass == 0) ? Color.white : s.color
                page.setPenColor(color)
                page.setPenWidth((pass == 0) ? 3.5 : 2.0)
                for points in runs {
                    page.moveTo(points[0][0], points[0][1])
                    for k in 1..<points.count {
                        page.lineTo(points[k][0], points[k][1])
                    }
                    page.strokePath()
                }
                page.setBrushColor(color)
                for points in runs {
                    for point in points {
                        page.fillCircle(point[0], point[1], (pass == 0) ? dot + 0.75 : dot)
                    }
                }
            }
        }

        // Axis titles
        page.setTextRotation(-90)
        let yTitle = ChartScale.shorten(f2, f2.getSize(), yAxisTitle, y8 - y5)
        BarChart.drawText(page, f2, yTitle, x1 + bodyHeight, y8 - ((y8 - y5) - f2.stringWidth(yTitle)) / 2.0,
                BarChart.MUTED)
        page.setTextRotation(0)
        let xTitle = ChartScale.shorten(f2, f2.getSize(), xAxisTitle, x6 - x5)
        BarChart.drawText(page, f2, xTitle, x5 + ((x6 - x5) - f2.stringWidth(xTitle)) / 2.0, y2 - bodyHeight / 2.0,
                BarChart.MUTED)

        page.restoreGraphicsState()
        page.setFigureBoundingBox(x1, y1, w, h)
        page.addEMC()

        return [x1 + w, y1 + h]
    }

    // The alternate description: the one set, or the title, the subtitle and,
    // for each series, its name and its values by category.
    private func getAltDescription() -> String {
        if let altDescription, !altDescription.isEmpty {
            return altDescription
        }
        var sb = ChartScale.join(title, subtitle)
        if sb.isEmpty {
            sb += "Bar chart"
        }
        var separator = ": "
        for (j, s) in allSeries().enumerated() {
            var values = ""
            for i in 0..<Swift.min(s.values.count, numberOfCategories()) {
                if !ChartScale.isValue(s.values[i]) {
                    continue
                }
                if !values.isEmpty {
                    values += ", "
                }
                let category = i < categories.count ? categories[i] : ""
                if !category.isEmpty {
                    values += category + " "
                }
                values += valueLabel(s.values[i])
            }
            if values.isEmpty {
                continue
            }
            sb += separator
            if j >= series.count {      // A line
                sb += s.name.isEmpty ? "line: " : s.name + ", a line: "
            } else if !s.name.isEmpty {
                sb += s.name + ": "
            }
            sb += values
            separator = "; "
        }
        return sb + "."
    }

    /// Returns the number of category slots: the categories or the longest series.
    private func numberOfCategories() -> Int {
        var n = categories.count
        for s in series {
            n = Swift.max(n, s.values.count)
        }
        // A line series is drawn over the categories of the bars, and adds
        // none of its own when there are bars: a longer target line added
        // empty slots
        if n == 0 {
            for s in lineSeries {
                n = Swift.max(n, s.values.count)
            }
        }
        return n
    }

    /// Returns the series of the bars, then those of the lines.
    private func allSeries() -> [Series] {
        return series + lineSeries
    }

    /// Returns true if a series has a name to list in the legend.
    private func hasSeriesNames() -> Bool {
        for s in allSeries() {
            if !s.name.isEmpty {
                return true
            }
        }
        return false
    }

    /// Returns the color of the bar at the index: the series' own, its color for the bar, or the palette's.
    private func barColor(_ s: Series, _ seriesIndex: Int, _ index: Int) -> Int32 {
        if let colors = s.colors, index >= 0 && index < colors.count {
            return colors[index]
        }
        return s.color == BarChart.NO_COLOR ? BarChart.PALETTE[seriesIndex % BarChart.PALETTE.count] : s.color
    }

    /// Formats the value written at the end of a bar.
    private func valueLabel(_ value: Float) -> String {
        return group(ChartScale.format(value, minFractionDigits, maxFractionDigits))
    }

    /// Formats a label of the value axis.
    private func axisLabel(_ value: Float, _ digits: Int) -> String {
        return group(ChartScale.format(value, digits, maxFractionDigits))
    }

    /// Groups the digits before the decimal point in thousands with commas, if grouping is used.
    private func group(_ label: String) -> String {
        return groupingUsed ? ChartScale.groupDigits(label) : label
    }

    /// Draws the text in the font at its size, in the color.
    private static func drawText(_ page: Page, _ font: Font, _ text: String, _ x: Float, _ y: Float, _ color: Int32) {
        page.drawString(font, font.getSize(), text, x, y, Util.toRGB(color), nil)
    }

    /// Returns the color of a label on a bar of the color: white or ink,
    /// whichever contrasts more with it, by the WCAG contrast ratio.
    static func labelColorOn(_ color: Int32) -> Int32 {
        // White contrasts more when (1 + 0.05) / (L + 0.05) is the larger of
        // the two ratios, (L + 0.05) / (ink + 0.05) the other
        let l = luminance(color) + 0.05
        return l * l <= 1.05 * (luminance(BarChart.INK) + 0.05) ? Color.white : BarChart.INK
    }

    /// Returns the relative luminance of the color, as WCAG defines it.
    private static func luminance(_ color: Int32) -> Double {
        let r = 0.2126 * linear((color >> 16) & 0xFF)
        let g = 0.7152 * linear((color >> 8) & 0xFF)
        let b = 0.0722 * linear(color & 0xFF)
        return r + g + b
    }

    /// Returns the sRGB component, 0 to 255, as a linear value from 0 to 1.
    private static func linear(_ component: Int32) -> Double {
        let c = Double(component) / 255.0
        return c <= 0.04045 ? c / 12.92 : pow((c + 0.055) / 1.055, 2.4)
    }

    /// Draws one grid line.
    private func gridLine(_ page: Page, _ xa: Float, _ ya: Float, _ xb: Float, _ yb: Float) {
        page.setPenColor(gridLineColor)
        page.setPenWidth(gridLineWidth)
        page.setStrokeDashPattern(gridLineDashPattern)
        page.drawLine(xa, ya, xb, yb)
    }

    // Draws the legend from the baseline of its first row, its rows wrapped
    // at the right edge of the chart, when there is a page; and returns the
    // number of rows. A series with a color per bar has the color of its
    // first bar.
    @discardableResult
    private func drawLegend(_ page: Page?, _ baseline: Float) -> Int {
        let swatch = f2.getAscent()
        let gap = swatch / 2.0
        var x = x1
        var baseline = baseline
        var rows = 1
        let all = allSeries()
        for j in 0..<all.count {
            let s = all[j]
            if s.name.isEmpty {
                continue
            }
            // A name too long for a row is shortened
            let name = ChartScale.shorten(f2, f2.getSize(), s.name, w - swatch - gap)
            if x > x1 && x + swatch + gap + f2.stringWidth(name) > x1 + w {
                x = x1
                baseline += 1.5 * f2.getBodyHeight()
                rows += 1
            }
            if let page, j >= series.count {
                // A line: its color across the swatch, a dot in the middle
                page.setPenColor(s.color)
                page.setPenWidth(1.5)
                page.drawLine(x, baseline - swatch / 2.0, x + swatch, baseline - swatch / 2.0)
                page.setBrushColor(s.color)
                page.fillCircle(x + swatch / 2.0, baseline - swatch / 2.0, swatch / 4.0)
            } else if let page {
                if let colors = s.colors, !colors.isEmpty {
                    page.setBrushColor(colors[0])
                } else {
                    page.setBrushColor(barColor(s, j, -1))
                }
                page.fillRect(x, baseline - swatch, swatch, swatch)
            }
            x += swatch + gap
            if let page {
                BarChart.drawText(page, f2, name, x, baseline, BarChart.INK)
            }
            x += f2.stringWidth(name) + f2.getBodyHeight()
        }
        return rows
    }

}   // End of BarChart.swift
