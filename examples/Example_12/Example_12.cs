/*
 * Example_12.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using PDFjet.NET;

/**
 * Example_12.cs
 * This example draws a PDF417 barcode that holds a whole source file, with an
 * explanation and a caption.
 */
public class Example_12 {
    public Example_12() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_12.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("PDF417 barcode example");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f2, "PDF417 Barcode");
        text.SetFontSize(22f);
        text.SetLocation(70f, 80f);
        text.DrawOn(page);

        TextBlock textBlock = new TextBlock(f1,
                "PDF417 is a stacked two-dimensional barcode, used on boarding passes, "
                + "identity cards and shipping labels. It holds text and binary data, "
                + "and its error correction lets a scanner read it even when part of "
                + "it is damaged.");
        textBlock.SetFontSize(12f);
        textBlock.SetLineSpacing(1.5f);
        textBlock.SetLocation(70f, 95f);
        textBlock.SetWidth(470f);
        float[] xy = textBlock.DrawOn(page);

        // A barcode that holds a whole source file.
        List<String> lines = Content.LinesOfTextFile("data/Example_12.java");
        StringBuilder buf = new StringBuilder();
        foreach (String line in lines) {
            buf.Append(line);
            buf.Append("\r\n"); // CR and LF both required!
        }

        PDF417 barcode = new PDF417(buf.ToString());
        barcode.SetModuleLength(1f);
        barcode.SetLocation(70f, xy[1] + 30f);
        xy = barcode.DrawOn(page);

        text = new TextLine(f1, "The source code of data/Example_12.java, "
                + lines.Count + " lines");
        text.SetFontSize(10f);
        text.SetTextColor(Color.gray);
        text.SetLocation(70f, xy[1] + 20f);
        text.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_12();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_12 => {time1 - time0,4} ms");
    }
}
