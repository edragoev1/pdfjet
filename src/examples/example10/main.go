package main

import (
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/src/alignment"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example10 shows how to lay out paragraphs in a text column.
func Example10() {
	pdf := pdfjet.NewPDFFile("Example_10.pdf")

	image1 := pdfjet.NewImageFromFile(pdf, "images/sz-map.png")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f1.SetSize(10.0)

	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f2.SetSize(14.0)

	f3 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f3.SetSize(12.0)

	f4 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Italic)
	f4.SetSize(10.0)

	page := pdfjet.NewPage(pdf, letter.Portrait)

	image1.SetLocation(90.0, 35.0)
	image1.ScaleBy(0.75)
	image1.DrawOn(page)

	rotate := 0
	// rotate := 90
	// rotate := 270
	column := pdfjet.NewTextColumn(rotate)
	column.SetLineSpacing(1.3)      // 1.3 x font height
	column.SetParagraphSpacing(1.0) // 1.0 x line spacing

	p1 := pdfjet.NewParagraph()
	p1.SetAlignment(alignment.Center)
	p1.Add(pdfjet.NewTextLine(f2, "Switzerland"))

	p2 := pdfjet.NewParagraph()
	p2.Add(pdfjet.NewTextLine(f2, "Introduction"))

	var buf strings.Builder
	buf.WriteString("The Swiss Confederation was founded in 1291 as a defensive ")
	buf.WriteString("alliance among three cantons. In succeeding years, other ")
	buf.WriteString("localities joined the original three. ")
	buf.WriteString("The Swiss Confederation secured its independence from the ")
	buf.WriteString("Holy Roman Empire in 1499. Switzerland's sovereignty and ")
	buf.WriteString("neutrality have long been honored by the major European ")
	buf.WriteString("powers, and the country was not involved in either of the ")
	buf.WriteString("two World Wars. The political and economic integration of ")
	buf.WriteString("Europe over the past half century, as well as Switzerland's ")
	buf.WriteString("role in many UN and international organizations, has ")
	buf.WriteString("strengthened Switzerland's ties with its neighbors. ")
	buf.WriteString("However, the country did not officially become a UN member ")
	buf.WriteString("until 2002.")

	p3 := pdfjet.NewParagraph()
	// p3.SetAlignment(align.Left)
	// p3.SetAlignment(align.Right)
	p3.SetAlignment(alignment.Justify)
	text := pdfjet.NewTextLine(f1, buf.String())
	p3.Add(text)

	buf.Reset()
	buf.WriteString("Switzerland remains active in many UN and international ")
	buf.WriteString("organizations but retains a strong commitment to neutrality.")

	text = pdfjet.NewTextLine(f1, buf.String())
	text.SetTextColor(color.Red)
	p3.Add(text)

	p4 := pdfjet.NewParagraph()
	p4.Add(pdfjet.NewTextLine(f3, "Economy"))

	buf.Reset()
	buf.WriteString("Switzerland is a peaceful, prosperous, and stable modern ")
	buf.WriteString("market economy with low unemployment, a highly skilled ")
	buf.WriteString("labor force, and a per capita GDP larger than that of the ")
	buf.WriteString("big Western European economies. The Swiss in recent years ")
	buf.WriteString("have brought their economic practices largely into ")
	buf.WriteString("conformity with the EU's to enhance their international ")
	buf.WriteString("competitiveness. Switzerland remains a safe haven for ")
	buf.WriteString("investors, because it has maintained a degree of bank secrecy ")
	buf.WriteString("and has kept up the franc's long-term external value. ")
	buf.WriteString("Reflecting the anemic economic conditions of Europe, GDP ")
	buf.WriteString("growth stagnated during the 2001-03 period, improved during ")
	buf.WriteString("2004-05 to 1.8% annually and to 2.9% in 2006.")

	p5 := pdfjet.NewParagraph()
	p5.SetAlignment(alignment.Justify)
	text = pdfjet.NewTextLine(f1, buf.String())
	p5.Add(text)

	text = pdfjet.NewTextLine(f4,
		"Even so, unemployment has remained at less than half the EU average.")
	text.SetTextColor(color.Blue)
	p5.Add(text)

	column.AddParagraph(p1)
	column.AddParagraph(p2)
	column.AddParagraph(p3)
	column.AddParagraph(p4)
	column.AddParagraph(p5)

	if rotate == 0 {
		column.SetLocation(90.0, 300.0)
	} else if rotate == 90 {
		column.SetLocation(90.0, 780.0)
	} else if rotate == 270 {
		column.SetLocation(550.0, 310.0)
	}

	columnWidth := float32(470.0)
	column.SetSize(columnWidth, 100.0)
	xy := column.DrawOn(page)

	if rotate == 0 {
		line := pdfjet.NewLine(
			xy[0],
			xy[1],
			xy[0]+columnWidth,
			xy[1])
		line.DrawOn(page)
	}

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example10()
	pdfjet.PrintDuration("Example_10", time.Since(start))
}
