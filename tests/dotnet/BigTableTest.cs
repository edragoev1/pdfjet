/*
 * BigTableTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace PDFjet.NET {
public sealed class BigTableTest : IDisposable {
    private readonly TestSupport.TempDir tempDir = new TestSupport.TempDir();

    public void Dispose() {
        tempDir.Dispose();
    }

    private static readonly string[] Header = {"Name", "City", "Total"};

    // A hundred rows, with a delimiter inside a quoted field, and a short row
    // in the middle, which is skipped.
    private static List<string[]> Rows() {
        List<string[]> rows = new List<string[]>();
        for (int i = 0; i < 100; i++) {
            if (i == 90) {
                rows.Add(new string[] {"short"});
            }
            rows.Add(new string[] {"n" + i, "City, " + i, i + ".5"});
        }
        return rows;
    }

    private static List<Page> Draw(BigTable table) {
        table.SetLocation(10f, 10f);
        table.Complete();
        return table.GetPages();
    }

    // The rows as a delimited file, with the fields that hold a comma quoted.
    private static string Csv() {
        StringBuilder csv = new StringBuilder("Name,City,Total\n");
        foreach (string[] row in Rows()) {
            csv.Append(row.Length == 1 ? row[0] : row[0] + ",\"" + row[1] + "\"," + row[2]).Append('\n');
        }
        return csv.ToString();
    }

    [Fact]
    public void RowsFromMemoryDrawWhatTheSameFileDraws() {
        string file = tempDir.Write("rows.csv", Encoding.UTF8.GetBytes(Csv()));

        PDF pdf1 = TestSupport.NewPDF();
        Font font1 = TestSupport.Helvetica(pdf1);
        List<Page> filePages = Draw(new BigTable(pdf1, font1, font1, Letter.PORTRAIT)
                .SetNumberOfColumns(3).SetTableData(file, ","));

        PDF pdf2 = TestSupport.NewPDF();
        Font font2 = TestSupport.Helvetica(pdf2);
        List<Page> memoryPages = Draw(new BigTable(pdf2, font2, font2, Letter.PORTRAIT)
                .SetNumberOfColumns(3).SetTableData(Header, Rows()));

        // A page releases its content when it is written, so the last page is
        // compared, which also shows the column widths of the first pass.
        Assert.Equal(2, filePages.Count);
        Assert.Equal(2, memoryPages.Count);
        string last = TestSupport.Content(memoryPages[1]);
        Assert.Equal(TestSupport.Content(filePages[1]), last);
        Assert.Contains(TestSupport.Hex("Name"), last);
        Assert.Contains(TestSupport.Hex("n99"), last);
        Assert.DoesNotContain(TestSupport.Hex("short"), last);
    }

    [Fact]
    public void ChosenColumnsAreDrawnInTheirOrder() {
        string file = tempDir.Write("rows.csv", Encoding.UTF8.GetBytes(Csv()));
        List<string[]> rows = Rows();
        rows.Insert(95, new string[] {"n95b", "x"});    // No third field, so it is skipped

        PDF pdf1 = TestSupport.NewPDF();
        Font font1 = TestSupport.Helvetica(pdf1);
        List<Page> filePages = Draw(new BigTable(pdf1, font1, font1, Letter.PORTRAIT)
                .SetColumns(2, 0).SetTableData(file, ","));
        PDF pdf2 = TestSupport.NewPDF();
        Font font2 = TestSupport.Helvetica(pdf2);
        List<Page> memoryPages = Draw(new BigTable(pdf2, font2, font2, Letter.PORTRAIT)
                .SetColumns(2, 0).SetTableData(Header, rows));

        Assert.Equal(2, memoryPages.Count);
        string last = TestSupport.Content(memoryPages[1]);
        Assert.Equal(TestSupport.Content(filePages[1]), last);
        Assert.True(last.IndexOf(TestSupport.Hex("Total")) < last.IndexOf(TestSupport.Hex("Name")));
        Assert.Contains(TestSupport.Hex("n99"), last);
        Assert.DoesNotContain(TestSupport.Hex("City"), last);
        Assert.DoesNotContain(TestSupport.Hex("n95b"), last);
    }

    [Fact]
    public void ANegativeColumnIndexIsRefused() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT);
        ArgumentException e = Assert.Throws<ArgumentException>(() => table.SetColumns(1, -1));
        Assert.Equal("A column index cannot be negative.", e.Message);
    }

    [Fact]
    public void TheRowsAreReadTwiceAndDisposed() {
        List<string[]> rows = Rows();
        int opened = 0;
        int disposed = 0;
        IEnumerable<string[]> Source() {
            opened++;
            try {
                foreach (string[] row in rows) {
                    yield return row;
                }
            } finally {
                disposed++;
            }
        }
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Draw(new BigTable(pdf, font, font, Letter.PORTRAIT).SetNumberOfColumns(3).SetTableData(Header, Source()));
        Assert.Equal(2, opened);
        Assert.Equal(2, disposed);
    }

    [Fact]
    public void AHeaderWithFewerFieldsThanColumnsIsRefused() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT).SetNumberOfColumns(4);
        ArgumentException e = Assert.Throws<ArgumentException>(() => table.SetTableData(Header, Rows()));
        Assert.Equal("The header does not have a field for every column.", e.Message);
    }
}
}
