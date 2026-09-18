/*
 * Example_21.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;
import com.pdfjet.qrcode.*;

/**
 * Example_21.java
 * This example draws the same web address as a QR code with each of the four
 * error correction levels. A higher level lets a scanner read a code that is
 * more damaged or covered, and leaves room for less data in the code.
 */
public class Example_21 {
    public Example_21() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_21.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("QR Code Error Correction");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f2, "QR Code Error Correction");
        text.setFontSize(22f);
        text.setLocation(70f, 80f);
        text.drawOn(page);

        TextBlock textBlock = new TextBlock(f1,
                "Each QR code below holds the same address, https://pdfjet.com. "
                + "A higher error correction level lets a scanner read the code when "
                + "more of it is damaged or covered, and leaves room for less data, "
                + "so longer data at a higher level makes a larger code.");
        textBlock.setFontSize(12f);
        textBlock.setLineSpacing(1.5f);
        textBlock.setLocation(70f, 95f);
        textBlock.setWidth(470f);
        textBlock.drawOn(page);

        ErrorCorrectionLevel[] levels = {
            ErrorCorrectionLevel.L,
            ErrorCorrectionLevel.M,
            ErrorCorrectionLevel.Q,
            ErrorCorrectionLevel.H,
        };
        String[] names = {
            "L (Low)",
            "M (Medium)",
            "Q (Quartile)",
            "H (High)",
        };
        String[] notes = {
            "About 7% can be restored, 78 bytes fit in this size",
            "About 15% can be restored, 62 bytes fit in this size",
            "About 25% can be restored, 46 bytes fit in this size",
            "About 30% can be restored, 34 bytes fit in this size",
        };

        // Two rows of two codes.
        for (int i = 0; i < levels.length; i++) {
            float x = 70f + (i % 2) * 250f;
            float y = 200f + (i / 2) * 250f;

            QRCode qr = new QRCode("https://pdfjet.com", levels[i]);
            qr.setModuleLength(5f);
            qr.setLocation(x, y);
            float[] xy = qr.drawOn(page);

            text = new TextLine(f2, names[i]);
            text.setFontSize(12f);
            text.setLocation(x, xy[1] + 20f);
            text.drawOn(page);

            text = new TextLine(f1, notes[i]);
            text.setFontSize(10f);
            text.setTextColor(Color.gray);
            text.setLocation(x, xy[1] + 35f);
            text.drawOn(page);
        }

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_21();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_21 => %4d ms%n", time1 - time0);
    }
}   // End of Example_21.java
