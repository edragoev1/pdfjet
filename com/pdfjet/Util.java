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
class Util {
    /** The default constructor */
    Util() {
    }

    /**
     * Returns a copy of the color, so that the caller cannot change the color
     * of an object by writing into the array it passed or got back.
     *
     * @param color the red, green and blue components, or null.
     * @return the copy, or null if the color is null.
     */
    static float[] copyOf(float[] color) {
        return (color == null) ? null : color.clone();
    }

    /** Returns the red, green and blue components, from 0.0 to 1.0, of a 0xRRGGBB color. */
    static float[] toRGB(int color) {
        return new float[] {((color >> 16) & 0xff)/255f, ((color >> 8) & 0xff)/255f, (color & 0xff)/255f};
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
     * Splits one line of a delimited data file into its fields, as RFC 4180
     * reads them: a field that starts with a quote runs to the closing quote,
     * a doubled quote inside it stands for one quote, and a delimiter inside
     * it is part of the text. A field that does not start with a quote keeps
     * any quotes it holds. The quotes around a field are not part of it.
     * <p>
     * A file that cannot be read this way is refused rather than guessed at:
     * a quoted field that is never closed, or one with text after its closing
     * quote, throws instead of being cut in the wrong place.
     *
     * @param line the line, without its line break.
     * @param delimiter the delimiter, which is text, not a regular expression.
     * @return the fields, including the empty ones at the end of the line.
     */
    static String[] split(String line, String delimiter) {
        if (delimiter.isEmpty()) {
            return new String[] {line};
        }
        List<String> fields = new ArrayList<String>();
        StringBuilder field = new StringBuilder();
        int i = 0;
        while (true) {
            if (i < line.length() && line.charAt(i) == '"') {
                i++;                            // The quote that opens the field
                while (true) {
                    int quote = line.indexOf('"', i);
                    if (quote == -1) {
                        throw new IllegalArgumentException(
                                "A quoted field is not closed on this line of the data file: " + excerpt(line));
                    }
                    field.append(line, i, quote);
                    i = quote + 1;
                    if (i < line.length() && line.charAt(i) == '"') {
                        field.append('"');      // Two quotes stand for one
                        i++;
                    } else {
                        break;                  // The quote that closes the field
                    }
                }
                if (i < line.length() && !line.startsWith(delimiter, i)) {
                    throw new IllegalArgumentException(
                            "A quoted field is followed by text on this line of the data file: " + excerpt(line));
                }
            } else {
                int end = line.indexOf(delimiter, i);
                if (end == -1) {
                    field.append(line, i, line.length());
                    i = line.length();
                } else {
                    field.append(line, i, end);
                    i = end;
                }
            }
            fields.add(field.toString());
            field.setLength(0);
            if (i == line.length()) {
                break;
            }
            i += delimiter.length();            // Step over the delimiter
            if (i == line.length()) {           // The line ends on a delimiter
                fields.add("");
                break;
            }
        }
        return fields.toArray(new String[] {});
    }

    // The start of the line, for the message of a file that cannot be read.
    private static String excerpt(String line) {
        return (line.length() <= 60) ? line : (line.substring(0, 60) + "...");
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
