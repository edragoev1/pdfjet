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

    private var xMax = Float.leastNonzeroMagnitude
    private var xMin = Float.greatestFiniteMagnitude
    private var yMax = Float.leastNonzeroMagnitude
    private var yMin = Float.greatestFiniteMagnitude

    private var xAxisGridLines = 0
    private var yAxisGridLines = 0

    private var title = ""
    private var xAxisTitle = ""
    private var yAxisTitle = ""

    private var drawXAxisLines = true
    private var drawYAxisLines = true
    private var drawXAxisLabels = true
    private var drawYAxisLabels = true

    private var xyChart = true

    private var hGridLineWidth: Float = 0.0
    private var vGridLineWidth: Float = 0.0

    private var hGridLinePattern = "[1 1] 0"
    private var vGridLinePattern = "[1 1] 0"

    private var chartBorderWidth: Float = 0.0
    private var innerBorderWidth: Float = 0.0

    private var minFractionDigits = 2
    private var maxFractionDigits = 2

    private var f1: Font?
    private var f2: Font?
    private var fontSize: Float = 8.0

    /// The data series of this chart, one array of points per series.
    private var chartData: [[Point]]?

    private static let DEFAULT_PALETTE = [
        Color.blue,
        Color.red,
        Color.green,
        Color.orange,
        Color.purple,
        Color.darkcyan,
        Color.magenta,
        Color.olive
    ]
    private var autoColors = true

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
     * Sets the data that will be used to draw this chart.
     *
     * - Parameter chartData: the data.
     * - Returns: this Chart object.
     */
    @discardableResult
    public func setData(_ chartData: [[Point]]?) -> Chart {
        self.chartData = chartData
        return self
    }

    /**
     * Returns the chart data.
     *
     * - Returns: the chart data.
     */
    public func getData() -> [[Point]]? {
        return self.chartData
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
     * Sets the minimum number of fractions digits do display for the X and Y axis labels.
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
     * Sets the maximum number of fractions digits do display for the X and Y axis labels.
     *
     * - Parameter maxFractionDigits: the maximum number of fraction digits.
     * - Returns: this Chart object.
     */
    @discardableResult
    public func setMaximumFractionDigits(_ maxFractionDigits: Int) -> Chart {
        self.maxFractionDigits = maxFractionDigits
        return self
    }

    /**
     * Calculates the slope of a trend line given a list of points.
     * See Example_09.
     *
     * - Parameter points: the list of points.
     * - Returns: the slope float value.
     */
    public func slope(_ points: [Point])-> Float {
        return (covar(points) / devsq(points) * Float(points.count - 1))
    }

    /**
     * Calculates the intercept of a trend line given a list of points.
     * See Example_09.
     *
     * - Parameter points: the list of points.
     * - Parameter slope: the slope of the trend line.
     * - Returns: the intercept float value.
     */
    public func intercept(_ points: [Point], _ slope: Double)-> Float {
        return intercept(points, Float(slope))
    }

    /**
     * Calculates the intercept of a trend line given a list of points.
     * See Example_09.
     *
     * - Parameter points: the list of points.
     * - Parameter slope: the slope of the trend line.
     * - Returns: the intercept float value.
     */
    public func intercept(_ points: [Point], _ slope: Float)-> Float {
        let _mean: [Float] = mean(points)
        return (_mean[1] - slope * _mean[0])
    }

    /** Toggles drawing of horizontal grid lines. */
    @discardableResult
    public func setDrawXAxisLines(_ drawXAxisLines: Bool) -> Chart {
        self.drawXAxisLines = drawXAxisLines
        return self
    }

    /** Toggles drawing of vertical grid lines. */
    @discardableResult
    public func setDrawYAxisLines(_ drawYAxisLines: Bool) -> Chart {
        self.drawYAxisLines = drawYAxisLines
        return self
    }

    /// Sets the font size used for the axis labels and point text.
    @discardableResult
    public func setFontSize(_ fontSize: Float) -> Chart {
        self.fontSize = fontSize
        return self
    }

    /// Sets the width of the chart border.
    @discardableResult
    public func setChartBorderWidth(_ width: Float) -> Chart {
        self.chartBorderWidth = width
        return self
    }

    /// Sets the width of the inner border.
    @discardableResult
    public func setInnerBorderWidth(_ width: Float) -> Chart {
        self.innerBorderWidth = width
        return self
    }

    /// Sets the width of the horizontal grid lines.
    @discardableResult
    public func setHGridLineWidth(_ width: Float) -> Chart {
        self.hGridLineWidth = width
        return self
    }

    /// Sets the width of the vertical grid lines.
    @discardableResult
    public func setVGridLineWidth(_ width: Float) -> Chart {
        self.vGridLineWidth = width
        return self
    }

    /// Sets the horizontal grid line dash pattern, e.g. "[1 1] 0".
    @discardableResult
    public func setHGridLinePattern(_ pattern: String) -> Chart {
        self.hGridLinePattern = pattern
        return self
    }

    /// Sets the vertical grid line dash pattern, e.g. "[1 1] 0".
    @discardableResult
    public func setVGridLinePattern(_ pattern: String) -> Chart {
        self.vGridLinePattern = pattern
        return self
    }

    /// Toggles the automatic stroke colors for the data series.
    @discardableResult
    public func setAutoColors(_ autoColors: Bool) -> Chart {
        self.autoColors = autoColors
        return self
    }

    /// Converts a 0xRRGGBB color to red, green and blue values between 0.0 and 1.0.
    public func toFloatArray(_ color: Int32) -> [Float] {
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

    /// Sets whether this is an XY scatter chart rather than a category chart.
    @discardableResult
    public func setXYChart(_ xyChart: Bool) -> Chart {
        self.xyChart = xyChart
        return self
    }

    /**
     * Draws this chart on the specified page.
     *
     * - Parameter page: the page to draw this chart on.
     */
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        x2 = x1 + w
        y2 = y1

        x3 = x2
        y3 = y1 + h

        x4 = x1
        y4 = y3

        setXAxisMinAndMaxChartValues()
        setYAxisMinAndMaxChartValues()
        roundXAxisMinAndMaxValues()
        roundYAxisMinAndMaxValues()

        // Draw chart title
        if page != nil {
            page!.drawString(
                    f1!,
                    fontSize,
                    title,
                    x1 + ((w - f1!.stringWidth(title)) / 2),
                    y1 + 1.5 * f1!.bodyHeight)
        }

        let topMargin = 2.5 * f1!.bodyHeight
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

            if drawXAxisLines {
                drawHorizontalGridLines(page!)
            }
            if drawYAxisLines {
                drawVerticalGridLines(page!)
            }
            if drawXAxisLabels {
                drawXAxisLabels(page!)
            }
            if drawYAxisLabels {
                drawYAxisLabels(page!)
            }
        }

        // Translate the point coordinates
        for points in chartData! {
            for point in points {
                if xyChart {
                    point.x = x5 + (point.x - xMin) * (x6 - x5) / (xMax - xMin)
                    point.y = y8 - (point.y - yMin) * (y8 - y5) / (yMax - yMin)
                    point.strokeWidth *= (x6 - x5) / w
                } else {
                    point.x = x5 + point.x * (x6 - x5) / w
                    point.y = y8 - (point.y - yMin) * (y8 - y5) / (yMax - yMin)
                }
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
                                0.0,    // Transparency
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
            drawPathsAndPoints(page!, chartData!)

            // Draw the Y axis title
            page!.setBrushColor(Color.black)
            page!.setTextDirection(90)
            page!.drawString(
                    f2!,
                    fontSize,
                    yAxisTitle,
                    x1 + f2!.bodyHeight,
                    y8 - ((y8 - y5) - f2!.stringWidth(yAxisTitle)) / 2)

            // Draw the X axis title
            page!.setTextDirection(0)
            page!.setBrushColor(Color.black)
            page!.drawString(
                    f2!,
                    fontSize,
                    xAxisTitle,
                    x5 + ((x6 - x5) - f2!.stringWidth(xAxisTitle)) / 2,
                    y4 - f2!.bodyHeight / 2)

            page!.setDefaultLineWidth()
            page!.setDefaultStrokeDashPattern()
            page!.setPenColor(Color.black)
        }

        return [self.x1 + self.w, self.y1 + self.h]
    }

    // Formats an axis label with minFractionDigits to maxFractionDigits
    // decimal places, rounding half to even like NumberFormat in Java.
    private func format(_ value: Float) -> String {
        var scale: Double = 1.0
        for _ in 0..<maxFractionDigits {
            scale *= 10.0
        }
        let scaled = (Double(value) * scale).rounded(.toNearestOrEven)
        let units = Int64(scaled.magnitude)
        let integer = units / Int64(scale)
        var fraction = String(units % Int64(scale))
        while fraction.count < maxFractionDigits {
            fraction = "0" + fraction
        }
        while fraction.count > minFractionDigits && fraction.hasSuffix("0") {
            fraction.removeLast()
        }
        var label = (value.sign == .minus ? "-" : "") + String(integer)
        if !fraction.isEmpty {
            label += "." + fraction
        }
        return label
    }

    private func getLongestAxisYLabelWidth()-> Float {
        let minLabelWidth = f2!.stringWidth(format(yMin) + "0")
        let maxLabelWidth = f2!.stringWidth(format(yMax) + "0")
        if maxLabelWidth > minLabelWidth {
            return maxLabelWidth
        }
        return minLabelWidth
    }

    private func setXAxisMinAndMaxChartValues() {
        if xAxisGridLines != 0 {
            return
        }
        for points in chartData! {
            for point in points {
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
        for points in chartData! {
            for point in points {
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
        let round = roundMaxAndMinValues(xMax, xMin)
        xMax = round.maxValue
        xMin = round.minValue
        xAxisGridLines = round.numOfGridLines
    }

    private func roundYAxisMinAndMaxValues() {
        if yAxisGridLines != 0 {
            return
        }
        let round = roundMaxAndMinValues(yMax, yMin)
        yMax = round.maxValue
        yMin = round.minValue
        yAxisGridLines = round.numOfGridLines
    }

    private func drawChartBorder(_ page: Page) {
        page.setPenWidth(chartBorderWidth)
        page.setPenColor(Color.black)
        page.moveTo(x1, y1)
        page.lineTo(x2, y2)
        page.lineTo(x3, y3)
        page.lineTo(x4, y4)
        page.closePath()
    }

    private func drawInnerBorder(_ page: Page) {
        page.setPenWidth(innerBorderWidth)
        page.setPenColor(Color.black)
        page.moveTo(x5, y5)
        page.lineTo(x6, y6)
        page.lineTo(x7, y7)
        page.lineTo(x8, y8)
        page.closePath()
    }

    private func drawHorizontalGridLines(_ page: Page) {
        page.setPenWidth(hGridLineWidth)
        page.setPenColor(Color.black)
        page.setStrokeDashPattern(hGridLinePattern)
        let x = x8
        var y = y8
        let step = (y8 - y5) / Float(yAxisGridLines)
        for _ in 0..<yAxisGridLines {
            page.drawLine(x, y, x6, y)
            y -= step
        }
    }

    private func drawVerticalGridLines(_ page: Page) {
        page.setPenWidth(vGridLineWidth)
        page.setPenColor(Color.black)
        page.setStrokeDashPattern(vGridLinePattern)
        var x = x5
        let y = y5
        let step = (x6 - x5) / Float(xAxisGridLines)
        for _ in 0..<xAxisGridLines {
            page.drawLine(x, y, x, y8)
            x += step
        }
    }

    private func drawXAxisLabels(_ page: Page) {
        var x = x5
        let y = y8 + f2!.getBodyHeight(f2!.getSize())
        let step = (x6 - x5) / Float(xAxisGridLines)
        page.setBrushColor(Color.black)
        var i = 0
        while i < (xAxisGridLines + 1) {
            let label = format(xMin + ((xMax - xMin) / Float(xAxisGridLines)) * Float(i))
            page.drawString(
                    f2!,
                    fontSize,
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
        page.setBrushColor(Color.black)
        var i = 0
        while i < (yAxisGridLines + 1) {
            let label = format(yMin + ((yMax - yMin) / Float(yAxisGridLines)) * Float(i))
            page.drawString(
                    f2!,
                    fontSize,
                    label,
                    x,
                    y)
            y -= step
            i += 1
        }
    }

    private func drawPathsAndPoints(
            _ page: Page, _ chartData: [[Point]]) {
        var seriesIndex = 0
        for points in chartData {
            if points.count > 0 {
                let p0 = points[0]
                if p0.drawPath {
                    if autoColors && p0.strokeColor == nil {
                        let index = seriesIndex % Chart.DEFAULT_PALETTE.count
                        p0.strokeColor = toFloatArray(Chart.DEFAULT_PALETTE[index])
                    }
                    page.setPenColor(p0.strokeColor)
                    page.setPenWidth(p0.strokeWidth)
                    page.setStrokeDashPattern(p0.strokeDashPattern)
                    page.drawPath(points, PathOperator.stroke)
                    if p0.getText() != nil {
                        page.setBrushColor(p0.getTextColor())
                        page.setTextDirection(p0.getTextDirection())
                        page.drawString(
                            f2!,
                            fontSize,
                            p0.getText(),
                            p0.x + (p0.strokeWidth - f2!.getAscent())/2.0,
                            p0.y,
                            p0.getTextColor(),
                            nil)
                    }
                }
                for point in points {
                    if point.getShape() != Point.INVISIBLE {
                        page.setPenColor(point.strokeColor)
                        page.setPenWidth(point.strokeWidth)
                        page.setStrokeDashPattern(point.strokeDashPattern)
                        page.setBrushColor(point.fillColor)
                        page.drawPoint(point)
                    }
                }
            }
            seriesIndex += 1
        }
    }

    ///
    /// Rounds the axis range to "nice" values for clean grid lines.
    /// Uses the span (max - min) to support negative values and
    /// zero crossings. Rounds max up and min down to step multiples.
    ///
    private func roundMaxAndMinValues(_ maxValue: Float, _ minValue: Float) -> Round {
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

    private func mean(_ points: [Point])-> [Float] {
        var _mean = [Float](repeating: 0, count: 2)
        for point in points {
            _mean[0] += point.x
            _mean[1] += point.y
        }
        let n = Float(points.count)
        _mean[0] /= n
        _mean[1] /= n
        return _mean
    }

    private func covar(_ points: [Point])-> Float {
        var covariance: Float = 0.0
        let _mean = mean(points)
        for point in points {
            covariance += (point.x - _mean[0]) * (point.y - _mean[1])
        }
        return (covariance / Float(points.count - 1))
    }

    /**
     * devsq() returns the sum of squares of deviations.
     */
    private func devsq(_ points: [Point])-> Float {
        var _devsq: Float = 0.0
        let _mean = mean(points)
        for point in points {
            _devsq += Float(pow(Double(point.x - _mean[0]), 2))
        }
        return _devsq
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
