/*
 * Example_38.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.BufferedOutputStream;
import java.io.FileOutputStream;
import java.util.LinkedList;
import java.util.List;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_38.java
 *
 * Draws a table whose cells span columns and rows, and explains how. A cell
 * spans columns with setColSpan, and rows by leaving out the borders between
 * it and the cells under it. The table is also a check of the geometry of the
 * cells: their backgrounds meet without gaps and their borders line up.
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
                + "A cell spans columns with setColSpan. It spans rows by leaving out its "
                + "bottom border and the top borders of the cells under it, which continue "
                + "it and are marked with ^ or left empty. The example is also a check of "
                + "the geometry of the cells: their backgrounds meet without gaps and their "
                + "borders line up.");
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
     * This will return a 10x10 matrix. The HTML-Like table will be like:
     * <table border="solid">
     * <tr>
     * <td colspan="2" rowspan="2">2x2</td>
     * <td colspan="2">2x1</td>
     * <td colspan="2">2x1</td>
     * <td colspan="2">2x1</td>
     * <td colspan="2">2x1</td>
     * </tr>
     * <tr>
     * <td colspan="2" rowspan="2">2x2</td>
     * <td>1x1</td>
     * <td colspan="5">5x1</td>
     * </tr>
     * <tr>
     * <td rowspan="2">1x2</td>
     * <td>1x1</td>
     * <td colspan="2" rowspan="2">2x2</td>
     * <td rowspan="2">1x2</td>
     * <td colspan="3">3x1</td>
     * </tr>
     * <tr>
     * <td>1x1</td>
     * <td rowspan="3">1x3</td>
     * <td>1x1</td>
     * <td colspan="2">2x1</td>
     * <td rowspan="2">1x2</td>
     * </tr>
     * <tr>
     * <td rowspan="2">1x2</td>
     * <td>1x1</td>
     * <td colspan="2">2x1</td>
     * <td colspan="4" rowspan="4">4x4</td>
     * </tr>
     * <tr>
     * <td>1x1</td>
     * <td rowspan="3">1x3</td>
     * <td rowspan="3">1x3</td>
     * <td rowspan="3">1x3</td>
     * </tr>
     * <tr>
     * <td rowspan="2">1x2</td>
     * <td>1x1</td>
     * <td rowspan="4">1x4</td>
     * </tr>
     * <tr>
     * <td>1x1</td>
     * </tr>
     * <tr>
     * <td rowspan="2">1x2</td>
     * <td>1x1</td>
     * <td colspan="2">2x1</td>
     * <td colspan="2" rowspan="2">2x2</td>
     * <td rowspan="2">1x2</td>
     * <td>1x1</td>
     * <td>1x1</td>
     * </tr>
     * <tr>
     * <td>1x1</td>
     * <td>1x1</td>
     * <td>1x1</td>
     * <td>1x1</td>
     * <td>1x1</td>
     * </tr>
     * </table>
     *
     * @return
     * @throws Exception
     */
    private List<List<Cell>> createTableData() throws Exception {
        List<List<Cell>> rows = new LinkedList<List<Cell>>();
        for (int i = 0; i < 10; i++) {
            List<Cell> row = new LinkedList<Cell>();
            switch (i) {
            case 0:
                row.add(getCell(font, 2, "2x2", true, false));
                row.add(getCell(font, 1,    "", true, false));
                row.add(getCell(font, 2, "2x1", true, true));
                row.add(getCell(font, 1,    "", true, false));
                row.add(getCell(font, 2, "2x1", true, true));
                row.add(getCell(font, 1,    "", true, false));
                row.add(getCell(font, 2, "2x1", true, true));
                row.add(getCell(font, 1,    "", true, false));
                row.add(getCell(font, 2, "2x1", true, true));
                row.add(getCell(font, 1,    "", true, false));
                break;
            case 1:
                row.add(getCell(font, 2,   "^", false, true));
                row.add(getCell(font, 1,    "", true,  true));
                row.add(getCell(font, 2, "2x2", true,  false));
                row.add(getCell(font, 1,    "", true,  true));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 5, "5x1", true,  true));
                row.add(getCell(font, 1,    "", true,  true));
                row.add(getCell(font, 1,    "", true,  true));
                row.add(getCell(font, 1,    "", true,  true));
                row.add(getCell(font, 1,    "", true,  true));
                break;
            case 2:
                row.add(getCell(font, 1, "1x2", true,  false));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 2,   "^", false, true));
                row.add(getCell(font, 1,    "", true,  true));
                row.add(getCell(font, 2, "2x2", true,  false));
                row.add(getCell(font, 1,    "", true,  true));
                row.add(getCell(font, 3, "3x1", true,  true));
                row.add(getCell(font, 1,    "", true,  true));
                row.add(getCell(font, 1,    "", true,  true));
                row.add(getCell(font, 1, "1x1", true,  true));
                break;
            case 3:
                row.add(getCell(font, 1,   "^", false, true));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 1, "1x3", true,  false));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 2,   "^", false, true));
                row.add(getCell(font, 1,    "", true,  false));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 2, "2x1", true,  true));
                row.add(getCell(font, 1,    "", true,  false));
                row.add(getCell(font, 1, "1x2", true,  false));
                break;
            case 4:
                row.add(getCell(font, 1, "1x2", true,  false));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 1,   "^", false, false));
                row.add(getCell(font, 2, "2x1", true,  true));
                row.add(getCell(font, 1,    "", false, true));
                row.add(getCell(font, 4, "4x4", true,  false));
                row.add(getCell(font, 1,    "", false, true));
                row.add(getCell(font, 1,    "", false, true));
                row.add(getCell(font, 1,    "", false, true));
                row.add(getCell(font, 1,   "^", false, true));
                break;
            case 5:
                row.add(getCell(font, 1,   "^", false, true));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 1,   "^", false, true));
                row.add(getCell(font, 1, "1x3", true,  false));
                row.add(getCell(font, 1, "1x3", true,  false));
                row.add(getCell(font, 4,   "^", false, false));
                row.add(getCell(font, 1,    "", false, false));
                row.add(getCell(font, 1,    "", false, false));
                row.add(getCell(font, 1,    "", false, false));
                row.add(getCell(font, 1, "1x3", true,  false));
                break;
            case 6:
                row.add(getCell(font, 1, "1x2", true,  false));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 1, "1x4", true,  false));
                row.add(getCell(font, 1,   "^", false, false));
                row.add(getCell(font, 1,   "^", false, false));
                row.add(getCell(font, 4,   "^", false, false));
                row.add(getCell(font, 1,    "", false, false));
                row.add(getCell(font, 1,    "", false, false));
                row.add(getCell(font, 1,    "", false, false));
                row.add(getCell(font, 1,   "^", false, false));
                break;
            case 7:
                row.add(getCell(font, 1,   "^", false, true));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 1,   "^", false, false));
                row.add(getCell(font, 1,   "^", false, true));
                row.add(getCell(font, 1,   "^", false, true));
                row.add(getCell(font, 4,   "^", false, true));
                row.add(getCell(font, 1,    "", false, true));
                row.add(getCell(font, 1,    "", false, true));
                row.add(getCell(font, 1,    "", false, true));
                row.add(getCell(font, 1,   "^", false, true));
                break;
            case 8:
                row.add(getCell(font, 1, "1x2", true,  false));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 1,   "^", false, false));
                row.add(getCell(font, 2, "2x1", true,  true));
                row.add(getCell(font, 1,    "", true,  true));
                row.add(getCell(font, 2, "2x2", true,  false));
                row.add(getCell(font, 1,    "", true,  true));
                row.add(getCell(font, 1, "1x2", true,  false));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 1, "1x1", true,  true));
                break;
            case 9:
                row.add(getCell(font, 1,   "^", false, true));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 1,   "^", false, true));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 2,   "^", false, true));
                row.add(getCell(font, 1,    "", false, true));
                row.add(getCell(font, 1,   "^", false, true));
                row.add(getCell(font, 1, "1x1", true, true));
                row.add(getCell(font, 1, "1x1", true, true));
                break;
            }
            rows.add(row);
        }

        return rows;
    }

    private Cell getCell(
            Font font,
            int colSpan,
            String text,
            boolean topBorder,
            boolean bottomBorder) throws Exception {
        Cell cell = new Cell(font);
        cell.setColSpan(colSpan);
        cell.setWidth(50f);
        cell.setText(text);
        cell.setBorder(Border.TOP, topBorder);
        cell.setBorder(Border.BOTTOM, bottomBorder);
        cell.setTextAlignment(Alignment.CENTER);
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
