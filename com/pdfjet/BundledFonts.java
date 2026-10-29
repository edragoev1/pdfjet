/*
 * BundledFonts.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.InputStream;

/**
 * The fonts PDFjet carries in itself, in resources/fonts, each family in a
 * folder of its name, as in the folder fonts of pdfjet-fonts: IBM Plex Sans,
 * Serif and Mono, Source Serif 4 and JetBrains Mono, from Thin or Light to
 * Bold, upright and italic; IBM Plex Sans Arabic, Hebrew and Thai, Thin to
 * Bold; IBM Plex Math; and IBM Plex Sans JP, KR, SC and TC, Regular,
 * SemiBold and Bold, as .otf. All are under the SIL Open Font License 1.1.
 * A path to one of them, such as fonts/IBMPlexSans/IBMPlexSans-Regular.ttf,
 * the constants of com.pdfjet.fonts, is read from the library when there is
 * no such file, and the text of an SVG image is drawn in them unless a font
 * is set.
 */
final class BundledFonts {
    private BundledFonts() {
    }

    // Where the fonts are among the resources of the jar.
    private static final String RESOURCES = "/com/pdfjet/fonts/";

    // The repository of the fonts PDFjet came with before 9.1.7, which has
    // those it does not carry, such as Noto Sans.
    static final String PDFJET_FONTS = "https://github.com/edragoev1/pdfjet-fonts";

    // Returns the name of a font under resources/fonts, Family/File, of a
    // path to it, such as fonts/IBMPlexSans/IBMPlexSans-Regular.ttf: its
    // last two parts.
    static String nameOf(String fontPath) {
        String path = fontPath.replace('\\', '/');
        int file = path.lastIndexOf('/');
        if (file <= 0) {
            return null;
        }
        int family = path.lastIndexOf('/', file - 1);
        return path.substring(family + 1);
    }

    // Opens the font of the path that PDFjet carries, or returns null.
    static InputStream open(String fontPath) {
        String name = nameOf(fontPath);
        return (name == null || name.indexOf("..") >= 0) ? null : BundledFonts.class.getResourceAsStream(RESOURCES + name);
    }

    static final int SANS = 0;
    static final int SERIF = 1;
    static final int MONO = 2;
    private static final String[] FAMILIES = {"IBMPlexSans", "SourceSerif4", "JetBrainsMono"};
    // The weights of the faces of each family, by the names of their files.
    private static final int[][] WEIGHTS = {
        {100, 300, 400, 450, 500, 600, 700},
        {300, 400, 500, 600, 700},
        {300, 400, 500, 600, 700},
    };

    // Returns the name of a face of an SVG family, Family/File: the weight
    // CSS matches to the one asked for among those the family has, and
    // italic or upright.
    static String fileName(int family, int weight, boolean italic) {
        int w = match(WEIGHTS[family], weight);
        String style = (w == 100) ? "Thin" : (w == 300) ? "Light" : (w == 450) ? "Text"
                : (w == 500) ? "Medium" : (w == 600) ? "SemiBold" : (w == 700) ? "Bold" : "";
        if (italic) {
            style += "Italic";
        }
        return FAMILIES[family] + "/" + FAMILIES[family] + "-" + (style.isEmpty() ? "Regular" : style) + ".ttf";
    }

    // Returns the weight CSS matches to the one asked for, of the weights a
    // family has in order (CSS Fonts 4, 5.2): that weight; from 400 to 500,
    // the weights up to 500, then the lighter ones, then the heavier ones;
    // below 400, the lighter ones, then the heavier ones; above 500, the
    // heavier ones, then the lighter ones; each nearest first.
    static int match(int[] weights, int weight) {
        int lighter = -1;     // The nearest lighter weight
        int heavier = -1;     // The nearest heavier weight
        int upTo500 = -1;     // The nearest heavier weight up to 500
        for (int w : weights) {
            if (w == weight) {
                return w;
            } else if (w < weight) {
                lighter = w;
            } else if (heavier < 0) {
                heavier = w;
            }
            if (w > weight && w <= 500 && upTo500 < 0) {
                upTo500 = w;
            }
        }
        if (weight >= 400 && weight <= 500 && upTo500 >= 0) {
            return upTo500;
        }
        if (weight <= 500) {
            return (lighter >= 0) ? lighter : heavier;
        }
        return (heavier >= 0) ? heavier : lighter;
    }

    // Returns the face for the document, read once for it, or null when the
    // library was built without it.
    static Font get(PDF pdf, int family, int weight, boolean italic) {
        String name = fileName(family, weight, italic);
        Font font = pdf.bundledFonts.get(name);
        if (font != null) {
            return font;
        }
        try (InputStream stream = BundledFonts.class.getResourceAsStream(RESOURCES + name)) {
            if (stream == null) {
                return null;
            }
            font = new Font(pdf, stream);
        } catch (Exception e) {
            throw new IllegalStateException("Reading the font " + name + ": " + e.getMessage(), e);
        }
        pdf.bundledFonts.put(name, font);
        return font;
    }

    // Returns the family of a font-family list of an SVG file: the first of
    // its names that PDFjet knows, as serif, monospace or sans-serif, and
    // sans-serif for none.
    static int familyOf(String fontFamily) {
        for (String name : fontFamily.split(",", -1)) {
            name = SVGState.toLower(SVGState.trim(name));
            if (name.length() >= 2 && (name.charAt(0) == '"' || name.charAt(0) == '\'')
                    && name.charAt(name.length() - 1) == name.charAt(0)) {
                name = SVGState.trim(name.substring(1, name.length() - 1));
            }
            for (int i = 0; i < NAMES.length; i++) {
                for (String known : NAMES[i]) {
                    if (name.equals(known)) {
                        return i;
                    }
                }
            }
        }
        return SANS;
    }

    // The names of the fonts of each family, in lower case: the generic one,
    // the core fonts and those that systems and charts name most.
    private static final String[][] NAMES = {
        {"sans-serif", "helvetica", "arial", "system-ui", "-apple-system", "blinkmacsystemfont",
                "segoe ui", "roboto", "verdana", "tahoma", "trebuchet ms", "open sans", "noto sans",
                "liberation sans", "dejavu sans", "ibm plex sans", "inter"},
        {"serif", "times", "times new roman", "georgia", "garamond", "cambria", "palatino",
                "book antiqua", "liberation serif", "dejavu serif", "noto serif", "source serif 4",
                "source serif pro"},
        {"monospace", "courier", "courier new", "consolas", "menlo", "monaco", "ui-monospace",
                "sfmono-regular", "lucida console", "liberation mono", "dejavu sans mono",
                "jetbrains mono", "ibm plex mono", "source code pro"},
    };
}   // End of BundledFonts.java
