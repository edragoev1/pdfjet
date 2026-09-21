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

    // A table of the first five rows on one page, drawn after the change.
    private static string DrawSmall(PDF pdf, Action<BigTable> change) {
        Font font = TestSupport.Helvetica(pdf);
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT)
                .SetNumberOfColumns(3).SetTableData(Header, Rows().GetRange(0, 5));
        change(table);
        List<Page> pages = Draw(table);
        Assert.Single(pages);
        return TestSupport.Content(pages[0]);
    }

    [Fact]
    public void TheShadingAndTheBorderColorsCanBeChangedOrLeftOut() {
        string defaults = DrawSmall(TestSupport.NewPDF(), table => {});
        Assert.Contains("0.94 0.94 0.94 rg\n", defaults);
        Assert.Contains("0.69 0.69 0.69 RG\n", defaults);
        Assert.Contains(" re\nf\n", defaults);     // A shaded row is one rectangle

        string colored = DrawSmall(TestSupport.NewPDF(),
                table => table.SetShadingColor(0xFF0000).SetBorderColor(new float[] {0f, 0f, 1f}));
        Assert.Contains("1 0 0 rg\n", colored);
        Assert.Contains("0 0 1 RG\n", colored);
        Assert.DoesNotContain("0.94 0.94 0.94 rg", colored);
        Assert.DoesNotContain("0.69 0.69 0.69 RG", colored);

        string plain = DrawSmall(TestSupport.NewPDF(),
                table => table.SetShadingColor(Color.transparent).SetBorderColor((float[]) null));
        Assert.DoesNotContain("\nf\n", plain);
        Assert.DoesNotContain("\nS\n", plain);
        Assert.Contains(TestSupport.Hex("n4"), plain);
    }

    [Fact]
    public void ThePaddingCanBeSetAfterTheData() {
        string content = DrawSmall(TestSupport.NewPDF(), table => table.SetPadding(10f));
        Assert.Contains("BT\n20 ", content);    // The location is 10, 10
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        ArgumentException e = Assert.Throws<ArgumentException>(
                () => new BigTable(pdf, font, font, Letter.PORTRAIT).SetPadding(-1f));
        Assert.Equal("The padding cannot be negative.", e.Message);
    }

    [Fact]
    public void TheFooterCanHaveItsOwnTextAndFontOrBeLeftOut() {
        PDF pdf = TestSupport.NewPDF();
        Font big = TestSupport.Helvetica(pdf).SetSize(20f);
        string custom = DrawSmall(pdf, table => table.SetFooter("{page}/{pages}", big));
        Assert.Contains(TestSupport.Hex("1/1"), custom);
        Assert.Contains(" 20 Tf\n", custom);

        string none = DrawSmall(TestSupport.NewPDF(), table => table.SetFooter(null, null));
        string defaults = DrawSmall(TestSupport.NewPDF(), table => {});
        Assert.StartsWith(none, defaults);
        Assert.True(defaults.Length > none.Length);
    }

    [Fact]
    public void LineBreaksInFieldsAreDrawnAsSpaces() {
        string file = tempDir.Write("breaks.csv", Encoding.UTF8.GetBytes(
                "Name,City,Total\n\"n\n0\",\"City\r\n0\",1\nn1,City 1,2\n"));
        PDF pdf1 = TestSupport.NewPDF();
        Font font1 = TestSupport.Helvetica(pdf1);
        List<Page> fromFile = Draw(new BigTable(pdf1, font1, font1, Letter.PORTRAIT)
                .SetNumberOfColumns(3).SetTableData(file, ","));

        PDF pdf2 = TestSupport.NewPDF();
        Font font2 = TestSupport.Helvetica(pdf2);
        List<string[]> rows = new List<string[]> {new string[] {"n\r0", "City\r\n0", "1"}, new string[] {"n1", "City 1", "2"}};
        List<Page> fromMemory = Draw(new BigTable(pdf2, font2, font2, Letter.PORTRAIT)
                .SetNumberOfColumns(3).SetTableData(Header, rows));

        PDF pdf3 = TestSupport.NewPDF();
        Font font3 = TestSupport.Helvetica(pdf3);
        List<string[]> spaces = new List<string[]> {new string[] {"n 0", "City 0", "1"}, new string[] {"n1", "City 1", "2"}};
        List<Page> withSpaces = Draw(new BigTable(pdf3, font3, font3, Letter.PORTRAIT)
                .SetNumberOfColumns(3).SetTableData(Header, spaces));

        Assert.Single(fromFile);
        Assert.Equal(TestSupport.Content(withSpaces[0]), TestSupport.Content(fromFile[0]));
        Assert.Equal(TestSupport.Content(withSpaces[0]), TestSupport.Content(fromMemory[0]));
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
    // The number of times the text is in the string.
    private static int Count(string str, string text) {
        return str.Split(new string[] {text}, StringSplitOptions.None).Length - 1;
    }

    [Fact]
    public void IsTaggedAsATableInAPDFUADocument() {
        // The table is one Table element over all its pages: a TR for each
        // row, and a TH for each header field the first time the header is
        // drawn or a TD for each field of a row, each holding its own text.
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        PDF pdf = new PDF(stream, Compliance.PDF_UA_1);
        pdf.SetTitle("Title");
        Font font = TestSupport.Helvetica(pdf);
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT);
        table.SetNumberOfColumns(3);
        table.SetTableData(Header, Rows());
        List<Page> pages = Draw(table);
        Assert.Equal(2, pages.Count);
        // The header that repeats on the second page is an artifact, and the
        // shading and the lines of every page are artifacts too.
        string content = TestSupport.Latin1(pages[1].GetContent());
        Assert.True(content.IndexOf(TestSupport.Hex("Name")) < content.IndexOf("BDC\n"), content);
        Assert.True(content.IndexOf("/Artifact BMC\n") < content.IndexOf(TestSupport.Hex("Name")), content);
        pdf.Complete();
        string raw = TestSupport.Latin1(stream.ToArray());
        Assert.Equal(1, Count(raw, "/S /Table\n"));
        // The 100 rows of the data, and the header row of the first page.
        Assert.Equal(101, Count(raw, "/S /TR\n"));
        Assert.Equal(3, Count(raw, "/S /TH\n"));
        Assert.Equal(300, Count(raw, "/S /TD\n"));
        // A cell holds the text itself and has no paragraph under it.
        Assert.Equal(0, Count(raw, "/S /P\n"));
        Assert.Equal(3, Count(raw, "/A <</O /Table /Scope /Column>>"));
    }

    [Fact]
    public void IsNotTaggedInADocumentThatIsNotPDFUA() {
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        PDF pdf = new PDF(stream);
        Font font = TestSupport.Helvetica(pdf);
        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT);
        table.SetNumberOfColumns(3);
        table.SetTableData(Header, Rows());
        Draw(table);
        pdf.Complete();
        string raw = TestSupport.Latin1(stream.ToArray());
        foreach (string text in new string[] {"/S /Table\n", "/S /TR\n", "BDC\n", "/Artifact BMC\n"}) {
            Assert.Equal(0, Count(raw, text));
        }
    }

}
}
