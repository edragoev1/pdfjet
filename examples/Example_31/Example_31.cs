/*
 * Example_31.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_31.cs
 * This example draws Hindi and Marathi text, and filled rectangles, first
 * opaque and then half transparent, so the colors mix where they overlap.
 */
public class Example_31 {
    public Example_31() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_31.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, IBMPlexSansDevanagari.Regular);
        f1.SetSize(13f);

        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);
        f2.SetSize(14f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        // Hindi: the second line of the file, after its label.
        new TextLine(f2, "Hindi").SetLocation(50f, 60f).DrawOn(page);
        List<String> lines = Content.LinesOfTextFile("data/languages/devanagari.txt");
        TextBlock textBlock = new TextBlock(f1, lines[1]);
        textBlock.SetLineSpacing(1.3f);
        textBlock.SetLocation(50f, 70f);
        textBlock.SetWidth(510f);
        float[] xy = textBlock.DrawOn(page);

        new TextLine(f2, "Marathi").SetLocation(50f, xy[1] + 35f).DrawOn(page);
        textBlock = new TextBlock(f1, Content.OfTextFile("data/languages/marathi.txt"));
        textBlock.SetLineSpacing(1.3f);
        textBlock.SetLocation(50f, xy[1] + 45f);
        textBlock.SetWidth(510f);
        xy = textBlock.DrawOn(page);

        float y = xy[1] + 50f;
        int[] colors = {Color.blue, Color.green, Color.red};

        // Opaque rectangles: each one hides the one under it.
        new TextLine(f2, "Opaque").SetLocation(50f, y).DrawOn(page);
        for (int i = 0; i < colors.Length; i++) {
            page.SetBrushColor(colors[i]);
            page.FillRect(50f + i * 60f, y + 15f + i * 30f, 120f, 120f);
        }

        // Half transparent rectangles: the colors mix where they overlap.
        new TextLine(f2, "50% transparent").SetLocation(320f, y).DrawOn(page);
        page.SaveGraphicsState();
        GraphicsState gs = new GraphicsState();
        gs.SetAlphaStroking(0.5f);      // The stroking alpha constant
        gs.SetAlphaNonStroking(0.5f);   // The non-stroking alpha constant
        page.SetGraphicsState(gs);
        for (int i = 0; i < colors.Length; i++) {
            page.SetBrushColor(colors[i]);
            page.FillRect(320f + i * 60f, y + 15f + i * 30f, 120f, 120f);
        }
        page.RestoreGraphicsState();

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_31();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_31 => {time1 - time0,4} ms");
    }
}   // End of Example_31.cs
