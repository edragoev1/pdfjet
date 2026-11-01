/*
 * IBMPlexSansArabic.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.fonts;

/**
 * The IBM Plex Sans Arabic fonts. Each constant is the path of a font file,
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
 */
public class IBMPlexSansArabic {
    private IBMPlexSansArabic() {
    }

    /** IBM Plex Sans Arabic Bold */
    public static final String Bold       = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Bold.ttf";

    /**
     * IBM Plex Sans Arabic ExtraLight, which PDFjet does not carry
     * from 9.1.7: a font file of its packages before, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
    public static final String ExtraLight = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-ExtraLight.ttf";
    /** IBM Plex Sans Arabic Light */
    public static final String Light      = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Light.ttf";
    /** IBM Plex Sans Arabic Medium */
    public static final String Medium     = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Medium.ttf";
    /** IBM Plex Sans Arabic Regular */
    public static final String Regular    = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Regular.ttf";
    /** IBM Plex Sans Arabic SemiBold */
    public static final String SemiBold   = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-SemiBold.ttf";
    /** IBM Plex Sans Arabic Text */
    public static final String Text       = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Text.ttf";
    /** IBM Plex Sans Arabic Thin */
    public static final String Thin       = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Thin.ttf";
}
