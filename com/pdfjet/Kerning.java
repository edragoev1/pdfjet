/*
 * Kerning.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.ArrayList;
import java.util.BitSet;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

/**
 * How far apart pairs of glyphs go, from the font: the PairPos lookups of the
 * kern feature of its GPOS table, pairs of glyphs (format 1) and pairs of
 * classes of glyphs (format 2), or, for an older TrueType font with no kern
 * feature, the pairs of its kern table. Font.setKernPairs(true) turns it on;
 * off, which it is unless asked for, nothing is read and nothing is slower.
 *
 * The tables are read when the font's kerning is turned on, once, under a
 * lock, so that text is measured with them from any number of threads, and
 * within the same limit of work as the marks of the GPOS table, so that a
 * table that claims more than a font holds is stopped rather than read to an
 * end it does not have.
 */
class Kerning {
    private final GPOS gpos;        // The GPOS table, read with its reader, or null
    private final GPOS kernTable;   // The kern table, read with the same reader, or null
    private volatile boolean read;  // Set last, once the tables are read
    // The kern feature's lookups, in their order, each its subtables
    private final List<PairSubtable[]> lookups = new ArrayList<PairSubtable[]>();
    private Map<Integer, Short> legacyKern;    // The pairs of the kern table, by glyph IDs

    // A PairPos subtable: the first glyphs it covers, and how far each pair
    // moves the second glyph, in font units, by the pair or by the classes of
    // the pair.
    private static final class PairSubtable {
        BitSet covered = new BitSet();
        Map<Integer, Short> pairs;          // Format 1, by (first << 16) | second
        Map<Integer, Integer> firstClass;   // Format 2, the class of each first glyph
        Map<Integer, Integer> secondClass;  // Format 2, the class of each second glyph
        int secondCount;                    // Format 2, the classes of the second glyphs
        short[] classAdvance;               // Format 2, by first class * secondCount + second class
    }

    private Kerning(GPOS gpos, GPOS kernTable) {
        this.gpos = gpos;
        this.kernTable = kernTable;
    }

    /**
     * Returns the kerning of the font, from its GPOS table, read on the
     * buffer of the font's own reader of it, and from its kern table, read
     * with the same reader, either of which can be missing; null when the
     * font has neither.
     */
    static Kerning of(GPOS gpos, GPOS kernTable) {
        if (gpos == null && kernTable == null) {
            return null;
        }
        return new Kerning(
                (gpos != null) ? new GPOS(gpos.buf, gpos.start, gpos.end, gpos.numGlyphs) : null,
                kernTable);
    }

    // Reads the tables, the first time it is called.
    void read() {
        if (read) {
            return;
        }
        synchronized (this) {
            if (!read) {
                if (gpos != null) {
                    gpos.gposWork = GPOS.MAX_GPOS_WORK;
                    readKernFeature();
                }
                if (lookups.isEmpty() && kernTable != null) {
                    kernTable.gposWork = GPOS.MAX_GPOS_WORK;
                    readKernTable();
                }
                read = true;
            }
        }
    }

    /**
     * Returns how far the second glyph moves after the first one, in font
     * units: the advance of the first glyph, made shorter or longer. Each
     * lookup of the kern feature adds its own, from the first of its
     * subtables that covers the first glyph and has the pair, as HarfBuzz
     * applies them.
     */
    int pair(int first, int second) {
        read();
        if (lookups.isEmpty()) {
            if (legacyKern == null) {
                return 0;
            }
            Short v = legacyKern.get((first << 16) | second);
            return (v != null) ? v : 0;
        }
        int total = 0;
        for (PairSubtable[] subtables : lookups) {
            for (PairSubtable s : subtables) {
                if (!s.covered.get(first)) {
                    continue;
                }
                if (s.pairs != null) {
                    Short v = s.pairs.get((first << 16) | second);
                    if (v != null) {
                        total += v;
                        break;
                    }
                    continue;   // A pair set without the second glyph: the next subtable
                }
                Integer c1 = s.firstClass.get(first);
                Integer c2 = s.secondClass.get(second);
                long class1 = (c1 != null) ? c1 : 0;
                long class2 = (c2 != null) ? c2 : 0;
                if (class2 >= s.secondCount || class1*s.secondCount + class2 >= s.classAdvance.length) {
                    continue;   // A class the subtable has no values for: the next subtable
                }
                total += s.classAdvance[(int) (class1*s.secondCount + class2)];
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
    private void readKernFeature() {
        GPOS g = gpos;
        if (g.end - g.start < 10) {
            return;
        }
        int featureList = g.start + g.getUInt16(g.start + 6);
        int lookupList = g.start + g.getUInt16(g.start + 8);
        int featureCount = g.getUInt16(featureList);
        int lookupCount = g.getUInt16(lookupList);
        boolean[] defaults = defaultFeatures(featureCount);
        boolean[] kern = new boolean[lookupCount];
        for (int i = 0; i < featureCount && g.gposWork > 0; i++) {
            g.gposWork--;
            if (!defaults[i]) {
                continue;
            }
            int record = featureList + 2 + 6*i;
            if (g.getUInt16(record) != 0x6B65 || g.getUInt16(record + 2) != 0x726E) {   // "kern"
                continue;
            }
            int feature = featureList + g.getUInt16(record + 4);
            int indexCount = Math.min(g.getUInt16(feature + 2), g.gposWork);
            g.gposWork -= indexCount;
            for (int j = 0; j < indexCount; j++) {
                int index = g.getUInt16(feature + 4 + 2*j);
                if (index < lookupCount) {
                    kern[index] = true;
                }
            }
        }
        for (int i = 0; i < lookupCount && g.gposWork > 0; i++) {
            if (!kern[i]) {
                continue;
            }
            int lookup = lookupList + g.getUInt16(lookupList + 2 + 2*i);
            int lookupType = g.getUInt16(lookup);
            int subTableCount = g.getUInt16(lookup + 4);
            List<PairSubtable> subtables = new ArrayList<PairSubtable>();
            for (int j = 0; j < subTableCount && g.gposWork > 0; j++) {
                g.gposWork--;
                int subTable = lookup + g.getUInt16(lookup + 6 + 2*j);
                int subTableType = lookupType;
                if (subTableType == 9) {
                    // An extension lookup holds the subtable at an offset of 32
                    // bits, read unsigned, as the marks' lookups read it.
                    subTableType = g.getUInt16(subTable + 2);
                    long extension = g.getUInt32(subTable + 4);
                    if (extension >= g.end - subTable) {
                        continue;
                    }
                    subTable += (int) extension;
                }
                if (subTableType != 2) {
                    continue;
                }
                int format = g.getUInt16(subTable);
                if (format == 1) {
                    subtables.add(pairsSubtable(subTable));
                } else if (format == 2) {
                    subtables.add(classesSubtable(subTable));
                }
            }
            if (!subtables.isEmpty()) {
                lookups.add(subtables.toArray(new PairSubtable[subtables.size()]));
            }
        }
    }

    // Returns the features of the default languages of the scripts of the
    // GPOS table, by their index in the feature list.
    private boolean[] defaultFeatures(int featureCount) {
        GPOS g = gpos;
        boolean[] features = new boolean[featureCount];
        int scriptList = g.start + g.getUInt16(g.start + 4);
        int scriptCount = g.getUInt16(scriptList);
        for (int i = 0; i < scriptCount && g.gposWork > 0; i++) {
            g.gposWork--;
            int script = scriptList + g.getUInt16(scriptList + 2 + 6*i + 4);
            int defaultLangSys = g.getUInt16(script);
            if (defaultLangSys == 0) {
                continue;
            }
            int langSys = script + defaultLangSys;
            int required = g.getUInt16(langSys + 2);
            if (required < featureCount) {
                features[required] = true;
            }
            int indexCount = Math.min(g.getUInt16(langSys + 4), g.gposWork);
            g.gposWork -= indexCount;
            for (int j = 0; j < indexCount; j++) {
                int index = g.getUInt16(langSys + 6 + 2*j);
                if (index < featureCount) {
                    features[index] = true;
                }
            }
        }
        return features;
    }

    // Returns the size of a value record of the format, in bytes, and the
    // offset of its XAdvance in it, or -1 when it has none.
    private static int[] valueSize(int format) {
        int size = 0;
        int xAdvance = 0;
        for (int bit = 0; bit < 8; bit++) {
            if ((format & (1 << bit)) != 0) {
                if (bit == 2) {
                    xAdvance = size;
                }
                size += 2;
            }
        }
        if ((format & 0x0004) == 0) {
            xAdvance = -1;
        }
        return new int[] {size, xAdvance};
    }

    // Reads a PairPos subtable of format 1: for each first glyph it covers, a
    // set of second glyphs, each with how far it moves.
    private PairSubtable pairsSubtable(int subTable) {
        GPOS g = gpos;
        int[] firstGlyphs = g.coverage(subTable + g.getUInt16(subTable + 2));
        int[] value1 = valueSize(g.getUInt16(subTable + 4));
        int size1 = value1[0];
        int xAdvance = value1[1];
        int size2 = valueSize(g.getUInt16(subTable + 6))[0];
        int pairSetCount = g.getUInt16(subTable + 8);
        PairSubtable s = new PairSubtable();
        s.pairs = new HashMap<Integer, Short>();
        for (int i = 0; i < firstGlyphs.length; i++) {
            if (i >= pairSetCount || g.gposWork == 0) {
                break;
            }
            int first = firstGlyphs[i];
            s.covered.set(first);
            int pairSet = subTable + g.getUInt16(subTable + 10 + 2*i);
            int pairCount = Math.min(g.getUInt16(pairSet), g.gposWork);
            g.gposWork -= pairCount;
            for (int j = 0; j < pairCount; j++) {
                int record = pairSet + 2 + j*(2 + size1 + size2);
                int second = g.getUInt16(record);
                int key = (first << 16) | second;
                if (s.pairs.containsKey(key)) {
                    continue;   // The first record of a pair is the one applied
                }
                int v = 0;
                if (xAdvance >= 0) {
                    v = g.getInt16(record + 2 + xAdvance);
                }
                s.pairs.put(key, (short) v);
            }
        }
        return s;
    }

    // Reads a PairPos subtable of format 2: the first glyphs it covers, the
    // class of each first and second glyph, and how far each pair of classes
    // moves the second glyph.
    private PairSubtable classesSubtable(int subTable) {
        GPOS g = gpos;
        int[] firstGlyphs = g.coverage(subTable + g.getUInt16(subTable + 2));
        int[] value1 = valueSize(g.getUInt16(subTable + 4));
        int size1 = value1[0];
        int xAdvance = value1[1];
        int size2 = valueSize(g.getUInt16(subTable + 6))[0];
        int firstClasses = g.getUInt16(subTable + 12);
        int secondClasses = g.getUInt16(subTable + 14);
        PairSubtable s = new PairSubtable();
        s.secondCount = secondClasses;
        for (int first : firstGlyphs) {
            s.covered.set(first);
        }
        s.firstClass = classDef(subTable + g.getUInt16(subTable + 8));
        s.secondClass = classDef(subTable + g.getUInt16(subTable + 10));
        // Of two counts of 16 bits, which an int does not hold
        long count = (long) firstClasses * secondClasses;
        if (count > g.gposWork) {
            return new PairSubtable();  // Covers nothing
        }
        g.gposWork -= (int) count;
        s.classAdvance = new short[(int) count];
        if (xAdvance >= 0) {
            int record = subTable + 16;
            for (int c = 0; c < count; c++) {
                s.classAdvance[c] = (short) g.getInt16(record + c*(size1 + size2) + xAdvance);
            }
        }
        return s;
    }

    // Reads a class definition table: the class of each glyph it gives one,
    // every other glyph of class 0.
    private Map<Integer, Integer> classDef(int offset) {
        GPOS g = gpos;
        Map<Integer, Integer> classes = new HashMap<Integer, Integer>();
        int limit = (g.numGlyphs > 0) ? g.numGlyphs : 0x10000;
        int format = g.getUInt16(offset);
        if (format == 1) {
            int start = g.getUInt16(offset + 2);
            int count = Math.min(Math.min(g.getUInt16(offset + 4), g.gposWork), limit);
            g.gposWork -= count;
            for (int i = 0; i < count; i++) {
                int glyphClass = g.getUInt16(offset + 6 + 2*i);
                if (glyphClass != 0) {
                    classes.put(start + i, glyphClass);
                }
            }
        } else if (format == 2) {
            int rangeCount = Math.min(g.getUInt16(offset + 2), g.gposWork);
            g.gposWork -= rangeCount;
            for (int i = 0; i < rangeCount && g.gposWork > 0; i++) {
                int record = offset + 4 + 6*i;
                int start = g.getUInt16(record);
                int end = Math.min(g.getUInt16(record + 2), limit - 1);
                int glyphClass = g.getUInt16(record + 4);
                for (int glyph = start; glyph <= end && g.gposWork > 0; glyph++) {
                    g.gposWork--;
                    if (glyphClass != 0) {
                        classes.put(glyph, glyphClass);
                    }
                }
            }
        }
        return classes;
    }

    // Reads the horizontal pairs of format 0 of the kern table of an older
    // TrueType font, version 0, which Windows reads; the version 1 table of
    // Apple's fonts is left out.
    private void readKernTable() {
        GPOS t = kernTable;
        if (t.end - t.start < 4 || t.getUInt16(t.start) != 0) {
            return;
        }
        legacyKern = new HashMap<Integer, Short>();
        int tableCount = t.getUInt16(t.start + 2);
        int subTable = t.start + 4;
        for (int i = 0; i < tableCount && t.gposWork > 0; i++) {
            t.gposWork--;
            int length = t.getUInt16(subTable + 2);
            int coverage = t.getUInt16(subTable + 4);
            // Format 0, horizontal, kerning and not minimum values, not cross-stream
            if (coverage >> 8 == 0) {
                // The 16 bits of the length of a table of many pairs overflow:
                // the pairs say where the next table is
                length = 14 + 6*t.getUInt16(subTable + 6);
            }
            if (coverage >> 8 == 0 && (coverage & 0x000F) == 0x0001) {
                int pairCount = Math.min(t.getUInt16(subTable + 6), t.gposWork);
                t.gposWork -= pairCount;
                for (int j = 0; j < pairCount; j++) {
                    int record = subTable + 14 + 6*j;
                    int key = (t.getUInt16(record) << 16) | t.getUInt16(record + 2);
                    if (!legacyKern.containsKey(key)) {
                        legacyKern.put(key, (short) t.getInt16(record + 4));
                    }
                }
            }
            // Past the end of the table there is nothing more to read, and
            // the offset of the next table would overflow an int
            if (length < 6 || length >= t.end - subTable) {
                break;
            }
            subTable += length;
        }
    }
}   // End of Kerning.java
