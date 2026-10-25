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
// classes of glyphs (format 2), or, for an older TrueType font with no kern
// feature, the pairs of its kern table. Font.SetKernPairs(true) turns it on;
// off, which it is unless asked for, nothing is read and nothing is slower.
//
// The tables are read when the font's kerning is turned on, once, under a
// lock, so that text is measured with them from any number of threads, and
// within the same limit of work as the marks of the GPOS table, so that a
// table that claims more than a font holds is stopped rather than read to an
// end it does not have.
internal class Kerning {
    private readonly GPOS gpos;         // The GPOS table, read with its reader, or null
    private readonly GPOS kernTable;    // The kern table, read with the same reader, or null
    private volatile bool read;     // Set last, once the tables are read
    private readonly object readLock = new object();
    // The kern feature's lookups, in their order, each a list of subtables.
    private readonly List<PairSubtable[]> lookups = new List<PairSubtable[]>();
    // The pairs of the kern table, by (first << 16) | second, or null.
    private Dictionary<uint, short> legacyKern;

    // A PairPos subtable: the first glyphs it covers, and how far each pair
    // moves the second glyph, in font units, by the pair or by the classes of
    // the pair.
    private class PairSubtable {
        internal HashSet<int> covered = new HashSet<int>();
        internal Dictionary<uint, short> pairs;     // Format 1, by (first << 16) | second
        internal Dictionary<int, int> firstClass;   // Format 2, the class of each first glyph
        internal Dictionary<int, int> secondClass;  // Format 2, the class of each second glyph
        internal int secondCount;                   // Format 2, the classes of the second glyphs
        internal short[] classAdvance;              // Format 2, by first class * secondCount + second class
    }

    private Kerning(GPOS gpos, GPOS kernTable) {
        this.gpos = gpos;
        this.kernTable = kernTable;
    }

    // Returns the kerning of the GPOS table and the kern table of a font,
    // either of which can be null, each read with a reader of its own; null
    // when the font has neither.
    internal static Kerning Of(GPOS gpos, GPOS kernTable) {
        if (gpos == null && kernTable == null) {
            return null;
        }
        return new Kerning(gpos?.Reader(), kernTable);
    }

    // Reads the tables, the first time it is called, and returns this.
    internal Kerning Read() {
        if (read) {
            return this;
        }
        lock (readLock) {
            if (!read) {
                if (gpos != null) {
                    gpos.gposWork = GPOS.MAX_GPOS_WORK;
                    ReadKernFeature();
                }
                if (lookups.Count == 0 && kernTable != null) {
                    kernTable.gposWork = GPOS.MAX_GPOS_WORK;
                    ReadKernTable();
                }
                read = true;
            }
        }
        return this;
    }

    // Returns how far the second glyph moves after the first one, in font
    // units: the advance of the first glyph, made shorter or longer. Each
    // lookup of the kern feature adds its own, from the first of its subtables
    // that covers the first glyph and has the pair, as HarfBuzz applies them.
    internal int Pair(int first, int second) {
        Read();
        uint key = ((uint) first << 16) | (uint) second;
        if (lookups.Count == 0) {
            short value;
            if (legacyKern != null && legacyKern.TryGetValue(key, out value)) {
                return value;
            }
            return 0;
        }
        int total = 0;
        foreach (PairSubtable[] subtables in lookups) {
            foreach (PairSubtable s in subtables) {
                if (!s.covered.Contains(first)) {
                    continue;
                }
                if (s.pairs != null) {
                    short v;
                    if (s.pairs.TryGetValue(key, out v)) {
                        total += v;
                        break;
                    }
                    continue;   // A pair set without the second glyph: the next subtable
                }
                int c1;
                int c2;
                s.firstClass.TryGetValue(first, out c1);
                s.secondClass.TryGetValue(second, out c2);
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
                if (subTableType != 2) {
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
    // set of second glyphs, each with how far it moves.
    private PairSubtable PairsSubtable(int subTable) {
        GPOS g = gpos;
        int[] firstGlyphs = g.Coverage(subTable + g.GetUInt16(subTable + 2));
        int xAdvance;
        int size1 = ValueSize(g.GetUInt16(subTable + 4), out xAdvance);
        int unused;
        int size2 = ValueSize(g.GetUInt16(subTable + 6), out unused);
        int pairSetCount = g.GetUInt16(subTable + 8);
        PairSubtable s = new PairSubtable();
        s.pairs = new Dictionary<uint, short>();
        for (int i = 0; i < firstGlyphs.Length; i++) {
            if (i >= pairSetCount || g.gposWork == 0) {
                break;
            }
            int first = firstGlyphs[i];
            s.covered.Add(first);
            int pairSet = subTable + g.GetUInt16(subTable + 10 + 2*i);
            int pairCount = Math.Min(g.GetUInt16(pairSet), g.gposWork);
            g.gposWork -= pairCount;
            for (int j = 0; j < pairCount; j++) {
                int record = pairSet + 2 + j*(2 + size1 + size2);
                int second = g.GetUInt16(record);
                uint key = ((uint) first << 16) | (uint) second;
                if (s.pairs.ContainsKey(key)) {
                    continue;   // The first record of a pair is the one applied
                }
                int v = 0;
                if (xAdvance >= 0) {
                    v = g.GetInt16(record + 2 + xAdvance);
                }
                s.pairs[key] = (short) v;
            }
        }
        return s;
    }

    // Reads a PairPos subtable of format 2: the first glyphs it covers, the
    // class of each first and second glyph, and how far each pair of classes
    // moves the second glyph.
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
            s.covered.Add(first);
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
    // every other glyph of class 0.
    private Dictionary<int, int> ClassDef(int offset) {
        GPOS g = gpos;
        Dictionary<int, int> classes = new Dictionary<int, int>();
        int limit = 0x10000;
        if (g.numGlyphs > 0) {
            limit = g.numGlyphs;
        }
        int format = g.GetUInt16(offset);
        if (format == 1) {
            int start = g.GetUInt16(offset + 2);
            int count = Math.Min(Math.Min(g.GetUInt16(offset + 4), g.gposWork), limit);
            g.gposWork -= count;
            for (int i = 0; i < count; i++) {
                int glyphClass = g.GetUInt16(offset + 6 + 2*i);
                if (glyphClass != 0) {
                    classes[start + i] = glyphClass;
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
                    if (glyphClass != 0) {
                        classes[glyph] = glyphClass;
                    }
                }
            }
        }
        return classes;
    }

    // Reads the horizontal pairs of format 0 of the kern table of an older
    // TrueType font, version 0, which Windows reads; the version 1 table of
    // Apple's fonts is left out.
    private void ReadKernTable() {
        GPOS t = kernTable;
        if (t.end - t.start < 4 || t.GetUInt16(t.start) != 0) {
            return;
        }
        legacyKern = new Dictionary<uint, short>();
        int tableCount = t.GetUInt16(t.start + 2);
        int subTable = t.start + 4;
        for (int i = 0; i < tableCount && t.gposWork > 0; i++) {
            t.gposWork--;
            int length = t.GetUInt16(subTable + 2);
            int coverage = t.GetUInt16(subTable + 4);
            // Format 0, horizontal, kerning and not minimum values, not cross-stream
            if ((coverage >> 8) == 0) {
                // The 16 bits of the length of a table of many pairs overflow:
                // the pairs say where the next table is
                length = 14 + 6*t.GetUInt16(subTable + 6);
            }
            if ((coverage >> 8) == 0 && (coverage & 0x000F) == 0x0001) {
                int pairCount = Math.Min(t.GetUInt16(subTable + 6), t.gposWork);
                t.gposWork -= pairCount;
                for (int j = 0; j < pairCount; j++) {
                    int record = subTable + 14 + 6*j;
                    uint key = ((uint) t.GetUInt16(record) << 16) | (uint) t.GetUInt16(record + 2);
                    if (!legacyKern.ContainsKey(key)) {
                        legacyKern[key] = (short) t.GetInt16(record + 4);
                    }
                }
            }
            // Past the end of the table there is nothing more to read.
            if (length < 6 || subTable + length >= t.end) {
                break;
            }
            subTable += length;
        }
    }
}
}   // End of namespace PDFjet.NET
