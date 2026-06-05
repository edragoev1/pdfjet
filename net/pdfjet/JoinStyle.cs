/**
 * JoinStyle.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

/**
 * Used to specify the join style when joining two lines.
 * See the Page and Line classes for more details.
 */
namespace PDFjet.NET {
public enum JoinStyle : Int32 {
    MITER = 0,
    ROUND,
    BEVEL
}
}   // End of namespace PDFjet.NET
