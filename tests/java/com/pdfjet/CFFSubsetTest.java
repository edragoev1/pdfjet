/*
 * CFFSubsetTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertArrayEquals;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.nio.file.Files;
import java.util.Arrays;
import java.util.List;
import java.util.regex.Pattern;
import org.junit.jupiter.api.Test;

class CFFSubsetTest {
    static final String PLEX = "fonts/IBMPlexSans/IBMPlexSans-Regular.otf";
    static final String HAN = "fonts/Test/SourceHanSansJP-Regular.otf";

    private static OTF otf(String path) throws Exception {
        return new OTF(new ByteArrayInputStream(Files.readAllBytes(TestSupport.file(path).toPath())));
    }

    private static byte[] cff(OTF otf) {
        return Arrays.copyOfRange(otf.buf, otf.cffOff, otf.cffOff + otf.cffLen);
    }

    // The charstrings, the global subroutines and the Top DICT of a CFF table.
    private static Object[] parts(byte[] cff) throws Exception {
        CFFSubset.Index names = CFFSubset.readIndex(cff, cff[2] & 0xFF);
        CFFSubset.Index tops = CFFSubset.readIndex(cff, names.end);
        CFFSubset.Index strings = CFFSubset.readIndex(cff, tops.end);
        CFFSubset.Index globals = CFFSubset.readIndex(cff, strings.end);
        List<CFFSubset.Entry> top = CFFSubset.readDict(cff, tops.objects[0], tops.objects[1]);
        CFFSubset.Index charStrings = CFFSubset.readIndex(cff, CFFSubset.entryOf(top, CFFSubset.CHAR_STRINGS)[0]);
        return new Object[] {CFFSubset.emptied(cff, charStrings, null, 0), CFFSubset.emptied(cff, globals, null, 0), top};
    }

    private static boolean[] used(OTF otf, String text) {
        boolean[] used = new boolean[0x10000];
        for (int i = 0; i < text.length(); i++) {
            used[otf.unicodeToGID[text.charAt(i)]] = true;
        }
        return used;
    }

    @Test
    @SuppressWarnings("unchecked")
    void keepsTheCharstringsUsedAndEmptiesTheRest() throws Exception {
        for (String path : new String[] {PLEX, HAN}) {
            OTF otf = otf(path);
            byte[] cff = cff(otf);
            boolean[] used = used(otf, "Hello 日本語");
            boolean[][] kept = new boolean[1][];
            byte[] subset = CFFSubset.subset(cff, used, kept);
            List<byte[]> whole = (List<byte[]>) parts(cff)[0];
            List<byte[]> wholeGlobals = (List<byte[]>) parts(cff)[1];
            List<byte[]> glyphs = (List<byte[]>) parts(subset)[0];
            List<byte[]> globals = (List<byte[]>) parts(subset)[1];
            assertEquals(whole.size(), glyphs.size(), path);
            for (int gid = 0; gid < glyphs.size(); gid++) {
                assertEquals(gid == 0 || used[gid], kept[0][gid], path + " glyph " + gid);
                assertArrayEquals(kept[0][gid] ? whole.get(gid) : new byte[] {14}, glyphs.get(gid), path + " glyph " + gid);
            }
            int emptied = 0;
            for (int i = 0; i < globals.size(); i++) {
                if (Arrays.equals(globals.get(i), new byte[] {11}) && !Arrays.equals(wholeGlobals.get(i), new byte[] {11})) {
                    emptied++;
                } else {
                    assertArrayEquals(wholeGlobals.get(i), globals.get(i), path + " global subroutine " + i);
                }
            }
            assertTrue(emptied > 0, path);
            assertTrue(subset.length <= cff.length / 3, path + ": " + subset.length + " bytes");
        }
    }

    @Test
    @SuppressWarnings("unchecked")
    void aCIDKeyedFontHasTheIdentityCharset() throws Exception {
        // Source Han Sans JP gives its glyphs CIDs of Adobe-Japan1, not their
        // numbers, and a PDF looks the glyphs of a CID-keyed font up by CID.
        byte[] cff = cff(otf(HAN));
        for (boolean[] used : new boolean[][] {null, new boolean[0x10000]}) {
            byte[] subset = CFFSubset.subset(cff, used, new boolean[1][]);
            List<byte[]> glyphs = (List<byte[]>) parts(subset)[0];
            int at = CFFSubset.entryOf((List<CFFSubset.Entry>) parts(subset)[2], CFFSubset.CHARSET)[0];
            int n = glyphs.size() - 2;
            assertArrayEquals(new byte[] {2, 0, 1, (byte) (n >> 8), (byte) n}, Arrays.copyOfRange(subset, at, at + 5));
        }
        List<byte[]> whole = (List<byte[]>) parts(cff)[0];
        List<byte[]> glyphs = (List<byte[]>) parts(CFFSubset.subset(cff, null, new boolean[1][]))[0];
        for (int gid = 0; gid < glyphs.size(); gid++) {
            assertArrayEquals(whole.get(gid), glyphs.get(gid), "glyph " + gid);
        }
    }

    private static String doc(Compliance compliance, String path, boolean subset, String text) throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, compliance).setTitle("Test");
        Font font = new Font(pdf, TestSupport.file(path).getPath());
        font.setSubset(subset);
        new TextLine(font, text).setLocation(50f, 50f).drawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.complete();
        return TestSupport.latin1(bos.toByteArray());
    }

    private static final Pattern TAG = Pattern.compile("/FontName /[A-Z]{6}\\+");

    @Test
    void aFontWithCFFOutlinesIsEmbeddedAsASubset() throws Exception {
        for (String path : new String[] {PLEX, HAN}) {
            String raw = doc(Compliance.PDF_A_1B, path, true, "Hello 日本語");
            assertTrue(TAG.matcher(raw).find(), path);
            assertTrue(raw.contains("/Subtype /CIDFontType0C\n") && raw.contains("/FontFile3 "), path);
            assertFalse(raw.contains("/Length1 "), path);
            assertTrue(raw.contains("/CIDSet "), path);
        }
        String raw = doc(Compliance.PDF_1_7, PLEX, false, "Hello");
        assertFalse(TAG.matcher(raw).find());
        assertTrue(raw.contains("/FontFile3 "));
    }

    @Test
    void aFontWhoseLicenseForbidsSubsettingIsEmbeddedWhole() throws Exception {
        byte[] data = Files.readAllBytes(TestSupport.file(PLEX).toPath());
        OTF otf = new OTF(new ByteArrayInputStream(data));
        assertEquals(0, otf.fsType);
        // The fsType is at offset 8 of the OS/2 table.
        int tables = (data[4] & 0xFF) << 8 | (data[5] & 0xFF);
        for (int i = 0; i < tables; i++) {
            int entry = 12 + 16 * i;
            if (new String(data, entry, 4, "ISO-8859-1").equals("OS/2")) {
                int at = (data[entry + 8] & 0xFF) << 24 | (data[entry + 9] & 0xFF) << 16 |
                        (data[entry + 10] & 0xFF) << 8 | (data[entry + 11] & 0xFF);
                data[at + 8] = 0x01;
                data[at + 9] = 0x00;
            }
        }
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = new Font(pdf, new ByteArrayInputStream(data));
        new TextLine(font, "Hello").setLocation(50f, 50f).drawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.complete();
        assertFalse(TAG.matcher(TestSupport.latin1(bos.toByteArray())).find());
    }

    @Test
    void anAccentedLetterDrawnWithSeacIsRefused() throws Exception {
        // Á's charstring made "0 0 65 194 endchar", seac: the A and the acute
        // it is drawn from were emptied, and it drew blank (the review of
        // 9 October 2026). The font is embedded whole.
        OTF otf = otf(PLEX);
        final byte[] cff = cff(otf);
        CFFSubset.Index names = CFFSubset.readIndex(cff, cff[2] & 0xFF);
        CFFSubset.Index tops = CFFSubset.readIndex(cff, names.end);
        List<CFFSubset.Entry> top = CFFSubset.readDict(cff, tops.objects[0], tops.objects[1]);
        CFFSubset.Index charStrings = CFFSubset.readIndex(cff, CFFSubset.entryOf(top, CFFSubset.CHAR_STRINGS)[0]);
        int gid = otf.unicodeToGID['Á'];
        int at = charStrings.objects[gid];
        assertTrue(charStrings.objects[gid + 1] - at >= 6, "the charstring of Á is too short");
        System.arraycopy(new byte[] {(byte) 139, (byte) 139, (byte) 204, (byte) 247, 86, 14}, 0, cff, at, 6);
        final boolean[] used = used(otf, "Á");
        assertThrows(Subset.NotSubset.class, () -> CFFSubset.subset(cff, used, new boolean[1][]));
    }

    @Test
    void anOffsetNearIntegerMaxValueIsRefused() throws Exception {
        // A CFF whose CharStrings are at 0x7FFFFFFF: at + 2 passed
        // Integer.MAX_VALUE, and the INDEX was read past the end of the table
        // (the review of 9 October 2026). It is not subset, and so embedded whole.
        final byte[] cff = {
            1, 0, 4, 1,                                 // The header
            0, 1, 1, 1, 2, 'A',                         // Name INDEX
            0, 1, 1, 1, 7, 29, 0x7F, (byte) 0xFF, (byte) 0xFF, (byte) 0xFF, 17,  // Top DICT INDEX
            0, 0,                                       // String INDEX
            0, 0,                                       // Global Subr INDEX
        };
        assertThrows(Subset.NotSubset.class, () -> CFFSubset.subset(cff, new boolean[0x10000], new boolean[1][]));
        assertThrows(Subset.NotSubset.class, () -> CFFSubset.subset(cff, null, new boolean[1][]));
    }
}
