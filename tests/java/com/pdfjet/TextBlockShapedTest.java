/*
 * TextBlockShapedTest.java
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

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

import org.junit.jupiter.api.Test;

// TextBlock and TextLine with text such as Hindi or Khmer: the platform's
// text engine, or a shaper given, wraps and shapes it, and the glyphs it
// found are drawn as outlines, each line tagged with its text; the other
// paragraphs are drawn as text.
class TextBlockShapedTest {

    private static final String DEVANAGARI = "resources/fonts/world/NotoSansDevanagari/NotoSansDevanagari-Regular.otf";
    private static final String ARABIC = "resources/fonts/world/IBMPlexSansArabic/IBMPlexSansArabic-Regular.otf";
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
        int calls;

        FixedShaper(ShapedLine... lines) {
            this.lines = Arrays.asList(lines);
        }

        @Override
        public List<ShapedLine> shape(ShapingRequest request) {
            this.files = request.getFontFiles();
            this.size = request.getFontSize();
            this.width = request.getWidth();
            this.language = request.getLanguage();
            calls++;
            return lines;
        }
    }

    // A shaper that gives each paragraph as many lines as its letters, one
    // glyph of the letter each
    private static final class LetterShaper implements Shaper {
        private final Font font;

        LetterShaper(Font font) {
            this.font = font;
        }

        @Override
        public List<ShapedLine> shape(ShapingRequest request) {
            List<ShapedLine> lines = new ArrayList<>();
            String text = request.getText();
            for (int i = 0; i < text.length(); i++) {
                lines.add(new ShapedLine(i, i + 1, new int[] {0}, new int[] {font.unicodeToGID.get(text.charAt(i))},
                        new float[] {0f, 0f}, 7f));
            }
            return lines;
        }
    }

    private static List<float[]> places(String content) {
        List<float[]> places = new ArrayList<>();
        Matcher m = Pattern.compile("q (\\S+) 0 0 \\S+ (\\S+) (\\S+) cm /Fm").matcher(content);
        while (m.find()) {
            places.add(new float[] {Float.parseFloat(m.group(1)), Float.parseFloat(m.group(2)),
                    Float.parseFloat(m.group(3))});
        }
        return places;
    }

    private static float body(Font font, float size) {
        return font.getAscent(size) + font.getDescent(size) + font.getLineGap(size);
    }

    // Java's java.awt.font shapes Hindi with an English name: tagged lines
    // of outlines, whose texts together are the paragraph
    @Test
    void javaShapesHindiIntoTaggedLinesOfOutlines() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream(), Compliance.PDF_UA_1).setTitle("Title");
        Font hindi = new Font(pdf, DEVANAGARI);
        Font latin = new Font(pdf, LATIN);
        Page page = new Page(pdf, Letter.PORTRAIT);
        float[] corner = new TextBlock(hindi, HINDI).setFallbackFont(latin).setLanguage("hi")
                .setWidth(200f).setLocation(50f, 100f).drawOn(page);
        String content = TestSupport.content(page);
        int lines = count(content, "/ActualText <");
        assertTrue(lines >= 3, "wrapped to 200 points: " + lines + " lines");
        assertEquals(100f + lines * body(hindi, 12f), corner[1], 0.01f);
        assertFalse(content.contains("Tj"), content);
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
        PDF pdf = new PDF(new ByteArrayOutputStream());
        Font font = new Font(pdf, LATIN);
        Font fallback = new Font(pdf, CASCADIA);
        int a = font.unicodeToGID.get('A');
        int b = fallback.unicodeToGID.get('B');
        FixedShaper shaper = new FixedShaper(
                new ShapedLine(0, 2, new int[] {0, 1}, new int[] {a, b}, new float[] {0f, 0f, 7f, 0f}, 13f),
                // A glyph of a font that was not given is not drawn
                new ShapedLine(2, 3, new int[] {-1}, new int[] {5}, new float[] {0f, 0f}, 6f));
        Page page = new Page(pdf, Letter.PORTRAIT);
        TextBlock text = new TextBlock(font, "कखग").setFallbackFont(fallback).setFontSize(10f)
                .setLanguage("en").setShaper(shaper).setWidth(120f).setLocation(50f, 100f);
        float[] corner = text.drawOn(page);
        assertEquals(2, shaper.files.length);
        assertEquals(10f, shaper.size);
        assertEquals(120f, shaper.width);
        assertEquals("en", shaper.language);
        assertEquals(100f + 2 * body(font, 10f), corner[1], 0.001f);
        // The fallback's glyph at the block's size, 10 points, on the first line's baseline
        float y = 792f - (100f + font.getAscent(10f));
        List<float[]> places = places(TestSupport.content(page));
        assertEquals(2, places.size());
        assertEquals(0.01f, places.get(0)[0], 1e-6f);           // Plex, 1,000 units an em
        assertEquals(10f / 2048f, places.get(1)[0], 1e-6f);     // Cascadia, 2,048
        assertEquals(50f, places.get(0)[1]);
        assertEquals(57f, places.get(1)[1]);
        assertEquals(y, places.get(0)[2], 0.006f);
        assertEquals(y, places.get(1)[2], 0.006f);
        // Shaped once however often the block is measured and drawn
        text.drawOn(null);
        text.drawOn(page);
        assertEquals(1, shaper.calls);
        pdf.complete();
    }

    // A block of a paragraph of English, one of Hindi and one of Arabic: the
    // English and the Arabic drawn as text, the Arabic right to left by itself
    // and aligned to the right; the Hindi as glyphs, between them.
    @Test
    void drawsEachParagraphAsItNeeds() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream(), Compliance.PDF_UA_1).setTitle("Title");
        Font font = new Font(pdf, DEVANAGARI);
        Font arabic = new Font(pdf, ARABIC);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextBlock(font, "Hello\nकखग\nمرحبا").setFallbackFont(arabic).setFontSize(10f).setLanguage("hi")
                .setShaper(new LetterShaper(font)).setWidth(300f).setLocation(50f, 100f).drawOn(page);
        String content = TestSupport.content(page);
        pdf.complete();
        assertEquals(3, count(content, " cm /Fm"));
        for (String letter : new String[] {"क", "ख", "ग"}) {
            assertTrue(content.contains("/ActualText <" + Page.toUTF16Hex(letter) + ">"), letter);
        }
        Matcher m = Pattern.compile("1 0 0 1 (\\S+) \\S+ Tm").matcher(content);
        List<Float> xs = new ArrayList<>();
        while (m.find()) {
            xs.add(Float.parseFloat(m.group(1)));
        }
        assertEquals(2, xs.size(), content);
        assertEquals(50f, xs.get(0));
        assertTrue(xs.get(1) > 300f, "Arabic at the left: " + xs.get(1));
    }

    @Test
    void theFontFileIsWhatTheFontWasLoadedFrom() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream());
        byte[] bytes = Files.readAllBytes(Paths.get(DEVANAGARI));
        Font fromBytes = new Font(pdf, bytes);
        Font fromPath = new Font(pdf, DEVANAGARI);
        FixedShaper shaper = new FixedShaper();
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextBlock(fromBytes, "क").setFallbackFont(fromPath).setShaper(shaper).drawOn(page);
        assertSame(bytes, shaper.files[0]);
        assertArrayEquals(bytes, shaper.files[1]);
    }

    @Test
    void anOpenTypeFontFromAStreamIsRefused() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream());
        Font font = new Font(pdf, new ByteArrayInputStream(Files.readAllBytes(Paths.get(LATIN))));
        Page page = new Page(pdf, Letter.PORTRAIT);
        IllegalArgumentException e = assertThrows(IllegalArgumentException.class,
                () -> new TextBlock(font, "क").setShaper(new FixedShaper()).drawOn(page));
        assertTrue(e.getMessage().contains("font files"), e.getMessage());
    }

    // Two lines: "A A B " of 30 points without its last space, then "B";
    // the x of each glyph drawn
    private static List<Float> aligned(Alignment alignment) throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream());
        Font font = new Font(pdf, LATIN);
        int a = font.unicodeToGID.get('A');
        int b = font.unicodeToGID.get('B');
        int space = font.unicodeToGID.get(' ');
        FixedShaper shaper = new FixedShaper(
                new ShapedLine(0, 6, new int[] {0, 0, 0, 0, 0, 0}, new int[] {a, space, a, space, b, space},
                        new float[] {0f, 0f, 10f, 0f, 12f, 0f, 22f, 0f, 24f, 0f, 30f, 0f}, 30f),
                new ShapedLine(6, 7, new int[] {0}, new int[] {b}, new float[] {0f, 0f}, 6f));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextBlock(font, "क क ख ख").setFontSize(10f).setShaper(shaper).setTextAlignment(alignment)
                .setWidth(100f).setLocation(50f, 100f).drawOn(page);
        List<Float> xs = new ArrayList<>();
        for (float[] place : places(TestSupport.content(page))) {
            xs.add(place[1]);
        }
        return xs;
    }

    @Test
    void theShapedLinesAreAligned() throws Exception {
        assertEquals(Arrays.asList(50f, 62f, 74f, 50f), aligned(Alignment.LEFT));
        assertEquals(Arrays.asList(85f, 97f, 109f, 97f), aligned(Alignment.CENTER));
        assertEquals(Arrays.asList(120f, 132f, 144f, 144f), aligned(Alignment.RIGHT));
    }

    @Test
    void shapedLinesAreSpacedAsItsText() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream());
        Font font = new Font(pdf, DEVANAGARI);
        float body = body(font, 10f);
        TextBlock text = new TextBlock(font, "कख").setFontSize(10f).setShaper(new LetterShaper(font)).setWidth(200f);
        assertEquals(2 * body, text.drawOn(null)[1], 0.001f);
        text.setLineSpacing(1.5f);
        assertEquals(3 * body, text.drawOn(null)[1], 0.001f);
        // With a height, the lines that fit, the glyphs not cut with "..."
        text.setLineSpacing(1f).setHeight(1.5f * body);
        Page page = new Page(pdf, Letter.PORTRAIT);
        text.drawOn(page);
        assertEquals(1, count(TestSupport.content(page), " cm /Fm"));
    }

    // Text such as Hindi in Table cells: each cell as tall as its text
    // wrapped to the column, the text drawn whole each time, as the header
    // row is on each page.
    @Test
    void inTableCellsItWrapsAndRepeatsTheHeader() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream(), Compliance.PDF_UA_1).setTitle("Title");
        Font font = new Font(pdf, DEVANAGARI);
        float line = body(font, 10f);
        LetterShaper shaper = new LetterShaper(font);
        List<List<Cell>> data = new ArrayList<>();
        data.add(Arrays.asList(new Cell(font).setTextBlock(hindi(font, shaper, "क")),
                new Cell(font).setTextBlock(hindi(font, shaper, "ख"))));
        for (int r = 0; r < 40; r++) {
            data.add(Arrays.asList(new Cell(font, "Row " + (r + 1)),
                    new Cell(font).setTextBlock(hindi(font, shaper, "कखग"))));
        }
        Table table = new Table().setTableData(data, 1);
        table.setLocation(50f, 50f);
        table.setColumnWidth(0, 100f).setColumnWidth(1, 250f);
        Cell cell = data.get(1).get(1);
        assertEquals(3 * line + cell.getTopPadding() + cell.getBottomPadding(), cell.getHeight(250f), 0.01f);
        List<Page> pages = new ArrayList<>();
        table.drawOn(pdf, pages, Letter.PORTRAIT);
        assertTrue(pages.size() >= 2, "the table on " + pages.size() + " page");
        String firstPage = TestSupport.content(pages.get(0));
        assertTrue(firstPage.contains("/ActualText <" + Page.toUTF16Hex("क") + ">"));
        assertTrue(firstPage.contains("/ActualText <" + Page.toUTF16Hex("ख") + ">"));
        for (int i = 1; i < pages.size(); i++) {
            assertTrue(count(TestSupport.content(pages.get(i)), " cm /Fm") >= 2, "page " + (i + 1) + ": no glyphs");
        }
    }

    private static TextBlock hindi(Font font, Shaper shaper, String text) {
        return new TextBlock(font, text).setFontSize(10f).setShaper(shaper).setLanguage("hi");
    }

    // TextLine with text such as Hindi: shaped as one line by Java, drawn as
    // glyphs, tagged with its text, its width the glyphs'.
    @Test
    void textLineShapesTextSuchAsHindi() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream(), Compliance.PDF_UA_1).setTitle("Title");
        Font font = new Font(pdf, DEVANAGARI).setSize(10f);
        TextLine line = new TextLine(font, HINDI.substring(0, 20));
        line.setLocation(50f, 100f);
        assertTrue(line.getWidth() > 0f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        line.drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(count(content, " cm /Fm") > 5, content);
        assertEquals(1, count(content, "/ActualText <" + Page.toUTF16Hex(HINDI.substring(0, 20)) + ">"), content);
        assertFalse(content.contains("Tj"), content);
        pdf.complete();
    }
}
