/*
 * JetBrainsMono.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.fonts;

/**
 * The JetBrains Mono fonts, which PDFjet does not carry from 9.1.7: each constant
 * is the path of a font file of the packages of PDFjet before 9.1.7,
 * which the Font constructor that takes a font path reads when the file is
 * there.
 *
 * @deprecated PDFjet carries Cascadia Mono, which has all of Greek and of
 * Cyrillic in every face, as JetBrains Mono does not, and which every constant
 * of CascadiaMono reads with nothing to download.
 */
@Deprecated
public class JetBrainsMono {
    private JetBrainsMono() {
    }

    /** JetBrains Mono Bold */
    public static final String Bold             = "fonts/JetBrainsMono/JetBrainsMono-Bold.ttf";
    /** JetBrains Mono BoldItalic */
    public static final String BoldItalic       = "fonts/JetBrainsMono/JetBrainsMono-BoldItalic.ttf";
    /** JetBrains Mono ExtraLight */
    public static final String ExtraLight       = "fonts/JetBrainsMono/JetBrainsMono-ExtraLight.ttf";
    /** JetBrains Mono ExtraLightItalic */
    public static final String ExtraLightItalic = "fonts/JetBrainsMono/JetBrainsMono-ExtraLightItalic.ttf";
    /** JetBrains Mono Italic */
    public static final String Italic           = "fonts/JetBrainsMono/JetBrainsMono-Italic.ttf";
    /** JetBrains Mono Light */
    public static final String Light            = "fonts/JetBrainsMono/JetBrainsMono-Light.ttf";
    /** JetBrains Mono LightItalic */
    public static final String LightItalic      = "fonts/JetBrainsMono/JetBrainsMono-LightItalic.ttf";
    /** JetBrains Mono Medium */
    public static final String Medium           = "fonts/JetBrainsMono/JetBrainsMono-Medium.ttf";
    /** JetBrains Mono MediumItalic */
    public static final String MediumItalic     = "fonts/JetBrainsMono/JetBrainsMono-MediumItalic.ttf";
    /** JetBrains Mono Regular */
    public static final String Regular          = "fonts/JetBrainsMono/JetBrainsMono-Regular.ttf";
    /** JetBrains Mono SemiBold */
    public static final String SemiBold         = "fonts/JetBrainsMono/JetBrainsMono-SemiBold.ttf";
    /** JetBrains Mono SemiBoldItalic */
    public static final String SemiBoldItalic   = "fonts/JetBrainsMono/JetBrainsMono-SemiBoldItalic.ttf";
}
