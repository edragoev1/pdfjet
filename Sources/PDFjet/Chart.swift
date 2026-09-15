/**
 * Chart.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/**
 * Used to create XY chart objects and draw them on a page.
 *
 * Please see Example_09.
 */
public class Chart : Drawable {
    private var w: Float = 300.0
    private var h: Float = 200.0

    private var x1: Float = 0.0
    private var y1: Float = 0.0
    private var x2: Float = 0.0
    private var y2: Float = 0.0
    private var x3: Float = 0.0
    private var y3: Float = 0.0
    private var x4: Float = 0.0
    private var y4: Float = 0.0
    private var x5: Float = 0.0
    private var y5: Float = 0.0
    private var x6: Float = 0.0
    private var y6: Float = 0.0
    private var x7: Float = 0.0
    private var y7: Float = 0.0
    private var x8: Float = 0.0
    private var y8: Float = 0.0

    private var xMax = -Float.greatestFiniteMagnitude
    private var xMin = Float.greatestFiniteMagnitude
    private var yMax = -Float.greatestFiniteMagnitude
    private var yMin = Float.greatestFiniteMagnitude

    private var xAxisGridLines = 0
    private var yAxisGridLines = 0

    private var title = ""
    private var subtitle = ""
    private var xAxisTitle = ""
    private var yAxisTitle = ""

    private var drawHorizontalGridLines = true
    private var drawVerticalGridLines = true
    private var drawXAxisLabels = true
    private var drawYAxisLabels = true

    private var gridLineColor: Int32 = Color.black
    private var horizontalGridLineWidth: Float = 0.0
    private var verticalGridLineWidth: Float = 0.0

    private var horizontalGridLineDashPattern = "[1 1] 0"
    private var verticalGridLineDashPattern = "[1 1] 0"

    private var axisLineWidth: Float = 0.5
    private var chartBorderWidth: Float = 0.0
    private var innerBorderWidth: Float = 0.0

    private var minFractionDigits = 0
    private var maxFractionDigits = 2

    private var f1: Font?
    private var f2: Font?

    private var series = [Series]()
    private var drawLegend = true

    static let DEFAULT_PALETTE = [
        Color.blue,
        Color.red,
        Color.green,
        Color.orange,
        Color.purple,
        Color.darkcyan,
        Color.magenta,
        Color.olive
    ]

    /**
     * Create a XY chart object.
     *
     * - Parameter f1: the font used for the chart title.
     * - Parameter f2: the font used for the X and Y axis titles.
     */
    public init(_ f1: Font, _ f2: Font) {
        self.f1 = f1
        self.f2 = f2
    }

    /**
     * Sets the title of the chart.
     *
     * - Parameter title: the title text.
     * - Returns: this Chart object.
     */
    @discardableResult
    public func setTitle(_ title: String) -> Chart {
        self.title = title
        return self
    }

    /// Sets the subtitle, written in gray under the title in the second font.
    ///
    /// - Parameter subtitle: the subtitle.
    /// - Returns: this Chart object.
    @discardableResult
    public func setSubtitle(_ subtitle: String) -> Chart {
        self.subtitle = subtitle
        return self
    }

    /**
     * Sets the title for the X axis.
     *
     * - Parameter title: the X axis title.
     * - Returns: this Chart object.
     */
    @discardableResult
    public func setXAxisTitle(_ title: String) -> Chart {
        self.xAxisTitle = title
        return self
    }

    /**
     * Sets the title for the Y axis.
     *
     * - Parameter title: the Y axis title.
     * - Returns: this Chart object.
     */
    @discardableResult
    public func setYAxisTitle(_ title: String) -> Chart {
        self.yAxisTitle = title
        return self
    }

    /**
     * Adds a series and returns it, to add its points and set its line and
     * marker. A series without a stroke color has the next color of the
     * palette. The legend lists the series that have a name.
     *
     * - Parameter name: the series name, shown in the legend; empty for none.
     * - Returns: the new Series object.
     */
    @discardableResult
    public func addSeries(_ name: String) -> Series {
        let s = Series(name)
        series.append(s)
        return s
    }

    /**
     * Sets whether the legend is drawn. The legend lists the series that have
     * a name, under the title, each with its line or its marker.
     *
     * - Parameter drawLegend: true to draw the legend.
     * - Returns: this Chart object.
     */
    @discardableResult
    public func setDrawLegend(_ drawLegend: Bool) -> Chart {
        self.drawLegend = drawLegend
        return self
    }

    /**
     * Sets the location of this chart on the page.
     *
     * - Parameter x: the x coordinate of the top left corner of this chart when drawn on the page.
     * - Parameter y: the y coordinate of the top left corner of this chart when drawn on the page.
     * - Returns: this Chart object.
     */
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x1 = x
        self.y1 = y
        return self
    }

    /**
     * Sets the size of this chart.
     *
     * - Parameter w: the width of this chart.
     * - Parameter h: the height of this chart.
     * - Returns: this Chart object.
     */
    @discardableResult
    public func setSize(_ w: Float, _ h: Float) -> Chart {
        self.w = w
        self.h = h
        return self
    }

    /**
     * Sets the minimum number of decimal places in the axis labels. The labels
     * of an axis have at least the decimal places of its step, so an axis with
     * a whole number step has whole number labels. The default is 0.
     *
     * - Parameter minFractionDigits: the minimum number of fraction digits.
     * - Returns: this Chart object.
     */
    @discardableResult
    public func setMinimumFractionDigits(_ minFractionDigits: Int) -> Chart {
        self.minFractionDigits = minFractionDigits
        return self
    }

    /**
     * Sets the maximum number of decimal places in the axis labels. The
     * default is 2.
     *
     * - Parameter maxFractionDigits: the maximum number of fraction digits.
     * - Returns: this Chart object.
     */
    @discardableResult
    public func setMaximumFractionDigits(_ maxFractionDigits: Int) -> Chart {
        self.maxFractionDigits = maxFractionDigits
        return self
    }

    /** Toggles drawing of horizontal grid lines. */
    @discardableResult
    public func setDrawHorizontalGridLines(_ drawHorizontalGridLines: Bool) -> Chart {
        self.drawHorizontalGridLines = drawHorizontalGridLines
        return self
    }

    /** Toggles drawing of vertical grid lines. */
    @discardableResult
    public func setDrawVerticalGridLines(_ drawVerticalGridLines: Bool) -> Chart {
        self.drawVerticalGridLines = drawVerticalGridLines
        return self
    }

    /// Sets the width of the axis lines, along the left and the bottom sides of
    /// the plot area. The default is 0.5; 0 hides them.
    @discardableResult
    public func setAxisLineWidth(_ width: Float) -> Chart {
        self.axisLineWidth = width
        return self
    }

    /// Sets the width of the outer chart border. A width of 0, the default,
    /// hides it.
    @discardableResult
    public func setChartBorderWidth(_ width: Float) -> Chart {
        self.chartBorderWidth = width
        return self
    }

    /// Sets the width of the plot area border. A width of 0, the default,
    /// hides it.
    @discardableResult
    public func setInnerBorderWidth(_ width: Float) -> Chart {
        self.innerBorderWidth = width
        return self
    }

    /// Sets the width of the horizontal grid lines.
    @discardableResult
    public func setHorizontalGridLineWidth(_ width: Float) -> Chart {
        self.horizontalGridLineWidth = width
        return self
    }

    /// Sets the width of the vertical grid lines.
    @discardableResult
    public func setVerticalGridLineWidth(_ width: Float) -> Chart {
        self.verticalGridLineWidth = width
        return self
    }

    /// Sets the horizontal grid line dash pattern, e.g. "[1 1] 0".
    @discardableResult
    public func setHorizontalGridLineDashPattern(_ pattern: String) -> Chart {
        self.horizontalGridLineDashPattern = pattern
        return self
    }

    /// Sets the vertical grid line dash pattern, e.g. "[1 1] 0".
    @discardableResult
    public func setVerticalGridLineDashPattern(_ pattern: String) -> Chart {
        self.verticalGridLineDashPattern = pattern
        return self
    }

    /// Sets the color of the grid lines as a 0xRRGGBB value. The default is black.
    @discardableResult
    public func setGridLineColor(_ color: Int32) -> Chart {
        self.gridLineColor = color
        return self
    }

    /// Converts a 0xRRGGBB color to red, green and blue values between 0.0 and 1.0.
    private func toFloatArray(_ color: Int32) -> [Float] {
        let r = Float((color >> 16) & 0xff)/255.0
        let g = Float((color >>  8) & 0xff)/255.0
        let b = Float((color)       & 0xff)/255.0
        return [r, g, b]
    }

    /// Sets whether the x axis labels are drawn.
    @discardableResult
    public func setDrawXAxisLabels(_ drawXAxisLabels: Bool) -> Chart {
        self.drawXAxisLabels = drawXAxisLabels
        return self
    }

    /// Sets whether the y axis labels are drawn.
    @discardableResult
    public func setDrawYAxisLabels(_ drawYAxisLabels: Bool) -> Chart {
        self.drawYAxisLabels = drawYAxisLabels
        return self
    }

    /**
     * Draws this chart on the specified page.
     *
     * - Parameter page: the page to draw this chart on.
     */
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        // Guard against nil or empty data
        if !hasPoints() {
            return [self.x1 + self.w, self.y1 + self.h]
        }

        x2 = x1 + w
        y2 = y1

        x3 = x2
        y3 = y1 + h

        x4 = x1
        y4 = y3

        setXAxisMinAndMaxChartValues()
        setYAxisMinAndMaxChartValues()

        // Guard against flat data (all same X or Y) before rounding,
        // so the rounded ranges have grid lines
        if xMax == xMin { xMax = xMin + 1.0 }
        if yMax == yMin { yMax = yMin + 1.0 }

        roundXAxisMinAndMaxValues()
        roundYAxisMinAndMaxValues()

        // Draw chart title, the subtitle and then the legend under it
        let legend = drawLegend && hasSeriesNames()
        let titleBaseline = y1 + 1.5 * f1!.bodyHeight
        let subtitleHeight: Float = subtitle.isEmpty ? 0.0 : f2!.bodyHeight
        if page != nil {
            page!.setBrushColor(Color.black)
            page!.drawString(
                    f1!,
                    f1!.getSize(),
                    title,
                    x1 + ((w - f1!.stringWidth(title)) / 2),
                    titleBaseline)
            if !subtitle.isEmpty {
                page!.setBrushColor(Color.dimgray)
                page!.drawString(
                        f2!,
                        f2!.getSize(),
                        subtitle,
                        x1 + ((w - f2!.stringWidth(subtitle)) / 2),
                        titleBaseline + subtitleHeight)
            }
            if legend {
                drawLegend(page!, titleBaseline + subtitleHeight + 1.5 * f2!.bodyHeight)
            }
        }

        let topMargin = 2.5 * f1!.bodyHeight + subtitleHeight + (legend ? 1.5 * f2!.bodyHeight : 0.0)
        let leftMargin = getLongestAxisYLabelWidth() + 2.0 * f2!.bodyHeight
        let rightMargin = 2.0 * f2!.bodyHeight
        let bottomMargin = 2.5 * f2!.bodyHeight

        x5 = x1 + leftMargin
        y5 = y1 + topMargin

        x6 = x2 - rightMargin
        y6 = y5

        x7 = x6
        y7 = y3 - bottomMargin

        x8 = x5
        y8 = y7

        if page != nil {
            drawChartBorder(page!)
            drawInnerBorder(page!)

            if drawHorizontalGridLines {
                drawHorizontalGrid(page!)
            }
            if drawVerticalGridLines {
                drawVerticalGrid(page!)
            }
            if axisLineWidth > 0.0 {
                drawAxisLines(page!)
            }
            if drawXAxisLabels {
                drawXAxisLabels(page!)
            }
            if drawYAxisLabels {
                drawYAxisLabels(page!)
            }
        }

        // Translate copies of the points, so the data of the chart is not changed
        var plotData = [[Point]]()
        for s in series {
            plotData.append(s.points.map { Point($0) })
        }
        for points in plotData {
            for point in points {
                point.x = x5 + (point.x - xMin) * (x6 - x5) / (xMax - xMin)
                point.y = y8 - (point.y - yMin) * (y8 - y5) / (yMax - yMin)
                if point.getURIAction() != nil {
                    if page != nil {
                        page!.addAnnotation(Annotation(
                                Annotation.Link,
                                point.x - point.r,
                                point.y - point.r,
                                point.x + point.r,
                                point.y + point.r,
                                nil,    // Vertices
                                nil,    // Fill Color
                                0.0,    // Opacity
                                nil,    // Title
                                nil,    // Contents
                                point.getURIAction(),
                                nil,
                                nil,
                                nil,
                                nil))
                    }
                }
            }
        }

        if page != nil {
            drawPathsAndPoints(page!, plotData)

            // Draw the Y axis title
            page!.setBrushColor(Color.black)
            page!.setTextRotation(90)
            page!.drawString(
                    f2!,
                    f2!.getSize(),
                    yAxisTitle,
                    x1 + f2!.bodyHeight,
                    y8 - ((y8 - y5) - f2!.stringWidth(yAxisTitle)) / 2)

            // Draw the X axis title
            page!.setTextRotation(0)
            page!.setBrushColor(Color.black)
            page!.drawString(
                    f2!,
                    f2!.getSize(),
                    xAxisTitle,
                    x5 + ((x6 - x5) - f2!.stringWidth(xAxisTitle)) / 2,
                    y4 - f2!.bodyHeight / 2)

            page!.setDefaultPenWidth()
            page!.setDefaultStrokeDashPattern()
            page!.setPenColor(Color.black)
        }

        return [self.x1 + self.w, self.y1 + self.h]
    }

    // Returns true if at least one series has points.
    private func hasPoints() -> Bool {
        return series.contains(where: { !$0.points.isEmpty })
    }

    // Returns true if a series has a name to list in the legend.
    private func hasSeriesNames() -> Bool {
        return series.contains(where: { !$0.name.isEmpty })
    }

    // Returns the color of the series at the index: its own or the palette's.
    private func seriesColor(_ s: Series, _ index: Int) -> [Float] {
        return s.strokeColor ?? toFloatArray(Chart.DEFAULT_PALETTE[index % Chart.DEFAULT_PALETTE.count])
    }

    // Draws the legend centered on the chart: the line of each named series
    // that draws its path, its marker when it has one, and its name.
    private func drawLegend(_ page: Page, _ baseline: Float) {
        let ascent = f2!.getAscent()
        let sample = 2.0 * ascent   // the width of the line or the marker
        let gap = ascent / 2.0
        var width: Float = 0.0
        var entries = 0
        for s in series where !s.name.isEmpty {
            width += sample + gap + f2!.stringWidth(s.name)
            entries += 1
        }
        width += Float(entries - 1) * f2!.bodyHeight
        var x = x1 + (w - width) / 2.0
        for j in 0..<series.count {
            let s = series[j]
            if s.name.isEmpty {
                continue
            }
            let color = seriesColor(s, j)
            let yMid = baseline - ascent / 2.0
            page.setPenColor(color)
            if s.drawPath {
                page.setPenWidth(s.strokeWidth)
                page.setStrokeDashPattern(s.strokeDashPattern)
                page.drawLine(x, yMid, x + sample, yMid)
            }
            if s.shape != Shape.INVISIBLE {
                page.setPenWidth(1.0)
                page.setDefaultStrokeDashPattern()
                page.drawPoint(Point(x + sample / 2.0, yMid).setShape(s.shape).setRadius(s.radius))
            }
            x += sample + gap
            page.setBrushColor(Color.black)
            page.drawString(f2!, f2!.getSize(), s.name, x, baseline)
            x += f2!.stringWidth(s.name) + f2!.bodyHeight
        }
        page.setDefaultPenWidth()
        page.setDefaultStrokeDashPattern()
    }

    // Formats a label with minDigits to maxDigits decimal places, rounding
    // the exact value half to even. The label has a "." decimal separator and
    // no grouping whatever the locale, and a value that rounds to zero has no
    // minus sign.
    static func format(_ value: Float, _ minDigits: Int, _ maxDigits: Int) -> String {
        if value.isNaN {
            return "NaN"
        }
        if value.isInfinite {
            return (value < 0) ? "-Infinity" : "Infinity"
        }
        // A minimum above the maximum is lowered to it, as in Java's NumberFormat
        let maxDigits = max(maxDigits, 0)
        let minDigits = min(max(minDigits, 0), maxDigits)
        // String(format:) without a locale writes the exact digits with a "."
        var label = String(format: "%.\(maxDigits)f", Double(value))
        if let point = label.firstIndex(of: ".") {
            while label.distance(from: point, to: label.endIndex) - 1 > minDigits && label.hasSuffix("0") {
                label.removeLast()
            }
            if label.hasSuffix(".") {
                label.removeLast()
            }
        }
        if label.hasPrefix("-") && label.allSatisfy({ $0 == "-" || $0 == "0" || $0 == "." }) {
            label.removeFirst()
        }
        return label
    }

    // Returns the number of decimal places, at most maxDigits, that write the
    // axis step exactly: 0 for 10, 1 for 2.5, 2 for 0.25.
    static func fractionDigitsOf(_ step: Float, _ maxDigits: Int) -> Int {
        var digits = 0
        while digits < maxDigits {
            let scaled = Double(step) * pow(10.0, Double(digits))
            if abs(scaled - scaled.rounded()) < 1e-4 {
                return digits
            }
            digits += 1
        }
        return max(maxDigits, 0)
    }

    // Formats the label of an axis with the specified step.
    private func format(_ value: Float, _ step: Float) -> String {
        let digits = max(minFractionDigits, Chart.fractionDigitsOf(step, maxFractionDigits))
        return Chart.format(value, digits, maxFractionDigits)
    }

    private func getLongestAxisYLabelWidth()-> Float {
        let step = (yMax - yMin) / Float(yAxisGridLines)
        let minLabelWidth = f2!.stringWidth(format(yMin, step) + "0")
        let maxLabelWidth = f2!.stringWidth(format(yMax, step) + "0")
        if maxLabelWidth > minLabelWidth {
            return maxLabelWidth
        }
        return minLabelWidth
    }

    private func setXAxisMinAndMaxChartValues() {
        if xAxisGridLines != 0 {
            return
        }
        for s in series {
            for point in s.points {
                if point.x < xMin {
                    xMin = point.x
                }
                if point.x > xMax {
                    xMax = point.x
                }
            }
        }
    }

    private func setYAxisMinAndMaxChartValues() {
        if yAxisGridLines != 0 {
            return
        }
        for s in series {
            for point in s.points {
                if point.y < yMin {
                    yMin = point.y
                }
                if point.y > yMax {
                    yMax = point.y
                }
            }
        }
    }

    private func roundXAxisMinAndMaxValues() {
        if xAxisGridLines != 0 {
            return
        }
        let round = Chart.roundMaxAndMinValues(xMax, xMin)
        xMax = round.maxValue
        xMin = round.minValue
        xAxisGridLines = round.numOfGridLines
    }

    private func roundYAxisMinAndMaxValues() {
        if yAxisGridLines != 0 {
            return
        }
        let round = Chart.roundMaxAndMinValues(yMax, yMin)
        yMax = round.maxValue
        yMin = round.minValue
        yAxisGridLines = round.numOfGridLines
    }

    /// Draws the outer chart border, unless its width is 0.
    private func drawChartBorder(_ page: Page) {
        if chartBorderWidth <= 0.0 {
            return
        }
        page.setPenWidth(chartBorderWidth)
        page.setPenColor(Color.black)
        page.moveTo(x1, y1)
        page.lineTo(x2, y2)
        page.lineTo(x3, y3)
        page.lineTo(x4, y4)
        page.closePath()
    }

    /// Draws the plot area border, unless its width is 0.
    private func drawInnerBorder(_ page: Page) {
        if innerBorderWidth <= 0.0 {
            return
        }
        page.setPenWidth(innerBorderWidth)
        page.setPenColor(Color.black)
        page.moveTo(x5, y5)
        page.lineTo(x6, y6)
        page.lineTo(x7, y7)
        page.lineTo(x8, y8)
        page.closePath()
    }

    /// Draws the axis lines along the left and the bottom sides of the plot area.
    private func drawAxisLines(_ page: Page) {
        page.setPenWidth(axisLineWidth)
        page.setPenColor(Color.black)
        page.setDefaultStrokeDashPattern()
        page.drawLine(x5, y5, x5, y8)
        page.drawLine(x8, y8, x6, y8)
    }

    /// Draws the horizontal grid lines, one at each label.
    private func drawHorizontalGrid(_ page: Page) {
        page.setPenWidth(horizontalGridLineWidth)
        page.setPenColor(gridLineColor)
        page.setStrokeDashPattern(horizontalGridLineDashPattern)
        let x = x8
        var y = y8
        let step = (y8 - y5) / Float(yAxisGridLines)
        for _ in 0...yAxisGridLines {
            page.drawLine(x, y, x6, y)
            y -= step
        }
    }

    /// Draws the vertical grid lines, one at each label.
    private func drawVerticalGrid(_ page: Page) {
        page.setPenWidth(verticalGridLineWidth)
        page.setPenColor(gridLineColor)
        page.setStrokeDashPattern(verticalGridLineDashPattern)
        var x = x5
        let y = y5
        let step = (x6 - x5) / Float(xAxisGridLines)
        for _ in 0...xAxisGridLines {
            page.drawLine(x, y, x, y8)
            x += step
        }
    }

    private func drawXAxisLabels(_ page: Page) {
        var x = x5
        let y = y8 + f2!.getBodyHeight(f2!.getSize())
        let step = (x6 - x5) / Float(xAxisGridLines)
        let valueStep = (xMax - xMin) / Float(xAxisGridLines)
        page.setBrushColor(Color.black)
        var i = 0
        while i < (xAxisGridLines + 1) {
            let label = format(xMin + valueStep * Float(i), valueStep)
            page.drawString(
                    f2!,
                    f2!.getSize(),
                    label,
                    x - (f2!.stringWidth(label) / 2),
                    y)
            x += step
            i += 1
        }
    }

    private func drawYAxisLabels(_ page: Page) {
        let x = x5 - getLongestAxisYLabelWidth()
        var y = y8 + f2!.ascent / 3
        let step = (y8 - y5) / Float(yAxisGridLines)
        let valueStep = (yMax - yMin) / Float(yAxisGridLines)
        page.setBrushColor(Color.black)
        var i = 0
        while i < (yAxisGridLines + 1) {
            let label = format(yMin + valueStep * Float(i), valueStep)
            page.drawString(
                    f2!,
                    f2!.getSize(),
                    label,
                    x,
                    y)
            y -= step
            i += 1
        }
    }

    // Draws the line of each series that draws its path, then the markers of
    // its points: a point without a stroke color in the color of the series.
    private func drawPathsAndPoints(_ page: Page, _ plotData: [[Point]]) {
        for j in 0..<series.count {
            let s = series[j]
            let points = plotData[j]
            if points.isEmpty {
                continue
            }
            let color = seriesColor(s, j)
            if s.drawPath {
                page.setPenColor(color)
                page.setPenWidth(s.strokeWidth)
                page.setStrokeDashPattern(s.strokeDashPattern)
                page.drawPath(points, PathOperator.STROKE)
            }
            for point in points {
                if point.shape != Shape.INVISIBLE {
                    page.setPenColor(point.strokeColor ?? color)
                    page.setPenWidth(point.strokeWidth)
                    page.setDefaultStrokeDashPattern()
                    page.setBrushColor(point.fillColor)
                    page.drawPoint(point)
                }
            }
        }
    }

    ///
    /// Rounds the axis range to "nice" values for clean grid lines.
    /// Uses the span (max - min) to support negative values and
    /// zero crossings. Rounds max up and min down to step multiples.
    ///
    static func roundMaxAndMinValues(_ maxValue: Float, _ minValue: Float) -> Round {
        var span = maxValue - minValue
        if span <= 0.0 { span = 1.0 }   // guard against flat data

        let exponent = Int(floor(log(Double(span)) / log(10.0)))
        let normalizedSpan = span * Float(pow(10.0, Double(-exponent)))

        // Snap span up to a "nice" value with paired grid line count
        var niceSpan: Float
        var numOfGridLines: Int

        if      normalizedSpan > 9.00 { niceSpan = 10.0; numOfGridLines = 10 }
        else if normalizedSpan > 8.00 { niceSpan =  9.00; numOfGridLines =  9 }
        else if normalizedSpan > 7.00 { niceSpan =  8.00; numOfGridLines =  8 }
        else if normalizedSpan > 6.00 { niceSpan =  7.00; numOfGridLines =  7 }
        else if normalizedSpan > 5.00 { niceSpan =  6.00; numOfGridLines =  6 }
        else if normalizedSpan > 4.00 { niceSpan =  5.00; numOfGridLines =  5 }
        else if normalizedSpan > 3.50 { niceSpan =  4.00; numOfGridLines =  8 }
        else if normalizedSpan > 3.00 { niceSpan =  3.50; numOfGridLines =  7 }
        else if normalizedSpan > 2.50 { niceSpan =  3.00; numOfGridLines =  6 }
        else if normalizedSpan > 2.00 { niceSpan =  2.50; numOfGridLines =  5 }
        else if normalizedSpan > 1.75 { niceSpan =  2.00; numOfGridLines =  8 }
        else if normalizedSpan > 1.50 { niceSpan =  1.75; numOfGridLines =  7 }
        else if normalizedSpan > 1.25 { niceSpan =  1.50; numOfGridLines =  6 }
        else if normalizedSpan > 1.00 { niceSpan =  1.25; numOfGridLines =  5 }
        else                          { niceSpan =  1.00; numOfGridLines = 10 }

        // Scale back to original magnitude and compute step
        let step = niceSpan * Float(pow(10.0, Double(exponent))) / Float(numOfGridLines)

        let round = Round()

        // Round max up, min down to nearest step multiple
        round.maxValue = ceil(maxValue / step) * step
        round.minValue = floor(minValue / step) * step

        // Recount grid lines from actual rounded range
        round.numOfGridLines = Int(((round.maxValue - round.minValue) / step).rounded())

        return round
    }

    /// Sets xMin and xMax for the X axis and the number of X grid lines.
    @discardableResult
    public func setXAxisMinMax(_ xMin: Float, _ xMax: Float, _ xAxisGridLines: Int) -> Chart {
        self.xMin = xMin
        self.xMax = xMax
        self.xAxisGridLines = xAxisGridLines
        return self
    }

    /// Sets yMin and yMax for the Y axis and the number of Y grid lines.
    @discardableResult
    public func setYAxisMinMax(_ yMin: Float, _ yMax: Float, _ yAxisGridLines: Int) -> Chart {
        self.yMin = yMin
        self.yMax = yMax
        self.yAxisGridLines = yAxisGridLines
        return self
    }
}   // End of Chart.swift
