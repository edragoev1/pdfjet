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
 * This example reads fonts from OpenType and TrueType files. Any .otf or
 * .ttf file on the computer is a font for PDFjet. A font is
 * embedded as a subset of the glyphs the document draws, unless it is set
 * to stay whole.
 */
public class Example_28 {
    public Example_28() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_28.pdf", FileMode.Create)));
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
                "PDFjet reads OpenType and TrueType fonts as they are: pass the path "
                + "of any .otf or .ttf file on the computer to the Font constructor. "
                + "The IBMPlexSans and NotoSans constants are the paths of the fonts "
                + "that come with PDFjet. A font is embedded with only the "
                + "glyphs the document draws, which keeps the file small, whatever its "
                + "outlines. The paragraph below is drawn four times.");
        textBlock.SetFontSize(12f);
        textBlock.SetLineSpacing(1.5f);
        textBlock.SetLocation(50f, 95f);
        textBlock.SetWidth(512f);
        float[] xy = textBlock.DrawOn(page);

        String[] files = {
            "fonts/IBMPlexSans/IBMPlexSans-Regular.otf",
            IBMPlexSans.Regular,
            SourceSerif4.Regular,
            NotoSans.Regular,
        };
        String[] kinds = {
            "OpenType with CFF outlines, from the .otf file, embedded as a subset",
            "TrueType, from the .ttf file, embedded as a subset",
            "Another TrueType font, embedded as a subset",
            "A TrueType font kept whole: subsetting turned off",
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
        new Example_28();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_28 => {time1 - time0,4} ms");
    }
}   // End of Example_28.cs
