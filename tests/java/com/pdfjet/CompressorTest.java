/*
 * CompressorTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertArrayEquals;

import java.io.ByteArrayOutputStream;
import java.util.ArrayList;
import java.util.List;
import java.util.Random;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Future;
import java.util.zip.Inflater;
import org.junit.jupiter.api.Test;

class CompressorTest {
    // Each thread compresses with a Deflater of its own: data compressed in
    // eight threads at once inflates back to itself, each piece of it.
    @Test
    void dataCompressedInManyThreadsAtOnceInflatesToItself() throws Exception {
        ExecutorService threads = Executors.newFixedThreadPool(8);
        try {
            List<Future<Void>> done = new ArrayList<Future<Void>>();
            for (int t = 0; t < 8; t++) {
                final long seed = t;
                done.add(threads.submit(() -> {
                    Random random = new Random(seed);
                    for (int i = 0; i < 200; i++) {
                        byte[] data = new byte[random.nextInt(20000)];
                        for (int j = 0; j < data.length; j++) {
                            data[j] = (byte) ('a' + random.nextInt(4));  // Compressible
                        }
                        assertArrayEquals(data, inflate(Compressor.deflate(data)));
                    }
                    return null;
                }));
            }
            for (Future<Void> future : done) {
                future.get();
            }
        } finally {
            threads.shutdown();
        }
    }

    private static byte[] inflate(byte[] compressed) throws Exception {
        Inflater inflater = new Inflater();
        inflater.setInput(compressed);
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        byte[] buf = new byte[4096];
        while (!inflater.finished()) {
            int n = inflater.inflate(buf);
            if (n == 0 && (inflater.needsInput() || inflater.needsDictionary())) {
                break;
            }
            bos.write(buf, 0, n);
        }
        inflater.end();
        return bos.toByteArray();
    }
}
