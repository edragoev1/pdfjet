/*
 * CascadiaMono.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.fonts;

/**
 * The Cascadia Mono fonts, Microsoft's monospace font, with all of Latin,
 * Greek and Cyrillic in every face, the italic ones too. Each constant is
 * the path of a font file, embedded as a subset of the glyphs a document
 * draws; pass it to the Font constructor that takes a font path. PDFjet
 * carries these fonts in itself: the path is read from the library when
 * there is no such file.
 */
public class CascadiaMono {
    private CascadiaMono() {
    }

    /** Cascadia Mono Bold */
    public static final String Bold            = "fonts/CascadiaMono/CascadiaMono-Bold.otf";
    /** Cascadia Mono BoldItalic */
    public static final String BoldItalic      = "fonts/CascadiaMono/CascadiaMono-BoldItalic.otf";
    /** Cascadia Mono Italic */
    public static final String Italic          = "fonts/CascadiaMono/CascadiaMono-Italic.otf";
    /** Cascadia Mono Light */
    public static final String Light           = "fonts/CascadiaMono/CascadiaMono-Light.otf";
    /** Cascadia Mono LightItalic */
    public static final String LightItalic     = "fonts/CascadiaMono/CascadiaMono-LightItalic.otf";
    /** Cascadia Mono Regular */
    public static final String Regular         = "fonts/CascadiaMono/CascadiaMono-Regular.otf";
    /** Cascadia Mono SemiBold */
    public static final String SemiBold        = "fonts/CascadiaMono/CascadiaMono-SemiBold.otf";
    /** Cascadia Mono SemiBoldItalic */
    public static final String SemiBoldItalic  = "fonts/CascadiaMono/CascadiaMono-SemiBoldItalic.otf";
    /** Cascadia Mono SemiLight */
    public static final String SemiLight       = "fonts/CascadiaMono/CascadiaMono-SemiLight.otf";
    /** Cascadia Mono SemiLightItalic */
    public static final String SemiLightItalic = "fonts/CascadiaMono/CascadiaMono-SemiLightItalic.otf";
}
