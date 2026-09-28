/*
 * Example_32.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

using PDFjet.NET;

/**
 * Example_32.cs
 */
public class Example_32 {
    public Example_32() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_32.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("The Source Code of Example_02");

        Font font = new Font(pdf, JetBrainsMono.Regular);
        // The longest line of Example_02 is 107 characters: at 8 points a
        // landscape page holds them.
        font.SetSize(8f);

        Dictionary<String, Int32> colors = new Dictionary<String, Int32>();
        colors["new"] = Color.firebrick;
        colors["class"] = Color.blue;
        colors["void"] = Color.green;
        float[] grayColor = new float[] {0.2f, 0.2f, 0.2f};

        Page page = new Page(pdf, Letter.LANDSCAPE);
        float x = 50f;
        float y = 50f;
        // A heading on the first page, which PAC asks a document for
        new TextLine(font, "The Source Code of Example_02")
                .SetStructureType(StructElem.H1)
                .SetFontSize(14f)
                .SetLocation(x, 32f)
                .DrawOn(page);
        float leading = font.GetBodyHeight();
        List<String> lines = Content.LinesOfTextFile("examples/Example_02.java");
        foreach (String line in lines) {
            new TextLine(font, line).SetTextColor(grayColor).SetHighlightColors(colors).SetLocation(x, y).DrawOn(page);
            y += leading;
            if (y > (page.GetHeight() - 20f)) {
                page = new Page(pdf, Letter.LANDSCAPE);
                y = 50f;
            }
        }

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_32();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_32 => {time1 - time0,4} ms");
    }
}   // End of Example_32.cs
