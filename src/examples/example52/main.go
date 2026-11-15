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
	"github.com/go-text/typesetting/di"
	"github.com/go-text/typesetting/font"
	"github.com/go-text/typesetting/language"
	"github.com/go-text/typesetting/shaping"
	"golang.org/x/image/math/fixed"
)

// Example_52 draws Hindi and Khmer, scripts PDFjet does not shape itself:
// go-text/typesetting, HarfBuzz in Go, shapes them -- conjuncts, vowel signs,
// stacked consonants -- and Page.DrawGlyphs draws the glyphs it found, at
// their places, as outlines read from the font file: sharp on screen and in
// print, and read by screen readers, as each paragraph is tagged with its
// text and its language. Any engine that finds the glyphs can do the
// shaping. A page of each, filled with the Universal Declaration of Human
// Rights from its beginning: Hindi on the first page, Khmer on the second.
//
// The example is a module of its own: go-text is its dependency, never
// PDFjet's. Run it from the folder of the repository:
//
//	cd src/examples/example52 && go build -o ../../../Example_52.exe . && cd ../../.. && ./Example_52.exe

const (
	devanagari = "examples/fonts/NotoSansDevanagari/NotoSansDevanagari-Regular.ttf"
	khmer      = "examples/fonts/NotoSansKhmer/NotoSansKhmer-Regular.ttf"
	latin      = "resources/fonts/IBMPlexSans/IBMPlexSans-Regular.otf"

	size   = float32(10.5)
	left   = float32(50)
	width  = float32(512)
	bottom = float32(742) // The page's 792 points less the margin
)

// A face of go-text, which shapes, and the same font file loaded by PDFjet,
// which reads the outlines, so that the glyph numbers match.
type shapingFont struct {
	face   *font.Face
	font   *pdfjet.Font
	script language.Script
}

func loadFont(pdf *pdfjet.PDF, path string, script language.Script) shapingFont {
	file, err := os.Open(path)
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	face, err := font.ParseTTF(file)
	if err != nil {
		log.Fatal(err)
	}
	outlines := pdfjet.NewFontFromFile(pdf, path)
	outlines.SetSize(size)
	return shapingFont{face, outlines, script}
}

// A glyph shaped, in points from the start of the paragraph
type glyph struct {
	id      int
	font    int // Its font: 0 the script's, 1 the Latin one
	cluster int // The index in the text, in runes, of its cluster's first character
	x, y    float32
	advance float32
}

// drawPage draws a page of the Declaration in the language, its paragraphs
// from the beginning until the page is full, and returns the text drawn.
func drawPage(pdf *pdfjet.PDF, heading, body *pdfjet.Font, name, lang, scriptFile string,
	script language.Script, textFile string) string {
	page := pdfjet.NewPage(pdf, letter.Portrait())
	pdfjet.NewTextLine(heading, "Universal Declaration of Human Rights, in "+name).
		SetStructureType(structelem.H1).
		SetLocation(left, 60).
		DrawOn(page)
	pdfjet.NewTextLine(body, "Shaped by go-text/typesetting, drawn by PDFjet with DrawGlyphs as outlines;"+
		" the text is attached to the document.").
		SetLocation(left, 80).
		DrawOn(page)

	fonts := []shapingFont{
		loadFont(pdf, scriptFile, script),
		loadFont(pdf, latin, language.Latin),
	}
	ascent := fonts[0].font.GetAscent(size)
	descent := fonts[0].font.GetDescent(size)

	var drawn strings.Builder
	y := float32(96) // The top of the next line
	for _, paragraph := range paragraphs(textFile) {
		text := drawParagraph(page, paragraph, lang, fonts, &y, ascent, descent)
		if text == "" {
			break // The page is full
		}
		drawn.WriteString(text)
		drawn.WriteString("\n")
		if len(text) < len(paragraph) {
			break // The page is full
		}
		y += 0.5 * size
	}
	return drawn.String()
}

// drawParagraph draws the paragraph, wrapped to the width at spaces, as many
// of its lines as fit above the bottom of the page, from the top *y, which it
// moves below them, and returns the text of the lines drawn. The script's
// font is used where it has the characters, IBM Plex Sans elsewhere.
func drawParagraph(page *pdfjet.Page, text, lang string, fonts []shapingFont,
	y *float32, ascent, descent float32) string {
	runes := []rune(text)

	// Each character in the script's font where it has it, else in the Latin
	// one; each run of one font shaped on its own
	fontOf := make([]int, len(runes))
	for i, r := range runes {
		if _, ok := fonts[0].face.NominalGlyph(r); !ok {
			fontOf[i] = 1
		}
	}
	var glyphs []glyph
	var pen float32
	shaper := shaping.HarfbuzzShaper{}
	for start := 0; start < len(runes); {
		end := start + 1
		for end < len(runes) && fontOf[end] == fontOf[start] {
			end++
		}
		f := fonts[fontOf[start]]
		upem := int(f.face.Upem())
		// Shaped at a size of one pixel a font unit, then scaled to points
		output := shaper.Shape(shaping.Input{
			Text: runes, RunStart: start, RunEnd: end,
			Direction: di.DirectionLTR, Face: f.face, Size: fixed.I(upem),
			Script: f.script, Language: language.NewLanguage(lang),
		})
		scale := size / float32(upem) / 64
		for _, g := range output.Glyphs {
			glyphs = append(glyphs, glyph{
				id: int(g.GlyphID), font: fontOf[start], cluster: g.TextIndex(),
				x: pen + float32(g.XOffset)*scale, y: -float32(g.YOffset) * scale,
				advance: float32(g.Advance) * scale,
			})
			pen += float32(g.Advance) * scale
		}
		start = end
	}

	drawnEnd := 0 // The end, in runes, of the text of the lines drawn
	for first := 0; first < len(glyphs); {
		if *y+ascent+descent > bottom {
			break
		}
		if drawnEnd == 0 {
			page.AddBDC(structelem.P, lang, "", "")
		}
		// The glyphs of the line: all that are left if they fit, else up to
		// the last space after a word that fits
		lineStart := glyphs[first].x
		last := len(glyphs)
		lastSpace := -1
		for i := first; i < len(glyphs); i++ {
			if runes[glyphs[i].cluster] == ' ' {
				lastSpace = i + 1
			} else if glyphs[i].x+glyphs[i].advance-lineStart > width && lastSpace > 0 {
				last = lastSpace
				break
			}
		}
		baseline := *y + ascent
		// The line, a run of one font at a time: its glyphs and their places
		for start := first; start < last; {
			end := start + 1
			for end < last && glyphs[end].font == glyphs[start].font {
				end++
			}
			textStart := glyphs[start].cluster
			textEnd := len(runes)
			if end < len(glyphs) {
				textEnd = glyphs[end].cluster
			}
			ids := make([]int, end-start)
			positions := make([]float32, 2*(end-start))
			for i := start; i < end; i++ {
				ids[i-start] = glyphs[i].id
				positions[2*(i-start)] = left + glyphs[i].x - lineStart
				positions[2*(i-start)+1] = baseline + glyphs[i].y
			}
			page.DrawGlyphs(fonts[glyphs[start].font].font, ids, positions,
				string(runes[textStart:textEnd]), lang)
			start = end
		}
		*y = baseline + descent + 0.25*size
		drawnEnd = len(runes)
		if last < len(glyphs) {
			drawnEnd = glyphs[last].cluster
		}
		first = last
	}
	if drawnEnd > 0 {
		page.AddEMC()
	}
	return string(runes[:drawnEnd])
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

	hindi := drawPage(pdf, heading, body, "Hindi", "hi", devanagari, language.Devanagari,
		"resources/data/udhr/udhr_hin.txt")
	khmerText := drawPage(pdf, heading, body, "Khmer", "km", khmer, language.Khmer,
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
