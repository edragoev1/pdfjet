using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_13.cs
 */
public class Example_13 {
    public Example_13() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_13.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        Font f2 = new Font(pdf, CoreFont.HELVETICA);
        f1.SetSize(7f);
        f2.SetSize(7f);

        List<List<Cell>> tableData = new List<List<Cell>>();
        StreamReader reader = new StreamReader(
                new FileStream("data/winter-2009.txt", FileMode.Open, FileAccess.Read));
        String line;
        while ((line = reader.ReadLine()) != null) {
            List<Cell> row = new List<Cell>();
            String[] columns = line.Split(new Char[] {'|'});
            for ( int i = 0; i < columns.Length; i++ ) {
                Cell cell = new Cell(f2, columns[i]);
                cell.SetTopPadding(2f);
                cell.SetBottomPadding(2f);
                cell.SetLeftPadding(2f);
                cell.SetRightPadding(2f);
                row.Add(cell);
            }
            tableData.Add(row);
        }
        reader.Close();

        Table table = new Table();
        table.SetData(tableData, Table.WITH_2_HEADER_ROWS);
        table.SetBottomMargin(15f);
        table.SetLocation(100f, 50f);
        table.RemoveLineBetweenRows(0, 1);

        Cell cell3 = table.GetCellAt(1, 1);
        cell3.SetBorder(Border.TOP, true);

        cell3 = table.GetCellAt(1, 2);
        cell3.SetBorder(Border.TOP, true);

        SetFontForRow(table, 0, f1);
        SetFontForRow(table, 1, f1);

        table.SetColumnWidths();
        List<Cell> column = table.GetColumn(7);
        for (int i = 0; i < column.Count; i++) {
            Cell cell = column[i];
            cell.SetTextAlignment(Align.CENTER);
        }

        column = table.GetColumn(4);
        for (int i = 2; i < column.Count; i++) {
            Cell cell = column[i];
            try {
                cell.SetTextAlignment(Align.CENTER);
                if (Int32.Parse( cell.GetText()) > 40) {
                    cell.SetBgColor(Color.darkseagreen);
                } else {
                    cell.SetBgColor(Color.yellow);
                }
            } catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        Cell cell2 = table.GetCellAt(0, 1);
        cell2.SetColSpan( 2 );
        cell2.SetTextAlignment(Align.CENTER);

        SetBgColorForRow(table, 0, Color.lightgray);
        SetBgColorForRow(table, 1, Color.lightgray);

        table.SetColumnWidth(3, 10);
        blankOutColumn(table, 3);

        table.SetColumnWidth(8, 10f);
        blankOutColumn(table, 8);

        List<Page> pages = new List<Page>();
        table.DrawOn(pdf, pages, A4.PORTRAIT);
        for (int i = 0; i < pages.Count; i++) {
            Page page = pages[i];
            page.AddFooter(new TextLine(f1, "Page " + (i + 1) + " of " + pages.Count));
            pdf.AddPage(page);
        }

        pdf.Complete();
    }

    public void blankOutColumn(Table table, int index) {
        List<Cell> column = table.GetColumn(index);
        for ( int i = 0; i < column.Count; i++ ) {
            Cell cell = column[i];
            cell.SetBgColor(Color.white);
            cell.SetBorder(Border.TOP, false);
            cell.SetBorder(Border.BOTTOM, false);
        }
    }

    public void SetBgColorForRow(Table table, int index, int color) {
        List<Cell> row = table.GetRow(index);
        for (int i = 0; i < row.Count; i++) {
            Cell cell = row[i];
            cell.SetBgColor(color);
        }
    }

    public void SetFontForRow(Table table, int index, Font font) {
        List<Cell> row = table.GetRow(index);
        for (int i = 0; i < row.Count; i++) {
            row[i].SetFont(font);
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
