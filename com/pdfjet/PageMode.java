/**
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
public class PageMode {
    /** The default constructor */
    public PageMode() {
    }

    /** Neither document outline nor thumbnail images visible */
    public static final String USE_NONE = "UseNone";

    /** Document outline visible */
    public static final String USE_OUTLINES = "UseOutlines";

    /** Thumbnail images visible */
    public static final String USE_THUMBS = "UseThumbs";

    /** Full-screen mode */
    public static final String FULL_SCREEN = "FullScreen";

    /** (PDF 1.5) Optional content group panel visible */
    public static final String USE_OC = "UseOC";

    /** Use Attachements */
    public static final String USE_ATTACHMENTS = "UseAttachements";
}
