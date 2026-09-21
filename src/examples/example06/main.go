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

// Example06 attaches two files to a page, and adds a note, a link and
// polygon, square and circle annotations next to labels that describe them.
func Example06() {
	pdf, err := pdfjet.NewPDFFile("Example_06.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Attachments and Annotations")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f1.SetSize(12.0)

	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f2.SetSize(14.0)

	file1 := pdfjet.NewEmbeddedFileAtPath(pdf, "images/linux-logo.png", false)
	file2 := pdfjet.NewEmbeddedFileAtPath(pdf, "src/examples/example02/main.go", true)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	text := pdfjet.NewTextLine(f2, "Attachments and annotations")
	text.SetStructureType(structelem.H1)
	text.SetFontSize(22.0)
	text.SetLocation(70.0, 80.0)
	text.DrawOn(page)

	text = pdfjet.NewTextLine(f1,
		"Open this page in a PDF viewer that shows annotations, and hover over the icons.")
	text.SetTextColor(color.Gray)
	text.SetLocation(70.0, 105.0)
	text.DrawOn(page)

	// File attachments. The files are stored inside the PDF.
	pdfjet.NewTextLine(f2, "Attached files").SetStructureType(structelem.H2).SetLocation(70.0, 160.0).DrawOn(page)

	attachment := pdfjet.NewFileAttachment(file1)
	attachment.SetLocation(70.0, 175.0)
	attachment.SetIconPushPin()
	attachment.SetTitle("Attached File: " + file1.GetFileName())
	attachment.SetContents(
		"Right mouse click on the icon to save the attached file.")
	attachment.DrawOn(page)
	pdfjet.NewTextLine(f1, "linux-logo.png, an image, with a push pin icon").
		SetLocation(105.0, 192.0).DrawOn(page)

	attachment = pdfjet.NewFileAttachment(file2)
	attachment.SetLocation(70.0, 210.0)
	attachment.SetIconPaperclip()
	attachment.SetTitle("Attached File: " + file2.GetFileName())
	attachment.SetContents(
		"Right mouse click on the icon to save the attached file.")
	attachment.DrawOn(page)
	pdfjet.NewTextLine(f1, "The source code of Example_02, with a paperclip icon").
		SetLocation(105.0, 227.0).DrawOn(page)

	// A note, and a link.
	pdfjet.NewTextLine(f2, "A note and a link").SetStructureType(structelem.H2).SetLocation(70.0, 290.0).DrawOn(page)

	textAnnotation := pdfjet.NewTextAnnotation()
	textAnnotation.SetLocation(70.0, 305.0)
	textAnnotation.SetSize(24.0, 24.0)
	textAnnotation.SetTitle("Reviewer")
	textAnnotation.SetContents("Please check the figures on page 2.")
	textAnnotation.DrawOn(page)
	pdfjet.NewTextLine(f1, "A text annotation: click the note icon to read it").
		SetLocation(105.0, 322.0).DrawOn(page)

	text = pdfjet.NewTextLine(f1, "Visit https://pdfjet.com")
	text.SetTextColor(color.Blue)
	text.SetUnderline(true)
	text.SetURIAction("https://pdfjet.com")
	text.SetLocation(105.0, 357.0)
	text.DrawOn(page)

	// Shape annotations, drawn half transparent over the page.
	pdfjet.NewTextLine(f2, "Shape annotations").SetLocation(70.0, 420.0).DrawOn(page)

	polygonAnnotation := pdfjet.NewPolygonAnnotation()
	polygonAnnotation.SetLocation(70.0, 440.0)
	polygonAnnotation.SetVertices([]float32{0.0, 60.0, 30.0, 0.0, 60.0, 60.0, 0.0, 60.0})
	polygonAnnotation.SetFillColor(color.Red)
	polygonAnnotation.SetOpacity(0.5)
	polygonAnnotation.SetTitle("Polygon")
	polygonAnnotation.SetContents("Polygon Annotation")
	polygonAnnotation.DrawOn(page)

	squareAnnotation := pdfjet.NewSquareAnnotation()
	squareAnnotation.SetLocation(170.0, 440.0)
	squareAnnotation.SetSize(60.0, 60.0)
	squareAnnotation.SetFillColorRGB([3]float32{0.0, 0.5, 0.0})
	squareAnnotation.SetOpacity(0.5)
	squareAnnotation.SetTitle("Square")
	squareAnnotation.SetContents("Square Annotation")
	squareAnnotation.DrawOn(page)

	circleAnnotation := pdfjet.NewCircleAnnotation()
	circleAnnotation.SetLocation(270.0, 440.0)
	circleAnnotation.SetSize(60.0, 60.0)
	circleAnnotation.SetFillColorRGB([3]float32{0.0, 0.0, 1.0})
	circleAnnotation.SetOpacity(0.5)
	circleAnnotation.SetTitle("Circle")
	circleAnnotation.SetContents("Circle Annotation")
	circleAnnotation.DrawOn(page)

	pdfjet.NewTextLine(f1, "Polygon").SetLocation(78.0, 520.0).DrawOn(page)
	pdfjet.NewTextLine(f1, "Square").SetLocation(180.0, 520.0).DrawOn(page)
	pdfjet.NewTextLine(f1, "Circle").SetLocation(283.0, 520.0).DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example06()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_06 => %4d ms\n", time1-time0)
}
