/*
 * Example_50.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_50.cs
 *
 * Does what programs most often do with an existing PDF: it reads the PDF,
 * stamps every page APPROVED with a date, and adds page numbers at the foot.
 * What is drawn on a page that was read goes on top of its content, and the
 * rest of the document stays as it was.
 */
public class Example_50 {
    public Example_50(String fileName) {
        PDF pdf = new PDF(new BufferedStream(new FileStream("Example_50.pdf", FileMode.Create)));
        List<PDFobj> objects = pdf.Read(
                new BufferedStream(new FileStream(fileName, FileMode.Open, FileAccess.Read)));

        // A font added to an existing PDF is embedded as a subset of the glyphs
        // drawn, as in a new PDF. IBM Plex Sans, which PDFjet carries, is read
        // from the library; the path of a font file of your own reads that file.
        Font bold = new Font(objects, IBMPlexSans.Bold);
        bold.SetSize(14f);
        Font regular = new Font(objects, IBMPlexSans.Regular);
        regular.SetSize(10f);

        // The date is fixed, so that the four ports write the same PDF: a program
        // would stamp the date of the day.
        TextBlock stamp = new TextBlock(bold, "APPROVED\n2026-10-30");
        stamp.SetWidth(120f);
        stamp.SetPadding(6f);
        stamp.SetTextColor(Color.red);
        stamp.SetBorderColor(Color.red);
        stamp.SetTextAlignment(Alignment.CENTER);

        List<PDFobj> pages = pdf.GetPageObjects(objects);
        for (int i = 0; i < pages.Count; i++) {
            Page page = new Page(pdf, pages[i]);
            page.AddResource(bold, objects);
            page.AddResource(regular, objects);

            stamp.SetLocation(page.GetWidth() - 156f, 36f);
            stamp.DrawOn(page);

            String text = "Page " + (i + 1) + " of " + pages.Count;
            float x = (page.GetWidth() - regular.StringWidth(text)) / 2f;
            new TextLine(regular, text).SetLocation(x, page.GetHeight() - 36f).DrawOn(page);

            page.Complete(objects); // The graphics stack is unwinded automatically
        }
        pdf.AddObjects(objects);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_50("resources/data/testPDFs/sample-document.pdf");
        long time1 = sw.ElapsedMilliseconds;
        Console.WriteLine($"Example_50 => {time1 - time0,4} ms");
    }
}   // End of Example_50.cs
