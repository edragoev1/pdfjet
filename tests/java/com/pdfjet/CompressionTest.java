/*
 * CompressionTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.*;

import com.pdfjet.encryption.Passwords;
import com.pdfjet.encryption.Permissions;
import java.io.ByteArrayOutputStream;
import java.util.List;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Test;

/**
 * The content of the pages is compressed unless setCompression(false) is
 * called, and then written as it is, encrypted or not.
 */
class CompressionTest {
    @AfterEach
    void forbidCoreFonts() {
        TestSupport.allowCoreFonts(false);
    }

    // A PDF of one page that draws the text, compressed or not, encrypted or
    // not.
    private static byte[] pdf(boolean compression, boolean encrypted) throws Exception {
        TestSupport.allowCoreFonts(true);
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        if (encrypted) {
            pdf.setEncryption(new Encryption(pdf, new Passwords(), new Permissions()));
        }
        pdf.setCompression(compression);
        // Forty lines, as a stream too short to gain is stored as it is
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font font = TestSupport.helvetica(pdf);
        for (int i = 0; i < 40; i++) {
            new TextLine(font, "Readable").setLocation(50f, 50f + 15f * i).drawOn(page);
        }
        pdf.complete();
        return bos.toByteArray();
    }

    // The content of the page, read back from the PDF.
    private static String content(byte[] bytes) throws Exception {
        List<PDFobj> objects = TestSupport.read(bytes, "");
        PDFobj page = new PDF().getPageObjects(objects).get(0);
        return TestSupport.latin1(page.getContentObject(objects).getData());
    }

    @Test
    void theContentOfThePagesIsCompressedUnlessSetOtherwise() throws Exception {
        String compressed = TestSupport.latin1(pdf(true, false));
        assertTrue(compressed.contains("/Filter /FlateDecode"));
        assertFalse(compressed.contains(TestSupport.hex("Readable")));

        String plain = TestSupport.latin1(pdf(false, false));
        assertFalse(plain.contains("/Filter /FlateDecode"), "the fonts are core fonts, so nothing else is compressed");
        assertTrue(plain.contains(TestSupport.hex("Readable")), "the text is in the file as it is drawn");
        assertTrue(plain.contains("\nBT\n"), plain);
    }

    @Test
    void uncompressedContentIsReadBackTheSameEncryptedOrNot() throws Exception {
        // An encrypted document numbers its font after the encryption, so each
        // is compared with the one compressed the other way.
        String plain = content(pdf(true, false));
        assertTrue(plain.contains(TestSupport.hex("Readable")));
        assertEquals(plain, content(pdf(false, false)));
        String encrypted = content(pdf(true, true));
        assertTrue(encrypted.contains(TestSupport.hex("Readable")));
        assertEquals(encrypted, content(pdf(false, true)));
    }
}
