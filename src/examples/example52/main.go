// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"bufio"
	"bytes"
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

// Example_52 draws Hindi and Khmer, scripts PDFjet does not shape itself,
// with ComplexText: go-text/typesetting, HarfBuzz in Go, shapes them --
// conjuncts, vowel signs, stacked consonants -- and PDFjet draws the glyphs
// it found, at their places, as outlines read from the font file: sharp on
// screen and in print, and read by screen readers, as each paragraph is
// tagged with its text and its language. Go has no text engine of its own
// that gives glyphs: goTextShaper below is the shaper ComplexText is given.
// A page of each, filled with the Universal Declaration of Human Rights from
// its beginning: Hindi on the first page, Khmer on the second.
//
// The example is a module of its own: go-text is its dependency, never
// PDFjet's. Run it from the folder of the repository:
//
//	cd src/examples/example52 && go build -o ../../../Example_52.exe . && cd ../../.. && ./Example_52.exe

const (
	devanagari = "examples/fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Regular.otf"
	khmer      = "examples/fonts/NotoSansKhmer/NotoSansKhmer-Regular.otf"
	latin      = "resources/fonts/IBMPlexSans/IBMPlexSans-Regular.otf"

	size   = float32(10.5)
	left   = float32(50)
	width  = float32(512)
	bottom = float32(742) // The page's 792 points less the margin
)

// goTextShaper is a shaper for ComplexText that shapes with
// go-text/typesetting: each character in the first font that has it, each
// run of one font shaped on its own, and the lines broken at spaces.
type goTextShaper struct {
	faces map[*byte]*font.Face // go-text's face of each font file, made once
}

// A glyph shaped, in points from the start of the text
type glyph struct {
	id      int
	font    int
	cluster int // The index in the text, in runes, of its cluster's first character
	x, y    float32
	advance float32
}

func (shaper *goTextShaper) face(file []byte) (*font.Face, error) {
	if face, ok := shaper.faces[&file[0]]; ok {
		return face, nil
	}
	face, err := font.ParseTTF(bytes.NewReader(file))
	if err != nil {
		return nil, err
	}
	shaper.faces[&file[0]] = face
	return face, nil
}

// scriptOf returns the script of the first character of the runes that has
// one of its own, Latin if none has.
func scriptOf(runes []rune) language.Script {
	for _, r := range runes {
		if script := language.LookupScript(r); script != language.Common && script != language.Inherited {
			return script
		}
	}
	return language.Latin
}

func (shaper *goTextShaper) Shape(text, lang string, fontFiles [][]byte, fontSize, width float32) (
	[]pdfjet.ShapedLine, error) {
	faces := make([]*font.Face, len(fontFiles))
	for i, file := range fontFiles {
		face, err := shaper.face(file)
		if err != nil {
			return nil, err
		}
		faces[i] = face
	}
	runes := []rune(text)
	// The index in the text, in bytes, of each rune, and of the end
	offsets := make([]int, 0, len(runes)+1)
	for i := range text {
		offsets = append(offsets, i)
	}
	offsets = append(offsets, len(text))

	// Each character in the first font that has it, else in the first
	fontOf := make([]int, len(runes))
	for i, r := range runes {
		for k, face := range faces {
			if _, ok := face.NominalGlyph(r); ok {
				fontOf[i] = k
				break
			}
		}
	}
	// Each run of one font shaped on its own
	var glyphs []glyph
	var pen float32
	harfbuzz := shaping.HarfbuzzShaper{}
	for start := 0; start < len(runes); {
		end := start + 1
		for end < len(runes) && fontOf[end] == fontOf[start] {
			end++
		}
		face := faces[fontOf[start]]
		upem := int(face.Upem())
		// Shaped at a size of one pixel a font unit, then scaled to points
		output := harfbuzz.Shape(shaping.Input{
			Text: runes, RunStart: start, RunEnd: end,
			Direction: di.DirectionLTR, Face: face, Size: fixed.I(upem),
			Script: scriptOf(runes[start:end]), Language: language.NewLanguage(lang),
		})
		scale := fontSize / float32(upem) / 64
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

	// The lines: all the glyphs left if they fit, else up to the last space
	// after a word that fits
	var lines []pdfjet.ShapedLine
	for first := 0; first < len(glyphs); {
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
		line := pdfjet.ShapedLine{Start: offsets[glyphs[first].cluster], End: len(text)}
		if last < len(glyphs) {
			line.End = offsets[glyphs[last].cluster]
		}
		for i := first; i < last; i++ {
			line.Fonts = append(line.Fonts, glyphs[i].font)
			line.Glyphs = append(line.Glyphs, glyphs[i].id)
			line.Positions = append(line.Positions, glyphs[i].x-lineStart, glyphs[i].y)
		}
		lines = append(lines, line)
		first = last
	}
	return lines, nil
}

// drawPage draws a page of the Declaration in the language, its paragraphs
// from the beginning until the page is full, and returns the text drawn.
func drawPage(pdf *pdfjet.PDF, heading, body *pdfjet.Font, shaper pdfjet.Shaper, name, lang,
	scriptFile, textFile string) string {
	page := pdfjet.NewPage(pdf, letter.Portrait())
	pdfjet.NewTextLine(heading, "Universal Declaration of Human Rights, in "+name).
		SetStructureType(structelem.H1).
		SetLocation(left, 60).
		DrawOn(page)
	pdfjet.NewTextLine(body, "Shaped by go-text/typesetting, drawn by PDFjet's ComplexText as outlines;"+
		" the text is attached to the document.").
		SetLocation(left, 80).
		DrawOn(page)

	// The script's font, and IBM Plex Sans for the characters it does not
	// have, such as digits and punctuation of the Latin script
	script := pdfjet.NewFontFromFile(pdf, scriptFile)
	script.SetSize(size)
	latinFont := pdfjet.NewFontFromFile(pdf, latin)

	var drawn strings.Builder
	y := float32(96) // The top of the next paragraph
	for _, paragraph := range paragraphs(textFile) {
		text := pdfjet.NewComplexText(script, paragraph).
			SetFallbackFont(latinFont).
			SetLanguage(lang).
			SetShaper(shaper).
			SetWidth(width)
		height, err := text.GetHeight()
		if err != nil {
			log.Fatal(err)
		}
		if y+height > bottom {
			break // The page is full
		}
		y = text.SetLocation(left, y).DrawOn(page)[1] + 0.5*size
		// A blank line between paragraphs, as plain text has them
		if drawn.Len() > 0 {
			drawn.WriteString("\n")
		}
		drawn.WriteString(paragraph)
		drawn.WriteString("\n")
	}
	return drawn.String()
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

	shaper := &goTextShaper{faces: make(map[*byte]*font.Face)}
	hindi := drawPage(pdf, heading, body, shaper, "Hindi", "hi", devanagari,
		"resources/data/udhr/udhr_hin.txt")
	khmerText := drawPage(pdf, heading, body, shaper, "Khmer", "km", khmer,
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
