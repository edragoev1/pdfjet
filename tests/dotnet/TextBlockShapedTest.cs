/**
 * TextBlockShapedTest.cs
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
// TextBlock and TextLine with text such as Hindi or Khmer: a shaper wraps
// and shapes it, and the glyphs it found are drawn as outlines, each line
// tagged with its text; the other paragraphs are drawn as text. The tests
// give a shaper that returns fixed lines. In the DefaultShaper collection,
// run alone: two tests set the default shaper, which is the process's.
[Collection("DefaultShaper")]
public class TextBlockShapedTest {

    private const string DEVANAGARI = "resources/fonts/world/NotoSansDevanagari/NotoSansDevanagari-Regular.otf";
    private const string ARABIC = "resources/fonts/world/IBMPlexSansArabic/IBMPlexSansArabic-Regular.otf";
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
        internal int calls;

        internal FixedShaper(params ShapedLine[] lines) {
            this.lines = new List<ShapedLine>(lines);
        }

        public List<ShapedLine> Shape(ShapingRequest request) {
            this.files = request.FontFiles;
            this.size = request.FontSize;
            this.width = request.Width;
            this.language = request.Language;
            calls++;
            return lines;
        }
    }

    // A shaper that gives each paragraph as many lines as its letters, one
    // glyph of the letter each
    private class LetterShaper : IShaper {
        private readonly Font font;

        internal LetterShaper(Font font) {
            this.font = font;
        }

        public List<ShapedLine> Shape(ShapingRequest request) {
            List<ShapedLine> lines = new List<ShapedLine>();
            for (int i = 0; i < request.Text.Length; i++) {
                lines.Add(new ShapedLine(i, i + 1, new int[] {0}, new int[] {font.unicodeToGID.Get(request.Text[i])},
                        new float[] {0f, 0f}, 7f));
            }
            return lines;
        }
    }

    private static List<float[]> Places(string content) {
        List<float[]> places = new List<float[]>();
        foreach (Match m in Regex.Matches(content, @"q (\S+) 0 0 \S+ (\S+) (\S+) cm /Fm")) {
            places.Add(new float[] {
                float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
                float.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture)});
        }
        return places;
    }

    [Fact]
    public void TheShaperGivesTheGlyphsAndTheirPlaces() {
        PDF pdf = new PDF(new MemoryStream());
        Font font = new Font(pdf, TestSupport.RepoPath(LATIN));
        Font fallback = new Font(pdf, TestSupport.RepoPath(CASCADIA));
        int a = font.unicodeToGID.Get('A');
        int b = fallback.unicodeToGID.Get('B');
        FixedShaper shaper = new FixedShaper(
                new ShapedLine(0, 2, new int[] {0, 1}, new int[] {a, b}, new float[] {0f, 0f, 7f, 0f}, 13f),
                // A glyph of a font that was not given is not drawn
                new ShapedLine(2, 3, new int[] {-1}, new int[] {5}, new float[] {0f, 0f}, 6f));
        Page page = new Page(pdf, Letter.PORTRAIT);
        TextBlock text = new TextBlock(font, "कखग").SetFallbackFont(fallback).SetFontSize(10f)
                .SetLanguage("en").SetShaper(shaper).SetWidth(120f).SetLocation(50f, 100f);
        float[] corner = text.DrawOn(page);
        Assert.Equal(2, shaper.files.Length);
        Assert.Equal(10f, shaper.size);
        Assert.Equal(120f, shaper.width);
        Assert.Equal("en", shaper.language);
        float lineHeight = font.GetAscent(10f) + font.GetDescent(10f) + font.GetLineGap(10f);
        Assert.Equal(100f + 2 * lineHeight, corner[1], 3);
        // The fallback's glyph at the block's size, 10 points, on the first line's baseline
        float y = 792f - (100f + font.GetAscent(10f));
        List<float[]> places = Places(TestSupport.Content(page));
        Assert.Equal(2, places.Count);
        Assert.True(Math.Abs(places[0][0] - 0.01f) < 1e-6f);            // Plex, 1,000 units an em
        Assert.True(Math.Abs(places[1][0] - 10f / 2048f) < 1e-6f);      // Cascadia, 2,048
        Assert.Equal(50f, places[0][1]);
        Assert.Equal(57f, places[1][1]);
        Assert.True(Math.Abs(places[0][2] - y) < 0.006f && Math.Abs(places[1][2] - y) < 0.006f);
        // Shaped once however often the block is measured and drawn
        text.DrawOn(null);
        text.DrawOn(page);
        Assert.Equal(1, shaper.calls);
        pdf.Complete();
    }

    // A block of a paragraph of English, one of Hindi and one of Arabic: the
    // English and the Arabic drawn as text, the Arabic right to left by itself
    // and aligned to the right; the Hindi as glyphs, between them.
    [Fact]
    public void DrawsEachParagraphAsItNeeds() {
        PDF pdf = new PDF(new MemoryStream(), Compliance.PDF_UA_1);
        pdf.SetTitle("Title");
        Font font = new Font(pdf, TestSupport.RepoPath(DEVANAGARI));
        Font arabic = new Font(pdf, TestSupport.RepoPath(ARABIC));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextBlock(font, "Hello\nकखग\nمرحبا").SetFallbackFont(arabic).SetFontSize(10f).SetLanguage("hi")
                .SetShaper(new LetterShaper(font)).SetWidth(300f).SetLocation(50f, 100f).DrawOn(page);
        string content = TestSupport.Content(page);
        pdf.Complete();
        Assert.Equal(3, Count(content, " cm /Fm"));
        foreach (string letter in new[] {"क", "ख", "ग"}) {
            Assert.Contains("/ActualText <" + Page.ToUTF16Hex(letter) + ">", content);
        }
        MatchCollection tms = Regex.Matches(content, @"1 0 0 1 (\S+) \S+ Tm");
        Assert.Equal(2, tms.Count);
        Assert.Equal(50f, float.Parse(tms[0].Groups[1].Value, CultureInfo.InvariantCulture));
        Assert.True(float.Parse(tms[1].Groups[1].Value, CultureInfo.InvariantCulture) > 300f, "Arabic at the left");
    }

    [Fact]
    public void TheFontFileIsWhatTheFontWasLoadedFrom() {
        PDF pdf = new PDF(new MemoryStream());
        byte[] data = File.ReadAllBytes(TestSupport.RepoPath(DEVANAGARI));
        Font fromBytes = new Font(pdf, data);
        Font fromPath = new Font(pdf, TestSupport.RepoPath(DEVANAGARI));
        FixedShaper shaper = new FixedShaper();
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextBlock(fromBytes, "क").SetFallbackFont(fromPath).SetShaper(shaper).DrawOn(page);
        Assert.Same(data, shaper.files[0]);
        Assert.Equal(data, shaper.files[1]);
    }

    [Fact]
    public void WithoutAShaperOrAFontFileItSaysSo() {
        // The test project has the package PDFjet.HarfBuzz: a shaper set to
        // one that cannot run stands in for none
        PDF pdf = new PDF(new MemoryStream());
        Font fromPath = new Font(pdf, TestSupport.RepoPath(LATIN));
        Page page = new Page(pdf, Letter.PORTRAIT);
        Exception missing = Assert.ThrowsAny<Exception>(() => new TextBlock(fromPath, "क")
                .SetShaper(new HBShape("/no/such/hb-shape")).DrawOn(page));
        Assert.Contains("SetShaper", missing.Message, StringComparison.Ordinal);
        pdf = new PDF(new MemoryStream());
        Font fromStream = new Font(pdf, new MemoryStream(File.ReadAllBytes(TestSupport.RepoPath(LATIN))));
        Page other = new Page(pdf, Letter.PORTRAIT);
        Exception noFile = Assert.ThrowsAny<Exception>(() => new TextBlock(fromStream, "क")
                .SetShaper(new FixedShaper()).DrawOn(other));
        Assert.Contains("font files", noFile.Message, StringComparison.Ordinal);
    }

    // Two lines: "A A B " of 30 points without its last space, then "B";
    // the x of each glyph drawn
    private static List<float> Aligned(Alignment alignment) {
        PDF pdf = new PDF(new MemoryStream());
        Font font = new Font(pdf, TestSupport.RepoPath(LATIN));
        int a = font.unicodeToGID.Get('A');
        int b = font.unicodeToGID.Get('B');
        int space = font.unicodeToGID.Get(' ');
        FixedShaper shaper = new FixedShaper(
                new ShapedLine(0, 6, new int[] {0, 0, 0, 0, 0, 0}, new int[] {a, space, a, space, b, space},
                        new float[] {0f, 0f, 10f, 0f, 12f, 0f, 22f, 0f, 24f, 0f, 30f, 0f}, 30f),
                new ShapedLine(6, 7, new int[] {0}, new int[] {b}, new float[] {0f, 0f}, 6f));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextBlock(font, "क क ख ख").SetFontSize(10f).SetShaper(shaper).SetTextAlignment(alignment)
                .SetWidth(100f).SetLocation(50f, 100f).DrawOn(page);
        List<float> xs = new List<float>();
        foreach (float[] place in Places(TestSupport.Content(page))) {
            xs.Add(place[1]);
        }
        return xs;
    }

    [Fact]
    public void TheShapedLinesAreAligned() {
        Assert.Equal(new List<float> {50f, 62f, 74f, 50f}, Aligned(Alignment.LEFT));
        Assert.Equal(new List<float> {85f, 97f, 109f, 97f}, Aligned(Alignment.CENTER));
        Assert.Equal(new List<float> {120f, 132f, 144f, 144f}, Aligned(Alignment.RIGHT));
    }

    [Fact]
    public void ShapedLinesAreSpacedAsItsText() {
        PDF pdf = new PDF(new MemoryStream());
        Font font = new Font(pdf, TestSupport.RepoPath(DEVANAGARI));
        float body = font.GetAscent(10f) + font.GetDescent(10f) + font.GetLineGap(10f);
        TextBlock text = new TextBlock(font, "कख").SetFontSize(10f).SetShaper(new LetterShaper(font)).SetWidth(200f);
        Assert.Equal(2 * body, text.DrawOn(null)[1], 3);
        text.SetLineSpacing(1.5f);
        Assert.Equal(3 * body, text.DrawOn(null)[1], 3);
        // With a height, the lines that fit, the glyphs not cut with "..."
        text.SetLineSpacing(1f).SetHeight(1.5f * body);
        Page page = new Page(pdf, Letter.PORTRAIT);
        text.DrawOn(page);
        Assert.Equal(1, Count(TestSupport.Content(page), " cm /Fm"));
    }

    // Text such as Hindi in Table cells: each cell as tall as its text
    // wrapped to the column, the text drawn whole each time, as the header
    // row is on each page.
    [Fact]
    public void InTableCellsItWrapsAndRepeatsTheHeader() {
        PDF pdf = new PDF(new MemoryStream(), Compliance.PDF_UA_1);
        pdf.SetTitle("Title");
        Font font = new Font(pdf, TestSupport.RepoPath(DEVANAGARI));
        float line = font.GetAscent(10f) + font.GetDescent(10f) + font.GetLineGap(10f);
        LetterShaper shaper = new LetterShaper(font);
        Func<string, TextBlock> hindi = text => new TextBlock(font, text).SetFontSize(10f).SetShaper(shaper).SetLanguage("hi");
        List<List<Cell>> data = new List<List<Cell>> {
            new List<Cell> {new Cell(font).SetTextBlock(hindi("क")), new Cell(font).SetTextBlock(hindi("ख"))},
        };
        for (int r = 0; r < 40; r++) {
            data.Add(new List<Cell> {new Cell(font, "Row " + (r + 1)), new Cell(font).SetTextBlock(hindi("कखग"))});
        }
        Table table = new Table().SetTableData(data, 1);
        table.SetLocation(50f, 50f);
        table.SetColumnWidth(0, 100f).SetColumnWidth(1, 250f);
        Cell cell = data[1][1];
        Assert.Equal(3 * line + cell.GetTopPadding() + cell.GetBottomPadding(), cell.GetHeight(250f), 2);
        List<Page> pages = new List<Page>();
        table.DrawOn(pdf, pages, Letter.PORTRAIT);
        Assert.True(pages.Count >= 2, "the table on " + pages.Count + " page");
        string firstPage = TestSupport.Content(pages[0]);
        Assert.Contains("/ActualText <" + Page.ToUTF16Hex("क") + ">", firstPage);
        Assert.Contains("/ActualText <" + Page.ToUTF16Hex("ख") + ">", firstPage);
        for (int i = 1; i < pages.Count; i++) {
            Assert.True(Count(TestSupport.Content(pages[i]), " cm /Fm") >= 2, "page " + (i + 1) + ": no glyphs");
        }
    }

    // A shaper as LetterShaper, which counts its calls and keeps the files
    private class CountingShaper : IShaper {
        private readonly LetterShaper letters;
        internal int calls;
        internal byte[][] files;

        internal CountingShaper(Font font) {
            this.letters = new LetterShaper(font);
        }

        public List<ShapedLine> Shape(ShapingRequest request) {
            calls++;
            files = request.FontFiles;
            return letters.Shape(request);
        }
    }

    // A TextFrame of Hindi words places them by their shaped widths, the
    // widths they are drawn at, each word shaped once however often measured
    [Fact]
    public void TextFrameMeasuresShapedWords() {
        PDF pdf = new PDF(new MemoryStream());
        Font font = new Font(pdf, TestSupport.RepoPath(DEVANAGARI)).SetSize(10f);
        CountingShaper shaper = new CountingShaper(font);
        TextBlock.SetDefaultShaper(shaper);
        try {
            Paragraph paragraph = new Paragraph().Add(new TextLine(font, "कख गघ ङच"));
            TextFrame frame = new TextFrame(new List<Paragraph> {paragraph});
            frame.SetLocation(10f, 10f);
            frame.SetWidth(300f);
            frame.SetHeight(500f);
            Page page = new Page(pdf, Letter.PORTRAIT);
            frame.DrawOn(page);
            List<float> xs = new List<float>();
            foreach (float[] place in Places(TestSupport.Content(page))) {
                xs.Add(place[1]);
            }
            // Each word drawn with the space after it, shaped with it: the
            // test's shaper gives a space 7 points, as every letter
            Assert.Equal(new List<float> {10f, 17f, 31f, 38f, 52f, 59f}, xs);
            Assert.True(shaper.calls <= 8, "shaped " + shaper.calls + " times");
            pdf.Complete();
        } finally {
            TextBlock.SetDefaultShaper(null);
        }
    }

    // A TextBlock or a TextLine given another font, fallback font or language
    // shapes its text again: the glyphs of one font are not drawn in another
    [Fact]
    public void ShapedTextIsShapedAgainInAnotherFont() {
        PDF pdf = new PDF(new MemoryStream());
        Font regular = new Font(pdf, TestSupport.RepoPath(DEVANAGARI));
        Font other = new Font(pdf, TestSupport.RepoPath(LATIN));
        CountingShaper shaper = new CountingShaper(regular);
        TextBlock block = new TextBlock(regular, "कख").SetShaper(shaper).SetWidth(200f);
        block.DrawOn(null);
        block.DrawOn(null);
        Assert.Equal(1, shaper.calls);
        block.SetFont(other);
        block.DrawOn(null);
        block.SetLanguage("mr");
        block.DrawOn(null);
        Assert.Equal(3, shaper.calls);
        TextBlock.SetDefaultShaper(shaper);
        try {
            TextLine line = new TextLine(regular, "कख");
            line.GetWidth();
            line.GetWidth();
            line.SetFont(other);
            line.GetWidth();
            Assert.Equal(5, shaper.calls);
        } finally {
            TextBlock.SetDefaultShaper(null);
        }
    }

    // A fallback font the text engine cannot read, as a core font, is left out
    // of the shaping: Hindi with a Latin core fallback is drawn
    [Fact]
    public void AFallbackFontWithoutAFileIsLeftOut() {
        PDF pdf = new PDF(new MemoryStream());
        Font font = new Font(pdf, TestSupport.RepoPath(DEVANAGARI));
        CountingShaper shaper = new CountingShaper(font);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextBlock(font, "कख").SetFallbackFont(new Font(pdf, CoreFont.HELVETICA)).SetShaper(shaper)
                .SetWidth(200f).DrawOn(page);
        Assert.Single(shaper.files);
        Assert.Equal(2, Count(TestSupport.Content(page), " cm /Fm"));
        pdf.Complete();
    }

    // In an untagged document, the outlines of shaped text carry its text, as
    // marked content of their own, which a reader may copy and search
    [Fact]
    public void UntaggedShapedTextCarriesItsText() {
        PDF pdf = new PDF(new MemoryStream());
        Font font = new Font(pdf, TestSupport.RepoPath(DEVANAGARI));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextBlock(font, "कख").SetShaper(new LetterShaper(font)).SetWidth(200f).DrawOn(page);
        Assert.Contains("/ActualText <" + Page.ToUTF16Hex("क") + ">>> BDC", TestSupport.Content(page));
        pdf.Complete();
    }

    // A paragraph that starts with a byte order mark is not right to left
    [Fact]
    public void AByteOrderMarkIsNotArabic() {
        PDF pdf = new PDF(new MemoryStream());
        Font font = new Font(pdf, TestSupport.RepoPath(LATIN));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextBlock(font, "\uFEFFHello").SetWidth(300f).SetLocation(50f, 100f).DrawOn(page);
        Match m = Regex.Match(TestSupport.Content(page), @"1 0 0 1 (\S+) \S+ Tm");
        Assert.True(m.Success);
        Assert.Equal(50f, float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture));
    }

    // TextLine with text such as Hindi: shaped as one line, drawn as glyphs,
    // tagged with its text, its width the glyphs'.
    [Fact]
    public void TextLineShapesTextSuchAsHindi() {
        PDF pdf = new PDF(new MemoryStream(), Compliance.PDF_UA_1);
        pdf.SetTitle("Title");
        Font font = new Font(pdf, TestSupport.RepoPath(DEVANAGARI)).SetSize(10f);
        TextBlock.SetDefaultShaper(new LetterShaper(font));
        try {
            TextLine line = new TextLine(font, "कखग");
            line.SetLocation(50f, 100f);
            Assert.Equal(21f, line.GetWidth(), 3);
            Page page = new Page(pdf, Letter.PORTRAIT);
            line.DrawOn(page);
            string content = TestSupport.Content(page);
            Assert.Equal(3, Count(content, " cm /Fm"));
            Assert.Contains("/ActualText <" + Page.ToUTF16Hex("कखग") + ">", content);
            Assert.DoesNotContain("Tj", content);
            pdf.Complete();
        } finally {
            TextBlock.SetDefaultShaper(null);
        }
    }

    // A TextLine of text such as Hindi turned: its glyphs turned around its
    // start as the same line of English is, the one rotation for both.
    [Fact]
    public void TextLineTurnsItsGlyphsAsItsText() {
        PDF pdf = new PDF(new MemoryStream());
        Font font = new Font(pdf, TestSupport.RepoPath(DEVANAGARI)).SetSize(10f);
        Font latin = new Font(pdf, TestSupport.RepoPath(LATIN)).SetSize(10f);
        TextBlock.SetDefaultShaper(new LetterShaper(font));
        try {
            foreach (int degrees in new[] {30, 90, -45}) {
                Page english = new Page(pdf, Letter.PORTRAIT);
                TextLine text = new TextLine(latin, "Hello");
                text.SetLocation(50f, 100f);
                text.SetTextRotation(degrees);
                text.DrawOn(english);
                string englishContent = TestSupport.Content(english);
                Page hindi = new Page(pdf, Letter.PORTRAIT);
                TextLine line = new TextLine(font, "कखग");
                line.SetLocation(50f, 100f);
                line.SetTextRotation(degrees);
                line.DrawOn(hindi);
                string content = TestSupport.Content(hindi);
                Match tm = Regex.Match(englishContent, @"(\S+) (\S+) (\S+) (\S+) \S+ \S+ Tm");
                Match cm = Regex.Match(content, @"q\n1 0 0 1 50 692 cm\n(\S+) (\S+) (\S+) (\S+) 0 0 cm\n1 0 0 1 -50 -692 cm\n");
                Assert.True(tm.Success && cm.Success, degrees + " degrees:\n" + englishContent + "\n" + content);
                for (int i = 1; i <= 4; i++) {
                    Assert.Equal(float.Parse(tm.Groups[i].Value, CultureInfo.InvariantCulture),
                            float.Parse(cm.Groups[i].Value, CultureInfo.InvariantCulture), 5);
                }
                Assert.Equal(3, Count(content, " cm /Fm"));
            }
            Page page = new Page(pdf, Letter.PORTRAIT);
            TextLine flat = new TextLine(font, "कखग");
            flat.SetLocation(50f, 100f);
            flat.DrawOn(page);
            Assert.DoesNotContain("1 0 0 1 -50 -692 cm", TestSupport.Content(page));
            pdf.Complete();
        } finally {
            TextBlock.SetDefaultShaper(null);
        }
    }
}

// The tests that set the default shaper, or need none set, run alone
[CollectionDefinition("DefaultShaper", DisableParallelization = true)]
public class DefaultShaperCollection {
}
}   // End of namespace PDFjet.NET
