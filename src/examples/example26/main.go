package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/mark"
)

// Example26 draws check boxes and radio buttons.
func Example26() {
	pdf := pdfjet.NewPDFFile("Example_26.pdf")

	f1 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	f1.SetSize(10.0)

	page := pdfjet.NewPage(pdf, letter.Portrait)

	var x float32 = 50.0
	var y float32 = 50.0

	pdfjet.NewCheckBox(f1, "Hello").
		SetCheckmark(color.Blue).
		Check(mark.Check).
		SetLocation(x, y).
		DrawOn(page)

	y += 30.0
	pdfjet.NewCheckBox(f1, "World!").
		SetCheckmark(color.Blue).
		SetURIAction("http://pdfjet.com").
		Check(mark.Check).
		SetLocation(x, y).
		DrawOn(page)

	y += 30.0
	pdfjet.NewCheckBox(f1, "This is a test.").
		SetURIAction("http://pdfjet.com").
		SetLocation(x, y).
		DrawOn(page)

	y += 30.0
	pdfjet.NewRadioButton(f1, "Hello, World!").
		SelectButton(true).
		SetLocation(x, y).
		DrawOn(page)

	xy := pdfjet.NewRadioButton(f1, "Yes").
		SetURIAction("http://pdfjet.com").
		SelectButton(true).
		SetLocation(x+100.0, 50.0).
		DrawOn(page)

	xy = pdfjet.NewRadioButton(f1, "No").
		SetLocation(xy[0], 50.0).
		DrawOn(page)

	xy = pdfjet.NewCheckBox(f1, "Hello").
		SetCheckmark(color.Blue).
		Check(mark.X).
		SetLocation(xy[0], 50.0).
		DrawOn(page)

	xy = pdfjet.NewCheckBox(f1, "Yahoo").
		SetCheckmark(color.Blue).
		Check(mark.Check).
		SetLocation(xy[0], 50.0).
		DrawOn(page)

	box := pdfjet.NewBox()
	box.SetLocation(xy[0], xy[1])
	box.SetSize(20.0, 20.0)
	box.DrawOn(page)

	pdf.Complete()
}

func main() {
	time0 := time.Now().UnixMilli()
	Example26()
	time1 := time.Now().UnixMilli()
	pdfjet.PrintDuration("Example_26", time0, time1)
}
