/*
 * CJKFont.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Used to select Chinese, Japanese and Korean fonts, for the Font constructor
 * that takes one: these fonts are not embedded, which keeps a document small,
 * and the viewer supplies them. To ship the glyphs with the document, use an
 * embedded font, such as IBM Plex Sans JP, KR, SC or TC.
 */
public enum CJKFont {
    /** Chinese (Traditional) font */
    ADOBE_MING_STD_LIGHT,

    /** Chinese (Simplified) font */
    ST_HEITI_SC_LIGHT,

    /** Japanese font */
    KOZ_MIN_PRO_VI_REGULAR,

    /** Korean font */
    ADOBE_MYUNGJO_STD_MEDIUM
}
