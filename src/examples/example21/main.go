package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/qrcode"
)

// Example21 uses QR code 2D barcodes.
func Example21() {
	pdf := pdfjet.NewPDFFile("Example_21.pdf")

	f1 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())

	page := pdfjet.NewPage(pdf, letter.Portrait)

	text := pdfjet.NewTextLine(f1,
		"QR codes encoded with Low, Medium, High and Very High error correction level")
	text.SetLocation(100.0, 30.0)
	text.DrawOn(page)

	// Please note:
	// The higher the error correction level - the shorter the string that you can encode.
	qr := qrcode.NewQRCode(
		"https://kazuhikoarase.github.io/qrcode-generator/js/demo",
		qrcode.ErrorCorrectLevelL) // Low
	qr.SetModuleLength(3.0)
	qr.SetLocation(100.0, 100.0)
	// qr.SetColor(color.Blue)
	qr.DrawOn(page)

	qr = qrcode.NewQRCode(
		"https://github.com/kazuhikoarase/qrcode-generator",
		qrcode.ErrorCorrectLevelM) // Medium
	qr.SetLocation(400.0, 100.0)
	qr.SetModuleLength(3.0)
	qr.DrawOn(page)

	qr = qrcode.NewQRCode(
		"https://github.com/kazuhikoarase/jaconv",
		qrcode.ErrorCorrectLevelQ) // High
	qr.SetLocation(100.0, 400.0)
	qr.SetModuleLength(3.0)
	qr.DrawOn(page)

	qr = qrcode.NewQRCode(
		"https://github.com/kazuhikoarase",
		qrcode.ErrorCorrectLevelH) // Very High
	qr.SetLocation(400.0, 400.0)
	qr.SetModuleLength(3.0)
	qr.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example21()
	pdfjet.PrintDuration("Example_21", time.Since(start))
}
