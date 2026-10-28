/*
 * Kerning.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
// How far apart pairs of glyphs go, from the font: the PairPos lookups of the
// kern feature of its GPOS table, pairs of glyphs (format 1) and pairs of
// classes of glyphs (format 2). Font.SetKernPairs(true) turns it on; off,
// which it is unless asked for, nothing is read and nothing is slower. The
// kern table of an older TrueType font with no GPOS table is not read: none of
// the fonts PDFjet ships, nor any of a Linux system's, has one without GPOS,
// and such a font draws its text unkerned.
//
// The table is read when the font's kerning is turned on, once, under a
// lock, so that text is measured with it from any number of threads, and
// within the same limit of work as the marks of the GPOS table, so that a
// table that claims more than a font holds is stopped rather than read to an
// end it does not have.
internal class Kerning {
    private readonly GPOS gpos;     // The GPOS table, read with its reader
    private volatile bool read;     // Set last, once the table is read
    private readonly object readLock = new object();
    // The kern feature's lookups, in their order, each a list of subtables.
    private readonly List<PairSubtable[]> lookups = new List<PairSubtable[]>();
    // Made with the lookups, for the font: the glyphs kerned, not a mark, a
    // right to left letter or a glyph of no character, and the pairs of the
    // printable ASCII characters, looked up once, by asciiIndex
    private ulong[] kernable = new ulong[0];
    private sbyte[] asciiIndex = new sbyte[0];  // By glyph, the ASCII character - 0x20 drawn with it, or -1
    private short[] asciiKern;

    // A PairPos subtable: the first glyphs it covers, and how far each pair
    // moves the second glyph, in font units, by the pair or by the classes of
    // the pair. Each pair drawn of a kerned font is looked up in them, so they
    // are arrays by glyph number, not dictionaries: a map lookup for each
    // subtable that a pair goes through made kerned text twice as slow in
    // Java, and four times in Go (25 October 2026).
    private class PairSubtable {
        internal ulong[] covered = new ulong[0];    // A bit for each first glyph covered
        internal int[] pairSets;        // Format 1, by first glyph: its pair set + 1, or 0
        internal int[][] seconds;       // Format 1, the second glyphs of each pair set, in order
        internal short[][] values;      // Format 1, the moves of the pairs of each pair set
        internal ushort[] firstClass;   // Format 2, the class of each first glyph, 0 past the end
        internal ushort[] secondClass;  // Format 2, the class of each second glyph, 0 past the end
        internal int secondCount;       // Format 2, the classes of the second glyphs
        internal short[] classAdvance;  // Format 2, by first class * secondCount + second class

        internal bool Covers(int gid) {
            return HasBit(covered, gid);
        }

        // Returns true if the pair set of the first glyph has the second, with
        // its move, found by halves in its second glyphs, which are in order.
        internal bool PairValue(int first, int second, out int value) {
            value = 0;
            if (first >= pairSets.Length || pairSets[first] == 0) {
                return false;
            }
            int set = pairSets[first] - 1;
            int i = Array.BinarySearch(seconds[set], second);
            if (i < 0) {
                return false;
            }
            value = values[set][i];
            return true;
        }
    }

    private static bool HasBit(ulong[] bits, int i) {
        return i >= 0 && (i >> 6) < bits.Length && (bits[i >> 6] & (1UL << (i & 63))) != 0;
    }

    private Kerning(GPOS gpos) {
        this.gpos = gpos;
    }

    // Returns the kerning of the GPOS table of a font, read with a reader of
    // its own; null when the font has none.
    internal static Kerning Of(GPOS gpos) {
        if (gpos == null) {
            return null;
        }
        return new Kerning(gpos.Reader());
    }

    // Reads the table, the first time it is called, and makes the glyphs
    // kerned and the ASCII pairs of the font, when there is one; returns this.
    internal Kerning Read(Font font) {
        if (read) {
            return this;
        }
        lock (readLock) {
            if (!read) {
                gpos.gposWork = GPOS.MAX_GPOS_WORK;
                ReadKernFeature();
                if (font != null) {
                    Prepare(font);
                }
                read = true;
            }
        }
        return this;
    }

    // Makes the glyphs the font kerns and the pairs of its printable ASCII
    // characters, so that most pairs drawn are an array read.
    private void Prepare(Font font) {
        int[] unicodeOf = font.unicodeOf;
        kernable = new ulong[(unicodeOf.Length + 63) / 64];
        for (int gid = 1; gid < unicodeOf.Length; gid++) {
            int c = unicodeOf[gid];
            if (c >= 0 && !Page.IsMark(c) && !Page.IsRightToLeft(c)) {
                kernable[gid >> 6] |= 1UL << (gid & 63);
            }
        }
        asciiIndex = new sbyte[unicodeOf.Length];
        for (int gid = 0; gid < asciiIndex.Length; gid++) {
            asciiIndex[gid] = -1;
        }
        for (int i = 0; i < 95; i++) {
            int gid = font.asciiGID[i];
            if (gid != 0 && gid < asciiIndex.Length && asciiIndex[gid] < 0) {
                asciiIndex[gid] = (sbyte) i;
            }
        }
        asciiKern = new short[95*95];
        for (int i = 0; i < 95; i++) {
            int first = font.asciiGID[i];
            for (int j = 0; j < 95; j++) {
                int second = font.asciiGID[j];
                if (first != 0 && second != 0 && HasBit(kernable, first) && HasBit(kernable, second)) {
                    asciiKern[i*95 + j] = (short) Pair(first, second);
                }
            }
        }
    }

    // Returns how far the second glyph moves after the first, in font units:
    // 0 unless both are kerned, from the ASCII pairs when both are printable
    // ASCII characters.
    internal int GlyphPair(int first, int second) {
        if (first <= 0 || second <= 0 || !HasBit(kernable, first) || !HasBit(kernable, second)) {
            return 0;
        }
        if (first < asciiIndex.Length && second < asciiIndex.Length) {
            int a = asciiIndex[first];
            int b = asciiIndex[second];
            if (a >= 0 && b >= 0) {
                return asciiKern[a*95 + b];
            }
        }
        return Pair(first, second);
    }

    // Returns how far the second glyph moves after the first one, in font
    // units: the advance of the first glyph, made shorter or longer. Each
    // lookup of the kern feature adds its own, from the first of its subtables
    // that covers the first glyph and has the pair, as HarfBuzz applies them.
    internal int Pair(int first, int second) {
        if (first < 0 || second < 0) {
            return 0;
        }
        int total = 0;
        foreach (PairSubtable[] subtables in lookups) {
            foreach (PairSubtable s in subtables) {
                if (!s.Covers(first)) {
                    continue;
                }
                if (s.pairSets != null) {
                    int v;
                    if (s.PairValue(first, second, out v)) {
                        total += v;
                        break;
                    }
                    continue;   // A pair set without the second glyph: the next subtable
                }
                int c1 = (first < s.firstClass.Length) ? s.firstClass[first] : 0;
                int c2 = (second < s.secondClass.Length) ? s.secondClass[second] : 0;
                long index = (long) c1 * s.secondCount + c2;
                if (c2 >= s.secondCount || index >= s.classAdvance.Length) {
                    continue;   // A class the subtable has no values for: the next subtable
                }
                total += s.classAdvance[index];
                break;
            }
        }
        return total;
    }

    // Reads the lookups of the kern feature of the GPOS table, from the
    // default language of every script that has the feature, each lookup once
    // and in the order of the lookup list. The kern features of other
    // languages are left out, as HarfBuzz leaves them out of text of no
    // language.
    private void ReadKernFeature() {
        GPOS g = gpos;
        if (g.end - g.start < 10) {
            return;
        }
        int featureList = g.start + g.GetUInt16(g.start + 6);
        int lookupList = g.start + g.GetUInt16(g.start + 8);
        int featureCount = g.GetUInt16(featureList);
        int lookupCount = g.GetUInt16(lookupList);
        bool[] defaults = DefaultFeatures(featureCount);
        bool[] kern = new bool[lookupCount];
        for (int i = 0; i < featureCount && g.gposWork > 0; i++) {
            g.gposWork--;
            if (!defaults[i]) {
                continue;
            }
            int record = featureList + 2 + 6*i;
            if (g.GetUInt16(record) != 0x6B65 || g.GetUInt16(record + 2) != 0x726E) {    // "kern"
                continue;
            }
            int feature = featureList + g.GetUInt16(record + 4);
            int indexCount = Math.Min(g.GetUInt16(feature + 2), g.gposWork);
            g.gposWork -= indexCount;
            for (int j = 0; j < indexCount; j++) {
                int index = g.GetUInt16(feature + 4 + 2*j);
                if (index < lookupCount) {
                    kern[index] = true;
                }
            }
        }
        for (int i = 0; i < lookupCount && g.gposWork > 0; i++) {
            if (!kern[i]) {
                continue;
            }
            int lookup = lookupList + g.GetUInt16(lookupList + 2 + 2*i);
            int lookupType = g.GetUInt16(lookup);
            int subTableCount = g.GetUInt16(lookup + 4);
            List<PairSubtable> subtables = new List<PairSubtable>();
            // A subtable the lookup names again is read once: the second time
            // it kerns no pair the first did not (the review of 28 October
            // 2026).
            HashSet<int> offsetsRead = new HashSet<int>();
            for (int j = 0; j < subTableCount && g.gposWork > 0; j++) {
                g.gposWork--;
                int subTable = lookup + g.GetUInt16(lookup + 6 + 2*j);
                int subTableType = lookupType;
                if (subTableType == 9) {
                    // An extension lookup holds the subtable at an offset of
                    // 32 bits, read unsigned, as the marks' lookups read it.
                    subTableType = g.GetUInt16(subTable + 2);
                    long extension = g.GetUInt32(subTable + 4);
                    if (extension >= g.end - subTable) {
                        continue;
                    }
                    subTable += (int) extension;
                }
                if (subTableType != 2 || !offsetsRead.Add(subTable)) {
                    continue;
                }
                int format = g.GetUInt16(subTable);
                if (format == 1) {
                    subtables.Add(PairsSubtable(subTable));
                } else if (format == 2) {
                    subtables.Add(ClassesSubtable(subTable));
                }
            }
            if (subtables.Count > 0) {
                lookups.Add(subtables.ToArray());
            }
        }
    }

    // Returns the features of the default languages of the scripts of the
    // GPOS table, by their index in the feature list.
    private bool[] DefaultFeatures(int featureCount) {
        GPOS g = gpos;
        bool[] features = new bool[featureCount];
        int scriptList = g.start + g.GetUInt16(g.start + 4);
        int scriptCount = g.GetUInt16(scriptList);
        for (int i = 0; i < scriptCount && g.gposWork > 0; i++) {
            g.gposWork--;
            int script = scriptList + g.GetUInt16(scriptList + 2 + 6*i + 4);
            int defaultLangSys = g.GetUInt16(script);
            if (defaultLangSys == 0) {
                continue;
            }
            int langSys = script + defaultLangSys;
            int required = g.GetUInt16(langSys + 2);
            if (required < featureCount) {
                features[required] = true;
            }
            int indexCount = Math.Min(g.GetUInt16(langSys + 4), g.gposWork);
            g.gposWork -= indexCount;
            for (int j = 0; j < indexCount; j++) {
                int index = g.GetUInt16(langSys + 6 + 2*j);
                if (index < featureCount) {
                    features[index] = true;
                }
            }
        }
        return features;
    }

    // Returns the size of a value record of the format, in bytes, and the
    // offset of its XAdvance in it, or -1 when it has none.
    private static int ValueSize(int format, out int xAdvance) {
        int size = 0;
        xAdvance = -1;
        for (int bit = 0; bit < 8; bit++) {
            if ((format & (1 << bit)) != 0) {
                if (bit == 2) {
                    xAdvance = size;
                }
                size += 2;
            }
        }
        return size;
    }

    // Reads a PairPos subtable of format 1: for each first glyph it covers, a
    // set of second glyphs, each with how far it moves, put in order, the first
    // record of a pair kept, as it is the one applied. A glyph past the glyphs
    // of the font is left out.
    private PairSubtable PairsSubtable(int subTable) {
        GPOS g = gpos;
        int[] firstGlyphs = g.Coverage(subTable + g.GetUInt16(subTable + 2));
        int xAdvance;
        int size1 = ValueSize(g.GetUInt16(subTable + 4), out xAdvance);
        int unused;
        int size2 = ValueSize(g.GetUInt16(subTable + 6), out unused);
        int pairSetCount = g.GetUInt16(subTable + 8);
        PairSubtable s = new PairSubtable();
        s.pairSets = new int[0];
        int pairSetsLength = 0;     // To the last first glyph, as Go's array is
        List<int[]> seconds = new List<int[]>();
        List<short[]> values = new List<short[]>();
        for (int i = 0; i < firstGlyphs.Length; i++) {
            if (i >= pairSetCount || g.gposWork == 0) {
                break;
            }
            int first = firstGlyphs[i];
            if (!IsGlyph(first)) {
                continue;
            }
            if (!Cover(s, first)) {
                break;
            }
            if (first >= pairSetsLength) {
                if (!Grow(pairSetsLength, first + 1, 2)) {
                    break;
                }
                pairSetsLength = first + 1;
                if (first >= s.pairSets.Length) {
                    Array.Resize(ref s.pairSets, Math.Max(first + 1, 2*s.pairSets.Length));
                }
            }
            int pairSet = subTable + g.GetUInt16(subTable + 10 + 2*i);
            int pairCount = Math.Min(g.GetUInt16(pairSet), g.gposWork);
            g.gposWork -= pairCount;
            // Each pair, by its second glyph and then its record
            long[] records = new long[pairCount];
            for (int j = 0; j < pairCount; j++) {
                int record = pairSet + 2 + j*(2 + size1 + size2);
                int v = (xAdvance >= 0) ? g.GetInt16(record + 2 + xAdvance) : 0;
                records[j] = ((long) g.GetUInt16(record) << 40) | ((long) j << 16) | (long) (v & 0xFFFF);
            }
            Array.Sort(records);
            int[] setSeconds = new int[pairCount];
            short[] setValues = new short[pairCount];
            int n = 0;
            foreach (long r in records) {
                int second = (int) (r >> 40);
                if (n > 0 && setSeconds[n - 1] == second) {
                    continue;   // The first record of a pair is the one applied
                }
                setSeconds[n] = second;
                setValues[n] = (short) (r & 0xFFFF);
                n++;
            }
            if (s.pairSets[first] == 0) {   // A glyph covered twice: its first pair set
                Array.Resize(ref setSeconds, n);
                Array.Resize(ref setValues, n);
                seconds.Add(setSeconds);
                values.Add(setValues);
                s.pairSets[first] = seconds.Count;
            }
        }
        s.seconds = seconds.ToArray();
        s.values = values.ToArray();
        return s;
    }

    // Reads a PairPos subtable of format 2: the first glyphs it covers, the
    // class of each first and second glyph, and how far each pair of classes
    // moves the second glyph. A glyph past the glyphs of the font is left out.
    private PairSubtable ClassesSubtable(int subTable) {
        GPOS g = gpos;
        int[] firstGlyphs = g.Coverage(subTable + g.GetUInt16(subTable + 2));
        int xAdvance;
        int size1 = ValueSize(g.GetUInt16(subTable + 4), out xAdvance);
        int unused;
        int size2 = ValueSize(g.GetUInt16(subTable + 6), out unused);
        int firstClasses = g.GetUInt16(subTable + 12);
        int secondClasses = g.GetUInt16(subTable + 14);
        PairSubtable s = new PairSubtable();
        s.secondCount = secondClasses;
        foreach (int first in firstGlyphs) {
            if (IsGlyph(first) && !Cover(s, first)) {
                break;
            }
        }
        s.firstClass = ClassDef(subTable + g.GetUInt16(subTable + 8));
        s.secondClass = ClassDef(subTable + g.GetUInt16(subTable + 10));
        long count = (long) firstClasses * secondClasses;
        if (count > g.gposWork) {
            return new PairSubtable();  // Covers nothing
        }
        g.gposWork -= (int) count;
        s.classAdvance = new short[count];
        if (xAdvance >= 0) {
            int record = subTable + 16;
            for (int c = 0; c < count; c++) {
                s.classAdvance[c] = (short) g.GetInt16(record + c*(size1 + size2) + xAdvance);
            }
        }
        return s;
    }

    // Reads a class definition table: the class of each glyph it gives one,
    // every other glyph of class 0. The room the classes take is work, as the
    // glyphs of a coverage table are.
    private ushort[] ClassDef(int offset) {
        GPOS g = gpos;
        ushort[] classes = new ushort[0];
        int length = 0;     // To the last glyph of a class, as Go's array is
        int limit = 0x10000;
        if (g.numGlyphs > 0) {
            limit = g.numGlyphs;
        }
        int format = g.GetUInt16(offset);
        if (format == 1) {
            int start = g.GetUInt16(offset + 2);
            int count = Math.Min(Math.Min(g.GetUInt16(offset + 4), g.gposWork), limit);
            g.gposWork -= count;
            for (int i = 0; i < count && start + i < limit && g.gposWork > 0; i++) {
                int glyphClass = g.GetUInt16(offset + 6 + 2*i);
                if (glyphClass != 0 && (start + i < length || Grow(length, start + i + 1, 4))) {
                    length = Math.Max(length, start + i + 1);
                    classes = WithClass(classes, start + i, glyphClass);
                }
            }
        } else if (format == 2) {
            int rangeCount = Math.Min(g.GetUInt16(offset + 2), g.gposWork);
            g.gposWork -= rangeCount;
            for (int i = 0; i < rangeCount && g.gposWork > 0; i++) {
                int record = offset + 4 + 6*i;
                int start = g.GetUInt16(record);
                int end = Math.Min(g.GetUInt16(record + 2), limit - 1);
                int glyphClass = g.GetUInt16(record + 4);
                for (int glyph = start; glyph <= end && g.gposWork > 0; glyph++) {
                    g.gposWork--;
                    if (glyphClass != 0 && (glyph < length || Grow(length, glyph + 1, 4))) {
                        length = Math.Max(length, glyph + 1);
                        classes = WithClass(classes, glyph, glyphClass);
                    }
                }
            }
        }
        return classes;
    }

    // Gives the glyph the class, the array made longer as needed.
    private static ushort[] WithClass(ushort[] classes, int glyph, int glyphClass) {
        if (glyph >= classes.Length) {
            Array.Resize(ref classes, Math.Max(glyph + 1, 2*classes.Length));
        }
        classes[glyph] = (ushort) glyphClass;
        return classes;
    }

    // Marks the glyph covered, and returns false, with nothing marked, when
    // there is not the work left for the words it adds to the bits.
    private bool Cover(PairSubtable s, int gid) {
        if ((gid >> 6) >= s.covered.Length) {
            if (!Grow(s.covered.Length, (gid >> 6) + 1, 1)) {
                return false;
            }
            Array.Resize(ref s.covered, (gid >> 6) + 1);
        }
        s.covered[gid >> 6] |= 1UL << (gid & 63);
        return true;
    }

    // Takes the work of an array grown from its length to the new one, with
    // so many of its elements to a word of 64 bits, a word each, and returns
    // true, or, when there is not that much work left, stops the reading, with
    // no work left, and returns false. The arrays of the kerning, by glyph
    // number, grow as far as the glyphs its subtables name, and the room they
    // make is work: a table of a few kilobytes whose subtables all named glyph
    // 65535 made gigabytes of them (the review of 28 October 2026). Of the 261
    // fonts PDFjet ships the kerning of IBM Plex Sans SC takes the most work,
    // 192,235.
    private bool Grow(int length, int grown, int perWord) {
        int work = (grown + perWord - 1)/perWord - (length + perWord - 1)/perWord;
        if (work > gpos.gposWork) {
            gpos.gposWork = 0;
            return false;
        }
        gpos.gposWork -= work;
        return true;
    }

    // Returns true if the glyph is one of the font's, or the font does not say
    // how many glyphs it has.
    private bool IsGlyph(int gid) {
        return gpos.numGlyphs <= 0 || gid < gpos.numGlyphs;
    }

    // Returns the room the arrays of the subtables take, in words of 64 bits,
    // for the tests.
    internal int Room() {
        int room = 0;
        foreach (PairSubtable[] subtables in lookups) {
            foreach (PairSubtable s in subtables) {
                room += s.covered.Length;
                room += (s.pairSets == null) ? 0 : (s.pairSets.Length + 1)/2;
                room += (s.firstClass == null) ? 0 : (s.firstClass.Length + 3)/4;
                room += (s.secondClass == null) ? 0 : (s.secondClass.Length + 3)/4;
            }
        }
        return room;
    }
}
}   // End of namespace PDFjet.NET
