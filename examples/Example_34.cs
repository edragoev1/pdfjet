using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_34.cs
 */
public class Example_34 {
    public Example_34() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_34.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_A_1B);

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        Font f2 = new Font(pdf, CoreFont.HELVETICA);
        Font f3 = new Font(pdf, CoreFont.HELVETICA_BOLD_OBLIQUE);

        f1.SetSize(7f);
        f2.SetSize(7f);
        f3.SetSize(7f);

        Table table = new Table();
        List<List<Cell>> tableData = GetData(
        		"data/world-communications.txt", "|", Table.WITH_2_HEADER_ROWS, f1, f2);

        Point p1 = new Point();
        p1.SetShape(Point.CIRCLE);
        p1.SetRadius(2f);
        p1.SetColor(Color.darkolivegreen);
        p1.SetFillShape(true);
        p1.SetAlignment(Align.RIGHT);
        p1.SetURIAction("https://en.wikipedia.org/wiki/India");
        tableData[4][3].SetPoint(p1);

        p1 = new Point();
        p1.SetShape(Point.DIAMOND);
        p1.SetRadius(2.5f);
        p1.SetColor(Color.blue);
        p1.SetFillShape(true);
        p1.SetAlignment(Align.RIGHT);
        p1.SetURIAction("https://en.wikipedia.org/wiki/European_Union");
        tableData[5][3].SetPoint(p1);

        p1 = new Point();
        p1.SetShape(Point.STAR);
        p1.SetRadius(3f);
        p1.SetColor(Color.red);
        p1.SetFillShape(true);
        p1.SetAlignment(Align.RIGHT);
        p1.SetURIAction("https://en.wikipedia.org/wiki/United_States");
        tableData[6][3].SetPoint(p1);

        table.SetData(tableData, Table.WITH_2_HEADER_ROWS);
        table.SetBottomMargin(15f);
        table.SetLocation(70f, 30f);
        table.SetTextColorInRow(6, Color.blue);
        table.SetTextColorInRow(39, Color.red);
        table.SetFontInRow(26, f3);
        table.RemoveLineBetweenRows(0, 1);
        table.SetColumnWidths();
        table.SetColumnWidth(0, 50f);
        table.SetColumnWidth(3, 80f);
        table.RightAlignNumbers();

        List<Page> pages = new List<Page>();
        table.DrawOn(pdf, pages, Letter.PORTRAIT);
        for (int i = 0; i < pages.Count; i++) {
            Page page = pages[i];
            page.AddFooter(new TextLine(f1, "Page " + (i + 1) + " of " + pages.Count));
            pdf.AddPage(page);
        }

        pdf.Complete();
    }

    public List<List<Cell>> GetData(
            String fileName,
            String delimiter,
            int numOfHeaderRows,
            Font f1,
            Font f2) {
        List<List<Cell>> tableData = new List<List<Cell>>();

        int currentRow = 0;
        StreamReader reader = new StreamReader(fileName);
        String line = null;
        while ((line = reader.ReadLine()) != null) {
            List<Cell> row = new List<Cell>();
            String[] cols = null;
            if (delimiter.Equals("|")) {
                cols = line.Split(new Char[] {'|'});
            } else if (delimiter.Equals("\t")) {
                cols = line.Split(new Char[] {'\t'});
            } else {
                throw new Exception(
                		"Only pipes and tabs can be used as delimiters");
            }
            for (int i = 0; i < cols.Length; i++) {
                String text = cols[i].Trim();
                Cell cell = null;
                if (currentRow < numOfHeaderRows) {
                    cell = new Cell(f1, text);
                } else {
                    cell = new Cell(f2, text);
                }
                // WITH:
                cell.SetTopPadding(2f);
                cell.SetBottomPadding(2f);
                cell.SetLeftPadding(2f);
                if (i == 3) {
                    cell.SetRightPadding(10f);
                } else {
                    cell.SetRightPadding(2f);
                }
                row.Add(cell);
            }
            tableData.Add(row);
            currentRow++;
        }
        reader.Close();
        AppendMissingCells(tableData, f2);

        return tableData;
    }


    private void AppendMissingCells(List<List<Cell>> tableData, Font f2) {
        List<Cell> firstRow = tableData[0];
        int numOfColumns = firstRow.Count;
        for (int i = 0; i < tableData.Count; i++) {
            List<Cell> dataRow = tableData[i];
            int dataRowColumns = dataRow.Count;
            if (dataRowColumns < numOfColumns) {
                for (int j = 0; j < (numOfColumns - dataRowColumns); j++) {
                    dataRow.Add(new Cell(f2));
                }
                dataRow[dataRowColumns - 1].SetColSpan(
                        (numOfColumns - dataRowColumns) + 1);
            }
        }
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_34();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_34", time0, time1);
    }
}   // End of Example_34.cs
