/*
 * Example_11.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_11.cs
 * This example draws a Code 128, a Code 39, a UPC-A and an EAN-13 barcode,
 * each next to a label, and then barcodes drawn from top to bottom and from
 * bottom to top.
 */
public class Example_11 {
    public Example_11() {
        PDF pdf = new PDF( new BufferedStream(
                new FileStream("Example_11.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Linear Barcodes");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.SetSize(12f);

        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);
        f2.SetSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f2, "Linear Barcodes");
        text.SetStructureType(StructElem.H1);
        text.SetFontSize(22f);
        text.SetLocation(70f, 80f);
        text.DrawOn(page);

        String[] labels = {
            "Code 128",
            "Code 39",
            "UPC-A",
            "EAN-13",
        };
        String[] notes = {
            "Letters, digits and symbols",
            "Upper case letters and digits",
            "11 digits, the check digit is added",
            "12 digits, the check digit is added",
        };
        Barcode[] barcodes = {
            new Barcode(Barcode.CODE_128, "Hellö, World!"),
            new Barcode(Barcode.CODE_39, "WIKIPEDIA"),
            new Barcode(Barcode.UPC_A, "51234567890"),
            new Barcode(Barcode.EAN_13, "051234567890"),
        };
        // UPC-A and EAN-13 need wider bars for the digits under them.
        float[] moduleLengths = {0.75f, 0.75f, 1f, 1f};

        float y = 130f;
        float[] xy;
        for (int i = 0; i < barcodes.Length; i++) {
            new TextLine(f2, labels[i]).SetLocation(70f, y + 15f).DrawOn(page);
            TextLine note = new TextLine(f1, notes[i]);
            note.SetFontSize(10f);
            note.SetTextColor(Color.gray);
            note.SetLocation(70f, y + 32f);
            note.DrawOn(page);

            barcodes[i].SetLocation(290f, y);
            barcodes[i].SetModuleLength(moduleLengths[i]);
            barcodes[i].SetFont(f1);
            xy = barcodes[i].DrawOn(page);
            y = xy[1] + 30f;
        }

        // The same barcodes can be drawn from top to bottom and from bottom to top.
        new TextLine(f2, "Vertical barcodes").SetLocation(70f, y + 15f).DrawOn(page);

        Barcode barcode = new Barcode(Barcode.CODE_128, "G86513JVW0C");
        barcode.SetLocation(70f, y + 35f);
        barcode.SetModuleLength(0.75f);
        barcode.SetDirection(Direction.TOP_TO_BOTTOM);
        barcode.SetFont(f1);
        xy = barcode.DrawOn(page);

        barcode = new Barcode(Barcode.CODE_39, "CODE39");
        barcode.SetLocation(xy[0] + 60f, y + 35f);
        barcode.SetModuleLength(0.75f);
        barcode.SetDirection(Direction.BOTTOM_TO_TOP);
        barcode.SetFont(f1);
        xy = barcode.DrawOn(page);

        barcode = new Barcode(Barcode.EAN_13, "051234567890");
        barcode.SetLocation(xy[0] + 60f, y + 35f);
        barcode.SetModuleLength(1f);
        barcode.SetDirection(Direction.BOTTOM_TO_TOP);
        barcode.SetFont(f1);
        barcode.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_11();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_11 => {time1 - time0,4} ms");
    }
}   // End of Example_11.cs
