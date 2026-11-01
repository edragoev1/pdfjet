/*
 * IBMPlexSansJP.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.fonts;

/**
 * The IBM Plex Sans JP fonts. Each constant is the path of a font file,
 * embedded as a subset of the glyphs a document draws; pass it to the Font
 * constructor that takes a font path. These fonts are not in the jar
 * com.pdfjet:pdfjet: they are in the package com.pdfjet:pdfjet-fonts-world,
 * with PDFjet's fonts for Chinese, Japanese, Korean, Arabic, Hebrew and Thai,
 * which a program that draws in them adds next to it, with Maven
 * {@code <dependency><groupId>com.pdfjet</groupId><artifactId>pdfjet-fonts-world</artifactId><version>9.1.7</version></dependency>},
 * or with Gradle {@code implementation("com.pdfjet:pdfjet-fonts-world:9.1.7")},
 * the same version as PDFjet's. The path is read from the file of that path,
 * else from the folder resources/fonts/world, as in the release packages on
 * GitHub, else from that jar, and a font found in none says what to add.
 */
public class IBMPlexSansJP {
    private IBMPlexSansJP() {
    }

    /** IBM Plex Sans JP Bold */
    public static final String Bold       = "fonts/IBMPlexSansJP/IBMPlexSansJP-Bold.otf";

    /**
     * IBM Plex Sans JP ExtraLight, which PDFjet does not carry
     * from 9.1.7: a font file of its packages before, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
    public static final String ExtraLight = "fonts/IBMPlexSansJP/IBMPlexSansJP-ExtraLight.ttf";

    /**
     * IBM Plex Sans JP Light, which PDFjet does not carry
     * from 9.1.7: a font file of its packages before, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
    public static final String Light      = "fonts/IBMPlexSansJP/IBMPlexSansJP-Light.ttf";

    /**
     * IBM Plex Sans JP Medium, which PDFjet does not carry
     * from 9.1.7: a font file of its packages before, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
    public static final String Medium     = "fonts/IBMPlexSansJP/IBMPlexSansJP-Medium.ttf";
    /** IBM Plex Sans JP Regular */
    public static final String Regular    = "fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf";
    /** IBM Plex Sans JP SemiBold */
    public static final String SemiBold   = "fonts/IBMPlexSansJP/IBMPlexSansJP-SemiBold.otf";

    /**
     * IBM Plex Sans JP Text, which PDFjet does not carry
     * from 9.1.7: a font file of its packages before, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
    public static final String Text       = "fonts/IBMPlexSansJP/IBMPlexSansJP-Text.ttf";

    /**
     * IBM Plex Sans JP Thin, which PDFjet does not carry
     * from 9.1.7: a font file of its packages before, read when the file is there.
     *
     * @deprecated PDFjet carries the other weights of the family.
     */
    @Deprecated
    public static final String Thin       = "fonts/IBMPlexSansJP/IBMPlexSansJP-Thin.ttf";
}
