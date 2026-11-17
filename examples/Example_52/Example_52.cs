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
using HarfBuzzSharp;
using PDFjet.NET;
using Font = PDFjet.NET.Font;
using Buffer = HarfBuzzSharp.Buffer;

/**
 * Example_52.cs
 *
 * Draws Hindi and Khmer, scripts PDFjet does not shape itself, with
 * ComplexText: HarfBuzz, through HarfBuzzSharp, shapes them -- conjuncts,
 * vowel signs, stacked consonants -- and PDFjet draws the glyphs it found, at
 * their places, as outlines read from the font file: sharp on screen and in
 * print, and read by screen readers, as each paragraph is tagged with its
 * text and its language. .NET has no text engine of its own that gives
 * glyphs: HarfBuzzShaper below is the shaper ComplexText is given. A page of
 * each, filled with the Universal Declaration of Human Rights from its
 * beginning: Hindi on the first page, Khmer on the second.
 *
 * HarfBuzzSharp is a package of this example alone, never of PDFjet.
 */
public class Example_52 {
    private const String DEVANAGARI = "examples/fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Regular.otf";
    private const String KHMER = "examples/fonts/NotoSansKhmer/NotoSansKhmer-Regular.otf";
    private const String LATIN = "resources/fonts/IBMPlexSans/IBMPlexSans-Regular.otf";

    private const float SIZE = 10.5f;
    private const float LEFT = 50f;
    private const float WIDTH = 512f;
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

        HarfBuzzShaper shaper = new HarfBuzzShaper();
        String hindi = DrawPage(pdf, heading, body, shaper, "Hindi", "hi", DEVANAGARI,
                "resources/data/udhr/udhr_hin.txt");
        String khmer = DrawPage(pdf, heading, body, shaper, "Khmer", "km", KHMER,
                "resources/data/udhr/udhr_khm.txt");

        // The pages draw the text as outlines, which cannot be selected or
        // searched; the exact text is attached for anyone who needs it.
        pdf.AttachText("udhr-hindi.txt", hindi);
        pdf.AttachText("udhr-khmer.txt", khmer);

        pdf.Complete();
    }

    // Draws a page of the Declaration in the language, its paragraphs from
    // the beginning until the page is full, and returns the text drawn.
    private static String DrawPage(PDF pdf, Font heading, Font body, IShaper shaper, String name,
            String language, String scriptFile, String textFile) {
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(heading, "Universal Declaration of Human Rights, in " + name)
                .SetStructureType(StructElem.H1)
                .SetLocation(LEFT, 60f)
                .DrawOn(page);
        new TextLine(body, "Shaped by HarfBuzz (HarfBuzzSharp), drawn by PDFjet's ComplexText as outlines;"
                + " the text is attached to the document.")
                .SetLocation(LEFT, 80f)
                .DrawOn(page);

        // The script's font, and IBM Plex Sans for the characters it does
        // not have, such as digits and punctuation of the Latin script
        Font script = new Font(pdf, scriptFile).SetSize(SIZE);
        Font latin = new Font(pdf, LATIN);

        StringBuilder drawn = new StringBuilder();
        float y = 96f;  // The top of the next paragraph
        foreach (String line in File.ReadAllLines(textFile, Encoding.UTF8)) {
            String paragraph = line.Trim();
            if (paragraph.Length == 0) {
                continue;
            }
            ComplexText text = new ComplexText(script, paragraph)
                    .SetFallbackFont(latin)
                    .SetLanguage(language)
                    .SetShaper(shaper)
                    .SetLocation(LEFT, y)
                    .SetWidth(WIDTH);
            if (y + text.GetHeight() > BOTTOM) {
                break;      // The page is full
            }
            y = text.DrawOn(page)[1] + 0.5f * SIZE;
            // A blank line between paragraphs, as plain text has them
            if (drawn.Length > 0) {
                drawn.Append('\n');
            }
            drawn.Append(paragraph).Append('\n');
        }
        return drawn.ToString();
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

/**
 * A shaper for ComplexText that shapes with HarfBuzz, through HarfBuzzSharp:
 * each character in the first font that has it, each run of one font shaped
 * on its own, and the lines broken at spaces.
 */
public class HarfBuzzShaper : IShaper {
    // HarfBuzz's font of each font file, made once
    private class ShapingFont {
        internal HarfBuzzSharp.Font font;
        internal int upem;
    }
    private readonly Dictionary<byte[], ShapingFont> fonts = new Dictionary<byte[], ShapingFont>();

    // A glyph shaped, in points from the start of the text
    private struct Glyph {
        internal int id;
        internal int font;
        internal int cluster;   // The index in the text of its cluster's first character
        internal float x;
        internal float y;
        internal float advance;
    }

    private ShapingFont FontOf(byte[] file) {
        if (!fonts.TryGetValue(file, out ShapingFont font)) {
            Face face = new Face(Blob.FromStream(new MemoryStream(file)), 0);
            font = new ShapingFont();
            font.upem = face.UnitsPerEm;
            font.font = new HarfBuzzSharp.Font(face);
            // One unit a font unit: the places come in the font's units
            font.font.SetScale(font.upem, font.upem);
            fonts[file] = font;
        }
        return font;
    }

    public List<ShapedLine> Shape(String text, String language, byte[][] fontFiles, float fontSize, float width) {
        ShapingFont[] shapers = new ShapingFont[fontFiles.Length];
        for (int i = 0; i < shapers.Length; i++) {
            shapers[i] = FontOf(fontFiles[i]);
        }
        // Each character in the first font that has it, else in the first
        int[] fontOf = new int[text.Length];
        for (int i = 0; i < text.Length; i += Char.IsSurrogatePair(text, i) ? 2 : 1) {
            int c = Char.ConvertToUtf32(text, i);
            for (int k = 0; k < shapers.Length; k++) {
                if (shapers[k].font.TryGetNominalGlyph(c, out uint _)) {
                    fontOf[i] = k;
                    break;
                }
            }
            if (Char.IsSurrogatePair(text, i)) {
                fontOf[i + 1] = fontOf[i];
            }
        }
        // Each run of one font shaped on its own
        List<Glyph> glyphs = new List<Glyph>();
        float pen = 0f;
        for (int start = 0; start < text.Length; ) {
            int end = start + 1;
            while (end < text.Length && fontOf[end] == fontOf[start]) {
                end++;
            }
            ShapingFont shaper = shapers[fontOf[start]];
            float scale = fontSize / shaper.upem;
            using (Buffer buffer = new Buffer()) {
                buffer.AddUtf16(text, start, end - start);
                if (!String.IsNullOrEmpty(language)) {
                    buffer.Language = new Language(language);
                }
                buffer.GuessSegmentProperties();
                shaper.font.Shape(buffer);
                GlyphInfo[] infos = buffer.GlyphInfos;
                GlyphPosition[] places = buffer.GlyphPositions;
                for (int i = 0; i < infos.Length; i++) {
                    Glyph g = new Glyph();
                    g.id = (int) infos[i].Codepoint;
                    g.font = fontOf[start];
                    g.cluster = (int) infos[i].Cluster;
                    g.x = pen + places[i].XOffset * scale;
                    g.y = -places[i].YOffset * scale;
                    g.advance = places[i].XAdvance * scale;
                    glyphs.Add(g);
                    pen += g.advance;
                }
            }
            start = end;
        }

        // The lines: all the glyphs left if they fit, else up to the last
        // space after a word that fits
        List<ShapedLine> lines = new List<ShapedLine>();
        for (int first = 0; first < glyphs.Count; ) {
            float lineStart = glyphs[first].x;
            int last = glyphs.Count;
            int lastSpace = -1;
            for (int i = first; i < glyphs.Count; i++) {
                if (text[glyphs[i].cluster] == ' ') {
                    lastSpace = i + 1;
                } else if (glyphs[i].x + glyphs[i].advance - lineStart > width && lastSpace > 0) {
                    last = lastSpace;
                    break;
                }
            }
            int n = last - first;
            int[] fonts = new int[n];
            int[] ids = new int[n];
            float[] positions = new float[2 * n];
            for (int i = first; i < last; i++) {
                fonts[i - first] = glyphs[i].font;
                ids[i - first] = glyphs[i].id;
                positions[2 * (i - first)] = glyphs[i].x - lineStart;
                positions[2 * (i - first) + 1] = glyphs[i].y;
            }
            int textEnd = last < glyphs.Count ? glyphs[last].cluster : text.Length;
            lines.Add(new ShapedLine(glyphs[first].cluster, textEnd, fonts, ids, positions));
            first = last;
        }
        return lines;
    }
}
