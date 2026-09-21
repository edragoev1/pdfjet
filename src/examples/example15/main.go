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
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// The sections of the page: the formulas as CompositeTextLine.AddFormula
// reads them, and what each one is called.
var compounds = [][2]string{
	{"H2O", "Water"},
	{"CO2", "Carbon dioxide"},
	{"NaCl", "Sodium chloride"},
	{"H2SO4", "Sulfuric acid"},
	{"NaHCO3", "Sodium bicarbonate"},
	{"Fe2O3", "Iron(III) oxide"},
	{"Ca(OH)2", "Calcium hydroxide"},
	{"CuSO4·5H2O", "Copper(II) sulfate"},
	{"(NH4)3PO4", "Ammonium phosphate"},
	{"KAl(SO4)2", "Potassium alum"},
}

var organic = [][2]string{
	{"C6H12O6", "Glucose"},
	{"CH3COOH", "Acetic acid"},
	{"C8H10N4O2", "Caffeine"},
	{"C9H8O4", "Aspirin"},
	{"C2H5OH", "Ethanol"},
	{"CH3(CH2)14COOH", "Palmitic acid"},
}

var ions = [][2]string{
	{"Na^+", "Sodium"},
	{"Ca^2+", "Calcium"},
	{"NH4^+", "Ammonium"},
	{"OH^-", "Hydroxide"},
	{"SO4^2-", "Sulfate"},
	{"PO4^3-", "Phosphate"},
}

var isotopes = [][2]string{
	{"^3H", "Tritium"},
	{"^14C", "Carbon-14"},
	{"^235U", "Uranium-235"},
}

var reactions = [][2]string{
	{"2H2 + O2 → 2H2O", "Hydrogen burns"},
	{"N2 + 3H2 → 2NH3", "Ammonia, the Haber process"},
	{"CaCO3 → CaO + CO2", "Limestone is calcined"},
	{"CH4 + 2O2 → CO2 + 2H2O", "Methane burns"},
}

// Example15 draws chemical formulas with CompositeTextLine: the digits of a
// formula are subscripts, the charge of an ion and the mass number of an
// isotope are superscripts.
func Example15() {
	pdf, err := pdfjet.NewPDFFile("Example_15.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Chemical Formulas")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	title := pdfjet.NewTextLine(f2, "Chemical Formulas")
	title.SetFontSize(18.0)
	title.SetStructureType(structelem.H1)
	title.SetLocation(60.0, 70.0)
	title.DrawOn(page)

	intro := pdfjet.NewTextBlock(f1,
		"A CompositeTextLine draws one line of text out of parts that sit on "+
			"the baseline, above it or below it. AddFormula reads a formula and "+
			"places its parts: the digits that follow an element or a bracket are "+
			"subscripts, and what follows a circumflex is a superscript, the "+
			"charge of an ion or the mass number of an isotope. Each formula "+
			"below is one line and, in this tagged document, one structure "+
			"element, so a screen reader reads the formula and not its parts.")
	intro.SetFontSize(11.0)
	intro.SetLineSpacing(1.5)
	intro.SetLocation(60.0, 90.0)
	intro.SetWidth(492.0)
	xy := intro.DrawOn(page)

	y := xy[1] + 26.0
	y = drawSection(page, f1, f2, "Compounds", compounds, 2, 266.0, 150.0, y)
	y = drawSection(page, f1, f2, "Organic compounds", organic, 2, 266.0, 150.0, y)
	y = drawSection(page, f1, f2, "Ions", ions, 3, 170.0, 60.0, y)
	y = drawSection(page, f1, f2, "Isotopes", isotopes, 3, 170.0, 60.0, y)
	drawSection(page, f1, f2, "Reactions", reactions, 1, 0.0, 210.0, y)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// drawSection draws the heading of a section and its formulas in the number
// of columns, and returns the y where the next section begins.
func drawSection(page *pdfjet.Page, f1, f2 *pdfjet.Font, title string,
	items [][2]string, columns int, columnWidth, nameOffset, y float32) float32 {
	heading := pdfjet.NewTextLine(f2, title)
	heading.SetFontSize(13.0)
	heading.SetStructureType(structelem.H2)
	heading.SetLocation(60.0, y)
	heading.DrawOn(page)

	y += 22.0
	for i, item := range items {
		x := 70.0 + float32(i%columns)*columnWidth
		drawFormula(page, f1, item[0], item[1], x, y+float32(i/columns)*24.0, nameOffset)
	}
	rows := (len(items) + columns - 1) / columns
	return y + float32(rows)*24.0 + 10.0
}

// drawFormula draws the formula at the location and its name the offset to
// the right of it.
func drawFormula(
	page *pdfjet.Page, font *pdfjet.Font, formula, name string, x, y, nameOffset float32) {
	composite := pdfjet.NewCompositeTextLine(x, y)
	composite.SetFontSize(14.0)
	composite.AddFormula(font, formula)
	composite.DrawOn(page)

	text := pdfjet.NewTextLine(font, name)
	text.SetFontSize(10.0)
	text.SetTextColor(color.Gray)
	text.SetLocation(x+nameOffset, y)
	text.DrawOn(page)
}

func main() {
	time0 := time.Now().UnixMilli()
	Example15()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_15 => %4d ms\n", time1-time0)
}
