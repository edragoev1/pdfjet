/**
 * Util.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Text;
using System.Globalization;

namespace PDFjet.NET {
public class Util {
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
}
}   // End of namespace PDFjet.NET
