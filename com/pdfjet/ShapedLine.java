/*
 * ShapedLine.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * A line of text a Shaper shaped: the part of the text it holds, and its
 * glyphs, each with its font and its place.
 */
public final class ShapedLine {
    private final int start;
    private final int end;
    private final int[] fontIndexes;
    private final int[] glyphs;
    private final float[] positions;
    private final float width;

    /**
     * A shaped line.
     *
     * @param start the index in the text of the line's first character.
     * @param end the index in the text after the line's last character.
     * @param fontIndexes for each glyph, the index of its font among the font files
     *     the shaper was given, or -1 for a glyph of none of them, which is
     *     not drawn.
     * @param glyphs the glyph numbers, in their fonts.
     * @param positions the x and the y of each glyph, in points, from the left
     *     end of the line at its baseline, y growing down: x0, y0, x1, y1 and
     *     so on.
     * @param width the width of the line, in points, without the white space
     *     at its end: what is centered, aligned to the right or justified.
     */
    public ShapedLine(int start, int end, int[] fontIndexes, int[] glyphs, float[] positions, float width) {
        if (fontIndexes.length != glyphs.length || positions.length != 2 * glyphs.length) {
            throw new IllegalArgumentException("A shaped line needs a font and an x and a y for each glyph.");
        }
        this.start = start;
        this.end = end;
        this.fontIndexes = fontIndexes;
        this.glyphs = glyphs;
        this.positions = positions;
        this.width = width;
    }

    /**
     * Returns the index in the text of the line's first character.
     *
     * @return the index in the text of the line's first character.
     */
    public int getStart() {
        return start;
    }

    /**
     * Returns the index in the text after the line's last character.
     *
     * @return the index in the text after the line's last character.
     */
    public int getEnd() {
        return end;
    }

    /**
     * Returns for each glyph, the index of its font among the font files, or -1.
     *
     * @return for each glyph, the index of its font among the font files, or -1.
     */
    public int[] getFontIndexes() {
        return fontIndexes;
    }

    /**
     * Returns the glyph numbers.
     *
     * @return the glyph numbers.
     */
    public int[] getGlyphs() {
        return glyphs;
    }

    /**
     * Returns the x and the y of each glyph, from the line's left end at its baseline.
     *
     * @return the x and the y of each glyph, from the line's left end at its baseline.
     */
    public float[] getPositions() {
        return positions;
    }

    /**
     * Returns the width of the line, without the white space at its end.
     *
     * @return the width of the line, without the white space at its end.
     */
    public float getWidth() {
        return width;
    }
}   // End of ShapedLine.java
