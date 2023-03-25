package main

import (
	"fmt"
	"log"
	"os"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example04 shows how to use CJK fonts.
func Example04() {
	pdf := pdfjet.NewPDFFile("Example_04.pdf", compliance.PDF15)

	f1 := pdfjet.NewCJKFont(pdf, "AdobeMingStd-Light")

	// Chinese (Simplified) font
	f2 := pdfjet.NewCJKFont(pdf, "STHeitiSC-Light")

	// Japanese font
	f3 := pdfjet.NewCJKFont(pdf, "KozMinProVI-Regular")

	// Korean font
	f4 := pdfjet.NewCJKFont(pdf, "AdobeMyungjoStd-Medium")

	page := pdfjet.NewPageAddTo(pdf, letter.Portrait)

	f1.SetSize(14.0)
	f2.SetSize(14.0)
	f3.SetSize(14.0)
	f4.SetSize(14.0)

	var xPos float32 = 100.0
	var yPos float32
	yPos = 100.0

	content, err := os.ReadFile("data/happy-new-year.txt")
	if err != nil {
		log.Fatal(err)
	}
	strContent := string(content)
	lines := strings.Split(strContent, "\n")
	text := pdfjet.NewTextLine(f1, "")
	for _, line := range lines {
		if strings.Contains(line, "Simplified") {
			text.SetFont(f2)
		} else if strings.Contains(line, "Japanese") {
			text.SetFont(f3)
		} else if strings.Contains(line, "Korean") {
			text.SetFont(f4)
		}
		text.SetText(line)
		text.SetLocation(xPos, yPos)
		text.DrawOn(page)
		yPos += 25.0
	}

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example04()
	elapsed := time.Since(start)
	fmt.Printf("Example_04 => %dµs\n", elapsed.Microseconds())
}
