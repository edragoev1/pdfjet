/*
 * IBMPlexSans.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
    /// <summary>
    /// The IBM Plex Sans fonts. Each constant is the path of a font file,
    /// embedded as a subset of the glyphs a document draws; pass it to the Font
    /// constructor that takes a font path. PDFjet carries these fonts in
    /// itself: the path is read from the library when there is no such file.
    /// </summary>
    public static class IBMPlexSans {
        /// <summary>IBM Plex Sans BoldItalic</summary>
        public const string BoldItalic       = "fonts/IBMPlexSans/IBMPlexSans-BoldItalic.ttf";
        /// <summary>IBM Plex Sans Bold</summary>
        public const string Bold             = "fonts/IBMPlexSans/IBMPlexSans-Bold.ttf";

        /// <summary>
        /// IBM Plex Sans ExtraLightItalic, which PDFjet does not carry from
        /// 9.1.7: a font file of its packages before, read when the file is there.
        /// </summary>
        [Obsolete("PDFjet carries the other weights of the family.")]
        public const string ExtraLightItalic = "fonts/IBMPlexSans/IBMPlexSans-ExtraLightItalic.ttf";

        /// <summary>
        /// IBM Plex Sans ExtraLight, which PDFjet does not carry from 9.1.7: a
        /// font file of its packages before, read when the file is there.
        /// </summary>
        [Obsolete("PDFjet carries the other weights of the family.")]
        public const string ExtraLight       = "fonts/IBMPlexSans/IBMPlexSans-ExtraLight.ttf";
        /// <summary>IBM Plex Sans Italic</summary>
        public const string Italic           = "fonts/IBMPlexSans/IBMPlexSans-Italic.ttf";
        /// <summary>IBM Plex Sans LightItalic</summary>
        public const string LightItalic      = "fonts/IBMPlexSans/IBMPlexSans-LightItalic.ttf";
        /// <summary>IBM Plex Sans Light</summary>
        public const string Light            = "fonts/IBMPlexSans/IBMPlexSans-Light.ttf";
        /// <summary>IBM Plex Sans MediumItalic</summary>
        public const string MediumItalic     = "fonts/IBMPlexSans/IBMPlexSans-MediumItalic.ttf";
        /// <summary>IBM Plex Sans Medium</summary>
        public const string Medium           = "fonts/IBMPlexSans/IBMPlexSans-Medium.ttf";
        /// <summary>IBM Plex Sans Regular</summary>
        public const string Regular          = "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf";
        /// <summary>IBM Plex Sans SemiBoldItalic</summary>
        public const string SemiBoldItalic   = "fonts/IBMPlexSans/IBMPlexSans-SemiBoldItalic.ttf";
        /// <summary>IBM Plex Sans SemiBold</summary>
        public const string SemiBold         = "fonts/IBMPlexSans/IBMPlexSans-SemiBold.ttf";
        /// <summary>IBM Plex Sans TextItalic</summary>
        public const string TextItalic       = "fonts/IBMPlexSans/IBMPlexSans-TextItalic.ttf";
        /// <summary>IBM Plex Sans Text</summary>
        public const string Text             = "fonts/IBMPlexSans/IBMPlexSans-Text.ttf";
        /// <summary>IBM Plex Sans ThinItalic</summary>
        public const string ThinItalic       = "fonts/IBMPlexSans/IBMPlexSans-ThinItalic.ttf";
        /// <summary>IBM Plex Sans Thin</summary>
        public const string Thin             = "fonts/IBMPlexSans/IBMPlexSans-Thin.ttf";
    }
}
