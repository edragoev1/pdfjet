package main

import (
	"fmt"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/corefont"
	"github.com/edragoev1/pdfjet/src/effect"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example25 draws the Canadian flag using a Path object that contains both lines
// and curve segments. Every curve segment must have exactly 2 control points.
func Example25() {
	pdf := pdfjet.NewPDFFile("Example_25.pdf", compliance.PDF15)

	f1 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())
	f2 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	f3 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())
	f4 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	f5 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())
	f6 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())

	page := pdfjet.NewPage(pdf, letter.Portrait)

	composite := pdfjet.NewCompositeTextLine(50.0, 50.0)
	composite.SetFontSize(14.0)

	text1 := pdfjet.NewTextLine(f1, "C")
	text2 := pdfjet.NewTextLine(f2, "6")
	text3 := pdfjet.NewTextLine(f3, "H")
	text4 := pdfjet.NewTextLine(f4, "12")
	text5 := pdfjet.NewTextLine(f5, "O")
	text6 := pdfjet.NewTextLine(f6, "6")

	text1.SetColor(color.Dodgerblue)
	text3.SetColor(color.Dodgerblue)
	text5.SetColor(color.Dodgerblue)

	text2.SetTextEffect(effect.Subscript)
	text4.SetTextEffect(effect.Subscript)
	text6.SetTextEffect(effect.Subscript)

	composite.AddComponent(text1)
	composite.AddComponent(text2)
	composite.AddComponent(text3)
	composite.AddComponent(text4)
	composite.AddComponent(text5)
	composite.AddComponent(text6)

	xy := composite.DrawOn(page)

	box := pdfjet.NewBox()
	box.SetLocation(xy[0], xy[1])
	box.SetSize(20.0, 20.0)
	box.DrawOn(page)

	composite2 := pdfjet.NewCompositeTextLine(50.0, 100.0)
	composite2.SetFontSize(14.0)

	text1 = pdfjet.NewTextLine(f1, "SO")
	text2 = pdfjet.NewTextLine(f2, "4")
	text3 = pdfjet.NewTextLine(f4, "2-") // Use bold font here

	text2.SetTextEffect(effect.Subscript)
	text3.SetTextEffect(effect.Superscript)

	composite2.AddComponent(text1)
	composite2.AddComponent(text2)
	composite2.AddComponent(text3)

	composite2.DrawOn(page)
	composite2.SetLocation(100.0, 150.0)
	composite2.DrawOn(page)

	yy := composite2.GetMinMax()

	line1 := pdfjet.NewLine(50.0, yy[0], 200.0, yy[0])
	line1.DrawOn(page)

	line2 := pdfjet.NewLine(50.0, yy[1], 200.0, yy[1])
	line2.DrawOn(page)

	chart := pdfjet.NewDonutChart()
	chart.SetLocation(300.0, 300.0)
	chart.SetR1AndR2(200.0, 100.0)
	chart.AddSlice(pdfjet.NewSlice(10.0, color.Red))
	chart.AddSlice(pdfjet.NewSlice(20.0, color.Green))
	chart.AddSlice(pdfjet.NewSlice(30.0, color.Blue))
	chart.AddSlice(pdfjet.NewSlice(40.0, color.Peachpuff))
	chart.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example25()
	elapsed := time.Since(start)
	fmt.Printf("Example_25 => %dµs\n", elapsed.Microseconds())
}
