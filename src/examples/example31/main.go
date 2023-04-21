package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/border"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example31 -- TODO:
func Example31() {
	pdf := pdfjet.NewPDFFile("Example_31.pdf", compliance.PDF15)
	pdf.SetTitle("Hello")
	pdf.SetAuthor("Eugene")
	pdf.SetSubject("Example")
	pdf.SetKeywords("Hello World This is a test")
	pdf.SetCreator("Application Name")

	font1 := pdfjet.NewFontFromFile(pdf, "fonts/Noto/NotoSansDevanagari-Regular.ttf.stream")
	font2 := pdfjet.NewFontFromFile(pdf, "fonts/Droid/DroidSans.ttf.stream")

	font1.SetSize(15.0)
	font2.SetSize(15.0)

	page := pdfjet.NewPage(pdf, letter.Portrait)

	file3, err := os.Open("data/marathi.txt")
	if err != nil {
		log.Fatal(err)
	}
	defer file3.Close()

	var buf strings.Builder
	scanner := bufio.NewScanner(file3)
	for scanner.Scan() {
		buf.WriteString(scanner.Text())
	}
	if err := scanner.Err(); err != nil {
		log.Fatal(err)
	}

	textBox := pdfjet.NewTextBox(font1)
	textBox.SetText(buf.String())
	textBox.SetLocation(500.0, 300.0)
	textBox.SetFallbackFont(font2)
	textBox.SetLocation(50.0, 50.0)
	textBox.SetBorder(border.Left)
	textBox.SetBorder(border.Right)
	textBox.DrawOn(page)

	str := "असम के बाद UP में भी CM कैंडिडेट का ऐलान करेगी BJP?"
	textLine := pdfjet.NewTextLine(font1, str)
	textLine.SetFallbackFont(font2)
	textLine.SetLocation(50.0, 175.0)
	textLine.DrawOn(page)

	page.SetPenColor(color.Blue)
	page.SetBrushColor(color.Blue)
	page.FillRect(50.0, 200.0, 200.0, 200.0)

	gs := pdfjet.NewGraphicsState()
	gs.SetAlphaStroking(0.5)    // The stroking alpha constant
	gs.SetAlphaNonStroking(0.5) // The nonstroking alpha constant
	page.SetGraphicsState(gs)

	page.SetPenColor(color.Green)
	page.SetBrushColor(color.Green)
	page.FillRect(100.0, 250.0, 200.0, 200.0)

	page.SetPenColor(color.Red)
	page.SetBrushColor(color.Red)
	page.FillRect(150.0, 300.0, 200.0, 200.0)

	// Reset the parameters to the default values
	page.SetGraphicsState(pdfjet.NewGraphicsState())

	page.SetPenColor(color.Orange)
	page.SetBrushColor(color.Orange)
	page.FillRect(200.0, 350.0, 200.0, 200.0)

	page.SetBrushColor(0x00003865)
	page.FillRect(50.0, 550.0, 200.0, 200.0)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example31()
	elapsed := time.Since(start)
	fmt.Printf("Example_31 => %dµs\n", elapsed.Microseconds())
}
