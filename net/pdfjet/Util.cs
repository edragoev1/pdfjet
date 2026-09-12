/*
 * Util.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>Utility methods.</summary>
public class Util {
    /// <summary>Reads the lines of a UTF-8 text file, without carriage returns.</summary>
    /// <param name="filePath">the path of the text file.</param>
    /// <returns>the lines.</returns>
    public static List<String> ReadLines(String filePath) {
        List<String> lines = new List<String>();
        String contents = Content.OfTextFile(filePath);
        StringBuilder buffer = new StringBuilder();
        foreach (char ch in contents) {
            if (ch == '\n') {
                lines.Add(buffer.ToString());
                buffer.Length = 0;
            } else {
                buffer.Append(ch);
            }
        }
        if (buffer.Length > 0) {
            lines.Add(buffer.ToString());
        }
        return lines;
    }

    internal static string ToHexString(byte[] data) {
        var sb = new StringBuilder(data.Length * 2);
        foreach (byte b in data) {
            sb.AppendFormat("{0:x2}", b);
        }
        return sb.ToString();
    }

    private static readonly char[] HEX = {
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
        'A', 'B', 'C', 'D', 'E', 'F'
    };

    internal string ToHex(String str) {
        if (string.IsNullOrEmpty(str)) {
            return "";
        }

        StringBuilder buf = new StringBuilder(str.Length * 6);
        TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(str);
        while (enumerator.MoveNext()) {
            string textElement = enumerator.GetTextElement();
            int codePoint = char.ConvertToUtf32(textElement, 0);

            if (codePoint == 0xFEFF) continue; // Skip BOM

            if (codePoint <= 0xFFFF) {
                // BMP character (4 hex digits)
                buf.Append(HEX[(codePoint >> 12) & 0xF]);
                buf.Append(HEX[(codePoint >> 8)  & 0xF]);
                buf.Append(HEX[(codePoint >> 4)  & 0xF]);
                buf.Append(HEX[ codePoint        & 0xF]);
            } else {
                // Supplementary character (6 hex digits)
                buf.Append(HEX[(codePoint >> 20) & 0xF]);
                buf.Append(HEX[(codePoint >> 16) & 0xF]);
                buf.Append(HEX[(codePoint >> 12) & 0xF]);
                buf.Append(HEX[(codePoint >> 8)  & 0xF]);
                buf.Append(HEX[(codePoint >> 4)  & 0xF]);
                buf.Append(HEX[ codePoint        & 0xF]);
            }
        }

        return buf.ToString();
    }

    // The ASCII whitespace that Java's \s matches; a no-break space does not break a line.
    private static readonly char[] WHITESPACE = {' ', '\t', '\n', '\x0B', '\f', '\r'};

    /// <summary>
    /// Splits the text on runs of ASCII whitespace, like Java's split("\\s+"),
    /// except that no empty tokens are returned.
    /// </summary>
    internal static String[] SplitOnWhitespace(String text) {
        return text.Split(WHITESPACE, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Removes the leading and trailing characters with a code of 0x20 or less,
    /// like Java's String.trim. .NET's Trim also removes U+00A0 and the other
    /// Unicode spaces, which Java's does not.
    /// </summary>
    internal static String Trim(String str) {
        int start = 0;
        int end = str.Length;
        while (start < end && str[start] <= ' ') {
            start++;
        }
        while (end > start && str[end - 1] <= ' ') {
            end--;
        }
        return (start > 0 || end < str.Length) ? str.Substring(start, end - start) : str;
    }

    /// <summary>
    /// Returns true if the code point is whitespace as Java's Character.isWhitespace
    /// defines it: U+0009 to U+000D, U+001C to U+001F, and the space, line and
    /// paragraph separators except the no-break spaces U+00A0, U+2007 and U+202F.
    /// </summary>
    internal static bool IsJavaWhitespace(int codePoint) {
        if ((codePoint >= 0x09 && codePoint <= 0x0D) || (codePoint >= 0x1C && codePoint <= 0x1F)) {
            return true;
        }
        if (codePoint == 0x00A0 || codePoint == 0x2007 || codePoint == 0x202F) {
            return false;
        }
        UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(codePoint);
        return cat == UnicodeCategory.SpaceSeparator ||
                cat == UnicodeCategory.LineSeparator ||
                cat == UnicodeCategory.ParagraphSeparator;
    }

    /// <summary>
    /// Returns true if more than half of the code points of the string are CJK:
    /// CJK Unified Ideographs (4E00-9FD5), Hiragana (3040-309F), Katakana
    /// (30A0-30FF) or Hangul Jamo (1100-11FF).
    /// </summary>
    internal static bool IsCJK(String str) {
        int numOfCodePoints = 0;
        int numOfCJK = 0;
        for (int i = 0; i < str.Length; i += CharCount(str, i)) {
            int ch = CodePointAt(str, i);
            numOfCodePoints++;
            if ((ch >= 0x4E00 && ch <= 0x9FD5) ||
                    (ch >= 0x3040 && ch <= 0x309F) ||
                    (ch >= 0x30A0 && ch <= 0x30FF) ||
                    (ch >= 0x1100 && ch <= 0x11FF)) {
                numOfCJK++;
            }
        }
        return numOfCJK > (numOfCodePoints / 2);
    }

    /// <summary>
    /// Returns the code point at the index, like Java's String.codePointAt: the
    /// supplementary code point of a surrogate pair, or the char itself.
    /// </summary>
    internal static int CodePointAt(String str, int i) {
        return Char.IsSurrogatePair(str, i) ? Char.ConvertToUtf32(str, i) : str[i];
    }

    /// <summary>Returns the number of chars, 1 or 2, of the code point at the index.</summary>
    internal static int CharCount(String str, int i) {
        return Char.IsSurrogatePair(str, i) ? 2 : 1;
    }
}
}   // End of namespace PDFjet.NET
