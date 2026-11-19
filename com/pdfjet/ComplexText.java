/*
 * ComplexText.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collections;
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
 * Paragraphs, given as a list, flow from page to page, as a TextFrame's: with
 * a height, drawOn draws the lines that fit and keeps the rest, a paragraph
 * cut where the height ends, for the next drawOn, on the next page.
 * <pre>
 * ComplexText text = new ComplexText(hindi, paragraphs)
 *         .setFallbackFont(latin)
 *         .setLanguage("hi")
 *         .setWidth(512f)
 *         .setHeight(692f);
 * while (text.hasMoreText()) {
 *     text.setLocation(50f, 50f).drawOn(new Page(pdf, Letter.PORTRAIT));
 * }
 * </pre>
 * The engine: Java's java.awt.font on servers and desktops, and Android's own
 * on Android 12 and later; setShaper gives another. Left-to-right scripts:
 * Arabic and Hebrew are TextLine's and TextBlock's.
 * <p>
 * In a tagged document, PDF/UA or a PDF/A of level A, each paragraph is a
 * paragraph in its language, each line with its text, which screen readers
 * read. The text cannot be selected, copied or searched, as it is drawn as
 * outlines; a document that needs its exact text along can carry it with
 * PDF.attachText.
 * <p>
 * The fonts must be loaded from their font files, by path, as the fonts PDFjet
 * carries are, or from their bytes, as the engine reads the same file: the
 * glyph numbers it finds are those of that file.
 */
public class ComplexText implements Drawable {
    private final Font font;
    private Font fallbackFont;
    private final List<String> paragraphs;
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
    private Shaper shaper;

    // The paragraph drawn next, and its line drawn next
    private int paragraph;
    private int line;

    // The lines of each paragraph, shaped the first time they are needed,
    // until a font, the language or the width changes; and the font's size
    // they were shaped at
    private List<List<ShapedLine>> shapedLines;
    private float shapedSize;

    /**
     * Text to draw in the font.
     *
     * @param font the font, at its size, loaded from its font file.
     * @param text the text.
     */
    public ComplexText(Font font, String text) {
        this(font, Collections.singletonList(text == null ? "" : text));
    }

    /**
     * Paragraphs to draw in the font, one after the other.
     *
     * @param font the font, at its size, loaded from its font file.
     * @param paragraphs the paragraphs.
     */
    public ComplexText(Font font, List<String> paragraphs) {
        this.font = font;
        this.paragraphs = new ArrayList<String>();
        for (String p : paragraphs) {
            this.paragraphs.add(p == null ? "" : p);
        }
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
        this.shapedLines = null;
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
        this.shapedLines = null;
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
        if (width != this.width) {
            this.width = width;
            this.shapedLines = null;
        }
        return this;
    }

    /**
     * Sets the height the text is drawn in: drawOn draws the lines that fit
     * and keeps the rest for the next drawOn. With a height of 0, the default,
     * drawOn draws all of the text.
     *
     * @param height the height, in points.
     * @return this ComplexText.
     */
    public ComplexText setHeight(float height) {
        this.height = height;
        return this;
    }

    /**
     * Sets the space between paragraphs, from the bottom of a paragraph's last
     * line to the top of the next one's first; one empty line by default, as
     * TextFrame's.
     *
     * @param paragraphGap the space, 0 or more; a negative gap is taken as 0.
     * @return this ComplexText.
     */
    public ComplexText setParagraphGap(float paragraphGap) {
        this.paragraphGap = Math.max(0f, paragraphGap);
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
     * Sets the alignment of the lines: LEFT, the default, CENTER, RIGHT or
     * JUSTIFY, which spreads the words of each line but the last of a
     * paragraph to the width.
     *
     * @param textAlignment the alignment.
     * @return this ComplexText.
     */
    public ComplexText setTextAlignment(Alignment textAlignment) {
        this.textAlignment = textAlignment;
        return this;
    }

    /**
     * Sets the line spacing as a multiple of the font's body height, as
     * TextBlock's: 1.0 by default.
     *
     * @param lineSpacing the line spacing.
     * @return this ComplexText.
     */
    public ComplexText setLineSpacing(float lineSpacing) {
        this.lineSpacing = lineSpacing;
        return this;
    }

    /**
     * Sets the structure type of each paragraph in a tagged document; P, a
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
        this.shapedLines = null;
        return this;
    }

    /**
     * Returns true if some of the text has not been drawn yet.
     *
     * @return true if there is more text to draw.
     * @throws Exception if the text cannot be shaped.
     */
    public boolean hasMoreText() throws Exception {
        for (int p = paragraph; p < paragraphs.size(); p++) {
            if (shaped(p).size() > (p == paragraph ? line : 0)) {
                return true;
            }
        }
        return false;
    }

    /**
     * Returns the height set with setHeight, as TextFrame's; 0 for none.
     *
     * @return the height, in points.
     */
    public float getHeight() {
        return height;
    }

    /**
     * Returns the height of the text not drawn yet, wrapped to the width,
     * whatever the height set: shapes it, if it is not shaped yet.
     *
     * @return the height, in points.
     * @throws Exception if the text cannot be shaped.
     */
    public float getTextHeight() throws Exception {
        float total = 0f;
        boolean first = true;
        for (int p = paragraph; p < paragraphs.size(); p++) {
            int lines = shaped(p).size() - (p == paragraph ? line : 0);
            if (lines > 0) {
                total += (first ? 0f : gap()) + lines * lineHeight();
                first = false;
            }
        }
        return total;
    }

    /**
     * Draws the text not drawn yet, wrapped to the width, from its location:
     * all of it, or with a height, the lines that fit, the rest kept for the
     * next drawOn. Without a page, measures them instead: the corner it
     * returns is the one drawOn would, and nothing is drawn or kept, so that
     * a Table cell can hold it, as it holds a TextBlock.
     *
     * @param page the page to draw on, or null to measure.
     * @return the x and the y of the bottom right corner of the text drawn.
     * @throws Exception if the text cannot be shaped or drawn.
     */
    @Override
    public float[] drawOn(Page page) throws Exception {
        if (page == null) {
            return measure();
        }
        Font[] fonts = fonts();
        float size = font.getSize();
        float ascent = font.getAscent();
        float lineHeight = lineHeight();
        float bottom = height > 0f ? y + height : Float.MAX_VALUE;
        // The space of each font, for justified lines
        int[] spaces = new int[fonts.length];
        for (int i = 0; i < fonts.length; i++) {
            spaces[i] = fonts[i].unicodeToGID == null ? 0 : fonts[i].unicodeToGID.get(' ');
        }

        page.setBrushColor(textColor);
        float top = y;
        boolean drawn = false;      // A line drawn by this drawOn
        while (paragraph < paragraphs.size()) {
            List<ShapedLine> lines = shaped(paragraph);
            if (line >= lines.size()) {
                paragraph++;
                line = 0;
                continue;
            }
            float gap = drawn && line == 0 ? gap() : 0f;
            // At least one line each drawOn, so that text in a height too small
            // for a line still ends
            if (drawn && top + gap + lineHeight > bottom) {
                break;
            }
            top += gap;
            String text = paragraphs.get(paragraph);
            boolean own = page.pdf.isTagged() && !page.insideTaggedContent();
            if (own) {
                page.addBDC(structureType, language, null, null);
            }
            while (line < lines.size() && (!drawn || top + lineHeight <= bottom)) {
                ShapedLine shapedLine = lines.get(line);
                boolean last = line == lines.size() - 1;
                if (!drawLine(page, shapedLine, text, top + ascent, last, fonts, spaces, size)) {
                    return new float[] {x + width, top};
                }
                top += lineHeight;
                line++;
                drawn = true;
            }
            if (own) {
                page.addEMC();
            }
            if (line < lines.size()) {
                break;      // The height is full
            }
            paragraph++;
            line = 0;
        }
        return new float[] {x + width, top};
    }

    // The corner drawOn would return, the lines it would draw counted as it
    // counts them, from the paragraph and the line drawn next, which stay
    private float[] measure() throws Exception {
        float lineHeight = lineHeight();
        float bottom = height > 0f ? y + height : Float.MAX_VALUE;
        float top = y;
        boolean drawn = false;
        int p = paragraph;
        int l = line;
        while (p < paragraphs.size()) {
            List<ShapedLine> lines = shaped(p);
            if (l >= lines.size()) {
                p++;
                l = 0;
                continue;
            }
            float gap = drawn && l == 0 ? gap() : 0f;
            if (drawn && top + gap + lineHeight > bottom) {
                break;
            }
            top += gap;
            while (l < lines.size() && (!drawn || top + lineHeight <= bottom)) {
                top += lineHeight;
                l++;
                drawn = true;
            }
            if (l < lines.size()) {
                break;
            }
            p++;
            l = 0;
        }
        return new float[] {x + width, top};
    }

    // Back to the start of the text, as a Table cell draws it whole each time
    void rewind() {
        paragraph = 0;
        line = 0;
    }

    // Draws the line at its baseline: the glyphs of each font, one run of
    // them at a time, tagged with the line's text; false if the document failed.
    private boolean drawLine(Page page, ShapedLine line, String text, float baseline, boolean last,
            Font[] fonts, int[] spaces, float size) throws Exception {
        String lineText = text.substring(line.getStart(), line.getEnd());
        if (lineText.trim().isEmpty()) {
            return true;
        }
        int[] fontOf = line.getFontIndexes();
        int[] glyphs = line.getGlyphs();
        float[] positions = line.getPositions();
        float[] shifts = shifts(line, last, spaces);
        page.beginActualText(lineText, language);
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
                    places[2 * (i - from)] = x + positions[2 * i] + shifts[i];
                    places[2 * (i - from) + 1] = baseline + positions[2 * i + 1];
                }
                Font runFont = fonts[fontOf[from]];
                if (!page.glyphsDrawable(runFont, run, places)) {
                    return false;
                }
                page.placeGlyphs(runFont, size, run, places);
            }
            from = to;
        }
        page.endActualText();
        return true;
    }

    private Font[] fonts() {
        return fallbackFont == null ? new Font[] {font} : new Font[] {font, fallbackFont};
    }

    // The distance from a line to the next, as TextBlock's
    private float lineHeight() {
        float size = font.getSize();
        return (font.getAscent() + font.getDescent() + font.getLineGap(size)) * lineSpacing;
    }

    // One empty line by default, as TextFrame's: the ascent and the descent,
    // and the line gap on each side
    private float gap() {
        if (paragraphGap >= 0f) {
            return paragraphGap;
        }
        float size = font.getSize();
        return font.getAscent() + font.getDescent() + 2f * font.getLineGap(size);
    }

    // How far each glyph of the line moves to the right, for its alignment:
    // all the same for CENTER and RIGHT; for JUSTIFY, each glyph by the room
    // given to the spaces before it, the last line left as it is.
    private float[] shifts(ShapedLine line, boolean last, int[] spaces) {
        int[] fontOf = line.getFontIndexes();
        int[] glyphs = line.getGlyphs();
        float[] shifts = new float[glyphs.length];
        float room = width - line.getWidth();
        if (line.getWidth() <= 0f || room <= 0f) {
            return shifts;
        }
        if (textAlignment == Alignment.CENTER || textAlignment == Alignment.RIGHT) {
            Arrays.fill(shifts, textAlignment == Alignment.CENTER ? room / 2f : room);
        } else if (textAlignment == Alignment.JUSTIFY && !last) {
            // The spaces between the words: not those at the end of the line
            int end = glyphs.length;
            while (end > 0 && isSpace(fontOf[end - 1], glyphs[end - 1], spaces)) {
                end--;
            }
            int count = 0;
            for (int i = 0; i < end; i++) {
                if (isSpace(fontOf[i], glyphs[i], spaces)) {
                    count++;
                }
            }
            if (count > 0) {
                float shift = 0f;
                for (int i = 0; i < glyphs.length; i++) {
                    shifts[i] = shift;
                    if (i < end && isSpace(fontOf[i], glyphs[i], spaces)) {
                        shift += room / count;
                    }
                }
            }
        }
        return shifts;
    }

    private static boolean isSpace(int font, int glyph, int[] spaces) {
        return font >= 0 && font < spaces.length && spaces[font] != 0 && glyph == spaces[font];
    }

    // The lines of the paragraph, shaped the first time they are needed
    private List<ShapedLine> shaped(int p) throws Exception {
        if (shapedLines == null || shapedSize != font.getSize()) {
            shapedLines = new ArrayList<List<ShapedLine>>(Collections.<List<ShapedLine>>nCopies(
                    paragraphs.size(), null));
            shapedSize = font.getSize();
        }
        List<ShapedLine> lines = shapedLines.get(p);
        if (lines == null) {
            Font[] fonts = fonts();
            byte[][] files = new byte[fonts.length][];
            for (int i = 0; i < fonts.length; i++) {
                files[i] = fonts[i].fontFile();
                if (files[i] == null) {
                    throw new IllegalArgumentException("ComplexText needs fonts loaded from their font files, "
                            + "by path or from their bytes, as the text engine reads the same file.");
                }
            }
            lines = shaper().shape(new ShapingRequest(paragraphs.get(p), language, files, shapedSize, width));
            shapedLines.set(p, lines);
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
