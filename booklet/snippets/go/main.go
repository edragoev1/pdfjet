// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// The code of the PDFjet booklet: each function after an "@snippet" comment
// is one snippet, and the booklet shows its body. A snippet that takes a PDF
// and a font draws in a PDF of its own, named after it, with IBM Plex Sans at
// 12 points; one that takes nothing writes its own files. Run it in a folder
// with the fonts, images and data folders of the repository, as
// booklet/check-snippets.sh does.
package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"strconv"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSansArabic"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSansHebrew"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSansJP"
	"github.com/edragoev1/pdfjet/v9/src/JetBrainsMono"
	"github.com/edragoev1/pdfjet/v9/src/SourceSerif4"
	"github.com/edragoev1/pdfjet/v9/src/a4"
	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/border"
	"github.com/edragoev1/pdfjet/v9/src/cjkfont"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/content"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/datamatrix"
	"github.com/edragoev1/pdfjet/v9/src/encryption"
	"github.com/edragoev1/pdfjet/v9/src/errorcorrectionlevel"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/mark"
	"github.com/edragoev1/pdfjet/v9/src/pagesize"
	"github.com/edragoev1/pdfjet/v9/src/pdf417"
	"github.com/edragoev1/pdfjet/v9/src/qrcode"
	"github.com/edragoev1/pdfjet/v9/src/shape"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// --- Documents and pages ----------------------------------------------------

// @snippet document-info
func documentInfo(pdf *pdfjet.PDF, font *pdfjet.Font) {
	pdf.SetTitle("Annual Report 2026")
	pdf.SetAuthor("PDFjet Software")
	pdf.SetSubject("The year in numbers")
	pdf.SetKeywords("report, 2026")
	pdf.SetLanguage("en-US")
	page := pdfjet.NewPage(pdf, letter.Portrait())
	pdfjet.NewTextLine(font, "Annual Report 2026").SetLocation(50, 50).DrawOn(page)
}

// @snippet page-sizes
func pageSizes(pdf *pdfjet.PDF, font *pdfjet.Font) {
	page := pdfjet.NewPage(pdf, a4.Landscape())
	size := fmt.Sprintf("%d x %d points", int(page.GetWidth()), int(page.GetHeight()))
	pdfjet.NewTextLine(font, size).SetLocation(50, 50).DrawOn(page)
	page = pdfjet.NewPage(pdf, pagesize.NewPageSize(400, 300)) // Any width and height
	pdfjet.NewTextLine(font, "400 x 300 points").SetLocation(50, 50).DrawOn(page)
}

// @snippet page-numbers
func pageNumbers(pdf *pdfjet.PDF, font *pdfjet.Font) {
	pages := make([]*pdfjet.Page, 0)
	for i := 0; i < 3; i++ {
		page := pdfjet.NewPageDetached(pdf, letter.Portrait())
		pdfjet.NewTextLine(font, fmt.Sprintf("Chapter %d", i+1)).SetLocation(50, 50).DrawOn(page)
		pages = append(pages, page)
	}
	for i := 0; i < len(pages); i++ {
		footer := pdfjet.NewTextLine(font, fmt.Sprintf("Page %d of %d", i+1, len(pages)))
		pages[i].AddFooterOffsetBy(footer, 30) // The baseline 30 points from the bottom
	}
	pdf.AddPages(pages)
}

// @snippet watermark
func watermark(pdf *pdfjet.PDF, font *pdfjet.Font) {
	bold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold).SetSize(120)
	page := pdfjet.NewPage(pdf, letter.Portrait())
	page.AddWatermark(bold, "DRAFT")
	pdfjet.NewTextLine(font, "The text is drawn over the watermark.").
		SetLocation(50, 50).DrawOn(page)
}

// @snippet accessible-document
func accessibleDocument() {
	pdf, err := pdfjet.NewPDFFile("accessible.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("An accessible document")
	font := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	page := pdfjet.NewPage(pdf, letter.Portrait())
	pdfjet.NewTextLine(font, "A heading").SetFontSize(18).SetStructureType(structelem.H1).
		SetLocation(50, 50).DrawOn(page)
	pdfjet.NewTextBlock(font, "A paragraph, which a screen reader reads after the heading.").
		SetWidth(400).SetLocation(50, 70).DrawOn(page)
	image := pdfjet.NewImageFromFile(pdf, "images/linux-logo.png")
	image.SetAltDescription("Tux, the penguin of Linux")
	image.SetLocation(50, 110).DrawOn(page)
	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// @snippet archival-document
func archivalDocument() {
	pdf, err := pdfjet.NewPDFFile("archival.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_A_2B)
	pdf.SetTitle("An archival document")
	font := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	page := pdfjet.NewPage(pdf, letter.Portrait())
	pdfjet.NewTextLine(font, "This document will look the same in fifty years.").
		SetLocation(50, 50).DrawOn(page)
	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// --- Fonts ------------------------------------------------------------------

// @snippet fonts
func fonts(pdf *pdfjet.PDF, font *pdfjet.Font) {
	serif := pdfjet.NewFontFromFile(pdf, SourceSerif4.Regular).SetSize(14)
	bold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)
	mono := pdfjet.NewFontFromFile(pdf, JetBrainsMono.Regular).SetSize(10)
	page := pdfjet.NewPage(pdf, letter.Portrait())
	pdfjet.NewTextLine(serif, "Source Serif 4 at 14 points").SetLocation(50, 50).DrawOn(page)
	pdfjet.NewTextLine(bold, "IBM Plex Sans Bold at 12 points").SetLocation(50, 75).DrawOn(page)
	pdfjet.NewTextLine(mono, "JetBrains Mono at 10 points").SetLocation(50, 100).DrawOn(page)
	pdfjet.NewTextLine(bold, "The same font at 24 points").SetFontSize(24).
		SetLocation(50, 135).DrawOn(page)
}

// @snippet font-files
func fontFiles(pdf *pdfjet.PDF, font *pdfjet.Font) {
	otf := pdfjet.NewFontFromFile(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.otf")
	ttf := pdfjet.NewFontFromFile(pdf, "fonts/NotoSans/NotoSans-Regular.ttf")
	page := pdfjet.NewPage(pdf, letter.Portrait())
	pdfjet.NewTextLine(otf, "An OpenType font").SetLocation(50, 50).DrawOn(page)
	pdfjet.NewTextLine(ttf, "A TrueType font").SetLocation(50, 75).DrawOn(page)
}

// @snippet core-fonts
func coreFonts(pdf *pdfjet.PDF, font *pdfjet.Font) {
	helvetica := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold()).SetSize(24)
	page := pdfjet.NewPage(pdf, letter.Portrait())
	pdfjet.NewTextLine(helvetica, "WAVE AWAY").SetLocation(50, 50).DrawOn(page)
	helvetica.SetKernPairs(true)
	pdfjet.NewTextLine(helvetica, "WAVE AWAY").SetLocation(50, 85).DrawOn(page)
}

// @snippet cjk-fonts
func cjkFonts(pdf *pdfjet.PDF, font *pdfjet.Font) {
	chinese := pdfjet.NewCJKFont(pdf, cjkfont.STHeitiSCLight).SetSize(24)
	japanese := pdfjet.NewCJKFont(pdf, cjkfont.KozMinProVIRegular).SetSize(24)
	korean := pdfjet.NewCJKFont(pdf, cjkfont.AdobeMyungjoStdMedium).SetSize(24)
	page := pdfjet.NewPage(pdf, letter.Portrait())
	pdfjet.NewTextLine(chinese, "新年快乐!").SetLocation(50, 50).DrawOn(page)
	pdfjet.NewTextLine(japanese, "明けましておめでとう!").SetLocation(50, 90).DrawOn(page)
	pdfjet.NewTextLine(korean, "새해 복 많이 받으세요!").SetLocation(50, 130).DrawOn(page)
}

// @snippet fallback-font
func fallbackFont(pdf *pdfjet.PDF, font *pdfjet.Font) {
	japanese := pdfjet.NewFontFromFile(pdf, IBMPlexSansJP.Regular)
	page := pdfjet.NewPage(pdf, letter.Portrait())
	pdfjet.NewTextLine(font, "Japanese is 日本語 in Japanese.").
		SetFallbackFont(japanese).SetLocation(50, 50).DrawOn(page)
}

// @snippet font-metrics
func fontMetrics(pdf *pdfjet.PDF, font *pdfjet.Font) {
	page := pdfjet.NewPage(pdf, letter.Portrait())
	text := "Right aligned at x = 500"
	width := font.StringWidth(font.GetSize(), text)
	pdfjet.NewTextLine(font, text).SetLocation(500-width, 50).DrawOn(page)
	// The next line goes one line height of the font below.
	y := 50 + font.GetBodyHeight(font.GetSize())
	pdfjet.NewTextLine(font, "The next line").
		SetLocation(500-font.StringWidth(font.GetSize(), "The next line"), y).DrawOn(page)
}

// --- Text -------------------------------------------------------------------

// @snippet text-line
func textLine(pdf *pdfjet.PDF, font *pdfjet.Font) {
	page := pdfjet.NewPage(pdf, letter.Portrait())
	text := pdfjet.NewTextLine(font, "Hello, World!")
	text.SetFontSize(20)
	text.SetTextColor(color.Navy)
	text.SetUnderline(true)
	text.SetLocation(50, 50)
	xy := text.DrawOn(page)
	// DrawOn returns where the text ends: the next line starts after it.
	pdfjet.NewTextLine(font, "Turned by 15 degrees").SetTextRotation(15).
		SetLocation(xy[0]+20, 50).DrawOn(page)
}

// @snippet text-block
func textBlock(pdf *pdfjet.PDF, font *pdfjet.Font) {
	page := pdfjet.NewPage(pdf, letter.Portrait())
	block := pdfjet.NewTextBlock(font,
		"A text block wraps its text at its width. Its lines break between words, "+
			"and an empty line starts a new paragraph.\n\nLike this one.")
	block.SetLocation(50, 50)
	block.SetWidth(250)
	block.SetLineSpacing(1.4)
	block.SetPadding(10)
	block.SetBorderColor(color.Blue)
	xy := block.DrawOn(page)
	pdfjet.NewTextLine(font, "Below the block").SetLocation(50, xy[1]+20).DrawOn(page)
}

// @snippet highlighted-words
func highlightedWords(pdf *pdfjet.PDF, font *pdfjet.Font) {
	colors := map[string]int32{
		"PDFjet": color.Blue,
		"fast":   color.Red,
	}
	page := pdfjet.NewPage(pdf, letter.Portrait())
	pdfjet.NewTextBlock(font, "PDFjet is fast, and PDFjet is small.").
		SetHighlightColors(colors).SetWidth(400).SetLocation(50, 50).DrawOn(page)
}

// @snippet paragraphs
func paragraphs(pdf *pdfjet.PDF, font *pdfjet.Font) {
	bold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)
	italic := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Italic)
	paragraphs := make([]*pdfjet.Paragraph, 0)
	paragraphs = append(paragraphs, pdfjet.NewParagraph().
		Add(pdfjet.NewTextLine(font, "A paragraph mixes")).
		Add(pdfjet.NewTextLine(bold, "fonts,")).
		Add(pdfjet.NewTextLine(italic, "styles").SetTextColor(color.DarkRed)).
		Add(pdfjet.NewTextLine(font, "and colors, and wraps them as one text.")))
	paragraphs = append(paragraphs, pdfjet.NewParagraph().
		Add(pdfjet.NewTextLine(font, "Centered.")).SetTextAlignment(alignment.Center))
	frame := pdfjet.NewTextFrameFromParagraphs(paragraphs)
	frame.SetLocation(50, 50)
	frame.SetWidth(250)
	frame.DrawOn(pdfjet.NewPage(pdf, letter.Portrait()))
}

// @snippet lists
func lists(pdf *pdfjet.PDF, font *pdfjet.Font) {
	items := []string{"Create the PDF", "Draw on its pages", "Complete it"}
	paragraphs := make([]*pdfjet.Paragraph, 0)
	for i := 0; i < len(items); i++ {
		paragraphs = append(paragraphs, pdfjet.NewParagraph().
			Add(pdfjet.NewTextLine(font, items[i])).
			SetListLabel(pdfjet.NewTextLine(font, fmt.Sprintf("%d.", i+1)), 20))
	}
	pdfjet.NewTextFrameFromParagraphs(paragraphs).SetWidth(300).SetLocation(50, 50).
		DrawOn(pdfjet.NewPage(pdf, letter.Portrait()))
}

// @snippet markup
func markup(pdf *pdfjet.PDF, font *pdfjet.Font) {
	bold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)
	italic := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Italic)
	boldItalic := pdfjet.NewFontFromFile(pdf, IBMPlexSans.BoldItalic)
	code := pdfjet.NewFontFromFile(pdf, JetBrainsMono.Regular).SetSize(11)
	markup := pdfjet.NewMarkup(font, bold, italic, boldItalic, code)
	paragraphs := markup.Paragraphs(
		"Write **bold**, *italic* and `code`, and link to [PDFjet](https://pdfjet.com).\n\n" +
			"An empty line starts a new paragraph.")
	pdfjet.NewTextFrameFromParagraphs(paragraphs).SetWidth(300).SetLocation(50, 50).
		DrawOn(pdfjet.NewPage(pdf, letter.Portrait()))
}

// @snippet text-column
func textColumn(pdf *pdfjet.PDF, font *pdfjet.Font) {
	column := pdfjet.NewTextColumn()
	column.AddParagraph(pdfjet.NewParagraph().Add(pdfjet.NewTextLine(font,
		"A text column draws its paragraphs one under the other.")))
	column.AddParagraph(pdfjet.NewParagraph().Add(pdfjet.NewTextLine(font,
		"It justifies them, and puts space between them.")))
	column.SetLocation(50, 50)
	column.SetWidth(200)
	column.SetTextAlignment(alignment.Justify)
	column.SetParagraphSpacing(1.5)
	column.DrawOn(pdfjet.NewPage(pdf, letter.Portrait()))
}

// @snippet text-frame-pages
func textFramePages(pdf *pdfjet.PDF, font *pdfjet.Font) {
	text := strings.Split(content.OfTextFile("data/dostoevsky.txt"), "\n\n")
	frame := pdfjet.NewTextFrame(font, text)
	frame.SetLocation(72, 72)
	frame.SetWidth(468)
	pages := make([]*pdfjet.Page, 0)
	frame.DrawOnPages(pdf, &pages, letter.Portrait()) // As many pages as the text needs
	pdf.AddPages(pages)
}

// @snippet text-frame-columns
func textFrameColumns(pdf *pdfjet.PDF, font *pdfjet.Font) {
	text := strings.Split(content.OfTextFile("data/dostoevsky.txt"), "\n\n")
	frame := pdfjet.NewTextFrame(font, text)
	for frame.HasMoreText() {
		page := pdfjet.NewPage(pdf, letter.Landscape())
		for x := float32(50); x < 700 && frame.HasMoreText(); x += 250 {
			frame.SetWidth(230).SetHeight(500).SetLocation(x, 50)
			frame.DrawOn(page) // Draws what fits and keeps the rest
		}
	}
}

// @snippet right-to-left
func rightToLeft(pdf *pdfjet.PDF, font *pdfjet.Font) {
	hebrew := pdfjet.NewFontFromFile(pdf, IBMPlexSansHebrew.Regular)
	arabic := pdfjet.NewFontFromFile(pdf, IBMPlexSansArabic.Regular)
	page := pdfjet.NewPage(pdf, letter.Portrait())
	pdfjet.NewTextBlock(hebrew, "שלום עולם! זהו טקסט בעברית.").
		SetRightToLeft(true).SetLanguage("he").
		SetWidth(300).SetLocation(50, 50).DrawOn(page)
	text := pdfjet.ReorderVisually("مرحبا بالعالم!")
	pdfjet.NewTextLine(arabic, text).SetLanguage("ar").
		SetLocation(350-arabic.StringWidth(arabic.GetSize(), text), 100).DrawOn(page)
}

// @snippet formulas
func formulas(pdf *pdfjet.PDF, font *pdfjet.Font) {
	page := pdfjet.NewPage(pdf, letter.Portrait())
	pdfjet.NewCompositeTextLine(50, 50).SetFontSize(16).
		AddFormula(font, "C6H12O6").DrawOn(page)
	pdfjet.NewCompositeTextLine(50, 80).SetFontSize(16).
		AddFormula(font, "SO4^2-").DrawOn(page)
}

// --- Tables -----------------------------------------------------------------

// @snippet table
func table(pdf *pdfjet.PDF, font *pdfjet.Font) {
	bold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	data := [][]string{
		{"Planet", "Moons", "Day, hours"},
		{"Earth", "1", "24"},
		{"Mars", "2", "24.6"},
		{"Jupiter", "95", "9.9"},
	}
	rows := make([][]*pdfjet.Cell, 0)
	for i := 0; i < len(data); i++ {
		row := make([]*pdfjet.Cell, 0)
		for _, text := range data[i] {
			f := font
			if i == 0 {
				f = bold
			}
			row = append(row, pdfjet.NewCell(f, text))
		}
		rows = append(rows, row)
	}
	table := pdfjet.NewTable()
	table.SetTableData(rows, 1) // One header row
	table.SetLocation(50, 50)
	table.AutoAdjustColumnWidths()
	table.RightAlignNumbers()
	table.DrawOn(pdfjet.NewPage(pdf, letter.Portrait()))
}

// @snippet table-pages
func tablePages(pdf *pdfjet.PDF, font *pdfjet.Font) {
	bold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	rows := make([][]*pdfjet.Cell, 0)
	rows = append(rows, []*pdfjet.Cell{pdfjet.NewCell(bold, "Number"),
		pdfjet.NewCell(bold, "Square")})
	for i := 1; i <= 200; i++ {
		rows = append(rows, []*pdfjet.Cell{pdfjet.NewCell(font, strconv.Itoa(i)),
			pdfjet.NewCell(font, strconv.Itoa(i*i))})
	}
	table := pdfjet.NewTable()
	table.SetTableData(rows, 1) // The header row is drawn on every page
	table.SetLocation(50, 50)
	table.SetBottomMargin(50)
	table.SetColumnWidth(0, 100).SetColumnWidth(1, 100)
	pages := make([]*pdfjet.Page, 0)
	table.DrawOnPages(pdf, &pages, letter.Portrait())
	pdf.AddPages(pages)
}

// @snippet cells
func cells(pdf *pdfjet.PDF, font *pdfjet.Font) {
	title := pdfjet.NewCell(font, "Spans two columns")
	title.SetColSpan(2)
	title.SetTextAlignment(alignment.Center)
	title.SetBackgroundColor(color.LightBlue)
	left := pdfjet.NewCell(font, "Left").SetTextColor(color.DarkRed)
	right := pdfjet.NewCell(font, "Right").SetTextAlignment(alignment.Right)
	right.SetBorder(border.Left, false)
	rows := make([][]*pdfjet.Cell, 0)
	rows = append(rows, []*pdfjet.Cell{title,
		pdfjet.NewEmptyCell(font)}) // The cell the span covers
	rows = append(rows, []*pdfjet.Cell{left, right})
	table := pdfjet.NewTable()
	table.SetTableData(rows, 0)
	table.SetColumnWidth(0, 120).SetColumnWidth(1, 120)
	table.SetLocation(50, 50).DrawOn(pdfjet.NewPage(pdf, letter.Portrait()))
}

// @snippet cell-content
func cellContent(pdf *pdfjet.PDF, font *pdfjet.Font) {
	image := pdfjet.NewImageFromFile(pdf, "images/linux-logo.png").ScaleBy(0.25)
	image.SetAltDescription("Tux")
	imageCell := pdfjet.NewEmptyCell(font).SetImage(image)
	barcodeCell := pdfjet.NewEmptyCell(font).
		SetBarcode(pdfjet.NewBarcode(pdfjet.CODE_128, "PDFjet"))
	textCell := pdfjet.NewEmptyCell(font).SetTextBlock(
		pdfjet.NewTextBlock(font, "A text block wraps in its cell, which grows to fit it."))
	rows := make([][]*pdfjet.Cell, 0)
	rows = append(rows, []*pdfjet.Cell{imageCell, barcodeCell, textCell})
	table := pdfjet.NewTable()
	table.SetTableData(rows, 0)
	table.SetColumnWidth(0, 80).SetColumnWidth(1, 150).SetColumnWidth(2, 150)
	table.SetLocation(50, 50).DrawOn(pdfjet.NewPage(pdf, letter.Portrait()))
}

// @snippet table-style
func tableStyle(pdf *pdfjet.PDF, font *pdfjet.Font) {
	bold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	rows := make([][]*pdfjet.Cell, 0)
	rows = append(rows, []*pdfjet.Cell{pdfjet.NewCell(font, "Item"), pdfjet.NewCell(font, "Price")})
	for i := 1; i <= 6; i++ {
		rows = append(rows, []*pdfjet.Cell{pdfjet.NewCell(font, fmt.Sprintf("Item %d", i)),
			pdfjet.NewCell(font, fmt.Sprintf("%d.99", i))})
	}
	table := pdfjet.NewTable()
	table.SetTableData(rows, 1)
	table.SetHeaderRowStyle(bold, color.White, color.Navy)
	table.SetAlternateRowColor(color.AliceBlue)
	table.SetColumnWidthsInPercent(70, 30).SetWidth(300)
	table.RightAlignNumbers()
	table.SetLocation(50, 50).DrawOn(pdfjet.NewPage(pdf, letter.Portrait()))
}

// @snippet table-sums
func tableSums(pdf *pdfjet.PDF, font *pdfjet.Font) {
	bold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	rows := make([][]*pdfjet.Cell, 0)
	rows = append(rows, []*pdfjet.Cell{pdfjet.NewCell(bold, "Day"), pdfjet.NewCell(bold, "Sales")})
	for i := 1; i <= 100; i++ {
		rows = append(rows, []*pdfjet.Cell{pdfjet.NewCell(font, fmt.Sprintf("Day %d", i)),
			pdfjet.NewCell(font, strconv.Itoa(10*i))})
	}
	rows = append(rows, []*pdfjet.Cell{pdfjet.NewCell(bold, "Page total"),
		pdfjet.NewEmptyCell(bold)})
	rows = append(rows, []*pdfjet.Cell{pdfjet.NewCell(bold, "Carried forward"),
		pdfjet.NewEmptyCell(bold)})
	table := pdfjet.NewTable()
	table.SetTableData(rows, 1)
	table.SetNumberOfFooterRows(2)      // The last two rows end every page
	table.SetPageSum(len(rows)-2, 1, 0) // Row, column, decimals
	table.SetRunningSum(len(rows)-1, 1, 0)
	table.RightAlignNumbers()
	table.SetColumnWidth(0, 150).SetColumnWidth(1, 100)
	table.SetBottomMargin(50).SetLocation(50, 50)
	pages := make([]*pdfjet.Page, 0)
	table.DrawOnPages(pdf, &pages, letter.Portrait())
	pdf.AddPages(pages)
}

// @snippet big-table
func bigTable(pdf *pdfjet.PDF, font *pdfjet.Font) {
	bold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold).SetSize(10)
	body := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular).SetSize(9)
	table := pdfjet.NewBigTable(pdf, bold, body, letter.Landscape())
	table.SetNumberOfColumns(9) // Set in this order
	_, err := table.SetTableData("data/Electric_Vehicle_Population_10_Pages.csv", ",")
	if err != nil {
		log.Fatal(err)
	}
	table.SetLocation(0, 0)
	table.SetBottomMargin(20)
	if err := table.Complete(); err != nil {
		log.Fatal(err)
	}
}

// --- Graphics ---------------------------------------------------------------

// @snippet shapes
func shapes(pdf *pdfjet.PDF, font *pdfjet.Font) {
	page := pdfjet.NewPage(pdf, letter.Portrait())
	pdfjet.NewLine(50, 50, 250, 50).SetStrokeWidth(2).SetStrokeColor(color.Blue).DrawOn(page)
	pdfjet.NewRect(50, 70, 100, 60).SetFillColor(color.LightBlue).SetCornerRadius(8).
		DrawOn(page)
	pdfjet.NewPoint(210, 100).SetShape(shape.Star).SetRadius(25).SetFillColor(color.Gold).
		DrawOn(page)
	pdfjet.NewEllipse().SetRadiusX(50).SetRadiusY(30).
		SetFillColor(color.LightGreen).SetLocation(320, 100).DrawOn(page)
	pdfjet.NewArc().SetRadiusX(30).SetRadiusY(30).
		SetStartAngle(0).SetSweep(270).SetStrokeWidth(4).SetLocation(450, 100).DrawOn(page)
}

// @snippet paths
func paths(pdf *pdfjet.PDF, font *pdfjet.Font) {
	triangle := pdfjet.NewPath()
	triangle.Add(pdfjet.NewPoint(100, 50))
	triangle.Add(pdfjet.NewPoint(150, 130))
	triangle.Add(pdfjet.NewPoint(50, 130))
	triangle.SetClosed(true)
	triangle.SetFillShape(true)
	triangle.SetStrokeColor(color.DarkOrange)
	curve := pdfjet.NewPath()
	curve.Add(pdfjet.NewPoint(200, 130))
	curve.Add(pdfjet.NewControlPointC(250, 20)) // Bezier control points
	curve.Add(pdfjet.NewControlPointC(300, 200))
	curve.Add(pdfjet.NewPoint(350, 90))
	curve.SetStrokeWidth(3)
	page := pdfjet.NewPage(pdf, letter.Portrait())
	triangle.DrawOn(page)
	curve.DrawOn(page)
}

// @snippet page-graphics
func pageGraphics(pdf *pdfjet.PDF, font *pdfjet.Font) {
	page := pdfjet.NewPage(pdf, letter.Portrait())
	page.SetPenColor(color.Navy)
	page.SetPenWidth(2)
	page.SetStrokeDashPattern("[6 3] 0")
	page.MoveTo(50, 50)
	page.LineTo(250, 50)
	page.LineTo(250, 150)
	page.StrokePath()
	page.SetBrushColor(color.Coral)
	page.FillRect(50, 80, 150, 70)
}

// @snippet transparency
func transparency(pdf *pdfjet.PDF, font *pdfjet.Font) {
	page := pdfjet.NewPage(pdf, letter.Portrait())
	page.SetBrushColor(color.Blue)
	page.FillRect(50, 50, 100, 100)
	gs := pdfjet.NewGraphicsState()
	gs.SetAlphaNonStroking(0.5) // Fills are 50% transparent
	page.SaveGraphicsState()
	page.SetGraphicsState(gs)
	page.SetBrushColor(color.Red)
	page.FillRect(100, 100, 100, 100)
	page.RestoreGraphicsState()
}

// @snippet containers
func containers(pdf *pdfjet.PDF, font *pdfjet.Font) {
	container := pdfjet.NewContainer(200, 100)
	container.Add(pdfjet.NewRect(0, 0, 200, 100).SetFillColor(color.LightYellow))
	container.Add(pdfjet.NewTextLine(font, "Turned together").SetLocation(20, 55))
	container.SetLocation(150, 100)
	container.SetRotation(-30)
	container.DrawOn(pdfjet.NewPage(pdf, letter.Portrait()))
}

// @snippet stamps
func stamps(pdf *pdfjet.PDF, font *pdfjet.Font) {
	stamp := pdfjet.NewStamp(pdf).SetSize(120, 40).AddFont(font)
	stamp.SetStrokeColor(color.Red).SetStrokeWidth(3).DrawRect(0, 0, 120, 40)
	stamp.SetFillColor(color.Red).DrawTextUsingParams(pdfjet.NewTextParameters().
		SetFont(font).SetFontSize(20).SetLocation(18, 28).SetText("PAID"))
	stamp.Complete() // Written once, drawn any number of times
	page := pdfjet.NewPage(pdf, letter.Portrait())
	stamp.SetLocation(50, 50).DrawOn(page)
	stamp.SetRotation(-15).SetLocation(250, 60).DrawOn(page)
}

// --- Images -----------------------------------------------------------------

// @snippet images
func images(pdf *pdfjet.PDF, font *pdfjet.Font) {
	photo := pdfjet.NewImageFromFile(pdf, "images/gr-map.jpg") // JPEG, PNG or BMP
	photo.SetAltDescription("A map of Greece")
	page := pdfjet.NewPage(pdf, letter.Portrait())
	photo.SetLocation(50, 50)
	photo.ResizeWidth(250)
	xy := photo.DrawOn(page)
	// The image is written to the PDF once, however many times it is drawn.
	photo.ResizeWidth(100).SetLocation(50, xy[1]+10)
	photo.DrawOn(page)
}

// @snippet svg-images
func svgImages(pdf *pdfjet.PDF, font *pdfjet.Font) {
	icon, err := pdfjet.NewSVGImageFromFile("images/svg/star_FILL0_wght400_GRAD0_opsz48.svg")
	if err != nil {
		log.Fatal(err)
	}
	icon.SetAltDescription("A star")
	icon.SetLocation(50, 50)
	icon.ScaleBy(2) // Vector graphics stay sharp at any size
	icon.DrawOn(pdfjet.NewPage(pdf, letter.Portrait()))
}

// --- Charts -----------------------------------------------------------------

// @snippet line-chart
func lineChart(pdf *pdfjet.PDF, font *pdfjet.Font) {
	small := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular).SetSize(8)
	chart := pdfjet.NewChart(font, small)
	chart.SetLocation(50, 50)
	chart.SetSize(400, 250)
	chart.SetTitle("Visitors")
	chart.SetXAxisTitle("Month")
	chart.SetYAxisTitle("Thousands")
	series := chart.AddSeries("2026").SetDrawPath(true).SetStrokeColor(color.Blue)
	visitors := []float32{3, 5, 4, 7, 9, 8}
	for month := 1; month <= len(visitors); month++ {
		series.AddPoint(float32(month), visitors[month-1])
	}
	chart.DrawOn(pdfjet.NewPage(pdf, letter.Portrait()))
}

// @snippet bar-chart
func barChart(pdf *pdfjet.PDF, font *pdfjet.Font) {
	small := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular).SetSize(8)
	chart := pdfjet.NewBarChart(font, small)
	chart.SetLocation(50, 50)
	chart.SetSize(400, 250)
	chart.SetTitle("Units sold")
	chart.SetCategories("Q1", "Q2", "Q3", "Q4")
	chart.AddSeriesWithColor("2025", []float32{45, 65, 31, 52}, color.SeaGreen)
	chart.AddSeriesWithColor("2026", []float32{75, 20, 73, 80}, color.IndianRed)
	chart.DrawOn(pdfjet.NewPage(pdf, letter.Portrait()))
}

// @snippet donut-chart
func donutChart(pdf *pdfjet.PDF, font *pdfjet.Font) {
	bold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold).SetSize(10)
	chart := pdfjet.NewDonutChart(font, bold)
	chart.SetLocation(100, 100)
	chart.SetRadii(100, 60) // An inner radius of 0 makes a pie chart
	chart.AddSlice(pdfjet.NewSlice(50, color.SteelBlue, "Java"))
	chart.AddSlice(pdfjet.NewSlice(30, color.SeaGreen, "C#"))
	chart.AddSlice(pdfjet.NewSlice(20, color.DarkOrange, "Go"))
	chart.DrawOn(pdfjet.NewPage(pdf, letter.Portrait()))
}

// @snippet calendar
func calendar(pdf *pdfjet.PDF, font *pdfjet.Font) {
	bold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold).SetSize(10)
	small := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular).SetSize(9)
	month := pdfjet.NewCalendarMonth(bold, small, 2026, 12)
	month.SetFirstDayOfWeek(time.Monday)
	month.SetLocation(50, 50)
	month.DrawOn(pdfjet.NewPage(pdf, letter.Portrait()))
}

// --- Barcodes ---------------------------------------------------------------

// @snippet barcodes
func barcodes(pdf *pdfjet.PDF, font *pdfjet.Font) {
	page := pdfjet.NewPage(pdf, letter.Portrait())
	code128 := pdfjet.NewBarcode(pdfjet.CODE_128, "PDFjet 9.0")
	code128.SetFont(font) // The text under the bars
	code128.SetLocation(50, 50)
	xy := code128.DrawOn(page)
	ean13 := pdfjet.NewBarcode(pdfjet.EAN_13, "051234567890") // The check digit is added
	ean13.SetFont(font)
	ean13.SetModuleLength(1)
	ean13.SetLocation(50, xy[1]+30)
	ean13.DrawOn(page)
}

// @snippet qr-code
func qrCode(pdf *pdfjet.PDF, font *pdfjet.Font) {
	qr := qrcode.NewQRCode("https://pdfjet.com", errorcorrectionlevel.M)
	qr.SetModuleLength(4)
	qr.SetLocation(50, 50)
	qr.DrawOn(pdfjet.NewPage(pdf, letter.Portrait()))
}

// @snippet data-matrix
func dataMatrix(pdf *pdfjet.PDF, font *pdfjet.Font) {
	code := datamatrix.NewDataMatrix("Grüße aus München!")
	code.SetModuleLength(4)
	code.SetLocation(50, 50)
	code.DrawOn(pdfjet.NewPage(pdf, letter.Portrait()))
}

// @snippet pdf417
func pdf417Code(pdf *pdfjet.PDF, font *pdfjet.Font) {
	code := pdf417.NewPDF417("PDF417 holds text and binary data, and survives damage.")
	code.SetModuleLength(1)
	code.SetLocation(50, 50)
	code.DrawOn(pdfjet.NewPage(pdf, letter.Portrait()))
}

// --- Links, bookmarks, annotations and layers -------------------------------

// @snippet web-links
func webLinks(pdf *pdfjet.PDF, font *pdfjet.Font) {
	page := pdfjet.NewPage(pdf, letter.Portrait())
	pdfjet.NewTextLine(font, "Visit pdfjet.com").SetTextColor(color.Blue).SetUnderline(true).
		SetURIAction("https://pdfjet.com").SetLocation(50, 50).DrawOn(page)
}

// @snippet internal-links
func internalLinks(pdf *pdfjet.PDF, font *pdfjet.Font) {
	contents := pdfjet.NewPage(pdf, letter.Portrait())
	pdfjet.NewTextLine(font, "Go to chapter 1").SetGoToAction("chapter1").
		SetLocation(50, 50).DrawOn(contents)
	chapter := pdfjet.NewPage(pdf, letter.Portrait())
	pdfjet.NewTextLine(font, "Chapter 1").SetDestination("chapter1").
		SetLocation(50, 50).DrawOn(chapter)
}

// @snippet bookmarks
func bookmarks(pdf *pdfjet.PDF, font *pdfjet.Font) {
	outline := pdfjet.NewBookmark(pdf)
	page := pdfjet.NewPage(pdf, letter.Portrait())
	title := pdfjet.NewTitle(font, "Chapter 1", 50, 50)
	chapter := outline.AddBookmark(page, title)
	title.DrawOn(page)
	title = pdfjet.NewTitle(font, "Section 1.1", 70, 80)
	chapter.AddBookmark(page, title) // A bookmark under the chapter
	title.DrawOn(page)
}

// @snippet attachments
func attachments(pdf *pdfjet.PDF, font *pdfjet.Font) {
	file := pdfjet.NewEmbeddedFileAtPath(pdf, "data/winter-2009.txt", true)
	page := pdfjet.NewPage(pdf, letter.Portrait())
	attachment := pdfjet.NewFileAttachment(file)
	attachment.SetLocation(50, 50)
	attachment.SetIconPaperclip()
	attachment.SetTitle("winter-2009.txt")
	attachment.SetContents("The data of the table, attached to the page.")
	attachment.DrawOn(page)
}

// @snippet notes
func notes(pdf *pdfjet.PDF, font *pdfjet.Font) {
	page := pdfjet.NewPage(pdf, letter.Portrait())
	note := pdfjet.NewTextAnnotation()
	note.SetLocation(50, 50)
	note.SetSize(24, 24)
	note.SetTitle("Reviewer")
	note.SetContents("Please check the figures on this page.")
	note.DrawOn(page)
}

// @snippet layers
func layers(pdf *pdfjet.PDF, font *pdfjet.Font) {
	grid := pdfjet.NewOptionalContentGroup(pdf, "Grid")
	grid.SetVisible(true)
	grid.SetPrintable(false) // Shown on the screen, not printed
	for x := float32(50); x <= 250; x += 50 {
		grid.Add(pdfjet.NewLine(x, 50, x, 250).SetStrokeColor(color.LightGray))
	}
	grid.DrawOn(pdfjet.NewPage(pdf, letter.Portrait()))
}

// --- Forms ------------------------------------------------------------------

// @snippet check-boxes
func checkBoxes(pdf *pdfjet.PDF, font *pdfjet.Font) {
	page := pdfjet.NewPage(pdf, letter.Portrait())
	pdfjet.NewCheckBox(font, "Java").Check(mark.Check).SetLocation(50, 50).DrawOn(page)
	pdfjet.NewCheckBox(font, "Swift").SetLocation(50, 75).DrawOn(page)
	xy := pdfjet.NewRadioButton(font, "Yes").Select(true).SetLocation(50, 110).DrawOn(page)
	pdfjet.NewRadioButton(font, "No").SetLocation(xy[0]+20, 110).DrawOn(page)
}

// @snippet form-fields
func formFields(pdf *pdfjet.PDF, font *pdfjet.Font) {
	bold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	fields := make([]*pdfjet.Field, 0)
	fields = append(fields, pdfjet.NewField(0, "Company", "Smart Widgets Inc."))
	fields = append(fields, pdfjet.NewField(0, "City", "Toronto")) // x 0 starts a new row
	fields = append(fields, pdfjet.NewField(200, "Province", "Ontario"))
	pdfjet.NewForm(fields).SetLabelFont(font).SetLabelFontSize(8).
		SetValueFont(bold).SetValueFontSize(10).
		SetWidth(400).SetLocation(50, 50).
		DrawOn(pdfjet.NewPage(pdf, letter.Portrait()))
}

// --- Existing PDFs and security ---------------------------------------------

// @snippet merge
func merge() {
	pdf, err := pdfjet.NewPDFFile("merged.pdf")
	if err != nil {
		log.Fatal(err)
	}
	for _, file := range []string{"data/testPDFs/PDFjetLogo.pdf", "data/testPDFs/rc65-16e.pdf"} {
		objects, err := pdf.Read(content.OfBinaryFile(file))
		if err != nil {
			log.Fatal(err)
		}
		if err := pdf.Merge(objects); err != nil {
			log.Fatal(err)
		}
	}
	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// @snippet split
func split() {
	objects, err := pdfjet.NewPDFReader().Read(content.OfBinaryFile("data/testPDFs/rc65-16e.pdf"))
	if err != nil {
		log.Fatal(err)
	}
	firstPage, err := pdfjet.NewPDFFile("page1.pdf")
	if err != nil {
		log.Fatal(err)
	}
	err = firstPage.MergePages(objects, 1) // The page numbers to keep, from 1
	if err != nil {
		log.Fatal(err)
	}
	if err := firstPage.Complete(); err != nil {
		log.Fatal(err)
	}
}

// @snippet existing-pages
func existingPages() {
	pdf, err := pdfjet.NewPDFFile("stamped.pdf")
	if err != nil {
		log.Fatal(err)
	}
	objects, err := pdf.Read(content.OfBinaryFile("data/testPDFs/rc65-16e.pdf"))
	if err != nil {
		log.Fatal(err)
	}
	file, err := os.Open(IBMPlexSans.Regular)
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	font := pdfjet.NewFontStream2(&objects, bufio.NewReader(file)).SetSize(24)
	for _, pageObj := range pdf.GetPageObjects(objects) {
		page := pdfjet.NewPageFromObject(pdf, pageObj)
		page.AddFontResource(font, &objects)
		pdfjet.NewTextLine(font, "COPY").SetTextColor(color.Red).SetLocation(50, 50).DrawOn(page)
		page.Complete(&objects)
	}
	if err := pdf.AddObjects(objects); err != nil {
		log.Fatal(err)
	}
	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// @snippet encryption
func encryptDocument() {
	pdf, err := pdfjet.NewPDFFile("encrypted.pdf")
	if err != nil {
		log.Fatal(err)
	}
	passwords := encryption.NewPasswords()
	passwords.SetUserPassword("hello")  // To open the document
	passwords.SetOwnerPassword("world") // To change what the user may do
	permissions := encryption.NewPermissions()
	permissions.Grant(encryption.Print | encryption.PrintHighQuality)
	enc, err := pdfjet.NewEncryption(pdf, passwords, permissions)
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetEncryption(enc)
	font := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	page := pdfjet.NewPage(pdf, letter.Portrait())
	pdfjet.NewTextLine(font, "A secret").SetLocation(50, 50).DrawOn(page)
	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// --- Running the snippets ---------------------------------------------------

func run(name string, snippet func(pdf *pdfjet.PDF, font *pdfjet.Font)) {
	pdf, err := pdfjet.NewPDFFile(name + ".pdf")
	if err != nil {
		log.Fatal(err)
	}
	font := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular).SetSize(12)
	snippet(pdf, font)
	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	run("document-info", documentInfo)
	run("page-sizes", pageSizes)
	run("page-numbers", pageNumbers)
	run("watermark", watermark)
	accessibleDocument()
	archivalDocument()
	run("fonts", fonts)
	run("font-files", fontFiles)
	run("core-fonts", coreFonts)
	run("cjk-fonts", cjkFonts)
	run("fallback-font", fallbackFont)
	run("font-metrics", fontMetrics)
	run("text-line", textLine)
	run("text-block", textBlock)
	run("highlighted-words", highlightedWords)
	run("paragraphs", paragraphs)
	run("lists", lists)
	run("markup", markup)
	run("text-column", textColumn)
	run("text-frame-pages", textFramePages)
	run("text-frame-columns", textFrameColumns)
	run("right-to-left", rightToLeft)
	run("formulas", formulas)
	run("table", table)
	run("table-pages", tablePages)
	run("cells", cells)
	run("cell-content", cellContent)
	run("table-style", tableStyle)
	run("table-sums", tableSums)
	run("big-table", bigTable)
	run("shapes", shapes)
	run("paths", paths)
	run("page-graphics", pageGraphics)
	run("transparency", transparency)
	run("containers", containers)
	run("stamps", stamps)
	run("images", images)
	run("svg-images", svgImages)
	run("line-chart", lineChart)
	run("bar-chart", barChart)
	run("donut-chart", donutChart)
	run("calendar", calendar)
	run("barcodes", barcodes)
	run("qr-code", qrCode)
	run("data-matrix", dataMatrix)
	run("pdf417", pdf417Code)
	run("web-links", webLinks)
	run("internal-links", internalLinks)
	run("bookmarks", bookmarks)
	run("attachments", attachments)
	run("notes", notes)
	run("layers", layers)
	run("check-boxes", checkBoxes)
	run("form-fields", formFields)
	merge()
	split()
	existingPages()
	encryptDocument()
}
