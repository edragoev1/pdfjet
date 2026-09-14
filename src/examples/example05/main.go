package main

import (
	"fmt"
	"strconv"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/shape"
)

// Example05 draws text at every angle around a point, and the words "WAVE AWAY" with and
// without kerning, in the core font Helvetica-Bold, which is not embedded.
//
// A core font is one of the fourteen fonts every PDF viewer has, so the
// document carries no font program: it is small, it is written fast, and the
// kerning pairs and the widths of the font are built into the library, which
// is what SetKernPairs shows. The disadvantages: the viewer draws the text with
// its own version of the font, so the look differs a little between viewers;
// only the WinAnsi characters can be drawn, so no Cyrillic, Greek or CJK text;
// and a document with a font that is not embedded cannot claim PDF/A or PDF/UA
// compliance. For those, use an embedded font like IBMPlexSans, as the
// other examples do.
func Example05() {
	pdf := pdfjet.NewPDFFile("Example_05.pdf")

	f1 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	f1.SetItalic(true)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	text := pdfjet.NewTextLine(f1, "")
	text.SetLocation(300.0, 300.0)
	for i := 0; i < 360; i += 15 {
		text.SetTextRotation(i)
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
	point.SetFillColor(color.Blue)
	point.SetRadius(37.0)
	point.DrawOn(page)
	point.SetRadius(25.0)
	point.SetFillColor(color.White)
	point.DrawOn(page)

	arc := pdfjet.NewArc()
	arc.SetLocation(300.0, 600.0)
	arc.SetRadiusX(75.0)
	arc.SetRadiusY(75.0)
	arc.SetStartAngle(0.0)
	arc.SetSweepDegreesCW(270.0)
	// arc.SetSweepDegreesCCW(270.0)
	// arc.ScaleBy(2.0)
	// arc.SetRotationClockwise(90.0)
	// arc.SetRotation(90.0)
	arc.SetStrokeWidth(5.0)
	arc.SetStrokeColor(color.Blue)
	arc.DrawOn(page)

	ellipse := pdfjet.NewEllipse()
	ellipse.SetLocation(300.0, 720.0)
	ellipse.SetRadiusX(100.0)
	ellipse.SetRadiusY(50.0)
	ellipse.SetFillColor(color.Azure)
	ellipse.SetStrokeWidth(1.5)
	ellipse.SetStrokeColor(color.Blue)
	ellipse.ScaleBy(0.5)
	ellipse.SetRotation(-45.0)
	// ellipse.SetRotation(45.0)
	ellipse.DrawOn(page)

	pdf.Complete()
}

func main() {
	time0 := time.Now().UnixMilli()
	Example05()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_05 => %d ms\n", time1-time0)
}
