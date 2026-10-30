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
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// Example33 uses the SVGImage component to draw a map of Europe, scaled to
// fit the page, and a bar chart made with D3, its text in the fonts of the
// document.
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
	text.SetStructureType(structelem.H1)
	text.SetFontSize(22.0)
	text.SetLocation(50.0, 80.0)
	text.DrawOn(page)

	textBlock := pdfjet.NewTextBlock(f1,
		"SVGImage reads the paths of an SVG file and draws them as vector "+
			"graphics, which stay sharp at any zoom. The map is 865 points wide "+
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
	svgMap.SetAltDescription(
		"A map of Europe drawn from an SVG file, the countries filled in peachpuff and outlined in dim gray, Spain in aliceblue and Austria outlined in light gray.")
	svgMap.ScaleBy(0.5)
	// In the middle of the page, and the text under it as wide as it
	mapX := (page.GetWidth() - svgMap.GetWidth()) / 2.0
	svgMap.SetLocation(mapX, xy[1]+20.0)
	xy = svgMap.DrawOn(page)

	textBlock = pdfjet.NewTextBlock(f1,
		"The colors are set in the SVG file itself. The svg element gives every "+
			"country a peachpuff fill and a dimgray outline; Spain overrides the fill "+
			"with aliceblue, and Austria the outline with lightgray.")
	textBlock.SetFontSize(10.0)
	textBlock.SetTextColor(color.DimGray)
	textBlock.SetLocation(mapX, xy[1]+10.0)
	textBlock.SetWidth(svgMap.GetWidth())
	xy = textBlock.DrawOn(page)

	// A chart made with D3, as a browser draws it, by util/make-d3-chart.mjs:
	// its text, in sans-serif, is drawn in IBM Plex Sans, which PDFjet carries
	// and embeds, as PDF/UA asks; on a page of its own
	page = pdfjet.NewPage(pdf, a4.Portrait())
	heading := pdfjet.NewTextLine(f2, "A Chart Made with D3")
	heading.SetStructureType(structelem.H2)
	heading.SetFontSize(22.0)
	heading.SetLocation(50.0, 80.0)
	heading.DrawOn(page)

	textBlock = pdfjet.NewTextBlock(f1,
		"SVGImage draws the charts of D3 and the like, their text in the fonts "+
			"PDFjet carries, embedded: IBM Plex Sans for "+
			"sans-serif, IBM Plex Serif for serif and Cascadia Mono for monospace. "+
			"This chart was made by D3 in util/make-d3-chart.mjs.")
	textBlock.SetFontSize(12.0)
	textBlock.SetLineSpacing(1.5)
	textBlock.SetLocation(50.0, 95.0)
	textBlock.SetWidth(495.0)
	xy = textBlock.DrawOn(page)

	chart, err := pdfjet.NewSVGImageFromFile("images/svg/d3-life-expectancy-by-region.svg")
	if err != nil {
		return err
	}
	chart.SetAltDescription("A bar chart of the life expectancy at birth of the World Bank's " +
		"regions in 2023: North America 78.7 years, Europe and Central Asia 78.4, " +
		"East Asia and Pacific 76.7, Latin America and the Caribbean 75.6, South Asia 72.3, " +
		"the Middle East, North Africa, Afghanistan and Pakistan 72.1, " +
		"and Sub-Saharan Africa 62.6; the world 73.3.")
	chart.ScaleBy(495.0 / chart.GetWidth())
	chart.SetLocation(50.0, xy[1]+20.0)
	chart.DrawOn(page)

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
