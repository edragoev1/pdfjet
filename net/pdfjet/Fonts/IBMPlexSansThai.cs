/*
 * IBMPlexSansThai.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
    /// <summary>
    /// The IBM Plex Sans Thai fonts. Each constant is the path of a font file,
    /// embedded as a subset of the glyphs a document draws; pass it to the Font
    /// constructor that takes a font path. PDFjet carries these fonts in
    /// itself: the path is read from the library when there is no such file.
    /// </summary>
    public static class IBMPlexSansThai {
        /// <summary>IBM Plex Sans Thai Bold</summary>
        public const string Bold       = "fonts/IBMPlexSansThai/IBMPlexSansThai-Bold.ttf";

        /// <summary>
        /// IBM Plex Sans Thai ExtraLight, which PDFjet does not carry from
        /// 9.1.7: a font file of pdfjet-fonts, read when the file is there.
        /// </summary>
        [Obsolete("PDFjet carries the other weights of the family.")]
        public const string ExtraLight = "fonts/IBMPlexSansThai/IBMPlexSansThai-ExtraLight.ttf";
        /// <summary>IBM Plex Sans Thai Light</summary>
        public const string Light      = "fonts/IBMPlexSansThai/IBMPlexSansThai-Light.ttf";
        /// <summary>IBM Plex Sans Thai Medium</summary>
        public const string Medium     = "fonts/IBMPlexSansThai/IBMPlexSansThai-Medium.ttf";
        /// <summary>IBM Plex Sans Thai Regular</summary>
        public const string Regular    = "fonts/IBMPlexSansThai/IBMPlexSansThai-Regular.ttf";
        /// <summary>IBM Plex Sans Thai SemiBold</summary>
        public const string SemiBold   = "fonts/IBMPlexSansThai/IBMPlexSansThai-SemiBold.ttf";
        /// <summary>IBM Plex Sans Thai Text</summary>
        public const string Text       = "fonts/IBMPlexSansThai/IBMPlexSansThai-Text.ttf";
        /// <summary>IBM Plex Sans Thai Thin</summary>
        public const string Thin       = "fonts/IBMPlexSansThai/IBMPlexSansThai-Thin.ttf";
    }
}
