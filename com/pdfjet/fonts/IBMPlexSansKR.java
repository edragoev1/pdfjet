/*
 * IBMPlexSansKR.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.fonts;

/**
 * The IBM Plex Sans KR fonts. Each constant is the path of a font file,
 * embedded as a subset of the glyphs a document draws; pass it to the Font
 * constructor that takes a font path. PDFjet carries these fonts in itself:
 * the path is read from the library when there is no such file.
 */
public class IBMPlexSansKR {
    private IBMPlexSansKR() {
    }

    /** IBM Plex Sans KR Bold */
    public static final String Bold       = "fonts/IBMPlexSansKR/IBMPlexSansKR-Bold.otf";

    /**
     * IBM Plex Sans KR ExtraLight, which PDFjet does not carry
     * from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
    public static final String ExtraLight = "fonts/IBMPlexSansKR/IBMPlexSansKR-ExtraLight.ttf";

    /**
     * IBM Plex Sans KR Light, which PDFjet does not carry
     * from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
    public static final String Light      = "fonts/IBMPlexSansKR/IBMPlexSansKR-Light.ttf";

    /**
     * IBM Plex Sans KR Medium, which PDFjet does not carry
     * from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
    public static final String Medium     = "fonts/IBMPlexSansKR/IBMPlexSansKR-Medium.ttf";
    /** IBM Plex Sans KR Regular */
    public static final String Regular    = "fonts/IBMPlexSansKR/IBMPlexSansKR-Regular.otf";
    /** IBM Plex Sans KR SemiBold */
    public static final String SemiBold   = "fonts/IBMPlexSansKR/IBMPlexSansKR-SemiBold.otf";

    /**
     * IBM Plex Sans KR Text, which PDFjet does not carry
     * from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
    public static final String Text       = "fonts/IBMPlexSansKR/IBMPlexSansKR-Text.ttf";

    /**
     * IBM Plex Sans KR Thin, which PDFjet does not carry
     * from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
    public static final String Thin       = "fonts/IBMPlexSansKR/IBMPlexSansKR-Thin.ttf";
}
