/*
 * Example_25.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_25.java
 *
 * Draws a SimpleLineChart of two lines over the same years: the vehicles of
 * the data file by model year, battery electric highlighted in navy, each
 * line labeled at its end with its last count, so that the chart needs no
 * legend, and a dot at each year so that each count can be read off its line.
 */
public class Example_25 {
    // The vehicles of data/Electric_Vehicle_Population_Data.csv by model year
    // and kind, which Example_24 draws as a table: the rows of the file whose
    // "Electric Vehicle Type" begins with "Battery Electric" and with
    // "Plug-in Hybrid", counted by the "Model Year" column. The file holds
    // 124,716 rows, of which 124,419 are registered in Washington State, and
    // model years from 1997; the years before 2011 are a few dozen vehicles
    // in all and are left out here.
    private static final int[] YEARS =
            {2011, 2012, 2013, 2014, 2015, 2016, 2017, 2018, 2019, 2020, 2021, 2022, 2023};
    private static final float[] BATTERY =
            { 753,  798, 2936, 1805, 3612, 3891, 4458, 9946, 8576, 9315, 14755, 23507, 11853};
    private static final float[] HYBRID =
            {  75,  870, 1645, 1804, 1323, 1811, 4100, 4278, 1874, 1611,  3541,  4015,  1500};

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
                + "The counts are the vehicles of "
                + "data/Electric_Vehicle_Population_Data.csv, the file Example_24 draws "
                + "as a table of 2,546 pages, by model year and by kind.");
        textBlock.setFontSize(11f);
        textBlock.setLineSpacing(1.4f);
        textBlock.setLocation(50f, 72f);
        textBlock.setWidth(512f);
        float[] xy = textBlock.drawOn(page);

        f1.setSize(12f);
        f2.setSize(9f);

        String[] years = new String[YEARS.length];
        for (int i = 0; i < YEARS.length; i++) {
            years[i] = String.valueOf(YEARS[i]);
        }
        new SimpleLineChart(f1, f2)
                .setTitle("Battery electric and plug-in hybrid vehicles")
                .setSubtitle("By model year, in the Washington State data file")
                .setYAxisTitle("Vehicles")
                .setCategories(years)
                .addLine("Battery electric", BATTERY)
                .addLine("Plug-in hybrid", HYBRID)
                .setHighlight(0)
                .setMarkers(true)
                .setLocation(50f, xy[1] + 30f)
                .setSize(512f, 340f)
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
