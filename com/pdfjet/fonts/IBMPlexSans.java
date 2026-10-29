/*
 * IBMPlexSans.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.fonts;

/**
 * The IBM Plex Sans fonts. Each constant is the path of a font file,
 * embedded as a subset of the glyphs a document draws; pass it to the Font
 * constructor that takes a font path. PDFjet carries these fonts in itself:
 * the path is read from the library when there is no such file.
 */
public class IBMPlexSans {
    private IBMPlexSans() {
    }

    /** IBM Plex Sans BoldItalic */
    public static final String BoldItalic       = "fonts/IBMPlexSans/IBMPlexSans-BoldItalic.ttf";
    /** IBM Plex Sans Bold */
    public static final String Bold             = "fonts/IBMPlexSans/IBMPlexSans-Bold.ttf";

    /**
     * IBM Plex Sans ExtraLightItalic, which PDFjet does not carry
     * from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
    public static final String ExtraLightItalic = "fonts/IBMPlexSans/IBMPlexSans-ExtraLightItalic.ttf";

    /**
     * IBM Plex Sans ExtraLight, which PDFjet does not carry
     * from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
    public static final String ExtraLight       = "fonts/IBMPlexSans/IBMPlexSans-ExtraLight.ttf";
    /** IBM Plex Sans Italic */
    public static final String Italic           = "fonts/IBMPlexSans/IBMPlexSans-Italic.ttf";
    /** IBM Plex Sans LightItalic */
    public static final String LightItalic      = "fonts/IBMPlexSans/IBMPlexSans-LightItalic.ttf";
    /** IBM Plex Sans Light */
    public static final String Light            = "fonts/IBMPlexSans/IBMPlexSans-Light.ttf";
    /** IBM Plex Sans MediumItalic */
    public static final String MediumItalic     = "fonts/IBMPlexSans/IBMPlexSans-MediumItalic.ttf";
    /** IBM Plex Sans Medium */
    public static final String Medium           = "fonts/IBMPlexSans/IBMPlexSans-Medium.ttf";
    /** IBM Plex Sans Regular */
    public static final String Regular          = "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf";
    /** IBM Plex Sans SemiBoldItalic */
    public static final String SemiBoldItalic   = "fonts/IBMPlexSans/IBMPlexSans-SemiBoldItalic.ttf";
    /** IBM Plex Sans SemiBold */
    public static final String SemiBold         = "fonts/IBMPlexSans/IBMPlexSans-SemiBold.ttf";
    /** IBM Plex Sans TextItalic */
    public static final String TextItalic       = "fonts/IBMPlexSans/IBMPlexSans-TextItalic.ttf";
    /** IBM Plex Sans Text */
    public static final String Text             = "fonts/IBMPlexSans/IBMPlexSans-Text.ttf";
    /** IBM Plex Sans ThinItalic */
    public static final String ThinItalic       = "fonts/IBMPlexSans/IBMPlexSans-ThinItalic.ttf";
    /** IBM Plex Sans Thin */
    public static final String Thin             = "fonts/IBMPlexSans/IBMPlexSans-Thin.ttf";
}
