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
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

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

    // A file of the repository, or a font PDFjet carries, as the Font
    // constructor that takes a path reads it.
    public static InputStream open(String path) throws IOException {
        File file = file(path);
        if (!file.exists() && path.startsWith("fonts/")) {
            return Font.open(path);
        }
        return new BufferedInputStream(new FileInputStream(file));
    }

    // The bytes of a file of the repository, or of a font PDFjet carries, as
    // open reads them: for the tests that read or patch a font file itself.
    public static byte[] fontBytes(String path) throws IOException {
        try (InputStream in = open(path)) {
            return readAll(in);
        }
    }

    /**
     * Returns the font with its character map rewritten so that the second
     * character of each pair is drawn with the glyph of the first, as Noto
     * Sans Arabic draws ١ and ۱, and ل and its isolated form ﻝ, with one
     * glyph, which no font PDFjet carries does in Arabic. The map is a format
     * 12 subtable of the Windows platform, of every character of the font to
     * its glyph, put at the end of the font in place of its cmap table, which
     * PDFjet reads as it reads any font's.
     */
    public static byte[] withSharedGlyphs(byte[] font, int[][] pairs) throws Exception {
        final java.util.TreeMap<Integer, Integer> map = new java.util.TreeMap<Integer, Integer>();
        new OTF(new ByteArrayInputStream(font)).unicodeToGID.each((c, gid) -> map.put(c, gid));
        for (int[] pair : pairs) {
            Integer gid = map.get(pair[0]);
            if (gid == null) {
                throw new IllegalArgumentException(String.format("the font has no U+%04X", pair[0]));
            }
            map.put(pair[1], gid);
        }
        java.nio.ByteBuffer cmap = java.nio.ByteBuffer.allocate(12 + 16 + 12 * map.size());
        cmap.putShort((short) 0).putShort((short) 1);                   // The version, one record
        cmap.putShort((short) 3).putShort((short) 10).putInt(12);       // Windows, Unicode full
        cmap.putShort((short) 12).putShort((short) 0).putInt(16 + 12 * map.size()).putInt(0).putInt(map.size());
        for (java.util.Map.Entry<Integer, Integer> e : map.entrySet()) {
            cmap.putInt(e.getKey()).putInt(e.getKey()).putInt(e.getValue());
        }
        byte[] patched = java.util.Arrays.copyOf(font, font.length + cmap.capacity());
        System.arraycopy(cmap.array(), 0, patched, font.length, cmap.capacity());
        int tables = (font[4] & 0xFF) << 8 | (font[5] & 0xFF);
        for (int i = 0; i < tables; i++) {
            int entry = 12 + 16 * i;
            if (new String(font, entry, 4, StandardCharsets.ISO_8859_1).equals("cmap")) {
                java.nio.ByteBuffer.wrap(patched, entry + 8, 8).putInt(font.length).putInt(cmap.capacity());
                return patched;
            }
        }
        throw new IllegalArgumentException("the font has no cmap table");
    }

    public static PDF newPDF() throws Exception {
        return new PDF(new ByteArrayOutputStream());
    }

    public static Font helvetica(PDF pdf) throws Exception {
        return new Font(pdf, CoreFont.HELVETICA);
    }

    /**
     * Returns IBM Plex Sans, a font that is embedded, as a document of PDF/A
     * or PDF/UA needs: a core font is refused in one (review of 11 October
     * 2026, #6).
     */
    public static Font plexSans(PDF pdf) throws Exception {
        return new Font(pdf, open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"));
    }

    /**
     * Lets the test draw in a core font in a document of PDF/A or PDF/UA,
     * which complete() refuses: for the tests of what such a document tags,
     * which find the text of a core font by its encoding. A test that sets it
     * sets it back to false when it ends, in a finally.
     */
    public static void allowCoreFonts(boolean allow) {
        PDF.coreFontsInCompliance = allow;
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

    /**
     * Returns the x and the y of the Td before the first text, drawn in a core
     * font, that starts with the text.
     */
    public static float[] positionOf(String content, String text) {
        int i = content.indexOf("<" + hex(text));
        if (i == -1) {
            throw new AssertionError("\"" + text + "\" is not drawn:\n" + content);
        }
        int td = content.lastIndexOf(" Td\n", i);
        String[] parts = content.substring(content.lastIndexOf('\n', td) + 1, td).split(" ");
        return new float[] {Float.parseFloat(parts[0]), Float.parseFloat(parts[1])};
    }

    /**
     * Returns the fill color set last before the text, drawn in a core font, as
     * its operator, for example "1 1 1 rg".
     */
    public static String fillColorBefore(String content, String text) {
        int rg = content.lastIndexOf(" rg\n", content.indexOf("<" + hex(text) + ">"));
        return content.substring(content.lastIndexOf('\n', rg) + 1, rg + 3);
    }

    /**
     * Returns the structure elements of the raw PDF by their object numbers:
     * the structure type, the parent's number and the /K of each.
     */
    public static Map<String, String[]> structElements(String raw) {
        Map<String, String[]> elements = new HashMap<String, String[]>();
        Matcher m = Pattern.compile(
                "(\\d+) 0 obj\n<<\n/Type /StructElem /S /(\\w+)\n/P (\\d+) 0 R /Pg \\d+ 0 R\n(?:/K (\\[[^\n]*\\]|<<[^\n]*>>|\\d+)\n)?")
                .matcher(raw);
        while (m.find()) {
            elements.put(m.group(1), new String[] {m.group(2), m.group(3), m.group(4)});
        }
        return elements;
    }

    /**
     * Returns the text as PDF writes a text string: UTF-16 with its byte order
     * mark, in lower case hexadecimal, as appendTextString writes it.
     */
    public static String textString(String text) {
        StringBuilder sb = new StringBuilder("<feff");
        for (char c : text.toCharArray()) {
            sb.append(String.format("%04x", (int) c));
        }
        return sb.append(">").toString();
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

    /**
     * Returns a PDF of the objects, numbered from 1, and a last one that is
     * the content stream, with a cross-reference table and a trailer whose
     * /Root is object 1.
     */
    public static byte[] minimalPDF(String[] objects, String content) {
        StringBuilder sb = new StringBuilder("%PDF-1.7\n");
        int[] offsets = new int[objects.length + 1];
        for (int i = 0; i < objects.length; i++) {
            offsets[i] = sb.length();
            sb.append(i + 1).append(" 0 obj\n").append(objects[i]).append("\nendobj\n");
        }
        offsets[objects.length] = sb.length();
        sb.append(objects.length + 1).append(" 0 obj\n<< /Length ").append(content.length())
                .append(" >>\nstream\n").append(content).append("\nendstream\nendobj\n");
        int xref = sb.length();
        sb.append("xref\n0 ").append(objects.length + 2).append("\n0000000000 65535 f \n");
        for (int offset : offsets) {
            sb.append(String.format("%010d 00000 n \n", offset));
        }
        sb.append("trailer\n<< /Size ").append(objects.length + 2).append(" /Root 1 0 R >>\nstartxref\n")
                .append(xref).append("\n%%EOF\n");
        return sb.toString().getBytes(StandardCharsets.ISO_8859_1);
    }

    public static List<PDFobj> read(byte[] pdf) throws Exception {
        return new PDF().read(new ByteArrayInputStream(pdf));
    }

    public static List<PDFobj> read(byte[] pdf, String password) throws Exception {
        return new PDF().read(new ByteArrayInputStream(pdf), password);
    }

    /** Returns the bytes the decryptor tries for an AES-256 password, in order. */
    public static byte[][] aes256Passwords(String password) {
        return Decryptor.aes256Passwords(password);
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

    /** A piece of work whose time a test measures. */
    interface Timed {
        void run() throws Exception;
    }

    /**
     * Returns the milliseconds the work takes, the shortest of up to three
     * runs: a run over the limit is run again, as a machine under load can
     * make a linear run slow once, where a quadratic one is slow every time;
     * a test failed at 7,066 ms of 5,000 under load (review of 12 October
     * 2026, C6).
     */
    static long shortestMillis(long limit, Timed work) throws Exception {
        long shortest = Long.MAX_VALUE;
        for (int run = 0; run < 3 && shortest >= limit; run++) {
            long start = System.nanoTime();
            work.run();
            shortest = Math.min(shortest, (System.nanoTime() - start) / 1000000L);
        }
        return shortest;
    }

    interface Sized {
        void run(int n) throws Exception;
    }

    // Checks that the work of size 2n takes less than 3.8 times
    // the work of size n, each the shortest of three runs: the time of a linear
    // algorithm doubles, and that of a quadratic one, whose time dominates at n,
    // comes near four times. A ratio holds on a busy computer, where limits in
    // seconds failed (CI, 30 October 2026); the sizes are timed in turns, and all
    // again up to three times, as load can fall on one of them only, where a
    // quadratic algorithm fails every time.
    static void assertLinearTime(int n, Sized work) throws Exception {
        long small = 0L;
        long large = 0L;
        for (int attempt = 0; attempt < 3; attempt++) {
            small = Long.MAX_VALUE;
            large = Long.MAX_VALUE;
            for (int run = 0; run < 3; run++) {
                long start = System.nanoTime();
                work.run(n);
                small = Math.min(small, System.nanoTime() - start);
                start = System.nanoTime();
                work.run(2*n);
                large = Math.min(large, System.nanoTime() - start);
            }
            if (5L*large < 19L*small) {
                return;
            }
        }
        org.junit.jupiter.api.Assertions.fail(
                "size " + 2*n + " took " + large/1000000L + " ms, and size " + n + " " + small/1000000L + " ms");
    }
}
