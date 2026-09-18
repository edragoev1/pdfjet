/*
 * GenerateStreamFontsFiles.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
// In the package of the library, whose OTF parser it uses, but kept out of
// the library: util/generate-stream-fonts-files.sh compiles it on its own.
package com.pdfjet;

import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;
import java.util.zip.*;

/**
 * This program generates .ttf.stream or .otf.stream fonts files from standard TTF or OTF fonts.
 * The .otf.stream and .ttf.stream files can be embedded in PDFs much faster.
 * The generated PDFs using these stream fonts will me smaller in size.
 */
public class GenerateStreamFontsFiles {
    private GenerateStreamFontsFiles() {
    }

    private static boolean useZopfli = true;

    // True to write the format that every version of the library reads: the
    // CFF data of an OpenType font without its other tables, and no marks.
    private static boolean oldFormat = false;

    /**
     * Generates .ttf.stream or .otf.stream font files from standard TTF or OTF fonts.
     *
     * @param fileName the file name
     * @throws Exception if the font file is not found
     */
    public static void generateStreamFontFile(String fileName) throws Exception {
        BufferedOutputStream fos =
                new BufferedOutputStream(new FileOutputStream(fileName + ".stream"));

        OTF otf = new OTF(new FileInputStream(fileName));
        byte[] name = otf.fontName.getBytes(StandardCharsets.UTF_8);
        fos.write(name.length);
        fos.write(name);

        byte[] info = otf.fontInfo.getBytes(StandardCharsets.UTF_8);
        writeInt24(info.length, fos);
        fos.write(info);

        ByteArrayOutputStream baos = new ByteArrayOutputStream(32768);
        writeInt32(otf.unitsPerEm, baos);
        writeInt32(otf.bBoxLLx, baos);
        writeInt32(otf.bBoxLLy, baos);
        writeInt32(otf.bBoxURx, baos);
        writeInt32(otf.bBoxURy, baos);
        writeInt32(otf.ascent, baos);
        writeInt32(otf.descent, baos);
        writeInt32(otf.firstChar, baos);
        writeInt32(otf.lastChar, baos);
        writeInt32(otf.capHeight, baos);
        writeInt32(otf.underlinePosition, baos);
        writeInt32(otf.underlineThickness, baos);

        writeInt32(otf.advanceWidth.length, baos);
        for (int width : otf.advanceWidth) {
            writeInt16(width, baos);
        }

        writeInt32(otf.unicodeToGID.length, baos);
        for (int gid : otf.unicodeToGID) {
            writeInt16(gid, baos);
        }

        // Where the GPOS table of the font puts the marks, after the metrics,
        // where a library that does not read it stops: for each MarkToBase and
        // MarkToLigature subtable the class and anchor of each mark and the
        // anchors of each letter, and then the offsets of the marks that go on
        // other marks. Every number is an int32, in the order of the glyph IDs.
        // It is compressed on its own, so that the library keeps it as it is
        // and reads it only when it draws a mark.
        if (otf.markAnchors != null && !oldFormat) {
            ByteArrayOutputStream marksBuf = new ByteArrayOutputStream(32768);
            writeInt32(otf.markAnchors.size(), marksBuf);
            for (int i = 0; i < otf.markAnchors.size(); i++) {
                java.util.Map<Integer, int[]> marks = new java.util.TreeMap<Integer, int[]>(otf.markAnchors.get(i));
                writeInt32(marks.size(), marksBuf);
                for (java.util.Map.Entry<Integer, int[]> mark : marks.entrySet()) {
                    writeInt32(mark.getKey(), marksBuf);
                    for (int value : mark.getValue()) {     // Class, x and y
                        writeInt32(value, marksBuf);
                    }
                }
                java.util.Map<Integer, int[]> bases = new java.util.TreeMap<Integer, int[]>(otf.baseAnchors.get(i));
                writeInt32(bases.size(), marksBuf);
                for (java.util.Map.Entry<Integer, int[]> base : bases.entrySet()) {
                    writeInt32(base.getKey(), marksBuf);
                    writeInt32(base.getValue().length, marksBuf);
                    for (int value : base.getValue()) {
                        writeInt32(value, marksBuf);
                    }
                }
            }
            // The key of a pair is the glyph ID of the mark it goes on, times
            // 65536, plus the glyph ID of the mark, as the library keeps it.
            java.util.Map<Integer, int[]> pairs = new java.util.TreeMap<Integer, int[]>(otf.markToMarkOffsets);
            writeInt32(pairs.size(), marksBuf);
            for (java.util.Map.Entry<Integer, int[]> pair : pairs.entrySet()) {
                writeInt32(pair.getKey() >>> 16, marksBuf);
                writeInt32(pair.getKey() & 0xFFFF, marksBuf);
                writeInt32(pair.getValue()[0], marksBuf);
                writeInt32(pair.getValue()[1], marksBuf);
            }
            writeMarks(fileName, baos, marksBuf.toByteArray());
        }

        // The line gap of a font that has one, after the marks, where a library
        // that does not read it stops. A font without marks gets empty ones
        // first: no subtables and no pairs.
        if (otf.lineGap != 0 && !oldFormat) {
            if (otf.markAnchors == null) {
                writeMarks(fileName, baos, new byte[8]);
            }
            writeInt32(otf.lineGap, baos);
        }

        byte[] buf1 = baos.toByteArray();
        if (GenerateStreamFontsFiles.useZopfli) {
            compressWithZopfli(fileName, fos, buf1, false);
        } else {
            ByteArrayOutputStream buf2 = new ByteArrayOutputStream(0xFFFF);
            Deflater deflater = new Deflater(Deflater.BEST_COMPRESSION);
            DeflaterOutputStream dos1 = new DeflaterOutputStream(buf2, deflater);
            dos1.write(buf1, 0, buf1.length);
            dos1.finish();
            deflater.end();
            writeInt32(buf2.size(), fos);
            buf2.writeTo(fos);
        }

        if (otf.cff == true && !oldFormat) {
            // The tables that are not in the CFF data, so that the stream
            // holds the whole font: the original is these bytes with the CFF
            // table put back at the offset its table directory entry gives.
            byte[] rest = new byte[otf.buf.length - otf.cffLen];
            System.arraycopy(otf.buf, 0, rest, 0, otf.cffOff);
            System.arraycopy(otf.buf, otf.cffOff + otf.cffLen,
                    rest, otf.cffOff, otf.buf.length - otf.cffOff - otf.cffLen);
            fos.write('R');
            if (GenerateStreamFontsFiles.useZopfli) {
                compressWithZopfli(fileName, fos, rest, false);
            } else {
                ByteArrayOutputStream buf5 = new ByteArrayOutputStream(0xFFFF);
                Deflater deflater = new Deflater(Deflater.BEST_COMPRESSION);
                DeflaterOutputStream dos = new DeflaterOutputStream(buf5, deflater);
                dos.write(rest, 0, rest.length);
                dos.finish();
                deflater.end();
                writeInt32(buf5.size(), fos);
                buf5.writeTo(fos);
            }
        }

        byte[] buf3 = otf.buf;
        if (otf.cff == true) {
            fos.write('Y');
            buf3 = new byte[otf.cffLen];
            for (int i = 0; i < otf.cffLen; i++) {
                buf3[i] = otf.buf[otf.cffOff + i];
            }
        } else {
            fos.write('N');
        }

        if (GenerateStreamFontsFiles.useZopfli) {
            compressWithZopfli(fileName, fos, buf3, true);
        } else {
            ByteArrayOutputStream buf4 = new ByteArrayOutputStream(0xFFFF);
            Deflater deflater = new Deflater(Deflater.BEST_COMPRESSION);
            DeflaterOutputStream dos = new DeflaterOutputStream(buf4, deflater);
            dos.write(buf3, 0, buf3.length);
            dos.finish();
            deflater.end();
            writeInt32(buf3.length, fos);   // Uncompressed font size
            writeInt32(buf4.size(), fos);   // Compressed font size
            buf4.writeTo(fos);
        }
        fos.close();
    }

    // Writes the marks compressed on their own, after their compressed size.
    private static void writeMarks(String fileName, ByteArrayOutputStream baos, byte[] marks) throws IOException {
        if (GenerateStreamFontsFiles.useZopfli) {
            compressWithZopfli(fileName, baos, marks, false);
        } else {
            ByteArrayOutputStream buf6 = new ByteArrayOutputStream(0xFFFF);
            Deflater deflater = new Deflater(Deflater.BEST_COMPRESSION);
            DeflaterOutputStream dos = new DeflaterOutputStream(buf6, deflater);
            dos.write(marks, 0, marks.length);
            dos.finish();
            deflater.end();
            writeInt32(buf6.size(), baos);
            buf6.writeTo(baos);
        }
    }

    private static void compressWithZopfli(
            String fileName,
            OutputStream fos,
            byte[] buf3,
            boolean uncompressed) throws IOException {
        BufferedOutputStream fos4 =
                new BufferedOutputStream(new FileOutputStream(fileName + ".tmp"));
        fos4.write(buf3, 0, buf3.length);
        fos4.close();
        final List<String> command = new ArrayList<String>();
        command.add("util/zopfli/zopfli");
        command.add("-c");
        command.add("--zlib");
        command.add("--i100");
        command.add(fileName + ".tmp");
        final Process process = new ProcessBuilder(command).start();
        final InputStream input = process.getInputStream();
        final byte[] buf = new byte[4096];
        ByteArrayOutputStream buf5 = new ByteArrayOutputStream(0xFFFF);
        int len;
        while ((len = input.read(buf)) != -1) {
            buf5.write(buf, 0, len);
        }
        if (uncompressed) {
            writeInt32(buf3.length, fos);   // Uncompressed font size
        }
        writeInt32(buf5.size(), fos);       // Compressed font size
        buf5.writeTo(fos);
        new File(fileName + ".tmp").delete();
    }

    private static void writeInt16(int i, OutputStream stream) throws IOException {
        stream.write((i >>  8) & 0xff);
        stream.write((i >>  0) & 0xff);
    }

    private static void writeInt24(int i, OutputStream stream) throws IOException {
        stream.write((i >> 16) & 0xff);
        stream.write((i >>  8) & 0xff);
        stream.write((i >>  0) & 0xff);
    }

    private static void writeInt32(int i, OutputStream stream) throws IOException {
        stream.write((i >> 24) & 0xff);
        stream.write((i >> 16) & 0xff);
        stream.write((i >>  8) & 0xff);
        stream.write((i >>  0) & 0xff);
    }

    /**
     * Entry point for the GenerateStreamFontsFiles converter
     *
     * @param args the arguments
     * @throws Exception if there is a problem
     */
    public static void main(String[] args) throws Exception {
        String directory = null;
        for (String arg : args) {
            if (arg.equals("--old-format")) {
                oldFormat = true;
            } else if (directory == null && !arg.startsWith("-")) {
                directory = arg;
            } else {
                directory = null;
                break;
            }
        }
        File file = (directory == null) ? null : new File(directory);
        if (file == null || !file.isDirectory()) {
            System.err.println("Usage:");
            System.err.println("    util/generate-stream-fonts-files.sh [--old-format] <directory>");
            System.err.println();
            System.err.println("--old-format writes .otf.stream files that every version of");
            System.err.println("PDFjet reads: the CFF data of the font without its other tables,");
            System.err.println("and no GPOS marks. By default the whole font is kept.");
            System.err.println();
            System.err.println("Example:");
            System.err.println("    util/generate-stream-fonts-files.sh fonts/IBMPlexSans");
            System.exit(1);
        }
        String path = file.getPath();
        String[] list = file.list();
        java.util.Arrays.sort(list);
        for (String fileName : list) {
            if (fileName.endsWith(".ttf") || fileName.endsWith(".otf")) {
                System.out.println("Reading: " + fileName);
                generateStreamFontFile(path + File.separator + fileName);
                System.out.println("Writing: " + fileName + ".stream");
            }
        }
    }
}
