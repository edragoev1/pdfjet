/*
 * BundledFontsTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.File;
import java.io.InputStream;
import java.lang.reflect.Field;
import java.lang.reflect.Modifier;
import java.util.List;
import java.util.Set;
import java.util.TreeSet;
import org.junit.jupiter.api.Test;

/**
 * The fonts PDFjet carries, in resources/fonts: each has a constant in
 * com.pdfjet.fonts, and each constant that is not deprecated names one of
 * them, which the library has.
 */
class BundledFontsTest {
    private static final String[] CLASSES = {
        "IBMPlexMath", "IBMPlexMono", "IBMPlexSans", "IBMPlexSansArabic", "IBMPlexSansHebrew",
        "IBMPlexSansJP", "IBMPlexSansKR", "IBMPlexSansSC", "IBMPlexSansTC", "IBMPlexSansThai",
        "IBMPlexSerif", "JetBrainsMono", "CascadiaMono", "SourceSerif4", "NotoSans",
    };

    @Test
    void everyFontCarriedHasAConstantAndEveryConstantNotDeprecatedIsCarried() throws Exception {
        Set<String> carried = new TreeSet<String>();
        File root = TestSupport.file("resources/fonts");
        for (File family : root.listFiles()) {
            if (!family.isDirectory()) {
                continue;   // The Go and Swift files that carry the fonts in those ports
            }
            for (File font : family.listFiles()) {
                if (font.getName().endsWith(".ttf") || font.getName().endsWith(".otf")) {
                    carried.add("fonts/" + family.getName() + "/" + font.getName());
                }
            }
        }
        assertEquals(72, carried.size());
        Set<String> constants = new TreeSet<String>();
        for (String name : CLASSES) {
            Class<?> c = Class.forName("com.pdfjet.fonts." + name);
            for (Field field : c.getFields()) {
                if (!Modifier.isStatic(field.getModifiers()) || field.getType() != String.class) {
                    continue;
                }
                String path = (String) field.get(null);
                boolean deprecated = field.isAnnotationPresent(Deprecated.class)
                        || c.isAnnotationPresent(Deprecated.class);
                if (deprecated) {
                    assertTrue(!carried.contains(path), name + "." + field.getName() + " is carried");
                } else {
                    assertTrue(carried.contains(path), name + "." + field.getName() + " is not carried");
                    // In the library, as the jar has it, not only in the folder
                    try (InputStream stream = BundledFonts.open(path)) {
                        assertNotNull(stream, path);
                    }
                    constants.add(path);
                }
            }
        }
        assertEquals(carried, constants);
    }

    @Test
    void aFontAddedToAnExistingPDFIsReadFromTheLibraryByItsPath() throws Exception {
        // The objects of a PDF of one page, and the constant of a font PDFjet
        // carries, with no fonts folder: the font is read from the library,
        // as Font(pdf, path) reads it
        java.io.ByteArrayOutputStream bos = new java.io.ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        new Page(pdf, Letter.PORTRAIT);
        pdf.complete();
        List<PDFobj> objects = TestSupport.read(bos.toByteArray());
        Font font = new Font(objects, com.pdfjet.fonts.IBMPlexSans.Bold);
        assertEquals("IBMPlexSans-Bold", font.name);
        // A font it does not carry says so
        java.io.FileNotFoundException e = assertThrows(java.io.FileNotFoundException.class,
                () -> new Font(objects, "fonts/NotoSans/NotoSans-Regular.ttf"));
        assertTrue(e.getMessage().contains("nor one of the fonts PDFjet carries"), e.getMessage());
    }

    @Test
    void cssWeightsMatchFacesThatLookAlikeAcrossTheFamilies() {
        // Cascadia Mono SemiLight is as thick as IBM Plex Sans Regular: it
        // stands for 400, and Regular for 500
        String[][] cases = {
            {"0", "400", "IBMPlexSans/IBMPlexSans-Regular.ttf"},
            {"0", "200", "IBMPlexSans/IBMPlexSans-Thin.ttf"},
            {"0", "800", "IBMPlexSans/IBMPlexSans-Bold.ttf"},
            {"1", "100", "IBMPlexSerif/IBMPlexSerif-Thin.ttf"},
            {"1", "900", "IBMPlexSerif/IBMPlexSerif-Bold.ttf"},
            {"2", "400", "CascadiaMono/CascadiaMono-SemiLight.ttf"},
            {"2", "500", "CascadiaMono/CascadiaMono-Regular.ttf"},
            {"2", "600", "CascadiaMono/CascadiaMono-SemiBold.ttf"},
            {"2", "700", "CascadiaMono/CascadiaMono-Bold.ttf"},
        };
        for (String[] c : cases) {
            assertEquals(c[2], BundledFonts.fileName(Integer.parseInt(c[0]), Integer.parseInt(c[1]), false),
                    c[0] + " " + c[1]);
        }
        assertEquals("CascadiaMono/CascadiaMono-SemiLightItalic.ttf", BundledFonts.fileName(2, 400, true));
        assertEquals("CascadiaMono/CascadiaMono-Italic.ttf", BundledFonts.fileName(2, 500, true));
        assertEquals("IBMPlexSans/IBMPlexSans-Italic.ttf", BundledFonts.fileName(0, 400, true));
    }
}   // End of BundledFontsTest.java
