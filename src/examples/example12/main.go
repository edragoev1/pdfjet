package main

import (
	"bufio"
	"fmt"
	"io/ioutil"
	"log"
	"os"
	"github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/corefont"
	"github.com/edragoev1/pdfjet/src/letter"
	"strings"
	"time"
)

// Example12 constructs and draws PDF417 barcode.
func Example12() {
	f, err := os.Create("Example_12.pdf")
	if err != nil {
		log.Fatal(err)
	}
	w := bufio.NewWriter(f)

	pdf := pdfjet.NewPDF(w, compliance.PDF15)

	font := pdfjet.NewCoreFont(pdf, corefont.Helvetica())

	page := pdfjet.NewPage(pdf, letter.Portrait, true)

	var buf strings.Builder
	content, err := ioutil.ReadFile("examples/Example_12.java")
	if err != nil {
		log.Fatal(err)
	}
	lines := strings.Split(string(content), "\n")
	for i := 0; i < len(lines); i++ {
		buf.WriteString(lines[i])
		// Both CR and LF are required by the scanner!
		if i < len(lines)-1 {
			buf.WriteString("\r\n")
		}
	}

	code2D := pdfjet.NewBarCode2D(buf.String())
	code2D.SetModuleWidth(0.5)
	code2D.SetLocation(100.0, 60.0)
	code2D.DrawOn(page)
	/*
	   box := pdfjet.NewBox()
	   box.SetLocation(xy[0], xy[1])
	   box.SetSize(20.0, 20.0)
	   box.DrawOn(page)
	*/
	textLine := pdfjet.NewTextLine(font,
		"PDF417 barcode containing the program that created it.")
	textLine.SetLocation(100.0, 40.0)
	textLine.DrawOn(page)

	pdf.Complete()

	f.Close()
}

func main() {
	start := time.Now()
	Example12()
	elapsed := time.Since(start).String()
	fmt.Printf("Example_12 => %s\n", elapsed[:strings.Index(elapsed, ".")])
}
