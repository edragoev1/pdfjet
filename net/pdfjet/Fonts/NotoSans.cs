/*
 * NotoSans.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
    /// <summary>
    /// The Noto Sans fonts, which PDFjet does not carry from 9.1.7: each
    /// constant is the path of a font file of the packages of PDFjet before
    /// 9.1.7, which the Font constructor that takes a font path reads when the
    /// file is there.
    /// </summary>
    [Obsolete("PDFjet carries IBM Plex Sans, which every constant of IBMPlexSans reads with nothing to download.")]
    public static class NotoSans {
        /// <summary>Noto Sans Black</summary>
        public const string Black            = "fonts/NotoSans/NotoSans-Black.ttf";
        /// <summary>Noto Sans BlackItalic</summary>
        public const string BlackItalic      = "fonts/NotoSans/NotoSans-BlackItalic.ttf";
        /// <summary>Noto Sans Bold</summary>
        public const string Bold             = "fonts/NotoSans/NotoSans-Bold.ttf";
        /// <summary>Noto Sans BoldItalic</summary>
        public const string BoldItalic       = "fonts/NotoSans/NotoSans-BoldItalic.ttf";
        /// <summary>Noto Sans ExtraBold</summary>
        public const string ExtraBold        = "fonts/NotoSans/NotoSans-ExtraBold.ttf";
        /// <summary>Noto Sans ExtraBoldItalic</summary>
        public const string ExtraBoldItalic  = "fonts/NotoSans/NotoSans-ExtraBoldItalic.ttf";
        /// <summary>Noto Sans ExtraLight</summary>
        public const string ExtraLight       = "fonts/NotoSans/NotoSans-ExtraLight.ttf";
        /// <summary>Noto Sans ExtraLightItalic</summary>
        public const string ExtraLightItalic = "fonts/NotoSans/NotoSans-ExtraLightItalic.ttf";
        /// <summary>Noto Sans Italic</summary>
        public const string Italic           = "fonts/NotoSans/NotoSans-Italic.ttf";
        /// <summary>Noto Sans Light</summary>
        public const string Light            = "fonts/NotoSans/NotoSans-Light.ttf";
        /// <summary>Noto Sans LightItalic</summary>
        public const string LightItalic      = "fonts/NotoSans/NotoSans-LightItalic.ttf";
        /// <summary>Noto Sans Medium</summary>
        public const string Medium           = "fonts/NotoSans/NotoSans-Medium.ttf";
        /// <summary>Noto Sans MediumItalic</summary>
        public const string MediumItalic     = "fonts/NotoSans/NotoSans-MediumItalic.ttf";
        /// <summary>Noto Sans Regular</summary>
        public const string Regular          = "fonts/NotoSans/NotoSans-Regular.ttf";
        /// <summary>Noto Sans SemiBold</summary>
        public const string SemiBold         = "fonts/NotoSans/NotoSans-SemiBold.ttf";
        /// <summary>Noto Sans SemiBoldItalic</summary>
        public const string SemiBoldItalic   = "fonts/NotoSans/NotoSans-SemiBoldItalic.ttf";
        /// <summary>Noto Sans Thin</summary>
        public const string Thin             = "fonts/NotoSans/NotoSans-Thin.ttf";
        /// <summary>Noto Sans ThinItalic</summary>
        public const string ThinItalic       = "fonts/NotoSans/NotoSans-ThinItalic.ttf";
    }
}
