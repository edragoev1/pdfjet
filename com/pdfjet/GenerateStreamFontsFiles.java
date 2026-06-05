/**
 * GenerateStreamFontsFiles.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
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
    private static boolean useZopfli = true;

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
        for (int i = 0; i < otf.advanceWidth.length; i++) {
            writeInt16(otf.advanceWidth[i], baos);
        }

        writeInt32(otf.unicodeToGID.length, baos);
        for (int i = 0; i < otf.unicodeToGID.length; i++) {
            writeInt16(otf.unicodeToGID[i], baos);
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

    private static void compressWithZopfli(
            String fileName,
            BufferedOutputStream fos,
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
        if (args.length < 1) {
            System.err.println("Usage:");
            System.err.println("    ./generate-stream-fonts-files.sh <directory>");
            System.err.println("Examples:");
            System.err.println("    ./generate-stream-fonts-files.sh fonts/RedHatText/");
            System.err.println("    ./generate-stream-fonts-files.sh fonts/SourceCodePro/");
            System.exit(1);
        }
        File file = new File(args[0]);
        if (file.isDirectory()) {
            String path = file.getPath();
            String[] list = file.list();
            for (String fileName : list) {
                if (fileName.endsWith(".ttf") || fileName.endsWith(".otf")) {
                    System.out.println("Reading: " + fileName);
                    generateStreamFontFile(path + File.separator + fileName);
                    System.out.println("Writing: " + fileName + ".stream");
                }
            }
        } else {
            System.err.println("Usage:");
            System.err.println("    ./generate-stream-fonts-files.sh <directory>");
            System.err.println("Examples:");
            System.err.println("    ./generate-stream-fonts-files.sh fonts/RedHatText/");
            System.err.println("    ./generate-stream-fonts-files.sh fonts/SourceCodePro/");
            System.exit(1);
        }
    }
}
