// bundledfonts.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"io/fs"
	"strings"

	"github.com/edragoev1/pdfjet/v9/resources/fonts"
)

// The fonts PDFjet carries in itself, in resources/fonts, each family in a
// folder of its name, as in the folder fonts of pdfjet-fonts: IBM Plex Sans
// and Serif, from Thin to Bold, Source Serif 4 and Cascadia Mono, from Light
// to Bold, upright and italic; IBM Plex Sans Arabic, Hebrew and Thai, Thin to
// Bold; IBM Plex Math; and IBM Plex Sans JP, KR, SC and TC, Regular, SemiBold
// and Bold, as .otf. All are under the SIL Open Font License 1.1. The package
// fonts of resources/fonts embeds them. A path to one of them, such as
// fonts/IBMPlexSans/IBMPlexSans-Regular.ttf, the constants of the packages
// IBMPlexSans and the others, is read from the library when there is no such
// file, and the text of an SVG image is drawn in them unless a font is set.

// pdfjetFonts is the repository of the fonts PDFjet came with before 9.1.7,
// which has those it does not carry, such as Noto Sans.
const pdfjetFonts = "https://github.com/edragoev1/pdfjet-fonts"

// bundledFontName returns the name of a font under resources/fonts,
// Family/File, of a path to it, such as
// fonts/IBMPlexSans/IBMPlexSans-Regular.ttf: its last two parts, or "" for a
// path of fewer.
func bundledFontName(fontPath string) string {
	path := strings.ReplaceAll(fontPath, "\\", "/")
	file := strings.LastIndex(path, "/")
	if file <= 0 {
		return ""
	}
	family := strings.LastIndex(path[:file], "/")
	return path[family+1:]
}

// bundledFontData returns the font of the path that PDFjet carries, or nil.
func bundledFontData(fontPath string) []byte {
	if !bundledFontHas(fontPath) {
		return nil
	}
	buf, err := fonts.FS.ReadFile(bundledFontName(fontPath))
	if err != nil {
		return nil
	}
	return buf
}

// bundledFontHas tells if PDFjet carries the font of the path.
func bundledFontHas(fontPath string) bool {
	name := bundledFontName(fontPath)
	if name == "" || strings.Contains(name, "..") {
		return false
	}
	_, err := fs.Stat(fonts.FS, name)
	return err == nil
}

// The families of the fonts PDFjet carries that SVG text is drawn in.
const (
	bundledSans = iota
	bundledSerif
	bundledMono
)

var bundledFamilies = []string{"IBMPlexSans", "SourceSerif4", "CascadiaMono"}

// bundledWeights are the CSS weights the faces of each family stand for, and
// bundledStyles their styles. Cascadia Mono's strokes are heavier than IBM
// Plex Sans's at the same weight: its SemiLight is as thick as Plex Sans
// Regular, so it stands for 400, and its Regular for 500, as in PDFjet Forms
// (the owner, 29 October 2026); its SemiBold and Bold match Plex Sans's as
// they are.
var bundledWeights = [][]int{
	{100, 300, 400, 450, 500, 600, 700},
	{300, 400, 500, 600, 700},
	{300, 400, 500, 600, 700},
}

var bundledStyles = [][]string{
	{"Thin", "Light", "Regular", "Text", "Medium", "SemiBold", "Bold"},
	{"Light", "Regular", "Medium", "SemiBold", "Bold"},
	{"Light", "SemiLight", "Regular", "SemiBold", "Bold"},
}

// bundledFontFile returns the name of a face of an SVG family, Family/File:
// the weight CSS matches to the one asked for among those the family has, and
// italic or upright.
func bundledFontFile(family, weight int, italic bool) string {
	w := matchWeight(bundledWeights[family], weight)
	style := "Regular"
	for i, fw := range bundledWeights[family] {
		if fw == w {
			style = bundledStyles[family][i]
		}
	}
	if italic {
		if style == "Regular" {
			style = "Italic"
		} else {
			style += "Italic"
		}
	}
	return bundledFamilies[family] + "/" + bundledFamilies[family] + "-" + style + ".ttf"
}

// matchWeight returns the weight CSS matches to the one asked for, of the
// weights a family has in order (CSS Fonts 4, 5.2): that weight; from 400 to
// 500, the weights up to 500, then the lighter ones, then the heavier ones;
// below 400, the lighter ones, then the heavier ones; above 500, the heavier
// ones, then the lighter ones; each nearest first.
func matchWeight(weights []int, weight int) int {
	lighter := -1 // The nearest lighter weight
	heavier := -1 // The nearest heavier weight
	upTo500 := -1 // The nearest heavier weight up to 500
	for _, w := range weights {
		if w == weight {
			return w
		} else if w < weight {
			lighter = w
		} else if heavier < 0 {
			heavier = w
		}
		if w > weight && w <= 500 && upTo500 < 0 {
			upTo500 = w
		}
	}
	if weight >= 400 && weight <= 500 && upTo500 >= 0 {
		return upTo500
	}
	if weight <= 500 {
		if lighter >= 0 {
			return lighter
		}
		return heavier
	}
	if heavier >= 0 {
		return heavier
	}
	return lighter
}

// bundledFont returns the face for the document, read once for it.
func bundledFont(pdf *PDF, family, weight int, italic bool) *Font {
	name := bundledFontFile(family, weight, italic)
	if font, ok := pdf.bundledFonts[name]; ok {
		return font
	}
	buf, err := fonts.FS.ReadFile(name)
	if err != nil {
		panic("Reading the font " + name + ": " + err.Error())
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
		"jetbrains mono", "ibm plex mono", "source code pro", "cascadia mono", "cascadia code"},
}
