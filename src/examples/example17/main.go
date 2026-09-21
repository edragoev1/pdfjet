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
	"github.com/edragoev1/pdfjet/v9/src/a4"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
)

// Example17 is a test case for PNG images.
func Example17() {
	pdf, err := pdfjet.NewPDFFile("Example_17.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("PNG Images from PngSuite")

	image1 := pdfjet.NewImageFromFile(pdf, "PngSuite/BASN3P08.PNG")
	image1.SetAltDescription(
		"Vertical bands of red, orange, yellow, green, cyan, blue and magenta, each shading from black at the top to white at the bottom, from a palette of 256 colors.")
	image2 := pdfjet.NewImageFromFile(pdf, "PngSuite/BASN3P04.PNG")
	image2.SetAltDescription(
		"A rainbow that runs from the bottom left to the top right, from a palette of 16 colors.")
	image3 := pdfjet.NewImageFromFile(pdf, "PngSuite/BASN3P02.PNG")
	image3.SetAltDescription(
		"A diagonal check of red, green, blue and yellow, from a palette of four colors.")
	image4 := pdfjet.NewImageFromFile(pdf, "PngSuite/BASN3P01.PNG")
	image4.SetAltDescription(
		"A check of blue and yellow squares, from a palette of two colors.")
	image5 := pdfjet.NewImageFromFile(pdf, "PngSuite/S01N3P01.PNG")
	image5.SetAltDescription(
		"The size test image of 1 by 1 pixels, from a palette of two colors.")
	image6 := pdfjet.NewImageFromFile(pdf, "PngSuite/S02N3P01.PNG")
	image6.SetAltDescription(
		"The size test image of 2 by 2 pixels, from a palette of two colors.")
	image7 := pdfjet.NewImageFromFile(pdf, "PngSuite/S03N3P01.PNG")
	image7.SetAltDescription(
		"The size test image of 3 by 3 pixels, from a palette of two colors.")
	image8 := pdfjet.NewImageFromFile(pdf, "PngSuite/S04N3P01.PNG")
	image8.SetAltDescription(
		"The size test image of 4 by 4 pixels, from a palette of two colors.")
	image9 := pdfjet.NewImageFromFile(pdf, "PngSuite/S05N3P02.PNG")
	image9.SetAltDescription(
		"The size test image of 5 by 5 pixels: frames of color around its center, from a palette of four colors.")
	image10 := pdfjet.NewImageFromFile(pdf, "PngSuite/S06N3P02.PNG")
	image10.SetAltDescription(
		"The size test image of 6 by 6 pixels: frames of color around its center, from a palette of four colors.")
	image11 := pdfjet.NewImageFromFile(pdf, "PngSuite/S07N3P02.PNG")
	image11.SetAltDescription(
		"The size test image of 7 by 7 pixels: frames of color around its center, from a palette of four colors.")
	image12 := pdfjet.NewImageFromFile(pdf, "PngSuite/S08N3P02.PNG")
	image12.SetAltDescription(
		"The size test image of 8 by 8 pixels: frames of color around its center, from a palette of four colors.")
	image13 := pdfjet.NewImageFromFile(pdf, "PngSuite/S09N3P02.PNG")
	image13.SetAltDescription(
		"The size test image of 9 by 9 pixels: frames of color around its center, from a palette of four colors.")
	image14 := pdfjet.NewImageFromFile(pdf, "PngSuite/S32N3P04.PNG")
	image14.SetAltDescription(
		"The size test image of 32 by 32 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.")
	image15 := pdfjet.NewImageFromFile(pdf, "PngSuite/S33N3P04.PNG")
	image15.SetAltDescription(
		"The size test image of 33 by 33 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.")
	image16 := pdfjet.NewImageFromFile(pdf, "PngSuite/S34N3P04.PNG")
	image16.SetAltDescription(
		"The size test image of 34 by 34 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.")
	image17 := pdfjet.NewImageFromFile(pdf, "PngSuite/S35N3P04.PNG")
	image17.SetAltDescription(
		"The size test image of 35 by 35 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.")
	image18 := pdfjet.NewImageFromFile(pdf, "PngSuite/S36N3P04.PNG")
	image18.SetAltDescription(
		"The size test image of 36 by 36 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.")
	image19 := pdfjet.NewImageFromFile(pdf, "PngSuite/S37N3P04.PNG")
	image19.SetAltDescription(
		"The size test image of 37 by 37 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.")
	image20 := pdfjet.NewImageFromFile(pdf, "PngSuite/S38N3P04.PNG")
	image20.SetAltDescription(
		"The size test image of 38 by 38 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.")
	image21 := pdfjet.NewImageFromFile(pdf, "PngSuite/S39N3P04.PNG")
	image21.SetAltDescription(
		"The size test image of 39 by 39 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.")
	image22 := pdfjet.NewImageFromFile(pdf, "PngSuite/S40N3P04.PNG")
	image22.SetAltDescription(
		"The size test image of 40 by 40 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.")

	image23 := pdfjet.NewImageFromFile(pdf, "images/qrcode.png")
	image23.SetAltDescription(
		"A QR code for https://pdfjet.com.")

	image24 := pdfjet.NewImageFromFile(pdf, "PngSuite/F00N2C08.PNG")
	image24.SetAltDescription(
		"A zero with a stroke through it, over a gradient of red, yellow, green and blue, written with the PNG filter type 0, none.")
	image25 := pdfjet.NewImageFromFile(pdf, "PngSuite/F01N2C08.PNG")
	image25.SetAltDescription(
		"A zero with a stroke through it, over a gradient of red, yellow, green and blue, written with the PNG filter type 1, sub.")
	image26 := pdfjet.NewImageFromFile(pdf, "PngSuite/F02N2C08.PNG")
	image26.SetAltDescription(
		"A zero with a stroke through it, over a gradient of red, yellow, green and blue, written with the PNG filter type 2, up.")
	image27 := pdfjet.NewImageFromFile(pdf, "PngSuite/F03N2C08.PNG")
	image27.SetAltDescription(
		"A zero with a stroke through it, over a gradient of red, yellow, green and blue, written with the PNG filter type 3, average.")
	image28 := pdfjet.NewImageFromFile(pdf, "PngSuite/F04N2C08.PNG")
	image28.SetAltDescription(
		"A zero with a stroke through it, over a gradient of red, yellow, green and blue, written with the PNG filter type 4, Paeth.")

	image29 := pdfjet.NewImageFromFile(pdf, "PngSuite/Z00N2C08.PNG") // color, no interlacing, compression level 0 (none)
	image29.SetAltDescription(
		"A gradient of red, yellow, green and blue, deflated at compression level 0, which does not compress.")
	image30 := pdfjet.NewImageFromFile(pdf, "PngSuite/Z03N2C08.PNG") // color, no interlacing, compression level 3
	image30.SetAltDescription(
		"A gradient of red, yellow, green and blue, deflated at compression level 3.")
	image31 := pdfjet.NewImageFromFile(pdf, "PngSuite/Z06N2C08.PNG") // color, no interlacing, compression level 6 (default)
	image31.SetAltDescription(
		"A gradient of red, yellow, green and blue, deflated at compression level 6, the default.")
	image32 := pdfjet.NewImageFromFile(pdf, "PngSuite/Z09N2C08.PNG") // color, no interlacing, compression level 9 (maximum)
	image32.SetAltDescription(
		"A gradient of red, yellow, green and blue, deflated at compression level 9, the most.")

	image33 := pdfjet.NewImageFromFile(pdf, "PngSuite/F00N0G08.PNG") // 8 bit greyscale, no interlacing, filter-type 0
	image33.SetAltDescription(
		"A zero with a stroke through it, over a gradient from black to white, written with the PNG filter type 0, none.")
	image34 := pdfjet.NewImageFromFile(pdf, "PngSuite/F01N0G08.PNG") // 8 bit greyscale, no interlacing, filter-type 1
	image34.SetAltDescription(
		"A zero with a stroke through it, over a gradient from black to white, written with the PNG filter type 1, sub.")
	image35 := pdfjet.NewImageFromFile(pdf, "PngSuite/F02N0G08.PNG") // 8 bit greyscale, no interlacing, filter-type 2
	image35.SetAltDescription(
		"A zero with a stroke through it, over a gradient from black to white, written with the PNG filter type 2, up.")
	image36 := pdfjet.NewImageFromFile(pdf, "PngSuite/F03N0G08.PNG") // 8 bit greyscale, no interlacing, filter-type 3
	image36.SetAltDescription(
		"A zero with a stroke through it, over a gradient from black to white, written with the PNG filter type 3, average.")
	image37 := pdfjet.NewImageFromFile(pdf, "PngSuite/F04N0G08.PNG") // 8 bit greyscale, no interlacing, filter-type 4
	image37.SetAltDescription(
		"A zero with a stroke through it, over a gradient from black to white, written with the PNG filter type 4, Paeth.")

	image38 := pdfjet.NewImageFromFile(pdf, "PngSuite/BASN0G08.PNG") // 8 bit grayscale
	image38.SetAltDescription(
		"Horizontal bands of gray, from black to white, 8 bits a pixel.")
	image39 := pdfjet.NewImageFromFile(pdf, "PngSuite/BASN0G04.PNG") // 4 bit grayscale
	image39.SetAltDescription(
		"A gradient of 16 grays from black at the top left to white at the bottom right, 4 bits a pixel.")
	image40 := pdfjet.NewImageFromFile(pdf, "PngSuite/BASN0G02.PNG") // 2 bit grayscale
	image40.SetAltDescription(
		"A diagonal check in four grays, 2 bits a pixel.")
	image41 := pdfjet.NewImageFromFile(pdf, "PngSuite/BASN0G01.PNG") // Black and White image
	image41.SetAltDescription(
		"The letters W and B on the two halves of a diagonal, in black and white, 1 bit a pixel.")

	image42 := pdfjet.NewImageFromFile(pdf, "PngSuite/BGAN6A08.PNG") // Image with alpha transparency
	image42.SetAltDescription(
		"A rainbow that shades from red at the top to blue at the bottom, in 8 bit color with an alpha channel.")

	image43 := pdfjet.NewImageFromFile(pdf, "PngSuite/OI1N2C16.PNG") // Color image with 1 IDAT chunk
	image43.SetAltDescription(
		"A gradient of red, yellow, green and blue in 16 bit color, in one IDAT chunk.")
	image44 := pdfjet.NewImageFromFile(pdf, "PngSuite/OI2N2C16.PNG") // Color image with 2 IDAT chunks
	image44.SetAltDescription(
		"A gradient of red, yellow, green and blue in 16 bit color, in two IDAT chunks.")
	image45 := pdfjet.NewImageFromFile(pdf, "PngSuite/OI4N2C16.PNG") // Color image with 4 IDAT chunks
	image45.SetAltDescription(
		"A gradient of red, yellow, green and blue in 16 bit color, in four IDAT chunks.")
	image46 := pdfjet.NewImageFromFile(pdf, "PngSuite/OI9N2C16.PNG") // IDAT chunks with length == 1
	image46.SetAltDescription(
		"A gradient of red, yellow, green and blue in 16 bit color, in IDAT chunks of one byte.")

	image47 := pdfjet.NewImageFromFile(pdf, "PngSuite/OI1N0G16.PNG") // Grayscale image with 1 IDAT chunk
	image47.SetAltDescription(
		"A gradient from black to white in 16 bit grayscale, in one IDAT chunk.")
	image48 := pdfjet.NewImageFromFile(pdf, "PngSuite/OI2N0G16.PNG") // Grayscale image with 2 IDAT chunks
	image48.SetAltDescription(
		"A gradient from black to white in 16 bit grayscale, in two IDAT chunks.")
	image49 := pdfjet.NewImageFromFile(pdf, "PngSuite/OI4N0G16.PNG") // Grayscale image with 4 IDAT chunks
	image49.SetAltDescription(
		"A gradient from black to white in 16 bit grayscale, in four IDAT chunks.")
	image50 := pdfjet.NewImageFromFile(pdf, "PngSuite/OI9N0G16.PNG") // IDAT chunks with length == 1
	image50.SetAltDescription(
		"A gradient from black to white in 16 bit grayscale, in IDAT chunks of one byte.")

	image51 := pdfjet.NewImageFromFile(pdf, "PngSuite/TBBN3P08.PNG") // Transparent, black background chunk
	image51.SetAltDescription(
		"A black cube with the word NeXT on it in colored letters, with transparent pixels around it and a black background color in the file.")
	image52 := pdfjet.NewImageFromFile(pdf, "PngSuite/TBGN3P08.PNG")
	image52.SetAltDescription(
		"A black cube with the word NeXT on it in colored letters, with transparent pixels around it and a gray background color in the file.")
	image53 := pdfjet.NewImageFromFile(pdf, "PngSuite/TBWN3P08.PNG")
	image53.SetAltDescription(
		"A black cube with the word NeXT on it in colored letters, with transparent pixels around it and a white background color in the file.")
	image54 := pdfjet.NewImageFromFile(pdf, "PngSuite/TBYN3P08.PNG")
	image54.SetAltDescription(
		"A black cube with the word NeXT on it in colored letters, with transparent pixels around it and a yellow background color in the file.")
	image55 := pdfjet.NewImageFromFile(pdf, "images/rgba-8bit-chunks.png")
	image55.SetAltDescription(
		"Three half transparent circles in red, green and blue that overlap, beside the heading 8-bit RGBA PNG, 380 by 100, and the note that the image has anti-aliased text and half transparent circles on a transparent background, not interlaced, with iCCP, bKGD, pHYs, tIME and two IDAT chunks.")

	page := pdfjet.NewPage(pdf, a4.Portrait())

	image1.SetLocation(100.0, 80.0)
	image1.DrawOn(page)

	image2.SetLocation(100.0, 120.0)
	image2.DrawOn(page)

	image3.SetLocation(100.0, 160.0)
	image3.DrawOn(page)

	image4.SetLocation(100.0, 200.0)
	image4.DrawOn(page)

	image5.SetLocation(200.0, 80.0)
	image5.DrawOn(page)

	image6.SetLocation(200.0, 120.0)
	image6.DrawOn(page)

	image7.SetLocation(200.0, 160.0)
	image7.DrawOn(page)

	image8.SetLocation(200.0, 200.0)
	image8.DrawOn(page)

	image9.SetLocation(200.0, 240.0)
	image9.DrawOn(page)

	image10.SetLocation(200.0, 280.0)
	image10.DrawOn(page)

	image11.SetLocation(200.0, 320.0)
	image11.DrawOn(page)

	image12.SetLocation(200.0, 360.0)
	image12.DrawOn(page)

	image13.SetLocation(200.0, 400.0)
	image13.DrawOn(page)

	image14.SetLocation(300.0, 80.0)
	image14.DrawOn(page)

	image15.SetLocation(300.0, 120.0)
	image15.DrawOn(page)

	image16.SetLocation(300.0, 160.0)
	image16.DrawOn(page)

	image17.SetLocation(300.0, 200.0)
	image17.DrawOn(page)

	image18.SetLocation(300.0, 240.0)
	image18.DrawOn(page)

	image19.SetLocation(300.0, 280.0)
	image19.DrawOn(page)

	image20.SetLocation(300.0, 320.0)
	image20.DrawOn(page)

	image21.SetLocation(300.0, 360.0)
	image21.DrawOn(page)

	image22.SetLocation(300.0, 400.0)
	image22.DrawOn(page)

	image23.SetLocation(350.0, 50.0)
	image23.DrawOn(page)

	image24.SetLocation(100.0, 650.0)
	image24.DrawOn(page)

	image25.SetLocation(140.0, 650.0)
	image25.DrawOn(page)

	image26.SetLocation(180.0, 650.0)
	image26.DrawOn(page)

	image27.SetLocation(220.0, 650.0)
	image27.DrawOn(page)

	image28.SetLocation(260.0, 650.0)
	image28.DrawOn(page)

	image29.SetLocation(300.0, 650.0)
	image29.DrawOn(page)

	image30.SetLocation(340.0, 650.0)
	image30.DrawOn(page)

	image31.SetLocation(380.0, 650.0)
	image31.DrawOn(page)

	image32.SetLocation(420.0, 650.0)
	image32.DrawOn(page)

	image33.SetLocation(100.0, 700.0)
	image33.DrawOn(page)

	image34.SetLocation(140.0, 700.0)
	image34.DrawOn(page)

	image35.SetLocation(180.0, 700.0)
	image35.DrawOn(page)

	image36.SetLocation(220.0, 700.0)
	image36.DrawOn(page)

	image37.SetLocation(260.0, 700.0)
	image37.DrawOn(page)

	image38.SetLocation(300.0, 700.0)
	image38.DrawOn(page)

	image39.SetLocation(340.0, 700.0)
	image39.DrawOn(page)

	image40.SetLocation(380.0, 700.0)
	image40.DrawOn(page)

	image41.SetLocation(420.0, 700.0)
	image41.DrawOn(page)

	image42.SetLocation(100.0, 750.0)
	image42.DrawOn(page)

	image43.SetLocation(140.0, 750.0)
	image43.DrawOn(page)

	image44.SetLocation(180.0, 750.0)
	image44.DrawOn(page)

	image45.SetLocation(220.0, 750.0)
	image45.DrawOn(page)

	image46.SetLocation(260.0, 750.0)
	image46.DrawOn(page)

	image47.SetLocation(300.0, 750.0)
	image47.DrawOn(page)

	image48.SetLocation(340.0, 750.0)
	image48.DrawOn(page)

	image49.SetLocation(380.0, 750.0)
	image49.DrawOn(page)

	image50.SetLocation(420.0, 750.0)
	image50.DrawOn(page)

	image51.SetLocation(300.0, 800.0)
	image51.DrawOn(page)

	image52.SetLocation(340.0, 800.0)
	image52.DrawOn(page)

	image53.SetLocation(380.0, 800.0)
	image53.DrawOn(page)

	image54.SetLocation(420.0, 800.0)
	image54.DrawOn(page)

	image55.SetLocation(100.0, 500.0)
	image55.DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example17()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_17 => %4d ms\n", time1-time0)
}
