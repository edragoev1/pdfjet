package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/a4"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/corefont"
	"strings"
	"time"
)

// Example43 -- TODO:
func Example43() {
	file, err := os.Create("Example_43.pdf")
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	w := bufio.NewWriter(file)

	pdf := pdfjet.NewPDF(w, compliance.PDF15)

	// f1 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())
	f2 := pdfjet.NewCoreFont(pdf, corefont.HelveticaOblique())

	page := pdfjet.NewPage(pdf, a4.Portrait, true)

	paragraphs := make([]*pdfjet.Paragraph, 0)
	p1 := pdfjet.NewParagraph()
	tl1 := pdfjet.NewTextLine(f2,
		"The Swiss Confederation was founded in 1291 as a defensive alliance among three cantons. In succeeding years, other localities joined the original three. The Swiss Confederation secured its independence from the Holy Roman Empire in 1499. Switzerland's sovereignty and neutrality have long been honored by the major European powers, and the country was not involved in either of the two World Wars. The political and economic integration of Europe over the past half century, as well as Switzerland's role in many UN and international organizations, has strengthened Switzerland's ties with its neighbors. However, the country did not officially become a UN member until 2002.")
	p1.Add(tl1)

	p2 := pdfjet.NewParagraph()
	tl2 := pdfjet.NewTextLine(f2, "Even so, unemployment has remained at less than half the EU average.")
	p2.Add(tl2)

	paragraphs = append(paragraphs, p1)
	paragraphs = append(paragraphs, p2)

	text := pdfjet.NewText(paragraphs)
	text.SetLocation(50.0, 50.0)
	text.SetWidth(500.0)
	text.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example43()
	elapsed := time.Since(start).String()
	fmt.Printf("Example_43 => %s\n", elapsed[:strings.Index(elapsed, ".")])
}
