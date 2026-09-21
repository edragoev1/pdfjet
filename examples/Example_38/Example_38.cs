/*
 * Example_38.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_38.cs
 *
 * Draws a table whose cells span columns and rows, and explains how. A cell
 * spans columns with setColSpan and rows with setRowSpan. The table is also a
 * check of the geometry of the cells: their backgrounds meet without gaps and
 * their borders line up.
 */
public class Example_38 {
    Font font = null;

    public Example_38() {
        BufferedStream bos = new BufferedStream(
                new FileStream("Example_38.pdf", FileMode.Create));

        PDF pdf = new PDF(bos);
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Table Cells That Span Rows and Columns");

        font = new Font(pdf, IBMPlexMono.Regular);
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
                + "up.");
        textBlock.SetFontSize(11f);
        textBlock.SetLineSpacing(1.3f);
        textBlock.SetLocation(50f, 65f);
        textBlock.SetWidth(500f);
        float[] xy = textBlock.DrawOn(page);

        Table table = new Table();
        table.SetTableData(CreateTableData());
        table.SetBottomMargin(10f);
        table.SetLocation(50f, xy[1] + 20f);
        table.DrawOn(page);

        pdf.Complete();
    }

    /**
     * Returns the cells of a 10 by 10 table whose cells span columns and
     * rows. It is the table of this HTML, cell for cell:
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
     * @return the rows of the table.
     */
    private List<List<Cell>> CreateTableData() {
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
        new Example_38();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_38 => {time1 - time0,4} ms");
    }
}   // End of Example_38.cs
