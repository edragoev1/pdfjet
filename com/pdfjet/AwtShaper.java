/*
 * AwtShaper.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.awt.font.FontRenderContext;
import java.awt.font.GlyphVector;
import java.awt.font.LineBreakMeasurer;
import java.awt.font.TextAttribute;
import java.awt.geom.AffineTransform;
import java.awt.geom.Point2D;
import java.io.ByteArrayInputStream;
import java.text.AttributedCharacterIterator;
import java.text.AttributedString;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.WeakHashMap;

// The shaper of Java on servers and desktops: java.awt.font, part of every
// JDK, which shapes with HarfBuzz since Java 9. Each character is in the first
// font that has it; a line is broken where java.awt.font breaks it.
final class AwtShaper implements Shaper {
    // Fractional metrics, as a PDF has them: no rounding to whole pixels
    private static final FontRenderContext CONTEXT =
            new FontRenderContext(new AffineTransform(), true, true);

    // The fonts of the font files, read once
    private static final Map<byte[], java.awt.Font> FONTS = new WeakHashMap<byte[], java.awt.Font>();

    private static java.awt.Font font(byte[] file) throws Exception {
        synchronized (FONTS) {
            java.awt.Font font = FONTS.get(file);
            if (font == null) {
                font = java.awt.Font.createFont(java.awt.Font.TRUETYPE_FONT, new ByteArrayInputStream(file));
                FONTS.put(file, font);
            }
            return font;
        }
    }

    @Override
    public List<ShapedLine> shape(String text, String language, byte[][] fontFiles, float fontSize, float width)
            throws Exception {
        java.awt.Font[] fonts = new java.awt.Font[fontFiles.length];
        for (int i = 0; i < fonts.length; i++) {
            fonts[i] = font(fontFiles[i]).deriveFont(fontSize);
        }
        // Each character in the first font that has it, else in the first
        AttributedString styled = new AttributedString(text);
        for (int i = 0; i < text.length(); ) {
            int c = text.codePointAt(i);
            int n = Character.charCount(c);
            java.awt.Font font = fonts[0];
            for (java.awt.Font f : fonts) {
                if (f.canDisplay(c)) {
                    font = f;
                    break;
                }
            }
            styled.addAttribute(TextAttribute.FONT, font, i, i + n);
            i += n;
        }

        List<ShapedLine> lines = new ArrayList<ShapedLine>();
        if (text.isEmpty()) {
            return lines;
        }
        char[] chars = text.toCharArray();
        LineBreakMeasurer measurer = new LineBreakMeasurer(styled.getIterator(), CONTEXT);
        while (measurer.getPosition() < text.length()) {
            int start = measurer.getPosition();
            // The width of the line without the white space at its end
            float visible = measurer.nextLayout(width).getVisibleAdvance();
            int end = measurer.getPosition();
            // The line, a run of one font at a time
            List<Integer> fontOf = new ArrayList<Integer>();
            List<Integer> glyphs = new ArrayList<Integer>();
            List<Float> positions = new ArrayList<Float>();
            AttributedCharacterIterator runs = styled.getIterator(null, start, end);
            float offset = 0f;
            int runStart = start;
            while (runStart < end) {
                runs.setIndex(runStart);
                int runEnd = Math.min(runs.getRunLimit(TextAttribute.FONT), end);
                java.awt.Font font = (java.awt.Font) runs.getAttribute(TextAttribute.FONT);
                int index = 0;
                while (fonts[index] != font) {
                    index++;
                }
                GlyphVector shaped = font.layoutGlyphVector(
                        CONTEXT, chars, runStart, runEnd, java.awt.Font.LAYOUT_LEFT_TO_RIGHT);
                int count = shaped.getNumGlyphs();
                for (int i = 0; i < count; i++) {
                    Point2D p = shaped.getGlyphPosition(i);
                    fontOf.add(index);
                    glyphs.add(shaped.getGlyphCode(i));
                    positions.add(offset + (float) p.getX());
                    positions.add((float) p.getY());
                }
                offset += (float) shaped.getGlyphPosition(count).getX();
                runStart = runEnd;
            }
            lines.add(new ShapedLine(start, end, ints(fontOf), ints(glyphs), floats(positions), visible));
        }
        return lines;
    }

    private static int[] ints(List<Integer> list) {
        int[] array = new int[list.size()];
        for (int i = 0; i < array.length; i++) {
            array[i] = list.get(i);
        }
        return array;
    }

    private static float[] floats(List<Float> list) {
        float[] array = new float[list.size()];
        for (int i = 0; i < array.length; i++) {
            array[i] = list.get(i);
        }
        return array;
    }
}   // End of AwtShaper.java
