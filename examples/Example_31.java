/*
 * Example_31.java
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
 * Example_31.java
 * This example draws Hindi and Marathi text, and filled rectangles, first
 * opaque and then half transparent, so the colors mix where they overlap.
 */
public class Example_31 {
    public Example_31() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_31.pdf")));

        Font f1 = new Font(pdf, IBMPlexSansDevanagari.Regular);
        f1.setSize(13f);

        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);
        f2.setSize(14f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        // Hindi: the second line of the file, after its label.
        new TextLine(f2, "Hindi").setLocation(50f, 60f).drawOn(page);
        List<String> lines = Content.linesOfTextFile("data/languages/devanagari.txt");
        TextBlock textBlock = new TextBlock(f1, lines.get(1));
        textBlock.setLineSpacing(1.3f);
        textBlock.setLocation(50f, 70f);
        textBlock.setWidth(510f);
        float[] xy = textBlock.drawOn(page);

        new TextLine(f2, "Marathi").setLocation(50f, xy[1] + 35f).drawOn(page);
        textBlock = new TextBlock(f1, Content.ofTextFile("data/languages/marathi.txt"));
        textBlock.setLineSpacing(1.3f);
        textBlock.setLocation(50f, xy[1] + 45f);
        textBlock.setWidth(510f);
        xy = textBlock.drawOn(page);

        float y = xy[1] + 50f;
        int[] colors = {Color.blue, Color.green, Color.red};

        // Opaque rectangles: each one hides the one under it.
        new TextLine(f2, "Opaque").setLocation(50f, y).drawOn(page);
        for (int i = 0; i < colors.length; i++) {
            page.setBrushColor(colors[i]);
            page.fillRect(50f + i * 60f, y + 15f + i * 30f, 120f, 120f);
        }

        // Half transparent rectangles: the colors mix where they overlap.
        new TextLine(f2, "50% transparent").setLocation(320f, y).drawOn(page);
        page.saveGraphicsState();
        GraphicsState gs = new GraphicsState();
        gs.setAlphaStroking(0.5f);      // The stroking alpha constant
        gs.setAlphaNonStroking(0.5f);   // The non-stroking alpha constant
        page.setGraphicsState(gs);
        for (int i = 0; i < colors.length; i++) {
            page.setBrushColor(colors[i]);
            page.fillRect(320f + i * 60f, y + 15f + i * 30f, 120f, 120f);
        }
        page.restoreGraphicsState();

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_31();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_31 => %4d ms%n", time1 - time0);
    }
}   // End of Example_31.java
