package main

import (
	"fmt"
	"strconv"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/a4"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/corefont"
)

// Example41 -- TODO:
func Example41() {
	pdf := pdfjet.NewPDFFile("Example_41.pdf", compliance.PDF15)

	f1 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())
	f2 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	f3 := pdfjet.NewCoreFont(pdf, corefont.HelveticaOblique())

	f1.SetSize(10.0)
	f2.SetSize(10.0)
	f3.SetSize(10.0)

	page := pdfjet.NewPage(pdf, a4.Portrait)

	// paragraphs := make([]*pdfjet.Paragraph, 0)

	// paragraph := pdfjet.NewParagraph()
	// paragraph.Add(pdfjet.NewTextLine(f1,
	// 	"The small business centres offer practical resources, from step-by-step info on setting up your business to sample business plans to a range of business-related articles and books in our resource libraries.").SetUnderline(true))
	// paragraph.Add(pdfjet.NewTextLine(f2, "This text is bold!").SetColor(color.Blue))
	// paragraphs = append(paragraphs, paragraph)

	// paragraph = pdfjet.NewParagraph()
	// paragraph.Add(pdfjet.NewTextLine(f1,
	// 	"The centres also offer free one-on-one consultations with business advisors who can review your business plan and make recommendations to improve it.").SetUnderline(true))
	// paragraph.Add(pdfjet.NewTextLine(f3, "This text is using italic font.").SetColor(color.Green))
	// paragraphs = append(paragraphs, paragraph)

	paragraphs := pdfjet.ParagraphsFromFile(f1, "data/physics.txt")
	colorMap := make(map[string]int32)
	colorMap["Physics"] = color.Red
	colorMap["physics"] = color.Red
	colorMap["Experimentation"] = color.Orange
	f2size := f2.GetSize()
	for _, p := range paragraphs {
		if p.StartsWith("**") {
			f2.SetSize(24.0)
			p.GetTextLines()[0].SetFont(f2)
			p.GetTextLines()[0].SetColor(color.Navy)
		} else {
			p.SetColor(color.Gray)
			p.SetColorMap(colorMap)
		}
	}
	f2.SetSize(f2size)

	text := pdfjet.NewText(paragraphs)
	text.SetLocation(70.0, 90.0)
	text.SetWidth(500.0)
	// text.SetBorder(true)
	text.DrawOn(page)

	paragraphNumber := 1
	for _, p := range paragraphs {
		if p.StartsWith("**") {
			paragraphNumber = 1
		} else {
			textLine := pdfjet.NewTextLine(f2, strconv.Itoa(paragraphNumber)+".")
			textLine.SetLocation(p.GetX1()-15.0, p.GetY1())
			textLine.DrawOn(page)
			paragraphNumber++
		}
	}

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example41()
	elapsed := time.Since(start)
	fmt.Printf("Example_41 => %dµs\n", elapsed.Microseconds())
}
