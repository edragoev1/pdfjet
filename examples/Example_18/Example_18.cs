/*
 * Example_18.cs
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
 * Example_18.cs
 * This example shows how to write "Page X of N" footer on every page.
 */
public class Example_18 {
    public Example_18() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_18.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("How to Number Pages");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        String[] titles = {
            "1. Create the pages",
            "2. Draw the content",
            "3. Add the footers",
        };
        String[] texts = {
            "The total number of pages is not known until all the content is drawn. "
                + "That is why the pages in this document are created with Page.DETACHED: "
                + "they are not added to the PDF yet, and they are kept in a list instead.",
            "Each page gets its content, like this heading and this paragraph. "
                + "Long documents would flow their text or tables from page to page here.",
            "Now the list holds every page, so its size is the total number of pages. "
                + "The footer \"Page X of N\" is drawn on each page, "
                + "and then all the pages are added to the PDF with addPages.",
        };

        List<Page> pages = new List<Page>();
        for (int i = 0; i < titles.Length; i++) {
            Page page = new Page(pdf, A4.PORTRAIT, Page.DETACHED);

            TextLine header = new TextLine(f1, "How to number pages");
            header.SetFontSize(10f);
            header.SetTextColor(Color.gray);
            header.SetLocation(70f, 50f);
            header.DrawOn(page);

            Line line = new Line(70f, 60f, page.GetWidth() - 70f, 60f);
            line.SetStrokeColor(Color.lightgray);
            line.DrawOn(page);

            TextLine title = new TextLine(f2, titles[i]);
            title.SetStructureType(StructElem.H1);
            title.SetFontSize(20f);
            title.SetLocation(70f, 120f);
            title.DrawOn(page);

            TextBlock textBlock = new TextBlock(f1, texts[i]);
            textBlock.SetFontSize(12f);
            textBlock.SetLineSpacing(1.5f);
            textBlock.SetLocation(70f, 140f);
            textBlock.SetWidth(page.GetWidth() - 140f);
            textBlock.DrawOn(page);

            pages.Add(page);
        }

        float fontSize = 10f;
        for (int i = 0; i < pages.Count; i++) {
            Page page = pages[i];

            Line line = new Line(70f, page.GetHeight() - 60f,
                    page.GetWidth() - 70f, page.GetHeight() - 60f);
            line.SetStrokeColor(Color.lightgray);
            line.DrawOn(page);

            // A page number is an artifact: a screen reader skips it.
            String footer = "Page " + (i + 1) + " of " + pages.Count;
            page.AddArtifactBMC();
            page.SetBrushColor(Color.black);
            page.DrawString(
                    f1,
                    fontSize,
                    footer,
                    (page.GetWidth() - f1.StringWidth(fontSize, footer))/2f,
                    page.GetHeight() - 40f);
            page.AddEMC();
        }
        pdf.AddPages(pages);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_18();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_18 => {time1 - time0,4} ms");
    }

}   // End of Example_18.cs
