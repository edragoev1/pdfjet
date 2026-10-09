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
	"github.com/edragoev1/pdfjet/v9/src/NotoSans"
	"github.com/edragoev1/pdfjet/v9/src/SourceSerif4"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// Example28 reads fonts from OpenType and TrueType files. Any .otf or .ttf
// file on the computer is a font for PDFjet. A TrueType font is embedded as a
// subset of the glyphs the document draws, unless it is set to stay whole; a
// font with CFF outlines is embedded whole.
func Example28() {
	pdf, err := pdfjet.NewPDFFile("Example_28.pdf")
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
		"PDFjet reads OpenType and TrueType fonts as they are: pass the path "+
			"of any .otf or .ttf file on the computer to the Font constructor. "+
			"The IBMPlexSans and NotoSans constants are the paths of the fonts "+
			"that come with PDFjet. A TrueType font is embedded with only the "+
			"glyphs the document draws, which keeps the file small; a font with "+
			"CFF outlines is embedded whole. The paragraph below is drawn four times.")
	textBlock.SetFontSize(12.0)
	textBlock.SetLineSpacing(1.5)
	textBlock.SetLocation(50.0, 95.0)
	textBlock.SetWidth(512.0)
	xy := textBlock.DrawOn(page)

	files := []string{
		"fonts/IBMPlexSans/IBMPlexSans-Regular.otf",
		IBMPlexSans.Regular,
		SourceSerif4.Regular,
		NotoSans.Regular,
	}
	kinds := []string{
		"OpenType with CFF outlines, from the .otf file, embedded whole",
		"TrueType, from the .ttf file, embedded as a subset",
		"Another TrueType font, embedded as a subset",
		"A TrueType font kept whole: subsetting turned off",
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
	Example28()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_28 => %4d ms\n", time1-time0)
}
