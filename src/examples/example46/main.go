package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/corefont"
	"github.com/edragoev1/pdfjet/src/encryption"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example46 -- TODO:
func Example46() {
	pdf := pdfjet.NewPDFFile("Example_46.pdf")

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
	// Test OTF with CFF outlines!
	// f1 := NewFont(pdf, "data/SourceSansPro-Regular.otf")
	f1.SetSize(36.0)

	page := pdfjet.NewPage(pdf, letter.Portrait)

	textLine := pdfjet.NewTextLine(f1, "Hello, World!")
	textLine.SetLocation(100.0, 100.0)
	textLine.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example46()
	pdfjet.PrintDuration("Example_46", time.Since(start))
}
