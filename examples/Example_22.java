/*
 * Example_22.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_22.java
 * This example links a contents page to three chapters, and each chapter
 * back to the contents, with destinations and "Go To" actions.
 */
public class Example_22 {
    public Example_22() throws Exception {
        PDF pdf = new PDF(
            new BufferedOutputStream(new FileOutputStream("Example_22.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Internal links and destinations");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        String[] chapters = {
            "Destinations",
            "Go To actions",
            "Links on shapes and images",
        };
        String[] texts = {
            "A destination is a named place in a document. The title of this chapter "
                + "is the destination \"chapter1\", set with setDestination.",
            "A Go To action makes something a link to a destination. The chapter titles "
                + "on the contents page are text lines with a Go To action.",
            "Rectangles and images can be links too. Click the arrow in the top left "
                + "corner, or the arrow image next to it, to go back to the contents.",
        };

        // The contents page. The destination is the top of the page.
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.addDestination("contents", 0f, 0f);

        TextLine text = new TextLine(f2, "Contents");
        text.setStructureType(StructElem.H1);
        text.setFontSize(24f);
        text.setLocation(90f, 100f);
        text.drawOn(page);

        float y = 150f;
        for (int i = 0; i < chapters.length; i++) {
            text = new TextLine(f1, "Chapter " + (i + 1) + ": " + chapters[i]);
            text.setFontSize(14f);
            text.setTextColor(Color.blue);
            text.setUnderline(true);
            text.setGoToAction("chapter" + (i + 1));
            text.setLocation(90f, y);
            text.drawOn(page);
            y += 30f;
        }

        for (int i = 0; i < chapters.length; i++) {
            page = new Page(pdf, Letter.PORTRAIT);

            // The title of the chapter is its destination.
            text = new TextLine(f2, "Chapter " + (i + 1) + ": " + chapters[i]);
            text.setFontSize(20f);
            text.setDestination("chapter" + (i + 1));
            text.setLocation(90f, 100f);
            text.drawOn(page);

            TextBlock textBlock = new TextBlock(f1, texts[i]);
            textBlock.setFontSize(12f);
            textBlock.setLineSpacing(1.5f);
            textBlock.setLocation(90f, 125f);
            textBlock.setWidth(430f);
            textBlock.drawOn(page);

            text = new TextLine(f1, "Back to the contents");
            text.setFontSize(12f);
            text.setTextColor(Color.blue);
            text.setUnderline(true);
            text.setGoToAction("contents");
            text.setLocation(90f, 250f);
            text.drawOn(page);
        }

        // On the last page, a rect with no border links to the contents too.
        Rect rect = new Rect(20f, 20f, 20f, 20f);
        rect.setGoToAction("contents");
        rect.drawOn(page);

        // Create an up arrow and place it in the rect
        Path path = new Path();
        path.add(new Point(30f, 21f));
        path.add(new Point(37f, 29f));
        path.add(new Point(33f, 29f));
        path.add(new Point(33f, 39f));
        path.add(new Point(27f, 39f));
        path.add(new Point(27f, 29f));
        path.add(new Point(23f, 29f));
        path.setClosed(true);
        path.setStrokeColor(Color.deepskyblue);
        path.setFillShape(true);
        path.drawOn(page);

        // And so does an image of an arrow.
        Image image = new Image(pdf, "images/up-arrow.png");
        image.setLocation(50f, 20f);
        image.setGoToAction("contents");
        image.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_22();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_22 => %4d ms%n", time1 - time0);
    }
}   // End of Example_22.java
