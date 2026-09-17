/*
 * Util.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.BufferedReader;
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

    /**
     * Returns the color as a 0xRRGGBB value, each component rounded to the nearest
     * of 256 steps and kept between 0.0 and 1.0, or -1 if the color is null.
     */
    static int toPackedRGB(float[] color) {
        if (color == null) {
            return -1;
        }
        return (toByte(color[0]) << 16) | (toByte(color[1]) << 8) | toByte(color[2]);
    }

    private static int toByte(float component) {
        float value = Math.max(0f, Math.min(1f, component));
        return Math.round(value * 255f);
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
        return split(line, delimiter, false);
    }

    // The most lines a record may take, so that a quote that is never closed
    // fails instead of reading the rest of the file into one field.
    static final int MAX_LINES_IN_RECORD = 10000;

    /**
     * Returns the fields of the record of a delimited data file that starts
     * with the line. When a quoted field holds line breaks, the record goes on
     * over the next lines of the reader, and each line break in the field is a
     * space, as a table cell is drawn on one line. Only a record of several
     * lines is looked at for them, so a line costs nothing more to read.
     *
     * @param line the first line of the record.
     * @param reader the reader of the lines after it.
     * @param delimiter the delimiter.
     * @return the fields of the record.
     * @throws IOException if the reader fails.
     */
    static String[] readRecord(String line, BufferedReader reader, String delimiter) throws IOException {
        String[] fields = split(line, delimiter, true);
        if (fields != null) {
            return fields;
        }
        StringBuilder record = new StringBuilder(line);
        int lines = 1;
        while (true) {
            String next = reader.readLine();
            if (next == null) {
                throw new IllegalArgumentException(
                        "A quoted field is not closed by the end of the data file: " + excerpt(record.toString()));
            }
            if (++lines > MAX_LINES_IN_RECORD) {
                throw new IllegalArgumentException("A quoted field is not closed within "
                        + MAX_LINES_IN_RECORD + " lines of the data file: " + excerpt(record.toString()));
            }
            record.append('\n').append(next);
            // The quoted field goes on until a quote that is not doubled; only
            // then can the record end, so only then is it split again.
            if (closesQuotedField(next)) {
                fields = split(record.toString(), delimiter, true);
                if (fields != null) {
                    for (int i = 0; i < fields.length; i++) {
                        fields[i] = lineBreaksToSpaces(fields[i]);
                    }
                    return fields;
                }
            }
        }
    }

    // Returns true when the line, read inside a quoted field, holds the quote
    // that closes it: a quote that is not one of a doubled pair.
    private static boolean closesQuotedField(String line) {
        int i = line.indexOf('"');
        while (i != -1) {
            if (i + 1 < line.length() && line.charAt(i + 1) == '"') {
                i = line.indexOf('"', i + 2);
            } else {
                return true;
            }
        }
        return false;
    }

    /**
     * Returns the text with each line break, "\r\n", "\r" or "\n", replaced by
     * a space, for a table cell that is drawn on one line.
     *
     * @param text the text.
     * @return the text on one line.
     */
    static String lineBreaksToSpaces(String text) {
        if (text.indexOf('\n') == -1 && text.indexOf('\r') == -1) {
            return text;
        }
        return text.replace("\r\n", " ").replace('\r', ' ').replace('\n', ' ');
    }

    // With open, a line that ends inside a quoted field returns null, for
    // readRecord to read on; without it the line is refused.
    private static String[] split(String line, String delimiter, boolean open) {
        if (delimiter.isEmpty()) {
            return new String[] {line};
        }
        // Every field is a substring of the line, as String.split makes them;
        // the one further copy is of a quoted field that holds a doubled quote.
        // BigTable reads every line of its file through here.
        List<String> fields = new ArrayList<String>();
        int i = 0;
        while (true) {
            if (i < line.length() && line.charAt(i) == '"') {
                i++;                            // The quote that opens the field
                int start = i;
                while (true) {
                    int quote = line.indexOf('"', i);
                    if (quote == -1) {
                        if (open) {
                            return null;
                        }
                        throw new IllegalArgumentException(
                                "A quoted field is not closed on this line of the data file: " + excerpt(line));
                    }
                    i = quote + 1;
                    if (i < line.length() && line.charAt(i) == '"') {
                        i++;                    // Two quotes stand for one; the closing quote is the first single one
                    } else {
                        break;                  // The quote that closes the field
                    }
                }
                // The text between the quotes, where every quote is doubled.
                // JDK 8's String.replace compiles a Pattern, so it runs only
                // for a field that holds one.
                String field = line.substring(start, i - 1);
                fields.add(field.indexOf("\"\"") == -1 ? field : field.replace("\"\"", "\""));
                if (i < line.length() && !line.startsWith(delimiter, i)) {
                    throw new IllegalArgumentException(
                            "A quoted field is followed by text on this line of the data file: " + excerpt(line));
                }
            } else {
                int end = line.indexOf(delimiter, i);
                if (end == -1) {
                    end = line.length();
                }
                fields.add(line.substring(i, end));
                i = end;
            }
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
        // The scan is what split("\\s+") does, without compiling a Pattern for
        // every call; the other three ports have never used a regex here.
        List<String> tokens = new ArrayList<String>();
        int i = 0;
        while (i < text.length()) {
            while (i < text.length() && isASCIIWhitespace(text.charAt(i))) {
                i++;
            }
            int start = i;
            while (i < text.length() && !isASCIIWhitespace(text.charAt(i))) {
                i++;
            }
            if (i > start) {
                tokens.add(text.substring(start, i));
            }
        }
        return tokens.toArray(new String[] {});
    }

    // The six characters that Java's \s matches: space, tab, line feed,
    // vertical tab, form feed and carriage return.
    private static boolean isASCIIWhitespace(char ch) {
        return ch == ' ' || (ch >= '\t' && ch <= '\r');
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
