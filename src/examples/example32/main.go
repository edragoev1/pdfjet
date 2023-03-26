package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/a4"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/corefont"
)

// Example32 -- TODO:
func Example32() {
	x := float32(50.0)
	y := float32(50.0)

	pdf := pdfjet.NewPDFFile("Example_32.pdf", compliance.PDF15)

	font := pdfjet.NewCoreFont(pdf, corefont.Courier())
	font.SetSize(8.0)
	leading := font.GetBodyHeight()

	file2, err := os.Open("examples/Example_02.java")
	if err != nil {
		log.Fatal(err)
	}
	defer file2.Close()

	colors := make(map[string]int32)

	page := pdfjet.NewPageAddTo(pdf, a4.Portrait)
	scanner := bufio.NewScanner(file2)
	for scanner.Scan() {
		line := scanner.Text()
		page.DrawStringUsingColorMap(font, nil, line, x, y, colors)
		y += leading
		if y > (page.GetHeight() - 20.0) {
			page = pdfjet.NewPageAddTo(pdf, a4.Portrait)
			y = 50.0
		}
	}
	if err := scanner.Err(); err != nil {
		log.Fatal(err)
	}

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example32()
	elapsed := time.Since(start)
	fmt.Printf("Example_32 => %dµs\n", elapsed.Microseconds())
}
