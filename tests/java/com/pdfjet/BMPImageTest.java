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

    @Test
    void aTruncatedFileThrows() {
        byte[] truncated = Arrays.copyOf(bmp24(false), 60);
        assertThrows(Exception.class, () -> new BMPImage(new ByteArrayInputStream(truncated)));
    }
}
