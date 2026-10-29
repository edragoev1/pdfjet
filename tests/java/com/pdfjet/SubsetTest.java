/*
 * SubsetTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertArrayEquals;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.ByteArrayOutputStream;
import java.nio.charset.StandardCharsets;
import java.util.Arrays;
import java.util.HashSet;
import java.util.Set;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import java.util.zip.Inflater;
import org.junit.jupiter.api.Test;

class SubsetTest {
    private static byte[] fontBytes(String path) throws Exception {
        return TestSupport.fontBytes(path);
    }

    // IBM Plex Sans, which has the short offsets of loca; the subsets of a
    // font with the long ones read Cascadia Mono or IBM Plex Math.
    private static final String PLEX = "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf";

    private static int u16(byte[] font, int at) {
        return (font[at] & 0xFF) << 8 | (font[at + 1] & 0xFF);
    }

    private static int u32(byte[] font, int at) {
        return (font[at] & 0xFF) << 24 | (font[at + 1] & 0xFF) << 16 | (font[at + 2] & 0xFF) << 8 | (font[at + 3] & 0xFF);
    }

    // Returns where the directory entry of the table begins, or -1.
    private static int entry(byte[] font, String name) {
        for (int i = 0; i < u16(font, 4); i++) {
            int entry = 12 + 16 * i;
            if (new String(font, entry, 4, StandardCharsets.ISO_8859_1).equals(name)) {
                return entry;
            }
        }
        return -1;
    }

    private static int table(byte[] font, String name) {
        int entry = entry(font, name);
        assertTrue(entry != -1, "the font has no " + name + " table");
        return u32(font, entry + 8);
    }

    // Returns the bytes of the glyph in the glyf table, at the offsets its
    // loca table gives.
    private static byte[] glyph(byte[] font, int gid) {
        int loca = table(font, "loca");
        int glyf = table(font, "glyf");
        int start, end;
        if (u16(font, table(font, "head") + 50) == 1) {
            start = u32(font, loca + 4 * gid);
            end = u32(font, loca + 4 * gid + 4);
        } else {
            start = 2 * u16(font, loca + 2 * gid);
            end = 2 * u16(font, loca + 2 * gid + 2);
        }
        return Arrays.copyOfRange(font, glyf + start, glyf + end);
    }

    private static boolean[] used(int... gids) {
        boolean[] used = new boolean[0x10000];
        for (int gid : gids) {
            used[gid] = true;
        }
        return used;
    }

    @Test
    void keepsTheGlyphsUsedAndThePartsOfTheComposites() throws Exception {
        // In Cascadia Mono, whose loca has the long offsets, as that of Noto
        // Sans, which this test read before 9.1.7, had: Ä (16) is made of A
        // (1) and the dieresis of capitals (3051), and ặ (229) of a (225),
        // the dot below (3029) and the breve (3020); 4,319 glyphs. Noto Sans
        // had Ä (134) of A (36) and the dieresis (106), and ǅ (913) of three.
        byte[] ttf = fontBytes("fonts/CascadiaMono/CascadiaMono-Regular.ttf");
        assertEquals(1, u16(ttf, table(ttf, "head") + 50));
        boolean[][] kept = new boolean[1][];
        byte[] subset = Subset.subsetTrueType(ttf, used(16, 229), kept);
        assertEquals(4319, kept[0].length);
        Set<Integer> want = new HashSet<Integer>(Arrays.asList(0, 16, 1, 3051, 229, 225, 3029, 3020));
        for (int gid = 0; gid < kept[0].length; gid++) {
            assertEquals(want.contains(gid), kept[0][gid], "glyph " + gid);
            byte[] glyph = glyph(subset, gid);
            if (kept[0][gid]) {
                // The glyph as it was, padded to four bytes.
                byte[] whole = glyph(ttf, gid);
                assertArrayEquals(whole, Arrays.copyOf(glyph, whole.length), "glyph " + gid);
                assertTrue(glyph.length - whole.length < 4);
            } else {
                assertEquals(0, glyph.length, "glyph " + gid);
            }
        }
        assertTrue(subset.length <= ttf.length / 4, "the subset is " + subset.length + " bytes");
        // The other tables a reader needs as they were; shaping left out, and
        // the cmap and name tables, which a CIDFontType2 does not need.
        for (String name : new String[] {"GPOS", "GSUB", "GDEF", "cmap", "name"}) {
            assertEquals(-1, entry(subset, name), "the subset has the " + name + " table");
        }
        assertEquals(0x00030000, u32(subset, table(subset, "post")));
        for (String name : new String[] {"hmtx", "hhea", "OS/2", "maxp"}) {
            int length = u32(ttf, entry(ttf, name) + 12);
            int at = table(ttf, name);
            int subsetAt = table(subset, name);
            assertArrayEquals(Arrays.copyOfRange(ttf, at, at + length),
                    Arrays.copyOfRange(subset, subsetAt, subsetAt + length), "the " + name + " table changed");
        }
        // The whole font sums to the magic number of its head table.
        assertEquals(0xB1B0AFBAL, Subset.tableChecksum(subset));
    }

    @Test
    void subsetsAFontWithShortOffsets() throws Exception {
        // IBM Plex Sans Thai, whose loca has the short offsets, as Noto Sans
        // Thai's had.
        byte[] ttf = fontBytes("fonts/IBMPlexSansThai/IBMPlexSansThai-Regular.ttf");
        assertEquals(0, u16(ttf, table(ttf, "head") + 50));
        byte[] subset = Subset.subsetTrueType(ttf, used(5), new boolean[1][]);
        byte[] whole = glyph(ttf, 5);
        assertArrayEquals(whole, Arrays.copyOf(glyph(subset, 5), whole.length));
        assertEquals(0, glyph(subset, 6).length);
    }

    @Test
    void isRefusedByAFontWhoseLicenseForbidsIt() throws Exception {
        byte[] ttf = fontBytes(PLEX);
        int os2 = table(ttf, "OS/2");
        ttf[os2 + 8] = 0x01;
        ttf[os2 + 9] = 0x00;
        final byte[] patched = ttf;
        assertThrows(Subset.NotSubset.class, () -> Subset.subsetTrueType(patched, used(36), new boolean[1][]));
    }

    // Draws the texts, each with a font made from the file, and returns the
    // document.
    private static String doc(Compliance compliance, String path, boolean subset, String... texts) throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, compliance).setTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        for (int i = 0; i < texts.length; i++) {
            Font font = new Font(pdf, TestSupport.file(path).getPath());
            font.setSubset(subset);
            new TextLine(font, texts[i]).setLocation(50f, 50f + 20f * i).drawOn(page);
        }
        pdf.complete();
        return TestSupport.latin1(bos.toByteArray());
    }

    private static byte[] inflate(String raw, int at, int length) throws Exception {
        Inflater inflater = new Inflater();
        inflater.setInput(raw.substring(at, at + length).getBytes(StandardCharsets.ISO_8859_1));
        ByteArrayOutputStream out = new ByteArrayOutputStream();
        byte[] buf = new byte[65536];
        while (!inflater.finished()) {
            int count = inflater.inflate(buf);
            assertTrue(count > 0 || !inflater.needsInput(), "the stream ends too soon");
            out.write(buf, 0, count);
        }
        return out.toByteArray();
    }

    // Returns the font program embedded in the document.
    private static byte[] program(String raw) throws Exception {
        Matcher m = Pattern.compile("/Length1 (\\d+)\n(?:/Metadata \\d+ 0 R\n)?/Length (\\d+)\n>>\nstream\n").matcher(raw);
        assertTrue(m.find(), "no font program");
        byte[] program = inflate(raw, m.end(), Integer.parseInt(m.group(2)));
        assertEquals(Integer.parseInt(m.group(1)), program.length, "/Length1");
        return program;
    }

    private static int count(Pattern pattern, String raw) {
        int n = 0;
        for (Matcher m = pattern.matcher(raw); m.find(); ) {
            n++;
        }
        return n;
    }

    private static final Pattern TAGGED = Pattern.compile("/BaseFont /([A-Z]{6}\\+IBMPlexSans)\n");

    @Test
    void aTrueTypeFontIsEmbeddedAsASubsetUnderATaggedName() throws Exception {
        for (String path : new String[] {PLEX}) {
            String raw = doc(Compliance.PDF_1_7, path, true, "Ä");
            Matcher m = TAGGED.matcher(raw);
            assertTrue(m.find(), path);
            assertTrue(raw.contains("/FontName /" + m.group(1) + "\n"), path);
            assertEquals(2, count(TAGGED, raw), path);
            byte[] program = program(raw);
            // In IBM Plex Sans, Ä (395) is made of A (33) and the dieresis
            // of capitals (971), and B is 34. Noto Sans had Ä (134), A (36),
            // the dieresis (106) and B (37).
            assertTrue(glyph(program, 395).length > 0 && glyph(program, 33).length > 0, path);
            assertEquals(0, glyph(program, 34).length, path);
            // The widths of the glyphs kept: .notdef, A, Ä and the dieresis.
            assertTrue(Pattern.compile("/W \\[\n0\\[\\d+ \\]\n33\\[\\d+ \\]\n395\\[\\d+ \\]\n971\\[\\d+ \\]\\]\n")
                    .matcher(raw).find(), path + ": the widths");
            assertFalse(raw.contains("/CIDSet"), path);
        }
    }

    @Test
    void aFontSetToStayWholeIsEmbeddedWhole() throws Exception {
        byte[] ttf = fontBytes(PLEX);
        for (String path : new String[] {PLEX}) {
            String raw = doc(Compliance.PDF_1_7, path, false, "Ä");
            assertTrue(raw.contains("/BaseFont /IBMPlexSans\n") && !raw.contains("+IBMPlexSans"), path);
            assertArrayEquals(ttf, program(raw), path);
        }
    }

    @Test
    void twoFontsOfOneFileShareOneSubset() throws Exception {
        String raw = doc(Compliance.PDF_1_7, PLEX, true, "A", "B");
        assertEquals(1, count(Pattern.compile("/Length1 "), raw));
        byte[] program = program(raw);
        // A and B, 33 and 34 in IBM Plex Sans; 36 and 37 in Noto Sans.
        assertTrue(glyph(program, 33).length > 0 && glyph(program, 34).length > 0);
        // Two Type0 fonts and their CID font.
        assertEquals(3, count(TAGGED, raw));
    }

    @Test
    void aPDFA1HasTheCIDSetOfTheGlyphsKept() throws Exception {
        String raw = doc(Compliance.PDF_A_1B, PLEX, true, "A");
        Matcher m = Pattern.compile("/CIDSet (\\d+) 0 R\n").matcher(raw);
        assertTrue(m.find(), "no CIDSet");
        int at = raw.indexOf("\n" + m.group(1) + " 0 obj\n");
        assertTrue(at != -1);
        Matcher s = Pattern.compile("/Length (\\d+)\n>>\nstream\n").matcher(raw);
        assertTrue(s.find(at));
        byte[] bits = inflate(raw, s.end(), Integer.parseInt(s.group(1)));
        // Glyphs 0 and 33, A, of the 1,019 of IBM Plex Sans; Noto Sans had A
        // at 36 of 4,503.
        byte[] want = new byte[(1019 + 7) / 8];
        want[0] = (byte) 0x80;
        want[33 / 8] |= (byte) (0x80 >> (33 % 8));
        assertArrayEquals(want, bits);
        assertNotNull(bits);
    }

    // Adds the font to the objects of a PDF, draws a word with it on the page,
    // and returns the objects of the PDF written.
    private static java.util.List<PDFobj> subsetInExistingPDF(String path, boolean subset) throws Exception {
        java.util.List<PDFobj> objects = TestSupport.read(PDFTest.pdfWithObjects(new String[] {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << >> >>"}));
        Font font = new Font(objects, TestSupport.open(path)).setSubset(subset);
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Page page = new Page(pdf, pdf.getPageObjects(objects).get(0));
        page.addResource(font, objects);
        page.drawString(font, null, 12f, "Hello", 72f, 72f);
        page.complete(objects);
        pdf.addObjects(objects);
        pdf.complete();
        return TestSupport.read(bos.toByteArray());
    }

    // Returns the /BaseFont of the Type0 font.
    private static String subsetFontName(java.util.List<PDFobj> objects) {
        for (PDFobj obj : objects) {
            if ("/Type0".equals(obj.getValue("/Subtype"))) {
                return obj.getValue("/BaseFont");
            }
        }
        throw new AssertionError("no Type0 font");
    }

    @Test
    void subsetOfATrueTypeFontAddedToAnExistingPDF() throws Exception {
        String path = PLEX;
        int whole = fontBytes(path).length;
        java.util.List<PDFobj> objects = subsetInExistingPDF(path, true);
        String name = subsetFontName(objects);
        assertTrue(name.matches("/[A-Z]{6}\\+IBMPlexSans"), name);
        assertEquals(name, TestSupport.findObject(objects, "/FontName").getValue("/FontName"));
        assertEquals(name, TestSupport.findObject(objects, "/CIDToGIDMap").getValue("/BaseFont"));
        PDFobj file = TestSupport.findObject(objects, "/Length1");
        int length1 = Integer.parseInt(file.getValue("/Length1"));
        assertTrue(length1 > 0 && length1 < whole, "/Length1 " + length1);
        assertEquals(length1, file.getData().length);
        for (PDFobj obj : objects) {
            byte[] data = obj.getData();
            String cmap = data == null ? "" : new String(data, StandardCharsets.ISO_8859_1);
            if (cmap.startsWith("/CIDInit")) {
                // The codespace range, .notdef and H, e, l, o
                assertEquals(6, cmap.split("> <", -1).length - 1);
                return;
            }
        }
        throw new AssertionError("no ToUnicode map");
    }

    @Test
    void subsetOfACFFFontAddedToAnExistingPDF() throws Exception {
        java.util.List<PDFobj> objects = subsetInExistingPDF("tests/fonts/IBMPlexSans-Regular.otf", true);
        String name = subsetFontName(objects);
        assertTrue(name.matches("/[A-Z]{6}\\+IBMPlexSans"), name);
        assertNotNull(TestSupport.findObject(objects, "/FontFile3"));
    }

    @Test
    void setSubsetFalseKeepsAFontAddedToAnExistingPDFWhole() throws Exception {
        String path = PLEX;
        java.util.List<PDFobj> objects = subsetInExistingPDF(path, false);
        assertEquals("/IBMPlexSans", subsetFontName(objects));
        assertEquals(fontBytes(path).length,
                Integer.parseInt(TestSupport.findObject(objects, "/Length1").getValue("/Length1")));
    }

    // Checks that the font, made wrong, is not subset, and so embedded whole,
    // rather than an exception or a subset made of it.
    private static void assertRefused(final byte[] ttf, final boolean[] used) {
        assertThrows(Subset.NotSubset.class, () -> Subset.subsetTrueType(ttf, used, new boolean[1][]));
    }

    private static void putU16(byte[] font, int at, int value) {
        font[at] = (byte) (value >> 8);
        font[at + 1] = (byte) value;
    }

    private static void putU32(byte[] font, int at, long value) {
        font[at] = (byte) (value >> 24);
        font[at + 1] = (byte) (value >> 16);
        font[at + 2] = (byte) (value >> 8);
        font[at + 3] = (byte) value;
    }

    @Test
    void aTableTwiceIsRefused() throws Exception {
        // The gasp table's entry made a second head table of 8 bytes, whose
        // checksum adjustment the subset wrote past its end (the review of
        // 9 October 2026).
        byte[] ttf = fontBytes(PLEX);
        int gasp = entry(ttf, "gasp");
        System.arraycopy("head".getBytes(StandardCharsets.ISO_8859_1), 0, ttf, gasp, 4);
        putU32(ttf, gasp + 12, 8);
        assertRefused(ttf, used(36));
    }

    @Test
    void aLocaFormatOtherThan0Or1IsRefused() throws Exception {
        byte[] ttf = fontBytes("fonts/IBMPlexMath/IBMPlexMath-Regular.ttf");
        putU16(ttf, table(ttf, "head") + 50, 0xFFFF);
        assertRefused(ttf, used(36));
    }

    @Test
    void anOffsetPast2To31OfAGlyphNotKeptIsNotRefused() throws Exception {
        // A glyph whose loca offset is past 2^31 - 1 cannot be read, and is
        // refused only if it is kept, as the other ports do (the review of
        // 9 October 2026). IBM Plex Math has the long offsets of loca, as IBM
        // Plex Sans JP's .ttf, which this test read before 9.1.7, had.
        byte[] ttf = fontBytes("fonts/IBMPlexMath/IBMPlexMath-Regular.ttf");
        assertEquals(1, u16(ttf, table(ttf, "head") + 50));
        int numGlyphs = u16(ttf, table(ttf, "maxp") + 4);
        putU32(ttf, table(ttf, "loca") + 4 * numGlyphs, 0xFFFFFFF0L);
        assertNotNull(Subset.subsetTrueType(ttf, used(36), new boolean[1][]));
        assertRefused(ttf, used(36, numGlyphs - 1));
    }

    @Test
    void overlappingGlyphsAreRefused() throws Exception {
        // Every even glyph's outline the whole glyf table, so that a subset of
        // them copied it again for each (the review of 9 October 2026: 112 KB
        // made into 384 MB). A subset is never larger than the glyf table and the
        // padding of its glyphs.
        byte[] ttf = fontBytes(PLEX);
        int head = table(ttf, "head");
        int loca = table(ttf, "loca");
        int glyfLength = u32(ttf, entry(ttf, "glyf") + 12);
        int numGlyphs = u16(ttf, table(ttf, "maxp") + 4);
        boolean isLong = u16(ttf, head + 50) == 1;
        boolean[] used = new boolean[0x10000];
        for (int gid = 0; gid <= numGlyphs; gid++) {
            int offset = gid % 2 == 1 ? glyfLength & ~3 : 0;
            if (isLong) {
                putU32(ttf, loca + 4 * gid, offset);
            } else {
                putU16(ttf, loca + 2 * gid, offset / 2);
            }
            if (gid % 2 == 0 && gid < numGlyphs) {
                used[gid] = true;
            }
        }
        assertRefused(ttf, used);
    }
}
