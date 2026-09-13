/**
 * PageMode.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

/**
 * Used to specify the PDF page mode.
 */
public enum PageMode: String {
    /// Neither the document outline nor the thumbnails are visible.
    case USE_NONE = "UseNone"
    /// The document outline is visible.
    case USE_OUTLINES = "UseOutlines"
    /// The thumbnails are visible.
    case USE_THUMBS = "UseThumbs"
    /// Full-screen mode.
    case FULL_SCREEN = "FullScreen"
    /// The optional content group panel is visible.
    case USE_OC = "UseOC"
    /// The attachments panel is visible.
    case USE_ATTACHMENTS = "UseAttachments"
}
