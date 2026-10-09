/*
 * CFFSubset.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

/**
 * The subset of a font with CFF outlines, a .otf: the CFF table with the
 * charstrings of the glyphs the document does not draw emptied, and the
 * subroutines, global and local, that the glyphs kept do not call. Every glyph
 * and every subroutine keeps its number, so the glyphs kept are drawn by the
 * same bytes as in the whole font. The CFF is written again in a fixed order,
 * with every offset of its Top DICT, of the Font DICTs of a CID-keyed font and
 * of its Private DICTs in the five-byte form, so that an offset of any value
 * has the same size: the header, the Name, Top DICT, String and Global Subr
 * INDEXes, the charset, the Encoding and the FDSelect, the new CharStrings
 * INDEX, the FDArray, and each Private DICT with its local subroutines after
 * it.
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

    static int charsetLength(byte[] cff, int at, int numGlyphs) throws Subset.NotSubset {
        if (at < 0 || at >= cff.length) {
            throw new Subset.NotSubset();
        }
        int format = cff[at] & 0xFF;
        if (format == 0) {
            return 1 + 2 * (numGlyphs - 1);
        }
        if (format == 1 || format == 2) {
            int size = format;  // The size of nLeft: 1 byte in format 1, 2 in format 2
            int covered = 1;    // .notdef is not in it
            int i = at + 1;
            while (covered < numGlyphs) {
                if (i > cff.length - 2 - size) {
                    throw new Subset.NotSubset();
                }
                int nLeft = cff[i + 2] & 0xFF;
                if (size == 2) {
                    nLeft = nLeft << 8 | (cff[i + 3] & 0xFF);
                }
                covered += nLeft + 1;
                i += 2 + size;
            }
            return i - at;
        }
        throw new Subset.NotSubset();
    }

    static int encodingLength(byte[] cff, int at) throws Subset.NotSubset {
        if (at < 0 || at > cff.length - 2) {
            throw new Subset.NotSubset();
        }
        int length;
        int format = cff[at] & 0x7F;
        if (format == 0) {
            length = 2 + (cff[at + 1] & 0xFF);
        } else if (format == 1) {
            length = 2 + 2 * (cff[at + 1] & 0xFF);
        } else {
            throw new Subset.NotSubset();
        }
        if ((cff[at] & 0x80) != 0) {    // Supplements
            if (length >= cff.length - at) {
                throw new Subset.NotSubset();
            }
            length += 1 + 3 * (cff[at + length] & 0xFF);
        }
        return length;
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

    // Returns the objects of the INDEX, those not used emptied to the
    // charstring of the byte given.
    static List<byte[]> emptied(byte[] cff, Index index, boolean[] used, int empty) {
        List<byte[]> objects = new ArrayList<byte[]>(index.count());
        for (int i = 0; i < index.count(); i++) {
            if (used == null || used[i]) {
                objects.add(Arrays.copyOfRange(cff, index.objects[i], index.objects[i + 1]));
            } else {
                objects.add(new byte[] {(byte) empty});
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
     * Returns the CFF table with the charstrings of the glyphs not used
     * emptied, and the subroutines they do not call, and gives in kept[0] the
     * glyphs kept: those used and glyph 0, or every glyph for used null, which
     * writes the table again with nothing left out, for the identity charset
     * of a CID-keyed font. A table that cannot be read throws NotSubset.
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
        final List<Entry> top = readDict(cff, topDicts.objects[0], topDicts.objects[1]);
        int[] charStringsAt = entryOf(top, CHAR_STRINGS);
        if (charStringsAt == null || charStringsAt.length != 1) {
            throw new Subset.NotSubset();
        }
        Index charStrings = readIndex(cff, charStringsAt[0]);
        int numGlyphs = charStrings.count();
        if (numGlyphs == 0) {
            throw new Subset.NotSubset();
        }

        // The blocks copied as they are, but the charset of a CID-keyed font,
        // which maps each glyph to its CID: it is made the identity, so that
        // the glyph numbers PDFjet writes are the CIDs of the glyphs, as they
        // are in a name-keyed font.
        byte[] charset = null;
        byte[] encoding = null;
        byte[] fdSelect = null;
        if (entryOf(top, ROS) != null) {
            int[] count = entryOf(top, CID_COUNT);
            if ((count != null && (count.length != 1 || count[0] < numGlyphs)) ||
                    (count == null && numGlyphs > 8720)) {  // The default CIDCount
                throw new Subset.NotSubset();
            }
            charset = new byte[] {0};   // Format 0, for .notdef alone
            if (numGlyphs > 1) {
                // Format 2, one range: CIDs 1 to numGlyphs-1.
                charset = new byte[] {2, 0, 1, (byte) ((numGlyphs - 2) >> 8), (byte) (numGlyphs - 2)};
            }
        } else {
            int[] at = entryOf(top, CHARSET);
            if (at != null && at.length == 1 && at[0] > 2) {
                charset = block(cff, at[0], charsetLength(cff, at[0], numGlyphs));
            }
        }
        int[] encodingAt = entryOf(top, ENCODING);
        if (encodingAt != null && encodingAt.length == 1 && encodingAt[0] > 1) {
            encoding = block(cff, encodingAt[0], encodingLength(cff, encodingAt[0]));
        }
        int[] fdSelectAt = entryOf(top, FD_SELECT);
        if (fdSelectAt != null) {
            if (fdSelectAt.length != 1) {
                throw new Subset.NotSubset();
            }
            fdSelect = block(cff, fdSelectAt[0], fdSelectLength(cff, fdSelectAt[0], numGlyphs));
        }

        // The Private DICT of a name-keyed font, or the Font DICTs of a
        // CID-keyed one, each with its Private DICT.
        List<PrivateDict> privates = new ArrayList<PrivateDict>();
        List<List<Entry>> fontDicts = null;
        int[] fdArrayAt = entryOf(top, FD_ARRAY);
        if (fdArrayAt != null) {
            if (fdArrayAt.length != 1 || fdSelect == null) {
                throw new Subset.NotSubset();
            }
            Index fdArray = readIndex(cff, fdArrayAt[0]);
            fontDicts = new ArrayList<List<Entry>>();
            for (int i = 0; i < fdArray.count(); i++) {
                List<Entry> entries = readDict(cff, fdArray.objects[i], fdArray.objects[i + 1]);
                int[] p = entryOf(entries, PRIVATE);
                if (p == null || p.length != 2) {
                    throw new Subset.NotSubset();
                }
                fontDicts.add(entries);
                privates.add(readPrivate(cff, p[0], p[1]));
            }
        } else {
            int[] p = entryOf(top, PRIVATE);
            if (p != null) {
                if (p.length != 2) {
                    throw new Subset.NotSubset();
                }
                privates.add(readPrivate(cff, p[0], p[1]));
            }
        }

        // The charstrings, of the glyphs kept as they were, of the others
        // endchar.
        boolean[] keep = new boolean[numGlyphs];
        List<byte[]> glyphs = new ArrayList<byte[]>(numGlyphs);
        for (int gid = 0; gid < numGlyphs; gid++) {
            if (used == null || gid == 0 || (gid < used.length && used[gid])) {
                keep[gid] = true;
                glyphs.add(Arrays.copyOfRange(cff, charStrings.objects[gid], charStrings.objects[gid + 1]));
            } else {
                glyphs.add(new byte[] {14});
            }
        }
        byte[] newCharStrings = index(glyphs);

        // The subroutines the glyphs kept call, the others emptied to return.
        // A font whose charstrings cannot be followed keeps them all.
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
        for (int gid = 0; gid < numGlyphs && !usage.failed; gid++) {
            if (!keep[gid]) {
                continue;
            }
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
        byte[] newGlobalSubrs = index(emptied(cff, globalSubrs, usedG, 11));

        // Each Private DICT is written again with its local subroutines right
        // after it, at its size, whatever they were before.
        byte[][] blocks = new byte[privates.size()][];
        final int[] dictSizes = new int[privates.size()];
        for (int i = 0; i < privates.size(); i++) {
            PrivateDict p = privates.get(i);
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
                appendIndex(b, emptied(cff, p.subrs, usedL[i], 11));
            }
            blocks[i] = b.toBytes();
        }

        // The layout. The Top DICT and the Font DICTs have the same size
        // whatever their offsets, so they are written once with none to know
        // where the blocks after them go, and once more with their offsets.
        boolean namePrivate = (fontDicts == null && privates.size() == 1);
        Map<Integer, Integer> offsets = topOffsets(charset, encoding, fdSelect, namePrivate, 0, 0, 0, 0, 0, 0);
        int topSize = writeDict(top, offsets, 0).length;
        List<byte[]> sizedTop = new ArrayList<byte[]>();
        sizedTop.add(new byte[topSize]);
        int at = headerSize + (names.end - names.start) + index(sizedTop).length +
                (strings.end - strings.start) + newGlobalSubrs.length;
        int charsetAt = at;
        at += (charset != null) ? charset.length : 0;
        int newEncodingAt = at;
        at += (encoding != null) ? encoding.length : 0;
        int newFDSelectAt = at;
        at += (fdSelect != null) ? fdSelect.length : 0;
        int newCharStringsAt = at;
        at += newCharStrings.length;
        int newFDArrayAt = at;
        if (fontDicts != null) {
            List<byte[]> sized = new ArrayList<byte[]>();
            for (int i = 0; i < fontDicts.size(); i++) {
                sized.add(writeDict(fontDicts.get(i), privateOffset(0), dictSizes[i]));
            }
            at += index(sized).length;
        }
        int[] privateAts = new int[blocks.length];
        for (int i = 0; i < blocks.length; i++) {
            privateAts[i] = at;
            at += blocks[i].length;
        }
        byte[] fdArray = new byte[0];
        if (fontDicts != null) {
            List<byte[]> dicts = new ArrayList<byte[]>();
            for (int i = 0; i < fontDicts.size(); i++) {
                dicts.add(writeDict(fontDicts.get(i), privateOffset(privateAts[i]), dictSizes[i]));
            }
            fdArray = index(dicts);
        }
        int privateAt = namePrivate ? privateAts[0] : 0;
        byte[] newTop = writeDict(top, topOffsets(charset, encoding, fdSelect, namePrivate,
                charsetAt, newEncodingAt, newFDSelectAt, newCharStringsAt, newFDArrayAt, privateAt),
                namePrivate ? dictSizes[0] : 0);

        ByteBuffer out = new ByteBuffer();
        out.add(Arrays.copyOfRange(cff, 0, headerSize));
        out.add(Arrays.copyOfRange(cff, names.start, names.end));
        List<byte[]> tops = new ArrayList<byte[]>();
        tops.add(newTop);
        appendIndex(out, tops);
        out.add(Arrays.copyOfRange(cff, strings.start, strings.end));
        out.add(newGlobalSubrs);
        if (charset != null) {
            out.add(charset);
        }
        if (encoding != null) {
            out.add(encoding);
        }
        if (fdSelect != null) {
            out.add(fdSelect);
        }
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

    private static Map<Integer, Integer> topOffsets(byte[] charset, byte[] encoding, byte[] fdSelect,
            boolean namePrivate, int charsetAt, int encodingAt, int fdSelectAt, int charStringsAt,
            int fdArrayAt, int privateAt) {
        Map<Integer, Integer> offsets = new HashMap<Integer, Integer>();
        offsets.put(CHAR_STRINGS, charStringsAt);
        if (charset != null) {
            offsets.put(CHARSET, charsetAt);
        }
        if (encoding != null) {
            offsets.put(ENCODING, encodingAt);
        }
        if (fdSelect != null) {
            offsets.put(FD_SELECT, fdSelectAt);
            offsets.put(FD_ARRAY, fdArrayAt);
        } else if (namePrivate) {
            offsets.put(PRIVATE, privateAt);
        }
        return offsets;
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
