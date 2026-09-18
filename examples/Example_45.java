/*
 * Example_45.java
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
 * Example_45.java
 * Using the Form and Field classes to draw a shipment request. A field at x = 0
 * starts a new row, the other fields of the row start at their own x, and a
 * field with an empty label continues the value above it on a new line.
 */
public class Example_45 {
    public Example_45() throws Exception {

        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_45.pdf")));

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f2, "Shipment Request");
        text.setFontSize(22f);
        text.setLocation(56f, 80f);
        text.drawOn(page);

        TextBlock textBlock = new TextBlock(f1,
                "A Form draws its fields in rows. A field at x = 0 starts a new row, "
                + "the other fields of the row start at their own x, and a field with "
                + "an empty label continues the value above it on a new line.");
        textBlock.setFontSize(12f);
        textBlock.setLineSpacing(1.5f);
        textBlock.setLocation(56f, 95f);
        textBlock.setWidth(500f);
        float[] xy = textBlock.drawOn(page);

        float w = 500f; // The width of the form

        List<Field> fields = new ArrayList<Field>();
        fields.add(new Field(   0f, "Sender", "Maple Leaf Instruments Ltd."));
        fields.add(new Field(   0f, "Street Address", "480 King Street West"));
        fields.add(new Field(6*w/8, "Suite", "1200"));
        fields.add(new Field(   0f, "City", "Toronto"));
        fields.add(new Field(3*w/8, "Province", "Ontario"));
        fields.add(new Field(5*w/8, "Postal Code", "M5V 1L7"));
        fields.add(new Field(6*w/8, "Country", "Canada"));
        fields.add(new Field(   0f, "Recipient", "Nordic Sensor Labs AB"));
        fields.add(new Field(   0f, "Street Address", "Drottninggatan 55"));
        fields.add(new Field(6*w/8, "Floor", "3"));
        fields.add(new Field(   0f, "City", "Stockholm"));
        fields.add(new Field(5*w/8, "Postal Code", "111 21"));
        fields.add(new Field(6*w/8, "Country", "Sweden"));
        fields.add(new Field(   0f, "Contact", "Anna Lindqvist"));
        fields.add(new Field(3*w/8, "Email", "anna.lindqvist@example.com"));
        fields.add(new Field(   0f, "Contents", "Two calibrated pressure sensors"));
        fields.add(new Field(5*w/8, "Weight", "3.2 kg"));
        fields.add(new Field(6*w/8, "Declared Value", "CAD 1,450.00"));
        fields.add(new Field(   0f, "Instructions",
                "Keep upright and away from magnets. Deliver on a weekday"));
        fields.add(new Field(   0f, "", "between 9:00 and 17:00, to the reception on the third floor."));

        xy = new Form(fields)
                .setLabelFont(f1)
                .setLabelFontSize(8f)
                .setLabelColor(Color.gray)
                .setValueFont(f2)
                .setValueFontSize(10f)
                .setValueColor(Color.black)
                .setLocation(56f, xy[1] + 20f)
                .setWidth(w)
                .setStrokeWidth(0.5f)
                .drawOn(page);

        text = new TextLine(f1, "The recipient signs for the package on delivery.");
        text.setFontSize(10f);
        text.setTextColor(Color.gray);
        text.setLocation(56f, xy[1] + 20f);
        text.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_45();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_45 => %4d ms%n", time1 - time0);
    }
}   // End of Example_45.java
