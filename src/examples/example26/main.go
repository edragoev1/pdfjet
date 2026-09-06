package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/corefont"
	"github.com/edragoev1/pdfjet/src/letter"
	"github.com/edragoev1/pdfjet/src/mark"
)

func Example26() {
	pdf := pdfjet.NewPDFFile("Example_26.pdf")

	f1 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	f1.SetSize(10.0)

	page := pdfjet.NewPage(pdf, letter.Portrait)

	var x float32 = 50.0
	var y float32 = 50.0

	checkBox := pdfjet.NewCheckBox(f1, "Hello")
	checkBox.SetLocation(x, y)
	checkBox.SetCheckmark(color.Blue)
	checkBox.Check(mark.Check)
	checkBox.DrawOn(page)

	y += 30.0
	checkBox = pdfjet.NewCheckBox(f1, "World!")
	checkBox.SetLocation(x, y)
	checkBox.SetCheckmark(color.Blue)
	checkBox.SetURIAction("http://pdfjet.com")
	checkBox.Check(mark.Check)
	checkBox.DrawOn(page)

	y += 30.0
	checkBox = pdfjet.NewCheckBox(f1, "This is a test.")
	checkBox.SetLocation(x, y)
	checkBox.SetURIAction("http://pdfjet.com")
	checkBox.DrawOn(page)

	y += 30.0
	radioButton := pdfjet.NewRadioButton(f1, "Hello, World!")
	radioButton.SetLocation(x, y)
	radioButton.SelectButton(true)
	radioButton.DrawOn(page)

	radioButton = pdfjet.NewRadioButton(f1, "Yes")
	radioButton.SetLocation(x+100.0, 50.0)
	radioButton.SetURIAction("http://pdfjet.com")
	radioButton.SelectButton(true)
	xy := radioButton.DrawOn(page)

	radioButton = pdfjet.NewRadioButton(f1, "No")
	radioButton.SetLocation(xy[0], 50.0)
	xy = radioButton.DrawOn(page)

	checkBox = pdfjet.NewCheckBox(f1, "Hello")
	checkBox.SetLocation(xy[0], 50.0)
	checkBox.SetCheckmark(color.Blue)
	checkBox.Check(mark.X)
	xy = checkBox.DrawOn(page)

	checkBox = pdfjet.NewCheckBox(f1, "Yahoo")
	checkBox.SetLocation(xy[0], 50.0)
	checkBox.SetCheckmark(color.Blue)
	checkBox.Check(mark.Check)
	xy = checkBox.DrawOn(page)

	box := pdfjet.NewBox()
	box.SetLocation(xy[0], xy[1])
	box.SetSize(20.0, 20.0)
	box.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example26()
	pdfjet.PrintDuration("Example_26", time.Since(start))
}
