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
 * Using the TextBlock component to draw text next to images. The drawOn
 * methods of Image and TextBlock return the bottom of what they drew, so each
 * row starts below the taller of the image and the text next to it.
 */
public class Example_19 {
    public Example_19() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_19.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Text Next to Images");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f2, "Text Next to Images");
        text.setStructureType(StructElem.H1);
        text.setFontSize(22f);
        text.setLocation(50f, 80f);
        text.drawOn(page);

        TextBlock textBlock = new TextBlock(f1,
                "Each map below is an Image with a TextBlock next to it. The drawOn "
                + "method of both returns the bottom of what it drew, so every row "
                + "starts below the taller of the two.");
        textBlock.setFontSize(12f);
        textBlock.setLineSpacing(1.5f);
        textBlock.setLocation(50f, 95f);
        textBlock.setWidth(512f);
        float[] xy = textBlock.drawOn(page);

        String[] imageFiles = {
            "images/ee-map.png",
            "images/spain-admin.jpg",
        };
        String[] titles = {
            "The European Union",
            "The Regions of Spain",
        };
        String[] descriptions = {
            "A map of Europe with the member states of the European Union and "
                + "the countries that were candidates to join it when the map was "
                + "made. The image is a PNG file of 687 by 710 pixels, drawn 200 "
                + "points wide.",
            "A map of the 17 autonomous communities of Spain and its two "
                + "autonomous cities, Ceuta and Melilla, with their capitals. The "
                + "image is a JPEG file of 2,017 by 2,412 pixels, drawn 200 points "
                + "wide, which prints at more than 700 dots per inch.",
        };

        float x1 = 50f;     // The images
        float x2 = 270f;    // The text next to them
        float y = xy[1] + 25f;
        for (int i = 0; i < imageFiles.length; i++) {
            Image image = new Image(pdf, imageFiles[i]);
            // The text beside a map describes it, so it describes it to a
            // screen reader too.
            image.setAltDescription(descriptions[i]);
            image.resizeWidth(200f);
            image.setLocation(x1, y);
            float[] imageXY = image.drawOn(page);

            text = new TextLine(f2, titles[i]);
            text.setFontSize(14f);
            text.setLocation(x2, y + f2.getAscent(14f));
            text.drawOn(page);

            textBlock = new TextBlock(f1, descriptions[i]);
            textBlock.setFontSize(11f);
            textBlock.setLineSpacing(1.5f);
            textBlock.setLocation(x2, y + 25f);
            textBlock.setWidth(292f);
            float[] textXY = textBlock.drawOn(page);

            y = Math.max(imageXY[1], textXY[1]) + 25f;
        }

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_19();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_19 => %4d ms%n", time1 - time0);
    }
}   // End of Example_19.java
