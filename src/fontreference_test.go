// fontreference_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"bytes"
	"encoding/json"
	"fmt"
	"io"
	"math"
	"os"
	"runtime"
	"strconv"
	"strings"
	"sync"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/corefont"
)

// TestFontReference is the Go port's side of the font check,
// tests/references/fonts/check-fonts.py, which runs it as
//
//	PDFJET_FONT_REFERENCE=LIST PDFJET_FONT_REFERENCE_OUT=OUT go test ./src -run '^TestFontReference$'
//
// LIST has a font on each line: the absolute path of a .otf, .ttf or .stream
// file, or core/NAME for one of the 14 core fonts. Each font is loaded as a
// user loads it, with NewFontFromFile or NewCoreFont, and what PDFjet read is
// written to OUT as a line of JSON: the values it keeps, the glyph of each
// character, the advance widths, where the GPOS table puts the marks, and the
// width StringWidth gives each character of the BMP. A font PDFjet refuses has
// the message it panics with. Without PDFJET_FONT_REFERENCE the test is
// skipped; it is a harness, not a test of its own.
func TestFontReference(t *testing.T) {
	list := os.Getenv("PDFJET_FONT_REFERENCE")
	if list == "" {
		t.Skip("PDFJET_FONT_REFERENCE names no list of fonts")
	}
	data, err := os.ReadFile(list)
	if err != nil {
		t.Fatal(err)
	}
	out, err := os.Create(os.Getenv("PDFJET_FONT_REFERENCE_OUT"))
	if err != nil {
		t.Fatal(err)
	}
	defer out.Close()
	writer := bufio.NewWriter(out)
	defer writer.Flush()

	var paths []string
	for _, line := range strings.Split(string(data), "\n") {
		if line = strings.TrimSpace(line); line != "" {
			paths = append(paths, line)
		}
	}
	var mutex sync.Mutex
	work := make(chan string)
	var wg sync.WaitGroup
	for range runtime.NumCPU() {
		wg.Add(1)
		go func() {
			defer wg.Done()
			for path := range work {
				line, err := json.Marshal(fontReferenceOf(path))
				if err != nil {
					line, _ = json.Marshal(map[string]string{"path": path, "error": err.Error()})
				}
				mutex.Lock()
				writer.Write(line)
				writer.WriteByte('\n')
				mutex.Unlock()
			}
		}()
	}
	for _, path := range paths {
		work <- path
	}
	close(work)
	wg.Wait()
}

// fontReference is what PDFjet read of a font, in font units.
type fontReference struct {
	Path               string `json:"path"`
	Error              string `json:"error,omitempty"`
	Name               string `json:"name"`
	UnitsPerEm         int    `json:"unitsPerEm"`
	BBox               [4]int `json:"bbox"`
	Ascent             int    `json:"ascent"`
	Descent            int    `json:"descent"`
	LineGap            int    `json:"lineGap"`
	CapHeight          int    `json:"capHeight"`
	UnderlinePosition  int    `json:"underlinePosition"`
	UnderlineThickness int    `json:"underlineThickness"`
	FirstChar          int    `json:"firstChar"`
	LastChar           int    `json:"lastChar"`
	CFF                bool   `json:"cff"`
	// The advance widths, one for each of the first glyphs.
	Widths []int `json:"widths"`
	// The glyph of each character of the BMP that has one, as [char, glyph].
	CMap [][2]int `json:"cmap"`
	// The width StringWidth gives each character of the BMP but the
	// surrogates, at a size of one em, as runs [first, last, width].
	Advances [][3]float64 `json:"advances"`
	// Where the GPOS table puts the marks, by glyph: for each MarkToBase and
	// MarkToLigature subtable the [class, x, y] of each mark and the anchors
	// of each base, and the [dx, dy] of each mark on a mark, by "mark2 mark1".
	MarkAnchors []map[string][]int `json:"markAnchors"`
	BaseAnchors []map[string][]int `json:"baseAnchors"`
	MarkToMark  map[string][2]int  `json:"markToMark"`

	// A core font: the width of each code of its encoding, the kerning pairs
	// of its table as [code1, code2, kerning], the character that each code
	// is drawn for, and the kerning StringWidth gives each pair of those
	// characters, as [char1, char2, kerning].
	CoreWidths  map[string]int `json:"coreWidths,omitempty"`
	CorePairs   [][3]int       `json:"corePairs,omitempty"`
	CoreChars   map[string]int `json:"coreChars,omitempty"`
	CoreKerning [][3]int       `json:"coreKerning,omitempty"`
}

var fontReferenceCoreFonts = map[string]func() *corefont.CoreFont{
	"Courier":               corefont.Courier,
	"Courier-Bold":          corefont.CourierBold,
	"Courier-BoldOblique":   corefont.CourierBoldOblique,
	"Courier-Oblique":       corefont.CourierOblique,
	"Helvetica":             corefont.Helvetica,
	"Helvetica-Bold":        corefont.HelveticaBold,
	"Helvetica-BoldOblique": corefont.HelveticaBoldOblique,
	"Helvetica-Oblique":     corefont.HelveticaOblique,
	"Symbol":                corefont.Symbol,
	"Times-Bold":            corefont.TimesBold,
	"Times-BoldItalic":      corefont.TimesBoldItalic,
	"Times-Italic":          corefont.TimesItalic,
	"Times-Roman":           corefont.TimesRoman,
	"ZapfDingbats":          corefont.ZapfDingbats,
}

func fontReferenceOf(path string) (ref *fontReference) {
	ref = &fontReference{Path: path}
	defer func() {
		if r := recover(); r != nil {
			ref = &fontReference{Path: path, Error: fmt.Sprint(r)}
		}
	}()
	pdf := NewPDF(bufio.NewWriter(io.Discard))
	if name, ok := strings.CutPrefix(path, "core/"); ok {
		newCoreFont, ok := fontReferenceCoreFonts[name]
		if !ok {
			panic("no core font " + name)
		}
		fontReferenceOfCoreFont(ref, NewCoreFont(pdf, newCoreFont()))
		return ref
	}

	font := NewFontFromFile(pdf, path)
	ref.CapHeight = int(font.capHeight)
	ref.CFF = font.cff
	if !strings.HasSuffix(path, ".stream") {
		// The cap height and the kind of outlines of an OpenType font go
		// into its descriptor from what newOpenTypeFont read; the Font does
		// not keep them.
		data, err := os.ReadFile(path)
		if err != nil {
			panic(err)
		}
		otf := newOpenTypeFont(bytes.NewReader(data))
		ref.CapHeight = int(otf.capHeight)
		ref.CFF = otf.cff
	}
	if font.markData != nil {
		readMarks(font)
	}
	ref.Name = font.name
	ref.UnitsPerEm = font.unitsPerEm
	ref.BBox = [4]int{int(font.bBoxLLx), int(font.bBoxLLy), int(font.bBoxURx), int(font.bBoxURy)}
	ref.Ascent = int(font.fontAscent)
	ref.Descent = int(font.fontDescent)
	ref.LineGap = int(font.fontLineGap)
	ref.UnderlinePosition = int(font.fontUnderlinePosition)
	ref.UnderlineThickness = int(font.fontUnderlineThickness)
	ref.FirstChar = int(font.firstChar)
	ref.LastChar = int(font.lastChar)
	ref.Widths = make([]int, len(font.advanceWidth))
	for i, width := range font.advanceWidth {
		ref.Widths[i] = int(width)
	}
	ref.CMap = [][2]int{}
	for c, gid := range font.unicodeToGID {
		if gid != 0 {
			ref.CMap = append(ref.CMap, [2]int{c, gid})
		}
	}
	ref.Advances = [][3]float64{}
	size := float32(font.unitsPerEm)
	for c := rune(0); c <= 0xFFFF; c++ {
		if c >= 0xD800 && c <= 0xDFFF {
			continue // A surrogate is not a character of a Go string.
		}
		width := float64(font.StringWidth(size, string(c)))
		if n := len(ref.Advances); n > 0 && ref.Advances[n-1][1] == float64(c-1) &&
			ref.Advances[n-1][2] == width {
			ref.Advances[n-1][1] = float64(c)
		} else {
			ref.Advances = append(ref.Advances, [3]float64{float64(c), float64(c), width})
		}
	}
	ref.MarkAnchors = []map[string][]int{}
	for _, marks := range font.markAnchors {
		ref.MarkAnchors = append(ref.MarkAnchors, fontReferenceByGlyph(marks))
	}
	ref.BaseAnchors = []map[string][]int{}
	for _, bases := range font.baseAnchors {
		ref.BaseAnchors = append(ref.BaseAnchors, fontReferenceByGlyph(bases))
	}
	ref.MarkToMark = map[string][2]int{}
	for key, offset := range font.markToMarkOffsets {
		ref.MarkToMark[strconv.Itoa(key>>16)+" "+strconv.Itoa(key&0xFFFF)] = offset
	}
	return ref
}

func fontReferenceByGlyph(anchors map[int][]int) map[string][]int {
	byGlyph := make(map[string][]int, len(anchors))
	for gid, values := range anchors {
		byGlyph[strconv.Itoa(gid)] = values
	}
	return byGlyph
}

func fontReferenceOfCoreFont(ref *fontReference, font *Font) {
	ref.Name = font.name
	ref.UnitsPerEm = font.unitsPerEm
	ref.BBox = [4]int{int(font.bBoxLLx), int(font.bBoxLLy), int(font.bBoxURx), int(font.bBoxURy)}
	ref.Ascent = int(font.fontAscent)
	ref.Descent = int(font.fontDescent)
	ref.UnderlinePosition = int(font.fontUnderlinePosition)
	ref.UnderlineThickness = int(font.fontUnderlineThickness)
	ref.FirstChar = int(font.firstChar)
	ref.LastChar = int(font.lastChar)
	ref.CoreWidths = map[string]int{}
	for i, row := range font.metrics {
		if row[0] != i+32 {
			panic(fmt.Sprintf("the metrics of code %d are in row %d", row[0], i))
		}
		ref.CoreWidths[strconv.Itoa(row[0])] = row[1]
		for j := 2; j+1 < len(row); j += 2 {
			ref.CorePairs = append(ref.CorePairs, [3]int{row[0], row[j], row[j+1]})
		}
	}
	// The character each code is drawn for: the one coreFontCode gives the
	// code, and for the space U+0020, not the characters drawn as a space.
	ref.CoreChars = map[string]int{}
	var chars []rune
	for c := rune(0x20); c <= 0xFFFF; c++ {
		code := font.coreFontCode(c)
		if code == 32 && c != 32 {
			continue
		}
		if _, ok := ref.CoreChars[strconv.Itoa(code)]; ok {
			panic(fmt.Sprintf("two characters have the code %d", code))
		}
		ref.CoreChars[strconv.Itoa(code)] = int(c)
		chars = append(chars, c)
	}
	ref.Advances = [][3]float64{}
	for _, c := range chars {
		width := float64(font.StringWidth(1000, string(c)))
		ref.Advances = append(ref.Advances, [3]float64{float64(c), float64(c), width})
	}
	font.SetKernPairs(true)
	for _, c1 := range chars {
		w1 := font.StringWidth(1000, string(c1))
		for _, c2 := range chars {
			pair := font.StringWidth(1000, string([]rune{c1, c2}))
			if kerning := pair - w1 - font.StringWidth(1000, string(c2)); kerning != 0 {
				ref.CoreKerning = append(ref.CoreKerning, [3]int{int(c1), int(c2), int(math.Round(float64(kerning)))})
			}
		}
	}
	font.SetKernPairs(false)
}
