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
import java.nio.file.Files;
import java.util.Arrays;
import java.util.HashSet;
import java.util.Set;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import java.util.zip.Inflater;
import org.junit.jupiter.api.Test;

class SubsetTest {
    private static byte[] fontBytes(String path) throws Exception {
        return Files.readAllBytes(TestSupport.file(path).toPath());
    }

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
        // In Noto Sans, Ä (134) is made of A (36) and the dieresis (106), and
        // ǅ (913) of D (39), z (93) and the caron (331).
        byte[] ttf = fontBytes("fonts/NotoSans/NotoSans-Regular.ttf");
        boolean[][] kept = new boolean[1][];
        byte[] subset = Subset.subsetTrueType(ttf, used(134, 913), kept);
        assertEquals(4503, kept[0].length);
        Set<Integer> want = new HashSet<Integer>(Arrays.asList(0, 134, 36, 106, 913, 39, 93, 331));
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
        // The other tables a reader needs as they were; shaping left out.
        for (String name : new String[] {"GPOS", "GSUB", "GDEF"}) {
            assertEquals(-1, entry(subset, name), "the subset has the " + name + " table");
        }
        assertEquals(0x00030000, u32(subset, table(subset, "post")));
        for (String name : new String[] {"cmap", "hmtx", "hhea", "name", "OS/2", "maxp"}) {
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
        byte[] ttf = fontBytes("fonts/NotoSansThai/NotoSansThai-Regular.ttf");
        assertEquals(0, u16(ttf, table(ttf, "head") + 50));
        byte[] subset = Subset.subsetTrueType(ttf, used(5), new boolean[1][]);
        byte[] whole = glyph(ttf, 5);
        assertArrayEquals(whole, Arrays.copyOf(glyph(subset, 5), whole.length));
        assertEquals(0, glyph(subset, 6).length);
    }

    @Test
    void isRefusedByAFontWhoseLicenseForbidsIt() throws Exception {
        byte[] ttf = fontBytes("fonts/NotoSans/NotoSans-Regular.ttf");
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

    private static final Pattern TAGGED = Pattern.compile("/BaseFont /([A-Z]{6}\\+NotoSans-Regular)\n");

    @Test
    void aTrueTypeFontIsEmbeddedAsASubsetUnderATaggedName() throws Exception {
        for (String path : new String[] {"fonts/NotoSans/NotoSans-Regular.ttf", "fonts/NotoSans/NotoSans-Regular.ttf.stream"}) {
            String raw = doc(Compliance.PDF_1_7, path, true, "Ä");
            Matcher m = TAGGED.matcher(raw);
            assertTrue(m.find(), path);
            assertTrue(raw.contains("/FontName /" + m.group(1) + "\n"), path);
            assertEquals(2, count(TAGGED, raw), path);
            byte[] program = program(raw);
            assertTrue(glyph(program, 134).length > 0 && glyph(program, 36).length > 0, path);
            assertEquals(0, glyph(program, 37).length, path);
            // The widths of the glyphs kept: .notdef, A, the dieresis and Ä.
            assertTrue(Pattern.compile("/W \\[\n0\\[\\d+ \\]\n36\\[\\d+ \\]\n106\\[\\d+ \\]\n134\\[\\d+ \\]\\]\n")
                    .matcher(raw).find(), path + ": the widths");
            assertFalse(raw.contains("/CIDSet"), path);
        }
    }

    @Test
    void aFontSetToStayWholeIsEmbeddedWhole() throws Exception {
        byte[] ttf = fontBytes("fonts/NotoSans/NotoSans-Regular.ttf");
        for (String path : new String[] {"fonts/NotoSans/NotoSans-Regular.ttf", "fonts/NotoSans/NotoSans-Regular.ttf.stream"}) {
            String raw = doc(Compliance.PDF_1_7, path, false, "Ä");
            assertTrue(raw.contains("/BaseFont /NotoSans-Regular\n") && !raw.contains("+NotoSans"), path);
            assertArrayEquals(ttf, program(raw), path);
        }
    }

    @Test
    void twoFontsOfOneFileShareOneSubset() throws Exception {
        String raw = doc(Compliance.PDF_1_7, "fonts/NotoSans/NotoSans-Regular.ttf", true, "A", "B");
        assertEquals(1, count(Pattern.compile("/Length1 "), raw));
        byte[] program = program(raw);
        assertTrue(glyph(program, 36).length > 0 && glyph(program, 37).length > 0);
        // Two Type0 fonts and their CID font.
        assertEquals(3, count(TAGGED, raw));
    }

    @Test
    void aPDFA1HasTheCIDSetOfTheGlyphsKept() throws Exception {
        String raw = doc(Compliance.PDF_A_1B, "fonts/NotoSans/NotoSans-Regular.ttf", true, "A");
        Matcher m = Pattern.compile("/CIDSet (\\d+) 0 R\n").matcher(raw);
        assertTrue(m.find(), "no CIDSet");
        int at = raw.indexOf("\n" + m.group(1) + " 0 obj\n");
        assertTrue(at != -1);
        Matcher s = Pattern.compile("/Length (\\d+)\n>>\nstream\n").matcher(raw);
        assertTrue(s.find(at));
        byte[] bits = inflate(raw, s.end(), Integer.parseInt(s.group(1)));
        // Glyphs 0 and 36 of 4503.
        byte[] want = new byte[(4503 + 7) / 8];
        want[0] = (byte) 0x80;
        want[36 / 8] |= (byte) (0x80 >> (36 % 8));
        assertArrayEquals(want, bits);
        assertNotNull(bits);
    }
}
