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
        List<File> families = new java.util.ArrayList<File>();
        for (File family : root.listFiles()) {
            if (family.isDirectory() && !family.getName().equals("world")) {
                families.add(family);   // Not the Go and Swift files beside them
            }
        }
        for (File family : new File(root, "world").listFiles()) {
            if (family.isDirectory()) {
                families.add(family);   // The fonts of the other scripts, in resources/fonts/world
            }
        }
        for (File family : families) {
            for (File font : family.listFiles()) {
                if (font.getName().endsWith(".ttf") || font.getName().endsWith(".otf")) {
                    carried.add("fonts/" + family.getName() + "/" + font.getName());
                }
            }
        }
        assertEquals(71, carried.size());
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
    void aWorldFontNotFoundNamesThePackageAndTheLinesToAdd() throws Exception {
        // No folder resources/fonts and a classpath without the jar
        // com.pdfjet:pdfjet-fonts-world: the font is not found, and the
        // error says what to add, in Maven's and in Gradle's words
        try (java.net.URLClassLoader loader = new java.net.URLClassLoader(new java.net.URL[0], null)) {
            assertEquals(null, BundledFonts.open(com.pdfjet.fonts.IBMPlexSansJP.Regular, "no-such-folder/", loader));
            assertEquals(null, BundledFonts.open(com.pdfjet.fonts.IBMPlexSansArabic.Regular, "no-such-folder/", loader));
        }
        assertEquals("fonts/IBMPlexSansArabic/IBMPlexSansArabic-Regular.ttf is not a file, nor in resources/fonts/world. "
                + "IBM Plex Sans Arabic is in the package com.pdfjet:pdfjet-fonts-world, with PDFjet's fonts for "
                + "Chinese, Japanese, Korean, Arabic, Hebrew and Thai. Add it next to com.pdfjet:pdfjet: "
                + "<dependency><groupId>com.pdfjet</groupId><artifactId>pdfjet-fonts-world</artifactId>"
                + "<version>" + PDF.VERSION + "</version></dependency> "
                + "(Gradle: implementation(\"com.pdfjet:pdfjet-fonts-world:" + PDF.VERSION + "\"))",
                BundledFonts.worldMissing(com.pdfjet.fonts.IBMPlexSansArabic.Regular));
        assertTrue(PDF.VERSION.matches("[0-9]+\\.[0-9]+\\.[0-9]+"), PDF.VERSION);
        assertTrue(BundledFonts.worldMissing(com.pdfjet.fonts.IBMPlexSansJP.Regular)
                .startsWith("fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf is not a file, nor in resources/fonts/world. "
                + "IBM Plex Sans JP is in the package com.pdfjet:pdfjet-fonts-world"));
        assertTrue(BundledFonts.worldMissing(com.pdfjet.fonts.IBMPlexSansTC.Bold).contains("IBM Plex Sans TC is in"));
        assertTrue(BundledFonts.worldMissing(com.pdfjet.fonts.IBMPlexSansThai.Thin).contains("IBM Plex Sans Thai is in"));
        assertTrue(BundledFonts.worldMissing(com.pdfjet.fonts.IBMPlexSansHebrew.Text).contains("IBM Plex Sans Hebrew is in"));
        // Not for the European fonts, nor for a face PDFjet does not carry,
        // nor for IBM Plex Math, which it no longer carries
        assertEquals(null, BundledFonts.worldMissing(com.pdfjet.fonts.IBMPlexSans.Regular));
        assertEquals(null, BundledFonts.worldMissing("fonts/IBMPlexSansJP/IBMPlexSansJP-Thin.ttf"));
        assertEquals(null, BundledFonts.worldMissing("fonts/IBMPlexSansArabic/IBMPlexSansArabic-ExtraLight.ttf"));
        assertEquals(null, BundledFonts.worldMissing("fonts/IBMPlexMath/IBMPlexMath-Regular.ttf"));
    }

    @Test
    void aWorldFontIsFoundInTheJarOfTheWorldFontsOnTheClasspath() throws Exception {
        // A jar laid out as com.pdfjet:pdfjet-fonts-world is (package-maven.sh),
        // with no folder resources/fonts: the font is read from it
        File jar = File.createTempFile("pdfjet-fonts-world", ".jar");
        jar.deleteOnExit();
        String[] paths = {com.pdfjet.fonts.IBMPlexSansJP.Regular, com.pdfjet.fonts.IBMPlexSansKR.Bold,
                com.pdfjet.fonts.IBMPlexSansArabic.Regular};
        try (java.util.zip.ZipOutputStream zip = new java.util.zip.ZipOutputStream(new java.io.FileOutputStream(jar))) {
            for (String path : paths) {
                String name = BundledFonts.nameOf(path);
                zip.putNextEntry(new java.util.zip.ZipEntry("com/pdfjet/fonts/world/" + name));
                zip.write(java.nio.file.Files.readAllBytes(TestSupport.file("resources/fonts/world/" + name).toPath()));
                zip.closeEntry();
            }
        }
        try (java.net.URLClassLoader loader = new java.net.URLClassLoader(new java.net.URL[] {jar.toURI().toURL()}, null)) {
            for (String path : paths) {
                try (InputStream stream = BundledFonts.open(path, "no-such-folder/", loader)) {
                    assertNotNull(stream, path);
                    Font font = new Font(new PDF(new java.io.ByteArrayOutputStream()), stream);
                    String family = BundledFonts.nameOf(path);
                    assertTrue(font.name.startsWith(family.substring(0, family.indexOf('/'))), font.name);
                }
            }
        }
        // And from the folder resources/fonts/world of the repository, the
        // working directory of the tests, before the classpath
        try (InputStream stream = BundledFonts.open(com.pdfjet.fonts.IBMPlexSansThai.SemiBold)) {
            assertTrue(stream instanceof java.io.FileInputStream);
        }
    }

    @Test
    void aPathOfTheOldStreamFilesOfAWorldFontNamesThePackage() {
        assertTrue(BundledFonts.worldMissing("fonts/IBMPlexSansKR/IBMPlexSansKR-Regular.otf.stream")
                .contains("IBM Plex Sans KR is in"));
        assertTrue(BundledFonts.worldMissing("fonts/IBMPlexSansThai/IBMPlexSansThai-Bold.ttf.stream")
                .contains("IBM Plex Sans Thai is in"));
        assertTrue(BundledFonts.worldMissing("fonts/IBMPlexSansJP/IBMPlexSansJP-Bold.ttf.stream")
                .contains("IBM Plex Sans JP is in"));
        assertEquals(null, BundledFonts.worldMissing("fonts/IBMPlexSansJP/IBMPlexSansJP-Thin.otf.stream"));
    }

    // PDFjet carries IBM Plex Sans and Serif as .otf from 9.1.9, and as .ttf
    // before: a path written for the .ttf is the .otf of the same face, as is
    // one of the .stream files of before 9.0.5, from the folder resources/fonts
    // or from the jar of Maven Central; a .ttf of one's own at that path, or of
    // that name in the folder, is read first.
    @Test
    void aPathToTheTTFOfIBMPlexSansOrSerifIsTheOTFPDFjetCarries(@org.junit.jupiter.api.io.TempDir File folder)
            throws Exception {
        String[] styles = {"Thin", "ThinItalic", "Light", "LightItalic", "Text", "TextItalic", "Regular", "Italic",
                "Medium", "MediumItalic", "SemiBold", "SemiBoldItalic", "Bold", "BoldItalic"};
        File jar = new File(folder, "pdfjet.jar");
        try (java.util.zip.ZipOutputStream zip = new java.util.zip.ZipOutputStream(new java.io.FileOutputStream(jar))) {
            for (String family : new String[] {"IBMPlexSans", "IBMPlexSerif"}) {
                for (String style : styles) {
                    String name = family + "/" + family + "-" + style + ".otf";
                    zip.putNextEntry(new java.util.zip.ZipEntry("com/pdfjet/fonts/" + name));
                    zip.write(java.nio.file.Files.readAllBytes(TestSupport.file("resources/fonts/" + name).toPath()));
                    zip.closeEntry();
                }
            }
        }
        try (java.net.URLClassLoader loader = new java.net.URLClassLoader(new java.net.URL[] {jar.toURI().toURL()}, null)) {
            for (String family : new String[] {"IBMPlexSans", "IBMPlexSerif"}) {
                for (String style : styles) {
                    byte[] otf = java.nio.file.Files.readAllBytes(
                            TestSupport.file("resources/fonts/" + family + "/" + family + "-" + style + ".otf").toPath());
                    String path = "fonts/" + family + "/" + family + "-" + style + ".ttf";
                    try (InputStream stream = Font.open(Font.fontFileOf(path))) {
                        assertTrue(java.util.Arrays.equals(otf, TestSupport.readAll(stream)), path);
                    }
                    try (InputStream stream = Font.open(Font.fontFileOf(path + ".stream"))) {
                        assertTrue(java.util.Arrays.equals(otf, TestSupport.readAll(stream)), path + ".stream");
                    }
                    try (InputStream stream = BundledFonts.open(path, "no-such-folder/", loader)) {
                        assertTrue(java.util.Arrays.equals(otf, TestSupport.readAll(stream)), path + " in the jar");
                    }
                }
            }
        }
        assertEquals("IBMPlexSans-Bold", new Font(TestSupport.newPDF(), com.pdfjet.fonts.IBMPlexSans.Bold
                .replace(".otf", ".ttf")).getName());
        // A .ttf of one's own at that path, and of that name in the folder
        byte[] ttf = TestSupport.fontBytes("tests/fonts/IBMPlexSans-Regular.ttf");
        File own = new File(folder, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf");
        File inFolder = new File(folder, "resources/fonts/IBMPlexSerif/IBMPlexSerif-Regular.ttf");
        for (File file : new File[] {own, inFolder}) {
            assertTrue(file.getParentFile().mkdirs());
            java.nio.file.Files.write(file.toPath(), ttf);
        }
        try (InputStream stream = Font.open(own.getPath())) {
            assertTrue(java.util.Arrays.equals(ttf, TestSupport.readAll(stream)));
        }
        try (InputStream stream = BundledFonts.open("fonts/IBMPlexSerif/IBMPlexSerif-Regular.ttf",
                new File(folder, "resources/fonts").getPath() + "/", null)) {
            assertTrue(java.util.Arrays.equals(ttf, TestSupport.readAll(stream)));
        }
        // Only those two families, and only their .ttf: a face PDFjet does not
        // carry is not found still, nor a .otf of Cascadia Mono
        for (String path : new String[] {"fonts/IBMPlexSans/IBMPlexSans-ExtraLight.ttf",
                "fonts/CascadiaMono/CascadiaMono-Regular.otf", "fonts/IBMPlexMono/IBMPlexMono-Regular.ttf"}) {
            assertEquals(null, BundledFonts.open(path), path);
        }
        assertEquals(null, BundledFonts.renamed("CascadiaMono/CascadiaMono-Regular.ttf"));
        assertEquals("IBMPlexSerif/IBMPlexSerif-Bold.otf", BundledFonts.renamed("IBMPlexSerif/IBMPlexSerif-Bold.ttf"));
        // And back: the .otf of 9.1.9 is the .ttf of a resources/fonts folder of before
        assertEquals("IBMPlexSans/IBMPlexSans-Regular.ttf", BundledFonts.renamed("IBMPlexSans/IBMPlexSans-Regular.otf"));
        assertEquals(null, BundledFonts.renamed("CascadiaMono/CascadiaMono-Regular.otf"));
    }

    // PDFjet in a server's shared lib, the world jar in the WEB-INF/lib of the
    // application: the jar is on the classpath of the context class loader alone.
    @Test
    void aWorldFontIsFoundThroughTheContextClassLoader(@org.junit.jupiter.api.io.TempDir File folder) throws Exception {
        String path = com.pdfjet.fonts.IBMPlexSansThai.Regular;
        String name = BundledFonts.nameOf(path);
        File jar = new File(folder, "pdfjet-fonts-world.jar");
        try (java.util.zip.ZipOutputStream zip = new java.util.zip.ZipOutputStream(new java.io.FileOutputStream(jar))) {
            zip.putNextEntry(new java.util.zip.ZipEntry("com/pdfjet/fonts/world/" + name));
            zip.write(java.nio.file.Files.readAllBytes(TestSupport.file("resources/fonts/world/" + name).toPath()));
            zip.closeEntry();
        }
        ClassLoader saved = Thread.currentThread().getContextClassLoader();
        try (java.net.URLClassLoader shared = new java.net.URLClassLoader(new java.net.URL[0], null);
                java.net.URLClassLoader application = new java.net.URLClassLoader(new java.net.URL[] {jar.toURI().toURL()}, null)) {
            assertEquals(null, BundledFonts.open(path, "no-such-folder/", shared));
            Thread.currentThread().setContextClassLoader(application);
            try (InputStream stream = BundledFonts.open(path, "no-such-folder/", shared)) {
                assertNotNull(stream);
            }
        } finally {
            Thread.currentThread().setContextClassLoader(saved);
        }
    }

    @Test
    void cssWeightsMatchFacesThatLookAlikeAcrossTheFamilies() {
        // Cascadia Mono SemiLight is as thick as IBM Plex Sans Regular: it
        // stands for 400, and Regular for 500
        String[][] cases = {
            {"0", "400", "IBMPlexSans/IBMPlexSans-Regular.otf"},
            {"0", "200", "IBMPlexSans/IBMPlexSans-Thin.otf"},
            {"0", "800", "IBMPlexSans/IBMPlexSans-Bold.otf"},
            {"1", "100", "IBMPlexSerif/IBMPlexSerif-Thin.otf"},
            {"1", "900", "IBMPlexSerif/IBMPlexSerif-Bold.otf"},
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
        assertEquals("IBMPlexSans/IBMPlexSans-Italic.otf", BundledFonts.fileName(0, 400, true));
    }
}   // End of BundledFontsTest.java
