using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PDFjet.NET {
    /// <summary>
    /// Bidi.cs
    ///
    /// Copyright (c) 2026 PDFjet Software
    /// Licensed under the MIT License. See LICENSE file in the project root.
    /// </summary>

    /// <summary>
    /// Provides BIDI processing for Arabic and Hebrew.
    ///
    /// Please see Example_27.
    /// </summary>
    public static class Bidi {
        /* General, Isolated, End, Middle, Beginning */
        private static readonly int[] forms = {
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

        private static bool IsArabicLetter(int ch) {
            for (int i = 0; i < forms.Length; i += 5) {
                if (ch == forms[i]) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if the character is a Transparent joining type
        /// (combining mark / diacritic) that should be skipped when
        /// determining joining context, and kept attached to its base
        /// letter during visual reordering.
        /// </summary>
        private static bool IsTransparent(int ch) {
            UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            return cat == UnicodeCategory.NonSpacingMark    // Mn
                || cat == UnicodeCategory.EnclosingMark     // Me
                || cat == UnicodeCategory.Format;           // Cf
        }

        /// <summary>
        /// Reorders the string so that Arabic and Hebrew text flows from right
        /// to left while numbers and Latin text flows from left to right.
        /// </summary>
        /// <param name="str">The input string.</param>
        /// <returns>The reordered string.</returns>
        public static string ReorderVisually(string str) {
            // Work with code points so that supplementary characters are
            // handled correctly (mirrors Swift's Character iteration).
            int[] input = StringToCodePoints(str);

            StringBuilder buf1 = new StringBuilder();
            StringBuilder buf2 = new StringBuilder();
            bool rightToLeft = false;

            for (int j = 0; j < input.Length; j++) {
                int ch = input[j];

                if (ch == 0x200E) {                 // LRM
                    rightToLeft = false;
                    continue;
                }
                if (ch == 0x200F || ch == 0x061C) { // RLM / ALM
                    rightToLeft = true;
                    continue;
                }

                int? m = Mirrored(ch);
                if (IsArabic(ch) || IsHebrew(ch) || m.HasValue) {
                    rightToLeft = true;
                    if (buf2.Length > 0) {
                        buf1.Append(Process(buf2.ToString()));
                        buf2.Length = 0;
                    }
                    buf1.AppendCodePoint(m.HasValue ? m.Value : ch);
                } else if (IsAlphaNumeric(ch)) {
                    rightToLeft = false;
                    buf2.AppendCodePoint(ch);
                } else {
                    if (rightToLeft) {
                        buf1.AppendCodePoint(ch);
                    } else {
                        buf2.AppendCodePoint(ch);
                    }
                }
            }

            if (buf2.Length > 0) {
                buf1.Append(Process(buf2.ToString()));
            }

            // Convert to array for O(1) indexing (fixes Bug #5)
            int[] chars = StringToCodePoints(buf1.ToString());
            int n = chars.Length;

            StringBuilder buf3 = new StringBuilder();
            int i = n - 1;
            while (i >= 0) {
                int ch = chars[i];

                // If this is a transparent character (diacritic) with no
                // base letter to its right (in buf1 order), emit as-is.
                if (IsTransparent(ch)) {
                    buf3.AppendCodePoint(ch);
                    i--;
                    continue;
                }

                int diacriticCount = 0;
                int d = i - 1;
                while (d >= 0) {
                    if (!IsTransparent(chars[d])) {
                        break;
                    }
                    diacriticCount++;
                    d--;
                }

                if (IsArabicLetter(ch)) {
                    // Find previous non-transparent character (skip diacritics)
                    int prevIdx = d;
                    while (prevIdx >= 0) {
                        if (!IsTransparent(chars[prevIdx])) {
                            break;
                        }
                        prevIdx--;
                    }
                    int prevCh = prevIdx >= 0 ? chars[prevIdx] : 0x0000;

                    // Find next non-transparent character (skip diacritics)
                    int nextIdx = i + 1;
                    while (nextIdx < n) {
                        if (!IsTransparent(chars[nextIdx])) {
                            break;
                        }
                        nextIdx++;
                    }
                    int nextCh = nextIdx < n ? chars[nextIdx] : 0x0000;

                    for (int j = 0; j < forms.Length; j += 5) {
                        if (ch == forms[j]) {
                            bool canJoinPrev = JoinsBackward(ch);
                            bool canJoinNext = JoinsForward(ch);
                            bool prevJoins   = JoinsForward(prevCh);
                            bool nextJoins   = JoinsBackward(nextCh);

                            bool joinsOnLeft  = canJoinPrev && prevJoins;
                            bool joinsOnRight = canJoinNext && nextJoins;

                            if (!joinsOnLeft && !joinsOnRight) {
                                buf3.AppendCodePoint(forms[j + 1]);
                            } else if (joinsOnLeft && !joinsOnRight) {
                                buf3.AppendCodePoint(forms[j + 2]);
                            } else if (joinsOnLeft && joinsOnRight) {
                                buf3.AppendCodePoint(forms[j + 3]);
                            } else if (!joinsOnLeft && joinsOnRight) {
                                buf3.AppendCodePoint(forms[j + 4]);
                            }
                            break;
                        }
                    }
                } else {
                    buf3.AppendCodePoint(ch);
                }

                // Emit diacritics in their original order
                for (int k = 0; k < diacriticCount; k++) {
                    buf3.AppendCodePoint(chars[i - 1 - k]);
                }

                i = d;
            }

            return buf3.ToString();
        }

        public static bool IsArabic(int ch) {
            return ch >= 0x0600 && ch <= 0x06FF;
        }

        private static bool IsHebrew(int ch) {
            return ch >= 0x0590 && ch <= 0x05FF;
        }

        private static bool IsAlphaNumeric(int ch) {
            UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            return cat == UnicodeCategory.DecimalDigitNumber    // Nd
                || cat == UnicodeCategory.UppercaseLetter       // Lu
                || cat == UnicodeCategory.LowercaseLetter       // Ll
                || cat == UnicodeCategory.TitlecaseLetter       // Lt
                || cat == UnicodeCategory.ModifierLetter        // Lm
                || cat == UnicodeCategory.OtherLetter;          // Lo
        }

        /// <summary>
        /// Returns the mirror image of a bidirectionally mirrored character,
        /// or <c>null</c> if the character is not mirrored.
        /// Data source: Unicode BidiMirroring.txt.
        /// </summary>
        private static int? Mirrored(int ch) {
            switch (ch) {
                case '(':  return ')';
                case ')':  return '(';
                case '[':  return ']';
                case ']':  return '[';
                case '{':  return '}';
                case '}':  return '{';
                case '<':  return '>';
                case '>':  return '<';
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

        private static readonly HashSet<int> DUAL_JOINING;
        private static readonly HashSet<int> RIGHT_JOINING;

        static Bidi() {
            DUAL_JOINING = new HashSet<int> {
                0x0628, // BEH
                0x062A, // TEH
                0x062B, // THEH
                0x062C, // JEEM
                0x062D, // HAH
                0x062E, // KHAH
                0x0633, // SEEN
                0x0634, // SHEEN
                0x0635, // SAD
                0x0636, // DAD
                0x0637, // TAH
                0x0638, // ZAH
                0x0639, // AIN
                0x063A, // GHAIN
                0x063B, // KEHEH WITH 2 DOTS ABOVE
                0x063C, // KEHEH WITH 3 DOTS BELOW
                0x063D, // FARSI YEH WITH INVERTED V ABOVE
                0x063E, // FARSI YEH WITH 2 DOTS ABOVE
                0x063F, // FARSI YEH WITH 3 DOTS ABOVE
                0x0641, // FEH
                0x0642, // QAF
                0x0643, // KAF
                0x0644, // LAM
                0x0645, // MEEM
                0x0646, // NOON
                0x0647, // HEH
                0x064A, // YEH
                0x0626, // YEH WITH HAMZA (Dual_Joining)
            };

            RIGHT_JOINING = new HashSet<int> {
                0x0622, // ALEF WITH MADDA ABOVE
                0x0623, // ALEF WITH HAMZA ABOVE
                0x0624, // WAW WITH HAMZA ABOVE
                0x0625, // ALEF WITH HAMZA BELOW
                0x0627, // ALEF
                0x0629, // TEH MARBUTA
                0x062F, // DAL
                0x0630, // THAL
                0x0631, // REH
                0x0632, // ZAIN
                0x0648, // WAW
                0x0649, // ALEF MAKSURA (DOTLESS YEH)
            };
        }

        public static bool JoinsForward(int ch) {
            if (ch == 0x0640) {
                return true;   // TATWEEL — joins both sides
            }
            return DUAL_JOINING.Contains(ch);
        }

        public static bool JoinsBackward(int ch) {
            if (ch == 0x0640) {
                return true;
            }
            if (JoinsForward(ch)) {
                return true;
            }
            return RIGHT_JOINING.Contains(ch);
        }

        // ---- Helpers ----------------------------------------------------------

        /// <summary>
        /// Reverses the buffer, then peels separator characters (space, comma,
        /// period, hyphen) off the front (which was the end of the LTR run) and
        /// re-appends them at the back, so that e.g. trailing punctuation stays
        /// visually at the end.
        /// </summary>
        private static string Process(string buf) {
            string buf1 = ReverseCodePoints(buf);
            StringBuilder buf2 = new StringBuilder();
            StringBuilder buf3 = new StringBuilder();

            int[] cps = StringToCodePoints(buf1);
            for (int j = 0; j < cps.Length; j++) {
                int ch = cps[j];
                if (ch == ' ' || ch == ',' || ch == '.' || ch == '-') {
                    buf2.AppendCodePoint(ch);
                    continue;
                }
                buf3.Append(CodePointsToString(cps, j, cps.Length - j));
                buf3.Append(ReverseCodePoints(buf2.ToString()));
                break;
            }

            // If the entire input was separators (loop never hit break),
            // buf3 is empty but buf2 holds the reversed separators.
            // Return them so they aren't silently dropped.
            if (buf3.Length == 0) {
                return ReverseCodePoints(buf2.ToString());
            }
            return buf3.ToString();
        }

        /// <summary>
        /// Reverses a string at the code-point level (not UTF-16 unit level).
        /// </summary>
        private static string ReverseCodePoints(string s) {
            int[] cps = StringToCodePoints(s);
            StringBuilder sb = new StringBuilder(cps.Length);
            for (int i = cps.Length - 1; i >= 0; i--) {
                sb.AppendCodePoint(cps[i]);
            }
            return sb.ToString();
        }

        // ---- Code-point utilities --------------------------------------------

        /// <summary>
        /// Converts a string to an array of Unicode code points.
        /// </summary>
        private static int[] StringToCodePoints(string s) {
            if (string.IsNullOrEmpty(s)) {
                return new int[0];
            }
            int[] result = new int[s.Length];
            int idx = 0;
            int i = 0;
            while (i < s.Length) {
                result[idx++] = char.ConvertToUtf32(s, i);
                i += char.IsSurrogatePair(s, i) ? 2 : 1;
            }
            int[] trimmed = new int[idx];
            Array.Copy(result, trimmed, idx);
            return trimmed;
        }

        /// <summary>
        /// Converts an array of code points to a string.
        /// </summary>
        private static string CodePointsToString(int[] cps, int offset, int count) {
            StringBuilder sb = new StringBuilder(count);
            int end = offset + count;
            for (int i = offset; i < end; i++) {
                sb.AppendCodePoint(cps[i]);
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Extension methods for StringBuilder to support code-point appends,
    /// matching Java's StringBuilder.appendCodePoint().
    /// </summary>
    internal static class StringBuilderExtensions {
        /// <summary>
        /// Appends a Unicode code point to the StringBuilder.
        /// </summary>
        public static void AppendCodePoint(this StringBuilder sb, int codePoint) {
            sb.Append(char.ConvertFromUtf32(codePoint));
        }
    }
}   // End of namespace PDFjet.NET
