// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"os"
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

// Example_52 draws Hindi and Khmer as real text: go-text/typesetting, HarfBuzz
// in Go, shapes them -- conjuncts, vowel signs, stacked consonants -- and
// Page.DrawGlyphs draws the glyphs it found, at their places, with the text
// they stand for, so that copying, searching and screen readers get it.
// PDFjet does not shape these scripts itself; any engine that finds the
// glyphs can. The text is Article 1 of the Universal Declaration of Human
// Rights.
//
// The example is a module of its own: go-text is its dependency, never
// PDFjet's. Run it from the folder of the repository:
//
//	cd src/examples/example52 && go build -o ../../../Example_52.exe . && cd ../../.. && ./Example_52.exe

const (
	devanagari = "examples/fonts/NotoSansDevanagari/NotoSansDevanagari-Regular.ttf"
	khmer      = "examples/fonts/NotoSansKhmer/NotoSansKhmer-Regular.ttf"
	latin      = "resources/fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"
)

// A face of go-text, which shapes, and the same font file loaded by PDFjet,
// which embeds it, so that the glyph numbers match.
type shapingFont struct {
	face   *font.Face
	font   *pdfjet.Font
	script language.Script
}

func loadFont(pdf *pdfjet.PDF, path string, script language.Script, size float32) shapingFont {
	file, err := os.Open(path)
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	face, err := font.ParseTTF(file)
	if err != nil {
		log.Fatal(err)
	}
	embedded := pdfjet.NewFontFromFile(pdf, path)
	embedded.SetSize(size)
	return shapingFont{face, embedded, script}
}

// A glyph shaped, in points from the start of the paragraph
type glyph struct {
	id      int
	font    int // Its font: 0 the script's, 1 the Latin one
	cluster int // The index in the text, in runes, of its cluster's first character
	x, y    float32
	advance float32
}

// drawShaped draws a paragraph in a script PDFjet does not shape, wrapped to
// the width at spaces, as real text, and returns the y below its last line.
// The script's font is used where it has the characters, IBM Plex Sans
// elsewhere.
func drawShaped(pdf *pdfjet.PDF, page *pdfjet.Page, text, lang, scriptFile string, script language.Script,
	size, x, y, width float32) float32 {
	fonts := []shapingFont{
		loadFont(pdf, scriptFile, script, size),
		loadFont(pdf, latin, language.Latin, size),
	}
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

	// The line height from the script's font
	extents, _ := fonts[0].face.FontHExtents()
	em := size / float32(fonts[0].face.Upem())
	ascent, descent, gap := extents.Ascender*em, -extents.Descender*em, extents.LineGap*em

	page.AddBDC(structelem.P, lang, "", "")
	for first := 0; first < len(glyphs); {
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
		baseline := y + ascent
		// The line, a run of one font at a time: its glyphs, their places and
		// the characters each one comes from (its cluster)
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
			clusters := make([]int, end-start)
			for i := start; i < end; i++ {
				ids[i-start] = glyphs[i].id
				positions[2*(i-start)] = x + glyphs[i].x - lineStart
				positions[2*(i-start)+1] = baseline + glyphs[i].y
				clusters[i-start] = glyphs[i].cluster - textStart
			}
			page.DrawGlyphs(fonts[glyphs[start].font].font, ids, positions, clusters,
				string(runes[textStart:textEnd]), lang)
			start = end
		}
		y = baseline + descent + gap
		first = last
	}
	page.AddEMC()
	return y
}

// Example52 draws Article 1 of the Universal Declaration of Human Rights in
// Hindi and in Khmer.
func Example52() {
	pdf, err := pdfjet.NewPDFFile("Example_52.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Hindi and Khmer as real text") // Required for PDF/UA !
	pdf.SetLanguage("en")

	heading := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	heading.SetSize(16)
	body := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	body.SetSize(10)

	page := pdfjet.NewPage(pdf, letter.Portrait())
	pdfjet.NewTextLine(heading, "Article 1 of the Universal Declaration of Human Rights").
		SetStructureType(structelem.H1).
		SetLocation(50, 60).
		DrawOn(page)
	pdfjet.NewTextLine(body, "Shaped by go-text/typesetting, drawn by PDFjet with DrawGlyphs: real text, in Hindi and in Khmer.").
		SetLocation(50, 82).
		DrawOn(page)

	y := float32(100)
	y = drawShaped(pdf, page,
		"सभी मनुष्यों को गौरव और अधिकारों के मामले में जन्मजात स्वतन्त्रता और समानता प्राप्त है । "+
			"उन्हें बुद्धि और अन्तरात्मा की देन प्राप्त है और परस्पर उन्हें भाईचारे के भाव से बर्ताव करना चाहिए ।",
		"hi", devanagari, language.Devanagari, 12, 50, y, 512)
	drawShaped(pdf, page,
		"មនុស្សទាំងអស់ កើតមកមានសេរីភាព និងសមភាព ក្នុងផ្នែកសេចក្ដីថ្លៃថ្នូរនិងសិទ្ធិ។ "+
			"មនុស្ស មានវិចារណញ្ញាណនិងសតិសម្បជញ្ញៈជាប់ពីកំណើត "+
			"ហើយគប្បីប្រព្រឹត្ដចំពោះគ្នាទៅវិញទៅមកក្នុងស្មារតីភាតរភាពជាបងប្អូន។",
		"km", khmer, language.Khmer, 12, 50, y+14, 512)

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
