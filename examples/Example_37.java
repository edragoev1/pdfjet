/*
 * Example_37.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;
import com.pdfjet.qrcode.*;
import com.pdfjet.datamatrix.*;

/**
 * Example_37.java
 *
 * Draws two-dimensional barcodes. The same web address as a QR code with
 * each of the four error correction levels, where a higher level lets a
 * scanner read a code that is more damaged or covered and leaves room for
 * less data; GS1 data as a GS1 Digital Link in a QR code; and Data Matrix
 * symbols of a web address, text in UTF-8, a rectangle, a larger symbol and
 * GS1 data.
 */
final public class Example_37 {
    public Example_37() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_37.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("QR Codes and Data Matrix Barcodes");

        drawQRCodes(pdf);
        drawDataMatrix(pdf);

        pdf.complete();
    }

    private void drawQRCodes(PDF pdf) throws Exception {
        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f2, "QR Code Error Correction");
        text.setStructureType(StructElem.H1);
        text.setFontSize(22f);
        text.setLocation(70f, 80f);
        text.drawOn(page);

        TextBlock textBlock = new TextBlock(f1,
                "Each QR code below holds the same address, the home page of PDFjet. "
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
            text.setTextColor(Color.dimgray);
            text.setLocation(x, xy[1] + 35f);
            text.drawOn(page);
        }

        // GS1 data as a GS1 Digital Link, a web address an ordinary QR code carries.
        Page page2 = new Page(pdf, Letter.PORTRAIT);
        text = new TextLine(f2, "GS1 Digital Link");
        text.setStructureType(StructElem.H2);
        text.setFontSize(18f);
        text.setLocation(70f, 80f);
        text.drawOn(page2);

        String link = GS1.digitalLink("https://id.gs1.org", "(01)09506000134352(10)ABC123(17)261231");
        TextBlock note = new TextBlock(f1, "The GS1 data (01)09506000134352(10)ABC123(17)261231, a GTIN, a batch and an expiry date, "
                + "is this web address, a GS1 Digital Link, which an ordinary QR code carries and any phone opens:");
        note.setFontSize(12f);
        note.setLineSpacing(1.5f);
        note.setLocation(70f, 95f);
        note.setWidth(470f);
        float[] corner = note.drawOn(page2);

        // The address as a link, not as text in the paragraph, which PDF/UA
        // checkers take for a link without its Link element
        text = new TextLine(f1, link);
        text.setFontSize(12f);
        text.setTextColor(Color.blue);
        text.setUnderline(true);
        text.setURIAction(link);
        text.setLocation(70f, corner[1] + 20f);
        text.drawOn(page2);

        QRCode code = new QRCode(link, ErrorCorrectionLevel.M);
        code.setModuleLength(4f);
        code.setLocation(70f, corner[1] + 50f);
        code.drawOn(page2);
    }

    private void drawDataMatrix(PDF pdf) throws Exception {
        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.setSize(10f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        // The heading of the document, above the symbols
        new TextLine(f1, "Data Matrix Barcodes")
                .setStructureType(StructElem.H1)
                .setFontSize(18f)
                .setLocation(50f, 36f)
                .drawOn(page);

        DataMatrix barcode = new DataMatrix("https://github.com/edragoev1/pdfjet");
        barcode.setLocation(50f, 50f);
        barcode.setModuleLength(3f);
        float[] xy = barcode.drawOn(page);
        TextLine caption = new TextLine(f1, "A web address");
        caption.setLocation(50f, xy[1] + 20f);
        caption.drawOn(page);

        barcode = new DataMatrix("Grüße aus München! こんにちは 😀");
        barcode.setLocation(300f, 50f);
        barcode.setModuleLength(3f);
        xy = barcode.drawOn(page);
        caption = new TextLine(f1, "Text in UTF-8");
        caption.setLocation(300f, xy[1] + 20f);
        caption.drawOn(page);

        barcode = new DataMatrix("PDFjet 9.1.1", DataMatrix.RECTANGLE);
        barcode.setLocation(50f, 250f);
        barcode.setModuleLength(4f);
        barcode.setModuleColor(Color.blue);
        xy = barcode.drawOn(page);
        caption = new TextLine(f1, "A rectangular symbol");
        caption.setLocation(50f, xy[1] + 20f);
        caption.drawOn(page);

        StringBuilder sb = new StringBuilder();
        for (int i = 1; i <= 20; i++) {
            sb.append("Line ").append(i).append(" of a longer text in a larger symbol.\n");
        }
        barcode = new DataMatrix(sb.toString());
        barcode.setLocation(300f, 250f);
        barcode.setModuleLength(2f);
        xy = barcode.drawOn(page);
        caption = new TextLine(f1, "A larger symbol");
        caption.setLocation(300f, xy[1] + 20f);
        caption.drawOn(page);

        barcode = DataMatrix.fromGS1("(01)09506000134352(17)261231(10)ABC123(21)XYZ-42");
        barcode.setLocation(50f, 400f);
        barcode.setModuleLength(3f);
        xy = barcode.drawOn(page);
        // On two lines, clear of the larger symbol at its right
        caption = new TextLine(f1, "GS1: a GTIN, an expiry date,");
        caption.setLocation(50f, xy[1] + 20f);
        caption.drawOn(page);
        caption = new TextLine(f1, "a batch and a serial number");
        caption.setLocation(50f, xy[1] + 20f + 14f);  // A line of 10 points, and its spacing
        caption.drawOn(page);
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_37();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_37 => %4d ms%n", time1 - time0);
    }
}   // End of Example_37.java
