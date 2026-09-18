// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/a4"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
)

// Example33 uses the SVGImage component to draw a map of Europe, scaled to
// fit the page, and a set of icons, each labeled with its name.
func Example33() error {
	pdf, err := pdfjet.NewPDFFile("Example_33.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("SVG Images")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)

	page := pdfjet.NewPage(pdf, a4.Portrait())

	text := pdfjet.NewTextLine(f2, "SVG Images")
	text.SetFontSize(22.0)
	text.SetLocation(50.0, 80.0)
	text.DrawOn(page)

	textBlock := pdfjet.NewTextBlock(f1,
		"SVGImage reads the paths of an SVG file and draws them as vector "+
			"graphics, which stay sharp at any zoom. The map is 1,000 points wide "+
			"in its file and is scaled by 0.5 to fit the page.")
	textBlock.SetFontSize(12.0)
	textBlock.SetLineSpacing(1.5)
	textBlock.SetLocation(50.0, 95.0)
	textBlock.SetWidth(495.0)
	xy := textBlock.DrawOn(page)

	svgMap, err := pdfjet.NewSVGImageFromFile("images/svg-test/europe.svg")
	if err != nil {
		return err
	}
	svgMap.ScaleBy(0.5)
	svgMap.SetLocation((page.GetWidth()-svgMap.GetWidth())/2.0, xy[1]+20.0)
	xy = svgMap.DrawOn(page)

	textBlock = pdfjet.NewTextBlock(f1,
		"The colors come from the file: the peachpuff fill of the svg element "+
			"for most countries, an aliceblue fill for Spain and an olive "+
			"outline for Austria.")
	textBlock.SetFontSize(10.0)
	textBlock.SetTextColor(color.Gray)
	textBlock.SetLocation(50.0, xy[1]+10.0)
	textBlock.SetWidth(495.0)
	xy = textBlock.DrawOn(page)

	iconFiles := []string{
		"images/svg/home_FILL0_wght400_GRAD0_opsz48.svg",
		"images/svg/search_FILL0_wght400_GRAD0_opsz48.svg",
		"images/svg/shopping_cart_checkout_FILL0_wght400_GRAD0_opsz48.svg",
		"images/svg/palette_FILL0_wght400_GRAD0_opsz48.svg",
		"images/svg/auto_stories_FILL0_wght400_GRAD0_opsz48.svg",
		"images/svg/add_circle_FILL0_wght400_GRAD0_opsz48.svg",
		"images/svg/star_FILL0_wght400_GRAD0_opsz48.svg",
		"images/svg/settings_FILL0_wght400_GRAD0_opsz48.svg",
		"images/svg-test/menu-icon.svg",
		"images/svg-test/menu-icon-close.svg",
		"images/svg-test/test-CS.svg",
		"images/svg-test/test-QQ1.svg",
	}
	iconNames := []string{
		"home",
		"search",
		"checkout",
		"palette",
		"stories",
		"add",
		"star",
		"settings",
		"menu",
		"close",
		"C and S curves",
		"Q curves",
	}

	// Two rows of six icons, 48 by 48 points each.
	y := xy[1] + 30.0
	for i := 0; i < len(iconFiles); i++ {
		x := 50.0 + float32(i%6)*85.0
		yIcon := y + float32(i/6)*90.0

		icon, err := pdfjet.NewSVGImageFromFile(iconFiles[i])
		if err != nil {
			return err
		}
		icon.SetLocation(x, yIcon)
		iconXY := icon.DrawOn(page)

		text = pdfjet.NewTextLine(f1, iconNames[i])
		text.SetFontSize(9.0)
		text.SetTextColor(color.Gray)
		text.SetLocation(x, iconXY[1]+15.0)
		text.DrawOn(page)
	}

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
