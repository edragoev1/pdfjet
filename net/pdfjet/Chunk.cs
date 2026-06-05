/**
 * Chunk.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
public class Chunk {
    internal UInt32 length;
    internal byte[] type;
    internal byte[] data;
    internal UInt32 crc;

    public byte[] GetData() {
        return this.data;
    }

    public void SetData(byte[] data) {
        this.data = data;
    }
}   // End of Chunk.cs
}   // End of namespace PDFjet.NET
