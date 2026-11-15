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
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertNull;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.junit.jupiter.api.Assertions.fail;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.nio.file.Files;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.regex.Pattern;
import org.junit.jupiter.api.Test;

class CFFSubsetTest {
    static final String PLEX = "tests/fonts/IBMPlexSans-Regular.otf";
    static final String HAN = "tests/fonts/SourceHanSansJP-Regular.otf";

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
        return new Object[] {CFFSubset.emptied(cff, charStrings, null), CFFSubset.emptied(cff, globals, null), top};
    }

    private static boolean[] used(OTF otf, String text) {
        boolean[] used = new boolean[0x10000];
        for (int i = 0; i < text.length(); i++) {
            used[otf.unicodeToGID.get(text.charAt(i))] = true;
        }
        return used;
    }

    // The CIDs the charset of the subset gives its glyphs after .notdef, of
    // formats 0, 1 and 2.
    private static List<Integer> charset(byte[] cff, List<CFFSubset.Entry> top, int glyphs) {
        int at = CFFSubset.entryOf(top, CFFSubset.CHARSET)[0];
        int format = cff[at] & 0xFF;
        at++;
        List<Integer> cids = new ArrayList<Integer>();
        while (cids.size() < glyphs - 1) {
            if (format == 0) {
                cids.add((cff[at] & 0xFF) << 8 | (cff[at + 1] & 0xFF));
                at += 2;
            } else if (format == 1 || format == 2) {
                int first = (cff[at] & 0xFF) << 8 | (cff[at + 1] & 0xFF);
                int left = cff[at + 2] & 0xFF;
                at += 3;
                if (format == 2) {
                    left = left << 8 | (cff[at] & 0xFF);
                    at++;
                }
                for (int cid = first; cid <= first + left; cid++) {
                    cids.add(cid);
                }
            } else {
                fail("charset format " + format);
            }
        }
        return cids;
    }

    @Test
    @SuppressWarnings("unchecked")
    void isACIDFontOfTheGlyphsUsedAlone() throws Exception {
        for (String path : new String[] {PLEX, HAN}) {
            OTF otf = otf(path);
            byte[] cff = cff(otf);
            boolean[] used = used(otf, "Hello 日本語");
            boolean[][] kept = new boolean[1][];
            byte[] subset = CFFSubset.subset(cff, used, kept);
            List<byte[]> whole = (List<byte[]>) parts(cff)[0];
            Object[] parts = parts(subset);
            List<byte[]> glyphs = (List<byte[]>) parts[0];
            List<byte[]> globals = (List<byte[]>) parts[1];
            List<CFFSubset.Entry> top = (List<CFFSubset.Entry>) parts[2];
            assertEquals(whole.size(), kept[0].length, path + ": kept");
            // The glyphs kept, .notdef and those used, alone, in their order
            List<Integer> keptGIDs = new ArrayList<Integer>();
            for (int gid = 0; gid < kept[0].length; gid++) {
                assertEquals(gid == 0 || used[gid], kept[0][gid], path + " glyph " + gid + " kept");
                if (kept[0][gid] && gid > 0) {
                    keptGIDs.add(gid);
                }
            }
            assertEquals(keptGIDs.size() + 1, glyphs.size(), path + ": glyphs");
            // A CIDFont, whose charset gives each glyph its number in the font
            // as its CID, which the content streams draw it by
            assertNotNull(CFFSubset.entryOf(top, CFFSubset.ROS), path + ": the subset is not a CIDFont");
            assertNotNull(CFFSubset.entryOf(top, CFFSubset.FD_ARRAY), path + ": the subset is not a CIDFont");
            assertNull(CFFSubset.entryOf(top, CFFSubset.ENCODING), path + ": the subset has an Encoding");
            assertEquals(keptGIDs, charset(subset, top, glyphs.size()), path + ": the CIDs");
            // No subroutines: the glyphs are written with them in place, and
            // drawn as in the whole font
            // (withItsSubroutinesInPlaceDrawsTheSameOutlines)
            assertEquals(0, globals.size(), path + ": global subroutines");
            assertTrue(subset.length <= cff.length / 10, path + ": " + subset.length + " bytes of " + cff.length);
        }
    }

    @Test
    @SuppressWarnings("unchecked")
    void ofEveryGlyphHasTheIdentityCharset() throws Exception {
        // Source Han Sans JP gives its glyphs CIDs of Adobe-Japan1, not their
        // numbers, and a PDF looks the glyphs of a CID-keyed font up by CID:
        // written whole, each glyph's CID is its number.
        for (String path : new String[] {PLEX, HAN}) {
            byte[] cff = cff(otf(path));
            List<byte[]> whole = (List<byte[]>) parts(cff)[0];
            boolean[][] kept = new boolean[1][];
            byte[] subset = CFFSubset.subset(cff, null, kept);
            Object[] parts = parts(subset);
            List<byte[]> glyphs = (List<byte[]>) parts[0];
            List<CFFSubset.Entry> top = (List<CFFSubset.Entry>) parts[2];
            assertEquals(whole.size(), glyphs.size(), path + ": glyphs");
            assertEquals(whole.size(), kept[0].length, path + ": kept");
            List<Integer> cids = charset(subset, top, glyphs.size());
            for (int i = 0; i < cids.size(); i++) {
                assertEquals(i + 1, (int) cids.get(i), path + ": the CID of glyph " + (i + 1));
            }
            for (int gid = 0; gid < glyphs.size(); gid++) {
                assertArrayEquals(whole.get(gid), glyphs.get(gid), path + " glyph " + gid);
            }
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
        int gid = otf.unicodeToGID.get('Á');
        int at = charStrings.objects[gid];
        assertTrue(charStrings.objects[gid + 1] - at >= 6, "the charstring of Á is too short");
        System.arraycopy(new byte[] {(byte) 139, (byte) 139, (byte) 204, (byte) 247, 86, 14}, 0, cff, at, 6);
        final boolean[] used = used(otf, "Á");
        assertThrows(Subset.NotSubset.class, () -> CFFSubset.subset(cff, used, new boolean[1][]));
    }

    // The CharStrings INDEX of the table.
    private static CFFSubset.Index charStrings(byte[] cff) throws Exception {
        CFFSubset.Index names = CFFSubset.readIndex(cff, cff[2] & 0xFF);
        CFFSubset.Index tops = CFFSubset.readIndex(cff, names.end);
        List<CFFSubset.Entry> top = CFFSubset.readDict(cff, tops.objects[0], tops.objects[1]);
        return CFFSubset.readIndex(cff, CFFSubset.entryOf(top, CFFSubset.CHAR_STRINGS)[0]);
    }

    // Returns the CFF table of a name-keyed font with its Top DICT written
    // again with the entries, before its own, and its offsets moved by as many
    // bytes.
    private static byte[] withTopDict(byte[] cff, CFFSubset.Entry... entries) throws Exception {
        CFFSubset.Index names = CFFSubset.readIndex(cff, cff[2] & 0xFF);
        CFFSubset.Index tops = CFFSubset.readIndex(cff, names.end);
        List<CFFSubset.Entry> top = CFFSubset.readDict(cff, tops.objects[0], tops.objects[1]);
        byte[] written = writtenTop(top, entries, 0);
        int delta = CFFSubset.index(Arrays.asList(written)).length - (tops.end - tops.start);
        CFFSubset.ByteBuffer out = new CFFSubset.ByteBuffer();
        out.add(Arrays.copyOfRange(cff, 0, tops.start));
        out.add(CFFSubset.index(Arrays.asList(writtenTop(top, entries, delta))));
        out.add(Arrays.copyOfRange(cff, tops.end, cff.length));
        return out.toBytes();
    }

    private static byte[] writtenTop(List<CFFSubset.Entry> top, CFFSubset.Entry[] entries, int delta) {
        Map<Integer, Integer> offsets = new HashMap<Integer, Integer>();
        int privateSize = 0;
        for (CFFSubset.Entry entry : top) {
            if (entry.op == CFFSubset.CHARSET || entry.op == CFFSubset.CHAR_STRINGS) {
                offsets.put(entry.op, entry.integers[0] + delta);
            } else if (entry.op == CFFSubset.PRIVATE) {
                privateSize = entry.integers[0];
                offsets.put(entry.op, entry.integers[1] + delta);
            }
        }
        List<CFFSubset.Entry> all = new ArrayList<CFFSubset.Entry>(Arrays.asList(entries));
        all.addAll(top);
        return CFFSubset.writeDict(all, offsets, privateSize);
    }

    // The FontMatrix entries of the Top DICT and of the one Font DICT.
    @SuppressWarnings("unchecked")
    private static String matrices(byte[] cff) throws Exception {
        List<CFFSubset.Entry> top = (List<CFFSubset.Entry>) parts(cff)[2];
        List<byte[]> raws = new ArrayList<byte[]>();
        for (CFFSubset.Entry entry : top) {
            if (entry.op == CFFSubset.FONT_MATRIX) {
                raws.add(entry.raw);
            }
        }
        CFFSubset.Index fdArray = CFFSubset.readIndex(cff, CFFSubset.entryOf(top, CFFSubset.FD_ARRAY)[0]);
        assertEquals(1, fdArray.count(), "not one Font DICT");
        for (CFFSubset.Entry entry : CFFSubset.readDict(cff, fdArray.objects[0], fdArray.objects[1])) {
            if (entry.op == CFFSubset.FONT_MATRIX) {
                raws.add(entry.raw);
            }
        }
        return raws(raws.toArray(new byte[0][]));
    }

    private static String raws(byte[]... raws) {
        StringBuilder sb = new StringBuilder();
        for (byte[] raw : raws) {
            for (byte b : raw) {
                sb.append(b & 0xFF).append(' ');
            }
            sb.append("| ");
        }
        return sb.toString();
    }

    @Test
    @SuppressWarnings("unchecked")
    void ofANameKeyedFontWithAFontMatrixOfItsOwn() throws Exception {
        // As Cascadia Mono's .otf, of 2048 units to the em, has: the matrix
        // stays in the Top DICT, and the Font DICT has the identity matrix,
        // the two concatenated the font's.
        OTF otf = otf(PLEX);
        byte[] cff = cff(otf);
        byte[] matrix = {30, (byte) 0xa0, 0x00, 0x48, (byte) 0x82, (byte) 0x8f};  // .00048828
        CFFSubset.ByteBuffer b = new CFFSubset.ByteBuffer();
        b.add(matrix);
        b.add(139);
        b.add(139);
        b.add(matrix);
        b.add(139);
        b.add(139);
        b.add(12);
        b.add(7);
        byte[] raw = b.toBytes();
        byte[] font = withTopDict(cff, new CFFSubset.Entry(CFFSubset.FONT_MATRIX, raw, null));
        List<byte[]> whole = (List<byte[]>) parts(font)[0];
        String want = raws(raw, new byte[] {(byte) 140, (byte) 139, (byte) 139, (byte) 140, (byte) 139, (byte) 139, 12, 7});
        byte[] subset = null;
        for (boolean[] used : new boolean[][] {used(otf, "Hello"), null}) {
            subset = CFFSubset.subset(font, used, new boolean[1][]);
            List<byte[]> glyphs = (List<byte[]>) parts(subset)[0];
            if (used == null) {
                assertEquals(whole.size(), glyphs.size(), "glyphs");
            }
            assertEquals(want, matrices(subset), "the matrices");
        }
        // A CID-keyed font's matrices are kept as they are: the subset's
        // subset has them both again.
        byte[] again = CFFSubset.subset(subset, used(otf, "He"), new boolean[1][]);
        assertEquals(want, matrices(again), "the matrices of a CID-keyed font");
        // A name-keyed font of no FontMatrix, as IBM Plex Sans, has the
        // identity matrix in its Font DICT too, and the default written in
        // its Top DICT: no reader takes the default of 1000 units to the em
        // twice, or the Font DICT's matrix as the whole one.
        for (boolean[] used : new boolean[][] {used(otf, "Hello"), null}) {
            subset = CFFSubset.subset(cff, used, new boolean[1][]);
            assertEquals(raws(CFFSubset.THOUSANDTH_MATRIX, CFFSubset.IDENTITY_MATRIX), matrices(subset), "the matrices");
        }
    }

    // Returns the outline of each glyph of the CFF table, the points of its
    // path operators in order, drawn by its Type 2 charstring run with its
    // subroutines.
    @SuppressWarnings("unchecked")
    private static List<String> outlines(byte[] cff) throws Exception {
        Object[] parts = parts(cff);
        List<byte[]> charStrings = (List<byte[]>) parts[0];
        List<byte[]> globals = (List<byte[]>) parts[1];
        List<CFFSubset.Entry> top = (List<CFFSubset.Entry>) parts[2];
        List<List<byte[]>> locals = new ArrayList<List<byte[]>>();
        int[] fdOf = null;
        int[] at = CFFSubset.entryOf(top, CFFSubset.FD_ARRAY);
        if (at != null) {
            CFFSubset.Index fdArray = CFFSubset.readIndex(cff, at[0]);
            for (int i = 0; i < fdArray.count(); i++) {
                List<CFFSubset.Entry> fd = CFFSubset.readDict(cff, fdArray.objects[i], fdArray.objects[i + 1]);
                locals.add(locals(cff, CFFSubset.entryOf(fd, CFFSubset.PRIVATE)));
            }
            int fdSelectAt = CFFSubset.entryOf(top, CFFSubset.FD_SELECT)[0];
            int length = CFFSubset.fdSelectLength(cff, fdSelectAt, charStrings.size());
            fdOf = CFFSubset.fdOf(CFFSubset.block(cff, fdSelectAt, length), charStrings.size(), locals.size());
        } else {
            locals.add(locals(cff, CFFSubset.entryOf(top, CFFSubset.PRIVATE)));
        }
        List<String> outlines = new ArrayList<String>();
        for (int gid = 0; gid < charStrings.size(); gid++) {
            int fd = (fdOf != null) ? fdOf[gid] : 0;
            Type2 p = new Type2(globals, locals.get(fd));
            p.run(charStrings.get(gid), 0);
            outlines.add(p.path.toString());
        }
        return outlines;
    }

    // Returns the local subroutines of the Private DICT.
    private static List<byte[]> locals(byte[] cff, int[] p) throws Exception {
        CFFSubset.PrivateDict block = CFFSubset.readPrivate(cff, p[0], p[1]);
        if (block.subrs == null) {
            return null;
        }
        return CFFSubset.emptied(cff, block.subrs, null);
    }

    /**
     * Draws a Type 2 charstring (Technical Note #5177) as the points of its
     * path, apart from the subsetter's code.
     */
    static final class Type2 {
        final List<byte[]> globals;
        final List<byte[]> locals;
        List<Double> stack = new ArrayList<Double>();
        double x;
        double y;
        int stems;
        boolean width;      // Looked for already
        boolean ended;
        final StringBuilder path = new StringBuilder();

        Type2(List<byte[]> globals, List<byte[]> locals) {
            this.globals = globals;
            this.locals = locals;
        }

        void point(String op, double dx, double dy) {
            x += dx;
            y += dy;
            path.append(op).append(' ').append(x).append(' ').append(y).append(' ');
        }

        // Takes the width off the stack, the first time, when it has more
        // operands than even, for even, or than n.
        void args(boolean even, int n) {
            if (!width) {
                width = true;
                if ((even && stack.size() % 2 == 1) || (!even && stack.size() > n)) {
                    stack.remove(0);
                }
            }
        }

        void curve(double dx1, double dy1, double dx2, double dy2, double dx3, double dy3) {
            point("C", dx1, dy1);
            point("", dx2, dy2);
            point("", dx3, dy3);
        }

        void run(byte[] cs, int depth) {
            int i = 0;
            while (i < cs.length && !ended) {
                int b0 = cs[i] & 0xFF;
                if (b0 == 28) {
                    stack.add((double) (short) ((cs[i + 1] & 0xFF) << 8 | (cs[i + 2] & 0xFF)));
                    i += 3;
                    continue;
                } else if (b0 >= 32 && b0 <= 246) {
                    stack.add((double) (b0 - 139));
                    i++;
                    continue;
                } else if (b0 >= 247 && b0 <= 250) {
                    stack.add((double) ((b0 - 247) * 256 + (cs[i + 1] & 0xFF) + 108));
                    i += 2;
                    continue;
                } else if (b0 >= 251 && b0 <= 254) {
                    stack.add((double) (-(b0 - 251) * 256 - (cs[i + 1] & 0xFF) - 108));
                    i += 2;
                    continue;
                } else if (b0 == 255) {
                    stack.add(((cs[i + 1] & 0xFF) << 24 | (cs[i + 2] & 0xFF) << 16 |
                            (cs[i + 3] & 0xFF) << 8 | (cs[i + 4] & 0xFF)) / 65536.0);
                    i += 5;
                    continue;
                }
                double[] s = new double[stack.size()];
                for (int j = 0; j < s.length; j++) {
                    s[j] = stack.get(j);
                }
                i++;
                switch (b0) {
                case 1: case 3: case 18: case 23:   // hstem, vstem, hstemhm, vstemhm
                    args(true, 0);
                    stems += stack.size() / 2;
                    path.append("stem").append(b0).append(' ').append(stack).append(' ');
                    break;
                case 19: case 20:                   // hintmask, cntrmask
                    args(true, 0);
                    stems += stack.size() / 2;
                    int n = (stems + 7) / 8;
                    path.append("mask").append(b0).append(' ').append(stack).append(' ');
                    for (int j = 0; j < n; j++) {
                        path.append(String.format("%02x", cs[i + j] & 0xFF));
                    }
                    path.append(' ');
                    i += n;
                    break;
                case 21:                            // rmoveto
                    args(false, 2);
                    point("M", stack.get(0), stack.get(1));
                    break;
                case 22:                            // hmoveto
                    args(false, 1);
                    point("M", stack.get(0), 0);
                    break;
                case 4:                             // vmoveto
                    args(false, 1);
                    point("M", 0, stack.get(0));
                    break;
                case 5:                             // rlineto
                    for (int j = 0; j + 1 < s.length; j += 2) {
                        point("L", s[j], s[j + 1]);
                    }
                    break;
                case 6: case 7: {                   // hlineto, vlineto
                    boolean h = b0 == 6;
                    for (int j = 0; j < s.length; j++, h = !h) {
                        if (h) {
                            point("L", s[j], 0);
                        } else {
                            point("L", 0, s[j]);
                        }
                    }
                    break;
                }
                case 8:                             // rrcurveto
                    for (int j = 0; j + 5 < s.length; j += 6) {
                        curve(s[j], s[j + 1], s[j + 2], s[j + 3], s[j + 4], s[j + 5]);
                    }
                    break;
                case 24: {                          // rcurveline
                    int j = 0;
                    for (; j + 5 < s.length - 2; j += 6) {
                        curve(s[j], s[j + 1], s[j + 2], s[j + 3], s[j + 4], s[j + 5]);
                    }
                    point("L", s[j], s[j + 1]);
                    break;
                }
                case 25: {                          // rlinecurve
                    int j = 0;
                    for (; j + 1 < s.length - 6; j += 2) {
                        point("L", s[j], s[j + 1]);
                    }
                    curve(s[j], s[j + 1], s[j + 2], s[j + 3], s[j + 4], s[j + 5]);
                    break;
                }
                case 26: case 27: {                 // vvcurveto, hhcurveto
                    int j = 0;
                    double d = 0.0;
                    if (s.length % 4 == 1) {
                        d = s[0];
                        j = 1;
                    }
                    for (; j + 3 < s.length; j += 4) {
                        if (b0 == 26) {
                            curve(d, s[j], s[j + 1], s[j + 2], 0, s[j + 3]);
                        } else {
                            curve(s[j], d, s[j + 1], s[j + 2], s[j + 3], 0);
                        }
                        d = 0;
                    }
                    break;
                }
                case 30: case 31: {                 // vhcurveto, hvcurveto
                    boolean h = b0 == 31;
                    for (int j = 0; j + 3 < s.length; j += 4, h = !h) {
                        double last = 0.0;
                        if (s.length - j == 5) {
                            last = s[j + 4];
                        }
                        if (h) {
                            curve(s[j], 0, s[j + 1], s[j + 2], last, s[j + 3]);
                        } else {
                            curve(0, s[j], s[j + 1], s[j + 2], s[j + 3], last);
                        }
                    }
                    break;
                }
                case 10: case 29: {                 // callsubr, callgsubr
                    List<byte[]> subrs = (b0 == 29) ? globals : locals;
                    int number = (int) s[s.length - 1] + CFFSubset.bias(subrs.size());
                    stack.remove(stack.size() - 1);
                    run(subrs.get(number), depth + 1);
                    continue;
                }
                case 11:                            // return
                    return;
                case 14:                            // endchar
                    args(false, 0);
                    path.append("Z");
                    ended = true;
                    return;
                case 12: {
                    int b1 = cs[i] & 0xFF;
                    i++;
                    if (b1 == 35) {                 // flex
                        curve(s[0], s[1], s[2], s[3], s[4], s[5]);
                        curve(s[6], s[7], s[8], s[9], s[10], s[11]);
                    } else if (b1 == 34) {          // hflex
                        curve(s[0], 0, s[1], s[2], s[3], 0);
                        curve(s[4], 0, s[5], -s[2], s[6], 0);
                    } else if (b1 == 36) {          // hflex1
                        curve(s[0], s[1], s[2], s[3], s[4], 0);
                        curve(s[5], 0, s[6], s[7], s[8], -(s[1] + s[3] + s[7]));
                    } else if (b1 == 37) {          // flex1
                        double dx = Math.abs(s[0] + s[2] + s[4] + s[6] + s[8]);
                        double dy = Math.abs(s[1] + s[3] + s[5] + s[7] + s[9]);
                        curve(s[0], s[1], s[2], s[3], s[4], s[5]);
                        if (dx > dy) {
                            curve(s[6], s[7], s[8], s[9], s[10], -(s[1] + s[3] + s[5] + s[7] + s[9]));
                        } else {
                            curve(s[6], s[7], s[8], s[9], -(s[0] + s[2] + s[4] + s[6] + s[8]), s[10]);
                        }
                    } else {
                        path.append("op12.").append(b1).append(' ');
                    }
                    break;
                }
                default:
                    path.append("op").append(b0).append(' ');
                    break;
                }
                stack.clear();
            }
        }
    }

    @Test
    @SuppressWarnings("unchecked")
    void withItsSubroutinesInPlaceDrawsTheSameOutlines() throws Exception {
        for (String path : new String[] {PLEX, "tests/fonts/IBMPlexSans-Italic.otf", HAN}) {
            OTF otf = otf(path);
            byte[] cff = cff(otf);
            boolean[] used = used(otf, "Hello, Wörld! Ýou ﬁnd 日本語の文字 ½ € @ & ŞŢ");
            boolean[] every = new boolean[otf.numGlyphs];
            for (int gid = 0; gid < otf.numGlyphs; gid += 3) {
                every[gid] = true;
            }
            List<String> want = outlines(cff);
            for (boolean[] u : new boolean[][] {used, every}) {
                boolean[][] kept = new boolean[1][];
                byte[] subset = CFFSubset.subset(cff, u, kept);
                // No subroutines, global or local
                Object[] parts = parts(subset);
                List<byte[]> globals = (List<byte[]>) parts[1];
                List<CFFSubset.Entry> top = (List<CFFSubset.Entry>) parts[2];
                assertEquals(0, globals.size(), path + ": global subroutines");
                CFFSubset.Index fdArray = CFFSubset.readIndex(subset, CFFSubset.entryOf(top, CFFSubset.FD_ARRAY)[0]);
                for (int i = 0; i < fdArray.count(); i++) {
                    List<CFFSubset.Entry> fd = CFFSubset.readDict(subset, fdArray.objects[i], fdArray.objects[i + 1]);
                    int[] p = CFFSubset.entryOf(fd, CFFSubset.PRIVATE);
                    assertNull(CFFSubset.readPrivate(subset, p[0], p[1]).subrs,
                            path + ": local subroutines in Font DICT " + i);
                }
                // Each glyph drawn as in the whole font, its hints and masks too
                List<String> got = outlines(subset);
                int i = 0;
                for (int gid = 0; gid < kept[0].length; gid++) {
                    if (!kept[0][gid]) {
                        continue;
                    }
                    assertEquals(want.get(gid), got.get(i), path + ": glyph " + gid + " drawn otherwise");
                    String g = got.get(i);
                    assertFalse(!g.endsWith("Z") || !g.contains("M ") && gid > 0 && !g.equals("Z"),
                            path + ": glyph " + gid + ": " + g);
                    i++;
                }
                assertEquals(i, got.size(), path + ": glyphs");
            }
        }
    }

    @Test
    @SuppressWarnings("unchecked")
    void ofAGlyphThatComputesANumberKeepsItsSubroutines() throws Exception {
        // "0 0 add 0 endchar": a number computed, by add, which the
        // subroutines are not written in place for. The subset keeps the
        // glyphs as they were and the subroutines they call, the others
        // emptied to no bytes, keeping their numbers.
        OTF otf = otf(PLEX);
        byte[] cff = cff(otf);
        int gid = otf.unicodeToGID.get('Á');
        System.arraycopy(new byte[] {(byte) 139, (byte) 139, 12, 10, (byte) 139, 14}, 0,
                cff, charStrings(cff).objects[gid], 6);
        boolean[] used = used(otf, "Hello AÁ");
        boolean[][] kept = new boolean[1][];
        byte[] subset = CFFSubset.subset(cff, used, kept);
        List<byte[]> glyphs = (List<byte[]>) parts(subset)[0];
        List<byte[]> globals = (List<byte[]>) parts(subset)[1];
        List<byte[]> whole = (List<byte[]>) parts(cff)[0];
        List<byte[]> wholeGlobals = (List<byte[]>) parts(cff)[1];
        int i = 0;
        for (int g = 0; g < kept[0].length; g++) {
            if (kept[0][g]) {
                assertArrayEquals(whole.get(g), glyphs.get(i), "glyph " + g + " is not as it was");
                i++;
            }
        }
        assertEquals(wholeGlobals.size(), globals.size(), "global subroutines");
        int emptied = 0;
        for (int j = 0; j < globals.size(); j++) {
            if (globals.get(j).length == 0 && wholeGlobals.get(j).length > 0) {
                emptied++;
            } else {
                assertArrayEquals(wholeGlobals.get(j), globals.get(j), "global subroutine " + j + " changed");
            }
        }
        assertTrue(emptied > 0 && emptied < globals.size(), emptied + " of " + globals.size() + " global subroutines emptied");
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
