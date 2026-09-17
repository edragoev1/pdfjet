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

// Example04 draws Chinese, Japanese and Korean text with the CJK fonts, and
// Latin text with Helvetica. None of these fonts is embedded: the PDF names them
// and the viewer supplies them.
//
// The advantage is size and speed. A CJK font holds tens of thousands of
// glyphs, and this document carries none of them, so it is a few kilobytes
// and is written in a moment. The disadvantages: the viewer must have the
// Adobe Asian font packs, or a substitute, and the text takes the shapes and
// widths of whatever font it finds, so the document does not look the same
// everywhere; the core font Helvetica is limited to the WinAnsi characters; and
// a document with a font that is not embedded cannot claim PDF/A or PDF/UA
// compliance. To ship the glyphs with the document, use an embedded font like
// IBMPlexSansJP, KR, SC or TC, as Example_02 and 19 do.
//
// See: pdfjet.NewCJKFont
func Example04() {
	pdf, err := pdfjet.NewPDFFile("Example_04.pdf")
	if err != nil {
		log.Fatal(err)
	}

	// Core fonts for the Latin text
	f0 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	f5 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())

	// Chinese (Traditional) font
	// Uses Adobe's Ming Standard Light font (明體)
	f1 := pdfjet.NewCJKFont(pdf, cjkfont.AdobeMingStdLight)

	// Chinese (Simplified) font
	// Uses Adobe's Heiti SC Light font (黑体-简)
	f2 := pdfjet.NewCJKFont(pdf, cjkfont.STHeitiSCLight)

	// Japanese font
	// Uses Kozuka Mincho Pro VI Regular font (小塚明朝)
	f3 := pdfjet.NewCJKFont(pdf, cjkfont.KozMinProVIRegular)

	// Korean font
	// Uses Adobe's Myungjo Standard Medium font (명조체)
	f4 := pdfjet.NewCJKFont(pdf, cjkfont.AdobeMyungjoStdMedium)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	text := pdfjet.NewTextLine(f0, "Happy New Year!")
	text.SetFontSize(26.0)
	text.SetLocation(70.0, 90.0)
	text.DrawOn(page)

	text = pdfjet.NewTextLine(f5, "In four languages, with CJK fonts that are not embedded in this PDF.")
	text.SetFontSize(11.0)
	text.SetTextColor(color.Gray)
	text.SetLocation(70.0, 112.0)
	text.DrawOn(page)

	languages := []string{
		"Chinese (Traditional)",
		"Chinese (Simplified)",
		"Japanese",
		"Korean",
	}
	fontNames := []string{
		"Adobe Ming Std Light",
		"STHeiti SC Light",
		"Kozuka Mincho Pro VI Regular",
		"Adobe Myungjo Std Medium",
	}
	greetings := []string{
		"新年快樂!",
		"新年快乐!",
		"明けましておめでとう!",
		"새해 복 많이 받으세요!",
	}
	fonts := []*pdfjet.Font{f1, f2, f3, f4}

	y := float32(170.0)
	for i := 0; i < len(languages); i++ {
		text = pdfjet.NewTextLine(f0, languages[i])
		text.SetFontSize(12.0)
		text.SetLocation(70.0, y)
		text.DrawOn(page)

		text = pdfjet.NewTextLine(f5, fontNames[i])
		text.SetFontSize(10.0)
		text.SetTextColor(color.Gray)
		text.SetLocation(70.0, y+15.0)
		text.DrawOn(page)

		text = pdfjet.NewTextLine(fonts[i], greetings[i])
		text.SetFontSize(32.0)
		text.SetLocation(70.0, y+60.0)
		text.DrawOn(page)

		line := pdfjet.NewLine(70.0, y+80.0, 540.0, y+80.0)
		line.SetStrokeColor(color.LightGray)
		line.DrawOn(page)

		y += 115.0
	}

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example04()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_04 => %4d ms\n", time1-time0)
}
