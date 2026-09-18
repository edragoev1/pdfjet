/**
 * Series.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// One series of a Chart: its points, the line that connects them when it is
/// drawn, and the marker of the points added by their coordinates. A series is
/// created with Chart.addSeries; its name is listed in the legend of the chart.
/// See Example_09.
///
public class Series {
    let name: String
    var points = [Point]()
    // True for a point added by its coordinates, drawn with the marker of the series
    var seriesMarker = [Bool]()

    var strokeColor: [Float]?       // nil: the next color of the palette
    var strokeWidth: Float = 1.0
    var strokeDashPattern = "[] 0"
    var drawPath = false

    var shape = Shape.CIRCLE
    var radius: Float = 2.0

    init(_ name: String) {
        self.name = name
    }

    ///
    /// Adds a point with the marker of this series.
    ///
    /// - Parameter x: the x value.
    /// - Parameter y: the y value.
    /// - Returns: this Series object.
    ///
    @discardableResult
    public func addPoint(_ x: Float, _ y: Float) -> Series {
        points.append(Point(x, y))
        seriesMarker.append(true)
        return self
    }

    ///
    /// Adds a point with its own marker: its shape, radius and colors.
    /// A point without a stroke color is drawn in the color of the series.
    ///
    /// - Parameter point: the point.
    /// - Returns: this Series object.
    ///
    @discardableResult
    public func addPoint(_ point: Point) -> Series {
        points.append(point)
        seriesMarker.append(false)
        return self
    }

    ///
    /// Sets whether the points are connected with a line, in the order they
    /// were added. The default is false: only the markers are drawn.
    ///
    /// - Parameter drawPath: true to draw the line.
    /// - Returns: this Series object.
    ///
    @discardableResult
    public func setDrawPath(_ drawPath: Bool) -> Series {
        self.drawPath = drawPath
        return self
    }

    ///
    /// Sets the color of the line and of the markers that have no color of
    /// their own. Without it the series has the next color of the palette.
    ///
    /// - Parameter color: the color as a 0xRRGGBB value, for example Color.blue.
    /// - Returns: this Series object.
    ///
    @discardableResult
    public func setStrokeColor(_ color: Int32) -> Series {
        let r = Float((color >> 16) & 0xff)/255.0
        let g = Float((color >>  8) & 0xff)/255.0
        let b = Float((color)       & 0xff)/255.0
        self.strokeColor = [r, g, b]
        return self
    }

    ///
    /// Sets the width of the line. The default is 1.
    ///
    /// - Parameter strokeWidth: the line width.
    /// - Returns: this Series object.
    ///
    @discardableResult
    public func setStrokeWidth(_ strokeWidth: Float) -> Series {
        self.strokeWidth = strokeWidth
        return self
    }

    ///
    /// Sets the dash pattern of the line, for example "[3 3] 0". The default
    /// is a solid line.
    ///
    /// - Parameter strokeDashPattern: the dash pattern.
    /// - Returns: this Series object.
    ///
    @discardableResult
    public func setStrokeDashPattern(_ strokeDashPattern: String) -> Series {
        self.strokeDashPattern = strokeDashPattern
        return self
    }

    ///
    /// Sets the shape of the marker of the points added by their coordinates.
    /// The default is Shape.CIRCLE; Shape.INVISIBLE draws no markers.
    ///
    /// - Parameter shape: the shape.
    /// - Returns: this Series object.
    ///
    @discardableResult
    public func setShape(_ shape: Shape) -> Series {
        self.shape = shape
        return self
    }

    ///
    /// Sets the radius of the marker of the points added by their coordinates.
    /// The default is 2.
    ///
    /// - Parameter radius: the radius.
    /// - Returns: this Series object.
    ///
    @discardableResult
    public func setRadius(_ radius: Float) -> Series {
        self.radius = radius
        return self
    }
}   // End of Series.swift
