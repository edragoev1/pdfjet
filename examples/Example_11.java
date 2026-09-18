/*
 * Example_11.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;
import com.pdfjet.barcodes.*;

/**
 * Example_11.java
 * This example draws a Code 128, a Code 39, a UPC-A and an EAN-13 barcode,
 * each next to a label, and then barcodes drawn from top to bottom and from
 * bottom to top.
 */
public class Example_11 {
    public Example_11() throws Exception {
        PDF pdf = new PDF(
            new BufferedOutputStream(new FileOutputStream("Example_11.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Linear Barcodes");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.setSize(12f);

        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);
        f2.setSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f2, "Linear Barcodes");
        text.setFontSize(22f);
        text.setLocation(70f, 80f);
        text.drawOn(page);

        String[] labels = {
            "Code 128",
            "Code 39",
            "UPC-A",
            "EAN-13",
        };
        String[] notes = {
            "Letters, digits and symbols",
            "Upper case letters and digits",
            "11 digits, the check digit is added",
            "12 digits, the check digit is added",
        };
        Barcode[] barcodes = {
            new Barcode(Barcode.CODE_128, "Hellö, World!"),
            new Barcode(Barcode.CODE_39, "WIKIPEDIA"),
            new Barcode(Barcode.UPC_A, "51234567890"),
            new Barcode(Barcode.EAN_13, "051234567890"),
        };
        // UPC-A and EAN-13 need wider bars for the digits under them.
        float[] moduleLengths = {0.75f, 0.75f, 1f, 1f};

        float y = 130f;
        for (int i = 0; i < barcodes.length; i++) {
            new TextLine(f2, labels[i]).setLocation(70f, y + 15f).drawOn(page);
            TextLine note = new TextLine(f1, notes[i]);
            note.setFontSize(10f);
            note.setTextColor(Color.gray);
            note.setLocation(70f, y + 32f);
            note.drawOn(page);

            Barcode barcode = barcodes[i];
            barcode.setLocation(290f, y);
            barcode.setModuleLength(moduleLengths[i]);
            barcode.setFont(f1);
            float[] xy = barcode.drawOn(page);
            y = xy[1] + 30f;
        }

        // The same barcodes can be drawn from top to bottom and from bottom to top.
        new TextLine(f2, "Vertical barcodes").setLocation(70f, y + 15f).drawOn(page);

        Barcode barcode = new Barcode(Barcode.CODE_128, "G86513JVW0C");
        barcode.setLocation(70f, y + 35f);
        barcode.setModuleLength(0.75f);
        barcode.setDirection(Direction.TOP_TO_BOTTOM);
        barcode.setFont(f1);
        float[] xy = barcode.drawOn(page);

        barcode = new Barcode(Barcode.CODE_39, "CODE39");
        barcode.setLocation(xy[0] + 60f, y + 35f);
        barcode.setModuleLength(0.75f);
        barcode.setDirection(Direction.BOTTOM_TO_TOP);
        barcode.setFont(f1);
        xy = barcode.drawOn(page);

        barcode = new Barcode(Barcode.EAN_13, "051234567890");
        barcode.setLocation(xy[0] + 60f, y + 35f);
        barcode.setModuleLength(1f);
        barcode.setDirection(Direction.BOTTOM_TO_TOP);
        barcode.setFont(f1);
        barcode.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_11();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_11 => %4d ms%n", time1 - time0);
    }
}   // End of Example_11.java
