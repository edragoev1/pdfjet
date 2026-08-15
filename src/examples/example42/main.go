package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/src/a4"
	"github.com/edragoev1/pdfjet/src/color"
)

// Example42 -- TODO:
func Example42() {
	pdf := pdfjet.NewPDFFile("Example_42.pdf")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)

	page := pdfjet.NewPage(pdf, a4.Portrait)

	var w float32 = 500.0

	fields := make([]*pdfjet.Field, 0)
	fields = append(fields, pdfjet.NewField(0.0, "Company", "Smart Widgets Inc."))
	fields = append(fields, pdfjet.NewField(0.0, "Street #", "120"))
	fields = append(fields, pdfjet.NewField(w/8, "Street Name", "Oak"))
	fields = append(fields, pdfjet.NewField(4*w/8, "Street Type", "Street"))
	fields = append(fields, pdfjet.NewField(5*w/8, "Direction", "West"))
	fields = append(fields, pdfjet.NewField(6*w/8, "Suite/Floor/Apartment", "8W"))
	fields = append(fields, pdfjet.NewField(0.0, "City/Town", "Toronto"))
	fields = append(fields, pdfjet.NewField(4*w/8, "Province", "Ontario"))
	fields = append(fields, pdfjet.NewField(7*w/8, "Postal Code", "M5M 2N2"))
	fields = append(fields, pdfjet.NewField(0.0, "Telephone #", "(416) 331-2245"))
	fields = append(fields, pdfjet.NewField(2*w/8, "Fax #", "(416) 124-9879"))
	fields = append(fields, pdfjet.NewField(4*w/8, "Email", "jsmith12345@gmail.ca"))
	fields = append(fields, pdfjet.NewField(0.0, "Other Information",
		"Smart Widgets Inc. designs intelligent IoT widgets that connect everyday appliances to cloud ecosystems,"))
	fields = append(fields, pdfjet.NewField(0.0, "", "enabling remote control and predictive maintenance."))

	form := pdfjet.NewForm(fields)
	form.SetLabelFont(f1)
	form.SetLabelFontSize(9.0)
	form.SetValueFont(f2)
	form.SetValueFontSize(9.0)
	form.SetLocation(50.0, 50.0)
	form.SetFormWidth(w)
	form.SetLineWidth(0.2)
	xy := form.DrawOn(page)

	rect := pdfjet.NewRect(xy[0], xy[1], 10.0, 10.0)
	rect.SetBorderWidth(0.2)
	rect.SetBorderColor(color.Blue)
	rect.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example42()
	pdfjet.PrintDuration("Example_42", time.Since(start))
}
