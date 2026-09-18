/*
 * Example_36.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_36.cs
 * This example draws two map pages first and their contents page last, and
 * then adds the pages to the PDF in reading order, with the contents first.
 */
public class Example_36 {
    public Example_36() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_36.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Maps");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        String[] titles = {"Europe", "Spain"};
        String[] files = {"images/ee-map.png", "images/spain-admin.jpg"};

        // 1. Draw the map pages. They are detached, so they are not in the PDF yet.
        Page[] mapPages = new Page[titles.Length];
        for (int i = 0; i < titles.Length; i++) {
            Page page = new Page(pdf, A4.PORTRAIT, Page.DETACHED);

            TextLine title = new TextLine(f2, titles[i]);
            title.SetFontSize(24f);
            title.SetLocation(50f, 80f);
            title.DrawOn(page);

            // Scale the image to the width of the page between the margins.
            Image image = new Image(pdf, files[i]);
            image.ScaleBy((page.GetWidth() - 100f) / image.GetWidth());
            image.SetLocation(50f, 100f);
            image.DrawOn(page);

            TextLine footer = new TextLine(f1, "Page " + (i + 2));
            footer.SetFontSize(10f);
            footer.SetTextColor(Color.gray);
            footer.SetLocation(50f, page.GetHeight() - 40f);
            footer.DrawOn(page);

            mapPages[i] = page;
        }

        // 2. Draw the contents page last, now that the map pages are ready.
        Page contents = new Page(pdf, A4.PORTRAIT, Page.DETACHED);

        TextLine text = new TextLine(f2, "Maps");
        text.SetFontSize(24f);
        text.SetLocation(50f, 80f);
        text.DrawOn(contents);

        float y = 130f;
        for (int i = 0; i < titles.Length; i++) {
            text = new TextLine(f1, titles[i] + " . . . . . . . . . . page " + (i + 2));
            text.SetFontSize(14f);
            text.SetLocation(50f, y);
            text.DrawOn(contents);
            y += 25f;
        }

        TextBlock textBlock = new TextBlock(f1,
                "This page was drawn after the two map pages, but it is the first page "
                + "of the document, because the pages were created detached and added "
                + "to the PDF in reading order with addPage.");
        textBlock.SetFontSize(12f);
        textBlock.SetLineSpacing(1.5f);
        textBlock.SetTextColor(Color.gray);
        textBlock.SetLocation(50f, y + 20f);
        textBlock.SetWidth(contents.GetWidth() - 100f);
        textBlock.DrawOn(contents);

        // 3. Add the pages in reading order.
        pdf.AddPage(contents);
        for (int i = 0; i < mapPages.Length; i++) {
            pdf.AddPage(mapPages[i]);
        }

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_36();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_36 => {time1 - time0,4} ms");
    }
}   // End of Example_36.cs
