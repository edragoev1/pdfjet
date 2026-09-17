/*
 * Example_04.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;

using PDFjet.NET;

/// <summary>
/// Draws Chinese, Japanese and Korean text with the CJK fonts, and Latin text
/// with Helvetica. None of these fonts is embedded: the PDF names them and the
/// viewer supplies them.
/// </summary>
/// <remarks>
/// The advantage is size and speed. A CJK font holds tens of thousands of
/// glyphs, and this document carries none of them, so it is a few kilobytes
/// and is written in a moment. The disadvantages: the viewer must have the
/// Adobe Asian font packs, or a substitute, and the text takes the shapes and
/// widths of whatever font it finds, so the document does not look the same
/// everywhere; the core font Helvetica is limited to the WinAnsi characters; and
/// a document with a font that is not embedded cannot claim PDF/A or PDF/UA
/// compliance. To ship the glyphs with the document, use an embedded font like
/// IBM Plex Sans JP, KR, SC or TC, as Example_02 and 19 do.
/// </remarks>
public class Example_04 {

    public Example_04() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_04.pdf", FileMode.Create)));

        // Core fonts for the Latin text
        Font f0 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        Font f5 = new Font(pdf, CoreFont.HELVETICA);

        Font f1 = new Font(pdf, CJKFont.ADOBE_MING_STD_LIGHT);

        Font f2 = new Font(pdf, CJKFont.ST_HEITI_SC_LIGHT);

        Font f3 = new Font(pdf, CJKFont.KOZ_MIN_PRO_VI_REGULAR);

        Font f4 = new Font(pdf, CJKFont.ADOBE_MYUNGJO_STD_MEDIUM);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f0, "Happy New Year!");
        text.SetFontSize(26f);
        text.SetLocation(70f, 90f);
        text.DrawOn(page);

        text = new TextLine(f5, "In four languages, with CJK fonts that are not embedded in this PDF.");
        text.SetFontSize(11f);
        text.SetTextColor(Color.gray);
        text.SetLocation(70f, 112f);
        text.DrawOn(page);

        String[] languages = {
            "Chinese (Traditional)",
            "Chinese (Simplified)",
            "Japanese",
            "Korean",
        };
        String[] fontNames = {
            "Adobe Ming Std Light",
            "STHeiti SC Light",
            "Kozuka Mincho Pro VI Regular",
            "Adobe Myungjo Std Medium",
        };
        String[] greetings = {
            "新年快樂!",
            "新年快乐!",
            "明けましておめでとう!",
            "새해 복 많이 받으세요!",
        };
        Font[] fonts = {f1, f2, f3, f4};

        float y = 170f;
        for (int i = 0; i < languages.Length; i++) {
            text = new TextLine(f0, languages[i]);
            text.SetFontSize(12f);
            text.SetLocation(70f, y);
            text.DrawOn(page);

            text = new TextLine(f5, fontNames[i]);
            text.SetFontSize(10f);
            text.SetTextColor(Color.gray);
            text.SetLocation(70f, y + 15f);
            text.DrawOn(page);

            text = new TextLine(fonts[i], greetings[i]);
            text.SetFontSize(32f);
            text.SetLocation(70f, y + 60f);
            text.DrawOn(page);

            Line line = new Line(70f, y + 80f, 540f, y + 80f);
            line.SetStrokeColor(Color.lightgray);
            line.DrawOn(page);

            y += 115f;
        }

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_04();
        long time1 = sw.ElapsedMilliseconds;
        Console.WriteLine($"Example_04 => {time1 - time0,4} ms");
    }
}   // End of Example_04.cs
