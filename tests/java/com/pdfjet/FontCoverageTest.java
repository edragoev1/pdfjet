/*
 * FontCoverageTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;

import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.InputStream;
import org.junit.jupiter.api.Test;

/**
 * Every face of the fonts PDFjet carries for Latin text has every letter of
 * modern Greek and of the Cyrillic of Russian, Ukrainian, Belarusian,
 * Bulgarian, Serbian and Macedonian: a document in one of them never shows
 * an empty box (the owner, 29 October 2026, of ѝ, the Bulgarian "her", which
 * JetBrains Mono does not have).
 */
class FontCoverageTest {
    private static final String[] FAMILIES = {"IBMPlexSans", "IBMPlexSerif", "SourceSerif4", "CascadiaMono"};

    // The letters of modern Greek, U+0386 to U+03CE, and of Cyrillic, U+0400
    // to U+045F; what the code points between them are not, as U+0387, a
    // punctuation mark, is skipped by the test.
    private static String letters() {
        StringBuilder sb = new StringBuilder();
        for (int c = 0x0386; c <= 0x03CE; c++) {
            if (Character.isLetter(c)) {
                sb.appendCodePoint(c);
            }
        }
        for (int c = 0x0400; c <= 0x045F; c++) {
            if (Character.isLetter(c)) {
                sb.appendCodePoint(c);
            }
        }
        return sb.toString();
    }

    @Test
    void everyFaceForLatinTextHasGreekAndCyrillic() throws Exception {
        String letters = letters();
        assertEquals(69 + 96, letters.length());
        PDF pdf = new PDF(new ByteArrayOutputStream());
        int faces = 0;
        for (String family : FAMILIES) {
            for (File file : TestSupport.file("resources/fonts/" + family).listFiles()) {
                if (!file.getName().endsWith(".ttf")) {
                    continue;
                }
                Font font;
                try (InputStream stream = TestSupport.open("resources/fonts/" + family + "/" + file.getName())) {
                    font = new Font(pdf, stream);
                }
                StringBuilder missing = new StringBuilder();
                for (int i = 0; i < letters.length(); i++) {
                    if (font.unicodeToGID.get(letters.charAt(i)) == 0) {
                        missing.append(letters.charAt(i));
                    }
                }
                assertEquals("", missing.toString(), file.getName());
                faces++;
            }
        }
        assertEquals(14 + 14 + 10 + 10, faces);
    }
}   // End of FontCoverageTest.java
