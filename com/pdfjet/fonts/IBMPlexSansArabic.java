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
 * constructor that takes a font path. PDFjet carries these fonts in itself:
 * the path is read from the library when there is no such file.
 */
public class IBMPlexSansArabic {
    private IBMPlexSansArabic() {
    }

    /** IBM Plex Sans Arabic Bold */
    public static final String Bold       = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Bold.ttf";

    /**
     * IBM Plex Sans Arabic ExtraLight, which PDFjet does not carry
     * from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
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
