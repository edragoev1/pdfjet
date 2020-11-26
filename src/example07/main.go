package main

import (
    "a4"
	"bufio"
    "compliance"
	"fmt"
	"log"
	"os"
	"pdfjet"
	"strings"
	"time"
)

// Example07 -- TODO:
func Example07(mode string) {
	f, err := os.Create("Example_07.pdf")
	if err != nil {
		log.Fatal(err)
	}
	w := bufio.NewWriter(f)
/*
	pdf := pdfjet.NewPDF(w, compliance.PDFUA)
    pdf.SetTitle("PDF/UA compliant PDF");
*/
	pdf := pdfjet.NewPDF(w, compliance.PDF_A_1B)
    pdf.SetTitle("PDF/A-1B compliant PDF");

	var f1 *pdfjet.Font
    // Use .ttf.stream fonts
	f, err = os.Open("fonts/OpenSans/OpenSans-Regular.ttf.stream")
	if err != nil {
		log.Fatal(err)
	}
	defer f.Close()
	reader := bufio.NewReader(f)
	f1 = pdfjet.NewFontStream1(pdf, reader)

	page := pdfjet.NewPage(pdf, a4.Landscape, true)

	f1.SetSize(72.0)
	page.AddWatermark(f1, "This is a Draft")
	f1.SetSize(18.0)

	xPos := float32(20.0)
	yPos := float32(20.0)
	textLine := pdfjet.NewTextLine(f1, "")
	var buf strings.Builder
    j := 0
	for i := 0x410; i < 0x46F; i++ {
		if j%64 == 0 {
			textLine.SetText(buf.String())
			textLine.SetLocation(xPos, yPos)
			textLine.DrawOn(page)
			buf.Reset()
			yPos += 24.0
		}
		buf.WriteRune(rune(i))
        j += 1
	}
	textLine.SetText(buf.String())
	textLine.SetLocation(xPos, yPos)
	textLine.DrawOn(page)

	yPos += 24.0
	buf.Reset()
    j = 0
	for i := 0x20; i < 0x7F; i++ {
		if j%64 == 0 {
			textLine.SetText(buf.String())
			textLine.SetLocation(xPos, yPos)
			textLine.DrawOn(page)
			buf.Reset()
			yPos += 24.0
		}
        buf.WriteRune(rune(i))
        j += 1
	}
    textLine.SetText(buf.String())
    textLine.SetLocation(xPos, yPos)
    textLine.DrawOn(page)

	page = pdfjet.NewPage(pdf, a4.Landscape, true)
    textLine.SetText("Hello, World!")
    textLine.SetUnderline(true)
    textLine.SetLocation(xPos, 34.0)
    textLine.DrawOn(page)

	pdf.Complete()

	f.Close()
}

func main() {
	start := time.Now()
	Example07("stream")
	elapsed := time.Since(start).String()
	fmt.Printf("Example_07 => %s\n", elapsed[:strings.Index(elapsed, ".")])
}
