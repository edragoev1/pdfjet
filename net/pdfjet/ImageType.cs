/*
 * ImageType.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// Used to specify the image type of an image.
/// Supported types: ImageType.JPEG, ImageType.PNG and ImageType.BMP
/// See the Image class for more information.
/// </summary>
public class ImageType {
    /// <summary>JPEG image.</summary>
    public static readonly int JPG = 0;
    /// <summary>PNG image.</summary>
    public static readonly int PNG = 1;
    /// <summary>BMP image.</summary>
    public static readonly int BMP = 2;
}
}   // End of namespace PDFjet.NET
