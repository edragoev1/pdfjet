package main

import (
	"bufio"
	"fmt"
	"imagetype"
	"log"
	"os"
	"pdfjet"
	"pdfjet/src/a4"
	"pdfjet/src/compliance"
	"strings"
	"time"
)

// Example17 draws the Canadian flag using a Path object that contains both lines
// and curve segments. Every curve segment must have exactly 2 control points.
func Example17() {
	file, err := os.Create("Example_17.pdf")
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	w := bufio.NewWriter(file)

	pdf := pdfjet.NewPDF(w, compliance.PDF15)

	file1, err := os.Open("PngSuite/BASN3P08.PNG")
	reader := bufio.NewReader(file1)
	defer file1.Close()
	image1 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file2, err := os.Open("PngSuite/BASN3P04.PNG") // Indexed Image with Bit Depth == 4
	reader = bufio.NewReader(file2)
	defer file2.Close()
	image2 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file3, err := os.Open("PngSuite/BASN3P02.PNG")
	reader = bufio.NewReader(file3)
	defer file3.Close()
	image3 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file4, err := os.Open("PngSuite/BASN3P01.PNG")
	reader = bufio.NewReader(file4)
	defer file4.Close()
	image4 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file5, err := os.Open("PngSuite/S01N3P01.PNG")
	reader = bufio.NewReader(file5)
	defer file5.Close()
	image5 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file6, err := os.Open("PngSuite/S02N3P01.PNG")
	reader = bufio.NewReader(file6)
	defer file6.Close()
	image6 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file7, err := os.Open("PngSuite/S03N3P01.PNG")
	reader = bufio.NewReader(file7)
	defer file7.Close()
	image7 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file8, err := os.Open("PngSuite/S04N3P01.PNG")
	reader = bufio.NewReader(file8)
	defer file8.Close()
	image8 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file9, err := os.Open("PngSuite/S05N3P02.PNG")
	reader = bufio.NewReader(file9)
	defer file9.Close()
	image9 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file10, err := os.Open("PngSuite/S06N3P02.PNG")
	reader = bufio.NewReader(file10)
	defer file10.Close()
	image10 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file11, err := os.Open("PngSuite/S07N3P02.PNG")
	reader = bufio.NewReader(file11)
	defer file11.Close()
	image11 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file12, err := os.Open("PngSuite/S08N3P02.PNG")
	reader = bufio.NewReader(file12)
	defer file12.Close()
	image12 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file13, err := os.Open("PngSuite/S09N3P02.PNG")
	reader = bufio.NewReader(file13)
	defer file13.Close()
	image13 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file14, err := os.Open("PngSuite/S32N3P04.PNG")
	reader = bufio.NewReader(file14)
	defer file14.Close()
	image14 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file15, err := os.Open("PngSuite/S33N3P04.PNG")
	reader = bufio.NewReader(file15)
	defer file15.Close()
	image15 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file16, err := os.Open("PngSuite/S34N3P04.PNG")
	reader = bufio.NewReader(file16)
	defer file16.Close()
	image16 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file17, err := os.Open("PngSuite/S35N3P04.PNG")
	reader = bufio.NewReader(file17)
	defer file17.Close()
	image17 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file18, err := os.Open("PngSuite/S36N3P04.PNG")
	reader = bufio.NewReader(file18)
	defer file18.Close()
	image18 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file19, err := os.Open("PngSuite/S37N3P04.PNG")
	reader = bufio.NewReader(file19)
	defer file19.Close()
	image19 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file20, err := os.Open("PngSuite/S38N3P04.PNG")
	reader = bufio.NewReader(file20)
	defer file20.Close()
	image20 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file21, err := os.Open("PngSuite/S39N3P04.PNG")
	reader = bufio.NewReader(file21)
	defer file21.Close()
	image21 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file22, err := os.Open("PngSuite/S40N3P04.PNG")
	reader = bufio.NewReader(file22)
	defer file22.Close()
	image22 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file23, err := os.Open("images/qrcode.png")
	reader = bufio.NewReader(file23)
	defer file23.Close()
	image23 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file24, err := os.Open("PngSuite/F00N2C08.PNG")
	reader = bufio.NewReader(file24)
	defer file24.Close()
	image24 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file25, err := os.Open("PngSuite/F01N2C08.PNG")
	reader = bufio.NewReader(file25)
	defer file25.Close()
	image25 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file26, err := os.Open("PngSuite/F02N2C08.PNG")
	reader = bufio.NewReader(file26)
	defer file26.Close()
	image26 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file27, err := os.Open("PngSuite/F03N2C08.PNG")
	reader = bufio.NewReader(file27)
	defer file27.Close()
	image27 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file28, err := os.Open("PngSuite/F04N2C08.PNG")
	reader = bufio.NewReader(file28)
	defer file28.Close()
	image28 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file29, err := os.Open("PngSuite/Z00N2C08.PNG") // color, no interlacing, compression level 0 (none)
	reader = bufio.NewReader(file29)
	defer file29.Close()
	image29 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file30, err := os.Open("PngSuite/Z03N2C08.PNG") // color, no interlacing, compression level 3
	reader = bufio.NewReader(file30)
	defer file30.Close()
	image30 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file31, err := os.Open("PngSuite/Z06N2C08.PNG") // color, no interlacing, compression level 6 (default)
	reader = bufio.NewReader(file31)
	defer file31.Close()
	image31 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file32, err := os.Open("PngSuite/Z09N2C08.PNG") // color, no interlacing, compression level 9 (maximum)
	reader = bufio.NewReader(file32)
	defer file32.Close()
	image32 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file33, err := os.Open("PngSuite/F00N0G08.PNG") // 8 bit greyscale, no interlacing, filter-type 0
	reader = bufio.NewReader(file33)
	defer file33.Close()
	image33 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file34, err := os.Open("PngSuite/F01N0G08.PNG") // 8 bit greyscale, no interlacing, filter-type 1
	reader = bufio.NewReader(file34)
	defer file34.Close()
	image34 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file35, err := os.Open("PngSuite/F02N0G08.PNG") // 8 bit greyscale, no interlacing, filter-type 2
	reader = bufio.NewReader(file35)
	defer file35.Close()
	image35 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file36, err := os.Open("PngSuite/F03N0G08.PNG") // 8 bit greyscale, no interlacing, filter-type 3
	reader = bufio.NewReader(file36)
	defer file36.Close()
	image36 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file37, err := os.Open("PngSuite/F04N0G08.PNG") // 8 bit greyscale, no interlacing, filter-type 4
	reader = bufio.NewReader(file37)
	defer file37.Close()
	image37 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file38, err := os.Open("PngSuite/BASN0G08.PNG") // 8 bit greyscale
	reader = bufio.NewReader(file38)
	defer file38.Close()
	image38 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file39, err := os.Open("PngSuite/BASN0G04.PNG") // 4 bit greyscale
	reader = bufio.NewReader(file39)
	defer file39.Close()
	image39 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file40, err := os.Open("PngSuite/BASN0G02.PNG") // 2 bit greyscale
	reader = bufio.NewReader(file40)
	defer file40.Close()
	image40 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file41, err := os.Open("PngSuite/BASN0G01.PNG") // Black and White image
	reader = bufio.NewReader(file41)
	defer file41.Close()
	image41 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file42, err := os.Open("PngSuite/BGAN6A08.PNG") // Image with alpha transparency
	reader = bufio.NewReader(file42)
	defer file42.Close()
	image42 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file43, err := os.Open("PngSuite/OI1N2C16.PNG") // Color image with 1 IDAT chunk
	reader = bufio.NewReader(file43)
	defer file43.Close()
	image43 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file44, err := os.Open("PngSuite/OI4N2C16.PNG") // Color image with 2 IDAT chunks
	reader = bufio.NewReader(file44)
	defer file44.Close()
	image44 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file45, err := os.Open("PngSuite/OI4N2C16.PNG") // Color image with 4 IDAT chunks
	reader = bufio.NewReader(file45)
	defer file45.Close()
	image45 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file46, err := os.Open("PngSuite/OI9N2C16.PNG") // IDAT chunks with length == 1
	reader = bufio.NewReader(file46)
	defer file46.Close()
	image46 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file47, err := os.Open("PngSuite/OI1N0G16.PNG") // Grayscale image with 1 IDAT chunk
	reader = bufio.NewReader(file47)
	defer file47.Close()
	image47 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file48, err := os.Open("PngSuite/OI4N0G16.PNG") // Grayscale image with 2 IDAT chunks
	reader = bufio.NewReader(file48)
	defer file48.Close()
	image48 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file49, err := os.Open("PngSuite/OI4N0G16.PNG") // Grayscale image with 4 IDAT chunks
	reader = bufio.NewReader(file49)
	defer file49.Close()
	image49 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file50, err := os.Open("PngSuite/OI9N0G16.PNG") // IDAT chunks with length == 1
	reader = bufio.NewReader(file50)
	defer file50.Close()
	image50 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file51, err := os.Open("PngSuite/TBBN3P08.PNG") // Transparent, black background chunk
	reader = bufio.NewReader(file51)
	defer file51.Close()
	image51 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file52, err := os.Open("PngSuite/TBGN3P08.PNG")
	reader = bufio.NewReader(file52)
	defer file52.Close()
	image52 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file53, err := os.Open("PngSuite/TBWN3P08.PNG")
	reader = bufio.NewReader(file53)
	defer file53.Close()
	image53 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file54, err := os.Open("PngSuite/TBYN3P08.PNG")
	reader = bufio.NewReader(file54)
	defer file54.Close()
	image54 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	file55, err := os.Open("images/LGK_ADDRESS.PNG")
	reader = bufio.NewReader(file55)
	defer file55.Close()
	image55 := pdfjet.NewImage(pdf, reader, imagetype.PNG)

	page := pdfjet.NewPage(pdf, a4.Portrait, true)

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
	start := time.Now()
	Example17()
	elapsed := time.Since(start).String()
	fmt.Printf("Example_17 => %s\n", elapsed[:strings.Index(elapsed, ".")])
}
