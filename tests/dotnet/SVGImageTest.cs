/*
 * SVGImageTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Text;
using Xunit;

namespace PDFjet.NET {
public class SVGImageTest {
    private static SVGImage Parse(string svg) {
        return new SVGImage(new MemoryStream(Encoding.UTF8.GetBytes(svg)));
    }

    private static string Draw(string svg) {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        SVGImage image = Parse(svg);
        image.SetLocation(0f, 0f);
        TestSupport.AssertXY(image.GetWidth(), image.GetHeight(), image.DrawOn(page));
        return TestSupport.Content(page);
    }

    [Fact]
    public void ReadsTheSizeFromTheAttributes() {
        SVGImage image = Parse("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40\"/></svg>");
        Assert.Equal(100f, image.GetWidth());
        Assert.Equal(50f, image.GetHeight());
    }

    [Fact]
    public void ScalingScalesTheSizeWithThePaths() {
        SVGImage image = Parse("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40\"/></svg>");
        image.ScaleBy(0.5f);
        Assert.Equal(50f, image.GetWidth());
        Assert.Equal(25f, image.GetHeight());
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        image.SetLocation(0f, 0f);
        TestSupport.AssertXY(50f, 25f, image.DrawOn(page));
        Assert.Contains("5 787 m\n45 772 l\n", TestSupport.Content(page));
    }

    [Fact]
    public void KeepsTheLastNumberOfAPathThatIsNotClosed() {
        Assert.Contains("10 782 m\n90 752 l\n", Draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40\"/></svg>"));
    }

    [Fact]
    public void SingleQuotesAndLineBreaksBetweenAttributesParseLikeDoubleQuotes() {
        string doubleQuotes = Draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\" fill=\"red\"/></svg>");
        string singleQuotes = Draw("<svg width='100' height='50'><path\n fill='red'\n d='M10 10 L90 40 L10 40 Z'/></svg>");
        Assert.Equal(doubleQuotes, singleQuotes);
        Assert.StartsWith("1 0 0 rg\n", doubleQuotes);
    }

    [Fact]
    public void EllipticalArcsBecomeCubicCurves() {
        string content = Draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 A 20 20 0 0 1 50 10\"/></svg>");
        // A half circle over the top, from (10, 10) to (50, 10) in SVG coordinates.
        Assert.Contains("10 793.05 18.95 802 30 802 c\n41.05 802 50 793.05 50 782 c\n", content);
    }

    [Fact]
    public void AStrokeOnlyPathIsStroked() {
        string content = Draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\" fill=\"none\" stroke=\"red\"/></svg>");
        Assert.Contains("1 0 0 RG\n", content);
        Assert.EndsWith("s\n", content);
    }

    private static int Count(string text, string part) {
        return (text.Length - text.Replace(part, "").Length) / part.Length;
    }

    [Fact]
    public void AnOpenPathWithAStrokeIsStroked() {
        string content = Draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40\" fill=\"none\" stroke=\"red\"/></svg>");
        Assert.EndsWith("10 782 m\n90 752 l\nS\n", content);
    }

    [Fact]
    public void AClosedSubpathIsClosedAndAnOpenOneStrokedAtTheEnd() {
        string content = Draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 Z M20 20 L30 30\" fill=\"none\" stroke=\"red\"/></svg>");
        Assert.EndsWith("10 782 m\n90 752 l\ns\n20 772 m\n30 762 l\nS\n", content);
    }

    [Fact]
    public void FillNoneWithoutAStrokeDrawsNothing() {
        Assert.Empty(Draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\" fill=\"none\"/></svg>"));
        Assert.Empty(Draw("<svg width=\"100\" height=\"50\" fill=\"none\"><path d=\"M10 10 L90 40 L10 40 Z\"/></svg>"));
    }

    [Fact]
    public void NoneOnThePathWinsOverTheColorsOfTheSvgElement() {
        string content = Draw("<svg width=\"100\" height=\"50\" fill=\"red\" stroke=\"green\">"
                + "<path d=\"M10 10 L90 40 L10 40 Z\" fill=\"none\" stroke=\"blue\"/>"
                + "<path d=\"M20 20 L80 30 L20 30 Z\" stroke=\"none\"/></svg>");
        Assert.Contains("0 0 1 RG\n", content);
        Assert.DoesNotContain("0 0.5 0 RG", content);
        // The second path takes the red fill of the svg element and has no stroke.
        Assert.Contains("1 0 0 rg\n", content);
        Assert.Equal(1, Count(content, "\nf\n"));
        Assert.Equal(1, Count(content, "\ns\n"));
    }

    [Fact]
    public void APathWithoutColorsOrWithAnUnknownFillIsFilledBlack() {
        string unset = Draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\"/></svg>");
        Assert.StartsWith("0 0 0 rg\n", unset);
        Assert.EndsWith("\nf\n", unset);
        string gradient = Draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\" fill=\"url(#g)\"/></svg>");
        Assert.Equal(unset, gradient);
    }
    // The numbers of path data are written as SVG 1.1 section 8.3.9 gives them:
    // a sign, digits, a point and an exponent, and a sign or a point starts the
    // next number where no space or comma separates them.

    [Fact]
    public void ReadsTheNumbersOfPathDataAsSVGWritesThem() {
        // Every path draws the line from 10, 10 to 90, 40, written another way.
        string[] paths = {
            "M10 10 L90 40",
            "M10,10L90,40",
            "M 1e1 1e1 L 9e1 4e1",
            "M 1000e-2 1000e-2 L 9000e-2 4000e-2",
            "M 1000E-2 1000E-2 L 9000E-2 4000E-2",
            "M+10+10L+90+40",
            "M 10.0 10.0 L 90.0 40.0",
            "M 10 10 L 9e+1 4e+1",
            "M10 10\nL90 40",      // Path data is often written over several lines
            "M10\t10\tL90\t40",
            "M10 10\r\nL90 40",
        };
        foreach (string data in paths) {
            string content = Draw("<svg width=\"100\" height=\"50\"><path d=\"" + data + "\"/></svg>");
            Assert.Contains("10 782 m\n90 752 l\n", content);
        }
    }

    [Fact]
    public void PathDataThatStartsWithANumberDrawsNothing() {
        // Path data starts with a moveto; the numbers before the first command
        // belong to no operation, and are left out rather than read as one.
        foreach (string data in new string[] {"10 10 L90 40", ".5.5L90 40", "-10L90 40"}) {
            string content = Draw("<svg width=\"100\" height=\"50\"><path d=\"" + data + "\"/></svg>");
            Assert.DoesNotContain(" m\n", content);
        }
    }
    [Fact]
    public void APathWithoutDataDrawsNothing() {
        // A path element without a d attribute is one this port threw on.
        string content = Draw("<svg width=\"100\" height=\"50\"><path/><path d=\"M10 10 L90 40\"/></svg>");
        Assert.Contains("10 782 m\n90 752 l\n", content);
    }

    [Fact]
    public void PathDataThatNeedsTheCurrentPointStartsAtTheOrigin() {
        // The first command of the path data needs a current point, which this
        // port left unset: it threw.
        string[] paths = {
            "L90 40", "H90", "V40", "Q10 10 90 40", "T90 40",
            "C1 1 2 2 90 40", "S1 1 90 40", "A5 5 0 0 1 90 40", "l90 40",
        };
        foreach (string data in paths) {
            string content = Draw("<svg width=\"100\" height=\"50\"><path d=\"" + data + "\"/></svg>");
            Assert.True(content.Contains(" l\n") || content.Contains(" c\n"), data + " draws " + content);
        }
    }

    [Fact]
    public void TheFlagsOfAnArcAreOneCharacterAndNeedNoSeparator() {
        // SVG 1.1 section 8.3.9 writes each flag of an elliptical arc as a
        // single character, so that nothing has to separate it from what
        // follows. Every path here draws the two half circles of the first.
        string separated = Draw("<svg width=\"100\" height=\"50\">" +
                "<path d=\"M10 10 A 20 20 0 0 1 50 10 A 20 20 0 1 0 90 10\"/></svg>");
        string[] paths = {
            "M10 10 A20 20 0 01 50 10 A20 20 0 10 90 10",
            "M10 10 A20 20 0 0150 10 A20 20 0 1090 10",
            "M10 10A20 20 0 0150,10A20 20 0 1090,10",
            "M10 10 a20 20 0 0140 0 a20 20 0 1040 0",
            "M10 10 A20,20,0,0,1,50,10 A20,20,0,1,0,90,10",
        };
        foreach (string data in paths) {
            string content = Draw("<svg width=\"100\" height=\"50\"><path d=\"" + data + "\"/></svg>");
            Assert.Equal(separated, content);
        }
    }

    [Fact]
    public void OnlyTheFlagsOfAnArcAreReadOneCharacterAtATime() {
        // The radii, the rotation and the end point of an arc are numbers like
        // any other, and the digits of every other command are too.
        string content = Draw("<svg width=\"100\" height=\"50\">" +
                "<path d=\"M10 10 A10 10 0 0 1 10 40 L10 10 A10 10 0 0 0 10 40\"/></svg>");
        string other = Draw("<svg width=\"100\" height=\"50\">" +
                "<path d=\"M10 10 A10 10 0 01 10 40 L10 10 A10 10 0 00 10 40\"/></svg>");
        Assert.Equal(content, other);
        Assert.True(content.Contains("10 782 m\n") && content.Contains(" c\n"), content);
    }

    [Fact]
    public void ArcRadiiTooSmallForTheEndPointsAreScaledToFitThem() {
        // SVG 1.1 section F.6.6 scales up radii too small to reach the end
        // points until the ellipse just does: the arc is then half of it,
        // whichever way the large arc flag points, and the same as the arc of
        // the fitting radii.
        string fitted = Draw("<svg width=\"200\" height=\"200\"><path d=\"M0 0 A50 50 0 0 1 100 0\"/></svg>");
        Assert.Equal(2, Curves(fitted));
        string[] paths = {
            "M0 0 A10 10 0 0 1 100 0",
            "M0 0 A1 1 0 0 1 100 0",
            "M0 0 A10 10 0 1 1 100 0",
        };
        foreach (string data in paths) {
            Assert.Equal(fitted, Draw("<svg width=\"200\" height=\"200\"><path d=\"" + data + "\"/></svg>"));
        }
    }

    [Fact]
    public void AnArcOfWholeQuarterTurnsIsDrawnInThatManyCurves() {
        // An arc is drawn in pieces of at most a quarter turn. One of exactly a
        // quarter, a half or a whole turn is not split once more for the last
        // bit of the sweep, which the four ports do not compute alike.
        string[] paths = {
            "A 120 120 120 0 0 120 120",    // A quarter turn, of the fuzz corpus
            "M10 10 A 20 20 0 0 1 50 10",
            "M0 0 A50 50 0 0 1 100 0",
        };
        int[] curves = {1, 2, 2};
        for (int i = 0; i < paths.Length; i++) {
            string content = Draw("<svg width=\"200\" height=\"200\"><path d=\"" + paths[i] + "\"/></svg>");
            Assert.True(curves[i] == Curves(content), paths[i] + " draws " + content);
        }
    }

    [Fact]
    public void ASizeWithAUnitIsReadInPoints() {
        // A number is in the user unit of the file, which PDFjet draws as a
        // point, and so is a number in px; the units of length are converted.
        string[] sizes = {"48", "48px", "48pt", "1in", "1pc", "210mm", "21cm", " 48 "};
        float[] points = {48f, 48f, 48f, 72f, 12f, 595.2756f, 595.2756f, 48f};
        for (int i = 0; i < sizes.Length; i++) {
            SVGImage svg = Parse("<svg width=\"" + sizes[i] + "\" height=\"" + sizes[i] + "\"/>");
            Assert.Equal(points[i], svg.GetWidth(), 3);
            Assert.Equal(points[i], svg.GetHeight(), 3);
        }
    }

    [Fact]
    public void ASizeThatCannotBeReadIsTheSizeOfTheViewBox() {
        // Scaling the paths by a width of zero would draw every one of them
        // at the origin, so a size in a unit PDFjet cannot read, a percentage
        // among them, and a size the file does not give, leave the drawing
        // 1:1 with its viewBox.
        string[] svgs = {
            "<svg viewBox=\"0 0 100 50\"><path d=\"M10 10 L90 40\"/></svg>",
            "<svg width=\"100%\" height=\"100%\" viewBox=\"0 0 100 50\"><path d=\"M10 10 L90 40\"/></svg>",
            "<svg width=\"10em\" height=\"5em\" viewBox=\"0 0 100 50\"><path d=\"M10 10 L90 40\"/></svg>",
        };
        foreach (string svg in svgs) {
            Assert.Equal(100f, Parse(svg).GetWidth());
            Assert.Equal(50f, Parse(svg).GetHeight());
            Assert.Contains("10 782 m\n90 752 l\n", Draw(svg));
        }
    }

    [Fact]
    public void AViewBoxThatIsNotFourNumbersOrHasNoSizeFails() {
        string[] boxes = {"0 0", "0 0 10 10 10", "0 0 ten 10", "0 0 0 10", "0 0 10 0"};
        string[] messages = {
            "four numbers are needed.", "four numbers are needed.", "four numbers are needed.",
            "its width and height cannot be zero.", "its width and height cannot be zero.",
        };
        for (int i = 0; i < boxes.Length; i++) {
            string svg = "<svg width=\"10\" height=\"10\" viewBox=\"" + boxes[i] + "\"/>";
            Exception e = Assert.Throws<Exception>(() => Parse(svg));
            Assert.Equal("Invalid SVG viewBox \"" + boxes[i] + "\": " + messages[i], e.Message);
        }
    }

    // What SVGImage draws of the SVG files of drawing programs: groups and
    // what they give their paths, transforms, the basic shapes, style
    // attributes and classes, fill rules, opacity, line caps and joins.

    [Fact]
    public void AGroupGivesItsPathsItsColorsAndWidthUnlessTheyHaveTheirOwn() {
        string content = Draw("<svg width=\"100\" height=\"100\"><g fill=\"red\" stroke=\"blue\" stroke-width=\"2\">"
                + "<path d=\"M10 10 H90 V90 Z\"/><path d=\"M10 10 H50 V50 Z\" fill=\"green\" stroke-width=\"4\"/></g></svg>");
        Assert.Equal("1 0 0 rg\n10 782 m\n90 782 l\n90 702 l\nf\n0 0 1 RG\n2 w\n10 782 m\n90 782 l\n90 702 l\ns\n"
                + "0 0.5 0 rg\n10 782 m\n50 782 l\n50 742 l\nf\n4 w\n10 782 m\n50 782 l\n50 742 l\ns\n", content);
    }

    [Fact]
    public void APathIsFilledBlackUnlessItsFillIsNoneAndStrokedOneUnitWide() {
        // As SVG draws them: a stroke does not take the fill away, and a
        // stroke width of 0 draws no stroke, where a PDF would draw the
        // thinnest line.
        string content = Draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\" stroke=\"red\"/></svg>");
        Assert.StartsWith("0 0 0 rg\n", content);
        Assert.Contains("1 0 0 RG\n1 w\n", content);
        content = Draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\" stroke=\"red\" stroke-width=\"0\"/></svg>");
        Assert.DoesNotContain("RG", content);
        Assert.EndsWith("\nf\n", content);
    }

    [Fact]
    public void TransformsOfGroupsAndPathsAreAppliedInTurn() {
        // The path is scaled, then moved by the group; its stroke is scaled too.
        string content = Draw("<svg width=\"100\" height=\"100\"><g transform=\"translate(10 20)\">"
                + "<path d=\"M0 0 L10 0\" transform=\"scale(2)\" stroke=\"red\" fill=\"none\"/></g></svg>");
        Assert.Equal("1 0 0 RG\n2 w\n10 772 m\n30 772 l\nS\n", content);
        string[][] transforms = {
            new string[] {"rotate(90)", "0 792 m\n0 782 l\n"},
            new string[] {"rotate(90 10 10)", "20 792 m\n20 782 l\n"},
            new string[] {"matrix(1 0 0 1 5 6)", "5 786 m\n15 786 l\n"},
            new string[] {"translate(5,6)", "5 786 m\n15 786 l\n"},
            new string[] {"translate(5)", "5 792 m\n15 792 l\n"},
            new string[] {"scale(2 3)", "0 792 m\n20 792 l\n"},
            new string[] {"skewY(45)", "0 792 m\n10 782 l\n"},
            new string[] {"translate(10) scale(2)", "10 792 m\n30 792 l\n"},
            new string[] {"scale(2) translate(10)", "20 792 m\n40 792 l\n"},
            new string[] {"translate(10)\n,rotate(90)", "10 792 m\n10 782 l\n"},
            new string[] {"translate(10) rotate", "0 792 m\n10 792 l\n"},    // Cannot be read: none
            new string[] {"translate(10) unknown(1 2)", "0 792 m\n10 792 l\n"},
            new string[] {"translate(10) scale(1 2 3)", "0 792 m\n10 792 l\n"},
            new string[] {"translate(1e1) scale(.5.5)", "10 792 m\n15 792 l\n"},
            new string[] {"translate(-5-5) scale(+1+1)", "-5 797 m\n5 797 l\n"},
        };
        foreach (string[] transform in transforms) {
            content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0\" transform=\"" + transform[0] + "\"/></svg>");
            Assert.True(content.Contains(transform[1]), transform[0] + " draws " + content);
        }
        // skewX leaves the line along the x axis as it is, and moves the point below.
        content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L0 10\" transform=\"skewX(45)\"/></svg>");
        Assert.Contains("0 792 m\n10 782 l\n", content);
    }

    [Fact]
    public void AShapeWithATransformThatFlattensItIsNotDrawn() {
        Assert.Empty(Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" transform=\"scale(0)\"/></svg>"));
    }

    [Fact]
    public void DrawsTheBasicShapes() {
        string[][] shapes = {
            new string[] {"<rect x=\"10\" y=\"20\" width=\"30\" height=\"40\"/>",
                "0 0 0 rg\n10 772 m\n40 772 l\n40 732 l\n10 732 l\nf\n"},
            new string[] {"<rect x=\"10\" y=\"20\" width=\"30\" height=\"40\" rx=\"5\"/>",
                "0 0 0 rg\n15 772 m\n35 772 l\n"
                + "37.76 772 40 769.76 40 767 c\n40 737 l\n40 734.24 37.76 732 35 732 c\n15 732 l\n"
                + "12.24 732 10 734.24 10 737 c\n10 767 l\n10 769.76 12.24 772 15 772 c\nf\n"},
            // A radius larger than half the side is half the side, and one
            // that is not given is the other one.
            new string[] {"<rect width=\"10\" height=\"10\" ry=\"20\"/>",
                "0 0 0 rg\n5 792 m\n5 792 l\n"
                + "7.76 792 10 789.76 10 787 c\n10 787 l\n10 784.24 7.76 782 5 782 c\n5 782 l\n"
                + "2.24 782 0 784.24 0 787 c\n0 787 l\n0 789.76 2.24 792 5 792 c\nf\n"},
            new string[] {"<circle cx=\"50\" cy=\"50\" r=\"10\"/>",
                "0 0 0 rg\n60 742 m\n60 736.48 55.52 732 50 732 c\n"
                + "44.48 732 40 736.48 40 742 c\n40 747.52 44.48 752 50 752 c\n55.52 752 60 747.52 60 742 c\nf\n"},
            new string[] {"<ellipse cx=\"50\" cy=\"50\" rx=\"20\" ry=\"10\"/>",
                "0 0 0 rg\n70 742 m\n70 736.48 61.05 732 50 732 c\n"
                + "38.95 732 30 736.48 30 742 c\n30 747.52 38.95 752 50 752 c\n61.05 752 70 747.52 70 742 c\nf\n"},
            new string[] {"<line x1=\"10\" y1=\"20\" x2=\"30\" y2=\"40\" stroke=\"red\"/>",
                "1 0 0 RG\n1 w\n10 772 m\n30 752 l\nS\n"},
            new string[] {"<polyline points=\"10,10 20,20 10,20\" fill=\"none\" stroke=\"red\"/>",
                "1 0 0 RG\n1 w\n10 782 m\n20 772 l\n10 772 l\nS\n"},
            new string[] {"<polygon points=\"10 10 20 20 10 20 5\" fill=\"none\" stroke=\"red\"/>",
                "1 0 0 RG\n1 w\n10 782 m\n20 772 l\n10 772 l\ns\n"},
            // Shapes of no size draw nothing.
            new string[] {"<rect width=\"0\" height=\"10\"/><circle r=\"0\"/><ellipse rx=\"5\"/><polygon points=\"1 2\"/><line/>", ""},
        };
        foreach (string[] shape in shapes) {
            string content = Draw("<svg width=\"100\" height=\"100\">" + shape[0] + "</svg>");
            Assert.True(shape[1] == content, shape[0] + " draws " + content);
        }
    }

    [Fact]
    public void StyleAttributesWinOverClassesWhichWinOverAttributes() {
        string svg = "<svg width=\"100\" height=\"100\"><defs><style>"
                + "/* the {colors} */ .red{fill:#ff0000} .blue, .other { fill: blue !important; stroke: red }"
                + "g .green{fill:green} .green:hover{fill:green}</style></defs>"
                + "<path d=\"M0 0 L10 0 L10 10 Z\" fill=\"yellow\" class=\"red\"/>"
                + "<path d=\"M0 0 L10 0 L10 10 Z\" class=\"blue red\" style=\"stroke: none\"/>"
                + "<path d=\"M0 0 L10 0 L10 10 Z\" class=\"red blue\" style=\"fill:yellow;stroke-width:3\"/>"
                + "<path d=\"M0 0 L10 0 L10 10 Z\" class=\"green\"/></svg>";
        string content = Draw(svg);
        System.Collections.Generic.List<string> fills = new System.Collections.Generic.List<string>();
        foreach (string line in content.Split('\n')) {
            if (line.EndsWith(" rg") || line.EndsWith(" RG") || line.EndsWith(" w")) {
                fills.Add(line);
            }
        }
        // The rules are applied in the order of the style sheet, whatever the
        // order of the classes; the selectors that are not a class alone are
        // left out.
        Assert.Equal("1 0 0 rg|0 0 1 rg|1 1 0 rg|1 0 0 RG|3 w|0 0 0 rg", string.Join("|", fills));
    }

    [Fact]
    public void ColorsAreHexadecimalRGBNamesOrTheCurrentColor() {
        string[][] fills = {
            new string[] {"fill=\"#F00\"", "1 0 0 rg"},
            new string[] {"fill=\"rgb(255, 0, 0)\"", "1 0 0 rg"},
            new string[] {"fill=\"rgb(100%,0%,0%)\"", "1 0 0 rg"},
            new string[] {"fill=\"RGBA(255 0 0 / 0.5)\"", "1 0 0 rg"},
            new string[] {"fill=\"Red\"", "1 0 0 rg"},
            new string[] {"fill=\"currentColor\" color=\"red\"", "1 0 0 rg"},
            new string[] {"fill=\"currentColor\"", "0 0 0 rg"},
            new string[] {"fill=\"url(#gradient) red\"", "1 0 0 rg"},
            new string[] {"fill=\"url(#gradient)\"", "0 0 0 rg"},   // Not drawn: as if not given
            new string[] {"fill=\"unknown\"", "0 0 0 rg"},
            new string[] {"fill=\"rgb(nan, 0, 0)\"", "0 0 0 rg"}, // Not a number: as if not given
            new string[] {"fill=\"rgb(inf, 0, 0)\"", "0 0 0 rg"},
            new string[] {"style=\"fill: currentColor; color: #0000ff\"", "0 0 1 rg"},
        };
        foreach (string[] fill in fills) {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" " + fill[0] + "/></svg>");
            Assert.True(content.StartsWith(fill[1] + "\n"), fill[0] + " draws " + content);
        }
        // The current color is the color where the fill is used, not where it is set.
        Assert.StartsWith("1 0 0 rg\n", Draw("<svg width=\"100\" height=\"100\" fill=\"currentColor\"><g color=\"red\">"
                + "<path d=\"M0 0 L10 0 L10 10 Z\"/></g></svg>"));
        Assert.ThrowsAny<Exception>(() => Parse("<svg><path d=\"M0 0 L1 1\" fill=\"#zzzzzz\"/></svg>"));
    }

    [Fact]
    public void TheEvenOddRuleFillsWithFStar() {
        string content = Draw("<svg width=\"100\" height=\"100\" style=\"fill-rule:evenodd\">"
                + "<path d=\"M0 0 H30 V30 H0 Z M10 10 H20 V20 H10 Z\"/><path d=\"M0 0 H30 V30 Z\" fill-rule=\"nonzero\"/></svg>");
        Assert.Equal(1, Count(content, "\nf*\n"));
        Assert.EndsWith("\nf\n", content);
    }

    [Fact]
    public void OpacityIsSetInAGraphicsStateOfThePathsOwn() {
        string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" opacity=\"0.5\"/>"
                + "<path d=\"M0 0 L10 0 L10 10 Z\"/></svg>");
        Assert.Equal("q\n/GS1 gs\n0 0 0 rg\n0 792 m\n10 792 l\n10 782 l\nf\nQ\n0 0 0 rg\n0 792 m\n10 792 l\n10 782 l\nf\n", content);
        // The opacity of a group multiplies those of its paths, and a fill or
        // a stroke of no opacity is not drawn.
        SVGImage image = Parse("<svg width=\"100\" height=\"100\"><g opacity=\"0.5\"><g style=\"opacity:50%\">"
                + "<path d=\"M0 0 L10 0 L10 10 Z\" fill-opacity=\"0.5\" stroke=\"red\" stroke-opacity=\"0\"/></g></g></svg>");
        SVGPath path = image.paths[0];
        Assert.Equal(0.125f, path.fillAlpha);
        Assert.Equal(0f, path.strokeAlpha);
        Assert.Equal(-1, path.stroke);
    }

    [Fact]
    public void LineCapsAndJoinsAreSetInAGraphicsStateOfThePathsOwn() {
        string content = Draw("<svg width=\"100\" height=\"100\" stroke-linecap=\"round\" stroke-linejoin=\"bevel\">"
                + "<path d=\"M0 0 L10 0\" stroke=\"red\" fill=\"none\"/><path d=\"M0 0 L10 0 L10 10 Z\"/></svg>");
        Assert.Equal("q\n1 J\n2 j\n1 0 0 RG\n1 w\n0 792 m\n10 792 l\nS\nQ\n0 0 0 rg\n0 792 m\n10 792 l\n10 782 l\nf\n", content);
    }

    [Fact]
    public void TheContentOfDefsAndOfWhatIsNotDisplayedIsNotDrawn() {
        string content = Draw("<svg width=\"100\" height=\"100\">"
                + "<defs><path d=\"M0 0 L10 0 L10 10 Z\"/></defs>"
                + "<clipPath><rect width=\"10\" height=\"10\"/></clipPath>"
                + "<symbol><circle r=\"5\"/></symbol>"
                + "<g display=\"none\"><path d=\"M0 0 L10 0 L10 10 Z\"/></g>"
                + "<g style=\"display:none\"><path d=\"M0 0 L10 0 L10 10 Z\" display=\"inline\"/></g>"
                + "<path d=\"M20 20 L30 20 L30 30 Z\"/></svg>");
        Assert.Equal("0 0 0 rg\n20 772 m\n30 772 l\n30 762 l\nf\n", content);
    }

    [Fact]
    public void AttributesOfOtherNamespacesAreLeftOut() {
        string content = Draw("<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:x=\"urn:x\" width=\"100\" height=\"100\">"
                + "<path d=\"M0 0 L10 0 L10 10 Z\" x:fill=\"red\" x:transform=\"scale(2)\"/></svg>");
        Assert.Equal("0 0 0 rg\n0 792 m\n10 792 l\n10 782 l\nf\n", content);
    }

    [Fact]
    public void TheViewBoxScalesTheStrokesAndKeepsItsProportions() {
        // The viewBox is scaled by 10 to fit the height, and centered across.
        string content = Draw("<svg width=\"200\" height=\"100\" viewBox=\"0 0 10 10\">"
                + "<path d=\"M0 0 L10 10\" stroke=\"red\" fill=\"none\"/></svg>");
        Assert.Equal("1 0 0 RG\n10 w\n50 792 m\n150 692 l\nS\n", content);
        string[][] aspects = {
            new string[] {"xMinYMin", "0 792 m\n100 692 l\n"},
            new string[] {"xMaxYMax meet", "100 792 m\n200 692 l\n"},
            new string[] {"defer xMidYMid", "50 792 m\n150 692 l\n"},
            new string[] {"xMidYMin slice", "20 w\n0 792 m\n200 592 l\n"},    // Covers it, from the top
            new string[] {"none", "0 792 m\n200 692 l\n"},
        };
        foreach (string[] aspect in aspects) {
            content = Draw("<svg width=\"200\" height=\"100\" viewBox=\"0 0 10 10\" preserveAspectRatio=\""
                    + aspect[0] + "\"><path d=\"M0 0 L10 10\" stroke=\"red\" fill=\"none\"/></svg>");
            Assert.True(content.Contains(aspect[1]), aspect[0] + " draws " + content);
        }
    }

    [Fact]
    public void ASizeThatIsNotGivenIsInTheProportionsOfTheViewBox() {
        string[] svgs = {
            "<svg width=\"48\" viewBox=\"0 0 960 480\"/>",
            "<svg height=\"24\" viewBox=\"0 0 960 480\"/>",
            "<svg width=\"10%\" viewBox=\"0 0 960 480\"/>",
            "<svg width=\"100\" height=\"50\" viewBox=\"0 0 960 480\"/>",
        };
        float[][] sizes = {
            new float[] {48f, 24f},
            new float[] {48f, 24f},
            new float[] {960f, 480f},
            new float[] {100f, 50f},
        };
        for (int i = 0; i < svgs.Length; i++) {
            SVGImage image = Parse(svgs[i]);
            Assert.Equal(sizes[i][0], image.GetWidth());
            Assert.Equal(sizes[i][1], image.GetHeight());
        }
    }

    [Fact]
    public void ScalingScalesTheStrokes() {
        SVGImage image = Parse("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40\" stroke=\"red\" stroke-width=\"4\"/></svg>");
        image.ScaleBy(0.5f);
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        image.SetLocation(0f, 0f);
        image.DrawOn(page);
        Assert.Contains("1 0 0 RG\n2 w\n", TestSupport.Content(page));
    }

    [Fact]
    public void ValuesThatCannotBeReadAreLeftOut() {
        string content = Draw("<svg width=\"100\" height=\"100\" viewBox=\"0,0,100,100\" stroke-width=\"3\">"
                + "<path d=\"M0 0 L10 0\" fill=\"none\" stroke=\"red\" stroke-width=\"calc(1px + 1px)\" opacity=\"half\"/>"
                + "<rect width=\"Inf\" height=\"10\" stroke-width=\"NaN\"/><polygon points=\"0 0 1e999 1 2 2\"/></svg>");
        Assert.Equal("1 0 0 RG\n3 w\n0 792 m\n10 792 l\nS\n", content);
    }

    [Fact]
    public void ASizeOrATransformTooLargeForAFloatDrawsNothing() {
        // A PDF holds numbers below 2^31 and no infinity: a size of inf is no
        // size, and a path a transform takes out of that range is not drawn.
        SVGImage image = Parse("<svg width=\"inf\" height=\"nan\" viewBox=\"0 0 100 50\"/>");
        Assert.Equal(100f, image.GetWidth());
        Assert.Equal(50f, image.GetHeight());
        string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" transform=\"scale(1e30)\"/>"
                + "<path d=\"M0 0 L10 0\" stroke=\"red\" transform=\"scale(1e20 1)\"/><path d=\"M0 0 L10 0 L10 10 Z\"/></svg>");
        Assert.Equal("0 0 0 rg\n0 792 m\n10 792 l\n10 782 l\nf\n", content);
    }

    [Fact]
    public void ASkewOfNinetyDegreesIsNotRead() {
        foreach (string transform in new string[] {"skewX(90)", "skewY(-270)"}) {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" transform=\""
                    + transform + "\"/></svg>");
            Assert.Equal("0 0 0 rg\n0 792 m\n10 792 l\n10 782 l\nf\n", content);
        }
    }

    private static int Curves(string content) {
        int count = 0;
        for (int i = content.IndexOf(" c\n"); i != -1; i = content.IndexOf(" c\n", i + 1)) {
            count++;
        }
        return count;
    }
}
}
