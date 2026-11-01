/*
 * Example_39.cs
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
 * Example_39.cs
 *
 * Draws the pages of a document first and finishes them last. The two map
 * pages are drawn first and the contents page last, and the three are added
 * to the PDF in reading order, the contents first. They are made detached,
 * outside the PDF, so a "Page X of N" footer is drawn on each once the
 * number of pages is known.
 */
public class Example_39 {
    public Example_39() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_39.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Maps");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        String[] titles = {"Europe", "Spain"};
        String[] files = {"resources/images/ee-map.png", "resources/images/spain-admin.jpg"};
        String[] descriptions = {
                "A map of Europe in which the member states of the European Union are shaded, and Turkey, a candidate to join when the map was made, in another shade.",
                "A map of the 17 autonomous communities of Spain and its two autonomous cities, Ceuta and Melilla, with their capitals.",
        };

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
            image.SetAltDescription(descriptions[i]);
            image.ScaleBy((page.GetWidth() - 100f) / image.GetWidth());
            image.SetLocation(50f, 100f);
            image.DrawOn(page);

            mapPages[i] = page;
        }

        // 2. Draw the contents page last, now that the map pages are ready.
        Page contents = new Page(pdf, A4.PORTRAIT, Page.DETACHED);

        TextLine text = new TextLine(f2, "Maps");
        text.SetStructureType(StructElem.H1);
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
                + "of the document: the pages were made detached and are added to the PDF "
                + "in reading order. The footer \"Page X of N\" is drawn on each page last, "
                + "when the number of pages is known.");
        textBlock.SetFontSize(12f);
        textBlock.SetLineSpacing(1.5f);
        textBlock.SetTextColor(Color.dimgray);
        textBlock.SetLocation(50f, y + 20f);
        textBlock.SetWidth(contents.GetWidth() - 100f);
        textBlock.DrawOn(contents);

        // 3. The pages in reading order, and a footer on each, now that their
        // number is known.
        List<Page> pages = new List<Page>();
        pages.Add(contents);
        pages.AddRange(mapPages);
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
        new Example_39();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_39 => {time1 - time0,4} ms");
    }
}   // End of Example_39.cs
