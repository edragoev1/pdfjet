/**
 * PageMode.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

/**
 * Used to specify the PDF page mode.
 */
public class PageMode {
    public static let USE_NONE = "UseNone"             // Neither document outline nor thumbnail images visible
    public static let USE_OUTLINES = "UseOutlines"     // Document outline visible
    public static let USE_THUMBS = "UseThumbs"         // Thumbnail images visible
    public static let FULL_SCREEN = "FullScreen"       // Full-screen mode
    public static let USE_OC = "UseOC"                 // (PDF 1.5) Optional content group panel visible
    public static let USE_ATTACHMENTS = "UseAttachements"
}
