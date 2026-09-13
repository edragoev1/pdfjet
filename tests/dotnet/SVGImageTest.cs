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
}
}
