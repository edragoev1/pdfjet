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

func TestSVGImageScalingScalesTheSizeWithThePaths(t *testing.T) {
	image := testNewSVG(t, `<svg width="100" height="50"><path d="M10 10 L90 40"/></svg>`)
	image.ScaleBy(0.5)
	if image.GetWidth() != 50 || image.GetHeight() != 25 {
		t.Errorf("size %v x %v", image.GetWidth(), image.GetHeight())
	}
	page := testNewPage()
	image.SetLocation(0, 0)
	testAssertXY(t, 50, 25, image.DrawOn(page))
	if content := testContent(page); !strings.Contains(content, "5 787 m\n45 772 l\n") {
		t.Errorf("content %q", content)
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
	// A half circle over the top, from (10, 10) to (50, 10) in svgParser coordinates.
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

// The numbers of path data are written as SVG 1.1 section 8.3.9 gives them:
// a sign, digits, a point and an exponent, and a sign or a point starts the
// next number where no space or comma separates them.

func TestSVGImageReadsTheNumbersOfPathDataAsSVGWritesThem(t *testing.T) {
	// Every path draws the line from 10, 10 to 90, 40, written another way.
	for _, data := range []string{
		"M10 10 L90 40",
		"M10,10L90,40",
		"M 1e1 1e1 L 9e1 4e1",
		"M 1000e-2 1000e-2 L 9000e-2 4000e-2",
		"M 1000E-2 1000E-2 L 9000E-2 4000E-2",
		"M+10+10L+90+40",
		"M 10.0 10.0 L 90.0 40.0",
		"M 10 10 L 9e+1 4e+1",
		"M10 10\nL90 40", // Path data is often written over several lines
		"M10\t10\tL90\t40",
		"M10 10\r\nL90 40",
	} {
		svg := `<svg width="100" height="50"><path d="` + data + `"/></svg>`
		image, err := NewSVGImage(strings.NewReader(svg))
		if err != nil {
			t.Errorf("%q: %v", data, err)
			continue
		}
		page := testNewPage()
		image.SetLocation(0, 0)
		image.DrawOn(page)
		if content := testContent(page); !strings.Contains(content, "10 782 m\n90 752 l\n") {
			t.Errorf("%q draws %q", data, content)
		}
	}
}

func TestSVGImagePathDataThatStartsWithANumberDrawsNothing(t *testing.T) {
	// Path data starts with a moveto; the numbers before the first command
	// belong to no operation, and are left out rather than read as one.
	for _, data := range []string{"10 10 L90 40", ".5.5L90 40", "-10L90 40"} {
		image, err := NewSVGImage(strings.NewReader(
			`<svg width="100" height="50"><path d="` + data + `"/></svg>`))
		if err != nil {
			t.Errorf("%q: %v", data, err)
			continue
		}
		page := testNewPage()
		image.SetLocation(0, 0)
		image.DrawOn(page)
		if content := testContent(page); strings.Contains(content, " m\n") {
			t.Errorf("%q draws %q", data, content)
		}
	}
}

func TestSVGImageAPathWithoutDataDrawsNothing(t *testing.T) {
	// A path element without a d attribute is one the other ports threw on.
	content := testDrawSVG(t, `<svg width="100" height="50"><path/><path d="M10 10 L90 40"/></svg>`)
	if !strings.Contains(content, "10 782 m\n90 752 l\n") {
		t.Errorf("content %q", content)
	}
}

func TestSVGImagePathDataThatNeedsTheCurrentPointStartsAtTheOrigin(t *testing.T) {
	// The first command of the path data needs a current point, which the
	// other ports left unset: they threw, or trapped in Swift.
	for _, data := range []string{
		"L90 40", "H90", "V40", "Q10 10 90 40", "T90 40",
		"C1 1 2 2 90 40", "S1 1 90 40", "A5 5 0 0 1 90 40", "l90 40",
	} {
		content := testDrawSVG(t, `<svg width="100" height="50"><path d="`+data+`"/></svg>`)
		if !strings.Contains(content, " l\n") && !strings.Contains(content, " c\n") {
			t.Errorf("%q draws %q", data, content)
		}
	}
}

func TestSVGImageTheFlagsOfAnArcAreOneCharacterAndNeedNoSeparator(t *testing.T) {
	// SVG 1.1 section 8.3.9 writes each flag of an elliptical arc as a single
	// character, so that nothing has to separate it from what follows. Every
	// path here draws the two half circles of the separated one.
	separated := testDrawSVG(t, `<svg width="100" height="50">`+
		`<path d="M10 10 A 20 20 0 0 1 50 10 A 20 20 0 1 0 90 10"/></svg>`)
	for _, data := range []string{
		"M10 10 A20 20 0 01 50 10 A20 20 0 10 90 10",
		"M10 10 A20 20 0 0150 10 A20 20 0 1090 10",
		"M10 10A20 20 0 0150,10A20 20 0 1090,10",
		"M10 10 a20 20 0 0140 0 a20 20 0 1040 0",
		"M10 10 A20,20,0,0,1,50,10 A20,20,0,1,0,90,10",
	} {
		content := testDrawSVG(t, `<svg width="100" height="50"><path d="`+data+`"/></svg>`)
		if content != separated {
			t.Errorf("%q draws %q, not %q", data, content, separated)
		}
	}
}

func TestSVGImageOnlyTheFlagsOfAnArcAreReadOneCharacterAtATime(t *testing.T) {
	// The radii, the rotation and the end point of an arc are numbers like any
	// other, and the digits of every other command are too.
	content := testDrawSVG(t, `<svg width="100" height="50">`+
		`<path d="M10 10 A10 10 0 0 1 10 40 L10 10 A10 10 0 0 0 10 40"/></svg>`)
	other := testDrawSVG(t, `<svg width="100" height="50">`+
		`<path d="M10 10 A10 10 0 01 10 40 L10 10 A10 10 0 00 10 40"/></svg>`)
	if content != other {
		t.Errorf("%q != %q", content, other)
	}
	if !strings.Contains(content, "10 782 m\n") || !strings.Contains(content, " c\n") {
		t.Errorf("content %q", content)
	}
}

func TestSVGImageArcRadiiTooSmallForTheEndPointsAreScaledToFitThem(t *testing.T) {
	// SVG 1.1 section F.6.6 scales up radii too small to reach the end points
	// until the ellipse just does: the arc is then half of it, whichever way
	// the large arc flag points, and the same as the arc of the fitting radii.
	fitted := testDrawSVG(t, `<svg width="200" height="200"><path d="M0 0 A50 50 0 0 1 100 0"/></svg>`)
	if strings.Count(fitted, " c\n") != 2 {
		t.Fatalf("half an ellipse is %q", fitted)
	}
	for _, data := range []string{
		"M0 0 A10 10 0 0 1 100 0",
		"M0 0 A1 1 0 0 1 100 0",
		"M0 0 A10 10 0 1 1 100 0",
	} {
		content := testDrawSVG(t, `<svg width="200" height="200"><path d="`+data+`"/></svg>`)
		if content != fitted {
			t.Errorf("%q draws %q, not %q", data, content, fitted)
		}
	}
}

func TestSVGImageAnArcOfWholeQuarterTurnsIsDrawnInThatManyCurves(t *testing.T) {
	// An arc is drawn in pieces of at most a quarter turn. One of exactly a
	// quarter, a half or a whole turn is not split once more for the last bit
	// of the sweep, which the four ports do not compute alike.
	for _, arc := range []struct {
		data   string
		curves int
	}{
		{"A 120 120 120 0 0 120 120", 1}, // A quarter turn, of the fuzz corpus
		{"M10 10 A 20 20 0 0 1 50 10", 2},
		{"M0 0 A50 50 0 0 1 100 0", 2},
	} {
		content := testDrawSVG(t, `<svg width="200" height="200"><path d="`+arc.data+`"/></svg>`)
		if curves := strings.Count(content, " c\n"); curves != arc.curves {
			t.Errorf("%q is %d curves, not %d: %q", arc.data, curves, arc.curves, content)
		}
	}
}
