package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/cjkfont"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example04 draws Chinese, Japanese and Korean text with the CJK fonts, and
// Latin text with Courier. None of these fonts is embedded: the PDF names them
// and the viewer supplies them.
//
// The advantage is size and speed. A CJK font holds tens of thousands of
// glyphs, and this document carries none of them, so it is a few kilobytes
// and is written in a moment. The disadvantages: the viewer must have the
// Adobe Asian font packs, or a substitute, and the text takes the shapes and
// widths of whatever font it finds, so the document does not look the same
// everywhere; the core font Courier is limited to the WinAnsi characters; and
// a document with a font that is not embedded cannot claim PDF/A or PDF/UA
// compliance. To ship the glyphs with the document, use an embedded font like
// IBMPlexSansJP, KR, SC or TC, as Example_02 and 19 do.
//
// See: pdfjet.NewCJKFont
func Example04() {
	pdf, err := pdfjet.NewPDFFile("Example_04.pdf")
	if err != nil {
		log.Fatal(err)
	}

	f0 := pdfjet.NewCoreFont(pdf, corefont.Courier())
	f0.SetSize(14.0)

	// Chinese (Traditional) font
	// Uses Adobe's Ming Standard Light font (明體)
	f1 := pdfjet.NewCJKFont(pdf, cjkfont.AdobeMingStdLight)
	f1.SetSize(14.0)

	// Chinese (Simplified) font
	// Uses Adobe's Heiti SC Light font (黑体-简)
	f2 := pdfjet.NewCJKFont(pdf, cjkfont.STHeitiSCLight)
	f2.SetSize(14.0)

	// Japanese font
	// Uses Kozuka Mincho Pro VI Regular font (小塚明朝)
	f3 := pdfjet.NewCJKFont(pdf, cjkfont.KozMinProVIRegular)
	f3.SetSize(14.0)

	// Korean font
	// Uses Adobe's Myungjo Standard Medium font (명조체)
	f4 := pdfjet.NewCJKFont(pdf, cjkfont.AdobeMyungjoStdMedium)
	f4.SetSize(14.0)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	var xPos float32 = 100.0
	var yPos float32
	yPos = 100.0

	file, err := os.Open("data/happy-new-year.txt")
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	reader := bufio.NewScanner(file)
	text := pdfjet.NewTextLine(f0, "")
	for reader.Scan() {
		line := reader.Text()
		text.SetText(line)
		text.SetLocation(xPos, yPos)
		text.DrawOn(page)
		if strings.Contains(line, "Traditional") {
			text.SetFont(f1)
		} else if strings.Contains(line, "Simplified") {
			text.SetFont(f2)
		} else if strings.Contains(line, "Japanese") {
			text.SetFont(f3)
		} else if strings.Contains(line, "Korean") {
			text.SetFont(f4)
		} else {
			text.SetFont(f0)
		}
		yPos += 25.0
	}
	if err := reader.Err(); err != nil {
		log.Fatal(err)
	}

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example04()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_04 => %4d ms\n", time1-time0)
}
