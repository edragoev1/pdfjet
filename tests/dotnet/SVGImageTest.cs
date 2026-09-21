/*
 * SVGImageTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
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
}
}
