package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/IBMPlexSans"
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
	// attachment.SetIconSize(25.0)
	attachment.SetTitle("Attached File: " + file1.GetFileName())
	attachment.SetDescription(
		"Right mouse click on the icon to save the attached file.")
	attachment.DrawOn(page)

	attachment = pdfjet.NewFileAttachment(pdf, file2)
	attachment.SetLocation(200.0, 600.0)
	attachment.SetIconPaperclip()
	// attachment.SetIconSize(25.0)
	attachment.SetTitle("Attached File: " + file2.GetFileName())
	attachment.SetDescription(
		"Right mouse click on the icon to save the attached file.")
	attachment.DrawOn(page)

	textLine := pdfjet.NewTextLine(f1, "pdfjet.com")
	textLine.SetLocation(300.0, 618.0)
	textLine.SetURIAction("https://pdfjet.com")
	textLine.DrawOn(page)

	// textAnnotation := new(pdfjet.Annotation)
	//textAnnotation.SetLocation(400.0, 600.0)
	//textAnnotation.SetSize(25.0, 25.0)
	//textAnnotation.SetTitle("Hello")
	//textAnnotation.SetContents("World")
	//textAnnotation.DrawOn(page)

	container := pdfjet.NewContainer(400.0, 400.0)
	container.SetLocation(100.0, 100.0)
	container.AddBorder()
	// container.Rotate(-90)
	// container.Rotate(-180)

	rect := new(pdfjet.Rect)
	rect.SetSize(25.0, 25.0)
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

	//CircleAnnotation circleAnnotation = new CircleAnnotation()
	//circleAnnotation.SetLocation(50f, 0f)
	//circleAnnotation.SetSize(50f, 50f)
	//circleAnnotation.SetFillColor(new float[] {0f, 0f, 1f})
	//circleAnnotation.SetTransparency(0.5f)
	//circleAnnotation.SetTitle("Circle")
	//circleAnnotation.SetContents("Annotation")
	//container.Add(circleAnnotation)

	container.DrawOn(page)

	embeddedFile1 := pdfjet.NewEmbeddedFileAtPath(pdf, "images/linux-logo.png", compress.No)
	embeddedFile2 := pdfjet.NewEmbeddedFileAtPath(pdf, "examples/Example_06.java", compress.Yes)

	page = pdfjet.NewPage(pdf, letter.Portrait)

	attachment = pdfjet.NewFileAttachment(pdf, embeddedFile1)
	attachment.SetLocation(100.0, 300.0)
	attachment.SetIconPushPin()
	attachment.SetIconSize(24.0)
	attachment.SetTitle("Attached File: " + embeddedFile1.GetFileName())
	attachment.SetDescription(
		"Right mouse click on the icon to save the attached file.")
	attachment.DrawOn(page)

	attachment = pdfjet.NewFileAttachment(pdf, embeddedFile2)
	attachment.SetLocation(200.0, 300.0)
	attachment.SetIconPaperclip()
	attachment.SetIconSize(24.0)
	attachment.SetTitle("Attached File: " + embeddedFile2.GetFileName())
	attachment.SetDescription(
		"Right mouse click on the icon to save the attached file.")
	attachment.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example06()
	pdfjet.PrintDuration("Example_06", time.Since(start))
}
