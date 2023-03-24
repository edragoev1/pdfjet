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

func Example28() {
	f, err := os.Create("Example_28.pdf")
	if err != nil {
		log.Fatal(err)
	}
	w := bufio.NewWriter(f)
	pdf := pdfjet.NewPDF(w, compliance.PDF15)

	f, err = os.Open("fonts/Droid/DroidSans.ttf.stream")
	if err != nil {
		log.Fatal(err)
	}
	defer f.Close()
	reader := bufio.NewReader(f)
	f1 := pdfjet.NewFontStream1(pdf, reader)

	f, err = os.Open("fonts/Droid/DroidSansFallback.ttf.stream")
	if err != nil {
		log.Fatal(err)
	}
	defer f.Close()
	reader = bufio.NewReader(f)
	f2 := pdfjet.NewFontStream1(pdf, reader)

	f, err = os.Open("fonts/Noto/NotoSansSymbols-Regular-Subsetted.ttf.stream")
	if err != nil {
		log.Fatal(err)
	}
	defer f.Close()
	reader = bufio.NewReader(f)
	f3 := pdfjet.NewFontStream1(pdf, reader)

	page := pdfjet.NewPageAddTo(pdf, letter.Landscape)

	f, err = os.Open("data/report.csv")
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
