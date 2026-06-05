/**
 * Compressor.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.IO.Compression;

namespace PDFjet.NET {
class Compressor {
    internal static byte[] Deflate(byte[] data) {
        return Deflate(data, 0, data.Length);
    }

    internal static byte[] Deflate(byte[] data, int off, int len) {
        if (data == null || data.Length == 0) return Array.Empty<byte>();
        using var ms = new MemoryStream();
        using (var zlib = new ZLibStream(ms, CompressionMode.Compress, leaveOpen: true)) {
            zlib.Write(data, off, len);
        }
        return ms.ToArray();
    }
}   // End of Compressor.cs
}   // End of package PDFjet.NET
