/*
 * SourceSerif4.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
    /// <summary>
    /// The Source Serif 4 fonts, which PDFjet does not carry from 9.1.7: each
    /// constant is the path of a font file of the packages of PDFjet before
    /// 9.1.7, which the Font constructor that takes a font path reads when the
    /// file is there.
    /// </summary>
    [Obsolete("PDFjet carries IBM Plex Serif, which every constant of IBMPlexSerif reads with nothing to download.")]
    public static class SourceSerif4 {
        /// <summary>Source Serif 4 Black</summary>
        public const string Black            = "fonts/SourceSerif4/SourceSerif4-Black.ttf";
        /// <summary>Source Serif 4 BlackItalic</summary>
        public const string BlackItalic      = "fonts/SourceSerif4/SourceSerif4-BlackItalic.ttf";
        /// <summary>Source Serif 4 Bold</summary>
        public const string Bold             = "fonts/SourceSerif4/SourceSerif4-Bold.ttf";
        /// <summary>Source Serif 4 BoldItalic</summary>
        public const string BoldItalic       = "fonts/SourceSerif4/SourceSerif4-BoldItalic.ttf";
        /// <summary>Source Serif 4 ExtraLight</summary>
        public const string ExtraLight       = "fonts/SourceSerif4/SourceSerif4-ExtraLight.ttf";
        /// <summary>Source Serif 4 ExtraLightItalic</summary>
        public const string ExtraLightItalic = "fonts/SourceSerif4/SourceSerif4-ExtraLightItalic.ttf";
        /// <summary>Source Serif 4 Italic</summary>
        public const string Italic           = "fonts/SourceSerif4/SourceSerif4-Italic.ttf";
        /// <summary>Source Serif 4 Light</summary>
        public const string Light            = "fonts/SourceSerif4/SourceSerif4-Light.ttf";
        /// <summary>Source Serif 4 LightItalic</summary>
        public const string LightItalic      = "fonts/SourceSerif4/SourceSerif4-LightItalic.ttf";
        /// <summary>Source Serif 4 Medium</summary>
        public const string Medium           = "fonts/SourceSerif4/SourceSerif4-Medium.ttf";
        /// <summary>Source Serif 4 MediumItalic</summary>
        public const string MediumItalic     = "fonts/SourceSerif4/SourceSerif4-MediumItalic.ttf";
        /// <summary>Source Serif 4 Regular</summary>
        public const string Regular          = "fonts/SourceSerif4/SourceSerif4-Regular.ttf";
        /// <summary>Source Serif 4 SemiBold</summary>
        public const string SemiBold         = "fonts/SourceSerif4/SourceSerif4-SemiBold.ttf";
        /// <summary>Source Serif 4 SemiBoldItalic</summary>
        public const string SemiBoldItalic   = "fonts/SourceSerif4/SourceSerif4-SemiBoldItalic.ttf";
    }
}
