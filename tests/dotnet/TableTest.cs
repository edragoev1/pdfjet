/*
 * TableTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace PDFjet.NET {
public sealed class TableTest : IDisposable {
    private readonly TestSupport.TempDir tempDir = new TestSupport.TempDir();

    public void Dispose() {
        tempDir.Dispose();
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

    [Fact]
    public void MeasuringAndDrawingReturnTheSameCorner() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Table table = new Table().SetTableData(Rows(font, 5, 3), 1).SetLocation(20f, 20f);
        TestSupport.AssertXY(245f, 109.36f, table.DrawOn((Page) null));
        TestSupport.AssertXY(245f, 109.36f, table.DrawOn(new Page(pdf, Letter.PORTRAIT)));
    }

    [Fact]
    public void MeasuringFirstStillDrawsEveryRowOnThePage() {
        PDF pdf = TestSupport.NewPDF();
        Table table = new Table().SetTableData(Rows(TestSupport.Helvetica(pdf), 60, 1), 1).SetLocation(20f, 20f);
        table.DrawOn((Page) null);
        Page page = new Page(pdf, Letter.PORTRAIT);
        table.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Contains(TestSupport.Hex("row0"), content);
        Assert.Contains(TestSupport.Hex("row1"), content);
        Assert.Equal(42, table.GetRowsRendered());
    }

    [Fact]
    public void HeaderRowsRepeatOnEveryPage() {
        PDF pdf = TestSupport.NewPDF();
        Table table = new Table().SetTableData(Rows(TestSupport.Helvetica(pdf), 60, 1), 1).SetLocation(20f, 20f);
        List<Page> pages = new List<Page>();
        TestSupport.AssertXY(95f, 341.696f, table.DrawOn(pdf, pages, Letter.PORTRAIT));
        Assert.Equal(2, pages.Count);
        string first = TestSupport.Content(pages[0]);
        string second = TestSupport.Content(pages[1]);
        Assert.True(first.Contains(TestSupport.Hex("row0")) && second.Contains(TestSupport.Hex("row0")), "header");
        Assert.True(first.Contains(TestSupport.Hex("row1")) && !second.Contains(TestSupport.Hex("row1")), "row1");
        Assert.True(!first.Contains(TestSupport.Hex("row59")) && second.Contains(TestSupport.Hex("row59")), "row59");
    }

    [Fact]
    public void TheFileConstructorReadsQuotedFields() {
        string file = tempDir.Write("quoted.csv", Encoding.UTF8.GetBytes(
                "\"Name\",\"Note\",\"Amount\"\n"
                + "\"Smith, John\",\"said \"\"hi\"\"\",\"1,200\"\n"
                + "Plain,,7\n"));
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        Table table = new Table(font, font, file);
        Assert.Equal(3, table.GetRow(0).Count);
        Assert.Equal("Name", table.GetCellAt(0, 0).GetText());
        Assert.Equal("Smith, John", table.GetCellAt(1, 0).GetText());
        Assert.Equal("said \"hi\"", table.GetCellAt(1, 1).GetText());
        Assert.Equal("1,200", table.GetCellAt(1, 2).GetText());
        Assert.Equal(3, table.GetRow(2).Count);
        Assert.Equal("Plain", table.GetCellAt(2, 0).GetText());
        Assert.Equal("", table.GetCellAt(2, 1).GetText());
        Assert.Equal("7", table.GetCellAt(2, 2).GetText());
    }

    [Fact]
    public void ARowTallerThanThePageIsDrawnRatherThanAskedForForever() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        StringBuilder text = new StringBuilder();
        for (int i = 0; i < 200; i++) {
            text.Append("word").Append(i).Append(' ');
        }
        List<List<Cell>> data = new List<List<Cell>>();
        data.Add(new List<Cell> { new Cell(font, "header") });
        Cell tall = new Cell(font);
        tall.SetTextBlock(new TextBlock(font, text.ToString())).SetWidth(70f);
        data.Add(new List<Cell> { tall });
        Table table = new Table().SetTableData(data, 1).SetLocation(50f, 50f).SetBottomMargin(20f);
        Assert.True(tall.GetHeight(66f) > 792f);      // taller than a Letter page
        List<Page> pages = new List<Page>();
        table.DrawOn(pdf, pages, Letter.PORTRAIT);    // asked for pages forever before
        Assert.Single(pages);
        Assert.Equal(-1, table.GetRowsRendered());
        Assert.Contains(TestSupport.Hex("word0"), TestSupport.Content(pages[0]));
    }

    [Fact]
    public void TheFileConstructorDropsAByteOrderMarkAndPadsShortRows() {
        string file = tempDir.Write("table.txt", Encoding.UTF8.GetBytes("﻿a|b|c\n1||\n2\n"));
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        Table table = new Table(font, font, file);
        Assert.Equal("a", table.GetCellAt(0, 0).GetText());
        Assert.Equal("c", table.GetCellAt(0, 2).GetText());
        Assert.Equal(3, table.GetRow(1).Count);
        Assert.Equal("1", table.GetCellAt(1, 0).GetText());
        Assert.Equal("", table.GetCellAt(1, 2).GetText());
        Assert.Equal(3, table.GetRow(2).Count);
        Assert.Equal("", table.GetCellAt(2, 2).GetText());
        Assert.Equal(3, table.GetColumn(0).Count);
    }

    [Fact]
    public void TheFileConstructorReadsLineBreaksInQuotedFieldsAsSpaces() {
        string file = tempDir.Write("breaks.csv", Encoding.UTF8.GetBytes(
                "Name,Address\r\n"
                + "\"Smith, John\",\"12 Main St\r\nApt 4\"\r\n"
                + "Plain,\"one\n\ntwo\"\n"));
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        Table table = new Table(font, font, file);
        Assert.Equal("12 Main St Apt 4", table.GetCellAt(1, 1).GetText());
        Assert.Equal("Plain", table.GetCellAt(2, 0).GetText());
        Assert.Equal("one  two", table.GetCellAt(2, 1).GetText());
        Assert.Equal(3, table.GetColumn(0).Count);
    }

    [Fact]
    public void GetCellAtGetRowAndGetColumnAgree() {
        Table table = new Table().SetTableData(Rows(TestSupport.Helvetica(TestSupport.NewPDF()), 4, 3), 1);
        Assert.Same(table.GetCellAt(2, 1), table.GetRow(2)[1]);
        Assert.Same(table.GetCellAt(2, 1), table.GetColumn(1)[2]);
        Assert.Equal("r2c1", table.GetCellAt(2, 1).GetText());
    }

    [Fact]
    public void RightAlignNumbersRightAlignsOnlyNumbers() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        List<List<Cell>> data = new List<List<Cell>>();
        foreach (string text in new[] {"header", "-1.5e3", "12a", "+7", "3."}) {
            data.Add(new List<Cell> {new Cell(font, text)});
        }
        Table table = new Table().SetTableData(data, 1).RightAlignNumbers();
        Assert.Equal(Alignment.RIGHT, table.GetCellAt(1, 0).GetTextAlignment());
        Assert.Equal(Alignment.LEFT, table.GetCellAt(2, 0).GetTextAlignment());
        Assert.Equal(Alignment.RIGHT, table.GetCellAt(3, 0).GetTextAlignment());
        Assert.Equal(Alignment.RIGHT, table.GetCellAt(4, 0).GetTextAlignment());
        Assert.NotEqual(Alignment.RIGHT, table.GetCellAt(0, 0).GetTextAlignment());
    }

    [Fact]
    public void AnEmptyTableHasNoWidth() {
        Assert.Equal(0f, new Table().GetWidth());
    }

    [Fact]
    public void AnEmptyTableDrawsNothing() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        TestSupport.AssertXY(20f, 30f, new Table().SetLocation(20f, 30f).DrawOn(page));
        TestSupport.AssertXY(20f, 30f, new Table().SetLocation(20f, 30f).DrawOn((Page) null));
        List<Page> pages = new List<Page>();
        TestSupport.AssertXY(20f, 30f, new Table().SetLocation(20f, 30f).DrawOn(pdf, pages, Letter.PORTRAIT));
        Assert.Empty(pages);
        Table empty = new Table().SetTableData(new List<List<Cell>>(), 1);
        empty.AutoAdjustColumnWidths().RightAlignNumbers();
        TestSupport.AssertXY(0f, 0f, empty.DrawOn(page));
        Assert.Equal("", TestSupport.Content(page));
    }

    [Fact]
    public void MoreHeaderRowsThanRowsDrawsTheRows() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        float[] expected = new Table().SetTableData(Rows(font, 2, 1), 1).SetLocation(20f, 20f).DrawOn((Page) null);
        float[] xy = new Table().SetTableData(Rows(font, 2, 1), 5).SetLocation(20f, 20f).DrawOn(new Page(pdf, Letter.PORTRAIT));
        TestSupport.AssertXY(expected[0], expected[1], xy);
    }

    // The number of times the text is in the string.
    private static int Count(string str, string text) {
        return str.Split(new string[] {text}, StringSplitOptions.None).Length - 1;
    }

    [Fact]
    public void ATableInAPDFUADocumentIsTaggedAsATable() {
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        PDF pdf = new PDF(stream, Compliance.PDF_UA_1);
        pdf.SetTitle("Title");
        Font font = TestSupport.Helvetica(pdf);
        List<List<Cell>> data = new List<List<Cell>>();
        data.Add(new List<Cell> {new Cell(font, "Name"), new Cell(font, "Notes")});
        data.Add(new List<Cell> {new Cell(font, "a"),
                new Cell(font, "a note long enough to wrap to four lines")});
        Cell spanned = new Cell(font, "spanned").SetColSpan(2);
        data.Add(new List<Cell> {spanned, new Cell(font, "")});
        Cell underlined = new Cell(font, "b");
        underlined.SetUnderline(true);
        data.Add(new List<Cell> {underlined, new Cell(font, "c")});
        new Table().SetTableData(data, 1).SetLocation(20f, 20f).DrawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.Complete();
        string raw = TestSupport.Latin1(stream.ToArray());
        Assert.Equal(1, Count(raw, "/S /Table\n"));
        // The lines of the wrapped note are one row and one cell.
        Assert.Equal(4, Count(raw, "/S /TR\n"));
        Assert.Equal(2, Count(raw, "/S /TH\n"));
        Assert.Equal(5, Count(raw, "/S /TD\n"));
        Assert.Equal(2, Count(raw, "/A <</O /Table /Scope /Column>>"));
        Assert.Equal(1, Count(raw, "/A <</O /Table /ColSpan 2>>"));
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(raw,
                "/S /TD\n[^\n]*\n/K \\[\\d+ 0 R \\d+ 0 R \\d+ 0 R \\d+ 0 R \\]"));
        // The text of the cells, and not the underline, is in P elements.
        Assert.Equal(10, Count(raw, "/S /P\n"));
    }

    [Fact]
    public void TheHeaderRowsOnTheNextPagesAreArtifacts() {
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        PDF pdf = new PDF(stream, Compliance.PDF_UA_1);
        pdf.SetTitle("Title");
        Table table = new Table().SetTableData(Rows(TestSupport.Helvetica(pdf), 60, 2), 1).SetLocation(20f, 20f);
        List<Page> pages = new List<Page>();
        table.DrawOn(pdf, pages, Letter.PORTRAIT);
        Assert.Equal(2, pages.Count);
        string content = TestSupport.Content(pages[1]);
        Assert.StartsWith("/Artifact BMC\n", content);
        int end = content.IndexOf("EMC\n", StringComparison.Ordinal);
        Assert.True(content.IndexOf(TestSupport.Hex("r0c1"), StringComparison.Ordinal) < end);
        Assert.True(content.IndexOf("BDC", StringComparison.Ordinal) > end);
        foreach (Page page in pages) {
            pdf.AddPage(page);
        }
        pdf.Complete();
        string raw = TestSupport.Latin1(stream.ToArray());
        Assert.Equal(1, Count(raw, "/S /Table\n"));
        Assert.Equal(60, Count(raw, "/S /TR\n"));
        Assert.Equal(2, Count(raw, "/S /TH\n"));
        Assert.Equal(118, Count(raw, "/S /TD\n"));
    }

    [Fact]
    public void ATableIsNotTaggedInADocumentThatIsNotPDFUA() {
        PDF pdf = new PDF(new System.IO.MemoryStream());
        Table table = new Table().SetTableData(Rows(TestSupport.Helvetica(pdf), 60, 2), 1).SetLocation(20f, 20f);
        List<Page> pages = new List<Page>();
        table.DrawOn(pdf, pages, Letter.PORTRAIT);
        string content = TestSupport.Content(pages[1]);
        Assert.False(content.Contains("BMC") || content.Contains("BDC") || content.Contains("EMC"), content);
    }

    [Fact]
    public void ATableWithAPageLeftOutOfTheDocumentStillHasAStructureTree() {
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        PDF pdf = new PDF(stream, Compliance.PDF_UA_1);
        pdf.SetTitle("Title");
        Table table = new Table().SetTableData(Rows(TestSupport.Helvetica(pdf), 60, 2), 1).SetLocation(20f, 20f);
        List<Page> pages = new List<Page>();
        table.DrawOn(pdf, pages, Letter.PORTRAIT);
        pdf.AddPage(pages[1]);  // The page with the Table element is left out.
        pdf.Complete();
        string raw = TestSupport.Latin1(stream.ToArray());
        Assert.DoesNotContain("/P 0 0 R", raw);
        Assert.False(raw.Contains("/K [0 0 R") || raw.Contains(" 0 0 R ]"));
    }
    // The y coordinates of the horizontal rules the page draws, top to bottom.
    private static List<float> Rules(Page page) {
        SortedSet<float> ys = new SortedSet<float>();
        foreach (System.Text.RegularExpressions.Match m in
                System.Text.RegularExpressions.Regex.Matches(TestSupport.Content(page),
                        "([-0-9.]+) ([-0-9.]+) m\n([-0-9.]+) ([-0-9.]+) l")) {
            float y1 = float.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
            float y2 = float.Parse(m.Groups[4].Value, System.Globalization.CultureInfo.InvariantCulture);
            if (Math.Abs(y1 - y2) < 0.01f) {
                ys.Add(y1);
            }
        }
        List<float> list = new List<float>(ys);
        list.Reverse();
        return list;
    }

    // A table of rows by columns of cells with every border, the cell at 0,0
    // spanning the rows.
    private static List<List<Cell>> Spanning(Font font, int rows, int columns, int rowspan) {
        List<List<Cell>> data = new List<List<Cell>>();
        for (int r = 0; r < rows; r++) {
            List<Cell> row = new List<Cell>();
            for (int c = 0; c < columns; c++) {
                Cell cell = new Cell(font, (r == 0 && c == 0) ? "spans" : (r < rowspan && c == 0) ? "" : "r" + r + "c" + c);
                cell.SetWidth(60f);
                cell.SetBorder(Border.TOP, true);
                cell.SetBorder(Border.BOTTOM, true);
                cell.SetBorder(Border.LEFT, true);
                cell.SetBorder(Border.RIGHT, true);
                row.Add(cell);
            }
            data.Add(row);
        }
        data[0][0].SetRowSpan(rowspan);
        return data;
    }

    [Fact]
    public void ACellThatSpansRowsIsDrawnOnceOverAllOfThem() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new Table().SetTableData(Spanning(font, 3, 2, 2)).SetLocation(50f, 50f).DrawOn(page);
        string content = TestSupport.Content(page);
        // The spanning cell is drawn once, and the cell it covers not at all.
        Assert.Equal(1, Count(content, TestSupport.Hex("spans")));
        Assert.Equal(0, Count(content, TestSupport.Hex("r1c0")));
        Assert.Equal(1, Count(content, TestSupport.Hex("r1c1")));
        // Its left border runs from the top of its row to the bottom of the
        // row under it, which is two rows of the three rules the table draws.
        List<float> ys = Rules(page);
        Assert.Equal(4, ys.Count);
        float rowHeight = ys[0] - ys[1];
        System.Text.RegularExpressions.Match border =
                System.Text.RegularExpressions.Regex.Match(content, "50 ([-0-9.]+) m\n50 ([-0-9.]+) l");
        Assert.True(border.Success, content);
        float top = float.Parse(border.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        float bottom = float.Parse(border.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(Math.Abs(2 * rowHeight - (top - bottom)) < 0.01f,
                "the spanning cell is not two rows tall");
    }

    [Fact]
    public void APageBreakKeepsTheRowsOfASpanTogether() {
        // The rows a cell spans go to the next page with it, so that a span is
        // never cut in two.
        foreach (int rowspan in new int[] {1, 2, 3, 4}) {
            PDF pdf = TestSupport.NewPDF();
            Font font = TestSupport.Helvetica(pdf);
            List<List<Cell>> data = Spanning(font, 60, 2, rowspan);
            // The span sits where the first page ends.
            data[0][0].SetRowSpan(1);
            data[48][0].SetRowSpan(rowspan).SetText("spans");
            for (int r = 49; r < 48 + rowspan; r++) {
                data[r][0].SetText("");
            }
            Table table = new Table().SetTableData(data).SetLocation(50f, 50f);
            table.SetBottomMargin(20f);
            List<Page> pages = new List<Page>();
            table.DrawOn(pdf, pages, Letter.PORTRAIT);
            List<string> contents = new List<string>();
            foreach (Page page in pages) {
                contents.Add(TestSupport.Content(page));
            }
            int spanPage = -1;
            for (int i = 0; i < contents.Count; i++) {
                if (Count(contents[i], TestSupport.Hex("spans")) > 0) {
                    spanPage = i;
                }
            }
            Assert.True(spanPage >= 0, "the spanning cell was not drawn");
            for (int r = 48; r < 48 + rowspan; r++) {
                Assert.True(Count(contents[spanPage], TestSupport.Hex("r" + r + "c1")) == 1,
                        "row " + r + " is not on the page of the span it belongs to");
            }
        }
    }

    [Fact]
    public void ACellThatSpansRowsSaysSoInAPDFUADocument() {
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        PDF pdf = new PDF(stream, Compliance.PDF_UA_1);
        pdf.SetTitle("Title");
        Font font = TestSupport.Helvetica(pdf);
        List<List<Cell>> data = Spanning(font, 3, 2, 2);
        data[0][0].SetColSpan(2);
        data[0][1].SetText("");
        data[1][1].SetText("");     // The second row is covered whole.
        new Table().SetTableData(data, 1).SetLocation(50f, 50f).DrawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.Complete();
        string raw = TestSupport.Latin1(stream.ToArray());
        Assert.Equal(1, Count(raw, "/A <</O /Table /Scope /Column /ColSpan 2 /RowSpan 2>>"));
        // A row that a span covers whole holds no cell of its own.
        Assert.Equal(2, Count(raw, "/S /TR\n"));
        Assert.Equal(1, Count(raw, "/S /TH\n"));
        Assert.Equal(2, Count(raw, "/S /TD\n"));
    }
    private const string LONG_TEXT =
            "one two three four five six seven eight nine ten eleven twelve";

    // A one row table of a cell that wraps and a cell that does not, each with
    // every border.
    private static List<List<Cell>> Wrapping(Font font) {
        List<Cell> row = new List<Cell>();
        foreach (string text in new string[] {LONG_TEXT, "one"}) {
            Cell cell = new Cell(font, text);
            cell.SetWidth(60f);
            cell.SetBorders(true);
            row.Add(cell);
        }
        List<List<Cell>> data = new List<List<Cell>>();
        data.Add(row);
        return data;
    }

    [Fact]
    public void ACellWhoseTextWrapsDrawsOneBorderUnderIt() {
        // The rows a table wraps the text of a cell into are one cell, so the
        // border under it is drawn once, under the last of its lines. Each of
        // those rows kept the borders of the cell, so a cell of seven lines
        // drew seven rules across itself.
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Table table = new Table().SetTableData(Wrapping(font)).SetLocation(50f, 50f);
        float[] xy = table.DrawOn(page);
        Assert.NotEqual(LONG_TEXT, table.GetRow(0)[0].GetText());
        // The table draws two rules: the one over the row and the one under it.
        List<float> ys = Rules(page);
        Assert.Equal(2, ys.Count);
        Assert.True(Math.Abs(page.height - 50f - ys[0]) < 0.01f, "the rule over the row");
        Assert.True(Math.Abs(page.height - xy[1] - ys[1]) < 0.01f, "the rule under the row");
    }

    [Fact]
    public void TheRowsACellWrapsIntoAreOneCellOfEveryBorderButTheirOwn() {
        // The left and the right borders are drawn down every row of the wrap,
        // which is what makes the rows one cell; only the top and the bottom
        // of the cell are drawn once.
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Table table = new Table().SetTableData(Wrapping(font)).SetLocation(50f, 50f);
        float[] xy = table.DrawOn(page);
        // A vertical rule of each row of the wrap, down each of the three
        // edges the two cells have between and beside them.
        SortedDictionary<string, float> down = new SortedDictionary<string, float>(StringComparer.Ordinal);
        foreach (System.Text.RegularExpressions.Match m in
                System.Text.RegularExpressions.Regex.Matches(TestSupport.Content(page),
                        "([-0-9.]+) ([-0-9.]+) m\n([-0-9.]+) ([-0-9.]+) l")) {
            if (m.Groups[1].Value == m.Groups[3].Value) {
                float y1 = float.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                float y2 = float.Parse(m.Groups[4].Value, System.Globalization.CultureInfo.InvariantCulture);
                float sum;
                down.TryGetValue(m.Groups[1].Value, out sum);
                down[m.Groups[1].Value] = sum + Math.Abs(y1 - y2);
            }
        }
        Assert.Equal(new List<string> {"110", "170", "50"}, new List<string>(down.Keys));
        float height = xy[1] - 50f;
        // The edge between the two cells is the right border of the one and
        // the left border of the other, so it is drawn twice.
        Assert.True(Math.Abs(height - down["50"]) < 0.01f, "the left edge of the row");
        Assert.True(Math.Abs(2 * height - down["110"]) < 0.01f, "the edge between the cells");
        Assert.True(Math.Abs(height - down["170"]) < 0.01f, "the right edge of the row");
    }
    // The y of every rule the page draws across the first column, in the order
    // they are drawn and with none of them left out.
    private static List<float> RulesAcrossTheFirstColumn(Page page) {
        List<float> ys = new List<float>();
        foreach (System.Text.RegularExpressions.Match m in
                System.Text.RegularExpressions.Regex.Matches(TestSupport.Content(page),
                        "50 ([-0-9.]+) m\n110 ([-0-9.]+) l")) {
            if (m.Groups[1].Value == m.Groups[2].Value) {
                ys.Add(float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));
            }
        }
        return ys;
    }

    [Fact]
    public void ACellThatWrapsAndSpansRowsDrawsItsBorderUnderTheWholeSpan() {
        // A cell that spans rows is drawn over all of them at once, so its
        // bottom border is drawn under the whole span and not at the end of
        // the rows its own text wraps into, which is where a cell that spans
        // no rows draws it.
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        List<List<Cell>> data = Spanning(font, 3, 2, 2);
        data[0][0].SetText(LONG_TEXT);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Table table = new Table().SetTableData(data).SetLocation(50f, 50f);
        float[] xy = table.DrawOn(page);
        Assert.NotEqual(LONG_TEXT, table.GetRow(0)[0].GetText());
        // The rule over the span, the one under it, the one over the row below
        // it, which is the same line drawn by that row, and the one under the
        // table. The rows the text wrapped into draw none of their own.
        List<float> ys = RulesAcrossTheFirstColumn(page);
        Assert.Equal(4, ys.Count);
        Assert.True(Math.Abs(page.height - 50f - ys[0]) < 0.01f, "the rule over the span");
        Assert.True(Math.Abs(ys[1] - ys[2]) < 0.01f, "the span and the row under it do not meet");
        Assert.True(ys[1] < ys[0] && ys[1] > ys[3],
                "the rule under the span is not between the top and the bottom of the table");
        Assert.True(Math.Abs(page.height - xy[1] - ys[3]) < 0.01f, "the rule under the table");
    }
}
}
