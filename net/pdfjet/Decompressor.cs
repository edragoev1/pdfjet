/**
 * Decompressor.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System.IO;
using System.IO.Compression;

namespace PDFjet.NET {
class Decompressor {
    internal static byte[] Inflate(byte[] data) {
        using var outStream = new MemoryStream();
        using var inStream = new MemoryStream(data);
        using var zlib = new ZLibStream(inStream, CompressionMode.Decompress);
        zlib.CopyTo(outStream);
        return outStream.ToArray();
    }
}   // End of Decompressor.cs
}   // End of package PDFjet.NET