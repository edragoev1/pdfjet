/*
 * CFFSubset.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// The subset of a font with CFF outlines, a .otf: the CFF table with the
/// charstrings of the glyphs the document does not draw emptied, and the
/// subroutines, global and local, that the glyphs kept do not call. Every glyph
/// and every subroutine keeps its number, so the glyphs kept are drawn by the
/// same bytes as in the whole font. The CFF is written again in a fixed order,
/// with every offset of its Top DICT, of the Font DICTs of a CID-keyed font and
/// of its Private DICTs in the five-byte form, so that an offset of any value
/// has the same size: the header, the Name, Top DICT, String and Global Subr
/// INDEXes, the charset, the Encoding and the FDSelect, the new CharStrings
/// INDEX, the FDArray, and each Private DICT with its local subroutines after
/// it.
/// </summary>
class CFFSubset {
    // The operators whose operands are offsets in the CFF table.
    internal const int CHARSET = 15;
    internal const int ENCODING = 16;
    internal const int CHAR_STRINGS = 17;
    internal const int PRIVATE = 18;
    internal const int SUBRS = 19;
    internal const int FD_ARRAY = 12 << 8 | 36;
    internal const int FD_SELECT = 12 << 8 | 37;
    internal const int ROS = 12 << 8 | 30;
    internal const int CID_COUNT = 12 << 8 | 34;

    // The most bytes of charstrings the subroutines are looked for in, which a
    // font of nested calls that come back to the same subroutines could
    // otherwise make take as long as it likes.
    internal const int MAX_WORK = 1 << 24;

    /// <summary>An INDEX: where it starts and ends, and the offsets of its objects.</summary>
    internal sealed class Index {
        internal readonly int start;
        internal readonly int end;
        internal readonly int[] objects;    // count+1 offsets, the last one the end of the data

        internal Index(int start, int end, int[] objects) {
            this.start = start;
            this.end = end;
            this.objects = objects;
        }

        internal int count() {
            return objects.Length - 1;
        }
    }

    /// <summary>An entry of a DICT: its operator, its bytes, its integer operands.</summary>
    internal sealed class Entry {
        internal readonly int op;           // 12 << 8 | b1 for an escaped one
        internal readonly byte[] raw;
        internal readonly int[] integers;   // null when an operand is a real

        internal Entry(int op, byte[] raw, int[] integers) {
            this.op = op;
            this.raw = raw;
            this.integers = integers;
        }
    }

    /// <summary>A Private DICT, its entries and the INDEX of its subroutines or null.</summary>
    internal sealed class PrivateDict {
        internal readonly List<Entry> entries;
        internal readonly Index subrs;

        internal PrivateDict(List<Entry> entries, Index subrs) {
            this.entries = entries;
            this.subrs = subrs;
        }
    }

    internal static Index readIndex(byte[] cff, int at) {
        if (at < 0 || at + 2 > cff.Length) {
            throw new Subset.NotSubset();
        }
        int count = (cff[at] & 0xFF) << 8 | (cff[at + 1] & 0xFF);
        if (count == 0) {
            return new Index(at, at + 2, new int[] {at + 2});
        }
        if (at + 3 > cff.Length) {
            throw new Subset.NotSubset();
        }
        int offSize = cff[at + 2] & 0xFF;
        if (offSize < 1 || offSize > 4 || (long) at + 3 + (long) (count + 1) * offSize > cff.Length) {
            throw new Subset.NotSubset();
        }
        int data = at + 3 + (count + 1) * offSize - 1;  // The offsets count from 1.
        int[] objects = new int[count + 1];
        for (int i = 0; i < objects.Length; i++) {
            long offset = 0;
            for (int j = 0; j < offSize; j++) {
                offset = offset << 8 | (long) cff[at + 3 + i * offSize + j];
            }
            if (offset < 1 || data + offset > cff.Length || (i > 0 && data + offset < objects[i - 1])) {
                throw new Subset.NotSubset();
            }
            objects[i] = (int) (data + offset);
        }
        return new Index(at, objects[count], objects);
    }

    internal static void appendIndex(ByteBuffer output, List<byte[]> objects) {
        output.Add2(objects.Count);
        if (objects.Count == 0) {
            return;
        }
        int size = 1;
        foreach (byte[] obj in objects) {
            size += obj.Length;
        }
        int offSize = 1;
        while (offSize < 4 && size >= 1 << (8 * offSize)) {
            offSize++;
        }
        output.Add(offSize);
        int offset = 1;
        for (int i = 0; i <= objects.Count; i++) {
            for (int j = offSize - 1; j >= 0; j--) {
                output.Add(offset >> (8 * j));
            }
            if (i < objects.Count) {
                offset += objects[i].Length;
            }
        }
        foreach (byte[] obj in objects) {
            output.Add(obj);
        }
    }

    internal static byte[] index(List<byte[]> objects) {
        ByteBuffer output = new ByteBuffer();
        appendIndex(output, objects);
        return output.ToBytes();
    }

    internal static List<Entry> readDict(byte[] cff, int from, int to) {
        List<Entry> entries = new List<Entry>();
        int start = from;
        List<int> integers = new List<int>();
        bool isInteger = true;
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
                    values = new int[integers.Count];
                    for (int k = 0; k < values.Length; k++) {
                        values[k] = integers[k];
                    }
                }
                entries.Add(new Entry(op, Slice(cff, start, i), values));
                start = i;
                integers.Clear();
                isInteger = true;
            } else if (b0 == 28) {
                if (i + 3 > to) {
                    throw new Subset.NotSubset();
                }
                integers.Add((int) (short) ((cff[i + 1] & 0xFF) << 8 | (cff[i + 2] & 0xFF)));
                i += 3;
            } else if (b0 == 29) {
                if (i + 5 > to) {
                    throw new Subset.NotSubset();
                }
                integers.Add((cff[i + 1] & 0xFF) << 24 | (cff[i + 2] & 0xFF) << 16 |
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
                integers.Add(b0 - 139);
                i++;
            } else if (b0 >= 247 && b0 <= 250) {
                if (i + 2 > to) {
                    throw new Subset.NotSubset();
                }
                integers.Add((b0 - 247) * 256 + (cff[i + 1] & 0xFF) + 108);
                i += 2;
            } else if (b0 >= 251 && b0 <= 254) {
                if (i + 2 > to) {
                    throw new Subset.NotSubset();
                }
                integers.Add(-(b0 - 251) * 256 - (cff[i + 1] & 0xFF) - 108);
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

    internal static int[] entryOf(List<Entry> entries, int op) {
        foreach (Entry entry in entries) {
            if (entry.op == op) {
                return entry.integers;
            }
        }
        return null;
    }

    internal static void appendOffset(ByteBuffer output, int value) {
        output.Add(29);
        output.Add(value >> 24);
        output.Add(value >> 16);
        output.Add(value >> 8);
        output.Add(value);
    }

    // Writes the entries again, the operands of the operators in offsets in
    // the five-byte form: the offset, or for Private its size and its offset.
    internal static byte[] writeDict(List<Entry> entries, Dictionary<int, int> offsets, int privateSize) {
        ByteBuffer output = new ByteBuffer();
        foreach (Entry entry in entries) {
            int offset;
            if (!offsets.TryGetValue(entry.op, out offset)) {
                output.Add(entry.raw);
                continue;
            }
            if (entry.op == PRIVATE) {
                appendOffset(output, privateSize);
            }
            appendOffset(output, offset);
            if (entry.op > 0xFF) {
                output.Add(12);
                output.Add(entry.op & 0xFF);
            } else {
                output.Add(entry.op);
            }
        }
        return output.ToBytes();
    }

    internal static int charsetLength(byte[] cff, int at, int numGlyphs) {
        if (at < 0 || at >= cff.Length) {
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
                if (i + 2 + size > cff.Length) {
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

    internal static int encodingLength(byte[] cff, int at) {
        if (at < 0 || at + 2 > cff.Length) {
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
            if (at + length >= cff.Length) {
                throw new Subset.NotSubset();
            }
            length += 1 + 3 * (cff[at + length] & 0xFF);
        }
        return length;
    }

    internal static int fdSelectLength(byte[] cff, int at, int numGlyphs) {
        if (at < 0 || at >= cff.Length) {
            throw new Subset.NotSubset();
        }
        if (cff[at] == 0) {
            return 1 + numGlyphs;
        }
        if (cff[at] == 3) {
            if (at + 3 > cff.Length) {
                throw new Subset.NotSubset();
            }
            int ranges = (cff[at + 1] & 0xFF) << 8 | (cff[at + 2] & 0xFF);
            return 1 + 2 + 3 * ranges + 2;
        }
        throw new Subset.NotSubset();
    }

    internal static byte[] block(byte[] cff, int at, int length) {
        if (at < 0 || length < 0 || (long) at + length > cff.Length) {
            throw new Subset.NotSubset();
        }
        return Slice(cff, at, at + length);
    }

    internal static PrivateDict readPrivate(byte[] cff, int size, int at) {
        block(cff, at, size);
        List<Entry> entries = readDict(cff, at, at + size);
        int[] subrs = entryOf(entries, SUBRS);
        if (subrs == null) {
            return new PrivateDict(entries, null);
        }
        if (subrs.Length != 1) {
            throw new Subset.NotSubset();
        }
        return new PrivateDict(entries, readIndex(cff, at + subrs[0]));
    }

    internal static int bias(int count) {
        if (count < 1240) {
            return 107;
        } else if (count < 33900) {
            return 1131;
        }
        return 32768;
    }

    /// <summary>
    /// Finds the subroutines that the charstrings of the glyphs kept call,
    /// directly or through other subroutines.
    /// </summary>
    internal sealed class Usage {
        internal readonly byte[] cff;
        internal readonly Index global;
        internal readonly bool[] usedG;
        internal Index local;
        internal bool[] usedL;
        internal int stack;      // The operands on the stack
        internal int last;       // The last of them, a subroutine number when one is called
        internal int stems;
        internal int work;
        internal bool failed;
        internal bool stopped;    // By endchar

        internal Usage(byte[] cff, Index global, bool[] usedG) {
            this.cff = cff;
            this.global = global;
            this.usedG = usedG;
        }

        internal void run(int from, int to, int depth) {
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
                    if (i + 3 > to) {
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
                    if (i + 2 > to) {
                        failed = true;
                        return;
                    }
                    last = (b0 - 247) * 256 + (cff[i + 1] & 0xFF) + 108;
                    stack++;
                    i += 2;
                } else if (b0 >= 251 && b0 <= 254) {
                    if (i + 2 > to) {
                        failed = true;
                        return;
                    }
                    last = -(b0 - 251) * 256 - (cff[i + 1] & 0xFF) - 108;
                    stack++;
                    i += 2;
                } else if (b0 == 255) {     // A 16.16 fixed number
                    if (i + 5 > to) {
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
                    bool[] used = (b0 == 29) ? usedG : usedL;
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
                        // would have to be kept too.
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
    internal static List<byte[]> emptied(byte[] cff, Index index, bool[] used, int empty) {
        List<byte[]> objects = new List<byte[]>(index.count());
        for (int i = 0; i < index.count(); i++) {
            if (used == null || used[i]) {
                objects.Add(Slice(cff, index.objects[i], index.objects[i + 1]));
            } else {
                objects.Add(new byte[] {(byte) empty});
            }
        }
        return objects;
    }

    // Returns the Font DICT of each glyph, from the FDSelect of a CID-keyed
    // font, or null for a name-keyed one.
    internal static int[] fdOf(byte[] fdSelect, int numGlyphs, int fds) {
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
        foreach (int fd in fdOf) {
            if (fd >= fds) {
                throw new Subset.NotSubset();
            }
        }
        return fdOf;
    }

    /// <summary>
    /// Returns the CFF table with the charstrings of the glyphs not used
    /// emptied, and the subroutines they do not call, and gives in kept the
    /// glyphs kept: those used and glyph 0, or every glyph for used null, which
    /// writes the table again with nothing left out, for the identity charset
    /// of a CID-keyed font. A table that cannot be read throws NotSubset.
    /// </summary>
    internal static byte[] subset(byte[] cff, bool[] used, out bool[] kept) {
        if (cff.Length < 4 || cff[0] != 1) {
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
        if (charStringsAt == null || charStringsAt.Length != 1) {
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
            if ((count != null && (count.Length != 1 || count[0] < numGlyphs)) ||
                    (count == null && numGlyphs > 8720)) {  // The default CIDCount
                throw new Subset.NotSubset();
            }
            charset = new byte[] {0};   // Format 0, for .notdef alone
            if (numGlyphs > 1) {
                // Format 2, one range: CIDs 1 to numGlyphs-1.
                charset = new byte[] {2, 0, 1, (byte) ((numGlyphs - 2) >> 8), (byte) (numGlyphs - 2)};
            }
        } else {
            int[] charsetOffset = entryOf(top, CHARSET);
            if (charsetOffset != null && charsetOffset.Length == 1 && charsetOffset[0] > 2) {
                charset = block(cff, charsetOffset[0], charsetLength(cff, charsetOffset[0], numGlyphs));
            }
        }
        int[] encodingAt = entryOf(top, ENCODING);
        if (encodingAt != null && encodingAt.Length == 1 && encodingAt[0] > 1) {
            encoding = block(cff, encodingAt[0], encodingLength(cff, encodingAt[0]));
        }
        int[] fdSelectAt = entryOf(top, FD_SELECT);
        if (fdSelectAt != null) {
            if (fdSelectAt.Length != 1) {
                throw new Subset.NotSubset();
            }
            fdSelect = block(cff, fdSelectAt[0], fdSelectLength(cff, fdSelectAt[0], numGlyphs));
        }

        // The Private DICT of a name-keyed font, or the Font DICTs of a
        // CID-keyed one, each with its Private DICT.
        List<PrivateDict> privates = new List<PrivateDict>();
        List<List<Entry>> fontDicts = null;
        int[] fdArrayAt = entryOf(top, FD_ARRAY);
        if (fdArrayAt != null) {
            if (fdArrayAt.Length != 1 || fdSelect == null) {
                throw new Subset.NotSubset();
            }
            Index fdIndex = readIndex(cff, fdArrayAt[0]);
            fontDicts = new List<List<Entry>>();
            for (int i = 0; i < fdIndex.count(); i++) {
                List<Entry> entries = readDict(cff, fdIndex.objects[i], fdIndex.objects[i + 1]);
                int[] p = entryOf(entries, PRIVATE);
                if (p == null || p.Length != 2) {
                    throw new Subset.NotSubset();
                }
                fontDicts.Add(entries);
                privates.Add(readPrivate(cff, p[0], p[1]));
            }
        } else {
            int[] p = entryOf(top, PRIVATE);
            if (p != null) {
                if (p.Length != 2) {
                    throw new Subset.NotSubset();
                }
                privates.Add(readPrivate(cff, p[0], p[1]));
            }
        }

        // The charstrings, of the glyphs kept as they were, of the others
        // endchar.
        bool[] keep = new bool[numGlyphs];
        List<byte[]> glyphs = new List<byte[]>(numGlyphs);
        for (int gid = 0; gid < numGlyphs; gid++) {
            if (used == null || gid == 0 || (gid < used.Length && used[gid])) {
                keep[gid] = true;
                glyphs.Add(Slice(cff, charStrings.objects[gid], charStrings.objects[gid + 1]));
            } else {
                glyphs.Add(new byte[] {14});
            }
        }
        byte[] newCharStrings = index(glyphs);

        // The subroutines the glyphs kept call, the others emptied to return.
        // A font whose charstrings cannot be followed keeps them all.
        int[] fdOfGlyph = fdOf(fdSelect, numGlyphs, privates.Count);
        bool[] usedG = new bool[globalSubrs.count()];
        bool[][] usedL = new bool[privates.Count][];
        for (int i = 0; i < privates.Count; i++) {
            if (privates[i].subrs != null) {
                usedL[i] = new bool[privates[i].subrs.count()];
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
            int fd = (fdOfGlyph != null) ? fdOfGlyph[gid] : 0;
            usage.local = null;
            usage.usedL = null;
            if (fd < privates.Count && privates[fd].subrs != null) {
                usage.local = privates[fd].subrs;
                usage.usedL = usedL[fd];
            }
            usage.stack = 0;
            usage.stems = 0;
            usage.stopped = false;
            usage.run(charStrings.objects[gid], charStrings.objects[gid + 1], 0);
        }
        if (usage.failed) {
            if (usage.work > MAX_WORK) {
                throw new Subset.NotSubset();
            }
            usedG = null;   // Kept whole
            for (int i = 0; i < usedL.Length; i++) {
                usedL[i] = null;
            }
        }
        byte[] newGlobalSubrs = index(emptied(cff, globalSubrs, usedG, 11));

        // Each Private DICT is written again with its local subroutines right
        // after it, at its size, whatever they were before.
        byte[][] blocks = new byte[privates.Count][];
        int[] dictSizes = new int[privates.Count];
        for (int i = 0; i < privates.Count; i++) {
            PrivateDict p = privates[i];
            Dictionary<int, int> subrs = new Dictionary<int, int>();
            if (p.subrs != null) {
                subrs[SUBRS] = 0;
            }
            dictSizes[i] = writeDict(p.entries, subrs, 0).Length;
            if (p.subrs != null) {
                subrs[SUBRS] = dictSizes[i];
            }
            ByteBuffer b = new ByteBuffer();
            b.Add(writeDict(p.entries, subrs, 0));
            if (p.subrs != null) {
                appendIndex(b, emptied(cff, p.subrs, usedL[i], 11));
            }
            blocks[i] = b.ToBytes();
        }

        // The layout. The Top DICT and the Font DICTs have the same size
        // whatever their offsets, so they are written once with none to know
        // where the blocks after them go, and once more with their offsets.
        bool namePrivate = (fontDicts == null && privates.Count == 1);
        Dictionary<int, int> offsets = topOffsets(charset, encoding, fdSelect, namePrivate, 0, 0, 0, 0, 0, 0);
        int topSize = writeDict(top, offsets, 0).Length;
        List<byte[]> sizedTop = new List<byte[]>();
        sizedTop.Add(new byte[topSize]);
        int at = headerSize + (names.end - names.start) + index(sizedTop).Length +
                (strings.end - strings.start) + newGlobalSubrs.Length;
        int charsetAt = at;
        at += (charset != null) ? charset.Length : 0;
        int newEncodingAt = at;
        at += (encoding != null) ? encoding.Length : 0;
        int newFDSelectAt = at;
        at += (fdSelect != null) ? fdSelect.Length : 0;
        int newCharStringsAt = at;
        at += newCharStrings.Length;
        int newFDArrayAt = at;
        if (fontDicts != null) {
            List<byte[]> sized = new List<byte[]>();
            for (int i = 0; i < fontDicts.Count; i++) {
                sized.Add(writeDict(fontDicts[i], privateOffset(0), dictSizes[i]));
            }
            at += index(sized).Length;
        }
        int[] privateAts = new int[blocks.Length];
        for (int i = 0; i < blocks.Length; i++) {
            privateAts[i] = at;
            at += blocks[i].Length;
        }
        byte[] fdArray = new byte[0];
        if (fontDicts != null) {
            List<byte[]> dicts = new List<byte[]>();
            for (int i = 0; i < fontDicts.Count; i++) {
                dicts.Add(writeDict(fontDicts[i], privateOffset(privateAts[i]), dictSizes[i]));
            }
            fdArray = index(dicts);
        }
        int privateAt = namePrivate ? privateAts[0] : 0;
        byte[] newTop = writeDict(top, topOffsets(charset, encoding, fdSelect, namePrivate,
                charsetAt, newEncodingAt, newFDSelectAt, newCharStringsAt, newFDArrayAt, privateAt),
                namePrivate ? dictSizes[0] : 0);

        ByteBuffer output = new ByteBuffer();
        output.Add(Slice(cff, 0, headerSize));
        output.Add(Slice(cff, names.start, names.end));
        List<byte[]> tops = new List<byte[]>();
        tops.Add(newTop);
        appendIndex(output, tops);
        output.Add(Slice(cff, strings.start, strings.end));
        output.Add(newGlobalSubrs);
        if (charset != null) {
            output.Add(charset);
        }
        if (encoding != null) {
            output.Add(encoding);
        }
        if (fdSelect != null) {
            output.Add(fdSelect);
        }
        output.Add(newCharStrings);
        output.Add(fdArray);
        foreach (byte[] b in blocks) {
            output.Add(b);
        }
        byte[] result = output.ToBytes();
        if (result.Length != at) {
            throw new Subset.NotSubset();
        }
        kept = keep;
        return result;
    }

    private static Dictionary<int, int> privateOffset(int at) {
        Dictionary<int, int> offsets = new Dictionary<int, int>();
        offsets[PRIVATE] = at;
        return offsets;
    }

    private static Dictionary<int, int> topOffsets(byte[] charset, byte[] encoding, byte[] fdSelect,
            bool namePrivate, int charsetAt, int encodingAt, int fdSelectAt, int charStringsAt,
            int fdArrayAt, int privateAt) {
        Dictionary<int, int> offsets = new Dictionary<int, int>();
        offsets[CHAR_STRINGS] = charStringsAt;
        if (charset != null) {
            offsets[CHARSET] = charsetAt;
        }
        if (encoding != null) {
            offsets[ENCODING] = encodingAt;
        }
        if (fdSelect != null) {
            offsets[FD_SELECT] = fdSelectAt;
            offsets[FD_ARRAY] = fdArrayAt;
        } else if (namePrivate) {
            offsets[PRIVATE] = privateAt;
        }
        return offsets;
    }

    private static byte[] Slice(byte[] buf, int from, int to) {
        byte[] slice = new byte[to - from];
        Array.Copy(buf, from, slice, 0, to - from);
        return slice;
    }

    private static byte[] Resize(byte[] buf, int length) {
        byte[] resized = new byte[length];
        Array.Copy(buf, 0, resized, 0, Math.Min(buf.Length, length));
        return resized;
    }

    /// <summary>A growing array of bytes.</summary>
    internal sealed class ByteBuffer {
        private byte[] buf = new byte[256];
        private int size;

        private void grow(int n) {
            if (size + n > buf.Length) {
                buf = Resize(buf, Math.Max(buf.Length * 2, size + n));
            }
        }

        internal void Add(int b) {
            grow(1);
            buf[size++] = (byte) b;
        }

        internal void Add2(int v) {
            Add(v >> 8);
            Add(v);
        }

        internal void Add(byte[] bytes) {
            grow(bytes.Length);
            Array.Copy(bytes, 0, buf, size, bytes.Length);
            size += bytes.Length;
        }

        internal byte[] ToBytes() {
            return Resize(buf, size);
        }
    }
}   // End of CFFSubset.cs
}   // End of namespace PDFjet.NET
