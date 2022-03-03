package main

import (
	"bufio"
	"fmt"
	"io/ioutil"
	"log"
	"os"
	"github.com/edragoev1/pdfjet/src/pdfjet"
	"github.com/edragoev1/pdfjet/src/pdfjet/compliance"
	"github.com/edragoev1/pdfjet/src/pdfjet/letter"
	"strings"
	"time"
)

// Example04 shows how to use CJK fonts.
func Example04() {
	f, err := os.Create("Example_04.pdf")
	if err != nil {
		log.Fatal(err)
	}
	w := bufio.NewWriter(f)
	pdf := pdfjet.NewPDF(w, compliance.PDF15)

	f1 := pdfjet.NewCJKFont(pdf, "AdobeMingStd-Light")

	// Chinese (Simplified) font
	f2 := pdfjet.NewCJKFont(pdf, "STHeitiSC-Light")

	// Japanese font
	f3 := pdfjet.NewCJKFont(pdf, "KozMinProVI-Regular")

	// Korean font
	f4 := pdfjet.NewCJKFont(pdf, "AdobeMyungjoStd-Medium")

	page := pdfjet.NewPage(pdf, letter.Portrait, true)

	f1.SetSize(14.0)
	f2.SetSize(14.0)
	f3.SetSize(14.0)
	f4.SetSize(14.0)

	var xPos float32
	xPos = 100.0
	var yPos float32
	yPos = 100.0

	content, err := ioutil.ReadFile("data/happy-new-year.txt")
	if err != nil {
		log.Fatal(err)
	}
	strContent := string(content)
	lines := strings.Split(strContent, "\n")
	text := pdfjet.NewTextLine(f1, "")
	for _, line := range lines {
		if strings.Index(line, "Simplified") != -1 {
			text.SetFont(f2)
		} else if strings.Index(line, "Japanese") != -1 {
			text.SetFont(f3)
		} else if strings.Index(line, "Korean") != -1 {
			text.SetFont(f4)
		}
		text.SetText(line)
		text.SetLocation(xPos, yPos)
		text.DrawOn(page)
		yPos += 25.0
	}

	pdf.Complete()

	f.Close()
}

func main() {
	start := time.Now()
	Example04()
	elapsed := time.Since(start).String()
	fmt.Printf("Example_04 => %s\n", elapsed[:strings.Index(elapsed, ".")])
}
