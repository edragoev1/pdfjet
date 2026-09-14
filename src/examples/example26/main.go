package main

import (
	"fmt"
	"log"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/mark"
)

// Example26 draws check boxes and radio buttons.
func Example26() {
	pdf, err := pdfjet.NewPDFFile("Example_26.pdf")
	if err != nil {
		log.Fatal(err)
	}

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)
	f1.SetSize(10.0)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	var x float32 = 50.0
	var y float32 = 50.0

	pdfjet.NewCheckBox(f1, "Hello").
		SetCheckmarkColor(color.Blue).
		Check(mark.Check).
		SetLocation(x, y).
		DrawOn(page)

	y += 30.0
	pdfjet.NewCheckBox(f1, "World!").
		SetCheckmarkColor(color.Blue).
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
		Select(true).
		SetLocation(x, y).
		DrawOn(page)

	xy := pdfjet.NewRadioButton(f1, "Yes").
		SetURIAction("http://pdfjet.com").
		Select(true).
		SetLocation(x+100.0, 50.0).
		DrawOn(page)

	xy = pdfjet.NewRadioButton(f1, "No").
		SetLocation(xy[0], 50.0).
		DrawOn(page)

	xy = pdfjet.NewCheckBox(f1, "Hello").
		SetCheckmarkColor(color.Blue).
		Check(mark.X).
		SetLocation(xy[0], 50.0).
		DrawOn(page)

	xy = pdfjet.NewCheckBox(f1, "Yahoo").
		SetCheckmarkColor(color.Blue).
		Check(mark.Check).
		SetLocation(xy[0], 50.0).
		DrawOn(page)

	rect := pdfjet.NewRect(xy[0], xy[1], 20.0, 20.0)
	rect.SetBorderColor(color.Black)
	rect.DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example26()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_26 => %d ms\n", time1-time0)
}
