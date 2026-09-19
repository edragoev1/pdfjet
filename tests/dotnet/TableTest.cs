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
}
}
