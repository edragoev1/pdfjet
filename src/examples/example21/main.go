package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/corefont"
	"github.com/edragoev1/pdfjet/src/dproject"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example21 -- TODO:
func Example21() {
	file, err := os.Create("Example_21.pdf")
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	w := bufio.NewWriter(file)

	pdf := pdfjet.NewPDF(w, compliance.PDF15)

	font := pdfjet.NewCoreFont(pdf, corefont.Helvetica())

	page := pdfjet.NewPageAddTo(pdf, letter.Portrait)

	textLine := pdfjet.NewTextLine(font,
		"QR codes encoded with Low, Medium, High and Very High error correction level - Go")
	textLine.SetLocation(100.0, 30.0)
	textLine.DrawOn(page)

	// Please note:
	// The higher the error correction level - the shorter the string that you can encode.
	qr := dproject.NewQRCode(
		"https://kazuhikoarase.github.io/qrcode-generator/js/demo",
		dproject.ErrorCorrectLevelL) // Low
	qr.SetModuleLength(3.0)
	qr.SetLocation(100.0, 100.0)
	// qr.SetColor(color.Blue)
	qr.DrawOn(page)

	qr = dproject.NewQRCode(
		"https://github.com/kazuhikoarase/qrcode-generator",
		dproject.ErrorCorrectLevelM) // Medium
	qr.SetLocation(400.0, 100.0)
	qr.SetModuleLength(3.0)
	qr.DrawOn(page)

	qr = dproject.NewQRCode(
		"https://github.com/kazuhikoarase/jaconv",
		dproject.ErrorCorrectLevelQ) // High
	qr.SetLocation(100.0, 400.0)
	qr.SetModuleLength(3.0)
	qr.DrawOn(page)

	qr = dproject.NewQRCode(
		"https://github.com/kazuhikoarase",
		dproject.ErrorCorrectLevelH) // Very High
	qr.SetLocation(400.0, 400.0)
	qr.SetModuleLength(3.0)
	qr.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example21()
	elapsed := time.Since(start)
	fmt.Printf("Example_21 => %dµs\n", elapsed.Microseconds())
}
