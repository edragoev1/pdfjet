package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/compress"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example06
func Example06() {
	pdf := pdfjet.NewPDFFile("Example_06.pdf", compliance.PDF15)
	pdf.SetTitle("Hello")
	pdf.SetAuthor("World")
	pdf.SetSubject("This is a test")
	pdf.SetKeywords("Hello World This is a test")
	pdf.SetCreator("Application Name")

	embeddedFile1 := pdfjet.NewEmbeddedFileAtPath(pdf, "images/linux-logo.png", compress.No)
	embeddedFile2 := pdfjet.NewEmbeddedFileAtPath(pdf, "examples/Example_06.java", compress.Yes)

	page := pdfjet.NewPage(pdf, letter.Portrait)

	attachment := pdfjet.NewFileAttachment(pdf, embeddedFile1)
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
