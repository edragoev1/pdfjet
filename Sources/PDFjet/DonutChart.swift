/**
 *  Chart.swift
 *
Copyright 2023 Innovatics Inc.
*/
import Foundation


/**
 *  Used to create XY chart objects and draw them on a page.
 *
 *  Please see Example_09.
 */
public class DonutChart : Drawable {

    var f1: Font?
    var f2: Font?
    var chartData: [[Point]]?
	var xc: Float = 0.0
    var yc: Float = 0.0
    var r1: Float = 0.0
    var r2: Float = 0.0
	var angles: [Float]?
	var colors: [Int32]?
    var isDonutChart: Bool = true


    /**
     *  Create a Donut chart object.
     *
     *  @param f1 the font used for the chart title.
     *  @param f2 the font used for the X and Y axis titles.
     */
    public init(_ f1: Font, _ f2: Font) {
        self.f1 = f1
        self.f2 = f2
        self.angles = [Float]()
        self.colors = [Int32]()
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
        self.xc = x
        self.yc = y
    }


    public func setPosition(_ x: Float, _ y: Float) {
        setLocation(x, y)
    }


    private func getBezierCurvePoints(
            _ xc: Float, _ yc: Float, _ r: Float, _ a1: Float, _ a2: Float) -> [Point] {
        let angle1 = -1.0*a1
        let angle2 = -1.0*a2

        // Start point coordinates
        let x1 = xc + r*((cos(angle1)*(Float.pi/180.0)))
        let y1 = yc + r*((sin(angle1)*(Float.pi/180.0)))
        // End point coordinates
        let x4 = xc + r*((cos(angle2)*(Float.pi/180.0)))
        let y4 = yc + r*((sin(angle2)*(Float.pi/180.0)))
    
        let ax = x1 - xc
        let ay = y1 - yc
        let bx = x4 - xc
        let by = y4 - yc
        let q1 = ax*ax + ay*ay
        let q2 = q1 + ax*bx + ay*by
    
        let k2 = 4.0/3.0 * ((sqrt(2.0*q1*q2)) - q2) / (ax*by - ay*bx)
    
        let x2 = xc + ax - k2*ay
        let y2 = yc + ay + k2*ax
        let x3 = xc + bx + k2*by
        let y3 = yc + by - k2*bx
    
        var list = [Point]()
        list.append(Point(x1, y1))
        list.append(Point(x2, y2, Point.CONTROL_POINT))
        list.append(Point(x3, y3, Point.CONTROL_POINT))
        list.append(Point(x4, y4))
    
        return list
    }


    // GetArcPoints calculates a list of points for a given arc of a circle
    // @param xc the x-coordinate of the circle's centre.
    // @param yc the y-coordinate of the circle's centre
    // @param r the radius of the circle.
    // @param angle1 the start angle of the arc in degrees.
    // @param angle2 the end angle of the arc in degrees.
    // @param includeOrigin whether the origin should be included in the list (thus creating a pie shape).
    private func getArcPoints(
            _ xc: Float,
            _ yc: Float,
            _ r1: Float,
            _ angle1: Float,
            _ angle2: Float,
            _ includeOrigin: Bool) -> [Point] {
        var list = [Point]()

        if includeOrigin {
            list.append(Point(xc, yc))
        }

        var startAngle: Float = 0.0
        var endAngle: Float = 0.0
        if angle1 <= angle2 {
            startAngle = angle1
            endAngle = angle1 + 90
            while endAngle < angle2 {
                list.append(contentsOf: getBezierCurvePoints(xc, yc, r1, startAngle, endAngle))
                startAngle += 90
                endAngle += 90
            }
            endAngle -= 90
            list.append(contentsOf: getBezierCurvePoints(xc, yc, r1, endAngle, angle2))
        }
        else {
            startAngle = angle1
            endAngle = angle1 - 90
            while endAngle > angle2 {
                list.append(contentsOf: getBezierCurvePoints(xc, yc, r1, startAngle, endAngle))
                startAngle -= 90
                endAngle -= 90
            }
            endAngle += 90
            list.append(contentsOf: getBezierCurvePoints(xc, yc, r1, endAngle, angle2))
        }

        return list
    }


    // GetDonutPoints calculates a list of points for a given donut sector of a circle.
    // @param xc the x-coordinate of the circle's centre.
    // @param yc the y-coordinate of the circle's centre.
    // @param r1 the inner radius of the donut.
    // @param r2 the outer radius of the donut.
    // @param angle1 the start angle of the donut sector in degrees.
    // @param angle2 the end angle of the donut sector in degrees.
    private func getDonutPoints(
            _ xc: Float,
            _ yc: Float,
            _ r1: Float,
            _ r2: Float,
            _ angle1: Float,
            _ angle2: Float) -> [Point] {
        var list = [Point]()
        list.append(contentsOf: getArcPoints(xc, yc, r1, angle1, angle2, false))
        list.append(contentsOf: getArcPoints(xc, yc, r2, angle2, angle1, false))
        return list
    }


    // AddSector -- TODO:
    public func addSector(_ angle: Float, _ color: Int32) {
        angles!.append(angle)
        colors!.append(color)
    }


    /**
     *  Draws this chart on the specified page.
     *
     *  @param page the page to draw this chart on.
     */
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        var startAngle: Float = 0.0
        var endAngle: Float = 0.0
        var lastColorIndex = 0
        var i = 0
        while i < angles!.count {
            endAngle = startAngle + angles![i]
            var list = [Point]()
            if isDonutChart {
                list.append(contentsOf: getDonutPoints(xc, yc, r1, r2, startAngle, endAngle))
            }
            else {
                list.append(contentsOf: getArcPoints(xc, yc, r2, startAngle, endAngle, true))
            }
            // for (Point point : list) {
            // 	point.drawOn(page)
            // }
            page!.setBrushColor(colors![i])
            page!.drawPath(list, Operation.FILL)
            startAngle = endAngle
            lastColorIndex = i
            i += 1
        }

        if endAngle < 360.0 {
            endAngle = 360.0
            var list = [Point]()
            if isDonutChart {
                list.append(contentsOf: getDonutPoints(xc, yc, r1, r2, startAngle, endAngle))
            }
            else {
                list.append(contentsOf: getArcPoints(xc, yc, r2, startAngle, endAngle, true))
            }
            // for (Point point : list) {
            // 	point.drawOn(page)
            // }
            page!.setBrushColor(colors![lastColorIndex + 1])
            page!.drawPath(list, Operation.FILL)
        }

        return [0.0, 0.0]
    }

}   // End of DonutChart.swift
