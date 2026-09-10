/**
 *  Arc.swift
 *
 *  Copyright (c) 2026 PDFjet Software
 *  Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Used to create arc objects.
///
public class Arc : Drawable {
    private var cx: Float = 0.0
    private var cy: Float = 0.0
    private var rx: Float = 0.0
    private var ry: Float = 0.0
    private var startAngle: Float = 0.0
    private var sweepDegrees: Float = 0.0
    private var rotateDegrees: Float = 0.0

    private var fillColor: [Float]?
    private var strokeColor: [Float]? = [0.0, 0.0, 0.0]     // Black color
    private var strokeWidth: Float = 0.0
    private var strokeDashPattern: String? = "[] 0"

    private var language: String?
    private var actualText: String = Single.space
    private var altDescription: String = Single.space

    private var line: Line?

    ///
    /// The default constructor.
    ///
    public init() {
    }

    @discardableResult
    public func setPosition(_ cx: Float, _ cy: Float) -> Self {
        _ = setCenterXY(cx, cy)
        return self
    }

    @discardableResult
    public func setStartPointToEndOf(_ line: Line) -> Arc {
        self.line = line
        return self
    }

    @discardableResult
    public func setCenterXY(_ cx: Float, _ cy: Float) -> Arc {
        self.cx = cx
        self.cy = cy
        return self
    }

    @discardableResult
    public func setRadiusX(_ rx: Float) -> Arc {
        self.rx = rx
        return self
    }

    @discardableResult
    public func setRadiusY(_ ry: Float) -> Arc {
        self.ry = ry
        return self
    }

    @discardableResult
    public func setRadius(_ r: Float) -> Arc {
        self.rx = r
        self.ry = r
        return self
    }

    @discardableResult
    public func setStartAngle(_ angle: Float) -> Arc {
        self.startAngle = angle
        return self
    }

    @discardableResult
    public func setSweepDegreesCW(_ sweepDegrees: Float) -> Arc {
        self.sweepDegrees = sweepDegrees
        return self
    }

    @discardableResult
    public func setSweepDegreesCCW(_ sweepDegrees: Float) -> Arc {
        self.sweepDegrees = -sweepDegrees
        return self
    }

    ///
    /// The line dash pattern controls the pattern of dashes and gaps used to stroke paths.
    /// It is specified by a dash array and a dash phase.
    /// The elements of the dash array are positive numbers that specify the lengths of
    /// alternating dashes and gaps.
    /// The dash phase specifies the distance into the dash pattern at which to start the dash.
    /// The elements of both the dash array and the dash phase are expressed in user space units.
    ///
    ///     Examples of line dash patterns:
    ///
    ///         "[Array] Phase"     Appearance          Description
    ///         _______________     _________________   ____________________________________
    ///         "[] 0"              -----------------   Solid line
    ///         "[3] 0"             ---   ---   ---     3 units on, 3 units off, ...
    ///         "[2] 1"             -  --  --  --  --   1 on, 2 off, 2 on, 2 off, ...
    ///         "[2 1] 0"           -- -- -- -- -- --   2 on, 1 off, 2 on, 1 off, ...
    ///         "[3 5] 6"             ---     ---       2 off, 3 on, 5 off, 3 on, 5 off, ...
    ///         "[2 3] 11"          -   --   --   --    1 on, 3 off, 2 on, 3 off, 2 on, ...
    ///
    /// - Parameter strokeDashPattern the line dash pattern.
    /// - Returns: this Arc object.
    ///
    @discardableResult
    public func setStrokeDashPattern(_ strokeDashPattern: String) -> Arc {
        self.strokeDashPattern = strokeDashPattern
        return self
    }

    ///
    /// Sets the width of this line.
    ///
    /// - Parameter strokeWidth the width.
    /// - Returns: this Arc object.
    ///
    @discardableResult
    public func setStrokeWidth(_ strokeWidth: Float) -> Arc {
        self.strokeWidth = strokeWidth
        return self
    }

    ///
    /// Sets the color for this line.
    ///
    /// - Parameter color the color specified as an integer.
    /// - Returns: this Arc object.
    ///
    @discardableResult
    public func setStrokeColor(_ color: Int32) -> Arc {
        let r = Float((color >> 16) & 0xff)/255.0
        let g = Float((color >>  8) & 0xff)/255.0
        let b = Float((color)       & 0xff)/255.0
        return setStrokeColor(r, g, b)
    }

    @discardableResult
    public func setStrokeColor(_ r: Float, _ g: Float, _ b: Float) -> Arc {
        self.strokeColor = [r, g, b]
        return self
    }

    @discardableResult
    public func setStrokeColor(_ rgbColor: [Float]) -> Arc {
        self.strokeColor = rgbColor
        return self
    }

    @discardableResult
    public func setFillColor(_ color: Int32) -> Arc {
        let r = Float((color >> 16) & 0xff)/255.0
        let g = Float((color >>  8) & 0xff)/255.0
        let b = Float((color)       & 0xff)/255.0
        return setFillColor(r, g, b)
    }

    @discardableResult
    public func setFillColor(_ r: Float, _ g: Float, _ b: Float) -> Arc {
        self.fillColor = [r, g, b]
        return self
    }

    @discardableResult
    public func setFillColor(_ rgbColor: [Float]) -> Arc {
        self.fillColor = rgbColor
        return self
    }

    @discardableResult
    public func setRotateDegreesCW(_ degrees: Float) -> Arc {
        self.rotateDegrees = -degrees
        return self
    }

    @discardableResult
    public func setRotateDegreesCCW(_ degrees: Float) -> Arc {
        self.rotateDegrees = degrees
        return self
    }

    ///
    /// Sets the alternate description of this line.
    ///
    /// - Parameter altDescription the alternate description of the line.
    /// - Returns: this Arc.
    ///
    @discardableResult
    public func setAltDescription(_ altDescription: String) -> Arc {
        self.altDescription = altDescription
        return self
    }

    ///
    /// Sets the actual text for this line.
    ///
    /// - Parameter actualText the actual text for the line.
    /// - Returns: this Arc.
    ///
    @discardableResult
    public func setActualText(_ actualText: String) -> Arc {
        self.actualText = actualText
        return self
    }

    ///
    /// Scales this line by the specified factor.
    ///
    /// - Parameter factor the factor used to scale the line.
    /// - Returns: this Arc object.
    ///
    @discardableResult
    public func setScaleFactor(_ factor: Float) -> Arc {
        self.rx *= factor
        self.ry *= factor
        return self
    }

    ///
    /// Draws this line on the specified page.
    ///
    /// - Parameter page the page to draw on.
    /// - Returns: x and y coordinates of the bottom right corner of this component.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        // If a start point was set, calculate center so arc begins there
        if line != nil {
            let p1 = line!.getStartPoint()
            let p2 = line!.getEndPoint()
            let dx = p2.x - p1.x
            let dy = p2.y - p1.y
            // Normalize and rotate 90 degrees (clockwise perpendicular)
            let invLength = 1.0 / sqrt(dx*dx + dy*dy)
            let nx = -dy * invLength
            let ny = dx * invLength
            // Adjust direction based on sweep
            let sign: Float = sweepDegrees > 0.0 ? 1.0 : -1.0
            cx = p2.x + nx * rx * sign
            cy = p2.y + ny * ry * sign
            startAngle = atan2(p2.y - cy, p2.x - cx) * (180.0 / Float.pi)
        }

        page!.addBMC(StructElem.P, language, actualText, altDescription)
        page!.saveGraphicsState()
        let centerX = cx
        let centerY = page!.height - cy
        page!.rotateAroundCenter(centerX, centerY, rotateDegrees)
        let arcPoints = page!.drawArc(cx, cy, rx, ry, startAngle, sweepDegrees)
        if strokeColor != nil && strokeDashPattern != nil {
            page!.setStrokeDashPattern(strokeDashPattern!)
        }
        if fillColor != nil && strokeColor != nil {
            page!.setBrushColor(fillColor!)
            page!.setPenWidth(strokeWidth)
            page!.setPenColor(strokeColor!)
            page!.append("B\n")
        } else if fillColor != nil && strokeColor == nil {
            page!.setBrushColor(fillColor!)
            page!.append("f\n")
        } else if fillColor == nil && strokeColor != nil {
            page!.setPenWidth(strokeWidth)
            page!.setPenColor(strokeColor!)
            page!.append("S\n")
        } else {    // Both brushColor == nil and penColor == nil
            page!.setPenWidth(0.0)
            page!.setPenColor(Color.black)
            page!.append("S\n")
        }
        page!.restoreGraphicsState()
        page!.addEMC()
        return arcPoints
    }
}   // End of Arc.swift
