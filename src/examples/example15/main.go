package main

import (
	"fmt"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/letter"
)

func Example15() {
	pdf := pdfjet.NewPDFFile("Example_15.pdf", compliance.PDF15)

	f1 := pdfjet.NewFontFromFile(pdf, "fonts/Droid/DroidSans.ttf.stream")
	f2 := pdfjet.NewFontFromFile(pdf, "fonts/Droid/DroidSansFallback.ttf.stream")

	f1.SetSize(12.0)
	f2.SetSize(12.0)

	page := pdfjet.NewPage(pdf, letter.Portrait)

	colors := make(map[string]int32)
	colors["Lorem"] = color.Blue
	colors["ipsum"] = color.Red
	colors["dolor"] = color.Green
	colors["ullamcorper"] = color.Gray

	textBox := pdfjet.NewTextBox(f1)
	textBox.SetFallbackFont(f2)
	textBox.SetText("保健所によると、女性は１３日に旅行先のタイから札幌に戻り、１６日午後５～８時ごろ同店を訪れ、帰宅後に発熱などの症状 が出て、２３日に医療機関ではしかと診断された。はしかのウイルスは発症日の１日前から感染者の呼吸などから放出され、本人がいなくなっても、２時間>程度空気中に漂い、空気感染する。保健所は１６日午後５～１１時に同店を訪れた人に、発熱などの異常が出た場合、早期にマスクをして医療機関を受診す>るよう呼びかけている。（本郷由美子）")
	textBox.SetLocation(50.0, 50.0)
	textBox.SetMargin(20.0)
	textBox.SetWidth(300.0)
	textBox.SetBgColor(color.Lightblue)
	textBox.SetTextColors(colors)
	xy := textBox.DrawOn(page)

	box := pdfjet.NewBox()
	box.SetLocation(xy[0], xy[1])
	box.SetSize(20.0, 20.0)
	box.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example15()
	elapsed := time.Since(start)
	fmt.Printf("Example_15 => %.2fms\n", float32(elapsed.Microseconds())/float32(1000.0))
}
