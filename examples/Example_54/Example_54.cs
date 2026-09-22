/*
 * Example_54.cs
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
 * Example_54.cs
 * A Markdown text, data/markdown/pdfjet.md, drawn down the pages by Markdown
 * as a PDF/UA document: its headings, paragraphs, lists, quote, code, table
 * and image, each tagged for screen readers, with a number at the foot of
 * every page. The image is read from data/markdown, the directory that
 * SetImageDirectory names.
 */
public class Example_54 {
    public Example_54() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_54.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("PDFjet");

        Font regular = new Font(pdf, IBMPlexSans.Regular).SetSize(11f);
        Font bold = new Font(pdf, IBMPlexSans.Bold).SetSize(11f);
        Font italic = new Font(pdf, IBMPlexSans.Italic).SetSize(11f);
        Font boldItalic = new Font(pdf, IBMPlexSans.BoldItalic).SetSize(11f);
        Font code = new Font(pdf, IBMPlexMono.Regular).SetSize(9.5f);

        Markdown markdown = new Markdown(regular, bold, italic, boldItalic, code);
        markdown.SetImageDirectory("data/markdown");
        List<Page> pages = new List<Page>();
        markdown.DrawOn(pdf, Content.OfTextFile("data/markdown/pdfjet.md"), pages, Letter.PORTRAIT);

        for (int i = 0; i < pages.Count; i++) {
            TextLine number = new TextLine(regular, (i + 1).ToString());
            number.SetFontSize(9f);
            pages[i].AddFooter(number, 36f);
        }
        pdf.AddPages(pages);
        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_54();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_54 => {time1 - time0,4} ms");
    }
}   // End of Example_54.cs
