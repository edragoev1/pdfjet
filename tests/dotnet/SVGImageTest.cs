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

    // What the image draws, without the miter limit that is set around an
    // image that strokes, which TheMiterLimitOfAStrokeIsWritten checks.
    internal static string WithoutMiterLimit(string content) {
        return content.StartsWith("q\n4 M\n", StringComparison.Ordinal)
                ? content.Substring(6, content.Length - 8) : content;
    }

    private static string Draw(string svg) {
        return WithoutMiterLimit(DrawAsItIs(svg));
    }

    private static string DrawAsItIs(string svg) {
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
        Assert.EndsWith("h\nS\n", content);
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
    public void AClosedSubpathIsClosedAndThePathStrokedOnceAtTheEnd() {
        string content = Draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 Z M20 20 L30 30\" fill=\"none\" stroke=\"red\"/></svg>");
        Assert.EndsWith("10 782 m\n90 752 l\nh\n20 772 m\n30 762 l\nS\n", content);
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
        Assert.Equal(1, Count(content, "\nh\nS\n"));
    }

    [Fact]
    public void APathWithoutColorsIsFilledBlackAndOneOfAnUnknownURLIsNotFilled() {
        string unset = Draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\"/></svg>");
        Assert.StartsWith("0 0 0 rg\n", unset);
        Assert.EndsWith("\nf\n", unset);
        // A url of an element that is not there is none, as a browser draws it.
        string unknown = Draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\" fill=\"url(#g)\"/></svg>");
        Assert.Equal("", unknown);
    }

    [Fact]
    public void AGradientIsDrawnInTheMeanColorOfItsStops() {
        string stops = "<stop offset=\"0\" stop-color=\"#ff0000\"/><stop offset=\"1\" style=\"stop-color: blue\"/>";
        string content = Draw("<svg width=\"100\" height=\"50\"><defs><linearGradient id=\"g\">" + stops
                + "</linearGradient><radialGradient id=\"h\" href=\"#g\"/></defs>"
                + "<path d=\"M10 10 L90 40 L10 40 Z\" fill=\"url(#g)\"/><path d=\"M10 10 L90 40\" stroke=\"url('#h')\"/></svg>");
        // 255/2 rounds half up; the second gradient takes the stops of the first.
        Assert.True(content.StartsWith("0.5 0 0.5 rg\n"), content);
        Assert.True(content.Contains("0.5 0 0.5 RG\n"), content);
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
    public void PathDataThatDoesNotStartWithAMovetoDrawsNothing() {
        // The first command of the path data needs a current point, which this
        // port left unset: it threw. Path data starts with a moveto, and SVG
        // 1.1 section 8.3.2 draws data in error up to where it is in error,
        // which here is nothing; a PDF has no current point to draw a line
        // from either.
        string[] paths = {
            "L90 40", "H90", "V40", "Q10 10 90 40", "T90 40",
            "C1 1 2 2 90 40", "S1 1 90 40", "A5 5 0 0 1 90 40", "l90 40", "Z L90 40",
        };
        foreach (string data in paths) {
            string content = Draw("<svg width=\"100\" height=\"50\"><path d=\"" + data + "\"/>"
                    + "<path d=\"M1 1 L2 2\" fill=\"none\" stroke=\"red\"/></svg>");
            Assert.True(content == "1 0 0 RG\n1 w\n1 791 m\n2 790 l\nS\n", data + " draws " + content);
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
            "M0 0 A 120 120 120 0 0 120 120",   // A quarter turn, of the fuzz corpus
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
    public void AViewBoxInLowerCaseIsRead() {
        // A file saved from a web page may write viewbox, which an HTML page's
        // parser reads as viewBox; viewBox wins when a file has both.
        string[] svgs = {
            "<svg width=\"50\" height=\"25\" viewbox=\"10 10 100 50\"><path d=\"M20 20 L110 60\"/></svg>",
            "<svg width=\"50\" height=\"25\" viewBox=\"10 10 100 50\" viewbox=\"0 0 200 100\"><path d=\"M20 20 L110 60\"/></svg>",
        };
        foreach (string svg in svgs) {
            Assert.Contains("5 787 m\n50 767 l\n", Draw(svg));
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
            "its width and height have to be above zero.", "its width and height have to be above zero.",
        };
        for (int i = 0; i < boxes.Length; i++) {
            string svg = "<svg width=\"10\" height=\"10\" viewBox=\"" + boxes[i] + "\"/>";
            Exception e = Assert.Throws<InvalidDataException>(() => Parse(svg));
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
        Assert.Equal("1 0 0 rg\n10 782 m\n90 782 l\n90 702 l\nf\n0 0 1 RG\n2 w\n10 782 m\n90 782 l\n90 702 l\nh\nS\n"
                + "0 0.5 0 rg\n10 782 m\n50 782 l\n50 742 l\nf\n4 w\n10 782 m\n50 782 l\n50 742 l\nh\nS\n", content);
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
                "1 0 0 RG\n1 w\n10 782 m\n20 772 l\n10 772 l\nh\nS\n"},
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
            new string[] {"fill=\"RGBA(255 0 0 / 1)\"", "1 0 0 rg"},
            new string[] {"fill=\"Red\"", "1 0 0 rg"},
            new string[] {"fill=\"currentColor\" color=\"red\"", "1 0 0 rg"},
            new string[] {"fill=\"currentColor\"", "0 0 0 rg"},
            new string[] {"fill=\"url(#gradient) red\"", "1 0 0 rg"},
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

    [Fact]
    public void ReadsTheNumbersOfSVG() {
        foreach (string number in new string[] {"5.", ".5", "-.5", "+5", "-0.5e+2", "5E-1", "007"}) {
            Parse("<svg width=\"100\" height=\"100\"><path d=\"M 0 0 L " + number + " 5\"/></svg>");
        }
    }

    [Fact]
    public void RefusesPathDataWithANumberThatIsNotOne() {
        // The numbers the fuzzers made of path data: refused in words, in the
        // four ports, where C# failed with a FormatException (found by the
        // fuzz replay, 8 October 2026).
        // And the numbers one language reads and the others do not, refused by
        // SVG's grammar in the four (the review of 9 October 2026).
        foreach (string number in new string[] {"-.", ".", "2.@5", "1d775", "1e40", "-1e40",
                "0x1p3", "1_0", "1f", "1e"}) {
            string svg = "<svg width=\"100\" height=\"100\"><path d=\"M 0 0 L " + number + " 5\"/></svg>";
            Exception e = Assert.ThrowsAny<Exception>(() => Parse(svg));
            Assert.Contains("invalid path data", e.Message.ToLowerInvariant());
        }
    }

    // A number of an attribute is written in decimal: a hexadecimal one, as
    // 0x1p3, or one with underscores, as 1_000, is none of SVG's, and is not
    // read in any port (review of 17 October 2026)
    [Fact]
    public void ANumberOfAnAttributeIsWrittenInDecimal() {
        var parse = typeof(PDF).Assembly.GetType("PDFjet.NET.SVGState").GetMethod("ParseDouble",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        foreach (String text in new[] {"0x1p3", "1_000", "0x10", "1f"}) {
            Assert.False((bool) parse.Invoke(null, new object[] {text, 0.0}), text);
        }
        object[] args = {"1e3", 0.0};
        Assert.True((bool) parse.Invoke(null, args));
        Assert.Equal(1000.0, (double) args[1]);
        args = new object[] {"-inf", 0.0};
        Assert.True((bool) parse.Invoke(null, args));
        Assert.Equal(double.NegativeInfinity, (double) args[1]);
    }

    // İ in a color is i, its simple lower case, as in the other ports: .NET's
    // invariant culture left it as it was (review of 17 October 2026)
    [Fact]
    public void ADottedCapitalIInAColorIsI() {
        var parse = typeof(PDF).Assembly.GetType("PDFjet.NET.SVGState").GetMethod("ParseColor",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        object[] args = {"fill", "L\u0130ME", 0, 0f};
        Assert.True((bool) parse.Invoke(null, args));
        object[] lime = {"fill", "lime", 0, 0f};
        parse.Invoke(null, lime);
        Assert.Equal(lime[2], args[2]);
    }

    [Fact]
    public void DrawsTheSeedOfTheFuzzTestOfGroupsTransformsShapesAndStyles() {
        // The seed of the Go port's fuzz test for what drawing programs write.
        string content = Draw("<svg width=\"100\" height=\"50\" viewBox=\"0 0 200 100\" preserveAspectRatio=\"xMinYMax slice\">"
                + "<style>.a{fill:rgb(10%,20,30);stroke:currentColor;stroke-width:2px} .b{opacity:.5}</style>"
                + "<g transform=\"translate(10,5) rotate(30 5 5) skewX(10)\" color=\"blue\" class=\"a\" fill-rule=\"evenodd\">"
                + "<rect x=\"1\" y=\"2\" width=\"30\" height=\"20\" rx=\"4\" class=\"b\"/><circle cx=\"50\" cy=\"20\" r=\"8\"/>"
                + "<ellipse cx=\"80\" cy=\"20\" rx=\"10\" ry=\"5\" style=\"fill-opacity:0.3;stroke-linecap:round\"/>"
                + "<line x1=\"0\" y1=\"40\" x2=\"90\" y2=\"45\" stroke-linejoin=\"bevel\"/>"
                + "<polyline points=\"0,50 10,60 20,50\"/><polygon points=\"30 50 40 60 50 50\"/></g>"
                + "<defs><path d=\"M0 0 L5 5\"/></defs><g display=\"none\"><rect width=\"5\" height=\"5\"/></g></svg>");
        // Every shape but the line, which has no inside, is filled with the
        // even-odd rule; the first in a graphics state of its opacity.
        Assert.Equal(5, Count(content, "\nf*\n"));
        // The slice draws past the size of the image, which cuts it off.
        Assert.True(content.StartsWith("q\n0 792 m\n100 792 l\n100 742 l\n0 742 l\nW\nn\n"
                + "q\n/GS1 gs\n0.1 0.08 0.12 rg\n8.4 788.21 m\n17.93 782.71 l\n"), content);
    }

    // The text of a chart: text and tspan elements, placed as a browser
    // places them, in the fonts PDFjet carries unless a font is set.

    [Fact]
    public void DrawsATextAtItsXAndY() {
        string content = Draw("<svg width=\"200\" height=\"100\"><text x=\"10\" y=\"20\" font-size=\"12\">Hi</text></svg>");
        Assert.True(content.Contains(" 12 Tf\n1 0 0 1 10 772 Tm\n<"), content);
        Assert.True(content.Contains("> Tj\nET\n"), content);     // IBM Plex Sans, embedded
        Assert.False(content.Contains("W\n"), content);     // Inside the image: nothing is cut off
    }

    [Fact]
    public void AnAnchorMovesTheTextByItsWidthAndDyIsInEm() {
        // Hi is 957 thousandths of an em wide in IBM Plex Sans: 11.484 at 12.
        string content = Draw("<svg width=\"200\" height=\"100\"><text x=\"100\" y=\"20\" dy=\"0.5em\" "
                + "font-size=\"12\" text-anchor=\"end\">Hi</text></svg>");
        Assert.True(content.Contains("1 0 0 1 88.52 766 Tm\n"), content);
    }

    [Fact]
    public void TheTextAndTheTspansAreDrawnInTheOrderOfTheDocument() {
        string content = Draw("<svg width=\"200\" height=\"100\"><text x=\"0\" y=\"20\">"
                + " A <tspan font-weight=\"bold\">B</tspan>C<title>not drawn</title></text></svg>");
        // Three runs: "A " (the space at the start left out), the bold B in
        // IBM Plex Sans Bold, and C; the title is not drawn
        Assert.Equal(3, Count(content, " Tj\n"));
        string[] fonts = content.Split(" 16 Tf\n");
        Assert.Equal(4, fonts.Length);
        string regular = fonts[0].Substring(fonts[0].LastIndexOf('/'));
        Assert.False(fonts[1].EndsWith(regular, StringComparison.Ordinal), content);
        Assert.True(fonts[2].EndsWith(regular, StringComparison.Ordinal), content);
    }

    [Fact]
    public void ATspanWithAnXOrAYStartsAChunkAnchoredOnItsOwn() {
        string content = Draw("<svg width=\"200\" height=\"100\"><text text-anchor=\"middle\" "
                + "transform=\"translate(100,50)\" font-size=\"10\"><tspan y=\"-0.4em\">Go</tspan>"
                + "<tspan x=\"0\" y=\"0.7em\">12%</tspan></text></svg>");
        // Go is 1255 thousandths of an em, and 12% 2127.
        Assert.True(content.Contains("1 0 0 1 93.72 746 Tm\n"), content);
        Assert.True(content.Contains("1 0 0 1 89.36 735 Tm\n"), content);
    }

    [Fact]
    public void TheFontShorthandSetsTheSizeAndTheWeight() {
        string content = Draw("<svg width=\"200\" height=\"100\" style=\"font: bold 10px sans-serif\">"
                + "<text y=\"20\">A</text><text y=\"40\" font-weight=\"400\">B</text></svg>");
        Assert.Equal(2, Count(content, " 10 Tf\n"));
        Assert.True(content.IndexOf("/F", StringComparison.Ordinal) != content.LastIndexOf("/F", StringComparison.Ordinal), content);
    }

    [Fact]
    public void ACoreFontDrawsTheMinusSignAsAHyphenMinus() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Parse("<svg width=\"200\" height=\"100\"><text y=\"20\">\u22125</text></svg>")
                .SetFont(new Font(pdf, CoreFont.HELVETICA)).DrawOn(page);
        Assert.True(TestSupport.Content(page).Contains("[<2D35>] TJ"), TestSupport.Content(page));
    }

    [Fact]
    public void TheFamilyTheWeightAndTheStyleChooseTheFontPDFjetCarries() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Parse("<svg width=\"200\" height=\"100\">"
                + "<text y=\"10\" font-family=\"'Times New Roman', serif\" font-weight=\"600\" font-style=\"italic\">a</text>"
                + "<text y=\"20\" style=\"font: bold 10px Courier, monospace\">b</text>"
                + "<text y=\"30\" font-family=\"Unknown, Arial\">c</text>"
                + "<text y=\"40\" font-family=\"Unknown\" font-weight=\"bolder\">d</text></svg>").DrawOn(page);
        // Another image draws in the same fonts of the document, not new ones
        Parse("<svg width=\"200\" height=\"100\"><text y=\"10\" font-family=\"Georgia\" "
                + "font-weight=\"600\" font-style=\"oblique\">e</text></svg>").DrawOn(page);
        Assert.Equal(new System.Collections.Generic.SortedSet<string>(new[] {
                "IBMPlexSerif/IBMPlexSerif-SemiBoldItalic.ttf", "CascadiaMono/CascadiaMono-Bold.ttf",
                "IBMPlexSans/IBMPlexSans-Regular.ttf", "IBMPlexSans/IBMPlexSans-Bold.ttf"}, StringComparer.Ordinal),
                new System.Collections.Generic.SortedSet<string>(pdf.bundledFonts.Keys, StringComparer.Ordinal));
    }

    [Fact]
    public void TheTextOfADocumentOfPDFAOrPDFUAIsInEmbeddedFonts() {
        // The fonts PDFjet carries are embedded, so the text needs no font set
        string svg = "<svg width=\"200\" height=\"100\"><text y=\"20\">A</text></svg>";
        foreach (Compliance compliance in new Compliance[] {Compliance.PDF_A_3B, Compliance.PDF_UA_1}) {
            PDF pdf = new PDF(new MemoryStream(), compliance);
            pdf.SetTitle("SVG");
            Page page = new Page(pdf, Letter.PORTRAIT);
            Parse(svg).SetAltDescription("A").DrawOn(page);
            pdf.Complete();
        }
    }

    [Fact]
    public void AStrokeBeforeTheFillDrawsAHaloUnderTheText() {
        string content = Draw("<svg width=\"200\" height=\"100\"><text y=\"20\" stroke=\"white\" stroke-width=\"3\" "
                + "stroke-linejoin=\"round\" paint-order=\"stroke\">A</text></svg>");
        int stroke = content.IndexOf("1 Tr\n", StringComparison.Ordinal);
        int fill = content.IndexOf("0 Tr\n", StringComparison.Ordinal);
        Assert.True(stroke > 0 && fill > stroke, content);
        Assert.True(content.Contains("1 1 1 RG\n3 w\n"), content);
    }

    [Fact]
    public void ASetFontDrawsTheText() {
        PDF pdf = TestSupport.NewPDF();
        Font font = new Font(pdf, TestSupport.Open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"));
        Page page = new Page(pdf, Letter.PORTRAIT);
        SVGImage image = Parse("<svg width=\"200\" height=\"100\"><text y=\"20\" font-weight=\"bold\">Hi</text></svg>");
        image.SetFont(font);    // The bold text too, as no bold font is set
        image.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.True(content.Contains("> Tj\n"), content);
        Assert.False(content.Contains("TJ"), content);
    }

    [Fact]
    public void ADashArrayOfAnOddNumberOfLengthsIsRepeated() {
        string content = Draw("<svg width=\"200\" height=\"100\"><path d=\"M0 10 L100 10\" stroke=\"red\" "
                + "stroke-dasharray=\"4, 2 1\" stroke-dashoffset=\"1\"/>"
                + "<path d=\"M0 20 L100 20\" stroke=\"red\" stroke-dasharray=\"0 0\"/>"
                + "<path d=\"M0 30 L100 30\" stroke=\"red\" stroke-dasharray=\"4 -2\"/></svg>");
        Assert.True(content.Contains("q\n[4 2 1 4 2 1] 1 d\n"), content);
        Assert.Equal(1, Count(content, " d\n"));    // The others are solid
    }

    [Fact]
    public void AUseDrawsTheElementItNamesMovedByItsXAndY() {
        string content = Draw("<svg xmlns:xlink=\"http://www.w3.org/1999/xlink\" width=\"200\" height=\"100\">"
                + "<defs><rect id=\"r\" width=\"10\" height=\"10\"/><symbol id=\"s\"><circle r=\"2\"/></symbol></defs>"
                + "<use href=\"#r\" x=\"20\" y=\"30\" fill=\"red\"/><use xlink:href=\"#s\" x=\"50\" y=\"50\"/>"
                + "<g id=\"loop\"><use href=\"#loop\"/></g></svg>");
        Assert.True(content.StartsWith("1 0 0 rg\n20 762 m\n30 762 l\n"), content);
        Assert.True(content.Contains("\n52 742 m\n"), content);
    }

    [Fact]
    public void UsesOfUsesThatDrawTooManyElementsThrow() {
        StringBuilder svg = new StringBuilder("<svg width=\"10\" height=\"10\"><rect id=\"g0\" width=\"1\" height=\"1\"/>");
        for (int level = 1; level <= 7; level++) {
            svg.Append("<g id=\"g").Append(level).Append("\">");
            for (int i = 0; i < 10; i++) {
                svg.Append("<use href=\"#g").Append(level - 1).Append("\"/>");
            }
            svg.Append("</g>");
        }
        svg.Append("</svg>");
        Assert.Throws<ArgumentException>(() => Parse(svg.ToString()));
    }

    [Fact]
    public void AMarkerIsDrawnAtTheEndTurnedWithThePath() {
        string content = Draw("<svg width=\"200\" height=\"100\"><defs><marker id=\"m\" markerWidth=\"4\" "
                + "markerHeight=\"4\" orient=\"auto\" refX=\"1\"><rect width=\"2\" height=\"1\"/></marker></defs>"
                + "<path d=\"M10 10 L10 50\" stroke=\"black\" stroke-width=\"2\" marker-end=\"url(#m)\"/></svg>");
        // Turned 90 degrees down the path and twice the size, for the stroke
        // width: from 10, 48 to 8, 52.
        Assert.True(content.Contains("S\nq\n10 744 m\n10 736 l\n2 736 l\n2 744 l\nh\nW\nn\n10 744 m\n10 740 l\n8 740 l\n8 744 l\nf\nQ\n"), content);
    }

    [Fact]
    public void AClipPathCutsOffWhatIsOutsideIt() {
        string content = Draw("<svg width=\"200\" height=\"100\"><clipPath id=\"c\"><rect width=\"50\" height=\"50\"/>"
                + "</clipPath><g clip-path=\"url(#c)\" transform=\"translate(10,0)\"><rect x=\"10\" y=\"10\" width=\"80\" "
                + "height=\"20\"/></g><clipPath id=\"none\"/><rect width=\"5\" height=\"5\" clip-path=\"url(#none)\"/></svg>");
        // The clip path is in the user space of the group, moved with it.
        Assert.Equal("q\n10 792 m\n60 792 l\n60 742 l\n10 742 l\nh\nW\nn\n"
                + "0 0 0 rg\n20 782 m\n100 782 l\n100 762 l\n20 762 l\nf\nQ\n", content);
    }

    // An SVG of a chain of use elements, flat in the file.
    private static string UseChain(int links) {
        StringBuilder svg = new StringBuilder("<svg width=\"10\" height=\"10\"><defs><rect id=\"u0\" width=\"1\" height=\"1\"/>");
        for (int i = 1; i <= links; i++) {
            svg.Append("<use id=\"u").Append(i).Append("\" href=\"#u").Append(i - 1).Append("\"/>");
        }
        return svg.Append("</defs><use href=\"#u").Append(links).Append("\"/></svg>").ToString();
    }

    // An SVG of elements nested levels deep in the svg element, around the content.
    private static string Nested(string open, string end, string content, int levels) {
        StringBuilder svg = new StringBuilder("<svg width=\"10\" height=\"10\">");
        for (int i = 0; i < levels; i++) {
            svg.Append(open);
        }
        svg.Append(content);
        for (int i = 0; i < levels; i++) {
            svg.Append(end);
        }
        return svg.Append("</svg>").ToString();
    }

    // An SVG of markers each drawn by a path in the one before it.
    private static string MarkerChain(int links) {
        StringBuilder svg = new StringBuilder("<svg width=\"10\" height=\"10\">");
        for (int i = 0; i < links; i++) {
            svg.Append("<marker id=\"m").Append(i).Append("\"><path d=\"M0 0 L1 1\" stroke=\"red\" marker-end=\"url(#m")
                    .Append(i + 1).Append(")\"/></marker>");
        }
        svg.Append("<marker id=\"m").Append(links).Append("\"><rect width=\"1\" height=\"1\"/></marker>");
        return svg.Append("<path d=\"M0 0 L1 1\" stroke=\"red\" marker-end=\"url(#m0)\"/></svg>").ToString();
    }

    private static string Tspans(int levels) {
        return Nested("<g>", "</g>", "<text>a" + Nested("<tspan>b", "</tspan>", "", levels)
                .Replace("<svg width=\"10\" height=\"10\">", "").Replace("</svg>", "") + "</text>", 2);
    }

    [Fact]
    public void TheDepthOfWhatIsDrawnIsLimitedOnASmallStack() {
        Exception failure = null;
        System.Threading.Thread thread = new System.Threading.Thread(() => {
            try {
                Parse(UseChain(22));
                Parse(MarkerChain(14));
                Parse(Nested("<g>", "</g>", "<rect width=\"1\" height=\"1\"/>", 46));
                Parse(Tspans(43));
                foreach (string svg in new string[] {
                        UseChain(23), UseChain(2000), MarkerChain(15), Tspans(44),
                        Nested("<g>", "</g>", "<rect width=\"1\" height=\"1\"/>", 47), Nested("<g>", "</g>", "", 250)}) {
                    ArgumentException e = Assert.Throws<ArgumentException>(() => Parse(svg));
                    Assert.Contains("deep", e.Message);
                }
            } catch (Exception e) {
                failure = e;
            }
        }, 512*1024);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void ManyGradientsOfOneGradientOfManyStopsAreReadOnce() {
        const int count = 5000;
        var svgs = new System.Collections.Generic.Dictionary<int, string>();
        foreach (int n in new int[] {count, 2*count}) {
            StringBuilder svg = new StringBuilder("<svg width=\"10\" height=\"10\"><linearGradient id=\"s\">");
            for (int i = 0; i < n; i++) {
                svg.Append("<stop stop-color=\"red\"/>");
            }
            svg.Append("</linearGradient>");
            for (int i = 0; i < n; i++) {
                svg.Append("<linearGradient id=\"g").Append(i).Append("\" href=\"#s\"/>");
            }
            svg.Append("<rect width=\"5\" height=\"5\" fill=\"url(#g7)\"/></svg>");
            svgs[n] = svg.ToString();
        }
        string content = null;
        TestSupport.AssertLinearTime(count, n => content = Draw(svgs[n]));
        Assert.StartsWith("1 0 0 rg\n", content);
    }

    private static string Repeat(string text, int count) {
        StringBuilder repeated = new StringBuilder();
        for (int i = 0; i < count; i++) {
            repeated.Append(text);
        }
        return repeated.ToString();
    }

    private static string Polygon(int points) {
        return "<polygon id=\"p\" points=\"" + Repeat("0,0 1,1 ", points/2) + "\"/>";
    }

    [Fact]
    public void TheOperationsDrawnAgainAreLimited() {
        string clipped = "<svg width=\"10\" height=\"10\"><clipPath id=\"c\">" + Polygon(10000) + "</clipPath>";
        foreach (string svg in new string[] {
                "<svg width=\"10\" height=\"10\"><defs>" + Polygon(10000) + "</defs>"
                        + Repeat("<use href=\"#p\"/>", 1000) + "</svg>",
                clipped + Repeat("<g clip-path=\"url(#c)\"/>", 1000) + "</svg>",
                clipped + "<g clip-path=\"url(#c)\">" + Repeat("<rect width=\"1\" height=\"1\"/>", 1000) + "</g></svg>",
                clipped + "<g clip-path=\"url(#c)\"><text>" + Repeat("<tspan>a</tspan>", 1000) + "</text></g></svg>",
                "<svg width=\"10\" height=\"10\"><defs><text id=\"p\">" + Repeat("a", 10000) + "</text></defs>"
                        + Repeat("<use href=\"#p\"/>", 1000) + "</svg>",
                "<svg width=\"10\" height=\"10\"><clipPath id=\"c\"><text>" + Repeat("a", 1000)
                        + "</text></clipPath><g clip-path=\"url(#c)\">" + Repeat("<rect width=\"1\" height=\"1\"/>", 5000) + "</g></svg>",
                "<svg width=\"10\" height=\"10\"><clipPath id=\"c\"><text>a" + Repeat("<tspan/>", 100000)
                        + "</text></clipPath>" + Repeat("<g clip-path=\"url(#c)\"/>", 100) + "</svg>",
                Nested("<svg width=\"1\" height=\"1\">", "</svg>", Repeat("<rect width=\"1\" height=\"1\"/>", 20000), 45)}) {
            ArgumentException e = Assert.Throws<ArgumentException>(() => Parse(svg));
            Assert.Contains("path operations", e.Message);
        }
        // A file drawn once is not counted, however large
        Parse("<svg width=\"10\" height=\"10\">" + Repeat(Polygon(10000), 500) + "</svg>");
        Parse("<svg width=\"10\" height=\"10\"><defs>" + Polygon(1000) + "</defs>"
                + Repeat("<use href=\"#p\"/>", 1000) + "</svg>");
    }

    [Fact]
    public void AUseInAClipPathDrawsTheShapeItNames() {
        {
            string content = Draw("<svg width=\"200\" height=\"100\"><defs><rect id=\"r\" width=\"50\" height=\"50\"/></defs><clipPath id=\"c\"><use href=\"#r\" x=\"5\" y=\"5\" transform=\"translate(10,0)\"/></clipPath><g clip-path=\"url(#c)\"><rect x=\"10\" y=\"10\" width=\"80\" height=\"20\"/></g></svg>");
            Assert.Equal("q\n15 787 m\n65 787 l\n65 737 l\n15 737 l\nh\nW\nn\n0 0 0 rg\n10 782 m\n90 782 l\n90 762 l\n10 762 l\nf\nQ\n", content);
        }
        {
            string content = Draw("<svg width=\"200\" height=\"100\"><clipPath id=\"c\"><use href=\"#missing\"/></clipPath><rect width=\"5\" height=\"5\" clip-path=\"url(#c)\"/></svg>");
            Assert.Equal("", content);
        }
    }

    [Fact]
    public void TheClipRuleOfAClipPathIsDrawn() {
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><path clip-rule=\"evenodd\" d=\"M0 0 H50 V50 H0 Z\"/></clipPath><rect width=\"5\" height=\"5\" clip-path=\"url(#c)\"/></svg>");
            Assert.Contains("h\nW*\nn\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\" style=\"clip-rule:evenodd\"><rect width=\"50\" height=\"50\"/></clipPath><rect width=\"5\" height=\"5\" clip-path=\"url(#c)\"/></svg>");
            Assert.Contains("h\nW*\nn\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><rect width=\"50\" height=\"50\"/></clipPath><rect width=\"5\" height=\"5\" clip-path=\"url(#c)\"/></svg>");
            Assert.Contains("h\nW\nn\n", content);
        }
    }

    [Fact]
    public void ATextInAClipPathClips() {
        string content = Draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><text x=\"10\" y=\"20\" font-size=\"12\">Hi</text></clipPath><rect width=\"50\" height=\"50\" clip-path=\"url(#c)\"/></svg>");
        Assert.StartsWith("q\nBT\n7 Tr\n/", content);
        Assert.Contains(" 12 Tf\n1 0 0 1 10 772 Tm\n<", content);
        Assert.Contains("> Tj\nET\n0 0 0 rg\n0 792 m\n", content);
        Assert.EndsWith("f\nQ\n", content);
        Assert.DoesNotContain("W\n", content);
        // A text drawn in it is drawn in the mode of its own.
        Assert.Contains("0 Tr\n1 0 0 1 0 772 Tm\n", Draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><text y=\"20\">Hi</text></clipPath><text y=\"20\" clip-path=\"url(#c)\">Hi</text></svg>"));
        // All the texts are in one text object, each with its transform.
        content = Draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\" transform=\"translate(5,0)\"><text x=\"1\" y=\"20\">a<tspan x=\"1\" y=\"40\" font-weight=\"bold\">b</tspan></text><text x=\"1\" y=\"60\" transform=\"scale(2)\">c</text></clipPath><rect width=\"50\" height=\"50\" clip-path=\"url(#c)\"/></svg>");
        Assert.Equal(2, content.Split(new[] {"BT\n"}, StringSplitOptions.None).Length);
        Assert.Equal(4, content.Split(new[] {"> Tj\n"}, StringSplitOptions.None).Length);
        Assert.Equal(4, content.Split(new[] {" Tf\n"}, StringSplitOptions.None).Length);
        Assert.Contains("1 0 0 1 6 772 Tm\n", content);
        Assert.Contains("1 0 0 1 6 752 Tm\n", content);
        Assert.Contains("2 0 0 2 7 672 Tm\n", content);
        // A use names a text.
        Assert.Contains("1 0 0 1 5 772 Tm\n", Draw("<svg width=\"100\" height=\"100\"><defs><text id=\"t\" y=\"20\">Hi</text></defs><clipPath id=\"c\"><use href=\"#t\" x=\"5\"/></clipPath><rect width=\"50\" height=\"50\" clip-path=\"url(#c)\"/></svg>"));
        // Shapes and texts: a PDF cannot have them in one path, and the shapes stay.
        content = Draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><text y=\"20\">Hi</text><rect width=\"5\" height=\"5\"/></clipPath><rect width=\"50\" height=\"50\" clip-path=\"url(#c)\"/></svg>");
        Assert.DoesNotContain(" Tr\n", content);
        Assert.Contains("h\nW\nn\n", content);
        // With no glyph it clips all away; out of the range of a PDF nothing is drawn.
        foreach (string text in new[] {"<text y=\"20\"> </text>", "<text y=\"20\" font-size=\"0\">Hi</text>", "<text y=\"20\" display=\"none\">Hi</text>", "<text y=\"20\" visibility=\"hidden\">Hi</text>", "<text y=\"20\"><tspan visibility=\"hidden\">Hi</tspan></text>", "<text y=\"20\" transform=\"scale(0)\">Hi</text>", "<text y=\"20\" transform=\"scale(1e30)\">Hi</text>"}) {
            Assert.Equal("", Draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\">" + text + "</clipPath><rect width=\"50\" height=\"50\" clip-path=\"url(#c)\"/></svg>"));
        }
        // In the fill of no color, too: a clip path takes the shape of the glyphs.
        Assert.Contains("7 Tr\n", Draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><text y=\"20\" fill=\"none\">Hi</text></clipPath><rect width=\"50\" height=\"50\" clip-path=\"url(#c)\"/></svg>"));
        // In the space of the image, and of its scale.
        SVGImage image = Parse("<svg width=\"200\" height=\"200\" viewBox=\"0 0 100 100\"><clipPath id=\"c\"><text x=\"10\" y=\"20\" font-size=\"12\">Hi</text></clipPath><rect width=\"50\" height=\"50\" clip-path=\"url(#c)\"/></svg>");
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        image.SetLocation(0f, 0f);
        image.DrawOn(page);
        Assert.Contains(" 12 Tf\n2 0 0 2 20 752 Tm\n", TestSupport.Content(page));
        image.ScaleBy(0.5f);
        image.SetLocation(10f, 0f);
        page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        image.DrawOn(page);
        Assert.Contains(" 12 Tf\n1 0 0 1 20 772 Tm\n", TestSupport.Content(page));
    }

    [Fact]
    public void TheShapesOfAClipPathGoTheSameWayRound() {
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><polygon points=\"0,0 20,0 20,20 0,20\"/><polygon clip-rule=\"evenodd\" points=\"10,10 10,30 30,30 30,10\"/></clipPath><rect width=\"5\" height=\"5\" clip-path=\"url(#c)\"/></svg>");
            Assert.Contains("0 792 m\n20 792 l\n20 772 l\n0 772 l\nh\n10 782 m\n30 782 l\n30 762 l\n10 762 l\n10 782 l\nW\nn\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><rect width=\"20\" height=\"20\"/><path d=\"M50 0 C50 10 60 20 70 20 L70 0 Z\"/></clipPath><rect width=\"5\" height=\"5\" clip-path=\"url(#c)\"/></svg>");
            Assert.Contains("50 792 m\n70 792 l\n70 772 l\n60 772 50 782 50 792 c\nW\nn\n", content);
        }
    }

    [Fact]
    public void TheAlphaOfAColorIsDrawn() {
        Assert.Equal(0.5f, Parse("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"rgba(255,0,0,0.5)\"/></svg>").paths[0].fillAlpha);
        Assert.Equal(0.25f, Parse("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"rgb(255 0 0 / 25%)\"/></svg>").paths[0].fillAlpha);
        Assert.Equal(0.25f, Parse("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"rgba(255,0,0,0.5)\" fill-opacity=\"0.5\"/></svg>").paths[0].fillAlpha);
        Assert.Equal(0.2509804f, Parse("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"#ff000040\"/></svg>").paths[0].fillAlpha);
        Assert.Equal(0.26666668f, Parse("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"#f004\"/></svg>").paths[0].fillAlpha);
        Assert.Equal(0.5019608f, Parse("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" stroke=\"#00ff0080\"/></svg>").paths[0].strokeAlpha);
        Assert.Equal(1.0f, Parse("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"red\"/></svg>").paths[0].fillAlpha);
        Assert.Equal(0.5f, Parse("<svg width=\"100\" height=\"100\"><g fill=\"rgba(0,0,0,0.5)\"><path d=\"M0 0 L10 0 L10 10 Z\"/></g></svg>").paths[0].fillAlpha);
        Assert.Equal(0.5f, Parse("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" color=\"rgba(0,0,255,0.5)\" fill=\"currentColor\"/></svg>").paths[0].fillAlpha);
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"rgba(0,0,0,0.5)\"/></svg>");
            Assert.StartsWith("q\n/GS1 gs\n0 0 0 rg\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"#ff000040\"/></svg>");
            Assert.StartsWith("q\n/GS1 gs\n1 0 0 rg\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"#f004\"/></svg>");
            Assert.StartsWith("q\n/GS1 gs\n1 0 0 rg\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"#ff000000\"/></svg>");
            Assert.Equal("", content);
        }
    }

    [Fact]
    public void HslColorsAreDrawn() {
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"hsl(120, 100%, 50%)\"/></svg>");
            Assert.StartsWith("0 1 0 rg\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"hsl(240deg 100% 50%)\"/></svg>");
            Assert.StartsWith("0 0 1 rg\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"HSL(-120, 100%, 50%)\"/></svg>");
            Assert.StartsWith("0 0 1 rg\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"hsl(0.5turn 100% 50%)\"/></svg>");
            Assert.StartsWith("0 1 1 rg\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"hsl(0, 0%, 100%)\"/></svg>");
            Assert.StartsWith("1 1 1 rg\n", content);
        }
        Assert.Equal(0.5f, Parse("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"hsla(0, 100%, 50%, 0.5)\"/></svg>").paths[0].fillAlpha);
        Assert.Equal(0.25f, Parse("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"hsl(0 100% 50% / 25%)\"/></svg>").paths[0].fillAlpha);
    }

    [Fact]
    public void TheKeywordsOfAPaintAreInAnyCase() {
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" color=\"red\" fill=\"currentcolor\"/></svg>");
            Assert.StartsWith("1 0 0 rg\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" color=\"red\" fill=\"CURRENTCOLOR\"/></svg>");
            Assert.StartsWith("1 0 0 rg\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"NONE\"/></svg>");
            Assert.Equal("", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"None\"/></svg>");
            Assert.Equal("", content);
        }
    }

    [Fact]
    public void WhatIsNotVisibleIsNotDrawn() {
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><g visibility=\"hidden\"><rect width=\"5\" height=\"5\"/></g></svg>");
            Assert.Equal("", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><rect width=\"5\" height=\"5\" style=\"visibility:collapse\"/></svg>");
            Assert.Equal("", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><g visibility=\"hidden\"><rect width=\"5\" height=\"5\" fill=\"red\"/><rect width=\"5\" height=\"5\" fill=\"blue\" visibility=\"visible\"/></g></svg>");
            Assert.StartsWith("0 0 1 rg\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><text visibility=\"hidden\" x=\"1\" y=\"50\">a</text></svg>");
            Assert.Equal("", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><text x=\"1\" y=\"9\">a<tspan visibility=\"hidden\">b</tspan>c</text></svg>");
            Assert.Equal(2, content.Split(new string[] {"BT\n"}, StringSplitOptions.None).Length - 1);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><defs><rect id=\"r\" width=\"5\" height=\"5\"/></defs><use href=\"#r\" visibility=\"hidden\"/></svg>");
            Assert.Equal("", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><rect width=\"5\" height=\"5\" visibility=\"hidden\"/></clipPath><rect width=\"5\" height=\"5\" clip-path=\"url(#c)\"/></svg>");
            Assert.Equal("", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><marker id=\"m\"><rect width=\"5\" height=\"5\"/></marker><path d=\"M0 0 L1 1\" stroke=\"red\" visibility=\"hidden\" marker-end=\"url(#m)\"/></svg>");
            Assert.Equal("", content);
        }
    }

    [Fact]
    public void ASwitchDrawsTheFirstChildItCan() {
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><switch><rect width=\"5\" height=\"5\" fill=\"red\"/><rect width=\"5\" height=\"5\" fill=\"blue\"/></switch></svg>");
            Assert.Equal("1 0 0 rg\n0 792 m\n5 792 l\n5 787 l\n0 787 l\nf\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><switch><title>a</title><foreignObject width=\"5\" height=\"5\"><rect width=\"5\" height=\"5\" fill=\"blue\"/></foreignObject><g><rect width=\"5\" height=\"5\" fill=\"red\"/></g><rect width=\"5\" height=\"5\" fill=\"blue\"/></switch></svg>");
            Assert.StartsWith("1 0 0 rg\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><switch><rect width=\"5\" height=\"5\" requiredExtensions=\"http://a\" fill=\"blue\"/><rect width=\"5\" height=\"5\" fill=\"red\"/><rect width=\"5\" height=\"5\" fill=\"blue\"/></switch></svg>");
            Assert.Equal(0, content.Split(new string[] {"0 0 1 rg"}, StringSplitOptions.None).Length - 1);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><switch><rect width=\"5\" height=\"5\" requiredExtensions=\"http://a\" fill=\"blue\"/><rect width=\"5\" height=\"5\" fill=\"red\"/><rect width=\"5\" height=\"5\" fill=\"blue\"/></switch></svg>");
            Assert.Equal(1, content.Split(new string[] {"1 0 0 rg"}, StringSplitOptions.None).Length - 1);
        }
    }

    private static string DrawInLanguage(string svg, string language) {
        PDF pdf = TestSupport.NewPDF();
        if (language.Length > 0) {
            pdf.SetLanguage(language);
        }
        Page page = new Page(pdf, Letter.PORTRAIT);
        SVGImage image = Parse(svg);
        image.SetLocation(0f, 0f);
        image.DrawOn(page);
        return TestSupport.Content(page);
    }

    [Fact]
    public void ASwitchHonorsTheSystemLanguage() {
        string svg = "<svg width=\"100\" height=\"100\"><switch><rect width=\"5\" height=\"5\" systemLanguage=\"de, fr-CA\" fill=\"red\"/><rect width=\"5\" height=\"5\" systemLanguage=\"\" fill=\"lime\"/><g systemLanguage=\"EN-gb\"><rect width=\"5\" height=\"5\" fill=\"blue\"/></g><rect width=\"5\" height=\"5\" fill=\"black\"/></switch></svg>";
        string[][] tests = {new[] {"", "0 0 1 rg"}, new[] {"en-US", "0 0 1 rg"}, new[] {"en", "0 0 1 rg"}, new[] {"de", "1 0 0 rg"}, new[] {"de-AT", "1 0 0 rg"}, new[] {"FR", "1 0 0 rg"}, new[] {"bg", "0 0 0 rg"}, new[] {"fra", "0 0 0 rg"}};
        foreach (string[] test in tests) {
            string drawn = DrawInLanguage(svg, test[0]);
            Assert.Contains(test[1], drawn);
            Assert.Equal(1, drawn.Split(new[] {" rg\n"}, StringSplitOptions.None).Length - 1);
        }
        // No child of the language, and none without a systemLanguage: nothing.
        Assert.Equal("", DrawInLanguage("<svg width=\"100\" height=\"100\"><switch><rect width=\"5\" height=\"5\" systemLanguage=\"de\"/></switch></svg>", ""));
        // One image, drawn in the language of each PDF.
        SVGImage image = Parse(svg);
        string[][] languages = {new[] {"de", "1 0 0 rg"}, new[] {"en", "0 0 1 rg"}};
        foreach (string[] test in languages) {
            PDF pdf = TestSupport.NewPDF();
            pdf.SetLanguage(test[0]);
            Page page = new Page(pdf, Letter.PORTRAIT);
            image.DrawOn(page);
            Assert.Contains(test[1], TestSupport.Content(page));
        }
        // Texts, in a switch in a used group, nested, and a marker in a child.
        string content = DrawInLanguage("<svg width=\"100\" height=\"100\"><defs><g id=\"g\"><switch><text systemLanguage=\"de\" y=\"10\" fill=\"red\">a</text><switch><text systemLanguage=\"en\" y=\"10\" fill=\"blue\">b</text></switch></switch></g></defs><use href=\"#g\"/></svg>", "en");
        Assert.Contains("0 0 1 rg", content);
        Assert.DoesNotContain("1 0 0 rg", content);
        Assert.Equal("", DrawInLanguage("<svg width=\"100\" height=\"100\"><marker id=\"m\"><rect width=\"5\" height=\"5\" fill=\"red\"/></marker><switch><path systemLanguage=\"de\" d=\"M0 0 L9 9\" stroke=\"blue\" marker-end=\"url(#m)\"/></switch></svg>", "en"));
        // What is not drawn leaves no cut, and no miter limit, for itself.
        content = DrawInLanguage("<svg width=\"100\" height=\"100\"><switch><path systemLanguage=\"de\" d=\"M0 0 L900 900\" stroke=\"blue\"/><rect width=\"5\" height=\"5\"/></switch></svg>", "en");
        Assert.DoesNotContain("W\n", content);
        Assert.DoesNotContain(" M\n", content);
    }

    [Fact]
    public void AUseGivesASymbolItsSize() {
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><symbol id=\"s\" viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></symbol><use href=\"#s\" x=\"5\" y=\"5\" width=\"20\" height=\"20\"/></svg>");
            Assert.Contains("5 787 m\n25 787 l\n25 767 l\n5 767 l\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><symbol id=\"s\" viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></symbol><use href=\"#s\" width=\"20\" height=\"40\"/></svg>");
            Assert.Contains("0 782 m\n20 782 l\n20 762 l\n0 762 l\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><symbol id=\"s\" viewBox=\"0 0 10 10\" preserveAspectRatio=\"xMinYMin\"><rect width=\"10\" height=\"10\"/></symbol><use href=\"#s\" width=\"20\" height=\"40\"/></svg>");
            Assert.Contains("0 792 m\n20 792 l\n20 772 l\n0 772 l\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><symbol id=\"s\" viewBox=\"0 0 10 10\" preserveAspectRatio=\"none\"><rect width=\"10\" height=\"10\"/></symbol><use href=\"#s\" width=\"20\" height=\"40\"/></svg>");
            Assert.Contains("0 792 m\n20 792 l\n20 752 l\n0 752 l\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><symbol id=\"s\" viewBox=\"0 0 10 10\" preserveAspectRatio=\"xMaxYMax slice\"><rect width=\"10\" height=\"10\"/></symbol><use href=\"#s\" width=\"20\" height=\"40\"/></svg>");
            Assert.Contains("-20 792 m\n20 792 l\n20 752 l\n-20 752 l\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><symbol id=\"s\" viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></symbol><use href=\"#s\"/></svg>");
            Assert.Contains("0 792 m\n100 792 l\n100 692 l\n0 692 l\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><symbol id=\"s\" viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></symbol><use href=\"#s\" width=\"0\" height=\"20\"/></svg>");
            Assert.Equal("", content);
        }
    }

    [Fact]
    public void ANestedSvgIsAViewport() {
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><svg x=\"10\" y=\"20\" width=\"20\" height=\"20\" viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></svg></svg>");
            Assert.Contains("10 772 m\n30 772 l\n30 752 l\n10 752 l\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><svg x=\"10\" y=\"20\"><rect width=\"10\" height=\"10\"/></svg></svg>");
            Assert.Contains("10 772 m\n20 772 l\n20 762 l\n10 762 l\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><svg x=\"50%\" y=\"10%\" width=\"20\" height=\"20\" viewBox=\"5 5 10 10\"><rect x=\"5\" y=\"5\" width=\"10\" height=\"10\"/></svg></svg>");
            Assert.Contains("50 782 m\n70 782 l\n70 762 l\n50 762 l\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\" viewBox=\"0 0 200 200\"><svg width=\"50%\" height=\"50%\" viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></svg></svg>");
            Assert.Contains("0 792 m\n50 792 l\n50 742 l\n0 742 l\n", content);
        }
        {
            string content = Draw("<svg><svg viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></svg></svg>");
            Assert.Contains("0 792 m\n10 792 l\n10 782 l\n0 782 l\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><svg width=\"0\" viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></svg></svg>");
            Assert.Equal("", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><svg viewBox=\"0 0 -10 10\"><rect width=\"10\" height=\"10\"/></svg></svg>");
            Assert.Equal("", content);
        }
    }

    [Fact]
    public void ASymbolAndANestedSvgClipToTheirViewports() {
        const string clip = "h\nW\nn\n";
        Assert.Equal("q\n5 787 m\n25 787 l\n25 767 l\n5 767 l\nh\nW\nn\n0 0 0 rg\n5 787 m\n45 787 l\n45 747 l\n5 747 l\nf\nQ\n", Draw("<svg width=\"100\" height=\"100\"><symbol id=\"s\" viewBox=\"0 0 10 10\"><rect width=\"20\" height=\"20\"/></symbol><use href=\"#s\" x=\"5\" y=\"5\" width=\"20\" height=\"20\"/></svg>"));
        Assert.StartsWith("q\n10 772 m\n30 772 l\n30 752 l\n10 752 l\n" + clip, Draw("<svg width=\"100\" height=\"100\"><svg x=\"10\" y=\"20\" width=\"20\" height=\"20\"><rect width=\"50\" height=\"50\"/></svg></svg>"));
        // The viewport, not the viewBox, and in the space of the image.
        Assert.Contains("q\n0 792 m\n20 792 l\n20 752 l\n0 752 l\n" + clip, Draw("<svg width=\"100\" height=\"100\"><svg width=\"20\" height=\"40\" viewBox=\"0 0 10 10\" preserveAspectRatio=\"xMaxYMax slice\"><rect width=\"10\" height=\"10\"/></svg></svg>"));
        Assert.StartsWith("q\n0 792 m\n20 792 l\n20 772 l\n0 772 l\n" + clip, Draw("<svg width=\"200\" height=\"200\" viewBox=\"0 0 100 100\"><svg width=\"10\" height=\"10\"><rect width=\"5\" height=\"5\"/></svg></svg>"));
        // Moved and scaled by the scale of the image, too.
        SVGImage image = Parse("<svg width=\"100\" height=\"100\"><svg width=\"10\" height=\"10\"><rect width=\"5\" height=\"5\"/></svg></svg>");
        image.ScaleBy(2f);
        image.SetLocation(10f, 0f);
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        image.DrawOn(page);
        Assert.StartsWith("q\n10 792 m\n30 792 l\n30 772 l\n10 772 l\n" + clip, TestSupport.Content(page));
        // Each viewport clips, and an overflow is not inherited.
        Assert.Equal(3, Draw("<svg width=\"100\" height=\"100\"><svg width=\"50\" height=\"50\"><svg width=\"20\" height=\"20\"><rect width=\"5\" height=\"5\"/></svg></svg></svg>").Split(new[] {clip}, StringSplitOptions.None).Length);
        Assert.Equal(2, Draw("<svg width=\"100\" height=\"100\"><svg width=\"50\" height=\"50\" overflow=\"visible\"><svg width=\"20\" height=\"20\"><rect width=\"5\" height=\"5\"/></svg></svg></svg>").Split(new[] {clip}, StringSplitOptions.None).Length);
        // Overflow, as an attribute, in a style and in rules; hidden and scroll clip.
        foreach (string overflow in new[] {"overflow=\"visible\"", "overflow=\"auto\"", "overflow=\"VISIBLE\"", "style=\"overflow: visible\"", "class=\"o\""}) {
            Assert.DoesNotContain(clip, Draw("<svg width=\"100\" height=\"100\"><style>.o { overflow: visible }</style><symbol id=\"s\" " + overflow + "><rect width=\"500\" height=\"5\"/></symbol><use href=\"#s\"/></svg>"));
        }
        foreach (string overflow in new[] {"overflow=\"hidden\"", "overflow=\"scroll\""}) {
            Assert.Contains(clip, Draw("<svg width=\"100\" height=\"100\"><svg width=\"5\" height=\"5\" " + overflow + "><rect width=\"5\" height=\"5\"/></svg></svg>"));
        }
        // A viewport of no known size clips nothing, and one of no size hides.
        Assert.DoesNotContain("W\n", Draw("<svg><svg x=\"1\"><rect width=\"5\" height=\"5\"/></svg></svg>"));
        Assert.Equal("", Draw("<svg width=\"100\" height=\"100\"><svg width=\"0\"><rect width=\"5\" height=\"5\"/></svg></svg>"));
    }

    [Fact]
    public void ThePaintOrderOfAShapeIsDrawn() {
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"red\" stroke=\"blue\" paint-order=\"stroke\"/></svg>");
            Assert.Contains("S\n1 0 0 rg\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"red\" stroke=\"blue\" style=\"paint-order: stroke fill markers\"/></svg>");
            Assert.Contains("S\n1 0 0 rg\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"red\" stroke=\"blue\" /></svg>");
            Assert.Contains("f\n0 0 1 RG\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"red\" stroke=\"blue\" paint-order=\"fill stroke\"/></svg>");
            Assert.Contains("f\n0 0 1 RG\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"red\" stroke=\"blue\" paint-order=\"normal\"/></svg>");
            Assert.Contains("f\n0 0 1 RG\n", content);
        }
    }

    [Fact]
    public void TheMiterLimitOfAStrokeIsWritten() {
        {
            string content = DrawAsItIs("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"none\" stroke=\"red\" /></svg>");
            Assert.StartsWith("q\n4 M\n1 0 0 RG\n", content);
        }
        {
            string content = DrawAsItIs("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"none\" stroke=\"red\" stroke-miterlimit=\"2.5\"/></svg>");
            Assert.StartsWith("q\n2.5 M\n", content);
        }
        {
            string content = DrawAsItIs("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"none\" stroke=\"red\" style=\"stroke-miterlimit:1\"/></svg>");
            Assert.StartsWith("q\n1 M\n", content);
        }
        {
            string content = DrawAsItIs("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"none\" stroke=\"red\" stroke-miterlimit=\"10\"/></svg>");
            Assert.StartsWith("q\n10 M\n", content);
        }
        {
            string content = DrawAsItIs("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"none\" stroke=\"red\" stroke-miterlimit=\"0.5\"/></svg>");
            Assert.StartsWith("q\n4 M\n1 0 0 RG\n", content);
        }
        {
            string content = DrawAsItIs("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"none\" stroke=\"red\" stroke-miterlimit=\"1e30\"/></svg>");
            Assert.StartsWith("q\n4 M\n1 0 0 RG\n", content);
        }
        {
            string content = DrawAsItIs("<svg width=\"100\" height=\"100\"><g fill=\"none\" stroke=\"red\"><path d=\"M0 0 L10 0 L10 10 Z\"/><path d=\"M0 0 L9 0 L9 9 Z\"/></g></svg>");
            Assert.Equal(1, content.Split(new string[] {" M\n"}, StringSplitOptions.None).Length - 1);
        }
        {
            string content = DrawAsItIs("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"none\" stroke=\"red\" stroke-linejoin=\"round\"/></svg>");
            Assert.DoesNotContain(" M\n", content);
        }
        {
            string content = DrawAsItIs("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\"/></svg>");
            Assert.DoesNotContain(" M\n", content);
        }
        {
            string content = DrawAsItIs("<svg width=\"100\" height=\"100\"><text x=\"1\" y=\"50\" stroke=\"red\">a</text></svg>");
            Assert.StartsWith("q\n4 M\n", content);
        }
        {
            string content = DrawAsItIs("<svg width=\"100\" height=\"100\"><text x=\"1\" y=\"50\" stroke=\"red\" stroke-miterlimit=\"3\">a</text></svg>");
            Assert.Contains("3 M\n", content);
        }
        {
            string content = DrawAsItIs("<svg width=\"100\" height=\"100\"><text x=\"1\" y=\"50\">a</text></svg>");
            Assert.DoesNotContain(" M\n", content);
        }
    }

    [Fact]
    public void ADashOffsetBelowZeroIsWrittenInsideThePattern() {
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 10 L100 10\" stroke=\"red\" stroke-dasharray=\"4 2\" stroke-dashoffset=\"-1\"/></svg>");
            Assert.Contains("[4 2] 5 d\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 10 L100 10\" stroke=\"red\" stroke-dasharray=\"4 2\" stroke-dashoffset=\"-7\"/></svg>");
            Assert.Contains("[4 2] 5 d\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 10 L100 10\" stroke=\"red\" stroke-dasharray=\"4 2\" stroke-dashoffset=\"-6\"/></svg>");
            Assert.Contains("[4 2] 0 d\n", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 10 L100 10\" stroke=\"red\" stroke-dasharray=\"4 2\" stroke-dashoffset=\"7\"/></svg>");
            Assert.Contains("[4 2] 7 d\n", content);
        }
    }

    [Fact]
    public void HugeDashesAndClipPathsSkipThePath() {
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 10 L100 10\" stroke=\"red\" stroke-dasharray=\"1e30 1\"/></svg>");
            Assert.Equal("", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 10 L100 10\" stroke=\"red\" stroke-dasharray=\"4 2\" stroke-dashoffset=\"1e30\"/></svg>");
            Assert.Equal("", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 10 L100 10\" stroke=\"red\" stroke-dasharray=\"1e30 1e30\" stroke-dashoffset=\"-1\"/></svg>");
            Assert.Equal("", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><rect width=\"1e30\" height=\"5\"/></clipPath><rect width=\"5\" height=\"5\" clip-path=\"url(#c)\"/></svg>");
            Assert.Equal("", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><rect width=\"1e30\" height=\"5\"/></clipPath><text x=\"1\" y=\"50\" clip-path=\"url(#c)\">a</text></svg>");
            Assert.Equal("", content);
        }
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 10 L100 10 L0 20\" stroke-dasharray=\"1e30 1\"/></svg>");
            Assert.StartsWith("0 0 0 rg\n", content);
        }
    }

    [Fact]
    public void ASizeInAUnitWithoutAViewBoxIsInPixels() {
        {
            string content = Draw("<svg width=\"75pt\" height=\"75pt\"><rect width=\"100\" height=\"100\"/></svg>");
            Assert.Equal("0 0 0 rg\n0 792 m\n75 792 l\n75 717 l\n0 717 l\nf\n", content);
        }
        {
            string content = Draw("<svg width=\"1in\" height=\"1in\"><rect width=\"96\" height=\"96\"/></svg>");
            Assert.Equal("0 0 0 rg\n0 792 m\n72 792 l\n72 720 l\n0 720 l\nf\n", content);
        }
        {
            string content = Draw("<svg width=\"10mm\" height=\"10mm\"><rect width=\"37.79\" height=\"37.79\"/></svg>");
            Assert.DoesNotContain("W\n", content);
        }
        Assert.Equal(75f, Parse("<svg width=\"75pt\" height=\"75pt\"><rect width=\"100\" height=\"100\"/></svg>").GetWidth());
        Assert.Equal(75f, Parse("<svg width=\"75pt\" height=\"75pt\"><rect width=\"100\" height=\"100\"/></svg>").GetHeight());
        {
            string content = Draw("<svg width=\"100px\" height=\"100\"><rect width=\"100\" height=\"100\"/></svg>");
            Assert.Equal("0 0 0 rg\n0 792 m\n100 792 l\n100 692 l\n0 692 l\nf\n", content);
        }
        {
            string content = Draw("<svg width=\"75pt\" height=\"75pt\" viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></svg>");
            Assert.Equal("0 0 0 rg\n0 792 m\n75 792 l\n75 717 l\n0 717 l\nf\n", content);
        }
        {
            string content = Draw("<svg width=\"75pt\" height=\"75pt\"><rect width=\"10\" height=\"10\" fill=\"none\" stroke=\"red\" stroke-width=\"4\"/></svg>");
            Assert.Contains("3 w\n", content);
        }
    }

    [Fact]
    public void AViewBoxOfNegativeSizeIsRefused() {
        Assert.Contains("have to be above zero", Assert.ThrowsAny<Exception>(() => Parse("<svg width=\"10\" height=\"10\" viewBox=\"0 0 -10 10\"/>")).Message);
        Assert.Contains("have to be above zero", Assert.ThrowsAny<Exception>(() => Parse("<svg width=\"10\" height=\"10\" viewBox=\"0 0 10 -10\"/>")).Message);
    }

    [Fact]
    public void AHueTooLargeForItsUnitIsNotAColor() {
        {
            string content = Draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"hsl(1e308turn, 100%, 50%)\"/></svg>");
            Assert.StartsWith("0 0 0 rg\n", content);
        }
    }

    [Fact]
    public void AMarkerClipsToItsViewport() {
        const string head = "<svg width=\"100\" height=\"100\"><style>.o { overflow: visible }</style><marker id=\"m\" ";
        const string tail = "><rect width=\"20\" height=\"20\"/></marker><path d=\"M10 10 L20 20\" fill=\"none\" marker-end=\"url(#m)\"/></svg>";
        foreach (string[] test in new string[][] {
            new string[] {"markerWidth=\"4\" markerHeight=\"4\" markerUnits=\"userSpaceOnUse\"", "q\n20 772 m\n24 772 l\n24 768 l\n20 768 l\nh\nW\nn\n"},
            // Its size is 3 unless given, in units of the stroke width.
            new string[] {"", "q\n20 772 m\n23 772 l\n23 769 l\n20 769 l\nh\nW\nn\n"},
            // The reference point goes to the vertex, and the viewport with it.
            new string[] {"markerWidth=\"4\" markerHeight=\"4\" refX=\"1\" refY=\"2\"", "q\n19 774 m\n23 774 l\n23 770 l\n19 770 l\nh\nW\nn\n"},
            // A viewBox, which may start anywhere, fitted to the viewport.
            new string[] {"markerWidth=\"5\" markerHeight=\"5\" viewBox=\"0 0 10 10\"", "q\n20 772 m\n25 772 l\n25 767 l\n20 767 l\nh\nW\nn\n"},
            new string[] {"markerWidth=\"6\" markerHeight=\"6\" viewBox=\"0 -5 10 10\" refX=\"5\"", "q\n17 775 m\n23 775 l\n23 769 l\n17 769 l\nh\nW\nn\n"},
            // The viewport, not the viewBox: the aspect moves one in the other.
            new string[] {"markerWidth=\"20\" markerHeight=\"10\" viewBox=\"0 0 10 10\"", "q\n15 772 m\n35 772 l\n35 762 l\n15 762 l\nh\nW\nn\n"},
            new string[] {"markerWidth=\"20\" markerHeight=\"10\" viewBox=\"0 0 10 10\" preserveAspectRatio=\"xMinYMin\"", "q\n20 772 m\n40 772 l\n40 762 l\n20 762 l\nh\nW\nn\n"},
            new string[] {"markerWidth=\"20\" markerHeight=\"10\" viewBox=\"0 0 10 10\" preserveAspectRatio=\"xMaxYMax\"", "q\n10 772 m\n30 772 l\n30 762 l\n10 762 l\nh\nW\nn\n"},
            new string[] {"markerWidth=\"20\" markerHeight=\"10\" viewBox=\"0 0 10 10\" preserveAspectRatio=\"none\"", "q\n20 772 m\n40 772 l\n40 762 l\n20 762 l\nh\nW\nn\n"},
            new string[] {"markerWidth=\"20\" markerHeight=\"10\" viewBox=\"0 0 10 10\" preserveAspectRatio=\"xMidYMid slice\"", "q\n20 767 m\n40 767 l\n40 757 l\n20 757 l\nh\nW\nn\n"},
            // Hidden and scroll clip, in an attribute and a style.
            new string[] {"markerWidth=\"4\" markerHeight=\"4\" overflow=\"hidden\"", "q\n20 772 m\n24 772 l\n24 768 l\n20 768 l\nh\nW\nn\n"},
            new string[] {"markerWidth=\"4\" markerHeight=\"4\" style=\"overflow: scroll\"", "q\n20 772 m\n24 772 l\n24 768 l\n20 768 l\nh\nW\nn\n"},
        }) {
            Assert.StartsWith(test[1], Draw(head + test[0] + tail));
        }
        foreach (string attributes in new string[] {"overflow=\"visible\"", "overflow=\"auto\"", "style=\"overflow: visible\"", "class=\"o\""}) {
            string content = Draw(head + attributes + tail);
            Assert.True(!content.Contains("W\n") && content.Contains("f\n"), attributes + " " + content);
        }
        // Turned with the path, and the rectangle with it.
        Assert.StartsWith("q\n20 772 m\n20 768 l\n18 768 l\n18 772 l\nh\nW\nn\n", Draw("<svg width=\"100\" height=\"100\"><marker id=\"m\" markerWidth=\"4\" markerHeight=\"2\" orient=\"auto\"><rect width=\"20\" height=\"20\"/></marker><path d=\"M20 10 L20 20\" fill=\"none\" marker-end=\"url(#m)\"/></svg>"));
        // The clip is on the marker, what is in it stays in it, and each is clipped.
        Assert.Equal(4, Draw("<svg width=\"100\" height=\"100\"><marker id=\"m\" markerWidth=\"4\" markerHeight=\"4\"><rect width=\"20\" height=\"20\"/><rect width=\"30\" height=\"30\"/></marker><path d=\"M10 10 L20 20 L30 20\" fill=\"none\" marker-mid=\"url(#m)\" marker-end=\"url(#m)\"/></svg>").Split("h\nW\nn\n").Length - 1);
        // A size of nothing draws nothing, with a viewBox or without.
        foreach (string attributes in new string[] {"markerWidth=\"0\"", "markerHeight=\"0\"", "markerWidth=\"0\" overflow=\"visible\"", "markerHeight=\"0\" viewBox=\"0 0 10 10\""}) {
            Assert.Equal("", Draw(head + attributes + tail));
        }
    }

    [Fact]
    public void AnOverflowOfInheritTakesThatOfTheParent() {
        foreach (string[] test in new string[][] {
            new[] {"<g overflow=\"visible\"><svg width=\"20\" height=\"20\" overflow=\"inherit\"><rect width=\"50\" height=\"50\"/></svg></g>", "0"},
            new[] {"<g overflow=\"auto\"><svg width=\"20\" height=\"20\" overflow=\"inherit\"><rect width=\"50\" height=\"50\"/></svg></g>", "0"},
            new[] {"<g overflow=\"hidden\"><svg width=\"20\" height=\"20\" overflow=\"inherit\"><rect width=\"50\" height=\"50\"/></svg></g>", "1"},
            new[] {"<g overflow=\"scroll\"><svg width=\"20\" height=\"20\" overflow=\"inherit\"><rect width=\"50\" height=\"50\"/></svg></g>", "1"},
            // A group's own overflow is visible, unless given.
            new[] {"<g><svg width=\"20\" height=\"20\" overflow=\"inherit\"><rect width=\"50\" height=\"50\"/></svg></g>", "0"},
            new[] {"<svg width=\"20\" height=\"20\" overflow=\"inherit\"><rect width=\"50\" height=\"50\"/></svg>", "0"},
            // That of a nested svg is hidden, which clips it too, unless given.
            new[] {"<svg width=\"50\" height=\"50\"><svg width=\"20\" height=\"20\" overflow=\"inherit\"><rect width=\"50\" height=\"50\"/></svg></svg>", "2"},
            new[] {"<svg width=\"50\" height=\"50\" overflow=\"visible\"><svg width=\"20\" height=\"20\" overflow=\"inherit\"><rect width=\"50\" height=\"50\"/></svg></svg>", "0"},
            new[] {"<svg width=\"50\" height=\"50\" overflow=\"auto\"><svg width=\"20\" height=\"20\" overflow=\"inherit\"><rect width=\"50\" height=\"50\"/></svg></svg>", "0"},
            // Up the chain, which inherit passes on.
            new[] {"<g overflow=\"visible\"><g overflow=\"inherit\"><svg width=\"20\" height=\"20\" overflow=\"inherit\"><rect width=\"50\" height=\"50\"/></svg></g></g>", "0"},
            new[] {"<g overflow=\"hidden\"><g overflow=\"inherit\"><svg width=\"20\" height=\"20\" overflow=\"inherit\"><rect width=\"50\" height=\"50\"/></svg></g></g>", "1"},
            new[] {"<g overflow=\"visible\"><g><svg width=\"20\" height=\"20\" overflow=\"inherit\"><rect width=\"50\" height=\"50\"/></svg></g></g>", "0"},
            new[] {"<g overflow=\"hidden\"><g><svg width=\"20\" height=\"20\" overflow=\"inherit\"><rect width=\"50\" height=\"50\"/></svg></g></g>", "0"},
            // In a style and in rules; the last of them counts.
            new[] {"<g overflow=\"visible\"><svg width=\"20\" height=\"20\" style=\"overflow: inherit\"><rect width=\"50\" height=\"50\"/></svg></g>", "0"},
            new[] {"<style>.o { overflow: inherit }</style><g overflow=\"visible\"><svg width=\"20\" height=\"20\" class=\"o\"><rect width=\"50\" height=\"50\"/></svg></g>", "0"},
            new[] {"<g overflow=\"visible\"><svg width=\"20\" height=\"20\" overflow=\"INHERIT\"><rect width=\"50\" height=\"50\"/></svg></g>", "0"},
            new[] {"<g overflow=\"visible\"><svg width=\"20\" height=\"20\" overflow=\"inherit\" style=\"overflow: hidden\"><rect width=\"50\" height=\"50\"/></svg></g>", "1"},
            new[] {"<g overflow=\"visible\"><svg width=\"20\" height=\"20\" overflow=\"hidden\" style=\"overflow: inherit\"><rect width=\"50\" height=\"50\"/></svg></g>", "0"},
            // A symbol takes it from the use element.
            new[] {"<symbol id=\"s\" overflow=\"inherit\"><rect width=\"500\" height=\"5\"/></symbol><use href=\"#s\" overflow=\"visible\"/>", "0"},
            new[] {"<symbol id=\"s\" overflow=\"inherit\"><rect width=\"500\" height=\"5\"/></symbol><use href=\"#s\" overflow=\"hidden\"/>", "1"},
            new[] {"<symbol id=\"s\" overflow=\"inherit\"><rect width=\"500\" height=\"5\"/></symbol><use href=\"#s\"/>", "0"},
            // A marker takes it from the base it is drawn on: visible.
            new[] {"<marker id=\"m\" overflow=\"inherit\"><rect width=\"20\" height=\"20\"/></marker><path d=\"M0 0 L9 9\" fill=\"none\" marker-end=\"url(#m)\"/>", "0"},
        }) {
            string content = Draw("<svg width=\"100\" height=\"100\">" + test[0] + "</svg>");
            Assert.True(int.Parse(test[1]) == content.Split("h\nW\nn\n").Length - 1, test[0] + " " + content);
            Assert.Contains("f\n", content);
        }
    }
}
}
