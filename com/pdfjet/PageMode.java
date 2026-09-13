/*
 * PageMode.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 *  Used to specify the PDF page mode.
 *
 */
public enum PageMode {
    /** Neither document outline nor thumbnail images visible */
    USE_NONE("UseNone"),
    /** Document outline visible */
    USE_OUTLINES("UseOutlines"),
    /** Thumbnail images visible */
    USE_THUMBS("UseThumbs"),
    /** Full-screen mode */
    FULL_SCREEN("FullScreen"),
    /** (PDF 1.5) Optional content group panel visible */
    USE_OC("UseOC"),
    /** The attachments panel is visible. */
    USE_ATTACHMENTS("UseAttachments");

    /** The name written to the /PageMode entry. */
    final String value;

    PageMode(String value) {
        this.value = value;
    }
}
