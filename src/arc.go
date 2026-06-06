package pdfjet

import "math"

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

func (arc *Arc) SetStrokePattern(pattern string) *Arc {
	arc.strokePattern = pattern
	return arc
}

func (arc *Arc) SetStrokeWidth(width float32) *Arc {
	arc.strokeWidth = width
	return arc
}

func (arc *Arc) SetStrokeColor(color int32) *Arc {
	r := float32((color>>16)&0xff) / 255.0
	g := float32((color>>8)&0xff) / 255.0
	b := float32((color)&0xff) / 255.0
	arc.SetStrokeColorRGB(r, g, b)
	return arc
}

func (arc *Arc) SetStrokeColorRGB(r, g, b float32) *Arc {
	arc.strokeColor = [3]float32{r, g, b}
	return arc
}

func (arc *Arc) SetFillColor(color int32) *Arc {
	r := float32((color>>16)&0xff) / 255.0
	g := float32((color>>8)&0xff) / 255.0
	b := float32((color)&0xff) / 255.0
	arc.SetFillColorRGB(r, g, b)
	return arc
}

func (arc *Arc) SetFillColorRGB(r, g, b float32) *Arc {
	arc.fillColor = [3]float32{r, g, b}
	return arc
}

func (arc *Arc) SetFillColorRGBArray(rgbColor [3]float32) *Arc {
	arc.fillColor = rgbColor
	return arc
}

func (arc *Arc) SetRotateDegreesCW(degrees float32) *Arc {
	arc.rotateDegrees = -degrees
	return arc
}

func (arc *Arc) SetRotateDegreesCWFloat64(degrees float64) *Arc {
	arc.rotateDegrees = float32(-degrees)
	return arc
}

func (arc *Arc) SetRotateDegreesCCW(degrees float32) *Arc {
	arc.rotateDegrees = degrees
	return arc
}

func (arc *Arc) SetRotateDegreesCCWFloat64(degrees float64) *Arc {
	arc.rotateDegrees = float32(degrees)
	return arc
}

func (arc *Arc) SetAltDescription(altDescription string) *Arc {
	arc.altDescription = altDescription
	return arc
}

func (arc *Arc) SetActualText(actualText string) *Arc {
	arc.actualText = actualText
	return arc
}

func (arc *Arc) SetScaleFactorFloat64(factor float64) *Arc {
	return arc.SetScaleFactor(float32(factor))
}

func (arc *Arc) SetScaleFactor(factor float32) *Arc {
	arc.rx *= factor
	arc.ry *= factor
	return arc
}

func (arc *Arc) DrawOn(page *Page) []float32 {
	// If a start point was set, calculate center so arc begins there
	if arc.line != nil {
		dx := arc.line.x2 - arc.line.x1
		dy := arc.line.y2 - arc.line.y1
		// Normalize and rotate 90° (clockwise perpendicular)
		invLength := float32(1.0 / math.Sqrt(float64(dx*dx+dy*dy)))
		nx := -dy * invLength
		ny := dx * invLength
		// Adjust direction based on sweep
		sign := float32(-1.0)
		if arc.sweepDegrees > 0.0 {
			sign = float32(1.0)
		}
		arc.cx = arc.line.x2 + nx*arc.rx*sign
		arc.cy = arc.line.y2 + ny*arc.ry*sign
		arc.startAngle = float32(math.Atan2(
			float64(arc.line.y2-arc.cy), float64(arc.line.x2-arc.cx)) * (180.0 / math.Pi))
	}

	page.AddBMC("P", arc.language, arc.actualText, arc.altDescription)
	page.appendString("q\n")
	centerX := arc.cx
	centerY := page.height - arc.cy

	page.RotateAroundCenter(centerX, centerY, arc.rotateDegrees)
	arcPoints := page.DrawArc(
		arc.cx,
		arc.cy,
		arc.rx,
		arc.ry,
		arc.startAngle,
		arc.sweepDegrees)

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

	return arcPoints
}
