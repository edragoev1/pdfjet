/**
 * PageMode.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

/**
 * Used to specify the PDF page layout.
 */
namespace PDFjet.NET {
public class PageMode {
    public const String USE_NONE = "UseNone";            // Neither document outline nor thumbnail images visible
    public const String USE_OUTLINES = "UseOutlines";    // Document outline visible
    public const String USE_THUMBS = "UseThumbs";        // Thumbnail images visible
    public const String FULL_SCREEN = "FullScreen";      // Full-screen mode
    public const String USE_OC = "UseOC";                // (PDF 1.5) Optional content group panel visible
    public const String USE_ATTACHMENTS = "UseAttachements";
}
}   // End of namespace PDFjet.NET
