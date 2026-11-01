/*
 * Example_50.java
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
 * Example_50.java
 *
 * Does what programs most often do with an existing PDF: it reads the PDF,
 * stamps every page APPROVED with a date, and adds page numbers at the foot.
 * What is drawn on a page that was read goes on top of its content, and the
 * rest of the document stays as it was.
 */
class Example_50 {
    public Example_50(String fileName) throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_50.pdf")));
        List<PDFobj> objects = pdf.read(new FileInputStream(fileName));

        // A font added to an existing PDF is embedded as a subset of the glyphs
        // drawn, as in a new PDF. IBM Plex Sans, which PDFjet carries, is read
        // from the library; the path of a font file of your own reads that file.
        Font bold = new Font(objects, IBMPlexSans.Bold);
        bold.setSize(14f);
        Font regular = new Font(objects, IBMPlexSans.Regular);
        regular.setSize(10f);

        // The date is fixed, so that the four ports write the same PDF: a program
        // would stamp the date of the day.
        TextBlock stamp = new TextBlock(bold, "APPROVED\n2026-10-30");
        stamp.setWidth(120f);
        stamp.setPadding(6f);
        stamp.setTextColor(Color.red);
        stamp.setBorderColor(Color.red);
        stamp.setTextAlignment(Alignment.CENTER);

        List<PDFobj> pages = pdf.getPageObjects(objects);
        for (int i = 0; i < pages.size(); i++) {
            Page page = new Page(pdf, pages.get(i));
            page.addResource(bold, objects);
            page.addResource(regular, objects);

            stamp.setLocation(page.getWidth() - 156f, 36f);
            stamp.drawOn(page);

            String text = "Page " + (i + 1) + " of " + pages.size();
            float x = (page.getWidth() - regular.stringWidth(text)) / 2f;
            new TextLine(regular, text).setLocation(x, page.getHeight() - 36f).drawOn(page);

            page.complete(objects); // The graphics stack is unwinded automatically
        }
        pdf.addObjects(objects);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_50("resources/data/testPDFs/sample-document.pdf");
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_50 => %4d ms%n", time1 - time0);
    }
}   // End of Example_50.java
