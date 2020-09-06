package main

import (
	"bufio"
	"fmt"
	"letter"
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

	pdf := pdfjet.NewPDF(w, 0)

	var f1 *pdfjet.Font
	var f2 *pdfjet.Font
	if mode == "stream" {
		// Use .ttf.stream fonts
		f, err = os.Open("fonts/OpenSans/OpenSans-Regular.ttf.stream")
		if err != nil {
			log.Fatal(err)
		}
		defer f.Close()
		reader := bufio.NewReader(f)
		f1 = pdfjet.NewFontStream1(pdf, reader)

		// f, err = os.Open("fonts/SourceSansPro/SourceSansPro-Regular.otf.stream")
		f, err = os.Open("fonts/Droid/DroidSansFallback.ttf.stream")
		if err != nil {
			log.Fatal(err)
		}
		defer f.Close()

		reader = bufio.NewReader(f)
		f2 = pdfjet.NewFontStream1(pdf, reader)
	} else {
		// TODO:
		// Use standard TTF fonts
		f, err = os.Open("fonts/OpenSans/OpenSans-Regular.ttf")
		if err != nil {
			log.Fatal(err)
		}
		defer f.Close()
		reader := bufio.NewReader(f)
		f1 = pdfjet.NewFontStream1(pdf, reader)

		f, err = os.Open("fonts/Droid/DroidSansFallback.ttf")
		if err != nil {
			log.Fatal(err)
		}
		defer f.Close()

		reader = bufio.NewReader(f)
		f2 = pdfjet.NewFontStream1(pdf, reader)
	}
	f1.SetSize(12.0)
	f2.SetSize(12.0)

	page := pdfjet.NewPage(pdf, letter.Portrait, true)

	f1.SetSize(72.0)
	page.AddWatermark(f1, "This is a Draft")
	f1.SetSize(15.0)

	xPos := float32(70.0)
	yPos := float32(70.0)
	text := pdfjet.NewTextLine(f1, "")
	text.SetLocation(xPos, yPos)

	var buf strings.Builder
	for i := 0x20; i < 0x7F; i++ {
		if i%16 == 0 {
			text.SetText(buf.String())
			text.SetLocation(xPos, yPos)
			yPos += 24.0
			text.SetUnderline(true)
			text.DrawOn(page)
			buf.Reset()
		}
		buf.WriteRune(rune(i))
	}

	text.SetText(buf.String())
	text.SetLocation(xPos, yPos)
	yPos += 24.0
	text.DrawOn(page)

	yPos += 24.0
	buf.Reset()
	for i := 0x390; i < 0x3EF; i++ {
		if i%16 == 0 {
			text.SetText(buf.String())
			text.SetLocation(xPos, yPos)
			yPos += 24.0
			text.DrawOn(page)
			buf.Reset()
		}
		if i == 0x3A2 || i == 0x3CF || i == 0x3D0 || i == 0x3D3 ||
			i == 0x3D4 || i == 0x3D5 || i == 0x3D7 || i == 0x3D8 ||
			i == 0x3D9 || i == 0x3DA || i == 0x3DB || i == 0x3DC ||
			i == 0x3DD || i == 0x3DE || i == 0x3DF || i == 0x3E0 ||
			i == 0x3EA || i == 0x3EB || i == 0x3EC || i == 0x3ED ||
			i == 0x3EF {
			// Replace .notdef with space to generate PDF/A compliant PDF
			buf.WriteRune(0x0020)
		} else {
			buf.WriteRune(rune(i))
		}
	}

	yPos += 24.0
	buf.Reset()
	for i := 0x410; i <= 0x46F; i++ {
		if i%16 == 0 {
			text.SetText(buf.String())
			text.SetLocation(xPos, yPos)
			yPos += 24.0
			text.DrawOn(page)
			buf.Reset()
		}
		buf.WriteRune(rune(i))
	}

	xPos = 370.0
	yPos = 70.0
	text = pdfjet.NewTextLine(f2, "")
	text.SetLocation(xPos, yPos)
	buf.Reset()
	for i := 0x20; i < 0x7F; i++ {
		if i%16 == 0 {
			text.SetText(buf.String())
			text.SetLocation(xPos, yPos)
			yPos += 24.0
			text.DrawOn(page)
			buf.Reset()
		}
		buf.WriteRune(rune(i))
	}
	text.SetText(buf.String())
	text.SetLocation(xPos, yPos)
	yPos += 24.0
	text.DrawOn(page)

	yPos += 24.0
	buf.Reset()
	for i := 0x390; i < 0x3EF; i++ {
		if i%16 == 0 {
			text.SetText(buf.String())
			text.SetLocation(xPos, yPos)
			yPos += 24.0
			text.DrawOn(page)
			buf.Reset()
		}
		if i == 0x3A2 || i == 0x3CF || i == 0x3D0 || i == 0x3D3 ||
			i == 0x3D4 || i == 0x3D5 || i == 0x3D7 || i == 0x3D8 ||
			i == 0x3D9 || i == 0x3DA || i == 0x3DB || i == 0x3DC ||
			i == 0x3DD || i == 0x3DE || i == 0x3DF || i == 0x3E0 ||
			i == 0x3EA || i == 0x3EB || i == 0x3EC || i == 0x3ED ||
			i == 0x3EF {
			// Replace .notdef with space to generate PDF/A compliant PDF
			buf.WriteRune(0x0020)
		} else {
			buf.WriteRune(rune(i))
		}
	}

	yPos += 24.0
	buf.Reset()
	for i := 0x410; i < 0x46F; i++ {
		if i%16 == 0 {
			text.SetText(buf.String())
			text.SetLocation(xPos, yPos)
			yPos += 24.0
			text.DrawOn(page)
			buf.Reset()
		}
		buf.WriteRune(rune(i))
	}

	pdf.Complete()

	f.Close()
}

func main() {
	start := time.Now()
	Example07("stream")
	elapsed := time.Since(start).String()
	fmt.Printf("Example_07 => %s\n", elapsed[:strings.Index(elapsed, ".")])
}
