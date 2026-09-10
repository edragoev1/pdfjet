/*
 * Chunk.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>A chunk of a PNG image.</summary>
public class Chunk {
    internal UInt32 length;
    internal byte[] type;
    internal byte[] data;
    internal UInt32 crc;

    /// <summary>Returns the chunk data.</summary>
    public byte[] GetData() {
        return this.data;
    }

    /// <summary>Sets the chunk data.</summary>
    public Chunk SetData(byte[] data) {
        this.data = data;
        return this;
    }
}   // End of Chunk.cs
}   // End of namespace PDFjet.NET
