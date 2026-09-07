/*
 * Decompressor.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.*;
import java.util.zip.*;

class Decompressor {
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
