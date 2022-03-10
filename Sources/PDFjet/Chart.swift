/**
 *  Chart.swift
 *
Copyright 2020 Innovatics Inc.
*/
import Foundation


/**
 *  Used to create XY chart objects and draw them on a page.
 *
 *  Please see Example_09.
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

    private var drawXAxisLabels = true
    private var drawYAxisLabels = true

    private var xyChart = true

    private var hGridLineWidth: Float = 0.0
    private var vGridLineWidth: Float = 0.0

    private var hGridLinePattern = "[1 1] 0"
    private var vGridLinePattern = "[1 1] 0"

    private var chartBorderWidth: Float = 0.3
    private var innerBorderWidth: Float = 0.3

    private var formatter = NumberFormatter()
    private var minFractionDigits = 2
    private var maxFractionDigits = 2

    private var f1: Font?
    private var f2: Font?

    public var chartData: [[Point]]?


    /**
     *  Create a XY chart object.
     *
     *  @param f1 the font used for the chart title.
     *  @param f2 the font used for the X and Y axis titles.
     */
    public init(_ f1: Font, _ f2: Font) {
        self.f1 = f1
        self.f2 = f2
    }


    /**
     *  Sets the title of the chart.
     *
     *  @param title the title text.
     */
    public func setTitle(_ title: String) {
        self.title = title
    }


    /**
     *  Sets the title for the X axis.
     *
     *  @param title the X axis title.
     */
    public func setXAxisTitle(_ title: String) {
        self.xAxisTitle = title
    }


    /**
     *  Sets the title for the Y axis.
     *
     *  @param title the Y axis title.
     */
    public func setYAxisTitle(_ title: String) {
        self.yAxisTitle = title
    }


    /**
     *  Sets the data that will be used to draw this chart.
     *
     *  @param chartData the data.
     */
    public func setData(_ chartData: [[Point]]?) {
        self.chartData = chartData
    }


    /**
     *  Returns the chart data.
     *
     *  @return the chart data.
     */
    public func getData() -> [[Point]]? {
        return self.chartData
    }


    /**
     *  Sets the location of this chart on the page.
     *
     *  @param x the x coordinate of the top left corner of this chart when drawn on the page.
     *  @param y the y coordinate of the top left corner of this chart when drawn on the page.
     */
    public func setLocation(_ x: Float, _ y: Float) {
        self.x1 = x
        self.y1 = y
    }


    /**
     *  Sets the size of this chart.
     *
     *  @param w the width of this chart.
     *  @param h the height of this chart.
     */
    public func setSize(_ w: Float, _ h: Float) {
        self.w = w
        self.h = h
    }


    /**
     *  Sets the minimum number of fractions digits do display for the X and Y axis labels.
     *
     *  @param minFractionDigits the minimum number of fraction digits.
     */
    public func setMinimumFractionDigits(_ minFractionDigits: Int) {
        self.minFractionDigits = minFractionDigits
    }


    /**
     *  Sets the maximum number of fractions digits do display for the X and Y axis labels.
     *
     *  @param maxFractionDigits the maximum number of fraction digits.
     */
    public func setMaximumFractionDigits(_ maxFractionDigits: Int) {
        self.maxFractionDigits = maxFractionDigits
    }


    /**
     *  Calculates the slope of a trend line given a list of points.
     *  See Example_09.
     *
     *  @param points the list of points.
     *  @return the slope float value.
     */
    public func slope(_ points: [Point])-> Float {
        return (covar(points) / devsq(points) * Float(points.count - 1))
    }


    /**
     *  Calculates the intercept of a trend line given a list of points.
     *  See Example_09.
     *
     *  @param points the list of points.
     *  @return the intercept float value.
     */
    public func intercept(_ points: [Point], _ slope: Double)-> Float {
        return intercept(points, Float(slope))
    }


    /**
     *  Calculates the intercept of a trend line given a list of points.
     *  See Example_09.
     *
     *  @param points the list of points.
     *  @return the intercept float value.
     */
    public func intercept(_ points: [Point], _ slope: Float)-> Float {
        let _mean: [Float] = mean(points)
        return (_mean[1] - slope * _mean[0])
    }


    public func setDrawXAxisLabels(_ drawXAxisLabels: Bool) {
        self.drawXAxisLabels = drawXAxisLabels
    }


    public func setDrawYAxisLabels(_ drawYAxisLabels: Bool) {
        self.drawYAxisLabels = drawYAxisLabels
    }


    public func setXYChart(_ xyChart: Bool) {
        self.xyChart = xyChart
    }

    public func setPosition(_ x: Float, _ y: Float) {
        setLocation(x, y)
    }

    /**
     *  Draws this chart on the specified page.
     *
     *  @param page the page to draw this chart on.
     */
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {

        formatter.minimumFractionDigits = minFractionDigits
        formatter.maximumFractionDigits = maxFractionDigits

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

            drawHorizontalGridLines(page!)
            drawVerticalGridLines(page!)

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
                    point.lineWidth *= (x6 - x5) / w
                }
                else {
                    point.x = x5 + point.x * (x6 - x5) / w
                    point.y = y8 - (point.y - yMin) * (y8 - y5) / (yMax - yMin)
                }
                if point.getURIAction() != nil {
                    if page != nil {
                        page!.addAnnotation(Annotation(
                                point.getURIAction(),
                                nil,
                                point.x - point.r,
                                point.y - point.r,
                                point.x + point.r,
                                point.y + point.r,
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
                    f1!,
                    yAxisTitle,
                    x1 + f1!.bodyHeight,
                    y8 - ((y8 - y5) - f1!.stringWidth(yAxisTitle)) / 2)

            // Draw the X axis title
            page!.setTextDirection(0)
            page!.drawString(
                    f1!,
                    xAxisTitle,
                    x5 + ((x6 - x5) - f1!.stringWidth(xAxisTitle)) / 2,
                    y4 - f1!.bodyHeight / 2)

            page!.setDefaultLineWidth()
            page!.setDefaultLinePattern()
            page!.setPenColor(Color.black)
        }

        return [self.x1 + self.w, self.y1 + self.h]
    }


    private func getLongestAxisYLabelWidth()-> Float {
        let minLabelWidth =
                // f2!.stringWidth(String(format: "%04X", yMin) + "0")
                f2!.stringWidth(formatter.string(from: NSNumber(value: yMin))! + "0")
        let maxLabelWidth =
                // f2!.stringWidth(String(format: "%04X", yMax) + "0")
                f2!.stringWidth(formatter.string(from: NSNumber(value: yMax))! + "0")
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
        let round = roundMaxAndMinValues(&xMax, &xMin)
        xMax = round.maxValue
        xMin = round.minValue
        xAxisGridLines = round.numOfGridLines
    }


    private func roundYAxisMinAndMaxValues() {
        let round = roundMaxAndMinValues(&yMax, &yMin)
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
        page.strokePath()
    }


    private func drawInnerBorder(_ page: Page) {
        page.setPenWidth(innerBorderWidth)
        page.setPenColor(Color.black)
        page.moveTo(x5, y5)
        page.lineTo(x6, y6)
        page.lineTo(x7, y7)
        page.lineTo(x8, y8)
        page.closePath()
        page.strokePath()
    }


    private func drawHorizontalGridLines(_ page: Page) {
        page.setPenWidth(hGridLineWidth)
        page.setPenColor(Color.black)
        page.setLinePattern(hGridLinePattern)
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
        page.setLinePattern(vGridLinePattern)
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
        let y = y8 + f2!.bodyHeight
        let step = (x6 - x5) / Float(xAxisGridLines)
        page.setBrushColor(Color.black)
        var i = 0
        while i < (xAxisGridLines + 1) {
            let label = formatter.string(from: NSNumber(value:
                    xMin + ((xMax - xMin) / Float(xAxisGridLines)) * Float(i)))!
            page.drawString(
                    f2!, label, x - (f2!.stringWidth(label) / 2), y)
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
            let label = formatter.string(from: NSNumber(value:
                    yMin + ((yMax - yMin) / Float(yAxisGridLines)) * Float(i)))!
            page.drawString(f2!, label, x, y)
            y -= step
            i += 1
        }
    }


    private func drawPathsAndPoints(
            _ page: Page, _ chartData: [[Point]]) {

        for points in chartData {
            if points.count > 0 {
                let point = points[0]
                if point.drawPath {
                    page.setPenColor(point.color)
                    page.setPenWidth(point.lineWidth)
                    page.setLinePattern(point.linePattern)
                    page.drawPath(points, Operation.STROKE)
                    if point.getText() != nil {
                        page.setBrushColor(point.getTextColor())
                        page.setTextDirection(point.getTextDirection())
                        page.drawString(f2!, point.getText(), point.x, point.y)
                    }
                }
                for point in points {
                    if point.getShape() != Point.INVISIBLE {
                        page.setPenWidth(point.lineWidth)
                        page.setLinePattern(point.linePattern)
                        page.setPenColor(point.color)
                        page.setBrushColor(point.color)
                        page.drawPoint(point)
                    }
                }
            }
        }
    }


    private func roundMaxAndMinValues(
            _ maxValue: inout Float,
            _ minValue: inout Float)-> Round {

        let maxExponent = Int(floor(log(maxValue) / log(10)))
        maxValue *= Float(pow(Double(10), Double(-maxExponent)))

        if      maxValue > 9.00 { maxValue = 10.0 }
        else if maxValue > 8.00 { maxValue = 9.00 }
        else if maxValue > 7.00 { maxValue = 8.00 }
        else if maxValue > 6.00 { maxValue = 7.00 }
        else if maxValue > 5.00 { maxValue = 6.00 }
        else if maxValue > 4.00 { maxValue = 5.00 }
        else if maxValue > 3.50 { maxValue = 4.00 }
        else if maxValue > 3.00 { maxValue = 3.50 }
        else if maxValue > 2.50 { maxValue = 3.00 }
        else if maxValue > 2.00 { maxValue = 2.50 }
        else if maxValue > 1.75 { maxValue = 2.00 }
        else if maxValue > 1.50 { maxValue = 1.75 }
        else if maxValue > 1.25 { maxValue = 1.50 }
        else if maxValue > 1.00 { maxValue = 1.25 }
        else                    { maxValue = 1.00 }

        let round = Round()

        if      maxValue == 10.0 { round.numOfGridLines = 10 }
        else if maxValue == 9.00 { round.numOfGridLines =  9 }
        else if maxValue == 8.00 { round.numOfGridLines =  8 }
        else if maxValue == 7.00 { round.numOfGridLines =  7 }
        else if maxValue == 6.00 { round.numOfGridLines =  6 }
        else if maxValue == 5.00 { round.numOfGridLines =  5 }
        else if maxValue == 4.00 { round.numOfGridLines =  8 }
        else if maxValue == 3.50 { round.numOfGridLines =  7 }
        else if maxValue == 3.00 { round.numOfGridLines =  6 }
        else if maxValue == 2.50 { round.numOfGridLines =  5 }
        else if maxValue == 2.00 { round.numOfGridLines =  8 }
        else if maxValue == 1.75 { round.numOfGridLines =  7 }
        else if maxValue == 1.50 { round.numOfGridLines =  6 }
        else if maxValue == 1.25 { round.numOfGridLines =  5 }
        else if maxValue == 1.00 { round.numOfGridLines = 10 }

        round.maxValue = maxValue * (Float(pow(Double(10), Double(maxExponent))))
        let step = Float(round.maxValue) / Float(round.numOfGridLines)
        var temp = round.maxValue
        round.numOfGridLines = 0
        while true {
            round.numOfGridLines += 1
            temp -= step
            if temp <= minValue {
                round.minValue = temp
                break
            }
        }

        return round
    }


    private func mean(_ points: [Point])-> [Float] {
        var _mean = [Float](repeating: 0, count: 2)
        for point in points {
            _mean[0] += point.x
            _mean[1] += point.y
        }
        _mean[0] /= Float(points.count - 1)
        _mean[1] /= Float(points.count - 1)
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
     *
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
    public func setXAxisMinMax(_ xMin: Float, _ xMax: Float, _ xAxisGridLines: Int) {
        self.xMin = xMin
        self.xMax = xMax
        self.xAxisGridLines = xAxisGridLines
    }


    /// Sets yMin and yMax for the Y axis and the number of Y grid lines.
    public func setYAxisMinMax(_ yMin: Float, _ yMax: Float, _ yAxisGridLines: Int) {
        self.yMin = yMin
        self.yMax = yMax
        self.yAxisGridLines = yAxisGridLines
    }

}   // End of Chart.swift
