/*
 * MarkdownAdditionsTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace PDFjet.NET {
/// <summary>
/// The additions to Markdown of 27 October 2026: hard line breaks, &lt;br&gt;
/// in a table cell, entities, and the bookmarks of the headings of a document
/// that is not tagged.
/// </summary>
public class MarkdownAdditionsTest {
    private static Markup NewMarkup(PDF pdf) {
        Font font = TestSupport.Helvetica(pdf);
        return new Markup(font, font, font, font, font);
    }

    // The breaks of the text lines of the paragraph, in order.
    private static List<bool> Breaks(Paragraph paragraph) {
        List<bool> breaks = new List<bool>();
        foreach (TextLine line in paragraph.lines) {
            breaks.Add(line.lineBreaks > 0);
        }
        return breaks;
    }

    [Fact]
    public void HardLineBreaks() {
        Markup markup = NewMarkup(TestSupport.NewPDF());
        Assert.Equal(new List<bool> {true, false}, Breaks(markup.Paragraph("one  \ntwo")));
        Assert.Equal(new List<bool> {true, false}, Breaks(markup.Paragraph("one\\\ntwo")));
        Assert.Equal(new List<bool> {true, false}, Breaks(markup.Paragraph("one<br>two")));
        Assert.Equal(new List<bool> {true, false}, Breaks(markup.Paragraph("one<BR />two")));
        Assert.Equal(new List<bool> {true, false}, Breaks(markup.Paragraph("one<br/>two")));
        // A line break after one space, and a <br> in code, are no breaks
        Assert.Equal(new List<bool> {false}, Breaks(markup.Paragraph("one \ntwo")));
        Assert.Equal(new List<bool> {false}, Breaks(markup.Paragraph("`a<br>b`")));
        Assert.Equal("<br", markup.Paragraph("<br").lines[0].text);
        // Emphasis goes on over a break
        Paragraph bold = markup.Paragraph("**one<br>two**");
        Assert.Equal(2, bold.lines.Count);
        Assert.Equal(1, bold.lines[0].lineBreaks);
        Assert.False(bold.JoinsPrevious(1));
    }

    // A hard line break at the end of a text is no break, as in CommonMark,
    // and the spaces before it are not in the last text line; Paragraphs
    // strips the whitespace at the end of each paragraph.
    [Fact]
    public void AHardLineBreakAtTheEndIsNoBreak() {
        Markup markup = NewMarkup(TestSupport.NewPDF());
        foreach (String text in new String[] {"0  \n", "0  \r\n", "0   \n", "0  \n\n", "0  \n  "}) {
            Paragraph paragraph = markup.Paragraph(text);
            Assert.Single(paragraph.lines);
            Assert.Equal("0", paragraph.lines[0].text);
            Assert.Equal(0, paragraph.lines[0].lineBreaks);
            List<Paragraph> paragraphs = markup.Paragraphs(text);
            Assert.Single(paragraphs);
            Assert.Single(paragraphs[0].lines);
            Assert.Equal("0", paragraphs[0].lines[0].text);
        }
        // In bold, and a break before the last line
        Paragraph bold = markup.Paragraph("**0**  \n");
        Assert.Single(bold.lines);
        Assert.Equal("0", bold.lines[0].text);
        Assert.Equal(0, bold.lines[0].lineBreaks);
        Paragraph two = markup.Paragraph("one  \ntwo  \n");
        Assert.Equal(new List<bool> {true, false}, Breaks(two));
        Assert.Equal("two", two.lines[1].text);
        // Spaces at the end without a line break are text in Paragraph
        Assert.Equal("0  ", markup.Paragraph("0  ").lines[0].text);
        Assert.Equal("0", markup.Paragraphs("0  ")[0].lines[0].text);
    }

    [Fact]
    public void ATextFrameStartsARowAtABreak() {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Markup markup = NewMarkup(pdf);
        new TextFrame(new List<Paragraph> {markup.Paragraph("p1<br>p2 p3")})
                .SetLocation(50f, 50f).SetWidth(400f).DrawOn(page);
        String content = TestSupport.Content(page);
        float[] first = TestSupport.PositionOf(content, "p1");
        float[] second = TestSupport.PositionOf(content, "p2");
        Assert.Equal(first[0], second[0], TestSupport.DELTA);
        Assert.True(second[1] < first[1], content);     // Below it, y up
    }

    [Fact]
    public void EntitiesAreTheirCharacters() {
        Markup markup = NewMarkup(TestSupport.NewPDF());
        Assert.Equal("Fish & Chips — — — © \u00A0 < >",
                Util.Trim(markup.Paragraph("Fish &amp; Chips &mdash; &#8212; &#x2014; &copy; &nbsp; &lt; &gt;").lines[0].text));
        // Not an entity: an unknown name, no ;, no character, an escaped &
        Assert.Equal("&bogus; &amp &#0; &#xD800; &#12345678; &amp;",
                Util.Trim(markup.Paragraph("&bogus; &amp &#0; &#xD800; &#12345678; \\&amp;").lines[0].text));
        // &lt;br&gt; is the text <br>, not a break
        Paragraph paragraph = markup.Paragraph("a &lt;br&gt; b");
        Assert.Equal(new List<bool> {false}, Breaks(paragraph));
        Assert.Equal("a <br> b", Util.Trim(paragraph.lines[0].text));
        // In code an entity is its text
        Assert.Equal("&amp;", markup.Paragraph("`&amp;`").lines[0].text);
        // A character above U+FFFF is its surrogate pair, as in Java
        Assert.Equal("😀", markup.Paragraph("&#x1F600;").lines[0].text);
    }

    [Fact]
    public void ABrInATableCellBreaksItsLine() {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        List<Page> pages = new List<Page>();
        new Markdown(font, font, font, font, font)
                .DrawOn(pdf, "| r0 |\n|---|\n| r1<br>r2 |", pages, Letter.PORTRAIT);
        String content = TestSupport.Content(pages[0]);
        float[] first = TestSupport.PositionOf(content, "r1");
        float[] second = TestSupport.PositionOf(content, "r2");
        Assert.Equal(first[0], second[0], TestSupport.DELTA);
        Assert.True(second[1] < first[1], content);     // Below it, y up
        Assert.DoesNotContain(TestSupport.Hex("<br>"), content);
    }

    [Fact]
    public void TheParagraphOfAMarkdownTextKeepsItsBreaks() {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        List<Page> pages = new List<Page>();
        new Markdown(font, font, font, font, font)
                .DrawOn(pdf, "p1  \np2\\\np3\np4", pages, Letter.PORTRAIT);
        String content = TestSupport.Content(pages[0]);
        float y1 = TestSupport.PositionOf(content, "p1")[1];
        float y2 = TestSupport.PositionOf(content, "p2")[1];
        float y3 = TestSupport.PositionOf(content, "p3")[1];
        Assert.True(y2 < y1 && y3 < y2, content);     // Each below the one before it, y up
        Assert.Contains(TestSupport.Hex("p4"), content);
        Assert.DoesNotContain(TestSupport.Hex("\\"), content);
    }

    // The PDF of the text drawn as Markdown, in a document that is not tagged.
    private static String PdfOf(String text) {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        MemoryStream bos = new MemoryStream();
        PDF pdf = new PDF(bos);
        Font font = TestSupport.Helvetica(pdf);
        List<Page> pages = new List<Page>();
        new Markdown(font, font, font, font, font).DrawOn(pdf, text, pages, Letter.PORTRAIT);
        pdf.AddPages(pages);
        pdf.Complete();
        return TestSupport.Latin1(bos.ToArray());
    }

    [Fact]
    public void TheHeadingsOfAnUntaggedDocumentAreItsBookmarks() {
        String pdf = PdfOf("# One\n\ntext\n\n## Two\n\nmore");
        Assert.Contains("/Outlines", pdf);
        Assert.True(pdf.Contains(TestSupport.TextString("One")) && pdf.Contains(TestSupport.TextString("Two")), pdf);
        Assert.DoesNotContain("/Outlines", PdfOf("text alone"));
    }

    [Fact]
    public void ADocumentWithBookmarksOfItsOwnKeepsThem() {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        MemoryStream bos = new MemoryStream();
        PDF pdf = new PDF(bos);
        Font font = TestSupport.Helvetica(pdf);
        List<Page> pages = new List<Page>();
        new Markdown(font, font, font, font, font).DrawOn(pdf, "# One", pages, Letter.PORTRAIT);
        Bookmark toc = new Bookmark(pdf);
        Title title = new Title(font, "Mine", 50f, 100f);
        toc.AddBookmark(pages[0], title);
        title.DrawOn(pages[0]);
        pdf.AddPages(pages);
        pdf.Complete();
        String text = TestSupport.Latin1(bos.ToArray());
        Assert.True(text.Contains(TestSupport.TextString("Mine")), "their bookmark");
        Assert.False(text.Contains(TestSupport.TextString("One")), "the heading's bookmark");
    }
}
}   // End of namespace PDFjet.NET
