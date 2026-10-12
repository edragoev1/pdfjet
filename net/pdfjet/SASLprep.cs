/*
 * SASLprep.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PDFjet.NET {
// Prepares a password as SASLprep, RFC 4013, does, which PDF 2.0 asks of the
// password of a document encrypted with AES-256 (ISO 32000-2, 7.6.4.3.3,
// algorithm 2.A): the non-ASCII spaces mapped to a space, the characters
// "commonly mapped to nothing" left out, the text in normalization form KC,
// and the prohibited characters and a mix of right to left and left to right
// text refused. The password is a query in the words of stringprep, RFC 3454:
// a character Unicode 3.2 did not assign is kept. The tables are those of
// Unicode 3.2, which both RFCs fix, in SASLprepTables.cs; string.Normalize is
// not used, as its version of Unicode is that of the runtime.
internal static class SASLprep {
    // Returns the password prepared, or throws an ArgumentException that names
    // the character that SASLprep prohibits, or that says its text mixes right
    // to left with left to right.
    internal static String Prepare(String password) {
        List<int> mapped = new List<int>(password.Length);
        foreach (int c in CodePoints(password)) {
            if (InRanges(SASLprepTables.space, c)) {
                mapped.Add(' ');
            } else if (!InRanges(SASLprepTables.nothing, c)) {
                mapped.Add(c);
            }
        }
        List<int> prepared = Nfkc(mapped);
        bool hasRandAL = false;
        bool hasL = false;
        foreach (int c in prepared) {
            if (InRanges(SASLprepTables.prohibited, c)) {
                throw new ArgumentException("The password has the character U+" +
                        c.ToString("X4", CultureInfo.InvariantCulture) +
                        ", which PDF 2.0 does not allow in one.");
            }
            hasRandAL = hasRandAL || InRanges(SASLprepTables.randal, c);
            hasL = hasL || InRanges(SASLprepTables.lcat, c);
        }
        // Right to left text has no left to right character, and starts and
        // ends with a right to left one (RFC 3454, 6).
        if (hasRandAL && (hasL ||
                !InRanges(SASLprepTables.randal, prepared[0]) ||
                !InRanges(SASLprepTables.randal, prepared[prepared.Count - 1]))) {
            throw new ArgumentException(
                    "The password mixes right to left with left to right text, which PDF 2.0 does not allow in one.");
        }
        StringBuilder buf = new StringBuilder(prepared.Count);
        foreach (int c in prepared) {
            AppendCodePoint(buf, c);
        }
        return buf.ToString();
    }

    // Returns the bytes of the password as PDF 2.0 asks of AES-256: prepared,
    // in UTF-8, at most 127 of them.
    internal static byte[] PasswordBytes(String password) {
        return Truncate(Prepare(password));
    }

    // Returns the bytes of the password, at most 127, as PDF 2.0 asks, cut
    // before a character that would not fit whole.
    internal static byte[] Truncate(String password) {
        int end = 0;    // The chars of the password that fit
        int length = 0; // Their UTF-8 bytes
        for (int i = 0; i < password.Length; ) {
            int c = password[i];
            int chars = 1;
            if (Char.IsHighSurrogate(password[i]) && i + 1 < password.Length &&
                    Char.IsLowSurrogate(password[i + 1])) {
                c = Char.ConvertToUtf32(password[i], password[i + 1]);
                chars = 2;
            }
            int bytes = (c < 0x80) ? 1 : (c < 0x800) ? 2 : (c < 0x10000) ? 3 : 4;
            if (length + bytes > 127) {
                break;
            }
            length += bytes;
            i += chars;
            end = i;
        }
        return Encoding.UTF8.GetBytes(password.Substring(0, end));
    }

    // Returns the code points of the string, a surrogate pair as one; a lone
    // surrogate is a code point of its own, which SASLprep prohibits.
    private static IEnumerable<int> CodePoints(String s) {
        for (int i = 0; i < s.Length; i++) {
            if (Char.IsHighSurrogate(s[i]) && i + 1 < s.Length && Char.IsLowSurrogate(s[i + 1])) {
                yield return Char.ConvertToUtf32(s[i], s[i + 1]);
                i++;
            } else {
                yield return s[i];
            }
        }
    }

    private static void AppendCodePoint(StringBuilder buf, int c) {
        if (c >= 0x10000) {
            buf.Append(Char.ConvertFromUtf32(c));
        } else {
            buf.Append((char) c);
        }
    }

    // Returns true if the character is in one of the ranges, first and last,
    // in order.
    private static bool InRanges(int[] ranges, int c) {
        int lo = 0;
        int hi = ranges.Length / 2;
        while (lo < hi) {   // The first range whose last is c or more
            int mid = (lo + hi) >> 1;
            if (ranges[2 * mid + 1] >= c) {
                hi = mid;
            } else {
                lo = mid + 1;
            }
        }
        return lo < ranges.Length / 2 && ranges[2 * lo] <= c;
    }

    // The Hangul syllables, which decompose and compose by arithmetic.
    private const int sBase = 0xAC00;
    private const int lBase = 0x1100;
    private const int vBase = 0x1161;
    private const int tBase = 0x11A7;
    private const int lCount = 19;
    private const int vCount = 21;
    private const int tCount = 28;
    private const int nCount = vCount * tCount;
    private const int sCount = lCount * nCount;

    // Returns the characters in normalization form KC: decomposed, the
    // combining marks of each run put in the order of their classes, and
    // composed again, as UAX #15 of Unicode 3.2 says.
    private static List<int> Nfkc(List<int> chars) {
        List<int> decomposed = new List<int>(chars.Count);
        foreach (int c in chars) {
            AppendDecomposition(decomposed, c);
        }
        // The marks of each run of non-zero classes, in the order of their
        // classes, the marks of one class kept in the order they are: an
        // insertion sort, which is stable.
        for (int i = 0; i < decomposed.Count; ) {
            if (CombiningClass(decomposed[i]) == 0) {
                i++;
                continue;
            }
            int j = i;
            while (j < decomposed.Count && CombiningClass(decomposed[j]) != 0) {
                j++;
            }
            for (int k = i + 1; k < j; k++) {
                int c = decomposed[k];
                int cc = CombiningClass(c);
                int m = k - 1;
                while (m >= i && CombiningClass(decomposed[m]) > cc) {
                    decomposed[m + 1] = decomposed[m];
                    m--;
                }
                decomposed[m + 1] = c;
            }
            i = j;
        }
        return Compose(decomposed);
    }

    // Appends the full compatibility decomposition of the character.
    private static void AppendDecomposition(List<int> output, int c) {
        if (c >= sBase && c < sBase + sCount) {
            int s = c - sBase;
            output.Add(lBase + s / nCount);
            output.Add(vBase + (s % nCount) / tCount);
            int t = s % tCount;
            if (t != 0) {
                output.Add(tBase + t);
            }
            return;
        }
        int i = Array.BinarySearch(SASLprepTables.decompCodes, c);
        if (i >= 0) {
            for (int k = SASLprepTables.decompStarts[i]; k < SASLprepTables.decompStarts[i + 1]; k++) {
                output.Add(SASLprepTables.decompValues[k]);
            }
            return;
        }
        output.Add(c);
    }

    // Returns the canonical combining class of the character.
    private static int CombiningClass(int c) {
        int[] ccc = SASLprepTables.ccc;
        int lo = 0;
        int hi = ccc.Length / 2;
        while (lo < hi) {
            int mid = (lo + hi) >> 1;
            if (ccc[2 * mid] >= c) {
                hi = mid;
            } else {
                lo = mid + 1;
            }
        }
        if (lo < ccc.Length / 2 && ccc[2 * lo] == c) {
            return ccc[2 * lo + 1];
        }
        return 0;
    }

    // Returns what the pair composes to, or -1.
    private static int ComposeTo(int first, int second) {
        if (first >= lBase && first < lBase + lCount && second >= vBase && second < vBase + vCount) {
            return sBase + ((first - lBase) * vCount + (second - vBase)) * tCount;
        }
        if (first >= sBase && first < sBase + sCount && (first - sBase) % tCount == 0 &&
                second > tBase && second < tBase + tCount) {
            return first + (second - tBase);
        }
        long key = (long) first * 0x110000 + second;
        int i = Array.BinarySearch(SASLprepTables.composeKeys, key);
        if (i >= 0) {
            return SASLprepTables.composeValues[i];
        }
        return -1;
    }

    // Composes the decomposed characters, each with the last starter before
    // it when nothing between them blocks it, as the sample code of UAX #15
    // does: a mark is blocked by a mark of its class or a higher one before it.
    private static List<int> Compose(List<int> chars) {
        if (chars.Count == 0) {
            return chars;
        }
        List<int> output = new List<int>(chars.Count);
        output.Add(chars[0]);
        int starter = 0;    // The index in output of the last starter
        int lastClass = CombiningClass(chars[0]);
        if (lastClass != 0) {
            lastClass = 256;    // A mark first has no starter to compose with
        }
        for (int i = 1; i < chars.Count; i++) {
            int c = chars[i];
            int cls = CombiningClass(c);
            int composite = ComposeTo(output[starter], c);
            if (composite != -1 && (lastClass < cls || lastClass == 0)) {
                output[starter] = composite;
                continue;
            }
            if (cls == 0) {
                starter = output.Count;
            }
            lastClass = cls;
            output.Add(c);
        }
        return output;
    }
}
}   // End of namespace PDFjet.NET
