/*
 * Example_31.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_31.cs
 * This example draws with transparency. A GraphicsState sets the alpha of the
 * fills and of the strokes: opaque shapes hide what is under them, transparent
 * ones mix with it, and the stroke of a shape can be more or less transparent
 * than its fill.
 */
public class Example_31 {
    public Example_31() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_31.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f2, "Transparency");
        text.SetFontSize(22f);
        text.SetLocation(50f, 80f);
        text.DrawOn(page);

        TextBlock textBlock = new TextBlock(f1,
                "A GraphicsState sets the alpha of the fills and of the strokes that "
                + "follow it, until the graphics state is restored. Opaque shapes hide "
                + "what is under them, transparent ones mix with it, and the stroke of "
                + "a shape can be more or less transparent than its fill.");
        textBlock.SetFontSize(12f);
        textBlock.SetLineSpacing(1.5f);
        textBlock.SetLocation(50f, 95f);
        textBlock.SetWidth(512f);
        float[] xy = textBlock.DrawOn(page);

        float y = xy[1] + 30f;
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

        // The same blue over a gray bar at four levels of alpha.
        y += 245f;
        new TextLine(f2, "Fill alpha").SetLocation(50f, y).DrawOn(page);
        page.SetBrushColor(Color.gray);
        page.FillRect(50f, y + 55f, 506f, 30f);
        float[] alphas = {0.25f, 0.5f, 0.75f, 1f};
        for (int i = 0; i < alphas.Length; i++) {
            float x = 50f + i * 132f;
            page.SaveGraphicsState();
            gs = new GraphicsState();
            gs.SetAlphaNonStroking(alphas[i]);
            page.SetGraphicsState(gs);
            page.SetBrushColor(Color.blue);
            page.FillRect(x, y + 15f, 110f, 110f);
            page.RestoreGraphicsState();

            text = new TextLine(f1, Math.Round(alphas[i] * 100f) + "%");
            text.SetFontSize(10f);
            text.SetTextColor(Color.gray);
            text.SetLocation(x, y + 140f);
            text.DrawOn(page);
        }

        // A thick stroke and a fill, each transparent on its own: the stroke
        // shows the fill through it, then the fill shows the stroke.
        y += 175f;
        new TextLine(f2, "Stroke alpha and fill alpha").SetLocation(50f, y).DrawOn(page);
        String[] labels = {
            "Stroke 25%, fill 100%",
            "Stroke 100%, fill 25%",
            "Stroke 50%, fill 50%",
        };
        float[,] strokeAndFill = {{0.25f, 1f}, {1f, 0.25f}, {0.5f, 0.5f}};
        for (int i = 0; i < labels.Length; i++) {
            float x = 100f + i * 180f;
            page.SaveGraphicsState();
            gs = new GraphicsState();
            gs.SetAlphaStroking(strokeAndFill[i, 0]);
            gs.SetAlphaNonStroking(strokeAndFill[i, 1]);
            page.SetGraphicsState(gs);
            page.SetBrushColor(Color.red);
            page.FillCircle(x, y + 65f, 40f);
            page.SetPenColor(Color.blue);
            page.SetPenWidth(16f);
            page.DrawCircle(x, y + 65f, 40f);
            page.RestoreGraphicsState();

            text = new TextLine(f1, labels[i]);
            text.SetFontSize(10f);
            text.SetTextColor(Color.gray);
            text.SetLocation(x - text.GetWidth() / 2f, y + 130f);
            text.DrawOn(page);
        }

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
