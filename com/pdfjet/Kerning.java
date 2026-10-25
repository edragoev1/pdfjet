/*
 * Kerning.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.BitSet;
import java.util.List;

/**
 * How far apart pairs of glyphs go, from the font: the PairPos lookups of the
 * kern feature of its GPOS table, pairs of glyphs (format 1) and pairs of
 * classes of glyphs (format 2). Font.setKernPairs(true) turns it on; off,
 * which it is unless asked for, nothing is read and nothing is slower. The
 * kern table of an older TrueType font with no GPOS table is not read: none
 * of the fonts PDFjet ships, nor any of a Linux system's, has one without
 * GPOS, and such a font draws its text unkerned.
 *
 * The table is read when the font's kerning is turned on, once, under a
 * lock, so that text is measured with it from any number of threads, and
 * within the same limit of work as the marks of the GPOS table, so that a
 * table that claims more than a font holds is stopped rather than read to an
 * end it does not have.
 */
class Kerning {
    private final GPOS gpos;        // The GPOS table, read with its reader
    private volatile boolean read;  // Set last, once the table is read
    // The kern feature's lookups, in their order, each its subtables
    private final List<PairSubtable[]> lookups = new ArrayList<PairSubtable[]>();
    // Made with the lookups, for the font: the glyphs kerned, not a mark, a
    // right to left letter or a glyph of no character, and the pairs of the
    // printable ASCII characters, looked up once, by asciiIndex
    private final BitSet kernable = new BitSet();
    private byte[] asciiIndex = new byte[0];    // By glyph, the ASCII character - 0x20 drawn with it, or -1
    private short[] asciiKern;

    // A PairPos subtable: the first glyphs it covers, and how far each pair
    // moves the second glyph, in font units, by the pair or by the classes of
    // the pair. Each pair drawn of a kerned font is looked up in them, so they
    // are arrays by glyph number, not maps: a map lookup for each subtable
    // that a pair goes through made kerned text twice as slow in Java, and
    // four times in Go (25 October 2026).
    private static final class PairSubtable {
        BitSet covered = new BitSet();
        int[] pairSets;         // Format 1, by first glyph: its pair set + 1, or 0
        int[][] seconds;        // Format 1, the second glyphs of each pair set, in order
        short[][] values;       // Format 1, the moves of the pairs of each pair set
        char[] firstClass;      // Format 2, the class of each first glyph, 0 past the end
        char[] secondClass;     // Format 2, the class of each second glyph, 0 past the end
        int secondCount;        // Format 2, the classes of the second glyphs
        short[] classAdvance;   // Format 2, by first class * secondCount + second class

        // Returns the move of the pair, and Integer.MIN_VALUE when the pair
        // set of the first glyph has not the second, found by halves in its
        // second glyphs, which are in order.
        int pairValue(int first, int second) {
            if (first >= pairSets.length || pairSets[first] == 0) {
                return NONE;
            }
            int set = pairSets[first] - 1;
            int i = Arrays.binarySearch(seconds[set], second);
            return (i >= 0) ? values[set][i] : NONE;
        }
    }

    // What pairValue returns for a pair a subtable does not have: no value of
    // 16 bits is it.
    private static final int NONE = Integer.MIN_VALUE;

    private Kerning(GPOS gpos) {
        this.gpos = gpos;
    }

    /**
     * Returns the kerning of the font, from its GPOS table, read on the
     * buffer of the font's own reader of it; null when the font has none.
     */
    static Kerning of(GPOS gpos) {
        if (gpos == null) {
            return null;
        }
        return new Kerning(new GPOS(gpos.buf, gpos.start, gpos.end, gpos.numGlyphs));
    }

    // Reads the table, the first time it is called, and makes the glyphs
    // kerned and the ASCII pairs of the font, when there is one; returns this.
    Kerning read(Font font) {
        if (read) {
            return this;
        }
        synchronized (this) {
            if (!read) {
                gpos.gposWork = GPOS.MAX_GPOS_WORK;
                readKernFeature();
                if (font != null) {
                    prepare(font);
                }
                read = true;
            }
        }
        return this;
    }

    // Makes the glyphs the font kerns and the pairs of its printable ASCII
    // characters, so that most pairs drawn are an array read.
    private void prepare(Font font) {
        for (int gid = 1; gid < font.unicodeOf.length; gid++) {
            int c = font.unicodeOf[gid];
            if (c >= 0 && !Page.isMark(c) && !Page.isRightToLeft(c)) {
                kernable.set(gid);
            }
        }
        asciiIndex = new byte[font.unicodeOf.length];
        Arrays.fill(asciiIndex, (byte) -1);
        for (int i = 0; i < 95; i++) {
            int gid = font.asciiGID[i];
            if (gid != 0 && gid < asciiIndex.length && asciiIndex[gid] < 0) {
                asciiIndex[gid] = (byte) i;
            }
        }
        asciiKern = new short[95*95];
        for (int i = 0; i < 95; i++) {
            int first = font.asciiGID[i];
            for (int j = 0; j < 95; j++) {
                int second = font.asciiGID[j];
                if (first != 0 && second != 0 && kernable.get(first) && kernable.get(second)) {
                    asciiKern[i*95 + j] = (short) pair(first, second);
                }
            }
        }
    }

    // Returns how far the second glyph moves after the first, in font units:
    // 0 unless both are kerned, from the ASCII pairs when both are printable
    // ASCII characters.
    int glyphPair(int first, int second) {
        if (first <= 0 || second <= 0 || !kernable.get(first) || !kernable.get(second)) {
            return 0;
        }
        if (first < asciiIndex.length && second < asciiIndex.length) {
            int a = asciiIndex[first];
            int b = asciiIndex[second];
            if (a >= 0 && b >= 0) {
                return asciiKern[a*95 + b];
            }
        }
        return pair(first, second);
    }

    /**
     * Returns how far the second glyph moves after the first one, in font
     * units: the advance of the first glyph, made shorter or longer. Each
     * lookup of the kern feature adds its own, from the first of its
     * subtables that covers the first glyph and has the pair, as HarfBuzz
     * applies them.
     */
    int pair(int first, int second) {
        if (first < 0 || second < 0) {
            return 0;
        }
        int total = 0;
        for (PairSubtable[] subtables : lookups) {
            for (PairSubtable s : subtables) {
                if (!s.covered.get(first)) {
                    continue;
                }
                if (s.pairSets != null) {
                    int v = s.pairValue(first, second);
                    if (v != NONE) {
                        total += v;
                        break;
                    }
                    continue;   // A pair set without the second glyph: the next subtable
                }
                long class1 = (first < s.firstClass.length) ? s.firstClass[first] : 0;
                long class2 = (second < s.secondClass.length) ? s.secondClass[second] : 0;
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
        int maxFirst = -1;
        for (int i = 0; i < firstGlyphs.length && i < pairSetCount; i++) {
            maxFirst = Math.max(maxFirst, firstGlyphs[i]);
        }
        s.pairSets = new int[maxFirst + 1];
        List<int[]> seconds = new ArrayList<int[]>();
        List<short[]> values = new ArrayList<short[]>();
        for (int i = 0; i < firstGlyphs.length; i++) {
            if (i >= pairSetCount || g.gposWork == 0) {
                break;
            }
            int first = firstGlyphs[i];
            s.covered.set(first);
            int pairSet = subTable + g.getUInt16(subTable + 10 + 2*i);
            int pairCount = Math.min(g.getUInt16(pairSet), g.gposWork);
            g.gposWork -= pairCount;
            // Each pair, by its second glyph, the first record of a pair
            // kept, as it is the one applied
            long[] records = new long[pairCount];
            for (int j = 0; j < pairCount; j++) {
                int record = pairSet + 2 + j*(2 + size1 + size2);
                int v = (xAdvance >= 0) ? g.getInt16(record + 2 + xAdvance) : 0;
                records[j] = ((long) g.getUInt16(record) << 40) | ((long) j << 16) | (v & 0xFFFF);
            }
            Arrays.sort(records);
            int[] setSeconds = new int[pairCount];
            short[] setValues = new short[pairCount];
            int n = 0;
            for (long r : records) {
                int second = (int) (r >>> 40);
                if (n > 0 && setSeconds[n - 1] == second) {
                    continue;   // The first record of a pair is the one applied
                }
                setSeconds[n] = second;
                setValues[n] = (short) r;
                n++;
            }
            if (s.pairSets[first] == 0) {   // A glyph covered twice: its first pair set
                seconds.add(Arrays.copyOf(setSeconds, n));
                values.add(Arrays.copyOf(setValues, n));
                s.pairSets[first] = seconds.size();
            }
        }
        s.seconds = seconds.toArray(new int[seconds.size()][]);
        s.values = values.toArray(new short[values.size()][]);
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
    private char[] classDef(int offset) {
        GPOS g = gpos;
        char[] classes = new char[0];
        int limit = (g.numGlyphs > 0) ? g.numGlyphs : 0x10000;
        int format = g.getUInt16(offset);
        if (format == 1) {
            int start = g.getUInt16(offset + 2);
            int count = Math.min(Math.min(g.getUInt16(offset + 4), g.gposWork), limit);
            g.gposWork -= count;
            for (int i = 0; i < count && start + i < limit; i++) {
                int glyphClass = g.getUInt16(offset + 6 + 2*i);
                if (glyphClass != 0) {
                    classes = setClass(classes, start + i, glyphClass);
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
                        classes = setClass(classes, glyph, glyphClass);
                    }
                }
            }
        }
        return classes;
    }

    // Gives the glyph the class, the array made longer as needed.
    private static char[] setClass(char[] classes, int glyph, int glyphClass) {
        if (glyph >= classes.length) {
            classes = Arrays.copyOf(classes, Math.max(glyph + 1, 2*classes.length));
        }
        classes[glyph] = (char) glyphClass;
        return classes;
    }
}   // End of Kerning.java
