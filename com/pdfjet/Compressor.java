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
    // A Deflater for each thread, reset after each use: one Deflater for every
    // thread, synchronized, made the threads of a server that writes PDFs at
    // once wait their turn to compress (third review, W7; 25 October 2026).
    private static final ThreadLocal<Deflater> DEFLATER = new ThreadLocal<Deflater>() {
        @Override
        protected Deflater initialValue() {
            return new Deflater();
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
