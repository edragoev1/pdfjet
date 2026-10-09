/*
 * XMLException.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>The document is not the XML that XMLParser reads; the message says where.</summary>
public sealed class XMLException : Exception {
    /// <summary>A document that is not the XML that XMLParser reads.</summary>
    /// <param name="message">what is wrong, and where.</param>
    public XMLException(string message) : base(message) {
    }
}
}   // End of namespace PDFjet.NET
