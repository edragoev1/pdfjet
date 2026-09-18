/*
 * Example_20.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;
import com.pdfjet.qrcode.*;

/**
 * Example_20.java
 * This example draws a letterhead: a logo read from a PDF file, a maple leaf
 * drawn as a path with curves, and a QR code with the address of a web site.
 */
class Example_20 {
    public Example_20() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_20.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("PDFjet Software Letterhead");

        // Read the logo from a PDF file, and add its fonts and images to this PDF.
        BufferedInputStream bis = new BufferedInputStream(
                new FileInputStream("data/testPDFs/PDFjetLogo.pdf"));
        List<PDFobj> objects = pdf.read(bis);

        pdf.addResourceObjects(objects);

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.setSize(11f);

        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);
        f2.setSize(11f);

        List<PDFobj> pages = pdf.getPageObjects(objects);
        PDFobj content = pages.get(0).getContentObject(objects);

        Page page = new Page(pdf, Letter.PORTRAIT);

        // Draw the content of the first page of the logo PDF, at half its size.
        float height = 105f;    // The logo height in points.
        float x = 60f;
        float y = 40f;
        float xScale = 0.5f;
        float yScale = 0.5f;

        // The logo is a figure, with an alternate description for screen readers.
        page.addBDC(StructElem.FIGURE, null, "The PDFjet logo");
        page.drawContents(
                content.getData(),
                height,
                x,
                y,
                xScale,
                yScale);
        page.addEMC();

        new TextLine(f2, "PDFjet Software").setLocation(390f, 60f).drawOn(page);
        new TextLine(f1, "Unionville, Ontario, Canada").setLocation(390f, 76f).drawOn(page);
        new TextLine(f1, "https://pdfjet.com").setLocation(390f, 92f).drawOn(page);

        // A thin rule under the letterhead, an artifact.
        page.addArtifactBMC();
        page.setPenColor(Color.darkred);
        page.setPenWidth(1f);
        page.drawLine(60f, 115f, 552f, 115f);
        page.addEMC();

        TextLine text = new TextLine(f2, "The logo on this page was read from a PDF file.");
        text.setFontSize(16f);
        text.setLocation(60f, 170f);
        text.drawOn(page);

        TextBlock textBlock = new TextBlock(f1,
                "The logo is the content of the first page of data/testPDFs/PDFjetLogo.pdf, "
                + "drawn here at half its size with page.drawContents. It stays sharp "
                + "at any zoom, because it is drawn as vector graphics and not as an image.\n\n"
                + "The maple leaf below is a Path with curves, and the QR code "
                + "holds the address of the PDFjet web site.");
        textBlock.setFontSize(12f);
        textBlock.setLineSpacing(1.5f);
        textBlock.setLocation(60f, 185f);
        textBlock.setWidth(490f);
        textBlock.drawOn(page);

        // A maple leaf, drawn with lines and with curves from control points.
        Path path = new Path();

        path.add(new Point(13.0f,  0.0f));
        path.add(new Point(15.5f,  4.5f));

        path.add(new Point(18.0f,  3.5f));
        path.add(new Point(15.5f, 13.5f, Point.CONTROL_POINT_C));
        path.add(new Point(15.5f, 13.5f, Point.CONTROL_POINT_C));
        path.add(new Point(20.5f,  7.5f));

        path.add(new Point(21.0f,  9.5f));
        path.add(new Point(25.0f,  9.0f));
        path.add(new Point(24.0f, 13.0f));
        path.add(new Point(25.5f, 14.0f));
        path.add(new Point(19.0f, 19.0f));
        path.add(new Point(20.0f, 21.5f));
        path.add(new Point(13.5f, 20.5f));
        path.add(new Point(13.5f, 27.0f));
        path.add(new Point(12.5f, 27.0f));
        path.add(new Point(12.5f, 20.5f));
        path.add(new Point( 6.0f, 21.5f));
        path.add(new Point( 7.0f, 19.0f));
        path.add(new Point( 0.5f, 14.0f));
        path.add(new Point( 2.0f, 13.0f));
        path.add(new Point( 1.0f,  9.0f));
        path.add(new Point( 5.0f,  9.5f));

        path.add(new Point( 5.5f,  7.5f));
        path.add(new Point(10.5f, 13.5f, Point.CONTROL_POINT_C));
        path.add(new Point(10.5f, 13.5f, Point.CONTROL_POINT_C));
        path.add(new Point( 8.0f,  3.5f));

        path.add(new Point(10.5f,  4.5f));
        path.setClosed(true);
        path.setStrokeColor(Color.red);
        path.setFillShape(true);
        path.setLocation(60f, 330f);
        path.scaleBy(6f);
        path.drawOn(page);

        QRCode qr = new QRCode(
                "https://pdfjet.com",
                ErrorCorrectionLevel.M);   // Medium
        qr.setModuleLength(5f);
        qr.setLocation(300f, 340f);
        float[] xy = qr.drawOn(page);

        // A frame around the QR code, an artifact.
        page.addArtifactBMC();
        page.setPenColor(Color.lightgray);
        page.setPenWidth(0.5f);
        page.drawRect(290f, 330f, xy[0] - 280f, xy[1] - 320f);
        page.addEMC();

        TextLine caption = new TextLine(f1, "A Path with curves");
        caption.setTextColor(Color.gray);
        caption.setLocation(60f, xy[1] + 35f);
        caption.drawOn(page);

        caption = new TextLine(f1, "Scan to visit https://pdfjet.com");
        caption.setTextColor(Color.gray);
        caption.setLocation(290f, xy[1] + 35f);
        caption.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_20();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_20 => %4d ms%n", time1 - time0);
    }
}   // End of Example_20.java
