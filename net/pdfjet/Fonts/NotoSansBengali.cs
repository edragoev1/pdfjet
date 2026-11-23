/*
 * NotoSansBengali.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

namespace PDFjet.NET {
    /// <summary>
    /// The Noto Sans Bengali fonts. Each constant is the path of a font file,
    /// embedded as a subset of the glyphs a document draws; pass it to the Font
    /// constructor that takes a font path. These fonts are not in the NuGet
    /// package PDFjet: they are in the package PDFjet.Fonts.World, with
    /// PDFjet's fonts for Chinese, Japanese, Korean, Arabic, Hebrew, Thai and others,
    /// which a program that draws in them adds next to it, with
    /// <c>dotnet add package PDFjet.Fonts.World</c>, or in the project file
    /// <c>&lt;PackageReference Include="PDFjet.Fonts.World" Version="9.1.7" /&gt;</c>,
    /// the same version as PDFjet's. The path is read from the file of that
    /// path, else from the folder resources/fonts/world, as in the release
    /// packages on GitHub, else from that package, and a font found in none
    /// says what to add.
    /// PDFjet does not shape Bengali: these fonts are for TextBlock and TextLine,
    /// which draw the text as a shaping engine shapes it with the same font file,
    /// for Bengali and Assamese. Regular and Bold, from Noto's release, unmodified.
    /// </summary>
    public static class NotoSansBengali {
        /// <summary>Noto Sans Bengali Bold</summary>
        public const string Bold       = "fonts/NotoSansBengali/NotoSansBengali-Bold.otf";

        /// <summary>Noto Sans Bengali Regular</summary>
        public const string Regular    = "fonts/NotoSansBengali/NotoSansBengali-Regular.otf";
    }
}
