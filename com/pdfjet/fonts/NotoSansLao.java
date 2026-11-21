/*
 * NotoSansLao.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.fonts;

/**
 * The Noto Sans Lao fonts. Each constant is the path of a font file,
 * embedded as a subset of the glyphs a document draws; pass it to the Font
 * constructor that takes a font path. These fonts are not in the jar
 * com.pdfjet:pdfjet: they are in the package com.pdfjet:pdfjet-fonts-world,
 * with PDFjet's fonts for Chinese, Japanese, Korean, Arabic, Hebrew, Thai and others,
 * which a program that draws in them adds next to it, with Maven
 * {@code <dependency><groupId>com.pdfjet</groupId><artifactId>pdfjet-fonts-world</artifactId><version>9.1.7</version></dependency>},
 * or with Gradle {@code implementation("com.pdfjet:pdfjet-fonts-world:9.1.7")},
 * the same version as PDFjet's. The path is read from the file of that path,
 * else from the folder resources/fonts/world, as in the release packages on
 * GitHub, else from that jar, and a font found in none says what to add.
 * <p>
 * PDFjet does not shape Lao: these fonts are for ComplexText, which
 * draws the text as the platform's text engine shapes it with the same font
 * file, for Lao. Regular and Bold, from Noto's release, unmodified.
 */
public class NotoSansLao {
    private NotoSansLao() {
    }

    /** Noto Sans Lao Bold */
    public static final String Bold       = "fonts/NotoSansLao/NotoSansLao-Bold.otf";

    /** Noto Sans Lao Regular */
    public static final String Regular    = "fonts/NotoSansLao/NotoSansLao-Regular.otf";
}
