/*
 * IBMPlexMono.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.fonts;

/**
 * The IBM Plex Mono fonts, which PDFjet does not carry from 9.1.7: each constant
 * is the path of a font file of the packages of PDFjet before 9.1.7,
 * which the Font constructor that takes a font path reads when the file is
 * there.
 *
 * @deprecated PDFjet carries Cascadia Mono, which has all of Greek and of
 * Cyrillic in every face, as IBM Plex Mono does not, and which every constant
 * of CascadiaMono reads with nothing to download.
 */
@Deprecated
public class IBMPlexMono {
    private IBMPlexMono() {
    }

    /** IBM Plex Mono BoldItalic */
    public static final String BoldItalic       = "fonts/IBMPlexMono/IBMPlexMono-BoldItalic.ttf";
    /** IBM Plex Mono Bold */
    public static final String Bold             = "fonts/IBMPlexMono/IBMPlexMono-Bold.ttf";
    /** IBM Plex Mono ExtraLightItalic */
    public static final String ExtraLightItalic = "fonts/IBMPlexMono/IBMPlexMono-ExtraLightItalic.ttf";
    /** IBM Plex Mono ExtraLight */
    public static final String ExtraLight       = "fonts/IBMPlexMono/IBMPlexMono-ExtraLight.ttf";
    /** IBM Plex Mono Italic */
    public static final String Italic           = "fonts/IBMPlexMono/IBMPlexMono-Italic.ttf";
    /** IBM Plex Mono LightItalic */
    public static final String LightItalic      = "fonts/IBMPlexMono/IBMPlexMono-LightItalic.ttf";
    /** IBM Plex Mono Light */
    public static final String Light            = "fonts/IBMPlexMono/IBMPlexMono-Light.ttf";
    /** IBM Plex Mono MediumItalic */
    public static final String MediumItalic     = "fonts/IBMPlexMono/IBMPlexMono-MediumItalic.ttf";
    /** IBM Plex Mono Medium */
    public static final String Medium           = "fonts/IBMPlexMono/IBMPlexMono-Medium.ttf";
    /** IBM Plex Mono Regular */
    public static final String Regular          = "fonts/IBMPlexMono/IBMPlexMono-Regular.ttf";
    /** IBM Plex Mono SemiBoldItalic */
    public static final String SemiBoldItalic   = "fonts/IBMPlexMono/IBMPlexMono-SemiBoldItalic.ttf";
    /** IBM Plex Mono SemiBold */
    public static final String SemiBold         = "fonts/IBMPlexMono/IBMPlexMono-SemiBold.ttf";
    /** IBM Plex Mono TextItalic */
    public static final String TextItalic       = "fonts/IBMPlexMono/IBMPlexMono-TextItalic.ttf";
    /** IBM Plex Mono Text */
    public static final String Text             = "fonts/IBMPlexMono/IBMPlexMono-Text.ttf";
    /** IBM Plex Mono ThinItalic */
    public static final String ThinItalic       = "fonts/IBMPlexMono/IBMPlexMono-ThinItalic.ttf";
    /** IBM Plex Mono Thin */
    public static final String Thin             = "fonts/IBMPlexMono/IBMPlexMono-Thin.ttf";
}
