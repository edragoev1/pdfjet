package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/compress"
	"github.com/edragoev1/pdfjet/src/corefont"
	"github.com/edragoev1/pdfjet/src/encryption"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example30 -- TODO:
func Example30() {
	pdf := pdfjet.NewPDFFile("Example_30.pdf")

	passwords := encryption.NewPasswords()
	passwords.SetPasswords("hello", "world")

	permissions := encryption.NewPermissions()
	permissions.SetPermissions(
		encryption.Print| // Set both to allow the user to print
			encryption.PrintHighQuality| // this document with high quality
			// encryption.ModifyContents|
			// encryption.CopyContents|
			encryption.AssembleDocument, true)

	pdf.SetEncryption(pdfjet.NewEncryption(pdf, passwords, permissions))

	f1 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())
	// f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f1.SetSize(36.0)

	image := pdfjet.NewImageFromFile(pdf, "images/ee-map.png")

	file1 := pdfjet.NewEmbeddedFileAtPath(pdf, "images/linux-logo.png", compress.No)

	page := pdfjet.NewPage(pdf, letter.Portrait)

	textLine := pdfjet.NewTextLine(f1, "Hello, World!")
	textLine.SetLocation(100.0, 100.0)
	textLine.DrawOn(page)

	image.SetLocation(100, 150)
	image.ScaleBy(0.5)
	image.DrawOn(page)

	// File attachment functionality
	attachment := pdfjet.NewFileAttachment(pdf, file1)
	attachment.SetLocation(100.0, 550.0)
	attachment.SetIconPushPin()
	attachment.SetIconSize(24.0)
	attachment.SetTitle("Attached File: " + file1.GetFileName())
	attachment.SetDescription(
		"Right mouse click on the icon to save the attached file.")
	attachment.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example30()
	pdfjet.PrintDuration("Example_46", time.Since(start))
}
