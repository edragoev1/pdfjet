using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_15.cs
 */
public class Example_15 {
    public Example_15() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_15.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        Font f2 = new Font(pdf, CoreFont.HELVETICA);
        Font f3 = new Font(pdf, CoreFont.HELVETICA);
        Font f4 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        Font f5 = new Font(pdf, CoreFont.HELVETICA);

        List<List<Cell>> tableData = new List<List<Cell>>();
        List<Cell> row = null;
        Cell cell = null;
        for (int i = 0; i < 60; i++) {
            row = new List<Cell>();
            for (int j = 0; j < 5; j++) {
                if (i == 0) {
                    cell = new Cell(f1);
                }
                else {
                    cell = new Cell(f2);
                }

                cell.SetTopPadding(10f);
                cell.SetBottomPadding(10f);
                cell.SetLeftPadding(10f);
                cell.SetRightPadding(10f);

                //  cell.SetNoBorders();
                cell.SetText("Hello " + i + " " + j);

                CompositeTextLine composite = new CompositeTextLine(0f, 0f);
                composite.SetFontSize(12.0f);
                TextLine line1 = new TextLine(f3, "H");
                TextLine line2 = new TextLine(f4, "2");
                TextLine line3 = new TextLine(f5, "O");
                line2.SetTextEffect(Effect.SUBSCRIPT);
                composite.AddComponent(line1);
                composite.AddComponent(line2);
                composite.AddComponent(line3);

                if (i == 0 || j == 0) {
                    cell.SetCompositeTextLine(composite);
                    cell.SetBgColor(Color.deepskyblue);
                }
                else {
                    cell.SetBgColor(Color.dodgerblue);
                }
                cell.SetPenColor(Color.lightgray);
                cell.SetBrushColor(Color.black);
                row.Add(cell);
            }
            tableData.Add(row);
        }

        Table table = new Table();
        table.SetData(tableData, Table.WITH_2_HEADER_ROWS);
        table.SetBottomMargin(15f);
        table.SetLocation(70f, 30f);
        table.SetColumnWidths();

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
        TextUtils.PrintDuration("Example_15", time0, time1);
    }
}   // End of Example_15.cs
