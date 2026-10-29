/*
 * BundledFontsTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.File;
import java.io.InputStream;
import java.lang.reflect.Field;
import java.lang.reflect.Modifier;
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
            for (File font : family.listFiles()) {
                if (font.getName().endsWith(".ttf") || font.getName().endsWith(".otf")) {
                    carried.add("fonts/" + family.getName() + "/" + font.getName());
                }
            }
        }
        assertEquals(82, carried.size());
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
}   // End of BundledFontsTest.java
