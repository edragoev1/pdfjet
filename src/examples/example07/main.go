// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"time"

	// The fonts PDFjet carries for the scripts but the European ones, built into
	// the program
	_ "github.com/edragoev1/pdfjet/v9/resources/fonts/world"
	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSansJP"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSerif"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// Example07 draws in fonts read from their files. PDFjet works with any
// TrueType (.ttf) or OpenType (.otf) font file: pass the path of the file to
// NewFontFromFile. The fonts PDFjet carries are .otf files; the third font
// here is a .ttf on purpose, IBM's TrueType edition of IBM Plex Sans in
// examples/fonts, to show a .ttf at work. A font is embedded as a subset of
// the glyphs the document draws, unless it is set to stay whole.
func Example07() {
	pdf, err := pdfjet.NewPDFFile("Example_07.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Fonts from .otf and .ttf Files")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	text := pdfjet.NewTextLine(f2, "Fonts from .otf and .ttf Files")
	text.SetStructureType(structelem.H1)
	text.SetFontSize(22.0)
	text.SetLocation(50.0, 80.0)
	text.DrawOn(page)

	textBlock := pdfjet.NewTextBlock(f1,
		"PDFjet works with any TrueType (.ttf) or OpenType (.otf) font file: pass "+
			"the path of the file to the Font constructor. The fonts that come with "+
			"PDFjet, the font constants such as IBMPlexSans and IBMPlexSansJP, are "+
			".otf files. The paragraph below is drawn four times; the third is from a "+
			".ttf file on purpose: IBM's TrueType edition of IBM Plex Sans, which the "+
			"examples have in examples/fonts, the same design as the second. A font is "+
			"embedded with only the glyphs the document draws, which keeps the file small.")
	textBlock.SetFontSize(12.0)
	textBlock.SetLineSpacing(1.5)
	textBlock.SetLocation(50.0, 95.0)
	textBlock.SetWidth(512.0)
	xy := textBlock.DrawOn(page)

	files := []string{
		IBMPlexSansJP.Regular,
		IBMPlexSans.Regular,
		"examples/fonts/IBMPlexSans/IBMPlexSans-Regular.ttf",
		IBMPlexSerif.Regular,
	}
	kinds := []string{
		"OpenType with CFF outlines, keyed by CIDs, from the .otf file, embedded as a subset",
		"OpenType with CFF outlines, from the .otf file, embedded as a subset",
		"TrueType, IBM Plex Sans again, from IBM's .ttf file, embedded as a subset",
		"A font kept whole: subsetting turned off",
	}
	sample := "The quick brown fox jumps over the lazy dog. " +
		"Ξεσκεπάζω την ψυχοφθόρα βδελυγμία. " +
		"Съешь же ещё этих мягких французских булок, да выпей чаю."

	y := xy[1] + 30.0
	for i := 0; i < len(files); i++ {
		text = pdfjet.NewTextLine(f2, files[i])
		text.SetFontSize(11.0)
		text.SetLocation(50.0, y)
		text.DrawOn(page)

		text = pdfjet.NewTextLine(f1, kinds[i])
		text.SetFontSize(10.0)
		text.SetTextColor(color.DimGray)
		text.SetLocation(50.0, y+15.0)
		text.DrawOn(page)

		font := pdfjet.NewFontFromFile(pdf, files[i])
		font.SetSubset(i != 3)
		textBlock = pdfjet.NewTextBlock(font, sample)
		textBlock.SetFontSize(13.0)
		textBlock.SetLineSpacing(1.4)
		textBlock.SetLocation(50.0, y+28.0)
		textBlock.SetWidth(512.0)
		xy = textBlock.DrawOn(page)
		y = xy[1] + 30.0
	}

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example07()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_07 => %4d ms\n", time1-time0)
}
