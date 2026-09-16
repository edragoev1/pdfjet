/*
 * Example_50.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_50.cs
 *
 * Fills in the fields of an existing PDF form: it reads the PDF, adds an image,
 * two fonts read from files and the core font Helvetica as resources of a page,
 * and writes the text on that page.
 *
 * The core font is added with AddResource(CoreFont, objects), which writes a
 * font dictionary that names the font and nothing else. The advantage is that
 * the page grows by a few hundred bytes and needs no font file, which suits a
 * stamp or a field value added to a document that already exists. The
 * disadvantages are those of every font that is not embedded: the viewer draws
 * the text with its own Helvetica, only the WinAnsi characters can be drawn,
 * and the document cannot claim PDF/A or PDF/UA compliance. The two embedded
 * fonts of this example show the alternative.
 */
public class Example_50 {
    public Example_50(String fileNumber, String fileName) {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_" + fileNumber + ".pdf", FileMode.Create)));

        List<PDFobj> objects = pdf.Read(
                new BufferedStream(new FileStream(fileName, FileMode.Open, FileAccess.Read)));

        Image image = new Image(objects,
                new BufferedStream(new FileStream(
                        "images/qrcode.png", FileMode.Open, FileAccess.Read)));
        image.SetLocation(495f, 65f);
        image.ScaleBy(0.40f);

        Font f1 = new Font(objects,
                new FileStream(IBMPlexSans.Regular,
                        FileMode.Open,
                        FileAccess.Read));
        f1.SetSize(12f);

        Font f2 = new Font(objects,
                new FileStream(IBMPlexSans.Bold,
                        FileMode.Open,
                        FileAccess.Read));
        f2.SetSize(12f);

        List<PDFobj> pages = pdf.GetPageObjects(objects);
        Page page = new Page(pdf, pages[0]);
        // page.InvertYAxis();

        page.AddResource(image, objects);
        page.AddResource(f1, objects);
        page.AddResource(f2, objects);
        Font f3 = page.AddResource(CoreFont.HELVETICA, objects).SetSize(12f);

        image.DrawOn(page);

        float x = 23f;
        float y = 185f;
        float dx = 15f;
        float dy = 24f;

        page.SetBrushColor(Color.blue);

        // First Name and Initial
        page.DrawString(f2, f2.GetSize(), "Иван", x, y);

        // Last Name
        page.DrawString(f3, f3.GetSize(), "Jones", x + 258f, y);

        // Social Insurance Number
        page.DrawString(f1, f1.GetSize(), StripSpacesAndDashes("243-590-129"), x + 437f, y, dx);

        // Last Name at Birth
        page.DrawString(f1, f1.GetSize(), "Culverton", x, y += dy);

        // Mailing Address
        page.DrawString(f1, f1.GetSize(), "10 Elm Street", x, y += dy);

        // City
        page.DrawString(f1, f1.GetSize(), "Toronto", x, y + dy);

        // Province or Territory
        page.DrawString(f1, f1.GetSize(), "Ontario", x + 365f, y += dy);

        // Postal Code
        page.DrawString(f1, f1.GetSize(), StripSpacesAndDashes("L7B 2E9"), x + 482f, y, dx);

        // Home Address
        page.DrawString(f1, f1.GetSize(), "10 Oak Road", x, y += dy);

        // City
        y += dy;
        page.DrawString(f1, f1.GetSize(), "Toronto", x, y);

        // Previous Province or Territory
        page.DrawString(f1, f1.GetSize(), "Ontario", x + 365f, y);

        // Postal Code
        page.DrawString(f1, f1.GetSize(), StripSpacesAndDashes("L7B 2E9"), x + 482f, y, dx);

        // Home telephone number
        page.DrawString(f1, f1.GetSize(), "905-222-3333", x, y + dy);
        // Work telephone number
        page.DrawString(f1, f1.GetSize(), "416-567-9903", x + 279f, y += dy);

        // Previous province or territory
        page.DrawString(f1, f1.GetSize(), "British Columbia", x + 452f, y += dy);

        // Move date from previous province or territory
        y += dy;
        page.DrawString(f1, f1.GetSize(), StripSpacesAndDashes("2016-04-12"), x + 452f, y, dx);

        // Date new marital status began
        page.DrawString(f1, f1.GetSize(), StripSpacesAndDashes("2014-11-02"), x + 452f, 467f, dx);

        // First name of spouse
        y = 521f;
        page.DrawString(f1, f1.GetSize(), "Melanie", x, y);
        // Last name of spouse
        page.DrawString(f1, f1.GetSize(), "Jones", x + 258f, y);

        // Social Insurance number of spouse
        page.DrawString(f1, f1.GetSize(), StripSpacesAndDashes("192-760-427"), x + 437f, y, dx);

        // Spouse or common-law partner's address
        page.DrawString(f1, f1.GetSize(), "12 Smithfield Drive", x, 554f);

        // Signature Date
        page.DrawString(f1, f1.GetSize(), "2016-08-07", x + 475f, 615f);

        // Signature Date of spouse
        page.DrawString(f1, f1.GetSize(), "2016-08-07", x + 475f, 651f);

        // Female Checkbox 1
        // CheckBox.DrawXMark(page, 477.5f, 197.5f, 7f);

        // Male Checkbox 1
        CheckBox.DrawXMark(page, 534.5f, 197.5f, 7f);

        // Married
        CheckBox.DrawXMark(page, 34.5f, 424f, 7f);

        // Living common-law
        // CheckBox.DrawXMark(page, 121.5f, 424f, 7f);

        // Widowed
        // CheckBox.DrawXMark(page, 235.5f, 424f, 7f);

        // Divorced
        // CheckBox.DrawXMark(page, 325.5f, 424f, 7f);

        // Separated
        // CheckBox.DrawXMark(page, 415.5f, 424f, 7f);

        // Single
        // CheckBox.DrawXMark(page, 505.5f, 424f, 7f);

        // Female Checkbox 2
        CheckBox.DrawXMark(page, 478.5f, 536.5f, 7f);

        // Male Checkbox 2
        // CheckBox.DrawXMark(page, 535.5f, 536.5f, 7f);
        page.Complete(objects);
        pdf.AddObjects(objects);

        pdf.Complete();
    }

    private String StripSpacesAndDashes(String str) {
        StringBuilder buf = new StringBuilder();
        for (int i = 0; i < str.Length; i++) {
            char ch = str[i];
            if (ch != ' ' && ch != '-') {
                buf.Append(ch);
            }
        }
        return buf.ToString();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_50("50", "data/testPDFs/rc65-16e.pdf");
        long time1 = sw.ElapsedMilliseconds;
        Console.WriteLine($"Example_50 => {time1 - time0,4} ms");
    }
}   // End of Example_50.cs
