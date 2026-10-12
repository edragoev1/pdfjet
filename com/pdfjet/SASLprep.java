/*
 * SASLprep.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.nio.charset.StandardCharsets;
import java.util.Arrays;
import java.util.Locale;

/**
 * Prepares a password as SASLprep, RFC 4013, does, which PDF 2.0 asks of the
 * password of a document encrypted with AES-256 (ISO 32000-2, 7.6.4.3.3,
 * algorithm 2.A): the non-ASCII spaces mapped to a space, the characters
 * "commonly mapped to nothing" left out, the text in normalization form KC,
 * and the prohibited characters and a mix of right to left and left to right
 * text refused. The password is a query in the words of stringprep, RFC 3454:
 * a character Unicode 3.2 did not assign is kept. The tables are those of
 * Unicode 3.2, which both RFCs fix, in SASLprepTables, the same in every port:
 * not java.text.Normalizer, whose Unicode is that of the Java it runs on.
 */
final class SASLprep {
    private SASLprep() {
    }

    // The tables, parsed from their hexadecimal text once, when first used.
    private static final class Tables {
        static final int[] space = parseInts(SASLprepTables.space);
        static final int[] nothing = parseInts(SASLprepTables.nothing);
        static final int[] prohibited = parseInts(SASLprepTables.prohibited);
        static final int[] randal = parseInts(SASLprepTables.randal);
        static final int[] lcat = parseInts(SASLprepTables.lcat);
        static final int[] ccc = parseInts(SASLprepTables.ccc);
        static final int[] decompCodes = parseInts(SASLprepTables.decompCodes);
        static final int[] decompStarts = parseInts(SASLprepTables.decompStarts);
        static final int[] decompValues = parseInts(SASLprepTables.decompValues);
        static final long[] composeKeys = parseLongs(SASLprepTables.composeKeys);
        static final int[] composeValues = parseInts(SASLprepTables.composeValues);

        private static long[] parseLongs(String[] chunks) {
            int count = 0;
            for (String chunk : chunks) {
                count += chunk.trim().split(" ").length;
            }
            long[] values = new long[count];
            int i = 0;
            for (String chunk : chunks) {
                for (String hex : chunk.trim().split(" ")) {
                    values[i++] = Long.parseLong(hex, 16);
                }
            }
            return values;
        }

        private static int[] parseInts(String[] chunks) {
            long[] longs = parseLongs(chunks);
            int[] values = new int[longs.length];
            for (int i = 0; i < longs.length; i++) {
                values[i] = (int) longs[i];
            }
            return values;
        }
    }

    /**
     * Returns the password prepared.
     *
     * @param password the password.
     * @return the password prepared.
     * @throws IllegalArgumentException that names the character that SASLprep
     *     prohibits, or that says its text mixes right to left with left to right.
     */
    static String prepare(String password) {
        int[] mapped = new int[password.length()];
        int n = 0;
        for (int i = 0; i < password.length(); ) {
            int c = password.codePointAt(i);
            i += Character.charCount(c);
            if (inRanges(Tables.space, c)) {
                mapped[n++] = ' ';
            } else if (!inRanges(Tables.nothing, c)) {
                mapped[n++] = c;
            }
        }
        int[] prepared = nfkc(Arrays.copyOf(mapped, n));
        boolean hasRandAL = false;
        boolean hasL = false;
        for (int c : prepared) {
            if (inRanges(Tables.prohibited, c)) {
                throw new IllegalArgumentException("The password has the character U+"
                        + String.format(Locale.ROOT, "%04X", c)
                        + ", which PDF 2.0 does not allow in one.");
            }
            hasRandAL = hasRandAL || inRanges(Tables.randal, c);
            hasL = hasL || inRanges(Tables.lcat, c);
        }
        // Right to left text has no left to right character, and starts and
        // ends with a right to left one (RFC 3454, 6).
        if (hasRandAL && (hasL || !inRanges(Tables.randal, prepared[0])
                || !inRanges(Tables.randal, prepared[prepared.length - 1]))) {
            throw new IllegalArgumentException(
                    "The password mixes right to left with left to right text, which PDF 2.0 does not allow in one.");
        }
        return new String(prepared, 0, prepared.length);
    }

    /**
     * Returns the bytes of the password as PDF 2.0 asks of AES-256: prepared,
     * in UTF-8, at most 127 of them.
     *
     * @param password the password.
     * @return the bytes.
     * @throws IllegalArgumentException if SASLprep refuses the password.
     */
    static byte[] passwordBytes(String password) {
        return truncate(prepare(password));
    }

    /**
     * Returns the first 127 UTF-8 bytes of the password, as PDF 2.0 asks, even
     * when that cuts a character in two, as other readers and writers do.
     *
     * @param password the password.
     * @return the bytes.
     */
    static byte[] truncate(String password) {
        byte[] bytes = password.getBytes(StandardCharsets.UTF_8);
        return (bytes.length > 127) ? Arrays.copyOf(bytes, 127) : bytes;
    }

    // Returns true if the character is in one of the ranges, first and last,
    // in order.
    private static boolean inRanges(int[] ranges, int c) {
        int lo = 0;
        int hi = ranges.length / 2;
        while (lo < hi) {
            int mid = (lo + hi) >>> 1;
            if (ranges[2 * mid + 1] >= c) {
                hi = mid;
            } else {
                lo = mid + 1;
            }
        }
        return lo < ranges.length / 2 && ranges[2 * lo] <= c;
    }

    // The Hangul syllables, which decompose and compose by arithmetic.
    private static final int S_BASE = 0xAC00;
    private static final int L_BASE = 0x1100;
    private static final int V_BASE = 0x1161;
    private static final int T_BASE = 0x11A7;
    private static final int L_COUNT = 19;
    private static final int V_COUNT = 21;
    private static final int T_COUNT = 28;
    private static final int N_COUNT = V_COUNT * T_COUNT;
    private static final int S_COUNT = L_COUNT * N_COUNT;

    // Returns the characters in normalization form KC: decomposed, the
    // combining marks of each run put in the order of their classes, and
    // composed again, as UAX #15 of Unicode 3.2 says.
    private static int[] nfkc(int[] chars) {
        IntList decomposed = new IntList(chars.length);
        for (int c : chars) {
            appendDecomposition(decomposed, c);
        }
        int[] d = decomposed.toArray();
        // The marks of each run of non-zero classes, in the order of their
        // classes, the marks of one class kept in the order they are.
        for (int i = 0; i < d.length; ) {
            if (combiningClass(d[i]) == 0) {
                i++;
                continue;
            }
            int j = i;
            while (j < d.length && combiningClass(d[j]) != 0) {
                j++;
            }
            // A stable insertion sort of the run
            for (int k = i + 1; k < j; k++) {
                int c = d[k];
                int cls = combiningClass(c);
                int m = k - 1;
                while (m >= i && combiningClass(d[m]) > cls) {
                    d[m + 1] = d[m];
                    m--;
                }
                d[m + 1] = c;
            }
            i = j;
        }
        return compose(d);
    }

    // Appends the full compatibility decomposition of the character.
    private static void appendDecomposition(IntList out, int c) {
        if (c >= S_BASE && c < S_BASE + S_COUNT) {
            int s = c - S_BASE;
            out.add(L_BASE + s / N_COUNT);
            out.add(V_BASE + (s % N_COUNT) / T_COUNT);
            int t = s % T_COUNT;
            if (t != 0) {
                out.add(T_BASE + t);
            }
            return;
        }
        int i = Arrays.binarySearch(Tables.decompCodes, c);
        if (i >= 0) {
            for (int k = Tables.decompStarts[i]; k < Tables.decompStarts[i + 1]; k++) {
                out.add(Tables.decompValues[k]);
            }
            return;
        }
        out.add(c);
    }

    // Returns the canonical combining class of the character.
    private static int combiningClass(int c) {
        int[] ccc = Tables.ccc;
        int lo = 0;
        int hi = ccc.length / 2;
        while (lo < hi) {
            int mid = (lo + hi) >>> 1;
            if (ccc[2 * mid] >= c) {
                hi = mid;
            } else {
                lo = mid + 1;
            }
        }
        if (lo < ccc.length / 2 && ccc[2 * lo] == c) {
            return ccc[2 * lo + 1];
        }
        return 0;
    }

    // Returns what the pair composes to, or -1.
    private static int composeTo(int first, int second) {
        if (first >= L_BASE && first < L_BASE + L_COUNT && second >= V_BASE && second < V_BASE + V_COUNT) {
            return S_BASE + ((first - L_BASE) * V_COUNT + (second - V_BASE)) * T_COUNT;
        }
        if (first >= S_BASE && first < S_BASE + S_COUNT && (first - S_BASE) % T_COUNT == 0
                && second > T_BASE && second < T_BASE + T_COUNT) {
            return first + (second - T_BASE);
        }
        long key = (long) first * 0x110000L + second;
        int i = Arrays.binarySearch(Tables.composeKeys, key);
        if (i >= 0) {
            return Tables.composeValues[i];
        }
        return -1;
    }

    // Composes the decomposed characters, each with the last starter before
    // it when nothing between them blocks it, as the sample code of UAX #15
    // does: a mark is blocked by a mark of its class or a higher one before it.
    private static int[] compose(int[] chars) {
        if (chars.length == 0) {
            return chars;
        }
        int[] out = new int[chars.length];
        out[0] = chars[0];
        int n = 1;
        int starter = 0; // The index in out of the last starter
        int lastClass = combiningClass(chars[0]);
        if (lastClass != 0) {
            lastClass = 256; // A mark first has no starter to compose with
        }
        for (int i = 1; i < chars.length; i++) {
            int c = chars[i];
            int cls = combiningClass(c);
            int composite = composeTo(out[starter], c);
            if (composite != -1 && (lastClass < cls || lastClass == 0)) {
                out[starter] = composite;
                continue;
            }
            if (cls == 0) {
                starter = n;
            }
            lastClass = cls;
            out[n++] = c;
        }
        return Arrays.copyOf(out, n);
    }

    // A growable list of characters.
    private static final class IntList {
        private int[] values;
        private int size;

        IntList(int capacity) {
            values = new int[Math.max(capacity, 4)];
        }

        void add(int value) {
            if (size == values.length) {
                values = Arrays.copyOf(values, 2 * size);
            }
            values[size++] = value;
        }

        int[] toArray() {
            return Arrays.copyOf(values, size);
        }
    }
}
