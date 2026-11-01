/*
 * Example_21.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_21.java
 *
 * Draws two tables. The first, of reading scores, has two header rows, a
 * header cell that spans two columns, and cells with background colors. The
 * second spans its cells over columns, with setColSpan, and over rows, with
 * setRowSpan.
 */
final public class Example_21 {
    private Font font;

    public Example_21() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_21.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Table Headers and Spans");

        drawHeaderRows(pdf);
        drawSpans(pdf);

        pdf.complete();
    }

    private void drawHeaderRows(PDF pdf) throws Exception {
        Font f1 = new Font(pdf, IBMPlexSans.Bold);
        f1.setSize(7f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.setSize(7f);

        List<List<Cell>> tableData = new ArrayList<List<Cell>>();
        BufferedReader reader = new BufferedReader(
                new InputStreamReader(new FileInputStream("resources/data/winter-2009.txt"), "UTF-8"));
        String line;
        while ((line = reader.readLine()) != null) {
            List<Cell> row = new ArrayList<Cell>();
            String[] columns = line.split("\\|", -1);
            for (String column : columns) {
                row.add(new Cell(f2, column));
            }
            tableData.add(row);
        }
        reader.close();

        Table table = new Table();
        table.setTableData(tableData, 2);
        table.setLocation(100f, 50f);
        table.setBottomMargin(10f);

        table.setFontInRow(0, f1);
        table.setFontInRow(1, f1);

        table.autoAdjustColumnWidths();
        table.removeLineBetweenRows(0, 1);

        Cell cell = table.getCellAt(1, 1);
        cell.setBorder(Border.TOP, true);

        cell = table.getCellAt(1, 2);
        cell.setBorder(Border.TOP, true);

        cell = table.getCellAt(0, 1);
        cell.setColSpan(2);
        cell.setTextAlignment(Alignment.CENTER);

        List<Cell> column = table.getColumn(7);
        for (int i = 0; i < column.size(); i++) {
            cell = column.get(i);
            cell.setTextAlignment(Alignment.CENTER);
        }

        column = table.getColumn(4);
        for (int i = 2; i < column.size(); i++) {
            cell = column.get(i);
            try {
                cell.setTextAlignment(Alignment.CENTER);
                if (Integer.valueOf(cell.getText()) > 40) {
                    cell.setBackgroundColor(Color.darkseagreen);
                } else {
                    cell.setBackgroundColor(Color.yellow);
                }
            } catch (Exception e) {
            }
        }

        column = table.getColumn(2);
        for (int i = 2; i < column.size(); i++) {
            cell = column.get(i);
            try {
                if (cell.getText().equals("Smith")) {
                    cell.setUnderline(true);
                }
                if (cell.getText().equals("Bowden")) {
                    cell.setStrikeout(true);
                }
            } catch (Exception e) {
            }
        }

        column = table.getColumn(2);
        for (int i = 2; i < column.size(); i++) {
            cell = column.get(i);
            try {
                if (cell.getText().equals("Bowden")) {
                    cell.setStrikeout(false);
                }
            } catch (Exception e) {
            }
        }

        setBgColorForRow(table, 0, Color.lightgray);
        setBgColorForRow(table, 1, Color.lightgray);

        table.setColumnWidth(3, 10f);
        blankOutColumn(table, 3);

        table.setColumnWidth(8, 10f);
        blankOutColumn(table, 8);

        // A heading on the first page only, above the table: the first page is
        // made here, and the table goes on to new pages.
        Page first = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        new TextLine(f1, "Winter Reading Scores")
                .setStructureType(StructElem.H1)
                .setFontSize(16f)
                .setLocation(100f, 38f)
                .drawOn(first);
        table.setFirstPageTopMargin(50f);

        List<Page> pages = new ArrayList<Page>();
        pages.add(first);
        table.drawOn(pdf, first, pages, Letter.PORTRAIT);
        for (Page page : pages) {
            pdf.addPage(page);
        }
    }

    private void drawSpans(PDF pdf) throws Exception {
        font = new Font(pdf, CascadiaMono.Regular);
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
                + "up. The header row above them numbers the columns.");
        textBlock.setFontSize(11f);
        textBlock.setLineSpacing(1.3f);
        textBlock.setLocation(50f, 65f);
        textBlock.setWidth(500f);
        float[] xy = textBlock.drawOn(page);

        Table table = new Table();
        table.setTableData(createTableData(f1), 1);
        table.setBottomMargin(10f);
        table.setLocation(50f, xy[1] + 20f);
        table.drawOn(page);
    }

    public void blankOutColumn(Table table, int index) throws Exception {
        List<Cell> column = table.getColumn(index);
        for (Cell cell : column) {
            cell.setBackgroundColor(Color.white);
            cell.setBorder(Border.TOP, false);
            cell.setBorder(Border.BOTTOM, false);
        }
    }

    public void setBgColorForRow(Table table, int index, int color) throws Exception {
        List<Cell> row = table.getRow(index);
        for (Cell cell : row) {
            cell.setBackgroundColor(color);
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
     * @throws Exception if a cell cannot be made.
     */
    private List<List<Cell>> createTableData(Font headerFont) throws Exception {
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
        // The header row, above the cells: it numbers the columns.
        List<Cell> header = new LinkedList<Cell>();
        for (int c = 0; c < columns; c++) {
            header.add(getCell(headerFont, 1, 1, String.valueOf(c + 1)));
        }
        rows.add(header);
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
        new Example_21();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_21 => %4d ms%n", time1 - time0);
    }
}   // End of Example_21.java
