package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

/**
 *  Example_34.java
 */
final public class Example_34 {
    public Example_34() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_34.pdf")));
        pdf.setCompliance(Compliance.PDF_A_1B);

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        Font f2 = new Font(pdf, CoreFont.HELVETICA);
        Font f3 = new Font(pdf, CoreFont.HELVETICA_BOLD_OBLIQUE);

        f1.setSize(7f);
        f2.setSize(7f);
        f3.setSize(7f);

        Table table = new Table();
        List<List<Cell>> tableData = getData(
        		"data/world-communications.txt", "|", Table.WITH_2_HEADER_ROWS, f1, f2);

        Point p1 = new Point();
        p1.setShape(Point.CIRCLE);
        p1.setRadius(2f);
        p1.setColor(Color.darkolivegreen);
        p1.setFillShape(true);
        p1.setAlignment(Align.RIGHT);
        p1.setURIAction("https://en.wikipedia.org/wiki/India");
        tableData.get(4).get(3).setPoint(p1);

        p1 = new Point();
        p1.setShape(Point.DIAMOND);
        p1.setRadius(2.5f);
        p1.setColor(Color.blue);
        p1.setFillShape(true);
        p1.setAlignment(Align.RIGHT);
        p1.setURIAction("https://en.wikipedia.org/wiki/European_Union");
        tableData.get(5).get(3).setPoint(p1);

        p1 = new Point();
        p1.setShape(Point.STAR);
        p1.setRadius(3f);
        p1.setColor(Color.red);
        p1.setFillShape(true);
        p1.setAlignment(Align.RIGHT);
        p1.setURIAction("https://en.wikipedia.org/wiki/United_States");
        tableData.get(6).get(3).setPoint(p1);

        table.setData(tableData, Table.WITH_2_HEADER_ROWS);
        table.setBottomMargin(15f);
        table.setLocation(70f, 30f);
        table.setTextColorInRow(6, Color.blue);
        table.setTextColorInRow(39, Color.red);
        table.setFontInRow(26, f3);
        table.removeLineBetweenRows(0, 1);
        table.setColumnWidths();
        table.setColumnWidth(0, 50f);
        table.setColumnWidth(3, 80f);
        table.rightAlignNumbers();

        List<Page> pages = new ArrayList<Page>();
        table.drawOn(pdf, pages, Letter.PORTRAIT);
        for (int i = 0; i < pages.size(); i++) {
            Page page = pages.get(i);
            page.addFooter(new TextLine(f1, "Page " + (i + 1) + " of " + pages.size()));
            pdf.addPage(page);
        }

        pdf.complete();
    }


    public List<List<Cell>> getData(
            String fileName,
            String delimiter,
            int numOfHeaderRows,
            Font f1,
            Font f2) throws Exception {
        List<List<Cell>> tableData = new ArrayList<List<Cell>>();

        int currentRow = 0;
        BufferedReader reader = null;
        try {
            reader = new BufferedReader(new FileReader(fileName));
            String line = null;
            while ((line = reader.readLine()) != null) {
                List<Cell> row = new ArrayList<Cell>();
                String[] cols = null;
                if (delimiter.equals("|")) {
                    cols = line.split("\\|", -1);
                } else if (delimiter.equals("\t")) {
                    cols = line.split("\t", -1);
                } else {
                    throw new Exception(
                            "Only pipes and tabs can be used as delimiters");
                }
                for (int i = 0; i < cols.length; i++) {
                    String text = cols[i].trim();
                    Cell cell = null;
                    if (currentRow < numOfHeaderRows) {
                        cell = new Cell(f1, text);
                    } else {
                        cell = new Cell(f2, text);
                    }
                    cell.setTopPadding(2f);
                    cell.setBottomPadding(2f);
                    cell.setLeftPadding(2f);
                    if (i == 3) {
                        cell.setRightPadding(10f);
                    } else {
                        cell.setRightPadding(2f);
                    }
                    row.add(cell);
                }
                tableData.add(row);
                currentRow++;
            }
        } finally {
            reader.close();
        }
        appendMissingCells(tableData, f2);

        return tableData;
    }

    private void appendMissingCells(List<List<Cell>> tableData, Font f2) {
        List<Cell> firstRow = tableData.get(0);
        int numOfColumns = firstRow.size();
        for (int i = 0; i < tableData.size(); i++) {
            List<Cell> dataRow = tableData.get(i);
            int dataRowColumns = dataRow.size();
            if (dataRowColumns < numOfColumns) {
                for (int j = 0; j < (numOfColumns - dataRowColumns); j++) {
                    dataRow.add(new Cell(f2));
                }
                dataRow.get(dataRowColumns - 1).setColSpan((numOfColumns - dataRowColumns) + 1);
            }
        }
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_34();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_34", time0, time1);
    }
}   // End of Example_34.java
