/*
 * JetBrainsMono.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
    /// <summary>
    /// The JetBrains Mono fonts, which PDFjet does not carry from 9.1.7: each
    /// constant is the path of a font file of the packages of PDFjet before
    /// 9.1.7, which the Font constructor that takes a font path reads when the
    /// file is there.
    /// </summary>
    [Obsolete("PDFjet carries Cascadia Mono, which has all of Greek and of Cyrillic in every face, as JetBrains Mono does not, and which every constant of CascadiaMono reads with nothing to download.")]
    public static class JetBrainsMono {
        /// <summary>JetBrains Mono Bold</summary>
        public const string Bold             = "fonts/JetBrainsMono/JetBrainsMono-Bold.ttf";
        /// <summary>JetBrains Mono BoldItalic</summary>
        public const string BoldItalic       = "fonts/JetBrainsMono/JetBrainsMono-BoldItalic.ttf";
        /// <summary>JetBrains Mono ExtraLight</summary>
        public const string ExtraLight       = "fonts/JetBrainsMono/JetBrainsMono-ExtraLight.ttf";
        /// <summary>JetBrains Mono ExtraLightItalic</summary>
        public const string ExtraLightItalic = "fonts/JetBrainsMono/JetBrainsMono-ExtraLightItalic.ttf";
        /// <summary>JetBrains Mono Italic</summary>
        public const string Italic           = "fonts/JetBrainsMono/JetBrainsMono-Italic.ttf";
        /// <summary>JetBrains Mono Light</summary>
        public const string Light            = "fonts/JetBrainsMono/JetBrainsMono-Light.ttf";
        /// <summary>JetBrains Mono LightItalic</summary>
        public const string LightItalic      = "fonts/JetBrainsMono/JetBrainsMono-LightItalic.ttf";
        /// <summary>JetBrains Mono Medium</summary>
        public const string Medium           = "fonts/JetBrainsMono/JetBrainsMono-Medium.ttf";
        /// <summary>JetBrains Mono MediumItalic</summary>
        public const string MediumItalic     = "fonts/JetBrainsMono/JetBrainsMono-MediumItalic.ttf";
        /// <summary>JetBrains Mono Regular</summary>
        public const string Regular          = "fonts/JetBrainsMono/JetBrainsMono-Regular.ttf";
        /// <summary>JetBrains Mono SemiBold</summary>
        public const string SemiBold         = "fonts/JetBrainsMono/JetBrainsMono-SemiBold.ttf";
        /// <summary>JetBrains Mono SemiBoldItalic</summary>
        public const string SemiBoldItalic   = "fonts/JetBrainsMono/JetBrainsMono-SemiBoldItalic.ttf";
    }
}
