/*
 * PageMode.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// Used to specify the PDF page mode.
/// </summary>
public enum PageMode {
    /// <summary>Neither the document outline nor the thumbnails are visible.</summary>
    USE_NONE,
    /// <summary>The document outline is visible.</summary>
    USE_OUTLINES,
    /// <summary>The thumbnails are visible.</summary>
    USE_THUMBS,
    /// <summary>Full-screen mode.</summary>
    FULL_SCREEN,
    /// <summary>The optional content group panel is visible.</summary>
    USE_OC,
    /// <summary>The attachments panel is visible.</summary>
    USE_ATTACHMENTS
}

internal static class PageModeExtensions {
    // The name written to the /PageMode entry.
    internal static String ToPDFName(this PageMode pageMode) {
        switch (pageMode) {
            case PageMode.USE_NONE: return "UseNone";
            case PageMode.USE_OUTLINES: return "UseOutlines";
            case PageMode.USE_THUMBS: return "UseThumbs";
            case PageMode.FULL_SCREEN: return "FullScreen";
            case PageMode.USE_OC: return "UseOC";
            case PageMode.USE_ATTACHMENTS: return "UseAttachments";
            default: throw new ArgumentException("Invalid page mode: " + pageMode);
        }
    }
}
}   // End of namespace PDFjet.NET
