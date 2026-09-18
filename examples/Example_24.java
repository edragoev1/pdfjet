/*
 * Example_24.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_24.java
 */
public class Example_24 {
    public Example_24() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_24.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("JPEG, PNG and BMP Images");

        Font font = new Font(pdf, IBMPlexSans.Regular);

        Image image1 = new Image(pdf, "images/gr-map.jpg");
        Image image2 = new Image(pdf, "images/ee-map.png");
        Image image3 = new Image(pdf, "images/rgb24pal.bmp");
        Image image4 = new Image(pdf, "images/cmyk.jpg");

        Page page = new Page(pdf, Letter.PORTRAIT);
        TextLine textLine1 = new TextLine(font, "This is a JPEG image.");
        textLine1.setTextRotation(0);
        textLine1.setLocation(50f, 50f);
        float[] point = textLine1.drawOn(page);
        image1.setLocation(50f, point[1] + 5f).scaleBy(0.25f).drawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);
        TextLine textLine2 = new TextLine(font, "This is a PNG image.");
        textLine2.setTextRotation(0);
        textLine2.setLocation(50f, 50f);
        point = textLine2.drawOn(page);
        image2.setLocation(50f, point[1] + 5f).scaleBy(0.75f).drawOn(page);

        TextLine textLine3 = new TextLine(font, "This is a BMP image.");
        textLine3.setTextRotation(0);
        textLine3.setLocation(50f, 620f);
        point = textLine3.drawOn(page);
        image3.setLocation(50f, point[1] + 5f).scaleBy(0.75f).drawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);
        TextLine textLine4 = new TextLine(font, "This is a CMYK JPEG image, with its inks stored inverted, as Photoshop saves them.");
        textLine4.setLocation(50f, 50f);
        point = textLine4.drawOn(page);
        image4.setLocation(50f, point[1] + 5f).scaleBy(0.425f).drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_24();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_24 => %4d ms%n", time1 - time0);
    }
}   // End of Example_24.java
