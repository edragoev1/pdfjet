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
using System.Linq;
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
        return Draw(text, imageDirectory, markdown => {});
    }

    // The Markdown drawn after the change, as an option set on it.
    private static Drawn Draw(String text, String imageDirectory, Action<Markdown> change) {
        MemoryStream bos = new MemoryStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1).SetTitle("Test");
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
        change(markdown);
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
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
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
    public void TasksAreLabeledByTheirBoxes() {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        Drawn drawn = Draw("- [x] done\n- [ ] open\n- plain", null);
        Assert.Equal(1, Count(drawn.pdf, "L"));
        Assert.Equal(3, Count(drawn.pdf, "LI"));
        Assert.Equal(3, Count(drawn.pdf, "Lbl"));
        Assert.Equal(3, Count(drawn.pdf, "LBody"));
        // The boxes are read as ☑ and ☐, in UTF-16BE, their actual text and
        // their description, the plain item by its bullet
        foreach (String actualText in new String[] {"<feff2611>", "<feff2610>"}) {
            Assert.True(MarkdownParserTest.Occurrences(drawn.pdf, "/ActualText " + actualText) == 1
                    && MarkdownParserTest.Occurrences(drawn.pdf, "/Alt " + actualText) == 1,
                    "no /ActualText " + actualText);
        }
    }

    // Color.transparent leaves the color of a done task's box green: it filled
    // the box with the color last set, black (review of 15 October 2026).
    [Fact]
    public void ATransparentCheckColorKeepsTheGreen() {
        Font font = TestSupport.PlexSans(TestSupport.NewPDF());
        Markdown markdown = new Markdown(font, font, font, font, font).SetCheckColor(Color.transparent);
        int checkColor = (int) typeof(Markdown).GetField("checkColor",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(markdown);
        Assert.True(checkColor == 0x1A7F37, "the check color is " + checkColor.ToString("X6"));
    }

    [Fact]
    public void HeadingLevelsSkipNone() {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        // A text that starts at ### and goes on to ##### is H1, then H2.
        Drawn drawn = Draw("### Three\n\n##### Five\n\n# One", null);
        Assert.Equal(2, Count(drawn.pdf, "H1"));
        Assert.Equal(1, Count(drawn.pdf, "H2"));
        Assert.Equal(0, Count(drawn.pdf, "H3"));
    }

    [Fact]
    public void TheTextFlowsOntoAsManyPagesAsItNeeds() {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
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
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        StringBuilder text = new StringBuilder();
        for (int i = 0; i < 80; i++) {
            text.Append("- item ").Append(i).Append('\n');
        }
        Drawn drawn = Draw(text.ToString(), null);
        Assert.True(drawn.pages.Count >= 2, drawn.pages.Count + " pages");
        Assert.Equal(1, Count(drawn.pdf, "L"));
        Assert.Equal(80, Count(drawn.pdf, "LI"));
    }

    // The font selected last before the text is drawn: the Tf of its line.
    private static String FontBefore(String content, String text) {
        int at = content.IndexOf("<" + TestSupport.Hex(text) + ">", StringComparison.Ordinal);
        Assert.True(at >= 0, text + " is not drawn");
        String font = null;
        foreach (System.Text.RegularExpressions.Match m in
                System.Text.RegularExpressions.Regex.Matches(content.Substring(0, at), @"/F\d+ [\d.]+ Tf")) {
            font = m.Value;
        }
        return font;
    }

    [Fact]
    public void ATableCellInOneEmphasisIsDrawnInItsFont() {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        // A whole cell in bold is drawn in the bold font, as the header is, so
        // that a table can mark a value; a cell of mixed emphasis stays regular.
        Drawn drawn = Draw("| N | V |\n|---|---:|\n| a | 15 |\n| b | **25** |\n| c | **7** 8 |", null);
        String content = drawn.contents[0];
        String header = FontBefore(content, "V");
        Assert.Equal(header, FontBefore(content, "25"));
        String plain = FontBefore(content, "15");
        Assert.NotEqual(header, plain);
        Assert.Equal(plain, FontBefore(content, "7 8"));
    }

    [Fact]
    public void ATableCellThatStartsWithACheckMarkIsInTheCheckColor() {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        // A cell that starts with a check mark is green, as a table of
        // features marks what each has; a header and other cells are not.
        String green = FillOf(0x1A7F37);
        String red = FillOf(0xFF0000);
        String table = "| Feature | \u2713 |\n|---|---|\n| Tables | \u2713 |\n| SVG | \u2713 with a module |\n| Charts | no |";
        String content = Draw(table, null).contents[0];
        Assert.Equal(2, content.Split(green).Length - 1);
        Assert.DoesNotContain(green, Draw("| Feature | A |\n|---|---|\n| Tables | yes |", null).contents[0]);
        // The color is the check color, which colors the task boxes too.
        content = Draw(table, null, markdown => markdown.SetCheckColor(0xFF0000)).contents[0];
        Assert.Contains(red, content);
        Assert.DoesNotContain(green, content);
    }

    // How a fill color is written: the content of a page with only that color set.
    private static String FillOf(int color) {
        Page page = new Page(new PDF(new MemoryStream()), Letter.PORTRAIT, Page.DETACHED);
        page.SetBrushColor(color);
        return TestSupport.Content(page);
    }

    // The text of k short paragraphs, p0, p1 and so on, then the lead-in, then
    // a table of the rows, the cells of row r named r1, r2 and so on: words
    // of no letters that the core fonts kern, so that each is drawn whole.
    private static string FillerAndTable(int k, string leadIn, int rows) {
        StringBuilder text = new StringBuilder();
        for (int i = 0; i < k; i++) {
            text.Append("p").Append(i).Append("\n\n");
        }
        if (leadIn != null) {
            text.Append(leadIn).Append("\n\n");
        }
        text.Append("| N | V |\n|---|---|\n");
        for (int r = 1; r <= rows; r++) {
            text.Append("| r").Append(r).Append(" | v").Append(r).Append(" |\n");
        }
        return text.ToString();
    }

    // The pages, counted from 0, that draw the text.
    private static List<int> PagesOf(Drawn drawn, string text) {
        List<int> pages = new List<int>();
        for (int i = 0; i < drawn.contents.Count; i++) {
            if (drawn.contents[i].Contains(TestSupport.Hex(text))) {
                pages.Add(i);
            }
        }
        return pages;
    }

    [Fact]
    public void ATableThatFitsOnAPageIsNotSplit() {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        // Whatever the text before it, the table of eight rows is on one
        // page, its first row and its last; at some length of the text it is
        // moved to the next page, which shows the rule at work.
        bool moved = false;
        for (int k = 0; k <= 60; k++) {
            Drawn drawn = Draw(FillerAndTable(k, null, 8), null);
            List<int> first = PagesOf(drawn, "r1");
            Assert.Equal(first, PagesOf(drawn, "r8"));
            int lastFiller = (k > 0) ? PagesOf(drawn, "p" + (k - 1))[0] : 0;
            moved |= first[0] > lastFiller;
        }
        Assert.True(moved, "the table never moved to the next page");
    }

    [Fact]
    public void AParagraphThatEndsInAColonStaysWithItsTable() {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        bool moved = false;
        for (int k = 0; k <= 60; k++) {
            Drawn drawn = Draw(FillerAndTable(k, "555:", 8), null);
            List<int> leadIn = PagesOf(drawn, "555:");
            Assert.Equal(leadIn, PagesOf(drawn, "r1"));
            Assert.Equal(leadIn, PagesOf(drawn, "r8"));
            int lastFiller = (k > 0) ? PagesOf(drawn, "p" + (k - 1))[0] : 0;
            moved |= leadIn[0] > lastFiller;
        }
        Assert.True(moved, "the lead-in never moved with its table");
    }

    // A heading over the lead-in goes with it and its table: the two went to
    // the next page and left the heading at the bottom of this one (the
    // report of the fair test, 26 October 2026).
    [Fact]
    public void AHeadingStaysWithALeadInAndItsTable() {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        bool moved = false;
        for (int k = 0; k <= 60; k++) {
            Drawn drawn = Draw(FillerAndTable(k, "## H7\n\n555:", 8), null);
            List<int> heading = PagesOf(drawn, "H7");
            Assert.True(heading.SequenceEqual(PagesOf(drawn, "555:")),
                    k + " paragraphs: the heading is apart from its lead-in");
            Assert.True(heading.SequenceEqual(PagesOf(drawn, "r8")),
                    k + " paragraphs: the heading is apart from its table");
            int lastFiller = (k > 0) ? PagesOf(drawn, "p" + (k - 1))[0] : 0;
            moved |= heading[0] > lastFiller;
        }
        Assert.True(moved, "the heading never moved with its table");
    }

    [Fact]
    public void ATableLongerThanAPageStillStartsWhereItIs() {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        Drawn drawn = Draw(FillerAndTable(3, null, 120), null);
        Assert.Equal(new List<int> {0}, PagesOf(drawn, "p2"));
        Assert.Equal(0, PagesOf(drawn, "r1")[0]);
        Assert.True(PagesOf(drawn, "r120")[0] > 0, "the long table goes on to the next pages");
    }

    [Fact]
    public void ATableIsAsWideAsTheTextWhenSet() {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        // The right-aligned header of the last column is drawn at the right
        // end of the table: near the text's right margin, 72 pt from the edge
        // of the page, only when the tables are as wide as the text.
        String text = "| N | V |\n|---|---:|\n| a | 15 |";
        float narrow = TestSupport.PositionOf(Draw(text, null).contents[0], "V")[0];
        float wide = TestSupport.PositionOf(
                Draw(text, null, markdown => markdown.SetFullWidthTables(true)).contents[0], "V")[0];
        Assert.True(narrow < 200f, "a table is as wide as its text: " + narrow);
        Assert.True(wide > 612f - 72f - 30f, "a table is as wide as the text: " + wide);
    }

    [Fact]
    public void ATableGoesOnOnTheNextPageWithItsHeaderRow() {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
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
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
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
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
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
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        Drawn drawn = Draw("3. three\n4. four", null);
        Assert.Contains(TestSupport.Hex("3."), drawn.text, StringComparison.Ordinal);
        Assert.Contains(TestSupport.Hex("4."), drawn.text, StringComparison.Ordinal);
        Assert.DoesNotContain("<" + TestSupport.Hex("1.") + ">", drawn.text, StringComparison.Ordinal);
    }

    [Fact]
    public void HtmlIsDrawnAsText() {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        Drawn drawn = Draw("<b>not bold</b>", null);
        Assert.Contains(TestSupport.Hex("<b>not"), drawn.text, StringComparison.Ordinal);
    }

    [Fact]
    public void LongCodeLinesAreCutInLinearTime() {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        StringBuilder line = new StringBuilder();
        for (int i = 0; i < 200000; i++) {
            line.Append((char) ('a' + i % 26));
        }
        Drawn drawn = null;
        long milliseconds = TestSupport.ShortestMillis(5000, () => drawn = Draw("```\n" + line + "\n```", null));
        Assert.True(drawn.pages.Count > 1);
        Assert.True(milliseconds < 5000, milliseconds + " ms");
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

    // A table of more than 64 columns, and tables of more than 100,000 cells,
    // make the text an error: the first was drawn as a paragraph of pipes, and a
    // header of 64 columns over rows of "|" made 64 cells of a row of 2 bytes,
    // 895 MB in Go from 64 KB (review of 12 October 2026, R33).
    [Fact]
    public void ATableTooWideOrTooLargeIsAnError() {
        string Repeat(string text, int count) {
            return new StringBuilder().Insert(0, text, count).ToString();
        }
        string Table(int columns, int rows) {
            return Repeat("| a ", columns) + "|\n" + Repeat("| - ", columns) + "|\n" + Repeat("|\n", rows);
        }
        void Draw(string text) {
            using IDisposable coreFonts = TestSupport.AllowCoreFonts();
            PDF pdf = new PDF(new MemoryStream());
            Font font = new Font(pdf, CoreFont.HELVETICA);
            new Markdown(font, font, font, font, font).DrawOn(pdf, text, new List<Page>(), Letter.PORTRAIT);
        }
        Draw(Table(64, 2));
        Assert.Contains("more than 64 columns", Assert.Throws<ArgumentException>(() => Draw(Table(65, 2))).Message);
        Assert.Contains("more than 100000 cells", Assert.Throws<ArgumentException>(() => Draw(Table(64, 1600))).Message);
        // A line of pipes that is no table, with no row of delimiters, is text
        Draw(Repeat("| a ", 70) + "|\n");
    }

    // A heading that starts a list item or a quote keeps a line of the text
    // after it on its page, as any other heading does: it was drawn into the
    // bottom margin, with nothing after it (review of 12 October 2026, F16).
    [Fact]
    public void AHeadingThatStartsAnItemStaysAboveTheMargin() {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        foreach (String container in new String[] {"- ", "> "}) {
            for (int lines = 15; lines < 35; lines++) {
                var text = new System.Text.StringBuilder();
                for (int i = 0; i < lines; i++) {
                    text.Append("Line\n\n");
                }
                text.Append(container).Append("# HEADING\n").Append(container).Append("\n")
                        .Append(container).Append("after\n");
                foreach (String content in Draw(text.ToString(), null).contents) {
                    if (content.Contains(TestSupport.Hex("HEADING"))) {
                        float y = TestSupport.PositionOf(content, "HEADING")[1];
                        Assert.True(y >= 72f + 12f, container + lines + ": the heading is at " + y);
                        Assert.True(content.Contains(TestSupport.Hex("after")), container + lines + ": nothing after it");
                    }
                }
            }
        }
    }
}
}
