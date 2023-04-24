package main

import (
	"fmt"
	"log"
	"os"
	"strconv"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example01 --  TODO: Add proper description.
func Example01(mode string) {
	pdf := pdfjet.NewPDFFile("Example_01.pdf", 0)

	font1 := pdfjet.NewFontFromFile(pdf, "fonts/Droid/DroidSans.ttf.stream")
	font2 := pdfjet.NewFontFromFile(pdf, "fonts/Droid/DroidSansFallback.ttf.stream")

	font1.SetSize(12.0)
	font2.SetSize(12.0)

	page := pdfjet.NewPage(pdf, letter.Portrait)

	textLine := pdfjet.NewTextLine(font1, "Happy New Year!")
	textLine.SetLocation(70.0, 70.0)
	textLine.DrawOn(page)

	textLine = pdfjet.NewTextLine(font1, "С Новым Годом!")
	textLine.SetLocation(70.0, 100.0)
	textLine.DrawOn(page)

	textLine = pdfjet.NewTextLine(font1, "Ευτυχισμένο το Νέο Έτος!")
	textLine.SetLocation(70.0, 130.0)
	textLine.DrawOn(page)

	textLine = pdfjet.NewTextLine(font1, "新年快樂！")
	textLine.SetFallbackFont(font2)
	textLine.SetLocation(300.0, 70.0)
	textLine.DrawOn(page)

	textLine = pdfjet.NewTextLine(font1, "新年快乐！")
	textLine.SetFallbackFont(font2)
	textLine.SetLocation(300.0, 100.0)
	textLine.DrawOn(page)

	textLine = pdfjet.NewTextLine(font1, "明けましておめでとうございます！")
	textLine.SetFallbackFont(font2)
	textLine.SetLocation(300.0, 130.0)
	textLine.DrawOn(page)

	textLine = pdfjet.NewTextLine(font1, "새해 복 많이 받으세요!")
	textLine.SetFallbackFont(font2)
	textLine.SetLocation(300.0, 160.0)
	textLine.DrawOn(page)

	page = pdfjet.NewPage(pdf, letter.Portrait)

	lcgText, err := os.ReadFile("data/LCG.txt")
	if err != nil {
		log.Fatal(err)
	}

	paragraphs := make([]*pdfjet.Paragraph, 0)
	lines := strings.Split(string(lcgText), "\n")
	textline := pdfjet.NewTextLine(font1, "")
	for i, line := range lines {
		if line == "" {
			continue
		}
		paragraph := pdfjet.NewParagraph()
		textline = pdfjet.NewTextLine(font1, line)
		paragraph.Add(textline)
		if i == 0 {
			textLine := pdfjet.NewTextLine(font1,
				"Hello, World! This is a test to check if this line will be wrapped around properly.")
			textLine.SetColor(color.Blue)
			textLine.SetUnderline(true)
			paragraph.Add(textLine)

			textLine = pdfjet.NewTextLine(font1, "This is a test!")
			textLine.SetColor(color.Oldgloryred)
			textLine.SetUnderline(true)
			paragraph.Add(textLine)
		}
		paragraphs = append(paragraphs, paragraph)
	}

	text := pdfjet.NewText(paragraphs)
	text.SetLocation(50.0, 50.0)
	text.SetWidth(500.0)
	xy := text.DrawOn(page)

	size := text.GetSize()
	box := pdfjet.NewBox()
	box.SetLocation(50.0, 50.0)
	box.SetSize(size[0], size[1])
	box.DrawOn(page)

	box = pdfjet.NewBox()
	box.SetLocation(xy[0], xy[1])
	box.SetSize(20.0, 20.0)
	box.DrawOn(page)

	paragraphNumber := 1
	for _, p := range paragraphs {
		if p.StartsWith("**") {
			paragraphNumber = 1
		} else {
			textLine := pdfjet.NewTextLine(font1, strconv.Itoa(paragraphNumber)+".")
			textLine.SetLocation(p.GetX1()-15.0, p.GetY1())
			textLine.DrawOn(page)
			paragraphNumber++
		}
	}

	page = pdfjet.NewPage(pdf, letter.Portrait)

	cjkText, err := os.ReadFile("data/CJK.txt")
	if err != nil {
		log.Fatal(err)
	}

	paragraphs = make([]*pdfjet.Paragraph, 0)
	lines = strings.Split(string(cjkText), "\n")
	for _, line := range lines {
		if line == "" {
			continue
		}
		paragraph := pdfjet.NewParagraph()
		textline = pdfjet.NewTextLine(font1, line)
		textline.SetFallbackFont(font2)
		paragraph.Add(textline)
		paragraphs = append(paragraphs, paragraph)
	}

	text = pdfjet.NewText(paragraphs)
	text.SetLocation(50.0, 50.0)
	text.SetWidth(500.0)
	text.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example01("stream")
	elapsed := time.Since(start)
	fmt.Printf("Example_01 => %.1fms\n", float32(elapsed.Microseconds())/float32(1000.0))
}
