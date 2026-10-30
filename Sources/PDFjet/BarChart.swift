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

    /// A value written by a bar, at its place, in its color.
    private struct Label {
        let text: String
        let x: Float
        let y: Float
        let color: Int32
    }

    private static let NO_COLOR: Int32 = -1

    // The look of the simple charts: ink for the title and the values, a
    // muted gray for the rest of the text, light grid lines, and the colors
    // of the series from the Tableau 10 palette, which D3 draws its charts
    // with: ten colors chosen to stay apart from one another.
    private static let INK = SimpleBarChart.INK
    private static let MUTED = SimpleBarChart.MUTED
    private static let GRID: Int32 = 0xE1E4E8
    private static let BASELINE: Int32 = 0x8C96A3
    static let PALETTE: [Int32] = [
        0x4E79A7,   // blue
        0xF28E2C,   // orange
        0xE15759,   // red
        0x76B7B2,   // teal
        0x59A14F,   // green
        0xEDC949,   // yellow
        0xAF7AA1,   // purple
        0xFF9DA7,   // pink
        0x9C755F,   // brown
        0xBAB0AB    // gray
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

    private var minFractionDigits = 0
    private var maxFractionDigits = 2
    private var fractionDigitsSet = false

    // The range of the value axis, from the data unless it is set
    private var min: Float = 0.0
    private var max: Float = 0.0
    private var rangeSet = false

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
    /// slot, from 0.0 to 0.9. The default is 0.3.
    ///
    /// - Parameter gap: the gap.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setGroupGap(_ gap: Float) -> BarChart {
        // From 0 to 0.9 of a slot: more left the bars no width, or less than none
        if ChartScale.isValue(gap) {
            self.groupGap = Swift.min(Swift.max(gap, 0.0), 0.9)
        }
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
        // 0 or more: a negative gap made bars of no width, and -1 divided by 0
        if ChartScale.isValue(gap) {
            self.barGap = Swift.max(gap, 0.0)
        }
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
    /// Sets the number of digits after the decimal point of the values written
    /// by the bars, all of them alike, as SimpleBarChart's. Without it a value
    /// has as many as it needs, two at most, or as many as the step of the value
    /// axis has. The numbers of the value axis have the digits of its step.
    ///
    /// - Parameter digits: the number of digits.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setFractionDigits(_ digits: Int) -> BarChart {
        self.minFractionDigits = digits
        self.maxFractionDigits = digits
        self.fractionDigitsSet = true
        return self
    }

    ///
    /// Sets the range of the value axis, with grid lines on a round step, as
    /// setYAxisRange does in SimpleLineChart and SimpleScatterChart. Without it
    /// the range is that of the data, rounded to steps, and always includes 0.
    /// A range whose end is not past its start is not set.
    ///
    /// - Parameter min: the value at the start of the axis.
    /// - Parameter max: the value at the end of the axis.
    /// - Returns: this BarChart object.
    ///
    @discardableResult
    public func setValueAxisRange(_ min: Float, _ max: Float) -> BarChart {
        self.min = min
        self.max = max
        self.rangeSet = max > min
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
        if maxFractionDigits > ChartScale.maxDigits {
            page.pdf.fail(ChartScale.tooManyDigits)
            return [x1 + w, y1 + h]
        }

        var vMin: Float
        var vMax: Float
        var lines: Int
        if rangeSet {
            vMin = min
            vMax = max
            lines = ChartScale.roundLines(min, max,
                    Swift.max(ChartScale.roundMaxAndMinValues(max, min).numOfGridLines, 1))
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
                // The values of a line series up to the last category, which
                // are all it draws
                for s in lineSeries {
                    for i in 0..<Swift.min(s.values.count, n) where ChartScale.isValue(s.values[i]) {
                        lo = Swift.min(lo, s.values[i])
                        hi = Swift.max(hi, s.values[i])
                    }
                }
            } else {
                for s in allSeries() {
                    for i in 0..<Swift.min(s.values.count, n) where ChartScale.isValue(s.values[i]) {
                        lo = Swift.min(lo, s.values[i])
                        hi = Swift.max(hi, s.values[i])
                    }
                }
            }
            if !ChartScale.isValue(hi - lo) {
                // Values, or sums of a stack, whose span is past the largest
                // Float, which no axis can draw: they were drawn on 0 to 1
                return [x1 + w, y1 + h]
            }
            if hi == lo { (lo, hi) = ChartScale.flatRange(lo) }
            let round = ChartScale.roundMaxAndMinValues(hi, lo)
            vMin = round.minValue
            vMax = round.maxValue
            lines = round.numOfGridLines
        }
        if vMax == vMin { (vMin, vMax) = ChartScale.flatRange(vMin) }
        let step = (vMax - vMin) / Float(lines)
        // The digits of the step, which setFractionDigits does not cap: with 0
        // an axis of 0.25 was written 0, 0, 1, 1
        let axisDigits = Swift.max(minFractionDigits, ChartScale.stepDigits(step))
        // The significant digits of the numbers of the axis written with a
        // power of ten, as many as write them apart
        let significant = ChartScale.axisSignificant(vMin, step, lines, axisDigits)
        // The digits of the values: those set, or as many as the step has, two
        // at least, as the simple charts write them. Values under 0.01 were
        // written 0 (the review of 28 October 2026)
        let digits = fractionDigitsSet ? maxFractionDigits : Swift.max(maxFractionDigits, ChartScale.stepDigits(step))
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
            let label = axisLabel(vMin + step * Float(i), axisDigits, significant)
            widestAxisLabel = Swift.max(widestAxisLabel, f2.stringWidth(label))
        }
        var widestValueLabel: Float = 0.0
        var widestNegative: Float = 0.0     // Of the bars below 0, written past their ends
        // Of the labels that are written: not those of the bars outside a
        // range that is set, which made room for labels that were not there
        for i in 0..<n where drawValueLabels {
            var up: Float = 0.0
            var down: Float = 0.0
            for s in series {
                if i >= s.values.count || !ChartScale.isValue(s.values[i]) {
                    continue
                }
                let v = s.values[i]
                var from = base
                if stacked {
                    if v >= 0.0 { from = up; up += v } else { from = down; down += v }
                }
                let to = stacked ? from + v : v
                if BarChart.outside(from, to, vMin, vMax) {
                    continue
                }
                let width = f2.stringWidth(valueLabel(v, digits))
                widestValueLabel = Swift.max(widestValueLabel, width)
                if v < 0.0 {
                    widestNegative = Swift.max(widestNegative, width)
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
                // A sum past the largest Float, or below a range that is set,
                // is not written
                if up > 0.0 && ChartScale.isValue(up) && up >= vMin {
                    widestTotal = Swift.max(widestTotal, f2.stringWidth(valueLabel(up, digits)))
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
        // Room between the plot and the categories for the label of a bar
        // below 0 that reaches the end of the axis: it was written over the
        // category's name
        var negative: Float = 0.0
        if drawValueLabels && !stacked && widestNegative > 0.0 {
            negative = horizontal ? widestNegative + pad / 2.0 : bodyHeight
        }
        let leftMargin = 1.5 * bodyHeight + pad + (horizontal ? widestCategory + negative : widestAxisLabel)
        var rightMargin = pad
        let bottomMargin = 2.5 * bodyHeight + (horizontal ? 0.0 : negative)
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
        // No room for the plot, or an axis whose span is past the largest
        // float, as a set range of -3e38 to 3e38, whose steps are infinite
        if x6 <= x5 || y8 <= y5 || !ChartScale.isValue(step) {
            return [x1 + w, y1 + h]
        }

        // The chart is one figure, described by its alternate description.
        page.addBDC(StructElem.FIGURE, nil, getAltDescription(digits))
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

        // Grid lines and value axis labels. A label is drawn at every grid line
        // that leaves it room, the widest label and a pad across, or a line high
        // up the vertical axis: at every second or third line, or more, when
        // the labels would run into each other, the lines all drawn.
        var every = horizontal
                ? ChartScale.axisLabelStep((x6 - x5) / Float(lines), widestAxisLabel + pad, lines)
                : ChartScale.axisLabelStep((y8 - y5) / Float(lines), bodyHeight, lines)
        if horizontal {
            // Inside the chart, and apart when the first and the last are moved in
            var centers = [Float]()
            var widths = [Float]()
            for i in 0...lines {
                let v = vMin + step * Float(i)
                centers.append(x5 + ChartScale.scaled(v - vMin, x6 - x5, vMax - vMin))
                widths.append(f2.stringWidth(axisLabel(v, axisDigits, significant)))
            }
            every = ChartScale.placedStep(every, centers, widths, x1, x2, pad / 2.0)
        }
        for i in 0...lines {
            let v = vMin + step * Float(i)
            let label = axisLabel(v, axisDigits, significant)
            let labeled = i % every == 0
            if horizontal {
                let x = x5 + ChartScale.scaled(v - vMin, x6 - x5, vMax - vMin)
                if drawGridLines {
                    gridLine(page, x, y5, x, y8)
                }
                if labeled {
                    BarChart.drawText(page, f2, label, ChartScale.placed(x, f2.stringWidth(label), x1, x2),
                            y8 + bodyHeight, BarChart.MUTED)
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
        // A category is written where it leaves the one before it room: at
        // every second or third bar, or more. By horizontal bars every name
        // was written, and forty of them ran into each other (the review of
        // 28 October 2026)
        var everyCategory = ChartScale.axisLabelStep(slot, horizontal ? bodyHeight : widestCategory + pad, n)
        if !horizontal {
            // Inside the chart, and apart when the first and the last are moved in
            var centers = [Float]()
            var widths = [Float]()
            for i in 0..<n {
                centers.append(x5 + Float(i) * slot + slot / 2.0)
                widths.append(i < names.count ? f2.stringWidth(names[i]) : 0.0)
            }
            everyCategory = ChartScale.placedStep(everyCategory, centers, widths, x1, x2, pad / 2.0)
        }
        // The values past the ends of grouped bars and the totals of the
        // stacks are written, as the categories are, where they leave the one
        // before them room, the nearest bars apart: at every second or third
        // bar, or more. Over narrow bars they ran into each other (the review
        // of 28 October 2026)
        var apart = slot
        if m > 1 && !stacked {
            let barStep = barWidth * (1.0 + barGap)
            apart = Swift.min(barStep, slot - Float(m - 1) * barStep)
        }
        let everyLabel = ChartScale.axisLabelStep(apart, horizontal ? bodyHeight : widestValueLabel + pad, n * m)
        let everyTotal = ChartScale.axisLabelStep(slot, horizontal ? bodyHeight : widestTotal + pad, n)
        // The values are written over all the bars: a label wider than its
        // bar was covered by the bar after it
        var labels = [Label]()
        for i in 0..<n {
            let slotStart = (horizontal ? y5 : x5) + Float(i) * slot
            let groupStart = slotStart + slot * groupGap / 2.0
            let category = i < names.count ? names[i] : ""
            if horizontal && i % everyCategory == 0 {
                BarChart.drawText(page, f2, category, x5 - pad - negative - f2.stringWidth(category),
                        slotStart + slot / 2.0 + ascent / 2.0, BarChart.MUTED)
            } else if !horizontal && i % everyCategory == 0 {
                // Centered under its slot, inside the chart: the last one
                // went past the chart's right edge
                BarChart.drawText(page, f2, category,
                        ChartScale.placed(slotStart + slot / 2.0, f2.stringWidth(category), x1, x2),
                        y8 + bodyHeight + negative, BarChart.MUTED)
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
                // A bar wholly outside a range that is set has no length,
                // and its value is not written by it
                let outside = BarChart.outside(from, to, vMin, vMax)
                from = Swift.min(Swift.max(from, vMin), vMax)
                to = Swift.min(Swift.max(to, vMin), vMax)
                let barStart = stacked ? groupStart : groupStart + Float(j) * barWidth * (1.0 + barGap)
                let color = barColor(s, j, i)
                page.setBrushColor(color)
                let label: String? = (drawValueLabels && !outside) ? valueLabel(s.values[i], digits) : nil
                let on = BarChart.labelColorOn(color)
                if horizontal {
                    let x0 = x5 + ChartScale.scaled(from - vMin, x6 - x5, vMax - vMin)
                    let x = x5 + ChartScale.scaled(to - vMin, x6 - x5, vMax - vMin)
                    page.fillRect(Swift.min(x0, x), barStart, abs(x - x0), barWidth)
                    let baseline = barStart + barWidth / 2.0 + ascent / 2.0
                    // A label inside a bar as thick as a line of it: a stacked
                    // one is left out, and one past the end is written instead
                    if let label = label, stacked {
                        if abs(x - x0) >= f2.stringWidth(label) + pad && barWidth >= bodyHeight {
                            labels.append(Label(text: label,
                                    x: Swift.min(x0, x) + (abs(x - x0) - f2.stringWidth(label)) / 2.0, y: baseline,
                                    color: on))
                        }
                    } else if let label = label, valueLabelsInside && abs(x - x0) >= f2.stringWidth(label) + pad
                            && barWidth >= bodyHeight {
                        labels.append(Label(text: label,
                                x: to >= base ? x - pad / 2.0 - f2.stringWidth(label) : x + pad / 2.0, y: baseline,
                                color: on))
                    } else if let label = label, (i * m + j) % everyLabel == 0 {
                        labels.append(Label(text: label,
                                x: to >= base ? x + pad / 2.0 : x - pad / 2.0 - f2.stringWidth(label), y: baseline,
                                color: BarChart.INK))
                    }
                } else {
                    let y0 = y8 - ChartScale.scaled(from - vMin, y8 - y5, vMax - vMin)
                    let y = y8 - ChartScale.scaled(to - vMin, y8 - y5, vMax - vMin)
                    page.fillRect(barStart, Swift.min(y0, y), barWidth, abs(y - y0))
                    let x = barStart + (barWidth - (label != nil ? f2.stringWidth(label!) : 0.0)) / 2.0
                    // A label inside a bar as wide as it: a stacked one is left
                    // out, and one past the end is written instead. Labels
                    // wider than their bars were written over the bars next
                    // to them (the review of 28 October 2026)
                    if let label = label, stacked {
                        if abs(y - y0) >= bodyHeight && barWidth >= f2.stringWidth(label) {
                            labels.append(Label(text: label, x: x, y: (y + y0) / 2.0 + ascent / 2.0, color: on))
                        }
                    } else if let label = label, valueLabelsInside && abs(y - y0) >= bodyHeight + pad
                            && barWidth >= f2.stringWidth(label) {
                        labels.append(Label(text: label, x: x,
                                y: to >= base ? y + ascent + pad / 2.0 : y - pad / 2.0, color: on))
                    } else if let label = label, (i * m + j) % everyLabel == 0 {
                        labels.append(Label(text: label, x: x,
                                y: to >= base ? y - pad / 2.0 : y + ascent + pad / 2.0, color: BarChart.INK))
                    }
                }
            }
            // The total of a stack above it, or past its end. A sum past the
            // largest float is not written, nor one below a range that is
            // set, which was written under the plot (the review of 28
            // October 2026)
            if totals && up > 0.0 && ChartScale.isValue(up) && up >= vMin && i % everyTotal == 0 {
                let total = valueLabel(up, digits)
                let groupMiddle = groupStart + groupWidth / 2.0
                if horizontal {
                    let x = x5 + ChartScale.scaled(Swift.min(up, vMax) - vMin, x6 - x5, vMax - vMin)
                    labels.append(Label(text: total, x: x + pad / 2.0, y: groupMiddle + ascent / 2.0,
                            color: BarChart.INK))
                } else {
                    let y = y8 - ChartScale.scaled(Swift.min(up, vMax) - vMin, y8 - y5, vMax - vMin)
                    labels.append(Label(text: total, x: groupMiddle - f2.stringWidth(total) / 2.0, y: y - pad / 2.0,
                            color: BarChart.INK))
                }
            }
        }
        for label in labels {
            BarChart.drawText(page, f2, label.text, label.x, label.y, label.color)
        }

        // The axis line at the base of the bars, which is the zero line of
        // negative values
        page.setPenColor(BarChart.BASELINE)
        page.setPenWidth(0.75)
        if horizontal {
            let x0 = x5 + ChartScale.scaled(base - vMin, x6 - x5, vMax - vMin)
            page.drawLine(x0, y5, x0, y8)
        } else {
            let y0 = y8 - ChartScale.scaled(base - vMin, y8 - y5, vMax - vMin)
            page.drawLine(x5, y0, x6, y0)
        }

        // The lines over the bars, through the middles of the categories, a
        // dot at each value, broken where a value is missing; each on a white
        // line a little wider, so that it stays clear where it crosses a bar.
        // A value outside a range that is set is drawn past the plot, which
        // clips the lines at its edges and as far past them as a dot is wide,
        // as in SimpleLineChart: a target of 150 over a range of 0 to 100 had
        // its dot at 100
        let axis = ChartScale.Axis(min: vMin, max: vMax, lines: lines)
        var outsideAxis = false
        for s in lineSeries {
            for i in 0..<Swift.min(s.values.count, n) {
                outsideAxis = outsideAxis || (ChartScale.isValue(s.values[i]) && !ChartScale.onAxis(s.values[i], axis))
            }
        }
        if outsideAxis {
            let edge = 0.2 * bodyHeight + 1.75
            page.saveGraphicsState()
            page.clipRect(x5 - edge, y5 - edge, x6 - x5 + 2.0 * edge, y8 - y5 + 2.0 * edge)
        }
        for s in lineSeries {
            var runs = [[[Float]]]()
            var inRun = false
            for i in 0..<n {
                if i >= s.values.count || !ChartScale.isValue(s.values[i]) {
                    inRun = false
                    continue
                }
                let v = ChartScale.held(s.values[i], axis)
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
        if outsideAxis {
            page.restoreGraphicsState()
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
    // for each series, its name and its values by category, with the digits
    // of the labels, or, without setFractionDigits, with two significant
    // digits at least.
    private func getAltDescription(_ digits: Int) -> String {
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
                values += valueLabel(s.values[i], describedDigits(s.values[i], digits))
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

    /// Tells if a bar from one value to another is wholly outside the axis, which gives it no length.
    private static func outside(_ from: Float, _ to: Float, _ vMin: Float, _ vMax: Float) -> Bool {
        return Swift.min(Swift.max(from, vMin), vMax) == Swift.min(Swift.max(to, vMin), vMax) && from != to
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

    /// Formats the value written at the end of a bar, with the digits at most.
    private func valueLabel(_ value: Float, _ digits: Int) -> String {
        return group(ChartScale.format(value, minFractionDigits, digits))
    }

    ///
    /// Returns the digits of the value in the description: those of the
    /// labels, or, without setFractionDigits, more for a small value, two
    /// significant digits. A value of 0.00012 on an axis of 1,000 was
    /// described as 0 (the review of 28 October 2026).
    ///
    private func describedDigits(_ value: Float, _ digits: Int) -> Int {
        if fractionDigitsSet || value == 0.0 {
            return digits
        }
        return Swift.max(digits, 1 - Int(floor(log10(Double(abs(value))))))
    }

    /// Formats a label of the value axis, with the significant digits when it
    /// is written with a power of ten.
    private func axisLabel(_ value: Float, _ digits: Int, _ significant: Int) -> String {
        return group(ChartScale.format(value, digits, digits, significant))
    }

    /// Groups the digits before the decimal point in thousands with commas, if grouping is used.
    private func group(_ label: String) -> String {
        return groupingUsed ? ChartScale.groupDigits(label) : label
    }

    /// Draws the text in the font at its size, in the color.
    private static func drawText(_ page: Page, _ font: Font, _ text: String, _ x: Float, _ y: Float, _ color: Int32) {
        page.drawString(font, font.getSize(), text, x, y, Util.toRGB(color), nil)
    }

    // The label on a color that neither white nor ink contrasts with as WCAG
    // asks, 4.5:1: the red, the purple and the brown of the palette.
    static let DARK: Int32 = 0x111111

    /// Returns the color of a label on a bar of the color: white or ink,
    /// whichever contrasts more with it, by the WCAG contrast ratio, or a
    /// near black when that is less than 4.5:1.
    static func labelColorOn(_ color: Int32) -> Int32 {
        // White contrasts more when (1 + 0.05) / (L + 0.05) is the larger of
        // the two ratios, (L + 0.05) / (ink + 0.05) the other
        let l = luminance(color) + 0.05
        let ink = luminance(BarChart.INK) + 0.05
        if l * l <= 1.05 * ink {
            return (1.05 / l >= 4.5) ? Color.white : BarChart.DARK
        }
        return (l / ink >= 4.5) ? BarChart.INK : BarChart.DARK
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

    /// Draws one grid line, thin and solid.
    private func gridLine(_ page: Page, _ xa: Float, _ ya: Float, _ xb: Float, _ yb: Float) {
        page.setPenColor(gridLineColor)
        page.setPenWidth(0.5)
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
