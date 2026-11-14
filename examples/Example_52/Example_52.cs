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
using HarfBuzzSharp;
using PDFjet.NET;
using Font = PDFjet.NET.Font;
using Buffer = HarfBuzzSharp.Buffer;

/**
 * Example_52.cs
 *
 * Draws Hindi and Khmer as real text: HarfBuzz, through HarfBuzzSharp, shapes
 * them -- conjuncts, vowel signs, stacked consonants -- and Page.DrawGlyphs
 * draws the glyphs it found, at their places, with the text they stand for,
 * so that copying, searching and screen readers get it. PDFjet does not shape
 * these scripts itself; any engine that finds the glyphs can. The text is
 * Article 1 of the Universal Declaration of Human Rights.
 *
 * HarfBuzzSharp is a package of this example alone, never of PDFjet.
 */
public class Example_52 {
    private const String DEVANAGARI = "examples/fonts/NotoSansDevanagari/NotoSansDevanagari-Regular.ttf";
    private const String KHMER = "examples/fonts/NotoSansKhmer/NotoSansKhmer-Regular.ttf";
    private const String LATIN = "resources/fonts/IBMPlexSans/IBMPlexSans-Regular.ttf";

    // A font of HarfBuzz, which shapes, and the same font file loaded by
    // PDFjet, which embeds it, so that the glyph numbers match
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
        pdf.SetTitle("Hindi and Khmer as real text");  // Required for PDF/UA !
        pdf.SetLanguage("en");

        Font heading = new Font(pdf, IBMPlexSans.SemiBold);
        heading.SetSize(16f);
        Font body = new Font(pdf, IBMPlexSans.Regular);
        body.SetSize(10f);

        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(heading, "Article 1 of the Universal Declaration of Human Rights")
                .SetStructureType(StructElem.H1)
                .SetLocation(50f, 60f)
                .DrawOn(page);
        new TextLine(body, "Shaped by HarfBuzz (HarfBuzzSharp), drawn by PDFjet with DrawGlyphs: real text, in Hindi and in Khmer.")
                .SetLocation(50f, 82f)
                .DrawOn(page);

        float y = 100f;
        y = DrawShaped(pdf, page,
                "सभी मनुष्यों को गौरव और अधिकारों के मामले में जन्मजात स्वतन्त्रता और समानता प्राप्त है । "
                + "उन्हें बुद्धि और अन्तरात्मा की देन प्राप्त है और परस्पर उन्हें भाईचारे के भाव से बर्ताव करना चाहिए ।",
                "hi", DEVANAGARI, 12f, 50f, y, 512f);
        DrawShaped(pdf, page,
                "មនុស្សទាំងអស់ កើតមកមានសេរីភាព និងសមភាព ក្នុងផ្នែកសេចក្ដីថ្លៃថ្នូរនិងសិទ្ធិ។ "
                + "មនុស្ស មានវិចារណញ្ញាណនិងសតិសម្បជញ្ញៈជាប់ពីកំណើត "
                + "ហើយគប្បីប្រព្រឹត្ដចំពោះគ្នាទៅវិញទៅមកក្នុងស្មារតីភាតរភាពជាបងប្អូន។",
                "km", KHMER, 12f, 50f, y + 14f, 512f);

        pdf.Complete();
    }

    private static ShapingFont LoadFont(PDF pdf, String path, float size) {
        ShapingFont font = new ShapingFont();
        Face face = new Face(Blob.FromFile(path), 0);
        font.upem = face.UnitsPerEm;
        font.shaper = new HarfBuzzSharp.Font(face);
        // One unit a font unit: the places come in the font's units
        font.shaper.SetScale(font.upem, font.upem);
        font.font = new Font(pdf, path);
        font.font.SetSize(size);
        return font;
    }

    // Draws a paragraph in a script PDFjet does not shape, wrapped to the
    // width at spaces, as real text, and returns the y below its last line.
    // The script's font is used where it has the characters, IBM Plex Sans
    // elsewhere.
    private static float DrawShaped(PDF pdf, Page page, String text, String language,
            String scriptFile, float size, float x, float y, float width) {
        ShapingFont[] fonts = { LoadFont(pdf, scriptFile, size), LoadFont(pdf, LATIN, size) };

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
                float scale = size / f.upem;
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

        // The line height from the script's font
        fonts[0].shaper.TryGetHorizontalFontExtents(out FontExtents extents);
        float em = size / fonts[0].upem;
        float ascent = extents.Ascender * em;
        float descent = -extents.Descender * em;
        float gap = extents.LineGap * em;

        page.AddBDC(StructElem.P, language, null, null);
        for (int first = 0; first < glyphs.Count; ) {
            // The glyphs of the line: all that are left if they fit, else up
            // to the last space after a word that fits
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
            float baseline = y + ascent;
            // The line, a run of one font at a time: its glyphs, their places
            // and the characters each one comes from (its cluster)
            for (int start = first; start < last; ) {
                int end = start + 1;
                while (end < last && glyphs[end].font == glyphs[start].font) {
                    end++;
                }
                int textStart = glyphs[start].cluster;
                int textEnd = end < glyphs.Count ? glyphs[end].cluster : text.Length;
                int[] ids = new int[end - start];
                float[] positions = new float[2 * (end - start)];
                int[] clusters = new int[end - start];
                for (int i = start; i < end; i++) {
                    ids[i - start] = glyphs[i].id;
                    positions[2 * (i - start)] = x + glyphs[i].x - lineStart;
                    positions[2 * (i - start) + 1] = baseline + glyphs[i].y;
                    clusters[i - start] = glyphs[i].cluster - textStart;
                }
                page.DrawGlyphs(fonts[glyphs[start].font].font, ids, positions, clusters,
                        text.Substring(textStart, textEnd - textStart), language);
                start = end;
            }
            y = baseline + descent + gap;
            first = last;
        }
        page.AddEMC();
        return y;
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
