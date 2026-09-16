/*
 * UtilTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertArrayEquals;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;

import java.io.ByteArrayInputStream;
import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.nio.charset.StandardCharsets;
import java.util.Arrays;
import java.util.Random;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

/** Content and Util: reading text and binary files and streams. */
class UtilTest {
    @TempDir
    File tempDir;

    private File write(String name, byte[] bytes) throws IOException {
        File file = new File(tempDir, name);
        OutputStream out = new FileOutputStream(file);
        try {
            out.write(bytes);
        } finally {
            out.close();
        }
        return file;
    }

    @Test
    void ofTextFileReadsUtf8AndDropsAByteOrderMark() throws Exception {
        File file = write("bom.txt", "﻿hello\nwörld".getBytes(StandardCharsets.UTF_8));
        assertEquals("hello\nwörld", Content.ofTextFile(file.getPath()));
    }

    @Test
    void readLinesDropsAByteOrderMarkAndKeepsEmptyLines() throws Exception {
        File file = write("lines.txt", "﻿a\n\nb".getBytes(StandardCharsets.UTF_8));
        assertEquals(Arrays.asList("a", "", "b"), Content.linesOfTextFile(file.getPath()));
    }

    @Test
    void readingAMissingFileThrows() {
        String missing = new File(tempDir, "missing.txt").getPath();
        assertThrows(IOException.class, () -> Content.ofTextFile(missing));
        assertThrows(Exception.class, () -> Content.ofBinaryFile(missing));
    }

    @Test
    void getFromStreamReadsAStreamThatReturnsFewBytesAtATime() throws Exception {
        final byte[] data = new byte[10000];
        new Random(7).nextBytes(data);
        InputStream slow = new ByteArrayInputStream(data) {
            @Override
            public synchronized int read(byte[] b, int off, int len) {
                return super.read(b, off, Math.min(len, 3));
            }
        };
        assertArrayEquals(data, Content.getFromStream(slow));
        assertArrayEquals(data, Content.getFromStream(new ByteArrayInputStream(data), 17));
    }

    @Test
    void ofBinaryFileReadsTheBytes() throws Exception {
        byte[] data = {0, 1, 2, (byte) 0xFF};
        assertArrayEquals(data, Content.ofBinaryFile(write("data.bin", data).getPath()));
    }

    @Test
    void toHexStringWritesLowerCaseDigits() {
        assertEquals("00abff", Util.toHexString(new byte[] {0x00, (byte) 0xAB, (byte) 0xFF}));
    }

    @Test
    void splitCutsTheLineAtTheDelimiterAndKeepsTheEmptyFields() {
        assertArrayEquals(new String[] {"a", "b", "c"}, Util.split("a,b,c", ","));
        assertArrayEquals(new String[] {"", "a", ""}, Util.split(",a,", ","));
        assertArrayEquals(new String[] {""}, Util.split("", ","));
        assertArrayEquals(new String[] {"a", "b"}, Util.split("a||b", "||"));
        assertArrayEquals(new String[] {"a,b"}, Util.split("a,b", ""));
    }

    @Test
    void splitReadsAQuotedFieldAsRfc4180Does() {
        assertArrayEquals(new String[] {"Smith, John", "42"}, Util.split("\"Smith, John\",42", ","));
        assertArrayEquals(new String[] {"a\"b"}, Util.split("\"a\"\"b\"", ","));
        assertArrayEquals(new String[] {"", "x", ""}, Util.split("\"\",x,\"\"", ","));
        assertArrayEquals(new String[] {"one\ttwo", "three"}, Util.split("\"one\ttwo\"\tthree", "\t"));
    }

    @Test
    void splitLeavesTheQuotesOfAFieldThatDoesNotStartWithOne() {
        assertArrayEquals(new String[] {"5\" pipe", "b"}, Util.split("5\" pipe,b", ","));
        assertArrayEquals(new String[] {"a\"b\"c"}, Util.split("a\"b\"c", ","));
    }

    @Test
    void splitRefusesALineItCannotRead() {
        assertThrows(IllegalArgumentException.class, () -> Util.split("a,\"b,c", ","));
        assertThrows(IllegalArgumentException.class, () -> Util.split("\"a\"b,c", ","));
    }
}
