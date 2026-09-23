// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"math"
	"strconv"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Words with the pairs of letters kerning closes up: WA, AV, AW, AY, Yo,
// To, Vo, VA and the like.
const sample = "WAVE AWAY: Your Tokyo voyage, VAT paid."

const background = 0xf1f4f8

// Example05 draws kerning with a core font: what it is, and the same words
// in two text blocks, one above the other, drawn in Helvetica-Bold without
// kerning and with it.
//
// The fonts are core fonts, of the fourteen fonts every PDF viewer has, so the
// document carries no font program. It is small, it is written fast, and the
// widths and the kerning pairs of the fonts are built into PDFjet, which is
// what SetKernPairs applies. The disadvantages: the viewer draws the text with
// its own version of the font, so the look differs a little between viewers;
// only the WinAnsi characters can be drawn, so no Cyrillic, Greek or CJK text;
// and a document with a font that is not embedded cannot claim PDF/A or PDF/UA
// compliance. For those, use an embedded font like IBM Plex Sans, as the other
// examples do.
func Example05() {
	pdf, err := pdfjet.NewPDFFile("Example_05.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetTitle("Kerning")

	regular := pdfjet.NewCoreFont(pdf, corefont.Helvetica())
	regular.SetSize(11.0)
	bold := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	bold.SetSize(24.0)

	// The same font twice: the one without kerning, which is the default,
	// and the one with it.
	plain := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	plain.SetSize(30.0)
	kerned := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	kerned.SetSize(30.0)
	kerned.SetKernPairs(true)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	title := pdfjet.NewTextLine(bold, "Kerning")
	title.SetLocation(50.0, 70.0)
	title.DrawOn(page)

	about := pdfjet.NewTextBlock(regular,
		"Kerning moves particular pairs of letters closer together, so that the space "+
			"between the letters of a word looks even. Every letter of a font has a width, "+
			"the box it is drawn in, and some pairs of letters leave a gap between their "+
			"boxes that the eye reads as a space: a capital A beside a V or a W, a capital T, "+
			"V or Y over a small o, an L before a T. A font lists these pairs and how far to "+
			"move each of them.\n\n"+
			"The fourteen core fonts every PDF viewer has come with their lists, which are "+
			"built into PDFjet. font.setKernPairs(true) turns kerning on for a font: PDFjet "+
			"moves the letters of each pair as it draws them, with the TJ operator, and "+
			"measures the text the same way, so that a TextBlock breaks its lines where the "+
			"kerned words end. The two blocks below are the same words in the same font, "+
			"without kerning and with it.")
	about.SetLineSpacing(1.4)
	about.SetLocation(50.0, 90.0)
	about.SetWidth(512.0)
	xy := about.DrawOn(page)

	y := xy[1] + 30.0
	y = drawSample(page, regular, plain, "Without kerning: font.setKernPairs(false), the default", y)
	y = drawSample(page, regular, kerned, "With kerning: font.setKernPairs(true)", y+25.0)

	// How much kerning takes off the width of the words, as PDFjet
	// measures them, to the nearest point.
	difference := plain.StringWidth(plain.GetSize(), sample) - kerned.StringWidth(kerned.GetSize(), sample)
	narrower := int(math.Floor(float64(difference) + 0.5))
	note := pdfjet.NewTextLine(regular,
		"Kerning makes these words "+strconv.Itoa(narrower)+" points narrower at 30 points.")
	note.SetTextColor(color.Gray)
	note.SetLocation(50.0, y+30.0)
	note.DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// drawSample draws the label, and under it the sample words in the font, in
// a text block with a light background, and returns the bottom of the block.
func drawSample(page *pdfjet.Page, labelFont, font *pdfjet.Font, label string, y float32) float32 {
	caption := pdfjet.NewTextLine(labelFont, label)
	caption.SetTextColor(color.Gray)
	caption.SetLocation(50.0, y)
	caption.DrawOn(page)

	block := pdfjet.NewTextBlock(font, sample)
	block.SetBackgroundColor(background)
	block.SetPadding(10.0)
	block.SetLineSpacing(1.2)
	block.SetLocation(50.0, y+8.0)
	block.SetWidth(512.0)
	return block.DrawOn(page)[1]
}

func main() {
	time0 := time.Now().UnixMilli()
	Example05()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_05 => %4d ms\n", time1-time0)
}
