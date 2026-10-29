/*
 * Example_21.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_21.cs
 *
 * Draws two tables. The first, of reading scores, has two header rows, a
 * header cell that spans two columns, and cells with background colors. The
 * second spans its cells over columns, with setColSpan, and over rows, with
 * setRowSpan.
 */
public class Example_21 {
    Font font = null;

    public Example_21() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_21.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Table Headers and Spans");

        DrawHeaderRows(pdf);
        DrawSpans(pdf);

        pdf.Complete();
    }

    private void DrawHeaderRows(PDF pdf) {
        Font f1 = new Font(pdf, IBMPlexSans.Bold);
        f1.SetSize(7f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.SetSize(7f);

        List<List<Cell>> tableData = new List<List<Cell>>();
        StreamReader reader = new StreamReader(
                new FileStream("resources/data/winter-2009.txt", FileMode.Open, FileAccess.Read));
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
        table.SetTableData(tableData, 2);
        table.SetLocation(100f, 50f);
        table.SetBottomMargin(10f);

        table.SetFontInRow(0, f1);
        table.SetFontInRow(1, f1);

        table.AutoAdjustColumnWidths();
        table.RemoveLineBetweenRows(0, 1);

        Cell cell = table.GetCellAt(1, 1);
        cell.SetBorder(Border.TOP, true);

        cell = table.GetCellAt(1, 2);
        cell.SetBorder(Border.TOP, true);

        cell = table.GetCellAt(0, 1);
        cell.SetColSpan(2);
        cell.SetTextAlignment(Alignment.CENTER);

        List<Cell> column = table.GetColumn(7);
        for (int i = 0; i < column.Count; i++) {
            cell = column[i];
            cell.SetTextAlignment(Alignment.CENTER);
        }

        column = table.GetColumn(4);
        for (int i = 2; i < column.Count; i++) {
            cell = column[i];
            try {
                cell.SetTextAlignment(Alignment.CENTER);
                if (Int32.Parse(cell.GetText()) > 40) {
                    cell.SetBackgroundColor(Color.darkseagreen);
                } else {
                    cell.SetBackgroundColor(Color.yellow);
                }
            } catch (Exception) {
            }
        }

        column = table.GetColumn(2);
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

        column = table.GetColumn(2);
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

        // A heading on the first page only, above the table: the first page is
        // made here, and the table goes on to new pages.
        Page first = new Page(pdf, Letter.PORTRAIT, false);
        new TextLine(f1, "Winter Reading Scores")
                .SetStructureType(StructElem.H1)
                .SetFontSize(16f)
                .SetLocation(100f, 38f)
                .DrawOn(first);
        table.SetFirstPageTopMargin(50f);

        List<Page> pages = new List<Page>();
        pages.Add(first);
        table.DrawOn(pdf, first, pages, Letter.PORTRAIT);
        foreach (Page page in pages) {
            pdf.AddPage(page);
        }
    }

    private void DrawSpans(PDF pdf) {
        font = new Font(pdf, CascadiaMono.Regular);
        Font f1 = new Font(pdf, IBMPlexSans.SemiBold);
        Font f2 = new Font(pdf, IBMPlexSans.Regular);

        Page page = new Page(pdf, Letter.LANDSCAPE);

        TextLine title = new TextLine(f1, "Table Cells That Span Rows and Columns");
        title.SetStructureType(StructElem.H1);
        title.SetFontSize(18f);
        title.SetLocation(50f, 50f);
        title.DrawOn(page);

        TextBlock textBlock = new TextBlock(f2,
                "The cells of this table span up to five columns and up to four rows, as "
                + "the name in each cell says: 1x3 is one column wide and three rows tall. "
                + "A cell spans columns with setColSpan and rows with setRowSpan, and draws "
                + "its text, its background and its borders once over all of them. The table "
                + "keeps its shape, so every row holds a cell for every column and the cells "
                + "a span covers are left empty. The example is also a check of the geometry "
                + "of the cells: their backgrounds meet without gaps and their borders line "
                + "up. The header row above them numbers the columns.");
        textBlock.SetFontSize(11f);
        textBlock.SetLineSpacing(1.3f);
        textBlock.SetLocation(50f, 65f);
        textBlock.SetWidth(500f);
        float[] xy = textBlock.DrawOn(page);

        Table table = new Table();
        table.SetTableData(CreateTableData(f1), 1);
        table.SetBottomMargin(10f);
        table.SetLocation(50f, xy[1] + 20f);
        table.DrawOn(page);
    }

    public void BlankOutColumn(Table table, int index) {
        List<Cell> column = table.GetColumn(index);
        foreach (Cell cell in column) {
            cell.SetBackgroundColor(Color.white);
            cell.SetBorder(Border.TOP, false);
            cell.SetBorder(Border.BOTTOM, false);
        }
    }

    public void SetBgColorForRow(Table table, int index, int color) {
        List<Cell> row = table.GetRow(index);
        foreach (Cell cell in row) {
            cell.SetBackgroundColor(color);
        }
    }

    /**
     * Returns a header row that numbers the columns, and under it the cells
     * of a 10 by 10 table whose cells span columns and rows. They are the
     * table of this HTML, cell for cell:
     * <pre>
     * &lt;table border="solid"&gt;
     * &lt;tr&gt;&lt;td colspan="2" rowspan="2"&gt;2x2&lt;/td&gt;&lt;td colspan="2"&gt;2x1&lt;/td&gt;
     *     &lt;td colspan="2"&gt;2x1&lt;/td&gt;&lt;td colspan="2"&gt;2x1&lt;/td&gt;&lt;td colspan="2"&gt;2x1&lt;/td&gt;&lt;/tr&gt;
     * &lt;tr&gt;&lt;td colspan="2" rowspan="2"&gt;2x2&lt;/td&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;td colspan="5"&gt;5x1&lt;/td&gt;&lt;/tr&gt;
     * &lt;tr&gt;&lt;td rowspan="2"&gt;1x2&lt;/td&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;td colspan="2" rowspan="2"&gt;2x2&lt;/td&gt;
     *     &lt;td rowspan="2"&gt;1x2&lt;/td&gt;&lt;td colspan="3"&gt;3x1&lt;/td&gt;&lt;/tr&gt;
     * &lt;tr&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;td rowspan="3"&gt;1x3&lt;/td&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;td colspan="2"&gt;2x1&lt;/td&gt;
     *     &lt;td rowspan="2"&gt;1x2&lt;/td&gt;&lt;/tr&gt;
     * &lt;tr&gt;&lt;td rowspan="2"&gt;1x2&lt;/td&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;td colspan="2"&gt;2x1&lt;/td&gt;
     *     &lt;td colspan="4" rowspan="4"&gt;4x4&lt;/td&gt;&lt;/tr&gt;
     * &lt;tr&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;td rowspan="3"&gt;1x3&lt;/td&gt;&lt;td rowspan="3"&gt;1x3&lt;/td&gt;
     *     &lt;td rowspan="3"&gt;1x3&lt;/td&gt;&lt;/tr&gt;
     * &lt;tr&gt;&lt;td rowspan="2"&gt;1x2&lt;/td&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;td rowspan="4"&gt;1x4&lt;/td&gt;&lt;/tr&gt;
     * &lt;tr&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;/tr&gt;
     * &lt;tr&gt;&lt;td rowspan="2"&gt;1x2&lt;/td&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;td colspan="2"&gt;2x1&lt;/td&gt;
     *     &lt;td colspan="2" rowspan="2"&gt;2x2&lt;/td&gt;&lt;td rowspan="2"&gt;1x2&lt;/td&gt;&lt;td&gt;1x1&lt;/td&gt;
     *     &lt;td&gt;1x1&lt;/td&gt;&lt;/tr&gt;
     * &lt;tr&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;/tr&gt;
     * &lt;/table&gt;
     * </pre>
     *
     * @param headerFont the font of the header row, which numbers the columns.
     * @return the rows of the table.
     */
    private List<List<Cell>> CreateTableData(Font headerFont) {
        // The columns and the rows each cell spans, in the order a browser
        // reads the cells of the HTML above.
        int[][][] spans = {
            new int[][] {new int[] {2, 2}, new int[] {2, 1}, new int[] {2, 1}, new int[] {2, 1}, new int[] {2, 1}},
            new int[][] {new int[] {2, 2}, new int[] {1, 1}, new int[] {5, 1}},
            new int[][] {new int[] {1, 2}, new int[] {1, 1}, new int[] {2, 2}, new int[] {1, 2}, new int[] {3, 1}},
            new int[][] {new int[] {1, 1}, new int[] {1, 3}, new int[] {1, 1}, new int[] {2, 1}, new int[] {1, 2}},
            new int[][] {new int[] {1, 2}, new int[] {1, 1}, new int[] {2, 1}, new int[] {4, 4}},
            new int[][] {new int[] {1, 1}, new int[] {1, 3}, new int[] {1, 3}, new int[] {1, 3}},
            new int[][] {new int[] {1, 2}, new int[] {1, 1}, new int[] {1, 4}},
            new int[][] {new int[] {1, 1}},
            new int[][] {new int[] {1, 2}, new int[] {1, 1}, new int[] {2, 1}, new int[] {2, 2}, new int[] {1, 2}, new int[] {1, 1}, new int[] {1, 1}},
            new int[][] {new int[] {1, 1}, new int[] {1, 1}, new int[] {1, 1}, new int[] {1, 1}, new int[] {1, 1}},
        };
        int columns = 10;
        Cell[][] grid = new Cell[spans.Length][];
        for (int r = 0; r < spans.Length; r++) {
            grid[r] = new Cell[columns];
        }
        for (int r = 0; r < spans.Length; r++) {
            int c = 0;
            foreach (int[] span in spans[r]) {
                // The next column that no cell of a row above spans over.
                while (c < columns && grid[r][c] != null) {
                    c++;
                }
                grid[r][c] = GetCell(font, span[0], span[1], span[0] + "x" + span[1]);
                // A table keeps its shape, so every row holds a cell for every
                // column: the cells a span covers are there and are empty.
                for (int r2 = r; r2 < r + span[1] && r2 < spans.Length; r2++) {
                    for (int c2 = c; c2 < c + span[0] && c2 < columns; c2++) {
                        if (grid[r2][c2] == null) {
                            grid[r2][c2] = GetCell(font, 1, 1, "");
                        }
                    }
                }
                c += span[0];
            }
        }
        List<List<Cell>> rows = new List<List<Cell>>();
        // The header row, above the cells: it numbers the columns.
        List<Cell> header = new List<Cell>();
        for (int c = 0; c < columns; c++) {
            header.Add(GetCell(headerFont, 1, 1, (c + 1).ToString()));
        }
        rows.Add(header);
        foreach (Cell[] row in grid) {
            rows.Add(new List<Cell>(row));
        }
        return rows;
    }

    private Cell GetCell(Font font, int colSpan, int rowSpan, String text) {
        Cell cell = new Cell(font);
        cell.SetColSpan(colSpan);
        cell.SetRowSpan(rowSpan);
        cell.SetWidth(50f);
        cell.SetText(text);
        cell.SetBorder(Border.TOP, true);
        cell.SetBorder(Border.BOTTOM, true);
        cell.SetBorder(Border.LEFT, true);
        cell.SetBorder(Border.RIGHT, true);
        cell.SetTextAlignment(Alignment.CENTER);
        cell.SetVerticalAlignment(Alignment.CENTER);
        cell.SetBackgroundColor(0xD8F0E4);     // A pastel mint
        cell.SetBorderWidth(1f);
        return cell;
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_21();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_21 => {time1 - time0,4} ms");
    }
}   // End of Example_21.cs
