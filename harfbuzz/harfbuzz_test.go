// harfbuzz_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package harfbuzz

import (
	"bufio"
	"bytes"
	"os"
	"os/exec"
	"reflect"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

const world = "resources/fonts/world/"

var languages = []struct{ name, lang, font, text string }{
	{"Hindi", "hi", world + "NotoSansDevanagari/NotoSansDevanagari-Regular.otf", "resources/data/udhr/udhr_hin.txt"},
	{"Khmer", "km", world + "NotoSansKhmer/NotoSansKhmer-Regular.otf", "resources/data/udhr/udhr_khm.txt"},
	{"Lao", "lo", world + "NotoSansLao/NotoSansLao-Regular.otf", "resources/data/udhr/udhr_lao.txt"},
	{"Burmese", "my", world + "NotoSansMyanmar/NotoSansMyanmar-Regular.otf", "resources/data/udhr/udhr_mya.txt"},
}

func paragraphs(t *testing.T, path string) []string {
	t.Helper()
	data, err := os.ReadFile(path)
	if err != nil {
		t.Fatal(err)
	}
	var list []string
	scanner := bufio.NewScanner(bytes.NewReader(data))
	scanner.Buffer(nil, 1<<20)
	for scanner.Scan() {
		if line := strings.TrimSpace(scanner.Text()); line != "" {
			list = append(list, line)
		}
	}
	return list
}

func read(t *testing.T, path string) []byte {
	t.Helper()
	data, err := os.ReadFile(path)
	if err != nil {
		t.Fatal(err)
	}
	return data
}

// go-text gives the same lines as hb-shape, glyph for glyph and place for
// place, on the Universal Declaration of Human Rights in Hindi, Khmer, Lao
// and Burmese, with IBM Plex Sans for what the script's font has not.
func TestShapesAsHBShape(t *testing.T) {
	if _, err := exec.LookPath("hb-shape"); err != nil {
		t.Skip("hb-shape is not installed")
	}
	t.Chdir("..")
	latin := read(t, "resources/fonts/IBMPlexSans/IBMPlexSans-Regular.otf")
	goText, hbShape := NewShaper(), pdfjet.NewHBShape()
	for _, l := range languages {
		files := [][]byte{read(t, l.font), latin}
		count := 0
		for _, paragraph := range paragraphs(t, l.text) {
			request := pdfjet.ShapingRequest{Text: paragraph, Language: l.lang, FontFiles: files, FontSize: 10.5, Width: 512}
			got, err := goText.Shape(request)
			if err != nil {
				t.Fatalf("%s: %v", l.name, err)
			}
			want, err := hbShape.Shape(request)
			if err != nil {
				t.Fatalf("%s: hb-shape: %v", l.name, err)
			}
			if !reflect.DeepEqual(got, want) {
				t.Errorf("%s: a paragraph shaped otherwise than by hb-shape: %.40q", l.name, paragraph)
			}
			count += len(got)
		}
		t.Logf("%s: %d lines alike", l.name, count)
	}
}

// Imported, the module is the shaper of a ComplexText given none, without
// hb-shape: the Khmer is drawn, its PDF complete.
func TestImportedItIsTheDefault(t *testing.T) {
	t.Chdir("..")
	t.Setenv("PATH", t.TempDir()) // No hb-shape
	var out bytes.Buffer
	w := bufio.NewWriter(&out)
	pdf := pdfjet.NewPDF(w)
	khmer := pdfjet.NewFontFromFile(pdf, languages[1].font)
	khmer.SetSize(12)
	text := pdfjet.NewComplexTextFromList(khmer, paragraphs(t, languages[1].text)[:3]).SetWidth(300)
	text.SetLocation(50, 50).DrawOn(pdfjet.NewPage(pdf, letter.Portrait()))
	if err := pdf.Complete(); err != nil {
		t.Fatal(err)
	}
	w.Flush()
	if out.Len() < 1000 {
		t.Errorf("a PDF of %d bytes", out.Len())
	}
}
