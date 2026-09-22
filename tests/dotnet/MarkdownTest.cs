/*
 * MarkdownTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace PDFjet.NET {
public class MarkdownTest {
    // A Markdown text drawn in a PDF/UA document: its pages and the whole PDF.
    private sealed class Drawn {
        internal List<Page> pages = new List<Page>();
        internal String pdf;
        internal String text;   // The text of the pages, from their content streams
        internal List<String> contents = new List<String>();    // Of each page, before it is written
    }

    private static Drawn Draw(String text, String imageDirectory) {
        MemoryStream bos = new MemoryStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1);
        pdf.SetTitle("Markdown");
        Font regular = new Font(pdf, CoreFont.HELVETICA);
        Font bold = new Font(pdf, CoreFont.HELVETICA_BOLD);
        Font italic = new Font(pdf, CoreFont.HELVETICA_OBLIQUE);
        Font boldItalic = new Font(pdf, CoreFont.HELVETICA_BOLD_OBLIQUE);
        Font code = new Font(pdf, CoreFont.COURIER);
        Markdown markdown = new Markdown(regular, bold, italic, boldItalic, code);
        if (imageDirectory != null) {
            markdown.SetImageDirectory(imageDirectory);
        }
        Drawn drawn = new Drawn();
        markdown.DrawOn(pdf, text, drawn.pages, Letter.PORTRAIT);
        StringBuilder content = new StringBuilder();
        foreach (Page page in drawn.pages) {
            drawn.contents.Add(TestSupport.Content(page));
            content.Append(TestSupport.Content(page));
        }
        drawn.text = content.ToString();
        if (drawn.pages.Count > 0) {
            pdf.AddPages(drawn.pages);
            pdf.Complete();
            drawn.pdf = TestSupport.Latin1(bos.ToArray());
        }
        return drawn;
    }

    private static int Count(String pdf, String structure) {
        return MarkdownParserTest.Occurrences(pdf, "/S /" + structure + "\n");
    }

    [Fact]
    public void AnEmptyTextNeedsNoPage() {
        Assert.Empty(Draw("", null).pages);
        Assert.Empty(Draw("\n  \n", null).pages);
    }

    [Fact]
    public void EveryBlockIsTaggedForPDFUA() {
        Drawn drawn = Draw("# Title\n\nText with **bold**.\n\n- one\n- two\n\n> quoted\n\n"
                + "```\ncode\n```\n\n| A | B |\n|---|---|\n| 1 | 2 |\n\n---\n\n## Next", null);
        Assert.Single(drawn.pages);
        Assert.Equal(1, Count(drawn.pdf, "H1"));
        Assert.Equal(1, Count(drawn.pdf, "H2"));
        Assert.Equal(1, Count(drawn.pdf, "L"));
        Assert.Equal(2, Count(drawn.pdf, "LI"));
        Assert.Equal(2, Count(drawn.pdf, "Lbl"));
        Assert.Equal(2, Count(drawn.pdf, "LBody"));
        Assert.Equal(1, Count(drawn.pdf, "BlockQuote"));
        Assert.Equal(1, Count(drawn.pdf, "Code"));
        Assert.Equal(1, Count(drawn.pdf, "Table"));
    }

    [Fact]
    public void HeadingLevelsSkipNone() {
        // A text that starts at ### and goes on to ##### is H1, then H2.
        Drawn drawn = Draw("### Three\n\n##### Five\n\n# One", null);
        Assert.Equal(2, Count(drawn.pdf, "H1"));
        Assert.Equal(1, Count(drawn.pdf, "H2"));
        Assert.Equal(0, Count(drawn.pdf, "H3"));
    }

    [Fact]
    public void TheTextFlowsOntoAsManyPagesAsItNeeds() {
        StringBuilder text = new StringBuilder("# A long text\n\n");
        for (int i = 0; i < 150; i++) {
            text.Append("Paragraph p").Append(1000 + i)
                    .Append(" has words enough to take a line or two of the page.\n\n");
        }
        Drawn drawn = Draw(text.ToString(), null);
        Assert.True(drawn.pages.Count >= 3, drawn.pages.Count + " pages");
        for (int i = 0; i < 150; i++) {
            String word = TestSupport.Hex("p" + (1000 + i));
            Assert.True(MarkdownParserTest.Occurrences(drawn.text, word) == 1, "p" + (1000 + i));
        }
        // No text is drawn under the bottom margin of 72 points.
        foreach (Match m in Regex.Matches(drawn.text, "[-0-9.]+ ([-0-9.]+) Td\n")) {
            Assert.True(float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) >= 72f,
                    "a baseline at y = " + m.Groups[1].Value);
        }
    }

    [Fact]
    public void AListOverPagesIsOneList() {
        StringBuilder text = new StringBuilder();
        for (int i = 0; i < 80; i++) {
            text.Append("- item ").Append(i).Append('\n');
        }
        Drawn drawn = Draw(text.ToString(), null);
        Assert.True(drawn.pages.Count >= 2, drawn.pages.Count + " pages");
        Assert.Equal(1, Count(drawn.pdf, "L"));
        Assert.Equal(80, Count(drawn.pdf, "LI"));
    }

    [Fact]
    public void ATableGoesOnOnTheNextPageWithItsHeaderRow() {
        // Letters of the core fonts that are kerned are drawn apart, so the
        // header is one letter a column.
        StringBuilder text = new StringBuilder("Some text first.\n\n| N | S |\n|---:|---:|\n");
        for (int i = 1; i <= 120; i++) {
            text.Append("| ").Append(i).Append(" | ").Append(i * i).Append(" |\n");
        }
        Drawn drawn = Draw(text.ToString(), null);
        Assert.True(drawn.pages.Count >= 2, drawn.pages.Count + " pages");
        Assert.Equal(1, Count(drawn.pdf, "Table"));
        foreach (String content in drawn.contents) {
            Assert.True(content.Contains("<" + TestSupport.Hex("S") + ">", StringComparison.Ordinal),
                    "the header row is on every page");
        }
        Assert.Contains(TestSupport.Hex("14400"), drawn.text, StringComparison.Ordinal);
    }

    [Fact]
    public void ImagesAreReadOnlyFromTheImageDirectory() {
        String text = "![Tux](linux-logo.png)";
        // With no directory, the image's text is drawn instead.
        Drawn drawn = Draw(text, null);
        Assert.Equal(0, Count(drawn.pdf, "Figure"));
        Assert.Contains(TestSupport.Hex("Tux"), drawn.text, StringComparison.Ordinal);
        // From the directory, the image is a figure.
        String images = TestSupport.RepoPath("images");
        drawn = Draw(text, images);
        Assert.Equal(1, Count(drawn.pdf, "Figure"));
        Match alt = Regex.Match(drawn.pdf, "/Alt <([0-9A-Fa-f]+)>");
        Assert.True(alt.Success && TestSupport.Utf16Hex(alt.Groups[1].Value) == "Tux",
                "the text of the image is its description");
        // Not above it, not an absolute path, not a URL.
        foreach (String source in new String[] {"../images/linux-logo.png", "/etc/passwd",
                System.IO.Path.GetFullPath(TestSupport.RepoPath("images/linux-logo.png")),
                "https://pdfjet.com/logo.png", "missing.png"}) {
            drawn = Draw("![Not read](" + source + ")", images);
            Assert.True(Count(drawn.pdf, "Figure") == 0, source);
            Assert.True(drawn.text.Contains(TestSupport.Hex("Not read"), StringComparison.Ordinal), source);
        }
    }

    [Fact]
    public void CodeKeepsItsLinesAndGoesOnOnTheNextPage() {
        StringBuilder text = new StringBuilder("```\n");
        for (int i = 0; i < 90; i++) {
            text.Append("  line ").Append(i).Append(" *not emphasis*\n");
        }
        text.Append("```\n");
        Drawn drawn = Draw(text.ToString(), null);
        Assert.True(drawn.pages.Count >= 2, drawn.pages.Count + " pages");
        Assert.Equal(1, Count(drawn.pdf, "Code"));
        Assert.Contains(TestSupport.Hex("  line 0 *not emphasis*"), drawn.text, StringComparison.Ordinal);
        Assert.Contains(TestSupport.Hex("  line 89 *not emphasis*"), drawn.text, StringComparison.Ordinal);
    }

    [Fact]
    public void NumberedListsStartAtTheirFirstNumber() {
        Drawn drawn = Draw("3. three\n4. four", null);
        Assert.Contains(TestSupport.Hex("3."), drawn.text, StringComparison.Ordinal);
        Assert.Contains(TestSupport.Hex("4."), drawn.text, StringComparison.Ordinal);
        Assert.DoesNotContain("<" + TestSupport.Hex("1.") + ">", drawn.text, StringComparison.Ordinal);
    }

    [Fact]
    public void HtmlIsDrawnAsText() {
        Drawn drawn = Draw("<b>not bold</b>", null);
        Assert.Contains(TestSupport.Hex("<b>not"), drawn.text, StringComparison.Ordinal);
    }

    [Fact]
    public void LongCodeLinesAreCutInLinearTime() {
        StringBuilder line = new StringBuilder();
        for (int i = 0; i < 200000; i++) {
            line.Append((char) ('a' + i % 26));
        }
        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
        Drawn drawn = Draw("```\n" + line + "\n```", null);
        Assert.True(drawn.pages.Count > 1);
        Assert.True(watch.ElapsedMilliseconds < 5000, watch.ElapsedMilliseconds + " ms");
    }

    [Fact]
    public void ACodeLineOfOneColumnKeepsItsSurrogatePairs() {
        PDF pdf = new PDF(new MemoryStream());
        Font font = new Font(pdf, CoreFont.HELVETICA);
        Font code = new Font(pdf, CoreFont.COURIER);
        // A width that one character of the code fills.
        Markdown markdown = new Markdown(font, font, font, font, code).SetMargins(300f, 72f, 300f, 72f);
        List<Page> pages = new List<Page>();
        markdown.DrawOn(pdf, "```\n\uD83D\uDE00x\uD83D\uDE00\n```", pages, Letter.PORTRAIT);
        Assert.Single(pages);
    }
}
}
