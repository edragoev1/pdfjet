/*
 * Compressor.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System.IO;
using System.IO.Compression;

namespace PDFjet.NET {
class Compressor {
    internal static byte[] Deflate(byte[] data) {
        return Deflate(data, 0, data.Length);
    }

    // The zlib stream of no bytes, as .NET 8 writes it. .NET 10's ZLibStream
    // writes nothing at all for no bytes, which is no zlib stream, and the
    // stream of an empty page, a /FlateDecode stream of length 0, could not
    // be decoded.
    private static readonly byte[] EMPTY = { 0x78, 0x9C, 0x03, 0x00, 0x00, 0x00, 0x00, 0x01 };

    internal static byte[] Deflate(byte[] data, int off, int len) {
        if (len == 0) {
            return (byte[]) EMPTY.Clone();
        }
        // Page content usually compresses to less than an eighth of its size.
        using var ms = new MemoryStream(len / 8 + 64);
        using (var zlib = new ZLibStream(ms, CompressionMode.Compress, leaveOpen: true)) {
            zlib.Write(data, off, len);
        }
        return ms.ToArray();
    }
}   // End of Compressor.cs
}   // End of package PDFjet.NET
