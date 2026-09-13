/*
 * PNGImageTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertNull;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.InputStream;
import java.util.zip.CRC32;
import org.junit.jupiter.api.Test;

/**
 * PNG decoding against the PngSuite images. The samples are compared as the
 * CRC-32 of the decompressed data. MuPDF and Pillow decode the 8-bit, filtered
 * and palette images, and the alpha of BASN6A08, BASN4A08 and TP1N3P08, to the
 * same samples, and Pillow the colors of BASN6A08 and the gray of BASN4A08; the
 * 1, 2, 4 and 16 bit grayscale and 16 bit RGB samples keep their bit depth here.
 */
class PNGImageTest {
    // name, width, height, color type, bit depth, sample bytes, sample CRC, alpha bytes, alpha CRC
    private static final String[][] SUITE = {
        {"BASN0G01", "32", "32", "0", "1", "128", "b71a0667", "", ""},
        {"BASN0G02", "32", "32", "0", "2", "256", "c429db1d", "", ""},
        {"BASN0G04", "32", "32", "0", "4", "512", "8089a6e9", "", ""},
        {"BASN0G08", "32", "32", "0", "8", "1024", "784b4a4e", "", ""},
        {"BASN0G16", "32", "32", "0", "16", "2048", "9362f0f0", "", ""},
        {"BASN2C08", "32", "32", "2", "8", "3072", "7855b9bf", "", ""},
        {"BASN2C16", "32", "32", "2", "16", "6144", "c278125a", "", ""},
        {"BASN3P01", "32", "32", "3", "1", "3072", "31ec284b", "", ""},
        {"BASN3P02", "32", "32", "3", "2", "3072", "279a463a", "", ""},
        {"BASN3P04", "32", "32", "3", "4", "3072", "3a9e038e", "", ""},
        {"BASN3P08", "32", "32", "3", "8", "3072", "ff6e2940", "", ""},
        {"BASN6A08", "32", "32", "6", "8", "3072", "a9b0c6b5", "1024", "fa6029ad"},
        {"TP1N3P08", "32", "32", "3", "8", "3072", "8b0a6c2c", "1024", "f83b2838"},
        {"F00N2C08", "32", "32", "2", "8", "3072", "3f1d66ad", "", ""},
        {"F01N2C08", "32", "32", "2", "8", "3072", "11c1b27e", "", ""},
        {"F02N2C08", "32", "32", "2", "8", "3072", "7f1ca785", "", ""},
        {"F03N2C08", "32", "32", "2", "8", "3072", "31645d89", "", ""},
        {"F04N2C08", "32", "32", "2", "8", "3072", "77056a6f", "", ""},
        {"F00N0G08", "32", "32", "0", "8", "1024", "1f18265f", "", ""},
        {"F04N0G08", "32", "32", "0", "8", "1024", "b8006228", "", ""},
        {"S01N3P01", "1", "1", "3", "1", "3", "d243369f", "", ""},
        {"S05N3P02", "5", "5", "3", "2", "75", "1242b6fb", "", ""},
    };

    private static String crc(byte[] data) {
        CRC32 crc = new CRC32();
        crc.update(data);
        return String.format("%08x", crc.getValue());
    }

    private static PNGImage decode(String name) throws Exception {
        InputStream in = TestSupport.open("PngSuite/" + name + ".PNG");
        try {
            return new PNGImage(in);
        } finally {
            in.close();
        }
    }

    @Test
    void decodesThePngSuiteImages() throws Exception {
        for (String[] row : SUITE) {
            PNGImage png = decode(row[0]);
            String name = row[0];
            assertEquals(Integer.parseInt(row[1]), png.getWidth(), name);
            assertEquals(Integer.parseInt(row[2]), png.getHeight(), name);
            assertEquals(Integer.parseInt(row[3]), png.getColorType(), name);
            assertEquals(Integer.parseInt(row[4]), png.getBitDepth(), name);
            byte[] samples = Decompressor.inflate(png.getData());
            assertEquals(Integer.parseInt(row[5]), samples.length, name);
            assertEquals(row[6], crc(samples), name);
            if (row[7].isEmpty()) {
                assertNull(png.getAlpha(), name);
            } else {
                byte[] alpha = Decompressor.inflate(png.getAlpha());
                assertEquals(Integer.parseInt(row[7]), alpha.length, name);
                assertEquals(row[8], crc(alpha), name);
            }
        }
    }

    @Test
    void truecolorTransparencyIsIgnored() throws Exception {
        // tRNS applies to palette images only; TBRN2C08 has the samples of TP1N3P08.
        PNGImage png = decode("TBRN2C08");
        assertEquals("8b0a6c2c", crc(Decompressor.inflate(png.getData())));
        assertNull(png.getAlpha());
    }

    @Test
    void rejects16BitRgbaWithAMessage() {
        Exception e = assertThrows(Exception.class, () -> decode("BASN6A16"));
        assertEquals("Image with unsupported bit depth == 16", e.getMessage());
    }

    @Test
    void rejectsDataThatIsNotAPng() {
        assertThrows(Exception.class, () -> new PNGImage(new ByteArrayInputStream("not a png file".getBytes("US-ASCII"))));
    }

    @Test
    void anImageFromAPngHasItsSize() throws Exception {
        InputStream in = TestSupport.open("PngSuite/BASN2C08.PNG");
        try {
            Image image = new Image(TestSupport.newPDF(), in, ImageType.PNG);
            assertEquals(32f, image.getWidth(), 0f);
            assertEquals(32f, image.getHeight(), 0f);
        } finally {
            in.close();
        }
    }

    @Test
    void decodesGrayscaleWithAlpha() throws Exception {
        PNGImage png = decode("BASN4A08");
        assertEquals(32, png.getWidth());
        assertEquals(32, png.getHeight());
        assertEquals(4, png.getColorType());
        assertEquals(8, png.getBitDepth());
        byte[] gray = Decompressor.inflate(png.getData());
        byte[] alpha = Decompressor.inflate(png.getAlpha());
        assertEquals(1024, gray.length);
        assertEquals("bfc7e22b", crc(gray));
        assertEquals(1024, alpha.length);
        assertEquals("fa6029ad", crc(alpha));
    }

    @Test
    void anImageFromAGrayscalePngWithAlphaIsGrayWithASoftMask() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        InputStream in = TestSupport.open("PngSuite/BASN4A08.PNG");
        try {
            Image image = new Image(pdf, in, ImageType.PNG);
            image.drawOn(new Page(pdf, Letter.PORTRAIT));
        } finally {
            in.close();
        }
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        assertTrue(raw.contains("DeviceGray"), "gray color space");
        assertTrue(raw.contains("/SMask"), "soft mask");
        assertFalse(raw.contains("DeviceRGB"), "no RGB color space");
    }

    @Test
    void rejects16BitGrayscaleWithAlphaWithAMessage() {
        Exception e = assertThrows(Exception.class, () -> decode("BASN4A16"));
        assertEquals("Image with unsupported bit depth == 16", e.getMessage());
    }

    @Test
    void rejectsInterlacedImagesWithAClearError() {
        Exception e = assertThrows(Exception.class, () -> decode("BASI0G08"));
        assertEquals("Interlaced PNG images are not supported.\n"
                + "Convert the image using OptiPNG:\noptipng -i0 -o7 myimage.png", e.getMessage());
    }
}
