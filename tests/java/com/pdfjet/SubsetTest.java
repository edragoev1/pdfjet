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
    // font with the long ones read Cascadia Mono's .ttf or IBM Plex Math.
    private static final String PLEX = "tests/fonts/IBMPlexSans-Regular.ttf";

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

    // The glyph numbers of the parts of a composite glyph, in their order.
    private static java.util.List<Integer> components(byte[] glyph) {
        java.util.List<Integer> parts = new java.util.ArrayList<Integer>();
        int at = 10;
        while (true) {
            int flags = u16(glyph, at);
            parts.add(u16(glyph, at + 2));
            at += 4;
            at += (flags & 0x0001) != 0 ? 4 : 2;
            if ((flags & 0x0008) != 0) {
                at += 2;
            } else if ((flags & 0x0040) != 0) {
                at += 4;
            } else if ((flags & 0x0080) != 0) {
                at += 8;
            }
            if ((flags & 0x0020) == 0) {
                return parts;
            }
        }
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
        // In Cascadia Mono's .ttf, whose loca has the long offsets, as that of Noto
        // Sans, which this test read before 9.1.7, had: Ä (16) is made of A
        // (1) and the dieresis of capitals (3051), and ặ (229) of a (225),
        // the dot below (3029) and the breve (3020); 4,319 glyphs. Noto Sans
        // had Ä (134) of A (36) and the dieresis (106), and ǅ (913) of three.
        byte[] ttf = fontBytes("tests/fonts/CascadiaMono-Regular.ttf");
        assertEquals(1, u16(ttf, table(ttf, "head") + 50));
        boolean[][] kept = new boolean[1][];
        byte[] subset = Subset.subsetTrueType(ttf, used(16, 229), kept);
        // kept is of the numbers in the font, up to the last glyph kept, 3051.
        assertEquals(3052, kept[0].length);
        Set<Integer> want = new HashSet<Integer>(Arrays.asList(0, 16, 1, 3051, 229, 225, 3029, 3020));
        for (int gid = 0; gid < kept[0].length; gid++) {
            assertEquals(want.contains(gid), kept[0][gid], "glyph " + gid);
        }
        // The glyphs kept, numbered again in their order: 0, 1, 16, 225, 229,
        // 3020, 3029 and 3051 are 0 to 7 in the subset, which has those alone.
        int[] olds = {0, 1, 16, 225, 229, 3020, 3029, 3051};
        assertEquals(olds.length, u16(subset, table(subset, "maxp") + 4));
        assertEquals(olds.length, u16(subset, table(subset, "hhea") + 34));
        assertEquals(4 * olds.length, u32(subset, entry(subset, "hmtx") + 12));
        int metrics = u16(ttf, table(ttf, "hhea") + 34);
        for (int n = 0; n < olds.length; n++) {
            int old = olds[n];
            byte[] whole = glyph(ttf, old);
            byte[] glyph = glyph(subset, n);
            assertTrue(glyph.length - whole.length >= 0 && glyph.length - whole.length < 4, "glyph " + old);
            if (old == 16 || old == 229) {
                // A composite glyph, its parts named by their new numbers:
                // Ä of A (1) and the dieresis (3051), 1 and 7; ặ of a (225),
                // the dot below (3029) and the breve (3020), 3, 6 and 5.
                int[] parts = old == 16 ? new int[] {1, 7} : new int[] {3, 6, 5};
                java.util.List<Integer> found = components(glyph);
                assertEquals(parts.length, found.size(), "glyph " + old);
                for (int part : parts) {
                    assertTrue(found.contains(part), "glyph " + old + ": " + found);
                }
            } else {
                // The glyph as it was, padded to four bytes.
                assertArrayEquals(whole, Arrays.copyOf(glyph, whole.length), "glyph " + old);
            }
            // Its advance and left side bearing as they were
            int advance = u16(ttf, table(ttf, "hmtx") + 4 * Math.min(old, metrics - 1));
            int lsb = old < metrics ? u16(ttf, table(ttf, "hmtx") + 4 * old + 2)
                    : u16(ttf, table(ttf, "hmtx") + 4 * metrics + 2 * (old - metrics));
            assertEquals(advance, u16(subset, table(subset, "hmtx") + 4 * n), "the advance of glyph " + old);
            assertEquals(lsb, u16(subset, table(subset, "hmtx") + 4 * n + 2), "the side bearing of glyph " + old);
        }
        assertTrue(subset.length <= ttf.length / 4, "the subset is " + subset.length + " bytes");
        // The other tables a reader needs as they were; shaping left out, and
        // the cmap and name tables, which a CIDFontType2 does not need.
        for (String name : new String[] {"GPOS", "GSUB", "GDEF", "cmap", "name"}) {
            assertEquals(-1, entry(subset, name), "the subset has the " + name + " table");
        }
        assertEquals(0x00030000, u32(subset, table(subset, "post")));
        // The OS/2 table as it was, and the hhea and maxp tables but for
        // their counts, of the long metrics and of the glyphs.
        for (String name : new String[] {"hhea", "OS/2", "maxp"}) {
            int length = u32(subset, entry(subset, name) + 12);
            byte[] was = Arrays.copyOfRange(ttf, table(ttf, name), table(ttf, name) + length);
            byte[] now = Arrays.copyOfRange(subset, table(subset, name), table(subset, name) + length);
            if (name.equals("maxp")) {
                now[4] = was[4];
                now[5] = was[5];
            } else if (name.equals("hhea")) {
                now[34] = was[34];
                now[35] = was[35];
            }
            assertArrayEquals(was, now, "the " + name + " table changed");
        }
        // The whole font sums to the magic number of its head table.
        assertEquals(0xB1B0AFBAL, Subset.tableChecksum(subset));
    }

    @Test
    void subsetsAFontWithShortOffsets() throws Exception {
        // IBM's .ttf of IBM Plex Sans, whose loca has the short offsets, as
        // IBM Plex Sans Thai's .ttf and Noto Sans Thai's had.
        byte[] ttf = fontBytes(PLEX);
        assertEquals(0, u16(ttf, table(ttf, "head") + 50));
        byte[] subset = Subset.subsetTrueType(ttf, used(5), new boolean[1][]);
        // Glyph 5, the second of the subset, after .notdef
        byte[] whole = glyph(ttf, 5);
        assertEquals(2, u16(subset, table(subset, "maxp") + 4));
        assertArrayEquals(whole, Arrays.copyOf(glyph(subset, 1), whole.length));
    }

    @Test
    void numbersTheGlyphsKeptAgain() throws Exception {
        // IBM Plex Sans has 1,019 glyphs; a subset of glyph 33, A, has two,
        // .notdef and A, a long metric for each, and kept is of the numbers
        // in the font, up to 33. The map of the CIDs to them gives A, CID 33,
        // the glyph 1.
        byte[] ttf = fontBytes(PLEX);
        assertEquals(1019, u16(ttf, table(ttf, "maxp") + 4));
        int metrics = u16(ttf, table(ttf, "hhea") + 34);
        assertTrue(metrics > 33);
        boolean[][] kept = new boolean[1][];
        byte[] subset = Subset.subsetTrueType(ttf, used(33), kept);
        assertEquals(34, kept[0].length);
        assertEquals(2, u16(subset, table(subset, "maxp") + 4));
        assertEquals(2, u16(subset, table(subset, "hhea") + 34));
        assertEquals(8, u32(subset, entry(subset, "hmtx") + 12));
        assertEquals(2 * 3, u32(subset, entry(subset, "loca") + 12));
        // The width and side bearing of A as they were
        assertEquals(u32(ttf, table(ttf, "hmtx") + 4 * 33), u32(subset, table(subset, "hmtx") + 4));
        assertArrayEquals(glyph(ttf, 33), Arrays.copyOf(glyph(subset, 1), glyph(ttf, 33).length));
        byte[] map = FontWriter.cidToGIDMap(kept[0]);
        assertEquals(2 * 34, map.length);
        for (int cid = 0; cid < 34; cid++) {
            int gid = (map[2 * cid] & 0xFF) << 8 | (map[2 * cid + 1] & 0xFF);
            assertEquals(cid == 33 ? 1 : 0, gid, "CID " + cid);
        }
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
            // The subset has the four glyphs kept, numbered again: .notdef,
            // A, Ä and the dieresis, 0 to 3, Ä of 1 and 3; the CIDs are their
            // numbers in the font, which the CIDToGIDMap maps to them.
            assertEquals(4, u16(program, table(program, "maxp") + 4), path);
            for (int gid = 1; gid < 4; gid++) {
                assertTrue(glyph(program, gid).length > 0, path + ": glyph " + gid);
            }
            assertEquals(Arrays.asList(1, 3), components(glyph(program, 2)), path);
            assertTrue(Pattern.compile("/CIDToGIDMap \\d+ 0 R\n").matcher(raw).find(), path);
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
        // A and B, 33 and 34 in IBM Plex Sans, 1 and 2 in the subset, after
        // .notdef; 36 and 37 in Noto Sans.
        assertEquals(3, u16(program, table(program, "maxp") + 4));
        assertTrue(glyph(program, 1).length > 0 && glyph(program, 2).length > 0);
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
        // Glyphs 0 and 33, A, of IBM Plex Sans, whose subset ends at 33; Noto
        // Sans had A at 36 of 4,503.
        byte[] want = new byte[(33 + 1 + 7) / 8];
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
        byte[] ttf = fontBytes("tests/fonts/IBMPlexMath-Regular.ttf");
        putU16(ttf, table(ttf, "head") + 50, 0xFFFF);
        assertRefused(ttf, used(36));
    }

    @Test
    void anOffsetPast2To31OfAGlyphNotKeptIsNotRefused() throws Exception {
        // A glyph whose loca offset is past 2^31 - 1 cannot be read, and is
        // refused only if it is kept, as the other ports do (the review of
        // 9 October 2026). IBM Plex Math has the long offsets of loca, as IBM
        // Plex Sans JP's .ttf, which this test read before 9.1.7, had.
        byte[] ttf = fontBytes("tests/fonts/IBMPlexMath-Regular.ttf");
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
