/*
 * Util.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.IOException;
import java.nio.ByteBuffer;
import java.util.ArrayList;
import java.util.List;

/**
 * Utility methods.
 */
public class Util {
    /** The default constructor */
    Util() {
    }

    /**
     * Reads the lines of a UTF-8 text file, without carriage returns.
     *
     * @param filePath the path of the text file.
     * @return the lines.
     * @throws IOException if the file cannot be read.
     */
    public static List<String> readLines(String filePath) throws IOException {
        List<String> lines = new ArrayList<>();
        StringBuilder buffer = new StringBuilder();
        for (char ch : Content.ofTextFile(filePath).toCharArray()) {
            if (ch == '\n') {
                lines.add(buffer.toString());
                buffer.setLength(0);
            } else {
                buffer.append(ch);
            }
        }
        if (buffer.length() > 0) {
            lines.add(buffer.toString());
        }
        return lines;
    }

    /**
     * Returns the bytes as a lowercase hexadecimal string.
     *
     * @param data the bytes.
     * @return the hexadecimal string.
     */
    static String toHexString(byte[] data) {
        StringBuilder sb = new StringBuilder(data.length * 2);
        for (byte b : data) {
            // & 0xFF makes the byte unsigned before formatting
            sb.append(String.format("%02x", b & 0xFF));
        }
        return sb.toString();
    }

    private static final char[] HEX = {
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
        'A', 'B', 'C', 'D', 'E', 'F'
    };

    private String toHex(String str) {
        if (str == null || str.isEmpty()) {
            return "";
        }

        StringBuilder buf = new StringBuilder(str.length() * 6);
        str.codePoints().forEach(codePoint -> {
            if (codePoint != 0xFEFF) {  // Skip BOM
                if (codePoint <= 0xFFFF) {
                    // BMP character (4 hex digits)
                    buf.append(HEX[(codePoint >> 12) & 0xF]);
                    buf.append(HEX[(codePoint >> 8)  & 0xF]);
                    buf.append(HEX[(codePoint >> 4)  & 0xF]);
                    buf.append(HEX[(codePoint)       & 0xF]);
                } else {
                    // Supplementary character (6 hex digits)
                    buf.append(HEX[(codePoint >> 20) & 0xF]);
                    buf.append(HEX[(codePoint >> 16) & 0xF]);
                    buf.append(HEX[(codePoint >> 12) & 0xF]);
                    buf.append(HEX[(codePoint >> 8)  & 0xF]);
                    buf.append(HEX[(codePoint >> 4)  & 0xF]);
                    buf.append(HEX[(codePoint)       & 0xF]);
                }
            }
        });

        return buf.toString();
    }

    /**
     * Splits the text on runs of ASCII whitespace: space, tab, line feed,
     * vertical tab, form feed and carriage return. Empty tokens are dropped,
     * so leading and trailing whitespace yield no tokens.
     *
     * @param text the text.
     * @return the non-empty tokens.
     */
    static String[] splitOnWhitespace(String text) {
        List<String> tokens = new ArrayList<String>();
        for (String token : text.split("\\s+")) {
            if (!token.isEmpty()) {
                tokens.add(token);
            }
        }
        return tokens.toArray(new String[] {});
    }

    /**
     * Returns true if more than half of the code points of the string are CJK:
     * CJK Unified Ideographs (4E00-9FD5), Hiragana (3040-309F),
     * Katakana (30A0-30FF) or Hangul Jamo (1100-11FF).
     *
     * @param str the string.
     * @return true if the string is mostly CJK.
     */
    static boolean isCJK(String str) {
        int numOfCJK = 0;
        int i = 0;
        while (i < str.length()) {
            int ch = str.codePointAt(i);
            if ((ch >= 0x4E00 && ch <= 0x9FD5) ||
                    (ch >= 0x3040 && ch <= 0x309F) ||
                    (ch >= 0x30A0 && ch <= 0x30FF) ||
                    (ch >= 0x1100 && ch <= 0x11FF)) {
                numOfCJK++;
            }
            i += Character.charCount(ch);
        }
        return numOfCJK > (str.codePointCount(0, str.length()) / 2);
    }
}
