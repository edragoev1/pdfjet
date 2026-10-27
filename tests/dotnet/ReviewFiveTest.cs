/*
 * ReviewFiveTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace PDFjet.NET {
/// <summary>
/// The findings of the fresh-eyes review of the charts, Markdown and the
/// barcodes, of 27 October 2026.
/// </summary>
public class ReviewFiveTest {
    private static Markup NewMarkup(PDF pdf) {
        Font font = TestSupport.Helvetica(pdf);
        return new Markup(font, font, font, font, font);
    }

    // The text with each run of white space one space, and none at its ends.
    private static String JoinFields(String text) {
        return String.Join(" ", text.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries));
    }

    [Fact]
    public void TheTextOfAParagraphHasNoSpaceWhereItsRunsAreJoined() {
        Markup markup = NewMarkup(TestSupport.NewPDF());
        Assert.Equal("Version 9.1.5: notes", JoinFields(markup.Paragraph("Version **9.1.5**: notes").Text()));
        Assert.Equal("The Tables", JoinFields(markup.Paragraph("The `Table`s").Text()));
        Assert.Equal("one two", JoinFields(markup.Paragraph("one<br>two").Text()));
    }

    [Fact]
    public void TheBookmarkOfAHeadingHasItsText() {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Font font = TestSupport.Helvetica(pdf);
        List<Page> pages = new List<Page>();
        new Markdown(font, font, font, font, font).DrawOn(pdf, "# Version **9.1.5**: notes", pages, Letter.PORTRAIT);
        pdf.AddPages(pages);
        pdf.Complete();
        String text = TestSupport.Latin1(stream.ToArray());
        Assert.True(text.Contains(TestSupport.TextString("Version 9.1.5: notes")), "the title");
    }

    [Fact]
    public void ABackslashAtTheEndOfAParagraphIsKept() {
        Markup markup = NewMarkup(TestSupport.NewPDF());
        List<Paragraph> paragraphs = markup.Paragraphs("Save it to C:\\Temp\\");
        String text = paragraphs[0].Text();
        Assert.Equal("Save it to C:\\Temp\\", Util.Trim(text));
    }

    [Fact]
    public void TheAltTextOfAnImageIsItsText() {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        List<Page> pages = new List<Page>();
        // No image directory: the text is drawn in the image's place
        new Markdown(font, font, font, font, font)
                .DrawOn(pdf, "![Fish &amp; Chips \\*](fish.png)", pages, Letter.PORTRAIT);
        String content = TestSupport.Content(pages[0]);
        Assert.True(content.Contains(TestSupport.Hex("Fish & Chips *")), content);
    }

    [Fact]
    public void ALineValueOutsideTheRangeIsNotDrawnAtTheEdge() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = new BarChart(font, font).SetLocation(50f, 50f).SetSize(300f, 200f)
                .SetCategories("a", "b").SetValueAxisRange(0f, 100f);
        chart.AddSeries("", new float[] {50f, 60f});
        chart.AddLineSeries("target", new float[] {150f, 80f});
        chart.DrawOn(page);
        Assert.True(TestSupport.Content(page).Contains("W\nn\n"), TestSupport.Content(page));    // Clipped
        // In range, no clip
        page = new Page(pdf, Letter.PORTRAIT);
        chart = new BarChart(font, font).SetLocation(50f, 50f).SetSize(300f, 200f)
                .SetCategories("a", "b").SetValueAxisRange(0f, 100f);
        chart.AddSeries("", new float[] {50f, 60f});
        chart.AddLineSeries("target", new float[] {90f, 80f});
        chart.DrawOn(page);
        Assert.False(TestSupport.Content(page).Contains("W\nn\n"), TestSupport.Content(page));
    }

    [Fact]
    public void AHeadingOnAPageThatIsNotAddedHasNoBookmark() {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Font font = TestSupport.Helvetica(pdf);
        StringBuilder text = new StringBuilder("# One\n\n");
        for (int i = 0; i < 80; i++) {
            text.Append('p').Append(i).Append("\n\n");
        }
        text.Append("# Two");
        List<Page> pages = new List<Page>();
        new Markdown(font, font, font, font, font).DrawOn(pdf, text.ToString(), pages, Letter.PORTRAIT);
        Assert.True(pages.Count > 1);
        pdf.AddPage(pages[0]);      // The page of One only
        pdf.Complete();             // It failed: "Two" was on a page that is not added
        String output = TestSupport.Latin1(stream.ToArray());
        Assert.Contains(TestSupport.TextString("One"), output);
        Assert.DoesNotContain(TestSupport.TextString("Two"), output);
    }

    [Fact]
    public void AnEmptyCode128IsRefused() {
        Assert.ThrowsAny<Exception>(() => new Barcode(Barcode.CODE_128, ""));
    }

    [Fact]
    public void TwoBreaksLeaveAnEmptyRow() {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        PDF pdf = TestSupport.NewPDF();
        Markup markup = NewMarkup(pdf);
        Assert.Equal(2, markup.Paragraph("p1<br><br>p2").lines[0].lineBreaks);
        float[] gaps = new float[2];
        String[] texts = {"p1<br>p2", "p1<br><br>p2"};
        for (int k = 0; k < 2; k++) {
            Page page = new Page(pdf, Letter.PORTRAIT);
            new TextFrame(new List<Paragraph> {markup.Paragraph(texts[k])})
                    .SetLocation(50f, 50f).SetWidth(400f).DrawOn(page);
            String content = TestSupport.Content(page);
            gaps[k] = TestSupport.PositionOf(content, "p1")[1] - TestSupport.PositionOf(content, "p2")[1];
        }
        TestSupport.AssertNear(2f * gaps[0], gaps[1], 0.05f);   // To the 0.01 of the positions written
    }

    [Fact]
    public void APipeInCodeInATableCell() {
        Assert.Equal("TABLE(a;`x|y`|L)",
                MarkdownParserTest.Describe(MarkdownParser.Parse("| a |\n|---|\n| `x\\|y` |")));
    }

    [Fact]
    public void HardBreaksOfCRLFText() {
        Markup markup = NewMarkup(TestSupport.NewPDF());
        Assert.Equal(1, markup.Paragraph("one  \r\ntwo").lines[0].lineBreaks);
        Assert.Equal(1, markup.Paragraph("one\\\r\ntwo").lines[0].lineBreaks);
        Assert.Equal(0, markup.Paragraph("one \r\ntwo").lines[0].lineBreaks);
    }

    [Fact]
    public void EntitiesInTheAddressOfALink() {
        Markup markup = NewMarkup(TestSupport.NewPDF());
        Assert.Equal("https://x/?a=1&b=2",
                markup.Paragraph("[a](https://x/?a=1&amp;b=2)").lines[0].GetURIAction());
    }

    [Fact]
    public void AControlCharacterOfAnEntityIsASpace() {
        Markup markup = NewMarkup(TestSupport.NewPDF());
        Assert.Equal("a b c d", markup.Paragraph("a&#10;b&#9;c&#x7;d").lines[0].text);
    }

    [Fact]
    public void TheTrendIsOfThePointsDrawn() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        SimpleScatterChart chart = new SimpleScatterChart(font, font).SetTitle("T")
                .Add(1f, 1f).Add(2f, 2f).Add(3f, 3f).Add(4f, -1000f).SetTrendLine(true).SetYAxisRange(0f, 10f);
        Assert.Equal("T: 3 points; the trend rises.", chart.AltDescription());
    }
}
}   // End of namespace PDFjet.NET
