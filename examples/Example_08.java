/*
 * Example_08.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_08.java
 */
public class Example_08 {
    public Example_08() throws Exception {
        PDF pdf = new PDF(new FileOutputStream("Example_08.pdf"));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("The Ascent and Descent of a Font");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.setSize(72f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.setSize(24f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        float x1 = 90f;
        float y1 = 100f;

        // A heading, which PAC asks a document for
        new TextLine(f2, "The Ascent and Descent of a Font")
                .setStructureType(StructElem.H1)
                .setFontSize(18f)
                .setLocation(x1, 45f)
                .drawOn(page);

        TextLine textLine = new TextLine(f2, "(x1, y1)");
        textLine.setLocation(x1, y1 - 15f);
        textLine.drawOn(page);

        TextBlock textBlock = new TextBlock(f1,
            "Hello, World! This example shows the functionality of the TextBlock.");
        textBlock.setLocation(x1, y1);
        textBlock.setWidth(500f);
        textBlock.setBorderColor(0xF1F3F6);     // The slate of the charts
        textBlock.setBackgroundColor(0xF1F3F6);
        textBlock.setTextColor(Color.black);
        float[] xy = textBlock.drawOn(page);

        // Text on the left
        TextLine ascentText = new TextLine(f2, "Ascent");
        ascentText.setFontSize(18f);
        ascentText.setLocation(x1 - 85f, y1 + 40f);
        ascentText.drawOn(page);

        TextLine descentText = new TextLine(f2, "Descent");
        descentText.setFontSize(18f);
        descentText.setLocation(x1 - 85f, y1 + f1.getAscent() + 15f);
        descentText.drawOn(page);

        // Line beside the text ascent
        Line ascentMark = new Line(
            x1 - 10f,
            y1,
            x1 - 10f,
            y1 + f1.getAscent());
        ascentMark.setStrokeColor(0x1F3A5F);     // Navy
        ascentMark.setStrokeWidth(3f);
        ascentMark.drawOn(page);

        // Line beside the text descent
        Line descentMark = new Line(
            x1 - 10f,
            y1 + f1.getAscent(),
            x1 - 10f,
            y1 + f1.getAscent() + f1.getDescent());
        descentMark.setStrokeColor(0xA7AFB8);      // Gray
        descentMark.setStrokeWidth(3f);
        descentMark.drawOn(page);

        Line baseLine = new Line(
                x1,
                y1 + f1.getAscent(),
                xy[0],
                y1 + f1.getAscent());
        baseLine.drawOn(page);

        Line descentLine = new Line(
                x1,
                y1 + (f1.getAscent() + f1.getDescent()),
                xy[0],
                y1 + (f1.getAscent() + f1.getDescent()));
        descentLine.drawOn(page);

        Line ascentLine = new Line(
                x1,
                y1 + f1.getBodyHeight() + f1.getAscent(),
                xy[0],
                y1 + f1.getBodyHeight() + f1.getAscent());
        ascentLine.drawOn(page);

        Point p1 = new Point(x1, y1);
        p1.setRadius(5f);
        p1.setStrokeColor(0x1F3A5F);
        p1.drawOn(page);

        Point p2 = new Point(xy[0], xy[1]);
        p2.setRadius(5f);
        p2.setStrokeColor(0x1F3A5F);
        p2.drawOn(page);

        f2.setSize(24f);
        TextLine textLine3 = new TextLine(f2, "(x2, y2)");
        textLine3.setLocation(xy[0] - 80f, xy[1] + 30f);
        textLine3.drawOn(page);

        Rect rect = new Rect(xy[0], xy[1], 20f, 20f);
        rect.setBorderColor(0x1F3A5F);
        rect.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_08();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_08 => %4d ms%n", time1 - time0);
    }
}   // End of Example_08.java
