package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example19 draws two images and three text blocks.
func Example19() {
	pdf := pdfjet.NewPDFFile("Example_19.pdf", compliance.PDF15)

	f1 := pdfjet.NewFontFromFile(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream")
	f2 := pdfjet.NewFontFromFile(pdf, "fonts/Droid/DroidSansFallback.ttf.stream")

	f1.SetSize(10.0)
	f2.SetSize(10.0)

	image1 := pdfjet.NewImageFromFile(pdf, "images/fruit.jpg")
	image2 := pdfjet.NewImageFromFile(pdf, "images/ee-map.png")

	page := pdfjet.NewPage(pdf, letter.Portrait)

	// Columns x coordinates
	x1 := float32(75.0)
	y1 := float32(75.0)
	x2 := float32(325.0)
	w2 := float32(200.0) // Width of the second column:

	// Draw the first image
	image1.SetLocation(x1, y1)
	image1.ScaleBy(0.75)
	image1.DrawOn(page)

	textBlock := pdfjet.NewTextBlock(f1, "")
	textBlock.SetText("Geometry arose independently in a number of early cultures as a practical way for dealing with lengths, areas, and volumes.")
	textBlock.SetLocation(x2, y1)
	textBlock.SetWidth(w2)
	textBlock.SetDrawBorder(true)
	// textBlock.SetTextAlignment(align.Right)
	// textBlock.SetTextAlignment(align.Center)
	xy := textBlock.DrawOn(page)

	// Draw the second image
	image2.SetLocation(x1, xy[1]+10.0)
	image2.ScaleBy(1.0 / 3.0)
	image2.DrawOn(page)

	textBlock = pdfjet.NewTextBlock(f1, "")
	textBlock.SetText("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla elementum interdum elit, quis vehicula urna interdum quis. Phasellus gravida ligula quam, nec blandit nulla. Sed posuere, lorem eget feugiat placerat, ipsum nulla euismod nisi, in semper mi nibh sed elit. Mauris libero est, sodales dignissim congue sed, pulvinar non ipsum. Sed risus nisi, ultrices nec eleifend at, viverra sed neque. Integer vehicula massa non arcu viverra ullamcorper. Ut id tellus id ante mattis commodo. Donec dignissim aliquam tortor, eu pharetra ipsum ullamcorper in. Vivamus ultrices imperdiet iaculis.\n\n")
	textBlock.SetLocation(x2, xy[1]+10.0)
	textBlock.SetWidth(w2)
	textBlock.SetDrawBorder(true)
	textBlock.DrawOn(page)

	textBlock = pdfjet.NewTextBlock(f1, "")
	textBlock.SetFallbackFont(f2)
	textBlock.SetText("保健所によると、女性は１３日に旅行先のタイから札幌に戻り、１６日午後５～８時ごろ同店を訪れ、帰宅後に発熱などの症状が出て、２３日に医療機関ではしかと診断された。はしかのウイルスは発症日の１日前から感染者の呼吸などから放出され、本人がいなくなっても、２時間程度空気中に漂い、空気感染する。保健所は１６日午後５～１１時に同店を訪れた人に、発熱などの異常が出た場合、早期にマスクをして医療機関を受診するよう呼びかけている。（本郷由美子）")
	textBlock.SetLocation(x1, 600.0)
	textBlock.SetWidth(350.0)
	textBlock.SetDrawBorder(true)
	textBlock.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example19()
	pdfjet.PrintDuration("Example_19", time.Since(start))
}
