/*
 * Example_28.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_28.cs
 * This example reads fonts from OpenType and TrueType files and from the
 * .stream files of the same fonts. Any .otf or .ttf file on the computer is a
 * font for PDFjet; a .stream file is the same font, compressed once, so that
 * it loads and embeds faster.
 */
public class Example_28 {
    public Example_28() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_28.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f2, "Fonts from .otf, .ttf and .stream Files");
        text.SetFontSize(22f);
        text.SetLocation(50f, 80f);
        text.DrawOn(page);

        TextBlock textBlock = new TextBlock(f1,
                "PDFjet reads OpenType and TrueType fonts as they are: pass the path "
                + "of any .otf or .ttf file on the computer to the Font constructor. "
                + "The .stream files that come with PDFjet hold the same fonts, "
                + "compressed once, so that a font loads and embeds faster; the "
                + "IBMPlexSans and NotoSans constants are their paths. "
                + "The paragraph below is drawn four times, from the two kinds of file.");
        textBlock.SetFontSize(12f);
        textBlock.SetLineSpacing(1.5f);
        textBlock.SetLocation(50f, 95f);
        textBlock.SetWidth(512f);
        float[] xy = textBlock.DrawOn(page);

        String[] files = {
            "fonts/IBMPlexSans/IBMPlexSans-Regular.otf",
            "fonts/NotoSans/NotoSans-Regular.ttf",
            IBMPlexSans.Regular,
            NotoSans.Regular,
        };
        String[] kinds = {
            "OpenType, with CFF outlines, read from the .otf file",
            "TrueType, read from the .ttf file",
            "The same OpenType font from its .stream file",
            "The same TrueType font from its .stream file",
        };
        String sample = "The quick brown fox jumps over the lazy dog. "
                + "Ξεσκεπάζω την ψυχοφθόρα βδελυγμία. "
                + "Съешь же ещё этих мягких французских булок, да выпей чаю.";

        float y = xy[1] + 30f;
        for (int i = 0; i < files.Length; i++) {
            text = new TextLine(f2, files[i]);
            text.SetFontSize(11f);
            text.SetLocation(50f, y);
            text.DrawOn(page);

            text = new TextLine(f1, kinds[i]);
            text.SetFontSize(10f);
            text.SetTextColor(Color.gray);
            text.SetLocation(50f, y + 15f);
            text.DrawOn(page);

            Font font = new Font(pdf, files[i]);
            textBlock = new TextBlock(font, sample);
            textBlock.SetFontSize(13f);
            textBlock.SetLineSpacing(1.4f);
            textBlock.SetLocation(50f, y + 28f);
            textBlock.SetWidth(512f);
            xy = textBlock.DrawOn(page);
            y = xy[1] + 30f;
        }

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_28();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_28 => {time1 - time0,4} ms");
    }
}   // End of Example_28.cs
