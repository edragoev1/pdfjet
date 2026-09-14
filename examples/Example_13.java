package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_13.java
 */
final public class Example_13 {
    public Example_13() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_13.pdf")));

        Font f1 = new Font(pdf, IBMPlexSans.Bold);
        f1.setSize(7f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.setSize(7f);

        List<List<Cell>> tableData = new ArrayList<List<Cell>>();
        BufferedReader reader = new BufferedReader(
                new InputStreamReader(new FileInputStream("data/winter-2009.txt"), "UTF-8"));
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
        table.setData(tableData, 2);
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

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_13();
        long time1 = System.currentTimeMillis();
        System.out.println("Example_13 => " + (time1 - time0) + " ms");
    }
}   // End of Example_13.java
