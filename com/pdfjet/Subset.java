/*
 * Subset.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import com.pdfjet.encryption.*;
import java.util.*;

/**
 * The subsets of the embedded fonts, .ttf and .otf files, embedded at
 * complete() with the outlines of the glyphs the document did not draw
 * emptied. The glyph numbers stay, so the widths, the character map and the
 * ToUnicode map are those of the whole font.
 */
class Subset {
    /**
     * The font program of a TrueType font, or the CFF table of an OpenType
     * font with CFF outlines, kept until complete(). Fonts read from one file
     * share one program, and the glyphs any of them drew.
     */
    static final class Program {
        byte[] font;        // The TrueType font, or the CFF table
        boolean cff;
        boolean forbidden;  // The fsType of a CFF font's OS/2 table forbids subsetting
        // The glyphs drawn, by glyph number, written in four hexadecimal digits.
        final boolean[] used = new boolean[0x10000];
        boolean whole;      // Set by setSubset(false): the font is embedded whole

        Program() {
            used[0] = true; // .notdef, drawn for a character the font lacks
        }
    }

    /** The font cannot be subset, and is embedded whole. */
    static final class NotSubset extends Exception {
        private static final long serialVersionUID = 1L;

        NotSubset() {
            super("the font cannot be subset", null, false, false);
        }
    }

    // The tables a subset keeps: those a PDF reader draws a TrueType font with
    // (ISO 32000-1, 9.9), and the cmap, OS/2, name and post tables, which some
    // readers and checkers look at. The tables of shaping and of vertical text,
    // which PDFjet reads from the font and the PDF does not need, are left
    // out, and so is the DSIG table, the signature of the whole font.
    private static final Set<String> SUBSET_TABLES = new HashSet<String>(Arrays.asList(
            "head", "hhea", "hmtx", "maxp", "loca", "glyf",
            "cvt ", "fpgm", "prep", "gasp",
            "cmap", "OS/2", "name", "post"));

    /**
     * Gives the font the program of a font the PDF already has from the same
     * file, if any, or else a new one of the font given.
     */
    static void share(PDF pdf, Font font, byte[] program, boolean cff, boolean forbidden) {
        for (Font f : pdf.fonts) {
            if (f.program != null && f.name.equals(font.name) && f.checksum == font.checksum) {
                font.program = f.program;
                return;
            }
        }
        font.program = new Program();
        font.program.font = program;
        font.program.cff = cff;
        font.program.forbidden = forbidden;
    }

    /**
     * Writes the TrueType fonts at complete(), when every glyph they draw is
     * known: each font program, its descriptor, its CID font and its ToUnicode
     * map once, and the Type0 font of each Font under the number its pages
     * refer to.
     */
    static void addTrueTypeFonts(PDF pdf) throws Exception {
        for (Font font : pdf.fonts) {
            if (font.program == null) {
                continue;
            }
            boolean[] kept = embedProgram(pdf, font);
            addCIDSetObject(pdf, font, kept);
            FontWriter.addFontDescriptorObject(pdf, font, font.baseFont);
            FontWriter.addCIDFontDictionaryObject(pdf, font, font.baseFont, kept);
            FontWriter.addToUnicodeCMapObject(pdf, font, kept);

            pdf.newObj(font.objNumber);
            pdf.append("<<\n");
            pdf.append("/Type /Font\n");
            pdf.append("/Subtype /Type0\n");
            pdf.append("/BaseFont /");
            pdf.append(font.baseFont);
            pdf.append('\n');
            pdf.append("/Encoding /Identity-H\n");
            pdf.append("/DescendantFonts [");
            pdf.append(font.cidFontDictObjNumber);
            pdf.append(" 0 R]\n");
            pdf.append("/ToUnicode ");
            pdf.append(font.toUnicodeCMapObjNumber);
            pdf.append(" 0 R\n");
            pdf.append(">>\n");
            pdf.endObj();
        }
        // The programs are no longer needed.
        for (Font font : pdf.fonts) {
            font.program = null;
        }
    }

    /**
     * Writes the font program, a subset unless it is to be whole, sets the
     * name of the font, with the tag of a subset, and returns the glyphs the
     * subset keeps, or null for a whole font. A font of the same program
     * embedded before is not written again: its name and object numbers are
     * used.
     */
    private static boolean[] embedProgram(PDF pdf, Font font) throws Exception {
        for (Font f : pdf.fonts) {
            if (f == font) {
                break;
            }
            if (f.fileObjNumber != 0 && f.name.equals(font.name) && f.checksum == font.checksum) {
                font.fileObjNumber = f.fileObjNumber;
                font.cidSetObjNumber = f.cidSetObjNumber;
                font.baseFont = f.baseFont;
                return f.kept;
            }
        }

        Program program = font.program;
        byte[] data = embeddedProgram(font);
        byte[] compressed = Compressor.deflate(data);
        int length = data.length;

        int metadataObjNumber = pdf.addMetadataObject(font.info, true);
        if (pdf.encryption != null) {
            compressed = AES256.encrypt(compressed, pdf.encryption.getKey());
        }
        pdf.newObj();
        pdf.append("<<\n");
        if (program.cff) {
            pdf.append("/Subtype /CIDFontType0C\n");
        }
        pdf.append("/Filter /FlateDecode\n");
        if (!program.cff) {
            pdf.append("/Length1 ");
            pdf.append(length);
            pdf.append('\n');
        }
        if (metadataObjNumber != -1) {
            pdf.append("/Metadata ");
            pdf.append(metadataObjNumber);
            pdf.append(" 0 R\n");
        }
        pdf.append("/Length ");
        pdf.append(compressed.length);
        pdf.append('\n');
        pdf.append(">>\n");
        pdf.append("stream\n");
        pdf.append(compressed);
        pdf.append("\nendstream\n");
        pdf.endObj();
        font.fileObjNumber = pdf.getObjNumber();
        return font.kept;
    }

    /**
     * Returns the font program to embed, a subset unless it is to be whole,
     * and sets the name of the font, with the tag of a subset, and the glyphs
     * the subset keeps, null for a whole font.
     */
    static byte[] embeddedProgram(Font font) throws Exception {
        Program program = font.program;
        byte[] data = program.font;
        if (!program.whole && !program.forbidden) {
            try {
                boolean[][] kept = new boolean[1][];
                byte[] subset = program.cff ?
                        CFFSubset.subset(data, program.used, kept) : subsetTrueType(data, program.used, kept);
                font.kept = kept[0];
                font.baseFont = subsetTag(font.checksum, kept[0]) + "+" + font.name;
                return subset;
            } catch (NotSubset e) {
                // Embedded whole
            }
        }
        if (program.cff) {
            // Whole, written again for the identity charset of a CID-keyed
            // font, or as it is when it cannot be.
            try {
                data = CFFSubset.subset(data, null, new boolean[1][]);
            } catch (NotSubset e) {
                // As it is
            }
        }
        font.kept = null;
        font.baseFont = font.name;
        return data;
    }

    /**
     * Writes, for PDF/A-1, which asks it of a subset, the CIDSet of the glyphs
     * the subset keeps: a bit for each glyph number, the first in the high bit
     * of the first byte.
     */
    private static void addCIDSetObject(PDF pdf, Font font, boolean[] kept) throws Exception {
        if (kept == null || font.cidSetObjNumber != 0 ||
                (pdf.compliance != Compliance.PDF_A_1A && pdf.compliance != Compliance.PDF_A_1B)) {
            return;
        }
        byte[] bits = new byte[(kept.length + 7) / 8];
        for (int gid = 0; gid < kept.length; gid++) {
            if (kept[gid]) {
                bits[gid / 8] |= (byte) (0x80 >> (gid % 8));
            }
        }
        FontWriter.addCompressedStream(pdf, bits);
        font.cidSetObjNumber = pdf.getObjNumber();
    }

    /**
     * Returns the six capitals that begin the name of a subset, from the font
     * and the glyphs it keeps, so that another subset of the font has another
     * tag, and the same document the same one, in the four ports.
     */
    static String subsetTag(long checksum, boolean[] kept) {
        long hash = checksum;
        for (int gid = 0; gid < kept.length; gid++) {
            if (kept[gid]) {
                hash = Font.fold(hash, gid);
            }
        }
        StringBuilder tag = new StringBuilder(6);
        for (int i = 0; i < 6; i++) {
            tag.append((char) ('A' + Long.remainderUnsigned(hash, 26)));
            hash = Long.divideUnsigned(hash, 26);
        }
        return tag.toString();
    }

    private static int u16(byte[] ttf, int at) throws NotSubset {
        if (at < 0 || at + 2 > ttf.length) {
            throw new NotSubset();
        }
        return (ttf[at] & 0xFF) << 8 | (ttf[at + 1] & 0xFF);
    }

    private static int u32(byte[] ttf, int at) throws NotSubset {
        if (at < 0 || at + 4 > ttf.length) {
            throw new NotSubset();
        }
        long value = (ttf[at] & 0xFFL) << 24 | (ttf[at + 1] & 0xFF) << 16 |
                (ttf[at + 2] & 0xFF) << 8 | (ttf[at + 3] & 0xFF);
        if (value > Integer.MAX_VALUE) {
            throw new NotSubset();
        }
        return (int) value;
    }

    /** A table of the font: its tag, offset and length. */
    private static final class Table {
        final String tag;
        final int offset;
        final int length;

        Table(String tag, int offset, int length) {
            this.tag = tag;
            this.offset = offset;
            this.length = length;
        }
    }

    /**
     * Returns the TrueType font with the outlines of the glyphs not used
     * emptied, and gives in kept[0] the glyphs kept: those used, glyph 0 and
     * the parts of the composite glyphs kept. Every glyph keeps its number, so
     * the tables kept, SUBSET_TABLES, are copied as they are, but for the glyf
     * and loca tables made again, the head table's checksum adjustment, and
     * the post table without the names of the glyphs. A font that forbids
     * subsetting in the fsType of its OS/2 table, or whose tables cannot be
     * read, throws NotSubset, and is embedded whole.
     */
    static byte[] subsetTrueType(byte[] ttf, boolean[] used, boolean[][] kept) throws NotSubset {
        int numTables = u16(ttf, 4);
        List<Table> tables = new ArrayList<Table>(numTables);
        Table head = null, loca = null, glyf = null, maxp = null, os2 = null;
        for (int i = 0; i < numTables; i++) {
            int entry = 12 + 16 * i;
            int offset = u32(ttf, entry + 8);
            int length = u32(ttf, entry + 12);
            if ((long) offset + length > ttf.length) {
                throw new NotSubset();
            }
            Table table = new Table(
                    new String(ttf, entry, 4, java.nio.charset.StandardCharsets.ISO_8859_1), offset, length);
            tables.add(table);
            switch (table.tag) {
                case "head": head = table; break;
                case "loca": loca = table; break;
                case "glyf": glyf = table; break;
                case "maxp": maxp = table; break;
                case "OS/2": os2 = table; break;
                default: break;
            }
        }
        if (head == null || maxp == null || loca == null || glyf == null ||
                head.length < 54 || maxp.length < 6 || loca.length == 0 || glyf.length == 0) {
            throw new NotSubset();
        }
        if (os2 != null && os2.length >= 10 && (u16(ttf, os2.offset + 8) & 0x0100) != 0) {
            throw new NotSubset(); // No subsetting
        }
        int numGlyphs = u16(ttf, maxp.offset + 4);
        int longOffsets = u16(ttf, head.offset + 50);
        if (numGlyphs == 0 || loca.length < (numGlyphs + 1) * (2 + 2 * longOffsets)) {
            throw new NotSubset();
        }
        int[] starts = new int[numGlyphs];
        int[] ends = new int[numGlyphs];
        for (int gid = 0; gid < numGlyphs; gid++) {
            int start, end;
            if (longOffsets == 1) {
                start = u32(ttf, loca.offset + 4 * gid);
                end = u32(ttf, loca.offset + 4 * gid + 4);
            } else {
                start = 2 * u16(ttf, loca.offset + 2 * gid);
                end = 2 * u16(ttf, loca.offset + 2 * gid + 2);
            }
            starts[gid] = glyf.offset + start;
            ends[gid] = glyf.offset + end;
            if (start > end || end > glyf.length) {
                starts[gid] = -1; // Read only if kept
            }
        }

        // The glyphs kept, with the parts of the composite glyphs.
        boolean[] keep = new boolean[numGlyphs];
        int[] stack = new int[numGlyphs];
        int top = 0;
        for (int gid = 0; gid < numGlyphs && gid < used.length; gid++) {
            if (used[gid] || gid == 0) {
                keep[gid] = true;
                stack[top++] = gid;
            }
        }
        while (top > 0) {
            int gid = stack[--top];
            int start = starts[gid];
            int end = ends[gid];
            if (start < 0) {
                throw new NotSubset();
            }
            if (end - start < 10) {
                continue; // No outline
            }
            if (u16(ttf, start) < 0x8000) {
                continue; // A simple glyph
            }
            int at = start + 10;
            while (true) {
                int flags = u16(ttf, at);
                int component = u16(ttf, at + 2);
                if (at + 4 > end || component >= numGlyphs) {
                    throw new NotSubset();
                }
                if (!keep[component]) {
                    keep[component] = true;
                    stack[top++] = component;
                }
                at += 4;
                at += (flags & 0x0001) != 0 ? 4 : 2;     // ARG_1_AND_2_ARE_WORDS
                if ((flags & 0x0008) != 0) {            // WE_HAVE_A_SCALE
                    at += 2;
                } else if ((flags & 0x0040) != 0) {     // WE_HAVE_AN_X_AND_Y_SCALE
                    at += 4;
                } else if ((flags & 0x0080) != 0) {     // WE_HAVE_A_TWO_BY_TWO
                    at += 8;
                }
                if ((flags & 0x0020) == 0) {            // MORE_COMPONENTS
                    break;
                }
            }
        }

        // The glyf and loca tables, the kept glyphs copied, each padded to a
        // multiple of four bytes, the others empty.
        int glyfLength = 0;
        for (int gid = 0; gid < numGlyphs; gid++) {
            if (keep[gid]) {
                glyfLength += (ends[gid] - starts[gid] + 3) & ~3;
            }
        }
        if (longOffsets == 0 && glyfLength > 0x1FFFE) {
            throw new NotSubset();
        }
        byte[] newGlyf = new byte[glyfLength];
        byte[] newLoca = new byte[(numGlyphs + 1) * (2 + 2 * longOffsets)];
        int offset = 0;
        for (int gid = 0; gid <= numGlyphs; gid++) {
            if (longOffsets == 1) {
                putInt(newLoca, 4 * gid, offset);
            } else {
                newLoca[2 * gid] = (byte) (offset >> 9);
                newLoca[2 * gid + 1] = (byte) (offset >> 1);
            }
            if (gid < numGlyphs && keep[gid]) {
                System.arraycopy(ttf, starts[gid], newGlyf, offset, ends[gid] - starts[gid]);
                offset += (ends[gid] - starts[gid] + 3) & ~3;
            }
        }

        // The font again, its tables in the order of their tags.
        Collections.sort(tables, new Comparator<Table>() {
            public int compare(Table a, Table b) {
                return a.tag.compareTo(b.tag);
            }
        });
        List<String> tags = new ArrayList<String>();
        List<byte[]> data = new ArrayList<byte[]>();
        for (Table t : tables) {
            if (t.tag.equals("glyf")) {
                data.add(newGlyf);
            } else if (t.tag.equals("loca")) {
                data.add(newLoca);
            } else if (t.tag.equals("head")) {
                byte[] h = Arrays.copyOfRange(ttf, t.offset, t.offset + t.length);
                h[8] = h[9] = h[10] = h[11] = 0; // checkSumAdjustment, set below
                data.add(h);
            } else if (t.tag.equals("post")) {
                if (t.length < 32) {
                    continue;
                }
                // Version 3, without the names of the glyphs.
                byte[] p = Arrays.copyOfRange(ttf, t.offset, t.offset + 32);
                p[0] = 0;
                p[1] = 3;
                p[2] = 0;
                p[3] = 0;
                data.add(p);
            } else if (SUBSET_TABLES.contains(t.tag)) {
                data.add(Arrays.copyOfRange(ttf, t.offset, t.offset + t.length));
            } else {
                continue;
            }
            tags.add(t.tag);
        }
        int count = data.size();
        int searchRange = 1;
        int entrySelector = 0;
        while (searchRange * 2 <= count) {
            searchRange *= 2;
            entrySelector++;
        }
        int total = 12 + 16 * count;
        for (byte[] d : data) {
            total += (d.length + 3) & ~3;
        }
        byte[] out = new byte[total];
        System.arraycopy(ttf, 0, out, 0, 4);
        putShort(out, 4, count);
        putShort(out, 6, 16 * searchRange);
        putShort(out, 8, entrySelector);
        putShort(out, 10, 16 * count - 16 * searchRange);
        offset = 12 + 16 * count;
        int headAt = 0;
        for (int i = 0; i < count; i++) {
            byte[] d = data.get(i);
            int entry = 12 + 16 * i;
            for (int j = 0; j < 4; j++) {
                out[entry + j] = (byte) tags.get(i).charAt(j);
            }
            putInt(out, entry + 4, (int) tableChecksum(d));
            putInt(out, entry + 8, offset);
            putInt(out, entry + 12, d.length);
            if (tags.get(i).equals("head")) {
                headAt = offset;
            }
            System.arraycopy(d, 0, out, offset, d.length);
            offset += (d.length + 3) & ~3;
        }
        putInt(out, headAt + 8, (int) ((0xB1B0AFBAL - tableChecksum(out)) & 0xFFFFFFFFL));
        kept[0] = keep;
        return out;
    }

    private static void putShort(byte[] buf, int at, int value) {
        buf[at] = (byte) (value >> 8);
        buf[at + 1] = (byte) value;
    }

    private static void putInt(byte[] buf, int at, int value) {
        buf[at] = (byte) (value >> 24);
        buf[at + 1] = (byte) (value >> 16);
        buf[at + 2] = (byte) (value >> 8);
        buf[at + 3] = (byte) value;
    }

    /**
     * Returns the sum of the big-endian 32-bit words of the data, the last one
     * padded with zeros, as an unsigned 32-bit number.
     */
    static long tableChecksum(byte[] data) {
        long sum = 0;
        for (int i = 0; i < data.length; i += 4) {
            long word = 0;
            for (int j = 0; j < 4; j++) {
                word <<= 8;
                if (i + j < data.length) {
                    word |= data[i + j] & 0xFF;
                }
            }
            sum = (sum + word) & 0xFFFFFFFFL;
        }
        return sum;
    }
}
