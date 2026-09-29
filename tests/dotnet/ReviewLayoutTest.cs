/*
 * ReviewLayoutTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace PDFjet.NET {
/// <summary>The tests of the text, table and Markdown layout, from a review of them.</summary>
public sealed class ReviewLayoutTest : IDisposable {
    private readonly TestSupport.TempDir tempDir = new TestSupport.TempDir();

    public void Dispose() {
        tempDir.Dispose();
    }

    private static readonly string[] Header = {"Name", "City", "Total"};

    private static int Count(string text, string part) {
        int count = 0;
        for (int i = text.IndexOf(part, StringComparison.Ordinal); i != -1;
                i = text.IndexOf(part, i + part.Length, StringComparison.Ordinal)) {
            count++;
        }
        return count;
    }

    private static string Repeat(string text, int count) {
        StringBuilder buf = new StringBuilder(text.Length * count);
        for (int i = 0; i < count; i++) {
            buf.Append(text);
        }
        return buf.ToString();
    }

    private static List<List<Cell>> Rows(Font font, int count, int columns) {
        List<List<Cell>> data = new List<List<Cell>>();
        for (int r = 0; r < count; r++) {
            List<Cell> row = new List<Cell>();
            for (int c = 0; c < columns; c++) {
                row.Add(new Cell(font, columns == 1 ? "row" + r : "r" + r + "c" + c));
            }
            data.Add(row);
        }
        return data;
    }

    // The first count rows like those of the BigTable tests.
    private static IEnumerable<string[]> BigTableRows(int count) {
        for (int i = 0; i < count; i++) {
            yield return new string[] {"n" + i, "City, " + i, i + ".5"};
        }
    }

    private static PDF TaggedPDF(MemoryStream stream) {
        PDF pdf = new PDF(stream, Compliance.PDF_UA_1);
        pdf.SetTitle("Title");
        return pdf;
    }

    // The lowest baseline of the text the page draws, in the coordinates of
    // the PDF, which grow upwards from the bottom of the page.
    private static double LowestBaseline(Page page) {
        double lowest = page.height;
        foreach (Match m in Regex.Matches(TestSupport.Content(page), @"[-0-9.]+ ([-0-9.]+) Td\n")) {
            double y = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            lowest = Math.Min(lowest, y);
        }
        return lowest;
    }

    // The first record of the text, as the data file readers read it.
    private static string[] FirstRecord(string text) {
        StringReader reader = new StringReader(text);
        return Util.ReadRecord(reader.ReadLine(), reader, ",");
    }

    [Fact]
    public void TextFrameBreaksALongWordInLinearTime() {
        // The limits of time in these tests are generous, as a machine of
        // continuous integration shared with other jobs is slow now and then:
        // what was quadratic took minutes, and what is linear takes a second.
        // The rest of the word was measured whole for every row it was broken
        // into: 100,000 characters took 75 seconds.
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        TextFrame frame = new TextFrame(font, new List<String> {new String('a', 200000)});
        frame.SetLocation(50f, 50f).SetWidth(400f);
        Stopwatch watch = Stopwatch.StartNew();
        frame.DrawOn(new Page(pdf, Letter.PORTRAIT, false));
        Assert.True(watch.Elapsed.TotalSeconds < 20, "took " + watch.Elapsed);
        Assert.False(frame.HasMoreText());
    }

    [Fact]
    public void MarkdownQuotesNestedDeeplyKeepTheTextWide() {
        // Quotes nested so deeply that the text was narrower than nothing drew
        // one character on each row.
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        List<Page> pages = new List<Page>();
        Stopwatch watch = Stopwatch.StartNew();
        new Markdown(font, font, font, font, font).DrawOn(pdf, new String('>', 20000), pages, Letter.PORTRAIT);
        Assert.True(watch.Elapsed.TotalSeconds < 20, "took " + watch.Elapsed);
        Assert.True(pages.Count <= 40, pages.Count + " pages for 20,000 characters");
    }

    [Fact]
    public void ARunningSumIsAddedUpInLinearTime() {
        // The sums were added up again from the first row for every page, with
        // the numbers read again: 40,000 rows took 33 seconds.
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        int rows = 40000;
        List<List<Cell>> data = Rows(font, rows, 2);
        for (int r = 1; r < rows; r++) {
            data[r][1].SetText("1,234.50");
        }
        Table table = new Table().SetTableData(data, 1);
        table.SetNumberOfFooterRows(1);
        table.SetRunningSum(rows - 1, 1, 2);
        table.SetLocation(20f, 20f);
        List<Page> pages = new List<Page>();
        Stopwatch watch = Stopwatch.StartNew();
        table.DrawOn(pdf, pages, Letter.PORTRAIT);
        Assert.True(watch.Elapsed.TotalSeconds < 20, "took " + watch.Elapsed);
        // The footer row is the last row, which has no number of its own.
        Assert.Contains(TestSupport.Hex("49,377,531.00"), TestSupport.Content(pages[pages.Count - 1]));
    }

    [Fact]
    public void ASumReadsANumberAsRightAlignNumbersDoes() {
        // A number with a line break or a control character after it is a
        // number to IsNumber, which trims them, and was refused by NumberOf.
        foreach (string text in new string[] {"100\n", "100\u0001", " 100 ", "(100)"}) {
            long? value = Table.NumberOf(text, 0);
            Assert.Equal(text.Contains("(") ? -100L : 100L, value);
        }
    }

    [Fact]
    public void ARowSpanCountsTheRowsOfTheTable() {
        // The span of a cell over a row that wraps to four lines was written as
        // the five rows of the drawing, and not as the two rows of the table.
        MemoryStream stream = new MemoryStream();
        PDF pdf = TaggedPDF(stream);
        Font font = TestSupport.Helvetica(pdf);
        List<List<Cell>> data = new List<List<Cell>> {
            new List<Cell> {new Cell(font, "H1"), new Cell(font, "H2")},
            new List<Cell> {new Cell(font, "span").SetRowSpan(2), new Cell(font, "a note long enough to wrap to four lines")},
            new List<Cell> {new Cell(font, ""), new Cell(font, "x")},
            new List<Cell> {new Cell(font, "y"), new Cell(font, "z")},
        };
        Table table = new Table().SetTableData(data, 1);
        table.SetColumnWidth(0, 60f).SetColumnWidth(1, 60f);
        table.SetLocation(20f, 20f);
        table.DrawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.Complete();
        string raw = TestSupport.Latin1(stream.ToArray());
        MatchCollection spans = Regex.Matches(raw, @"/RowSpan \d+");
        Assert.Single(spans);
        Assert.Equal("/RowSpan 2", spans[0].Value);
        Assert.Equal(4, Count(raw, "/S /TR\n"));
    }

    [Fact]
    public void ARowThatDoesNotFitUnderAHeadingGoesToTheNextPage() {
        // The first row of the first page was drawn where it was, past the
        // bottom of the page, when it did not fit under what was above the table.
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page first = new Page(pdf, Letter.PORTRAIT, false);
        List<List<Cell>> data = Rows(font, 5, 1);
        data[1][0].SetText("one two three four five six seven eight nine ten eleven twelve");
        Table table = new Table().SetTableData(data, 1);
        table.SetColumnWidth(0, 60f);
        table.SetLocation(20f, 20f);
        table.SetBottomMargin(20f);
        table.SetFirstPageTopMargin(720f);
        List<Page> pages = new List<Page> {first};
        table.DrawOn(pdf, first, pages, Letter.PORTRAIT);
        Assert.DoesNotContain(TestSupport.Hex("one"), TestSupport.Content(first));
        Assert.Equal(2, pages.Count);
        Assert.Contains(TestSupport.Hex("twelve"), TestSupport.Content(pages[1]));
        foreach (Page page in pages) {
            Assert.True(LowestBaseline(page) >= 20.0, "text under the bottom margin");
        }
    }

    [Fact]
    public void ARowSpanTallerThanAPageIsCutBetweenItsRows() {
        // A cell that spans rows taller than a page drew them all on one page,
        // past its bottom.
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        List<List<Cell>> data = Rows(font, 80, 2);
        data[3][0].SetRowSpan(70);
        Table table = new Table().SetTableData(data, 1);
        table.SetLocation(20f, 20f);
        table.SetBottomMargin(20f);
        List<Page> pages = new List<Page>();
        table.DrawOn(pdf, pages, Letter.PORTRAIT);
        Assert.True(pages.Count >= 2, pages.Count + " pages");
        int drawn = 0;
        int spanning = 0;
        foreach (Page page in pages) {
            Assert.True(LowestBaseline(page) >= 20.0, "text under the bottom margin");
            string content = TestSupport.Content(page);
            for (int r = 1; r < 80; r++) {
                drawn += Count(content, "<" + TestSupport.Hex("r" + r + "c1") + ">");
            }
            // The spanning cell draws its text once, and its box on each page.
            spanning += Count(content, "<" + TestSupport.Hex("r3c0") + ">");
        }
        Assert.Equal(79, drawn);
        Assert.Equal(1, spanning);
    }

    [Fact]
    public void ATableWithNoRowsLeftEndsWhereItStarts() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Table table = new Table().SetTableData(Rows(font, 3, 2), 1);
        table.SetLocation(20f, 30f);
        List<Page> pages = new List<Page>();
        table.DrawOn(pdf, pages, Letter.PORTRAIT);
        float[] xy = table.DrawOn(pdf, pages, Letter.PORTRAIT);
        Assert.Single(pages);
        TestSupport.AssertXY(20f + table.GetWidth(), 30f, xy);
    }

    [Fact]
    public void AColumnSpanIsWithinTheRow() {
        // A column span of 0 hung the drawing, and one past the end of the row
        // ran past the cells of the row.
        foreach (int colspan in new int[] {0, -3, 5}) {
            PDF pdf = TestSupport.NewPDF();
            Font font = TestSupport.Helvetica(pdf);
            List<List<Cell>> data = Rows(font, 3, 3);
            foreach (List<Cell> row in data) {
                foreach (Cell cell in row) {
                    cell.SetBorders(true);
                }
            }
            data[1][1].SetColSpan(colspan);
            Table table = new Table().SetTableData(data, 1);
            table.SetLocation(20f, 20f);
            table.DrawOn(new Page(pdf, Letter.PORTRAIT));
            if (colspan < 1) {
                Assert.Equal(1, data[1][1].GetColSpan());
            }
        }
    }

    [Fact]
    public void AListItemGoesOnInTheNextFrameWithoutItsLabel() {
        // The label of an item was drawn again, as a new item, in every frame
        // the item went on into.
        MemoryStream stream = new MemoryStream();
        PDF pdf = TaggedPDF(stream);
        Font font = TestSupport.Helvetica(pdf);
        Paragraph item = new Paragraph().Add(new TextLine(font, Repeat("word ", 400)));
        item.SetListLabel(new TextLine(font, "LABEL"), 15f);
        TextFrame frame = new TextFrame(new List<Paragraph> {item});
        frame.SetLocation(50f, 50f).SetWidth(200f).SetHeight(200f);
        List<Page> pages = new List<Page>();
        frame.DrawOn(pdf, pages, Letter.PORTRAIT);
        Assert.True(pages.Count >= 2, pages.Count + " pages");
        int labels = 0;
        foreach (Page page in pages) {
            labels += Count(TestSupport.Content(page), TestSupport.Hex("LABEL"));
        }
        Assert.Equal(1, labels);
        pdf.AddPages(pages);
        pdf.Complete();
        string raw = TestSupport.Latin1(stream.ToArray());
        Assert.Equal(1, Count(raw, "/S /Lbl\n"));
        Assert.Equal(pages.Count, Count(raw, "/S /LBody\n"));
    }

    [Fact]
    public void AHeadingThatGoesOnInTheNextFrameIsOneBookmark() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = TaggedPDF(stream);
        Font font = TestSupport.Helvetica(pdf);
        Paragraph heading = new Paragraph().Add(new TextLine(font, Repeat("heading ", 200)));
        heading.SetStructureType(StructElem.H1);
        TextFrame frame = new TextFrame(new List<Paragraph> {heading});
        frame.SetLocation(50f, 50f).SetWidth(200f).SetHeight(100f);
        List<Page> pages = new List<Page>();
        frame.DrawOn(pdf, pages, Letter.PORTRAIT);
        Assert.True(pages.Count >= 2, pages.Count + " pages");
        Assert.Single(pdf.headings);
    }

    [Fact]
    public void ATextColumnDrawsTheLabelOfAListItem() {
        // The label that Paragraph.SetListLabel sets was left out by TextColumn.
        MemoryStream stream = new MemoryStream();
        PDF pdf = TaggedPDF(stream);
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        TextColumn column = new TextColumn();
        column.SetLocation(50f, 50f);
        column.SetWidth(300f);
        foreach (string text in new string[] {"first item", "second item"}) {
            Paragraph item = new Paragraph().Add(new TextLine(font, text));
            item.SetListLabel(new TextLine(font, "LABEL"), 15f);
            column.AddParagraph(item);
        }
        column.AddParagraph(new Paragraph().Add(new TextLine(font, "after the list")));
        column.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Equal(2, Count(content, TestSupport.Hex("LABEL")));
        float[] label = TestSupport.PositionOf(content, "LABEL");
        float[] text2 = TestSupport.PositionOf(content, "first");
        TestSupport.AssertNear(35f, label[0], TestSupport.DELTA, "label x");
        TestSupport.AssertNear(text2[1], label[1], TestSupport.DELTA, "label y");
        pdf.Complete();
        string raw = TestSupport.Latin1(stream.ToArray());
        Assert.Equal("1 L, 2 LI, 2 Lbl, 2 LBody, 3 P",
                Count(raw, "/S /L\n") + " L, " + Count(raw, "/S /LI\n") + " LI, "
                + Count(raw, "/S /Lbl\n") + " Lbl, " + Count(raw, "/S /LBody\n") + " LBody, "
                + Count(raw, "/S /P\n") + " P");
    }

    [Fact]
    public void ATextFrameInTheBottomHalfOfThePageNeedsAHeight() {
        // The height the frame took on each page was less than nothing, so it
        // drew all of its text on the first page, past the bottom.
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        TextFrame frame = new TextFrame(font, new List<String> {Repeat("word ", 1000)});
        frame.SetLocation(50f, 500f).SetWidth(200f);
        List<Page> pages = new List<Page>();
        string message = Assert.Throws<ArgumentException>(() => frame.DrawOn(pdf, pages, Letter.PORTRAIT)).Message;
        Assert.Equal("The text frame has no height and is in the bottom half of the page: "
                + "set its height, or put it higher on the page.", message);
        Assert.Empty(pages);
    }

    [Fact]
    public void ARecordOfManyLinesIsReadInLinearTime() {
        // Every line closes a quoted field and opens the next, and the record
        // was split again at each line.
        StringBuilder text = new StringBuilder("\"a");
        for (int i = 0; i < 5000; i++) {
            text.Append('\n').Append('x', 1000).Append("\",\"");
        }
        text.Append("\nend\"");
        Stopwatch watch = Stopwatch.StartNew();
        string[] fields = FirstRecord(text.ToString());
        Assert.True(watch.Elapsed.TotalSeconds < 20, "took " + watch.Elapsed);
        Assert.Equal(5001, fields.Length);
        Assert.Equal("a " + new String('x', 1000), fields[0]);
        Assert.Equal(" end", fields[5000]);
        // A quote after the delimiter opens a field, and one inside a field
        // that does not start with one is text.
        Assert.Equal(new string[] {"a b", "c\"d", "e f"}, FirstRecord("\"a\nb\",c\"d,\"e\nf\""));
    }

    [Fact]
    public void ABigTableRefusesAQuoteThatIsNotClosed() {
        string file = tempDir.Write("open.csv", Encoding.UTF8.GetBytes("A,B\n1,\"2\n3,4\n"));
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        string message = Assert.Throws<ArgumentException>(() =>
                new BigTable(pdf, font, font, Letter.PORTRAIT).SetNumberOfColumns(2).SetTableData(file, ",")).Message;
        Assert.Equal("A quoted field is not closed by the end of the data file: 1,\"2\n3,4", message);
    }

    [Fact]
    public void TheLinesOfADataFileEndAtACarriageReturnToo() {
        string file = tempDir.Write("cr.csv", Encoding.UTF8.GetBytes("A,B\r1,2\r\n3,4\n5,6\r"));
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Table table = new Table(font, font, file);
        Assert.Equal(4, table.GetColumn(0).Count);
        Assert.Equal("5 6", table.GetCellAt(3, 0).GetText() + " " + table.GetCellAt(3, 1).GetText());
        BigTable bigTable = new BigTable(pdf, font, font, Letter.PORTRAIT).SetNumberOfColumns(2).SetTableData(file, ",");
        bigTable.SetLocation(10f, 10f);
        bigTable.Complete();
        string content = TestSupport.Content(bigTable.GetPages()[0]);
        foreach (string text in new string[] {"A", "B", "1", "2", "3", "4", "5", "6"}) {
            Assert.Contains("<" + TestSupport.Hex(text) + ">", content);
        }
    }

    [Fact]
    public void ABigTableThatDoesNotFitOnItsFirstPageStartsOnTheNext() {
        // The header and the first row were drawn wherever the first page had
        // them start, even under its bottom margin.
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page first = new Page(pdf, Letter.PORTRAIT);
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT).SetNumberOfColumns(3)
                .SetTableData(Header, BigTableRows(5));
        table.SetFirstPage(first, first.height - 15f);
        table.SetLocation(10f, 10f);
        table.Complete();
        List<Page> pages = table.GetPages();
        Assert.Single(pages);
        Assert.NotSame(first, pages[0]);
        Assert.DoesNotContain(TestSupport.Hex(Header[0]), TestSupport.Content(first));
        Assert.Contains(TestSupport.Hex("Page 1 of 1"), TestSupport.Content(pages[0]));
    }

    [Fact]
    public void ABigTableCountsItsFirstPageAtItsOwnHeight() {
        // The pages were counted as if the first were of the size of the next.
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page first = new Page(pdf, Letter.LANDSCAPE);
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT).SetNumberOfColumns(3)
                .SetTableData(Header, BigTableRows(200));
        table.SetFirstPage(first, 100f);
        table.SetLocation(10f, 10f);
        table.Complete();
        List<Page> pages = table.GetPages();
        string want = "Page " + pages.Count + " of " + pages.Count;
        Assert.Contains(TestSupport.Hex(want), TestSupport.Content(pages[pages.Count - 1]));
    }

    [Fact]
    public void ABigTableWithoutDataDrawsNothing() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT).SetNumberOfColumns(2);
        table.Complete();
        Assert.Empty(table.GetPages());
    }

    [Fact]
    public void ABigTableCutsItsTextBetweenCodePoints() {
        // The text was cut between UTF-16 code units, which could split a
        // surrogate pair; it is cut between code points, as Go cuts runes.
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        string text = Repeat("a\U0001F600", 40);
        for (float width = 10f; width < 200f; width += 3f) {
            string fit = BigTable.Fit(text, font, width);
            Assert.EndsWith(" ...", fit);
            string kept = fit.Substring(0, fit.Length - 4);
            Assert.StartsWith(kept, text);
            Assert.False(kept.Length > 0 && Char.IsHighSurrogate(kept[kept.Length - 1]), "a surrogate pair was cut");
        }
    }

    [Fact]
    public void ATextBlockWithALeadingOfZeroDrawsItsLines() {
        // The lines that fit were the height divided by a leading of 0.
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        TextBlock block = new TextBlock(font, "one two three four five six seven eight nine ten");
        block.SetWidth(40f).SetHeight(30f).SetLineSpacing(0f);
        Assert.True(block.Layout().textLines.Length >= 5);
        float zero = 0f;
        Assert.Equal("2147483647 0 -2147483648 3", Util.SaturatingInt(1f / zero) + " "
                + Util.SaturatingInt(zero / zero) + " " + Util.SaturatingInt(-1f / zero) + " "
                + Util.SaturatingInt(3.9f));
    }

    [Fact]
    public void MarkdownCodeInAFontOfSizeZeroIsDrawn() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Font code = new Font(pdf, CoreFont.COURIER).SetSize(0f);
        List<Page> pages = new List<Page>();
        new Markdown(font, font, font, font, code).DrawOn(pdf, "```\none\ntwo\n```", pages, Letter.PORTRAIT);
        Assert.Single(pages);
    }

    [Fact]
    public void MarkdownReadsTheSourceOfAnImageTheSameWayInEveryPort() {
        string images = TestSupport.RepoPath("images");
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        Markdown markdown = new Markdown(font, font, font, font, font).SetImageDirectory(images);
        Dictionary<string, bool> sources = new Dictionary<string, bool> {
            {"linux-logo.png", true},
            {"./linux-logo.png", true},
            {".//linux-logo.png", true},
            {"linux-logo.png/", false},
            {"linux-logo.png/.", false},
            {"../images/linux-logo.png", false},
            {"x/../linux-logo.png", false},
            {"", false},
            {".", false},
            {"́/../linux-logo.png", false},
            {"/́linux-logo.png", false},
            {"c:linux-logo.png", false},
            {"linux-logo.png\\..\\x.png", false},
            {"%2E%2E/images/linux-logo.p", false},
        };
        foreach (KeyValuePair<string, bool> source in sources) {
            Assert.True((markdown.ImagePath(source.Key) != null) == source.Value, source.Key);
        }
        // An empty directory is the working directory.
        string name = "pdfjet-review-" + Guid.NewGuid().ToString("N") + ".png";
        File.WriteAllBytes(name, new byte[] {0});
        try {
            Markdown inWorkingDirectory = new Markdown(font, font, font, font, font).SetImageDirectory("");
            Assert.NotNull(inWorkingDirectory.ImagePath(name));
        } finally {
            File.Delete(name);
        }
    }
}
}
