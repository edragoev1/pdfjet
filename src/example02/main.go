package main

import (
	"bufio"
	"color"
	"fmt"
	"letter"
	"log"
	"os"
	"pdfjet"
	"pdfjet/src/compliance"
	"strings"
	"time"
)

// Example02 draws the Canadian flag using a Path object that contains both lines
// and curve segments. Every curve segment must have exactly 2 control points.
func Example02() {
	file, err := os.Create("Example_02.pdf")
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	w := bufio.NewWriter(file)

	pdf := pdfjet.NewPDF(w, compliance.PDF15)

	page := pdfjet.NewPage(pdf, letter.Portrait, true)

	flag := pdfjet.NewBoxAt(85.0, 85.0, 64.0, 32.0)

	path := pdfjet.NewPath()

	path.Add(pdfjet.NewPoint(13.0, 0.0))
	path.Add(pdfjet.NewPoint(15.5, 4.5))

	path.Add(pdfjet.NewPoint(18.0, 3.5))
	path.Add(pdfjet.NewControlPoint(15.5, 13.5))
	path.Add(pdfjet.NewControlPoint(15.5, 13.5))
	path.Add(pdfjet.NewPoint(20.5, 7.5))

	path.Add(pdfjet.NewPoint(21.0, 9.5))
	path.Add(pdfjet.NewPoint(25.0, 9.0))
	path.Add(pdfjet.NewPoint(24.0, 13.0))
	path.Add(pdfjet.NewPoint(25.5, 14.0))
	path.Add(pdfjet.NewPoint(19.0, 19.0))
	path.Add(pdfjet.NewPoint(20.0, 21.5))
	path.Add(pdfjet.NewPoint(13.5, 20.5))
	path.Add(pdfjet.NewPoint(13.5, 27.0))
	path.Add(pdfjet.NewPoint(12.5, 27.0))
	path.Add(pdfjet.NewPoint(12.5, 20.5))
	path.Add(pdfjet.NewPoint(6.0, 21.5))
	path.Add(pdfjet.NewPoint(7.0, 19.0))
	path.Add(pdfjet.NewPoint(0.5, 14.0))
	path.Add(pdfjet.NewPoint(2.0, 13.0))
	path.Add(pdfjet.NewPoint(1.0, 9.0))
	path.Add(pdfjet.NewPoint(5.0, 9.5))

	path.Add(pdfjet.NewPoint(5.5, 7.5))
	path.Add(pdfjet.NewControlPoint(10.5, 13.5))
	path.Add(pdfjet.NewControlPoint(10.5, 13.5))
	path.Add(pdfjet.NewPoint(8.0, 3.5))

	path.Add(pdfjet.NewPoint(10.5, 4.5))
	path.SetClosePath(true)
	path.SetColor(color.Red)
	path.SetFillShape(true)
	path.PlaceIn(flag, 19.0, 3.0)

	path.DrawOn(page)

	box := pdfjet.NewBox()
	box.SetSize(16.0, 32.0)
	box.SetColor(color.Red)
	box.SetFillShape(true)
	box.PlaceIn(flag, 0.0, 0.0)
	box.DrawOn(page)
	box.PlaceIn(flag, 48.0, 0.0)
	box.DrawOn(page)

	path.ScaleBy(15.0)
	path.SetFillShape(false)
	xy := path.DrawOn(page)

	box = pdfjet.NewBox()
	box.SetLocation(xy[0], xy[1])
	box.SetSize(20.0, 20.0)
	box.DrawOn(page)

    // TODO:
    page.SetPenColorCMYK(1.0, 0.0, 0.0, 0.0)
    page.SetPenWidth(5.0)
    page.DrawLine(50.0, 500.0, 300.0, 500.0)

    page.SetPenColorCMYK(0.0, 1.0, 0.0, 0.0)
    page.SetPenWidth(5.0)
    page.DrawLine(50.0, 550.0, 300.0, 550.0)

    page.SetPenColorCMYK(0.0, 0.0, 1.0, 0.0)
    page.SetPenWidth(5.0)
    page.DrawLine(50.0, 600.0, 300.0, 600.0)

    page.SetPenColorCMYK(0.0, 0.0, 0.0, 1.0)
    page.SetPenWidth(5.0)
    page.DrawLine(50.0, 650.0, 300.0, 650.0)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example02()
	elapsed := time.Since(start).String()
	fmt.Printf("Example_02 => %s\n", elapsed[:strings.Index(elapsed, ".")])
}
