/*
 * IBMPlexSansTC.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.fonts;

/**
 * The IBM Plex Sans TC fonts. Each constant is the path of a font file,
 * embedded as a subset of the glyphs a document draws; pass it to the Font
 * constructor that takes a font path. PDFjet carries these fonts in itself:
 * the path is read from the library when there is no such file.
 */
public class IBMPlexSansTC {
    private IBMPlexSansTC() {
    }

    /** IBM Plex Sans TC Bold */
    public static final String Bold       = "fonts/IBMPlexSansTC/IBMPlexSansTC-Bold.otf";

    /**
     * IBM Plex Sans TC ExtraLight, which PDFjet does not carry
     * from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
    public static final String ExtraLight = "fonts/IBMPlexSansTC/IBMPlexSansTC-ExtraLight.ttf";

    /**
     * IBM Plex Sans TC Light, which PDFjet does not carry
     * from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
    public static final String Light      = "fonts/IBMPlexSansTC/IBMPlexSansTC-Light.ttf";

    /**
     * IBM Plex Sans TC Medium, which PDFjet does not carry
     * from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
    public static final String Medium     = "fonts/IBMPlexSansTC/IBMPlexSansTC-Medium.ttf";
    /** IBM Plex Sans TC Regular */
    public static final String Regular    = "fonts/IBMPlexSansTC/IBMPlexSansTC-Regular.otf";
    /** IBM Plex Sans TC SemiBold */
    public static final String SemiBold   = "fonts/IBMPlexSansTC/IBMPlexSansTC-SemiBold.otf";

    /**
     * IBM Plex Sans TC Text, which PDFjet does not carry
     * from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
    public static final String Text       = "fonts/IBMPlexSansTC/IBMPlexSansTC-Text.ttf";

    /**
     * IBM Plex Sans TC Thin, which PDFjet does not carry
     * from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
    public static final String Thin       = "fonts/IBMPlexSansTC/IBMPlexSansTC-Thin.ttf";
}
