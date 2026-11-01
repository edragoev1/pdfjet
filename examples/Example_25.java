/*
 * Example_25.java
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
 * Example_25.java
 *
 * Draws a SimpleLineChart of two lines over the same years: the vehicles of
 * the data file by model year, battery electric highlighted in navy, each
 * line labeled at its end with its last count, so that the chart needs no
 * legend, and a dot at each year so that each count can be read off its line.
 * The second page draws the plug-in hybrids alone as an area chart, the area
 * under the line filled and each count written above its year.
 */
public class Example_25 {
    // The vehicles of the Washington State Electric Vehicle Population Data
    // by model year and kind, summarized in
    // resources/data/Electric_Vehicles_by_Model_Year.csv: the rows of the data
    // whose "Electric Vehicle Type" begins with "Battery Electric" and with
    // "Plug-in Hybrid", counted by the "Model Year" column. The data holds
    // 124,716 rows, of which 124,419 are registered in Washington State, and
    // model years from 1997; the years before 2011 are a few dozen vehicles
    // in all and are left out here.
    private static final int FIRST_YEAR = 2011;

    public Example_25() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_25.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Electric Vehicles by Model Year");

        Font f1 = new Font(pdf, IBMPlexSans.SemiBold);
        Font f2 = new Font(pdf, IBMPlexSans.Regular);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine title = new TextLine(f1, "Electric Vehicles by Model Year");
        title.setStructureType(StructElem.H1);
        title.setFontSize(18f);
        title.setLocation(50f, 50f);
        title.drawOn(page);

        TextBlock textBlock = new TextBlock(f2,
                "A SimpleLineChart draws a few lines over the same categories, here "
                + "the model years, in their order left to right. Each line is labeled "
                + "at its end with its name and its last value, so the chart needs no "
                + "legend, and one line is highlighted in navy, the other gray. The dots "
                + "of setMarkers show the count for each model year. In a PDF/UA "
                + "document the chart describes itself from its data: each line's first "
                + "and last values and its highest.\n\n"
                + "The counts are of the Washington State Electric Vehicle Population "
                + "Data, summarized by model year and by kind in "
                + "resources/data/Electric_Vehicles_by_Model_Year.csv.");
        textBlock.setFontSize(11f);
        textBlock.setLineSpacing(1.4f);
        textBlock.setLocation(50f, 72f);
        textBlock.setWidth(512f);
        float[] xy = textBlock.drawOn(page);

        f1.setSize(12f);
        f2.setSize(9f);

        // The model years from FIRST_YEAR on, after the header line
        List<String[]> rows = new ArrayList<String[]>();
        List<String> lines = Content.linesOfTextFile("resources/data/Electric_Vehicles_by_Model_Year.csv");
        for (String line : lines.subList(1, lines.size())) {
            String[] field = line.split(",");
            if (Integer.parseInt(field[0]) >= FIRST_YEAR) {
                rows.add(field);
            }
        }
        String[] years = new String[rows.size()];
        float[] battery = new float[rows.size()];
        float[] hybrid = new float[rows.size()];
        for (int i = 0; i < rows.size(); i++) {
            years[i] = rows.get(i)[0];
            battery[i] = Float.parseFloat(rows.get(i)[1]);
            hybrid[i] = Float.parseFloat(rows.get(i)[2]);
        }
        new SimpleLineChart(f1, f2)
                .setTitle("Battery electric and plug-in hybrid vehicles")
                .setSubtitle("By model year, in the Washington State data file")
                .setYAxisTitle("Vehicles")
                .setCategories(years)
                .addLine("Battery electric", battery)
                .addLine("Plug-in hybrid", hybrid)
                .setHighlight(0)
                .setMarkers(true)
                .setLocation(50f, xy[1] + 30f)
                .setSize(512f, 340f)
                .drawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);
        new SimpleLineChart(f1, f2)
                .setTitle("Plug-in hybrid vehicles")
                .setSubtitle("By model year, the area under the line filled and each count written")
                .setYAxisTitle("Vehicles")
                .setCategories(years)
                .addLine("Plug-in hybrid", hybrid)
                .setHighlight(0)
                .setMarkers(true)
                .setAreaFilled(true)
                .setValueLabels(true)
                .setLocation(50f, 50f)
                .setSize(512f, 300f)
                .drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_25();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_25 => %4d ms%n", time1 - time0);
    }
}   // End of Example_25.java
