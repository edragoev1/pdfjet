/*
 * Example_26.java
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
 * Example_26.java
 * This example draws a SimpleScatterChart of the life expectancy and the
 * children per woman of 217 countries and economies, from the World Bank's
 * data of 2023: a gray dot for each, seven of them named, and the trend line,
 * with a table of the named ones under the chart.
 */
public class Example_26 {
    // The countries named on the chart, by their codes
    static final List<String> NAMED = Arrays.asList("BRA", "CAN", "IND", "JPN", "NER", "NGA", "USA");

    public Example_26() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_26.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Life Expectancy and Children per Woman");

        Font f1 = new Font(pdf, IBMPlexSans.SemiBold);
        f1.setSize(12f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.setSize(9f);

        // Country|Code|Life expectancy|Children per woman, after a header line
        List<String> lines = Content.linesOfTextFile("data/life-expectancy-and-fertility-2023.txt");
        int total = lines.size() - 1;

        Page page = new Page(pdf, Letter.PORTRAIT);

        new TextLine(f1, "Life Expectancy and Children per Woman")
                .setStructureType(StructElem.H1).setFontSize(20f)
                .setLocation(50f, 70f).drawOn(page);
        List<Paragraph> intro = new ArrayList<Paragraph>();
        intro.add(new Paragraph(new TextLine(f2, "Where women have fewer children, people live longer: the "
                + total + " countries and economies of the World Bank's data of 2023, each a gray dot, "
                + "seven of them named, and the trend line through them, with the named ones in the table "
                + "under the chart.").setFontSize(10f)));
        TextFrame frame = new TextFrame(intro).setLocation(50f, 84f).setWidth(512f);
        frame.drawOn(page);
        float y = frame.getTextBottom() + 18f;

        SimpleScatterChart chart = new SimpleScatterChart(f1, f2)
                .setTitle("Life expectancy and children per woman")
                .setSubtitle(total + " countries and economies, 2023")
                .setXAxisTitle("Children per woman")
                .setYAxisTitle("Life expectancy at birth, years")
                .setTrendLine(true);
        List<List<Cell>> data = new ArrayList<List<Cell>>();
        data.add(row(f1, true, "Country", "Children per woman", "Life expectancy"));
        for (String line : lines.subList(1, lines.size())) {
            String[] field = line.split("\\|");
            float life = Float.parseFloat(field[2]);
            float children = Float.parseFloat(field[3]);
            if (NAMED.contains(field[1])) {
                chart.add(field[0], children, life);    // A navy dot, named
                data.add(row(f2, false, field[0], String.format(Locale.US, "%.2f", children),
                        String.format(Locale.US, "%.1f years", life)));
            } else {
                chart.add(children, life);              // A gray dot
            }
        }
        chart.setLocation(50f, y).setSize(512f, 340f);
        y = chart.drawOn(page)[1] + 30f;

        // The countries named on the chart
        new TextLine(f1, "The countries named on the chart").setStructureType(StructElem.H2)
                .setFontSize(14f).setLocation(50f, y).drawOn(page);
        Table table = new Table();
        table.setTableData(data, 1);
        table.setColumnWidth(0, 232f);
        table.setColumnWidth(1, 140f);
        table.setColumnWidth(2, 140f);
        table.setLocation(50f, y + 10f);
        y = table.drawOn(page)[1] + 16f;

        new TextLine(f2, "Source: World Bank, World Development Indicators, SP.DYN.LE00.IN and "
                + "SP.DYN.TFRT.IN, 2023, licensed under CC BY 4.0.")
                .setFontSize(8f).setTextColor(0x5F6873).setLocation(50f, y).drawOn(page);

        pdf.complete();
    }

    // A row of cells of the font at 9 points, one for each text, in the
    // look of the chart: a light band behind the header row, a thin gray
    // line under each row, and the numbers, the cells after the first,
    // aligned right.
    static List<Cell> row(Font font, boolean header, String... texts) {
        List<Cell> row = new ArrayList<Cell>();
        for (int i = 0; i < texts.length; i++) {
            Cell cell = new Cell(font, texts[i]).setFontSize(9f);
            cell.setTopPadding(5f).setBottomPadding(5f).setLeftPadding(6f).setRightPadding(6f);
            cell.setBorders(false).setBorder(Border.BOTTOM, true);
            cell.setBorderWidth(0.5f).setBorderColor(0xD5D9DE);
            if (header) {
                cell.setBackgroundColor(0xF1F3F6);
            }
            if (i > 0) {
                cell.setTextAlignment(Alignment.RIGHT);
            }
            row.add(cell);
        }
        return row;
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_26();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_26 => %4d ms%n", time1 - time0);
    }
}   // End of Example_26.java
