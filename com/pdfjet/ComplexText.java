/*
 * ComplexText.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.List;

/**
 * Text in a script PDFjet does not shape itself, such as Hindi or Khmer,
 * wrapped to a width and drawn as simply as a TextBlock. The platform's own
 * text engine shapes it -- conjuncts, vowel signs, stacked consonants -- and
 * PDFjet draws the glyphs it found with Page.drawGlyphs: as outlines read from
 * the font file, sharp on screen and in print, each glyph drawn once and
 * reused.
 * <pre>
 * Font hindi = new Font(pdf, IBMPlexSansDevanagari.Regular);
 * Font latin = new Font(pdf, IBMPlexSans.Regular);    // The English words
 * hindi.setSize(11f);
 * float[] corner = new ComplexText(hindi, text)
 *         .setFallbackFont(latin)
 *         .setLanguage("hi")
 *         .setLocation(50f, 100f)
 *         .setWidth(512f)
 *         .drawOn(page);
 * </pre>
 * The engine: Java's java.awt.font on servers and desktops, and Android's own
 * on Android 12 and later; setShaper gives another. Left-to-right scripts:
 * Arabic and Hebrew are TextLine's and TextBlock's.
 * <p>
 * In a tagged document, PDF/UA or a PDF/A of level A, the text is a paragraph
 * in its language, each line with its text, which screen readers read. The
 * text cannot be selected, copied or searched, as it is drawn as outlines; a
 * document that needs its exact text along can carry it with PDF.attachText.
 * <p>
 * The fonts must be loaded from their font files, by path, as the fonts PDFjet
 * carries are, or from their bytes, as the engine reads the same file: the
 * glyph numbers it finds are those of that file.
 */
public class ComplexText implements Drawable {
    private final Font font;
    private Font fallbackFont;
    private String text;
    private String language;
    private float x;
    private float y;
    private float width = 300f;
    private int textColor = Color.black;
    private StructElem structureType = StructElem.P;
    private Shaper shaper;
    private List<ShapedLine> lines;     // Shaped, until the text, a font or the width changes
    private float shapedSize;           // The font's size the lines were shaped at

    /**
     * Text to draw in the font.
     *
     * @param font the font, at its size, loaded from its font file.
     * @param text the text.
     */
    public ComplexText(Font font, String text) {
        this.font = font;
        this.text = text;
    }

    /**
     * Sets the font of the characters the font does not have, such as English
     * words in Hindi text, drawn at the font's size.
     *
     * @param font the fallback font, loaded from its font file.
     * @return this ComplexText.
     */
    public ComplexText setFallbackFont(Font font) {
        this.fallbackFont = font;
        this.lines = null;
        return this;
    }

    /**
     * Sets the language of the text, such as "hi" or "km": the engine shapes
     * by it, and a tagged document gives it to screen readers.
     *
     * @param language the language.
     * @return this ComplexText.
     */
    public ComplexText setLanguage(String language) {
        this.language = language;
        this.lines = null;
        return this;
    }

    /**
     * Sets the location of the top left corner of the text.
     *
     * @param x the x, in points.
     * @param y the y, in points, from the top of the page.
     * @return this ComplexText.
     */
    public ComplexText setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     * Sets the width the text is wrapped to; 300 points by default.
     *
     * @param width the width, in points.
     * @return this ComplexText.
     */
    public ComplexText setWidth(float width) {
        this.width = width;
        this.lines = null;
        return this;
    }

    /**
     * Sets the color of the text.
     *
     * @param color the color, as 0xRRGGBB.
     * @return this ComplexText.
     */
    public ComplexText setTextColor(int color) {
        this.textColor = color;
        return this;
    }

    /**
     * Sets the structure type of the text in a tagged document; P, a
     * paragraph, by default.
     *
     * @param structureType the structure type.
     * @return this ComplexText.
     */
    public ComplexText setStructureType(StructElem structureType) {
        this.structureType = structureType;
        return this;
    }

    /**
     * Sets the text engine that shapes the text, in place of the platform's.
     *
     * @param shaper the shaper.
     * @return this ComplexText.
     */
    public ComplexText setShaper(Shaper shaper) {
        this.shaper = shaper;
        this.lines = null;
        return this;
    }

    /**
     * Returns the height of the text, wrapped to the width: shapes it, if it
     * is not shaped yet.
     *
     * @return the height, in points.
     * @throws Exception if the text cannot be shaped.
     */
    public float getHeight() throws Exception {
        return shaped().size() * lineHeight();
    }

    /**
     * Draws the text, wrapped to the width, from its location.
     *
     * @param page the page to draw on.
     * @return the x and the y of the bottom right corner of the text.
     * @throws Exception if the text cannot be shaped or drawn.
     */
    @Override
    public float[] drawOn(Page page) throws Exception {
        List<ShapedLine> shapedLines = shaped();
        Font[] fonts = fallbackFont == null ? new Font[] {font} : new Font[] {font, fallbackFont};
        float size = font.getSize();
        float ascent = font.getAscent();
        float lineHeight = lineHeight();

        boolean own = page.pdf.isTagged() && !page.insideTaggedContent();
        page.setBrushColor(textColor);
        if (own) {
            page.addBDC(structureType, language, null, null);
        }
        float top = y;
        for (ShapedLine line : shapedLines) {
            String lineText = text.substring(line.getStart(), line.getEnd());
            float baseline = top + ascent;
            top += lineHeight;
            int[] fontOf = line.getFonts();
            int[] glyphs = line.getGlyphs();
            float[] positions = line.getPositions();
            if (lineText.trim().isEmpty()) {
                continue;
            }
            page.beginActualText(lineText, language);
            // The glyphs of each font, one run of them at a time
            int from = 0;
            while (from < glyphs.length) {
                int to = from;
                while (to < glyphs.length && fontOf[to] == fontOf[from]) {
                    to++;
                }
                if (fontOf[from] >= 0 && fontOf[from] < fonts.length) {
                    int[] run = new int[to - from];
                    float[] places = new float[2 * run.length];
                    for (int i = from; i < to; i++) {
                        run[i - from] = glyphs[i];
                        places[2 * (i - from)] = x + positions[2 * i];
                        places[2 * (i - from) + 1] = baseline + positions[2 * i + 1];
                    }
                    Font runFont = fonts[fontOf[from]];
                    if (!page.glyphsDrawable(runFont, run, places)) {
                        return new float[] {x + width, y};
                    }
                    page.placeGlyphs(runFont, size, run, places);
                }
                from = to;
            }
            page.endActualText();
        }
        if (own) {
            page.addEMC();
        }
        return new float[] {x + width, top};
    }

    private float lineHeight() {
        // The line's ascent and descent, and a quarter of the size between lines
        return font.getAscent() + font.getDescent() + 0.25f * font.getSize();
    }

    // The lines, shaped the first time they are needed
    private List<ShapedLine> shaped() throws Exception {
        if (lines == null || shapedSize != font.getSize()) {
            Font[] fonts = fallbackFont == null ? new Font[] {font} : new Font[] {font, fallbackFont};
            byte[][] files = new byte[fonts.length][];
            for (int i = 0; i < fonts.length; i++) {
                files[i] = fonts[i].fontFile();
                if (files[i] == null) {
                    throw new IllegalArgumentException("ComplexText needs fonts loaded from their font files, "
                            + "by path or from their bytes, as the text engine reads the same file.");
                }
            }
            shapedSize = font.getSize();
            lines = shaper().shape(text == null ? "" : text, language, files, shapedSize, width);
        }
        return lines;
    }

    private Shaper shaper() {
        if (shaper != null) {
            return shaper;
        }
        if (AndroidShaper.available()) {
            return new AndroidShaper();
        }
        if (AndroidShaper.onAndroid()) {
            throw new IllegalStateException("ComplexText needs Android 12 or later, whose text engine shapes the text.");
        }
        return new AwtShaper();
    }
}   // End of ComplexText.java
