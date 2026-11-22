/*
 * Example_52.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Text;
using PDFjet.NET;

/**
 * Example_52.cs
 *
 * Draws Hindi and Khmer, scripts whose letters change shape and place, with
 * ComplexText: HarfBuzz shapes them -- conjuncts, vowel signs, stacked
 * consonants -- and PDFjet draws the glyphs it found, at their places, as
 * outlines read from the font file: sharp on screen and in print, and read by
 * screen readers, as each paragraph is tagged with its text and its language.
 * .NET has no text engine of its own that gives glyphs: the NuGet package
 * PDFjet.HarfBuzz, added beside PDFjet, gives ComplexText HarfBuzz on Windows,
 * Linux and macOS, x64 and arm64, with nothing to install and no code. The
 * Universal Declaration of Human Rights in Hindi, then in Khmer: the
 * paragraphs flow from page to page, a paragraph cut where a page ends.
 */
public class Example_52 {
    private const String DEVANAGARI = "resources/fonts/world/NotoSansDevanagari/NotoSansDevanagari-Regular.otf";
    private const String KHMER = "resources/fonts/world/NotoSansKhmer/NotoSansKhmer-Regular.otf";
    private const String LATIN = "resources/fonts/IBMPlexSans/IBMPlexSans-Regular.otf";

    private const float SIZE = 10.5f;
    private const float LEFT = 50f;
    private const float WIDTH = 512f;
    private const float TOP = 50f;      // The top margin
    private const float BOTTOM = 742f;  // The page's 792 points less the margin

    public Example_52() {
        PDF pdf = new PDF(
            new BufferedStream(new FileStream("Example_52.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("The Universal Declaration of Human Rights in Hindi and Khmer");  // Required for PDF/UA !
        pdf.SetLanguage("en");

        Font heading = new Font(pdf, IBMPlexSans.SemiBold);
        heading.SetSize(16f);
        Font body = new Font(pdf, IBMPlexSans.Regular);
        body.SetSize(9f);

        String hindi = Draw(pdf, heading, body, "Hindi", "hi", DEVANAGARI,
                "resources/data/udhr/udhr_hin.txt");
        String khmer = Draw(pdf, heading, body, "Khmer", "km", KHMER,
                "resources/data/udhr/udhr_khm.txt");

        // The pages draw the text as outlines, which cannot be selected or
        // searched; the exact text is attached for anyone who needs it.
        pdf.AttachText("udhr-hindi.txt", hindi);
        pdf.AttachText("udhr-khmer.txt", khmer);

        pdf.Complete();
    }

    // Draws the Declaration in the language, from a new page, on as many
    // pages as it takes, and returns its text.
    private static String Draw(PDF pdf, Font heading, Font body, String name,
            String language, String scriptFile, String textFile) {
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(heading, "Universal Declaration of Human Rights, in " + name)
                .SetStructureType(StructElem.H1)
                .SetLocation(LEFT, 60f)
                .DrawOn(page);
        new TextLine(body, "Shaped by HarfBuzz, drawn by PDFjet's ComplexText as outlines;"
                + " the text is attached to the document.")
                .SetLocation(LEFT, 80f)
                .DrawOn(page);

        // The script's font, and IBM Plex Sans for the characters it does
        // not have, such as digits and punctuation of the Latin script
        Font script = new Font(pdf, scriptFile).SetSize(SIZE);
        Font latin = new Font(pdf, LATIN);

        List<String> paragraphs = new List<String>();
        foreach (String line in File.ReadAllLines(textFile, Encoding.UTF8)) {
            if (line.Trim().Length > 0) {
                paragraphs.Add(line.Trim());
            }
        }
        ComplexText text = new ComplexText(script, paragraphs)
                .SetFallbackFont(latin)
                .SetLanguage(language)
                .SetWidth(WIDTH)
                .SetParagraphGap(0.5f * SIZE);
        float top = 96f;    // Below the heading, on the first page
        while (true) {
            text.SetHeight(BOTTOM - top).SetLocation(LEFT, top).DrawOn(page);
            if (!text.HasMoreText()) {
                break;
            }
            page = new Page(pdf, Letter.PORTRAIT);
            top = TOP;
        }

        // A blank line between paragraphs, as plain text has them
        StringBuilder all = new StringBuilder();
        foreach (String paragraph in paragraphs) {
            if (all.Length > 0) {
                all.Append('\n');
            }
            all.Append(paragraph).Append('\n');
        }
        return all.ToString();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_52();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_52 => {time1 - time0,4} ms");
    }
}   // End of Example_52.cs
