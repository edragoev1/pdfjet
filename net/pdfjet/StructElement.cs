/*
 * StructElement.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// A structure element of the structure tree that PDF writes for a tagged
/// document: the marked content it refers to and its kids.
/// </summary>
internal class StructElement {
    internal int objNumber;
    internal String structure = null;
    internal int pageObjNumber;
    // The marked content this element refers to, or -1 for an element that
    // groups its kids, like a table row.
    internal int mcid = 0;
    // The attributes dictionary, like <</O /Table /Scope /Column>>, or null.
    internal String attributes = null;
    // The parent element, or null for a child of the Document element.
    internal StructElement parent = null;
    internal String language = "en-US";
    internal String actualText = null;
    internal String altDescription = null;
    internal Annotation annotation = null;
    internal List<StructElement> kids = new List<StructElement>();
}
}   // End of namespace PDFjet.NET
