package main

import (
	"strconv"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/corefont"
	"github.com/edragoev1/pdfjet/src/letter"
	"github.com/edragoev1/pdfjet/src/shape"
)

// Example05 shows how to draw text at different angles.
func Example05() {
	pdf := pdfjet.NewPDFFile("Example_05.pdf")

	f1 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	f1.SetItalic(true)

	page := pdfjet.NewPage(pdf, letter.Portrait)

	text := pdfjet.NewTextLine(f1, "")
	text.SetLocation(300.0, 300.0)
	for i := 0; i < 360; i += 15 {
		text.SetTextDirection(i)
		text.SetUnderline(true)
		// text.setStrikeLine(true);
		text.SetText("             Hello, World -- " + strconv.Itoa(i) + " degrees.")
		text.DrawOn(page)
	}

	text = pdfjet.NewTextLine(f1, "WAVE AWAY")
	text.SetLocation(70.0, 50.0)
	text.DrawOn(page)

	f1.SetKernPairs(true)
	text = pdfjet.NewTextLine(f1, "WAVE AWAY")
	text.SetLocation(70.0, 70.0)
	text.DrawOn(page)

	f1.SetKernPairs(false)
	text = pdfjet.NewTextLine(f1, "WAVE AWAY")
	text.SetLocation(70.0, 90.0)
	text.DrawOn(page)

	f1.SetSize(8.0)
	text = pdfjet.NewTextLine(f1, "-- font.setKernPairs(false);")
	text.SetLocation(150.0, 50.0)
	text.DrawOn(page)
	text.SetLocation(150.0, 90.0)
	text.DrawOn(page)
	text = pdfjet.NewTextLine(f1, "-- font.setKernPairs(true);")
	text.SetLocation(150.0, 70.0)
	text.DrawOn(page)

	point := pdfjet.NewPoint(300.0, 300.0)
	point.SetShape(shape.Circle)
	point.SetFillShape(true)
	point.SetFillColor(color.Blue)
	point.SetRadius(37.0)
	point.DrawOn(page)
	point.SetRadius(25.0)
	point.SetTextColor(color.White)
	point.DrawOn(page)

	arc := new(pdfjet.Arc)
	arc.SetCenterXY(300.0, 600.0)
	arc.SetRadiusX(75.0)
	arc.SetRadiusY(75.0)
	arc.SetStartAngle(0.0)
	arc.SetSweepDegreesCW(270.0)
	// arc.SetSweepDegreesCCW(270.0)
	// arc.SetScaleFactor(2.0)
	// arc.SetRotateDegreesCW(90.0)
	// arc.SetRotateDegreesCCW(90.0)
	arc.SetStrokeWidth(5.0)
	arc.SetStrokeColor(color.Blue)
	arc.DrawOn(page)

	ellipse := pdfjet.NewEllipse()
	ellipse.SetCenterXY(300.0, 720.0)
	ellipse.SetRadiusX(100.0)
	ellipse.SetRadiusY(50.0)
	ellipse.SetFillColor(color.Azure)
	ellipse.SetStrokeWidth(1.5)
	ellipse.SetStrokeColor(color.Blue)
	ellipse.SetScaleFactor(0.5)
	ellipse.SetRotateDegreesCW(45.0)
	// ellipse.SetRotateDegreesCCW(45.0)
	ellipse.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example05()
	pdfjet.PrintDuration("Example_05", time.Since(start))
}
