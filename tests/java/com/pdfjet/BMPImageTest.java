/*
 * BMPImageTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertArrayEquals;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.Arrays;
import org.junit.jupiter.api.Test;

class BMPImageTest {
    // Red, green on the top row; blue, white on the bottom row.
    private static final int[][][] PIXELS = {{{255, 0, 0}, {0, 255, 0}}, {{0, 0, 255}, {255, 255, 255}}};

    private static byte[] bmp24(boolean topDown) {
        int width = 2;
        int height = 2;
        int rowSize = (width * 3 + 3) & ~3;
        ByteBuffer header = ByteBuffer.allocate(54).order(ByteOrder.LITTLE_ENDIAN);
        header.put((byte) 'B').put((byte) 'M').putInt(54 + rowSize * height).putInt(0).putInt(54);
        header.putInt(40).putInt(width).putInt(topDown ? -height : height).putShort((short) 1).putShort((short) 24);
        header.putInt(0).putInt(rowSize * height).putInt(2835).putInt(2835).putInt(0).putInt(0);
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        bos.write(header.array(), 0, 54);
        for (int i = 0; i < height; i++) {
            int[][] row = PIXELS[topDown ? i : height - 1 - i];
            for (int[] pixel : row) {
                bos.write(pixel[2]);
                bos.write(pixel[1]);
                bos.write(pixel[0]);
            }
            for (int pad = width * 3; pad < rowSize; pad++) {
                bos.write(0);
            }
        }
        return bos.toByteArray();
    }

    private static final byte[] RGB = {
        (byte) 255, 0, 0, 0, (byte) 255, 0, 0, 0, (byte) 255, (byte) 255, (byte) 255, (byte) 255};

    @Test
    void bottomUpRowsAreReadTopRowFirst() throws Exception {
        BMPImage bmp = new BMPImage(new ByteArrayInputStream(bmp24(false)));
        assertEquals(2, bmp.getWidth());
        assertEquals(2, bmp.getHeight());
        assertArrayEquals(RGB, Decompressor.inflate(bmp.getData()));
    }

    @Test
    void topDownRowsAreReadTheSame() throws Exception {
        assertArrayEquals(RGB, Decompressor.inflate(new BMPImage(new ByteArrayInputStream(bmp24(true))).getData()));
    }

    // The 54 byte header of a BMP file of the size, bits per pixel and palette colors.
    private static byte[] header(int width, int height, int bitsPerPixel, int colors) {
        ByteBuffer header = ByteBuffer.allocate(54).order(ByteOrder.LITTLE_ENDIAN);
        header.put((byte) 'B').put((byte) 'M').putInt(54).putInt(0).putInt(54);
        header.putInt(40).putInt(width).putInt(height).putShort((short) 1).putShort((short) bitsPerPixel);
        header.putInt(0).putInt(0).putInt(2835).putInt(2835).putInt(colors).putInt(0);
        return header.array();
    }

    private static String decodeError(byte[] bmp) {
        return assertThrows(Exception.class, () -> new BMPImage(new ByteArrayInputStream(bmp))).getMessage();
    }

    @Test
    void rejectsAnInvalidSize() {
        assertEquals("Invalid BMP image size.", decodeError(header(0, 2, 24, 0)));
        assertEquals("Invalid BMP image size.", decodeError(header(2, 0, 24, 0)));
        assertEquals("Invalid BMP image size.", decodeError(header(-2, 2, 24, 0)));
        assertEquals("Invalid BMP image size.", decodeError(header(2, Integer.MIN_VALUE, 24, 0)));
    }

    @Test
    void rejectsAnImageLargerThanTheLimitBeforeReadingIt() {
        // 20000 x 20000 pixels are 1.2 GB of RGB; the file has only the header.
        assertEquals("The BMP image is larger than 268435456 bytes.", decodeError(header(20000, 20000, 24, 0)));
        // The largest size, where the sizes multiplied overflow a long.
        int max = Integer.MAX_VALUE;
        assertEquals("The BMP image is larger than 268435456 bytes.", decodeError(header(max, max, 32, 0)));
        assertEquals("The BMP image is larger than 268435456 bytes.", decodeError(header(max, 1, 32, 0)));
    }

    @Test
    void rejectsAnUnsupportedBitDepthOrALargePalette() {
        assertEquals("Can only parse 1 bit, 4bit, 8bit, 16bit, 24bit and 32bit images",
                decodeError(header(2, 2, 2, 0)));
        assertEquals("Invalid BMP palette size 2147483647.", decodeError(header(2, 2, 8, 0x7FFFFFFF)));
    }

    @Test
    void aTruncatedFileThrows() {
        byte[] truncated = Arrays.copyOf(bmp24(false), 60);
        assertThrows(Exception.class, () -> new BMPImage(new ByteArrayInputStream(truncated)));
    }

    // A BMP file of the 2 by 2 pixels with a header of the size, 40 bytes or
    // more, the bits per pixel, the compression, the masks after the first 40
    // bytes of the header, the palette of 0xRRGGBB colors, and the pixels of
    // the bottom row and then the top row, each padded to 4 bytes.
    private static byte[] bmp(int headerSize, int bitsPerPixel, int compression,
            int[] masks, int[] palette, byte[] bottomRow, byte[] topRow) {
        int colors = (palette == null) ? 0 : palette.length;
        int afterHeader = (headerSize == 40 && masks != null) ? 12 : 0;
        int offset = 14 + headerSize + afterHeader + 4 * colors;
        int rowSize = (bottomRow.length + 3) & ~3;
        ByteBuffer buf = ByteBuffer.allocate(offset + 2 * rowSize).order(ByteOrder.LITTLE_ENDIAN);
        buf.put((byte) 'B').put((byte) 'M').putInt(buf.capacity()).putInt(0).putInt(offset);
        buf.putInt(headerSize).putInt(2).putInt(2).putShort((short) 1).putShort((short) bitsPerPixel);
        buf.putInt(compression).putInt(2 * rowSize).putInt(2835).putInt(2835).putInt(colors).putInt(0);
        if (masks != null) {
            buf.putInt(masks[0]).putInt(masks[1]).putInt(masks[2]);
        }
        buf.position(offset - 4 * colors);      // The rest of a larger header is 0
        for (int i = 0; i < colors; i++) {
            buf.put((byte) palette[i]).put((byte) (palette[i] >> 8)).put((byte) (palette[i] >> 16)).put((byte) 0);
        }
        buf.put(bottomRow).position(offset + rowSize);
        buf.put(topRow);
        return buf.array();
    }

    private static byte[] decode(byte[] bmp) throws Exception {
        return Decompressor.inflate(new BMPImage(new ByteArrayInputStream(bmp)).getData());
    }

    // The pixels as little endian 16 bit values.
    private static byte[] shorts(int... pixels) {
        ByteBuffer buf = ByteBuffer.allocate(2 * pixels.length).order(ByteOrder.LITTLE_ENDIAN);
        for (int pixel : pixels) {
            buf.putShort((short) pixel);
        }
        return buf.array();
    }

    private static byte[] bytes(int... values) {
        byte[] bytes = new byte[values.length];
        for (int i = 0; i < values.length; i++) {
            bytes[i] = (byte) values[i];
        }
        return bytes;
    }

    @Test
    void aThirtyTwoBitPixelIsBlueGreenRedAndAByteThatIsNotAColor() throws Exception {
        byte[] bmp = bmp(40, 32, 0, null, null,
                bytes(255, 0, 0, 0, 255, 255, 255, 0), bytes(0, 0, 255, 0, 0, 255, 0, 0));
        assertArrayEquals(RGB, decode(bmp));
    }

    @Test
    void theMasksOfSixteenAndThirtyTwoBitPixelsAreRead() throws Exception {
        // 5 bits a color without masks; 31 of 5 bits is 255.
        assertArrayEquals(RGB, decode(bmp(40, 16, 0, null, null,
                shorts(0x001F, 0x7FFF), shorts(0x7C00, 0x03E0))));
        // 5, 6 and 5 bits.
        assertArrayEquals(RGB, decode(bmp(40, 16, 3, new int[] {0xF800, 0x07E0, 0x001F}, null,
                shorts(0x001F, 0xFFFF), shorts(0xF800, 0x07E0))));
        // Red in the low byte, in a 108 byte header.
        assertArrayEquals(RGB, decode(bmp(108, 32, 3, new int[] {0x000000FF, 0x0000FF00, 0x00FF0000}, null,
                bytes(0, 0, 255, 0, 255, 255, 255, 0), bytes(255, 0, 0, 0, 0, 255, 0, 0))));
    }

    @Test
    void thePaletteFollowsAHeaderOfAnySize() throws Exception {
        int[] palette = {0xFF0000, 0x00FF00, 0x0000FF, 0xFFFFFF};
        assertArrayEquals(RGB, decode(bmp(40, 8, 0, null, palette, bytes(2, 3), bytes(0, 1))));
        assertArrayEquals(RGB, decode(bmp(124, 8, 0, null, palette, bytes(2, 3), bytes(0, 1))));
    }

    @Test
    void rejectsACompressedImage() {
        int[] palette = {0xFF0000, 0x00FF00, 0x0000FF, 0xFFFFFF};
        // RLE8: a run of 1 pixel of color 2, 1 of color 3, the end of the bitmap
        assertEquals("Compressed BMP images are not supported.",
                decodeError(bmp(40, 8, 1, null, palette, bytes(1, 2, 1, 3), bytes(0, 1, 0, 0))));
    }

    @Test
    void aPaletteIndexPastThePaletteIsBlack() throws Exception {
        // A palette of 2 colors, and the indexes 1 and 5 in the top row.
        byte[] bmp = bmp(40, 8, 0, null, new int[] {0x102030, 0x405060}, bytes(0, 0), bytes(1, 5));
        assertArrayEquals(bytes(0x40, 0x50, 0x60, 0, 0, 0, 0x10, 0x20, 0x30, 0x10, 0x20, 0x30), decode(bmp));
    }

    @Test
    void theLastRowCanBeWithoutItsPadding() throws Exception {
        byte[] bmp = bmp(40, 24, 0, null, null, bytes(1, 2, 3, 4, 5, 6), bytes(7, 8, 9, 10, 11, 12));
        // The 2 bytes of padding of the top row, which is the last one.
        assertArrayEquals(decode(bmp), decode(java.util.Arrays.copyOf(bmp, bmp.length - 2)));
        assertEquals("Unexpected end of stream: expected 6 bytes",
                decodeError(java.util.Arrays.copyOf(bmp, bmp.length - 3)));
    }

    @Test
    void thePixelsPerMeterOfTheHeaderGiveTheSizeTheImageIsDrawnAt() throws Exception {
        // The header of a BMP holds the pixels per metre of each axis.
        // palette.bmp is 100 by 100 pixels at 4724 per metre, which is 120
        // dots per inch and 60 by 60 points.
        BMPImage bmp = new BMPImage(TestSupport.open("images/palette.bmp"));
        assertEquals(100, bmp.getWidth());
        assertEquals(100, bmp.getHeight());
        assertEquals(60f, bmp.getPhysicalWidth(), 0.05f);
        assertEquals(60f, bmp.getPhysicalHeight(), 0.05f);

        PDF pdf = TestSupport.newPDF();
        Image image = new Image(pdf, TestSupport.open("images/palette.bmp"));
        assertEquals(60f, image.getWidth(), 0.05f, "the image is not drawn at the size it asks for");
        assertEquals(60f, image.getHeight(), 0.05f, "the image is not drawn at the size it asks for");
    }

    @Test
    void aHeaderWithNoPixelsPerMeterLeavesTheSizeOfThePixels() throws Exception {
        // Most writers leave the two fields at 0, which says nothing about
        // how large the image is, so it keeps one point for each of its
        // pixels. The header is built here because the files of the
        // repository both carry a resolution.
        byte[] bmp = java.nio.file.Files.readAllBytes(TestSupport.file("images/palette.bmp").toPath());
        java.util.Arrays.fill(bmp, 38, 46, (byte) 0);       // The two fields
        BMPImage image = new BMPImage(new java.io.ByteArrayInputStream(bmp));
        assertEquals(0f, image.getPhysicalWidth());
        assertEquals(0f, image.getPhysicalHeight());
    }
}
