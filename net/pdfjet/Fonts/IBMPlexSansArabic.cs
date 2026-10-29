/*
 * IBMPlexSansArabic.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
    /// <summary>
    /// The IBM Plex Sans Arabic fonts. Each constant is the path of a font
    /// file, embedded as a subset of the glyphs a document draws; pass it to
    /// the Font constructor that takes a font path. PDFjet carries these fonts
    /// in itself: the path is read from the library when there is no such file.
    /// </summary>
    public static class IBMPlexSansArabic {
        /// <summary>IBM Plex Sans Arabic Bold</summary>
        public const string Bold       = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Bold.ttf";

        /// <summary>
        /// IBM Plex Sans Arabic ExtraLight, which PDFjet does not carry from
        /// 9.1.7: a font file of pdfjet-fonts, read when the file is there.
        /// </summary>
        [Obsolete("PDFjet carries the other weights of the family.")]
        public const string ExtraLight = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-ExtraLight.ttf";
        /// <summary>IBM Plex Sans Arabic Light</summary>
        public const string Light      = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Light.ttf";
        /// <summary>IBM Plex Sans Arabic Medium</summary>
        public const string Medium     = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Medium.ttf";
        /// <summary>IBM Plex Sans Arabic Regular</summary>
        public const string Regular    = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Regular.ttf";
        /// <summary>IBM Plex Sans Arabic SemiBold</summary>
        public const string SemiBold   = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-SemiBold.ttf";
        /// <summary>IBM Plex Sans Arabic Text</summary>
        public const string Text       = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Text.ttf";
        /// <summary>IBM Plex Sans Arabic Thin</summary>
        public const string Thin       = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Thin.ttf";
    }
}
