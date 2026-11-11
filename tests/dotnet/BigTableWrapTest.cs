/*
 * BigTableWrapTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace PDFjet.NET {
// The tests of BigTable.SetWrapText, as those of bigtable_wrap_test.go.
public sealed class BigTableWrapTest {
    // Draws the rows in a table of the header's columns, with wrapping on or
    // off, and returns its pages.
    private static List<Page> WrapTable(PDF pdf, Font font, bool wrap, string[] header, List<string[]> rows,
            Action<BigTable> set) {
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT).SetNumberOfColumns(header.Length);
        table.SetTableData(header, rows);
        table.SetWrapText(wrap);
        set?.Invoke(table);
        table.SetLocation(20f, 20f);
        table.Complete();
        return table.GetPages();
    }

    // A text of n words of the random lengths.
    private static string Words(Random random, int n) {
        string[] words = new string[n];
        for (int i = 0; i < n; i++) {
            words[i] = new string((char) ('a' + random.Next(26)), 1 + random.Next(9));
        }
        return string.Join(" ", words);
    }

    private static string Repeat(string text, int count) {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < count; i++) {
            sb.Append(text);
        }
        return sb.ToString();
    }

    [Fact]
    public void WrapOnATableThatFitsDrawsTheSameAsOff() {
        // Nothing wraps, and the rows and pages are where they were, byte for byte.
        List<string> Draw(bool wrap) {
            PDF pdf = TestSupport.NewPDF();
            List<string> content = new List<string>();
            foreach (Page page in WrapTable(pdf, TestSupport.Helvetica(pdf), wrap,
                    BigTableTest.Header, BigTableTest.Rows(), null)) {
                content.Add(TestSupport.Content(page));
            }
            return content;
        }
        Assert.Equal(Draw(false), Draw(true));
    }

    [Fact]
    public void ALongFieldWrapsWholeWithinItsColumn() {
        // Its words whole and in their order, no line wider than the column,
        // and nothing cut.
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        string text = "The quick brown fox jumps over the lazy dog, again and again, until the page "
                + "is far too narrow for the whole of the sentence on one line of the table.";
        List<string[]> rows = new List<string[]> { new string[] {"1", text, "2.5"} };
        List<Page> pages = WrapTable(pdf, font, true, new string[] {"No", "Description", "Total"}, rows, null);
        List<string> drawn = BigTableTest.DrawnText(pages[0]);
        // The header, then the row's three fields, the description on lines
        Assert.True(drawn.Count >= 6, string.Join("|", drawn));
        List<string> lines = drawn.GetRange(4, drawn.Count - 6);    // Before the total and the footer
        Assert.Equal(text, string.Join(" ", lines));
        float room = Letter.PORTRAIT.GetWidth() - 40f;
        foreach (string line in lines) {
            Assert.False(line.EndsWith(" ...", StringComparison.Ordinal), "a line was cut: " + line);
            Assert.True(font.StringWidth(line) <= room, "a line is wider than the table: " + line);
        }
    }

    [Fact]
    public void AWordLongerThanItsColumnIsBroken() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        string word = Repeat("abcdefghij", 12);
        string[] lines = BigTable.Wrap(word, font, 60f);
        Assert.True(lines.Length >= 2, string.Join("|", lines));
        Assert.Equal(word, string.Join("", lines));
        foreach (string line in lines) {
            Assert.True(font.StringWidth(line) <= 60f, "a piece is wider than the column: " + line);
        }
        // A column too narrow for one character still draws one on each line
        Assert.Equal(new string[] {"a", "b", "c"}, BigTable.Wrap("abc", font, 0.5f));
        // Spaces are where lines break, not text: an empty field is one line
        Assert.Equal(new string[] {""}, BigTable.Wrap("  ", font, 60f));
        // A character is a code point: a surrogate pair is never broken in half
        string astral = Repeat("\U0001F600", 6);
        foreach (string line in BigTable.Wrap(astral, font, 0.5f)) {
            Assert.Equal("\U0001F600", line);
        }
    }

    [Fact]
    public void TheNarrowColumnsKeepTheirWidths() {
        // And the wide ones share the rest of the page equally.
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        string text = Repeat("word ", 60);
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT).SetNumberOfColumns(4);
        table.SetTableData(new string[] {"No", "A", "B", "Total"},
                new List<string[]> { new string[] {"1", text, text, "12.50"} });
        table.SetWrapText(true);
        table.SetLocation(20f, 20f);
        float[] natural = (float[]) table.widths.Clone();
        Assert.Equal(natural[0], table.textWidths[0]);
        Assert.Equal(natural[3], table.textWidths[3]);
        Assert.Equal(table.textWidths[1], table.textWidths[2]);
        Assert.True(table.textWidths[1] < natural[1], "the wide columns kept their widths");
        Assert.True(table.vertLines[4] <= Letter.PORTRAIT.GetWidth() - 20f + 0.01f,
                "the table is wider than its page: " + table.vertLines[4]);
    }

    [Fact]
    public void ThePagesAreCountedAsTheyAreDrawn() {
        // The footer of every page says how many pages there are, however the
        // rows wrap.
        for (int seed = 1; seed <= 20; seed++) {
            Random random = new Random(seed);
            int count = 40 + random.Next(120);
            List<string[]> rows = new List<string[]>();
            for (int i = 0; i < count; i++) {
                rows.Add(new string[] {i.ToString(), Words(random, random.Next(60)), Words(random, random.Next(12))});
            }
            PDF pdf = TestSupport.NewPDF();
            Font font = TestSupport.Helvetica(pdf);
            float verticalPadding = random.Next(4);
            List<Page> pages = WrapTable(pdf, font, true, new string[] {"No", "Notes", "Name"}, rows,
                    table => table.SetVerticalPadding(verticalPadding));
            string last = "Page " + pages.Count + " of " + pages.Count;
            Assert.True(TestSupport.Content(pages[pages.Count - 1]).Contains(TestSupport.Hex(last)),
                    "seed " + seed + ": " + pages.Count + " pages, and the last does not say " + last);
        }
    }

    [Fact]
    public void ARowThatDoesNotFitStartsTheNextPage() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        List<string[]> rows = new List<string[]>();
        for (int i = 0; i < 50; i++) {
            rows.Add(new string[] {i.ToString(), "short"});
        }
        rows.Add(new string[] {"tall", Repeat("several words of a long note ", 40)});
        List<Page> pages = WrapTable(pdf, font, true, new string[] {"No", "Notes"}, rows, null);
        Assert.True(pages.Count >= 2, pages.Count + " pages");
        Assert.DoesNotContain("|tall|", string.Join("|", BigTableTest.DrawnText(pages[0])));
        Assert.Contains("tall", string.Join(" ", BigTableTest.DrawnText(pages[pages.Count - 1])));
    }

    [Fact]
    public void ARowTallerThanAPageIsCutAndItsCellKeepsTheText() {
        // Cut at the lines that fit it, its last line ending in the mark, and
        // the cell of a PDF/UA document keeps the whole of its text.
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        PDF pdf = new PDF(stream, Compliance.PDF_UA_1).SetTitle("Test");
        Font font = new Font(pdf, CoreFont.HELVETICA).SetSize(12f);
        string text = Repeat("a note that runs on and on ", 400).Trim();
        List<Page> pages = WrapTable(pdf, font, true, new string[] {"No", "Notes"},
                new List<string[]> { new string[] {"1", text} }, null);
        Assert.Single(pages);
        List<string> drawn = BigTableTest.DrawnText(pages[0]);
        Assert.EndsWith(" ...", drawn[drawn.Count - 2]);
        pdf.Complete();
        string raw = TestSupport.Latin1(stream.ToArray());
        Assert.Contains("/Alt " + TestSupport.TextString(text), raw);
    }

    [Fact]
    public void TheHeaderWrapsToo() {
        // A header too long for its column wraps, and the rows start under it.
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        string[] header = {"No", "A heading of many words for a narrow column of short values"};
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT).SetNumberOfColumns(2);
        table.SetTableData(header, new List<string[]> { new string[] {"1", "x"} })
                .SetWrapText(true).SetColumnWidthsInPercent(80f, 20f);
        table.SetLocation(20f, 20f);
        Assert.True(table.headerCount >= 2, "the header did not wrap");
        table.Complete();
        List<string> drawn = BigTableTest.DrawnText(table.GetPages()[0]);
        Assert.Equal("x", drawn[drawn.Count - 2]);
    }

    [Fact]
    public void WrapReadsTheRowsThreeTimes() {
        // Once more, to count the pages: three times in all, and none of them kept.
        List<string[]> rows = BigTableTest.Rows();
        int opened = 0;
        IEnumerable<string[]> Source() {
            opened++;
            foreach (string[] row in rows) {
                yield return row;
            }
        }
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT).SetNumberOfColumns(3)
                .SetTableData(BigTableTest.Header, Source()).SetWrapText(true);
        table.SetLocation(10f, 10f);
        table.Complete();
        Assert.Equal(3, opened);
    }

    [Fact]
    public void WrapTurnedOffAgainIsAsNeverOn() {
        string text = Repeat("word ", 80);
        string Draw(Action<BigTable> set) {
            PDF pdf = TestSupport.NewPDF();
            List<Page> pages = WrapTable(pdf, TestSupport.Helvetica(pdf), false, new string[] {"No", "Notes"},
                    new List<string[]> { new string[] {"1", text}, new string[] {"2", "short"} }, set);
            return TestSupport.Content(pages[0]);
        }
        Assert.Equal(Draw(null), Draw(table => table.SetWrapText(true).SetWrapText(false)));
    }
}
}
