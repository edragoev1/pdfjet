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
    // The object numbers of the kids. A parent keeps the numbers and not the
    // kids, so that a page can write its elements and let go of them.
    internal List<int> kids = new List<int>();
    // The marked contents of an element that holds several of them, like a
    // paragraph whose words are drawn one at a time.
    internal List<int> mcids = new List<int>();
    // True for an element a drawable goes on adding to after the page it was
    // made on is written, like the Table of a table that runs over pages. It
    // is written when the document is completed; every other element is
    // written with its page and let go of.
    internal bool open = false;
}
}   // End of namespace PDFjet.NET
