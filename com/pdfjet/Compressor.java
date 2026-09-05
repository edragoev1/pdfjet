/**
 * Compressor.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.*;
import java.util.zip.*;

class Compressor {
    static byte[] deflate(byte[] data) {
        ByteArrayOutputStream bos = new ByteArrayOutputStream(data.length);
        Deflater deflater = new Deflater();
        try {
            deflater.setInput(data);
            deflater.finish();
            byte[] buf = new byte[4096];
            while (!deflater.finished()) {
                int count = deflater.deflate(buf);
                bos.write(buf, 0, count);
            }
        } finally {
            deflater.end();   // releases native memory even on error
        }
        return bos.toByteArray();
    }
}
