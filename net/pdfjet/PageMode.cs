/*
 * PageMode.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// Used to specify the PDF page layout.
/// </summary>
public class PageMode {
    /// <summary>Neither the document outline nor the thumbnails are visible.</summary>
    public const String USE_NONE = "UseNone";            // Neither document outline nor thumbnail images visible
    /// <summary>The document outline is visible.</summary>
    public const String USE_OUTLINES = "UseOutlines";    // Document outline visible
    /// <summary>The thumbnails are visible.</summary>
    public const String USE_THUMBS = "UseThumbs";        // Thumbnail images visible
    /// <summary>Full-screen mode.</summary>
    public const String FULL_SCREEN = "FullScreen";      // Full-screen mode
    /// <summary>The optional content group panel is visible.</summary>
    public const String USE_OC = "UseOC";                // (PDF 1.5) Optional content group panel visible
    /// <summary>The attachments panel is visible.</summary>
    public const String USE_ATTACHMENTS = "UseAttachements";
}
}   // End of namespace PDFjet.NET
