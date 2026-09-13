/*
 * TestSupport.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;

import java.io.BufferedInputStream;
import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.nio.charset.StandardCharsets;
import java.util.List;

/**
 * Helpers shared by the unit tests. The tests run from the repository root,
 * or from the directory in the pdfjet.root system property, and read the
 * PngSuite images and the fonts from there.
 */
public final class TestSupport {
    /** The tolerance for coordinates and widths, a hundredth of a point. */
    public static final float DELTA = 0.01f;

    private static final char[] HEX = "0123456789ABCDEF".toCharArray();

    private TestSupport() {
    }

    public static File file(String path) {
        return new File(System.getProperty("pdfjet.root", "."), path);
    }

    public static InputStream open(String path) throws IOException {
        return new BufferedInputStream(new FileInputStream(file(path)));
    }

    public static PDF newPDF() throws Exception {
        return new PDF(new ByteArrayOutputStream());
    }

    public static Font helvetica(PDF pdf) throws Exception {
        return new Font(pdf, CoreFont.HELVETICA);
    }

    public static String latin1(byte[] bytes) {
        return new String(bytes, StandardCharsets.ISO_8859_1);
    }

    /** Returns the content stream of the page, which is not compressed yet. */
    public static String content(Page page) {
        return latin1(page.getContent());
    }

    /** Returns the text as a core font draws it: a byte per character in upper case hexadecimal. */
    public static String hex(String text) {
        StringBuilder sb = new StringBuilder();
        for (byte b : text.getBytes(StandardCharsets.ISO_8859_1)) {
            sb.append(HEX[(b >> 4) & 0x0F]).append(HEX[b & 0x0F]);
        }
        return sb.toString();
    }

    /** Decodes a PDF text string written as a hexadecimal string with a UTF-16BE byte order mark. */
    public static String utf16Hex(String value) {
        String digits = value.replaceAll("[<>\\s]", "");
        byte[] bytes = new byte[digits.length() / 2];
        for (int i = 0; i < bytes.length; i++) {
            bytes[i] = (byte) Integer.parseInt(digits.substring(2 * i, 2 * i + 2), 16);
        }
        String text = new String(bytes, StandardCharsets.UTF_16BE);
        return text.startsWith("﻿") ? text.substring(1) : text;
    }

    public static List<PDFobj> read(byte[] pdf) throws Exception {
        return new PDF().read(new ByteArrayInputStream(pdf));
    }

    public static List<PDFobj> read(byte[] pdf, String password) throws Exception {
        return new PDF().read(new ByteArrayInputStream(pdf), password);
    }

    /** Returns the first /ID of the trailer. */
    public static String trailerID(byte[] pdf) {
        String raw = latin1(pdf);
        int start = raw.lastIndexOf("/ID[<") + 5;
        return raw.substring(start, raw.indexOf('>', start));
    }

    /** Returns the object that holds the key, or null. */
    public static PDFobj findObject(List<PDFobj> objects, String key) {
        for (PDFobj obj : objects) {
            if (!obj.getValue(key).isEmpty()) {
                return obj;
            }
        }
        return null;
    }

    public static byte[] readAll(InputStream in) throws IOException {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        byte[] buf = new byte[8192];
        int count;
        while ((count = in.read(buf)) != -1) {
            bos.write(buf, 0, count);
        }
        return bos.toByteArray();
    }

    public static void assertXY(float x, float y, float[] xy) {
        assertEquals(x, xy[0], DELTA, "x");
        assertEquals(y, xy[1], DELTA, "y");
    }

    public static void assertRGB(float r, float g, float b, float[] rgb) {
        assertEquals(3, rgb.length);
        assertEquals(r, rgb[0], 0.0001f, "red");
        assertEquals(g, rgb[1], 0.0001f, "green");
        assertEquals(b, rgb[2], 0.0001f, "blue");
    }
}
