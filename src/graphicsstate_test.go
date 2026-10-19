// graphicsstate_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"regexp"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/blendmode"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// The blend mode of a graphics state, a state set again on a page that was
// read, and the fields of a form that is drawn on (from Stefan Ostermann's
// fork of PDFjet, 19 October 2026).

func TestGraphicsStateTheBlendModeIsWrittenInTheState(t *testing.T) {
	doc := testNewDoc()
	page := NewPage(doc.pdf, letter.Portrait())
	page.SetGraphicsState(NewGraphicsState().SetAlphaNonStroking(0.5).SetBlendMode(blendmode.Multiply))
	page.SetGraphicsState(NewGraphicsState().SetAlphaNonStroking(0.5))
	page.SetGraphicsState(NewGraphicsState().SetAlphaNonStroking(0.5).SetBlendMode(blendmode.Multiply))
	page.SetGraphicsState(NewGraphicsState().SetBlendMode(blendmode.ColorDodge))
	content := regexp.MustCompile(`(?s)^.*?(/GS1 gs)`).ReplaceAllString(testContent(page), "$1")
	testWant(t, "/GS1 gs\n/GS2 gs\n/GS1 gs\n/GS3 gs\n", content)
	out := string(doc.complete())
	for _, want := range []string{
		"/GS1 <</CA 1 /ca 0.5 /BM /Multiply>>",
		"/GS2 <</CA 1 /ca 0.5>>",
		"/GS3 <</CA 1 /ca 1 /BM /ColorDodge>>",
	} {
		if !strings.Contains(out, want) {
			t.Errorf("no %q in\n%s", want, out)
		}
	}
}

func TestGraphicsStateAStateWithoutABlendModeIsWrittenAsBefore(t *testing.T) {
	gs := NewGraphicsState().SetAlphaStroking(0.25)
	testWant(t, "", string(gs.GetBlendMode()))
	testWant(t, "/CA 0.25 /ca 1", gs.entries())
	gs.SetBlendMode(blendmode.Multiply).SetBlendMode("")
	testWant(t, "/CA 0.25 /ca 1", gs.entries())
}

func TestGraphicsStatePDFA1TakesOnlyTheNormalBlendMode(t *testing.T) {
	failed := "The PDF was not completed because of an earlier error: "
	multiply := func(pdf *PDF, page *Page) {
		page.SetGraphicsState(NewGraphicsState().SetBlendMode(blendmode.Multiply))
	}
	got, _ := testPDFADrawError(t, compliance.PDF_A_1B, multiply)
	testWant(t, failed+"A document of PDF_A_1B cannot draw with the blend mode Multiply: "+
		"PDF/A-1 has no transparency.", got)
	got, _ = testPDFADrawError(t, compliance.PDF_A_2B, multiply)
	testWant(t, "", got)
	got, _ = testPDFADrawError(t, compliance.PDF_A_1B, func(pdf *PDF, page *Page) {
		page.SetGraphicsState(NewGraphicsState().SetBlendMode(blendmode.Normal))
	})
	testWant(t, "", got)
}

func TestGraphicsStateTheSameStateSetAgainOnAPageThatWasReadIsReused(t *testing.T) {
	objects := testRead(t, testPDFWithObjects(
		"<< /Type /Catalog /Pages 2 0 R >>",
		"<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
		"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 200] "+
			"/Resources << /ExtGState << /Gs1 << /CA 1 /ca 1 >> >> >> /Contents 4 0 R >>",
		"<< /Length 7 >>\nstream\n/Gs1 gs\nendstream"))
	page := testNewPDF().GetPageObjects(objects)[0]
	highlighter := NewGraphicsState().SetAlphaNonStroking(0.5).SetBlendMode(blendmode.Multiply)
	page.SetGraphicsState(highlighter, &objects)
	page.SetGraphicsState(NewGraphicsState().SetAlphaNonStroking(0.5), &objects)
	page.SetGraphicsState(highlighter, &objects)
	dict := strings.Join(page.dict, " ")
	for _, want := range []string{
		"/GS1 << /CA 1 /ca 0.5 /BM /Multiply >>",
		"/GS2 << /CA 1 /ca 0.5 >>",
		"/Gs1 << /CA 1 /ca 1 >>",
	} {
		if !strings.Contains(dict, want) {
			t.Errorf("no %q in %s", want, dict)
		}
	}
	if strings.Contains(dict, "/GS3") {
		t.Errorf("a state was added again: %s", dict)
	}
}

// A filled-in form that is drawn on keeps its fields: the catalog of the
// document carries over the /AcroForm of the one that was read, which a viewer
// needs to show the values. Without one, there is none.
func TestGraphicsStateAFormThatIsDrawnOnKeepsItsFields(t *testing.T) {
	form := []string{
		"<< /Type /Catalog /Pages 2 0 R /AcroForm << /Fields [4 0 R] /NeedAppearances true >> >>",
		"<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
		"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 200] /Annots [4 0 R] /Resources << >> /Contents 5 0 R >>",
		"<< /Type /Annot /Subtype /Widget /FT /Tx /T (name) /V (Alice) /Rect [20 100 200 130] /P 3 0 R >>",
		"<< /Length 5 >>\nstream\nBT ET\nendstream",
	}
	byReference := append([]string{"<< /Type /Catalog /Pages 2 0 R /AcroForm 6 0 R >>"}, form[1:]...)
	noForm := append([]string{"<< /Type /Catalog /Pages 2 0 R >>"}, form[1:]...)
	cases := [][]string{form, byReference, noForm}
	expected := []string{"/AcroForm << /Fields [ 4 0 R ] /NeedAppearances true >>\n", "/AcroForm 6 0 R\n", ""}
	for c, objectsOfCase := range cases {
		objects := testRead(t, testPDFWithObjects(objectsOfCase...))
		page := testNewPDF().GetPageObjects(objects)[0]
		page.AddContent([]byte("0 0 m 10 10 l S\n"), &objects)
		doc := testNewDoc()
		if err := doc.pdf.AddObjects(objects); err != nil {
			t.Fatal(err)
		}
		out := string(doc.complete())
		root := regexp.MustCompile(`/Root (\d+) 0 R`).FindStringSubmatch(out)
		if root == nil {
			t.Fatalf("no /Root in\n%s", out)
		}
		start := strings.Index(out, "\n"+root[1]+" 0 obj\n") + 1
		catalog := out[start : start+strings.Index(out[start:], "endobj")]
		if expected[c] == "" {
			if strings.Contains(catalog, "/AcroForm") {
				t.Errorf("case %d: an /AcroForm in\n%s", c, catalog)
			}
		} else if !strings.Contains(catalog, expected[c]) {
			t.Errorf("case %d: no %q in\n%s", c, expected[c], catalog)
		}
	}
}
