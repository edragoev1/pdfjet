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
/// Supported types: ImageType.JPG, ImageType.PNG and ImageType.BMP
/// See the Image class for more information.
/// </summary>
public enum ImageType {
    /// <summary>JPEG image.</summary>
    JPG,
    /// <summary>PNG image.</summary>
    PNG,
    /// <summary>BMP image.</summary>
    BMP
}
}   // End of namespace PDFjet.NET
