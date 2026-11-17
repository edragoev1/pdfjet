/*
 * IBMPlexSansDevanagari.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
    /// <summary>
    /// The IBM Plex Sans Devanagari fonts. Each constant is the path of a font file,
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
    /// PDFjet does not shape Devanagari: these fonts are for Page.DrawGlyphs,
    /// a shaping engine shaping the text with the same font file. Complete
    /// for Hindi, Marathi, Nepali and Sanskrit (without the Vedic accents).
    /// </summary>
    public static class IBMPlexSansDevanagari {
        /// <summary>IBM Plex Sans Devanagari Bold</summary>
        public const string Bold       = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Bold.otf";

        /// <summary>IBM Plex Sans Devanagari Light</summary>
        public const string Light      = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Light.otf";
        /// <summary>IBM Plex Sans Devanagari Medium</summary>
        public const string Medium     = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Medium.otf";
        /// <summary>IBM Plex Sans Devanagari Regular</summary>
        public const string Regular    = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Regular.otf";
        /// <summary>IBM Plex Sans Devanagari SemiBold</summary>
        public const string SemiBold   = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-SemiBold.otf";
        /// <summary>IBM Plex Sans Devanagari Text</summary>
        public const string Text       = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Text.otf";
        /// <summary>IBM Plex Sans Devanagari Thin</summary>
        public const string Thin       = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Thin.otf";
    }
}
