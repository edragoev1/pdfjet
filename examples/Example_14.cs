using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_14.cs
 */
public class Example_14 {
    public Example_14() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_14.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        Font f2 = new Font(pdf, CoreFont.HELVETICA);
        f1.SetSize(7f);
        f2.SetSize(7f);

        Page page = new Page(pdf, A4.PORTRAIT);

        Table table = new Table();
        // table.SetCellMargin(10f);

        List<List<Cell>> tableData = new List<List<Cell>>();

        List<Cell> row;
        Cell cell;
        for (int i = 0; i < 5; i++) {
            row = new List<Cell>();
            for (int j = 0; j < 5; j++) {
                if (i == 0) {
                    cell = new Cell(f1);
                }
                else {
                    cell = new Cell(f2);
                }
                // WITH:
                cell.SetTopPadding(10f);
                cell.SetBottomPadding(10f);
                cell.SetLeftPadding(10f);
                cell.SetRightPadding(10f);

                cell.SetBorders(false);
                cell.SetText("Hello " + i + " " + j);
                if (i == 0) {
                    cell.SetBorder(Border.TOP, true);
                    cell.SetUnderline(true);
                    cell.SetUnderline(false);
                }
                if (i == 4) {
                    cell.SetBorder(Border.BOTTOM, true);
                }
                if (j == 0) {
                    cell.SetBorder(Border.LEFT, true);
                }
                if (j == 4) {
                    cell.SetBorder(Border.RIGHT, true);
                }
                if (i == 2 && j == 2) {
                    cell.SetBorder(Border.TOP, true);
                    cell.SetBorder(Border.BOTTOM, true);
                    cell.SetBorder(Border.LEFT, true);
                    cell.SetBorder(Border.RIGHT, true);

                    cell.SetColSpan(3);
                    cell.SetBgColor(Color.darkseagreen);
                    cell.SetLineWidth(1f);
                    cell.SetTextAlignment(Align.RIGHT);
                }

                row.Add(cell);
            }
            tableData.Add(row);
        }
        table.SetData(tableData);
        table.SetCellBordersWidth(0.2f);
        table.SetLocation(70f, 30f);
        table.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_14();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_14", time0, time1);
    }
}   // End of Example_14.cs
