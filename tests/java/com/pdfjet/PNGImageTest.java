/*
 * PNGImageTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertArrayEquals;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertNull;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.FilterInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;
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
            Image image = new Image(TestSupport.newPDF(), in);
            assertEquals(32f, image.getWidth(), 0f);
            assertEquals(32f, image.getHeight(), 0f);
        } finally {
            in.close();
        }
    }

    // A PNG file with the IHDR of the size, bit depth and color type, a PLTE
    // chunk when there is a palette, and one IDAT chunk.
    private static byte[] png(int width, int height, int bitDepth, int colorType, byte[] palette, byte[] idat) {
        return png(width, height, bitDepth, colorType, 0, 0, palette, idat);
    }

    // The same file with the compression and the filter method of its IHDR
    // chunk, which are 0 in every PNG file that is defined.
    private static byte[] png(int width, int height, int bitDepth, int colorType,
            int compression, int filter, byte[] palette, byte[] idat) {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        bos.write(new byte[] {(byte) 0x89, 'P', 'N', 'G', '\r', '\n', 0x1A, '\n'}, 0, 8);
        ByteBuffer ihdr = ByteBuffer.allocate(13);
        ihdr.putInt(width).putInt(height).put((byte) bitDepth).put((byte) colorType)
                .put((byte) compression).put((byte) filter);
        chunk(bos, "IHDR", ihdr.array());
        if (palette != null) {
            chunk(bos, "PLTE", palette);
        }
        if (idat != null) {
            chunk(bos, "IDAT", idat);
        }
        chunk(bos, "IEND", new byte[0]);
        return bos.toByteArray();
    }

    private static void chunk(ByteArrayOutputStream bos, String type, byte[] data) {
        byte[] name = type.getBytes(StandardCharsets.US_ASCII);
        CRC32 crc = new CRC32();
        crc.update(name);
        crc.update(data);
        bos.write(ByteBuffer.allocate(4).putInt(data.length).array(), 0, 4);
        bos.write(name, 0, 4);
        bos.write(data, 0, data.length);
        bos.write(ByteBuffer.allocate(4).putInt((int) crc.getValue()).array(), 0, 4);
    }

    private static String decodeError(byte[] png) {
        return assertThrows(Exception.class, () -> new PNGImage(new ByteArrayInputStream(png))).getMessage();
    }

    @Test
    void decodesTheRowsOfTheImageAndIgnoresDataAfterThem() throws Exception {
        byte[] rgb = {1, 2, 3, 4, 5, 6};
        byte[] rows = {0, 1, 2, 3, 4, 5, 6};
        byte[] longer = {0, 1, 2, 3, 4, 5, 6, 9, 9, 9};
        for (byte[] data : new byte[][] {rows, longer}) {
            PNGImage png = new PNGImage(new ByteArrayInputStream(png(2, 1, 8, 2, null, Compressor.deflate(data))));
            assertArrayEquals(rgb, Decompressor.inflate(png.getData()));
        }
    }

    @Test
    void rejectsImageDataShorterThanTheImage() {
        byte[] rows = {0, 1, 2, 3};
        assertEquals("The PNG image data is shorter than the image.",
                decodeError(png(2, 1, 8, 2, null, Compressor.deflate(rows))));
    }

    @Test
    void rejectsAnImageLargerThanTheLimitBeforeDecodingIt() {
        // 20000 x 20000 RGB samples are 1.2 GB.
        assertEquals("The PNG image is larger than 268435456 bytes.",
                decodeError(png(20000, 20000, 8, 2, null, Compressor.deflate(new byte[1]))));
        // 10000 x 10000 palette indexes of 1 bit are 12.5 MB, but 400 MB of RGB and alpha.
        assertEquals("The PNG image is larger than 268435456 bytes.",
                decodeError(png(10000, 10000, 1, 3, new byte[6], Compressor.deflate(new byte[1]))));
        // The largest size, where the sizes multiplied overflow a long.
        int max = Integer.MAX_VALUE;
        assertEquals("The PNG image is larger than 268435456 bytes.",
                decodeError(png(max, max, 16, 6, null, Compressor.deflate(new byte[1]))));
        assertEquals("The PNG image is larger than 268435456 bytes.",
                decodeError(png(max, max, 1, 3, new byte[6], Compressor.deflate(new byte[1]))));
        assertEquals("The PNG image is larger than 268435456 bytes.",
                decodeError(png(max, 1, 16, 6, null, Compressor.deflate(new byte[1]))));
    }

    @Test
    void rejectsAnInvalidSizeBitDepthColorTypeOrPalette() {
        byte[] idat = Compressor.deflate(new byte[] {0, 0, 0, 0});
        assertEquals("Invalid PNG image size.", decodeError(png(0, 1, 8, 2, null, idat)));
        assertEquals("Invalid PNG image size.", decodeError(png(-1, 1, 8, 2, null, idat)));
        assertEquals("Invalid PNG bit depth 4 for color type 2.", decodeError(png(1, 1, 4, 2, null, idat)));
        assertEquals("Invalid PNG color type 5.", decodeError(png(1, 1, 8, 5, null, idat)));
        assertEquals("Invalid PNG color type 200.", decodeError(png(1, 1, 8, 200, null, idat)));
        assertEquals("Invalid PNG bit depth 200 for color type 2.", decodeError(png(1, 1, 200, 2, null, idat)));
        assertEquals("The PNG palette image has no PLTE chunk.", decodeError(png(1, 1, 8, 3, null, idat)));
        assertEquals("The PNG image has no image data.", decodeError(png(1, 1, 8, 2, null, null)));
    }

    @Test
    void aPaletteIndexPastThePaletteIsBlack() throws Exception {
        // A palette of 2 colors, and the indexes 1, 2 and 255.
        byte[] palette = {10, 20, 30, 40, 50, 60};
        PNGImage png = new PNGImage(new ByteArrayInputStream(
                png(3, 1, 8, 3, palette, Compressor.deflate(new byte[] {0, 1, 2, (byte) 255}))));
        assertArrayEquals(new byte[] {40, 50, 60, 0, 0, 0, 0, 0, 0}, Decompressor.inflate(png.getData()));
    }

    @Test
    void rejectsAPaletteOfNoColorsOrMoreThan256() {
        byte[] idat = Compressor.deflate(new byte[] {0, 0});
        assertEquals("Incorrect palette length.", decodeError(png(1, 1, 8, 3, new byte[0], idat)));
        assertEquals("Incorrect palette length.", decodeError(png(1, 1, 8, 3, new byte[3*257], idat)));
        assertEquals("Incorrect palette length.", decodeError(png(1, 1, 8, 3, new byte[4], idat)));
    }

    @Test
    void rejectsAnUnknownCompressionOrFilterMethod() throws Exception {
        // Only the deflate compression method and the adaptive filter method
        // are defined. libpng refuses a file of another one, and so does
        // Pillow for the filter method, where the rows of this one would be
        // read as if it were 0.
        byte[] idat = Compressor.deflate(new byte[] {0, 0});
        assertEquals("Unknown PNG compression method.",
                decodeError(png(1, 1, 8, 0, 1, 0, null, idat)));
        assertEquals("Unknown PNG filter method.",
                decodeError(png(1, 1, 8, 0, 0, 1, null, idat)));
        new PNGImage(new ByteArrayInputStream(png(1, 1, 8, 0, 0, 0, null, idat)));
    }

    @Test
    void rejectsAChunkLengthThatTheFileDoesNotHave() {
        byte[] valid = png(1, 1, 8, 2, null, Compressor.deflate(new byte[] {0, 0, 0, 0}));
        // The IDAT chunk starts after the signature and the 25 bytes of the IHDR chunk.
        byte[] lying = java.util.Arrays.copyOf(valid, 33 + 18);
        lying[33] = 0x7F;
        lying[34] = (byte) 0xFF;
        lying[35] = (byte) 0xFF;
        lying[36] = (byte) 0xF0;
        assertEquals("Unexpected end of the PNG stream.", decodeError(lying));
    }

    @Test
    void readsAStreamThatReturnsFewBytesAtATime() throws Exception {
        InputStream in = new FilterInputStream(TestSupport.open("PngSuite/BASN2C08.PNG")) {
            @Override
            public int read(byte[] b, int off, int len) throws IOException {
                return super.read(b, off, Math.min(len, 3));
            }
        };
        try {
            assertEquals("7855b9bf", crc(Decompressor.inflate(new PNGImage(in).getData())));
        } finally {
            in.close();
        }
    }

    @Test
    void aTruecolorImageWithASuggestedPaletteIsDecodedAsTruecolor() throws Exception {
        PNGImage png = decode("PS1N2C16");
        assertEquals(2, png.getColorType());
        assertEquals(6144, Decompressor.inflate(png.getData()).length);
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
            Image image = new Image(pdf, in);
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

    // A one pixel truecolor PNG with a pHYs chunk of the given pixels per unit
    // and unit: 1 is the metre and 0 is a ratio of the axes with no size.
    private static byte[] pngWithPhys(int width, int height, int x, int y, int unit) {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        bos.write(new byte[] {(byte) 0x89, 'P', 'N', 'G', '\r', '\n', 0x1A, '\n'}, 0, 8);
        ByteBuffer ihdr = ByteBuffer.allocate(13);
        ihdr.putInt(width).putInt(height).put((byte) 8).put((byte) 2)
                .put((byte) 0).put((byte) 0).put((byte) 0);
        chunk(bos, "IHDR", ihdr.array());
        chunk(bos, "pHYs", ByteBuffer.allocate(9).putInt(x).putInt(y).put((byte) unit).array());
        byte[] rows = new byte[height*(1 + 3*width)];
        chunk(bos, "IDAT", Compressor.deflate(rows));
        chunk(bos, "IEND", new byte[0]);
        return bos.toByteArray();
    }

    @Test
    void thePhysicalSizeChunkGivesTheSizeTheImageIsDrawnAt() throws Exception {
        // A pHYs chunk whose unit is the metre says how large the image is
        // meant to be, so it is drawn that size rather than one point for each
        // of its pixels. 11811 pixels per metre is 300 dots per inch, and 380
        // by 100 of them are 91.2 by 24 points.
        PNGImage png = new PNGImage(TestSupport.open("images/rgba-8bit-chunks.png"));
        assertEquals(380, png.getWidth());
        assertEquals(100, png.getHeight());
        assertEquals(91.2f, png.getPhysicalWidth(), 0.01f);
        assertEquals(24f, png.getPhysicalHeight(), 0.01f);

        PDF pdf = TestSupport.newPDF();
        Image image = new Image(pdf, TestSupport.open("images/rgba-8bit-chunks.png"));
        assertEquals(91.2f, image.getWidth(), 0.01f, "the image is not drawn at the size it asks for");
        assertEquals(24f, image.getHeight(), 0.01f, "the image is not drawn at the size it asks for");
    }

    @Test
    void thePhysicalSizeLeavesThePixelsOfTheImageObjectAlone() throws Exception {
        // The size the image is drawn at is not the size of its samples: the
        // image object of the PDF holds the pixels, whatever the chunk says.
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Image image = new Image(pdf, TestSupport.open("images/rgba-8bit-chunks.png"));
        image.setLocation(0f, 0f);
        image.drawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        assertTrue(raw.contains("/Width 380\n"), "the image object lost the pixels of the image");
        assertTrue(raw.contains("/Height 100\n"), "the image object lost the pixels of the image");
    }

    @Test
    void anImageWithNoPhysicalSizeIsDrawnAtOnePointForEachPixel() throws Exception {
        // Most PNG files carry no pHYs chunk, and are drawn as they were.
        PNGImage png = new PNGImage(TestSupport.open("PngSuite/BASN2C08.PNG"));
        assertEquals(0f, png.getPhysicalWidth());
        assertEquals(0f, png.getPhysicalHeight());
        PDF pdf = TestSupport.newPDF();
        Image image = new Image(pdf, TestSupport.open("PngSuite/BASN2C08.PNG"));
        assertEquals(32f, image.getWidth(), 0.01f);
        assertEquals(32f, image.getHeight(), 0.01f);
    }

    @Test
    void aPhysicalSizeChunkThatGivesNoSizeIsPassedOver() throws Exception {
        // Unit 0 is the ratio of the two axes and says nothing about how large
        // the image is, and a count of zero pixels gives no size either. The
        // image keeps the size of its pixels for both, and for a chunk that is
        // not the nine bytes the format has.
        assertEquals(0f, new PNGImage(new ByteArrayInputStream(
                pngWithPhys(8, 8, 1, 4, 0))).getPhysicalWidth(), "a ratio is not a size");
        assertEquals(0f, new PNGImage(new ByteArrayInputStream(
                pngWithPhys(8, 8, 0, 4724, 1))).getPhysicalWidth(), "no pixels per metre");
        assertEquals(0f, new PNGImage(new ByteArrayInputStream(
                pngWithPhys(8, 8, 4724, 0, 1))).getPhysicalWidth(), "no pixels per metre");
    }

    @Test
    void anImageOfPixelsThatAreNotSquareIsDrawnWithEachAxisOfItsOwnSize() throws Exception {
        // The chunk gives the pixels per metre of each axis on its own, so an
        // image of 4724 across and 2362 down is twice as tall as it is wide
        // for the same count of pixels.
        PNGImage png = new PNGImage(new ByteArrayInputStream(pngWithPhys(20, 20, 4724, 2362, 1)));
        assertEquals(12f, png.getPhysicalWidth(), 0.01f);
        assertEquals(24f, png.getPhysicalHeight(), 0.01f);
    }
}
