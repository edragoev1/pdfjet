/*
 * JPGImageTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.junit.jupiter.api.Assumptions.assumeTrue;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
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
}
