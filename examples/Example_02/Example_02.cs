/*
 * Example_02.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_02.cs
 */
public class Example_02 {
    public Example_02() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_02.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("The Universal Declaration of Human Rights in Four Languages");

        Font f0 = new Font(pdf, IBMPlexSans.Regular);
        f0.SetSize(12f);

        Font f1 = new Font(pdf, IBMPlexSansJP.Regular);
        f1.SetSize(12f);

        Font f2 = new Font(pdf, IBMPlexSansKR.Regular);
        f2.SetSize(12f);

        Font f3 = new Font(pdf, IBMPlexSansSC.Regular);
        f3.SetSize(12f);

        Font f4 = new Font(pdf, IBMPlexSansTC.Regular);
        f4.SetSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        // The heading is in IBM Plex Sans, and the characters it has no glyph for,
        // the name of the language, are in the fallback font.
        new TextLine(f0, "This block is Japanese: 日本語").SetFallbackFont(f1).SetLocation(50f, 50f).DrawOn(page);

        TextBlock textBlock = new TextBlock(
                f1, Content.OfTextFile("data/languages/japanese.txt"));
        textBlock.SetLanguage("ja");
        textBlock.SetLocation(50f, 70f);
        textBlock.SetWidth(512f);
        textBlock.DrawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        new TextLine(f0, "This block is Korean: 한국어").SetFallbackFont(f2).SetLocation(50f, 50f).DrawOn(page);

        textBlock = new TextBlock(
                f2, Content.OfTextFile("data/languages/korean.txt"));
        textBlock.SetLanguage("ko");
        textBlock.SetLocation(50f, 70f);
        textBlock.SetWidth(512f);
        textBlock.DrawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        new TextLine(f0, "This block is Simplified Chinese: 简体中文").SetFallbackFont(f3).SetLocation(50f, 50f).DrawOn(page);

        textBlock = new TextBlock(
                f3, Content.OfTextFile("data/languages/simplified-chinese.txt"));
        textBlock.SetLanguage("zh-Hans");
        textBlock.SetLocation(50f, 70f);
        textBlock.SetWidth(512f);
        textBlock.DrawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        new TextLine(f0, "This block is Traditional Chinese: 繁體中文").SetFallbackFont(f4).SetLocation(50f, 50f).DrawOn(page);

        textBlock = new TextBlock(
                f4, Content.OfTextFile("data/languages/traditional-chinese.txt"));
        textBlock.SetLanguage("zh-Hant");
        textBlock.SetLocation(50f, 70f);
        textBlock.SetWidth(512f);
        textBlock.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_02();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_02 => {time1 - time0,4} ms");
    }
}   // End of Example_02.cs
