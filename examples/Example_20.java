/*
 * Example_20.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;
import com.pdfjet.barcodes.*;

/**
 * Example_20.java
 * This example draws a page of results from a CSV file of electric vehicles:
 * two simple bar charts, the five makes and the model years, and under them a
 * table of the same numbers, then a table of one car with a picture and its
 * VIN as a barcode, in their cells.
 */
public class Example_20 {
    static final String[] MAKES = {"TESLA", "NISSAN", "CHEVROLET", "BMW", "FORD"};
    static final String[] NAMES = {"Tesla", "Nissan", "Chevrolet", "BMW", "Ford"};
    static final int FIRST_YEAR = 2019;
    static final int LAST_YEAR = 2023;

    public Example_20() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_20.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Electric Vehicles by Make and Model Year");

        Font f1 = new Font(pdf, IBMPlexSans.SemiBold);
        f1.setSize(11f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.setSize(9f);

        // The cars of the file: by make, the battery electric ones and the
        // electric range of those that give one, and by model year
        List<String> lines = Content.linesOfTextFile("resources/data/Electric_Vehicle_Population_10_Pages.csv");
        int[] cars = new int[MAKES.length];
        int[] battery = new int[MAKES.length];
        int[] ranges = new int[MAKES.length];
        int[] ranged = new int[MAKES.length];
        int[] years = new int[LAST_YEAR - FIRST_YEAR + 1];
        String[] modelX = null;     // The first Tesla Model X of the file
        for (String line : lines.subList(1, lines.size())) {
            String[] field = line.split(",", -1);
            int year = Integer.parseInt(field[5]);
            if (year >= FIRST_YEAR && year <= LAST_YEAR) {
                years[year - FIRST_YEAR]++;
            }
            int i = Arrays.asList(MAKES).indexOf(field[6]);
            if (i >= 0) {
                cars[i]++;
                if (field[8].startsWith("Battery")) {
                    battery[i]++;
                }
                int range = Integer.parseInt(field[10]);
                if (range > 0) {
                    ranges[i] += range;
                    ranged[i]++;
                }
            }
            if (modelX == null && field[7].equals("MODEL X")) {
                modelX = field;
            }
        }
        int total = lines.size() - 1;

        Page page = new Page(pdf, Letter.PORTRAIT);

        new TextLine(f1, "Electric Vehicles by Make and Model Year")
                .setStructureType(StructElem.H1).setFontSize(20f)
                .setLocation(50f, 70f).drawOn(page);
        List<Paragraph> intro = new ArrayList<Paragraph>();
        intro.add(new Paragraph(new TextLine(f2, "The " + total + " electric vehicles of the first rows of "
                + "the data file that Example_24 draws as a table: the five makes with the most cars, and the "
                + "cars of each model year from " + FIRST_YEAR + " to " + LAST_YEAR + ", with the numbers "
                + "in the table under the charts.").setFontSize(10f)));
        TextFrame frame = new TextFrame(intro).setLocation(50f, 84f).setWidth(512f);
        frame.drawOn(page);
        float y = frame.getTextBottom() + 18f;

        SimpleBarChart byMake = new SimpleBarChart(f1, f2)
                .setTitle("The five makes").setSubtitle("cars").setHighlight(0);
        for (int i = 0; i < MAKES.length; i++) {
            byMake.add(NAMES[i], cars[i]);
        }
        byMake.setLocation(50f, y).setWidth(248f).drawOn(page);

        // The chart highlights the busiest year itself, and marks the
        // average, in whole cars, with a dashed line; its bars are rounded
        SimpleBarChart byYear = new SimpleBarChart(f1, f2)
                .setTitle("By model year").setSubtitle("cars")
                .setHighlightHighest().setRoundedBars(true);
        int allYears = 0;
        for (int i = 0; i < years.length; i++) {
            byYear.add(String.valueOf(FIRST_YEAR + i), years[i]);
            allYears += years[i];
        }
        byYear.setReferenceLine(allYears / years.length, "Average");
        byYear.setLocation(314f, y).setWidth(248f).drawOn(page);
        y += Math.max(byMake.getHeight(), byYear.getHeight()) + 30f;

        // The numbers of the charts
        new TextLine(f1, "The five makes").setStructureType(StructElem.H2)
                .setFontSize(14f).setLocation(50f, y).drawOn(page);
        List<List<Cell>> data = new ArrayList<List<Cell>>();
        data.add(row(f1, true, "Make", "Cars", "Share of all", "Battery electric", "Average range"));
        for (int i = 0; i < MAKES.length; i++) {
            data.add(row(f2, false, NAMES[i], String.valueOf(cars[i]),
                    String.format(Locale.US, "%.1f%%", 100f * cars[i] / total),
                    String.valueOf(battery[i]),
                    ranged[i] == 0 ? "" : (ranges[i] / ranged[i]) + " mi"));
        }
        Table table = new Table();
        table.setTableData(data, 1);
        float[] widths = {142f, 80f, 90f, 100f, 100f};
        for (int i = 0; i < widths.length; i++) {
            table.setColumnWidth(i, widths[i]);
        }
        table.setLocation(50f, y + 10f);
        y = table.drawOn(page)[1] + 30f;

        // One car of the data, a picture of a car and its VIN in their cells
        new TextLine(f1, "One car of the data").setStructureType(StructElem.H2)
                .setFontSize(14f).setLocation(50f, y).drawOn(page);
        Image image = new Image(pdf, "resources/images/car.png");
        image.setAltDescription("A drawing of a blue car, seen from the side.");
        image.scaleBy(1f / 3f);     // The image is 120 DPI, so 0.6 of its pixels
        Barcode vin = new Barcode(Barcode.CODE_128, modelX[0]);
        vin.setModuleLength(0.75f);
        vin.setFont(f2);
        data = new ArrayList<List<Cell>>();
        data.add(row(f1, true, "Car", "Model", "VIN"));
        for (Cell cell : data.get(0)) {
            cell.setTextAlignment(Alignment.LEFT);     // Text, not numbers
        }
        List<Cell> car = row(f2, false, "", "Tesla Model X, " + modelX[5] + ", " + modelX[2] + ", "
                + modelX[10] + " mi of range", "");
        for (Cell cell : car) {
            cell.setTextAlignment(Alignment.LEFT);
        }
        car.get(0).setImage(image);
        car.get(2).setBarcode(vin);
        data.add(car);
        table = new Table();
        table.setTableData(data, 1);
        table.setColumnWidth(0, image.getWidth() + 8f);
        table.setColumnWidth(1, 220f);
        table.setColumnWidth(2, 512f - image.getWidth() - 228f);
        table.setLocation(50f, y + 10f);
        table.drawOn(page);

        pdf.complete();
    }

    // A row of cells of the font at 9 points, one for each text, in the
    // look of the charts: a light band behind the header row, a thin gray
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
        new Example_20();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_20 => %4d ms%n", time1 - time0);
    }
}   // End of Example_20.java
