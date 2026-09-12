package pdfjet

import (
	"math"

	"github.com/edragoev1/pdfjet/src/color"
)

/**
 * arc.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

// Arc is used to create arc objects.
type Arc struct {
	cx, cy, rx, ry    float32
	startAngle        float32
	sweepDegrees      float32
	rotateDegrees     float32
	fillColor         [3]float32
	hasFillColor      bool
	strokeColor       [3]float32
	hasStrokeColor    bool
	strokeWidth       float32
	strokeDashPattern string
	language          string
	actualText        string // = Single.space;
	altDescription    string // = Single.space;
	line              *Line
}

// NewArc creates an Arc with a solid stroke dash pattern, as the other
// three ports do in their Arc field initializers.
func NewArc() *Arc {
	arc := new(Arc)
	arc.strokeDashPattern = "[] 0"
	return arc
}

// SetLocation sets the center of this arc.
func (arc *Arc) SetLocation(cx, cy float32) Drawable {
	arc.SetCenterXY(cx, cy)
	return arc
}

// SetStartPointToEndOf starts this arc at the end point of the specified line.
func (arc *Arc) SetStartPointToEndOf(line *Line) *Arc {
	arc.line = line
	return arc
}

// SetCenterXY sets the center of this arc.
func (arc *Arc) SetCenterXY(cx, cy float32) *Arc {
	arc.cx = cx
	arc.cy = cy
	return arc
}

// SetRadiusX sets the horizontal radius of this arc.
func (arc *Arc) SetRadiusX(rx float32) *Arc {
	arc.rx = rx
	return arc
}

// SetRadiusY sets the vertical radius of this arc.
func (arc *Arc) SetRadiusY(ry float32) *Arc {
	arc.ry = ry
	return arc
}

// SetRadius sets both radii of this arc to the same value, making it circular.
func (arc *Arc) SetRadius(r float32) *Arc {
	arc.rx = r
	arc.ry = r
	return arc
}

// SetStartAngle sets the angle in degrees where this arc starts.
func (arc *Arc) SetStartAngle(angle float32) *Arc {
	arc.startAngle = angle
	return arc
}

// SetSweepDegreesCW sets how many degrees this arc sweeps clockwise from its start angle.
func (arc *Arc) SetSweepDegreesCW(sweepDegrees float32) *Arc {
	arc.sweepDegrees = sweepDegrees
	return arc
}

// SetSweepDegreesCCW sets how many degrees this arc sweeps counterclockwise from its start angle.
func (arc *Arc) SetSweepDegreesCCW(sweepDegrees float32) *Arc {
	arc.sweepDegrees = -sweepDegrees
	return arc
}

// SetStrokeDashPattern sets the line dash pattern, for example "[3 5] 6".
func (arc *Arc) SetStrokeDashPattern(strokeDashPattern string) *Arc {
	arc.strokeDashPattern = strokeDashPattern
	return arc
}

// SetStrokeWidth sets the width of the arc outline.
func (arc *Arc) SetStrokeWidth(width float32) *Arc {
	arc.strokeWidth = width
	return arc
}

// SetStrokeColor sets the stroke color as a 0xRRGGBB value, for example color.Blue.
func (arc *Arc) SetStrokeColor(color int32) *Arc {
	r := float32((color>>16)&0xff) / 255.0
	g := float32((color>>8)&0xff) / 255.0
	b := float32((color)&0xff) / 255.0
	arc.SetStrokeColorRGB(r, g, b)
	return arc
}

// SetStrokeColorRGB sets the stroke color from red, green and blue values between 0.0 and 1.0.
func (arc *Arc) SetStrokeColorRGB(r, g, b float32) *Arc {
	arc.strokeColor = [3]float32{r, g, b}
	arc.hasStrokeColor = true
	return arc
}

// SetFillColor sets the fill color as a 0xRRGGBB value, for example color.Blue.
func (arc *Arc) SetFillColor(color int32) *Arc {
	r := float32((color>>16)&0xff) / 255.0
	g := float32((color>>8)&0xff) / 255.0
	b := float32((color)&0xff) / 255.0
	arc.SetFillColorRGB(r, g, b)
	return arc
}

// SetFillColorRGB sets the fill color from red, green and blue values between 0.0 and 1.0.
func (arc *Arc) SetFillColorRGB(r, g, b float32) *Arc {
	arc.fillColor = [3]float32{r, g, b}
	arc.hasFillColor = true
	return arc
}

// SetFillColorRGBArray sets the fill color from an array of red, green and blue values.
func (arc *Arc) SetFillColorRGBArray(rgbColor [3]float32) *Arc {
	arc.fillColor = rgbColor
	arc.hasFillColor = true
	return arc
}

// SetRotateDegreesCW rotates this arc clockwise by the specified degrees.
func (arc *Arc) SetRotateDegreesCW(degrees float32) *Arc {
	arc.rotateDegrees = -degrees
	return arc
}

// SetRotateDegreesCWFloat64 is SetRotateDegreesCW for a float64 angle.
func (arc *Arc) SetRotateDegreesCWFloat64(degrees float64) *Arc {
	arc.rotateDegrees = float32(-degrees)
	return arc
}

// SetRotateDegreesCCW rotates this arc counterclockwise by the specified degrees.
func (arc *Arc) SetRotateDegreesCCW(degrees float32) *Arc {
	arc.rotateDegrees = degrees
	return arc
}

// SetRotateDegreesCCWFloat64 is SetRotateDegreesCCW for a float64 angle.
func (arc *Arc) SetRotateDegreesCCWFloat64(degrees float64) *Arc {
	arc.rotateDegrees = float32(degrees)
	return arc
}

// SetAltDescription sets the alternate description of this arc, used for accessibility.
func (arc *Arc) SetAltDescription(altDescription string) *Arc {
	arc.altDescription = altDescription
	return arc
}

// SetActualText sets the actual text of this arc, used for accessibility.
func (arc *Arc) SetActualText(actualText string) *Arc {
	arc.actualText = actualText
	return arc
}

// SetScaleFactorFloat64 is SetScaleFactor for a float64 factor.
func (arc *Arc) SetScaleFactorFloat64(factor float64) *Arc {
	return arc.SetScaleFactor(float32(factor))
}

// SetScaleFactor scales both radii of this arc by the specified factor.
func (arc *Arc) SetScaleFactor(factor float32) *Arc {
	arc.rx *= factor
	arc.ry *= factor
	return arc
}

// DrawOn draws this arc on the specified page.
func (arc *Arc) DrawOn(page *Page) [2]float32 {
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

	page.SaveGraphicsState()

	centerX := arc.cx
	centerY := page.height - arc.cy

	page.RotateAroundCenter(centerX, centerY, arc.rotateDegrees)
	page.DrawArc(
		arc.cx,
		arc.cy,
		arc.rx,
		arc.ry,
		arc.startAngle,
		arc.sweepDegrees)

	if arc.hasStrokeColor == true && arc.strokeDashPattern != "" {
		page.SetStrokeDashPattern(arc.strokeDashPattern)
	}

	if arc.hasFillColor == true && arc.hasStrokeColor == true {
		page.SetBrushColorRGB(arc.fillColor)
		page.SetPenWidth(arc.strokeWidth)
		page.SetPenColorRGB(arc.strokeColor)
		page.appendString("B\n")
	} else if arc.hasFillColor == true && arc.hasStrokeColor == false {
		page.SetBrushColorRGB(arc.fillColor)
		page.appendString("f\n")
	} else if arc.hasFillColor == false && arc.hasStrokeColor == true {
		page.SetPenWidth(arc.strokeWidth)
		page.SetPenColorRGB(arc.strokeColor)
		page.appendString("S\n")
	} else { // Both arc.brushColor == false and arc.strokeColor == false
		page.SetPenWidth(0.0)
		page.SetPenColor(color.Black)
		page.appendString("S\n")
	}

	page.RestoreGraphicsState()
	page.AddEMC()

	return [2]float32{arc.cx + arc.rx, arc.cy + arc.ry}
}
