/*
 * Example_24.java
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
 * Example_24.java
 */
public class Example_24 {
    public Example_24(String fileName, boolean pdfUA) throws Exception {
        PDF pdf = new PDF(  // Use 8MB buffer to speed operation
                new BufferedOutputStream(new FileOutputStream("Example_24.pdf"), 8*1024*1024));
        // The sample is a PDF/UA document: a BigTable is tagged as a table, a
        // TR for each row holding a TH or a TD with the text of each cell,
        // which is what a screen reader reads a table from. A file given as
        // the argument is left untagged, so that the benchmark stays
        // comparable with earlier releases: every tagged cell is an object of
        // its own, and the full data file goes from 5,108 objects and 11.8 MB
        // to 1.25 million objects and 249 MB.
        if (pdfUA) {
            pdf.setCompliance(Compliance.PDF_UA_1);
        }
        pdf.setTitle("Electric Vehicle Population Data");   // Required for PDF/UA !

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
        // The 10-page sample, or the file given as the first argument: the
        // full Electric Vehicle Population Data, 124,716 rows, makes 2,546
        // pages and is used for performance testing.
        String fileName = (args.length > 0) ?
                args[0] : "resources/data/Electric_Vehicle_Population_10_Pages.csv";
        long time0 = System.currentTimeMillis();
        new Example_24(fileName, args.length == 0);
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_24 => %4d ms%n", time1 - time0);
    }
}   // End of Example_24.java
