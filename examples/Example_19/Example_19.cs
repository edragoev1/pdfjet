/*
 * Example_19.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_19.cs
 * Using the TextBlock component to draw text next to images. The DrawOn
 * methods of Image and TextBlock return the bottom of what they drew, so each
 * row starts below the taller of the image and the text next to it.
 */
public class Example_19 {
    public Example_19() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_19.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f2, "Text Next to Images");
        text.SetFontSize(22f);
        text.SetLocation(50f, 80f);
        text.DrawOn(page);

        TextBlock textBlock = new TextBlock(f1,
                "Each map below is an Image with a TextBlock next to it. The drawOn "
                + "method of both returns the bottom of what it drew, so every row "
                + "starts below the taller of the two.");
        textBlock.SetFontSize(12f);
        textBlock.SetLineSpacing(1.5f);
        textBlock.SetLocation(50f, 95f);
        textBlock.SetWidth(512f);
        float[] xy = textBlock.DrawOn(page);

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
        for (int i = 0; i < imageFiles.Length; i++) {
            Image image = new Image(pdf, imageFiles[i]);
            image.ResizeWidth(200f);
            image.SetLocation(x1, y);
            float[] imageXY = image.DrawOn(page);

            text = new TextLine(f2, titles[i]);
            text.SetFontSize(14f);
            text.SetLocation(x2, y + f2.GetAscent(14f));
            text.DrawOn(page);

            textBlock = new TextBlock(f1, descriptions[i]);
            textBlock.SetFontSize(11f);
            textBlock.SetLineSpacing(1.5f);
            textBlock.SetLocation(x2, y + 25f);
            textBlock.SetWidth(292f);
            float[] textXY = textBlock.DrawOn(page);

            y = Math.Max(imageXY[1], textXY[1]) + 25f;
        }

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_19();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_19 => {time1 - time0,4} ms");
    }
}   // End of Example_19.cs
