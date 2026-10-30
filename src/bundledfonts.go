// bundledfonts.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"os"
	"path/filepath"
	"slices"
	"strings"

	"github.com/edragoev1/pdfjet/v9/resources/fonts"
)

// The fonts PDFjet carries in itself, in resources/fonts, each family in a
// folder of its name, with its license: IBM Plex Sans and Serif, from Thin to
// Bold, and Cascadia Mono, from Light to Bold, upright and italic, for the
// European languages; and, in resources/fonts/world, IBM Plex Sans Arabic,
// Hebrew and Thai, Thin to Bold, and IBM Plex Sans JP, KR, SC and TC,
// Regular, SemiBold and Bold, as .otf. All are under the SIL Open
// Font License 1.1. A path to one of them, such as
// fonts/IBMPlexSans/IBMPlexSans-Regular.ttf, the constants of the packages
// IBMPlexSans and the others, is read from the folder resources/fonts of the
// working directory when there is no such file, or else from the library,
// and the text of an SVG image is drawn in them unless a font is set. The
// package fonts of resources/fonts embeds the European fonts; a program
// builds in the others by importing the package world (worldImport): Go
// builds the files embedded into every program, uncompressed, and they are
// 53 MB.

// bundledFolder is the folder of the fonts PDFjet carries, in the working
// directory, as in the repository and the packages of the releases.
const bundledFolder = "resources/fonts"

// worldFaces are the families that the package world builds in, and the
// files of the faces of each that PDFjet carries.
var worldFaces = map[string][]string{}

func init() {
	cjk := []string{"Regular", "SemiBold", "Bold"}
	thinToBold := []string{"Thin", "Light", "Text", "Regular", "Medium", "SemiBold", "Bold"}
	for _, family := range []string{"IBMPlexSansJP", "IBMPlexSansKR", "IBMPlexSansSC", "IBMPlexSansTC"} {
		for _, style := range cjk {
			worldFaces[family] = append(worldFaces[family], family+"-"+style+".otf")
		}
	}
	for _, family := range []string{"IBMPlexSansArabic", "IBMPlexSansHebrew", "IBMPlexSansThai"} {
		for _, style := range thinToBold {
			worldFaces[family] = append(worldFaces[family], family+"-"+style+".ttf")
		}
	}
}

// worldImport is the line that builds the fonts of the package world into a
// program.
const worldImport = `import _ "github.com/edragoev1/pdfjet/v9/resources/fonts/world"`

// worldScripts says what the fonts of the package world are for.
const worldScripts = "with PDFjet's fonts for Chinese, Japanese, Korean, Arabic, Hebrew and Thai"

// bundledFamilyName returns the name of a family as people write it, such
// as IBM Plex Sans Arabic of IBMPlexSansArabic.
func bundledFamilyName(family string) string {
	name := strings.Replace(family, "IBMPlex", "IBM Plex ", 1)
	return strings.Replace(name, "Sans", "Sans ", 1)
}

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

// bundledFontFolderPath returns the path under resources/fonts of a font
// PDFjet carries, by its name, Family/File: the name itself, or, for the
// fonts of the package world, which are apart so that a program, a jar or a
// package carries them only when it asks for them, world/ and the name, such
// as world/IBMPlexSansJP/IBMPlexSansJP-Regular.otf.
func bundledFontFolderPath(name string) string {
	family, _, _ := strings.Cut(name, "/")
	if _, ok := worldFaces[family]; ok {
		return "world/" + name
	}
	return name
}

// bundledFontData returns the font of the path that PDFjet carries, from the
// folder resources/fonts or else from the library, or nil.
func bundledFontData(fontPath string) []byte {
	name := bundledFontName(fontPath)
	if name == "" || strings.Contains(name, "..") {
		return nil
	}
	if buf, err := os.ReadFile(filepath.Join(bundledFolder, filepath.FromSlash(bundledFontFolderPath(name)))); err == nil {
		return buf
	}
	if buf, err := fonts.ReadFile(name); err == nil {
		return buf
	}
	return nil
}

// bundledFontHas tells if PDFjet carries the font of the path, in the folder
// resources/fonts or in the library.
func bundledFontHas(fontPath string) bool {
	name := bundledFontName(fontPath)
	if name == "" || strings.Contains(name, "..") {
		return false
	}
	if info, err := os.Stat(filepath.Join(bundledFolder, filepath.FromSlash(bundledFontFolderPath(name)))); err == nil && info.Mode().IsRegular() {
		return true
	}
	_, err := fonts.Stat(name)
	return err == nil
}

// bundledWorldMissing returns what to do when the font of the path is one of
// the fonts of the package world that PDFjet carries, which neither the
// folder resources/fonts/world nor the program has, or "" for any other
// font: import the package world.
func bundledWorldMissing(fontPath string) string {
	family, file, _ := strings.Cut(bundledFontName(fontPath), "/")
	faces, ok := worldFaces[family]
	if !ok || !slices.Contains(faces, file) || bundledFontHas(fontPath) {
		return ""
	}
	return fontPath + " is not a file, nor in resources/fonts/world. " + bundledFamilyName(family) +
		" is in the package world, " + worldScripts + ". Add this line to your program: " + worldImport
}

// The families of the fonts PDFjet carries that SVG text is drawn in.
const (
	bundledSans = iota
	bundledSerif
	bundledMono
)

var bundledFamilies = []string{"IBMPlexSans", "IBMPlexSerif", "CascadiaMono"}

// bundledWeights are the CSS weights the faces of each family stand for, and
// bundledStyles their styles. Cascadia Mono's strokes are heavier than IBM
// Plex Sans's at the same weight: its SemiLight is as thick as Plex Sans
// Regular, so it stands for 400, and its Regular for 500, as in PDFjet Forms
// (the owner, 29 October 2026); its SemiBold and Bold match Plex Sans's as
// they are.
var bundledWeights = [][]int{
	{100, 300, 400, 450, 500, 600, 700},
	{100, 300, 400, 450, 500, 600, 700},
	{300, 400, 500, 600, 700},
}

var bundledStyles = [][]string{
	{"Thin", "Light", "Regular", "Text", "Medium", "SemiBold", "Bold"},
	{"Thin", "Light", "Regular", "Text", "Medium", "SemiBold", "Bold"},
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
	buf := bundledFontData(name)
	if buf == nil {
		panic("Reading the font " + name + ": not in resources/fonts nor in the library")
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
		"source serif pro", "ibm plex serif"},
	{"monospace", "courier", "courier new", "consolas", "menlo", "monaco", "ui-monospace",
		"sfmono-regular", "lucida console", "liberation mono", "dejavu sans mono",
		"jetbrains mono", "ibm plex mono", "source code pro", "cascadia mono", "cascadia code"},
}
