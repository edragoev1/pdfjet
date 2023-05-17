package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

/**
 *  Example_15.java
 */
public class Example_15 {
    public Example_15() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(
                new FileOutputStream("Example_15.pdf")), Compliance.PDF_UA);
        pdf.setTitle("PDF/UA compliant PDF");

        // We are trying to prove here that PDFjet will not embed the same font twice
        Font f1 = new Font(pdf, "fonts/OpenSans/OpenSans-Bold.ttf.stream");
        Font f2 = new Font(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream");
        Font f3 = new Font(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream");
        Font f4 = new Font(pdf, "fonts/OpenSans/OpenSans-Bold.ttf.stream");
        Font f5 = new Font(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream");

        // Page page = new Page(pdf, A4.PORTRAIT);

        List<List<Cell>> tableData = new ArrayList<List<Cell>>();
        List<Cell> row = null;
        Cell cell = null;
        for (int i = 0; i < 60; i++) {
            row = new ArrayList<Cell>();
            for (int j = 0; j < 5; j++) {
                if (i == 0) {
                    cell = new Cell(f1);
                } else {
                    cell = new Cell(f2);
                }
                // cell.setNoBorders();

                cell.setTopPadding(10f);
                cell.setBottomPadding(10f);
                cell.setLeftPadding(10f);
                cell.setRightPadding(10f);

                cell.setText("Hello " + i + " " + j);

                CompositeTextLine composite = new CompositeTextLine(0f, 0f);
                composite.setFontSize(12.0f);
                TextLine line1 = new TextLine(f3, "H");
                TextLine line2 = new TextLine(f4, "2");
                TextLine line3 = new TextLine(f5, "O");

                line2.setTextEffect(Effect.SUBSCRIPT);

                composite.addComponent(line1);
                composite.addComponent(line2);
                composite.addComponent(line3);

                if (i == 0 || j == 0) {
                    cell.setCompositeTextLine(composite);
                    cell.setBgColor(Color.deepskyblue);
                }
                else {
                    cell.setBgColor(Color.dodgerblue);
                }
                cell.setPenColor(Color.lightgray);
                cell.setBrushColor(Color.black);
                row.add(cell);
            }
            tableData.add(row);
        }

        Table table = new Table();
        table.setData(tableData, Table.DATA_HAS_2_HEADER_ROWS);
        table.setBottomMargin(15f);
        table.setLocation(70f, 30f);
        table.setColumnWidths();

        List<Page> pages = new ArrayList<Page>();
        table.drawOn(pdf, pages, A4.PORTRAIT);
        for (int i = 0; i < pages.size(); i++) {
            Page page = pages.get(i);
            page.addFooter(new TextLine(f1, "Page " + (i + 1) + " of " + pages.size()));
            pdf.addPage(page);
        }

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_15();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_15", time0, time1);
    }
}   // End of Example_15.java
