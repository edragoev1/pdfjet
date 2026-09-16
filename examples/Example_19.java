/*
 * Example_19.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_19.java
 * Using the TextBlock component to draw text next to images.
 */
public class Example_19 {
    public Example_19() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_19.pdf")));

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.setSize(10f);

        Font f2 = new Font(pdf, IBMPlexSansTC.Regular);
        f2.setSize(10f);

        Page page = new Page(pdf, Letter.PORTRAIT);
        // Columns x coordinates
        float x1 = 50f;
        float y1 = 50f;
        float x2 = 300f;
        float w2 = 300f;    // Width of the second column

        Image image1 = new Image(pdf, "images/ee-map.png");
        Image image2 = new Image(pdf, "images/spain-admin.jpg");

        // Draw the first image
        image1.setLocation(x1, y1);
        image1.scaleBy(0.3f);
        image1.drawOn(page);

        TextBlock textBlock = new TextBlock(f1, Content.ofTextFile("data/calculus-short.txt"));
        textBlock.setLocation(x2, y1);
        textBlock.setWidth(w2);
        textBlock.setBorderColor(Color.black);
        float[] xy = textBlock.drawOn(page);

        // Draw the second image
        image2.setLocation(x1, xy[1] + 10f);
        image2.scaleBy(0.1f);
        image2.drawOn(page);

        textBlock = new TextBlock(f1, Content.ofTextFile("data/physics.txt"));
        textBlock.setLocation(x2, xy[1] + 10f);
        textBlock.setWidth(w2);
        textBlock.setBorderColor(Color.black);
        xy = textBlock.drawOn(page);

        Rect rect = new Rect(xy[0], xy[1], 20f, 20f);
        rect.setBorderColor(Color.black);
        rect.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_19();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_19 => %4d ms%n", time1 - time0);
    }
}   // End of Example_19.java
