// svgimage_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"
	"testing"
)

func testNewSVG(t *testing.T, svg string) *SVGImage {
	t.Helper()
	image, err := NewSVGImage(strings.NewReader(svg))
	if err != nil {
		t.Fatal(err)
	}
	return image
}

func testDrawSVG(t *testing.T, svg string) string {
	t.Helper()
	page := testNewPage()
	image := testNewSVG(t, svg)
	image.SetLocation(0, 0)
	testAssertXY(t, image.GetWidth(), image.GetHeight(), image.DrawOn(page))
	return testContent(page)
}

func TestSVGImageReadsTheSizeFromTheAttributes(t *testing.T) {
	image := testNewSVG(t, `<svg width="100" height="50"><path d="M10 10 L90 40"/></svg>`)
	if image.GetWidth() != 100 || image.GetHeight() != 50 {
		t.Errorf("size %v x %v", image.GetWidth(), image.GetHeight())
	}
}

func TestSVGImageKeepsTheLastNumberOfAPathThatIsNotClosed(t *testing.T) {
	content := testDrawSVG(t, `<svg width="100" height="50"><path d="M10 10 L90 40"/></svg>`)
	if !strings.Contains(content, "10 782 m\n90 752 l\n") {
		t.Errorf("content %q", content)
	}
}

func TestSVGImageSingleQuotesAndLineBreaksBetweenAttributesParseLikeDoubleQuotes(t *testing.T) {
	doubleQuotes := testDrawSVG(t, `<svg width="100" height="50"><path d="M10 10 L90 40 L10 40 Z" fill="red"/></svg>`)
	singleQuotes := testDrawSVG(t, "<svg width='100' height='50'><path\n fill='red'\n d='M10 10 L90 40 L10 40 Z'/></svg>")
	if doubleQuotes != singleQuotes {
		t.Errorf("%q != %q", doubleQuotes, singleQuotes)
	}
	if !strings.HasPrefix(doubleQuotes, "1 0 0 rg\n") {
		t.Errorf("content %q", doubleQuotes)
	}
}

func TestSVGImageEllipticalArcsBecomeCubicCurves(t *testing.T) {
	content := testDrawSVG(t, `<svg width="100" height="50"><path d="M10 10 A 20 20 0 0 1 50 10"/></svg>`)
	// A half circle over the top, from (10, 10) to (50, 10) in SVG coordinates.
	if !strings.Contains(content, "10 793.05 18.95 802 30 802 c\n41.05 802 50 793.05 50 782 c\n") {
		t.Errorf("content %q", content)
	}
}

func TestSVGImageAStrokeOnlyPathIsStroked(t *testing.T) {
	content := testDrawSVG(t, `<svg width="100" height="50"><path d="M10 10 L90 40 L10 40 Z" fill="none" stroke="red"/></svg>`)
	if !strings.Contains(content, "1 0 0 RG\n") || !strings.HasSuffix(content, "s\n") {
		t.Errorf("content %q", content)
	}
}

func TestSVGImageAnOpenPathWithAStrokeIsStroked(t *testing.T) {
	content := testDrawSVG(t, `<svg width="100" height="50"><path d="M10 10 L90 40" fill="none" stroke="red"/></svg>`)
	if !strings.HasSuffix(content, "10 782 m\n90 752 l\nS\n") {
		t.Errorf("content %q", content)
	}
}

func TestSVGImageAClosedSubpathIsClosedAndAnOpenOneStrokedAtTheEnd(t *testing.T) {
	content := testDrawSVG(t, `<svg width="100" height="50"><path d="M10 10 L90 40 Z M20 20 L30 30" fill="none" stroke="red"/></svg>`)
	if !strings.HasSuffix(content, "10 782 m\n90 752 l\ns\n20 772 m\n30 762 l\nS\n") {
		t.Errorf("content %q", content)
	}
}

func TestSVGImageFillNoneWithoutAStrokeDrawsNothing(t *testing.T) {
	for _, svg := range []string{
		`<svg width="100" height="50"><path d="M10 10 L90 40 L10 40 Z" fill="none"/></svg>`,
		`<svg width="100" height="50" fill="none"><path d="M10 10 L90 40 L10 40 Z"/></svg>`,
	} {
		if content := testDrawSVG(t, svg); content != "" {
			t.Errorf("content %q", content)
		}
	}
}

func TestSVGImageNoneOnThePathWinsOverTheColorsOfTheSvgElement(t *testing.T) {
	content := testDrawSVG(t, `<svg width="100" height="50" fill="red" stroke="green">`+
		`<path d="M10 10 L90 40 L10 40 Z" fill="none" stroke="blue"/>`+
		`<path d="M20 20 L80 30 L20 30 Z" stroke="none"/></svg>`)
	if !strings.Contains(content, "0 0 1 RG\n") || strings.Contains(content, "0 0.5 0 RG") {
		t.Errorf("stroke colors in %q", content)
	}
	// The second path takes the red fill of the svg element and has no stroke.
	if !strings.Contains(content, "1 0 0 rg\n") {
		t.Errorf("fill color in %q", content)
	}
	if strings.Count(content, "\nf\n") != 1 || strings.Count(content, "\ns\n") != 1 {
		t.Errorf("paint operators in %q", content)
	}
}

func TestSVGImageAPathWithoutColorsOrWithAnUnknownFillIsFilledBlack(t *testing.T) {
	unset := testDrawSVG(t, `<svg width="100" height="50"><path d="M10 10 L90 40 L10 40 Z"/></svg>`)
	if !strings.HasPrefix(unset, "0 0 0 rg\n") || !strings.HasSuffix(unset, "\nf\n") {
		t.Errorf("content %q", unset)
	}
	gradient := testDrawSVG(t, `<svg width="100" height="50"><path d="M10 10 L90 40 L10 40 Z" fill="url(#g)"/></svg>`)
	if gradient != unset {
		t.Errorf("%q != %q", gradient, unset)
	}
}
