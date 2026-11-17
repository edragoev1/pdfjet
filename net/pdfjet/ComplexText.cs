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
        float top = y;
        foreach (ShapedLine line in shapedLines) {
            String lineText = text.Substring(line.Start, line.End - line.Start);
            float baseline = top + ascent;
            top += lineHeight;
            if (lineText.Trim().Length == 0) {
                continue;
            }
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
                        places[2 * (i - from)] = x + line.Positions[2 * i];
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

    private float LineHeight() {
        // The line's ascent and descent, and a quarter of the size between lines
        return font.GetAscent() + font.GetDescent() + 0.25f * font.GetSize();
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
