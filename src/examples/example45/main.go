// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// Example45 uses the Form and Field classes to draw a shipment request. A
// field at x = 0 starts a new row, the other fields of the row start at their
// own x, and a field with an empty label continues the value above it on a
// new line.
func Example45() {
	pdf, err := pdfjet.NewPDFFile("Example_45.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Shipment Request")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	text := pdfjet.NewTextLine(f2, "Shipment Request")
	text.SetStructureType(structelem.H1)
	text.SetFontSize(22.0)
	text.SetLocation(56.0, 80.0)
	text.DrawOn(page)

	textBlock := pdfjet.NewTextBlock(f1,
		"A Form draws its fields in rows. A field at x = 0 starts a new row, "+
			"the other fields of the row start at their own x, and a field with "+
			"an empty label continues the value above it on a new line.")
	textBlock.SetFontSize(12.0)
	textBlock.SetLineSpacing(1.5)
	textBlock.SetLocation(56.0, 95.0)
	textBlock.SetWidth(500.0)
	xy := textBlock.DrawOn(page)

	var w float32 = 500.0 // The width of the form

	fields := make([]*pdfjet.Field, 0)
	fields = append(fields, pdfjet.NewField(0.0, "Sender", "Maple Leaf Instruments Ltd."))
	fields = append(fields, pdfjet.NewField(0.0, "Street Address", "480 King Street West"))
	fields = append(fields, pdfjet.NewField(6*w/8, "Suite", "1200"))
	fields = append(fields, pdfjet.NewField(0.0, "City", "Toronto"))
	fields = append(fields, pdfjet.NewField(3*w/8, "Province", "Ontario"))
	fields = append(fields, pdfjet.NewField(5*w/8, "Postal Code", "M5V 1L7"))
	fields = append(fields, pdfjet.NewField(6*w/8, "Country", "Canada"))
	fields = append(fields, pdfjet.NewField(0.0, "Recipient", "Nordic Sensor Labs AB"))
	fields = append(fields, pdfjet.NewField(0.0, "Street Address", "Drottninggatan 55"))
	fields = append(fields, pdfjet.NewField(6*w/8, "Floor", "3"))
	fields = append(fields, pdfjet.NewField(0.0, "City", "Stockholm"))
	fields = append(fields, pdfjet.NewField(5*w/8, "Postal Code", "111 21"))
	fields = append(fields, pdfjet.NewField(6*w/8, "Country", "Sweden"))
	fields = append(fields, pdfjet.NewField(0.0, "Contact", "Anna Lindqvist"))
	fields = append(fields, pdfjet.NewField(3*w/8, "Email", "anna.lindqvist@example.com"))
	fields = append(fields, pdfjet.NewField(0.0, "Contents", "Two calibrated pressure sensors"))
	fields = append(fields, pdfjet.NewField(5*w/8, "Weight", "3.2 kg"))
	fields = append(fields, pdfjet.NewField(6*w/8, "Declared Value", "CAD 1,450.00"))
	fields = append(fields, pdfjet.NewField(0.0, "Instructions",
		"Keep upright and away from magnets. Deliver on a weekday"))
	fields = append(fields, pdfjet.NewField(0.0, "", "between 9:00 and 17:00, to the reception on the third floor."))

	xy = pdfjet.NewForm(fields).
		SetLabelFont(f1).
		SetLabelFontSize(8.0).
		SetLabelColor(color.Gray).
		SetValueFont(f2).
		SetValueFontSize(10.0).
		SetValueColor(color.Black).
		SetWidth(w).
		SetStrokeWidth(0.5).
		SetLocation(56.0, xy[1]+20.0).
		DrawOn(page)

	text = pdfjet.NewTextLine(f1, "The recipient signs for the package on delivery.")
	text.SetFontSize(10.0)
	text.SetTextColor(color.Gray)
	text.SetLocation(56.0, xy[1]+20.0)
	text.DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example45()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_45 => %4d ms\n", time1-time0)
}
