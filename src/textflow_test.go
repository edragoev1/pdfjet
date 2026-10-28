// textflow_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"fmt"
	"os"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// The flows of these tests are on Letter pages with margins of 50 points: the
// text is 512 points wide, from x 50, and the pages hold 692 points of it,
// from y 50 to 742.
const (
	testFlowMargin = 50
	testFlowBottom = 742
	testFlowWidth  = 512
	testFlowHeight = 692
)

func testFlow(pdf *PDF) *TextFlow {
	return NewTextFlow(pdf, letter.Portrait()).SetMargins(testFlowMargin)
}

func testFlowParagraph(font *Font, text string) *Paragraph {
	return NewParagraph().Add(NewTextLine(font, text))
}

// testFlowImage returns an image of the repository, 100 points wide and as
// tall as the height, narrower than the text.
func testFlowImage(t *testing.T, pdf *PDF, height float32) *Image {
	t.Helper()
	file, err := os.Open(testRepoPath(t, "images/up-arrow.png"))
	if err != nil {
		t.Fatal(err)
	}
	t.Cleanup(func() { file.Close() })
	image := NewImage(pdf, bufio.NewReader(file))
	return image.ScaleByWidthAndHeight(100/image.GetWidth(), height/image.GetHeight())
}

func TestTextFlowParagraphsFlowAcrossPagesBetweenTheMargins(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	paragraphs := make([]*Paragraph, 0)
	for i := 1; i <= 120; i++ {
		paragraphs = append(paragraphs, testFlowParagraph(font, fmt.Sprintf(
			"Paragraph %d, long enough to be wrapped onto a second line of the text, "+
				"which is as wide as the page less its margins of fifty points.", i)))
	}
	pages := make([]*Page, 0)
	xy := testFlow(pdf).AddParagraphs(paragraphs, 6).DrawOnPages(&pages)
	if len(pages) < 2 {
		t.Fatalf("120 paragraphs on %d page", len(pages))
	}
	for i, paragraph := range paragraphs {
		if paragraph.GetY1() < testFlowMargin-testDelta || paragraph.GetY2() > testFlowBottom+testDelta {
			t.Errorf("paragraph %d from %v to %v, outside the margins", i+1, paragraph.GetY1(), paragraph.GetY2())
		}
	}
	testNear(t, "the right margin", testFlowMargin+testFlowWidth, xy[0], testDelta)
	testNear(t, "the bottom of the text", paragraphs[119].GetY2(), xy[1], testDelta)
}

func TestTextFlowAnImageThatDoesNotFitGoesWholeToTheNextPage(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	small := testFlowImage(t, pdf, 40)
	large := testFlowImage(t, pdf, 200)
	paragraph := testFlowParagraph(font, "Above the images")
	pages := make([]*Page, 0)
	testFlow(pdf).Add(paragraph).AddSpace(600).AddImage(small).AddImage(large).DrawOnPages(&pages)
	testNear(t, "the small image, under the space", paragraph.GetY2()+600, small.y, testDelta)
	if len(pages) != 2 {
		t.Fatalf("%d pages, not 2", len(pages))
	}
	testNear(t, "the large image, at the top of the next page", testFlowMargin, large.y, testDelta)
	testNear(t, "its height", 200, large.GetHeight(), testDelta)
}

func TestTextFlowAnImageKeepsWithItsCaption(t *testing.T) {
	for _, captioned := range []bool{true, false} {
		pdf := testNewPDF()
		font := testHelvetica(pdf)
		caption := testFlowParagraph(font, "Figure 1: the image above")
		if captioned {
			caption.SetStructureType(structelem.Caption)
		}
		// Room for the image below the text, but not for its caption too
		text := testFlowParagraph(font, "Above the image")
		image := testFlowImage(t, pdf, testFlowHeight-firstLineHeight([]*Paragraph{text})-4)
		pages := make([]*Page, 0)
		testFlow(pdf).Add(text).AddImage(image).AddSpace(2).Add(caption).DrawOnPages(&pages)
		if len(pages) != 2 {
			t.Fatalf("%d pages, not 2", len(pages))
		}
		if captioned {
			testNear(t, "the image, on the next page with its caption", testFlowMargin, image.y, testDelta)
			testNear(t, "the caption, under it", image.y+image.GetHeight()+2, caption.GetY1(), testDelta)
		} else {
			testNear(t, "the image, under the text", text.GetY2(), image.y, testDelta)
			testNear(t, "a paragraph that is not a caption, on the next page", testFlowMargin, caption.GetY1(), testDelta)
		}
	}
}

func TestTextFlowAnImageTooLargeIsMadeAsLargeAsTheTextAndThePage(t *testing.T) {
	pdf := testNewPDF()
	wide := testFlowImage(t, pdf, 100)
	wide.ScaleByWidthAndHeight(20, 1) // 2,000 points wide
	tall := testFlowImage(t, pdf, 2000)
	shape := tall.GetWidth() / tall.GetHeight()
	pages := make([]*Page, 0)
	testFlow(pdf).AddImage(wide).AddImage(tall).DrawOnPages(&pages)
	testNear(t, "as wide as the text", testFlowWidth, wide.GetWidth(), testDelta)
	testNear(t, "and its height with it", 25.6, wide.GetHeight(), testDelta) // 100 * 512 / 2,000
	testNear(t, "as tall as the page's space", testFlowHeight, tall.GetHeight(), testDelta)
	testNear(t, "its shape kept", shape, tall.GetWidth()/tall.GetHeight(), 1e-4)
	testNear(t, "at the top of the second page", testFlowMargin, tall.y, testDelta)
	if len(pages) != 2 {
		t.Errorf("%d pages, not 2", len(pages))
	}
}

func TestTextFlowAnImageIsAlignedLeftCenterOrRight(t *testing.T) {
	pdf := testNewPDF()
	left := testFlowImage(t, pdf, 50)
	center := testFlowImage(t, pdf, 50)
	right := testFlowImage(t, pdf, 50)
	pages := make([]*Page, 0)
	testFlow(pdf).AddImage(left).AddImageAligned(center, alignment.Center).
		AddImageAligned(right, alignment.Right).DrawOnPages(&pages)
	w := left.GetWidth()
	testNear(t, "left", testFlowMargin, left.x, testDelta)
	testNear(t, "center", testFlowMargin+(testFlowWidth-w)/2, center.x, testDelta)
	testNear(t, "right", testFlowMargin+testFlowWidth-w, right.x, testDelta)
	testNear(t, "one below the other", left.y+50, center.y, testDelta)
}

func TestTextFlowAHeadingKeepsWithTheFirstLineAfterIt(t *testing.T) {
	for _, followed := range []bool{true, false} {
		pdf := testNewPDF()
		font := testHelvetica(pdf)
		heading := NewParagraph().Add(NewTextLine(font, "A heading").SetFontSize(16)).
			SetStructureType(structelem.H1)
		headingHeight := firstLineHeight([]*Paragraph{heading})
		// Room for the heading below the image, 2 points more, but not for a
		// line after it too
		filler := testFlowImage(t, pdf, testFlowHeight-headingHeight-2)
		flow := testFlow(pdf).AddImage(filler).Add(heading)
		text := testFlowParagraph(font, "The text under the heading")
		if followed {
			flow.Add(text)
		}
		pages := make([]*Page, 0)
		flow.DrawOnPages(&pages)
		if followed {
			testNear(t, "the heading, on the next page with its text", testFlowMargin, heading.GetY1(), testDelta)
			testNear(t, "the text, under it", heading.GetY2(), text.GetY1(), testDelta)
			if len(pages) != 2 {
				t.Errorf("%d pages, not 2", len(pages))
			}
		} else {
			testNear(t, "the heading alone, under the image", filler.y+filler.GetHeight(), heading.GetY1(), testDelta)
			if len(pages) != 1 {
				t.Errorf("%d pages, not 1", len(pages))
			}
		}
	}
}

// A heading keeps a heading after it whole, with what that one keeps, and an
// image with its caption: it kept the first line of the heading, or the image
// alone, and was left at the bottom of the page when they went to the next
// (the review of 28 October 2026).
func TestTextFlowAHeadingKeepsTheHeadingOrTheCaptionedImageAfterIt(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	h1 := NewParagraph().Add(NewTextLine(font, "Chapter").SetFontSize(22)).SetStructureType(structelem.H1)
	h2 := NewParagraph().Add(NewTextLine(font, "Section").SetFontSize(16)).SetStructureType(structelem.H2)
	text := testFlowParagraph(font, "The text under the headings")
	h1Height := firstLineHeight([]*Paragraph{h1})
	h2Height := firstLineHeight([]*Paragraph{h2})
	// Room below the image for the two headings, 2 points more, but not for a
	// line after them too
	filler := testFlowImage(t, pdf, testFlowHeight-h1Height-6-h2Height-6-2)
	pages := make([]*Page, 0)
	testFlow(pdf).AddImage(filler).Add(h1).AddSpace(6).Add(h2).AddSpace(6).Add(text).DrawOnPages(&pages)
	testNear(t, "the first heading, on the next page", testFlowMargin, h1.GetY1(), testDelta)
	testNear(t, "the second, under it", h1.GetY2()+6, h2.GetY1(), testDelta)
	testNear(t, "the text, under that", h2.GetY2()+6, text.GetY1(), testDelta)

	pdf = testNewPDF()
	font = testHelvetica(pdf)
	h1 = NewParagraph().Add(NewTextLine(font, "Figures").SetFontSize(22)).SetStructureType(structelem.H1)
	h1Height = firstLineHeight([]*Paragraph{h1})
	caption := testFlowParagraph(font, "Figure 1: the image above").SetStructureType(structelem.Caption)
	image := testFlowImage(t, pdf, 300)
	// Room below the filler for the heading and the image, 2 points more, but
	// not for the caption too
	filler = testFlowImage(t, pdf, testFlowHeight-h1Height-300-2)
	pages = make([]*Page, 0)
	testFlow(pdf).AddImage(filler).Add(h1).AddImage(image).AddSpace(4).Add(caption).DrawOnPages(&pages)
	testNear(t, "the heading, on the next page", testFlowMargin, h1.GetY1(), testDelta)
	testNear(t, "the image, under it", h1.GetY2(), image.y, testDelta)
	testNear(t, "the caption, under that", image.y+300+4, caption.GetY1(), testDelta)
	if len(pages) != 2 {
		t.Errorf("%d pages, not 2", len(pages))
	}
}

func TestTextFlowASpaceAtTheTopOfAPageIsLeftOut(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	first := testFlowParagraph(font, "At the top of the first page")
	filler := testFlowImage(t, pdf, testFlowHeight)
	second := testFlowParagraph(font, "At the top of the second page")
	pages := make([]*Page, 0)
	testFlow(pdf).AddSpace(30).Add(first).AddImage(filler).AddSpace(30).Add(second).DrawOnPages(&pages)
	testNear(t, "a space before everything", testFlowMargin, first.GetY1(), testDelta)
	testNear(t, "a space after a full page", testFlowMargin, second.GetY1(), testDelta)
	if len(pages) != 3 {
		t.Errorf("%d pages, not 3", len(pages)) // The text, the image, the text
	}
}

func TestTextFlowStartsOnAPageBelowTheFirstPageTop(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	first := NewPageDetached(pdf, letter.Portrait())
	paragraph := testFlowParagraph(font, "Under the letterhead")
	pages := make([]*Page, 0)
	testFlow(pdf).SetFirstPageTop(200).Add(paragraph).DrawOnPagesFrom(first, &pages)
	testNear(t, "below the first page's top", 200, paragraph.GetY1(), testDelta)
	if len(pages) != 0 {
		t.Errorf("%d pages added, where the first page is not", len(pages))
	}
}

func TestTextFlowNothingToDrawMakesNoPage(t *testing.T) {
	pdf := testNewPDF()
	pages := make([]*Page, 0)
	testFlow(pdf).AddSpace(20).Add(NewParagraph()).DrawOnPages(&pages)
	if len(pages) != 0 {
		t.Errorf("%d pages for no text", len(pages))
	}
}

func TestTextFlowMarginsThatLeaveNoRoomAreRefused(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	pages := make([]*Page, 0)
	testFlow(pdf).SetMargins(400).Add(testFlowParagraph(font, "text")).DrawOnPages(&pages)
	if pdf.err == nil || len(pages) != 0 {
		t.Errorf("margins of 400 points: error %v, %d pages", pdf.err, len(pages))
	}
}
