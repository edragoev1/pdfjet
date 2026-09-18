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
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example28 reads fonts from OpenType and TrueType files and from the .stream
// files of the same fonts. Any .otf or .ttf file on the computer is a font for
// PDFjet; a .stream file is the same font, compressed once, so that it loads
// and embeds faster.
func Example28() {
	pdf, err := pdfjet.NewPDFFile("Example_28.pdf")
	if err != nil {
		log.Fatal(err)
	}

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	text := pdfjet.NewTextLine(f2, "Fonts from .otf, .ttf and .stream Files")
	text.SetFontSize(22.0)
	text.SetLocation(50.0, 80.0)
	text.DrawOn(page)

	textBlock := pdfjet.NewTextBlock(f1,
		"PDFjet reads OpenType and TrueType fonts as they are: pass the path "+
			"of any .otf or .ttf file on the computer to the Font constructor. "+
			"The .stream files that come with PDFjet hold the same fonts, "+
			"compressed once, so that a font loads and embeds faster; the "+
			"IBMPlexSans and NotoSans constants are their paths. "+
			"The paragraph below is drawn four times, from the two kinds of file.")
	textBlock.SetFontSize(12.0)
	textBlock.SetLineSpacing(1.5)
	textBlock.SetLocation(50.0, 95.0)
	textBlock.SetWidth(512.0)
	xy := textBlock.DrawOn(page)

	files := []string{
		"fonts/IBMPlexSans/IBMPlexSans-Regular.otf",
		"fonts/NotoSans/NotoSans-Regular.ttf",
		IBMPlexSans.Regular,
		NotoSans.Regular,
	}
	kinds := []string{
		"OpenType, with CFF outlines, read from the .otf file",
		"TrueType, read from the .ttf file",
		"The same OpenType font from its .stream file",
		"The same TrueType font from its .stream file",
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
		text.SetTextColor(color.Gray)
		text.SetLocation(50.0, y+15.0)
		text.DrawOn(page)

		font := pdfjet.NewFontFromFile(pdf, files[i])
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
