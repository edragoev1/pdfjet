using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_15.cs
 */
public class Example_15 {
    public Example_15() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_15.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("PDF/UA compliant PDF");

        Font f1 = new Font(pdf, IBMPlexSans.Bold);
        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        Font f3 = new Font(pdf, IBMPlexSans.Regular);
        Font f4 = new Font(pdf, IBMPlexSans.Bold);
        Font f5 = new Font(pdf, IBMPlexSans.Regular);

        List<List<Cell>> tableData = new List<List<Cell>>();
        List<Cell> row = null;
        Cell cell = null;
        for (int i = 0; i < 60; i++) {
            row = new List<Cell>();
            for (int j = 0; j < 5; j++) {
                if (i == 0) {
                    cell = new Cell(f1);
                } else {
                    cell = new Cell(f2);
                }

                cell.SetTopPadding(10f);
                cell.SetBottomPadding(10f);
                cell.SetLeftPadding(10f);
                cell.SetRightPadding(10f);

                cell.SetText("Hello " + i + " " + j);

                CompositeTextLine composite = new CompositeTextLine(0f, 0f);
                composite.SetFontSize(12.0f);
                TextLine line1 = new TextLine(f3, "H");
                TextLine line2 = new TextLine(f4, "2");
                TextLine line3 = new TextLine(f5, "O");
                line2.SetScriptPosition(ScriptPosition.SUBSCRIPT);
                composite.AddComponent(line1);
                composite.AddComponent(line2);
                composite.AddComponent(line3);

                if (i == 0 || j == 0) {
                    cell.SetCompositeTextLine(composite);
                    cell.SetBackgroundColor(Color.deepskyblue);
                } else {
                    cell.SetBackgroundColor(Color.dodgerblue);
                }
                cell.SetBorderColor(Color.lightgray);
                cell.SetTextColor(Color.black);
                row.Add(cell);
            }
            tableData.Add(row);
        }

        Table table = new Table();
        table.SetData(tableData, 2);
        table.SetBottomMargin(15f);
        table.SetLocation(70f, 30f);
        table.AutoAdjustColumnWidths();

        List<Page> pages = new List<Page>();
        table.DrawOn(pdf, pages, A4.PORTRAIT);
        for (int i = 0; i < pages.Count; i++) {
            Page page = pages[i];
            page.AddFooter(new TextLine(f1, "Page " + (i + 1) + " of " + pages.Count));
            pdf.AddPage(page);
        }

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_15();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine("Example_15 => " + (time1 - time0) + " ms");
    }
}   // End of Example_15.cs
