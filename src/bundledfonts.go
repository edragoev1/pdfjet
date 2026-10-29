// bundledfonts.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"embed"
	"strings"
)

// The fonts PDFjet carries in itself, those of PDFjet Forms: IBM Plex Sans,
// Source Serif 4 and JetBrains Mono, each regular, semibold and bold, upright
// and italic, under the SIL Open Font License 1.1, whose texts are next to
// them in src/bundledfonts. An SVG image draws its text in them unless a font
// is set: embedded, as PDF/A and PDF/UA ask, where the core fonts are not.
//
//go:embed bundledfonts/*.ttf
var bundledFontFiles embed.FS

// The families of the fonts PDFjet carries.
const (
	bundledSans = iota
	bundledSerif
	bundledMono
)

var bundledFamilies = []string{"IBMPlexSans", "SourceSerif4", "JetBrainsMono"}

// bundledFontFile returns the name of the file of a face: a weight below 600
// is regular, from 600 to 699 semibold, and 700 or more bold.
func bundledFontFile(family, weight int, italic bool) string {
	style := ""
	if weight >= 700 {
		style = "Bold"
	} else if weight >= 600 {
		style = "SemiBold"
	}
	if italic {
		style += "Italic"
	}
	if style == "" {
		style = "Regular"
	}
	return bundledFamilies[family] + "-" + style + ".ttf"
}

// bundledFont returns the face for the document, read once for it.
func bundledFont(pdf *PDF, family, weight int, italic bool) *Font {
	name := bundledFontFile(family, weight, italic)
	if font, ok := pdf.bundledFonts[name]; ok {
		return font
	}
	buf, err := bundledFontFiles.ReadFile("bundledfonts/" + name)
	if err != nil {
		panic("Reading the bundled font " + name + ": " + err.Error())
	}
	font := new(Font)
	font.pdf = pdf
	registerOpenTypeFont(pdf, font, buf)
	font.SetSize(defaultFontSize)
	if pdf.bundledFonts == nil {
		pdf.bundledFonts = make(map[string]*Font)
	}
	pdf.bundledFonts[name] = font
	return font
}

// bundledFamilyOf returns the family of a font-family list of an SVG file:
// the first of its names that PDFjet knows, as serif, monospace or
// sans-serif, and sans-serif for none.
func bundledFamilyOf(fontFamily string) int {
	for _, name := range strings.Split(fontFamily, ",") {
		name = strings.ToLower(strings.TrimSpace(name))
		if len(name) >= 2 && (name[0] == '"' || name[0] == '\'') && name[len(name)-1] == name[0] {
			name = strings.TrimSpace(name[1 : len(name)-1])
		}
		for family, names := range bundledFontNames {
			for _, known := range names {
				if name == known {
					return family
				}
			}
		}
	}
	return bundledSans
}

// bundledFontNames are the names of the fonts of each family, in lower case:
// the generic one, the core fonts and those that systems and charts name most.
var bundledFontNames = [][]string{
	{"sans-serif", "helvetica", "arial", "system-ui", "-apple-system", "blinkmacsystemfont",
		"segoe ui", "roboto", "verdana", "tahoma", "trebuchet ms", "open sans", "noto sans",
		"liberation sans", "dejavu sans", "ibm plex sans", "inter"},
	{"serif", "times", "times new roman", "georgia", "garamond", "cambria", "palatino",
		"book antiqua", "liberation serif", "dejavu serif", "noto serif", "source serif 4",
		"source serif pro"},
	{"monospace", "courier", "courier new", "consolas", "menlo", "monaco", "ui-monospace",
		"sfmono-regular", "lucida console", "liberation mono", "dejavu sans mono",
		"jetbrains mono", "ibm plex mono", "source code pro"},
}
