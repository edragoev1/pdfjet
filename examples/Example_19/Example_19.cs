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
 * Using the TextBlock component to draw text next to images.
 */
public class Example_19 {
    public Example_19() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_19.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.SetSize(10f);

        Font f2 = new Font(pdf, IBMPlexSansTC.Regular);
        f2.SetSize(10f);

        Page page = new Page(pdf, Letter.PORTRAIT);
        // Columns x coordinates
        float x1 = 50f;
        float y1 = 50f;
        float x2 = 300f;
        float w2 = 300f;    // Width of the second column

        Image image1 = new Image(pdf, "images/ee-map.png");
        Image image2 = new Image(pdf, "images/spain-admin.jpg");

        // Draw the first image
        image1.SetLocation(x1, y1);
        image1.ScaleBy(0.3f);
        image1.DrawOn(page);

        TextBlock textBlock = new TextBlock(f1, Content.OfTextFile("data/calculus-short.txt"));
        textBlock.SetLocation(x2, y1);
        textBlock.SetWidth(w2);
        textBlock.SetBorderColor(Color.black);
        float[] xy = textBlock.DrawOn(page);

        // Draw the second image
        image2.SetLocation(x1, xy[1] + 10f);
        image2.ScaleBy(0.1f);
        image2.DrawOn(page);

        textBlock = new TextBlock(f1, Content.OfTextFile("data/physics.txt"));
        textBlock.SetLocation(x2, xy[1] + 10f);
        textBlock.SetWidth(w2);
        textBlock.SetBorderColor(Color.black);
        xy = textBlock.DrawOn(page);

        Rect rect = new Rect(xy[0], xy[1], 20f, 20f);
        rect.SetBorderColor(Color.black);
        rect.DrawOn(page);

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
