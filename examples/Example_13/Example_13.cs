using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_13.cs
 */
public class Example_13 {
    public Example_13() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_13.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        f1.SetSize(7f);

        Font f2 = new Font(pdf, CoreFont.HELVETICA);
        f2.SetSize(7f);

        List<List<Cell>> tableData = new List<List<Cell>>();
        StreamReader reader = new StreamReader(
                new FileStream("data/winter-2009.txt", FileMode.Open, FileAccess.Read));
        String line;
        while ((line = reader.ReadLine()) != null) {
            List<Cell> row = new List<Cell>();
            String[] columns = line.Split(new Char[] {'|'});
            // C# will not let this be named "column" like the Java original -
            // that name is used later in the method.
            foreach (String col in columns) {
                row.Add(new Cell(f2, col));
            }
            tableData.Add(row);
        }
        reader.Close();

        Table table = new Table();
        table.SetData(tableData, Table.WITH_2_HEADER_ROWS);
        table.SetLocation(100f, 50f);
        table.SetBottomMargin(10f);

        table.SetFontInRow(0, f1);
        table.SetFontInRow(1, f1);

        table.SetColumnWidths();
        table.RemoveLineBetweenRows(0, 1);

        Cell cell = table.GetCellAt(1, 1);
        cell.SetBorder(Border.TOP, true);

        cell = table.GetCellAt(1, 2);
        cell.SetBorder(Border.TOP, true);

        cell = table.GetCellAt(0, 1);
        cell.SetColSpan(2);
        cell.SetTextAlignment(Align.CENTER);

        List<Cell> column = table.GetColumnAtIndex(7);
        for (int i = 0; i < column.Count; i++) {
            cell = column[i];
            cell.SetTextAlignment(Align.CENTER);
        }

        column = table.GetColumnAtIndex(4);
        for (int i = 2; i < column.Count; i++) {
            cell = column[i];
            try {
                cell.SetTextAlignment(Align.CENTER);
                if (Int32.Parse(cell.GetText()) > 40) {
                    cell.SetBackgroundColor(Color.darkseagreen);
                } else {
                    cell.SetBackgroundColor(Color.yellow);
                }
            } catch (Exception) {
            }
        }

        column = table.GetColumnAtIndex(2);
        for (int i = 2; i < column.Count; i++) {
            cell = column[i];
            try {
                if (cell.GetText().Equals("Smith")) {
                    cell.SetUnderline(true);
                }
                if (cell.GetText().Equals("Bowden")) {
                    cell.SetStrikeout(true);
                }
            } catch (Exception) {
            }
        }

        column = table.GetColumnAtIndex(2);
        for (int i = 2; i < column.Count; i++) {
            cell = column[i];
            try {
                if (cell.GetText().Equals("Bowden")) {
                    cell.SetStrikeout(false);
                }
            } catch (Exception) {
            }
        }

        SetBgColorForRow(table, 0, Color.lightgray);
        SetBgColorForRow(table, 1, Color.lightgray);

        table.SetColumnWidth(3, 10f);
        BlankOutColumn(table, 3);

        table.SetColumnWidth(8, 10f);
        BlankOutColumn(table, 8);

        List<Page> pages = new List<Page>();
        table.DrawOn(pdf, pages, Letter.PORTRAIT);
        for (int i = 0; i < pages.Count; i++) {
            Page page = pages[i];
            page.AddFooter(new TextLine(f1, "Page " + (i + 1) + " of " + pages.Count));
            pdf.AddPage(page);
        }

        pdf.Complete();
    }

    public void BlankOutColumn(Table table, int index) {
        List<Cell> column = table.GetColumnAtIndex(index);
        foreach (Cell cell in column) {
            cell.SetBackgroundColor(Color.white);
            cell.SetBorder(Border.TOP, false);
            cell.SetBorder(Border.BOTTOM, false);
        }
    }

    public void SetBgColorForRow(Table table, int index, int color) {
        List<Cell> row = table.GetRowAtIndex(index);
        foreach (Cell cell in row) {
            cell.SetBackgroundColor(color);
        }
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_13();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_13", time0, time1);
    }
}   // End of Example_13.cs
