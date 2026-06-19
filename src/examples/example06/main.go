package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/compress"
	"github.com/edragoev1/pdfjet/src/letter"
)

func Example06() {
	pdf := pdfjet.NewPDFFile("Example_06.pdf")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)

	file1 := pdfjet.NewEmbeddedFileAtPath(pdf, "images/linux-logo.png", compress.No)
	file2 := pdfjet.NewEmbeddedFileAtPath(pdf, "examples/Example_02/Example_02.cs", compress.Yes)

	page := pdfjet.NewPage(pdf, letter.Portrait)

	// File attachment functionality
	attachment := pdfjet.NewFileAttachment(pdf, file1)
	attachment.SetLocation(100.0, 600.0)
	attachment.SetIconPushPin()
	attachment.SetTitle("Attached File: " + file1.GetFileName())
	attachment.SetDescription(
		"Right mouse click on the icon to save the attached file.")
	attachment.DrawOn(page)

	attachment = pdfjet.NewFileAttachment(pdf, file2)
	attachment.SetLocation(200.0, 600.0)
	attachment.SetIconPaperclip()
	attachment.SetTitle("Attached File: " + file2.GetFileName())
	attachment.SetDescription(
		"Right mouse click on the icon to save the attached file.")
	attachment.DrawOn(page)

	textLine := pdfjet.NewTextLine(f1, "pdfjet.com")
	textLine.SetLocation(300.0, 618.0)
	textLine.SetURIAction("https://pdfjet.com")
	textLine.DrawOn(page)

	textAnnotation := pdfjet.NewTextAnnotation()
	textAnnotation.SetLocation(400.0, 600.0)
	textAnnotation.SetSize(25.0, 25.0)
	textAnnotation.SetTitle("Hello")
	textAnnotation.SetContents("World")
	textAnnotation.DrawOn(page)

	container := pdfjet.NewContainer(400.0, 400.0)
	container.SetLocation(100.0, 100.0)
	container.SetBorderColor(color.Black)
	container.SetRotationClockwise(90)

	rect := pdfjet.NewRect(0.0, 0.0, 25.0, 25.0)
	rect.SetBorderColor(color.Black)
	rect.SetBorderWidth(1.0)
	container.Add(rect)

	polygonAnnotation := pdfjet.NewPolygonAnnotation()
	polygonAnnotation.SetLocation(0.0, 0.0)
	polygonAnnotation.SetVertices([]float32{0.0, 0.0, 50.0, 0.0, 0.0, 50.0, 0.0, 0.0})
	polygonAnnotation.SetFillColor([3]float32{1.0, 0.0, 0.0}) // Red color
	polygonAnnotation.SetTransparency(0.5)
	polygonAnnotation.SetTitle("This is a test ...")
	polygonAnnotation.SetContents("The quick brown cat caught the lazy mouse.")
	container.Add(polygonAnnotation)

	squareAnnotation := pdfjet.NewSquareAnnotation()
	squareAnnotation.SetLocation(25.0, 0.0)
	squareAnnotation.SetSize(50.0, 50.0)
	squareAnnotation.SetFillColor([3]float32{0.0, 0.0, 1.0}) // Blue color
	squareAnnotation.SetTransparency(0.5)
	squareAnnotation.SetTitle("Hello, World!")
	squareAnnotation.SetContents("The quick brown fox jumps over the lazy dog.")
	container.Add(squareAnnotation)

	circleAnnotation := pdfjet.NewCircleAnnotation()
	circleAnnotation.SetLocation(50.0, 0.0)
	circleAnnotation.SetSize(50.0, 50.0)
	circleAnnotation.SetFillColor([3]float32{0.0, 0.0, 1.0}) // Blue color
	circleAnnotation.SetTransparency(0.5)
	circleAnnotation.SetTitle("Circle")
	circleAnnotation.SetContents("Annotation")
	container.Add(circleAnnotation)

	container.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example06()
	pdfjet.PrintDuration("Example_06", time.Since(start))
}
