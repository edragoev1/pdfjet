/*
 * Example_51.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_51.java
 *
 * Draws eight columns of the Electric Vehicle data of Example_24 on portrait
 * pages, with setWrapText: a field too long for its column wraps onto more
 * lines, the row as tall as its tallest field, so no column is cut short and
 * none is lost off the page; the memory stays flat, a row at a time.
 */
public class Example_51 {
    public Example_51() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_51.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Electric Vehicle Population Data, wrapped");  // Required for PDF/UA !

        Font f1 = new Font(pdf, IBMPlexSans.SemiBold);
        f1.setSize(9f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.setSize(8f);

        BigTable table = new BigTable(pdf, f1, f2, Letter.PORTRAIT);
        // Make, Model, Model Year, City, Electric Vehicle Type, Clean Alternative
        // Fuel Vehicle (CAFV) Eligibility, Electric Range and Electric Utility
        table.setColumns(6, 7, 5, 2, 8, 9, 10, 15);
        table.setWrapText(true);
        table.setTableData("resources/data/Electric_Vehicle_Population_10_Pages.csv", ",");
        table.setVerticalPadding(2f);
        table.setLocation(20f, 20f);
        table.setBottomMargin(30f);
        table.complete();

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_51();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_51 => %4d ms%n", time1 - time0);
    }
}   // End of Example_51.java
