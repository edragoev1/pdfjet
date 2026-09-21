/*
 * JPGImageTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.junit.jupiter.api.Assumptions.assumeTrue;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.nio.file.Files;
import org.junit.jupiter.api.Test;

/**
 * A CMYK JPEG that Adobe software wrote, as images/cmyk.jpg is, has an APP14
 * marker and stores its inks inverted, so the image is written with a Decode
 * array that inverts them back; one without the marker is written as it is.
 */
class JPGImageTest {
    private static final String CMYK = "images/cmyk.jpg";

    // Returns the JPEG without its APP14 segment.
    private static byte[] withoutAPP14(byte[] jpeg) {
        for (int i = 2; i + 3 < jpeg.length; i++) {
            if ((jpeg[i] & 0xFF) == 0xFF && (jpeg[i + 1] & 0xFF) == 0xEE) {
                int end = i + 2 + (((jpeg[i + 2] & 0xFF) << 8) | (jpeg[i + 3] & 0xFF));
                byte[] stripped = new byte[jpeg.length - (end - i)];
                System.arraycopy(jpeg, 0, stripped, 0, i);
                System.arraycopy(jpeg, end, stripped, i, jpeg.length - end);
                return stripped;
            }
        }
        return jpeg;
    }

    private static String pdfWith(byte[] jpeg) throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        new Image(pdf, new ByteArrayInputStream(jpeg)).drawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.complete();
        return TestSupport.latin1(bos.toByteArray());
    }

    @Test
    void theInksOfACmykJpegAreInvertedBackOnlyWhenAdobeSoftwareWroteIt() throws Exception {
        assumeTrue(TestSupport.file(CMYK).exists(), "the images directory is not here");
        byte[] jpeg = Files.readAllBytes(TestSupport.file(CMYK).toPath());
        assertTrue(new JPGImage(new ByteArrayInputStream(jpeg)).isAdobe());
        assertTrue(pdfWith(jpeg).contains("/Decode [1.0 0.0 1.0 0.0 1.0 0.0 1.0 0.0]"));

        byte[] plain = withoutAPP14(jpeg);
        assertFalse(new JPGImage(new ByteArrayInputStream(plain)).isAdobe());
        String raw = pdfWith(plain);
        assertTrue(raw.contains("/DeviceCMYK"), raw.substring(0, 200));
        assertFalse(raw.contains("/Decode ["));
    }

    // The frame header of the JPEG is what PDFjet reads it for, so a marker
    // before it that is read as one more segment hides it.

    // Returns a JPEG of 8 by 8 pixels: the SOI marker, the bytes given, and a
    // frame header of the number of color components.
    private static byte[] jpegOf(byte[] before, int components) throws Exception {
        ByteArrayOutputStream jpeg = new ByteArrayOutputStream();
        jpeg.write(new byte[] {(byte) 0xFF, (byte) 0xD8});
        jpeg.write(before);
        int length = 8 + 3*components;
        jpeg.write(new byte[] {(byte) 0xFF, (byte) 0xC0, (byte) (length >> 8), (byte) length,
                8, 0, 8, 0, 8, (byte) components});
        for (int i = 0; i < components; i++) {
            jpeg.write(new byte[] {(byte) (i + 1), 0x11, 0});
        }
        return jpeg.toByteArray();
    }

    // Returns an APP14 segment of the bytes.
    private static byte[] app14(int... payload) {
        byte[] segment = new byte[4 + payload.length];
        segment[0] = (byte) 0xFF;
        segment[1] = (byte) 0xEE;
        segment[2] = (byte) ((payload.length + 2) >> 8);
        segment[3] = (byte) (payload.length + 2);
        for (int i = 0; i < payload.length; i++) {
            segment[4 + i] = (byte) payload[i];
        }
        return segment;
    }

    // The segment Adobe software writes: "Adobe", the version, two flags and
    // the color transform.
    private static final byte[] ADOBE_APP14 =
            app14('A', 'd', 'o', 'b', 'e', 0, 100, 0, 0, 0, 0, 2);

    @Test
    void theMarkersWithoutAParameterSegmentAreNotReadAsSegments() throws Exception {
        byte[][] markers = {
                {(byte) 0xFF, 0x00, 0x30},      // A stuffed 0xFF byte
                {(byte) 0xFF, (byte) 0xD3},     // A restart marker
                {(byte) 0xFF, 0x01},            // A TEM marker
                {(byte) 0xFF, (byte) 0xD8},     // A nested SOI marker
                {(byte) 0xFF, (byte) 0xFF},     // A fill byte
        };
        for (byte[] before : markers) {
            JPGImage image = new JPGImage(new ByteArrayInputStream(jpegOf(before, 3)));
            assertTrue(image.getWidth() == 8 && image.getHeight() == 8 &&
                    image.getColorComponents() == 3, "the frame header is hidden");
        }
    }

    @Test
    void aJPEGThatEndsBeforeItsFrameHeaderFails() {
        assertThrows(IOException.class, () -> new JPGImage(new ByteArrayInputStream(
                new byte[] {(byte) 0xFF, (byte) 0xD8, (byte) 0xFF, (byte) 0xD9})));
    }

    @Test
    void aJPEGOfOtherThanEightBitsPerComponentFails() throws Exception {
        // A PDF image stream of DCTDecode data delivers eight bit samples, and
        // PDFjet writes 8 as the bits per component, so a 12-bit JPEG would be
        // drawn as noise rather than refused.
        for (int precision : new int[] {0, 12, 16}) {
            byte[] jpeg = jpegOf(new byte[0], 3);
            jpeg[6] = (byte) precision;     // After the SOI marker, the SOF0 marker and the length
            assertThrows(IOException.class,
                    () -> new JPGImage(new ByteArrayInputStream(jpeg)));
        }
        new JPGImage(new ByteArrayInputStream(jpegOf(new byte[0], 3)));
    }

    @Test
    void aFrameHeaderOfTheWrongLengthFails() throws Exception {
        // The frame header holds three bytes for each component after its
        // eight, which libjpeg checks; a reader a PDF is drawn with refuses an
        // image of another length, where MuPDF draws nothing and says "Bogus
        // marker length", so PDFjet does not embed one.
        for (int wrong : new int[] {17 - 3, 17 + 3}) {
            byte[] jpeg = jpegOf(new byte[0], 3);
            jpeg[5] = (byte) wrong;     // The low byte of the length of the frame header
            assertThrows(IOException.class,
                    () -> new JPGImage(new ByteArrayInputStream(jpeg)));
        }
        new JPGImage(new ByteArrayInputStream(jpegOf(new byte[0], 3)));
    }

    @Test
    void onlyTheWholeAdobeAPP14SegmentMarksTheImage() throws Exception {
        assertTrue(new JPGImage(new ByteArrayInputStream(
                jpegOf(ADOBE_APP14, 4))).isAdobe());

        // Adobe's segment is twelve bytes; a shorter one is another APP14.
        assertFalse(new JPGImage(new ByteArrayInputStream(
                jpegOf(app14('A', 'd', 'o', 'b', 'e'), 4))).isAdobe());
    }

    @Test
    void theAdobeAPP14SegmentIsFoundAfterTheFrameHeader() throws Exception {
        // libjpeg reads the markers of a header to the scan, so an APP14 segment
        // between the frame header and the scan marks the image too; one after
        // the scan, which no header reads, does not.
        ByteArrayOutputStream after = new ByteArrayOutputStream();
        after.write(jpegOf(new byte[0], 4));
        after.write(ADOBE_APP14);
        assertTrue(new JPGImage(new ByteArrayInputStream(after.toByteArray())).isAdobe());

        ByteArrayOutputStream afterTheScan = new ByteArrayOutputStream();
        afterTheScan.write(jpegOf(new byte[0], 4));
        afterTheScan.write(new byte[] {(byte) 0xFF, (byte) 0xDA, 0x00, 0x02});
        afterTheScan.write(ADOBE_APP14);
        assertFalse(new JPGImage(new ByteArrayInputStream(afterTheScan.toByteArray())).isAdobe());
    }

    @Test
    void theComponentsOfTheFrameHeaderAreNotReadAsMarkers() throws Exception {
        // The component specifications fill the rest of the frame header and can
        // hold any bytes, the 0xFF of a marker among them; here they are the
        // bytes of an Adobe APP14 segment, which is none.
        byte[] jpeg = jpegOf(new byte[0], 4);
        byte[] components = {(byte) 0xFF, (byte) 0xEE, 0x00, 0x0E,
                'A', 'd', 'o', 'b', 'e', 0x00, 0x64, 0x00};
        System.arraycopy(components, 0, jpeg, jpeg.length - 12, 12);
        ByteArrayOutputStream whole = new ByteArrayOutputStream();
        whole.write(jpeg);
        whole.write(new byte[] {0, 0, 0, 0, (byte) 0xFF, (byte) 0xD9});
        assertFalse(new JPGImage(new ByteArrayInputStream(whole.toByteArray())).isAdobe());
    }

    @Test
    void anotherAPP14SegmentDoesNotUnmarkAnAdobeImage() throws Exception {
        ByteArrayOutputStream before = new ByteArrayOutputStream();
        before.write(ADOBE_APP14);
        before.write(app14('N', 'o', 't', ' ', 'A', 'd', 'o', 'b', 'e', 0, 0, 0));
        assertTrue(new JPGImage(new ByteArrayInputStream(
                jpegOf(before.toByteArray(), 4))).isAdobe());
    }
}
