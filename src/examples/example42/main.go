package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/a4"
	"github.com/edragoev1/pdfjet/src/corefont"
)

// Example42 -- TODO:
func Example42() {
	pdf := pdfjet.NewPDFFile("Example_42.pdf")

	f1 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	f2 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())

	page := pdfjet.NewPage(pdf, a4.Portrait)

	var w float32 = 500.0

	fields := make([]*pdfjet.Field, 0)
	fields = append(fields, pdfjet.NewField(0.0, "Company", "Smart Widgets Construction Inc."))
	fields = append(fields, pdfjet.NewField(0.0, "Street Number", "120"))
	fields = append(fields, pdfjet.NewField(1*w/8, "Street Name", "Oak"))
	fields = append(fields, pdfjet.NewField(4*w/8, "Street Type", "Street"))
	fields = append(fields, pdfjet.NewField(5*w/8, "Direction", "West"))
	fields = append(fields, pdfjet.NewField(6*w/8, "Suite/Floor/Apt.", "8W"))
	fields = append(fields, pdfjet.NewField(0.0, "City/Town", "Toronto"))
	fields = append(fields, pdfjet.NewField(4*w/8, "Province", "Ontario"))
	fields = append(fields, pdfjet.NewField(7*w/8, "Postal Code", "M5M 2N2"))
	fields = append(fields, pdfjet.NewField(0.0, "Telephone Number", "(416) 331-2245"))
	fields = append(fields, pdfjet.NewField(2*w/8, "Fax (if applicable)", "(416) 124-9879"))
	fields = append(fields, pdfjet.NewField(4*w/8, "Email", "jsmith12345@gmail.ca"))
	fields = append(fields, pdfjet.NewField(0.0, "Other Information", "We don't work on weekends."))
	fields = append(fields, pdfjet.NewField(0.0, "", "Please send us an Email."))

	form := pdfjet.NewForm(fields)
	form.SetLabelFont(f1)
	form.SetLabelFontSize(8.0)
	form.SetValueFont(f2)
	form.SetValueFontSize(10.0)
	form.SetLocation(50.0, 50.0)
	form.SetFormWidth(w)
	form.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example42()
	pdfjet.PrintDuration("Example_42", time.Since(start))
}
