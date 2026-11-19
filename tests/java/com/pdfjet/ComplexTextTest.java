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
import java.util.ArrayList;
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
        public List<ShapedLine> shape(ShapingRequest request) {
            this.files = request.getFontFiles();
            this.size = request.getFontSize();
            this.width = request.getWidth();
            this.language = request.getLanguage();
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
        float height = text.getTextHeight();
        float[] corner = text.drawOn(page);
        assertEquals(250f, corner[0], 0.001f);
        assertEquals(100f + height, corner[1], 0.001f);

        String content = TestSupport.content(page);
        // One paragraph in Hindi, a span of actual text for each line, no text
        assertEquals(1, count(content, "/P <</MCID 0>>\nBDC\n"), content);
        int lines = count(content, "/Span <</Lang <6869> /ActualText <");
        assertTrue(lines >= 3, "wrapped to 200 points: " + lines + " lines");
        assertEquals(lines * (hindi.getAscent() + hindi.getDescent() + hindi.getLineGap(12f)), height, 0.01f);
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
                new ShapedLine(0, 2, new int[] {0, 1}, new int[] {a, b}, new float[] {0f, 0f, 7f, 0f}, 13f),
                // A glyph of a font that was not given is not drawn
                new ShapedLine(2, 3, new int[] {-1}, new int[] {5}, new float[] {0f, 0f}, 6f));
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

        float lineHeight = font.getAscent() + font.getDescent() + font.getLineGap(10f);
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

    // The x of each glyph placed in the content
    private static List<Float> xs(String content) {
        List<Float> xs = new java.util.ArrayList<Float>();
        Matcher m = Pattern.compile("q \\S+ 0 0 \\S+ (\\S+) \\S+ cm /Fm").matcher(content);
        while (m.find()) {
            xs.add(Float.parseFloat(m.group(1)));
        }
        return xs;
    }

    // Two lines: "A A B " of 30 points without its last space, then "B"
    private static List<Float> aligned(Alignment alignment) throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream());
        Font font = new Font(pdf, LATIN).setSize(10f);
        int a = font.unicodeToGID.get('A');
        int b = font.unicodeToGID.get('B');
        int space = font.unicodeToGID.get(' ');
        FixedShaper shaper = new FixedShaper(
                new ShapedLine(0, 6, new int[] {0, 0, 0, 0, 0, 0}, new int[] {a, space, a, space, b, space},
                        new float[] {0f, 0f, 10f, 0f, 12f, 0f, 22f, 0f, 24f, 0f, 30f, 0f}, 30f),
                new ShapedLine(6, 7, new int[] {0}, new int[] {b}, new float[] {0f, 0f}, 6f));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new ComplexText(font, "A A B B").setShaper(shaper).setTextAlignment(alignment)
                .setLocation(50f, 100f).setWidth(100f).drawOn(page);
        return xs(TestSupport.content(page));
    }

    @Test
    void theLinesAreAligned() throws Exception {
        // The glyphs drawn: A, A, B of the first line, B of the second
        assertEquals(Arrays.asList(50f, 62f, 74f, 50f), aligned(Alignment.LEFT));
        assertEquals(Arrays.asList(85f, 97f, 109f, 97f), aligned(Alignment.CENTER));
        assertEquals(Arrays.asList(120f, 132f, 144f, 144f), aligned(Alignment.RIGHT));
        // Justified: the 70 points left over go to the two spaces between the
        // words; the last line is left as it is
        assertEquals(Arrays.asList(50f, 97f, 144f, 50f), aligned(Alignment.JUSTIFY));
    }

    @Test
    void theLineSpacingIsAMultipleOfTheBodyHeightAsTextBlocks() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream());
        Font font = new Font(pdf, LATIN).setSize(10f);
        int a = font.unicodeToGID.get('A');
        FixedShaper shaper = new FixedShaper(
                new ShapedLine(0, 1, new int[] {0}, new int[] {a}, new float[] {0f, 0f}, 7f),
                new ShapedLine(1, 2, new int[] {0}, new int[] {a}, new float[] {0f, 0f}, 7f));
        ComplexText text = new ComplexText(font, "AA").setShaper(shaper);
        float body = font.getAscent() + font.getDescent() + font.getLineGap(10f);
        assertEquals(2 * body, text.getTextHeight(), 0.001f);
        assertEquals(3 * body, text.setLineSpacing(1.5f).getTextHeight(), 0.001f);
        // As a TextBlock of the same font and spacing, line to line
        Page page = new Page(pdf, Letter.PORTRAIT);
        float[] corner = text.setLocation(50f, 100f).drawOn(page);
        assertEquals(100f + 3 * body, corner[1], 0.001f);
    }

    // A shaper that gives each paragraph as many lines as its letters, one
    // glyph of the letter each
    private static final class LetterShaper implements Shaper {
        final Font font;

        LetterShaper(Font font) {
            this.font = font;
        }

        @Override
        public List<ShapedLine> shape(ShapingRequest request) {
            String text = request.getText();
            List<ShapedLine> lines = new java.util.ArrayList<ShapedLine>();
            for (int i = 0; i < text.length(); i++) {
                lines.add(new ShapedLine(i, i + 1, new int[] {0}, new int[] {font.unicodeToGID.get(text.charAt(i))},
                        new float[] {0f, 0f}, 7f));
            }
            return lines;
        }
    }

    @Test
    void paragraphsFlowFromPageToPageCutWhereTheHeightEnds() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1).setTitle("Title");
        Font font = new Font(pdf, LATIN).setSize(10f);
        float line = font.getAscent() + font.getDescent() + font.getLineGap(10f);
        ComplexText text = new ComplexText(font, Arrays.asList("AAA", "BBB"))
                .setShaper(new LetterShaper(font))
                .setLanguage("en")
                .setParagraphGap(5f)
                .setHeight(4 * line + 5f);
        assertEquals(6 * line + 5f, text.getTextHeight(), 0.001f);
        // Three lines of the first paragraph, the gap, one of the second
        Page first = new Page(pdf, Letter.PORTRAIT);
        float[] corner = text.setLocation(50f, 100f).drawOn(first);
        assertEquals(100f + 4 * line + 5f, corner[1], 0.001f);
        assertTrue(text.hasMoreText());
        assertEquals(2 * line, text.getTextHeight(), 0.001f);
        String content = TestSupport.content(first);
        assertEquals(4, count(content, " cm /Fm"), content);
        assertEquals(2, count(content, "/P <<"), "a paragraph each, the second begun");
        // The rest of the second paragraph, from the top of the next page
        Page second = new Page(pdf, Letter.PORTRAIT);
        corner = text.setLocation(50f, 50f).drawOn(second);
        assertEquals(50f + 2 * line, corner[1], 0.001f);
        assertFalse(text.hasMoreText());
        assertEquals(0f, text.getTextHeight(), 0.001f);
        assertEquals(2, count(TestSupport.content(second), " cm /Fm"));
        pdf.complete();
    }

    @Test
    void getHeightIsTheHeightSetAndTheGapIsTextFramesEmptyLine() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream());
        Font font = new Font(pdf, LATIN).setSize(10f);
        ComplexText text = new ComplexText(font, Arrays.asList("A", "B")).setShaper(new LetterShaper(font));
        assertEquals(0f, text.getHeight());
        assertEquals(500f, text.setHeight(500f).getHeight());
        // Two lines, one empty line between them, as a TextFrame's default:
        // the ascent and the descent, and the line gap on each side
        float line = font.getAscent() + font.getDescent() + font.getLineGap(10f);
        float gap = font.getAscent() + font.getDescent() + 2f * font.getLineGap(10f);
        assertEquals(2 * line + gap, text.getTextHeight(), 0.001f);
    }

    @Test
    void aHeightTooSmallForALineStillDrawsOneEachTime() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream());
        Font font = new Font(pdf, LATIN).setSize(10f);
        ComplexText text = new ComplexText(font, "AB").setShaper(new LetterShaper(font)).setHeight(1f);
        int pages = 0;
        while (text.hasMoreText()) {
            text.drawOn(new Page(pdf, Letter.PORTRAIT));
            pages++;
        }
        assertEquals(2, pages);
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

    // Without a page, drawOn measures: the corner drawOn would return, the
    // text left as it was, so that a Table cell can measure it
    @Test
    void drawOnWithoutAPageMeasuresAndKeepsTheText() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream());
        Font hindi = new Font(pdf, DEVANAGARI).setSize(12f);
        ComplexText text = new ComplexText(hindi, HINDI).setLanguage("hi").setLocation(50f, 100f).setWidth(150f);
        float height = text.getTextHeight();
        float[] first = text.drawOn(null);
        float[] second = text.drawOn(null);
        assertEquals(200f, first[0], 0.001f);
        assertEquals(100f + height, first[1], 0.001f);
        assertArrayEquals(first, second, 0.001f);
        assertTrue(text.hasMoreText());
        assertEquals(height, text.getTextHeight(), 0.001f);
        // With a height, the lines that fit, as drawOn draws them
        float lineHeight = new ComplexText(hindi, "क").setWidth(150f).getTextHeight();
        text.setHeight(2.5f * lineHeight);
        assertEquals(100f + 2f * lineHeight, text.drawOn(null)[1], 0.001f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        assertEquals(100f + 2f * lineHeight, text.drawOn(page)[1], 0.001f);
    }

    // Hindi in Table cells: each cell as tall as its text wrapped to the
    // column, the text drawn whole each time, as the header row is on each
    // page; the caller's text where it was
    @Test
    void hindiInTableCellsWrapsAndRepeatsTheHeader() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1).setTitle("Title");
        Font hindi = new Font(pdf, DEVANAGARI).setSize(11f);
        Font latin = new Font(pdf, LATIN).setSize(11f);
        List<List<Cell>> data = new ArrayList<List<Cell>>();
        List<Cell> header = new ArrayList<Cell>();
        header.add(new Cell(latin).setDrawable(new ComplexText(hindi, "नाम").setLanguage("hi")));
        header.add(new Cell(latin).setDrawable(new ComplexText(hindi, "विवरण").setLanguage("hi")));
        data.add(header);
        ComplexText kept = new ComplexText(hindi, HINDI).setFallbackFont(latin).setLanguage("hi").setLocation(7f, 9f);
        for (int r = 0; r < 30; r++) {
            List<Cell> row = new ArrayList<Cell>();
            row.add(new Cell(latin, "Row " + (r + 1)));
            row.add(new Cell(latin).setDrawable(r == 0 ? kept
                    : new ComplexText(hindi, HINDI).setFallbackFont(latin).setLanguage("hi")));
            data.add(row);
        }
        Table table = new Table().setTableData(data, 1).setLocation(50f, 50f);
        table.setColumnWidth(0, 100f).setColumnWidth(1, 250f);
        // A cell of Hindi is as tall as its lines in the column, more than one
        Cell cell = data.get(1).get(1);
        float inner = 250f - (cell.getLeftPadding() + cell.getRightPadding());
        float lines = new ComplexText(hindi, HINDI).setFallbackFont(latin).setLanguage("hi").setWidth(inner).getTextHeight();
        float oneLine = new ComplexText(hindi, "क").setWidth(inner).getTextHeight();
        assertTrue(lines > 1.5f * oneLine, "wraps to more than one line: " + lines);
        assertEquals(lines + cell.getTopPadding() + cell.getBottomPadding(), cell.getHeight(250f), 0.01f);
        List<Page> pages = new ArrayList<Page>();
        table.drawOn(pdf, pages, Letter.PORTRAIT);
        assertTrue(pages.size() >= 2, "the table runs over pages: " + pages.size());
        // The header row: its words as text on the first page; on the next
        // pages drawn again, as an artifact, as PDF/UA has a repeated header
        String first = TestSupport.content(pages.get(0));
        assertEquals(1, count(first, "/ActualText <" + Page.toUTF16Hex("नाम") + ">"), first);
        assertEquals(1, count(first, "/ActualText <" + Page.toUTF16Hex("विवरण") + ">"), first);
        for (int i = 1; i < pages.size(); i++) {
            String content = TestSupport.content(pages.get(i));
            int start = content.indexOf("/Artifact BMC\n");
            assertTrue(start >= 0, "page " + (i + 1) + " has its header");
            String drawn = content.substring(start, content.indexOf("EMC\n", start));
            assertTrue(count(drawn, " Do Q\n") >= 3, "page " + (i + 1) + ": its header's glyphs drawn");
        }
        // The caller's text kept where it was, and whole
        assertArrayEquals(new float[] {7f, 9f}, Cell.locationOf(kept), 0.001f);
        assertTrue(kept.hasMoreText());
    }
}
