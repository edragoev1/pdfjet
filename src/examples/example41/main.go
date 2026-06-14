package main

import (
	"strconv"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/a4"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/corefont"
)

// Example41 -- TODO:
func Example41() {
	pdf := pdfjet.NewPDFFile("Example_41.pdf")

	f1 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())
	f1.SetSize(10.0)

	f2 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	f2.SetSize(10.0)

	f3 := pdfjet.NewCoreFont(pdf, corefont.HelveticaOblique())
	f3.SetSize(10.0)

	page := pdfjet.NewPage(pdf, a4.Portrait)

	paragraphs := make([]*pdfjet.Paragraph, 0)

	paragraph := pdfjet.NewParagraph()
	paragraph.Add(pdfjet.NewTextLine(f1,
		"The small business centres offer practical resources, from step-by-step info on setting up your business to sample business plans to a range of business-related articles and books in our resource libraries.").SetUnderline(true))
	paragraph.Add(pdfjet.NewTextLine(f2, "This text is bold!").SetTextColor(color.Blue))
	paragraphs = append(paragraphs, paragraph)

	paragraph = pdfjet.NewParagraph()
	paragraph.Add(pdfjet.NewTextLine(f1,
		"The centres also offer free one-on-one consultations with business advisors who can review your business plan and make recommendations to improve it.").SetUnderline(true))
	paragraph.Add(pdfjet.NewTextLine(f3, "This text is using italic font.").SetTextColor(color.Green))
	paragraphs = append(paragraphs, paragraph)

	text := pdfjet.NewText(paragraphs)
	text.SetLocation(70.0, 50.0)
	text.SetWidth(500.0)
	text.SetBorderColor(color.Blue)
	text.DrawOn(page)

	paragraphs = pdfjet.ParagraphsFromFile(f1, "data/physics.txt")
	colorMap := make(map[string]int32)
	colorMap["Physics"] = color.Red
	colorMap["physics"] = color.Red
	colorMap["Experimentation"] = color.Orange
	colorMap["science"] = color.Blue
	f2size := f2.GetSize()
	for _, p := range paragraphs {
		if p.StartsWith("**") {
			f2.SetSize(24.0)
			p.GetTextLines()[0].SetFont(f2)
			p.GetTextLines()[0].SetTextColor(color.Navy)
		} else {
			p.SetColor(color.Gray)
			p.SetColorMap(colorMap)
		}
	}
	f2.SetSize(f2size)

	text = pdfjet.NewText(paragraphs)
	text.SetLocation(70.0, 150.0)
	text.SetWidth(500.0)
	text.SetBorderColor(color.Blue)
	xy := text.DrawOn(page)

	rect := pdfjet.NewRect(xy[0], xy[1], 20.0, 20.0)
	rect.DrawOn(page)

	paragraphNumber := 1
	for _, p := range paragraphs {
		if p.StartsWith("**") {
			paragraphNumber = 1
		} else {
			textLine := pdfjet.NewTextLine(f2, strconv.Itoa(paragraphNumber)+".")
			textLine.SetLocation(p.GetTextX()-15.0, p.GetTextY())
			textLine.DrawOn(page)
			pdfjet.NewLine(
				p.GetX1()-3.0, p.GetY1(), p.GetX1()-3.0, p.GetY2()).SetColor(color.Navy).SetWidth(1.0).DrawOn(page)
			paragraphNumber++
		}
	}

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example41()
	pdfjet.PrintDuration("Example_41", time.Since(start))
}
