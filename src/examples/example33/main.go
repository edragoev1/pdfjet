package main

import (
	"log"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/a4"
)

func Example33() error {
	pdf := pdfjet.NewPDFFile("Example_33.pdf")

	page := pdfjet.NewPage(pdf, a4.Portrait)

	image, err := pdfjet.NewSVGImageFromFile("images/svg-test/europe.svg")
	if err != nil {
		return err
	}
	image.SetLocation(-150.0, 0.0)
	xy := image.DrawOn(page)

	image, err = pdfjet.NewSVGImageFromFile("images/svg/shopping_cart_checkout_FILL0_wght400_GRAD0_opsz48.svg")
	if err != nil {
		return err
	}
	image.SetLocation(20.0, 670.0)
	xy = image.DrawOn(page)

	image, err = pdfjet.NewSVGImageFromFile("images/svg/add_circle_FILL0_wght400_GRAD0_opsz48.svg")
	if err != nil {
		return err
	}
	image.SetLocation(xy[0], 670.0)
	xy = image.DrawOn(page)

	image, err = pdfjet.NewSVGImageFromFile("images/svg/palette_FILL0_wght400_GRAD0_opsz48.svg")
	if err != nil {
		return err
	}
	image.SetLocation(xy[0], 670.0)
	xy = image.DrawOn(page)

	image, err = pdfjet.NewSVGImageFromFile("images/svg/auto_stories_FILL0_wght400_GRAD0_opsz48.svg")
	if err != nil {
		return err
	}
	image.SetLocation(xy[0], 670.0)
	xy = image.DrawOn(page)

	image, err = pdfjet.NewSVGImageFromFile("images/svg/star_FILL0_wght400_GRAD0_opsz48.svg")
	if err != nil {
		return err
	}
	image.SetLocation(xy[0], 670.0)
	xy = image.DrawOn(page)

	image, err = pdfjet.NewSVGImageFromFile("images/svg-test/test-CS.svg")
	if err != nil {
		return err
	}
	image.SetLocation(xy[0], 670.0)
	xy = image.DrawOn(page)

	image, err = pdfjet.NewSVGImageFromFile("images/svg-test/test-QQ1.svg")
	if err != nil {
		return err
	}
	image.SetLocation(xy[0], 670.0)
	xy = image.DrawOn(page)

	image, err = pdfjet.NewSVGImageFromFile("images/svg-test/menu-icon.svg")
	if err != nil {
		return err
	}
	image.SetLocation(xy[0], 670.0)
	xy = image.DrawOn(page)

	image, err = pdfjet.NewSVGImageFromFile("images/svg-test/menu-icon-close.svg")
	if err != nil {
		return err
	}
	image.SetLocation(xy[0], 670.0)
	image.DrawOn(page)

	pdf.Complete()

	return nil
}

func main() {
	start := time.Now()
	err := Example33()
	if err != nil {
		log.Fatal(err)
	}
	pdfjet.PrintDuration("Example_33", time.Since(start))
}
