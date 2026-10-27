/**
 * SimpleScatterChart.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// A chart of points on two axes that shows how two values go together: many
/// points as small gray dots, a little transparent so that a cluster shows
/// darker, a few of them highlighted, navy and labeled with their names beside
/// them, and the trend line of the points if asked, labeled at its end. A light
/// horizontal grid alone, no border or legend, on a light panel, with a title
/// and a subtitle at the top left, in the look of SimpleBarChart.
///
/// In a PDF/UA document the chart is one figure, described from its data: the
/// number of points, the highlighted ones and their values, and which way the
/// trend goes, unless an alternate description is set. It draws with the page
/// alone, and needs no other chart. In PDF/A-1, which has no transparency, the
/// dots are drawn opaque. Please see Example_26.
///
public class SimpleScatterChart : Drawable {
    // The colors of a simple scatter chart unless set, those of
    // SimpleBarChart: a light panel, gray dots a little transparent, the
    // highlighted points navy, the grid a light gray, the trend line and the
    // numbers dark gray.
    static let DOT: Int32 = 0x8C96A3
    static let GRID: Int32 = 0xD5D9DE
    static let TREND: Int32 = 0x5F6873
    static let ALPHA: Float = 0.55

    // A point of the chart, highlighted when it has a name.
    struct Point {
        let x: Float
        let y: Float
        let name: String
        var uri: String? = nil      // The link of a highlighted point, or nil
    }

    private let f1: Font    // The title and the names
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
    private var points = [Point]()
    private var trendLine = false
    private var xMin: Float = 0.0
    private var xMax: Float = 0.0
    private var yMin: Float = 0.0
    private var yMax: Float = 0.0
    private var xRangeSet = false
    private var yRangeSet = false
    private var panelColor = SimpleBarChart.PANEL
    private var dotColor = SimpleScatterChart.DOT
    private var highlightColor = SimpleBarChart.HIGHLIGHT
    private var maxFractionDigits = 2       // As many as the steps of the axes need

    ///
    /// Creates a simple scatter chart, 400 by 300 points.
    ///
    /// - Parameter f1: the font of the title and of the names of the
    ///   highlighted points, a bold one.
    /// - Parameter f2: the font of the subtitle, the axis titles and the numbers.
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
    public func setTitle(_ title: String) -> SimpleScatterChart {
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
    public func setSubtitle(_ subtitle: String) -> SimpleScatterChart {
        self.subtitle = subtitle
        return self
    }

    ///
    /// Sets the title of the horizontal axis, drawn under it.
    ///
    /// - Parameter title: the title.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setXAxisTitle(_ title: String) -> SimpleScatterChart {
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
    public func setYAxisTitle(_ title: String) -> SimpleScatterChart {
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
    public func setAltDescription(_ altDescription: String) -> SimpleScatterChart {
        self.altDescription = altDescription
        return self
    }

    ///
    /// Adds a point, drawn as a small gray dot. A point whose x or y is not a
    /// number is left out.
    ///
    /// - Parameter x: the x of the point.
    /// - Parameter y: the y of the point.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func add(_ x: Float, _ y: Float) -> SimpleScatterChart {
        points.append(Point(x: x, y: y, name: ""))
        return self
    }

    ///
    /// Adds a point that is highlighted: a navy dot with its name beside it.
    ///
    /// - Parameter name: the name of the point.
    /// - Parameter x: the x of the point.
    /// - Parameter y: the y of the point.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func add(_ name: String, _ x: Float, _ y: Float) -> SimpleScatterChart {
        points.append(Point(x: x, y: y, name: name))
        return self
    }

    ///
    /// Adds a point that is highlighted and is a link: a navy dot with its name
    /// beside it, both of them the link to the URI. In a PDF/UA document each
    /// such point is a figure of its own, described by its name, in the Link
    /// that holds its annotation, as PDF/UA asks.
    ///
    /// - Parameter name: the name of the point.
    /// - Parameter x: the x of the point.
    /// - Parameter y: the y of the point.
    /// - Parameter uri: the URI the point links to.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func add(_ name: String, _ x: Float, _ y: Float, _ uri: String) -> SimpleScatterChart {
        // An empty URI is no link, as in the Go port
        points.append(Point(x: x, y: y, name: name, uri: uri.isEmpty ? nil : uri))
        return self
    }

    ///
    /// Draws the trend line of the points, their least squares line, thin and
    /// dark gray, labeled "trend" at its right end.
    ///
    /// - Parameter trendLine: true to draw it.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setTrendLine(_ trendLine: Bool) -> SimpleScatterChart {
        self.trendLine = trendLine
        return self
    }

    ///
    /// Sets the range of the horizontal axis. Without it the range is that of
    /// the points, rounded to steps. A range whose end is not past its start
    /// is not set.
    ///
    /// - Parameter min: the first value of the axis.
    /// - Parameter max: the last value of the axis.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setXAxisRange(_ min: Float, _ max: Float) -> SimpleScatterChart {
        self.xMin = min
        self.xMax = max
        self.xRangeSet = max > min
        return self
    }

    ///
    /// Sets the range of the vertical axis. Without it the range is that of the
    /// points, rounded to steps. A range whose end is not past its start is not
    /// set.
    ///
    /// - Parameter min: the first value of the axis.
    /// - Parameter max: the last value of the axis.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setYAxisRange(_ min: Float, _ max: Float) -> SimpleScatterChart {
        self.yMin = min
        self.yMax = max
        self.yRangeSet = max > min
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
    public func setPanelColor(_ color: Int32) -> SimpleScatterChart {
        self.panelColor = color
        return self
    }

    ///
    /// Sets the color of the points that are not highlighted. It is gray,
    /// 0x8C96A3, unless set.
    ///
    /// - Parameter color: the color.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setDotColor(_ color: Int32) -> SimpleScatterChart {
        self.dotColor = color
        return self
    }

    ///
    /// Sets the color of the highlighted points and of their names. It is
    /// navy, 0x1F3A5F, unless set.
    ///
    /// - Parameter color: the color.
    /// - Returns: this chart.
    ///
    @discardableResult
    public func setHighlightColor(_ color: Int32) -> SimpleScatterChart {
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
    public func setSize(_ w: Float, _ h: Float) -> SimpleScatterChart {
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

    // A number of an axis, with the decimals its step needs.
    private func label(_ value: Float, _ step: Float) -> String {
        return ChartScale.groupDigits(
                ChartScale.format(value, ChartScale.fractionDigitsOf(step, maxFractionDigits), maxFractionDigits))
    }

    // The points whose x and y are numbers.
    private func valid() -> [Point] {
        return points.filter { ChartScale.isValue($0.x) && ChartScale.isValue($0.y) }
    }

    // The slope and the intercept of the least squares line of the points, or
    // nil when there is none: fewer than two points, or all at one x.
    static func trend(_ points: [Point]) -> (slope: Double, intercept: Double)? {
        let n = Double(points.count)
        if n < 2 {
            return nil
        }
        // From the sums of the distances from the means, which keep their
        // precision where the raw sums lost it, for x near 1e8 of a small spread
        var sx = 0.0
        var sy = 0.0
        for p in points {
            sx += Double(p.x)
            sy += Double(p.y)
        }
        let mx = sx / n
        let my = sy / n
        var sxx = 0.0
        var sxy = 0.0
        for p in points {
            let dx = Double(p.x) - mx
            sxx += dx * dx
            sxy += dx * (Double(p.y) - my)
        }
        if sxx == 0 {
            return nil
        }
        let slope = sxy / sxx
        return (slope, my - slope * mx)
    }

    // The horizontal and the vertical axis of the points.
    private func axes(_ points: [Point]) -> (x: ChartScale.Axis, y: ChartScale.Axis) {
        return (ChartScale.axisOf(points.map { $0.x }, xMin, xMax, xRangeSet),
                ChartScale.axisOf(points.map { $0.y }, yMin, yMax, yRangeSet))
    }

    // The trend of the points as it is drawn: none where it misses the plot,
    // both of its ends past one edge, as a flat one over a range that is set,
    // which was drawn outside the chart.
    private static func shownTrend(_ points: [Point], _ xAxis: ChartScale.Axis, _ yAxis: ChartScale.Axis)
            -> (slope: Double, intercept: Double)? {
        if let line = trend(points) {
            let a = line.slope * Double(xAxis.min) + line.intercept
            let b = line.slope * Double(xAxis.max) + line.intercept
            if (a > Double(yAxis.max) && b > Double(yAxis.max)) || (a < Double(yAxis.min) && b < Double(yAxis.min)) {
                return nil
            }
            return line
        }
        return nil
    }

    // The alternate description: the one set, or the title, the number of
    // points, the highlighted ones and the trend.
    func getAltDescription() -> String {
        if !altDescription.isEmpty {
            return altDescription
        }
        let points = valid()
        let axes = points.isEmpty ? nil : self.axes(points)
        if let axes = axes {
            maxFractionDigits = Swift.max(ChartScale.digitsOf((axes.x.max - axes.x.min) / Float(axes.x.lines)),
                    ChartScale.digitsOf((axes.y.max - axes.y.min) / Float(axes.y.lines)))
        }
        // The points that are drawn: not those outside a range that is set
        var shown = [Point]()
        if let axes = axes {
            shown = points.filter { ChartScale.onAxis($0.x, axes.x) && ChartScale.onAxis($0.y, axes.y) }
        }
        var text = ChartScale.join(title, subtitle)
        if text.isEmpty {
            text = "Scatter chart"
        }
        text += ": \(shown.count) points"
        if !xAxisTitle.isEmpty && !yAxisTitle.isEmpty {
            text += ", " + yAxisTitle + " against " + xAxisTitle
        }
        var named = 0
        for p in shown where !p.name.isEmpty {
            text += (named == 0) ? "; " : ", "
            text += p.name + " " + ChartScale.groupDigits(ChartScale.format(p.x, 0, maxFractionDigits))
                    + " and " + ChartScale.groupDigits(ChartScale.format(p.y, 0, maxFractionDigits))
            named += 1
        }
        // Not one that is not drawn
        if trendLine, let axes = axes, let line = SimpleScatterChart.shownTrend(points, axes.x, axes.y) {
            text += line.slope > 0 ? "; the trend rises" : line.slope < 0 ? "; the trend falls" : "; the trend is flat"
        }
        return text + "."
    }

    ///
    /// Draws the chart on the page. A chart too small for its plot and its
    /// numbers draws nothing.
    ///
    /// - Parameter page: the page to draw on.
    /// - Returns: the x and y coordinates of the bottom right corner of the chart.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        let points = valid()
        guard let page = page, !points.isEmpty else {   // Measured, or nothing to draw
            return [x + w, y + h]
        }
        let size = f2.getSize()
        let pad = pad()
        let ascent = f2.getAscent(size)

        let xs = points.map { $0.x }
        let ys = points.map { $0.y }
        if (!xRangeSet && !ChartScale.spanFits(xs)) || (!yRangeSet && !ChartScale.spanFits(ys)) {
            return [x + w, y + h]       // A span past the largest float, which no axis can draw
        }
        let (xAxis, yAxis) = axes(points)
        let xStep = (xAxis.max - xAxis.min) / Float(xAxis.lines)
        let yStep = (yAxis.max - yAxis.min) / Float(yAxis.lines)
        maxFractionDigits = Swift.max(ChartScale.digitsOf(xStep), ChartScale.digitsOf(yStep))

        // The plot: right of the y title and the widest number, above the
        // numbers and the x title
        var widest: Float = 0.0
        for i in 0...yAxis.lines {
            widest = Swift.max(widest, f2.stringWidth(size, label(yAxis.min + yStep * Float(i), yStep)))
        }
        var left = x + pad + widest + size / 2.0
        if !yAxisTitle.isEmpty {
            left += 1.8 * size      // The title, as far from the numbers as the x title is
        }
        // Room for the halves of the first and the last number under the plot,
        // centered on its edges: the last, "1,000,000", went past the chart
        let firstWidth = f2.stringWidth(size, label(xAxis.min, xStep))
        let lastWidth = f2.stringWidth(size, label(xAxis.min + xStep * Float(xAxis.lines), xStep))
        left = Swift.max(left, x + pad / 2.0 + firstWidth / 2.0)
        let right = Swift.min(x + w - pad, x + w - pad / 2.0 - lastWidth / 2.0)
        let top = y + self.top() + ascent / 2.0     // Room for the top number
        var bottom = y + h - pad - 1.4 * size
        if !xAxisTitle.isEmpty {
            bottom -= 1.4 * size
        }
        if right <= left || bottom <= top || !ChartScale.drawable(xAxis) || !ChartScale.drawable(yAxis) {
            return [x + w, y + h]
        }
        let plotLeft = left
        let plotBottom = bottom
        // Tells if the point is on the plot: a point outside a range that is
        // set is not drawn
        func onPlot(_ p: Point) -> Bool {
            return ChartScale.onAxis(p.x, xAxis) && ChartScale.onAxis(p.y, yAxis)
        }
        func xAt(_ v: Float) -> Float {
            return plotLeft + ChartScale.scaled(v - xAxis.min, right - plotLeft, xAxis.max - xAxis.min)
        }
        func yAt(_ v: Float) -> Float {
            return plotBottom - ChartScale.scaled(v - yAxis.min, plotBottom - top, yAxis.max - yAxis.min)
        }

        // In a tagged document a point that is a link is drawn after the
        // chart, as a figure of its own in its Link, which holds its
        // annotation, as PDF/UA asks: a link inside the chart's figure has
        // nothing to hold
        let linksApart = page.isTaggedHere()

        page.addBDC(StructElem.FIGURE, nil, getAltDescription())
        page.saveGraphicsState()
        page.setDefaultStrokeDashPattern()      // Not the caller's, which dashed the lines

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

        // The horizontal grid and the numbers of both axes, each where it
        // leaves the one before it room
        page.setPenColor(SimpleScatterChart.GRID)
        page.setPenWidth(0.5)
        var every = ChartScale.axisLabelStep((plotBottom - top) / Float(yAxis.lines), f2.getBodyHeight(), yAxis.lines)
        for i in 0...yAxis.lines {
            let v = yAxis.min + yStep * Float(i)
            let gy = yAt(v)
            page.drawLine(plotLeft, gy, right, gy)
            if i % every == 0 {
                let label = self.label(v, yStep)
                page.drawString(f2, size, label, plotLeft - size / 2.0 - f2.stringWidth(size, label),
                        gy + ascent / 2.0, Util.toRGB(SimpleBarChart.MUTED), nil)
            }
        }
        var xWidest: Float = 0.0
        for i in 0...xAxis.lines {
            xWidest = Swift.max(xWidest, f2.stringWidth(size, label(xAxis.min + xStep * Float(i), xStep)))
        }
        every = ChartScale.axisLabelStep((right - plotLeft) / Float(xAxis.lines), xWidest + pad, xAxis.lines)
        // Each centered on its line, and inside the chart
        var centers = [Float]()
        var widths = [Float]()
        for i in 0...xAxis.lines {
            centers.append(xAt(xAxis.min + xStep * Float(i)))
            widths.append(f2.stringWidth(size, label(xAxis.min + xStep * Float(i), xStep)))
        }
        every = ChartScale.placedStep(every, centers, widths, x + pad / 2.0, x + w - pad / 2.0, pad / 2.0)
        for i in stride(from: 0, through: xAxis.lines, by: every) {
            let label = self.label(xAxis.min + xStep * Float(i), xStep)
            page.drawString(f2, size, label, ChartScale.placed(centers[i], widths[i], x + pad / 2.0, x + w - pad / 2.0),
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

        // The trend line, from the left of the plot to its right, within its
        // height
        if trendLine, let line = SimpleScatterChart.shownTrend(points, xAxis, yAxis) {
            let p1 = SimpleScatterChart.clip(Double(xAxis.min), line.slope * Double(xAxis.min) + line.intercept,
                    line.slope, line.intercept, yAxis)
            let p2 = SimpleScatterChart.clip(Double(xAxis.max), line.slope * Double(xAxis.max) + line.intercept,
                    line.slope, line.intercept, yAxis)
            page.setPenColor(SimpleScatterChart.TREND)
            page.setPenWidth(0.75)
            let x1 = xAt(Float(p1.x))
            let y1 = yAt(Float(p1.y))
            let x2 = xAt(Float(p2.x))
            let y2 = yAt(Float(p2.y))
            page.drawLine(x1, y1, x2, y2)
            // The label over the end of the line, lifted by as much as a
            // falling line rises under it
            let label = "trend"
            let width = f2.stringWidth(subtitleSize(), label)
            var lift = size / 3.0
            if x2 > x1 && y2 > y1 {
                lift += width * ((y2 - y1) / (x2 - x1))
            }
            // Not left of the plot, where a short steep line put it
            page.drawString(f2, subtitleSize(), label, Swift.max(plotLeft, x2 - width), y2 - lift,
                    Util.toRGB(SimpleScatterChart.TREND), nil)
        }

        // The dots, then the highlighted points and their names over them
        page.saveGraphicsState()
        if !page.pdf.isPDFA1() {
            let gs = GraphicsState()
            gs.setAlphaNonStroking(SimpleScatterChart.ALPHA)
            page.setGraphicsState(gs)
        }
        page.setBrushColor(dotColor)
        let r = 0.3 * size
        for p in points where p.name.isEmpty && onPlot(p) {
            page.fillCircle(xAt(p.x), yAt(p.y), r)
        }
        page.restoreGraphicsState()
        let big = 0.42 * size
        // Draws a highlighted point and its name, and returns the box of the
        // two: x, y, w, h
        func named(_ p: Point) -> [Float] {
            let px = xAt(p.x)
            let py = yAt(p.y)
            page.setBrushColor(highlightColor)
            page.fillCircle(px, py, big)
            // A name of two fifths of the plot at most, and in the plot
            let name = ChartScale.shorten(f1, size, p.name, 0.4 * (right - plotLeft))
            let width = f1.stringWidth(size, name)
            var lx = px + big + size / 3.0
            if lx + width > right {
                lx = px - big - size / 3.0 - width  // At the left, not past the plot
            }
            lx = Swift.max(lx, plotLeft)
            let baseline = py + ascent / 2.0
            // On a patch of the panel's color, which the dots and the grid do not
            // show through
            page.setBrushColor(panelColor >= 0 ? panelColor : Color.white)
            page.fillRect(lx - 1.0, baseline - f1.getAscent(size) - 1.0,
                    width + 2.0, f1.getAscent(size) + f1.getDescent(size) + 2.0)
            page.drawString(f1, size, name, lx, baseline, Util.toRGB(highlightColor), nil)
            let x1 = Swift.min(px - big, lx)
            let y1 = Swift.min(py - big, baseline - f1.getAscent(size))
            let x2 = Swift.max(px + big, lx + width)
            let y2 = Swift.max(py + big, baseline + f1.getDescent(size))
            return [x1, y1, x2 - x1, y2 - y1]
        }
        var linked = [Point]()
        for p in points where !p.name.isEmpty && onPlot(p) {
            if p.uri != nil && linksApart {
                linked.append(p)
                continue
            }
            let box = named(p)
            if p.uri != nil {
                page.addAnnotation(SimpleScatterChart.linkOf(p, box))
            }
        }

        page.restoreGraphicsState()
        page.setFigureBoundingBox(x, y, w, h)
        page.addEMC()

        for p in linked {
            page.saveGraphicsState()
            let link = page.beginLink()
            page.addBDC(StructElem.FIGURE, nil, nil, p.name)
            let box = named(p)
            page.setFigureBoundingBox(box[0], box[1], box[2], box[3])
            page.addEMC()
            page.endLink(link)
            page.restoreGraphicsState()
            page.addAnnotation(SimpleScatterChart.linkOf(p, box).joining(link))
        }
        return [x + w, y + h]
    }

    // The link annotation of a point, over the box of its dot and its name,
    // described by its name.
    private static func linkOf(_ p: Point, _ box: [Float]) -> Annotation {
        return Annotation(
                Annotation.Link,
                box[0],
                box[1],
                box[0] + box[2],
                box[1] + box[3],
                nil,    // Vertices
                nil,    // Fill Color
                0.0,    // Opacity
                nil,    // Title
                nil,    // Contents
                p.uri,
                nil,
                nil,
                nil,
                p.name)
    }

    // The point of the trend line at x, moved along the line to the top or
    // the bottom of the plot when it is past them.
    private static func clip(_ x: Double, _ y: Double, _ slope: Double, _ intercept: Double,
            _ axis: ChartScale.Axis) -> (x: Double, y: Double) {
        if slope != 0 && y < Double(axis.min) {
            return ((Double(axis.min) - intercept) / slope, Double(axis.min))
        }
        if slope != 0 && y > Double(axis.max) {
            return ((Double(axis.max) - intercept) / slope, Double(axis.max))
        }
        return (x, y)
    }
}   // End of SimpleScatterChart.swift
