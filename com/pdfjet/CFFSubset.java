/*
 * CFFSubset.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

/**
 * The subset of a font with CFF outlines, a .otf: a CIDFont of the glyphs the
 * document draws alone, its charset giving each glyph its number in the whole
 * font as its CID, by which the PDF draws it, and the subroutines, global and
 * local, that the glyphs kept do not call emptied to no bytes, keeping their
 * numbers, so the glyphs kept are drawn by the same bytes as in the whole
 * font. A name-keyed font becomes a CIDFont of Adobe-Identity-0. The CFF is
 * written again in a fixed order, with every offset of its Top DICT and of its
 * Font DICTs in the five-byte form, so that an offset of any value has the
 * same size: the header, the Name, Top DICT, String and Global Subr INDEXes,
 * the charset, the FDSelect, the new CharStrings INDEX, the FDArray, and each
 * Private DICT with its local subroutines.
 */
class CFFSubset {
    // The operators whose operands are offsets in the CFF table.
    static final int CHARSET = 15;
    static final int ENCODING = 16;
    static final int CHAR_STRINGS = 17;
    static final int PRIVATE = 18;
    static final int SUBRS = 19;
    static final int FD_ARRAY = 12 << 8 | 36;
    static final int FD_SELECT = 12 << 8 | 37;
    static final int ROS = 12 << 8 | 30;
    static final int CID_COUNT = 12 << 8 | 34;
    static final int FONT_MATRIX = 12 << 8 | 7;

    // The most bytes of charstrings the subroutines are looked for in, which a
    // font of nested calls that come back to the same subroutines could
    // otherwise make take as long as it likes.
    static final int MAX_WORK = 1 << 24;

    /** An INDEX: where it starts and ends, and the offsets of its objects. */
    static final class Index {
        final int start;
        final int end;
        final int[] objects;    // count+1 offsets, the last one the end of the data

        Index(int start, int end, int[] objects) {
            this.start = start;
            this.end = end;
            this.objects = objects;
        }

        int count() {
            return objects.length - 1;
        }
    }

    /** An entry of a DICT: its operator, its bytes, its integer operands. */
    static final class Entry {
        final int op;           // 12 << 8 | b1 for an escaped one
        final byte[] raw;
        final int[] integers;   // null when an operand is a real

        Entry(int op, byte[] raw, int[] integers) {
            this.op = op;
            this.raw = raw;
            this.integers = integers;
        }
    }

    /** A Private DICT, its entries and the INDEX of its subroutines or null. */
    static final class PrivateDict {
        final List<Entry> entries;
        final Index subrs;

        PrivateDict(List<Entry> entries, Index subrs) {
            this.entries = entries;
            this.subrs = subrs;
        }
    }

    static Index readIndex(byte[] cff, int at) throws Subset.NotSubset {
        // The offsets of the file compared without a sum that could pass
        // Integer.MAX_VALUE (the review of 9 October 2026)
        if (at < 0 || at > cff.length - 2) {
            throw new Subset.NotSubset();
        }
        int count = (cff[at] & 0xFF) << 8 | (cff[at + 1] & 0xFF);
        if (count == 0) {
            return new Index(at, at + 2, new int[] {at + 2});
        }
        if (at > cff.length - 3) {
            throw new Subset.NotSubset();
        }
        int offSize = cff[at + 2] & 0xFF;
        if (offSize < 1 || offSize > 4 || (long) at + 3 + (long) (count + 1) * offSize > cff.length) {
            throw new Subset.NotSubset();
        }
        int data = at + 3 + (count + 1) * offSize - 1;  // The offsets count from 1.
        int[] objects = new int[count + 1];
        for (int i = 0; i < objects.length; i++) {
            long offset = 0;
            for (int j = 0; j < offSize; j++) {
                offset = offset << 8 | (cff[at + 3 + i * offSize + j] & 0xFF);
            }
            if (offset < 1 || data + offset > cff.length || (i > 0 && data + offset < objects[i - 1])) {
                throw new Subset.NotSubset();
            }
            objects[i] = (int) (data + offset);
        }
        return new Index(at, objects[count], objects);
    }

    static void appendIndex(ByteBuffer out, List<byte[]> objects) {
        out.add2(objects.size());
        if (objects.isEmpty()) {
            return;
        }
        int size = 1;
        for (byte[] object : objects) {
            size += object.length;
        }
        int offSize = 1;
        while (offSize < 4 && size >= 1 << (8 * offSize)) {
            offSize++;
        }
        out.add(offSize);
        int offset = 1;
        for (int i = 0; i <= objects.size(); i++) {
            for (int j = offSize - 1; j >= 0; j--) {
                out.add(offset >> (8 * j));
            }
            if (i < objects.size()) {
                offset += objects.get(i).length;
            }
        }
        for (byte[] object : objects) {
            out.add(object);
        }
    }

    static byte[] index(List<byte[]> objects) {
        ByteBuffer out = new ByteBuffer();
        appendIndex(out, objects);
        return out.toBytes();
    }

    static List<Entry> readDict(byte[] cff, int from, int to) throws Subset.NotSubset {
        List<Entry> entries = new ArrayList<Entry>();
        int start = from;
        List<Integer> integers = new ArrayList<Integer>();
        boolean isInteger = true;
        int i = from;
        while (i < to) {
            int b0 = cff[i] & 0xFF;
            if (b0 <= 21) {     // An operator
                int op = b0;
                i++;
                if (b0 == 12) {
                    if (i >= to) {
                        throw new Subset.NotSubset();
                    }
                    op = 12 << 8 | (cff[i] & 0xFF);
                    i++;
                }
                int[] values = null;
                if (isInteger) {
                    values = new int[integers.size()];
                    for (int k = 0; k < values.length; k++) {
                        values[k] = integers.get(k);
                    }
                }
                entries.add(new Entry(op, Arrays.copyOfRange(cff, start, i), values));
                start = i;
                integers.clear();
                isInteger = true;
            } else if (b0 == 28) {
                if (i > to - 3) {
                    throw new Subset.NotSubset();
                }
                integers.add((int) (short) ((cff[i + 1] & 0xFF) << 8 | (cff[i + 2] & 0xFF)));
                i += 3;
            } else if (b0 == 29) {
                if (i > to - 5) {
                    throw new Subset.NotSubset();
                }
                integers.add((cff[i + 1] & 0xFF) << 24 | (cff[i + 2] & 0xFF) << 16 |
                        (cff[i + 3] & 0xFF) << 8 | (cff[i + 4] & 0xFF));
                i += 5;
            } else if (b0 == 30) {      // A real, in nibbles up to one of 0xF
                isInteger = false;
                i++;
                while (i < to && (cff[i] & 0x0F) != 0x0F && (cff[i] & 0xF0) != 0xF0) {
                    i++;
                }
                if (i >= to) {
                    throw new Subset.NotSubset();
                }
                i++;
            } else if (b0 >= 32 && b0 <= 246) {
                integers.add(b0 - 139);
                i++;
            } else if (b0 >= 247 && b0 <= 250) {
                if (i > to - 2) {
                    throw new Subset.NotSubset();
                }
                integers.add((b0 - 247) * 256 + (cff[i + 1] & 0xFF) + 108);
                i += 2;
            } else if (b0 >= 251 && b0 <= 254) {
                if (i > to - 2) {
                    throw new Subset.NotSubset();
                }
                integers.add(-(b0 - 251) * 256 - (cff[i + 1] & 0xFF) - 108);
                i += 2;
            } else {
                throw new Subset.NotSubset();
            }
        }
        if (start != to) {
            throw new Subset.NotSubset();   // Operands without an operator
        }
        return entries;
    }

    static int[] entryOf(List<Entry> entries, int op) {
        for (Entry entry : entries) {
            if (entry.op == op) {
                return entry.integers;
            }
        }
        return null;
    }

    static void appendOffset(ByteBuffer out, int value) {
        out.add(29);
        out.add(value >> 24);
        out.add(value >> 16);
        out.add(value >> 8);
        out.add(value);
    }

    // Writes the entries again, the operands of the operators in offsets in
    // the five-byte form: the offset, or for Private its size and its offset.
    static byte[] writeDict(List<Entry> entries, Map<Integer, Integer> offsets, int privateSize) {
        ByteBuffer out = new ByteBuffer();
        for (Entry entry : entries) {
            Integer offset = offsets.get(entry.op);
            if (offset == null) {
                out.add(entry.raw);
                continue;
            }
            if (entry.op == PRIVATE) {
                appendOffset(out, privateSize);
            }
            appendOffset(out, offset);
            if (entry.op > 0xFF) {
                out.add(12);
                out.add(entry.op & 0xFF);
            } else {
                out.add(entry.op);
            }
        }
        return out.toBytes();
    }

    static int fdSelectLength(byte[] cff, int at, int numGlyphs) throws Subset.NotSubset {
        if (at < 0 || at >= cff.length) {
            throw new Subset.NotSubset();
        }
        if (cff[at] == 0) {
            return 1 + numGlyphs;
        }
        if (cff[at] == 3) {
            if (at > cff.length - 3) {
                throw new Subset.NotSubset();
            }
            int ranges = (cff[at + 1] & 0xFF) << 8 | (cff[at + 2] & 0xFF);
            return 1 + 2 + 3 * ranges + 2;
        }
        throw new Subset.NotSubset();
    }

    static byte[] block(byte[] cff, int at, int length) throws Subset.NotSubset {
        if (at < 0 || length < 0 || (long) at + length > cff.length) {
            throw new Subset.NotSubset();
        }
        return Arrays.copyOfRange(cff, at, at + length);
    }

    static PrivateDict readPrivate(byte[] cff, int size, int at) throws Subset.NotSubset {
        block(cff, at, size);
        List<Entry> entries = readDict(cff, at, at + size);
        int[] subrs = entryOf(entries, SUBRS);
        if (subrs == null) {
            return new PrivateDict(entries, null);
        }
        if (subrs.length != 1) {
            throw new Subset.NotSubset();
        }
        long subrsAt = (long) at + subrs[0];
        if (subrsAt < 0 || subrsAt > cff.length) {
            throw new Subset.NotSubset();
        }
        return new PrivateDict(entries, readIndex(cff, (int) subrsAt));
    }

    static int bias(int count) {
        if (count < 1240) {
            return 107;
        } else if (count < 33900) {
            return 1131;
        }
        return 32768;
    }

    /**
     * Finds the subroutines that the charstrings of the glyphs kept call,
     * directly or through other subroutines.
     */
    static final class Usage {
        final byte[] cff;
        final Index global;
        final boolean[] usedG;
        Index local;
        boolean[] usedL;
        int stack;      // The operands on the stack
        int last;       // The last of them, a subroutine number when one is called
        int stems;
        int work;
        boolean failed;
        boolean stopped;    // By endchar
        boolean seac;       // An accented letter drawn from two others

        Usage(byte[] cff, Index global, boolean[] usedG) {
            this.cff = cff;
            this.global = global;
            this.usedG = usedG;
        }

        void run(int from, int to, int depth) {
            if (depth > 10) {   // Type 2 nests subroutines 10 deep at most.
                failed = true;
                return;
            }
            work += to - from;
            if (work > MAX_WORK) {
                failed = true;
                return;
            }
            int i = from;
            while (i < to && !failed && !stopped) {
                int b0 = cff[i] & 0xFF;
                if (b0 == 28) {
                    if (i > to - 3) {
                        failed = true;
                        return;
                    }
                    last = (short) ((cff[i + 1] & 0xFF) << 8 | (cff[i + 2] & 0xFF));
                    stack++;
                    i += 3;
                } else if (b0 >= 32 && b0 <= 246) {
                    last = b0 - 139;
                    stack++;
                    i++;
                } else if (b0 >= 247 && b0 <= 250) {
                    if (i > to - 2) {
                        failed = true;
                        return;
                    }
                    last = (b0 - 247) * 256 + (cff[i + 1] & 0xFF) + 108;
                    stack++;
                    i += 2;
                } else if (b0 >= 251 && b0 <= 254) {
                    if (i > to - 2) {
                        failed = true;
                        return;
                    }
                    last = -(b0 - 251) * 256 - (cff[i + 1] & 0xFF) - 108;
                    stack++;
                    i += 2;
                } else if (b0 == 255) {     // A 16.16 fixed number
                    if (i > to - 5) {
                        failed = true;
                        return;
                    }
                    last = ((cff[i + 1] & 0xFF) << 24 | (cff[i + 2] & 0xFF) << 16 |
                            (cff[i + 3] & 0xFF) << 8 | (cff[i + 4] & 0xFF)) >> 16;
                    stack++;
                    i += 5;
                } else if (b0 == 1 || b0 == 3 || b0 == 18 || b0 == 23) {    // The stem hints
                    stems += stack / 2;
                    stack = 0;
                    i++;
                } else if (b0 == 19 || b0 == 20) {  // hintmask and cntrmask, and their mask
                    stems += stack / 2;
                    stack = 0;
                    i += 1 + (stems + 7) / 8;
                } else if (b0 == 10 || b0 == 29) {  // callsubr and callgsubr
                    if (stack == 0) {
                        failed = true;
                        return;
                    }
                    stack--;
                    Index subrs = (b0 == 29) ? global : local;
                    boolean[] used = (b0 == 29) ? usedG : usedL;
                    if (subrs == null) {
                        failed = true;
                        return;
                    }
                    int number = last + bias(subrs.count());
                    if (number < 0 || number >= subrs.count()) {
                        failed = true;
                        return;
                    }
                    used[number] = true;
                    run(subrs.objects[number], subrs.objects[number + 1], depth + 1);
                    i++;
                } else if (b0 == 11) {      // return
                    return;
                } else if (b0 == 14) {      // endchar
                    if (stack >= 4) {
                        // seac, an accented letter drawn from two others, which
                        // would have to be kept too: the font is embedded whole
                        // (the review of 9 October 2026: the letter drew blank)
                        seac = true;
                        failed = true;
                    }
                    stopped = true;
                    return;
                } else if (b0 == 12) {      // An escaped operator
                    stack = 0;
                    i += 2;
                } else {
                    stack = 0;
                    i++;
                }
            }
        }
    }

    // Returns the objects of the INDEX, those not used emptied to no bytes,
    // which keeps their numbers and makes their offsets the same as the next
    // one's, or every one for used null.
    static List<byte[]> emptied(byte[] cff, Index index, boolean[] used) {
        List<byte[]> objects = new ArrayList<byte[]>(index.count());
        for (int i = 0; i < index.count(); i++) {
            if (used == null || used[i]) {
                objects.add(Arrays.copyOfRange(cff, index.objects[i], index.objects[i + 1]));
            } else {
                objects.add(new byte[0]);
            }
        }
        return objects;
    }

    // Returns the Font DICT of each glyph, from the FDSelect of a CID-keyed
    // font, or null for a name-keyed one.
    static int[] fdOf(byte[] fdSelect, int numGlyphs, int fds) throws Subset.NotSubset {
        if (fdSelect == null) {
            return null;
        }
        int[] fdOf = new int[numGlyphs];
        if (fdSelect[0] == 0) {
            for (int gid = 0; gid < numGlyphs; gid++) {
                fdOf[gid] = fdSelect[1 + gid] & 0xFF;
            }
        } else if (fdSelect[0] == 3) {
            int ranges = (fdSelect[1] & 0xFF) << 8 | (fdSelect[2] & 0xFF);
            for (int r = 0; r < ranges; r++) {
                int first = (fdSelect[3 + 3 * r] & 0xFF) << 8 | (fdSelect[4 + 3 * r] & 0xFF);
                // The first of the next range, or the sentinel
                int next = (fdSelect[6 + 3 * r] & 0xFF) << 8 | (fdSelect[7 + 3 * r] & 0xFF);
                int fd = fdSelect[5 + 3 * r] & 0xFF;
                if (first > next || next > numGlyphs) {
                    throw new Subset.NotSubset();
                }
                for (int gid = first; gid < next; gid++) {
                    fdOf[gid] = fd;
                }
            }
        }
        for (int fd : fdOf) {
            if (fd >= fds) {
                throw new Subset.NotSubset();
            }
        }
        return fdOf;
    }

    /**
     * Returns the CFF table of the glyphs used and glyph 0, or of every glyph
     * for used null, and gives in kept[0] the glyphs kept, by their numbers in
     * the font. The subset is a CIDFont of the glyphs kept alone, in the order
     * of their numbers, its charset giving each its number in the font as its
     * CID: the PDF draws a glyph of a CIDFontType0C program by its CID, which
     * the charset maps to the glyph (ISO 32000-2, 9.7.4.2; Technical Note
     * #5176, section 18, "Subset CIDFonts will generally need to use a more
     * complex charset table representing a non-identity mapping"), so the
     * content streams, the widths and the ToUnicode map stay those of the
     * whole font. A name-keyed font is written as a CIDFont of the ordering
     * Adobe-Identity-0 with one Font DICT, its Private DICT. The subroutines
     * the glyphs kept do not call are emptied to no bytes, keeping their
     * numbers, and the Font DICTs no glyph kept uses are left out. Every INDEX
     * has the least offSize its offsets need; the offsets of the Top DICT and
     * of the Font DICTs are in the five-byte form. A table that cannot be
     * read, or a name-keyed font of a FontMatrix of its own, throws NotSubset.
     */
    static byte[] subset(byte[] cff, boolean[] used, boolean[][] kept) throws Subset.NotSubset {
        if (cff.length < 4 || cff[0] != 1) {
            throw new Subset.NotSubset();   // Not CFF version 1
        }
        int headerSize = cff[2] & 0xFF;
        Index names = readIndex(cff, headerSize);
        Index topDicts = readIndex(cff, names.end);
        if (topDicts.count() != 1) {    // One font
            throw new Subset.NotSubset();
        }
        Index strings = readIndex(cff, topDicts.end);
        Index globalSubrs = readIndex(cff, strings.end);
        List<Entry> top = readDict(cff, topDicts.objects[0], topDicts.objects[1]);
        int[] charStringsAt = entryOf(top, CHAR_STRINGS);
        if (charStringsAt == null || charStringsAt.length != 1) {
            throw new Subset.NotSubset();
        }
        Index charStrings = readIndex(cff, charStringsAt[0]);
        int numGlyphs = charStrings.count();
        if (numGlyphs == 0) {
            throw new Subset.NotSubset();
        }

        boolean cidKeyed = entryOf(top, ROS) != null;
        byte[] fdSelect = null;
        if (cidKeyed) {
            int[] count = entryOf(top, CID_COUNT);
            if (count != null && (count.length != 1 || count[0] < numGlyphs)) {
                throw new Subset.NotSubset();
            }
            if (count == null && numGlyphs > 8720) {
                throw new Subset.NotSubset();   // The default CIDCount
            }
            int[] at = entryOf(top, FD_SELECT);
            if (at == null || at.length != 1) {
                throw new Subset.NotSubset();
            }
            fdSelect = block(cff, at[0], fdSelectLength(cff, at[0], numGlyphs));
        } else if (hasEntry(top, FONT_MATRIX)) {
            // Its matrix would have to go into the Font DICT, against the
            // one a CIDFont's Top DICT has by default.
            throw new Subset.NotSubset();
        }

        // The Font DICTs of a CID-keyed font, each with its Private DICT, or
        // the Private DICT of a name-keyed one, which becomes its one Font
        // DICT's.
        List<PrivateDict> privates = new ArrayList<PrivateDict>();
        List<List<Entry>> fontDicts = new ArrayList<List<Entry>>();
        if (cidKeyed) {
            int[] at = entryOf(top, FD_ARRAY);
            if (at == null || at.length != 1) {
                throw new Subset.NotSubset();
            }
            Index fdArray = readIndex(cff, at[0]);
            for (int i = 0; i < fdArray.count(); i++) {
                List<Entry> entries = readDict(cff, fdArray.objects[i], fdArray.objects[i + 1]);
                int[] p = entryOf(entries, PRIVATE);
                if (p == null || p.length != 2) {
                    throw new Subset.NotSubset();
                }
                PrivateDict block = readPrivate(cff, p[0], p[1]);
                fontDicts.add(entries);
                privates.add(block);
            }
        } else {
            int[] p = entryOf(top, PRIVATE);
            if (p == null || p.length != 2) {
                throw new Subset.NotSubset();
            }
            PrivateDict block = readPrivate(cff, p[0], p[1]);
            List<Entry> fontDict = new ArrayList<Entry>();
            fontDict.add(new Entry(PRIVATE, new byte[0], null));
            fontDicts.add(fontDict);
            privates.add(block);
        }

        // The glyphs kept, by their numbers in the font, which are their CIDs.
        boolean[] keep = new boolean[numGlyphs];
        List<Integer> keptGIDs = new ArrayList<Integer>();
        for (int gid = 0; gid < numGlyphs; gid++) {
            if (used == null || gid == 0 || (gid < used.length && used[gid])) {
                keep[gid] = true;
                keptGIDs.add(gid);
            }
        }
        List<byte[]> glyphs = new ArrayList<byte[]>(keptGIDs.size());
        for (int gid : keptGIDs) {
            glyphs.add(Arrays.copyOfRange(cff, charStrings.objects[gid], charStrings.objects[gid + 1]));
        }

        // The subroutines the glyphs kept call, the others emptied. A font
        // whose charstrings cannot be followed keeps them all.
        int[] fdOf = fdOf(fdSelect, numGlyphs, privates.size());
        boolean[] usedG = new boolean[globalSubrs.count()];
        boolean[][] usedL = new boolean[privates.size()][];
        for (int i = 0; i < privates.size(); i++) {
            if (privates.get(i).subrs != null) {
                usedL[i] = new boolean[privates.get(i).subrs.count()];
            }
        }
        Usage usage = new Usage(cff, globalSubrs, usedG);
        if (used == null) {
            usage.failed = true;    // Every glyph and subroutine kept
        }
        for (int i = 0; i < keptGIDs.size() && !usage.failed; i++) {
            int gid = keptGIDs.get(i);
            int fd = (fdOf != null) ? fdOf[gid] : 0;
            usage.local = null;
            usage.usedL = null;
            if (fd < privates.size() && privates.get(fd).subrs != null) {
                usage.local = privates.get(fd).subrs;
                usage.usedL = usedL[fd];
            }
            usage.stack = 0;
            usage.stems = 0;
            usage.stopped = false;
            usage.run(charStrings.objects[gid], charStrings.objects[gid + 1], 0);
        }
        if (usage.failed) {
            if (usage.work > MAX_WORK || usage.seac) {
                throw new Subset.NotSubset();
            }
            usedG = null;   // Kept whole
            for (int i = 0; i < usedL.length; i++) {
                usedL[i] = null;
            }
        }

        // The Font DICTs the glyphs kept use, in their order in the font, and
        // the new number of each.
        boolean[] usedFD = new boolean[privates.size()];
        for (int gid : keptGIDs) {
            usedFD[(fdOf != null) ? fdOf[gid] : 0] = true;
        }
        int[] newFD = new int[privates.size()];
        List<Integer> fds = new ArrayList<Integer>();
        for (int fd = 0; fd < usedFD.length; fd++) {
            if (usedFD[fd]) {
                newFD[fd] = fds.size();
                fds.add(fd);
            }
        }
        int[] fdOfGlyph = new int[keptGIDs.size()];
        for (int i = 0; i < keptGIDs.size(); i++) {
            if (fdOf != null) {
                fdOfGlyph[i] = newFD[fdOf[keptGIDs.get(i)]];
            }
        }

        // The blocks: the Name INDEX again; the String INDEX of the strings
        // the Top DICT and the Font DICTs kept name, in the order they name
        // them, a name-keyed font's with those of its ROS, Adobe and Identity,
        // after them, and not the names of its glyphs, which a CIDFont has
        // not; the charset of the CIDs; the FDSelect; the CharStrings of the
        // glyphs kept.
        byte[] nameIndex = index(emptied(cff, names, null));
        Strings sids = new Strings(emptied(cff, strings, null));
        List<Entry> entries = new ArrayList<Entry>();
        for (Entry entry : top) {
            if (entry.op == CHARSET || entry.op == ENCODING || entry.op == CHAR_STRINGS ||
                    entry.op == PRIVATE || entry.op == FD_ARRAY || entry.op == FD_SELECT) {
                continue;
            }
            entries.add(sids.renumber(entry));
        }
        List<List<Entry>> keptFontDicts = new ArrayList<List<Entry>>();
        for (int fd : fds) {
            List<Entry> fontDict = new ArrayList<Entry>();
            for (Entry entry : fontDicts.get(fd)) {
                fontDict.add(sids.renumber(entry));
            }
            keptFontDicts.add(fontDict);
        }
        if (!cidKeyed) {
            int sid = 391 + sids.kept.size();
            sids.kept.add(new byte[] {'A', 'd', 'o', 'b', 'e'});
            sids.kept.add(new byte[] {'I', 'd', 'e', 'n', 't', 'i', 't', 'y'});
            ByteBuffer ros = new ByteBuffer();
            appendInteger(ros, sid);
            appendInteger(ros, sid + 1);
            appendInteger(ros, 0);
            ros.add(12);
            ros.add(30);
            entries.add(0, new Entry(ROS, ros.toBytes(), null));
            ByteBuffer count = new ByteBuffer();
            appendInteger(count, numGlyphs);
            count.add(12);
            count.add(34);
            entries.add(new Entry(CID_COUNT, count.toBytes(), null));
        }
        byte[] stringIndex = index(sids.kept);
        entries.add(new Entry(CHARSET, new byte[0], null));
        entries.add(new Entry(FD_SELECT, new byte[0], null));
        entries.add(new Entry(CHAR_STRINGS, new byte[0], null));
        entries.add(new Entry(FD_ARRAY, new byte[0], null));
        int[] cids = new int[keptGIDs.size() - 1];
        for (int i = 0; i < cids.length; i++) {
            cids[i] = keptGIDs.get(i + 1);
        }
        byte[] charset = charsetOf(cids);
        byte[] newFDSelect = fdSelectOf(fdOfGlyph);
        byte[] newCharStrings = index(glyphs);
        // An INDEX of subroutines none of which is called is left out, global
        // ones as an empty INDEX, local ones with their Subrs operator: one of
        // objects of no bytes alone is not read by FreeType
        List<byte[]> globals = emptied(cff, globalSubrs, usedG);
        if (usedG != null && !anyUsed(usedG)) {
            globals = new ArrayList<byte[]>();
        }
        byte[] newGlobalSubrs = index(globals);

        // Each Private DICT of a Font DICT kept is written again with its
        // local subroutines right after it, at its size, whatever they were
        // before.
        byte[][] blocks = new byte[fds.size()][];
        int[] dictSizes = new int[fds.size()];
        for (int i = 0; i < fds.size(); i++) {
            int fd = fds.get(i);
            PrivateDict p = privates.get(fd);
            if (p.subrs != null && usedL[fd] != null && !anyUsed(usedL[fd])) {
                List<Entry> rest = new ArrayList<Entry>();
                for (Entry entry : p.entries) {
                    if (entry.op != SUBRS) {
                        rest.add(entry);
                    }
                }
                p = new PrivateDict(rest, null);
            }
            Map<Integer, Integer> subrs = new HashMap<Integer, Integer>();
            if (p.subrs != null) {
                subrs.put(SUBRS, 0);
            }
            dictSizes[i] = writeDict(p.entries, subrs, 0).length;
            if (p.subrs != null) {
                subrs.put(SUBRS, dictSizes[i]);
            }
            ByteBuffer b = new ByteBuffer();
            b.add(writeDict(p.entries, subrs, 0));
            if (p.subrs != null) {
                appendIndex(b, emptied(cff, p.subrs, usedL[fd]));
            }
            blocks[i] = b.toBytes();
        }

        // The layout. The Top DICT and the Font DICTs have the same size
        // whatever their offsets, so they are written once with none to know
        // where the blocks after them go, and once more with their offsets.
        int topSize = writeDict(entries, topOffsets(0, 0, 0, 0), 0).length;
        List<byte[]> sizedTop = new ArrayList<byte[]>();
        sizedTop.add(new byte[topSize]);
        int at = headerSize + nameIndex.length + index(sizedTop).length +
                stringIndex.length + newGlobalSubrs.length;
        int charsetAt = at;
        at += charset.length;
        int fdSelectAt = at;
        at += newFDSelect.length;
        int newCharStringsAt = at;
        at += newCharStrings.length;
        int fdArrayAt = at;
        List<byte[]> sized = new ArrayList<byte[]>();
        for (int i = 0; i < fds.size(); i++) {
            sized.add(writeDict(keptFontDicts.get(i), privateOffset(0), dictSizes[i]));
        }
        at += index(sized).length;
        int[] privateAts = new int[fds.size()];
        for (int i = 0; i < blocks.length; i++) {
            privateAts[i] = at;
            at += blocks[i].length;
        }
        List<byte[]> dicts = new ArrayList<byte[]>();
        for (int i = 0; i < fds.size(); i++) {
            dicts.add(writeDict(keptFontDicts.get(i), privateOffset(privateAts[i]), dictSizes[i]));
        }
        byte[] fdArray = index(dicts);
        byte[] newTop = writeDict(entries, topOffsets(charsetAt, fdSelectAt, newCharStringsAt, fdArrayAt), 0);

        ByteBuffer out = new ByteBuffer();
        out.add(Arrays.copyOfRange(cff, 0, headerSize));
        out.add(nameIndex);
        List<byte[]> tops = new ArrayList<byte[]>();
        tops.add(newTop);
        appendIndex(out, tops);
        out.add(stringIndex);
        out.add(newGlobalSubrs);
        out.add(charset);
        out.add(newFDSelect);
        out.add(newCharStrings);
        out.add(fdArray);
        for (byte[] b : blocks) {
            out.add(b);
        }
        byte[] result = out.toBytes();
        if (result.length != at) {
            throw new Subset.NotSubset();
        }
        kept[0] = keep;
        return result;
    }

    private static Map<Integer, Integer> privateOffset(int at) {
        Map<Integer, Integer> offsets = new HashMap<Integer, Integer>();
        offsets.put(PRIVATE, at);
        return offsets;
    }

    private static Map<Integer, Integer> topOffsets(int charsetAt, int fdSelectAt, int charStringsAt,
            int fdArrayAt) {
        Map<Integer, Integer> offsets = new HashMap<Integer, Integer>();
        offsets.put(CHARSET, charsetAt);
        offsets.put(FD_SELECT, fdSelectAt);
        offsets.put(CHAR_STRINGS, charStringsAt);
        offsets.put(FD_ARRAY, fdArrayAt);
        return offsets;
    }

    // The operators of the Top DICT and of the Font DICTs whose first
    // operands are SIDs, and how many: version, Notice, FullName, FamilyName,
    // Weight, Copyright, PostScript, BaseFontName, ROS and FontName.
    private static final Map<Integer, Integer> SID_OPERANDS = new HashMap<Integer, Integer>();
    static {
        for (int op : new int[] {0, 1, 2, 3, 4, 12 << 8, 12 << 8 | 21, 12 << 8 | 22, 12 << 8 | 38}) {
            SID_OPERANDS.put(op, 1);
        }
        SID_OPERANDS.put(ROS, 2);
    }

    /**
     * Gives the strings of the font that the entries name new SIDs, in the
     * order they are first named, after the standard strings.
     */
    static final class Strings {
        final List<byte[]> old;     // The String INDEX of the font
        final List<byte[]> kept = new ArrayList<byte[]>();  // The strings named, in the order of their new SIDs
        final Map<Integer, Integer> sids = new HashMap<Integer, Integer>();  // The new SID of each SID of the font named

        Strings(List<byte[]> old) {
            this.old = old;
        }

        // Returns the entry with its SIDs of the font's strings renumbered,
        // its operands written again in their shortest form, or as it was
        // when it names none.
        Entry renumber(Entry entry) throws Subset.NotSubset {
            Integer n = SID_OPERANDS.get(entry.op);
            if (n == null) {
                return entry;
            }
            if (entry.integers == null || entry.integers.length < n) {
                throw new Subset.NotSubset();
            }
            ByteBuffer raw = new ByteBuffer();
            int[] integers = new int[entry.integers.length];
            for (int i = 0; i < entry.integers.length; i++) {
                int v = entry.integers[i];
                if (i < n && v >= 391) {
                    if (v - 391 >= old.size()) {
                        throw new Subset.NotSubset();
                    }
                    Integer sid = sids.get(v);
                    if (sid == null) {
                        sid = 391 + kept.size();
                        sids.put(v, sid);
                        kept.add(old.get(v - 391));
                    }
                    v = sid;
                }
                integers[i] = v;
                appendInteger(raw, v);
            }
            if (entry.op > 0xFF) {
                raw.add(12);
                raw.add(entry.op & 0xFF);
            } else {
                raw.add(entry.op);
            }
            return new Entry(entry.op, raw.toBytes(), integers);
        }
    }

    // Tells whether any of the subroutines is used.
    static boolean anyUsed(boolean[] used) {
        for (boolean u : used) {
            if (u) {
                return true;
            }
        }
        return false;
    }

    // Tells whether the DICT has an entry of the operator, of any operands.
    static boolean hasEntry(List<Entry> entries, int op) {
        for (Entry entry : entries) {
            if (entry.op == op) {
                return true;
            }
        }
        return false;
    }

    // Appends the integer of a DICT in its shortest form.
    static void appendInteger(ByteBuffer out, int v) {
        if (v >= -107 && v <= 107) {
            out.add(v + 139);
        } else if (v >= 108 && v <= 1131) {
            v -= 108;
            out.add((v >> 8) + 247);
            out.add(v);
        } else if (v >= -1131 && v <= -108) {
            v = -v - 108;
            out.add((v >> 8) + 251);
            out.add(v);
        } else if (v >= -32768 && v <= 32767) {
            out.add(28);
            out.add(v >> 8);
            out.add(v);
        } else {
            appendOffset(out, v);
        }
    }

    // Returns the runs of consecutive CIDs, each its first and the number
    // after it, nLeft, of at most max.
    static List<int[]> ranges(int[] cids, int max) {
        List<int[]> ranges = new ArrayList<int[]>();
        int i = 0;
        while (i < cids.length) {
            int j = i;
            while (j + 1 < cids.length && cids[j + 1] == cids[j] + 1 && j + 1 - i <= max) {
                j++;
            }
            ranges.add(new int[] {cids[i], j - i});
            i = j + 1;
        }
        return ranges;
    }

    // Returns the charset of the CIDs of the glyphs after .notdef, in the
    // format of the three that is the shortest, the first of those as short.
    static byte[] charsetOf(int[] cids) {
        List<int[]> ranges1 = ranges(cids, 0xFF);
        List<int[]> ranges2 = ranges(cids, 0xFFFF);
        int size0 = 1 + 2 * cids.length;
        int size1 = 1 + 3 * ranges1.size();
        int size2 = 1 + 4 * ranges2.size();
        ByteBuffer out = new ByteBuffer();
        if (size0 <= size1 && size0 <= size2) {
            out.add(0);
            for (int cid : cids) {
                out.add2(cid);
            }
        } else if (size1 <= size2) {
            out.add(1);
            for (int[] r : ranges1) {
                out.add2(r[0]);
                out.add(r[1]);
            }
        } else {
            out.add(2);
            for (int[] r : ranges2) {
                out.add2(r[0]);
                out.add2(r[1]);
            }
        }
        return out.toBytes();
    }

    // Returns the FDSelect of the Font DICT of each glyph, in format 3,
    // ranges, when that is shorter than format 0, a byte for each.
    static byte[] fdSelectOf(int[] fdOf) {
        List<Integer> firsts = new ArrayList<Integer>();
        for (int gid = 0; gid < fdOf.length; gid++) {
            if (gid == 0 || fdOf[gid] != fdOf[gid - 1]) {
                firsts.add(gid);
            }
        }
        ByteBuffer out = new ByteBuffer();
        if (5 + 3 * firsts.size() >= 1 + fdOf.length) {
            out.add(0);
            for (int fd : fdOf) {
                out.add(fd);
            }
            return out.toBytes();
        }
        out.add(3);
        out.add2(firsts.size());
        for (int first : firsts) {
            out.add2(first);
            out.add(fdOf[first]);
        }
        out.add2(fdOf.length);
        return out.toBytes();
    }

    /** A growing array of bytes. */
    static final class ByteBuffer {
        private byte[] buf = new byte[256];
        private int size;

        private void grow(int n) {
            if (size + n > buf.length) {
                buf = Arrays.copyOf(buf, Math.max(buf.length * 2, size + n));
            }
        }

        void add(int b) {
            grow(1);
            buf[size++] = (byte) b;
        }

        void add2(int v) {
            add(v >> 8);
            add(v);
        }

        void add(byte[] bytes) {
            grow(bytes.length);
            System.arraycopy(bytes, 0, buf, size, bytes.length);
            size += bytes.length;
        }

        byte[] toBytes() {
            return Arrays.copyOf(buf, size);
        }
    }
}
