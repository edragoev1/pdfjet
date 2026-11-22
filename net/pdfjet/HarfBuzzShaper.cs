/*
 * HarfBuzzShaper.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace PDFjet.NET {
/// <summary>
/// The engine under a HarfBuzzShaper: it shapes runs of text, each in one
/// font, as HarfBuzz does. HBShape is one, which runs hb-shape; the NuGet
/// package PDFjet.HarfBuzz gives another, HarfBuzz itself through
/// HarfBuzzSharp, with nothing to install.
/// </summary>
public interface IHarfBuzz {
    /// <summary>
    /// Shapes each run of text in the font file, in the language (a BCP 47
    /// tag, or null), as HarfBuzz does.
    /// </summary>
    /// <param name="font">the font file.</param>
    /// <param name="runs">the runs of text, each shaped on its own.</param>
    /// <param name="language">the language, or null.</param>
    /// <returns>the glyphs of each run, in font units.</returns>
    IList<HarfBuzzGlyph[]> ShapeRuns(byte[] font, IList<String> runs, String language);
}

/// <summary>A glyph a HarfBuzz shaped, in font units.</summary>
public sealed class HarfBuzzGlyph {
    /// <summary>The glyph's id in the font.</summary>
    public int Glyph { get; }
    /// <summary>The index, in code points, in its run, of the first character of its cluster.</summary>
    public int Cluster { get; }
    /// <summary>The advance.</summary>
    public int XAdvance { get; }
    /// <summary>The offset to the right.</summary>
    public int XOffset { get; }
    /// <summary>The offset up.</summary>
    public int YOffset { get; }

    /// <summary>A glyph shaped.</summary>
    public HarfBuzzGlyph(int glyph, int cluster, int xAdvance, int xOffset, int yOffset) {
        Glyph = glyph;
        Cluster = cluster;
        XAdvance = xAdvance;
        XOffset = xOffset;
        YOffset = yOffset;
    }
}

/// <summary>
/// A shaper for ComplexText that shapes with a HarfBuzz as HBShape does with
/// hb-shape: each character in the first font that has it, a mark in the font
/// of the character it is on, each run of one font shaped in that font, and
/// the lines broken at spaces and, in Thai, Lao, Khmer and Burmese, between
/// words, by ICU's dictionaries of the world fonts. Two HarfBuzzes that shape
/// alike give the same lines.
/// </summary>
public sealed class HarfBuzzShaper : IShaper {
    private readonly IHarfBuzz harfBuzz;

    /// <summary>A shaper of the HarfBuzz.</summary>
    /// <param name="harfBuzz">the HarfBuzz.</param>
    public HarfBuzzShaper(IHarfBuzz harfBuzz) {
        if (harfBuzz == null) {
            throw new ArgumentNullException(nameof(harfBuzz));
        }
        this.harfBuzz = harfBuzz;
    }

    // A glyph shaped, in points from the start of the text
    private sealed class Glyph {
        internal int id;
        internal int font;
        internal int cluster;   // The index in the text, in code points, of its cluster's first character
        internal float x;
        internal float y;
        internal float advance;
    }

    /// <summary>
    /// Shapes the text with the HarfBuzz, each run of one font in that font,
    /// and breaks it into lines no wider than the width.
    /// </summary>
    /// <param name="request">the text, its language, the font files, the
    /// font size and the width of the lines.</param>
    /// <returns>the lines, in order.</returns>
    public List<ShapedLine> Shape(ShapingRequest request) {
        String text = request.Text;
        if (text.IndexOf('\n') >= 0 || text.IndexOf('\r') >= 0) {
            throw new ArgumentException("ComplexText's HarfBuzz shapes a paragraph: a text without line feeds");
        }
        byte[][] files = request.FontFiles;
        if (files.Length == 0) {
            throw new ArgumentException("ComplexText's HarfBuzz needs a font file");
        }
        OTF[] fonts = new OTF[files.Length];
        for (int i = 0; i < files.Length; i++) {
            fonts[i] = new OTF(new MemoryStream(files[i]));
        }
        int[] cps = DictionaryBreaks.CodePoints(text);
        // The index in the text, in chars, of each code point, and of the end
        int[] offsets = new int[cps.Length + 1];
        for (int i = 0, at = 0; i < cps.Length; i++) {
            offsets[i] = at;
            at += (cps[i] > 0xFFFF) ? 2 : 1;
            offsets[i + 1] = at;
        }

        // Each character in the first font that has it, else in the first; a
        // mark in the font of the character it is on
        int[] fontOf = new int[cps.Length];
        for (int i = 0; i < cps.Length; i++) {
            int c = cps[i];
            UnicodeCategory category = (c <= 0xFFFF)
                    ? CharUnicodeInfo.GetUnicodeCategory((char) c)
                    : CharUnicodeInfo.GetUnicodeCategory(Char.ConvertFromUtf32(c), 0);
            if (i > 0 && (category == UnicodeCategory.NonSpacingMark
                    || category == UnicodeCategory.SpacingCombiningMark || category == UnicodeCategory.EnclosingMark)) {
                fontOf[i] = fontOf[i - 1];
                continue;
            }
            for (int k = 0; k < fonts.Length; k++) {
                OTF otf = fonts[k];
                if (category == UnicodeCategory.Control
                        || (c >= otf.firstChar && c <= otf.lastChar && otf.unicodeToGID.Get(c) != 0)) {
                    fontOf[i] = k;
                    break;
                }
            }
        }
        // The runs of one font: start and end
        List<int[]> runs = new List<int[]>();
        for (int start = 0; start < cps.Length;) {
            int end = start + 1;
            while (end < cps.Length && fontOf[end] == fontOf[start]) {
                end++;
            }
            runs.Add(new int[] {start, end});
            start = end;
        }

        // Each font's runs shaped in one call of the HarfBuzz
        Dictionary<int, HarfBuzzGlyph[]> shapedRuns = new Dictionary<int, HarfBuzzGlyph[]>();
        for (int k = 0; k < fonts.Length; k++) {
            List<int[]> list = new List<int[]>();
            List<String> texts = new List<String>();
            foreach (int[] run in runs) {
                if (fontOf[run[0]] == k) {
                    list.Add(run);
                    StringBuilder runText = new StringBuilder();
                    for (int i = run[0]; i < run[1]; i++) {
                        runText.Append(Char.ConvertFromUtf32(cps[i]));
                    }
                    texts.Add(runText.ToString());
                }
            }
            if (list.Count == 0) {
                continue;
            }
            IList<HarfBuzzGlyph[]> shaped = harfBuzz.ShapeRuns(files[k], texts, request.Language);
            if (shaped == null || shaped.Count != list.Count) {
                throw new InvalidOperationException("HarfBuzz gave " + (shaped == null ? 0 : shaped.Count)
                        + " runs for " + list.Count);
            }
            for (int i = 0; i < list.Count; i++) {
                shapedRuns[list[i][0]] = shaped[i];
            }
        }

        // The glyphs of the runs in order, in points
        List<Glyph> glyphs = new List<Glyph>();
        float pen = 0f;
        foreach (int[] run in runs) {
            int k = fontOf[run[0]];
            float scale = request.FontSize / fonts[k].unitsPerEm;
            foreach (HarfBuzzGlyph g in shapedRuns[run[0]]) {
                if (g.Cluster < 0 || run[0] + g.Cluster >= run[1]) {
                    throw new InvalidOperationException("HarfBuzz gave a glyph of a character outside its run");
                }
                glyphs.Add(new Glyph {
                    id = g.Glyph, font = k, cluster = run[0] + g.Cluster,
                    x = pen + g.XOffset * scale, y = -g.YOffset * scale, advance = g.XAdvance * scale,
                });
                pen += g.XAdvance * scale;
            }
        }
        return Lines(glyphs, cps, offsets, text.Length, request.Width);
    }

    // The lines no wider than the width: broken after a space, or between two
    // words of Thai, Lao, Khmer or Burmese, the last place that fits; a word
    // wider than a line alone on its line
    private static List<ShapedLine> Lines(List<Glyph> glyphs, int[] cps, int[] offsets, int end, float width) {
        HashSet<int> wordEnd = new HashSet<int>(DictionaryBreaks.WordEnds(cps));
        List<ShapedLine> lines = new List<ShapedLine>();
        for (int first = 0; first < glyphs.Count;) {
            float lineStart = glyphs[first].x;
            int last = glyphs.Count;
            int lastBreak = -1;
            for (int i = first; i < glyphs.Count; i++) {
                Glyph g = glyphs[i];
                if (i > first && glyphs[i - 1].cluster != g.cluster
                        && (cps[g.cluster - 1] == ' ' || (wordEnd.Contains(g.cluster) && cps[g.cluster] != ' '))) {
                    lastBreak = i;
                }
                if (cps[g.cluster] != ' ' && g.x + g.advance - lineStart > width && lastBreak > first) {
                    last = lastBreak;
                    break;
                }
            }
            int lineEnd = (last < glyphs.Count) ? offsets[glyphs[last].cluster] : end;
            // The width of the line without the spaces at its end
            int visible = last;
            while (visible > first && cps[glyphs[visible - 1].cluster] == ' ') {
                visible--;
            }
            float lineWidth = 0f;
            if (visible > first) {
                Glyph g = glyphs[visible - 1];
                lineWidth = g.x + g.advance - lineStart;
            }
            int n = last - first;
            int[] fontIndexes = new int[n];
            int[] ids = new int[n];
            float[] positions = new float[2 * n];
            for (int i = 0; i < n; i++) {
                Glyph g = glyphs[first + i];
                fontIndexes[i] = g.font;
                ids[i] = g.id;
                positions[2 * i] = g.x - lineStart;
                positions[2 * i + 1] = g.y;
            }
            lines.Add(new ShapedLine(offsets[glyphs[first].cluster], lineEnd, fontIndexes, ids, positions, lineWidth));
            first = last;
        }
        return lines;
    }
}
}   // End of namespace PDFjet.NET
