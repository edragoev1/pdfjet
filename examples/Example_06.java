/*
 * Example_06.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;

import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_06.java
 * This example attaches two files to a page, and adds a note, a link and
 * polygon, square and circle annotations next to labels that describe them.
 */
public class Example_06 {
    public Example_06() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_06.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Attachments and Annotations");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.setSize(12f);

        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);
        f2.setSize(14f);

        EmbeddedFile file1 = new EmbeddedFile(pdf, "images/linux-logo.png", false);
        EmbeddedFile file2 = new EmbeddedFile(pdf, "examples/Example_02.java", true);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f2, "Attachments and annotations");
        text.setFontSize(22f);
        text.setLocation(70f, 80f);
        text.drawOn(page);

        text = new TextLine(f1,
                "Open this page in a PDF viewer that shows annotations, and hover over the icons.");
        text.setTextColor(Color.gray);
        text.setLocation(70f, 105f);
        text.drawOn(page);

        // File attachments. The files are stored inside the PDF.
        new TextLine(f2, "Attached files").setLocation(70f, 160f).drawOn(page);

        FileAttachment attachment = new FileAttachment(file1);
        attachment.setLocation(70f, 175f);
        attachment.setIconPushPin();
        attachment.setTitle("Attached File: " + file1.getFileName());
        attachment.setContents(
                "Right mouse click on the icon to save the attached file.");
        attachment.drawOn(page);
        new TextLine(f1, "linux-logo.png, an image, with a push pin icon")
                .setLocation(105f, 192f).drawOn(page);

        attachment = new FileAttachment(file2);
        attachment.setLocation(70f, 210f);
        attachment.setIconPaperclip();
        attachment.setTitle("Attached File: " + file2.getFileName());
        attachment.setContents(
                "Right mouse click on the icon to save the attached file.");
        attachment.drawOn(page);
        new TextLine(f1, "The source code of Example_02, with a paperclip icon")
                .setLocation(105f, 227f).drawOn(page);

        // A note, and a link.
        new TextLine(f2, "A note and a link").setLocation(70f, 290f).drawOn(page);

        TextAnnotation textAnnotation = new TextAnnotation();
        textAnnotation.setLocation(70f, 305f);
        textAnnotation.setSize(24f, 24f);
        textAnnotation.setTitle("Reviewer");
        textAnnotation.setContents("Please check the figures on page 2.");
        textAnnotation.drawOn(page);
        new TextLine(f1, "A text annotation: click the note icon to read it")
                .setLocation(105f, 322f).drawOn(page);

        text = new TextLine(f1, "Visit https://pdfjet.com");
        text.setTextColor(Color.blue);
        text.setUnderline(true);
        text.setURIAction("https://pdfjet.com");
        text.setLocation(105f, 357f);
        text.drawOn(page);

        // Shape annotations, drawn half transparent over the page.
        new TextLine(f2, "Shape annotations").setLocation(70f, 420f).drawOn(page);

        PolygonAnnotation polygonAnnotation = new PolygonAnnotation();
        polygonAnnotation.setLocation(70f, 440f);
        polygonAnnotation.setVertices(new float[] {0f, 60f, 30f, 0f, 60f, 60f, 0f, 60f});
        polygonAnnotation.setFillColor(Color.red);
        polygonAnnotation.setOpacity(0.5f);
        polygonAnnotation.setTitle("Polygon");
        polygonAnnotation.setContents("Polygon Annotation");
        polygonAnnotation.drawOn(page);

        SquareAnnotation squareAnnotation = new SquareAnnotation();
        squareAnnotation.setLocation(170f, 440f);
        squareAnnotation.setSize(60f, 60f);
        squareAnnotation.setFillColor(new float[] {0f, 0.5f, 0f});
        squareAnnotation.setOpacity(0.5f);
        squareAnnotation.setTitle("Square");
        squareAnnotation.setContents("Square Annotation");
        squareAnnotation.drawOn(page);

        CircleAnnotation circleAnnotation = new CircleAnnotation();
        circleAnnotation.setLocation(270f, 440f);
        circleAnnotation.setSize(60f, 60f);
        circleAnnotation.setFillColor(new float[] {0f, 0f, 1f});
        circleAnnotation.setOpacity(0.5f);
        circleAnnotation.setTitle("Circle");
        circleAnnotation.setContents("Circle Annotation");
        circleAnnotation.drawOn(page);

        new TextLine(f1, "Polygon").setLocation(78f, 520f).drawOn(page);
        new TextLine(f1, "Square").setLocation(180f, 520f).drawOn(page);
        new TextLine(f1, "Circle").setLocation(283f, 520f).drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_06();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_06 => %4d ms%n", time1 - time0);
    }
}   // End of Example_06.java
