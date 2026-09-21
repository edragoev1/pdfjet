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
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// pngKinds are the images of the grid: the file, what it is, the
// description of it, and whether it carries transparency, which is drawn
// over a color so it can be seen.
var pngKinds = [][4]string{
	{"BASN3P08", "Palette, 8 bits", "Vertical bands of red, orange, yellow, green, cyan, blue and magenta, each shading from black at the top to white at the bottom, from a palette of 256 colors.", "no"},
	{"BASN0G08", "Grayscale, 8 bits", "Horizontal bands of gray, each shading from black at the top to white at the bottom, 8 bits a pixel.", "no"},
	{"BASN2C08", "Truecolor, 8 bits", "Horizontal bands of yellow, magenta, cyan and gray, each shading from pale at the top of the band to full color at the bottom of it, in 8 bit color.", "no"},
	{"BASN0G16", "Grayscale, 16 bits", "A ramp of 16 bit gray samples that brightens from black at the left to white near the right edge and falls away again.", "no"},
	{"BASN2C16", "Truecolor, 16 bits", "Red, green and blue of 16 bit samples mixed across the square: yellow at the top left, green at the top right, red at the bottom left and blue at the bottom right.", "no"},
	{"BASN6A08", "Truecolor with alpha", "A rainbow that shades from red at the top to blue at the bottom, which its alpha channel fades from transparent at the left to opaque at the right, over a yellow square.", "yes"},
	{"BASN4A08", "Grayscale with alpha", "A gray ramp from white at the top to black at the bottom, which its alpha channel fades from transparent at the left to opaque at the right, over a yellow square.", "yes"},
	{"TP1N3P08", "Palette with transparency", "A black cube with the word NeXT on it in colored letters, and transparent pixels around it from the tRNS chunk of its palette, over a yellow square.", "yes"},
}

// Example24 draws the image formats PDFjet reads: a JPEG, a PNG and a BMP at their own
// sizes, a CMYK JPEG whose inks are stored inverted, then a PNG of each color
// type and bit depth the format has, the two ways a PNG carries transparency,
// and a PNG that asks to be drawn at 300 dots per inch. The images of the grid
// are from PngSuite, the test images of the PNG format, drawn at four times
// their 32 by 32 pixels so their samples can be seen; the samples themselves
// are checked in PNGImageTest, against the whole of PngSuite.
func Example24() {
	pdf, err := pdfjet.NewPDFFile("Example_24.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("JPEG, PNG and BMP Images")

	font := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)

	image1 := pdfjet.NewImageFromFile(pdf, "images/gr-map.jpg")
	image1.SetAltDescription(
		"A map of Greece with its cities, roads and airports, the Ionian Sea to the west, the Aegean Sea to the east and Crete to the south.")
	image2 := pdfjet.NewImageFromFile(pdf, "images/ee-map.png")
	image2.SetAltDescription(
		"A map of Europe in which the member states of the European Union are shaded, and Turkey, a candidate to join when the map was made, in another shade.")
	image3 := pdfjet.NewImageFromFile(pdf, "images/rgb24pal.bmp")
	image3.SetAltDescription(
		"The letters BMP in white over bars of red, green, blue, yellow, magenta and cyan.")
	image4 := pdfjet.NewImageFromFile(pdf, "images/cmyk.jpg")
	image4.SetAltDescription(
		"A CMYK test chart: rows of cyan, magenta, yellow and black from 0 to 100 percent in steps of 10, and bars of red, green, blue and rich black.")

	page := pdfjet.NewPage(pdf, letter.Portrait())
	textLine1 := pdfjet.NewTextLine(font, "This is a JPEG image.")
	textLine1.SetTextRotation(0)
	textLine1.SetLocation(50.0, 50.0)
	point := textLine1.DrawOn(page)
	image1.ScaleBy(0.25).SetLocation(50.0, point[1]+5.0).DrawOn(page)

	page = pdfjet.NewPage(pdf, letter.Portrait())
	textLine2 := pdfjet.NewTextLine(font, "This is a PNG image.")
	textLine2.SetTextRotation(0)
	textLine2.SetLocation(50.0, 50.0)
	point = textLine2.DrawOn(page)
	image2.ScaleBy(0.75).SetLocation(50.0, point[1]+5.0).DrawOn(page)

	textLine3 := pdfjet.NewTextLine(font, "This is a BMP image.")
	textLine3.SetTextRotation(0)
	textLine3.SetLocation(50.0, 620.0)
	point = textLine3.DrawOn(page)
	image3.ScaleBy(0.75).SetLocation(50.0, point[1]+5.0).DrawOn(page)

	page = pdfjet.NewPage(pdf, letter.Portrait())
	textLine4 := pdfjet.NewTextLine(font, "This is a CMYK JPEG image, with its inks stored inverted, as Photoshop saves them.")
	textLine4.SetLocation(50.0, 50.0)
	point = textLine4.DrawOn(page)
	// The image is 300 DPI, so its size is 72/300 of its pixels.
	image4.ScaleBy(0.425*300.0/72.0).SetLocation(50.0, point[1]+5.0).DrawOn(page)

	drawPngKinds(pdf, font)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// drawPngKinds draws a page of a PNG of each kind, and a page of one that
// asks for its size.
func drawPngKinds(pdf *pdfjet.PDF, font *pdfjet.Font) {
	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	title := pdfjet.NewTextLine(f1, "PNG Images")
	title.SetStructureType(structelem.H1)
	title.SetFontSize(18.0)
	title.SetLocation(50.0, 50.0)
	title.DrawOn(page)

	textBlock := pdfjet.NewTextBlock(f2,
		"PDFjet reads a PNG of any color type and bit depth the format has: a "+
			"palette, grayscale or truecolor image of 1, 2, 4, 8 or 16 bits a "+
			"sample. The samples go into the PDF as they are, so an image is "+
			"embedded once and drawn at any size without being resampled. The two "+
			"ways a PNG carries transparency -- the alpha channel of a truecolor "+
			"or grayscale image, and the tRNS chunk of a palette -- both become "+
			"the soft mask of the image, which is why the three below let the "+
			"yellow square behind them through. An interlaced PNG is refused with "+
			"a message that says how to convert it.")
	textBlock.SetFontSize(11.0)
	textBlock.SetLineSpacing(1.4)
	textBlock.SetLocation(50.0, 70.0)
	textBlock.SetWidth(512.0)
	xy := textBlock.DrawOn(page)

	// The grid: four across, each image over the name of what it is.
	size := float32(128.0) // 32 pixels drawn at four times their size
	columnWidth := float32(128.0)
	rowHeight := float32(168.0)
	top := xy[1] + 24.0
	for i, row := range pngKinds {
		x := 50.0 + float32(i%4)*columnWidth
		y := top + float32(i/4)*rowHeight

		// A transparent image is drawn over a color, which its soft mask
		// lets through where the image is not opaque.
		if row[3] == "yes" {
			page.AddArtifactBMC()
			page.SetBrushColor(0xFFE9A0) // A pale yellow
			page.FillRect(x, y, size, size)
			page.AddEMC()
		}

		image := pdfjet.NewImageFromFile(pdf, "PngSuite/"+row[0]+".PNG")
		image.SetAltDescription(row[2])
		image.ScaleBy(4.0)
		image.SetLocation(x, y)
		image.DrawOn(page)

		caption := pdfjet.NewTextLine(f2, row[1])
		caption.SetFontSize(9.0)
		caption.SetLocation(x, y+size+14.0)
		caption.DrawOn(page)
	}

	// A PNG from outside PngSuite, which carries the chunks a file written by
	// a drawing program has and asks to be drawn at 300 dots per inch.
	page = pdfjet.NewPage(pdf, letter.Portrait())
	heading := pdfjet.NewTextLine(f1, "A PNG that asks for its own size")
	heading.SetStructureType(structelem.H2)
	heading.SetFontSize(14.0)
	heading.SetLocation(50.0, 50.0)
	heading.DrawOn(page)

	note := pdfjet.NewTextBlock(f2,
		"The pHYs chunk of a PNG says how large the image is meant to be. This "+
			"one is 1520 by 400 pixels at 300 dots per inch, so it is drawn 364.8 "+
			"by 96 points: a quarter of the size it would be at one point for each "+
			"pixel, and sharp for it. It carries an iCCP color profile, a bKGD "+
			"background, a tIME timestamp and two IDAT chunks, which is what its "+
			"own text says.")
	note.SetFontSize(11.0)
	note.SetLineSpacing(1.4)
	note.SetLocation(50.0, 72.0)
	note.SetWidth(512.0)
	xy2 := note.DrawOn(page)

	chunks := pdfjet.NewImageFromFile(pdf, "images/rgba-8bit-chunks.png")
	chunks.SetAltDescription(
		"Three half transparent circles in red, green and blue that overlap, " +
			"beside the heading 8-bit RGBA PNG, 1520 by 400, and the note that the " +
			"image has anti-aliased text and half transparent circles on a " +
			"transparent background, not interlaced, with iCCP, bKGD, pHYs, tIME " +
			"and two IDAT chunks.")
	chunks.SetLocation(50.0, xy2[1]+20.0)
	chunks.DrawOn(page)
}

func main() {
	time0 := time.Now().UnixMilli()
	Example24()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_24 => %4d ms\n", time1-time0)
}
