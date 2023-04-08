package main

import (
	"fmt"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/a4"
	"github.com/edragoev1/pdfjet/src/compliance"
)

// Example33 -- TODO:
func Example33() {
	pdf := pdfjet.NewPDFFile("Example_33.pdf", compliance.PDF15)
	image1 := pdfjet.NewImageFromFile(pdf, "images/photoshop.jpg")

	page := pdfjet.NewPage(pdf, a4.Portrait)

	image1.SetLocation(10.0, 10.0)
	image1.ScaleBy(0.25)
	image1.DrawOn(page)

	// SVG test
	image2 := pdfjet.NewSVGImageFromFile("images/svg-test/test-CC.svg")
	image2.SetLocation(20.0, 670.0)
	xy := image2.DrawOn(page)

	image2 = pdfjet.NewSVGImageFromFile("images/svg-test/test-QQ.svg")
	image2.SetLocation(xy[0], 670.0)
	xy = image2.DrawOn(page)

	image2 = pdfjet.NewSVGImageFromFile("images/svg-test/test-qt.svg")
	image2.SetLocation(xy[0], 670.0)
	xy = image2.DrawOn(page)

	image2 = pdfjet.NewSVGImageFromFile("images/svg-test/test-qT.svg")
	image2.SetLocation(xy[0], 670.0)
	xy = image2.DrawOn(page)

	image2 = pdfjet.NewSVGImageFromFile("images/svg-test/test-QT.svg")
	image2.SetLocation(xy[0], 670.0)
	xy = image2.DrawOn(page)

	image2 = pdfjet.NewSVGImageFromFile("images/svg-test/test-qq.svg")
	image2.SetLocation(xy[0], 670.0)
	xy = image2.DrawOn(page)

	image2 = pdfjet.NewSVGImageFromFile("images/svg/palette_FILL0_wght400_GRAD0_opsz48.svg")
	image2.SetLocation(xy[0], 670.0)
	xy = image2.DrawOn(page)

	image2 = pdfjet.NewSVGImageFromFile(
		"images/svg/shopping_cart_checkout_FILL0_wght400_GRAD0_opsz48.svg")
	image2.SetLocation(xy[0], 670.0)
	xy = image2.DrawOn(page)

	image2 = pdfjet.NewSVGImageFromFile("images/svg-test/test-CS.svg")
	image2.SetLocation(xy[0], 670.0)
	xy = image2.DrawOn(page)

	image2 = pdfjet.NewSVGImageFromFile("images/svg-test/menu-icon.svg")
	image2.SetLocation(xy[0], 670.0)
	xy = image2.DrawOn(page)

	image2 = pdfjet.NewSVGImageFromFile("images/svg-test/menu-icon-close.svg")
	image2.SetLocation(xy[0], 670.0)
	image2.ScaleBy(2.0)
	image2.DrawOn(page)

	image2 = pdfjet.NewSVGImageFromFile("images/svg-test/europe.svg")
	image2.SetLocation(0.0, 0.0)
	image2.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example33()
	elapsed := time.Since(start)
	fmt.Printf("Example_33 => %dµs\n", elapsed.Microseconds())
}
