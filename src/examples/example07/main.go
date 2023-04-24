package main

import (
	"fmt"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/a4"
	"github.com/edragoev1/pdfjet/src/compliance"
)

// Example07 -- TODO:
func Example07(mode string) {
	// pdf := pdfjet.NewPDF(w, compliance.PDFUA)
	// pdf.SetTitle("PDF/UA compliant PDF");
	pdf := pdfjet.NewPDFFile("Example_07.pdf", compliance.PDF_A_1B)
	pdf.SetTitle("PDF/A-1B compliant PDF")
	var f1 = pdfjet.NewFontFromFile(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream")

	page := pdfjet.NewPage(pdf, a4.Landscape)

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

	page = pdfjet.NewPage(pdf, a4.Landscape)
	textLine.SetText("Hello, World!")
	textLine.SetUnderline(true)
	textLine.SetLocation(xPos, 34.0)
	textLine.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example07("stream")
	elapsed := time.Since(start)
	fmt.Printf("Example_07 => %.2fms\n", float32(elapsed.Microseconds())/float32(1000.0))
}
