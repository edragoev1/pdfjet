/*
 * CascadiaMono.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

namespace PDFjet.NET {
    /// <summary>
    /// The Cascadia Mono fonts, Microsoft's monospace font, with all of Latin,
    /// Greek and Cyrillic in every face, the italic ones too. Each constant is
    /// the path of a font file, embedded as a subset of the glyphs a document
    /// draws; pass it to the Font constructor that takes a font path. PDFjet
    /// carries these fonts in itself: the path is read from the library when
    /// there is no such file.
    /// </summary>
    public static class CascadiaMono {
        /// <summary>Cascadia Mono Bold</summary>
        public const string Bold            = "fonts/CascadiaMono/CascadiaMono-Bold.otf";
        /// <summary>Cascadia Mono BoldItalic</summary>
        public const string BoldItalic      = "fonts/CascadiaMono/CascadiaMono-BoldItalic.otf";
        /// <summary>Cascadia Mono Italic</summary>
        public const string Italic          = "fonts/CascadiaMono/CascadiaMono-Italic.otf";
        /// <summary>Cascadia Mono Light</summary>
        public const string Light           = "fonts/CascadiaMono/CascadiaMono-Light.otf";
        /// <summary>Cascadia Mono LightItalic</summary>
        public const string LightItalic     = "fonts/CascadiaMono/CascadiaMono-LightItalic.otf";
        /// <summary>Cascadia Mono Regular</summary>
        public const string Regular         = "fonts/CascadiaMono/CascadiaMono-Regular.otf";
        /// <summary>Cascadia Mono SemiBold</summary>
        public const string SemiBold        = "fonts/CascadiaMono/CascadiaMono-SemiBold.otf";
        /// <summary>Cascadia Mono SemiBoldItalic</summary>
        public const string SemiBoldItalic  = "fonts/CascadiaMono/CascadiaMono-SemiBoldItalic.otf";
        /// <summary>Cascadia Mono SemiLight</summary>
        public const string SemiLight       = "fonts/CascadiaMono/CascadiaMono-SemiLight.otf";
        /// <summary>Cascadia Mono SemiLightItalic</summary>
        public const string SemiLightItalic = "fonts/CascadiaMono/CascadiaMono-SemiLightItalic.otf";
    }
}
