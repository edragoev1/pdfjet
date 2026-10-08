/*
 * XMLException.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>The document is not the XML that XMLParser reads; the message says where.</summary>
internal sealed class XMLException : Exception {
    internal XMLException(string message) : base(message) {
    }
}
}   // End of namespace PDFjet.NET
