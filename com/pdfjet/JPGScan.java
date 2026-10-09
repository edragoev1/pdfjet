/*
 * JPGScan.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.ArrayList;
import java.util.List;

/**
 * Walks the scans of a sequential JPEG that has no end-of-image marker.
 */
final class JPGScan {
    private JPGScan() {
    }

    private static final class Component {
        int id;
        int h;
        int v;
        boolean seen;
    }

    // Returns whether the scans of a sequential JPEG that has no end-of-image
    // marker hold every block of its image: a JPEG whose only fault is the
    // missing marker, which viewers draw, rather than one cut short in its
    // upload or its copy. It reads the header from the start for the Huffman
    // tables, the restart interval and the components, and walks the
    // entropy-coded data of each scan, decoding the Huffman codes of each block
    // and passing over the bits of their values, without decoding the image.
    // Every step reads at least one bit, so the walk is no longer than the data.
    // A table, a scan or a header it cannot read makes the JPEG not whole.
    static boolean scansWhole(byte[] buffer) {
        Huffman[] dc = new Huffman[4];
        Huffman[] ac = new Huffman[4];
        List<Component> components = null;
        int width = 0;
        int height = 0;
        int maxH = 1;
        int maxV = 1;
        int restartInterval = 0;
        int[] index = {2};  // After the start of the image
        int scans = 0;
        while (true) {
            int ch = marker(buffer, index);
            if (ch < 0) {
                // The data ends after a whole scan, where the end-of-image
                // marker would be: every component must have been in one.
                if (scans == 0) {
                    return false;
                }
                for (Component c : components) {
                    if (!c.seen) {
                        return false;
                    }
                }
                return true;
            }
            if (ch == 0x01 || ch == 0xD8 || (ch >= 0xD0 && ch <= 0xD7)) {
                continue;
            }
            if (ch == 0xD9) {
                return scans > 0;
            }
            if (ch == 0xC0 || ch == 0xC1) {
                byte[] data = segment(buffer, index);
                if (data == null || data.length < 6 || components != null) {
                    return false;
                }
                height = (data[1] & 0xFF) << 8 | (data[2] & 0xFF);
                width = (data[3] & 0xFF) << 8 | (data[4] & 0xFF);
                int n = data[5] & 0xFF;
                if (width == 0 || height == 0 || data.length < 6 + 3*n || n == 0) {
                    return false;
                }
                components = new ArrayList<>();
                for (int i = 0; i < n; i++) {
                    Component c = new Component();
                    c.id = data[6 + 3*i] & 0xFF;
                    c.h = (data[7 + 3*i] & 0xFF) >> 4;
                    c.v = data[7 + 3*i] & 15;
                    if (c.h < 1 || c.h > 4 || c.v < 1 || c.v > 4) {
                        return false;
                    }
                    maxH = Math.max(maxH, c.h);
                    maxV = Math.max(maxV, c.v);
                    components.add(c);
                }
            } else if (ch == 0xC4) {    // Define Huffman tables
                byte[] data = segment(buffer, index);
                if (data == null) {
                    return false;
                }
                int offset = 0;
                while (offset < data.length) {
                    if (data.length - offset < 17) {
                        return false;
                    }
                    int tableClass = (data[offset] & 0xFF) >> 4;
                    int id = data[offset] & 15;
                    int count = 0;
                    for (int i = 1; i <= 16; i++) {
                        count += data[offset + i] & 0xFF;
                    }
                    if (tableClass > 1 || id > 3 || count > 256 || data.length - offset < 17 + count) {
                        return false;
                    }
                    Huffman table = Huffman.of(data, offset + 1, offset + 17, count);
                    if (table == null) {
                        return false;
                    }
                    if (tableClass == 0) {
                        dc[id] = table;
                    } else {
                        ac[id] = table;
                    }
                    offset += 17 + count;
                }
            } else if (ch == 0xDD) {    // Define restart interval
                byte[] data = segment(buffer, index);
                if (data == null || data.length < 2) {
                    return false;
                }
                restartInterval = (data[0] & 0xFF) << 8 | (data[1] & 0xFF);
            } else if (ch == 0xDA) {
                byte[] data = segment(buffer, index);
                if (data == null || components == null || data.length < 1) {
                    return false;
                }
                int n = data[0] & 0xFF;
                if (n < 1 || n > 4 || data.length < 1 + 2*n + 3) {
                    return false;
                }
                Component[] parts = new Component[n];
                Huffman[] partDC = new Huffman[n];
                Huffman[] partAC = new Huffman[n];
                for (int i = 0; i < n; i++) {
                    int id = data[1 + 2*i] & 0xFF;
                    int tables = data[2 + 2*i] & 0xFF;
                    Component c = null;
                    for (Component each : components) {
                        if (each.id == id) {
                            c = each;
                        }
                    }
                    if (c == null || tables >> 4 > 3 || (tables & 15) > 3 ||
                            dc[tables >> 4] == null || ac[tables & 15] == null) {
                        return false;
                    }
                    parts[i] = c;
                    partDC[i] = dc[tables >> 4];
                    partAC[i] = ac[tables & 15];
                }
                // The blocks of each unit of the scan, and the units across
                // and down: an interleaved scan's unit is an MCU, the blocks
                // of each component by its sampling; a scan of one component
                // has a unit of one block.
                long units;
                if (n == 1) {
                    Component c = parts[0];
                    long w = ceilDiv(ceilDiv(width*c.h, maxH), 8);
                    long h = ceilDiv(ceilDiv(height*c.v, maxV), 8);
                    units = w*h;
                } else {
                    units = (long) ceilDiv(width, 8*maxH)*ceilDiv(height, 8*maxV);
                }
                BitReader reader = new BitReader(buffer, index[0]);
                for (long u = 0; u < units; u++) {
                    if (restartInterval > 0 && u > 0 && u % restartInterval == 0 && !reader.restart()) {
                        return false;
                    }
                    for (int i = 0; i < n; i++) {
                        int blocks = n > 1 ? parts[i].h*parts[i].v : 1;
                        for (int b = 0; b < blocks; b++) {
                            if (!reader.block(partDC[i], partAC[i])) {
                                return false;
                            }
                        }
                    }
                }
                for (Component p : parts) {
                    p.seen = true;
                }
                scans++;
                index[0] = reader.index;
            } else {
                // SOF2 and the other frames are not walked: the caller asks only
                // of a sequential JPEG.
                if (ch >= 0xC0 && ch <= 0xCF && ch != 0xC4 && ch != 0xC8 && ch != 0xCC) {
                    return false;
                }
                if (segment(buffer, index) == null) {
                    return false;
                }
            }
        }
    }

    private static int ceilDiv(int a, int b) {
        return (a + b - 1)/b;
    }

    // The next marker after index[0], or -1 at the end of the data.
    private static int marker(byte[] buffer, int[] index) {
        while (index[0] < buffer.length) {
            if ((buffer[index[0]] & 0xFF) != 0xFF) {
                index[0]++;
                continue;
            }
            while (index[0] < buffer.length && (buffer[index[0]] & 0xFF) == 0xFF) {
                index[0]++;
            }
            if (index[0] >= buffer.length) {
                return -1;
            }
            int ch = buffer[index[0]] & 0xFF;
            index[0]++;
            if (ch != 0x00) {
                return ch;
            }
        }
        return -1;
    }

    // The parameter segment at index[0], without its length, or null.
    private static byte[] segment(byte[] buffer, int[] index) {
        if (index[0] + 2 > buffer.length) {
            return null;
        }
        int length = (buffer[index[0]] & 0xFF) << 8 | (buffer[index[0] + 1] & 0xFF);
        if (length < 2 || index[0] + length > buffer.length) {
            return null;
        }
        byte[] data = new byte[length - 2];
        System.arraycopy(buffer, index[0] + 2, data, 0, data.length);
        index[0] += length;
        return data;
    }

    // A Huffman table of a JPEG, as the JPEG standard decodes it (Annex
    // F.2.2.3): for each length, the smallest and the largest code, and where
    // its symbols begin.
    private static final class Huffman {
        final int[] minCode = new int[17];
        final int[] maxCode = new int[17];
        final int[] valPtr = new int[17];
        byte[] symbols;

        static Huffman of(byte[] data, int counts, int symbols, int count) {
            Huffman h = new Huffman();
            h.symbols = new byte[count];
            System.arraycopy(data, symbols, h.symbols, 0, count);
            int code = 0;
            int k = 0;
            for (int length = 1; length <= 16; length++) {
                int n = data[counts + length - 1] & 0xFF;
                h.valPtr[length] = k;
                h.minCode[length] = code;
                code += n;
                k += n;
                h.maxCode[length] = n == 0 ? -1 : code - 1;
                if (code > 1 << length) {
                    return null;    // More codes than the length has
                }
                code <<= 1;
            }
            return h;
        }
    }

    // Reads the entropy-coded data of a scan a bit at a time: a 0xFF is
    // followed by a 0x00 that is not data, and any other marker ends the data,
    // which a block that needs more bits does not survive.
    private static final class BitReader {
        final byte[] buffer;
        int index;
        int bits;
        int count;

        BitReader(byte[] buffer, int index) {
            this.buffer = buffer;
            this.index = index;
        }

        // The next bit, or -1 at a marker or the end of the data.
        int bit() {
            if (count == 0) {
                if (index >= buffer.length) {
                    return -1;
                }
                int b = buffer[index] & 0xFF;
                if (b == 0xFF) {
                    if (index + 1 >= buffer.length || buffer[index + 1] != 0x00) {
                        return -1;  // A marker, or the end of the data
                    }
                    index++;
                }
                index++;
                bits = b;
                count = 8;
            }
            count--;
            return (bits >> count) & 1;
        }

        boolean skip(int n) {
            for (; n > 0; n--) {
                if (bit() < 0) {
                    return false;
                }
            }
            return true;
        }

        // The symbol of the next code, or -1.
        int decode(Huffman h) {
            int code = 0;
            for (int length = 1; length <= 16; length++) {
                int b = bit();
                if (b < 0) {
                    return -1;
                }
                code = code << 1 | b;
                if (code <= h.maxCode[length]) {
                    int k = h.valPtr[length] + code - h.minCode[length];
                    if (k < 0 || k >= h.symbols.length) {
                        return -1;
                    }
                    return h.symbols[k] & 0xFF;
                }
            }
            return -1;
        }

        // Passes over the codes of one block of 64 coefficients.
        boolean block(Huffman dc, Huffman ac) {
            int s = decode(dc);
            if (s < 0 || s > 11 || !skip(s)) {
                return false;
            }
            for (int k = 1; k < 64;) {
                int rs = decode(ac);
                if (rs < 0) {
                    return false;
                }
                int run = rs >> 4;
                int size = rs & 15;
                if (size == 0) {
                    if (run != 15) {
                        return true;    // The end of the block
                    }
                    k += 16;
                    continue;
                }
                k += run;
                if (k > 63 || !skip(size)) {
                    return false;
                }
                k++;
            }
            return true;
        }

        // Goes past the restart marker that ends an interval, the bits left in
        // the byte before it being fill.
        boolean restart() {
            count = 0;
            while (index + 1 < buffer.length && (buffer[index] & 0xFF) == 0xFF &&
                    (buffer[index + 1] & 0xFF) == 0xFF) {
                index++;    // A fill byte before the marker
            }
            if (index + 1 >= buffer.length || (buffer[index] & 0xFF) != 0xFF ||
                    (buffer[index + 1] & 0xFF) < 0xD0 || (buffer[index + 1] & 0xFF) > 0xD7) {
                return false;
            }
            index += 2;
            return true;
        }
    }
}   // End of JPGScan.java
