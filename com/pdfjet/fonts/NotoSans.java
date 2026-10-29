/*
 * NotoSans.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.fonts;

/**
 * The Noto Sans fonts, which PDFjet does not carry from 9.1.7: each
 * constant is the path of a font file of pdfjet-fonts,
 * https://github.com/edragoev1/pdfjet-fonts, which the Font constructor that
 * takes a font path reads when the file is there.
 *
 * @deprecated PDFjet carries IBM Plex Sans, which every constant of
 * IBMPlexSans reads with nothing to download.
 */
@Deprecated
public class NotoSans {
    private NotoSans() {
    }

    /** Noto Sans Black */
    public static final String Black            = "fonts/NotoSans/NotoSans-Black.ttf";
    /** Noto Sans BlackItalic */
    public static final String BlackItalic      = "fonts/NotoSans/NotoSans-BlackItalic.ttf";
    /** Noto Sans Bold */
    public static final String Bold             = "fonts/NotoSans/NotoSans-Bold.ttf";
    /** Noto Sans BoldItalic */
    public static final String BoldItalic       = "fonts/NotoSans/NotoSans-BoldItalic.ttf";
    /** Noto Sans ExtraBold */
    public static final String ExtraBold        = "fonts/NotoSans/NotoSans-ExtraBold.ttf";
    /** Noto Sans ExtraBoldItalic */
    public static final String ExtraBoldItalic  = "fonts/NotoSans/NotoSans-ExtraBoldItalic.ttf";
    /** Noto Sans ExtraLight */
    public static final String ExtraLight       = "fonts/NotoSans/NotoSans-ExtraLight.ttf";
    /** Noto Sans ExtraLightItalic */
    public static final String ExtraLightItalic = "fonts/NotoSans/NotoSans-ExtraLightItalic.ttf";
    /** Noto Sans Italic */
    public static final String Italic           = "fonts/NotoSans/NotoSans-Italic.ttf";
    /** Noto Sans Light */
    public static final String Light            = "fonts/NotoSans/NotoSans-Light.ttf";
    /** Noto Sans LightItalic */
    public static final String LightItalic      = "fonts/NotoSans/NotoSans-LightItalic.ttf";
    /** Noto Sans Medium */
    public static final String Medium           = "fonts/NotoSans/NotoSans-Medium.ttf";
    /** Noto Sans MediumItalic */
    public static final String MediumItalic     = "fonts/NotoSans/NotoSans-MediumItalic.ttf";
    /** Noto Sans Regular */
    public static final String Regular          = "fonts/NotoSans/NotoSans-Regular.ttf";
    /** Noto Sans SemiBold */
    public static final String SemiBold         = "fonts/NotoSans/NotoSans-SemiBold.ttf";
    /** Noto Sans SemiBoldItalic */
    public static final String SemiBoldItalic   = "fonts/NotoSans/NotoSans-SemiBoldItalic.ttf";
    /** Noto Sans Thin */
    public static final String Thin             = "fonts/NotoSans/NotoSans-Thin.ttf";
    /** Noto Sans ThinItalic */
    public static final String ThinItalic       = "fonts/NotoSans/NotoSans-ThinItalic.ttf";
}
