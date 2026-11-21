/*
 * Compressor.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.*;
import java.util.zip.*;

class Compressor {
    // The compression level of everything PDFjet deflates: the content of the
    // pages, the fonts, the images and the embedded files. Level 5 of zlib
    // makes the documents of the fair test 4 to 25% faster than the default 6
    // (a 12-page statement 7.7 ms to 5.8) for PDFs 0.1 to 0.4% larger
    // (26 October 2026, the owner: "level 5 for Java"). Go keeps its own
    // default 6, as its encoder gained little and grew 3% at 5.
    static final int LEVEL = 5;

    // A Deflater for each thread, reset after each use: one Deflater for every
    // thread, synchronized, made the threads of a server that writes PDFs at
    // once wait their turn to compress (third review, W7; 25 October 2026).
    private static final ThreadLocal<Deflater> DEFLATER = new ThreadLocal<Deflater>() {
        @Override
        protected Deflater initialValue() {
            return new Deflater(LEVEL);
        }
    };

    static byte[] deflate(byte[] data) {
        Deflater deflater = DEFLATER.get();
        ByteArrayOutputStream bos = new ByteArrayOutputStream(data.length);
        try {
            deflater.setInput(data);
            deflater.finish();
            byte[] buf = new byte[4096];
            while (!deflater.finished()) {
                bos.write(buf, 0, deflater.deflate(buf));
            }
        } finally {
            deflater.reset();
        }
        return bos.toByteArray();
    }

    // Deflates the samples of an image as they are written, at LEVEL, to the
    // bytes deflate makes of them written whole, so that an image is
    // compressed a row at a time, its samples never in memory whole (21
    // November 2026). It tells whether every byte written was one value, as
    // a soft mask all of 255 is left out.
    static final class ImageWriter {
        private final Deflater deflater = new Deflater(LEVEL);
        private final ByteArrayOutputStream bos = new ByteArrayOutputStream();
        private final byte[] buf = new byte[4096];
        private long written = 0;
        private int same = -1;  // The one value of every byte, -1 for none yet, -2 for more
        private boolean ended = false;

        // Deflates the bytes, which it does not keep.
        void write(byte[] data, int off, int len) {
            written += len;
            if (same != -2) {
                for (int i = off; i < off + len; i++) {
                    int b = data[i] & 0xFF;
                    if (same == -1) {
                        same = b;
                    } else if (b != same) {
                        same = -2;
                        break;
                    }
                }
            }
            deflater.setInput(data, off, len);
            while (!deflater.needsInput()) {
                bos.write(buf, 0, deflater.deflate(buf));
            }
        }

        void write(byte[] data) {
            write(data, 0, data.length);
        }

        boolean isEmpty() {
            return written == 0;
        }

        // Whether bytes were written and every one of them was the value.
        boolean allAre(int value) {
            return written > 0 && same == value;
        }

        // Ends the stream and returns it.
        byte[] toByteArray() {
            try {
                deflater.finish();
                while (!deflater.finished()) {
                    bos.write(buf, 0, deflater.deflate(buf));
                }
                return bos.toByteArray();
            } finally {
                end();
            }
        }

        // Lets go of the deflater, with or without its stream.
        void end() {
            if (!ended) {
                ended = true;
                deflater.end();
            }
        }
    }
}
