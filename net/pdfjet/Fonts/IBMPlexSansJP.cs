/*
 * IBMPlexSansJP.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
    /// <summary>
    /// The IBM Plex Sans JP fonts. Each constant is the path of a font file,
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
    public static class IBMPlexSansJP {
        /// <summary>IBM Plex Sans JP Bold</summary>
        public const string Bold       = "fonts/IBMPlexSansJP/IBMPlexSansJP-Bold.otf";

        /// <summary>
        /// IBM Plex Sans JP ExtraLight, which PDFjet does not carry from 9.1.7:
        /// a font file of its packages before, read when the file is there.
        /// </summary>
        [Obsolete("PDFjet carries the other weights of the family.")]
        public const string ExtraLight = "fonts/IBMPlexSansJP/IBMPlexSansJP-ExtraLight.ttf";

        /// <summary>
        /// IBM Plex Sans JP Light, which PDFjet does not carry from 9.1.7: a
        /// font file of its packages before, read when the file is there.
        /// </summary>
        [Obsolete("PDFjet carries the other weights of the family.")]
        public const string Light      = "fonts/IBMPlexSansJP/IBMPlexSansJP-Light.ttf";

        /// <summary>
        /// IBM Plex Sans JP Medium, which PDFjet does not carry from 9.1.7: a
        /// font file of its packages before, read when the file is there.
        /// </summary>
        [Obsolete("PDFjet carries the other weights of the family.")]
        public const string Medium     = "fonts/IBMPlexSansJP/IBMPlexSansJP-Medium.ttf";
        /// <summary>IBM Plex Sans JP Regular</summary>
        public const string Regular    = "fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf";
        /// <summary>IBM Plex Sans JP SemiBold</summary>
        public const string SemiBold   = "fonts/IBMPlexSansJP/IBMPlexSansJP-SemiBold.otf";

        /// <summary>
        /// IBM Plex Sans JP Text, which PDFjet does not carry from 9.1.7: a
        /// font file of its packages before, read when the file is there.
        /// </summary>
        [Obsolete("PDFjet carries the other weights of the family.")]
        public const string Text       = "fonts/IBMPlexSansJP/IBMPlexSansJP-Text.ttf";

        /// <summary>
        /// IBM Plex Sans JP Thin, which PDFjet does not carry from 9.1.7: a
        /// font file of its packages before, read when the file is there.
        /// </summary>
        [Obsolete("PDFjet carries the other weights of the family.")]
        public const string Thin       = "fonts/IBMPlexSansJP/IBMPlexSansJP-Thin.ttf";
    }
}
