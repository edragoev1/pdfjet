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
/// The subset of a font with CFF outlines, a .otf: a CIDFont of the glyphs the
/// document draws alone, its charset giving each glyph its number in the whole
/// font as its CID, by which the PDF draws it, each glyph written with the
/// subroutines it calls, global and local, in place of its calls, and no
/// subroutines. A glyph that computes a number keeps the subroutines it calls
/// instead, the others emptied to no bytes, keeping their numbers. A
/// name-keyed font becomes a CIDFont of Adobe-Identity-0, with the identity
/// matrix in its one Font DICT and its own, or the default, in its Top DICT.
/// The CFF is written again in a fixed order, with every offset of its Top
/// DICT and of its Font DICTs in the five-byte form, so that an offset of any
/// value has the same size: the header, the Name, Top DICT, String and Global
/// Subr INDEXes, the charset, the FDSelect, the new CharStrings INDEX, the
/// FDArray, and each Private DICT with its local subroutines, if any.
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
    internal const int FONT_MATRIX = 12 << 8 | 7;

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
        // The bounds checked as at > length - n, as at + n overflows an int
        // for an offset of the file near 2^31 (the review of 9 October 2026)
        if (at < 0 || at > cff.Length - 2) {
            throw new Subset.NotSubset();
        }
        int count = (cff[at] & 0xFF) << 8 | (cff[at + 1] & 0xFF);
        if (count == 0) {
            return new Index(at, at + 2, new int[] {at + 2});
        }
        if (at > cff.Length - 3) {
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

    internal static int fdSelectLength(byte[] cff, int at, int numGlyphs) {
        if (at < 0 || at >= cff.Length) {
            throw new Subset.NotSubset();
        }
        if (cff[at] == 0) {
            return 1 + numGlyphs;
        }
        if (cff[at] == 3) {
            if (at > cff.Length - 3) {
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
        // The offset from the Private DICT, past an int refused rather than
        // wrapped round (the review of 9 October 2026)
        if (subrs.Length != 1 || (long) at + subrs[0] > int.MaxValue) {
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
        internal bool seac;       // An accented letter drawn from two others

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

    /// <summary>
    /// Writes charstrings again with each subroutine they call, local or
    /// global, in place of its call (Technical Note #5177): the number of the
    /// subroutine taken off the end, the bytes of the subroutine, its own calls
    /// in place too, without its return. The charstring written draws the
    /// glyph as the operators of the one it was written from do, run with its
    /// subroutines, one after another. A charstring that computes a number, by
    /// an arithmetic or storage operator, cannot be written so: failed.
    /// </summary>
    internal sealed class Inliner {
        internal readonly byte[] cff;
        internal readonly Index global;
        internal Index local;
        internal byte[] output;     // The charstrings written, one after another
        internal int length;        // The bytes of output written
        internal readonly int[] numbers = new int[48];  // Where each operand on the stack starts in output
        internal int stack;         // The operands on the stack, of the 48 of Type 2 at most
        internal int stems;
        internal int work;
        internal bool ended;        // By endchar
        internal bool failed;
        internal bool seac;

        internal Inliner(byte[] cff, Index global, int capacity) {
            this.cff = cff;
            this.global = global;
            this.output = new byte[Math.Max(capacity, 16)];
        }

        // Returns the charstring written again with its subroutines in place,
        // after the charstrings written before it in output, which holds them
        // all.
        internal byte[] glyph(byte[] cs) {
            int start = length;
            stack = 0;
            stems = 0;
            ended = false;
            run(cs, 0, cs.Length, 0);
            return Slice(output, start, length);
        }

        private void append(byte[] buf, int from, int to) {
            int n = to - from;
            if (length + n > output.Length) {
                output = Resize(output, Math.Max(output.Length * 2, length + n));
            }
            Array.Copy(buf, from, output, length, n);
            length += n;
        }

        // Writes the charstring, the bytes of cs from start to end, and the
        // subroutines it calls in place. Its bytes are written in runs, from
        // one call of a subroutine to the next.
        internal void run(byte[] cs, int start, int end, int depth) {
            if (depth > 10) {   // Type 2 nests subroutines 10 deep at most.
                failed = true;
                return;
            }
            int from = start;   // The bytes of the charstring from here on are not written yet.
            int i = start;
            while (i < end) {
                int b0 = cs[i] & 0xFF;
                if (b0 >= 28 && b0 != 29 && b0 != 30 && b0 != 31) {    // A number
                    int k = 1;
                    if (b0 == 28) {
                        k = 3;
                    } else if (b0 == 255) {
                        k = 5;
                    } else if (b0 >= 247) {
                        k = 2;
                    }
                    if (i + k > end || stack == numbers.Length) {
                        failed = true;
                        return;
                    }
                    numbers[stack] = length + i - from;
                    stack++;
                    i += k;
                    continue;
                }
                int n = 1;  // The bytes of the operator
                if (b0 == 1 || b0 == 3 || b0 == 18 || b0 == 23) {   // The stem hints
                    stems += stack / 2;
                } else if (b0 == 19 || b0 == 20) {  // hintmask and cntrmask, and their mask
                    stems += stack / 2;
                    n += (stems + 7) / 8;
                } else if (b0 == 10 || b0 == 29) {  // callsubr and callgsubr
                    if (stack == 0) {
                        failed = true;
                        return;
                    }
                    append(cs, from, i);
                    stack--;
                    int at = numbers[stack];
                    int last = number(output, at);
                    length = at;
                    Index subrs = (b0 == 29) ? global : local;
                    if (subrs == null) {
                        failed = true;
                        return;
                    }
                    int count = subrs.count();
                    int subr = last + bias(count);
                    if (subr < 0 || subr >= count) {
                        failed = true;
                        return;
                    }
                    work += subrs.objects[subr + 1] - subrs.objects[subr];
                    if (work > MAX_WORK) {
                        failed = true;
                        return;
                    }
                    run(cff, subrs.objects[subr], subrs.objects[subr + 1], depth + 1);
                    if (failed || ended) {
                        return;
                    }
                    i++;
                    from = i;
                    continue;
                } else if (b0 == 11) {      // return
                    append(cs, from, i);
                    return;
                } else if (b0 == 14) {      // endchar
                    if (stack >= 4) {
                        // seac, an accented letter drawn from two others: the
                        // font is embedded whole
                        seac = true;
                        failed = true;
                        return;
                    }
                    append(cs, from, i + 1);
                    ended = true;
                    return;
                } else if (b0 == 12) {
                    n = 2;
                    if (i + 1 < end) {
                        switch (cs[i + 1] & 0xFF) {
                        case 3: case 4: case 5: case 9: case 10: case 11: case 12: case 14: case 15:
                        case 18: case 20: case 21: case 22: case 23: case 24: case 26: case 27:
                        case 28: case 29: case 30:
                            // The arithmetic and storage operators, which leave
                            // numbers on the stack
                            failed = true;
                            return;
                        }
                    }
                }
                if (i + n > end) {
                    failed = true;
                    return;
                }
                // An operator, which takes its operands off the stack
                stack = 0;
                i += n;
            }
            append(cs, from, i);
        }
    }

    // Returns the integer part of the number of a charstring the bytes start
    // with at.
    internal static int number(byte[] b, int at) {
        int b0 = b[at] & 0xFF;
        if (b0 == 28) {
            return (short) ((b[at + 1] & 0xFF) << 8 | (b[at + 2] & 0xFF));
        } else if (b0 >= 32 && b0 <= 246) {
            return b0 - 139;
        } else if (b0 >= 247 && b0 <= 250) {
            return (b0 - 247) * 256 + (b[at + 1] & 0xFF) + 108;
        } else if (b0 >= 251 && b0 <= 254) {
            return -(b0 - 251) * 256 - (b[at + 1] & 0xFF) - 108;
        }
        return ((b[at + 1] & 0xFF) << 24 | (b[at + 2] & 0xFF) << 16 |
                (b[at + 3] & 0xFF) << 8 | (b[at + 4] & 0xFF)) >> 16;
    }

    // The FontMatrix entry [1 0 0 1 0 0].
    internal static readonly byte[] IDENTITY_MATRIX = {140, 139, 139, 140, 139, 139, 12, 7};

    // The FontMatrix entry [0.001 0 0 0.001 0 0], the default of a Top DICT,
    // its reals written as 1E-3.
    internal static readonly byte[] THOUSANDTH_MATRIX = {30, 0x1c, 0x3f, 139, 139, 30, 0x1c, 0x3f, 139, 139, 12, 7};

    // Returns the objects of the INDEX, those not used emptied to no bytes,
    // which keeps their numbers and makes their offsets the same as the next
    // one's, or every one for used null.
    internal static List<byte[]> emptied(byte[] cff, Index index, bool[] used) {
        List<byte[]> objects = new List<byte[]>(index.count());
        for (int i = 0; i < index.count(); i++) {
            if (used == null || used[i]) {
                objects.Add(Slice(cff, index.objects[i], index.objects[i + 1]));
            } else {
                objects.Add(new byte[0]);
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
    /// Returns the CFF table of the glyphs used and glyph 0, or of every glyph
    /// for used null, and gives in kept the glyphs kept, by their numbers in
    /// the font. The subset is a CIDFont of the glyphs kept alone, in the order
    /// of their numbers, its charset giving each its number in the font as its
    /// CID: the PDF draws a glyph of a CIDFontType0C program by its CID, which
    /// the charset maps to the glyph (ISO 32000-2, 9.7.4.2; Technical Note
    /// #5176, section 18, "Subset CIDFonts will generally need to use a more
    /// complex charset table representing a non-identity mapping"), so the
    /// content streams, the widths and the ToUnicode map stay those of the
    /// whole font. Each glyph kept is written with the subroutines it calls in
    /// place of its calls, and the subset has no subroutines; a glyph that
    /// computes a number, which cannot be written so, keeps the subroutines
    /// called, the others emptied to no bytes, keeping their numbers. The whole
    /// font keeps its subroutines. The Font DICTs no glyph kept uses are left
    /// out. Every INDEX has the least offSize its offsets need; the offsets of
    /// the Top DICT and of the Font DICTs are in the five-byte form.
    ///
    /// A name-keyed font is written as a CIDFont of the ordering
    /// Adobe-Identity-0 whose one Font DICT has its Private DICT and the
    /// FontMatrix [1 0 0 1 0 0], and its Top DICT the font's FontMatrix, its
    /// own, as Cascadia Mono's of 2048 units to the em, or the default,
    /// [0.001 0 0 0.001 0 0], written: the glyph space of a CIDFont is that of
    /// the FontMatrix of the glyph's Font DICT concatenated with the Top
    /// DICT's (PostScript Language Reference, third edition, 5.11.3, Table
    /// 5.14), the Top DICT's interpreting the coordinates of the CIDFont as a
    /// whole, as its FontBBox. Both are written, as each has a default that a
    /// reader may take otherwise: a Font DICT of no FontMatrix is taken as the
    /// identity, though Technical Note #5176 gives a FontMatrix no default but
    /// [0.001 0 0 0.001 0 0] (Table 9), and a CIDFont whose Top DICT has none
    /// has its Font DICTs' matrices taken as the whole ones (the PostScript
    /// Language Reference, and MuPDF, Poppler, PDFium and pdf.js). A CID-keyed
    /// font's matrices are kept as they are. A table that cannot be read
    /// throws NotSubset.
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

        bool cidKeyed = entryOf(top, ROS) != null;
        byte[] fdSelect = null;
        if (cidKeyed) {
            int[] count = entryOf(top, CID_COUNT);
            if (count != null && (count.Length != 1 || count[0] < numGlyphs)) {
                throw new Subset.NotSubset();
            }
            if (count == null && numGlyphs > 8720) {
                throw new Subset.NotSubset();   // The default CIDCount
            }
            int[] fdSelectAt = entryOf(top, FD_SELECT);
            if (fdSelectAt == null || fdSelectAt.Length != 1) {
                throw new Subset.NotSubset();
            }
            fdSelect = block(cff, fdSelectAt[0], fdSelectLength(cff, fdSelectAt[0], numGlyphs));
        }

        // The Font DICTs of a CID-keyed font, each with its Private DICT, or
        // the Private DICT of a name-keyed one, which becomes its one Font
        // DICT's.
        List<PrivateDict> privates = new List<PrivateDict>();
        List<List<Entry>> fontDicts = new List<List<Entry>>();
        if (cidKeyed) {
            int[] fdArrayAt = entryOf(top, FD_ARRAY);
            if (fdArrayAt == null || fdArrayAt.Length != 1) {
                throw new Subset.NotSubset();
            }
            Index fdIndex = readIndex(cff, fdArrayAt[0]);
            for (int i = 0; i < fdIndex.count(); i++) {
                List<Entry> entries = readDict(cff, fdIndex.objects[i], fdIndex.objects[i + 1]);
                int[] p = entryOf(entries, PRIVATE);
                if (p == null || p.Length != 2) {
                    throw new Subset.NotSubset();
                }
                PrivateDict privateDict = readPrivate(cff, p[0], p[1]);
                fontDicts.Add(entries);
                privates.Add(privateDict);
            }
        } else {
            int[] p = entryOf(top, PRIVATE);
            if (p == null || p.Length != 2) {
                throw new Subset.NotSubset();
            }
            PrivateDict privateDict = readPrivate(cff, p[0], p[1]);
            // Its one Font DICT has the identity matrix, written, and its Top
            // DICT its FontMatrix, its own or the default, written.
            List<Entry> fontDict = new List<Entry>();
            fontDict.Add(new Entry(FONT_MATRIX, IDENTITY_MATRIX, null));
            fontDict.Add(new Entry(PRIVATE, null, null));
            fontDicts.Add(fontDict);
            privates.Add(privateDict);
        }

        // The glyphs kept, by their numbers in the font, which are their CIDs.
        bool[] keep = new bool[numGlyphs];
        List<int> keptGIDs = new List<int>();
        for (int gid = 0; gid < numGlyphs; gid++) {
            if (used == null || gid == 0 || (gid < used.Length && used[gid])) {
                keep[gid] = true;
                keptGIDs.Add(gid);
            }
        }
        List<byte[]> glyphs = new List<byte[]>(keptGIDs.Count);
        foreach (int gid in keptGIDs) {
            glyphs.Add(Slice(cff, charStrings.objects[gid], charStrings.objects[gid + 1]));
        }

        // The subroutines the glyphs kept call, the others emptied. A font
        // whose charstrings cannot be followed keeps them all.
        int[] fdOfGID = fdOf(fdSelect, numGlyphs, privates.Count);
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
        // The glyphs of a subset written with the subroutines they call in
        // place of their calls, and no subroutines then, or, when that cannot
        // be done, with the subroutines they call.
        bool glyphsInlined = false;
        if (used != null) {
            int size = 0;
            foreach (byte[] glyph in glyphs) {
                size += glyph.Length;
            }
            Inliner inlined = new Inliner(cff, globalSubrs, 3 * size);
            List<byte[]> desub = new List<byte[]>(keptGIDs.Count);
            for (int i = 0; i < keptGIDs.Count && !inlined.failed; i++) {
                int fd = (fdOfGID != null) ? fdOfGID[keptGIDs[i]] : 0;
                inlined.local = privates[fd].subrs;
                desub.Add(inlined.glyph(glyphs[i]));
            }
            if (inlined.seac) {
                throw new Subset.NotSubset();
            }
            if (!inlined.failed) {
                glyphs = desub;
                glyphsInlined = true;
                for (int i = 0; i < usedL.Length; i++) {
                    if (usedL[i] != null) {
                        usedL[i] = new bool[usedL[i].Length];
                    }
                }
            }
        }
        for (int i = 0; i < keptGIDs.Count && !usage.failed && !glyphsInlined; i++) {
            int gid = keptGIDs[i];
            int fd = (fdOfGID != null) ? fdOfGID[gid] : 0;
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
            if (usage.work > MAX_WORK || usage.seac) {
                throw new Subset.NotSubset();
            }
            usedG = null;   // Kept whole
            for (int i = 0; i < usedL.Length; i++) {
                usedL[i] = null;
            }
        }

        // The Font DICTs the glyphs kept use, in their order in the font, and
        // the new number of each.
        bool[] usedFD = new bool[privates.Count];
        foreach (int gid in keptGIDs) {
            usedFD[(fdOfGID != null) ? fdOfGID[gid] : 0] = true;
        }
        int[] newFD = new int[privates.Count];
        List<int> fds = new List<int>();
        for (int fd = 0; fd < usedFD.Length; fd++) {
            if (usedFD[fd]) {
                newFD[fd] = fds.Count;
                fds.Add(fd);
            }
        }
        int[] fdOfGlyph = new int[keptGIDs.Count];
        for (int i = 0; i < keptGIDs.Count; i++) {
            if (fdOfGID != null) {
                fdOfGlyph[i] = newFD[fdOfGID[keptGIDs[i]]];
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
        List<Entry> topEntries = new List<Entry>();
        foreach (Entry entry in top) {
            if (entry.op == CHARSET || entry.op == ENCODING || entry.op == CHAR_STRINGS ||
                    entry.op == PRIVATE || entry.op == FD_ARRAY || entry.op == FD_SELECT) {
                continue;
            }
            topEntries.Add(sids.renumber(entry));
        }
        List<List<Entry>> keptFontDicts = new List<List<Entry>>();
        foreach (int fd in fds) {
            List<Entry> entries = new List<Entry>();
            foreach (Entry entry in fontDicts[fd]) {
                entries.Add(sids.renumber(entry));
            }
            keptFontDicts.Add(entries);
        }
        if (!cidKeyed) {
            int sid = 391 + sids.kept.Count;
            sids.kept.Add(Latin1("Adobe"));
            sids.kept.Add(Latin1("Identity"));
            ByteBuffer ros = new ByteBuffer();
            appendInteger(ros, sid);
            appendInteger(ros, sid + 1);
            appendInteger(ros, 0);
            ros.Add(12);
            ros.Add(30);
            topEntries.Insert(0, new Entry(ROS, ros.ToBytes(), null));
            if (!hasEntry(top, FONT_MATRIX)) {
                topEntries.Add(new Entry(FONT_MATRIX, THOUSANDTH_MATRIX, null));
            }
            ByteBuffer count = new ByteBuffer();
            appendInteger(count, numGlyphs);
            count.Add(12);
            count.Add(34);
            topEntries.Add(new Entry(CID_COUNT, count.ToBytes(), null));
        }
        byte[] stringIndex = index(sids.kept);
        topEntries.Add(new Entry(CHARSET, null, null));
        topEntries.Add(new Entry(FD_SELECT, null, null));
        topEntries.Add(new Entry(CHAR_STRINGS, null, null));
        topEntries.Add(new Entry(FD_ARRAY, null, null));
        byte[] charset = charsetOf(keptGIDs.GetRange(1, keptGIDs.Count - 1));
        byte[] newFDSelect = fdSelectOf(fdOfGlyph);
        byte[] newCharStrings = index(glyphs);
        // An INDEX of subroutines none of which is called is left out, global
        // ones as an empty INDEX, local ones with their Subrs operator: one of
        // objects of no bytes alone is not read by FreeType
        List<byte[]> globals = emptied(cff, globalSubrs, usedG);
        if (usedG != null && !anyUsed(usedG)) {
            globals = new List<byte[]>();
        }
        byte[] newGlobalSubrs = index(globals);

        // Each Private DICT of a Font DICT kept is written again with its
        // local subroutines right after it, at its size, whatever they were
        // before.
        byte[][] blocks = new byte[fds.Count][];
        int[] dictSizes = new int[fds.Count];
        for (int i = 0; i < fds.Count; i++) {
            int fd = fds[i];
            PrivateDict p = privates[fd];
            if (p.subrs != null && usedL[fd] != null && !anyUsed(usedL[fd])) {
                List<Entry> entries = new List<Entry>();
                foreach (Entry entry in p.entries) {
                    if (entry.op != SUBRS) {
                        entries.Add(entry);
                    }
                }
                p = new PrivateDict(entries, null);
            }
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
                appendIndex(b, emptied(cff, p.subrs, usedL[fd]));
            }
            blocks[i] = b.ToBytes();
        }

        // The layout. The Top DICT and the Font DICTs have the same size
        // whatever their offsets, so they are written once with none to know
        // where the blocks after them go, and once more with their offsets.
        int topSize = writeDict(topEntries, topOffsets(0, 0, 0, 0), 0).Length;
        List<byte[]> sizedTop = new List<byte[]>();
        sizedTop.Add(new byte[topSize]);
        int at = headerSize + nameIndex.Length + index(sizedTop).Length +
                stringIndex.Length + newGlobalSubrs.Length;
        int charsetAt = at;
        at += charset.Length;
        int newFDSelectAt = at;
        at += newFDSelect.Length;
        int newCharStringsAt = at;
        at += newCharStrings.Length;
        int newFDArrayAt = at;
        List<byte[]> sized = new List<byte[]>();
        for (int i = 0; i < fds.Count; i++) {
            sized.Add(writeDict(keptFontDicts[i], privateOffset(0), dictSizes[i]));
        }
        at += index(sized).Length;
        int[] privateAts = new int[fds.Count];
        for (int i = 0; i < blocks.Length; i++) {
            privateAts[i] = at;
            at += blocks[i].Length;
        }
        List<byte[]> dicts = new List<byte[]>();
        for (int i = 0; i < fds.Count; i++) {
            dicts.Add(writeDict(keptFontDicts[i], privateOffset(privateAts[i]), dictSizes[i]));
        }
        byte[] fdArray = index(dicts);
        byte[] newTop = writeDict(topEntries,
                topOffsets(charsetAt, newFDSelectAt, newCharStringsAt, newFDArrayAt), 0);

        ByteBuffer output = new ByteBuffer();
        output.Add(Slice(cff, 0, headerSize));
        output.Add(nameIndex);
        List<byte[]> tops = new List<byte[]>();
        tops.Add(newTop);
        appendIndex(output, tops);
        output.Add(stringIndex);
        output.Add(newGlobalSubrs);
        output.Add(charset);
        output.Add(newFDSelect);
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

    private static Dictionary<int, int> topOffsets(int charsetAt, int fdSelectAt, int charStringsAt,
            int fdArrayAt) {
        Dictionary<int, int> offsets = new Dictionary<int, int>();
        offsets[CHARSET] = charsetAt;
        offsets[FD_SELECT] = fdSelectAt;
        offsets[CHAR_STRINGS] = charStringsAt;
        offsets[FD_ARRAY] = fdArrayAt;
        return offsets;
    }

    // The operators of the Top DICT and of the Font DICTs whose first operands
    // are SIDs, and how many: version, Notice, FullName, FamilyName, Weight,
    // Copyright, PostScript, BaseFontName, ROS and FontName.
    private static readonly Dictionary<int, int> SID_OPERANDS = new Dictionary<int, int> {
        {0, 1}, {1, 1}, {2, 1}, {3, 1}, {4, 1}, {12 << 8, 1}, {12 << 8 | 21, 1}, {12 << 8 | 22, 1},
        {ROS, 2}, {12 << 8 | 38, 1}
    };

    /// <summary>
    /// Gives the strings of the font that the entries name new SIDs, in the
    /// order they are first named, after the standard strings.
    /// </summary>
    internal sealed class Strings {
        private readonly List<byte[]> old;      // The String INDEX of the font
        // The strings named, in the order of their new SIDs
        internal readonly List<byte[]> kept = new List<byte[]>();
        // The new SID of each SID of the font named
        private readonly Dictionary<int, int> sids = new Dictionary<int, int>();

        internal Strings(List<byte[]> old) {
            this.old = old;
        }

        // Returns the entry with its SIDs of the font's strings renumbered,
        // its operands written again in their shortest form, or as it was
        // when it names none.
        internal Entry renumber(Entry entry) {
            int n;
            if (!SID_OPERANDS.TryGetValue(entry.op, out n)) {
                return entry;
            }
            if (entry.integers == null || entry.integers.Length < n) {
                throw new Subset.NotSubset();
            }
            ByteBuffer raw = new ByteBuffer();
            int[] integers = new int[entry.integers.Length];
            for (int i = 0; i < entry.integers.Length; i++) {
                int v = entry.integers[i];
                if (i < n && v >= 391) {
                    if (v - 391 >= old.Count) {
                        throw new Subset.NotSubset();
                    }
                    int sid;
                    if (!sids.TryGetValue(v, out sid)) {
                        sid = 391 + kept.Count;
                        sids[v] = sid;
                        kept.Add(old[v - 391]);
                    }
                    v = sid;
                }
                integers[i] = v;
                appendInteger(raw, v);
            }
            if (entry.op > 0xFF) {
                raw.Add(12);
                raw.Add(entry.op & 0xFF);
            } else {
                raw.Add(entry.op);
            }
            return new Entry(entry.op, raw.ToBytes(), integers);
        }
    }

    // Whether any of the subroutines is used.
    internal static bool anyUsed(bool[] used) {
        foreach (bool u in used) {
            if (u) {
                return true;
            }
        }
        return false;
    }

    // Whether the DICT has an entry of the operator, of any operands.
    internal static bool hasEntry(List<Entry> entries, int op) {
        foreach (Entry entry in entries) {
            if (entry.op == op) {
                return true;
            }
        }
        return false;
    }

    // Appends the integer of a DICT in its shortest form.
    internal static void appendInteger(ByteBuffer output, int v) {
        if (v >= -107 && v <= 107) {
            output.Add(v + 139);
        } else if (v >= 108 && v <= 1131) {
            v -= 108;
            output.Add((v >> 8) + 247);
            output.Add(v);
        } else if (v >= -1131 && v <= -108) {
            v = -v - 108;
            output.Add((v >> 8) + 251);
            output.Add(v);
        } else if (v >= -32768 && v <= 32767) {
            output.Add(28);
            output.Add(v >> 8);
            output.Add(v);
        } else {
            appendOffset(output, v);
        }
    }

    // Returns the runs of consecutive CIDs, each its first and the number
    // after it, nLeft, of at most max.
    internal static List<int[]> ranges(List<int> cids, int max) {
        List<int[]> runs = new List<int[]>();
        int i = 0;
        while (i < cids.Count) {
            int j = i;
            while (j + 1 < cids.Count && cids[j + 1] == cids[j] + 1 && j + 1 - i <= max) {
                j++;
            }
            runs.Add(new int[] {cids[i], j - i});
            i = j + 1;
        }
        return runs;
    }

    // Returns the charset of the CIDs of the glyphs after .notdef, in the
    // format of the three that is the shortest, the first of those as short.
    internal static byte[] charsetOf(List<int> cids) {
        List<int[]> ranges1 = ranges(cids, 0xFF);
        List<int[]> ranges2 = ranges(cids, 0xFFFF);
        int size0 = 1 + 2 * cids.Count;
        int size1 = 1 + 3 * ranges1.Count;
        int size2 = 1 + 4 * ranges2.Count;
        ByteBuffer output = new ByteBuffer();
        if (size0 <= size1 && size0 <= size2) {
            output.Add(0);
            foreach (int cid in cids) {
                output.Add2(cid);
            }
        } else if (size1 <= size2) {
            output.Add(1);
            foreach (int[] r in ranges1) {
                output.Add2(r[0]);
                output.Add(r[1]);
            }
        } else {
            output.Add(2);
            foreach (int[] r in ranges2) {
                output.Add2(r[0]);
                output.Add2(r[1]);
            }
        }
        return output.ToBytes();
    }

    // Returns the FDSelect of the Font DICT of each glyph, in format 3,
    // ranges, when that is shorter than format 0, a byte for each.
    internal static byte[] fdSelectOf(int[] fdOf) {
        List<int> firsts = new List<int>();
        for (int gid = 0; gid < fdOf.Length; gid++) {
            if (gid == 0 || fdOf[gid] != fdOf[gid - 1]) {
                firsts.Add(gid);
            }
        }
        ByteBuffer output = new ByteBuffer();
        if (5 + 3 * firsts.Count >= 1 + fdOf.Length) {
            output.Add(0);
            foreach (int fd in fdOf) {
                output.Add(fd);
            }
            return output.ToBytes();
        }
        output.Add(3);
        output.Add2(firsts.Count);
        foreach (int first in firsts) {
            output.Add2(first);
            output.Add(fdOf[first]);
        }
        output.Add2(fdOf.Length);
        return output.ToBytes();
    }

    private static byte[] Latin1(string s) {
        byte[] bytes = new byte[s.Length];
        for (int i = 0; i < s.Length; i++) {
            bytes[i] = (byte) s[i];
        }
        return bytes;
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
