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
    private static final Deflater DEFLATER = new Deflater();

    static synchronized byte[] deflate(byte[] data) {
        ByteArrayOutputStream bos = new ByteArrayOutputStream(data.length);
        try {
            DEFLATER.setInput(data);
            DEFLATER.finish();
            byte[] buf = new byte[4096];
            while (!DEFLATER.finished()) {
                bos.write(buf, 0, DEFLATER.deflate(buf));
            }
        } finally {
            DEFLATER.reset();
        }
        return bos.toByteArray();
    }
}

