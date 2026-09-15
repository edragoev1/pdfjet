package main

import (
	"fmt"
	"log"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/errorcorrectionlevel"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/qrcode"
)

// Example21 uses QR code 2D barcodes.
func Example21() {
	pdf, err := pdfjet.NewPDFFile("Example_21.pdf")
	if err != nil {
		log.Fatal(err)
	}

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	text := pdfjet.NewTextLine(f1,
		"QR codes encoded with Low, Medium, High and Very High error correction level")
	text.SetLocation(100.0, 30.0)
	text.DrawOn(page)

	// Please note:
	// The higher the error correction level - the shorter the string that you can encode.
	qr := qrcode.NewQRCode(
		"https://kazuhikoarase.github.io/qrcode-generator/js/demo",
		errorcorrectionlevel.L) // Low
	qr.SetModuleLength(3.0)
	qr.SetLocation(100.0, 100.0)
	// qr.SetModuleColor(color.Blue)
	qr.DrawOn(page)

	qr = qrcode.NewQRCode(
		"https://github.com/kazuhikoarase/qrcode-generator",
		errorcorrectionlevel.M) // Medium
	qr.SetLocation(400.0, 100.0)
	qr.SetModuleLength(3.0)
	qr.DrawOn(page)

	qr = qrcode.NewQRCode(
		"https://github.com/kazuhikoarase/jaconv",
		errorcorrectionlevel.Q) // High
	qr.SetLocation(100.0, 400.0)
	qr.SetModuleLength(3.0)
	qr.DrawOn(page)

	qr = qrcode.NewQRCode(
		"https://github.com/kazuhikoarase",
		errorcorrectionlevel.H) // Very High
	qr.SetLocation(400.0, 400.0)
	qr.SetModuleLength(3.0)
	qr.DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example21()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_21 => %4d ms\n", time1-time0)
}
