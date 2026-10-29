/*
 * BundledFonts.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.InputStream;

/**
 * The fonts PDFjet carries in itself, those of PDFjet Forms: IBM Plex Sans,
 * Source Serif 4 and JetBrains Mono, each regular, semibold and bold, upright
 * and italic, under the SIL Open Font License 1.1. An SVG image draws its
 * text in them unless a font is set: embedded, as PDF/A and PDF/UA ask, where
 * the core fonts are not.
 */
final class BundledFonts {
    private BundledFonts() {
    }

    static final int SANS = 0;
    static final int SERIF = 1;
    static final int MONO = 2;
    private static final String[] FAMILIES = {"IBMPlexSans", "SourceSerif4", "JetBrainsMono"};

    // The name of the file of a face: a weight below 600 is regular, from 600
    // to 699 semibold, and 700 or more bold.
    static String fileName(int family, int weight, boolean italic) {
        String style = (weight >= 700) ? "Bold" : (weight >= 600) ? "SemiBold" : "";
        if (italic) {
            style += "Italic";
        }
        return FAMILIES[family] + "-" + (style.isEmpty() ? "Regular" : style) + ".ttf";
    }

    // Returns the face for the document, read once for it, or null when the
    // library was built without the fonts.
    static Font get(PDF pdf, int family, int weight, boolean italic) {
        String name = fileName(family, weight, italic);
        Font font = pdf.bundledFonts.get(name);
        if (font != null) {
            return font;
        }
        try (InputStream stream = BundledFonts.class.getResourceAsStream("/com/pdfjet/bundledfonts/" + name)) {
            if (stream == null) {
                return null;
            }
            font = new Font(pdf, stream);
        } catch (Exception e) {
            throw new IllegalStateException("Reading the bundled font " + name + ": " + e.getMessage(), e);
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
