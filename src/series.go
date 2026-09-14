// series.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"github.com/edragoev1/pdfjet/v9/src/shape"
)

// Series is one series of a Chart: its points, the line that connects them
// when it is drawn, and the marker of the points added by their coordinates.
// A series is created with Chart.AddSeries; its name is listed in the legend
// of the chart. See Example_09.
type Series struct {
	name   string
	points []*Point

	strokeColor       [3]float32
	hasStrokeColor    bool // false: the next color of the palette
	strokeWidth       float32
	strokeDashPattern string
	drawPath          bool

	shape  shape.Shape
	radius float32
}

// newSeries creates a series with the default line and marker.
func newSeries(name string) *Series {
	series := new(Series)
	series.name = name
	series.points = make([]*Point, 0)
	series.strokeWidth = 1.0
	series.strokeDashPattern = "[] 0"
	series.shape = shape.Circle
	series.radius = 2.0
	return series
}

// AddPoint adds a point with the marker of this series.
// @param x the x value.
// @param y the y value.
func (series *Series) AddPoint(x, y float32) *Series {
	series.points = append(series.points, NewPoint(x, y).SetShape(series.shape).SetRadius(series.radius))
	return series
}

// AddPointWithMarker adds a point with its own marker: its shape, radius and
// colors. A point without a stroke color is drawn in the color of the series.
func (series *Series) AddPointWithMarker(point *Point) *Series {
	series.points = append(series.points, point)
	return series
}

// SetDrawPath sets whether the points are connected with a line, in the order
// they were added. The default is false: only the markers are drawn.
func (series *Series) SetDrawPath(drawPath bool) *Series {
	series.drawPath = drawPath
	return series
}

// SetStrokeColor sets the color of the line and of the markers that have no
// color of their own. Without it the series has the next color of the palette.
// @param color the color as a 0xRRGGBB value, for example color.Blue.
func (series *Series) SetStrokeColor(color int32) *Series {
	r := float32((color>>16)&0xff) / 255.0
	g := float32((color>>8)&0xff) / 255.0
	b := float32((color)&0xff) / 255.0
	series.strokeColor = [3]float32{r, g, b}
	series.hasStrokeColor = true
	return series
}

// SetStrokeWidth sets the width of the line. The default is 1.
func (series *Series) SetStrokeWidth(strokeWidth float32) *Series {
	series.strokeWidth = strokeWidth
	return series
}

// SetStrokeDashPattern sets the dash pattern of the line, for example
// "[3 3] 0". The default is a solid line.
func (series *Series) SetStrokeDashPattern(strokeDashPattern string) *Series {
	series.strokeDashPattern = strokeDashPattern
	return series
}

// SetShape sets the shape of the marker of the points added by their
// coordinates. The default is shape.Circle; shape.Invisible draws no markers.
func (series *Series) SetShape(markerShape shape.Shape) *Series {
	series.shape = markerShape
	return series
}

// SetRadius sets the radius of the marker of the points added by their
// coordinates. The default is 2.
func (series *Series) SetRadius(radius float32) *Series {
	series.radius = radius
	return series
}
