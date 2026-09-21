/*
 * Example_38.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.BufferedOutputStream;
import java.io.FileOutputStream;
import java.util.Arrays;
import java.util.LinkedList;
import java.util.List;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_38.java
 *
 * Draws a table whose cells span columns and rows, and explains how. A cell
 * spans columns with setColSpan and rows with setRowSpan. The table is also a
 * check of the geometry of the cells: their backgrounds meet without gaps and
 * their borders line up.
 */
public class Example_38 {
    private Font font;

    public Example_38() throws Exception {
        BufferedOutputStream bos =
                new BufferedOutputStream(new FileOutputStream("Example_38.pdf"));

        PDF pdf = new PDF(bos);
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Table Cells That Span Rows and Columns");
        font = new Font(pdf, IBMPlexMono.Regular);
        Font f1 = new Font(pdf, IBMPlexSans.SemiBold);
        Font f2 = new Font(pdf, IBMPlexSans.Regular);

        Page page = new Page(pdf, Letter.LANDSCAPE);

        TextLine title = new TextLine(f1, "Table Cells That Span Rows and Columns");
        title.setStructureType(StructElem.H1);
        title.setFontSize(18f);
        title.setLocation(50f, 50f);
        title.drawOn(page);

        TextBlock textBlock = new TextBlock(f2,
                "The cells of this table span up to five columns and up to four rows, as "
                + "the name in each cell says: 1x3 is one column wide and three rows tall. "
                + "A cell spans columns with setColSpan and rows with setRowSpan, and draws "
                + "its text, its background and its borders once over all of them. The table "
                + "keeps its shape, so every row holds a cell for every column and the cells "
                + "a span covers are left empty. The example is also a check of the geometry "
                + "of the cells: their backgrounds meet without gaps and their borders line "
                + "up.");
        textBlock.setFontSize(11f);
        textBlock.setLineSpacing(1.3f);
        textBlock.setLocation(50f, 65f);
        textBlock.setWidth(500f);
        float[] xy = textBlock.drawOn(page);

        Table table = new Table();
        table.setTableData(createTableData());
        table.setBottomMargin(10f);
        table.setLocation(50f, xy[1] + 20f);
        table.drawOn(page);

        pdf.complete();
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
     * @throws Exception if a cell cannot be made.
     */
    private List<List<Cell>> createTableData() throws Exception {
        // The columns and the rows each cell spans, in the order a browser
        // reads the cells of the HTML above.
        int[][][] spans = {
            {{2, 2}, {2, 1}, {2, 1}, {2, 1}, {2, 1}},
            {{2, 2}, {1, 1}, {5, 1}},
            {{1, 2}, {1, 1}, {2, 2}, {1, 2}, {3, 1}},
            {{1, 1}, {1, 3}, {1, 1}, {2, 1}, {1, 2}},
            {{1, 2}, {1, 1}, {2, 1}, {4, 4}},
            {{1, 1}, {1, 3}, {1, 3}, {1, 3}},
            {{1, 2}, {1, 1}, {1, 4}},
            {{1, 1}},
            {{1, 2}, {1, 1}, {2, 1}, {2, 2}, {1, 2}, {1, 1}, {1, 1}},
            {{1, 1}, {1, 1}, {1, 1}, {1, 1}, {1, 1}},
        };
        int columns = 10;
        Cell[][] grid = new Cell[spans.length][columns];
        for (int r = 0; r < spans.length; r++) {
            int c = 0;
            for (int[] span : spans[r]) {
                // The next column that no cell of a row above spans over.
                while (c < columns && grid[r][c] != null) {
                    c++;
                }
                grid[r][c] = getCell(font, span[0], span[1], span[0] + "x" + span[1]);
                // A table keeps its shape, so every row holds a cell for every
                // column: the cells a span covers are there and are empty.
                for (int r2 = r; r2 < r + span[1] && r2 < spans.length; r2++) {
                    for (int c2 = c; c2 < c + span[0] && c2 < columns; c2++) {
                        if (grid[r2][c2] == null) {
                            grid[r2][c2] = getCell(font, 1, 1, "");
                        }
                    }
                }
                c += span[0];
            }
        }
        List<List<Cell>> rows = new LinkedList<List<Cell>>();
        for (Cell[] row : grid) {
            rows.add(new LinkedList<Cell>(Arrays.asList(row)));
        }
        return rows;
    }

    private Cell getCell(Font font, int colSpan, int rowSpan, String text) throws Exception {
        Cell cell = new Cell(font);
        cell.setColSpan(colSpan);
        cell.setRowSpan(rowSpan);
        cell.setWidth(50f);
        cell.setText(text);
        cell.setBorder(Border.TOP, true);
        cell.setBorder(Border.BOTTOM, true);
        cell.setBorder(Border.LEFT, true);
        cell.setBorder(Border.RIGHT, true);
        cell.setTextAlignment(Alignment.CENTER);
        cell.setVerticalAlignment(Alignment.CENTER);
        cell.setBackgroundColor(0xD8F0E4);     // A pastel mint
        cell.setBorderWidth(1f);
        return cell;
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_38();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_38 => %4d ms%n", time1 - time0);
    }
}   // End of Example_38.java
