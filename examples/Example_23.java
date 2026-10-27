/*
 * Example_23.java
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
 * Example_23.java
 *
 * A table of PDF/A-1a, tagged, over four pages: the life expectancy and the
 * children per woman of 217 countries and economies in 2023, from the World
 * Bank's World Development Indicators, with a marker on three of the rows,
 * each a link, and the longest and the shortest life in color.
 */
final public class Example_23 {
    public Example_23() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_23.pdf")));
        pdf.setCompliance(Compliance.PDF_A_1A);
        pdf.setTitle("Life Expectancy and Children per Woman, 2023");

        Font f1 = new Font(pdf, IBMPlexSans.Bold);
        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        Font f3 = new Font(pdf, IBMPlexSans.BoldItalic);

        f1.setSize(7f);
        f2.setSize(7f);
        f3.setSize(7f);

        Table table = new Table();
        // Country|Code|Life expectancy|Children per woman, after a header line
        List<List<Cell>> tableData = getData(
                "data/life-expectancy-and-fertility-2023.txt", "|", 1, f1, f2);

        // A marker on three rows, found by their countries, each a link
        Point p1 = new Point();
        p1.setShape(Shape.CIRCLE);
        p1.setRadius(2f);
        p1.setStrokeColor(Color.darkolivegreen);
        p1.setURIAction("https://data.worldbank.org/country/india");
        tableData.get(rowOf(tableData, "India")).get(3).setMarker(p1, Alignment.RIGHT);

        p1 = new Point();
        p1.setShape(Shape.DIAMOND);
        p1.setRadius(2.5f);
        p1.setStrokeColor(Color.blue);
        p1.setURIAction("https://data.worldbank.org/country/japan");
        tableData.get(rowOf(tableData, "Japan")).get(3).setMarker(p1, Alignment.RIGHT);

        p1 = new Point();
        p1.setShape(Shape.STAR);
        p1.setRadius(3f);
        p1.setStrokeColor(Color.red);
        p1.setURIAction("https://data.worldbank.org/country/united-states");
        tableData.get(rowOf(tableData, "United States")).get(3).setMarker(p1, Alignment.RIGHT);

        // The longest life in blue, the shortest in red, and Canada in bold italic
        int longest = 1;
        int shortest = 1;
        for (int i = 1; i < tableData.size(); i++) {
            float life = Float.parseFloat(tableData.get(i).get(2).getText());
            if (life > Float.parseFloat(tableData.get(longest).get(2).getText())) {
                longest = i;
            }
            if (life < Float.parseFloat(tableData.get(shortest).get(2).getText())) {
                shortest = i;
            }
        }

        table.setTableData(tableData, 1);
        table.setBottomMargin(26f);
        // 400 points wide, in the middle of the page
        table.setLocation(106f, 30f);
        table.setTextColorInRow(longest, Color.blue);
        table.setTextColorInRow(shortest, Color.firebrick);
        table.setFontInRow(rowOf(tableData, "Canada"), f3);
        table.setColumnWidth(0, 170f);
        table.setColumnWidth(1, 50f);
        table.setColumnWidth(2, 90f);
        table.setColumnWidth(3, 90f);
        table.rightAlignNumbers();

        List<Page> pages = new ArrayList<Page>();
        table.drawOn(pdf, pages, Letter.PORTRAIT);
        for (int i = 0; i < pages.size(); i++) {
            Page page = pages.get(i);
            page.addFooter(new TextLine(f2, "Source: World Bank, World Development Indicators, "
                    + "SP.DYN.LE00.IN and SP.DYN.TFRT.IN, 2023, licensed under CC BY 4.0. "
                    + "Page " + (i + 1) + " of " + pages.size()));
            pdf.addPage(page);
        }

        pdf.complete();
    }

    // The row of the country, in the first column.
    private static int rowOf(List<List<Cell>> tableData, String country) {
        for (int i = 0; i < tableData.size(); i++) {
            if (tableData.get(i).get(0).getText().equals(country)) {
                return i;
            }
        }
        throw new IllegalArgumentException(country + " is not in the table");
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
            reader = new BufferedReader(
                    new InputStreamReader(new FileInputStream(fileName), "UTF-8"));
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
        new Example_23();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_23 => %4d ms%n", time1 - time0);
    }
}   // End of Example_23.java
