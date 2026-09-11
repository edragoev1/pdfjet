/*
 * Decompressor.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.*;
import java.util.Arrays;
import java.util.zip.*;

class Decompressor {
    /**
     * Decodes the data of an LZWDecode stream, with the default EarlyChange
     * of 1: the codes get one bit longer one code before the table needs it.
     * Data that ends without the end code, or with an invalid code, returns
     * what was decoded up to there, as a missing end is common in real files.
     */
    static byte[] lzwDecode(byte[] data) {
        ByteArrayOutputStream bos = new ByteArrayOutputStream(data.length * 2);
        byte[][] table = new byte[4096][];
        for (int i = 0; i < 256; i++) {
            table[i] = new byte[] {(byte) i};
        }
        int next = 258;         // 256 clears the table and 257 ends the data.
        int codeLength = 9;
        int bits = 0;           // Only the low bitCount bits are still unread.
        int bitCount = 0;
        byte[] previous = null;
        for (byte b : data) {
            bits = (bits << 8) | (b & 0xff);
            bitCount += 8;
            while (bitCount >= codeLength) {
                bitCount -= codeLength;
                int code = (bits >> bitCount) & ((1 << codeLength) - 1);
                if (code == 256) {
                    next = 258;
                    codeLength = 9;
                    previous = null;
                    continue;
                }
                byte[] entry;
                if (code < next && code != 257 && table[code] != null) {
                    entry = table[code];
                } else if (code == next && previous != null) {
                    entry = Arrays.copyOf(previous, previous.length + 1);
                    entry[previous.length] = previous[0];
                } else {
                    return bos.toByteArray();   // The end code or an invalid one.
                }
                bos.write(entry, 0, entry.length);
                if (previous != null && next < 4096) {
                    byte[] added = Arrays.copyOf(previous, previous.length + 1);
                    added[previous.length] = entry[0];
                    table[next++] = added;
                }
                previous = entry;
                if (next + 1 >= (1 << codeLength) && codeLength < 12) {
                    codeLength++;
                }
            }
        }
        return bos.toByteArray();
    }

    /**
     * Undoes the predictor of the /DecodeParms of a stream: 2 is the TIFF
     * predictor, and 10 to 15 are the PNG predictors, where each row starts
     * with the type of the PNG filter of that row.
     */
    static byte[] applyPredictor(
            byte[] data,
            int predictor,
            int colors,
            int bitsPerComponent,
            int columns) {
        // Larger values are not in real files, and would overflow the row length.
        if (colors < 1 || colors > 256 ||
                bitsPerComponent < 1 || bitsPerComponent > 16 ||
                columns < 1 || columns > (1 << 18)) {
            return data;
        }
        if (predictor == 2) {
            return applyTIFFPredictor(data, colors, bitsPerComponent, columns);
        } else if (predictor >= 10) {
            return applyPNGPredictor(
                    data,
                    (colors * bitsPerComponent + 7) / 8,
                    (colors * bitsPerComponent * columns + 7) / 8);
        }
        return data;
    }

    // Each sample adds the sample of the same color to its left.
    private static byte[] applyTIFFPredictor(
            byte[] data, int colors, int bitsPerComponent, int columns) {
        byte[] decoded = data.clone();
        int rowLength = (colors * bitsPerComponent * columns + 7) / 8;
        for (int row = 0; row < decoded.length; row += rowLength) {
            if (bitsPerComponent == 8) {
                int end = Math.min(row + rowLength, decoded.length);
                for (int i = row + colors; i < end; i++) {
                    decoded[i] += decoded[i - colors];
                }
            } else {
                int samples = Math.min(
                        colors * columns, (decoded.length - row) * 8 / bitsPerComponent);
                for (int i = colors; i < samples; i++) {
                    int left = getSample(decoded, row, (i - colors) * bitsPerComponent, bitsPerComponent);
                    int sample = getSample(decoded, row, i * bitsPerComponent, bitsPerComponent);
                    setSample(decoded, row, i * bitsPerComponent, bitsPerComponent, left + sample);
                }
            }
        }
        return decoded;
    }

    private static int getSample(byte[] data, int row, int bit, int bitsPerComponent) {
        int sample = 0;
        for (int i = bit; i < bit + bitsPerComponent; i++) {
            sample = (sample << 1) | ((data[row + (i >> 3)] >> (7 - (i & 7))) & 1);
        }
        return sample;
    }

    // Sets the low bitsPerComponent bits of the sample.
    private static void setSample(
            byte[] data, int row, int bit, int bitsPerComponent, int sample) {
        for (int i = bit + bitsPerComponent - 1; i >= bit; i--) {
            int k = row + (i >> 3);
            int mask = 1 << (7 - (i & 7));
            data[k] = (byte) (((sample & 1) != 0) ? (data[k] | mask) : (data[k] & ~mask));
            sample >>= 1;
        }
    }

    // Only the last row can be shorter than rowLength.
    private static byte[] applyPNGPredictor(byte[] data, int bytesPerPixel, int rowLength) {
        int rows = (data.length + rowLength) / (rowLength + 1);
        byte[] decoded = new byte[data.length - rows];
        int j = 0;              // The index in decoded
        for (int i = 0; i < data.length; i += rowLength + 1) {
            int filter = data[i];
            int n = Math.min(rowLength, data.length - i - 1);
            for (int x = 0; x < n; x++, j++) {
                int left = (x >= bytesPerPixel) ? (decoded[j - bytesPerPixel] & 0xff) : 0;
                int up = (j >= rowLength) ? (decoded[j - rowLength] & 0xff) : 0;
                int upLeft = (x >= bytesPerPixel && j >= rowLength) ?
                        (decoded[j - rowLength - bytesPerPixel] & 0xff) : 0;
                int value = data[i + 1 + x] & 0xff;
                if (filter == 1) {          // Sub
                    value += left;
                } else if (filter == 2) {   // Up
                    value += up;
                } else if (filter == 3) {   // Average
                    value += (left + up) / 2;
                } else if (filter == 4) {   // Paeth
                    value += paeth(left, up, upLeft);
                }                           // 0 is None, and so are unknown types.
                decoded[j] = (byte) value;
            }
        }
        return decoded;
    }

    private static int paeth(int left, int up, int upLeft) {
        int p = left + up - upLeft;
        int pLeft = Math.abs(p - left);
        int pUp = Math.abs(p - up);
        int pUpLeft = Math.abs(p - upLeft);
        if (pLeft <= pUp && pLeft <= pUpLeft) {
            return left;
        }
        return (pUp <= pUpLeft) ? up : upLeft;
    }

    /**
     * Decodes the data of an ASCIIHexDecode stream. White space is skipped,
     * > ends the data, and a last digit without a pair is followed by 0.
     */
    static byte[] asciiHexDecode(byte[] data) {
        ByteArrayOutputStream bos = new ByteArrayOutputStream(data.length / 2);
        int high = -1;
        for (byte b : data) {
            if (b == '>') {
                break;
            }
            int digit = hexValue(b);
            if (digit == -1) {
                continue;       // White space, or a character that is not valid.
            }
            if (high == -1) {
                high = digit;
            } else {
                bos.write((high << 4) | digit);
                high = -1;
            }
        }
        if (high != -1) {
            bos.write(high << 4);
        }
        return bos.toByteArray();
    }

    /**
     * Returns the value of a hexadecimal digit, or -1 when it is not one.
     */
    static int hexValue(int c) {
        if (c >= '0' && c <= '9') {
            return c - '0';
        } else if (c >= 'a' && c <= 'f') {
            return c - 'a' + 10;
        } else if (c >= 'A' && c <= 'F') {
            return c - 'A' + 10;
        }
        return -1;
    }

    /**
     * Decodes the data of an ASCII85Decode stream. Each group of five
     * characters from ! to u is four bytes, z is four zero bytes, and ~>
     * ends the data. White space is skipped, and a last group of n
     * characters is n - 1 bytes.
     */
    static byte[] ascii85Decode(byte[] data) {
        ByteArrayOutputStream bos = new ByteArrayOutputStream(data.length * 4 / 5 + 4);
        long value = 0;
        int count = 0;
        int i = 0;
        if (data.length >= 2 && data[0] == '<' && data[1] == '~') {
            i = 2;              // The start of the data in PostScript.
        }
        for (; i < data.length; i++) {
            int c = data[i] & 0xff;
            if (c == '~') {
                break;
            } else if (c == 'z' && count == 0) {
                bos.write(0);
                bos.write(0);
                bos.write(0);
                bos.write(0);
            } else if (c >= '!' && c <= 'u') {
                value = value * 85 + (c - '!');
                if (++count == 5) {
                    for (int j = 24; j >= 0; j -= 8) {
                        bos.write((int) (value >> j));
                    }
                    value = 0;
                    count = 0;
                }
            }                   // White space, or a character that is not valid.
        }
        if (count > 1) {
            for (int j = count; j < 5; j++) {
                value = value * 85 + 84;
            }
            for (int j = 0; j < count - 1; j++) {
                bos.write((int) (value >> (24 - 8 * j)));
            }
        }
        return bos.toByteArray();
    }

    /**
     * Decodes the data of a RunLengthDecode stream. A length byte from 0 to
     * 127 is followed by that many plus one bytes to copy, one from 129 to
     * 255 by a byte to repeat 257 minus that many times, and 128 ends the data.
     */
    static byte[] runLengthDecode(byte[] data) {
        ByteArrayOutputStream bos = new ByteArrayOutputStream(data.length * 2);
        int i = 0;
        while (i < data.length) {
            int length = data[i++] & 0xff;
            if (length < 128) {
                int n = Math.min(length + 1, data.length - i);
                bos.write(data, i, n);
                i += n;
            } else if (length > 128 && i < data.length) {
                byte b = data[i++];
                for (int j = 0; j < 257 - length; j++) {
                    bos.write(b);
                }
            } else {
                break;
            }
        }
        return bos.toByteArray();
    }

    static byte[] inflate(byte[] data) throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream(data.length);
        Inflater inflater = new Inflater();
        try {
            inflater.setInput(data);
            byte[] buf = new byte[4096];
            while (!inflater.finished()) {
                int count = inflater.inflate(buf);
                if (count == 0 && inflater.needsInput()) {
                    throw new DataFormatException("Truncated or invalid Flate stream");
                }
                bos.write(buf, 0, count);
            }
        } finally {
            inflater.end();
        }
        return bos.toByteArray();
    }
}
