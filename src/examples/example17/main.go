package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/a4"
)

// Example17 is a test case for PNG images.
func Example17() {
	pdf := pdfjet.NewPDFFile("Example_17.pdf")

	image1 := pdfjet.NewImageFromFile(pdf, "PngSuite/BASN3P08.PNG")
	image2 := pdfjet.NewImageFromFile(pdf, "PngSuite/BASN3P04.PNG")
	image3 := pdfjet.NewImageFromFile(pdf, "PngSuite/BASN3P02.PNG")
	image4 := pdfjet.NewImageFromFile(pdf, "PngSuite/BASN3P01.PNG")
	image5 := pdfjet.NewImageFromFile(pdf, "PngSuite/S01N3P01.PNG")
	image6 := pdfjet.NewImageFromFile(pdf, "PngSuite/S02N3P01.PNG")
	image7 := pdfjet.NewImageFromFile(pdf, "PngSuite/S03N3P01.PNG")
	image8 := pdfjet.NewImageFromFile(pdf, "PngSuite/S04N3P01.PNG")
	image9 := pdfjet.NewImageFromFile(pdf, "PngSuite/S05N3P02.PNG")
	image10 := pdfjet.NewImageFromFile(pdf, "PngSuite/S06N3P02.PNG")
	image11 := pdfjet.NewImageFromFile(pdf, "PngSuite/S07N3P02.PNG")
	image12 := pdfjet.NewImageFromFile(pdf, "PngSuite/S08N3P02.PNG")
	image13 := pdfjet.NewImageFromFile(pdf, "PngSuite/S09N3P02.PNG")
	image14 := pdfjet.NewImageFromFile(pdf, "PngSuite/S32N3P04.PNG")
	image15 := pdfjet.NewImageFromFile(pdf, "PngSuite/S33N3P04.PNG")
	image16 := pdfjet.NewImageFromFile(pdf, "PngSuite/S34N3P04.PNG")
	image17 := pdfjet.NewImageFromFile(pdf, "PngSuite/S35N3P04.PNG")
	image18 := pdfjet.NewImageFromFile(pdf, "PngSuite/S36N3P04.PNG")
	image19 := pdfjet.NewImageFromFile(pdf, "PngSuite/S37N3P04.PNG")
	image20 := pdfjet.NewImageFromFile(pdf, "PngSuite/S38N3P04.PNG")
	image21 := pdfjet.NewImageFromFile(pdf, "PngSuite/S39N3P04.PNG")
	image22 := pdfjet.NewImageFromFile(pdf, "PngSuite/S40N3P04.PNG")

	image23 := pdfjet.NewImageFromFile(pdf, "images/qrcode.png")

	image24 := pdfjet.NewImageFromFile(pdf, "PngSuite/F00N2C08.PNG")
	image25 := pdfjet.NewImageFromFile(pdf, "PngSuite/F01N2C08.PNG")
	image26 := pdfjet.NewImageFromFile(pdf, "PngSuite/F02N2C08.PNG")
	image27 := pdfjet.NewImageFromFile(pdf, "PngSuite/F03N2C08.PNG")
	image28 := pdfjet.NewImageFromFile(pdf, "PngSuite/F04N2C08.PNG")

	image29 := pdfjet.NewImageFromFile(pdf, "PngSuite/Z00N2C08.PNG") // color, no interlacing, compression level 0 (none)
	image30 := pdfjet.NewImageFromFile(pdf, "PngSuite/Z03N2C08.PNG") // color, no interlacing, compression level 3
	image31 := pdfjet.NewImageFromFile(pdf, "PngSuite/Z06N2C08.PNG") // color, no interlacing, compression level 6 (default)
	image32 := pdfjet.NewImageFromFile(pdf, "PngSuite/Z09N2C08.PNG") // color, no interlacing, compression level 9 (maximum)

	image33 := pdfjet.NewImageFromFile(pdf, "PngSuite/F00N0G08.PNG") // 8 bit greyscale, no interlacing, filter-type 0
	image34 := pdfjet.NewImageFromFile(pdf, "PngSuite/F01N0G08.PNG") // 8 bit greyscale, no interlacing, filter-type 1
	image35 := pdfjet.NewImageFromFile(pdf, "PngSuite/F02N0G08.PNG") // 8 bit greyscale, no interlacing, filter-type 2
	image36 := pdfjet.NewImageFromFile(pdf, "PngSuite/F03N0G08.PNG") // 8 bit greyscale, no interlacing, filter-type 3
	image37 := pdfjet.NewImageFromFile(pdf, "PngSuite/F04N0G08.PNG") // 8 bit greyscale, no interlacing, filter-type 4

	image38 := pdfjet.NewImageFromFile(pdf, "PngSuite/BASN0G08.PNG") // 8 bit grayscale
	image39 := pdfjet.NewImageFromFile(pdf, "PngSuite/BASN0G04.PNG") // 4 bit grayscale
	image40 := pdfjet.NewImageFromFile(pdf, "PngSuite/BASN0G02.PNG") // 2 bit grayscale
	image41 := pdfjet.NewImageFromFile(pdf, "PngSuite/BASN0G01.PNG") // Black and White image

	image42 := pdfjet.NewImageFromFile(pdf, "PngSuite/BGAN6A08.PNG") // Image with alpha transparency

	image43 := pdfjet.NewImageFromFile(pdf, "PngSuite/OI1N2C16.PNG") // Color image with 1 IDAT chunk
	image44 := pdfjet.NewImageFromFile(pdf, "PngSuite/OI4N2C16.PNG") // Color image with 2 IDAT chunks
	image45 := pdfjet.NewImageFromFile(pdf, "PngSuite/OI4N2C16.PNG") // Color image with 4 IDAT chunks
	image46 := pdfjet.NewImageFromFile(pdf, "PngSuite/OI9N2C16.PNG") // IDAT chunks with length == 1

	image47 := pdfjet.NewImageFromFile(pdf, "PngSuite/OI1N0G16.PNG") // Grayscale image with 1 IDAT chunk
	image48 := pdfjet.NewImageFromFile(pdf, "PngSuite/OI4N0G16.PNG") // Grayscale image with 2 IDAT chunks
	image49 := pdfjet.NewImageFromFile(pdf, "PngSuite/OI4N0G16.PNG") // Grayscale image with 4 IDAT chunks
	image50 := pdfjet.NewImageFromFile(pdf, "PngSuite/OI9N0G16.PNG") // IDAT chunks with length == 1

	image51 := pdfjet.NewImageFromFile(pdf, "PngSuite/TBBN3P08.PNG") // Transparent, black background chunk
	image52 := pdfjet.NewImageFromFile(pdf, "PngSuite/TBGN3P08.PNG")
	image53 := pdfjet.NewImageFromFile(pdf, "PngSuite/TBWN3P08.PNG")
	image54 := pdfjet.NewImageFromFile(pdf, "PngSuite/TBYN3P08.PNG")
	image55 := pdfjet.NewImageFromFile(pdf, "images/LGK_ADDRESS.PNG")

	page := pdfjet.NewPage(pdf, a4.Portrait)

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

	pdf.Complete()
}

func main() {
	time0 := time.Now().UnixMilli()
	Example17()
	time1 := time.Now().UnixMilli()
	pdfjet.PrintDuration("Example_17", time0, time1)
}
