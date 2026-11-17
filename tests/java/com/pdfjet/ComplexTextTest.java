/*
 * ComplexTextTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertArrayEquals;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertSame;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.ByteArrayOutputStream;
import java.io.FileInputStream;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.Arrays;
import java.util.List;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

import org.junit.jupiter.api.Test;

// ComplexText: the platform's text engine shapes and wraps the text, and the
// glyphs it found are drawn with drawGlyphs, each line tagged with its text.
class ComplexTextTest {

    private static final String DEVANAGARI = "examples/fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Regular.otf";
    private static final String LATIN = "resources/fonts/IBMPlexSans/IBMPlexSans-Regular.otf";
    private static final String CASCADIA = "tests/fonts/CascadiaMono-Regular.ttf";
    private static final String HINDI = "सभी मनुष्यों को गौरव और अधिकारों के मामले में जन्मजात स्वतन्त्रता "
            + "और समानता प्राप्त है। John Smith, 1948.";

    private static int count(String text, String part) {
        int n = 0;
        for (int i = text.indexOf(part); i >= 0; i = text.indexOf(part, i + 1)) {
            n++;
        }
        return n;
    }

    // A shaper that gives the lines it was made with, and keeps what it was given
    private static final class FixedShaper implements Shaper {
        final List<ShapedLine> lines;
        byte[][] files;
        float size;
        float width;
        String language;

        FixedShaper(ShapedLine... lines) {
            this.lines = Arrays.asList(lines);
        }

        @Override
        public List<ShapedLine> shape(String text, String language, byte[][] fontFiles, float fontSize, float width) {
            this.files = fontFiles;
            this.size = fontSize;
            this.width = width;
            this.language = language;
            return lines;
        }
    }

    @Test
    void javaShapesHindiIntoTaggedLinesOfOutlines() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1).setTitle("Title");
        Font hindi = new Font(pdf, DEVANAGARI).setSize(12f);
        Font latin = new Font(pdf, LATIN);
        Page page = new Page(pdf, Letter.PORTRAIT);
        ComplexText text = new ComplexText(hindi, HINDI)
                .setFallbackFont(latin)
                .setLanguage("hi")
                .setLocation(50f, 100f)
                .setWidth(200f);
        float height = text.getHeight();
        float[] corner = text.drawOn(page);
        assertEquals(250f, corner[0], 0.001f);
        assertEquals(100f + height, corner[1], 0.001f);

        String content = TestSupport.content(page);
        // One paragraph in Hindi, a span of actual text for each line, no text
        assertEquals(1, count(content, "/P <</MCID 0>>\nBDC\n"), content);
        int lines = count(content, "/Span <</Lang <6869> /ActualText <");
        assertTrue(lines >= 3, "wrapped to 200 points: " + lines + " lines");
        assertEquals(lines * (hindi.getAscent() + hindi.getDescent() + 3f), height, 0.01f);
        assertFalse(content.contains("BT"), content);
        // The lines' texts, one after the other, are the text
        StringBuilder joined = new StringBuilder();
        Matcher m = Pattern.compile("/ActualText <FEFF([0-9A-F]*)>").matcher(content);
        while (m.find()) {
            String hex = m.group(1);
            byte[] utf16 = new byte[hex.length() / 2];
            for (int i = 0; i < utf16.length; i++) {
                utf16[i] = (byte) Integer.parseInt(hex.substring(2 * i, 2 * i + 2), 16);
            }
            joined.append(new String(utf16, "UTF-16BE"));
        }
        assertEquals(HINDI, joined.toString());
        pdf.complete();
    }

    @Test
    void theShaperGivesTheGlyphsAndTheirPlaces() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = new Font(pdf, LATIN).setSize(10f);
        Font fallback = new Font(pdf, CASCADIA);
        int a = font.unicodeToGID.get('A');
        int b = fallback.unicodeToGID.get('B');
        FixedShaper shaper = new FixedShaper(
                new ShapedLine(0, 2, new int[] {0, 1}, new int[] {a, b}, new float[] {0f, 0f, 7f, 0f}),
                // A glyph of a font that was not given is not drawn
                new ShapedLine(2, 3, new int[] {-1}, new int[] {5}, new float[] {0f, 0f}));
        Page page = new Page(pdf, Letter.PORTRAIT);
        float[] corner = new ComplexText(font, "ABC")
                .setFallbackFont(fallback)
                .setLanguage("en")
                .setShaper(shaper)
                .setLocation(50f, 100f)
                .setWidth(120f)
                .drawOn(page);
        assertEquals(2, shaper.files.length);
        assertEquals(10f, shaper.size);
        assertEquals(120f, shaper.width);
        assertEquals("en", shaper.language);

        float lineHeight = font.getAscent() + font.getDescent() + 2.5f;
        assertEquals(100f + 2 * lineHeight, corner[1], 0.001f);
        // The fallback's glyph at the font's size, 10 points, on the first line's baseline
        float y = 792f - (100f + font.getAscent());
        String content = TestSupport.content(page);
        Matcher m = Pattern.compile("q (\\S+) 0 0 \\S+ (\\S+) (\\S+) cm /Fm").matcher(content);
        List<float[]> places = new java.util.ArrayList<float[]>();
        while (m.find()) {
            places.add(new float[] {Float.parseFloat(m.group(1)), Float.parseFloat(m.group(2)),
                    Float.parseFloat(m.group(3))});
        }
        assertEquals(2, places.size(), content);
        assertEquals(0.01f, places.get(0)[0], 1e-6f);               // Plex, 1,000 units an em
        assertEquals(10f / 2048f, places.get(1)[0], 1e-6f);         // Cascadia, 2,048
        assertEquals(50f, places.get(0)[1], 0.001f);
        assertEquals(57f, places.get(1)[1], 0.001f);
        assertEquals(y, places.get(0)[2], 0.006f);
        assertEquals(y, places.get(1)[2], 0.006f);
        pdf.complete();
    }

    @Test
    void theFontFileIsWhatTheFontWasLoadedFrom() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream());
        byte[] bytes = Files.readAllBytes(Paths.get(DEVANAGARI));
        Font fromBytes = new Font(pdf, bytes);
        Font fromPath = new Font(pdf, DEVANAGARI);
        Font trueType = new Font(pdf, new FileInputStream(CASCADIA));
        FixedShaper shaper = new FixedShaper();
        Page page = new Page(pdf, Letter.PORTRAIT);
        new ComplexText(fromBytes, "x").setFallbackFont(fromPath).setShaper(shaper).drawOn(page);
        assertSame(bytes, shaper.files[0]);
        assertArrayEquals(bytes, shaper.files[1]);
        // A TrueType font keeps its whole file, from a stream too
        new ComplexText(trueType, "x").setShaper(shaper).drawOn(page);
        assertArrayEquals(Files.readAllBytes(Paths.get(CASCADIA)), shaper.files[0]);
    }

    @Test
    void anOpenTypeFontFromAStreamIsRefused() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream());
        Font font = TestSupport.plexSans(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        assertThrows(IllegalArgumentException.class,
                () -> new ComplexText(font, "x").setShaper(new FixedShaper()).drawOn(page));
    }

    @Test
    void untaggedTheGlyphsAreJustShapes() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font hindi = new Font(pdf, DEVANAGARI);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new ComplexText(hindi, HINDI).setLanguage("hi").setLocation(50f, 100f).setWidth(500f).drawOn(page);
        String content = TestSupport.content(page);
        assertFalse(content.contains("BDC"), content);
        assertTrue(count(content, " cm /Fm") > 40, content);
        pdf.complete();
    }
}   // End of ComplexTextTest.java
