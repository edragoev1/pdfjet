package com.pdfjet;

import java.util.HashSet;
import java.util.Set;

/**
 * Bidi.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

/**
 * Provides BIDI processing for Arabic and Hebrew.
 *
 * Please see Example_27.
 */
public class Bidi {

    /* General,Isolated,End,Middle,Beginning */
    private static final int[] forms = {
        0x0623, 0xFE83, 0xFE84, 0x0623, 0x0623,
        0x0628, 0xFE8F, 0xFE90, 0xFE92, 0xFE91,
        0x062A, 0xFE95, 0xFE96, 0xFE98, 0xFE97,
        0x062B, 0xFE99, 0xFE9A, 0xFE9C, 0xFE9B,
        0x062C, 0xFE9D, 0xFE9E, 0xFEA0, 0xFE9F,
        0x062D, 0xFEA1, 0xFEA2, 0xFEA4, 0xFEA3,
        0x062E, 0xFEA5, 0xFEA6, 0xFEA8, 0xFEA7,
        0x062F, 0xFEA9, 0xFEAA, 0x062F, 0x062F,
        0x0630, 0xFEAB, 0xFEAC, 0x0630, 0x0630,
        0x0631, 0xFEAD, 0xFEAE, 0x0631, 0x0631,
        0x0632, 0xFEAF, 0xFEB0, 0x0632, 0x0632,
        0x0633, 0xFEB1, 0xFEB2, 0xFEB4, 0xFEB3,
        0x0634, 0xFEB5, 0xFEB6, 0xFEB8, 0xFEB7,
        0x0635, 0xFEB9, 0xFEBA, 0xFEBC, 0xFEBB,
        0x0636, 0xFEBD, 0xFEBE, 0xFEC0, 0xFEBF,
        0x0637, 0xFEC1, 0xFEC2, 0xFEC4, 0xFEC3,
        0x0638, 0xFEC5, 0xFEC6, 0xFEC8, 0xFEC7,
        0x0639, 0xFEC9, 0xFECA, 0xFECC, 0xFECB,
        0x063A, 0xFECD, 0xFECE, 0xFED0, 0xFECF,
        0x0641, 0xFED1, 0xFED2, 0xFED4, 0xFED3,
        0x0642, 0xFED5, 0xFED6, 0xFED8, 0xFED7,
        0x0643, 0xFED9, 0xFEDA, 0xFEDC, 0xFEDB,
        0x0644, 0xFEDD, 0xFEDE, 0xFEE0, 0xFEDF,
        0x0645, 0xFEE1, 0xFEE2, 0xFEE4, 0xFEE3,
        0x0646, 0xFEE5, 0xFEE6, 0xFEE8, 0xFEE7,
        0x0647, 0xFEE9, 0xFEEA, 0xFEEC, 0xFEEB,
        0x0648, 0xFEED, 0xFEEE, 0x0648, 0x0648,
        0x064A, 0xFEF1, 0xFEF2, 0xFEF4, 0xFEF3,
        0x0622, 0xFE81, 0xFE82, 0x0622, 0x0622,
        0x0629, 0xFE93, 0xFE94, 0x0629, 0x0629,
        0x0649, 0xFEEF, 0xFEF0, 0x0649, 0x0649,
        0x0621, 0xFE80, 0x0621, 0x0621, 0x0621,
        0x0624, 0xFE85, 0xFE86, 0x0624, 0x0624,
        0x0625, 0xFE87, 0xFE88, 0x0625, 0x0625,
        0x0626, 0xFE89, 0xFE8A, 0xFE8C, 0xFE8B,
        0x0627, 0xFE8D, 0xFE8E, 0x0627, 0x0627,
    };

    private Bidi() {
        // Utility class — no instances.
    }

    private static boolean isArabicLetter(int ch) {
        for (int i = 0; i < forms.length; i += 5) {
            if (ch == forms[i]) {
                return true;
            }
        }
        return false;
    }

    /**
     * Returns true if the character is a Transparent joining type
     * (combining mark / diacritic) that should be skipped when
     * determining joining context, and kept attached to its base
     * letter during visual reordering.
     */
    private static boolean isTransparent(int ch) {
        int cat = Character.getType(ch);
        return cat == Character.NON_SPACING_MARK    // Mn
            || cat == Character.ENCLOSING_MARK      // Me
            || cat == Character.FORMAT;             // Cf
    }

    /**
     * Reorders the string so that Arabic and Hebrew text flows from right
     * to left while numbers and Latin text flows from left to right.
     *
     * @param str the input string.
     * @return the reordered string.
     */
    public static String reorderVisually(String str) {
        // Work with code points so that supplementary characters are
        // handled correctly (mirrors Swift's Character iteration).
        int[] input = str.codePoints().toArray();

        StringBuilder buf1 = new StringBuilder();
        StringBuilder buf2 = new StringBuilder();
        boolean rightToLeft = false;

        for (int i = 0; i < input.length; i++) {
            int ch = input[i];

            if (ch == 0x200E) {           // LRM
                rightToLeft = false;
                continue;
            }
            if (ch == 0x200F || ch == 0x061C) {   // RLM / ALM
                rightToLeft = true;
                continue;
            }

           Integer m = mirrored(ch);
            if (isArabic(ch) || isHebrew(ch) || mirrored(ch) != null) {
                rightToLeft = true;
                if (buf2.length() > 0) {
                    buf1.append(process(buf2.toString()));
                    buf2.setLength(0);
                }
                buf1.appendCodePoint(m != null ? m : ch);
            } else if (isAlphaNumeric(ch)) {
                rightToLeft = false;
                buf2.appendCodePoint(ch);
            } else {
                if (rightToLeft) {
                    buf1.appendCodePoint(ch);
                } else {
                    buf2.appendCodePoint(ch);
                }
            }
        }

        if (buf2.length() > 0) {
            buf1.append(process(buf2.toString()));
        }

        // Convert to array for O(1) indexing (fixes Bug #5)
        int[] chars = buf1.codePoints().toArray();
        int n = chars.length;

        StringBuilder buf3 = new StringBuilder();
        int i = n - 1;
        while (i >= 0) {
            int ch = chars[i];

            // If this is a transparent character (diacritic) with no
            // base letter to its right (in buf1 order), emit as-is.
            if (isTransparent(ch)) {
                buf3.appendCodePoint(ch);
                i--;
                continue;
            }

            int diacriticCount = 0;
            int d = i - 1;
            while (d >= 0) {
                if (!isTransparent(chars[d])) {
                    break;
                }
                diacriticCount++;
                d--;
            }

            if (isArabicLetter(ch)) {
                // Find previous non-transparent character (skip diacritics)
                int prevIdx = d;
                while (prevIdx >= 0) {
                    if (!isTransparent(chars[prevIdx])) {
                        break;
                    }
                    prevIdx--;
                }
                int prevCh = prevIdx >= 0 ? chars[prevIdx] : 0x0000;

                // Find next non-transparent character (skip diacritics)
                int nextIdx = i + 1;
                while (nextIdx < n) {
                    if (!isTransparent(chars[nextIdx])) {
                        break;
                    }
                    nextIdx++;
                }
                int nextCh = nextIdx < n ? chars[nextIdx] : 0x0000;

                for (int j = 0; j < forms.length; j += 5) {
                    if (ch == forms[j]) {
                        boolean canJoinPrev = joinsBackward(ch);
                        boolean canJoinNext = joinsForward(ch);
                        boolean prevJoins   = joinsForward(prevCh);
                        boolean nextJoins   = joinsBackward(nextCh);

                        boolean joinsOnLeft  = canJoinPrev && prevJoins;
                        boolean joinsOnRight = canJoinNext && nextJoins;

                        if (!joinsOnLeft && !joinsOnRight) {
                            buf3.appendCodePoint(forms[j + 1]);
                        } else if (joinsOnLeft && !joinsOnRight) {
                            buf3.appendCodePoint(forms[j + 2]);
                        } else if (joinsOnLeft && joinsOnRight) {
                            buf3.appendCodePoint(forms[j + 3]);
                        } else if (!joinsOnLeft && joinsOnRight) {
                            buf3.appendCodePoint(forms[j + 4]);
                        }
                        break;
                    }
                }
            } else {
                buf3.appendCodePoint(ch);
            }

            // Emit diacritics in their original order
            for (int k = 0; k < diacriticCount; k++) {
                buf3.appendCodePoint(chars[i - 1 - k]);
            }

            i = d;
        }

        return buf3.toString();
    }

    public static boolean isArabic(int ch) {
        return ch >= 0x0600 && ch <= 0x06FF;
    }

    private static boolean isHebrew(int ch) {
        return ch >= 0x0590 && ch <= 0x05FF;
    }

    private static boolean isAlphaNumeric(int ch) {
        int cat = Character.getType(ch);
        return cat == Character.DECIMAL_DIGIT_NUMBER    // Nd
            || cat == Character.UPPERCASE_LETTER        // Lu
            || cat == Character.LOWERCASE_LETTER        // Ll
            || cat == Character.TITLECASE_LETTER        // Lt
            || cat == Character.MODIFIER_LETTER         // Lm
            || cat == Character.OTHER_LETTER;           // Lo
    }

    /**
     * Returns the mirror image of a bidirectionally mirrored character,
     * or {@code null} if the character is not mirrored.
     * Data source: Unicode BidiMirroring.txt.
     */
    private static Integer mirrored(int ch) {
        switch (ch) {
        case '(':  return (int) ')';
        case ')':  return (int) '(';
        case '[':  return (int) ']';
        case ']':  return (int) '[';
        case '{':  return (int) '}';
        case '}':  return (int) '{';
        case '<':  return (int) '>';
        case '>':  return (int) '<';
        case 0x00AB: return 0x00BB;   // « »
        case 0x00BB: return 0x00AB;
        case 0x2039: return 0x203A;   // ‹ ›  single angle quotes
        case 0x203A: return 0x2039;
        case 0x207D: return 0x207E;   // superscript ( )
        case 0x207E: return 0x207D;
        case 0x208D: return 0x208E;   // subscript ( )
        case 0x208E: return 0x208D;
        case 0x2308: return 0x2309;   // ⌈ ⌉  left/right ceiling
        case 0x2309: return 0x2308;
        case 0x230A: return 0x230B;   // ⌊ ⌋  left/right floor
        case 0x230B: return 0x230A;
        case 0x2329: return 0x232A;   // ⟨ ⟩  angle brackets
        case 0x232A: return 0x2329;
        case 0xFF08: return 0xFF09;   // fullwidth ( )
        case 0xFF09: return 0xFF08;
        case 0xFF1C: return 0xFF1E;   // fullwidth < >
        case 0xFF1E: return 0xFF1C;
        case 0xFF3B: return 0xFF3D;   // fullwidth [ ]
        case 0xFF3D: return 0xFF3B;
        case 0xFF5B: return 0xFF5D;   // fullwidth { }
        case 0xFF5D: return 0xFF5B;
        case 0xFE59: return 0xFE5A;   // small ( )
        case 0xFE5A: return 0xFE59;
        case 0xFE5B: return 0xFE5C;   // small { }
        case 0xFE5C: return 0xFE5B;
        case 0xFE5D: return 0xFE5E;   // small tortoise shell
        case 0xFE5E: return 0xFE5D;
        case 0xFE64: return 0xFE65;   // small < >
        case 0xFE65: return 0xFE64;
        case 0x3008: return 0x3009;   // CJK 〈 〉
        case 0x3009: return 0x3008;
        case 0x300A: return 0x300B;   // CJK 《 》
        case 0x300B: return 0x300A;
        case 0x3010: return 0x3011;   // CJK BLACK LENTICULAR BRACKET
        case 0x3011: return 0x3010;
        case 0x3014: return 0x3015;   // CJK 〔 〕
        case 0x3015: return 0x3014;
        case 0x3016: return 0x3017;   // CJK 〖 〗
        case 0x3017: return 0x3016;
        case 0x3018: return 0x3019;   // CJK 〘 〙
        case 0x3019: return 0x3018;
        case 0x301A: return 0x301B;   // CJK 〚 〛
        case 0x301B: return 0x301A;
        default:   return null;
        }
    }

    // ---- Joining-type tables ---------------------------------------------

    private static final Set<Integer> DUAL_JOINING;
    private static final Set<Integer> RIGHT_JOINING;

    static {
        Set<Integer> dual = new HashSet<>();
        dual.add(0x0628); // BEH
        dual.add(0x062A); // TEH
        dual.add(0x062B); // THEH
        dual.add(0x062C); // JEEM
        dual.add(0x062D); // HAH
        dual.add(0x062E); // KHAH
        dual.add(0x0633); // SEEN
        dual.add(0x0634); // SHEEN
        dual.add(0x0635); // SAD
        dual.add(0x0636); // DAD
        dual.add(0x0637); // TAH
        dual.add(0x0638); // ZAH
        dual.add(0x0639); // AIN
        dual.add(0x063A); // GHAIN
        dual.add(0x063B); // KEHEH WITH 2 DOTS ABOVE
        dual.add(0x063C); // KEHEH WITH 3 DOTS BELOW
        dual.add(0x063D); // FARSI YEH WITH INVERTED V ABOVE
        dual.add(0x063E); // FARSI YEH WITH 2 DOTS ABOVE
        dual.add(0x063F); // FARSI YEH WITH 3 DOTS ABOVE
        dual.add(0x0641); // FEH
        dual.add(0x0642); // QAF
        dual.add(0x0643); // KAF
        dual.add(0x0644); // LAM
        dual.add(0x0645); // MEEM
        dual.add(0x0646); // NOON
        dual.add(0x0647); // HEH
        dual.add(0x064A); // YEH
        dual.add(0x0626); // YEH WITH HAMZA (Dual_Joining)
        DUAL_JOINING = dual;

        Set<Integer> right = new HashSet<>();
        right.add(0x0622); // ALEF WITH MADDA ABOVE
        right.add(0x0623); // ALEF WITH HAMZA ABOVE
        right.add(0x0624); // WAW WITH HAMZA ABOVE
        right.add(0x0625); // ALEF WITH HAMZA BELOW
        right.add(0x0627); // ALEF
        right.add(0x0629); // TEH MARBUTA
        right.add(0x062F); // DAL
        right.add(0x0630); // THAL
        right.add(0x0631); // REH
        right.add(0x0632); // ZAIN
        right.add(0x0648); // WAW
        right.add(0x0649); // ALEF MAKSURA (DOTLESS YEH)
        RIGHT_JOINING = right;
    }

    public static boolean joinsForward(int ch) {
        if (ch == 0x0640) {
            return true;   // TATWEEL — joins both sides
        }
        return DUAL_JOINING.contains(ch);
    }

    public static boolean joinsBackward(int ch) {
        if (ch == 0x0640) {
            return true;
        }
        if (joinsForward(ch)) {
            return true;
        }
        return RIGHT_JOINING.contains(ch);
    }

    // ---- Helpers ----------------------------------------------------------

    /**
     * Reverses the buffer, then peels separator characters (space, comma,
     * period, hyphen) off the front (which was the end of the LTR run) and
     * re-appends them at the back, so that e.g. trailing punctuation stays
     * visually at the end.
     */
    private static String process(String buf) {
        String buf1 = reverseCodePoints(buf);
        StringBuilder buf2 = new StringBuilder();
        StringBuilder buf3 = new StringBuilder();

        int[] cps = buf1.codePoints().toArray();
        for (int i = 0; i < cps.length; i++) {
            int ch = cps[i];
            if (ch == ' ' || ch == ',' || ch == '.' || ch == '-') {
                buf2.appendCodePoint(ch);
                continue;
            }
            buf3.append(new String(cps, i, cps.length - i));
            buf3.append(reverseCodePoints(buf2.toString()));
            break;
        }

        // If the entire input was separators (loop never hit break),
        // buf3 is empty but buf2 holds the reversed separators.
        // Return them so they aren't silently dropped.
        if (buf3.length() == 0) {
            return reverseCodePoints(buf2.toString());
        }
        return buf3.toString();
    }

    /** Reverses a string at the code-point level (not UTF-16 unit level). */
    private static String reverseCodePoints(String s) {
        int[] cps = s.codePoints().toArray();
        StringBuilder sb = new StringBuilder(cps.length);
        for (int i = cps.length - 1; i >= 0; i--) {
            sb.appendCodePoint(cps[i]);
        }
        return sb.toString();
    }

    /** Appends {@code value} to the end of {@code array}, growing it. */
    private static int[] appendInt(int[] array, int value) {
        int[] result = new int[array.length + 1];
        System.arraycopy(array, 0, result, 0, array.length);
        result[array.length] = value;
        return result;
    }
}
