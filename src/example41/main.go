package main

import (
	"a4"
	"bufio"
	"corefont"
	"fmt"
	"log"
	"os"
	"pdfjet"
	"pdfjet/src/color"
	"pdfjet/src/compliance"
	"strconv"
	"strings"
	"time"
)

// Example41 -- TODO:
func Example41() {
	file, err := os.Create("Example_41.pdf")
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	w := bufio.NewWriter(file)

	pdf := pdfjet.NewPDF(w, compliance.PDF15)

	f1 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())
	f2 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	f3 := pdfjet.NewCoreFont(pdf, corefont.HelveticaOblique())

	f1.SetSize(10.0)
	f2.SetSize(10.0)
	f3.SetSize(10.0)

	page := pdfjet.NewPage(pdf, a4.Portrait, true)

	paragraphs := make([]*pdfjet.Paragraph, 0)

	paragraph := pdfjet.NewParagraph()
	paragraph.Add(pdfjet.NewTextLine(f1,
		"The small business centres offer practical resources, from step-by-step info on setting up your business to sample business plans to a range of business-related articles and books in our resource libraries.").SetUnderline(true))
	paragraph.Add(pdfjet.NewTextLine(f2, "This text is bold!").SetColor(color.Blue))
	paragraphs = append(paragraphs, paragraph)

	paragraph = pdfjet.NewParagraph()
	paragraph.Add(pdfjet.NewTextLine(f1,
		"The centres also offer free one-on-one consultations with business advisors who can review your business plan and make recommendations to improve it.").SetUnderline(true))
	paragraph.Add(pdfjet.NewTextLine(f3, "This text is using italic font.").SetColor(color.Green))
	paragraphs = append(paragraphs, paragraph)

	text := pdfjet.NewText(paragraphs)
	text.SetLocation(70.0, 90.0)
	text.SetWidth(500.0)
	text.DrawOn(page)

	beginParagraphPoints := text.GetBeginParagraphPoints()
	paragraphNumber := 1

	for i := 0; i < len(beginParagraphPoints); i++ {
		point := beginParagraphPoints[i]
		textLine := pdfjet.NewTextLine(f1, strconv.Itoa(paragraphNumber)+".")
		textLine.SetLocation(point[0]-30.0, point[1])
		textLine.DrawOn(page)
		paragraphNumber++
	}

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example41()
	elapsed := time.Since(start).String()
	fmt.Printf("Example_41 => %s\n", elapsed[:strings.Index(elapsed, ".")])
}
