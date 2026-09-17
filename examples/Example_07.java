/*
 * Example_07.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_07.java
 * This example adds a "DRAFT" watermark to every page of a two-page
 * PDF/A-3B document. The watermark is drawn first, so the text of the page
 * is drawn over it.
 */
public class Example_07 {

    public Example_07() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_07.pdf")),
                Compliance.PDF_A_3B);
        pdf.setTitle("PDF/A-3B compliant PDF");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);
        Font f3 = new Font(pdf, IBMPlexSans.Bold);

        String[] titles = {
            "Project Proposal",
            "Budget and Schedule",
        };
        String[] texts = {
            "This proposal describes a new reporting service that creates invoices, "
                + "statements and delivery notes as PDF documents. The documents are "
                + "archived as PDF/A-3B, so they can be opened and printed exactly "
                + "the same way for many years.\n\n"
                + "The watermark tells every reader that this is a draft. It is drawn "
                + "in light gray behind the text, at an angle from the bottom left "
                + "corner to the top right corner of the page.",
            "The service will be built in three phases over six months. The first "
                + "phase delivers invoices, the second statements, and the third "
                + "delivery notes.\n\n"
                + "The budget and the schedule will be final once the proposal is "
                + "approved. Until then, every page of this document is marked as a draft.",
        };

        for (int i = 0; i < titles.length; i++) {
            Page page = new Page(pdf, A4.LANDSCAPE);

            // The watermark is drawn before the content of the page.
            f3.setSize(120f);
            page.addWatermark(f3, "DRAFT");

            TextLine title = new TextLine(f2, titles[i]);
            title.setFontSize(28f);
            title.setLocation(70f, 100f);
            title.drawOn(page);

            TextBlock textBlock = new TextBlock(f1, texts[i]);
            textBlock.setFontSize(14f);
            textBlock.setLineSpacing(1.5f);
            textBlock.setLocation(70f, 130f);
            textBlock.setWidth(page.getWidth() - 140f);
            textBlock.drawOn(page);

            TextLine footer = new TextLine(f1, "Page " + (i + 1) + " of " + titles.length);
            footer.setFontSize(10f);
            footer.setTextColor(Color.gray);
            footer.setLocation(70f, page.getHeight() - 40f);
            footer.drawOn(page);
        }

        pdf.complete();
    }


    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_07();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_07 => %4d ms%n", time1 - time0);
    }
}   // End of Example_07.java
