/*
 * IBMPlexMath.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
    /// <summary>
    /// The IBM Plex Math fonts, which PDFjet does not carry from 9.1.7: each
    /// constant is the path of a font file of the packages of PDFjet before
    /// 9.1.7, which the Font constructor that takes a font path reads when the
    /// file is there.
    /// </summary>
    [Obsolete("PDFjet carries IBM Plex Sans and Serif, whose math signs serve business documents.")]
    public static class IBMPlexMath {
        /// <summary>IBM Plex Math Regular</summary>
        public const string Regular = "fonts/IBMPlexMath/IBMPlexMath-Regular.ttf";
    }
}
