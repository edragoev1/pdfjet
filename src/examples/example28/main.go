package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example_28.go
// Example that shows how to use fallback font and the NotoSans symbols font.
func Example28() {
	pdf := pdfjet.NewPDFFile("Example_28.pdf", compliance.PDF15)

	f1 := pdfjet.NewFontFromFile(pdf, "fonts/Droid/DroidSans.ttf.stream")
	f2 := pdfjet.NewFontFromFile(pdf, "fonts/Droid/DroidSansFallback.ttf.stream")
	f3 := pdfjet.NewFontFromFile(pdf, "fonts/Noto/NotoSansSymbols-Regular-Subsetted.ttf.stream")

	f1.SetSize(11.0)
	f2.SetSize(11.0)
	f3.SetSize(11.0)

	page := pdfjet.NewPage(pdf, letter.Landscape)

	f, err := os.Open("data/report.csv")
	if err != nil {
		log.Fatal(err)
	}
	defer f.Close()

	lines := make([]string, 0)
	scanner := bufio.NewScanner(f)
	for scanner.Scan() {
		lines = append(lines, scanner.Text())
	}
	if err := scanner.Err(); err != nil {
		log.Fatal(err)
	}

	var y float32 = 40.0
	for _, line := range lines {
		textLine := pdfjet.NewTextLine(f1, line)
		textLine.SetFallbackFont(f2)
		textLine.SetLocation(50, y)
		textLine.DrawOn(page)
		y += 20.0
	}

	var x float32 = 50.0
	y = 210.0
	var dy float32 = 22.0

	var buf strings.Builder
	var text = pdfjet.NewTextLine(f3, buf.String())
	var count = 0
	for i := 0x2200; i <= 0x22FF; i++ {
		// Draw the Math Symbols
		if count%80 == 0 {
			text.SetText(buf.String())
			text.SetLocation(x, y)
			text.DrawOn(page)
			buf.Reset()
			y += dy
		}
		buf.WriteRune(rune(i))
		count++
	}
	text.SetText(buf.String())
	text.SetLocation(x, y)
	text.DrawOn(page)
	buf.Reset()
	y += dy

	count = 0
	for i := 0x25A0; i <= 0x25FF; i++ {
		// Draw the Geometric Shapes
		if count%80 == 0 {
			text.SetText(buf.String())
			text.SetLocation(x, y)
			text.DrawOn(page)
			buf.Reset()
			y += dy
		}
		buf.WriteRune(rune(i))
		count++
	}
	text.SetText(buf.String())
	text.SetLocation(x, y)
	text.DrawOn(page)
	buf.Reset()
	y += dy

	count = 0
	for i := 0x2701; i <= 0x27ff; i++ {
		// Draw the Dingbats
		if count%80 == 0 {
			text.SetText(buf.String())
			text.SetLocation(x, y)
			text.DrawOn(page)
			buf.Reset()
			y += dy
		}
		buf.WriteRune(rune(i))
		count++
	}
	text.SetText(buf.String())
	text.SetLocation(x, y)
	text.DrawOn(page)
	y += dy
	buf.Reset()

	count = 0
	for i := 0x2800; i <= 0x28FF; i++ {
		// Draw the Braille Patterns
		if count%80 == 0 {
			text.SetText(buf.String())
			text.SetLocation(x, y)
			text.DrawOn(page)
			buf.Reset()
			y += dy
		}
		buf.WriteRune(rune(i))
		count++
	}
	text.SetText(buf.String())
	text.SetLocation(x, y)
	text.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example28()
	elapsed := time.Since(start)
	fmt.Printf("Example_28 => %dµs\n", elapsed.Microseconds())
}
