package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/corefont"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example23 draws the Canadian flag using a Path object that contains both lines
// and curve segments. Every curve segment must have exactly 2 control points.
func Example23() {
	pdf := pdfjet.NewPDFFile("Example_23.pdf")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f1.SetSize(72.0)

	f2 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())
	f2.SetSize(24.0)

	page := pdfjet.NewPage(pdf, letter.Portrait)

	x1 := float32(90.0)
	y1 := float32(50.0)

	textLine := pdfjet.NewTextLine(f2, "(x1, y1)")
	textLine.SetLocation(x1, y1-15.0)
	textLine.DrawOn(page)

	textBlock := pdfjet.NewTextBlock(f1,
		"Hello, World! This example shows the functionality of the TextBlock.")
	textBlock.SetLocation(x1, y1)
	textBlock.SetWidth(500.0)
	textBlock.SetFillColor(color.LightGreen)
	textBlock.SetTextColor(color.Black)
	xy := textBlock.DrawOn(page)

	// Text on the left
	ascentText := pdfjet.NewTextLine(f2, "Ascent")
	ascentText.SetFontSize(18.0)
	ascentText.SetLocation(x1-85.0, y1+40.0)
	ascentText.DrawOn(page)

	descentText := pdfjet.NewTextLine(f2, "Descent")
	descentText.SetFontSize(18.0)
	descentText.SetLocation(x1-85.0, y1+f1.GetAscent(f1.GetSize())+15.0)
	descentText.DrawOn(page)

	// Line beside the text ascent
	ascentLine := pdfjet.NewLine(
		x1-10.0,
		y1,
		x1-10.0,
		y1+f1.GetAscent(f1.GetSize()))
	ascentLine.SetColor(color.Blue)
	ascentLine.SetWidth(3.0)
	ascentLine.DrawOn(page)

	// Line beside the text descent
	descentLine := pdfjet.NewLine(
		x1-10.0,
		y1+f1.GetAscent(f1.GetSize()),
		x1-10.0,
		y1+f1.GetAscent(f1.GetSize())+f1.GetDescent(f1.GetSize()))
	descentLine.SetColor(color.Red)
	descentLine.SetWidth(3.0)
	descentLine.DrawOn(page)

	// Lines for first line of text
	textLine1 := pdfjet.NewLine(
		x1,
		y1+f1.GetAscent(f1.GetSize()),
		xy[1],
		y1+f1.GetAscent(f1.GetSize()))
	textLine1.DrawOn(page)

	descentLine1 := pdfjet.NewLine(
		x1,
		y1+(f1.GetAscent(f1.GetSize())+f1.GetDescent(f1.GetSize())),
		xy[1],
		y1+(f1.GetAscent(f1.GetSize())+f1.GetDescent(f1.GetSize())))
	descentLine1.DrawOn(page)

	// Lines for second line of text
	curr_y := y1 + f1.GetBodyHeight(f1.GetSize())

	textLine2 := pdfjet.NewLine(
		x1,
		curr_y+f1.GetAscent(f1.GetSize()),
		xy[1],
		curr_y+f1.GetAscent(f1.GetSize()))
	textLine2.DrawOn(page)

	descentLine2 := pdfjet.NewLine(
		x1,
		curr_y+f1.GetAscent(f1.GetSize())+f1.GetDescent(f1.GetSize()),
		xy[1],
		curr_y+f1.GetAscent(f1.GetSize())+f1.GetDescent(f1.GetSize()))
	descentLine2.DrawOn(page)

	p1 := pdfjet.NewPoint(x1, y1)
	p1.SetRadius(5.0)
	p1.DrawOn(page)

	p2 := pdfjet.NewPoint(xy[0], xy[1])
	p2.SetRadius(5.0)
	p2.DrawOn(page)

	f2.SetSize(24.0)
	textLine3 := pdfjet.NewTextLine(f2, "(x2, y2)")
	textLine3.SetLocation(xy[0]-80.0, xy[1]+30.0)
	textLine3.DrawOn(page)

	box := pdfjet.NewBox()
	box.SetLocation(xy[0], xy[1])
	box.SetSize(20.0, 20.0)
	box.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example23()
	pdfjet.PrintDuration("Example_23", time.Since(start))
}
