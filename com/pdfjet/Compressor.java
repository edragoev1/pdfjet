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
        // deflater.setLevel(Deflater.BEST_COMPRESSION);
        // deflater.setLevel(Deflater.BEST_SPEED);
        deflater.setInput(data);
        // End compression with the current contents of the input buffer.
        deflater.finish();
        byte[] buf = new byte[4096];
        while (!deflater.finished()) {
            int count = deflater.deflate(buf);
            bos.write(buf, 0, count);
        }
        deflater.end();
        return bos.toByteArray();
    }
}
