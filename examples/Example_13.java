package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

/**
 *  Example_13.java
 *
 */
final public class Example_13 {
    public Example_13() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_13.pdf")));

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        Font f2 = new Font(pdf, CoreFont.HELVETICA);

        f1.setSize(7f);
        f2.setSize(7f);

        List<List<Cell>> tableData = new ArrayList<List<Cell>>();
        BufferedReader reader = new BufferedReader(new FileReader("data/winter-2009.txt"));
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
        table.setData(tableData, Table.WITH_2_HEADER_ROWS);
        table.setLocation(100f, 50f);
        table.setBottomMargin(10f);

        table.setFontInRow(0, f1);
        table.setFontInRow(1, f1);

        table.setColumnWidths();
        table.removeLineBetweenRows(0, 1);

        Cell cell = table.getCellAt(1, 1);
        cell.setBorder(Border.TOP, true);

        cell = table.getCellAt(1, 2);
        cell.setBorder(Border.TOP, true);

        cell = table.getCellAt(0, 1);
        cell.setColSpan(2);
        cell.setTextAlignment(Align.CENTER);

        List<Cell> column = table.getColumnAtIndex(7);
        for (int i = 0; i < column.size(); i++) {
            cell = column.get(i);
            cell.setTextAlignment(Align.CENTER);
        }

        column = table.getColumnAtIndex(4);
        for (int i = 2; i < column.size(); i++) {
            cell = column.get(i);
            try {
                cell.setTextAlignment(Align.CENTER);
                if (Integer.valueOf(cell.getText()) > 40) {
                    cell.setBgColor(Color.darkseagreen);
                } else {
                    cell.setBgColor(Color.yellow);
                }
            } catch (Exception e) {
            }
        }

        column = table.getColumnAtIndex(2);
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

        column = table.getColumnAtIndex(2);
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

        List<Page> pages = new ArrayList<Page>();
        table.drawOn(pdf, pages, Letter.PORTRAIT);
        for (int i = 0; i < pages.size(); i++) {
            Page page = pages.get(i);
            page.addFooter(new TextLine(f1, "Page " + (i + 1) + " of " + pages.size()));
            pdf.addPage(page);
        }

        pdf.complete();
    }

    public void blankOutColumn(Table table, int index) throws Exception {
        List<Cell> column = table.getColumnAtIndex(index);
        for (Cell cell : column) {
            cell.setBgColor(Color.white);
            cell.setBorder(Border.TOP, false);
            cell.setBorder(Border.BOTTOM, false);
        }
    }

    public void setBgColorForRow(Table table, int index, int color) throws Exception {
        List<Cell> row = table.getRowAtIndex(index);
        for (Cell cell : row) {
            cell.setBgColor(color);
        }
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_13();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_13", time0, time1);
    }
}   // End of Example_13.java
