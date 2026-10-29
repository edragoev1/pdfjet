/*
 * IBMPlexMono.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
    /// <summary>
    /// The IBM Plex Mono fonts, which PDFjet does not carry from 9.1.7: each
    /// constant is the path of a font file of pdfjet-fonts,
    /// https://github.com/edragoev1/pdfjet-fonts, which the Font constructor
    /// that takes a font path reads when the file is there.
    /// </summary>
    [Obsolete("PDFjet carries Cascadia Mono, which has all of Greek and of Cyrillic in every face, as IBM Plex Mono does not, and which every constant of CascadiaMono reads with nothing to download.")]
    public static class IBMPlexMono {
        /// <summary>IBM Plex Mono BoldItalic</summary>
        public const string BoldItalic       = "fonts/IBMPlexMono/IBMPlexMono-BoldItalic.ttf";
        /// <summary>IBM Plex Mono Bold</summary>
        public const string Bold             = "fonts/IBMPlexMono/IBMPlexMono-Bold.ttf";
        /// <summary>IBM Plex Mono ExtraLightItalic</summary>
        public const string ExtraLightItalic = "fonts/IBMPlexMono/IBMPlexMono-ExtraLightItalic.ttf";
        /// <summary>IBM Plex Mono ExtraLight</summary>
        public const string ExtraLight       = "fonts/IBMPlexMono/IBMPlexMono-ExtraLight.ttf";
        /// <summary>IBM Plex Mono Italic</summary>
        public const string Italic           = "fonts/IBMPlexMono/IBMPlexMono-Italic.ttf";
        /// <summary>IBM Plex Mono LightItalic</summary>
        public const string LightItalic      = "fonts/IBMPlexMono/IBMPlexMono-LightItalic.ttf";
        /// <summary>IBM Plex Mono Light</summary>
        public const string Light            = "fonts/IBMPlexMono/IBMPlexMono-Light.ttf";
        /// <summary>IBM Plex Mono MediumItalic</summary>
        public const string MediumItalic     = "fonts/IBMPlexMono/IBMPlexMono-MediumItalic.ttf";
        /// <summary>IBM Plex Mono Medium</summary>
        public const string Medium           = "fonts/IBMPlexMono/IBMPlexMono-Medium.ttf";
        /// <summary>IBM Plex Mono Regular</summary>
        public const string Regular          = "fonts/IBMPlexMono/IBMPlexMono-Regular.ttf";
        /// <summary>IBM Plex Mono SemiBoldItalic</summary>
        public const string SemiBoldItalic   = "fonts/IBMPlexMono/IBMPlexMono-SemiBoldItalic.ttf";
        /// <summary>IBM Plex Mono SemiBold</summary>
        public const string SemiBold         = "fonts/IBMPlexMono/IBMPlexMono-SemiBold.ttf";
        /// <summary>IBM Plex Mono TextItalic</summary>
        public const string TextItalic       = "fonts/IBMPlexMono/IBMPlexMono-TextItalic.ttf";
        /// <summary>IBM Plex Mono Text</summary>
        public const string Text             = "fonts/IBMPlexMono/IBMPlexMono-Text.ttf";
        /// <summary>IBM Plex Mono ThinItalic</summary>
        public const string ThinItalic       = "fonts/IBMPlexMono/IBMPlexMono-ThinItalic.ttf";
        /// <summary>IBM Plex Mono Thin</summary>
        public const string Thin             = "fonts/IBMPlexMono/IBMPlexMono-Thin.ttf";
    }
}
