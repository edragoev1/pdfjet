/*
 * Example_18.java
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
 * Example_18.java
 * This example shows how to write "Page X of N" footer on every page.
 */
public class Example_18 {
    public Example_18() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_18.pdf")));

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        String[] titles = {
            "1. Create the pages",
            "2. Draw the content",
            "3. Add the footers",
        };
        String[] texts = {
            "The total number of pages is not known until all the content is drawn. "
                + "That is why the pages in this document are created with Page.DETACHED: "
                + "they are not added to the PDF yet, and they are kept in a list instead.",
            "Each page gets its content, like this heading and this paragraph. "
                + "Long documents would flow their text or tables from page to page here.",
            "Now the list holds every page, so its size is the total number of pages. "
                + "The footer \"Page X of N\" is drawn on each page, "
                + "and then all the pages are added to the PDF with addPages.",
        };

        List<Page> pages = new ArrayList<Page>();
        for (int i = 0; i < titles.length; i++) {
            Page page = new Page(pdf, A4.PORTRAIT, Page.DETACHED);

            TextLine header = new TextLine(f1, "How to number pages");
            header.setFontSize(10f);
            header.setTextColor(Color.gray);
            header.setLocation(70f, 50f);
            header.drawOn(page);

            Line line = new Line(70f, 60f, page.getWidth() - 70f, 60f);
            line.setStrokeColor(Color.lightgray);
            line.drawOn(page);

            TextLine title = new TextLine(f2, titles[i]);
            title.setFontSize(20f);
            title.setLocation(70f, 120f);
            title.drawOn(page);

            TextBlock textBlock = new TextBlock(f1, texts[i]);
            textBlock.setFontSize(12f);
            textBlock.setLineSpacing(1.5f);
            textBlock.setLocation(70f, 140f);
            textBlock.setWidth(page.getWidth() - 140f);
            textBlock.drawOn(page);

            pages.add(page);
        }

        float fontSize = 10f;
        for (int i = 0; i < pages.size(); i++) {
            Page page = pages.get(i);

            Line line = new Line(70f, page.getHeight() - 60f,
                    page.getWidth() - 70f, page.getHeight() - 60f);
            line.setStrokeColor(Color.lightgray);
            line.drawOn(page);

            String footer = "Page " + (i + 1) + " of " + pages.size();
            page.setBrushColor(Color.black);
            page.drawString(
                    f1,
                    fontSize,
                    footer,
                    (page.getWidth() - f1.stringWidth(fontSize, footer))/2f,
                    page.getHeight() - 40f);
        }
        pdf.addPages(pages);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_18();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_18 => %4d ms%n", time1 - time0);
    }
}   // End of Example_18.java
