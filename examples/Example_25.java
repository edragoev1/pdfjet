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
 */
public class Example_25 {
    public Example_25() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_25.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Fruit Donut Chart");

        Page page = new Page(pdf, Letter.PORTRAIT);

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.setSize(12.0f);
        Font f2 = new Font(pdf, IBMPlexSans.Bold);
        f2.setSize(10.0f);

        DonutChart chart = new DonutChart(f1, f2);
        chart.setLocation(100.0f, 200.0f);
        chart.setRadii(200.0f, 120.0f);     // an inner radius of 0 makes a pie chart

        chart.addSlice(new Slice(25f, 0xC1121F, "Apples"));   // deep red
        chart.addSlice(new Slice(20f, 0x1D3557, "Oranges"));   // navy blue
        chart.addSlice(new Slice(30f, 0x1A7468, "Bananas"));   // dark teal
        chart.addSlice(new Slice(15f, 0xD97706, "Grapes"));   // burnt orange
        chart.addSlice(new Slice(10f, 0xCAAA2F, "Lemons"));   // dark gold
        chart.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_25();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_25 => %4d ms%n", time1 - time0);
    }
}   // End of Example_25.java
