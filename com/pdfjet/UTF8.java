/*
 * UTF8.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.ByteArrayOutputStream;
import java.io.Closeable;
import java.io.IOException;
import java.io.InputStream;
import java.io.PushbackInputStream;

/**
 * Reads UTF-8 text, replacing the bytes that are not UTF-8.
 */
final class UTF8 {
    private UTF8() {
    }

    /**
     * Returns the text of the bytes, with the bytes that are not a well formed
     * UTF-8 sequence replaced by U+FFFD. One replacement stands for each
     * maximal subpart of an ill formed sequence, the substitution the Unicode
     * Standard recommends in section 3.9, so that the four ports of PDFjet
     * read the same text from the same bytes: the three bytes ED A0 80, an
     * encoded surrogate, are three replacements, and a sequence cut short at
     * the end of the text is one, however many of its bytes are there.
     *
     * Java's own decoder reads an encoded surrogate as one malformed sequence
     * of three bytes, and so as a single replacement.
     *
     * @param bytes the bytes.
     * @return the text.
     */
    static String decode(byte[] bytes) {
        return decode(bytes, 0, bytes.length);
    }

    /**
     * Returns the text of the bytes of the range, as decode(byte[]) reads them.
     *
     * @param bytes the bytes.
     * @param offset the first byte of the range.
     * @param length the number of bytes.
     * @return the text.
     */
    static String decode(byte[] bytes, int offset, int length) {
        int end = offset + length;
        StringBuilder text = new StringBuilder(length);
        int i = offset;
        while (i < end) {
            int size = sequenceAt(bytes, i, end);
            if (size < 0) {
                text.append('�');
                i -= size;
            } else {
                text.appendCodePoint(codePoint(bytes, i, size));
                i += size;
            }
        }
        return text.toString();
    }

    // Returns the length of the UTF-8 sequence that starts at the index, as
    // the table of section 3.9 of the Unicode Standard gives them, or minus
    // the length of its maximal subpart when it is not well formed: the bytes
    // that could still have started a sequence, at least one.
    private static int sequenceAt(byte[] bytes, int index, int end) {
        int first = bytes[index] & 0xFF;
        if (first < 0x80) {
            return 1;
        }
        int length;
        int low = 0x80;     // The range of the byte after the first
        int high = 0xBF;
        if (first >= 0xC2 && first <= 0xDF) {
            length = 2;
        } else if (first == 0xE0) {
            length = 3;
            low = 0xA0;     // Not the overlong encodings
        } else if (first >= 0xE1 && first <= 0xEC) {
            length = 3;
        } else if (first == 0xED) {
            length = 3;
            high = 0x9F;    // Not the surrogates
        } else if (first >= 0xEE && first <= 0xEF) {
            length = 3;
        } else if (first == 0xF0) {
            length = 4;
            low = 0x90;     // Not the overlong encodings
        } else if (first >= 0xF1 && first <= 0xF3) {
            length = 4;
        } else if (first == 0xF4) {
            length = 4;
            high = 0x8F;    // Not past U+10FFFF
        } else {
            return -1;      // A byte of a sequence, or C0, C1 or F5 to FF
        }
        for (int next = 1; next < length; next++) {
            int ch = (index + next == end) ? -1 : bytes[index + next] & 0xFF;
            if (ch < low || ch > high) {
                return -next;
            }
            low = 0x80;
            high = 0xBF;
        }
        return length;
    }

    // Returns the code point of the well formed sequence of the given length.
    private static int codePoint(byte[] bytes, int index, int length) {
        int value = bytes[index] & (0xFF >> (length + 1));
        if (length == 1) {
            return bytes[index] & 0xFF;
        }
        for (int next = 1; next < length; next++) {
            value = (value << 6) | (bytes[index + next] & 0x3F);
        }
        return value;
    }

    /**
     * The lines of a stream of UTF-8 text. A line ends with \n, \r or \r\n, as
     * BufferedReader reads them, and its bytes are read as decode reads them.
     */
    static final class LineReader implements Closeable {
        private final PushbackInputStream stream;

        LineReader(InputStream stream) {
            this.stream = new PushbackInputStream(stream, 3);
        }

        // Skips the three bytes of a byte order mark at the start of the
        // stream, which are not part of the text.
        void skipByteOrderMark() throws IOException {
            byte[] mark = new byte[3];
            int read = 0;
            while (read < mark.length) {
                int count = stream.read(mark, read, mark.length - read);
                if (count == -1) {
                    break;
                }
                read += count;
            }
            if (read == mark.length && (mark[0] & 0xFF) == 0xEF
                    && (mark[1] & 0xFF) == 0xBB && (mark[2] & 0xFF) == 0xBF) {
                return;
            }
            stream.unread(mark, 0, read);
        }

        // Returns the next line, without its end of line marker, or null at
        // the end of the stream.
        String readLine() throws IOException {
            int ch = stream.read();
            if (ch == -1) {
                return null;
            }
            ByteArrayOutputStream line = new ByteArrayOutputStream(256);
            while (ch != -1 && ch != '\n' && ch != '\r') {
                line.write(ch);
                ch = stream.read();
            }
            if (ch == '\r') {
                int next = stream.read();
                if (next != -1 && next != '\n') {
                    stream.unread(next);
                }
            }
            byte[] bytes = line.toByteArray();
            return decode(bytes, 0, bytes.length);
        }

        @Override
        public void close() throws IOException {
            stream.close();
        }
    }
}
