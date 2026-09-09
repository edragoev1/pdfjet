package main

import (
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example28 shows how to use the NotoSansSymbols font.
func Example28() {
	pdf := pdfjet.NewPDFFile("Example_28.pdf")

	f1 := pdfjet.NewFontFromFile(pdf, "fonts/NotoSansSymbols/NotoSansSymbols-Regular.ttf.stream")
	f1.SetSize(28.0)

	page := pdfjet.NewPage(pdf, letter.Landscape)

	x := float32(35.0)
	y := float32(55.0)
	dy := float32(35.0)

	drawLineOfText(page, f1, x, y, 0x0041, 0x005A)
	y += dy

	drawLineOfText(page, f1, x, y, 0x0061, 0x007A)
	y += dy

	drawLineOfText(page, f1, x, y, 0x24B6, 0x24CF)
	y += dy

	drawLineOfText(page, f1, x, y, 0x24D0, 0x24E9)
	y += dy

	drawLineOfText(page, f1, x, y, 0x24F5, 0x24FE)
	y += dy

	drawLineOfText(page, f1, x, y, 0x2624, 0x262F)
	y += dy

	drawLineOfText(page, f1, x, y, 0x2638, 0x2653)
	y += dy

	drawLineOfText(page, f1, x, y, 0x2669, 0x267E)
	y += dy

	drawLineOfText(page, f1, x, y, 0x2690, 0x26A9)
	y += dy

	drawLineOfText(page, f1, x, y, 0x26AD, 0x26BC)
	y += dy

	drawLineOfText(page, f1, x, y, 0x26E2, 0x26FE)
	y += dy

	pdf.Complete()
}

func drawLineOfText(page *pdfjet.Page, f1 *pdfjet.Font, x, y float32, c1, c2 int) {
	var buf strings.Builder
	for i := c1; i <= c2; i++ {
		buf.WriteRune(rune(i))
	}
	text := pdfjet.NewTextLine(f1, buf.String())
	text.SetLocation(x, y)
	text.DrawOn(page)
}

func main() {
	start := time.Now()
	Example28()
	pdfjet.PrintDuration("Example_28", time.Since(start))
}
