/*
 * Example_24.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_24.cs
 */
public class Example_24 {
    public Example_24() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_24.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("JPEG, PNG and BMP Images");

        Font font = new Font(pdf, IBMPlexSans.Regular);

        Image image1 = new Image(pdf, "images/gr-map.jpg");
        image1.SetAltDescription(
                "A map of Greece with its cities, roads and airports, the Ionian Sea to the west, the Aegean Sea to the east and Crete to the south.");
        Image image2 = new Image(pdf, "images/ee-map.png");
        image2.SetAltDescription(
                "A map of Europe in which the member states of the European Union are shaded, and Turkey, a candidate to join when the map was made, in another shade.");
        Image image3 = new Image(pdf, "images/rgb24pal.bmp");
        image3.SetAltDescription(
                "The letters BMP in white over bars of red, green, blue, yellow, magenta and cyan.");
        Image image4 = new Image(pdf, "images/cmyk.jpg");
        image4.SetAltDescription(
                "A CMYK test chart: rows of cyan, magenta, yellow and black from 0 to 100 percent in steps of 10, and bars of red, green, blue and rich black.");

        Page page = new Page(pdf, Letter.PORTRAIT);
        TextLine textLine1 = new TextLine(font, "This is a JPEG image.");
        textLine1.SetTextRotation(0);
        textLine1.SetLocation(50f, 50f);
        float[] point = textLine1.DrawOn(page);
        image1.SetLocation(50f, point[1] + 5f).ScaleBy(0.25f).DrawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);
        TextLine textLine2 = new TextLine(font, "This is a PNG image.");
        textLine2.SetTextRotation(0);
        textLine2.SetLocation(50f, 50f);
        point = textLine2.DrawOn(page);
        image2.SetLocation(50f, point[1] + 5f).ScaleBy(0.75f).DrawOn(page);

        TextLine textLine3 = new TextLine(font, "This is a BMP image.");
        textLine3.SetTextRotation(0);
        textLine3.SetLocation(50f, 620f);
        point = textLine3.DrawOn(page);
        image3.SetLocation(50f, point[1] + 5f).ScaleBy(0.75f).DrawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);
        TextLine textLine4 = new TextLine(font, "This is a CMYK JPEG image, with its inks stored inverted, as Photoshop saves them.");
        textLine4.SetLocation(50f, 50f);
        point = textLine4.DrawOn(page);
        // The image is 300 DPI, so its size is 72/300 of its pixels.
        image4.SetLocation(50f, point[1] + 5f).ScaleBy(0.425f*300f/72f).DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_24();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_24 => {time1 - time0,4} ms");
    }
}   // End of Example_24.cs
