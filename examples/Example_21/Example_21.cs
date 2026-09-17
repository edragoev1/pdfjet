/*
 * Example_21.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_21.cs
 * This example draws the same web address as a QR code with each of the four
 * error correction levels. A higher level lets a scanner read a code that is
 * more damaged or covered, and leaves room for less data in the code.
 */
public class Example_21 {
    public Example_21() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_21.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f2, "QR Code Error Correction");
        text.SetFontSize(22f);
        text.SetLocation(70f, 80f);
        text.DrawOn(page);

        TextBlock textBlock = new TextBlock(f1,
                "Each QR code below holds the same address, https://pdfjet.com. "
                + "A higher error correction level lets a scanner read the code when "
                + "more of it is damaged or covered, and leaves room for less data: "
                + "PDFjet draws every code with 33 by 33 modules.");
        textBlock.SetFontSize(12f);
        textBlock.SetLineSpacing(1.5f);
        textBlock.SetLocation(70f, 95f);
        textBlock.SetWidth(470f);
        textBlock.DrawOn(page);

        ErrorCorrectionLevel[] levels = {
            ErrorCorrectionLevel.L,
            ErrorCorrectionLevel.M,
            ErrorCorrectionLevel.Q,
            ErrorCorrectionLevel.H,
        };
        String[] names = {
            "L (Low)",
            "M (Medium)",
            "Q (Quartile)",
            "H (High)",
        };
        String[] notes = {
            "About 7% can be restored, up to 78 bytes",
            "About 15% can be restored, up to 62 bytes",
            "About 25% can be restored, up to 46 bytes",
            "About 30% can be restored, up to 34 bytes",
        };

        // Two rows of two codes.
        for (int i = 0; i < levels.Length; i++) {
            float x = 70f + (i % 2) * 250f;
            float y = 200f + (i / 2) * 250f;

            QRCode qr = new QRCode("https://pdfjet.com", levels[i]);
            qr.SetModuleLength(5f);
            qr.SetLocation(x, y);
            float[] xy = qr.DrawOn(page);

            text = new TextLine(f2, names[i]);
            text.SetFontSize(12f);
            text.SetLocation(x, xy[1] + 20f);
            text.DrawOn(page);

            text = new TextLine(f1, notes[i]);
            text.SetFontSize(10f);
            text.SetTextColor(Color.gray);
            text.SetLocation(x, xy[1] + 35f);
            text.DrawOn(page);
        }

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_21();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_21 => {time1 - time0,4} ms");
    }
}   // End of Example_21.cs
