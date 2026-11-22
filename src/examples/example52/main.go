// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"

	// HarfBuzz for TextBlock and TextLine: one line, nothing to install
	_ "github.com/edragoev1/pdfjet/harfbuzz/v9"
)

// Example_52 draws Hindi and Khmer, scripts whose letters change shape and
// place, with TextBlock: HarfBuzz shapes them -- conjuncts, vowel signs,
// stacked consonants -- and PDFjet draws the glyphs it found, at their places,
// as outlines read from the font file: sharp on screen and in print, and read
// by screen readers, as each paragraph is tagged with its text and its
// language. Go has no text engine of its own that gives glyphs: the one line
//
//	import _ "github.com/edragoev1/pdfjet/harfbuzz/v9"
//
// gives PDFjet HarfBuzz in Go, go-text/typesetting, on every system and
// CPU, with nothing to install. The Universal Declaration of Human Rights in
// Hindi, then in Khmer: the paragraphs flow from page to page, a paragraph
// cut where a page ends.
//
// The example is a module of its own, as the HarfBuzz module is: PDFjet itself
// has no dependency. Run it from the folder of the repository:
//
//	cd src/examples/example52 && go build -o ../../../Example_52.exe . && cd ../../.. && ./Example_52.exe

const (
	devanagari = "resources/fonts/world/NotoSansDevanagari/NotoSansDevanagari-Regular.otf"
	khmer      = "resources/fonts/world/NotoSansKhmer/NotoSansKhmer-Regular.otf"
	latin      = "resources/fonts/IBMPlexSans/IBMPlexSans-Regular.otf"

	size   = float32(10.5)
	left   = float32(50)
	width  = float32(512)
	top    = float32(50)  // The top margin
	bottom = float32(742) // The page's 792 points less the margin
)

// draw draws the Declaration in the language, from a new page, on as many
// pages as it takes, and returns its text.
func draw(pdf *pdfjet.PDF, heading, body *pdfjet.Font, name, lang, scriptFile, textFile string) string {
	page := pdfjet.NewPage(pdf, letter.Portrait())
	pdfjet.NewTextLine(heading, "Universal Declaration of Human Rights, in "+name).
		SetStructureType(structelem.H1).
		SetLocation(left, 60).
		DrawOn(page)
	pdfjet.NewTextLine(body, "Shaped by HarfBuzz, drawn by PDFjet's TextBlock as outlines;"+
		" the text is attached to the document.").
		SetLocation(left, 80).
		DrawOn(page)

	// The script's font, and IBM Plex Sans for the characters it does not
	// have, such as digits and punctuation of the Latin script
	script := pdfjet.NewFontFromFile(pdf, scriptFile)
	script.SetSize(size)
	latinFont := pdfjet.NewFontFromFile(pdf, latin)

	// A TextBlock a paragraph: measured, without a page, and drawn on the
	// next page when it does not fit on this one
	paragraphs := paragraphs(textFile)
	y := float32(96) // Below the heading, on the first page
	for _, paragraph := range paragraphs {
		text := pdfjet.NewTextBlock(script, paragraph).
			SetFallbackFont(latinFont).
			SetLanguage(lang)
		text.SetWidth(width)
		text.SetLocation(left, y)
		if text.DrawOn(nil)[1] > bottom {
			page = pdfjet.NewPage(pdf, letter.Portrait())
			y = top
			text.SetLocation(left, y)
		}
		y = text.DrawOn(page)[1] + 0.5*size
	}

	// A blank line between paragraphs, as plain text has them
	var all strings.Builder
	for _, paragraph := range paragraphs {
		if all.Len() > 0 {
			all.WriteString("\n")
		}
		all.WriteString(paragraph)
		all.WriteString("\n")
	}
	return all.String()
}

// paragraphs returns the paragraphs of the text file, one to a line.
func paragraphs(path string) []string {
	file, err := os.Open(path)
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	var paragraphs []string
	scanner := bufio.NewScanner(file)
	scanner.Buffer(make([]byte, 1<<20), 1<<20)
	for scanner.Scan() {
		if line := strings.TrimSpace(scanner.Text()); line != "" {
			paragraphs = append(paragraphs, line)
		}
	}
	if err := scanner.Err(); err != nil {
		log.Fatal(err)
	}
	return paragraphs
}

// Example52 draws a page of the Universal Declaration of Human Rights in
// Hindi and one in Khmer.
func Example52() {
	pdf, err := pdfjet.NewPDFFile("Example_52.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("The Universal Declaration of Human Rights in Hindi and Khmer") // Required for PDF/UA !
	pdf.SetLanguage("en")

	heading := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	heading.SetSize(16)
	body := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	body.SetSize(9)

	hindi := draw(pdf, heading, body, "Hindi", "hi", devanagari,
		"resources/data/udhr/udhr_hin.txt")
	khmerText := draw(pdf, heading, body, "Khmer", "km", khmer,
		"resources/data/udhr/udhr_khm.txt")

	// The pages draw the text as outlines, which cannot be selected or
	// searched; the exact text is attached for anyone who needs it.
	pdf.AttachText("udhr-hindi.txt", hindi)
	pdf.AttachText("udhr-khmer.txt", khmerText)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example52()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_52 => %4d ms\n", time1-time0)
}
