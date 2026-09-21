/*
 * Example_12.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;
import com.pdfjet.pdf417.*;

/**
 * Example_12.java
 * This example draws a PDF417 barcode that holds a whole source file, with an
 * explanation and a caption.
 */
public class Example_12 {
    public Example_12() throws Exception {
        PDF pdf = new PDF(
            new BufferedOutputStream(new FileOutputStream("Example_12.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("PDF417 barcode example");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f2, "PDF417 Barcode");
        text.setStructureType(StructElem.H1);
        text.setFontSize(22f);
        text.setLocation(70f, 80f);
        text.drawOn(page);

        TextBlock textBlock = new TextBlock(f1,
                "PDF417 is a stacked two-dimensional barcode, used on boarding passes, "
                + "identity cards and shipping labels. It holds text and binary data, "
                + "and its error correction lets a scanner read it even when part of "
                + "it is damaged.");
        textBlock.setFontSize(12f);
        textBlock.setLineSpacing(1.5f);
        textBlock.setLocation(70f, 95f);
        textBlock.setWidth(470f);
        float[] xy = textBlock.drawOn(page);

        // A barcode that holds a whole source file.
        List<String> lines = Content.linesOfTextFile("data/Example_12.java");
        StringBuilder buf = new StringBuilder();
        for (String line : lines) {
            buf.append(line);
            // Both CR and LF are required!
            buf.append("\r\n");
        }

        PDF417 barcode = new PDF417(buf.toString());
        barcode.setModuleLength(1f);
        barcode.setLocation(70f, xy[1] + 30f);
        xy = barcode.drawOn(page);

        text = new TextLine(f1, "The source code of data/Example_12.java, "
                + lines.size() + " lines");
        text.setFontSize(10f);
        text.setTextColor(Color.gray);
        text.setLocation(70f, xy[1] + 20f);
        text.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_12();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_12 => %4d ms%n", time1 - time0);
    }
}   // End of Example_12.java
