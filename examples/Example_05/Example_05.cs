/*
 * Example_05.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/// <summary>
/// Embedded fonts: the same words in five weights of IBM Plex Sans, in a
/// PDF/UA document.
/// </summary>
/// <remarks>
/// An embedded font travels with the document, so the text looks the same in
/// every viewer, every character of the font can be drawn, and the document can
/// be PDF/UA and PDF/A. The fourteen core fonts, Helvetica, Times and Courier,
/// are in every viewer and make the smallest documents, but they draw only the
/// WinAnsi characters, and PDF/UA and PDF/A do not allow them.
/// </remarks>
public class Example_05 {
    private const String SAMPLE = "Embedded fonts look the same in every viewer.";

    private const int BACKGROUND = 0xf1f4f8;

    public Example_05() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_05.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Embedded Fonts");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.SetSize(11f);
        Font bold = new Font(pdf, IBMPlexSans.Bold);
        bold.SetSize(24f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine title = new TextLine(bold, "Embedded Fonts");
        title.SetStructureType(StructElem.H1);
        title.SetLocation(50f, 70f);
        title.DrawOn(page);

        TextBlock about = new TextBlock(regular,
                "An embedded font travels with the document: the PDF carries the font program, "
                + "so the text looks the same in every viewer, every character of the font can be "
                + "drawn, and the document can be PDF/UA and PDF/A. PDFjet comes with the IBM Plex "
                + "fonts, Sans, Serif and Mono, with Arabic, Hebrew, Thai, Japanese, Korean and "
                + "Chinese, in their weights.\n\n"
                + "The fourteen core fonts, Helvetica, Times and Courier with their bold and italic, "
                + "Symbol and ZapfDingbats, are in every viewer, so the document carries no font "
                + "program and is the smallest it can be. But they draw only the characters of "
                + "Windows Latin 1, the viewer draws them with its own version of the font, and "
                + "PDF/UA and PDF/A do not allow them. The boxes below are the same words in five "
                + "weights of IBM Plex Sans.");
        about.SetLineSpacing(1.4f);
        about.SetLocation(50f, 90f);
        about.SetWidth(512f);
        float y = about.DrawOn(page)[1] + 30f;

        String[] names = {"Light", "Regular", "Medium", "SemiBold", "Bold"};
        String[] weights = {
            IBMPlexSans.Light,
            IBMPlexSans.Regular,
            IBMPlexSans.Medium,
            IBMPlexSans.SemiBold,
            IBMPlexSans.Bold,
        };
        for (int i = 0; i < weights.Length; i++) {
            Font font = new Font(pdf, weights[i]);
            font.SetSize(20f);
            y = DrawSample(page, regular, font, "IBM Plex Sans " + names[i], y) + 22f;
        }

        pdf.Complete();
    }

    private static float DrawSample(Page page, Font labelFont, Font font, String label, float y) {
        TextLine caption = new TextLine(labelFont, label);
        caption.SetTextColor(Color.dimgray);
        caption.SetLocation(50f, y);
        caption.DrawOn(page);

        TextBlock block = new TextBlock(font, SAMPLE);
        block.SetBackgroundColor(BACKGROUND);
        block.SetPadding(10f);
        block.SetLineSpacing(1.2f);
        block.SetLocation(50f, y + 8f);
        block.SetWidth(512f);
        return block.DrawOn(page)[1];
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_05();
        long time1 = sw.ElapsedMilliseconds;
        Console.WriteLine($"Example_05 => {time1 - time0,4} ms");
    }
}   // End of Example_05.cs
