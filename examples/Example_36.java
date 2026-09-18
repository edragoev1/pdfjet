/*
 * Example_36.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_36.java
 * This example draws two map pages first and their contents page last, and
 * then adds the pages to the PDF in reading order, with the contents first.
 */
public class Example_36 {
    public Example_36() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_36.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Maps");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        String[] titles = {"Europe", "Spain"};
        String[] files = {"images/ee-map.png", "images/spain-admin.jpg"};

        // 1. Draw the map pages. They are detached, so they are not in the PDF yet.
        Page[] mapPages = new Page[titles.length];
        for (int i = 0; i < titles.length; i++) {
            Page page = new Page(pdf, A4.PORTRAIT, Page.DETACHED);

            TextLine title = new TextLine(f2, titles[i]);
            title.setFontSize(24f);
            title.setLocation(50f, 80f);
            title.drawOn(page);

            // Scale the image to the width of the page between the margins.
            Image image = new Image(pdf, files[i]);
            image.scaleBy((page.getWidth() - 100f) / image.getWidth());
            image.setLocation(50f, 100f);
            image.drawOn(page);

            TextLine footer = new TextLine(f1, "Page " + (i + 2));
            footer.setFontSize(10f);
            footer.setTextColor(Color.gray);
            footer.setLocation(50f, page.getHeight() - 40f);
            footer.drawOn(page);

            mapPages[i] = page;
        }

        // 2. Draw the contents page last, now that the map pages are ready.
        Page contents = new Page(pdf, A4.PORTRAIT, Page.DETACHED);

        TextLine text = new TextLine(f2, "Maps");
        text.setFontSize(24f);
        text.setLocation(50f, 80f);
        text.drawOn(contents);

        float y = 130f;
        for (int i = 0; i < titles.length; i++) {
            text = new TextLine(f1, titles[i] + " . . . . . . . . . . page " + (i + 2));
            text.setFontSize(14f);
            text.setLocation(50f, y);
            text.drawOn(contents);
            y += 25f;
        }

        TextBlock textBlock = new TextBlock(f1,
                "This page was drawn after the two map pages, but it is the first page "
                + "of the document, because the pages were created detached and added "
                + "to the PDF in reading order with addPage.");
        textBlock.setFontSize(12f);
        textBlock.setLineSpacing(1.5f);
        textBlock.setTextColor(Color.gray);
        textBlock.setLocation(50f, y + 20f);
        textBlock.setWidth(contents.getWidth() - 100f);
        textBlock.drawOn(contents);

        // 3. Add the pages in reading order.
        pdf.addPage(contents);
        for (int i = 0; i < mapPages.length; i++) {
            pdf.addPage(mapPages[i]);
        }

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_36();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_36 => %4d ms%n", time1 - time0);
    }
}   // End of Example_36.java
