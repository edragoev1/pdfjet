package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/a4"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/compliance"
)

// Example45 -- TODO:
func Example45() {
	pdf := pdfjet.NewPDFFile("Example_45.pdf", compliance.PDF_UA)
	pdf.SetLanguage("en-US")
	pdf.SetTitle("Hello, World!")

	f1 := pdfjet.NewFontFromFile(pdf, "fonts/Droid/DroidSerif-Regular.ttf.stream")
	f2 := pdfjet.NewFontFromFile(pdf, "fonts/Droid/DroidSerif-Italic.ttf.stream")
	f3 := pdfjet.NewFontFromFile(pdf, "fonts/SourceCodePro/SourceCodePro-Regular.ttf.stream")

	f1.SetSize(14.0)
	f2.SetSize(14.0)
	f3.SetSize(10.0)

	page := pdfjet.NewPage(pdf, a4.Portrait)

	var width float32 = 530.0
	var height float32 = 13.0

	fields := make([]*pdfjet.Field, 0)
	fields = append(fields, pdfjet.NewField(
		0.0, []string{"Company", "Smart Widget Designs"}, false))
	fields = append(fields, pdfjet.NewField(
		0.0, []string{"Street Number", "120"}, false))
	fields = append(fields, pdfjet.NewField(
		width/8, []string{"Street Name", "Oak"}, false))
	fields = append(fields, pdfjet.NewField(
		5*width/8, []string{"Street Type", "Street"}, false))
	fields = append(fields, pdfjet.NewField(
		6*width/8, []string{"Direction", "West"}, false))
	fields = append(fields, pdfjet.NewField(
		7*width/8, []string{"Suite/Floor/Apt.", "8W"}, false).SetAltDescription(
		"Suite/Floor/Apartment").SetActualText("Suite/Floor/Apartment"))
	fields = append(fields, pdfjet.NewField(
		0.0, []string{"City/Town", "Toronto"}, false))
	fields = append(fields, pdfjet.NewField(
		width/2, []string{"Province", "Ontario"}, false))
	fields = append(fields, pdfjet.NewField(
		7*width/8, []string{"Postal Code", "M5M 2N2"}, false))
	fields = append(fields, pdfjet.NewField(
		0.0, []string{"Telephone Number", "(416) 331-2245"}, false))
	fields = append(fields, pdfjet.NewField(
		width/4, []string{"Fax (if applicable)", "(416) 124-9879"}, false))
	fields = append(fields, pdfjet.NewField(
		width/2, []string{"Email", "jsmith12345@gmail.ca"}, false))
	fields = append(fields, pdfjet.NewField(
		0.0, []string{"Other Information",
			"We don't work on weekends.", "Please send us an Email."}, false))

	form := pdfjet.NewForm(fields)
	form.SetLabelFont(f1)
	form.SetLabelFontSize(7.0)
	form.SetValueFont(f2)
	form.SetValueFontSize(9.0)
	form.SetLocation(50.0, 50.0)
	form.SetRowWidth(width)
	form.SetRowHeight(height)
	form.DrawOn(page)

	colors := make(map[string]int32)
	colors["float32"] = color.Red
	colors["false"] = color.Red
	colors["ArrayList"] = color.Blue
	colors["List"] = color.Blue
	colors["String"] = color.Blue
	colors["Field"] = color.Blue
	colors["NewField"] = color.Blue
	colors["NewForm"] = color.Blue
	colors["Smart"] = color.Green
	colors["Widget"] = color.Green
	colors["Designs"] = color.Green

	var x float32 = 50.0
	var y float32 = 280.0
	leading := f3.GetBodyHeight()
	lines := pdfjet.ReadTextLines("data/form-code-go.txt")
	for _, line := range lines {
		page.DrawStringUsingColorMap(f3, nil, line, x, y, color.Black, colors)
		y += leading
	}

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example45()
	pdfjet.PrintDuration("Example_45", time.Since(start))
}
