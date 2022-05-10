package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example11 tests the one dimenstional barcodes.
func Example11() {
	f, err := os.Create("Example_11.pdf")
	if err != nil {
		log.Fatal(err)
	}
	defer f.Close()

	w := bufio.NewWriter(f)

	pdf := pdfjet.NewPDF(w, compliance.PDF15)

	file, err := os.Open("fonts/OpenSans/OpenSans-Regular.ttf.stream")
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	reader := bufio.NewReader(file)
	f1 := pdfjet.NewFontStream1(pdf, reader)

	page := pdfjet.NewPageAddTo(pdf, letter.Portrait)

	barcode := pdfjet.NewBarCode(pdfjet.CODE128, "Hellö, World!")
	barcode.SetLocation(170.0, 70.0)
	barcode.SetModuleLength(0.75)
	barcode.SetFont(f1)
	barcode.DrawOn(page)

	barcode = pdfjet.NewBarCode(pdfjet.CODE128, "G86513JVW0C")
	barcode.SetLocation(170.0, 170.0)
	barcode.SetModuleLength(0.75)
	barcode.SetDirection(pdfjet.TopToBottom)
	barcode.SetFont(f1)
	barcode.DrawOn(page)

	barcode = pdfjet.NewBarCode(pdfjet.CODE39, "WIKIPEDIA")
	barcode.SetLocation(270.0, 370.0)
	barcode.SetModuleLength(0.75)
	barcode.SetFont(f1)
	barcode.DrawOn(page)

	barcode = pdfjet.NewBarCode(pdfjet.CODE39, "CODE39")
	barcode.SetLocation(400.0, 70.0)
	barcode.SetModuleLength(0.75)
	barcode.SetDirection(pdfjet.TopToBottom)
	barcode.SetFont(f1)
	barcode.DrawOn(page)

	barcode = pdfjet.NewBarCode(pdfjet.CODE39, "CODE39")
	barcode.SetLocation(450.0, 70.0)
	barcode.SetModuleLength(0.75)
	barcode.SetDirection(pdfjet.BottomToTop)
	barcode.SetFont(f1)
	barcode.DrawOn(page)

	barcode = pdfjet.NewBarCode(pdfjet.Upc, "712345678904")
	barcode.SetLocation(450.0, 270.0)
	barcode.SetModuleLength(0.75)
	barcode.SetDirection(pdfjet.TopToBottom)
	barcode.SetFont(f1)
	barcode.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example11()
	elapsed := time.Since(start)
	fmt.Printf("Example_11 => %dµs\n", elapsed.Microseconds())
}
