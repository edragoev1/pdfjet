/*
 * IBMPlexSerif.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
    /// <summary>
    /// The IBM Plex Serif fonts. Each constant is the path of a font file,
    /// embedded as a subset of the glyphs a document draws; pass it to the Font
    /// constructor that takes a font path. PDFjet carries these fonts in
    /// itself: the path is read from the library when there is no such file.
    /// </summary>
    public static class IBMPlexSerif {
        /// <summary>IBM Plex Serif Bold</summary>
        public const string Bold           = "fonts/IBMPlexSerif/IBMPlexSerif-Bold.ttf";
        /// <summary>IBM Plex Serif BoldItalic</summary>
        public const string BoldItalic     = "fonts/IBMPlexSerif/IBMPlexSerif-BoldItalic.ttf";
        /// <summary>IBM Plex Serif Italic</summary>
        public const string Italic         = "fonts/IBMPlexSerif/IBMPlexSerif-Italic.ttf";
        /// <summary>IBM Plex Serif Light</summary>
        public const string Light          = "fonts/IBMPlexSerif/IBMPlexSerif-Light.ttf";
        /// <summary>IBM Plex Serif LightItalic</summary>
        public const string LightItalic    = "fonts/IBMPlexSerif/IBMPlexSerif-LightItalic.ttf";
        /// <summary>IBM Plex Serif Medium</summary>
        public const string Medium         = "fonts/IBMPlexSerif/IBMPlexSerif-Medium.ttf";
        /// <summary>IBM Plex Serif MediumItalic</summary>
        public const string MediumItalic   = "fonts/IBMPlexSerif/IBMPlexSerif-MediumItalic.ttf";
        /// <summary>IBM Plex Serif Regular</summary>
        public const string Regular        = "fonts/IBMPlexSerif/IBMPlexSerif-Regular.ttf";
        /// <summary>IBM Plex Serif SemiBold</summary>
        public const string SemiBold       = "fonts/IBMPlexSerif/IBMPlexSerif-SemiBold.ttf";
        /// <summary>IBM Plex Serif SemiBoldItalic</summary>
        public const string SemiBoldItalic = "fonts/IBMPlexSerif/IBMPlexSerif-SemiBoldItalic.ttf";
        /// <summary>IBM Plex Serif Text</summary>
        public const string Text           = "fonts/IBMPlexSerif/IBMPlexSerif-Text.ttf";
        /// <summary>IBM Plex Serif TextItalic</summary>
        public const string TextItalic     = "fonts/IBMPlexSerif/IBMPlexSerif-TextItalic.ttf";
        /// <summary>IBM Plex Serif Thin</summary>
        public const string Thin           = "fonts/IBMPlexSerif/IBMPlexSerif-Thin.ttf";
        /// <summary>IBM Plex Serif ThinItalic</summary>
        public const string ThinItalic     = "fonts/IBMPlexSerif/IBMPlexSerif-ThinItalic.ttf";
    }
}
