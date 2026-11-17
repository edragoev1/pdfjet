/*
 * IBMPlexSansDevanagari.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.fonts;

/**
 * The IBM Plex Sans Devanagari fonts. Each constant is the path of a font file,
 * embedded as a subset of the glyphs a document draws; pass it to the Font
 * constructor that takes a font path. These fonts are not in the jar
 * com.pdfjet:pdfjet: they are in the package com.pdfjet:pdfjet-fonts-world,
 * with PDFjet's fonts for Chinese, Japanese, Korean, Arabic, Hebrew and Thai,
 * which a program that draws in them adds next to it, with Maven
 * {@code <dependency><groupId>com.pdfjet</groupId><artifactId>pdfjet-fonts-world</artifactId><version>9.1.7</version></dependency>},
 * or with Gradle {@code implementation("com.pdfjet:pdfjet-fonts-world:9.1.7")},
 * the same version as PDFjet's. The path is read from the file of that path,
 * else from the folder resources/fonts/world, as in the release packages on
 * GitHub, else from that jar, and a font found in none says what to add.
 * <p>
 * PDFjet does not shape Devanagari: these fonts are for Page.drawGlyphs, the
 * platform's text engine shaping the text with the same font file. Complete
 * for Hindi, Marathi, Nepali and Sanskrit (without the Vedic accents).
 */
public class IBMPlexSansDevanagari {
    private IBMPlexSansDevanagari() {
    }

    /** IBM Plex Sans Devanagari Bold */
    public static final String Bold       = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Bold.otf";

    /** IBM Plex Sans Devanagari Light */
    public static final String Light      = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Light.otf";
    /** IBM Plex Sans Devanagari Medium */
    public static final String Medium     = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Medium.otf";
    /** IBM Plex Sans Devanagari Regular */
    public static final String Regular    = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Regular.otf";
    /** IBM Plex Sans Devanagari SemiBold */
    public static final String SemiBold   = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-SemiBold.otf";
    /** IBM Plex Sans Devanagari Text */
    public static final String Text       = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Text.otf";
    /** IBM Plex Sans Devanagari Thin */
    public static final String Thin       = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Thin.otf";
}
