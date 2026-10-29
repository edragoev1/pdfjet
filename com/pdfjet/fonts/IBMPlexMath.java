/*
 * IBMPlexMath.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.fonts;

/**
 * The IBM Plex Math fonts. Each constant is the path of a font file,
 * embedded as a subset of the glyphs a document draws; pass it to the Font
 * constructor that takes a font path. PDFjet carries these fonts in itself:
 * the path is read from the library when there is no such file.
 */
public class IBMPlexMath {
    private IBMPlexMath() {
    }

    /** IBM Plex Math Regular */
    public static final String Regular = "fonts/IBMPlexMath/IBMPlexMath-Regular.ttf";
}
