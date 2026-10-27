/*
 * Example_28.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_28.java
 */
public class Example_28 {
    public Example_28() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_28.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Fruit Donut Chart");

        Page page = new Page(pdf, Letter.PORTRAIT);

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.setSize(12.0f);
        Font f2 = new Font(pdf, IBMPlexSans.Bold);
        f2.setSize(10.0f);

        // A heading, which PAC asks a document for
        new TextLine(f2, "Fruit Donut Chart")
                .setStructureType(StructElem.H1)
                .setFontSize(18f)
                .setLocation(100f, 100f)
                .drawOn(page);

        DonutChart chart = new DonutChart(f2, f1);     // The bold font first
        chart.setLocation(100.0f, 200.0f);
        chart.setRadii(200.0f, 120.0f);     // an inner radius of 0 makes a pie chart

        chart.setTitle("Fruit sold this week");
        chart.setSubtitle("Share of 100 crates, in the default colors");
        chart.add("Apples", 25f);
        chart.add("Oranges", 20f);
        chart.add("Bananas", 30f);
        chart.add("Grapes", 15f);
        chart.add("Lemons", 10f);
        chart.drawOn(page);

        // Many slices, some small: a legend under the circle in place of the
        // leader lines, and the total in the hole
        page = new Page(pdf, Letter.PORTRAIT);
        DonutChart many = new DonutChart(f2, f1);
        many.setLocation(100.0f, 100.0f);
        many.setRadii(150.0f, 90.0f);
        many.setTitle("Fruit sold this month");
        many.setSubtitle("Share of 100 crates, eight kinds, a color each");
        many.setCenterText("100 crates");
        many.setLegend(true);
        String[] kinds = {"Apples", "Bananas", "Oranges", "Grapes", "Lemons",
                "Pears", "Plums", "Cherries"};
        float[] crates = {31f, 24f, 17f, 10f, 7f, 5f, 4f, 2f};
        for (int i = 0; i < kinds.length; i++) {
            many.add(kinds[i], crates[i]);
        }
        many.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_28();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_28 => %4d ms%n", time1 - time0);
    }
}   // End of Example_28.java
