/*
 * JPGOrientationTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.nio.file.Files;
import org.junit.jupiter.api.Test;

/**
 * A JPEG with an Exif orientation is drawn as it is meant to be seen:
 * tests/data/jpeg/orientation-N.jpg is stored turned so that it is seen as
 * 32 by 16 pixels, as orientation-1.jpg is; 6 and 8 are 16 by 32 as stored.
 */
class JPGOrientationTest {
    private static byte[] orientationJPEG(String orientation) throws Exception {
        return Files.readAllBytes(TestSupport.file(
                "tests/data/jpeg/orientation-" + orientation + ".jpg").toPath());
    }

    // The content stream of a page the JPEG is drawn on, then the PDF.
    private static String[] drawn(byte[] jpeg, Image[] image) throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Page page = new Page(pdf, Letter.PORTRAIT);
        image[0] = new Image(pdf, new ByteArrayInputStream(jpeg));
        image[0].setLocation(10f, 20f);
        image[0].drawOn(page);
        String content = TestSupport.content(page);
        pdf.complete();
        return new String[] {content, TestSupport.latin1(bos.toByteArray())};
    }

    @Test
    void theSizeAndTheMatrixAreAsSeen() throws Exception {
        Object[][] tests = {
            {"1", 32, 16, ""},
            {"3", 32, 16, "-1 0 0 -1 1 1 cm\n"},
            {"6", 16, 32, "0 -1 1 0 0 1 cm\n"},
            {"8", 16, 32, "0 1 -1 0 1 0 cm\n"},
        };
        for (Object[] test : tests) {
            String orientation = (String) test[0];
            Image[] image = new Image[1];
            String[] drawn = drawn(orientationJPEG(orientation), image);
            assertEquals(32f, image[0].getWidth(), "orientation " + orientation);
            assertEquals(16f, image[0].getHeight(), "orientation " + orientation);
            // The image object keeps the pixels as stored
            assertTrue(drawn[1].contains("/Width " + test[1] + "\n"), "orientation " + orientation);
            assertTrue(drawn[1].contains("/Height " + test[2] + "\n"), "orientation " + orientation);
            String box = "32 0 0 16 10 756 cm\n" + test[3] + "/Im";
            assertTrue(drawn[0].contains(box), "orientation " + orientation + ": " + drawn[0]);
        }
    }

    // Orientation 1, upright as stored, draws the page as a JPEG without Exif.
    @Test
    void orientationOneIsDrawnAsWithoutExif() throws Exception {
        byte[] data = orientationJPEG("1");
        String withExif = drawn(data, new Image[1])[0];
        String withoutExif = drawn(withoutSegment(data, 0xE1), new Image[1])[0];
        assertEquals(withoutExif, withExif);
    }

    // Returns the JPEG without its first segment of the marker.
    private static byte[] withoutSegment(byte[] jpeg, int marker) {
        for (int i = 2; i + 3 < jpeg.length; i++) {
            if ((jpeg[i] & 0xFF) == 0xFF && (jpeg[i + 1] & 0xFF) == marker) {
                int end = i + 2 + (((jpeg[i + 2] & 0xFF) << 8) | (jpeg[i + 3] & 0xFF));
                byte[] stripped = new byte[jpeg.length - (end - i)];
                System.arraycopy(jpeg, 0, stripped, 0, i);
                System.arraycopy(jpeg, end, stripped, i, jpeg.length - end);
                return stripped;
            }
        }
        return jpeg;
    }

    // An Exif segment, without its marker and length, with one entry in IFD0,
    // in either byte order.
    private static byte[] exif(boolean bigEndian, int tag, int kind, int count, int value) {
        ByteArrayOutputStream segment = new ByteArrayOutputStream();
        segment.write('E');
        segment.write('x');
        segment.write('i');
        segment.write('f');
        segment.write(0);
        segment.write(0);
        segment.write(bigEndian ? 'M' : 'I');
        segment.write(bigEndian ? 'M' : 'I');
        u16(segment, bigEndian, 42);
        u32(segment, bigEndian, 8);
        u16(segment, bigEndian, 1);
        u16(segment, bigEndian, tag);
        u16(segment, bigEndian, kind);
        u32(segment, bigEndian, count);
        u16(segment, bigEndian, value);
        u16(segment, bigEndian, 0);
        u32(segment, bigEndian, 0);
        return segment.toByteArray();
    }

    private static void u16(ByteArrayOutputStream out, boolean bigEndian, int v) {
        if (bigEndian) {
            out.write(v >> 8);
            out.write(v);
        } else {
            out.write(v);
            out.write(v >> 8);
        }
    }

    private static void u32(ByteArrayOutputStream out, boolean bigEndian, int v) {
        if (bigEndian) {
            u16(out, true, v >>> 16);
            u16(out, true, v & 0xFFFF);
        } else {
            u16(out, false, v & 0xFFFF);
            u16(out, false, v >>> 16);
        }
    }

    @Test
    void theOrientationIsReadInBothByteOrders() {
        for (boolean bigEndian : new boolean[] {false, true}) {
            for (int value = 1; value <= 8; value++) {
                assertEquals(value, JPGImage.exifOrientation(exif(bigEndian, 0x0112, 3, 1, value)),
                        "big endian " + bigEndian);
            }
        }
    }

    // A malformed Exif segment is passed over, never read out of its bounds.
    @Test
    void theOrientationOfAMalformedExifIsPassedOver() throws Exception {
        byte[] notExif = exif(false, 0x0112, 3, 1, 6);
        notExif[3] = 'v';
        byte[] otherOrder = exif(false, 0x0112, 3, 1, 6);
        otherOrder[7] = 'M';
        byte[] before8 = exif(false, 0x0112, 3, 1, 6);
        before8[10] = 4;
        byte[] pastTheEnd = exif(false, 0x0112, 3, 1, 6);
        pastTheEnd[10] = (byte) 0xFF;
        pastTheEnd[11] = (byte) 0xFF;
        pastTheEnd[12] = (byte) 0xFF;
        pastTheEnd[13] = (byte) 0xFF;
        byte[][] segments = {
            exif(false, 0x0112, 3, 1, 0),
            exif(true, 0x0112, 3, 1, 9),
            exif(false, 0x0112, 4, 1, 6),
            exif(true, 0x0112, 3, 2, 6),
            exif(false, 0x0110, 3, 1, 6),
            notExif, otherOrder, before8, pastTheEnd,
        };
        for (int i = 0; i < segments.length; i++) {
            assertEquals(0, JPGImage.exifOrientation(segments[i]), "segment " + i);
        }
        byte[] whole = exif(true, 0x0112, 3, 1, 6);
        for (int end = 0; end < whole.length - 4; end++) {
            assertEquals(0, JPGImage.exifOrientation(java.util.Arrays.copyOf(whole, end)), "cut at " + end);
        }
        // The JPEG with a malformed Exif is drawn as stored
        byte[] broken = orientationJPEG("6");
        for (int i = 0; i + 3 < broken.length; i++) {
            if (broken[i] == 'M' && broken[i + 1] == 'M' && broken[i + 2] == 0 && broken[i + 3] == 0x2A) {
                broken[i + 3] = 0x2B;
                break;
            }
        }
        Image[] image = new Image[1];
        drawn(broken, image);
        assertEquals(16f, image[0].getWidth());
        assertEquals(32f, image[0].getHeight());
    }
}
