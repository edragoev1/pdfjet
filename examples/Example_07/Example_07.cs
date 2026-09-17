/*
 * Example_07.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_07.cs
 * This example adds a "DRAFT" watermark to every page of a two-page
 * PDF/A-3B document. The watermark is drawn first, so the text of the page
 * is drawn over it.
 */
public class Example_07 {
    public Example_07() {
        PDF pdf = new PDF(
                new BufferedStream(new FileStream("Example_07.pdf", FileMode.Create)),
                Compliance.PDF_A_3B);
        pdf.SetTitle("PDF/A-3B compliant PDF");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);
        Font f3 = new Font(pdf, IBMPlexSans.Bold);

        String[] titles = {
            "Project Proposal",
            "Budget and Schedule",
        };
        String[] texts = {
            "This proposal describes a new reporting service that creates invoices, "
                + "statements and delivery notes as PDF documents. The documents are "
                + "archived as PDF/A-3B, so they can be opened and printed exactly "
                + "the same way for many years.\n\n"
                + "The watermark tells every reader that this is a draft. It is drawn "
                + "in light gray behind the text, at an angle from the bottom left "
                + "corner to the top right corner of the page.",
            "The service will be built in three phases over six months. The first "
                + "phase delivers invoices, the second statements, and the third "
                + "delivery notes.\n\n"
                + "The budget and the schedule will be final once the proposal is "
                + "approved. Until then, every page of this document is marked as a draft.",
        };

        for (int i = 0; i < titles.Length; i++) {
            Page page = new Page(pdf, A4.LANDSCAPE);

            // The watermark is drawn before the content of the page.
            f3.SetSize(120f);
            page.AddWatermark(f3, "DRAFT");

            TextLine title = new TextLine(f2, titles[i]);
            title.SetFontSize(28f);
            title.SetLocation(70f, 100f);
            title.DrawOn(page);

            TextBlock textBlock = new TextBlock(f1, texts[i]);
            textBlock.SetFontSize(14f);
            textBlock.SetLineSpacing(1.5f);
            textBlock.SetLocation(70f, 130f);
            textBlock.SetWidth(page.GetWidth() - 140f);
            textBlock.DrawOn(page);

            TextLine footer = new TextLine(f1, "Page " + (i + 1) + " of " + titles.Length);
            footer.SetFontSize(10f);
            footer.SetTextColor(Color.gray);
            footer.SetLocation(70f, page.GetHeight() - 40f);
            footer.DrawOn(page);
        }

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_07();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_07 => {time1 - time0,4} ms");
    }
}   // End of Example_07.cs
