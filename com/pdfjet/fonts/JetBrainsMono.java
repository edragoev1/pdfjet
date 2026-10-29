/*
 * JetBrainsMono.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.fonts;

/**
 * The JetBrains Mono fonts. Each constant is the path of a font file,
 * embedded as a subset of the glyphs a document draws; pass it to the Font
 * constructor that takes a font path. PDFjet carries these fonts in itself:
 * the path is read from the library when there is no such file.
 */
public class JetBrainsMono {
    private JetBrainsMono() {
    }

    /** JetBrains Mono Bold */
    public static final String Bold             = "fonts/JetBrainsMono/JetBrainsMono-Bold.ttf";
    /** JetBrains Mono BoldItalic */
    public static final String BoldItalic       = "fonts/JetBrainsMono/JetBrainsMono-BoldItalic.ttf";

    /**
     * JetBrains Mono ExtraLight, which PDFjet does not carry
     * from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
    public static final String ExtraLight       = "fonts/JetBrainsMono/JetBrainsMono-ExtraLight.ttf";

    /**
     * JetBrains Mono ExtraLightItalic, which PDFjet does not carry
     * from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
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
