/**
 * ComplexTextTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace PDFjet.NET {
// ComplexText: a shaper wraps and shapes the text, and the glyphs it found
// are drawn with DrawGlyphs, each line tagged with its text. .NET has no
// shaper of its own: the tests give one that returns fixed lines.
public class ComplexTextTest {
    private const string DEVANAGARI = "examples/fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Regular.otf";
    private const string LATIN = "resources/fonts/IBMPlexSans/IBMPlexSans-Regular.otf";
    private const string CASCADIA = "tests/fonts/CascadiaMono-Regular.ttf";

    private static int Count(string text, string part) {
        int n = 0;
        for (int i = text.IndexOf(part, StringComparison.Ordinal); i >= 0;
                i = text.IndexOf(part, i + 1, StringComparison.Ordinal)) {
            n++;
        }
        return n;
    }

    // A shaper that gives the lines it was made with, and keeps what it was given
    private class FixedShaper : IShaper {
        internal readonly List<ShapedLine> lines;
        internal byte[][] files;
        internal float size;
        internal float width;
        internal string language;

        internal FixedShaper(params ShapedLine[] lines) {
            this.lines = new List<ShapedLine>(lines);
        }

        public List<ShapedLine> Shape(ShapingRequest request) {
            this.files = request.FontFiles;
            this.size = request.FontSize;
            this.width = request.Width;
            this.language = request.Language;
            return lines;
        }
    }

    [Fact]
    public void TheShaperGivesTheGlyphsAndTheirPlaces() {
        PDF pdf = new PDF(new MemoryStream());
        Font font = new Font(pdf, LATIN).SetSize(10f);
        Font fallback = new Font(pdf, CASCADIA);
        int a = font.unicodeToGID.Get('A');
        int b = fallback.unicodeToGID.Get('B');
        FixedShaper shaper = new FixedShaper(
                new ShapedLine(0, 2, new int[] {0, 1}, new int[] {a, b}, new float[] {0f, 0f, 7f, 0f}, 13f),
                // A glyph of a font that was not given is not drawn
                new ShapedLine(2, 3, new int[] {-1}, new int[] {5}, new float[] {0f, 0f}, 6f));
        Page page = new Page(pdf, Letter.PORTRAIT);
        float[] corner = new ComplexText(font, "ABC")
                .SetFallbackFont(fallback)
                .SetLanguage("en")
                .SetShaper(shaper)
                .SetLocation(50f, 100f)
                .SetWidth(120f)
                .DrawOn(page);
        Assert.Equal(2, shaper.files.Length);
        Assert.Equal(10f, shaper.size);
        Assert.Equal(120f, shaper.width);
        Assert.Equal("en", shaper.language);

        float lineHeight = font.GetAscent() + font.GetDescent() + font.GetLineGap(10f);
        Assert.Equal(100f + 2 * lineHeight, corner[1], 3);
        // The fallback's glyph at the font's size, 10 points, on the first line's baseline
        float y = 792f - (100f + font.GetAscent());
        string content = TestSupport.Content(page);
        List<float[]> places = new List<float[]>();
        foreach (Match m in Regex.Matches(content, @"q (\S+) 0 0 \S+ (\S+) (\S+) cm /Fm")) {
            places.Add(new float[] {
                float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
                float.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture)});
        }
        Assert.Equal(2, places.Count);
        Assert.True(Math.Abs(places[0][0] - 0.01f) < 1e-6f);            // Plex, 1,000 units an em
        Assert.True(Math.Abs(places[1][0] - 10f / 2048f) < 1e-6f);      // Cascadia, 2,048
        Assert.Equal(50f, places[0][1]);
        Assert.Equal(57f, places[1][1]);
        Assert.True(Math.Abs(places[0][2] - y) < 0.006f && Math.Abs(places[1][2] - y) < 0.006f);
        pdf.Complete();
    }

    [Fact]
    public void ATaggedTextIsAParagraphOfLinesWithTheirText() {
        PDF pdf = new PDF(new MemoryStream(), Compliance.PDF_UA_1);
        pdf.SetTitle("Title");
        Font font = new Font(pdf, LATIN);
        int a = font.unicodeToGID.Get('A');
        int b = font.unicodeToGID.Get('B');
        FixedShaper shaper = new FixedShaper(
                new ShapedLine(0, 2, new int[] {0, 0}, new int[] {a, a}, new float[] {0f, 0f, 7f, 0f}, 13f),
                new ShapedLine(2, 4, new int[] {0, 0}, new int[] {b, b}, new float[] {0f, 0f, 7f, 0f}, 13f));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new ComplexText(font, "AABB").SetLanguage("hi").SetShaper(shaper).SetLocation(50f, 100f).DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Equal(1, Count(content, "/P <</MCID 0>>\nBDC\n"));
        Assert.Contains("/Span <</Lang <6869> /ActualText <" + Page.ToUTF16Hex("AA") + ">>> BDC\n", content);
        Assert.Contains("/Span <</Lang <6869> /ActualText <" + Page.ToUTF16Hex("BB") + ">>> BDC\n", content);
        Assert.DoesNotContain("BT", content);
        pdf.Complete();
    }

    // Two lines: "A A B " of 30 points without its last space, then "B";
    // the x of each glyph drawn
    private static List<float> Aligned(Alignment alignment) {
        PDF pdf = new PDF(new MemoryStream());
        Font font = new Font(pdf, LATIN).SetSize(10f);
        int a = font.unicodeToGID.Get('A');
        int b = font.unicodeToGID.Get('B');
        int space = font.unicodeToGID.Get(' ');
        FixedShaper shaper = new FixedShaper(
                new ShapedLine(0, 6, new int[] {0, 0, 0, 0, 0, 0}, new int[] {a, space, a, space, b, space},
                        new float[] {0f, 0f, 10f, 0f, 12f, 0f, 22f, 0f, 24f, 0f, 30f, 0f}, 30f),
                new ShapedLine(6, 7, new int[] {0}, new int[] {b}, new float[] {0f, 0f}, 6f));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new ComplexText(font, "A A B B").SetShaper(shaper).SetTextAlignment(alignment)
                .SetLocation(50f, 100f).SetWidth(100f).DrawOn(page);
        List<float> xs = new List<float>();
        foreach (Match m in Regex.Matches(TestSupport.Content(page), @"q \S+ 0 0 \S+ (\S+) \S+ cm /Fm")) {
            xs.Add(float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture));
        }
        return xs;
    }

    [Fact]
    public void TheLinesAreAligned() {
        // The glyphs drawn: A, A, B of the first line, B of the second
        Assert.Equal(new List<float> {50f, 62f, 74f, 50f}, Aligned(Alignment.LEFT));
        Assert.Equal(new List<float> {85f, 97f, 109f, 97f}, Aligned(Alignment.CENTER));
        Assert.Equal(new List<float> {120f, 132f, 144f, 144f}, Aligned(Alignment.RIGHT));
        // Justified: the 70 points left over go to the two spaces between the
        // words; the last line is left as it is
        Assert.Equal(new List<float> {50f, 97f, 144f, 50f}, Aligned(Alignment.JUSTIFY));
    }

    [Fact]
    public void TheLineSpacingIsAMultipleOfTheBodyHeightAsTextBlocks() {
        PDF pdf = new PDF(new MemoryStream());
        Font font = new Font(pdf, LATIN).SetSize(10f);
        int a = font.unicodeToGID.Get('A');
        FixedShaper shaper = new FixedShaper(
                new ShapedLine(0, 1, new int[] {0}, new int[] {a}, new float[] {0f, 0f}, 7f),
                new ShapedLine(1, 2, new int[] {0}, new int[] {a}, new float[] {0f, 0f}, 7f));
        ComplexText text = new ComplexText(font, "AA").SetShaper(shaper);
        float body = font.GetAscent() + font.GetDescent() + font.GetLineGap(10f);
        Assert.Equal(2 * body, text.GetTextHeight(), 3);
        Assert.Equal(3 * body, text.SetLineSpacing(1.5f).GetTextHeight(), 3);
    }

    // A shaper that gives each paragraph as many lines as its letters, one
    // glyph of the letter each
    private class LetterShaper : IShaper {
        private readonly Font font;

        internal LetterShaper(Font font) {
            this.font = font;
        }

        public List<ShapedLine> Shape(ShapingRequest request) {
            string text = request.Text;
            List<ShapedLine> lines = new List<ShapedLine>();
            for (int i = 0; i < text.Length; i++) {
                lines.Add(new ShapedLine(i, i + 1, new int[] {0}, new int[] {font.unicodeToGID.Get(text[i])},
                        new float[] {0f, 0f}, 7f));
            }
            return lines;
        }
    }

    [Fact]
    public void ParagraphsFlowFromPageToPageCutWhereTheHeightEnds() {
        PDF pdf = new PDF(new MemoryStream(), Compliance.PDF_UA_1);
        pdf.SetTitle("Title");
        Font font = new Font(pdf, LATIN).SetSize(10f);
        float line = font.GetAscent() + font.GetDescent() + font.GetLineGap(10f);
        ComplexText text = new ComplexText(font, new List<string> {"AAA", "BBB"})
                .SetShaper(new LetterShaper(font))
                .SetLanguage("en")
                .SetParagraphGap(5f)
                .SetHeight(4 * line + 5f);
        Assert.Equal(6 * line + 5f, text.GetTextHeight(), 3);
        // Three lines of the first paragraph, the gap, one of the second
        Page first = new Page(pdf, Letter.PORTRAIT);
        float[] corner = text.SetLocation(50f, 100f).DrawOn(first);
        Assert.Equal(100f + 4 * line + 5f, corner[1], 3);
        Assert.True(text.HasMoreText());
        Assert.Equal(2 * line, text.GetTextHeight(), 3);
        string content = TestSupport.Content(first);
        Assert.Equal(4, Count(content, " cm /Fm"));
        Assert.Equal(2, Count(content, "/P <<"));
        // The rest of the second paragraph, from the top of the next page
        Page second = new Page(pdf, Letter.PORTRAIT);
        corner = text.SetLocation(50f, 50f).DrawOn(second);
        Assert.Equal(50f + 2 * line, corner[1], 3);
        Assert.False(text.HasMoreText());
        Assert.Equal(0f, text.GetTextHeight());
        Assert.Equal(2, Count(TestSupport.Content(second), " cm /Fm"));
        pdf.Complete();
    }

    [Fact]
    public void GetHeightIsTheHeightSetAndTheGapIsTextFramesEmptyLine() {
        PDF pdf = new PDF(new MemoryStream());
        Font font = new Font(pdf, LATIN).SetSize(10f);
        ComplexText text = new ComplexText(font, new List<string> {"A", "B"}).SetShaper(new LetterShaper(font));
        Assert.Equal(0f, text.GetHeight());
        Assert.Equal(500f, text.SetHeight(500f).GetHeight());
        // Two lines, one empty line between them, as a TextFrame's default
        float line = font.GetAscent() + font.GetDescent() + font.GetLineGap(10f);
        float gap = font.GetAscent() + font.GetDescent() + 2f * font.GetLineGap(10f);
        Assert.Equal(2 * line + gap, text.GetTextHeight(), 3);
    }

    [Fact]
    public void AHeightTooSmallForALineStillDrawsOneEachTime() {
        PDF pdf = new PDF(new MemoryStream());
        Font font = new Font(pdf, LATIN).SetSize(10f);
        ComplexText text = new ComplexText(font, "AB").SetShaper(new LetterShaper(font)).SetHeight(1f);
        int pages = 0;
        while (text.HasMoreText()) {
            text.DrawOn(new Page(pdf, Letter.PORTRAIT));
            pages++;
        }
        Assert.Equal(2, pages);
    }

    [Fact]
    public void TheFontFileIsWhatTheFontWasLoadedFrom() {
        PDF pdf = new PDF(new MemoryStream());
        byte[] bytes = File.ReadAllBytes(DEVANAGARI);
        Font fromBytes = new Font(pdf, bytes);
        Font fromPath = new Font(pdf, DEVANAGARI);
        Font trueType = new Font(pdf, (Stream) new FileStream(CASCADIA, FileMode.Open, FileAccess.Read));
        FixedShaper shaper = new FixedShaper();
        Page page = new Page(pdf, Letter.PORTRAIT);
        new ComplexText(fromBytes, "x").SetFallbackFont(fromPath).SetShaper(shaper).DrawOn(page);
        Assert.Same(bytes, shaper.files[0]);
        Assert.Equal(bytes, shaper.files[1]);
        // A TrueType font keeps its whole file, from a stream too
        new ComplexText(trueType, "x").SetShaper(shaper).DrawOn(page);
        Assert.Equal(File.ReadAllBytes(CASCADIA), shaper.files[0]);
    }

    [Fact]
    public void WithoutAShaperOrAFontFileItSaysSo() {
        PDF pdf = new PDF(new MemoryStream());
        Font plex = new Font(pdf, (Stream) new FileStream(LATIN, FileMode.Open, FileAccess.Read));
        Font fromPath = new Font(pdf, LATIN);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Assert.Throws<InvalidOperationException>(() => new ComplexText(fromPath, "x").DrawOn(page));
        Assert.Throws<ArgumentException>(
                () => new ComplexText(plex, "x").SetShaper(new FixedShaper()).DrawOn(page));
    }
}
}   // End of namespace PDFjet.NET
