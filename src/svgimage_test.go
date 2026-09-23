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

func TestSVGImageASizeWithAUnitIsReadInPoints(t *testing.T) {
	// A number is in the user unit of the file, which PDFjet draws as a
	// point, and so is a number in px; the units of length are converted.
	for svg, want := range map[string][2]float32{
		`<svg width="48" height="24"/>`:       {48, 24},
		`<svg width="48px" height="24px"/>`:   {48, 24},
		`<svg width="48pt" height="24pt"/>`:   {48, 24},
		`<svg width="1in" height="2in"/>`:     {72, 144},
		`<svg width="1pc" height="2pc"/>`:     {12, 24},
		`<svg width="210mm" height="297mm"/>`: {595.2756, 841.8898},
		`<svg width="21cm" height="29.7cm"/>`: {595.2756, 841.8898},
		`<svg width=" 48 " height=" 24 "/>`:   {48, 24},
	} {
		image := testNewSVG(t, svg)
		testNear(t, svg+" width", want[0], image.GetWidth(), 0.001)
		testNear(t, svg+" height", want[1], image.GetHeight(), 0.001)
	}
}

func TestSVGImageASizeThatCannotBeReadIsTheSizeOfTheViewBox(t *testing.T) {
	// Scaling the paths by a width of zero would draw every one of them at
	// the origin, so a size in a unit PDFjet cannot read, a percentage among
	// them, and a size the file does not give, leave the drawing 1:1 with its
	// viewBox.
	for _, svg := range []string{
		`<svg viewBox="0 0 100 50"><path d="M10 10 L90 40"/></svg>`,
		`<svg width="100%" height="100%" viewBox="0 0 100 50"><path d="M10 10 L90 40"/></svg>`,
		`<svg width="10em" height="5em" viewBox="0 0 100 50"><path d="M10 10 L90 40"/></svg>`,
	} {
		image := testNewSVG(t, svg)
		if image.GetWidth() != 100 || image.GetHeight() != 50 {
			t.Errorf("%s: size %v x %v", svg, image.GetWidth(), image.GetHeight())
		}
		page := testNewPage()
		image.SetLocation(0, 0)
		image.DrawOn(page)
		if content := testContent(page); !strings.Contains(content, "10 782 m\n90 752 l\n") {
			t.Errorf("%s: content %q", svg, content)
		}
	}
}

func TestSVGImageAViewBoxThatIsNotFourNumbersOrHasNoSizeFails(t *testing.T) {
	for svg, want := range map[string]string{
		`<svg width="10" height="10" viewBox="0 0"/>`:          `Invalid SVG viewBox "0 0": four numbers are needed.`,
		`<svg width="10" height="10" viewBox="0 0 10 10 10"/>`: `Invalid SVG viewBox "0 0 10 10 10": four numbers are needed.`,
		`<svg width="10" height="10" viewBox="0 0 ten 10"/>`:   `Invalid SVG viewBox "0 0 ten 10": four numbers are needed.`,
		`<svg width="10" height="10" viewBox="0 0 0 10"/>`:     `Invalid SVG viewBox "0 0 0 10": its width and height cannot be zero.`,
		`<svg width="10" height="10" viewBox="0 0 10 0"/>`:     `Invalid SVG viewBox "0 0 10 0": its width and height cannot be zero.`,
	} {
		_, err := NewSVGImage(strings.NewReader(svg))
		if err == nil || err.Error() != want {
			t.Errorf("%s: %v", svg, err)
		}
	}
}

// What SVGImage draws of the SVG files of drawing programs: groups and what
// they give their paths, transforms, the basic shapes, style attributes and
// classes, fill rules, opacity, line caps and joins.

func TestSVGImageAGroupGivesItsPathsItsColorsAndWidthUnlessTheyHaveTheirOwn(t *testing.T) {
	content := testDrawSVG(t, `<svg width="100" height="100"><g fill="red" stroke="blue" stroke-width="2">`+
		`<path d="M10 10 H90 V90 Z"/><path d="M10 10 H50 V50 Z" fill="green" stroke-width="4"/></g></svg>`)
	want := "1 0 0 rg\n10 782 m\n90 782 l\n90 702 l\nf\n0 0 1 RG\n2 w\n10 782 m\n90 782 l\n90 702 l\ns\n" +
		"0 0.5 0 rg\n10 782 m\n50 782 l\n50 742 l\nf\n4 w\n10 782 m\n50 782 l\n50 742 l\ns\n"
	if content != want {
		t.Errorf("content %q", content)
	}
}

func TestSVGImageAPathIsFilledBlackUnlessItsFillIsNoneAndStrokedOneUnitWide(t *testing.T) {
	// As SVG draws them: a stroke does not take the fill away, and a stroke
	// width of 0 draws no stroke, where a PDF would draw the thinnest line.
	content := testDrawSVG(t, `<svg width="100" height="50"><path d="M10 10 L90 40 L10 40 Z" stroke="red"/></svg>`)
	if !strings.HasPrefix(content, "0 0 0 rg\n") || !strings.Contains(content, "1 0 0 RG\n1 w\n") {
		t.Errorf("content %q", content)
	}
	content = testDrawSVG(t, `<svg width="100" height="50"><path d="M10 10 L90 40 L10 40 Z" stroke="red" stroke-width="0"/></svg>`)
	if strings.Contains(content, "RG") || !strings.HasSuffix(content, "\nf\n") {
		t.Errorf("content %q", content)
	}
}

func TestSVGImageTransformsOfGroupsAndPathsAreAppliedInTurn(t *testing.T) {
	// The path is scaled, then moved by the group; its stroke is scaled too.
	content := testDrawSVG(t, `<svg width="100" height="100"><g transform="translate(10 20)">`+
		`<path d="M0 0 L10 0" transform="scale(2)" stroke="red" fill="none"/></g></svg>`)
	if content != "1 0 0 RG\n2 w\n10 772 m\n30 772 l\nS\n" {
		t.Errorf("content %q", content)
	}
	for transform, want := range map[string]string{
		"rotate(90)":                  "0 792 m\n0 782 l\n",
		"rotate(90 10 10)":            "20 792 m\n20 782 l\n",
		"matrix(1 0 0 1 5 6)":         "5 786 m\n15 786 l\n",
		"translate(5,6)":              "5 786 m\n15 786 l\n",
		"translate(5)":                "5 792 m\n15 792 l\n",
		"scale(2 3)":                  "0 792 m\n20 792 l\n",
		"skewY(45)":                   "0 792 m\n10 782 l\n",
		"translate(10) scale(2)":      "10 792 m\n30 792 l\n",
		"scale(2) translate(10)":      "20 792 m\n40 792 l\n",
		"translate(10)\n,rotate(90)":  "10 792 m\n10 782 l\n",
		"translate(10) rotate":        "0 792 m\n10 792 l\n", // Cannot be read: none
		"translate(10) unknown(1 2)":  "0 792 m\n10 792 l\n",
		"translate(10) scale(1 2 3)":  "0 792 m\n10 792 l\n",
		"translate(1e1) scale(.5.5)":  "10 792 m\n15 792 l\n",
		"translate(-5-5) scale(+1+1)": "-5 797 m\n5 797 l\n",
	} {
		content := testDrawSVG(t, `<svg width="100" height="100"><path d="M0 0 L10 0" transform="`+transform+`"/></svg>`)
		if !strings.Contains(content, want) {
			t.Errorf("%q draws %q, not %q", transform, content, want)
		}
	}
	// skewX leaves the line along the x axis as it is, and moves the point below.
	content = testDrawSVG(t, `<svg width="100" height="100"><path d="M0 0 L0 10" transform="skewX(45)"/></svg>`)
	if !strings.Contains(content, "0 792 m\n10 782 l\n") {
		t.Errorf("skewX draws %q", content)
	}
}

func TestSVGImageAShapeWithATransformThatFlattensItIsNotDrawn(t *testing.T) {
	content := testDrawSVG(t, `<svg width="100" height="100"><path d="M0 0 L10 0 L10 10 Z" transform="scale(0)"/></svg>`)
	if content != "" {
		t.Errorf("content %q", content)
	}
}

func TestSVGImageDrawsTheBasicShapes(t *testing.T) {
	for shape, want := range map[string]string{
		`<rect x="10" y="20" width="30" height="40"/>`: "0 0 0 rg\n10 772 m\n40 772 l\n40 732 l\n10 732 l\nf\n",
		`<rect x="10" y="20" width="30" height="40" rx="5"/>`: "0 0 0 rg\n15 772 m\n35 772 l\n" +
			"37.76 772 40 769.76 40 767 c\n40 737 l\n40 734.24 37.76 732 35 732 c\n15 732 l\n" +
			"12.24 732 10 734.24 10 737 c\n10 767 l\n10 769.76 12.24 772 15 772 c\nf\n",
		// A radius larger than half the side is half the side, and one that
		// is not given is the other one.
		`<rect width="10" height="10" ry="20"/>`: "0 0 0 rg\n5 792 m\n5 792 l\n" +
			"7.76 792 10 789.76 10 787 c\n10 787 l\n10 784.24 7.76 782 5 782 c\n5 782 l\n" +
			"2.24 782 0 784.24 0 787 c\n0 787 l\n0 789.76 2.24 792 5 792 c\nf\n",
		`<circle cx="50" cy="50" r="10"/>`: "0 0 0 rg\n60 742 m\n60 736.48 55.52 732 50 732 c\n" +
			"44.48 732 40 736.48 40 742 c\n40 747.52 44.48 752 50 752 c\n55.52 752 60 747.52 60 742 c\nf\n",
		`<ellipse cx="50" cy="50" rx="20" ry="10"/>`: "0 0 0 rg\n70 742 m\n70 736.48 61.05 732 50 732 c\n" +
			"38.95 732 30 736.48 30 742 c\n30 747.52 38.95 752 50 752 c\n61.05 752 70 747.52 70 742 c\nf\n",
		`<line x1="10" y1="20" x2="30" y2="40" stroke="red"/>`: "1 0 0 RG\n1 w\n10 772 m\n30 752 l\nS\n",
		`<polyline points="10,10 20,20 10,20" fill="none" stroke="red"/>`: "1 0 0 RG\n1 w\n" +
			"10 782 m\n20 772 l\n10 772 l\nS\n",
		`<polygon points="10 10 20 20 10 20 5" fill="none" stroke="red"/>`: "1 0 0 RG\n1 w\n" +
			"10 782 m\n20 772 l\n10 772 l\ns\n",
		// Shapes of no size draw nothing.
		`<rect width="0" height="10"/><circle r="0"/><ellipse rx="5"/><polygon points="1 2"/><line/>`: "",
	} {
		content := testDrawSVG(t, `<svg width="100" height="100">`+shape+`</svg>`)
		if content != want {
			t.Errorf("%s draws %q, not %q", shape, content, want)
		}
	}
}

func TestSVGImageStyleAttributesWinOverClassesWhichWinOverAttributes(t *testing.T) {
	svg := `<svg width="100" height="100"><defs><style>` +
		`/* the {colors} */ .red{fill:#ff0000} .blue, .other { fill: blue !important; stroke: red }` +
		`g .green{fill:green} .green:hover{fill:green}</style></defs>` +
		`<path d="M0 0 L10 0 L10 10 Z" fill="yellow" class="red"/>` +
		`<path d="M0 0 L10 0 L10 10 Z" class="blue red" style="stroke: none"/>` +
		`<path d="M0 0 L10 0 L10 10 Z" class="red blue" style="fill:yellow;stroke-width:3"/>` +
		`<path d="M0 0 L10 0 L10 10 Z" class="green"/></svg>`
	content := testDrawSVG(t, svg)
	fills := []string{}
	for _, line := range strings.Split(content, "\n") {
		if strings.HasSuffix(line, " rg") || strings.HasSuffix(line, " RG") || strings.HasSuffix(line, " w") {
			fills = append(fills, line)
		}
	}
	// The rules are applied in the order of the style sheet, whatever the
	// order of the classes; the selectors that are not a class alone are left out.
	want := "1 0 0 rg|0 0 1 rg|1 1 0 rg|1 0 0 RG|3 w|0 0 0 rg"
	if got := strings.Join(fills, "|"); got != want {
		t.Errorf("colors %q, not %q, of %q", got, want, content)
	}
}

func TestSVGImageColorsAreHexadecimalRGBNamesOrTheCurrentColor(t *testing.T) {
	for fill, want := range map[string]string{
		`fill="#F00"`:                                "1 0 0 rg",
		`fill="rgb(255, 0, 0)"`:                      "1 0 0 rg",
		`fill="rgb(100%,0%,0%)"`:                     "1 0 0 rg",
		`fill="RGBA(255 0 0 / 0.5)"`:                 "1 0 0 rg",
		`fill="Red"`:                                 "1 0 0 rg",
		`fill="currentColor" color="red"`:            "1 0 0 rg",
		`fill="currentColor"`:                        "0 0 0 rg",
		`fill="url(#gradient) red"`:                  "1 0 0 rg",
		`fill="url(#gradient)"`:                      "0 0 0 rg", // Not drawn: as if not given
		`fill="unknown"`:                             "0 0 0 rg",
		`fill="rgb(nan, 0, 0)"`:                      "0 0 0 rg", // Not a number: as if not given
		`fill="rgb(inf, 0, 0)"`:                      "0 0 0 rg",
		`style="fill: currentColor; color: #0000ff"`: "0 0 1 rg",
	} {
		content := testDrawSVG(t, `<svg width="100" height="100"><path d="M0 0 L10 0 L10 10 Z" `+fill+`/></svg>`)
		if !strings.HasPrefix(content, want+"\n") {
			t.Errorf("%s draws %q", fill, content)
		}
	}
	// The current color is the color where the fill is used, not where it is set.
	content := testDrawSVG(t, `<svg width="100" height="100" fill="currentColor"><g color="red">`+
		`<path d="M0 0 L10 0 L10 10 Z"/></g></svg>`)
	if !strings.HasPrefix(content, "1 0 0 rg\n") {
		t.Errorf("content %q", content)
	}
	if _, err := NewSVGImage(strings.NewReader(`<svg><path d="M0 0 L1 1" fill="#zzzzzz"/></svg>`)); err == nil {
		t.Error("a fill of # and not hexadecimal was read")
	}
}

func TestSVGImageTheEvenOddRuleFillsWithFStar(t *testing.T) {
	content := testDrawSVG(t, `<svg width="100" height="100" style="fill-rule:evenodd">`+
		`<path d="M0 0 H30 V30 H0 Z M10 10 H20 V20 H10 Z"/><path d="M0 0 H30 V30 Z" fill-rule="nonzero"/></svg>`)
	if strings.Count(content, "\nf*\n") != 1 || !strings.HasSuffix(content, "\nf\n") {
		t.Errorf("content %q", content)
	}
}

func TestSVGImageOpacityIsSetInAGraphicsStateOfThePathsOwn(t *testing.T) {
	content := testDrawSVG(t, `<svg width="100" height="100"><path d="M0 0 L10 0 L10 10 Z" opacity="0.5"/>`+
		`<path d="M0 0 L10 0 L10 10 Z"/></svg>`)
	if content != "q\n/GS1 gs\n0 0 0 rg\n0 792 m\n10 792 l\n10 782 l\nf\nQ\n0 0 0 rg\n0 792 m\n10 792 l\n10 782 l\nf\n" {
		t.Errorf("content %q", content)
	}
	// The opacity of a group multiplies those of its paths, and a fill or a
	// stroke of no opacity is not drawn.
	image := testNewSVG(t, `<svg width="100" height="100"><g opacity="0.5"><g style="opacity:50%">`+
		`<path d="M0 0 L10 0 L10 10 Z" fill-opacity="0.5" stroke="red" stroke-opacity="0"/></g></g></svg>`)
	path := image.paths[0]
	if path.fillAlpha != 0.125 || path.strokeAlpha != 0.0 || path.stroke != -1 {
		t.Errorf("fill alpha %v, stroke alpha %v, stroke %v", path.fillAlpha, path.strokeAlpha, path.stroke)
	}
}

func TestSVGImageLineCapsAndJoinsAreSetInAGraphicsStateOfThePathsOwn(t *testing.T) {
	content := testDrawSVG(t, `<svg width="100" height="100" stroke-linecap="round" stroke-linejoin="bevel">`+
		`<path d="M0 0 L10 0" stroke="red" fill="none"/><path d="M0 0 L10 0 L10 10 Z"/></svg>`)
	if content != "q\n1 J\n2 j\n1 0 0 RG\n1 w\n0 792 m\n10 792 l\nS\nQ\n0 0 0 rg\n0 792 m\n10 792 l\n10 782 l\nf\n" {
		t.Errorf("content %q", content)
	}
}

func TestSVGImageTheContentOfDefsAndOfWhatIsNotDisplayedIsNotDrawn(t *testing.T) {
	content := testDrawSVG(t, `<svg width="100" height="100">`+
		`<defs><path d="M0 0 L10 0 L10 10 Z"/></defs>`+
		`<clipPath><rect width="10" height="10"/></clipPath>`+
		`<symbol><circle r="5"/></symbol>`+
		`<g display="none"><path d="M0 0 L10 0 L10 10 Z"/></g>`+
		`<g style="display:none"><path d="M0 0 L10 0 L10 10 Z" display="inline"/></g>`+
		`<path d="M20 20 L30 20 L30 30 Z"/></svg>`)
	if content != "0 0 0 rg\n20 772 m\n30 772 l\n30 762 l\nf\n" {
		t.Errorf("content %q", content)
	}
}

func TestSVGImageAttributesOfOtherNamespacesAreLeftOut(t *testing.T) {
	content := testDrawSVG(t, `<svg xmlns="http://www.w3.org/2000/svg" xmlns:x="urn:x" width="100" height="100">`+
		`<path d="M0 0 L10 0 L10 10 Z" x:fill="red" x:transform="scale(2)"/></svg>`)
	if content != "0 0 0 rg\n0 792 m\n10 792 l\n10 782 l\nf\n" {
		t.Errorf("content %q", content)
	}
}

func TestSVGImageTheViewBoxScalesTheStrokesAndKeepsItsProportions(t *testing.T) {
	// The viewBox is scaled by 10 to fit the height, and centered across.
	content := testDrawSVG(t, `<svg width="200" height="100" viewBox="0 0 10 10">`+
		`<path d="M0 0 L10 10" stroke="red" fill="none"/></svg>`)
	if content != "1 0 0 RG\n10 w\n50 792 m\n150 692 l\nS\n" {
		t.Errorf("content %q", content)
	}
	for aspect, want := range map[string]string{
		"xMinYMin":       "0 792 m\n100 692 l\n",
		"xMaxYMax meet":  "100 792 m\n200 692 l\n",
		"defer xMidYMid": "50 792 m\n150 692 l\n",
		"xMidYMin slice": "20 w\n0 792 m\n200 592 l\n", // Covers it, from the top
		"none":           "0 792 m\n200 692 l\n",
	} {
		content := testDrawSVG(t, `<svg width="200" height="100" viewBox="0 0 10 10" preserveAspectRatio="`+
			aspect+`"><path d="M0 0 L10 10" stroke="red" fill="none"/></svg>`)
		if !strings.Contains(content, want) {
			t.Errorf("%s draws %q, not %q", aspect, content, want)
		}
	}
}

func TestSVGImageASizeThatIsNotGivenIsInTheProportionsOfTheViewBox(t *testing.T) {
	for svg, want := range map[string][2]float32{
		`<svg width="48" viewBox="0 0 960 480"/>`:              {48, 24},
		`<svg height="24" viewBox="0 0 960 480"/>`:             {48, 24},
		`<svg width="10%" viewBox="0 0 960 480"/>`:             {960, 480},
		`<svg width="100" height="50" viewBox="0 0 960 480"/>`: {100, 50},
	} {
		image := testNewSVG(t, svg)
		if image.GetWidth() != want[0] || image.GetHeight() != want[1] {
			t.Errorf("%s: size %v x %v", svg, image.GetWidth(), image.GetHeight())
		}
	}
}

func TestSVGImageScalingScalesTheStrokes(t *testing.T) {
	image := testNewSVG(t, `<svg width="100" height="50"><path d="M10 10 L90 40" stroke="red" stroke-width="4"/></svg>`)
	image.ScaleBy(0.5)
	page := testNewPage()
	image.SetLocation(0, 0)
	image.DrawOn(page)
	if content := testContent(page); !strings.Contains(content, "1 0 0 RG\n2 w\n") {
		t.Errorf("content %q", content)
	}
}

func TestSVGImageValuesThatCannotBeReadAreLeftOut(t *testing.T) {
	content := testDrawSVG(t, `<svg width="100" height="100" viewBox="0,0,100,100" stroke-width="3">`+
		`<path d="M0 0 L10 0" fill="none" stroke="red" stroke-width="calc(1px + 1px)" opacity="half"/>`+
		`<rect width="Inf" height="10" stroke-width="NaN"/><polygon points="0 0 1e999 1 2 2"/></svg>`)
	if content != "1 0 0 RG\n3 w\n0 792 m\n10 792 l\nS\n" {
		t.Errorf("content %q", content)
	}
}

func TestSVGImageASizeOrATransformTooLargeForAFloatDrawsNothing(t *testing.T) {
	// A PDF holds numbers below 2^31 and no infinity: a size of inf is no
	// size, and a path a transform takes out of that range is not drawn.
	image := testNewSVG(t, `<svg width="inf" height="nan" viewBox="0 0 100 50"/>`)
	if image.GetWidth() != 100 || image.GetHeight() != 50 {
		t.Errorf("size %v x %v", image.GetWidth(), image.GetHeight())
	}
	content := testDrawSVG(t, `<svg width="100" height="100"><path d="M0 0 L10 0 L10 10 Z" transform="scale(1e30)"/>`+
		`<path d="M0 0 L10 0" stroke="red" transform="scale(1e20 1)"/><path d="M0 0 L10 0 L10 10 Z"/></svg>`)
	if content != "0 0 0 rg\n0 792 m\n10 792 l\n10 782 l\nf\n" {
		t.Errorf("content %q", content)
	}
}

func TestSVGImageASkewOfNinetyDegreesIsNotRead(t *testing.T) {
	for _, transform := range []string{"skewX(90)", "skewY(-270)"} {
		content := testDrawSVG(t, `<svg width="100" height="100"><path d="M0 0 L10 0 L10 10 Z" transform="`+transform+`"/></svg>`)
		if content != "0 0 0 rg\n0 792 m\n10 792 l\n10 782 l\nf\n" {
			t.Errorf("%s draws %q", transform, content)
		}
	}
}
