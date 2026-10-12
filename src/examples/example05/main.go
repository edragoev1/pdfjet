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
	"github.com/edragoev1/pdfjet/v9/src/cjkfont"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example05 draws fonts that are not embedded: the twelve text faces of the
// core fonts, and Chinese, Japanese and Korean in the CJK fonts.
//
// The PDF names each font and the viewer supplies it, so the document carries
// no glyphs: it is a few kilobytes, where an embedded CJK font adds hundreds,
// and it is written in a moment. The cost: the text takes the shapes of the
// font the viewer has, so it does not look the same everywhere; the core fonts
// draw only the characters of Western European languages; the CJK fonts need
// the Adobe Asian font pack, and draw Latin letters and spaces full width; and
// a document with a font that is not embedded cannot be PDF/A or PDF/UA. To
// carry the glyphs with the document, use an embedded font, as Example_02 and
// Example_03 do.
//
// See: pdfjet.NewCoreFont and pdfjet.NewCJKFont
func Example05() {
	pdf, err := pdfjet.NewPDFFile("Example_05.pdf")
	if err != nil {
		log.Fatal(err)
	}

	title := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	title.SetSize(22.0)
	heading := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	heading.SetSize(13.0)
	body := pdfjet.NewCoreFont(pdf, corefont.Helvetica())
	body.SetSize(10.0)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	text := pdfjet.NewTextLine(title, "Fonts that are not embedded")
	text.SetLocation(50.0, 60.0)
	text.DrawOn(page)

	block := pdfjet.NewTextBlock(body,
		"None of the fonts of this document is embedded: the PDF names each font, "+
			"and the viewer supplies it. That keeps the file very small, a few kilobytes, "+
			"where an embedded Chinese, Japanese or Korean font adds hundreds, and it is "+
			"written in a moment. What it costs is at the bottom of the page.")
	block.SetLocation(50.0, 75.0)
	block.SetWidth(512.0)
	end := block.DrawOn(page)

	// The twelve text faces of the core fonts, each drawn in itself
	text = pdfjet.NewTextLine(heading, "The core fonts")
	text.SetLocation(50.0, end[1]+30.0)
	text.DrawOn(page)
	coreFonts := []*corefont.CoreFont{
		corefont.Helvetica(), corefont.HelveticaBold(),
		corefont.HelveticaOblique(), corefont.HelveticaBoldOblique(),
		corefont.TimesRoman(), corefont.TimesBold(),
		corefont.TimesItalic(), corefont.TimesBoldItalic(),
		corefont.Courier(), corefont.CourierBold(),
		corefont.CourierOblique(), corefont.CourierBoldOblique(),
	}
	coreNames := []string{
		"Helvetica", "Helvetica-Bold", "Helvetica-Oblique", "Helvetica-BoldOblique",
		"Times-Roman", "Times-Bold", "Times-Italic", "Times-BoldItalic",
		"Courier", "Courier-Bold", "Courier-Oblique", "Courier-BoldOblique",
	}
	y := end[1] + 52.0
	for i := 0; i < len(coreFonts); i++ {
		font := pdfjet.NewCoreFont(pdf, coreFonts[i])
		font.SetSize(11.0)
		text = pdfjet.NewTextLine(font, coreNames[i])
		text.SetLocation(50.0+float32(i%2)*256.0, y)
		text.DrawOn(page)
		if i%2 == 1 {
			y += 17.0
		}
	}

	// A greeting in each of the four CJK fonts
	text = pdfjet.NewTextLine(heading, "The CJK fonts")
	text.SetLocation(50.0, y+20.0)
	text.DrawOn(page)
	cjkFonts := []cjkfont.Font{
		cjkfont.AdobeMingStdLight,
		cjkfont.STHeitiSCLight,
		cjkfont.KozMinProVIRegular,
		cjkfont.AdobeMyungjoStdMedium,
	}
	languages := []string{
		"Chinese (Traditional), Adobe Ming Std Light",
		"Chinese (Simplified), STHeiti SC Light",
		"Japanese, Kozuka Mincho Pro VI Regular",
		"Korean, Adobe Myungjo Std Medium",
	}
	greetings := []string{
		"新年快樂!",
		"新年快乐!",
		"明けましておめでとう!",
		"새해 복 많이 받으세요!",
	}
	y += 42.0
	for i := 0; i < len(cjkFonts); i++ {
		text = pdfjet.NewTextLine(body, languages[i])
		text.SetTextColor(color.DimGray)
		text.SetLocation(50.0, y)
		text.DrawOn(page)
		font := pdfjet.NewCJKFont(pdf, cjkFonts[i])
		font.SetSize(20.0)
		text = pdfjet.NewTextLine(font, greetings[i])
		text.SetLocation(50.0, y+26.0)
		text.DrawOn(page)
		y += 48.0
	}

	// What it gains, and what it costs
	text = pdfjet.NewTextLine(heading, "Small, at a cost")
	text.SetLocation(50.0, y+8.0)
	text.DrawOn(page)
	mark := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	mark.SetSize(10.0)
	marks := []string{"+", "–", "–", "–", "–"}
	items := []string{
		"Very small and fast: the document carries no glyphs, where one CJK font holds " +
			"tens of thousands.",
		"The text takes the shapes and widths of the font the viewer has, so the document " +
			"does not look the same everywhere.",
		"The core fonts draw only the characters of Western European languages.",
		"The CJK fonts need the Adobe Asian font pack, which Acrobat and Reader offer to " +
			"download, and draw Latin letters and spaces full width.",
		"A document with a font that is not embedded cannot be PDF/A or PDF/UA, so it is " +
			"neither archival nor accessible.",
	}
	y += 26.0
	for i := 0; i < len(items); i++ {
		text = pdfjet.NewTextLine(mark, marks[i])
		text.SetLocation(52.0, y+8.0)
		text.DrawOn(page)
		block = pdfjet.NewTextBlock(body, items[i])
		block.SetLocation(66.0, y)
		block.SetWidth(496.0)
		end = block.DrawOn(page)
		y = end[1] + 6.0
	}
	block = pdfjet.NewTextBlock(body, "To carry its glyphs with the document, use an embedded "+
		"font, as Example_02 and Example_03 do.")
	block.SetLocation(50.0, y+6.0)
	block.SetWidth(512.0)
	block.DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example05()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_05 => %4d ms\n", time1-time0)
}
