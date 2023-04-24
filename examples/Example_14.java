package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

/**
 *  Example_14.java
 */
public class Example_14 {
    public Example_14() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_14.pdf")));

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        Font f2 = new Font(pdf, CoreFont.HELVETICA);
        f1.setSize(7f);
        f2.setSize(7f);

        Page page = new Page(pdf, A4.PORTRAIT);

        Table table = new Table();
        // table.setCellMargin(10f);

        List<List<Cell>> tableData = new ArrayList<List<Cell>>();

        List<Cell> row = null;
        Cell cell = null;

        for (int i = 0; i < 5; i++) {
            row = new ArrayList<Cell>();
            for (int j = 0; j < 5; j++) {
                if (i == 0) {
                    cell = new Cell(f1);
                } else {
                    cell = new Cell(f2);
                }
                cell.setNoBorders();

                // WITH:
                cell.setTopPadding(10f);
                cell.setBottomPadding(10f);
                cell.setLeftPadding(10f);
                cell.setRightPadding(10f);

                cell.setText("Hello " + i + " " + j);
                if (i == 0) {
                    cell.setBorder(Border.TOP, true);
                    cell.setUnderline(true);
                    cell.setUnderline(false);
                }
                if (i == 4) {
                    cell.setBorder(Border.BOTTOM, true);
                }
                if (j == 0) {
                    cell.setBorder(Border.LEFT, true);
                }
                if (j == 4) {
                    cell.setBorder(Border.RIGHT, true);
                }

                if (i == 2 && j == 2) {
                    cell.setBorder(Border.TOP, true);
                    cell.setBorder(Border.BOTTOM, true);
                    cell.setBorder(Border.LEFT, true);
                    cell.setBorder(Border.RIGHT, true);

                    cell.setColSpan(3);
                    cell.setBgColor(Color.darkseagreen);
                    cell.setLineWidth(1f);
                    cell.setTextAlignment(Align.RIGHT);
                }

                row.add(cell);
            }
            tableData.add(row);
        }

        table.setData(tableData);
        table.setCellBordersWidth(0.2f);
        table.setLocation(70f, 30f);
        table.drawOn(page);

        // Must call this method before drawing the table again.
        table.resetRenderedPagesCount();
        table.setLocation(70f, 200f);
        table.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_14();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_14", time0, time1);
    }
}   // End of Example_14.java
