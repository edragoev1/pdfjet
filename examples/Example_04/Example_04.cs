/*
 * Example_04.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_04.cs
 */
public class Example_04 {
    public Example_04() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_04.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("The Universal Declaration of Human Rights in Japanese and Korean");

        Font f0 = new Font(pdf, IBMPlexSans.Regular);
        f0.SetSize(12f);

        // The .otf: a page of Japanese or Korean draws hundreds of glyphs, whose CFF
        // outlines make a smaller subset than the .ttf's, about a quarter.
        Font f1 = new Font(pdf, "fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf");
        f1.SetSize(12f);

        Font f2 = new Font(pdf, "fonts/IBMPlexSansKR/IBMPlexSansKR-Regular.otf");
        f2.SetSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        // The heading is in IBM Plex Sans, and the characters it has no glyph for,
        // the name of the language, are in the fallback font.
        // The line above each block is its heading
        new TextLine(f0, "This block is Japanese: 日本語").SetFallbackFont(f1).SetStructureType(StructElem.H1).SetLocation(50f, 50f).DrawOn(page);

        TextBlock textBlock = new TextBlock(
                f1, Content.OfTextFile("data/languages/japanese.txt"));
        textBlock.SetLanguage("ja");
        textBlock.SetLocation(50f, 70f);
        textBlock.SetWidth(512f);
        textBlock.DrawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        new TextLine(f0, "This block is Korean: 한국어").SetFallbackFont(f2).SetStructureType(StructElem.H1).SetLocation(50f, 50f).DrawOn(page);

        textBlock = new TextBlock(
                f2, Content.OfTextFile("data/languages/korean.txt"));
        textBlock.SetLanguage("ko");
        textBlock.SetLocation(50f, 70f);
        textBlock.SetWidth(512f);
        textBlock.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_04();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_04 => {time1 - time0,4} ms");
    }
}   // End of Example_04.cs
