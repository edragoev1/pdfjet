/*
 * IBMPlexSansHebrew.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.fonts;

/**
 * The IBM Plex Sans Hebrew fonts. Each constant is the path of a font file,
 * embedded as a subset of the glyphs a document draws; pass it to the Font
 * constructor that takes a font path. PDFjet carries these fonts in itself:
 * the path is read from the library when there is no such file.
 */
public class IBMPlexSansHebrew {
    private IBMPlexSansHebrew() {
    }

    /** IBM Plex Sans Hebrew Bold */
    public static final String Bold       = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Bold.ttf";

    /**
     * IBM Plex Sans Hebrew ExtraLight, which PDFjet does not carry
     * from 9.1.7: a font file of its packages before, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
    public static final String ExtraLight = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-ExtraLight.ttf";
    /** IBM Plex Sans Hebrew Light */
    public static final String Light      = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Light.ttf";
    /** IBM Plex Sans Hebrew Medium */
    public static final String Medium     = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Medium.ttf";
    /** IBM Plex Sans Hebrew Regular */
    public static final String Regular    = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Regular.ttf";
    /** IBM Plex Sans Hebrew SemiBold */
    public static final String SemiBold   = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-SemiBold.ttf";
    /** IBM Plex Sans Hebrew Text */
    public static final String Text       = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Text.ttf";
    /** IBM Plex Sans Hebrew Thin */
    public static final String Thin       = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Thin.ttf";
}
