/*
 * IBMPlexSansHebrew.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
    /// <summary>
    /// The IBM Plex Sans Hebrew fonts. Each constant is the path of a font
    /// file, embedded as a subset of the glyphs a document draws; pass it to
    /// the Font constructor that takes a font path. PDFjet carries these fonts
    /// in itself: the path is read from the library when there is no such file.
    /// </summary>
    public static class IBMPlexSansHebrew {
        /// <summary>IBM Plex Sans Hebrew Bold</summary>
        public const string Bold       = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Bold.ttf";

        /// <summary>
        /// IBM Plex Sans Hebrew ExtraLight, which PDFjet does not carry from
        /// 9.1.7: a font file of its packages before, read when the file is there.
        /// </summary>
        [Obsolete("PDFjet carries the other weights of the family.")]
        public const string ExtraLight = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-ExtraLight.ttf";
        /// <summary>IBM Plex Sans Hebrew Light</summary>
        public const string Light      = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Light.ttf";
        /// <summary>IBM Plex Sans Hebrew Medium</summary>
        public const string Medium     = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Medium.ttf";
        /// <summary>IBM Plex Sans Hebrew Regular</summary>
        public const string Regular    = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Regular.ttf";
        /// <summary>IBM Plex Sans Hebrew SemiBold</summary>
        public const string SemiBold   = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-SemiBold.ttf";
        /// <summary>IBM Plex Sans Hebrew Text</summary>
        public const string Text       = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Text.ttf";
        /// <summary>IBM Plex Sans Hebrew Thin</summary>
        public const string Thin       = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Thin.ttf";
    }
}
