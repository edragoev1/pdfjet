/*
 * HarfBuzzEngine.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HarfBuzzSharp;
using Buffer = HarfBuzzSharp.Buffer;

namespace PDFjet.HarfBuzz {
/// <summary>
/// HarfBuzz for PDFjet's ComplexText: added to a program, the NuGet package
/// PDFjet.HarfBuzz is the shaper of every ComplexText given none, found by
/// PDFjet by its name, with nothing to install and no code. It shapes as
/// HBShape does with HarfBuzz's program hb-shape, and gives the same lines.
/// </summary>
public sealed class HarfBuzzEngine {
    // A font file read once, kept by the array PDFjet gives it
    private sealed class ShapingFont {
        internal Face face;
        internal Font font;
    }

    private readonly Dictionary<byte[], ShapingFont> fonts = new Dictionary<byte[], ShapingFont>();

    private ShapingFont FontOf(byte[] file) {
        lock (fonts) {
            if (!fonts.TryGetValue(file, out ShapingFont font)) {
                IntPtr memory = Marshal.AllocHGlobal(file.Length);
                Marshal.Copy(file, 0, memory, file.Length);
                font = new ShapingFont();
                font.face = new Face(new Blob(memory, file.Length, MemoryMode.ReadOnly,
                        () => Marshal.FreeHGlobal(memory)), 0);
                font.font = new Font(font.face);
                // A unit a font unit: the glyphs come in the font's units, as
                // hb-shape gives them
                font.font.SetScale(font.face.UnitsPerEm, font.face.UnitsPerEm);
                fonts[file] = font;
            }
            return font;
        }
    }

    /// <summary>
    /// Shapes each run of text in the font file, in the language, as hb-shape
    /// does: the script and the direction guessed from the text.
    /// </summary>
    /// <param name="font">the font file.</param>
    /// <param name="runs">the runs of text, each shaped on its own.</param>
    /// <param name="language">a BCP 47 tag, or null.</param>
    /// <returns>for each run, five ints a glyph: the glyph, the index in
    /// code points in its run of its cluster's first character, its advance,
    /// and its offsets to the right and up, in font units.</returns>
    public int[][] ShapeRuns(byte[] font, string[] runs, string language) {
        if (font == null || font.Length == 0) {
            throw new ArgumentException("PDFjet.HarfBuzz: an empty font file");
        }
        ShapingFont shaping = FontOf(font);
        int[][] shaped = new int[runs.Length][];
        for (int r = 0; r < runs.Length; r++) {
            string text = runs[r];
            List<int> codePoints = new List<int>(text.Length);
            for (int i = 0; i < text.Length; i += Char.IsSurrogatePair(text, i) ? 2 : 1) {
                codePoints.Add(Char.ConvertToUtf32(text, i));
            }
            using (Buffer buffer = new Buffer()) {
                buffer.AddCodepoints(codePoints.ToArray());
                if (!String.IsNullOrEmpty(language)) {
                    buffer.Language = new Language(language);
                }
                buffer.GuessSegmentProperties();
                shaping.font.Shape(buffer);
                GlyphInfo[] infos = buffer.GlyphInfos;
                GlyphPosition[] places = buffer.GlyphPositions;
                int[] glyphs = new int[5 * infos.Length];
                for (int i = 0; i < infos.Length; i++) {
                    glyphs[5 * i] = (int) infos[i].Codepoint;
                    glyphs[5 * i + 1] = (int) infos[i].Cluster;
                    glyphs[5 * i + 2] = places[i].XAdvance;
                    glyphs[5 * i + 3] = places[i].XOffset;
                    glyphs[5 * i + 4] = places[i].YOffset;
                }
                shaped[r] = glyphs;
            }
        }
        return shaped;
    }
}
}   // End of namespace PDFjet.HarfBuzz
