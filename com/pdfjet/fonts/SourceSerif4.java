/*
 * SourceSerif4.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.fonts;

/**
 * The Source Serif 4 fonts, which PDFjet does not carry from 9.1.7: each
 * constant is the path of a font file of the packages of PDFjet before 9.1.7,
 * which the Font constructor that takes a font path reads when the file is
 * there.
 *
 * @deprecated PDFjet carries IBM Plex Serif, which every constant of
 * IBMPlexSerif reads with nothing to download.
 */
@Deprecated
public class SourceSerif4 {
    private SourceSerif4() {
    }

    /** Source Serif 4 Black */
    public static final String Black            = "fonts/SourceSerif4/SourceSerif4-Black.ttf";
    /** Source Serif 4 BlackItalic */
    public static final String BlackItalic      = "fonts/SourceSerif4/SourceSerif4-BlackItalic.ttf";
    /** Source Serif 4 Bold */
    public static final String Bold             = "fonts/SourceSerif4/SourceSerif4-Bold.ttf";
    /** Source Serif 4 BoldItalic */
    public static final String BoldItalic       = "fonts/SourceSerif4/SourceSerif4-BoldItalic.ttf";
    /** Source Serif 4 ExtraLight */
    public static final String ExtraLight       = "fonts/SourceSerif4/SourceSerif4-ExtraLight.ttf";
    /** Source Serif 4 ExtraLightItalic */
    public static final String ExtraLightItalic = "fonts/SourceSerif4/SourceSerif4-ExtraLightItalic.ttf";
    /** Source Serif 4 Italic */
    public static final String Italic           = "fonts/SourceSerif4/SourceSerif4-Italic.ttf";
    /** Source Serif 4 Light */
    public static final String Light            = "fonts/SourceSerif4/SourceSerif4-Light.ttf";
    /** Source Serif 4 LightItalic */
    public static final String LightItalic      = "fonts/SourceSerif4/SourceSerif4-LightItalic.ttf";
    /** Source Serif 4 Medium */
    public static final String Medium           = "fonts/SourceSerif4/SourceSerif4-Medium.ttf";
    /** Source Serif 4 MediumItalic */
    public static final String MediumItalic     = "fonts/SourceSerif4/SourceSerif4-MediumItalic.ttf";
    /** Source Serif 4 Regular */
    public static final String Regular          = "fonts/SourceSerif4/SourceSerif4-Regular.ttf";
    /** Source Serif 4 SemiBold */
    public static final String SemiBold         = "fonts/SourceSerif4/SourceSerif4-SemiBold.ttf";
    /** Source Serif 4 SemiBoldItalic */
    public static final String SemiBoldItalic   = "fonts/SourceSerif4/SourceSerif4-SemiBoldItalic.ttf";
}
