package pdfjet

/**
 * arc.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

type Arc struct {
	cx, cy, rx, ry float32
	startAngle     float32
	sweepDegrees   float32
	rotateDegrees  float32
	fillColor      [3]float32
	strokeColor    [3]float32
	strokeWidth    float32
	strokePattern  string // = "[] 0";
	language       string
	actualText     string // = Single.space;
	altDescription string // = Single.space;
	line           *Line
}

func NewArc(
	cx, cy, rx, ry float32,
	startAngle float32,
	sweepDegrees float32,
	rotateDegrees float32,
	fillColor [3]float32,
	strokeColor [3]float32,
	strokeWidth float32,
	strokePattern string,
	language string,
	actualText string,
	altDescription string,
	line *Line) *Arc {
	arc := new(Arc)
	arc.cx = cx
	arc.cy = cy
	arc.rx = rx
	arc.ry = ry
	arc.startAngle = startAngle
	arc.sweepDegrees = sweepDegrees
	arc.rotateDegrees = rotateDegrees
	arc.fillColor = fillColor
	arc.strokeColor = strokeColor
	arc.strokeWidth = strokeWidth
	arc.strokePattern = strokePattern
	arc.language = language
	arc.actualText = actualText
	arc.altDescription = altDescription
	arc.line = line
	return arc
}

func (arc *Arc) SetPosition(cx, cy float32) {
	arc.SetCenterXY(cx, cy)
}

func (arc *Arc) SetStartPointToEndOf(line *Line) *Arc {
	arc.line = line
	return arc
}
func (arc *Arc) SetCenterXY(cx, cy float32) *Arc {
	arc.cx = cx
	arc.cy = cy
	return arc
}

func (arc *Arc) SetRadiusX(rx float32) *Arc {
	arc.rx = rx
	return arc
}

func (arc *Arc) SetRadiusY(ry float32) *Arc {
	arc.ry = ry
	return arc
}

func (arc *Arc) SetRadius(r float32) *Arc {
	arc.rx = r
	arc.ry = r
	return arc
}

func (arc *Arc) SetStartAngle(angle float32) *Arc {
	arc.startAngle = angle
	return arc
}

func (arc *Arc) SetSweepDegreesCW(sweepDegrees float32) *Arc {
	arc.sweepDegrees = sweepDegrees
	return arc
}

func (arc *Arc) SetSweepDegreesCCW(sweepDegrees float32) *Arc {
	arc.sweepDegrees = -sweepDegrees
	return arc
}

//    /**
//     * The line dash pattern controls the pattern of dashes and gaps used to stroke paths.
//     * It is specified by a dash array and a dash phase.
//     * The elements of the dash array are positive numbers that specify the lengths of
//     * alternating dashes and gaps.
//     * The dash phase specifies the distance into the dash pattern at which to start the dash.
//     * The elements of both the dash array and the dash phase are expressed in user space units.
//     * <pre>
//     * Examples of line dash patterns:
//     *
//     *     "[Array] Phase"     Appearance          Description
//     *     _______________     _________________   ____________________________________
//     *     "[] 0"              -----------------   Solid line
//     *     "[3] 0"             ---   ---   ---     3 units on, 3 units off, ...
//     *     "[2] 1"             -  --  --  --  --   1 on, 2 off, 2 on, 2 off, ...
//     *     "[2 1] 0"           -- -- -- -- -- --   2 on, 1 off, 2 on, 1 off, ...
//     *     "[3 5] 6"             ---     ---       2 off, 3 on, 5 off, 3 on, 5 off, ...
//     *     "[2 3] 11"          -   --   --   --    1 on, 3 off, 2 on, 3 off, 2 on, ...
//     * </pre>
//     *
//     * @param pattern the line dash pattern.
//     * @return this Arc object.
//     */
//    public Arc SetStrokePattern(String pattern) {
//        this.strokePattern = pattern;
//        return this;
//    }
//
//    /**
//     * Sets the width of this line.
//     *
//     * @param width the width.
//     * @return this Arc object.
//     */
//    public Arc SetStrokeWidth(double width) {
//        this.strokeWidth = (float) width;
//        return this;
//    }
//
//    /**
//     * Sets the width of this line.
//     *
//     * @param strokeWidth the width.
//     * @return this Arc object.
//     */
//    public Arc SetStrokeWidth(float width) {
//        this.strokeWidth = width;
//        return this;
//    }
//
//    /**
//     * Sets the color for this line.
//     *
//     * @param color the color specified as an integer.
//     * @return this Arc object.
//     */
//    public Arc SetStrokeColor(int color) {
//        float r = ((color >> 16) & 0xff)/255f;
//        float g = ((color >>  8) & 0xff)/255f;
//        float b = ((color)       & 0xff)/255f;
//        this.SetStrokeColor(r, g, b);
//        return this;
//    }
//
//    public Arc SetStrokeColor(float r, float g, float b) {
//        this.strokeColor = new float[] {r, g, b};
//        return this;
//    }
//
//    public Arc SetStrokeColor(float[] rgbColor) {
//        this.strokeColor = rgbColor;
//        return this;
//    }
//
//    public Arc SetFillColor(int color) {
//        float r = ((color >> 16) & 0xff)/255f;
//        float g = ((color >>  8) & 0xff)/255f;
//        float b = ((color)       & 0xff)/255f;
//        this.SetFillColor(r, g, b);
//        return this;
//    }
//
//    public Arc SetFillColor(float r, float g, float b) {
//        this.fillColor = new float[] {r, g, b};
//        return this;
//    }
//
//    public Arc SetFillColor(float[] rgbColor) {
//        this.fillColor = rgbColor;
//        return this;
//    }
//
//    public Arc SetRotateDegreesCW(float degrees) {
//        this.rotateDegrees = -degrees;
//        return this;
//    }
//
//    public Arc SetRotateDegreesCW(double degrees) {
//        this.rotateDegrees = (float) -degrees;
//        return this;
//    }
//
//    public Arc SetRotateDegreesCCW(float degrees) {
//        this.rotateDegrees = degrees;
//        return this;
//    }
//
//    public Arc SetRotateDegreesCCW(double degrees) {
//        this.rotateDegrees = (float) degrees;
//        return this;
//    }
//
//    /**
//     * Sets the alternate description of this line.
//     *
//     * @param altDescription the alternate description of the line.
//     * @return this Arc.
//     */
//    public Arc SetAltDescription(String altDescription) {
//        this.altDescription = altDescription;
//        return this;
//    }
//
//    /**
//     * Sets the actual text for this line.
//     *
//     * @param actualText the actual text for the line.
//     * @return this Arc.
//     */
//    public Arc SetActualText(String actualText) {
//        this.actualText = actualText;
//        return this;
//    }
//
//    /**
//     * Scales this line by the specified factor.
//     *
//     * @param factor the factor used to scale the line.
//     * @return this Arc object.
//     */
//    public Arc SetScaleFactor(double factor) {
//        return SetScaleFactor((float) factor);
//    }
//
//    /**
//     * Scales this line by the specified factor.
//     *
//     * @param factor the factor used to scale the line.
//     * @return this Arc object.
//     */
//    public Arc SetScaleFactor(float factor) {
//        this.rx *= factor;
//        this.ry *= factor;
//        return this;
//    }
//
//    /**
//     * Draws this line on the specified page.
//     *
//     * @param page the page to draw on.
//     * @return x and y coordinates of the bottom right corner of this component.
//     * @throws Exception
//     */
//    public float[] DrawOn(Page page) {
//        // If a start point was set, calculate center so arc begins there
//        if (line != null) {
//            float dx = line.x2 - line.x1;
//            float dy = line.y2 - line.y1;
//            // Normalize and rotate 90° (clockwise perpendicular)
//            float invLength = 1f / MathF.Sqrt(dx*dx + dy*dy);
//            float nx = -dy * invLength;
//            float ny = dx * invLength;
//            // Adjust direction based on sweep
//            float sign = sweepDegrees > 0f ? 1f : -1f;
//            cx = line.x2 + nx * rx * sign;
//            cy = line.y2 + ny * ry * sign;
//            startAngle = MathF.Atan2(line.y2 - cy, line.x2 - cx) * (180f / MathF.PI);
//        }
//
//        page.AddBMC(StructElem.P, language, actualText, altDescription);
//        page.Append("q\n");
//        float centerX = cx;
//        float centerY = page.height - cy;
//        page.RotateAroundCenter(centerX, centerY, rotateDegrees);
//        float[] arcPoints = page.DrawArc(
//                cx,
//                cy,
//                rx,
//                ry,
//                startAngle,
//                sweepDegrees);
//        if (strokeColor != null && strokePattern != null) {
//            page.SetStrokePattern(strokePattern);
//        }
//        if (fillColor != null && strokeColor != null) {
//            page.SetBrushColor(fillColor);
//            page.SetPenWidth(strokeWidth);
//            page.SetPenColor(strokeColor);
//            page.Append("B\n");
//        } else if (fillColor != null && strokeColor == null) {
//            page.SetBrushColor(fillColor);
//            page.Append("f\n");
//        } else if (fillColor == null && strokeColor != null) {
//            page.SetPenWidth(strokeWidth);
//            page.SetPenColor(strokeColor);
//            page.Append("S\n");
//        } else {    // Both brushColor == null and penColor == null
//            page.SetPenWidth(0f);
//            page.SetPenColor(Color.black);
//            page.Append("S\n");
//        }
//        page.Append("Q\n");
//        page.AddEMC();
//        return arcPoints;
//    }
//}   // End of arc.go
