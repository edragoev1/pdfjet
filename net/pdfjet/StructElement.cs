/*
 * StructElement.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// A structure element of the structure tree that PDF writes for a tagged
/// document: the marked content it refers to.
/// </summary>
internal class StructElement {
    internal int objNumber;
    internal String structure = null;
    internal int pageObjNumber;
    internal int mcid = 0;
    internal String language = "en-US";
    internal String actualText = null;
    internal String altDescription = null;
    internal Annotation annotation = null;
}
}   // End of namespace PDFjet.NET
