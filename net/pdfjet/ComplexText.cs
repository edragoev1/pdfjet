/*
 * ComplexText.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PDFjet.NET {
/// <summary>
/// Text in a script PDFjet does not shape itself, such as Hindi or Khmer,
/// wrapped to a width and drawn as simply as a TextBlock. A text engine shapes
/// it -- conjuncts, vowel signs, stacked consonants -- and PDFjet draws the
/// glyphs it found with Page.DrawGlyphs: as outlines read from the font file,
/// sharp on screen and in print, each glyph drawn once and reused.
/// <code>
/// Font hindi = new Font(pdf, NotoSansDevanagari.Regular);
/// Font latin = new Font(pdf, IBMPlexSans.Regular);    // The English words
/// hindi.SetSize(11f);
/// float[] corner = new ComplexText(hindi, text)
///         .SetFallbackFont(latin)
///         .SetLanguage("hi")
///         .SetShaper(shaper)
///         .SetLocation(50f, 100f)
///         .SetWidth(512f)
///         .DrawOn(page);
/// </code>
/// Paragraphs, given as a list, flow from page to page, as a TextFrame's:
/// with a height, DrawOn draws the lines that fit and keeps the rest, a
/// paragraph cut where the height ends, for the next DrawOn, on the next page.
/// <code>
/// ComplexText text = new ComplexText(hindi, paragraphs)
///         .SetFallbackFont(latin)
///         .SetLanguage("hi")
///         .SetShaper(shaper)
///         .SetWidth(512f)
///         .SetHeight(692f);
/// while (text.HasMoreText()) {
///     text.SetLocation(50f, 50f).DrawOn(new Page(pdf, Letter.PORTRAIT));
/// }
/// </code>
/// .NET has no text engine of its own that gives glyphs: add the NuGet
/// package PDFjet.HarfBuzz beside PDFjet, and ComplexText shapes with
/// HarfBuzz on Windows, Linux and macOS, nothing to install and no code;
/// without it and without SetShaper, ComplexText fails, saying so. SetShaper
/// gives another, such as HBShape, HarfBuzz's program hb-shape, used only
/// when set.
/// Left-to-right scripts: Arabic and Hebrew are TextLine's and TextBlock's.
/// <para>
/// In a tagged document, PDF/UA or a PDF/A of level A, each paragraph is a
/// paragraph in its language, each line with its text, which screen readers
/// read. The text cannot be selected, copied or searched, as it is drawn as
/// outlines; a document that needs its exact text along can carry it with
/// PDF.AttachText.
/// </para>
/// <para>
/// The fonts must be loaded from their font files, by path, as the fonts
/// PDFjet carries are, or from their bytes, as the engine reads the same
/// file: the glyph numbers it finds are those of that file.
/// </para>
/// </summary>
public class ComplexText : IDrawable {
    // The shaper of SetDefaultShaper; else the NuGet package PDFjet.HarfBuzz,
    // found by its name, as PDFjet.Fonts.World is, when the program has it;
    // hb-shape only when asked for, so that a program shapes the same
    // wherever it runs, whatever is installed there (22 November 2026)
    private static volatile IShaper defaultShaperSet;
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

    private static IShaper DefaultShaper {
        get {
            IShaper shaper = defaultShaperSet ?? harfBuzzPackage.Value;
            if (shaper == null) {
                throw new InvalidOperationException("ComplexText needs HarfBuzz to shape this text: add the NuGet "
                        + "package PDFjet.HarfBuzz (HarfBuzz, nothing to install, no code), or give ComplexText a "
                        + "shaper with SetShaper, such as new HBShape(), which runs HarfBuzz's program hb-shape "
                        + "where it is installed.");
            }
            return shaper;
        }
    }

    /// <summary>
    /// Sets the shaper of every ComplexText given none. Without it, a
    /// ComplexText shapes with the NuGet package PDFjet.HarfBuzz, when the
    /// program has it, HarfBuzz with nothing to install; HBShape, HarfBuzz's
    /// program hb-shape, only when set.
    /// </summary>
    /// <param name="shaper">the shaper, or null for the one PDFjet chooses.</param>
    public static void SetDefaultShaper(IShaper shaper) {
        defaultShaperSet = shaper;
    }

    private readonly Font font;
    private Font fallbackFont;
    private readonly List<String> paragraphs;
    private String language;
    private float x;
    private float y;
    private float width = 300f;
    private float height;               // 0: all of the text
    private float paragraphGap = -1f;   // Less than 0: one empty line, as TextFrame's
    private int textColor = Color.black;
    private Alignment textAlignment = Alignment.LEFT;
    private float lineSpacing = 1.0f;
    private StructElem structureType = StructElem.P;
    private IShaper shaper;

    // The paragraph drawn next, and its line drawn next
    private int paragraph;
    private int line;

    // The lines of each paragraph, shaped the first time they are needed,
    // until a font, the language or the width changes; and the font's size
    // they were shaped at
    private List<ShapedLine>[] shapedLines;
    private float shapedSize;

    /// <summary>Text to draw in the font, at its size, loaded from its font file.</summary>
    public ComplexText(Font font, String text) : this(font, new List<String> {text ?? ""}) {
    }

    /// <summary>Paragraphs to draw in the font, at its size, loaded from its
    /// font file, one after the other.</summary>
    public ComplexText(Font font, List<String> paragraphs) {
        this.font = font;
        this.paragraphs = new List<String>();
        foreach (String p in paragraphs) {
            this.paragraphs.Add(p ?? "");
        }
    }

    /// <summary>Sets the font of the characters the font does not have, such as
    /// English words in Hindi text, drawn at the font's size.</summary>
    public ComplexText SetFallbackFont(Font font) {
        this.fallbackFont = font;
        this.shapedLines = null;
        return this;
    }

    /// <summary>Sets the language of the text, such as "hi" or "km": the engine
    /// shapes by it, and a tagged document gives it to screen readers.</summary>
    public ComplexText SetLanguage(String language) {
        this.language = language;
        this.shapedLines = null;
        return this;
    }

    /// <summary>Sets the location of the top left corner of the text, in points.</summary>
    public ComplexText SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>Sets the width the text is wrapped to; 300 points by default.</summary>
    public ComplexText SetWidth(float width) {
        if (width != this.width) {
            this.width = width;
            this.shapedLines = null;
        }
        return this;
    }

    /// <summary>Sets the height the text is drawn in: DrawOn draws the lines
    /// that fit and keeps the rest for the next DrawOn. With a height of 0,
    /// the default, DrawOn draws all of the text.</summary>
    public ComplexText SetHeight(float height) {
        this.height = height;
        return this;
    }

    /// <summary>Sets the space between paragraphs, from the bottom of a
    /// paragraph's last line to the top of the next one's first; one empty
    /// line by default, as TextFrame's. A negative gap is taken as 0.</summary>
    public ComplexText SetParagraphGap(float paragraphGap) {
        this.paragraphGap = Math.Max(0f, paragraphGap);
        return this;
    }

    /// <summary>Sets the color of the text, as 0xRRGGBB.</summary>
    public ComplexText SetTextColor(int color) {
        this.textColor = color;
        return this;
    }

    /// <summary>Sets the alignment of the lines: LEFT, the default, CENTER,
    /// RIGHT or JUSTIFY, which spreads the words of each line but the last of
    /// a paragraph to the width.</summary>
    public ComplexText SetTextAlignment(Alignment textAlignment) {
        this.textAlignment = textAlignment;
        return this;
    }

    /// <summary>Sets the line spacing as a multiple of the font's body height,
    /// as TextBlock's: 1.0 by default.</summary>
    public ComplexText SetLineSpacing(float lineSpacing) {
        this.lineSpacing = lineSpacing;
        return this;
    }

    /// <summary>Sets the structure type of each paragraph in a tagged document;
    /// P, a paragraph, by default.</summary>
    public ComplexText SetStructureType(StructElem structureType) {
        this.structureType = structureType;
        return this;
    }

    /// <summary>Sets the text engine that shapes the text.</summary>
    public ComplexText SetShaper(IShaper shaper) {
        this.shaper = shaper;
        this.shapedLines = null;
        return this;
    }

    /// <summary>Returns true if some of the text has not been drawn yet.</summary>
    public bool HasMoreText() {
        for (int p = paragraph; p < paragraphs.Count; p++) {
            if (Shaped(p).Count > (p == paragraph ? line : 0)) {
                return true;
            }
        }
        return false;
    }

    /// <summary>Returns the height set with SetHeight, as TextFrame's; 0 for none.</summary>
    public float GetHeight() {
        return height;
    }

    /// <summary>Returns the height of the text not drawn yet, wrapped to the
    /// width, whatever the height set, in points: shapes it, if it is not
    /// shaped yet.</summary>
    public float GetTextHeight() {
        float total = 0f;
        bool first = true;
        for (int p = paragraph; p < paragraphs.Count; p++) {
            int lines = Shaped(p).Count - (p == paragraph ? line : 0);
            if (lines > 0) {
                total += (first ? 0f : Gap()) + lines * LineHeight();
                first = false;
            }
        }
        return total;
    }

    /// <summary>Draws the text not drawn yet, wrapped to the width, from its
    /// location: all of it, or with a height, the lines that fit, the rest
    /// kept for the next DrawOn. Without a page, measures them instead: the
    /// corner it returns is the one DrawOn would, and nothing is drawn or
    /// kept, so that a Table cell can hold it, as it holds a TextBlock.</summary>
    /// <param name="page">the page to draw on, or null to measure.</param>
    /// <returns>the x and the y of the bottom right corner of the text drawn.</returns>
    public float[] DrawOn(Page page) {
        if (page == null) {
            return Measure();
        }
        Font[] fonts = Fonts();
        float size = font.GetSize();
        float ascent = font.GetAscent();
        float lineHeight = LineHeight();
        float bottom = height > 0f ? y + height : float.MaxValue;
        // The space of each font, for justified lines
        int[] spaces = new int[fonts.Length];
        for (int i = 0; i < fonts.Length; i++) {
            spaces[i] = fonts[i].unicodeToGID == null ? 0 : fonts[i].unicodeToGID.Get(' ');
        }

        page.SetBrushColor(textColor);
        float top = y;
        bool drawn = false;     // A line drawn by this DrawOn
        while (paragraph < paragraphs.Count) {
            List<ShapedLine> lines = Shaped(paragraph);
            if (line >= lines.Count) {
                paragraph++;
                line = 0;
                continue;
            }
            float gap = drawn && line == 0 ? Gap() : 0f;
            // At least one line each DrawOn, so that text in a height too
            // small for a line still ends
            if (drawn && top + gap + lineHeight > bottom) {
                break;
            }
            top += gap;
            String text = paragraphs[paragraph];
            bool own = page.pdf.IsTagged() && !page.InsideTaggedContent();
            if (own) {
                page.AddBDC(structureType, language, null, null);
            }
            while (line < lines.Count && (!drawn || top + lineHeight <= bottom)) {
                bool last = line == lines.Count - 1;
                if (!DrawLine(page, lines[line], text, top + ascent, last, fonts, spaces, size)) {
                    return new float[] {x + width, top};
                }
                top += lineHeight;
                line++;
                drawn = true;
            }
            if (own) {
                page.AddEMC();
            }
            if (line < lines.Count) {
                break;      // The height is full
            }
            paragraph++;
            line = 0;
        }
        return new float[] {x + width, top};
    }

    // The corner DrawOn would return, the lines it would draw counted as it
    // counts them, from the paragraph and the line drawn next, which stay
    private float[] Measure() {
        float lineHeight = LineHeight();
        float bottom = height > 0f ? y + height : float.MaxValue;
        float top = y;
        bool drawn = false;
        int p = paragraph;
        int l = line;
        while (p < paragraphs.Count) {
            List<ShapedLine> lines = Shaped(p);
            if (l >= lines.Count) {
                p++;
                l = 0;
                continue;
            }
            float gap = drawn && l == 0 ? Gap() : 0f;
            if (drawn && top + gap + lineHeight > bottom) {
                break;
            }
            top += gap;
            while (l < lines.Count && (!drawn || top + lineHeight <= bottom)) {
                top += lineHeight;
                l++;
                drawn = true;
            }
            if (l < lines.Count) {
                break;
            }
            p++;
            l = 0;
        }
        return new float[] {x + width, top};
    }

    // Back to the start of the text, as a Table cell draws it whole each time
    internal void Rewind() {
        paragraph = 0;
        line = 0;
    }

    // Draws the line at its baseline: the glyphs of each font, one run of
    // them at a time, tagged with the line's text; false if the document failed.
    private bool DrawLine(Page page, ShapedLine line, String text, float baseline, bool last,
            Font[] fonts, int[] spaces, float size) {
        String lineText = text.Substring(line.Start, line.End - line.Start);
        if (lineText.Trim().Length == 0) {
            return true;
        }
        float[] shifts = Shifts(line, last, spaces);
        page.BeginActualText(lineText, language);
        int from = 0;
        while (from < line.Glyphs.Length) {
            int to = from;
            while (to < line.Glyphs.Length && line.FontIndexes[to] == line.FontIndexes[from]) {
                to++;
            }
            int index = line.FontIndexes[from];
            if (index >= 0 && index < fonts.Length) {
                int[] run = new int[to - from];
                float[] places = new float[2 * run.Length];
                for (int i = from; i < to; i++) {
                    run[i - from] = line.Glyphs[i];
                    places[2 * (i - from)] = x + line.Positions[2 * i] + shifts[i];
                    places[2 * (i - from) + 1] = baseline + line.Positions[2 * i + 1];
                }
                if (!page.GlyphsDrawable(fonts[index], run, places)) {
                    return false;
                }
                page.PlaceGlyphs(fonts[index], size, run, places);
            }
            from = to;
        }
        page.EndActualText();
        return true;
    }

    private Font[] Fonts() {
        return fallbackFont == null ? new Font[] {font} : new Font[] {font, fallbackFont};
    }

    // The distance from a line to the next, as TextBlock's
    private float LineHeight() {
        return (font.GetAscent() + font.GetDescent() + font.GetLineGap(font.GetSize())) * lineSpacing;
    }

    // One empty line by default, as TextFrame's: the ascent and the descent,
    // and the line gap on each side
    private float Gap() {
        if (paragraphGap >= 0f) {
            return paragraphGap;
        }
        return font.GetAscent() + font.GetDescent() + 2f * font.GetLineGap(font.GetSize());
    }

    // How far each glyph of the line moves to the right, for its alignment:
    // all the same for CENTER and RIGHT; for JUSTIFY, each glyph by the room
    // given to the spaces before it, the last line left as it is.
    private float[] Shifts(ShapedLine line, bool last, int[] spaces) {
        int[] fontOf = line.FontIndexes;
        int[] glyphs = line.Glyphs;
        float[] shifts = new float[glyphs.Length];
        float room = width - line.Width;
        if (line.Width <= 0f || room <= 0f) {
            return shifts;
        }
        if (textAlignment == Alignment.CENTER || textAlignment == Alignment.RIGHT) {
            float shift = textAlignment == Alignment.CENTER ? room / 2f : room;
            for (int i = 0; i < shifts.Length; i++) {
                shifts[i] = shift;
            }
        } else if (textAlignment == Alignment.JUSTIFY && !last) {
            // The spaces between the words: not those at the end of the line
            int end = glyphs.Length;
            while (end > 0 && IsSpace(fontOf[end - 1], glyphs[end - 1], spaces)) {
                end--;
            }
            int count = 0;
            for (int i = 0; i < end; i++) {
                if (IsSpace(fontOf[i], glyphs[i], spaces)) {
                    count++;
                }
            }
            if (count > 0) {
                float shift = 0f;
                for (int i = 0; i < glyphs.Length; i++) {
                    shifts[i] = shift;
                    if (i < end && IsSpace(fontOf[i], glyphs[i], spaces)) {
                        shift += room / count;
                    }
                }
            }
        }
        return shifts;
    }

    private static bool IsSpace(int font, int glyph, int[] spaces) {
        return font >= 0 && font < spaces.Length && spaces[font] != 0 && glyph == spaces[font];
    }

    // The lines of the paragraph, shaped the first time they are needed
    private List<ShapedLine> Shaped(int p) {
        if (shapedLines == null || shapedSize != font.GetSize()) {
            shapedLines = new List<ShapedLine>[paragraphs.Count];
            shapedSize = font.GetSize();
        }
        if (shapedLines[p] == null) {
            Font[] fonts = Fonts();
            byte[][] files = new byte[fonts.Length][];
            for (int i = 0; i < fonts.Length; i++) {
                files[i] = fonts[i].FontFile();
                if (files[i] == null) {
                    throw new ArgumentException("ComplexText needs fonts loaded from their font files, "
                            + "by path or from their bytes, as the text engine reads the same file.");
                }
            }
            shapedLines[p] = (shaper ?? DefaultShaper).Shape(new ShapingRequest(paragraphs[p], language, files, shapedSize, width));
        }
        return shapedLines[p];
    }
}
}   // End of namespace PDFjet.NET
