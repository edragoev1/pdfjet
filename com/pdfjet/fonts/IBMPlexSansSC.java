/*
 * IBMPlexSansSC.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.fonts;

/**
 * The IBM Plex Sans SC fonts. Each constant is the path of a font file,
 * embedded as a subset of the glyphs a document draws; pass it to the Font
 * constructor that takes a font path. PDFjet carries these fonts in itself:
 * the path is read from the library when there is no such file.
 */
public class IBMPlexSansSC {
    private IBMPlexSansSC() {
    }

    /** IBM Plex Sans SC Bold */
    public static final String Bold       = "fonts/IBMPlexSansSC/IBMPlexSansSC-Bold.otf";

    /**
     * IBM Plex Sans SC ExtraLight, which PDFjet does not carry
     * from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
    public static final String ExtraLight = "fonts/IBMPlexSansSC/IBMPlexSansSC-ExtraLight.ttf";

    /**
     * IBM Plex Sans SC Light, which PDFjet does not carry
     * from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
    public static final String Light      = "fonts/IBMPlexSansSC/IBMPlexSansSC-Light.ttf";

    /**
     * IBM Plex Sans SC Medium, which PDFjet does not carry
     * from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
    public static final String Medium     = "fonts/IBMPlexSansSC/IBMPlexSansSC-Medium.ttf";
    /** IBM Plex Sans SC Regular */
    public static final String Regular    = "fonts/IBMPlexSansSC/IBMPlexSansSC-Regular.otf";
    /** IBM Plex Sans SC SemiBold */
    public static final String SemiBold   = "fonts/IBMPlexSansSC/IBMPlexSansSC-SemiBold.otf";

    /**
     * IBM Plex Sans SC Text, which PDFjet does not carry
     * from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
    public static final String Text       = "fonts/IBMPlexSansSC/IBMPlexSansSC-Text.ttf";

    /**
     * IBM Plex Sans SC Thin, which PDFjet does not carry
     * from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
    public static final String Thin       = "fonts/IBMPlexSansSC/IBMPlexSansSC-Thin.ttf";
}
