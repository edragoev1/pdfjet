/*
 * IBMPlexSansThai.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.fonts;

/**
 * The IBM Plex Sans Thai fonts. Each constant is the path of a font file,
 * embedded as a subset of the glyphs a document draws; pass it to the Font
 * constructor that takes a font path. PDFjet carries these fonts in itself:
 * the path is read from the library when there is no such file.
 */
public class IBMPlexSansThai {
    private IBMPlexSansThai() {
    }

    /** IBM Plex Sans Thai Bold */
    public static final String Bold       = "fonts/IBMPlexSansThai/IBMPlexSansThai-Bold.ttf";

    /**
     * IBM Plex Sans Thai ExtraLight, which PDFjet does not carry
     * from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
    public static final String ExtraLight = "fonts/IBMPlexSansThai/IBMPlexSansThai-ExtraLight.ttf";
    /** IBM Plex Sans Thai Light */
    public static final String Light      = "fonts/IBMPlexSansThai/IBMPlexSansThai-Light.ttf";
    /** IBM Plex Sans Thai Medium */
    public static final String Medium     = "fonts/IBMPlexSansThai/IBMPlexSansThai-Medium.ttf";
    /** IBM Plex Sans Thai Regular */
    public static final String Regular    = "fonts/IBMPlexSansThai/IBMPlexSansThai-Regular.ttf";
    /** IBM Plex Sans Thai SemiBold */
    public static final String SemiBold   = "fonts/IBMPlexSansThai/IBMPlexSansThai-SemiBold.ttf";
    /** IBM Plex Sans Thai Text */
    public static final String Text       = "fonts/IBMPlexSansThai/IBMPlexSansThai-Text.ttf";
    /** IBM Plex Sans Thai Thin */
    public static final String Thin       = "fonts/IBMPlexSansThai/IBMPlexSansThai-Thin.ttf";
}
