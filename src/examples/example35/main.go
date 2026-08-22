//
// Example_35.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.
//

package main

import (
	"time"

	"github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example35 demonstrates stamp and container usage
func Example35() {
	// Create PDF output
	pdf := pdfjet.NewPDFFile("Example_18.pdf")

	// Create page
	page := pdfjet.NewPage(pdf, letter.Portrait)

	// Load fonts
	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f1.SetSize(14.0)

	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)
	f2.SetSize(14.0)

	// Base container
	container := pdfjet.NewContainer(400.0, 400.0)
	container.SetLocation(100.0, 100.0)

	// Add a rectangle to container
	rect := pdfjet.NewRect(0.0, 0.0, 400.0, 400.0)
	rect.SetFillColor(color.Gray)
	container.Add(rect)

	// Create stamp
	stamp := pdfjet.NewStamp(pdf).
		WithSize(400.0, 400.0).
		WithFont(f1).
		WithFont(f2)

	// Draw path ...
	stamp.SetFillColor(color.LightBlue).
		SetStrokeColor(color.Red).
		SetStrokeWidth(4.0).
		MoveTo(0.0, 0.0).
		LineTo(400.0, 0.0).
		LineTo(400.0, 400.0).
		LineTo(0.0, 400.0).
		CloseFillAndStrokePath()

	// Draw Rectangle
	stamp.SetStrokeColor(color.Blue).
		SetStrokeWidth(1.0).
		DrawRect(10.0, 10.0, 380.0, 380.0)

	// Fill Rectangle
	stamp.SetFillColor(color.Green).
		FillRect(10.0, 10.0, 20.0, 20.0)

	// Draw some text
	params := pdfjet.NewTextParameters().
		SetFont(f1).
		SetFontSize(14.0).
		SetTextLocation(25.0, 25.0).
		SetText("Hello, World!")
	stamp.DrawText(params)

	// Change some parameters and draw the text again
	params.SetFont(f2).SetTextLocation(25.0, 50.0)
	stamp.SetFillColor(color.DarkGreen).DrawText(params)

	// Complete the stamp
	stamp.Complete()

	// Draw stamp on page at original location
	stamp.SetLocation(50.0, 50.0).DrawOn(page)

	// Rotate the stamp clockwise and draw it again
	stamp.Rotate(15).DrawOn(page)

	// Rotate the stamp clockwise and draw it again
	stamp.Rotate(-15).DrawOn(page)

	// Add a text line to container
	title := pdfjet.NewTextLine(f1, "Container")
	title.SetLocation(150.0, 20.0)
	container.Add(title)

	// Nested container #1
	nested1 := pdfjet.NewContainer(200.0, 200.0)
	nested1.SetLocation(0.0, 0.0)
	nested1.SetRotationCounterClockwise(30)
	nested1.SetScaleFactor(0.8)

	innerRect := pdfjet.NewRect(0.0, 0.0, 200.0, 200.0)
	innerRect.SetFillColor(color.Blue)
	nested1.Add(innerRect)

	innerText := pdfjet.NewTextLine(f1, "Nested 1")
	innerText.SetLocation(50.0, 100.0)
	nested1.Add(innerText)

	container.Add(nested1)

	// Nested container #2
	nested2 := pdfjet.NewContainer(100.0, 100.0)
	nested2.SetLocation(250.0, 250.0)
	nested2.SetRotationCounterClockwise(45)

	smallRect := pdfjet.NewRect(0.0, 0.0, 100.0, 100.0)
	smallRect.SetFillColor(color.Red)
	nested2.Add(smallRect)

	smallText := pdfjet.NewTextLine(f1, "Nested 2")
	smallText.SetLocation(10.0, 50.0)
	nested2.Add(smallText)

	container.Add(nested2)

	container.SetRotationClockwise(45)
	// Draw the entire hierarchy on the page
	container.DrawOn(page)

	// Additional container demonstration
	container5 := pdfjet.NewContainer(200.0, 20.0)
	rect5 := pdfjet.NewRect(0.0, 0.0, 200.0, 20.0)
	container5.Add(rect5)

	rect6 := pdfjet.NewRect(0.0, 0.0, 10.0, 10.0)
	rect6.SetFillColor(color.Blue)
	container5.Add(rect6)

	rect7 := pdfjet.NewRect(190.0, 10.0, 10.0, 10.0)
	rect7.SetBorderColor(color.Red)
	rect7.SetBorderWidth(2.0)
	container5.Add(rect7)

	container5.SetLocation(50.0, 600.0)
	container5.DrawOn(page)

	container5.SetRotation(-90)
	container5.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example35()
	pdfjet.PrintDuration("Example_35", time.Since(start))
}
