/*
 * ComplexText.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// Text in a script PDFjet does not shape itself, such as Hindi or Khmer,
/// wrapped to a width and drawn as simply as a TextBlock. A text engine shapes
/// it -- conjuncts, vowel signs, stacked consonants -- and PDFjet draws the
/// glyphs it found with Page.DrawGlyphs: as outlines read from the font file,
/// sharp on screen and in print, each glyph drawn once and reused.
/// <code>
/// Font hindi = new Font(pdf, IBMPlexSansDevanagari.Regular);
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
/// .NET has no text engine of its own that gives glyphs: SetShaper gives
/// one, such as a shaper that calls HarfBuzz through HarfBuzzSharp.
/// Left-to-right scripts: Arabic and Hebrew are TextLine's and TextBlock's.
/// <para>
/// In a tagged document, PDF/UA or a PDF/A of level A, the text is a
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
    private readonly Font font;
    private Font fallbackFont;
    private readonly String text;
    private String language;
    private float x;
    private float y;
    private float width = 300f;
    private int textColor = Color.black;
    private Alignment textAlignment = Alignment.LEFT;
    private float lineSpacing = 1.0f;
    private StructElem structureType = StructElem.P;
    private IShaper shaper;
    private List<ShapedLine> lines;     // Shaped, until a font, the language or the width changes
    private float shapedSize;           // The font's size the lines were shaped at

    /// <summary>Text to draw in the font, at its size, loaded from its font file.</summary>
    public ComplexText(Font font, String text) {
        this.font = font;
        this.text = text;
    }

    /// <summary>Sets the font of the characters the font does not have, such as
    /// English words in Hindi text, drawn at the font's size.</summary>
    public ComplexText SetFallbackFont(Font font) {
        this.fallbackFont = font;
        this.lines = null;
        return this;
    }

    /// <summary>Sets the language of the text, such as "hi" or "km": the engine
    /// shapes by it, and a tagged document gives it to screen readers.</summary>
    public ComplexText SetLanguage(String language) {
        this.language = language;
        this.lines = null;
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
        this.width = width;
        this.lines = null;
        return this;
    }

    /// <summary>Sets the color of the text, as 0xRRGGBB.</summary>
    public ComplexText SetTextColor(int color) {
        this.textColor = color;
        return this;
    }

    /// <summary>Sets the alignment of the lines: LEFT, the default, CENTER,
    /// RIGHT or JUSTIFY, which spreads the words of each line but the last to
    /// the width.</summary>
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

    /// <summary>Sets the structure type of the text in a tagged document; P, a
    /// paragraph, by default.</summary>
    public ComplexText SetStructureType(StructElem structureType) {
        this.structureType = structureType;
        return this;
    }

    /// <summary>Sets the text engine that shapes the text.</summary>
    public ComplexText SetShaper(IShaper shaper) {
        this.shaper = shaper;
        this.lines = null;
        return this;
    }

    /// <summary>Returns the height of the text, wrapped to the width, in
    /// points: shapes it, if it is not shaped yet.</summary>
    public float GetHeight() {
        return Shaped().Count * LineHeight();
    }

    /// <summary>Draws the text, wrapped to the width, from its location.</summary>
    /// <returns>the x and the y of the bottom right corner of the text.</returns>
    public float[] DrawOn(Page page) {
        List<ShapedLine> shapedLines = Shaped();
        Font[] fonts = fallbackFont == null ? new Font[] {font} : new Font[] {font, fallbackFont};
        float size = font.GetSize();
        float ascent = font.GetAscent();
        float lineHeight = LineHeight();

        bool own = page.pdf.IsTagged() && !page.InsideTaggedContent();
        page.SetBrushColor(textColor);
        if (own) {
            page.AddBDC(structureType, language, null, null);
        }
        // The space of each font, for justified lines
        int[] spaces = new int[fonts.Length];
        for (int i = 0; i < fonts.Length; i++) {
            spaces[i] = fonts[i].unicodeToGID == null ? 0 : fonts[i].unicodeToGID.Get(' ');
        }
        float top = y;
        for (int n = 0; n < shapedLines.Count; n++) {
            ShapedLine line = shapedLines[n];
            String lineText = text.Substring(line.Start, line.End - line.Start);
            float baseline = top + ascent;
            top += lineHeight;
            if (lineText.Trim().Length == 0) {
                continue;
            }
            float[] shifts = Shifts(line, n == shapedLines.Count - 1, spaces);
            page.BeginActualText(lineText, language);
            // The glyphs of each font, one run of them at a time
            int from = 0;
            while (from < line.Glyphs.Length) {
                int to = from;
                while (to < line.Glyphs.Length && line.Fonts[to] == line.Fonts[from]) {
                    to++;
                }
                int index = line.Fonts[from];
                if (index >= 0 && index < fonts.Length) {
                    int[] run = new int[to - from];
                    float[] places = new float[2 * run.Length];
                    for (int i = from; i < to; i++) {
                        run[i - from] = line.Glyphs[i];
                        places[2 * (i - from)] = x + line.Positions[2 * i] + shifts[i];
                        places[2 * (i - from) + 1] = baseline + line.Positions[2 * i + 1];
                    }
                    if (!page.GlyphsDrawable(fonts[index], run, places)) {
                        return new float[] {x + width, y};
                    }
                    page.PlaceGlyphs(fonts[index], size, run, places);
                }
                from = to;
            }
            page.EndActualText();
        }
        if (own) {
            page.AddEMC();
        }
        return new float[] {x + width, top};
    }

    // The distance from a line to the next, as TextBlock's
    private float LineHeight() {
        return (font.GetAscent() + font.GetDescent() + font.GetLineGap(font.GetSize())) * lineSpacing;
    }

    // How far each glyph of the line moves to the right, for its alignment:
    // all the same for CENTER and RIGHT; for JUSTIFY, each glyph by the room
    // given to the spaces before it, the last line left as it is.
    private float[] Shifts(ShapedLine line, bool last, int[] spaces) {
        int[] fontOf = line.Fonts;
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

    // The lines, shaped the first time they are needed
    private List<ShapedLine> Shaped() {
        if (lines == null || shapedSize != font.GetSize()) {
            if (shaper == null) {
                throw new InvalidOperationException("ComplexText needs a text engine to shape the text, "
                        + "given with SetShaper, such as one that calls HarfBuzz through HarfBuzzSharp.");
            }
            Font[] fonts = fallbackFont == null ? new Font[] {font} : new Font[] {font, fallbackFont};
            byte[][] files = new byte[fonts.Length][];
            for (int i = 0; i < fonts.Length; i++) {
                files[i] = fonts[i].FontFile();
                if (files[i] == null) {
                    throw new ArgumentException("ComplexText needs fonts loaded from their font files, "
                            + "by path or from their bytes, as the text engine reads the same file.");
                }
            }
            shapedSize = font.GetSize();
            lines = shaper.Shape(text ?? "", language, files, shapedSize, width);
        }
        return lines;
    }
}
}   // End of namespace PDFjet.NET
