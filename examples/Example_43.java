/*
 * Example_43.java
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
 * Example_43.java
 */
public class Example_43 {
    public Example_43() throws Exception {
        PDF pdf = new PDF(  // Use 8MB buffer to speed operation
                new BufferedOutputStream(new FileOutputStream("Example_43.pdf"), 8*1024*1024));
        // Uncomment the line below to make this a PDF/UA document. A BigTable
        // is tagged as a table: a TR for each row, holding a TH or a TD with
        // the text of each cell, which is what a screen reader reads a table
        // from. It is off here because of what it costs at this size: every
        // tagged cell is an object of its own, so this document goes from
        // 5,108 objects and 11.8 MB to 1.25 million objects and 249 MB. The
        // 10-page file below is the size to see the tagging at.
        // pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Electric Vehicle Population Data");   // Required for PDF/UA !

        // Used for performance testing. Results in 2000+ pages PDF.
        String fileName = "data/Electric_Vehicle_Population_Data.csv";
        // String fileName = "data/Electric_Vehicle_Population_10_Pages.csv";

        Font f1 = new Font(pdf, IBMPlexSans.SemiBold);
        f1.setSize(10f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.setSize(9f);

        BigTable table = new BigTable(pdf, f1, f2, Letter.LANDSCAPE);
        table.setNumberOfColumns(9);        // The order of the
        table.setTableData(fileName, ",");  // these statements
        table.setLocation(0f, 0f);          // is
        table.setBottomMargin(20f);         // very
        table.complete();                   // important!

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_43();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_43 => %4d ms%n", time1 - time0);
    }
}   // End of Example_43.java
