/*
 * Example_20.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_20.cs
 * This example draws a letterhead: a logo read from a PDF file, a maple leaf
 * drawn as a path with curves, and a QR code with the address of a web site.
 */
class Example_20 {
    public Example_20() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_20.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("PDFjet Software Letterhead");

        // Read the logo from a PDF file, and add the resources of its pages, its
        // fonts and images, to this PDF. The logo itself is drawn with paths and
        // has none.
        BufferedStream bis = new BufferedStream(
                new FileStream("data/testPDFs/PDFjetLogo.pdf", FileMode.Open));
        List<PDFobj> objects = pdf.Read(bis);

        pdf.AddResourceObjects(objects);

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.SetSize(11f);

        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);
        f2.SetSize(11f);

        List<PDFobj> pages = pdf.GetPageObjects(objects);
        PDFobj content = pages[0].GetContentObject(objects);

        Page page = new Page(pdf, Letter.PORTRAIT);

        // Draw the content of the first page of the logo PDF, at half its size.
        float height = 72f;     // The logo height in points.
        float x = 60f;
        float y = 40f;
        float xScale = 0.5f;
        float yScale = 0.5f;

        // The logo is a figure, with an alternate description for screen readers.
        page.AddBDC(StructElem.FIGURE, null, "The PDFjet logo");
        page.DrawContents(
                content.GetData(),
                height,
                x,
                y,
                xScale,
                yScale);
        page.AddEMC();

        new TextLine(f2, "PDFjet Software").SetLocation(390f, 60f).DrawOn(page);
        new TextLine(f1, "Unionville, Ontario, Canada").SetLocation(390f, 76f).DrawOn(page);
        new TextLine(f1, "https://pdfjet.com").SetLocation(390f, 92f).DrawOn(page);

        // A thin rule under the letterhead, an artifact.
        page.AddArtifactBMC();
        page.SetPenColor(Color.darkred);
        page.SetPenWidth(1f);
        page.DrawLine(60f, 115f, 552f, 115f);
        page.AddEMC();

        TextLine text = new TextLine(f2, "The logo on this page was read from a PDF file.");
        text.SetFontSize(16f);
        text.SetLocation(60f, 170f);
        text.DrawOn(page);

        TextBlock textBlock = new TextBlock(f1,
                "The logo is the content of the first page of data/testPDFs/PDFjetLogo.pdf, "
                + "drawn here at half its size with page.drawContents. It stays sharp "
                + "at any zoom, because it is drawn as vector graphics and not as an image.\n\n"
                + "The maple leaf below is a Path with curves, and the QR code "
                + "holds the address of the PDFjet web site.");
        textBlock.SetFontSize(12f);
        textBlock.SetLineSpacing(1.5f);
        textBlock.SetLocation(60f, 185f);
        textBlock.SetWidth(490f);
        textBlock.DrawOn(page);

        // A maple leaf, drawn with lines and with curves from control points.
        PDFjet.NET.Path path = new PDFjet.NET.Path();

        path.Add(new Point(13.0f,  0.0f));
        path.Add(new Point(15.5f,  4.5f));

        path.Add(new Point(18.0f,  3.5f));
        path.Add(new Point(15.5f, 13.5f, Point.CONTROL_POINT_C));
        path.Add(new Point(15.5f, 13.5f, Point.CONTROL_POINT_C));
        path.Add(new Point(20.5f,  7.5f));

        path.Add(new Point(21.0f,  9.5f));
        path.Add(new Point(25.0f,  9.0f));
        path.Add(new Point(24.0f, 13.0f));
        path.Add(new Point(25.5f, 14.0f));
        path.Add(new Point(19.0f, 19.0f));
        path.Add(new Point(20.0f, 21.5f));
        path.Add(new Point(13.5f, 20.5f));
        path.Add(new Point(13.5f, 27.0f));
        path.Add(new Point(12.5f, 27.0f));
        path.Add(new Point(12.5f, 20.5f));
        path.Add(new Point( 6.0f, 21.5f));
        path.Add(new Point( 7.0f, 19.0f));
        path.Add(new Point( 0.5f, 14.0f));
        path.Add(new Point( 2.0f, 13.0f));
        path.Add(new Point( 1.0f,  9.0f));
        path.Add(new Point( 5.0f,  9.5f));

        path.Add(new Point( 5.5f,  7.5f));
        path.Add(new Point(10.5f, 13.5f, Point.CONTROL_POINT_C));
        path.Add(new Point(10.5f, 13.5f, Point.CONTROL_POINT_C));
        path.Add(new Point( 8.0f,  3.5f));

        path.Add(new Point(10.5f,  4.5f));
        path.SetClosed(true);
        path.SetStrokeColor(Color.red);
        path.SetFillShape(true);
        path.SetLocation(60f, 330f);
        path.ScaleBy(6f);
        path.DrawOn(page);

        QRCode qr = new QRCode(
                "https://pdfjet.com",
                ErrorCorrectionLevel.M);   // Medium
        qr.SetModuleLength(5f);
        qr.SetLocation(300f, 340f);
        float[] xy = qr.DrawOn(page);

        // A frame around the QR code, an artifact.
        page.AddArtifactBMC();
        page.SetPenColor(Color.lightgray);
        page.SetPenWidth(0.5f);
        page.DrawRect(290f, 330f, xy[0] - 280f, xy[1] - 320f);
        page.AddEMC();

        TextLine caption = new TextLine(f1, "A Path with curves");
        caption.SetTextColor(Color.gray);
        caption.SetLocation(60f, xy[1] + 35f);
        caption.DrawOn(page);

        caption = new TextLine(f1, "Scan to visit https://pdfjet.com");
        caption.SetTextColor(Color.gray);
        caption.SetLocation(290f, xy[1] + 35f);
        caption.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_20();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_20 => {time1 - time0,4} ms");
    }
}   // End of Example_20.cs
