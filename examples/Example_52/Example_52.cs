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
 * Draws Hindi and Khmer, scripts PDFjet does not shape itself: HarfBuzz,
 * through HarfBuzzSharp, shapes them -- conjuncts, vowel signs, stacked
 * consonants -- and Page.DrawGlyphs draws the glyphs it found, at their
 * places, as outlines read from the font file: sharp on screen and in print,
 * and read by screen readers, as each paragraph is tagged with its text and
 * its language. Any engine that finds the glyphs can do the shaping. A page
 * of each, filled with the Universal Declaration of Human Rights from its
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

    // A font of HarfBuzz, which shapes, and the same font file loaded by
    // PDFjet, which reads the outlines, so that the glyph numbers match
    private class ShapingFont {
        internal HarfBuzzSharp.Font shaper;
        internal int upem;
        internal Font font;
    }

    // A glyph shaped, in points from the start of the paragraph
    private struct Glyph {
        internal int id;
        internal int font;      // Its font: 0 the script's, 1 the Latin one
        internal int cluster;   // The index in the text, in UTF-16 code units, of its cluster's first character
        internal float x;
        internal float y;
        internal float advance;
    }

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

        String hindi = DrawPage(pdf, heading, body, "Hindi", "hi", DEVANAGARI,
                "resources/data/udhr/udhr_hin.txt");
        String khmer = DrawPage(pdf, heading, body, "Khmer", "km", KHMER,
                "resources/data/udhr/udhr_khm.txt");

        // The pages draw the text as outlines, which cannot be selected or
        // searched; the exact text is attached for anyone who needs it.
        pdf.AttachText("udhr-hindi.txt", hindi);
        pdf.AttachText("udhr-khmer.txt", khmer);

        pdf.Complete();
    }

    // Draws a page of the Declaration in the language, its paragraphs from
    // the beginning until the page is full, and returns the text drawn.
    private static String DrawPage(PDF pdf, Font heading, Font body, String name, String language,
            String scriptFile, String textFile) {
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(heading, "Universal Declaration of Human Rights, in " + name)
                .SetStructureType(StructElem.H1)
                .SetLocation(LEFT, 60f)
                .DrawOn(page);
        new TextLine(body, "Shaped by HarfBuzz (HarfBuzzSharp), drawn by PDFjet with DrawGlyphs as outlines;"
                + " the text is attached to the document.")
                .SetLocation(LEFT, 80f)
                .DrawOn(page);

        ShapingFont[] fonts = { LoadFont(pdf, scriptFile), LoadFont(pdf, LATIN) };
        float ascent = fonts[0].font.GetAscent();
        float descent = fonts[0].font.GetDescent();

        StringBuilder drawn = new StringBuilder();
        float[] y = {96f};  // The top of the next line
        foreach (String line in File.ReadAllLines(textFile, Encoding.UTF8)) {
            String paragraph = line.Trim();
            if (paragraph.Length == 0) {
                continue;
            }
            String text = DrawParagraph(page, paragraph, language, fonts, y, ascent, descent);
            if (text.Length == 0) {
                break;      // The page is full
            }
            // A blank line between paragraphs, as plain text has them
            if (drawn.Length > 0) {
                drawn.Append('\n');
            }
            drawn.Append(text).Append('\n');
            if (text.Length < paragraph.Length) {
                break;      // The page is full
            }
            y[0] += 0.5f * SIZE;
        }
        return drawn.ToString();
    }

    private static ShapingFont LoadFont(PDF pdf, String path) {
        ShapingFont font = new ShapingFont();
        Face face = new Face(Blob.FromFile(path), 0);
        font.upem = face.UnitsPerEm;
        font.shaper = new HarfBuzzSharp.Font(face);
        // One unit a font unit: the places come in the font's units
        font.shaper.SetScale(font.upem, font.upem);
        font.font = new Font(pdf, path);
        font.font.SetSize(SIZE);
        return font;
    }

    // Draws the paragraph, wrapped to the width at spaces, as many of its
    // lines as fit above the bottom of the page, from the top y[0], which it
    // moves below them, and returns the text of the lines drawn. The script's
    // font is used where it has the characters, IBM Plex Sans elsewhere.
    private static String DrawParagraph(Page page, String text, String language,
            ShapingFont[] fonts, float[] y, float ascent, float descent) {
        // Each character in the script's font where it has it, else in the
        // Latin one; each run of one font shaped on its own
        int[] fontOf = new int[text.Length];
        for (int i = 0; i < text.Length; i += Char.IsSurrogatePair(text, i) ? 2 : 1) {
            int c = Char.ConvertToUtf32(text, i);
            fontOf[i] = fonts[0].shaper.TryGetNominalGlyph(c, out uint _) ? 0 : 1;
            if (Char.IsSurrogatePair(text, i)) {
                fontOf[i + 1] = fontOf[i];
            }
        }
        List<Glyph> glyphs = new List<Glyph>();
        float pen = 0f;
        for (int start = 0; start < text.Length; ) {
            int end = start + 1;
            while (end < text.Length && fontOf[end] == fontOf[start]) {
                end++;
            }
            ShapingFont f = fonts[fontOf[start]];
            using (Buffer buffer = new Buffer()) {
                buffer.AddUtf16(text, start, end - start);
                buffer.Language = new Language(language);
                buffer.GuessSegmentProperties();
                f.shaper.Shape(buffer);
                GlyphInfo[] infos = buffer.GlyphInfos;
                GlyphPosition[] places = buffer.GlyphPositions;
                float scale = SIZE / f.upem;
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

        int drawnEnd = 0;   // The end of the text of the lines drawn
        for (int first = 0; first < glyphs.Count; ) {
            if (y[0] + ascent + descent > BOTTOM) {
                break;
            }
            if (drawnEnd == 0) {
                page.AddBDC(StructElem.P, language, null, null);
            }
            // The glyphs of the line: all that are left if they fit, else up
            // to the last space after a word that fits
            float lineStart = glyphs[first].x;
            int last = glyphs.Count;
            int lastSpace = -1;
            for (int i = first; i < glyphs.Count; i++) {
                if (text[glyphs[i].cluster] == ' ') {
                    lastSpace = i + 1;
                } else if (glyphs[i].x + glyphs[i].advance - lineStart > WIDTH && lastSpace > 0) {
                    last = lastSpace;
                    break;
                }
            }
            float baseline = y[0] + ascent;
            // The line, a run of one font at a time: its glyphs and their places
            for (int start = first; start < last; ) {
                int end = start + 1;
                while (end < last && glyphs[end].font == glyphs[start].font) {
                    end++;
                }
                int textStart = glyphs[start].cluster;
                int textEnd = end < glyphs.Count ? glyphs[end].cluster : text.Length;
                int[] ids = new int[end - start];
                float[] positions = new float[2 * (end - start)];
                for (int i = start; i < end; i++) {
                    ids[i - start] = glyphs[i].id;
                    positions[2 * (i - start)] = LEFT + glyphs[i].x - lineStart;
                    positions[2 * (i - start) + 1] = baseline + glyphs[i].y;
                }
                page.DrawGlyphs(fonts[glyphs[start].font].font, ids, positions,
                        text.Substring(textStart, textEnd - textStart), language);
                start = end;
            }
            y[0] = baseline + descent + 0.25f * SIZE;
            drawnEnd = last < glyphs.Count ? glyphs[last].cluster : text.Length;
            first = last;
        }
        if (drawnEnd > 0) {
            page.AddEMC();
        }
        return text.Substring(0, drawnEnd);
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
