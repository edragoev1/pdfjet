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
    internal static byte[] inflate(byte[] data) {
        MemoryStream outStream = new MemoryStream();
        MemoryStream inStream = new MemoryStream(data, 2, data.Length - 6);
        DeflateStream dsStream = new DeflateStream(
                inStream, CompressionMode.Decompress, true);
        byte[] buf = new byte[4096];
        int count;
        while ((count = dsStream.Read(buf, 0, buf.Length)) > 0) {
            outStream.Write(buf, 0, count);
        }
        dsStream.Dispose();
        return outStream.ToArray();
    }
}   // End of Decompressor.cs
}   // End of package PDFjet.NET
