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

        // The bidirectional character types of the Unicode Bidirectional
        // Algorithm, https://www.unicode.org/reports/tr9/
        private const int L = 0;        // Left to right
        private const int R = 1;        // Right to left
        private const int AL = 2;       // Arabic letter
        private const int EN = 3;       // European number
        private const int AN = 4;       // Arabic number
        private const int ES = 5;       // European number separator
        private const int ET = 6;       // European number terminator
        private const int CS = 7;       // Common number separator
        private const int NSM = 8;      // Nonspacing mark
        private const int ON = 9;       // Other neutral

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
        /// letter during visual reordering. The zero width non-joiner is
        /// not transparent: it keeps the letters on either side of it from
        /// joining.
        /// </summary>
        private static bool IsTransparent(int ch) {
            if (ch == 0x200C) {                             // ZWNJ
                return false;
            }
            UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            return cat == UnicodeCategory.NonSpacingMark    // Mn
                || cat == UnicodeCategory.EnclosingMark     // Me
                || cat == UnicodeCategory.Format;           // Cf
        }

        /// <summary>
        /// Reorders the string so that Arabic and Hebrew text flows from right
        /// to left while numbers and Latin text flows from left to right.
        /// The string is laid out as a right to left line with left to right
        /// text nested in it one level deep. Spaces, punctuation and brackets
        /// take their direction from the text around them, as in the Unicode
        /// Bidirectional Algorithm.
        /// </summary>
        /// <param name="str">The input string.</param>
        /// <returns>The reordered string.</returns>
        public static string ReorderVisually(string str) {
            // Work with code points so that supplementary characters are
            // handled correctly.
            int[] input = StringToCodePoints(str);
            int[] types = ResolveTypes(input);

            // buf1 gets the right to left text in logical order and each left
            // to right run reversed, so that reversing buf1 below puts the right
            // to left text in visual order and the left to right runs back in
            // theirs.
            StringBuilder buf1 = new StringBuilder();
            StringBuilder buf2 = new StringBuilder();
            for (int j = 0; j < input.Length; j++) {
                int ch = input[j];
                if (types[j] == L) {
                    if (ch != 0x200E) {                         // LRM
                        buf2.AppendCodePoint(ch);
                    }
                    continue;
                }
                // An RLM or ALM is left out, but still ends the left to right run.
                if (buf2.Length > 0) {
                    buf1.Append(ReverseCodePoints(buf2.ToString()));
                    buf2.Length = 0;
                }
                if (ch == 0x200F || ch == 0x061C) {             // RLM, ALM
                    continue;
                }
                // Brackets and the other mirrored characters are mirrored in
                // right to left text.
                int? m = Mirrored(ch);
                buf1.AppendCodePoint(m.HasValue ? m.Value : ch);
            }
            if (buf2.Length > 0) {
                buf1.Append(ReverseCodePoints(buf2.ToString()));
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
                } else if (ch != 0x200C) {
                    // A zero width non-joiner is left out: it only keeps the
                    // letters on either side of it from joining.
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

        /// <summary>Returns true if the character is in the Arabic Unicode block, U+0600 to U+06FF.</summary>
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

        // ---- Bidirectional types ----------------------------------------------

        /// <summary>
        /// Returns the bidirectional character type of the letters, digits and
        /// punctuation used in Arabic, Hebrew and Latin text.
        /// </summary>
        private static int BidiType(int ch) {
            if (ch == 0x200E) {                                         // LRM
                return L;
            }
            if (ch == 0x200F) {                                         // RLM
                return R;
            }
            if (ch == 0x061C) {                                         // ALM
                return AL;
            }
            UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == UnicodeCategory.NonSpacingMark || cat == UnicodeCategory.EnclosingMark) {
                return NSM;
            }
            if ((ch >= '0' && ch <= '9')
                    || ch == 0x00B2 || ch == 0x00B3 || ch == 0x00B9     // superscript 2, 3 and 1
                    || ch == 0x2070 || (ch >= 0x2074 && ch <= 0x2079)   // superscript digits
                    || (ch >= 0x2080 && ch <= 0x2089)                   // subscript digits
                    || (ch >= 0x06F0 && ch <= 0x06F9)                   // extended Arabic-Indic digits
                    || (ch >= 0xFF10 && ch <= 0xFF19)) {                // fullwidth digits
                return EN;
            }
            if ((ch >= 0x0660 && ch <= 0x0669)                          // Arabic-Indic digits
                    || ch == 0x066B || ch == 0x066C) {                  // Arabic decimal and thousands separators
                return AN;
            }
            if (ch == '+' || ch == '-' || ch == 0x2212) {               // minus sign
                return ES;
            }
            if (ch == ',' || ch == '.' || ch == '/' || ch == ':'
                    || ch == 0x00A0 || ch == 0x060C) {                  // no-break space, Arabic comma
                return CS;
            }
            if (ch == '#' || ch == '%'
                    || ch == 0x00B0 || ch == 0x00B1                     // degree, plus-minus
                    || ch == 0x0609 || ch == 0x060A || ch == 0x066A     // Arabic per mille, per ten thousand, percent
                    || (ch >= 0x2030 && ch <= 0x2034)) {                // per mille, per ten thousand, primes
                return ET;
            }
            if (IsHebrew(ch)) {
                return R;
            }
            if (IsArabic(ch)) {
                return AL;
            }
            if (cat == UnicodeCategory.CurrencySymbol) {
                return ET;
            }
            if (IsAlphaNumeric(ch)) {
                return L;
            }
            return ON;
        }

        /// <summary>
        /// Resolves the direction of each code point with the rules of the
        /// Unicode Bidirectional Algorithm for a right to left line without
        /// explicit embeddings: W1 to W7, N0 to N2 and I2.
        /// </summary>
        /// <param name="input">The code points.</param>
        /// <returns>L for each code point in a left to right run and R for the others.</returns>
        private static int[] ResolveTypes(int[] input) {
            int n = input.Length;
            int[] classes = new int[n];
            for (int i = 0; i < n; i++) {
                classes[i] = BidiType(input[i]);
            }
            int[] types = (int[]) classes.Clone();

            // W1: a nonspacing mark takes the type of the character before it.
            for (int i = 0; i < n; i++) {
                if (types[i] == NSM) {
                    types[i] = (i == 0) ? R : types[i - 1];
                }
            }

            // W2: a European number after an Arabic letter is an Arabic number.
            // W3: an Arabic letter is right to left.
            int lastStrong = R;
            for (int i = 0; i < n; i++) {
                if (types[i] == AL) {
                    lastStrong = AL;
                    types[i] = R;
                } else if (types[i] == L || types[i] == R) {
                    lastStrong = types[i];
                } else if (types[i] == EN && lastStrong == AL) {
                    types[i] = AN;
                }
            }

            // W4: a single separator between two numbers of the same kind is
            // part of the number.
            for (int i = 1; i < n - 1; i++) {
                int before = types[i - 1];
                int after = types[i + 1];
                if (types[i] == ES && before == EN && after == EN) {
                    types[i] = EN;
                } else if (types[i] == CS && before == after && (before == EN || before == AN)) {
                    types[i] = before;
                }
            }

            // W5: currency, percent and similar signs next to a European number
            // are part of the number.
            int start = 0;
            while (start < n) {
                if (types[start] != ET) {
                    start++;
                    continue;
                }
                int end = start;
                while (end < n && types[end] == ET) {
                    end++;
                }
                if ((start > 0 && types[start - 1] == EN) || (end < n && types[end] == EN)) {
                    Array.Fill(types, EN, start, end - start);
                }
                start = end;
            }

            // W6: the other separators and terminators are neutral.
            for (int i = 0; i < n; i++) {
                if (types[i] == ES || types[i] == ET || types[i] == CS) {
                    types[i] = ON;
                }
            }

            // W7: a European number after left to right text is left to right.
            lastStrong = R;
            for (int i = 0; i < n; i++) {
                if (types[i] == L || types[i] == R) {
                    lastStrong = types[i];
                } else if (types[i] == EN && lastStrong == L) {
                    types[i] = L;
                }
            }

            // N0: both brackets of a pair take the same direction.
            ResolveBrackets(input, classes, types);

            // N1, N2: neutral characters with left to right text on both sides
            // are left to right, and the others are right to left. Numbers count
            // as right to left here, and so do the start and the end of the line.
            start = 0;
            while (start < n) {
                if (types[start] != ON) {
                    start++;
                    continue;
                }
                int end = start;
                while (end < n && types[end] == ON) {
                    end++;
                }
                bool leftToRight =
                        start > 0 && types[start - 1] == L && end < n && types[end] == L;
                Array.Fill(types, leftToRight ? L : R, start, end - start);
                start = end;
            }

            // I2: numbers are displayed left to right.
            for (int i = 0; i < n; i++) {
                if (types[i] != R) {
                    types[i] = L;
                }
            }
            return types;
        }

        /// <summary>
        /// Applies rule N0. Finds the pairs of brackets with rule BD16 and gives
        /// both brackets of a pair the direction of the text between them, or
        /// of the text before them if the text between them is left to right.
        /// </summary>
        private static void ResolveBrackets(int[] input, int[] classes, int[] types) {
            int n = input.Length;
            int[] closing = new int[n];     // The position of each opening bracket's pair
            Array.Fill(closing, -1);
            int[] stack = new int[63];
            int depth = 0;
            for (int i = 0; i < n; i++) {
                if (types[i] != ON) {
                    continue;
                }
                int ch = input[i];
                if (IsOpeningBracket(ch)) {
                    if (depth == stack.Length) {
                        break;
                    }
                    stack[depth++] = i;
                    continue;
                }
                int? m = Mirrored(ch);
                if (!m.HasValue || !IsOpeningBracket(m.Value)) {
                    continue;
                }
                for (int k = depth - 1; k >= 0; k--) {
                    if (input[stack[k]] == m.Value) {
                        closing[stack[k]] = i;
                        depth = k;
                        break;
                    }
                }
            }

            for (int open = 0; open < n; open++) {
                int close = closing[open];
                if (close < 0) {
                    continue;
                }
                int direction = ON;
                for (int i = open + 1; i < close; i++) {
                    int strong = StrongDirection(types[i]);
                    if (strong == R) {
                        direction = R;
                        break;
                    }
                    if (strong == L) {
                        direction = L;
                    }
                }
                if (direction == L) {
                    direction = R;
                    for (int i = open - 1; i >= 0; i--) {
                        int strong = StrongDirection(types[i]);
                        if (strong != ON) {
                            direction = strong;
                            break;
                        }
                    }
                }
                if (direction != ON) {
                    SetBracketType(classes, types, open, direction);
                    SetBracketType(classes, types, close, direction);
                }
            }
        }

        /// <summary>Sets the type of a bracket and of the nonspacing marks after it.</summary>
        private static void SetBracketType(int[] classes, int[] types, int i, int type) {
            types[i] = type;
            for (int k = i + 1; k < types.Length && classes[k] == NSM; k++) {
                types[k] = type;
            }
        }

        /// <summary>Returns L or R for a strong type, with numbers counting as R, or ON.</summary>
        private static int StrongDirection(int type) {
            if (type == L) {
                return L;
            }
            if (type == R || type == EN || type == AN) {
                return R;
            }
            return ON;
        }

        /// <summary>
        /// Returns true if the character is an opening bracket that pairs with the
        /// closing bracket it mirrors. The angle brackets and angle quotation marks
        /// in the mirrored table are not paired brackets.
        /// </summary>
        private static bool IsOpeningBracket(int ch) {
            switch (ch) {
                case '(': case '[': case '{':
                case 0x207D: case 0x208D:                   // superscript and subscript (
                case 0x2308: case 0x230A:                   // left ceiling and floor
                case 0x2329:                                // left-pointing angle bracket
                case 0x3008: case 0x300A: case 0x3010:      // CJK brackets
                case 0x3014: case 0x3016: case 0x3018: case 0x301A:
                case 0xFE59: case 0xFE5B: case 0xFE5D:      // small ( { and tortoise shell
                case 0xFF08: case 0xFF3B: case 0xFF5B:      // fullwidth ( [ {
                    return true;
                default:
                    return false;
            }
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

        /// <summary>Returns true if the Arabic character joins the character that follows it.</summary>
        public static bool JoinsForward(int ch) {
            if (ch == 0x0640) {
                return true;   // TATWEEL — joins both sides
            }
            return DUAL_JOINING.Contains(ch);
        }

        /// <summary>Returns true if the Arabic character joins the character before it.</summary>
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
        /// Converts a string to an array of Unicode code points. A lone
        /// surrogate is kept as a code point of its own, as Java's
        /// String.codePoints() keeps it.
        /// </summary>
        private static int[] StringToCodePoints(string s) {
            if (string.IsNullOrEmpty(s)) {
                return new int[0];
            }
            int[] result = new int[s.Length];
            int idx = 0;
            int i = 0;
            while (i < s.Length) {
                if (char.IsSurrogatePair(s, i)) {
                    result[idx++] = char.ConvertToUtf32(s, i);
                    i += 2;
                } else {
                    result[idx++] = s[i];
                    i++;
                }
            }
            int[] trimmed = new int[idx];
            Array.Copy(result, trimmed, idx);
            return trimmed;
        }
    }

    /// <summary>
    /// Extension methods for StringBuilder to support code-point appends,
    /// matching Java's StringBuilder.appendCodePoint().
    /// </summary>
    internal static class StringBuilderExtensions {
        /// <summary>
        /// Appends a Unicode code point to the StringBuilder. A lone surrogate
        /// is appended as it is.
        /// </summary>
        public static void AppendCodePoint(this StringBuilder sb, int codePoint) {
            if (codePoint >= 0xD800 && codePoint <= 0xDFFF) {
                sb.Append((char) codePoint);
            } else {
                sb.Append(char.ConvertFromUtf32(codePoint));
            }
        }
    }
}   // End of namespace PDFjet.NET
