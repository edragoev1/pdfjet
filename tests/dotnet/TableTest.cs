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

    [Fact]
    public void APageBreakKeepsTheWrappedLinesOfARowTogether() {
        // The lines a cell's text wraps into are rows of their own, which a
        // page break moves to the next page together, with the other cells of
        // the row. The row is put at each place near the end of the first page.
        bool moved = false;
        for (int at = 30; at < 50; at++) {
            PDF pdf = TestSupport.NewPDF();
            Font font = TestSupport.Helvetica(pdf);
            List<List<Cell>> data = new List<List<Cell>>();
            for (int r = 0; r < 60; r++) {
                List<Cell> row = new List<Cell>();
                foreach (string text in (r == at) ? new string[] {LONG_TEXT, "beside"} : new string[] {"r" + r, "x"}) {
                    Cell cell = new Cell(font, text);
                    cell.SetWidth(60f);
                    row.Add(cell);
                }
                data.Add(row);
            }
            Table table = new Table().SetTableData(data, 1).SetLocation(50f, 50f);
            table.SetBottomMargin(20f);
            List<Page> pages = new List<Page>();
            table.DrawOn(pdf, pages, Letter.PORTRAIT);
            Assert.NotEqual(LONG_TEXT, table.GetRow(at)[0].GetText());
            int first = -1;
            int last = -1;
            int beside = -1;
            for (int i = 0; i < pages.Count; i++) {
                string content = TestSupport.Content(pages[i]);
                if (content.Contains(TestSupport.Hex("one"))) {
                    first = i;
                }
                if (content.Contains(TestSupport.Hex("twelve"))) {
                    last = i;
                }
                if (content.Contains(TestSupport.Hex("beside"))) {
                    beside = i;
                }
            }
            Assert.True(first >= 0, "the wrapped text was not drawn");
            Assert.True(first == last, "row " + at + ": the page break cut the wrapped text");
            Assert.True(first == beside, "row " + at + ": the other cell of the row is not with its lines");
            moved |= (first == 1 && TestSupport.Content(pages[0]).Contains(TestSupport.Hex("r" + (at - 1))));
        }
        Assert.True(moved, "no row was moved to the next page whole");
    }

    [Fact]
    public void TheWrappedLinesOfARowThatFitNoPageAreCutWhereThePageEnds() {
        // Moved to the next page, lines taller than a page would go past its
        // end there too, so they are drawn from where the row starts and go
        // on over the next pages.
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        StringBuilder text = new StringBuilder();
        for (int i = 0; i < 200; i++) {
            text.Append("word").Append(i).Append(' ');
        }
        List<List<Cell>> data = new List<List<Cell>>();
        foreach (string[] texts in new string[][] {
                new[] {"header", "h"}, new[] {"r1", "x"}, new[] {text.ToString().Trim(), "beside"}}) {
            List<Cell> row = new List<Cell>();
            foreach (string t in texts) {
                Cell cell = new Cell(font, t);
                cell.SetWidth(60f);
                row.Add(cell);
            }
            data.Add(row);
        }
        Table table = new Table().SetTableData(data, 1).SetLocation(50f, 50f);
        table.SetBottomMargin(20f);
        List<Page> pages = new List<Page>();
        table.DrawOn(pdf, pages, Letter.PORTRAIT);
        Assert.True(pages.Count >= 3, pages.Count + " pages");
        Assert.Equal(-1, table.GetRowsRendered());
        string firstPage = TestSupport.Content(pages[0]);
        Assert.True(firstPage.Contains(TestSupport.Hex("r1")) && firstPage.Contains(TestSupport.Hex("word0")),
                "the row does not start on the first page, after the row above it");
        Assert.Contains(TestSupport.Hex("word199"), TestSupport.Content(pages[pages.Count - 1]));
    }

    // A table of 60 rows of two cells, "r0" to "r59" and "x", whose row at
    // index has the texts given instead.
    private static List<List<Cell>> RowsWith(Font font, int index, params string[] texts) {
        List<List<Cell>> data = new List<List<Cell>>();
        for (int r = 0; r < 60; r++) {
            List<Cell> row = new List<Cell>();
            foreach (string text in (r == index) ? texts : new string[] {"r" + r, "x"}) {
                Cell cell = new Cell(font, text);
                cell.SetWidth(60f);
                row.Add(cell);
            }
            data.Add(row);
        }
        return data;
    }

    // The index of the last page that draws the text, or -1.
    private static int PageOf(List<Page> pages, string text) {
        int page = -1;
        for (int i = 0; i < pages.Count; i++) {
            if (TestSupport.Content(pages[i]).Contains(TestSupport.Hex(text))) {
                page = i;
            }
        }
        return page;
    }

    [Fact]
    public void ARowKeptWithTheNextOneGoesToThePageOfTheNextOne() {
        // A heading row, whose text wraps, is kept with the row under it: a
        // page break does not fall between them, and moves both to the next
        // page. The rows are put at each place near the end of the first page.
        bool moved = false;
        for (int at = 30; at < 50; at++) {
            PDF pdf = TestSupport.NewPDF();
            List<List<Cell>> data = RowsWith(TestSupport.Helvetica(pdf), at, LONG_TEXT, "heading");
            data[at + 1][0].SetText("follows");
            Table table = new Table().SetTableData(data, 1).SetLocation(50f, 50f);
            table.SetBottomMargin(20f);
            table.KeepRowWithNext(at);
            List<Page> pages = new List<Page>();
            table.DrawOn(pdf, pages, Letter.PORTRAIT);
            int heading = PageOf(pages, "one");
            Assert.True(heading >= 0, "the heading was not drawn");
            Assert.True(heading == PageOf(pages, "twelve"), "row " + at + ": the page break cut the heading");
            Assert.True(heading == PageOf(pages, "follows"), "row " + at + ": the heading is not with the next row");
            moved |= (heading == 1 && PageOf(pages, "r" + (at - 1)) == 0);
        }
        Assert.True(moved, "no heading was moved to the next page with the next row");
    }

    [Fact]
    public void RowsKeptWithTheNextOneOneAfterAnotherAreKeptTogether() {
        for (int at = 30; at < 50; at++) {
            PDF pdf = TestSupport.NewPDF();
            Table table = new Table().SetTableData(RowsWith(TestSupport.Helvetica(pdf), -1), 1).SetLocation(50f, 50f);
            table.SetBottomMargin(20f);
            table.KeepRowWithNext(at).KeepRowWithNext(at + 1).KeepRowWithNext(at + 2);
            List<Page> pages = new List<Page>();
            table.DrawOn(pdf, pages, Letter.PORTRAIT);
            int page = PageOf(pages, "r" + at);
            for (int r = at + 1; r <= at + 3; r++) {
                Assert.True(page == PageOf(pages, "r" + r), "row " + r + " is not with row " + at);
            }
        }
    }

    [Fact]
    public void RowsKeptTogetherThatFitNoPageAreDrawnEachOnItsOwn() {
        // Moved to the next page, rows taller than a page would go past its
        // end there too, so they are drawn from where they start as if they
        // were not kept together: the pages hold the rows they hold without
        // the marks.
        List<List<Page>> drawn = new List<List<Page>>();
        foreach (bool kept in new bool[] {false, true}) {
            PDF pdf = TestSupport.NewPDF();
            Table table = new Table().SetTableData(RowsWith(TestSupport.Helvetica(pdf), -1), 1).SetLocation(50f, 50f);
            table.SetBottomMargin(20f);
            for (int r = 1; kept && r < 59; r++) {
                table.KeepRowWithNext(r);
            }
            List<Page> pages = new List<Page>();
            table.DrawOn(pdf, pages, Letter.PORTRAIT);
            Assert.Equal(-1, table.GetRowsRendered());
            drawn.Add(pages);
        }
        Assert.Equal(2, drawn[1].Count);
        for (int r = 1; r < 60; r++) {
            Assert.True(PageOf(drawn[0], "r" + r) == PageOf(drawn[1], "r" + r), "row " + r);
        }
    }

    // The y of the baseline of the text on the page, or NaN.
    private static float YOf(Page page, string text) {
        System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(
                TestSupport.Content(page),
                "[-0-9.]+ ([-0-9.]+) Td\\n(?:/F\\d+ [0-9.]+ Tf\\n)?\\[<" + TestSupport.Hex(text) + ">\\] TJ");
        return m.Success ? float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : float.NaN;
    }

    // A table of 60 rows, a header row, the rows "r1" to "r58", and a footer
    // row with the texts given, drawn on as many pages as it needs.
    private static List<Page> WithFooter(PDF pdf, params string[] footer) {
        List<List<Cell>> data = RowsWith(TestSupport.Helvetica(pdf), 59, footer);
        Table table = new Table().SetTableData(data, 1).SetNumberOfFooterRows(1).SetLocation(50f, 50f);
        table.SetBottomMargin(20f);
        List<Page> pages = new List<Page>();
        table.DrawOn(pdf, pages, Letter.PORTRAIT);
        Assert.Equal(-1, table.GetRowsRendered());
        return pages;
    }

    [Fact]
    public void TheFooterRowsAreDrawnUnderTheLastRowOfEveryPage() {
        List<Page> pages = WithFooter(TestSupport.NewPDF(), "total", "sum");
        Assert.Equal(2, pages.Count);
        for (int r = 1; r < 59; r++) {
            int n = 0;
            foreach (Page page in pages) {
                n += Count(TestSupport.Content(page), "<" + TestSupport.Hex("r" + r) + ">");
            }
            Assert.True(n == 1, "row " + r + " is drawn " + n + " times");
        }
        float rowHeight = YOf(pages[0], "r1") - YOf(pages[0], "r2");
        for (int i = 0; i < pages.Count; i++) {
            Page page = pages[i];
            int last = 0;
            for (int r = 1; r < 59; r++) {
                if (!float.IsNaN(YOf(page, "r" + r))) {
                    last = r;
                }
            }
            TestSupport.AssertNear(rowHeight, YOf(page, "r" + last) - YOf(page, "total"), 0.02f);
            // The bottom of the footer, 4.7 under the baseline, is over the
            // bottom margin.
            Assert.True(YOf(page, "total") - 4.7f >= 20f - 0.01f, "page " + i + ": the footer is in the margin");
        }
    }

    [Fact]
    public void AFooterRowThatWrapsIsDrawnWholeOnEveryPage() {
        List<Page> pages = WithFooter(TestSupport.NewPDF(), LONG_TEXT, "sum");
        Assert.Equal(2, pages.Count);
        foreach (Page page in pages) {
            string content = TestSupport.Content(page);
            Assert.Contains(TestSupport.Hex("one"), content);
            Assert.Contains(TestSupport.Hex("twelve"), content);
            Assert.Equal(1, Count(content, TestSupport.Hex("sum")));
        }
    }

    [Fact]
    public void TheFooterRowsBeforeTheEndOfTheTableAreArtifacts() {
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        PDF pdf = new PDF(stream, Compliance.PDF_UA_1);
        pdf.SetTitle("Title");
        List<Page> pages = WithFooter(pdf, "total", "sum");
        Assert.Equal(2, pages.Count);
        for (int i = 0; i < pages.Count; i++) {
            string content = TestSupport.Content(pages[i]);
            int at = content.IndexOf(TestSupport.Hex("total"), StringComparison.Ordinal);
            string before = content.Substring(0, at);
            bool artifact = before.LastIndexOf("/Artifact BMC", StringComparison.Ordinal)
                    > before.LastIndexOf("EMC", StringComparison.Ordinal);
            Assert.True(artifact == (i < pages.Count - 1), "page " + i);
        }
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
    public void MeasuringATableWithFooterRowsReturnsTheCornerDrawingDoes() {
        PDF pdf = TestSupport.NewPDF();
        List<List<Cell>> data = Rows(TestSupport.Helvetica(pdf), 5, 3);
        data[4][0].SetText("total");
        Table table = new Table().SetTableData(data, 1).SetNumberOfFooterRows(1).SetLocation(20f, 20f);
        float[] measured = table.DrawOn((Page) null);
        Page page = new Page(pdf, Letter.PORTRAIT);
        TestSupport.AssertXY(measured[0], measured[1], table.DrawOn(page));
        TestSupport.AssertXY(245f, 109.36f, measured);
        Assert.Equal(1, Count(TestSupport.Content(page), TestSupport.Hex("total")));
    }
}
}
