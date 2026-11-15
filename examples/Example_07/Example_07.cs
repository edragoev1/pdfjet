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
 * This example draws in fonts read from their files. PDFjet works with any
 * TrueType (.ttf) or OpenType (.otf) font file: pass the path of the file to
 * the Font constructor. The fonts PDFjet carries are .otf files; the third
 * font here is a .ttf on purpose, IBM's TrueType edition of IBM Plex Sans in
 * examples/fonts, to show a .ttf at work. A font is embedded as a subset of
 * the glyphs the document draws, unless it is set to stay whole.
 */
public class Example_07 {
    public Example_07() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_07.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Fonts from .otf and .ttf Files");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f2, "Fonts from .otf and .ttf Files");
        text.SetStructureType(StructElem.H1);
        text.SetFontSize(22f);
        text.SetLocation(50f, 80f);
        text.DrawOn(page);

        TextBlock textBlock = new TextBlock(f1,
                "PDFjet works with any TrueType (.ttf) or OpenType (.otf) font file: pass "
                + "the path of the file to the Font constructor. The fonts that come with "
                + "PDFjet, the font constants such as IBMPlexSans and IBMPlexSansJP, are "
                + ".otf files. The paragraph below is drawn four times; the third is from a "
                + ".ttf file on purpose: IBM's TrueType edition of IBM Plex Sans, which the "
                + "examples have in examples/fonts, the same design as the second. A font is "
                + "embedded with only the glyphs the document draws, which keeps the file small.");
        textBlock.SetFontSize(12f);
        textBlock.SetLineSpacing(1.5f);
        textBlock.SetLocation(50f, 95f);
        textBlock.SetWidth(512f);
        float[] xy = textBlock.DrawOn(page);

        String[] files = {
            IBMPlexSansJP.Regular,
            IBMPlexSans.Regular,
            "examples/fonts/IBMPlexSans/IBMPlexSans-Regular.ttf",
            IBMPlexSerif.Regular,
        };
        String[] kinds = {
            "OpenType with CFF outlines, keyed by CIDs, from the .otf file, embedded as a subset",
            "OpenType with CFF outlines, from the .otf file, embedded as a subset",
            "TrueType, IBM Plex Sans again, from IBM's .ttf file, embedded as a subset",
            "A font kept whole: subsetting turned off",
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
            text.SetTextColor(Color.dimgray);
            text.SetLocation(50f, y + 15f);
            text.DrawOn(page);

            Font font = new Font(pdf, files[i]);
            font.SetSubset(i != 3);
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
        new Example_07();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_07 => {time1 - time0,4} ms");
    }
}   // End of Example_07.cs
