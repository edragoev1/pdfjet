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
        internal byte[] file;
        internal Face face;
        internal Font font;
    }

    // The fonts kept: a document's, read once for all its paragraphs, and
    // never all the fonts of a server's documents, each of its own file, nor
    // their copies in native memory (ultra review, 22 November 2026)
    private const int MAX_FONTS = 8;

    // The last MAX_FONTS used, the least recently used first; shaping holds
    // its lock, so that a font let go is never one being shaped with
    private readonly List<ShapingFont> fonts = new List<ShapingFont>();

    private ShapingFont FontOf(byte[] file) {
        for (int i = 0; i < fonts.Count; i++) {
            if (ReferenceEquals(fonts[i].file, file)) {
                ShapingFont found = fonts[i];
                fonts.RemoveAt(i);
                fonts.Add(found);
                return found;
            }
        }
        if (fonts.Count >= MAX_FONTS) {
            ShapingFont oldest = fonts[0];
            fonts.RemoveAt(0);
            oldest.font.Dispose();
            oldest.face.Dispose();
        }
        IntPtr memory = Marshal.AllocHGlobal(file.Length);
        Marshal.Copy(file, 0, memory, file.Length);
        ShapingFont font = new ShapingFont();
        font.file = file;
        font.face = new Face(new Blob(memory, file.Length, MemoryMode.ReadOnly,
                () => Marshal.FreeHGlobal(memory)), 0);
        font.font = new Font(font.face);
        // A unit a font unit: the glyphs come in the font's units, as
        // hb-shape gives them
        font.font.SetScale(font.face.UnitsPerEm, font.face.UnitsPerEm);
        fonts.Add(font);
        return font;
    }

    // How many fonts are kept, for the tests
    public int FontsKept {
        get {
            lock (fonts) {
                return fonts.Count;
            }
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
        lock (fonts) {
            return Shape(FontOf(font), runs, language);
        }
    }

    private static int[][] Shape(ShapingFont shaping, string[] runs, string language) {
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
                // Left to right, as ComplexText draws, not guessed: an Arabic
                // or Hebrew run would come back right to left, its clusters
                // falling (ultra review, 22 November 2026)
                buffer.Direction = Direction.LeftToRight;
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
