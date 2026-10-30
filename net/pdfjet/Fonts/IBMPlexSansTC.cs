/*
 * IBMPlexSansTC.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
    /// <summary>
    /// The IBM Plex Sans TC fonts. Each constant is the path of a font file,
    /// embedded as a subset of the glyphs a document draws; pass it to the Font
    /// constructor that takes a font path. These fonts are not in the NuGet
    /// package PDFjet: they are in the package PDFjet.Fonts.CJK, which a
    /// program that draws in them adds next to it, with
    /// <c>dotnet add package PDFjet.Fonts.CJK</c>, or in the project file
    /// <c>&lt;PackageReference Include="PDFjet.Fonts.CJK" Version="9.1.7" /&gt;</c>,
    /// the same version as PDFjet's. The path is read from the file of that
    /// path, else from the folder resources/fonts/cjk, as in the release
    /// packages on GitHub, else from that package, and a font found in none
    /// says what to add.
    /// </summary>
    public static class IBMPlexSansTC {
        /// <summary>IBM Plex Sans TC Bold</summary>
        public const string Bold       = "fonts/IBMPlexSansTC/IBMPlexSansTC-Bold.otf";

        /// <summary>
        /// IBM Plex Sans TC ExtraLight, which PDFjet does not carry from 9.1.7:
        /// a font file of its packages before, read when the file is there.
        /// </summary>
        [Obsolete("PDFjet carries the other weights of the family.")]
        public const string ExtraLight = "fonts/IBMPlexSansTC/IBMPlexSansTC-ExtraLight.ttf";

        /// <summary>
        /// IBM Plex Sans TC Light, which PDFjet does not carry from 9.1.7: a
        /// font file of its packages before, read when the file is there.
        /// </summary>
        [Obsolete("PDFjet carries the other weights of the family.")]
        public const string Light      = "fonts/IBMPlexSansTC/IBMPlexSansTC-Light.ttf";

        /// <summary>
        /// IBM Plex Sans TC Medium, which PDFjet does not carry from 9.1.7: a
        /// font file of its packages before, read when the file is there.
        /// </summary>
        [Obsolete("PDFjet carries the other weights of the family.")]
        public const string Medium     = "fonts/IBMPlexSansTC/IBMPlexSansTC-Medium.ttf";
        /// <summary>IBM Plex Sans TC Regular</summary>
        public const string Regular    = "fonts/IBMPlexSansTC/IBMPlexSansTC-Regular.otf";
        /// <summary>IBM Plex Sans TC SemiBold</summary>
        public const string SemiBold   = "fonts/IBMPlexSansTC/IBMPlexSansTC-SemiBold.otf";

        /// <summary>
        /// IBM Plex Sans TC Text, which PDFjet does not carry from 9.1.7: a
        /// font file of its packages before, read when the file is there.
        /// </summary>
        [Obsolete("PDFjet carries the other weights of the family.")]
        public const string Text       = "fonts/IBMPlexSansTC/IBMPlexSansTC-Text.ttf";

        /// <summary>
        /// IBM Plex Sans TC Thin, which PDFjet does not carry from 9.1.7: a
        /// font file of its packages before, read when the file is there.
        /// </summary>
        [Obsolete("PDFjet carries the other weights of the family.")]
        public const string Thin       = "fonts/IBMPlexSansTC/IBMPlexSansTC-Thin.ttf";
    }
}
