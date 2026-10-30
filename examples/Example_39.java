/*
 * Example_39.java
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
 * Example_39.java
 *
 * Draws the pages of a document first and finishes them last. The two map
 * pages are drawn first and the contents page last, and the three are added
 * to the PDF in reading order, the contents first. They are made detached,
 * outside the PDF, so a "Page X of N" footer is drawn on each once the
 * number of pages is known.
 */
final public class Example_39 {
    public Example_39() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_39.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Maps");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        String[] titles = {"Europe", "Spain"};
        String[] files = {"resources/images/ee-map.png", "resources/images/spain-admin.jpg"};
        String[] descriptions = {
                "A map of Europe in which the member states of the European Union are shaded, and Turkey, a candidate to join when the map was made, in another shade.",
                "A map of the 17 autonomous communities of Spain and its two autonomous cities, Ceuta and Melilla, with their capitals.",
        };

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
            image.setAltDescription(descriptions[i]);
            image.scaleBy((page.getWidth() - 100f) / image.getWidth());
            image.setLocation(50f, 100f);
            image.drawOn(page);

            mapPages[i] = page;
        }

        // 2. Draw the contents page last, now that the map pages are ready.
        Page contents = new Page(pdf, A4.PORTRAIT, Page.DETACHED);

        TextLine text = new TextLine(f2, "Maps");
        text.setStructureType(StructElem.H1);
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
                + "of the document: the pages were made detached and are added to the PDF "
                + "in reading order. The footer \"Page X of N\" is drawn on each page last, "
                + "when the number of pages is known.");
        textBlock.setFontSize(12f);
        textBlock.setLineSpacing(1.5f);
        textBlock.setTextColor(Color.dimgray);
        textBlock.setLocation(50f, y + 20f);
        textBlock.setWidth(contents.getWidth() - 100f);
        textBlock.drawOn(contents);

        // 3. The pages in reading order, and a footer on each, now that their
        // number is known.
        List<Page> pages = new ArrayList<Page>();
        pages.add(contents);
        pages.addAll(Arrays.asList(mapPages));
        float fontSize = 10f;
        for (int i = 0; i < pages.size(); i++) {
            Page page = pages.get(i);

            Line line = new Line(70f, page.getHeight() - 60f,
                    page.getWidth() - 70f, page.getHeight() - 60f);
            line.setStrokeColor(Color.lightgray);
            line.drawOn(page);

            // A page number is an artifact: a screen reader skips it.
            String footer = "Page " + (i + 1) + " of " + pages.size();
            page.addArtifactBMC();
            page.setBrushColor(Color.black);
            page.drawString(
                    f1,
                    fontSize,
                    footer,
                    (page.getWidth() - f1.stringWidth(fontSize, footer))/2f,
                    page.getHeight() - 40f);
            page.addEMC();
        }
        pdf.addPages(pages);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_39();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_39 => %4d ms%n", time1 - time0);
    }
}   // End of Example_39.java
