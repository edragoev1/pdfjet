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

    // The fonts text is shaped in: the font, and the fallback font for the
    // characters the font has not, when it has a file the text engine can
    // read; a fallback without one, as a core font, is left out, so that a
    // Latin fallback does not fail Hindi text.
    internal static Font[] ShapingFonts(Font font, Font fallback) {
        if (fallback != null && fallback != font) {
            try {
                if (fallback.FontFile() != null) {
                    return new Font[] {font, fallback};
                }
            } catch (Exception) {
                // Left out, as a font without a file
            }
        }
        return new Font[] {font};
    }

    // Shapes the text as one line, not wrapped, with the default shaper: the
    // lines of a shaper that broke it anyway, joined.
    internal static ShapedText ShapeOneLine(Font[] fonts, float size, String language, String text) {
        List<ShapedLine> lines = ShapeText(null, fonts, size, language, text, float.MaxValue / 4);
        ShapedText glyphs = new ShapedText {text = text, fonts = fonts, size = size};
        for (int i = 0; i < lines.Count; i++) {
            ShapedLine line = lines[i];
            if (i == 0) {
                glyphs.line = line;
                continue;
            }
            ShapedLine joined = glyphs.line;
            float x0 = joined.Width;
            int n = joined.Glyphs.Length + line.Glyphs.Length;
            int[] fontIndexes = new int[n];
            int[] ids = new int[n];
            float[] positions = new float[2 * n];
            Array.Copy(joined.FontIndexes, fontIndexes, joined.Glyphs.Length);
            Array.Copy(line.FontIndexes, 0, fontIndexes, joined.Glyphs.Length, line.Glyphs.Length);
            Array.Copy(joined.Glyphs, ids, joined.Glyphs.Length);
            Array.Copy(line.Glyphs, 0, ids, joined.Glyphs.Length, line.Glyphs.Length);
            Array.Copy(joined.Positions, positions, joined.Positions.Length);
            for (int j = 0; j < line.Positions.Length; j += 2) {
                positions[joined.Positions.Length + j] = x0 + line.Positions[j];
                positions[joined.Positions.Length + j + 1] = line.Positions[j + 1];
            }
            glyphs.line = new ShapedLine(joined.Start, line.End, fontIndexes, ids, positions, x0 + line.Width);
        }
        if (glyphs.line == null) {
            glyphs.line = new ShapedLine(0, text.Length, new int[0], new int[0], new float[0], 0f);
        }
        return glyphs;
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

// What shaped text depends on besides its text: a cache of shaped text is
// good only for the style it was made in.
internal sealed class ShapingStyle {
    private readonly Font font;
    private readonly Font fallback;
    private readonly float size;
    private readonly String language;

    internal ShapingStyle(Font font, Font fallback, float size, String language) {
        this.font = font;
        this.fallback = fallback;
        this.size = size;
        this.language = language;
    }

    public override bool Equals(object other) {
        return other is ShapingStyle style && font == style.font && fallback == style.fallback
                && size == style.size && language == style.language;
    }

    public override int GetHashCode() {
        return (font?.GetHashCode() ?? 0) ^ (fallback?.GetHashCode() ?? 0) ^ size.GetHashCode()
                ^ (language?.GetHashCode() ?? 0);
    }
}

// A TextLine's cache of the texts it shaped, its own and its words', which
// the copies a TextFrame or a TextColumn draws share, so that each is shaped
// once, however often it is measured and drawn.
internal sealed class ShapedPieces {
    internal readonly ShapingStyle style;
    internal readonly Dictionary<String, ShapedText> shaped = new Dictionary<String, ShapedText>();

    internal ShapedPieces(ShapingStyle style) {
        this.style = style;
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
