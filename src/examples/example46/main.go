package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example46 draws the Canadian flag using a Path object that contains both lines
// and curve segments. Every curve segment must have exactly 2 control points.
func Example46() {
	pdf := pdfjet.NewPDFFile("Example_46.pdf")

	// font := pdfjet.NewCoreFont(pdf, corefont.Helvetica())
	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)

	image1 := pdfjet.NewImageFromFile(pdf, "images/map407.png")
	image1.SetLocation(10.0, 100.0)

	image2 := pdfjet.NewImageFromFile(pdf, "images/qrcode.png")
	image2.SetLocation(10.0, 100.0)

	// Create the first page after all the resources have been added to the PDF.
	page := pdfjet.NewPage(pdf, letter.Portrait)

	textLine := pdfjet.NewTextLine(f1, "© OpenStreetMap contributors")
	textLine.SetLocation(10.0, 655.0)
	xy := textLine.DrawOn(page)

	uri := "http://www.openstreetmap.org/copyright"
	textLine = pdfjet.NewTextLine(f1, "http://www.openstreetmap.org/copyright")
	textLine.SetURIAction(uri)
	textLine.SetLocation(10.0, xy[1]+f1.GetHeight())
	textLine.DrawOn(page)

	group := pdfjet.NewOptionalContentGroup(pdf, "map")
	group.Add(image1)
	group.SetVisible(true)
	group.SetPrintable(true)
	group.DrawOn(page)

	textBox := pdfjet.NewTextBox(f1)
	textBox.SetText("Blue Layer Text")
	textBox.SetLocation(10.0, 130.0)

	line := pdfjet.NewLine(300.0, 150.0, 500.0, 150.0)
	line.SetWidth(2.0)
	line.SetColor(color.Blue)

	group = pdfjet.NewOptionalContentGroup(pdf, "Blue Line")
	group.Add(textBox)
	group.Add(line)
	group.SetVisible(true)
	group.DrawOn(page)

	line = pdfjet.NewLine(350.0, 160.0, 550.0, 160.0)
	line.SetWidth(2.0)
	line.SetColor(color.Red)

	group = pdfjet.NewOptionalContentGroup(pdf, "barcode")
	group.Add(image2)
	group.Add(line)
	group.SetPrintable(true)
	group.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example46()
	pdfjet.PrintDuration("Example_30", time.Since(start))
}
