// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"math"
	"strconv"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/capstyle"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/shape"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

const navy = 0x1d3557
const teal = 0x2a9d8f
const red = 0xe63946
const pale = 0xe8f1f2

// The cells of the grid: four across, 128 points wide, and 160 tall.
const left float32 = 50.0
const top float32 = 150.0
const cellWidth float32 = 128.0
const cellHeight float32 = 160.0

// Example56 draws the shapes PDFjet draws, each in a cell of its own under
// its name: lines with dashes and caps, rectangles with square and rounded
// corners, the markers of charts, an ellipse, arcs, a path of Bézier curves,
// and a closed path, filled. The second page draws text at every angle around
// a point.
//
// Each shape is a Drawable: it is given its location, its size, its colors
// and the width of its stroke, and DrawOn draws it as vector graphics, which
// stay sharp at any zoom. The shapes carry no text, so they are artifacts of
// the PDF/UA document, and the name under each shape says what it is.
func Example56() {
	pdf, err := pdfjet.NewPDFFile("Example_56.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Shapes")

	regular := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	semiBold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	label := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	label.SetSize(9.0)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	title := pdfjet.NewTextLine(semiBold, "Shapes")
	title.SetStructureType(structelem.H1)
	title.SetFontSize(22.0)
	title.SetLocation(left, 70.0)
	title.DrawOn(page)

	about := pdfjet.NewTextBlock(regular,
		"PDFjet draws shapes as vector graphics, which stay sharp at any zoom. Each "+
			"shape is a Drawable: give it a location, a size, its colors and the width "+
			"of its stroke, and drawOn draws it on the page.")
	about.SetFontSize(11.0)
	about.SetLineSpacing(1.4)
	about.SetLocation(left, 85.0)
	about.SetWidth(512.0)
	about.DrawOn(page)

	// Lines: solid, dashed and dotted with round caps
	c := cell(page, label, 0, 0, "Line: solid, dashed, and dotted with round caps")
	pdfjet.NewLine(c[0]+10.0, c[1]+30.0, c[0]+110.0, c[1]+30.0).
		SetStrokeWidth(3.0).SetStrokeColor(navy).DrawOn(page)
	pdfjet.NewLine(c[0]+10.0, c[1]+60.0, c[0]+110.0, c[1]+60.0).
		SetStrokeWidth(3.0).SetStrokeColor(teal).SetStrokeDashPattern("[8 4] 0").DrawOn(page)
	pdfjet.NewLine(c[0]+10.0, c[1]+90.0, c[0]+110.0, c[1]+90.0).
		SetStrokeWidth(5.0).SetStrokeColor(red).SetStrokeDashPattern("[0 10] 0").
		SetLineCapStyle(capstyle.Round).DrawOn(page)

	// A rectangle, filled and outlined
	c = cell(page, label, 1, 0, "Rect: filled, with a border")
	pdfjet.NewRect(c[0]+10.0, c[1]+20.0, 100.0, 80.0).
		SetFillColor(pale).SetBorderColor(navy).SetBorderWidth(2.0).DrawOn(page)

	// A rectangle with rounded corners
	c = cell(page, label, 2, 0, "Rect: setCornerRadius")
	pdfjet.NewRect(c[0]+10.0, c[1]+20.0, 100.0, 80.0).
		SetFillColor(teal).SetBorderColor(navy).SetBorderWidth(2.0).
		SetCornerRadius(16.0).DrawOn(page)

	// The markers of a chart, each a Point with a shape
	c = cell(page, label, 3, 0, "Point: the markers of charts")
	shapes := []shape.Shape{
		shape.Circle, shape.Diamond, shape.Box, shape.Star,
		shape.UpArrow, shape.DownArrow, shape.Plus, shape.XMark,
	}
	for i := 0; i < len(shapes); i++ {
		point := pdfjet.NewPoint(c[0]+20.0+float32(i%4)*27.0, c[1]+35.0+float32(i/4)*45.0)
		point.SetShape(shapes[i])
		point.SetRadius(9.0)
		if i%2 == 0 {
			point.SetFillColor(teal)
		} else {
			point.SetFillColor(red)
		}
		point.SetStrokeColor(navy)
		point.DrawOn(page)
	}

	// An ellipse, turned by 30 degrees
	c = cell(page, label, 0, 1, "Ellipse: setRotation")
	ellipse := pdfjet.NewEllipse()
	ellipse.SetLocation(c[0]+60.0, c[1]+60.0)
	ellipse.SetRadiusX(50.0)
	ellipse.SetRadiusY(25.0)
	ellipse.SetFillColor(pale)
	ellipse.SetStrokeWidth(2.0)
	ellipse.SetStrokeColor(navy)
	ellipse.SetRotation(30.0)
	ellipse.DrawOn(page)

	// An arc of three quarters of a circle
	c = cell(page, label, 1, 1, "Arc: setStartAngle and setSweep")
	arc := pdfjet.NewArc()
	arc.SetLocation(c[0]+60.0, c[1]+60.0)
	arc.SetRadius(40.0)
	arc.SetStartAngle(0.0)
	arc.SetSweep(270.0)
	arc.SetStrokeWidth(6.0)
	arc.SetStrokeColor(teal)
	arc.DrawOn(page)

	// A wave of Bézier curves: a point, two control points, a point
	c = cell(page, label, 2, 1, "Path: Bézier curves")
	wave := pdfjet.NewPath()
	wave.Add(pdfjet.NewPoint(0.0, 30.0))
	wave.Add(pdfjet.NewControlPointC(20.0, 0.0))
	wave.Add(pdfjet.NewControlPointC(30.0, 0.0))
	wave.Add(pdfjet.NewPoint(50.0, 30.0))
	wave.Add(pdfjet.NewControlPointC(70.0, 60.0))
	wave.Add(pdfjet.NewControlPointC(80.0, 60.0))
	wave.Add(pdfjet.NewPoint(100.0, 30.0))
	wave.SetStrokeColor(red)
	wave.SetStrokeWidth(3.0)
	wave.SetLocation(c[0]+10.0, c[1]+30.0)
	wave.DrawOn(page)

	// A closed path of lines, filled: a six-pointed star
	c = cell(page, label, 3, 1, "Path: closed and filled")
	star := pdfjet.NewPath()
	for i := 0; i < 12; i++ {
		angle := math.Pi/6.0*float64(i) - math.Pi/2.0
		var r float32 = 25.0
		if i%2 == 0 {
			r = 50.0
		}
		star.Add(pdfjet.NewPoint(
			50.0+r*float32(math.Cos(angle)), 50.0+r*float32(math.Sin(angle))))
	}
	star.SetClosed(true)
	star.SetFillShape(true)
	star.SetStrokeColor(navy)
	star.SetLocation(c[0]+10.0, c[1]+10.0)
	star.DrawOn(page)

	// The second page: text at every angle around a point
	page = pdfjet.NewPage(pdf, letter.Portrait())
	heading := pdfjet.NewTextLine(semiBold, "Text at every angle")
	heading.SetStructureType(structelem.H2)
	heading.SetFontSize(18.0)
	heading.SetLocation(left, 70.0)
	heading.DrawOn(page)

	rotation := pdfjet.NewTextBlock(regular,
		"setTextRotation turns a TextLine about the point of its location, here every "+
			"15 degrees about the middle of the page, underlined with setUnderline.")
	rotation.SetFontSize(11.0)
	rotation.SetLineSpacing(1.4)
	rotation.SetLocation(left, 85.0)
	rotation.SetWidth(512.0)
	rotation.DrawOn(page)

	cx := page.GetWidth() / 2.0
	var cy float32 = 380.0
	text := pdfjet.NewEmptyTextLine(regular)
	text.SetFontSize(10.0)
	text.SetUnderline(true)
	text.SetLocation(cx, cy)
	for i := 0; i < 360; i += 15 {
		text.SetTextRotation(-i)
		// The spaces keep the start of the text clear of the circle
		text.SetText("                        Hello, World: " + strconv.Itoa(i) + " degrees")
		text.DrawOn(page)
	}
	hub := pdfjet.NewPoint(cx, cy)
	hub.SetShape(shape.Circle)
	hub.SetFillColor(navy)
	hub.SetRadius(46.0)
	hub.DrawOn(page)
	hub.SetFillColor(color.White)
	hub.SetRadius(32.0)
	hub.DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// cell draws the name of the cell of the grid under it, and returns the top
// left corner of its drawing area.
func cell(page *pdfjet.Page, label *pdfjet.Font, column, row int, name string) [2]float32 {
	x := left + float32(column)*cellWidth
	y := top + float32(row)*cellHeight
	caption := pdfjet.NewTextBlock(label, name)
	caption.SetTextColor(color.Gray)
	caption.SetLocation(x, y+125.0)
	caption.SetWidth(cellWidth - 12.0)
	caption.DrawOn(page)
	return [2]float32{x, y}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example56()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_56 => %4d ms\n", time1-time0)
}
