/*
 * Review919Test.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.*;

import java.io.ByteArrayOutputStream;
import java.util.*;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Test;

/**
 * The faults found by the review of 9.1.9 (16 November 2026), each kept from
 * coming back.
 */
class Review919Test {
    private static final String CASCADIA = "tests/fonts/CascadiaMono-Regular.ttf";

    @AfterEach
    void forbidCoreFonts() {
        TestSupport.allowCoreFonts(false);
    }

    private static int u16(byte[] b, int at) {
        return (b[at] & 0xFF) << 8 | (b[at + 1] & 0xFF);
    }

    private static int u32(byte[] b, int at) {
        return (b[at] & 0xFF) << 24 | (b[at + 1] & 0xFF) << 16 | (b[at + 2] & 0xFF) << 8 | (b[at + 3] & 0xFF);
    }

    private static void putU32(byte[] b, int at, int value) {
        b[at] = (byte) (value >>> 24);
        b[at + 1] = (byte) (value >>> 16);
        b[at + 2] = (byte) (value >>> 8);
        b[at + 3] = (byte) value;
    }

    // The entry of the table in the table directory of the font.
    private static int entry(byte[] font, String tag) {
        int numTables = u16(font, 4);
        for (int i = 0; i < numTables; i++) {
            int at = 12 + 16 * i;
            if (new String(font, at, 4, java.nio.charset.StandardCharsets.ISO_8859_1).equals(tag)) {
                return at;
            }
        }
        throw new AssertionError("no " + tag + " table");
    }

    private static int count(String text, String part) {
        int n = 0;
        for (int i = text.indexOf(part); i >= 0; i = text.indexOf(part, i + 1)) {
            n++;
        }
        return n;
    }

    // The outline of Cascadia Mono with the glyphs given made the bytes given,
    // written after its glyf table, which is made to reach the end of the font.
    private static GlyphOutline outlineWithGlyphs(Map<Integer, byte[]> glyphs) throws Exception {
        byte[] font = TestSupport.fontBytes(CASCADIA);
        int head = u32(font, entry(font, "head") + 8);
        assertEquals(1, u16(font, head + 50), "short loca offsets");
        int loca = u32(font, entry(font, "loca") + 8);
        int glyfEntry = entry(font, "glyf");
        int glyf = u32(font, glyfEntry + 8);
        ByteArrayOutputStream data = new ByteArrayOutputStream();
        data.write(font, 0, font.length);
        for (Map.Entry<Integer, byte[]> e : glyphs.entrySet()) {
            while ((data.size() - glyf) % 4 != 0) {
                data.write(0);
            }
            int start = data.size() - glyf;
            data.write(e.getValue(), 0, e.getValue().length);
            int end = data.size() - glyf;
            // The glyph after is not one of those given, so its start is kept
            byte[] bytes = data.toByteArray();
            putU32(bytes, loca + 4 * e.getKey(), start);
            putU32(bytes, loca + 4 * e.getKey() + 4, end);
            data.reset();
            data.write(bytes, 0, bytes.length);
        }
        byte[] bytes = data.toByteArray();
        putU32(bytes, glyfEntry + 12, bytes.length - glyf);
        return new GlyphOutline(bytes, false);
    }

    // A composite glyph of the parts, each placed at 0 0.
    private static byte[] compositeGlyph(int... parts) {
        ByteArrayOutputStream glyph = new ByteArrayOutputStream();
        glyph.write(0xFF);  // -1 contours, the box
        glyph.write(0xFF);
        for (int i = 0; i < 8; i++) {
            glyph.write(0);
        }
        for (int i = 0; i < parts.length; i++) {
            int flags = 0x0002;     // ARGS_ARE_XY_VALUES, the arguments bytes
            if (i < parts.length - 1) {
                flags |= 0x0020;    // MORE_COMPONENTS
            }
            glyph.write(flags >> 8);
            glyph.write(flags);
            glyph.write(parts[i] >> 8);
            glyph.write(parts[i]);
            glyph.write(0);
            glyph.write(0);
        }
        return glyph.toByteArray();
    }

    // A composite glyph of four parts, each of four parts, 16 deep, down to an
    // O, was 4^16 Os read; the work of a glyph is bounded.
    @Test
    void aTrueTypeCompositeGlyphOfManyPartsIsRefusedQuickly() throws Exception {
        Font cascadia = new Font(TestSupport.newPDF(), TestSupport.open(CASCADIA));
        int o = cascadia.unicodeToGID.get('O');
        // Every other glyph of the last 40, so that the glyph after each keeps
        // its start, and none is the O
        Map<Integer, byte[]> glyphs = new LinkedHashMap<Integer, byte[]>();
        int below = o;
        int first = cascadia.advanceWidth.length - 40;
        int gid = first;
        for (int level = 0; level < 16; level++) {
            glyphs.put(gid, compositeGlyph(below, below, below, below));
            below = gid;
            gid += 2;
        }
        final GlyphOutline outline = outlineWithGlyphs(glyphs);
        // Two levels are drawn, as a font may have them
        assertEquals(32, count(outline.outline(first + 2).operators, " m\n"));
        long start = System.nanoTime();
        final int top = below;
        assertThrows(IllegalArgumentException.class, () -> outline.outline(top));
        long elapsed = (System.nanoTime() - start) / 1000000;
        assertTrue(elapsed < 2000, elapsed + " ms");
    }

    // The outline of IBM Plex Sans with the charstring of glyph 1 made the
    // bytes given.
    private static GlyphOutline cffOutlineWithCharString(byte[] cs) throws Exception {
        byte[] base = TestSupport.plexSans(TestSupport.newPDF()).program.font;
        byte[] data = Arrays.copyOf(base, base.length + cs.length);
        System.arraycopy(cs, 0, data, base.length, cs.length);
        GlyphOutline o = new GlyphOutline(data, true);
        java.lang.reflect.Field field = GlyphOutline.class.getDeclaredField("charStrings");
        field.setAccessible(true);
        int[] objects = ((CFFSubset.Index) field.get(o)).objects;
        objects[1] = base.length;
        objects[2] = data.length;
        return o;
    }

    // A charstring's arithmetic made a point of 1.15e18 units, written as a
    // number of no meaning, or one Swift trapped on; a point past the bound is
    // a damaged glyph.
    @Test
    void aCFFGlyphOfAPointFarOutIsRefused() throws Exception {
        // 32767 dup mul dup mul 0 rmoveto 0 1 rlineto endchar
        byte[] cs = {0x1C, 0x7F, (byte) 0xFF, 0x0C, 0x1B, 0x0C, 0x18, 0x0C, 0x1B, 0x0C, 0x18,
            (byte) 0x8B, 0x15, (byte) 0x8B, (byte) 0x8C, 0x05, 0x0E};
        final GlyphOutline outline = cffOutlineWithCharString(cs);
        assertThrows(IllegalArgumentException.class, () -> outline.outline(1));
    }

    private static byte[] concat(byte[]... parts) {
        ByteArrayOutputStream all = new ByteArrayOutputStream();
        for (byte[] p : parts) {
            all.write(p, 0, p.length);
        }
        return all.toByteArray();
    }

    // roll and index take their operands as numbers before integers, so that
    // a count of 2^31 is refused in the four ports alike.
    @Test
    void aCFFRollOrIndexOfAnOperandTooLargeIsRefused() throws Exception {
        // 2^31 as 32767 1 add 65536 mul, with 65536 as 32767 2 mul 2 add
        byte[] big = {0x1C, 0x7F, (byte) 0xFF, (byte) 0x8C, 0x0C, 0x0A, 0x1C, 0x7F, (byte) 0xFF,
            (byte) 0x8D, 0x0C, 0x18, (byte) 0x8D, 0x0C, 0x0A, 0x0C, 0x18};
        Map<String, byte[]> ops = new LinkedHashMap<String, byte[]>();
        ops.put("roll", concat(new byte[] {(byte) 0x8C, (byte) 0x8D, (byte) 0x8E, (byte) 0x8E}, big,
                new byte[] {0x0C, 0x1E}));
        ops.put("index", concat(new byte[] {(byte) 0x8C}, big, new byte[] {0x0C, 0x1D}));
        for (Map.Entry<String, byte[]> op : ops.entrySet()) {
            // 0 0 rmoveto endchar
            byte[] cs = concat(op.getValue(), new byte[] {(byte) 0x8B, (byte) 0x8B, 0x15, 0x0E});
            final GlyphOutline outline = cffOutlineWithCharString(cs);
            assertThrows(IllegalArgumentException.class, () -> outline.outline(1), op.getKey());
        }
    }

    // A position of a glyph not finite, or far off the page, is an error, not
    // a number of no meaning in the content.
    @Test
    void drawGlyphsAPositionNotFiniteIsAnError() throws Exception {
        for (float x : new float[] {Float.POSITIVE_INFINITY, Float.NaN, 2e6f}) {
            PDF pdf = TestSupport.newPDF();
            final Font font = TestSupport.plexSans(pdf);
            final Page page = new Page(pdf, Letter.PORTRAIT);
            final float[] positions = {x, 100f};
            Exception e = assertThrows(IllegalArgumentException.class,
                    () -> page.drawGlyphs(font, new int[] {36}, positions, "A", ""));
            assertTrue(e.getMessage().contains("positions between"), x + ": " + e.getMessage());
        }
    }

    // The header of a BigTable whose header and rows have one font took the
    // rows' fallback font: the header has its own, and the rows theirs.
    @Test
    void bigTableOfOneFontGivesTheHeaderItsOwnFallbackFont() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font plex = TestSupport.plexSans(pdf);
        Font kr = new Font(pdf, com.pdfjet.fonts.IBMPlexSansKR.Regular);
        BigTable bt = new BigTable(pdf, plex, plex, Letter.PORTRAIT).setFallbackFonts(kr, null);
        assertTrue(bt.measureOf(true).fallback == kr && bt.measureOf(false).fallback == null,
                "the header's fallback font is not its own");
        bt = new BigTable(pdf, plex, plex, Letter.PORTRAIT).setFallbackFonts(null, kr);
        assertTrue(bt.measureOf(true).fallback == null && bt.measureOf(false).fallback == kr,
                "the rows' fallback font is not theirs");
    }

    // The widths set for more columns than the table has: wrapped, it threw
    // an ArrayIndexOutOfBoundsException before the check of the widths, which
    // comes first, as it does not wrapped.
    @Test
    void bigTableWrappedWithWidthsOfMoreColumnsIsAnError() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font plex = TestSupport.plexSans(pdf);
        final BigTable table = new BigTable(pdf, plex, plex, Letter.PORTRAIT)
                .setNumberOfColumns(3).setColumnWidths(100f, 100f, 100f)
                .setNumberOfColumns(2).setWrapText(true);
        final List<String[]> rows = new ArrayList<String[]>();
        rows.add(new String[] {"a", "b", "c"});
        Exception e = assertThrows(IllegalArgumentException.class,
                () -> table.setTableData(new String[] {"A", "B", "C"}, rows));
        assertEquals("The table has 2 columns, and 3 widths: give one for each column.", e.getMessage());
    }

    // Two lines that meet end to end, or edge to edge, may paint one pixel,
    // the hairline of width 0 most of all: they are stroked in the order of
    // the cells, or the color on top of that pixel changed when the borders
    // were joined.
    @Test
    void tableLinesThatMeetOverlap() {
        TableLines.Segment black = new TableLines.Segment(true, 50f, 332.94f, 350.82f, 0f, 0f, 0f, 0f, 0f);
        TableLines.Segment blue = new TableLines.Segment(true, 50f, 350.82f, 368.69f, 0f, 0f, 1f, 0f, 0f);
        assertTrue(black.overlaps(blue) && blue.overlaps(black), "hairlines that meet end to end do not overlap");
        TableLines.Segment red = new TableLines.Segment(false, 100f, 50f, 148f, 1f, 0f, 0f, 3f, 0f);
        TableLines.Segment hairline = new TableLines.Segment(true, 148f, 100f, 120f, 0f, 0f, 0f, 0f, 0f);
        assertTrue(red.overlaps(hairline), "a hairline at the end of a wide line does not overlap it");
        TableLines.Segment far = new TableLines.Segment(true, 160f, 100f, 120f, 0f, 0f, 0f, 0f, 0f);
        assertFalse(red.overlaps(far), "lines 10 points apart overlap");
    }

    // A heading, a rule and a table: the rule is not a paragraph to measure,
    // and Java and C# threw on its text, which is none.
    @Test
    void markdownHeadingRuleAndTableAreDrawn() throws Exception {
        StringBuilder words = new StringBuilder();
        for (int i = 0; i < 1500; i++) {
            words.append("word ");
        }
        String md = "# " + words + "\n\n---\n\n| a | b |\n|---|---|\n| 1 | 2 |\n";
        TestSupport.allowCoreFonts(true);
        MarkdownTest.Drawn drawn = MarkdownTest.draw(md, null);
        assertTrue(drawn.pages.size() >= 2 && drawn.text.contains("a"), drawn.pages.size() + " pages");
    }

    // Drawing on a page of an existing PDF that has no content: the content
    // restored a graphics state it never saved, "Q" first.
    @Test
    void drawingOnAnEmptyPageOfAnExistingPDFIsBalanced() throws Exception {
        List<PDFobj> objects = TestSupport.read(PDFTest.pdfWithObjects(new String[] {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << >> >>"}));
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, pdf.getPageObjects(objects).get(0));
        page.drawRect(10f, 10f, 20f, 20f);
        page.complete(objects);
        for (PDFobj obj : objects) {
            String content = obj.stream == null ? "" : TestSupport.latin1(obj.stream);
            if (content.contains(" l\n")) {
                assertTrue(content.startsWith("q\nQ\n"), "the content starts " + content);
                return;
            }
        }
        fail("no content drawn");
    }
}
