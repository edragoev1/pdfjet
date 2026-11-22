/*
 * Shaping.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PDFjet.NET {
// The shaping of text such as Hindi or Khmer, for TextBlock and TextLine:
// which text needs a shaper, which shaper, and the shaping, checked.
internal static class Shaping {
    // The shaper of SetDefaultShaper; else the NuGet package PDFjet.HarfBuzz,
    // found by its name, as PDFjet.Fonts.World is, when the program has it;
    // hb-shape only when asked for, so that a program shapes the same
    // wherever it runs, whatever is installed there (22 November 2026)
    internal static volatile IShaper defaultShaperSet;
    private static readonly Lazy<IShaper> harfBuzzPackage = new Lazy<IShaper>(LoadHarfBuzzPackage);

    // The NuGet package PDFjet.HarfBuzz: an assembly for .NET Standard 2.0,
    // with no reference to PDFjet, so that one build serves every framework,
    // as PDFjet.Fonts.World does; its engine's one method, ShapeRuns(byte[]
    // font, string[] runs, string language), gives each run's glyphs as ints,
    // five a glyph: the glyph, its cluster in code points in its run, its
    // advance and its two offsets, in font units
    internal const String HARFBUZZ_ASSEMBLY = "PDFjet.HarfBuzz";
    internal const String HARFBUZZ_TYPE = "PDFjet.HarfBuzz.HarfBuzzEngine";

    private sealed class PackageHarfBuzz : IHarfBuzz {
        private readonly Object engine;
        private readonly MethodInfo shapeRuns;

        internal PackageHarfBuzz(Object engine, MethodInfo shapeRuns) {
            this.engine = engine;
            this.shapeRuns = shapeRuns;
        }

        public IList<HarfBuzzGlyph[]> ShapeRuns(byte[] font, IList<String> runs, String language) {
            String[] texts = new String[runs.Count];
            runs.CopyTo(texts, 0);
            int[][] shaped;
            try {
                shaped = (int[][]) shapeRuns.Invoke(engine, new Object[] {font, texts, language});
            } catch (TargetInvocationException e) when (e.InnerException != null) {
                throw new InvalidOperationException("PDFjet.HarfBuzz: " + e.InnerException.Message, e.InnerException);
            }
            List<HarfBuzzGlyph[]> list = new List<HarfBuzzGlyph[]>();
            foreach (int[] run in shaped) {
                HarfBuzzGlyph[] glyphs = new HarfBuzzGlyph[run.Length / 5];
                for (int i = 0; i < glyphs.Length; i++) {
                    glyphs[i] = new HarfBuzzGlyph(run[5 * i], run[5 * i + 1], run[5 * i + 2], run[5 * i + 3], run[5 * i + 4]);
                }
                list.Add(glyphs);
            }
            return list;
        }
    }

    private static IShaper LoadHarfBuzzPackage() {
        try {
            Assembly assembly = Assembly.Load(new AssemblyName(HARFBUZZ_ASSEMBLY));
            Type type = assembly.GetType(HARFBUZZ_TYPE);
            MethodInfo method = type?.GetMethod("ShapeRuns", new Type[] {typeof(byte[]), typeof(String[]), typeof(String)});
            if (method != null && method.ReturnType == typeof(int[][])) {
                return new HarfBuzzShaper(new PackageHarfBuzz(Activator.CreateInstance(type), method));
            }
        } catch (FileNotFoundException) {
            // The program has not the package
        } catch (FileLoadException) {
            // Nor one that loads
        } catch (BadImageFormatException) {
            // Nor
        }
        return null;
    }

    // The shaper of the package PDFjet.HarfBuzz, null where the program has it not
    internal static IShaper HarfBuzzPackage {
        get { return harfBuzzPackage.Value; }
    }

    // The shaper of TextBlock and TextLine when none is set: SetDefaultShaper's,
    // else the package's; else an error that says what to add
    internal static IShaper DefaultShaper {
        get {
            IShaper shaper = defaultShaperSet ?? harfBuzzPackage.Value;
            if (shaper == null) {
                throw new InvalidOperationException("Text such as Hindi or Khmer needs HarfBuzz to be drawn: add "
                        + "the NuGet package PDFjet.HarfBuzz (HarfBuzz, nothing to install, no code), or give the "
                        + "TextBlock a shaper with SetShaper, such as new HBShape(), which runs HarfBuzz's program "
                        + "hb-shape where it is installed.");
            }
            return shaper;
        }
    }

    // Whether the text has a character of a script whose letters change
    // shape and place, which a shaper draws: Devanagari, Bengali, Gurmukhi,
    // Gujarati, Oriya, Tamil, Telugu, Kannada, Malayalam, Sinhala, Lao,
    // Myanmar and Khmer. Thai is not one: TextBlock draws it as text, its
    // lines broken by ICU's dictionary.
    internal static bool NeedsShaper(String text) {
        for (int i = 0; i < text.Length; i++) {
            int c = text[i];
            if (c < 0x0900) {
                continue;
            }
            if (Char.IsHighSurrogate(text[i]) && i + 1 < text.Length && Char.IsLowSurrogate(text[i + 1])) {
                c = Char.ConvertToUtf32(text[i], text[i + 1]);
                i++;
            }
            if ((c >= 0x0900 && c <= 0x0DFF)           // Devanagari to Sinhala
                    || (c >= 0x0E80 && c <= 0x0EFF)    // Lao
                    || (c >= 0x1000 && c <= 0x109F)    // Myanmar
                    || (c >= 0x1780 && c <= 0x17FF) || (c >= 0x19E0 && c <= 0x19FF)    // Khmer
                    || (c >= 0xA8E0 && c <= 0xA8FF)    // Devanagari Extended
                    || (c >= 0xA9E0 && c <= 0xA9FF) || (c >= 0xAA60 && c <= 0xAA7F)    // Myanmar Extended
                    || (c >= 0x111E0 && c <= 0x111FF)) {   // Sinhala Archaic Numbers
                return true;
            }
        }
        return false;
    }

    // Shapes the text with the shaper, or the default one, in the fonts, at
    // the size, wrapped to the width; the lines checked, so that a shaper of
    // the program's own cannot make PDFjet draw past them.
    internal static List<ShapedLine> ShapeText(IShaper shaper, Font[] fonts, float size, String language,
            String text, float width) {
        IShaper engine = shaper ?? DefaultShaper;
        byte[][] files = new byte[fonts.Length][];
        for (int i = 0; i < fonts.Length; i++) {
            files[i] = fonts[i].FontFile();
            if (files[i] == null) {
                throw new ArgumentException("Text such as Hindi or Khmer needs fonts loaded from their font files, "
                        + "by path or from their bytes, as the text engine reads the same file.");
            }
        }
        List<ShapedLine> lines = engine.Shape(new ShapingRequest(text, language, files, size, width))
                ?? new List<ShapedLine>();
        foreach (ShapedLine line in lines) {
            if (line.FontIndexes == null || line.Glyphs == null || line.Positions == null
                    || line.FontIndexes.Length != line.Glyphs.Length || line.Positions.Length != 2 * line.Glyphs.Length
                    || line.Start < 0 || line.Start > line.End || line.End > text.Length) {
                throw new ArgumentException("A shaped line needs a font and an x and a y for each glyph, "
                        + "and its part of the text.");
            }
        }
        return lines;
    }
}

// A line of TextBlock or TextLine a shaper shaped: its glyphs, drawn as
// outlines in its fonts, and its text, which tags them.
internal sealed class ShapedText {
    internal ShapedLine line;
    internal String text;
    internal Font[] fonts;
    internal float size;
}
}   // End of namespace PDFjet.NET
