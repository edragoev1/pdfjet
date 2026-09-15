package main

import (
	"fmt"
	"log"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/a4"
)

// Example33 draws SVG images on the page.
func Example33() error {
	pdf, err := pdfjet.NewPDFFile("Example_33.pdf")
	if err != nil {
		log.Fatal(err)
	}

	page := pdfjet.NewPage(pdf, a4.Portrait())

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

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}

	return nil
}

func main() {
	time0 := time.Now().UnixMilli()
	err := Example33()
	if err != nil {
		log.Fatal(err)
	}
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_33 => %4d ms\n", time1-time0)
}
