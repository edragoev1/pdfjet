package main

import (
	"regexp"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/src/content"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example47 -- TODO:
func Example47() {
	pdf := pdfjet.NewPDFFile("Example_47.pdf")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f1.SetSize(14.0)

	text := content.OfTextFile("data/dostoevsky.txt")
	re := regexp.MustCompile(`\n\n`)
	splitContent := re.Split(text, -1)

	// Create slice of paragraphs
	paragraphs := make([]string, len(splitContent))
	copy(paragraphs, splitContent)

	x := float32(50)
	y := float32(50)
	w := float32(230)
	h := float32(500)
	gap := float32(20)

	textFrame := pdfjet.NewTextFrame(f1, paragraphs)

	var page *pdfjet.Page
	for textFrame.HasMoreText() {
		page = pdfjet.NewPage(pdf, letter.Landscape)

		textFrame.SetLocation(x, y)
		textFrame.SetWidth(w)
		textFrame.SetHeight(h)
		_, err := textFrame.DrawOn(page)
		if err != nil {
			return
		}

		if textFrame.HasMoreText() {
			x += w + gap
			textFrame.SetLocation(x, y)
			textFrame.SetWidth(w)
			textFrame.SetHeight(h)
			_, err := textFrame.DrawOn(page)
			if err != nil {
				return
			}
		}

		if textFrame.HasMoreText() {
			x += w + gap
			textFrame.SetLocation(x, y)
			textFrame.SetWidth(w)
			textFrame.SetHeight(h)
			_, err := textFrame.DrawOn(page)
			if err != nil {
				return
			}
		}

		x = 50
		y = 50
	}

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example47()
	pdfjet.PrintDuration("Example_47", time.Since(start))
}
