/*
 * Util.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>Utility methods.</summary>
internal class Util {
    /// <summary>
    /// Returns a copy of the color, or null if the color is null, so that the caller
    /// cannot change the color of an object by writing into the array it passed or got back.
    /// </summary>
    internal static float[] CopyOf(float[] color) {
        return (color == null) ? null : (float[]) color.Clone();
    }

    /// <summary>
    /// Returns the color as a 0xRRGGBB value, each component rounded to the nearest
    /// of 256 steps and kept between 0.0 and 1.0, or -1 if the color is null.
    /// </summary>
    internal static int ToPackedRGB(float[] color) {
        if (color == null) {
            return -1;
        }
        return (ToByte(color[0]) << 16) | (ToByte(color[1]) << 8) | ToByte(color[2]);
    }

    private static int ToByte(float component) {
        float value = Math.Max(0f, Math.Min(1f, component));
        return (int) Math.Floor(value * 255f + 0.5f);   // Rounds half up, as Java's Math.round
    }

    /// <summary>Returns the red, green and blue components, from 0.0 to 1.0, of a 0xRRGGBB color.</summary>
    internal static float[] ToRGB(int color) {
        return new float[] {((color >> 16) & 0xff)/255f, ((color >> 8) & 0xff)/255f, (color & 0xff)/255f};
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

    /// <summary>
    /// Splits one line of a delimited data file into its fields, as RFC 4180
    /// reads them: a field that starts with a quote runs to the closing quote,
    /// a doubled quote inside it stands for one quote, and a delimiter inside
    /// it is part of the text. A field that does not start with a quote keeps
    /// any quotes it holds. The quotes around a field are not part of it.
    /// A file that cannot be read this way is refused rather than guessed at.
    /// </summary>
    internal static String[] Split(String line, String delimiter) {
        return Split(line, delimiter, false);
    }

    // The most lines a record may take, so that a quote that is never closed
    // fails instead of reading the rest of the file into one field.
    internal const int MAX_LINES_IN_RECORD = 10000;

    /// <summary>
    /// Returns the fields of the record of a delimited data file that starts with the line.
    /// When a quoted field holds line breaks, the record goes on over the next lines of the
    /// reader, and each line break in the field is a space, as a table cell is drawn on one line.
    /// Only a record of several lines is looked at for them, so a line costs nothing more to read.
    /// </summary>
    internal static String[] ReadRecord(String line, TextReader reader, String delimiter) {
        String[] fields = Split(line, delimiter, true);
        if (fields != null) {
            return fields;
        }
        StringBuilder record = new StringBuilder(line);
        int lines = 1;
        while (true) {
            String next = reader.ReadLine();
            if (next == null) {
                throw new ArgumentException(
                        "A quoted field is not closed by the end of the data file: " + Excerpt(record.ToString()));
            }
            if (++lines > MAX_LINES_IN_RECORD) {
                throw new ArgumentException("A quoted field is not closed within "
                        + MAX_LINES_IN_RECORD + " lines of the data file: " + Excerpt(record.ToString()));
            }
            record.Append('\n').Append(next);
            // The quoted field goes on until a quote that is not doubled; only
            // then can the record end, so only then is it split again.
            if (ClosesQuotedField(next)) {
                fields = Split(record.ToString(), delimiter, true);
                if (fields != null) {
                    for (int i = 0; i < fields.Length; i++) {
                        fields[i] = LineBreaksToSpaces(fields[i]);
                    }
                    return fields;
                }
            }
        }
    }

    // Returns true when the line, read inside a quoted field, holds the quote
    // that closes it: a quote that is not one of a doubled pair.
    private static bool ClosesQuotedField(String line) {
        int i = line.IndexOf('"');
        while (i != -1) {
            if (i + 1 < line.Length && line[i + 1] == '"') {
                i = line.IndexOf('"', i + 2);
            } else {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns the text with each line break, "\r\n", "\r" or "\n", replaced by a space, for a
    /// table cell that is drawn on one line.
    /// </summary>
    internal static String LineBreaksToSpaces(String text) {
        if (text.IndexOfAny(LINE_BREAKS) == -1) {
            return text;
        }
        return text.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');
    }

    private static readonly char[] LINE_BREAKS = {'\r', '\n'};

    // With open, a line that ends inside a quoted field returns null, for
    // ReadRecord to read on; without it the line is refused.
    private static String[] Split(String line, String delimiter, bool open) {
        if (delimiter.Length == 0) {
            return new String[] {line};
        }
        // A line without a quote is what String.Split reads: the same fields,
        // the empty ones included, by its vectorized single-separator path.
        // BigTable reads every line of its file through here.
        if (line.IndexOf('"') == -1) {
            return line.Split(delimiter, StringSplitOptions.None);
        }
        // Otherwise every field is a substring of the line, as String.Split
        // makes them; the one further copy is of a quoted field that holds a
        // doubled quote.
        List<String> fields = new List<String>();
        int i = 0;
        while (true) {
            if (i < line.Length && line[i] == '"') {
                i++;                            // The quote that opens the field
                int start = i;
                while (true) {
                    int quote = line.IndexOf('"', i);
                    if (quote == -1) {
                        if (open) {
                            return null;
                        }
                        throw new ArgumentException(
                                "A quoted field is not closed on this line of the data file: " + Excerpt(line));
                    }
                    i = quote + 1;
                    if (i < line.Length && line[i] == '"') {
                        i++;                    // Two quotes stand for one; the closing quote is the first single one
                    } else {
                        break;                  // The quote that closes the field
                    }
                }
                // The text between the quotes, where every quote is doubled.
                fields.Add(line.Substring(start, i - 1 - start).Replace("\"\"", "\""));
                if (i < line.Length && String.CompareOrdinal(line, i, delimiter, 0, delimiter.Length) != 0) {
                    throw new ArgumentException(
                            "A quoted field is followed by text on this line of the data file: " + Excerpt(line));
                }
            } else {
                int end = line.IndexOf(delimiter, i, StringComparison.Ordinal);
                if (end == -1) {
                    end = line.Length;
                }
                fields.Add(line.Substring(i, end - i));
                i = end;
            }
            if (i == line.Length) {
                break;
            }
            i += delimiter.Length;              // Step over the delimiter
            if (i == line.Length) {             // The line ends on a delimiter
                fields.Add("");
                break;
            }
        }
        return fields.ToArray();
    }

    // The start of the line, for the message of a file that cannot be read.
    private static String Excerpt(String line) {
        return (line.Length <= 60) ? line : (line.Substring(0, 60) + "...");
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

    // The characters that do not start a line of Chinese or Japanese text: closing
    // brackets and quotes, punctuation, iteration marks, the prolonged sound mark
    // and small kana. And those that do not end one: opening brackets and quotes.
    private const String NOT_AT_LINE_START =
            ")]},.:;!?、。，．・：；？！゛゜ヽヾゝゞ々〻ー‐゠–〜～）］｝」』】〕〉》〙〗〟’”｠»ぁぃぅぇぉっゃゅょゎゕゖァィゥェォッャュョヮヵヶㇰㇱㇲㇳㇴㇵㇶㇷㇸㇹㇺㇻㇼㇽㇾㇿ";
    private const String NOT_AT_LINE_END =
            "([{（［｛「『【〔〈《〘〖〝‘“｟«";

    /// <summary>
    /// Returns where a full line of Chinese or Japanese text ends when the next
    /// character does not fit on it, by the line breaking rules of these languages
    /// (kinsoku shori): when the next character does not start a line, the one
    /// before it moves to the next line with it, and so do opening brackets at the
    /// end of the line. A line that has no other place to break ends where it is full.
    /// </summary>
    internal static int CjkLineEnd(String line, String next) {
        int end = line.Length;
        if (NOT_AT_LINE_START.Contains(next)) {
            while (end > 0) {
                String ch = LastCharacter(line, end);
                end -= ch.Length;
                if (!NOT_AT_LINE_START.Contains(ch)) {
                    break;
                }
            }
        }
        while (end > 0 && NOT_AT_LINE_END.Contains(LastCharacter(line, end))) {
            end -= LastCharacter(line, end).Length;
        }
        return (end > 0) ? end : line.Length;
    }

    // The character that ends at index end of the string.
    private static String LastCharacter(String str, int end) {
        int count = (end >= 2 && Char.IsSurrogatePair(str, end - 2)) ? 2 : 1;
        return str.Substring(end - count, count);
    }
}
}   // End of namespace PDFjet.NET
