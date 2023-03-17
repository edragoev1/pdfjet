package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/a4"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/imagetype"
)

// Example33 -- TODO:
func Example33() {
	file, err := os.Create("Example_33.pdf")
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	w := bufio.NewWriter(file)

	pdf := pdfjet.NewPDF(w, compliance.PDF15)

	file1, err := os.Open("images/photoshop.jpg")
	if err != nil {
		log.Fatal(err)
	}
	defer file1.Close()
	reader := bufio.NewReader(file1)
	image1 := pdfjet.NewImage(pdf, reader, imagetype.JPG)

	page := pdfjet.NewPageAddTo(pdf, a4.Portrait)

	image1.SetLocation(10.0, 10.0)
	image1.ScaleBy(0.25)
	image1.DrawOn(page)

	// SVG test
	file2, err := os.Open("images/svg-test/test-CC.svg")
	if err != nil {
		log.Fatal(err)
	}
	defer file2.Close()
	reader2 := bufio.NewReader(file2)
	image2 := pdfjet.NewSVGImage(reader2)
	image2.SetLocation(20.0, 670.0)
	xy := image2.DrawOn(page)

	file2, err = os.Open("images/svg-test/test-QQ.svg")
	if err != nil {
		log.Fatal(err)
	}
	defer file2.Close()
	reader2 = bufio.NewReader(file2)
	image2 = pdfjet.NewSVGImage(reader2)
	image2.SetLocation(xy[0], 670.0)
	xy = image2.DrawOn(page)

	file2, err = os.Open("images/svg-test/test-qt.svg")
	if err != nil {
		log.Fatal(err)
	}
	defer file2.Close()
	reader2 = bufio.NewReader(file2)
	image2 = pdfjet.NewSVGImage(reader2)
	image2.SetLocation(xy[0], 670.0)
	xy = image2.DrawOn(page)

	file2, err = os.Open("images/svg-test/test-qT.svg")
	if err != nil {
		log.Fatal(err)
	}
	defer file2.Close()
	reader2 = bufio.NewReader(file2)
	image2 = pdfjet.NewSVGImage(reader2)
	image2.SetLocation(xy[0], 670.0)
	xy = image2.DrawOn(page)

	file2, err = os.Open("images/svg-test/test-QT.svg")
	if err != nil {
		log.Fatal(err)
	}
	defer file2.Close()
	reader2 = bufio.NewReader(file2)
	image2 = pdfjet.NewSVGImage(reader2)
	image2.SetLocation(xy[0], 670.0)
	xy = image2.DrawOn(page)

	file2, err = os.Open("images/svg-test/test-qq.svg")
	if err != nil {
		log.Fatal(err)
	}
	defer file2.Close()
	reader2 = bufio.NewReader(file2)
	image2 = pdfjet.NewSVGImage(reader2)
	image2.SetLocation(xy[0], 670.0)
	xy = image2.DrawOn(page)

	file2, err = os.Open("images/svg/palette_FILL0_wght400_GRAD0_opsz48.svg")
	if err != nil {
		log.Fatal(err)
	}
	defer file2.Close()
	reader2 = bufio.NewReader(file2)
	image2 = pdfjet.NewSVGImage(reader2)
	image2.SetLocation(xy[0], 670.0)
	xy = image2.DrawOn(page)

	file2, err = os.Open("images/svg/shopping_cart_checkout_FILL0_wght400_GRAD0_opsz48.svg")
	if err != nil {
		log.Fatal(err)
	}
	defer file2.Close()
	reader2 = bufio.NewReader(file2)
	image2 = pdfjet.NewSVGImage(reader2)
	image2.SetLocation(xy[0], 670.0)
	xy = image2.DrawOn(page)

	file2, err = os.Open("images/svg-test/test-CS.svg")
	if err != nil {
		log.Fatal(err)
	}
	defer file2.Close()
	reader2 = bufio.NewReader(file2)
	image2 = pdfjet.NewSVGImage(reader2)
	image2.SetLocation(xy[0], 670.0)
	xy = image2.DrawOn(page)

	file2, err = os.Open("images/svg-test/test-QQ.svg")
	if err != nil {
		log.Fatal(err)
	}
	defer file2.Close()
	reader2 = bufio.NewReader(file2)
	image2 = pdfjet.NewSVGImage(reader2)
	image2.SetLocation(xy[0], 670.0)
	image2.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example33()
	elapsed := time.Since(start)
	fmt.Printf("Example_33 => %dµs\n", elapsed.Microseconds())
}
