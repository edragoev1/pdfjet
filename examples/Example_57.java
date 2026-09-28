/*
 * Example_57.java
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
 * Example_57.java
 */
public class Example_57 {
    public Example_57() throws Exception {
        PDF pdf = new PDF(  // Use 8MB buffer to speed operation
                new BufferedOutputStream(new FileOutputStream("Example_57.pdf"), 8*1024*1024));
        // Example_43's table, cut to 550 rows, 12 pages, as a PDF/UA document: a BigTable is
        // tagged as a table, a TR for each row, holding a TH or a TD with the text
        // of each cell, which is what a screen reader reads a table from. It is
        // the size a PDF/UA checker such as PAC can open, which the 2,000 pages of
        // Example_43 are not.
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Electric Vehicle Population Data");   // Required for PDF/UA !

        String fileName = "data/Electric_Vehicle_Population_10_Pages.csv";

        Font f1 = new Font(pdf, IBMPlexSans.SemiBold);
        f1.setSize(10f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.setSize(9f);

        BigTable table = new BigTable(pdf, f1, f2, Letter.LANDSCAPE);
        table.setNumberOfColumns(9);        // The order of the
        table.setTableData(fileName, ",");  // these statements
        // A heading on the first page only, which the table starts under
        Page first = new Page(pdf, Letter.LANDSCAPE);
        new TextLine(f1, "Electric Vehicle Population Data")
                .setStructureType(StructElem.H1)
                .setFontSize(14f)
                .setLocation(10f, 24f)
                .drawOn(first);
        table.setFirstPage(first, 34f);
        table.setLocation(0f, 0f);          // is
        table.setBottomMargin(20f);         // very
        table.complete();                   // important!

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_57();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_57 => %4d ms%n", time1 - time0);
    }
}   // End of Example_57.java
