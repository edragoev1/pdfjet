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
}
}
