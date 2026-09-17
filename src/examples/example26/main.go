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
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/mark"
)

// Example26 draws a survey with check boxes and radio buttons.
func Example26() {
	pdf, err := pdfjet.NewPDFFile("Example_26.pdf")
	if err != nil {
		log.Fatal(err)
	}

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f1.SetSize(11.0)

	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f2.SetSize(12.0)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	var x float32 = 70.0
	var y float32 = 90.0

	text := pdfjet.NewTextLine(f2, "Customer Survey")
	text.SetFontSize(22.0)
	text.SetLocation(x, y)
	text.DrawOn(page)

	// Check boxes, one below the other.
	y += 50.0
	pdfjet.NewTextLine(f2, "Which PDFjet ports do you use?").SetLocation(x, y).DrawOn(page)

	y += 15.0
	pdfjet.NewCheckBox(f1, "Java").
		SetCheckmarkColor(color.Blue).
		Check(mark.Check).
		SetLocation(x, y).
		DrawOn(page)

	y += 25.0
	pdfjet.NewCheckBox(f1, "C#").
		SetCheckmarkColor(color.Blue).
		Check(mark.Check).
		SetLocation(x, y).
		DrawOn(page)

	y += 25.0
	pdfjet.NewCheckBox(f1, "Swift").
		SetLocation(x, y).
		DrawOn(page)

	y += 25.0
	pdfjet.NewCheckBox(f1, "Go").
		SetLocation(x, y).
		DrawOn(page)

	// Radio buttons in a row. Each one starts where the one before it ends.
	y += 50.0
	pdfjet.NewTextLine(f2, "How did you hear about PDFjet?").SetLocation(x, y).DrawOn(page)

	y += 15.0
	xy := pdfjet.NewRadioButton(f1, "Web search").
		Select(true).
		SetLocation(x, y).
		DrawOn(page)

	xy = pdfjet.NewRadioButton(f1, "A colleague").
		SetLocation(xy[0]+20.0, y).
		DrawOn(page)

	pdfjet.NewRadioButton(f1, "Other").
		SetLocation(xy[0]+20.0, y).
		DrawOn(page)

	y += 50.0
	pdfjet.NewTextLine(f2, "Would you recommend PDFjet?").SetLocation(x, y).DrawOn(page)

	y += 15.0
	xy = pdfjet.NewRadioButton(f1, "Yes").
		Select(true).
		SetLocation(x, y).
		DrawOn(page)

	pdfjet.NewRadioButton(f1, "No").
		SetLocation(xy[0]+20.0, y).
		DrawOn(page)

	// A check box marked with an X, and one with a link.
	y += 50.0
	pdfjet.NewTextLine(f2, "Stay in touch").SetLocation(x, y).DrawOn(page)

	y += 15.0
	pdfjet.NewCheckBox(f1, "Send me news about new releases").
		SetCheckmarkColor(color.Red).
		Check(mark.X).
		SetLocation(x, y).
		DrawOn(page)

	y += 25.0
	xy = pdfjet.NewCheckBox(f1, "Visit https://pdfjet.com").
		SetURIAction("https://pdfjet.com").
		SetLocation(x, y).
		DrawOn(page)

	// A border around the survey.
	rect := pdfjet.NewRect(50.0, 50.0, 512.0, xy[1]+25.0-50.0)
	rect.SetBorderColor(color.LightGray)
	rect.DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example26()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_26 => %4d ms\n", time1-time0)
}
