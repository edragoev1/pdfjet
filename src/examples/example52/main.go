// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"regexp"
	"strconv"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/SourceSerif4"
	"github.com/edragoev1/pdfjet/v9/src/a5"
	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/content"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

var (
	partRe    = regexp.MustCompile(`^PART [IVX]+$`)
	chapterRe = regexp.MustCompile(`^[IVXL]+\.$`)
	blockRe   = regexp.MustCompile(`\n\s*\n`)
)

// Example52 sets a whole novel, "The Idiot" by Fyodor Dostoyevsky, as a
// book: a title page, and each chapter on new A5 pages that a TextFrame flows
// onto, with the words in italics and a number on every page. The time it
// prints is the time PDFjet takes to set the 241,527 words of
// data/the-idiot.txt.
func Example52() {
	pdf, err := pdfjet.NewPDFFile("Example_52.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("The Idiot")
	pdf.SetAuthor("Fyodor Dostoyevsky")

	regular := pdfjet.NewFontFromFile(pdf, SourceSerif4.Regular)
	italic := pdfjet.NewFontFromFile(pdf, SourceSerif4.Italic)
	semiBold := pdfjet.NewFontFromFile(pdf, SourceSerif4.SemiBold)
	regular.SetSize(10.0)
	italic.SetSize(10.0)

	pages := make([]*pdfjet.Page, 0)

	// The title page.
	title := []*pdfjet.Paragraph{
		centered(pdfjet.NewTextLine(semiBold, "The Idiot").SetFontSize(28.0), structelem.H1),
		centered(pdfjet.NewTextLine(regular, "Fyodor Dostoyevsky").SetFontSize(14.0), structelem.P),
		centered(pdfjet.NewTextLine(italic, "Translated by Eva Martin"), structelem.P),
	}
	titleFrame := pdfjet.NewTextFrameFromParagraphs(title)
	titleFrame.SetLocation(54.0, 200.0)
	titleFrame.SetWidth(312.0).SetParagraphGap(16.0)
	titleFrame.DrawOnPages(pdf, &pages, a5.Portrait())
	titlePages := len(pages)

	// The novel: parts begin with "PART I" and chapters with their number,
	// "I.", and the paragraphs are separated by an empty line.
	var chapter []*pdfjet.Paragraph
	for _, block := range blockRe.Split(content.OfTextFile("data/the-idiot.txt"), -1) {
		text := strings.Join(strings.Fields(block), " ")
		if partRe.MatchString(text) {
			drawChapter(pdf, &pages, chapter)
			chapter = []*pdfjet.Paragraph{
				centered(pdfjet.NewTextLine(semiBold, text).SetFontSize(16.0), structelem.H1),
			}
		} else if chapterRe.MatchString(text) {
			if len(chapter) > 1 {
				drawChapter(pdf, &pages, chapter)
				chapter = nil
			}
			chapter = append(chapter,
				centered(pdfjet.NewTextLine(semiBold, text).SetFontSize(14.0), structelem.H2))
		} else if text != "" {
			chapter = append(chapter, paragraph(text, regular, italic))
		}
	}
	drawChapter(pdf, &pages, chapter)

	// A number at the foot of every page but the title page.
	for i := titlePages; i < len(pages); i++ {
		number := pdfjet.NewTextLine(regular, strconv.Itoa(i-titlePages+1))
		number.SetFontSize(9.0)
		pages[i].AddFooterOffsetBy(number, 30.0)
	}
	pdf.AddPages(pages)
	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// drawChapter draws a chapter on new pages, which it starts at the top of.
func drawChapter(pdf *pdfjet.PDF, pages *[]*pdfjet.Page, chapter []*pdfjet.Paragraph) {
	if chapter != nil {
		frame := pdfjet.NewTextFrameFromParagraphs(chapter)
		frame.SetLocation(54.0, 54.0)
		frame.SetWidth(312.0).SetParagraphGap(4.0)
		frame.DrawOnPages(pdf, pages, a5.Portrait())
	}
}

func centered(textLine *pdfjet.TextLine, structureType structelem.StructElem) *pdfjet.Paragraph {
	return pdfjet.NewParagraph().Add(textLine).SetTextAlignment(alignment.Center).
		SetStructureType(structureType)
}

// paragraph returns a justified paragraph of the text, in which _underscores_
// mark the words in italics. A word in italics is set in italics whole, with
// the punctuation around it, since a paragraph puts a space between its text
// lines.
func paragraph(text string, regular, italic *pdfjet.Font) *pdfjet.Paragraph {
	p := pdfjet.NewParagraph().SetTextAlignment(alignment.Justify)
	var run strings.Builder
	runItalic := false
	inItalic := false
	fontOf := func(isItalic bool) *pdfjet.Font {
		if isItalic {
			return italic
		}
		return regular
	}
	for _, word := range strings.Split(text, " ") {
		wordItalic := inItalic || strings.Contains(word, "_")
		for _, ch := range word {
			if ch == '_' {
				inItalic = !inItalic
			}
		}
		if run.Len() > 0 && wordItalic != runItalic {
			p.Add(pdfjet.NewTextLine(fontOf(runItalic), run.String()))
			run.Reset()
		}
		if run.Len() > 0 {
			run.WriteByte(' ')
		}
		run.WriteString(strings.ReplaceAll(word, "_", ""))
		runItalic = wordItalic
	}
	if run.Len() > 0 {
		p.Add(pdfjet.NewTextLine(fontOf(runItalic), run.String()))
	}
	return p
}

func main() {
	time0 := time.Now().UnixMilli()
	Example52()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_52 => %4d ms\n", time1-time0)
}
