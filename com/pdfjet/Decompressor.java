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
