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
 * data of 2023: a series for each of the World Bank's seven regions, in its
 * color and shape, each point a link to its row
 * of a table on pdfjet.com, with its life expectancy and children per woman,
 * described by its figures, and the trend line, with a table of seven of them
 * under the chart.
 */
public class Example_26 {
    // Seven of the countries, by their codes
    static final List<String> NAMED = Arrays.asList("BRA", "CAN", "IND", "JPN", "NER", "NGA", "USA");
    // The World Bank's regions, a series each, in the order of its list
    static final String[] REGIONS = {
        "East Asia & Pacific",
        "Europe & Central Asia",
        "Latin America & Caribbean",
        "Middle East, North Africa, Afghanistan & Pakistan",
        "North America",
        "South Asia",
        "Sub-Saharan Africa"
    };

    public Example_26() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_26.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Life Expectancy and Children per Woman");

        Font f1 = new Font(pdf, IBMPlexSans.SemiBold);
        f1.setSize(12f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.setSize(9f);

        // Country|Code|Life expectancy|Children per woman|Region|ISO2, after a header line
        List<String> lines = Content.linesOfTextFile("resources/data/life-expectancy-and-fertility-2023.txt");
        int total = lines.size() - 1;

        Page page = new Page(pdf, Letter.PORTRAIT);

        new TextLine(f1, "Life Expectancy and Children per Woman")
                .setStructureType(StructElem.H1).setFontSize(20f)
                .setLocation(50f, 70f).drawOn(page);
        List<Paragraph> intro = new ArrayList<Paragraph>();
        intro.add(new Paragraph(new TextLine(f2, "Where women have fewer children, people live longer: the "
                + total + " countries and economies of the World Bank's data of 2023, in a color and "
                + "a shape for each of its seven regions, each point a link to its row of a table on pdfjet.com, with its life expectancy and children per woman, and the trend line "
                + "through them, with seven of them in the table "
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
        for (String region : REGIONS) {
            chart.addSeries(region);
            for (String line : lines.subList(1, lines.size())) {
                String[] field = line.split("\\|");
                if (!field[4].equals(region)) {
                    continue;
                }
                float life = Float.parseFloat(field[2]);
                float children = Float.parseFloat(field[3]);
                String uri = "https://pdfjet.com/countries.html#" + field[5];
                // The outline of its shape, a link described by its figures
                String name = field[0] + ": " + decimals(field[2], 1) + " years, "
                        + decimals(field[3], 2) + " children per woman";
                chart.addLinkedDot(name, children, life, uri);
            }
        }
        for (String line : lines.subList(1, lines.size())) {
            String[] field = line.split("\\|");
            if (NAMED.contains(field[1])) {
                // The figures as the file writes them, two decimals and one,
                // not rounded again, so that every port writes them alike
                data.add(row(f2, false, field[0], decimals(field[3], 2), decimals(field[2], 1) + " years"));
            }
        }
        chart.setLocation(50f, y).setSize(512f, 340f);
        y = chart.drawOn(page)[1] + 30f;

        // Seven of the countries
        new TextLine(f1, "Seven of the countries").setStructureType(StructElem.H2)
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

    // The number of the text with the decimals, zeros added: 1.5 is 1.50.
    private static String decimals(String number, int count) {
        int point = number.indexOf('.');
        StringBuilder sb = new StringBuilder(point < 0 ? number + "." : number);
        int have = (point < 0) ? 0 : number.length() - point - 1;
        for (int i = have; i < count; i++) {
            sb.append('0');
        }
        return sb.toString();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_26();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_26 => %4d ms%n", time1 - time0);
    }
}   // End of Example_26.java
