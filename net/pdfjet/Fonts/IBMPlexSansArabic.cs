/*
 * IBMPlexSansArabic.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
    /// <summary>
    /// The IBM Plex Sans Arabic fonts. Each constant is the path of a font file,
    /// embedded as a subset of the glyphs a document draws; pass it to the Font
    /// constructor that takes a font path. These fonts are not in the NuGet
    /// package PDFjet: they are in the package PDFjet.Fonts.World, with
    /// PDFjet's fonts for Chinese, Japanese, Korean, Arabic, Hebrew and Thai,
    /// which a program that draws in them adds next to it, with
    /// <c>dotnet add package PDFjet.Fonts.World</c>, or in the project file
    /// <c>&lt;PackageReference Include="PDFjet.Fonts.World" Version="9.1.7" /&gt;</c>,
    /// the same version as PDFjet's. The path is read from the file of that
    /// path, else from the folder resources/fonts/world, as in the release
    /// packages on GitHub, else from that package, and a font found in none
    /// says what to add.
    /// </summary>
    public static class IBMPlexSansArabic {
        /// <summary>IBM Plex Sans Arabic Bold</summary>
        public const string Bold       = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Bold.ttf";

        /// <summary>
        /// IBM Plex Sans Arabic ExtraLight, which PDFjet does not carry from
        /// 9.1.7: a font file of its packages before, read when the file is there.
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
