package main

import (
	"regexp"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/content"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example47 flows text through columns using the TextFrame class.
func Example47() {
	pdf := pdfjet.NewPDFFile("Example_47.pdf")
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Text flowing through columns")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f1.SetSize(14.0)

	// Use regexp.Split to match Java's split("\\n\\n") regex behavior
	re := regexp.MustCompile(`\n\n`)
	paragraphs := re.Split(content.OfTextFile("data/dostoevsky.txt"), -1)

	x := float32(50.0)
	y := float32(50.0)
	w := float32(230.0)
	h := float32(500.0)
	gap := float32(20.0)

	textFrame := pdfjet.NewTextFrame(f1, paragraphs)

	var page *pdfjet.Page
	for textFrame.HasMoreText() {
		page = pdfjet.NewPage(pdf, letter.Landscape)

		textFrame.SetLocation(x, y)
		textFrame.SetWidth(w)
		textFrame.SetHeight(h)
		textFrame.DrawOn(page)

		if textFrame.HasMoreText() {
			x += w + gap
			textFrame.SetLocation(x, y)
			textFrame.SetWidth(w)
			textFrame.SetHeight(h)
			textFrame.DrawOn(page)
		}

		if textFrame.HasMoreText() {
			x += w + gap
			textFrame.SetLocation(x, y)
			textFrame.SetWidth(w)
			textFrame.SetHeight(h)
			textFrame.DrawOn(page)
		}

		x = 50.0
		y = 50.0
	}

	pdf.Complete()
}

func main() {
	time0 := time.Now().UnixMilli()
	Example47()
	time1 := time.Now().UnixMilli()
	pdfjet.PrintDuration("Example_47", time0, time1)
}
