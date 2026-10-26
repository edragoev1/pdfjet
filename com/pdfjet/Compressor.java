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
}
